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

    AnimationCurve resolutionFactorCurve;

    void Start()
    {
       
        resolutionFactorCurve = new();
        resolutionFactorCurve.AddKey(new(720, 15));
        resolutionFactorCurve.AddKey(new(1080, 1));
        
        
    }

    void Update()
    {
        if (Pressed)
        {
            Vector2 pos = Vector2.left * float.MaxValue;
            for (int i = 0; i < Input.touches.Length; i++)
            {
                if(Input.touches[i].position.x > pos.x)
                {
                    pos = Input.touches[i].position;
                }
            }

            if(Input.touches.Length > 0)
            {
                TouchDist = pos - PointerOld;// * resolutionFactorCurve.Evaluate(Screen.height);
                PointerOld = pos;
            }

            if (PointerId >= 0 && PointerId < Input.touches.Length)
            {
                //TouchDist = Input.touches[PointerId].position - PointerOld;
                //PointerOld = Input.touches[PointerId].position;
                //TouchDist = pos - PointerOld;
                //PointerOld = pos;
            }
            else
            {
                //TouchDist = new Vector2(Input.mousePosition.x, Input.mousePosition.y) - PointerOld;
                //PointerOld = Input.mousePosition;
            }
            if (invertY)
            {
                TouchDist.y *= -1;
            }
        }
        else
        {
            TouchDist  = Vector2.zero;
            PointerOld = Vector2.zero;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Pressed = true;
        PointerId = eventData.pointerId;
        PointerOld = eventData.position;
	}


    public void OnPointerUp(PointerEventData eventData)
    {
        Pressed = false;
        //UI.PrintCurrentUI();
    }

    public Vector2 olda;
    public void OnPointerMove(PointerEventData eventData)
    {
        var offsetX = Screen.width / 2;
        var offsetY = Screen.height / 2;
        
        
    }


}