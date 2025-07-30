using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PortraitEvaluator : MonoBehaviour
{
    public DrawingCanvas drawingCanvas;
    public Texture2D maskTexture;
    public Text resultText;

    public byte alphaThreshold = 1;
    public float weightF1 = 0.5f;
    public float weightCluster = 0.25f;
    public float weightShape = 0.25f;
    public float minBaseScore = 0.2f;

    public void Evaluate()
    {
        var userTex = drawingCanvas.GetTexture();
        int w = userTex.width,
            h = userTex.height;
        if (w != maskTexture.width || h != maskTexture.height)
        {
            resultText.text = "Eroare: rezoluții inegale";
            return;
        }

        byte[] userBin = GetBinaryAlpha(userTex);
        byte[] maskBin = GetBinaryAlpha(maskTexture);

        var faceRect = ComputeBoundingBox(userBin, w, h);
        if (faceRect.width == 0)
        {
            resultText.text = "Nicio linie trasă!";
            return;
        }
        byte[] centered = CenterBinary(userBin, w, h, faceRect);

        int inside = 0,
            outside = 0,
            maskCount = 0;
        for (int i = 0; i < centered.Length; i++)
        {
            bool m = maskBin[i] != 0,
                u = centered[i] != 0;
            if (m)
                maskCount++;
            if (u && m)
                inside++;
            if (u && !m)
                outside++;
        }
        if (maskCount == 0)
        {
            resultText.text = "Masca goală!";
            return;
        }
        double recall = (double)inside / maskCount,
            precision = (inside + outside) > 0 ? (double)inside / (inside + outside) : 1,
            f1 = (precision + recall) > 0 ? 2 * precision * recall / (precision + recall) : 0;

        double clusterCoef = ComputeClusterCoefficient(centered, w, h);

        var maskEdges = ExtractEdges(maskBin, w, h);
        var userEdges = ExtractEdges(centered, w, h);
        double avgDist = ComputeAvgEdgeDistance(userEdges, maskEdges, w, h, 20),
            shapeScore = 1.0 / (1.0 + (avgDist / Math.Max(w, h)));

        double baseScore = Math.Max(
            minBaseScore,
            weightF1 * f1 + weightCluster * clusterCoef + weightShape * shapeScore
        );
        float finalScore = 100f * (float)baseScore;

        resultText.text = $"Scor: {finalScore:F1}%";
    }

    byte[] GetBinaryAlpha(Texture2D tex)
    {
        var px = tex.GetPixels32();
        byte[] bin = new byte[px.Length];
        for (int i = 0; i < px.Length; i++)
            bin[i] = (px[i].a > alphaThreshold ? (byte)1 : (byte)0);
        return bin;
    }

    RectInt ComputeBoundingBox(byte[] bin, int w, int h)
    {
        int x0 = w,
            x1 = -1,
            y0 = h,
            y1 = -1;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            if (bin[y * w + x] != 0)
            {
                x0 = Math.Min(x0, x);
                x1 = Math.Max(x1, x);
                y0 = Math.Min(y0, y);
                y1 = Math.Max(y1, y);
            }
        return x1 < x0 ? new RectInt(0, 0, 0, 0) : new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
    }

    byte[] CenterBinary(byte[] bin, int w, int h, RectInt r)
    {
        byte[] dst = new byte[bin.Length];
        int cx = w / 2,
            cy = h / 2,
            rx = r.x + r.width / 2,
            ry = r.y + r.height / 2;
        int sx = cx - rx,
            sy = cy - ry;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int ox = x - sx,
                oy = y - sy;
            dst[y * w + x] = (ox >= 0 && ox < w && oy >= 0 && oy < h) ? bin[oy * w + ox] : (byte)0;
        }
        return dst;
    }

    double ComputeClusterCoefficient(byte[] bin, int w, int h)
    {
        var vis = new bool[bin.Length];
        int total = 0,
            maxC = 0;
        int[,] d =
        {
            { 1, 0 },
            { -1, 0 },
            { 0, 1 },
            { 0, -1 },
        };
        var q = new Queue<int>();
        for (int i = 0; i < bin.Length; i++)
            if (bin[i] != 0)
                total++;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = y * w + x;
            if (!vis[i] && bin[i] != 0)
            {
                vis[i] = true;
                q.Enqueue(i);
                int cnt = 0;
                while (q.Count > 0)
                {
                    int c = q.Dequeue();
                    cnt++;
                    int cx = c % w,
                        cy = c / w;
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = cx + d[k, 0],
                            ny = cy + d[k, 1],
                            ni = ny * w + nx;
                        if (nx >= 0 && nx < w && ny >= 0 && ny < h && !vis[ni] && bin[ni] != 0)
                        {
                            vis[ni] = true;
                            q.Enqueue(ni);
                        }
                    }
                }
                maxC = Math.Max(maxC, cnt);
            }
        }
        return total > 0 ? (double)maxC / total : 1.0;
    }

    List<Vector2Int> ExtractEdges(byte[] bin, int w, int h)
    {
        var edges = new List<Vector2Int>();
        for (int y = 1; y < h - 1; y++)
        for (int x = 1; x < w - 1; x++)
        {
            if (bin[y * w + x] == 0)
                continue;
            if (
                bin[(y - 1) * w + x] == 0
                || bin[(y + 1) * w + x] == 0
                || bin[y * w + x - 1] == 0
                || bin[y * w + x + 1] == 0
            )
                edges.Add(new Vector2Int(x, y));
        }
        return edges;
    }

    double ComputeAvgEdgeDistance(
        List<Vector2Int> userEdges,
        List<Vector2Int> maskEdges,
        int w,
        int h,
        int maxR
    )
    {
        double sum = 0;
        if (userEdges.Count == 0)
            return maxR;
        foreach (var ue in userEdges)
        {
            double best = maxR * maxR;
            foreach (var me in maskEdges)
            {
                int dx = me.x - ue.x,
                    dy = me.y - ue.y;
                double d2 = dx * dx + dy * dy;
                if (d2 < best)
                    best = d2;
            }
            sum += Math.Sqrt(best);
        }
        return sum / userEdges.Count;
    }
}
