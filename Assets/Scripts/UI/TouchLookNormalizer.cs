using UnityEngine;

public static class TouchLookNormalizer
{
    private const float ReferenceShortSide = 1080f;
    private const float ReferenceFrameTime = 0.016f;

    /// <summary>
    /// Converts raw touch pixels to a reference screen size.
    ///
    /// Android devices often report unreliable Screen.dpi values, so using DPI can still make
    /// the same swipe rotate differently on different phones. For camera look we normalize by
    /// the rendered short side instead: a swipe that covers the same part of the visible screen
    /// produces the same camera input regardless of resolution or pixel density.
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
