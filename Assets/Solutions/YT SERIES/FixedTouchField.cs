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


    void Start()
    {
       
    }

    void Update()
    {
        if (Pressed)
        {
            Vector2 currentPos = PointerOld;

            if (Input.touchCount > 0)
            {
                // Возвращаем �тарый, рабочий метод пои�ка пальца (�амый правый на экране), 
                // так как PointerId из EventSystem не �овпадает � fingerId из-за конфликта �и�тем ввода.
                Vector2 pos = Vector2.left * float.MaxValue;
                for (int i = 0; i < Input.touches.Length; i++)
                {
                    if (Input.touches[i].position.x > pos.x)
                    {
                        pos = Input.touches[i].position;
                    }
                }
                currentPos = pos;
            }
            else if (Input.GetMouseButton(0))
            {
                currentPos = Input.mousePosition;
            }

            Vector2 rawDelta = currentPos - PointerOld;
            PointerOld = currentPos;

            // Ма�штабируем по вы�оте (аналог Canvas Scaler -> Match Height)
            float scaleFactor = 1080f / Screen.height;

            TouchDist.x = rawDelta.x * scaleFactor;
            TouchDist.y = rawDelta.y * scaleFactor;

            if (invertY)
            {
                TouchDist.y *= -1;
            }
        }
        else
        {
            TouchDist = Vector2.zero;
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

    //Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    //Vector2 localRawDelta;

    //bool isInside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
    //    canvas.transform as RectTransform,
    //    rawDelta,
    //    uiCamera,
    //    out localRawDelta
    //);
}