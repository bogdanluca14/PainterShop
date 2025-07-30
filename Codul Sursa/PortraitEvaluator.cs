using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PortraitEvaluatorOptimized : MonoBehaviour
{
    public DrawingCanvas drawingCanvas;
    public Texture2D maskTexture;
    public Text resultText;

    public byte alphaThreshold = 128;
    public float gaussianBlurRadius = 1.5f;

    [Range(0f, 1f)]
    public float accWeight = 0.35f;

    [Range(0f, 1f)]
    public float covWeight = 0.25f;

    [Range(0f, 1f)]
    public float shpWeight = 0.20f;

    [Range(0f, 1f)]
    public float quaWeight = 0.15f;

    [Range(0f, 1f)]
    public float proWeight = 0.05f;

    public float maxAreaMult = 2.5f;
    public float minCoverageLim = 0.15f;
    public float fragmentationPen = 0.7f;
    public float fillRatioPen = 0.8f;

    private struct EvalMetrics
    {
        public int userPixels,
            maskPixels,
            overlap,
            outsideMask;
        public float precision,
            recall,
            f1Score,
            areaRatio;
        public Vector2 userCenter,
            maskCenter;
        public Bounds userBounds,
            maskBounds;
    }

    private struct ShapeDescriptor
    {
        public float aspectRatio,
            compactness,
            elongation;
        public Vector2 principalAxis;
        public float[] moments;
    }

    // Evaluam portretul
    public void Evaluate()
    {
        if (!ValidateInput())
            return;

        var userTexture = drawingCanvas.GetTexture();
        var processedData = PreprocessImages(userTexture, maskTexture);

        if (processedData.userBinary == null || processedData.maskBinary == null)
        {
            return;
        }

        var metrics = ComputeMetrics(processedData);

        if (DetectFace(metrics, processedData))
        {
            float faceScore = Mathf.Max(5f, 20f / metrics.areaRatio);
            resultText.text = $"Scor: {faceScore:F1}% (Fata detectata)";
            return;
        }

        float finalScore = CalculateFinalScore(metrics, processedData);
        resultText.text = $"Scor: {finalScore:F1}%";
    }

    // Validarea pozei de pe care se va efectua portretul
    private bool ValidateInput()
    {
        if (drawingCanvas == null || maskTexture == null || resultText == null)
        {
            return false;
        }

        var userTex = drawingCanvas.GetTexture();
        if (userTex.width != maskTexture.width || userTex.height != maskTexture.height)
        {
            return false;
        }

        return true;
    }

    // Preprocesarea imaginii Input
    private (byte[] userBinary, byte[] maskBinary, int width, int height) PreprocessImages(
        Texture2D userTex,
        Texture2D maskTex
    )
    {
        int w = userTex.width,
            h = userTex.height;

        var userBinary = ExtractBinaryWithNoise(userTex, w, h);
        var maskBinary = ExtractBinary(maskTex, w, h);

        userBinary = MorphologicalClean(userBinary, w, h);
        userBinary = AlignDrawings(userBinary, maskBinary, w, h);

        return (userBinary, maskBinary, w, h);
    }

    // Extragem binar din imagine cu noise
    private byte[] ExtractBinaryWithNoise(Texture2D tex, int w, int h)
    {
        var pixels = tex.GetPixels32();
        var binary = new byte[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            var pixel = pixels[i];
            int intensity = (pixel.r + pixel.g + pixel.b) / 3;
            binary[i] = (byte)(pixel.a > alphaThreshold && intensity < 200 ? 1 : 0);
        }

        return binary;
    }

    // Extragem binar din imagine
    private byte[] ExtractBinary(Texture2D tex, int w, int h)
    {
        var pixels = tex.GetPixels32();
        var binary = new byte[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            binary[i] = (byte)(pixels[i].a > alphaThreshold ? 1 : 0);
        }

        return binary;
    }

    // Stergem noise-ul mic
    private byte[] MorphologicalClean(byte[] binary, int w, int h)
    {
        binary = MorphologicalOpen(binary, w, h, 2);
        binary = MorphologicalClose(binary, w, h, 3);
        return binary;
    }

    // Dilatam erodarea
    private byte[] MorphologicalOpen(byte[] binary, int w, int h, int kernelSize)
    {
        var eroded = MorphologicalErode(binary, w, h, kernelSize);
        return MorphologicalDilate(eroded, w, h, kernelSize);
    }

    // Erodam dilatarea
    private byte[] MorphologicalClose(byte[] binary, int w, int h, int kernelSize)
    {
        var dilated = MorphologicalDilate(binary, w, h, kernelSize);
        return MorphologicalErode(dilated, w, h, kernelSize);
    }

    // Erodare (stergem mici imperfectiuni)
    private byte[] MorphologicalErode(byte[] binary, int w, int h, int kernelSize)
    {
        var result = new byte[binary.Length];
        int radius = kernelSize / 2;

        for (int y = radius; y < h - radius; y++)
        for (int x = radius; x < w - radius; x++)
        {
            bool allSet = true;
            for (int ky = -radius; ky <= radius && allSet; ky++)
            for (int kx = -radius; kx <= radius && allSet; kx++)
            {
                if (binary[(y + ky) * w + (x + kx)] == 0)
                    allSet = false;
            }
            result[y * w + x] = (byte)(allSet ? 1 : 0);
        }

        return result;
    }

    // Dilatare
    private byte[] MorphologicalDilate(byte[] binary, int w, int h, int kernelSize)
    {
        var result = new byte[binary.Length];
        int radius = kernelSize / 2;

        for (int y = radius; y < h - radius; y++)
        for (int x = radius; x < w - radius; x++)
        {
            bool anySet = false;
            for (int ky = -radius; ky <= radius && !anySet; ky++)
            for (int kx = -radius; kx <= radius && !anySet; kx++)
            {
                if (binary[(y + ky) * w + (x + kx)] != 0)
                    anySet = true;
            }
            result[y * w + x] = (byte)(anySet ? 1 : 0);
        }

        return result;
    }

    // Suprapunem imaginile
    private byte[] AlignDrawings(byte[] userBinary, byte[] maskBinary, int w, int h)
    {
        var userCenter = CntOfMass(userBinary, w, h);
        var maskCenter = CntOfMass(maskBinary, w, h);

        if (userCenter == Vector2.zero || maskCenter == Vector2.zero)
            return userBinary;

        float bestScore = 0f;
        byte[] bestAlignment = userBinary;

        var scales = new float[] { 0.7f, 0.8f, 0.9f, 1.0f, 1.1f, 1.2f, 1.3f };
        var rotations = new float[] { -15f, -10f, -5f, 0f, 5f, 10f, 15f };

        foreach (var scale in scales)
        foreach (var rotation in rotations)
        {
            var aligned = TransformBinary(
                userBinary,
                w,
                h,
                userCenter,
                maskCenter,
                scale,
                rotation
            );
            var score = ComputeScoreAlig(aligned, maskBinary, w, h);

            if (score > bestScore)
            {
                bestScore = score;
                bestAlignment = aligned;
            }
        }

        return bestAlignment;
    }

    // Gasim punctul care este centrul de greutate al imaginii
    private Vector2 CntOfMass(byte[] binary, int w, int h)
    {
        float cx = 0f,
            cy = 0f,
            total = 0f;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (binary[y * w + x] != 0)
            {
                cx += x;
                cy += y;
                total += 1f;
            }
        }

        return total > 0 ? new Vector2(cx / total, cy / total) : Vector2.zero;
    }

    // Transformam din binar
    private byte[] TransformBinary(
        byte[] binary,
        int w,
        int h,
        Vector2 fromCenter,
        Vector2 toCenter,
        float scale,
        float rotationDeg
    )
    {
        var result = new byte[binary.Length];
        var rotation = rotationDeg * Mathf.Deg2Rad;
        var cos = Mathf.Cos(rotation);
        var sin = Mathf.Sin(rotation);

        var offset = toCenter - fromCenter;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float dx = x - fromCenter.x;
            float dy = y - fromCenter.y;

            float nx = (dx * cos - dy * sin) * scale + fromCenter.x + offset.x;
            float ny = (dx * sin + dy * cos) * scale + fromCenter.y + offset.y;

            int ix = Mathf.RoundToInt(nx);
            int iy = Mathf.RoundToInt(ny);

            if (ix >= 0 && ix < w && iy >= 0 && iy < h)
            {
                result[y * w + x] = binary[iy * w + ix];
            }
        }

        return result;
    }

    // Calculam scorul de precizie avand Inputul si Outputul
    private float ComputeScoreAlig(byte[] userBinary, byte[] maskBinary, int w, int h)
    {
        int overlap = 0,
            userTotal = 0,
            maskTotal = 0;

        for (int i = 0; i < userBinary.Length; i++)
        {
            bool user = userBinary[i] != 0;
            bool mask = maskBinary[i] != 0;

            if (user)
                userTotal++;
            if (mask)
                maskTotal++;
            if (user && mask)
                overlap++;
        }

        if (userTotal == 0 || maskTotal == 0)
            return 0f;

        float precision = (float)overlap / userTotal;
        float recall = (float)overlap / maskTotal;

        return precision * recall;
    }

    // Efectuam calcule metrice
    private EvalMetrics ComputeMetrics(
        (byte[] userBinary, byte[] maskBinary, int width, int height) data
    )
    {
        var metrics = new EvalMetrics();
        int w = data.width,
            h = data.height;

        for (int i = 0; i < data.userBinary.Length; i++)
        {
            bool user = data.userBinary[i] != 0;
            bool mask = data.maskBinary[i] != 0;

            if (user)
                metrics.userPixels++;
            if (mask)
                metrics.maskPixels++;
            if (user && mask)
                metrics.overlap++;
            if (user && !mask)
                metrics.outsideMask++;
        }

        if (metrics.userPixels > 0)
        {
            metrics.precision = (float)metrics.overlap / metrics.userPixels;
        }

        if (metrics.maskPixels > 0)
        {
            metrics.recall = (float)metrics.overlap / metrics.maskPixels;
            metrics.areaRatio = (float)metrics.userPixels / metrics.maskPixels;
        }

        if (metrics.precision + metrics.recall > 0)
        {
            metrics.f1Score =
                2 * metrics.precision * metrics.recall / (metrics.precision + metrics.recall);
        }

        metrics.userCenter = CntOfMass(data.userBinary, w, h);
        metrics.maskCenter = CntOfMass(data.maskBinary, w, h);
        metrics.userBounds = ComputeBounds(data.userBinary, w, h);
        metrics.maskBounds = ComputeBounds(data.maskBinary, w, h);

        return metrics;
    }

    // Calculam margini
    private Bounds ComputeBounds(byte[] binary, int w, int h)
    {
        int minX = w,
            maxX = -1,
            minY = h,
            maxY = -1;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (binary[y * w + x] != 0)
            {
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < minX)
            return new Bounds();

        var center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0);
        var size = new Vector3(maxX - minX + 1, maxY - minY + 1, 0);

        return new Bounds(center, size);
    }

    // Detectam (posibila fata) - excludem astfel cazuri clare
    private bool DetectFace(
        EvalMetrics metrics,
        (byte[] userBinary, byte[] maskBinary, int width, int height) data
    )
    {
        if (metrics.areaRatio > maxAreaMult)
            return true;

        var components = FindConnectedComponents(data.userBinary, data.width, data.height);
        if (components.Count > 5)
            return true;

        if (metrics.precision < 0.2f && metrics.userPixels > metrics.maskPixels * 0.3f)
            return true;

        float fillRatio = ComputeFillRatio(data.userBinary, data.width, data.height);
        if (fillRatio > 0.7f && metrics.areaRatio > 1.5f)
            return true;

        float outsideRatio =
            metrics.userPixels > 0 ? (float)metrics.outsideMask / metrics.userPixels : 0f;
        if (outsideRatio > 0.6f)
            return true;

        return false;
    }

    private List<List<Vector2Int>> FindConnectedComponents(byte[] binary, int w, int h)
    {
        var visited = new bool[binary.Length];
        var components = new List<List<Vector2Int>>();
        var directions = new Vector2Int[]
        {
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, 1),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1),
            new Vector2Int(1, -1),
            new Vector2Int(1, 0),
            new Vector2Int(1, 1),
        };

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int idx = y * w + x;
            if (binary[idx] != 0 && !visited[idx])
            {
                var component = new List<Vector2Int>();
                var stack = new Stack<Vector2Int>();
                stack.Push(new Vector2Int(x, y));
                visited[idx] = true;

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    component.Add(current);

                    foreach (var dir in directions)
                    {
                        var next = current + dir;
                        int nextIdx = next.y * w + next.x;

                        if (
                            next.x >= 0
                            && next.x < w
                            && next.y >= 0
                            && next.y < h
                            && !visited[nextIdx]
                            && binary[nextIdx] != 0
                        )
                        {
                            visited[nextIdx] = true;
                            stack.Push(next);
                        }
                    }
                }

                if (component.Count > 10)
                {
                    components.Add(component);
                }
            }
        }

        return components;
    }

    private float ComputeFillRatio(byte[] binary, int w, int h)
    {
        int totalPixels = 0,
            edgePixels = 0;

        for (int y = 1; y < h - 1; y++)
        for (int x = 1; x < w - 1; x++)
        {
            if (binary[y * w + x] == 0)
                continue;

            totalPixels++;

            bool isEdge = false;
            for (int dy = -1; dy <= 1 && !isEdge; dy++)
            for (int dx = -1; dx <= 1 && !isEdge; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                if (binary[(y + dy) * w + (x + dx)] == 0)
                    isEdge = true;
            }

            if (isEdge)
                edgePixels++;
        }

        return totalPixels > 0 ? 1f - ((float)edgePixels / totalPixels) : 0f;
    }

    private float CalculateFinalScore(
        EvalMetrics metrics,
        (byte[] userBinary, byte[] maskBinary, int width, int height) data
    )
    {
        if (metrics.userPixels == 0)
        {
            return 0f;
        }

        float accuracyScore = ComputeAccuracyScore(metrics);
        float coverageScore = ComputeCoverageScore(metrics);
        float shapeScore = ComputeShapeScore(metrics, data);
        float qualityScore = ComputeQualityScore(data);
        float proportionScore = ComputeProportionScore(metrics);

        accuracyScore = Mathf.Clamp01(accuracyScore);
        coverageScore = Mathf.Clamp01(coverageScore);
        shapeScore = Mathf.Clamp01(shapeScore);
        qualityScore = Mathf.Clamp01(qualityScore);
        proportionScore = Mathf.Clamp01(proportionScore);

        float baseScore =
            accWeight * accuracyScore
            + covWeight * coverageScore
            + shpWeight * shapeScore
            + quaWeight * qualityScore
            + proWeight * proportionScore;

        float scribblePenalty = ComputeScribblePenalty(metrics, data);

        float finalScore = baseScore * scribblePenalty;

        if (finalScore > 0.8f && metrics.precision > 0.9f && metrics.recall > 0.7f)
        {
            float bonus = 1f + (finalScore - 0.8f) * 0.5f;
            finalScore = Mathf.Min(1f, finalScore * bonus);
        }

        return Mathf.Clamp(finalScore * 100f, 0f, 100f);
    }

    private float ComputeAccuracyScore(EvalMetrics metrics)
    {
        if (metrics.userPixels == 0)
            return 0f;

        float baseAccuracy = metrics.precision;

        float outsideRatio = (float)metrics.outsideMask / metrics.userPixels;
        float outsidePenalty = 1f;

        if (outsideRatio > 0.1f)
        {
            outsidePenalty = Mathf.Exp(-outsideRatio * 3f);

            if (outsideRatio > 0.5f)
            {
                outsidePenalty *= 0.3f;
            }
            else if (outsideRatio > 0.3f)
            {
                outsidePenalty *= 0.6f;
            }
        }

        return Mathf.Clamp01(baseAccuracy * outsidePenalty);
    }

    private float ComputeCoverageScore(EvalMetrics metrics)
    {
        if (metrics.maskPixels == 0)
            return 0f;

        float baseCoverage = metrics.recall;

        float areaBonus = 1f;
        if (metrics.areaRatio >= 0.8f && metrics.areaRatio <= 1.3f)
        {
            areaBonus = 1.1f;
        }
        else if (metrics.areaRatio > 1.3f)
        {
            areaBonus = 1.3f / metrics.areaRatio;
        }
        else if (metrics.areaRatio < 0.8f)
        {
            areaBonus = metrics.areaRatio / 0.8f;
        }

        return Mathf.Clamp01(baseCoverage * areaBonus);
    }

    private float ComputeShapeScore(
        EvalMetrics metrics,
        (byte[] userBinary, byte[] maskBinary, int width, int height) data
    )
    {
        float baseShapeScore = 0.3f;

        var userDesc = ComputeShapeDescriptor(data.userBinary, data.width, data.height);
        var maskDesc = ComputeShapeDescriptor(data.maskBinary, data.width, data.height);

        if (userDesc.aspectRatio == 0f || maskDesc.aspectRatio == 0f)
            return baseShapeScore;

        float aspectSimilarity =
            1f
            - Mathf.Min(
                1f,
                Mathf.Abs(userDesc.aspectRatio - maskDesc.aspectRatio)
                    / Mathf.Max(userDesc.aspectRatio, maskDesc.aspectRatio)
            );

        float compactnessSimilarity =
            1f
            - Mathf.Min(
                1f,
                Mathf.Abs(userDesc.compactness - maskDesc.compactness)
                    / Math.Max(1f, Mathf.Max(userDesc.compactness, maskDesc.compactness))
            );

        float centerDistance =
            Vector2.Distance(metrics.userCenter, metrics.maskCenter)
            / Mathf.Max(data.width, data.height);
        float centerSimilarity = Mathf.Exp(-centerDistance * 2f);

        float advancedScore = (aspectSimilarity + compactnessSimilarity + centerSimilarity) / 3f;

        return Mathf.Clamp01(baseShapeScore + advancedScore * 0.7f);
    }

    private float ComputeQualityScore(
        (byte[] userBinary, byte[] maskBinary, int width, int height) data
    )
    {
        float baseQuality = 0.4f;

        var edges = ExtractEdges(data.userBinary, data.width, data.height);
        if (edges.Count == 0)
            return baseQuality;

        float continuity = ComputeContinuity(edges, data.userBinary, data.width, data.height);
        float smoothness = ComputeSmoothness(edges);

        float advancedQuality = (continuity + smoothness) * 0.5f;

        return Mathf.Clamp01(baseQuality + advancedQuality * 0.6f);
    }

    private float ComputeProportionScore(EvalMetrics metrics)
    {
        float deviation = Mathf.Abs(metrics.areaRatio - 1f);

        if (deviation <= 0.3f)
            return 1f;
        if (deviation <= 0.6f)
            return 1f - (deviation - 0.3f) / 0.3f * 0.3f;
        if (deviation <= 1.0f)
            return 0.7f - (deviation - 0.6f) / 0.4f * 0.4f;

        return Mathf.Max(0.3f, 0.3f * Mathf.Exp(-(deviation - 1f)));
    }

    private float ComputeScribblePenalty(
        EvalMetrics metrics,
        (byte[] userBinary, byte[] maskBinary, int width, int height) data
    )
    {
        float penalty = 1f;

        if (metrics.areaRatio > maxAreaMult)
        {
            float excess = (metrics.areaRatio - maxAreaMult) / maxAreaMult;
            penalty *= Mathf.Max(0.2f, Mathf.Exp(-excess * 2f));
        }

        float outsideRatio =
            metrics.userPixels > 0 ? (float)metrics.outsideMask / metrics.userPixels : 0f;
        if (outsideRatio > 0.25f)
        {
            float outsidePenalty = Mathf.Max(0.3f, Mathf.Exp(-(outsideRatio - 0.25f) * 4f));
            penalty *= outsidePenalty;
        }

        var components = FindConnectedComponents(data.userBinary, data.width, data.height);
        if (components.Count > 3)
        {
            float fragPenalty = 1f / (1f + (components.Count - 3) * 0.2f);
            penalty *= Mathf.Max(0.4f, fragPenalty);
        }

        float fillRatio = ComputeFillRatio(data.userBinary, data.width, data.height);
        if (fillRatio > 0.6f)
        {
            penalty *= Mathf.Max(0.6f, 1f - (fillRatio - 0.6f) * 0.5f);
        }

        if (metrics.recall < 0.4f && outsideRatio > 0.4f)
        {
            penalty *= 0.7f;
        }

        return Mathf.Max(0.1f, penalty);
    }

    private ShapeDescriptor ComputeShapeDescriptor(byte[] binary, int w, int h)
    {
        var desc = new ShapeDescriptor();

        var bounds = ComputeBounds(binary, w, h);
        desc.aspectRatio = bounds.size.y > 0 ? bounds.size.x / bounds.size.y : 1f;

        int area = 0,
            perimeter = 0;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (binary[y * w + x] != 0)
            {
                area++;

                bool isBoundary = false;
                for (int dy = -1; dy <= 1 && !isBoundary; dy++)
                for (int dx = -1; dx <= 1 && !isBoundary; dx++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    int nx = x + dx,
                        ny = y + dy;
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h || binary[ny * w + nx] == 0)
                        isBoundary = true;
                }

                if (isBoundary)
                    perimeter++;
            }
        }

        desc.compactness = area > 0 ? (float)(perimeter * perimeter) / area : 0f;

        return desc;
    }

    private List<Vector2Int> ExtractEdges(byte[] binary, int w, int h)
    {
        var edges = new List<Vector2Int>();

        for (int y = 1; y < h - 1; y++)
        for (int x = 1; x < w - 1; x++)
        {
            if (binary[y * w + x] == 0)
                continue;

            int gx =
                -binary[(y - 1) * w + (x - 1)]
                + binary[(y - 1) * w + (x + 1)]
                + -2 * binary[y * w + (x - 1)]
                + 2 * binary[y * w + (x + 1)]
                + -binary[(y + 1) * w + (x - 1)]
                + binary[(y + 1) * w + (x + 1)];

            int gy =
                -binary[(y - 1) * w + (x - 1)]
                - 2 * binary[(y - 1) * w + x]
                - binary[(y - 1) * w + (x + 1)]
                + binary[(y + 1) * w + (x - 1)]
                + 2 * binary[(y + 1) * w + x]
                + binary[(y + 1) * w + (x + 1)];

            if (gx * gx + gy * gy > 2)
            {
                edges.Add(new Vector2Int(x, y));
            }
        }

        return edges;
    }

    private float ComputeContinuity(List<Vector2Int> edges, byte[] binary, int w, int h)
    {
        if (edges.Count == 0)
            return 0f;

        int continuous = 0;
        foreach (var edge in edges)
        {
            int neighbors = 0;
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                int nx = edge.x + dx,
                    ny = edge.y + dy;
                if (nx >= 0 && nx < w && ny >= 0 && ny < h && binary[ny * w + nx] != 0)
                    neighbors++;
            }

            if (neighbors >= 2)
                continuous++;
        }

        return (float)continuous / edges.Count;
    }

    private float ComputeSmoothness(List<Vector2Int> edges)
    {
        if (edges.Count < 3)
            return 1f;

        var orderedEdges = OrderEdgesByProximity(edges);

        float totalCurvature = 0f;
        int validSegments = 0;

        for (int i = 1; i < orderedEdges.Count - 1; i++)
        {
            var v1 = (Vector2)(orderedEdges[i] - orderedEdges[i - 1]);
            var v2 = (Vector2)(orderedEdges[i + 1] - orderedEdges[i]);

            if (v1.magnitude > 0 && v2.magnitude > 0)
            {
                float angle = Vector2.Angle(v1, v2);
                totalCurvature += angle;
                validSegments++;
            }
        }

        if (validSegments == 0)
            return 1f;

        float avgCurvature = totalCurvature / validSegments;
        return Mathf.Exp(-avgCurvature / 30f);
    }

    private List<Vector2Int> OrderEdgesByProximity(List<Vector2Int> edges)
    {
        if (edges.Count <= 2)
            return edges;

        var ordered = new List<Vector2Int> { edges[0] };
        var remaining = new HashSet<Vector2Int>(edges);
        remaining.Remove(edges[0]);

        while (remaining.Count > 0)
        {
            var current = ordered[ordered.Count - 1];
            Vector2Int closest = current;
            float minDist = float.MaxValue;

            foreach (var candidate in remaining)
            {
                float dist = Vector2Int.Distance(current, candidate);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = candidate;
                }
            }

            if (minDist <= 3f)
            {
                ordered.Add(closest);
                remaining.Remove(closest);
            }
            else
            {
                break;
            }
        }

        return ordered;
    }
}
