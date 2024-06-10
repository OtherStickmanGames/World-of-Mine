using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchTracker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
{
    public bool Pressed;
    public float touchTime;


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
        Pressed = false;
    }

  
    public void OnPointerMove(PointerEventData eventData)
    {
        
    }
}
