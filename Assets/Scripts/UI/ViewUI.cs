using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using System;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class ViewUI : MonoBehaviour
{
    [SerializeField] LeanTweenType animType;
    [SerializeField] float scaleDuration = 0.38f;

    protected Vector2 targetPosition;

    CanvasGroup canvasGroup;
    RectTransform uiTransform;
    Vector2 originPosition;
    Vector2 lastPositionTarget;
    float lastScaleTarget;
    float lastAlphaTarget;

    public virtual void Init()
    {
        originPosition = transform.position;

        canvasGroup = GetComponent<CanvasGroup>();
        uiTransform = transform as RectTransform;

        Hide();
    }

    public virtual void Show()
    {
        Activate();
        ScaleAnim(1, null);
        TransparencyAnim(1);
        PositionAnim(originPosition);
    }

    public virtual void Hide()
    {
        ScaleAnim(0, Deactivate);
        TransparencyAnim(0);
        PositionAnim(targetPosition);
    }

    void Deactivate() => gameObject.SetActive(false);
    void Activate() => gameObject.SetActive(true);


    void ScaleAnim(float to, Action onComplete)
    {
        var from = transform.localScale.x;
        var diff = Mathf.Abs(from - to);

        if(diff < 0.01f)
        {
            return;
        }

        lastScaleTarget = to;

        LeanTween.value
        (
            gameObject, 
            s => 
            {
                if(Mathf.Abs(lastScaleTarget - to) < 0.001f)
                {
                    transform.localScale = Vector3.one * s;
                }
            }, 
            from, 
            to, 
            scaleDuration
        ).setEaseInOutQuad().setOnComplete(onComplete);


    }

    void TransparencyAnim(float to, Action onComplete = null)
    {
        var from = canvasGroup.alpha;
        var diff = Mathf.Abs(from - to);

        if (diff < 0.01f)
        {
            return;
        }

        lastAlphaTarget = to;

        LeanTween.value
        (
            gameObject,
            a =>
            {
                if (Mathf.Abs(lastAlphaTarget - to) < 0.001f)
                {
                    canvasGroup.alpha = a;
                }
            },
            from,
            to,
            scaleDuration
        ).setEaseInOutQuad().setOnComplete(onComplete);


    }

    void PositionAnim(Vector2 to, Action onComplete = null)
    {
        var from = (Vector2)transform.position;
        var diff = (from - to).magnitude;

        if (diff < 0.1f)
        {
            return;
        }

        lastPositionTarget = to;

        LeanTween.value
        (
            gameObject,
            p =>
            {
                if ((lastPositionTarget - to).magnitude < 0.01f)
                {
                    transform.position = p;
                    var loc = transform.localPosition;
                    loc.z = 0;
                    transform.localPosition = loc;
                }
            },
            from,
            to,
            scaleDuration
        ).setEaseInOutQuad().setOnComplete(onComplete);


    }

    //[Serializable]
    //public class AnimModul
    //{
    //    public LeanTweenType easyType;
    //    public
    //}

    //public enum AnimType
    //{
    //    Scale,
    //    Position,
    //    Transparency,
    //}
}
