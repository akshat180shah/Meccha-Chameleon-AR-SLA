using UnityEngine;

public class ModeManager : MonoBehaviour
{
    [SerializeField] private ARPlaceCube placementScript;
    [SerializeField] private BrushPainter brushPainter;
    [SerializeField] private GameFlowManager gameFlowManager;

    void Start()
    {
        RotateMode();
    }

    public void RotateMode()
    {
        placementScript.canRotate = true;
        if (brushPainter != null) brushPainter.enabled = false;
    }

    public void PaintMode()
    {
        placementScript.canRotate = false;
        if (brushPainter != null) brushPainter.enabled = true;
    }

    public void DoneMode()
    {
        if (brushPainter != null) brushPainter.enabled = false;
        placementScript.canRotate = false;

        // MULTIPLAYER FIX: Call the new network sync method instead of starting the turn locally!
        gameFlowManager.OnDoneClicked();
    }
}