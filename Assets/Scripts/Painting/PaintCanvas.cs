using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public struct PaintStroke : INetworkSerializable
{
    public Vector2 uv;
    public Color color;
    public float size;
    public bool isEraser;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref uv);
        serializer.SerializeValue(ref color);
        serializer.SerializeValue(ref size);
        serializer.SerializeValue(ref isEraser);
    }
}

public class PaintCanvas : MonoBehaviour
{
    [SerializeField] private ARPlaceCube placementScript;
    [SerializeField] private Texture2D originalTexture;
    [SerializeField] private RenderTexture paintRT;
    [SerializeField] private RenderTexture paintRT_Temp;
    [SerializeField] private Material brushMaterial;

    private bool initialized = false;
    
    // Stroke History for Network Sync
    public List<PaintStroke> strokeHistory = new List<PaintStroke>();
    private Vector2 lastRecordedUV = new Vector2(-1, -1);

    public void InitializeCanvas()
    {
        if (initialized || placementScript.CurrentChameleon == null) return;

        MeshRenderer[] allRenderers = placementScript.CurrentChameleon.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer rend in allRenderers)
        {
            rend.material.SetTexture("_BaseMap", paintRT);
        }

        // Copy original texture into RenderTexture (This erases everything back to default!)
        Graphics.Blit(originalTexture, paintRT);
        brushMaterial.SetTexture("_OriginalTex", originalTexture);

        initialized = true;
    }

    void Update()
    {
        if (placementScript.CurrentChameleon == null)
        {
            initialized = false;
            return;
        }

        InitializeCanvas();
    }

    public void PaintAt(Vector2 uv)
    {
        brushMaterial.SetVector("_BrushPos", new Vector4(uv.x, uv.y, 0, 0));
        brushMaterial.SetFloat("_BrushEnabled", 1);
        brushMaterial.SetTexture("_MainTex", paintRT);

        Graphics.Blit(paintRT, paintRT_Temp, brushMaterial);
        Graphics.CopyTexture(paintRT_Temp, paintRT);

        brushMaterial.SetFloat("_BrushEnabled", 0);

        // RECORD STROKE (Only if it moved enough to save network bandwidth)
        if (Vector2.Distance(uv, lastRecordedUV) > 0.01f && strokeHistory.Count < 500)
        {
            strokeHistory.Add(new PaintStroke 
            { 
                uv = uv, 
                color = brushMaterial.GetColor("_BrushColor"), 
                size = brushMaterial.GetFloat("_BrushSize"), 
                isEraser = brushMaterial.GetFloat("_EraserEnabled") > 0.5f 
            });
            lastRecordedUV = uv;
        }
    }

    public void ReplayStrokes(PaintStroke[] strokes)
    {
        if (strokes == null || strokes.Length == 0) return;
        
        foreach (var stroke in strokes)
        {
            brushMaterial.SetColor("_BrushColor", stroke.color);
            brushMaterial.SetFloat("_BrushSize", stroke.size);
            brushMaterial.SetFloat("_EraserEnabled", stroke.isEraser ? 1 : 0);
            
            brushMaterial.SetVector("_BrushPos", new Vector4(stroke.uv.x, stroke.uv.y, 0, 0));
            brushMaterial.SetFloat("_BrushEnabled", 1);
            brushMaterial.SetTexture("_MainTex", paintRT);

            Graphics.Blit(paintRT, paintRT_Temp, brushMaterial);
            Graphics.CopyTexture(paintRT_Temp, paintRT);
        }
        brushMaterial.SetFloat("_BrushEnabled", 0);
    }

    public void SetBrushColor(Color color)
    {
        brushMaterial.SetColor("_BrushColor", color);
        brushMaterial.SetFloat("_EraserEnabled", 0);
    }

    public void SetBrushSize(float size)
    {
        brushMaterial.SetFloat("_BrushSize", size);
    }

    public void SetEraser(bool enabled)
    {
        brushMaterial.SetFloat("_EraserEnabled", enabled ? 1 : 0);
    }

    public void Paint_Restore()
    {
        if (placementScript.CurrentChameleon == null) return;
        Graphics.Blit(originalTexture, paintRT);
        brushMaterial.SetFloat("_EraserEnabled", 0);
        
        strokeHistory.Clear();
        lastRecordedUV = new Vector2(-1, -1);
    }

    private string debugMsg = "Paint System Ready";

    void OnGUI()
    {
        if (debugMsg != "Paint System Ready")
        {
            GUI.color = Color.red;
            GUI.skin.label.fontSize = 35;
            GUI.Label(new Rect(20, 100, Screen.width - 40, 500), "ERROR: " + debugMsg);
        }
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            if (logString.Contains("NullReference") || logString.Contains("Paint"))
            {
                debugMsg = logString;
            }
        }
    }
}