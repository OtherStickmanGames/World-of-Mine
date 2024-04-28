using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using StarterAssets;

public class MobileInput : MonoBehaviour
{
    [SerializeField] RectTransform mineIcon;
    [SerializeField] FixedTouchField lookTouch;

    ThirdPersonController thirdPersonController;
    PlayerBehaviour player;
    Vector3 mineIconPos;
    Vector2 oldTouchPos;
    Vector3 newTouchPos;
    float touchTimer;
    bool touchDown;
    bool touchWasMoved;

    public void Init(PlayerBehaviour player)
    {
        this.player = player;
        thirdPersonController = player.GetComponent<ThirdPersonController>();
    }

    private void Update()
    {
        MineInputHandler();
    }

    private void MineInputHandler()
    {
        mineIconPos.x = -888;
        mineIconPos.y = -888;
        mineIconPos.z = 300;

        if (thirdPersonController.CurrentSpeed > 0)
        {

        }
        else
        {
            if (Input.touches.Length == 1 && !touchWasMoved)
            {
                var touch = Input.touches[0];
                
                // Если было нажаьте первый раз
                if (!touchDown)
                {
                    touchDown = true;
                    oldTouchPos = touch.position;
                }
                // Вычисляем было ли движение тача
                var dir = touch.position - oldTouchPos;
                // Если нет движения больше 0,5 сек, то активируем майнинг
                if (dir.magnitude < 0.1f)
                {
                    touchTimer += Time.deltaTime;

                    if (touchTimer > 0.5f)
                    {
                        var offsetX = Screen.width / 2;
                        var offsetY = Screen.height / 2;
                        mineIconPos.x = touch.position.x - offsetX;
                        mineIconPos.y = touch.position.y - offsetY;
                    }
                }
                else// Иначе блокируем майнинг до нового тача
                {
                    touchWasMoved = true;
                }
                oldTouchPos = touch.position;


            }
            if (Input.touches.Length == 0)
            {
                touchWasMoved = false;
                touchDown = false;
                touchTimer = 0;
            }
        }

        mineIcon.position = mineIconPos;
    }
}
