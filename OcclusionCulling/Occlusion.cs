using UnityEngine;
using UnityEngine.Rendering; // ShadowCastingMode için gerekli
using System.Collections.Generic;

public class Occlusion : MonoBehaviour
{
    [Header("Ayarlar")]
    public Camera cam;
    public LayerMask occlusionMask = ~0; // Hangi layer’lar engelleyici olsun
    public float checkInterval = 0.2f;   // Kontrol aralýðý
    public int maxRaycastHits = 4;       // RaycastNonAlloc buffer boyutu

    private List<Renderer> allRenderers = new List<Renderer>();
    private float timer;
    private RaycastHit[] rayHits;

    private static Occlusion _instance;
    public static Occlusion Instance => _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        // Sahnedeki tüm Renderer’larý al
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (Renderer rend in renderers)
        {
            if (!rend.CompareTag("RenderAlways"))
                rend.enabled = false; // Baþlangýçta tamamen görünmez
            allRenderers.Add(rend);
        }

        rayHits = new RaycastHit[maxRaycastHits];
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        Vector3 camPos = cam.transform.position;
        Vector3 camForward = cam.transform.forward;

        foreach (Renderer rend in allRenderers)
        {
            if (rend == null) continue;

            // Her zaman renderlenecek objeler
            if (rend.CompareTag("RenderAlways"))
            {
                rend.enabled = true;
                rend.shadowCastingMode = ShadowCastingMode.On;
                continue;
            }

            Vector3 dir = rend.bounds.center - camPos;

            // Kamera arkasýndaki objeler
            if (Vector3.Dot(camForward, dir) < 0)
            {
                SetShadowsOnly(rend);
                continue;
            }

            // Frustum testi
            if (!GeometryUtility.TestPlanesAABB(planes, rend.bounds))
            {
                SetShadowsOnly(rend);
                continue;
            }

            float dist = dir.magnitude;

            // Raycast ile engelleyici kontrol
            int hitCount = Physics.RaycastNonAlloc(camPos, dir.normalized, rayHits, dist, occlusionMask, QueryTriggerInteraction.Ignore);

            bool visible = true;
            if (hitCount > 0)
            {
                visible = false;
                for (int i = 0; i < hitCount; i++)
                {
                    if (rayHits[i].collider != null && rayHits[i].collider.gameObject == rend.gameObject)
                    {
                        visible = true;
                        break;
                    }
                }
            }

            if (visible)
            {
                rend.enabled = true;
                rend.shadowCastingMode = ShadowCastingMode.On;
            }
            else
            {
                SetShadowsOnly(rend);
            }
        }
    }

    private void SetShadowsOnly(Renderer rend)
    {
        rend.enabled = true; // Renderer açýk, ama sadece gölge býrakacak
        rend.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
    }

    // Yeni spawn objeleri eklemek için kullan
    public void RegisterNewRenderer(Renderer rend)
    {
        if (rend == null || allRenderers.Contains(rend))
            return;

        // Baþlangýçta tamamen görünmez
        if (!rend.CompareTag("RenderAlways"))
            rend.enabled = false;

        allRenderers.Add(rend);
    }
}
