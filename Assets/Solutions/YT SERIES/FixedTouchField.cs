using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class FixedTouchField : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
{
    [SerializeField] bool invertY;
    [SerializeField] TMP_Text ebatos;
    [SerializeField] RectTransform touch1;
    [SerializeField] RectTransform touch2;
    
    [Space]

    [ReadOnlyField]
    public Vector2 TouchDist;
    [ReadOnlyField]
    public Vector2 PointerOld;
    [ReadOnlyField]
    protected int PointerId;
    [ReadOnlyField]
    public bool Pressed;

    Vector2 accumulatedPointerDelta;

    void Start()
    {
       
        
        
        
    }

    void Update()
    {
        if (Pressed)
        {
            // Use EventSystem deltas from the pointer that actually started on this field.
            // Polling Input.touches and choosing the right-most finger makes the look delta depend
            // on other active touches (joystick/UI) and on device-specific touch ordering.
            TouchDist = accumulatedPointerDelta;
            accumulatedPointerDelta = Vector2.zero;
        }
        else
        {
            TouchDist = Vector2.zero;
            accumulatedPointerDelta = Vector2.zero;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Pressed = true;
        PointerId = eventData.pointerId;
        PointerOld = eventData.position;
        accumulatedPointerDelta = Vector2.zero;
	}


    public void OnPointerUp(PointerEventData eventData)
    {
        Pressed = false;
        TouchDist = Vector2.zero;
        accumulatedPointerDelta = Vector2.zero;
        //UI.PrintCurrentUI();
    }

    public Vector2 olda;
    public void OnPointerMove(PointerEventData eventData)
    {
        if (!Pressed || eventData.pointerId != PointerId)
        {
            return;
        }

        Vector2 delta = eventData.position - PointerOld;
        PointerOld = eventData.position;

        if (invertY)
        {
            delta.y *= -1;
        }

        accumulatedPointerDelta += delta;
    }


}