using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BlinkAlpha : MonoBehaviour
{
    // Variabile publice

    public float minAlpha = 0.2f;
    public float maxAlpha = 1f;
    public float blinkSpeed = 1f;

    // Variabile locale

    private RawImage rawImage;
    private Color baseColor;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        baseColor = rawImage.color;
        StartCoroutine(BlinkLoop());
    }

    IEnumerator BlinkLoop()
    {
        while (true)
        {
            float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, eased);

            rawImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            yield return null;
        }
    }
}
