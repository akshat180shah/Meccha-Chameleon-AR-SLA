using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CustomColorPicker : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler
{
    [SerializeField] private PaintCanvas paintCanvas;
    [SerializeField] private Slider hueSlider;
    [SerializeField] private RawImage colorPickerArea;
    [SerializeField] private Image selectedColorPreview;

    private float hue = 0f;
    private Color selectedColor = Color.white;

    public void SetHue(float value)
    {
        hue = value;
        selectedColor = Color.HSVToRGB(hue, 1f, 1f);
        selectedColorPreview.color = selectedColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PickColor(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        PickColor(eventData);
    }

    private void PickColor(PointerEventData eventData)
    {
        RectTransform rect =
            colorPickerArea.rectTransform;

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        float saturation =
            Mathf.InverseLerp(
                rect.rect.xMin,
                rect.rect.xMax,
                localPoint.x
            );

        float brightness =
            Mathf.InverseLerp(
                rect.rect.yMin,
                rect.rect.yMax,
                localPoint.y
            );

        selectedColor =
            Color.HSVToRGB(
                hue,
                saturation,
                brightness
            );
        selectedColorPreview.color = selectedColor;
    }

    public void SelectColor()
    {
        if (paintCanvas != null)
        {
            try 
            {
                paintCanvas.SetBrushColor(selectedColor);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[ColorPicker] Error setting color: " + e.Message);
            }
        }
        else
        {
            Debug.LogError("[ColorPicker] PaintCanvas is not assigned!");
        }
        
        // Guarantee the UI closes even if there is an error above
        gameObject.SetActive(false);
    }
}