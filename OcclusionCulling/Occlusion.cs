using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class Occlusion : MonoBehaviour
{
    [Header("Ayarlar")]
    public Camera cam;
    public LayerMask occlusionMask = ~0;
    public float checkInterval = 0.2f;
    public int maxRaycastHits = 4;

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

        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (Renderer rend in renderers)
        {
            if (!rend.CompareTag("RenderAlways"))
                rend.enabled = false;
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

            if (rend.CompareTag("RenderAlways"))
            {
                rend.enabled = true;
                rend.shadowCastingMode = ShadowCastingMode.On;
                continue;
            }

            Vector3 dir = rend.bounds.center - camPos;

            if (Vector3.Dot(camForward, dir) < 0)
            {
                SetShadowsOnly(rend);
                continue;
            }

            if (!GeometryUtility.TestPlanesAABB(planes, rend.bounds))
            {
                SetShadowsOnly(rend);
                continue;
            }

            float dist = dir.magnitude;

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
        rend.enabled = true;
        rend.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
    }

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
