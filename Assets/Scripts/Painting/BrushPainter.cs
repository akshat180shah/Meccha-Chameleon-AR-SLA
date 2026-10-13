using UnityEngine;

public class BrushPainter : MonoBehaviour
{
    [SerializeField] private ARPlaceCube placementScript;
    [SerializeField] private PaintCanvas paintCanvas;
    [SerializeField] private float maxScreenDistance = 200f;

    private Camera arCamera;
    private Vector3[] cachedVertices;
    private Vector2[] cachedUVs;
    private Transform cachedMeshTransform;
    private bool meshDataReady = false;
    private GameObject lastChameleon = null;

    void Start()
    {
        arCamera = Camera.main;
    }

    void Update()
    {
        if (placementScript.CurrentChameleon == null)
        {
            meshDataReady = false;
            lastChameleon = null;
            return;
        }

        if (placementScript.CurrentChameleon != lastChameleon)
        {
            CacheMeshData();
            lastChameleon = placementScript.CurrentChameleon;
        }

        if (!meshDataReady) return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                Paint(touch.position);
        }
        else if (Input.GetMouseButton(0))
        {
            Paint(Input.mousePosition);
        }
    }

    void CacheMeshData()
    {
        meshDataReady = false;
        MeshFilter mf = placementScript.CurrentChameleon.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        cachedVertices = mf.sharedMesh.vertices;
        cachedUVs = mf.sharedMesh.uv;
        cachedMeshTransform = mf.transform;

        if (cachedUVs != null && cachedUVs.Length > 0)
        {
            meshDataReady = true;
        }
    }

    void Paint(Vector2 screenPos)
    {
        float closestDist = float.MaxValue;
        Vector2 bestUV = Vector2.zero;
        bool found = false;

        for (int i = 0; i < cachedVertices.Length; i++)
        {
            Vector3 worldPos = cachedMeshTransform.TransformPoint(cachedVertices[i]);
            Vector3 screenPos3D = arCamera.WorldToScreenPoint(worldPos);

            if (screenPos3D.z < 0) continue;

            float dist = Vector2.Distance(screenPos, new Vector2(screenPos3D.x, screenPos3D.y));

            if (dist < closestDist)
            {
                closestDist = dist;
                bestUV = cachedUVs[i];
                found = true;
            }
        }

        if (found && closestDist < maxScreenDistance)
        {
            paintCanvas.PaintAt(bestUV);
        }
    }
}