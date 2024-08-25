using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class InteractableStateTracker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler, IDragHandler
{
    public RectTransform rectTransform;
    public bool Pressed;
    public float touchTime;

    public UnityEvent onPointerUp;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
    }

    private void Update()
    {
        if (Pressed)
        {
            touchTime += Time.deltaTime;
        }
        else
        {
            touchTime = 0;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Pressed = true;
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        onPointerUp?.Invoke();
        Pressed = false;
        //UI.PrintCurrentUI();
    }


    public void OnPointerMove(PointerEventData eventData)
    {

    }

    public void SetPos(Vector2 value)
    {
        rectTransform.anchoredPosition = value;
    }

    public Vector2 GetPos()
    {
        return rectTransform.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        
    }
}
