using UnityEngine;
using UnityEngine.UI;

public class DrawingCanvas : MonoBehaviour
{
    // Varibile Globale

    public Slider slider;
    public RawImage canvasImage;
    public int textureWidth = 512;
    public int textureHeight = 512;

    // Variabile Locale

    private bool hasLast = false;
    private bool isDrawing;
    private bool isErasing = false;
    private int lastX,
        lastY;
    private int brushSize = 0;
    private Texture2D drawingTexture;
    private Color drawColor = Color.black;
    

    // Assign referinte si initializare
    void Start()
    {
        drawingTexture = new Texture2D(textureWidth, textureHeight);
        drawingTexture.filterMode = FilterMode.Point;
        ClearCanvas();
        canvasImage.texture = drawingTexture;
    }

    // Sistemul de desenare/stergere
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

        brushSize = (int)slider.value;

        Vector2 mousePos = Input.mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasImage.rectTransform,
            mousePos,
            null,
            out Vector2 pos
        );

        int texX = Mathf.FloorToInt(
            (pos.x + canvasImage.rectTransform.sizeDelta.x * 0.5f)
                / canvasImage.rectTransform.sizeDelta.x
                * textureWidth
        );
        int texY = Mathf.FloorToInt(
            (pos.y + canvasImage.rectTransform.sizeDelta.y * 0.5f)
                / canvasImage.rectTransform.sizeDelta.y
                * textureHeight
        );

        if (!hasLast)
        {
            if (isErasing)
                EraseCircle(texX, texY);
            else
                DrawCircle(texX, texY);
            hasLast = true;
        }
        else
        {
            int dx = texX - lastX;
            int dy = texY - lastY;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            int pasi = Mathf.CeilToInt(dist / brushSize * 1.5f);

            for (int i = 1; i <= pasi; i++)
            {
                float t = i / (float)pasi;
                int ix = Mathf.RoundToInt(Mathf.Lerp(lastX, texX, t));
                int iy = Mathf.RoundToInt(Mathf.Lerp(lastY, texY, t));
                if (isErasing)
                    EraseCircle(ix, iy);
                else
                    DrawCircle(ix, iy);
            }
        }

        drawingTexture.Apply();
        lastX = texX;
        lastY = texY;
    }

    public void StartErase()
    {
        isErasing = true;
    }

    public void StartDraw()
    {
        isErasing = false;
    }

    // Desenam un cerc (cand pensula cu culoarea neagra atinge panza)
    void DrawCircle(int cx, int cy)
    {
        int r = brushSize;

        for (int x = cx - r; x <= cx + r; x++)
        for (int y = cy - r; y <= cy + r; y++)
        {
            if (x < 0 || y < 0 || x >= textureWidth || y >= textureHeight)
                continue;
            if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                drawingTexture.SetPixel(x, y, drawColor);
        }
    }

    // Stergem un cerc (cand pensula cu culoarea alba atinge panza)
    void EraseCircle(int cx, int cy)
    {
        int r = brushSize;

        for (int x = cx - r; x <= cx + r; x++)
        for (int y = cy - r; y <= cy + r; y++)
        {
            if (x < 0 || y < 0 || x >= textureWidth || y >= textureHeight)
                continue;
            if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                drawingTexture.SetPixel(x, y, Color.clear);
        }
    }

    // Golim panza
    public void ClearCanvas()
    {
        var clearPixels = new Color32[textureWidth * textureHeight];
        for (int i = 0; i < clearPixels.Length; i++)
            clearPixels[i] = Color.clear;
        drawingTexture.SetPixels32(clearPixels);
        drawingTexture.Apply();
    }

    public Texture2D GetTexture()
    {
        return drawingTexture;
    }

    public void QuitApp()
    {
        Application.Quit();
    }
}
