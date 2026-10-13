using UnityEngine;

public class PaintUIManager : MonoBehaviour
{
    [SerializeField] private PaintCanvas paintCanvas;

    public void SetRed()
    {
        paintCanvas.SetBrushColor(Color.red);
    }

    public void SetBlue()
    {
        paintCanvas.SetBrushColor(Color.blue);
    }

    public void SetGreen()
    {
        paintCanvas.SetBrushColor(Color.green);
    }

    public void SetYellow()
    {
        paintCanvas.SetBrushColor(Color.yellow);
    }

    public void SetWhite()
    {
        paintCanvas.SetBrushColor(Color.white);
    }

    public void SetBlack()
    {
        paintCanvas.SetBrushColor(Color.black);
    }
    public void SetBrushSize(float size)
    {
        paintCanvas.SetBrushSize(size);
    }
    public void EnableEraser()
    {
        paintCanvas.SetEraser(true);
    }
    public void Paint_Restore()
    {
        paintCanvas.Paint_Restore();
    }
}