using UnityEngine;

public static class TouchLookNormalizer
{
    private const float ReferenceShortSide = 1080f;
    private const float ReferenceFrameTime = 0.016f;

    public static Vector2 NormalizeScreenDelta(Vector2 pixelDelta)
    {
        float shortSide = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height));
        return pixelDelta * (ReferenceShortSide / shortSide);
    }

    public static Vector2 ToLookRate(Vector2 pixelDelta, float sensitivity)
    {
        if (Time.deltaTime <= 0f)
        {
            return Vector2.zero;
        }

        return NormalizeScreenDelta(pixelDelta) * sensitivity * ReferenceFrameTime / Time.deltaTime;
    }
}
