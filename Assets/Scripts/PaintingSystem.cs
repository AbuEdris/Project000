using UnityEngine;
using UnityEngine.UI;

public class PaintingSystem : MonoBehaviour
{
    [SerializeField] private RawImage canvasImage;
    [SerializeField] private Texture2D brushTexture;
    private RenderTexture renderTexture;
    private Vector2 lastPos;

    private void Start()
    {
        renderTexture = new RenderTexture(512, 512, 0);
        canvasImage.texture = renderTexture;
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector2 mousePos = Input.mousePosition;
            if (lastPos != Vector2.zero)
            {
                DrawLine(lastPos, mousePos);
            }
            lastPos = mousePos;
        }
        else
        {
            lastPos = Vector2.zero;
        }
    }

    private void DrawLine(Vector2 start, Vector2 end)
    {
        // Simplified drawing logic (use a shader or sprite for real implementation)
        Graphics.Blit(brushTexture, renderTexture);
        // Analyze drawing for effects (e.g., color dominance)
        AnalyzeDrawing();
    }

    private void AnalyzeDrawing()
    {
        // Example: Check dominant color to trigger effects
        Texture2D tex = ToTexture2D(renderTexture);
        Color dominantColor = GetDominantColor(tex);
        if (dominantColor.r > 0.7f) // Red dominant
        {
            CreatePortal();
        }
    }

    private Texture2D ToTexture2D(RenderTexture rt)
    {
        Texture2D tex = new Texture2D(rt.width, rt.height);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        return tex;
    }

    private Color GetDominantColor(Texture2D tex)
    {
        Color[] pixels = tex.GetPixels();
        float r = 0, g = 0, b = 0;
        foreach (Color pixel in pixels)
        {
            r += pixel.r;
            g += pixel.g;
            b += pixel.b;
        }
        int count = pixels.Length;
        return new Color(r / count, g / count, b / count);
    }

    private void CreatePortal()
    {
        // Instantiate portal prefab at canvas position
        Debug.Log("Portal Created!");
    }
}