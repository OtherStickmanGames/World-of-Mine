using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePointerTutorial : MonoBehaviour
{
    [SerializeField] float offsetAngle;
    [SerializeField] float scaleFactor = 100f;

    public float? distance;

    Vector2 start, end;
    RectTransform rectTransform;

    private void Start()
    { 
        rectTransform = transform as RectTransform;
    }

    public void SetCorners(Vector2 start, Vector2 end)
    {
        this.start = start;
        this.end = end;
    }

    private void Update()
    {
        if (start.Equals(Vector2.zero) || end.Equals(Vector2.zero))
            return;

        rectTransform.anchoredPosition = start;

        var dir = end - start;
        distance = dir.magnitude;
        dir.y *= -1;
        dir.Normalize();

        var angle = Vector2.SignedAngle(dir, Vector2.right) + offsetAngle;

        rectTransform.rotation = Quaternion.Euler(0, 0, angle);

        rectTransform.localScale = Vector3.one * (distance.Value / scaleFactor);
    }
}
