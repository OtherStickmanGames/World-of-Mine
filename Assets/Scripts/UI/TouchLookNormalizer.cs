using UnityEngine;

public static class TouchLookNormalizer
{
    private const float ReferenceDpi = 420f;
    private const float ReferenceShortSide = 1080f;
    private const float ReferenceFrameTime = 0.016f;
    private const float MinReliableDpi = 120f;
    private const float MaxReliableDpi = 900f;

    /// <summary>
    /// Converts raw Android/iOS touch pixels to the same reference screen size so camera
    /// sensitivity stays stable across phones with different render resolutions.
    /// </summary>
    public static Vector2 NormalizeScreenDelta(Vector2 pixelDelta)
    {
        return pixelDelta * GetNormalizationFactor();
    }

    /// <summary>
    /// Returns a look rate that can be multiplied by Time.deltaTime in the controller.
    /// </summary>
    public static Vector2 ToLookRate(Vector2 pixelDelta, float sensitivity)
    {
        if (Time.deltaTime <= 0f)
        {
            return Vector2.zero;
        }

        return NormalizeScreenDelta(pixelDelta) * sensitivity * ReferenceFrameTime / Time.deltaTime;
    }

    private static float GetNormalizationFactor()
    {
        float shortSide = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height));
        return ReferenceShortSide / shortSide;
    }
}
