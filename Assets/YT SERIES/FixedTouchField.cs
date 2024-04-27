using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class FixedTouchField : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
{
    [SerializeField] bool invertY;
    [SerializeField] TMP_Text ebatos;
    [SerializeField] RectTransform touch1;
    [SerializeField] RectTransform touch2;

    [HideInInspector]
    public Vector2 TouchDist;
    [HideInInspector]
    public Vector2 PointerOld;
    [HideInInspector]
    protected int PointerId;
    [HideInInspector]
    public bool Pressed;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
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
                TouchDist = pos - PointerOld;
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
            TouchDist = new Vector2();
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
    }

    public Vector2 olda;
    public void OnPointerMove(PointerEventData eventData)
    {
        var offsetX = Screen.width / 2;
        var offsetY = Screen.height / 2;
        
        //TouchDist = eventData.position - olda;
        //olda = eventData.position;

        //ebatos.text = $"{TouchDist}";

        //if (invertY)
        //{
        //    TouchDist.y *= -1;
        //}
    }
}