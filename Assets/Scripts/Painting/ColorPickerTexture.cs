using UnityEngine;
using UnityEngine.UI;

public class ColorPickerTexture : MonoBehaviour
{
    [SerializeField] private RawImage colorPickerArea;
    [SerializeField] private Slider hueSlider;

    private void Start()
    {
        CreateGradient(hueSlider.value);
    }

    public void UpdateHue(float hue)
    {
        CreateGradient(hue);
    }

    private void CreateGradient(float hue)
    {
        int width = 256;
        int height = 256;

        Texture2D texture = new Texture2D(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float saturation = (float)x / (width - 1);
                float brightness = (float)y / (height - 1);

                Color color =
                    Color.HSVToRGB(hue, saturation, brightness);

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        colorPickerArea.texture = texture;
    }
}