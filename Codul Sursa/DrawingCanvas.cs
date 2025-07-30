using UnityEngine;
using UnityEngine.UI;

public class DrawingCanvas : MonoBehaviour
{
    public RawImage canvasImage;
    public int textureWidth = 512;
    public int textureHeight = 512;
    public int brushSize = 8;
    private Texture2D drawingTexture;
    private Color drawColor = Color.black;
    private bool isDrawing;

    private int lastX,
        lastY;
    private bool hasLast = false;

    void Start()
    {
        drawingTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        var clearPixels = new Color32[textureWidth * textureHeight];
        for (int i = 0; i < clearPixels.Length; i++)
            clearPixels[i] = Color.clear;
        drawingTexture.SetPixels32(clearPixels);
        drawingTexture.Apply();
        canvasImage.texture = drawingTexture;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDrawing = true;
            hasLast = false;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isDrawing = false;
        }
        if (!isDrawing)
            return;

        RectTransform rt = canvasImage.rectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt,
            Input.mousePosition,
            null,
            out localPoint
        );
        float px = localPoint.x + rt.rect.width * 0.5f;
        float py = localPoint.y + rt.rect.height * 0.5f;
        int texX = Mathf.FloorToInt(px / rt.rect.width * textureWidth);
        int texY = Mathf.FloorToInt(py / rt.rect.height * textureHeight);

        if (!hasLast)
        {
            DrawCircle(texX, texY);
            hasLast = true;
        }
        else
        {
            int dx = texX - lastX;
            int dy = texY - lastY;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            int steps = Mathf.CeilToInt(dist / brushSize * 1.5f);
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                int ix = Mathf.RoundToInt(Mathf.Lerp(lastX, texX, t));
                int iy = Mathf.RoundToInt(Mathf.Lerp(lastY, texY, t));
                DrawCircle(ix, iy);
            }
        }

        drawingTexture.Apply();

        lastX = texX;
        lastY = texY;
    }

    void DrawCircle(int cx, int cy)
    {
        int r = brushSize;
        for (int x = cx - r; x <= cx + r; x++)
        {
            for (int y = cy - r; y <= cy + r; y++)
            {
                if (x < 0 || y < 0 || x >= textureWidth || y >= textureHeight)
                    continue;
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                    drawingTexture.SetPixel(x, y, drawColor);
            }
        }
    }

    public Texture2D GetTexture()
    {
        return drawingTexture;
    }
}
