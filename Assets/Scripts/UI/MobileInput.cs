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
    [SerializeField] RectTransform innerMineIcon;
    [SerializeField] float mineTime = 1.5f;
    [SerializeField] RectTransform TouchTest;

    AnimationCurve mineCurve;
    ThirdPersonController thirdPersonController;
    PlayerBehaviour player;
    Character character;
    Vector3 mineIconPos;
    Vector2 oldTouchPos;
    Vector3 newTouchPos;
    float touchTimer;
    float mineTimer;
    bool touchDown;
    bool touchWasMoved;

    public void Init(PlayerBehaviour player)
    {
        this.player = player;
        character = player.GetComponent<Character>();
        thirdPersonController = player.GetComponent<ThirdPersonController>();

        Keyframe start = new Keyframe(0, 0);
        Keyframe end = new Keyframe(mineTime, 1);
        mineCurve = new AnimationCurve(new Keyframe[] { start, end });
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
            if (Input.touches.Length == 1)
            {
                var offsetX = Screen.width / 2;
                var offsetY = Screen.height / 2;
                TouchTest.position = Input.touches[0].position;
                var localPos = TouchTest.localPosition;
                localPos.x -= offsetX;
                localPos.z = 0;
                TouchTest.localPosition = localPos;
            }

            if (Input.touches.Length == 1 && !touchWasMoved)
            {
                var touch = Input.touches[0];
                
                // Если было нажатье первый раз
                if (!touchDown)
                {
                    touchDown = true;
                    oldTouchPos = touch.position;
                }
                // Вычисляем было ли движение тача
                var dir = touch.position - oldTouchPos;
                // Если нет движения больше 0,5 сек, то активируем майнинг
                if (dir.magnitude < 0.1f && IsBlockRaycast(out var hit))
                {
                    touchTimer += Time.deltaTime;

                    Vector3 normalPos = hit.point - (hit.normal / 2);

                    int x = Mathf.FloorToInt(normalPos.x);
                    int y = Mathf.FloorToInt(normalPos.y);
                    int z = Mathf.FloorToInt(normalPos.z);

                    Vector3 blockPosition = new(x, y, z);

                    player.blockHighlight.position = blockPosition;

                    if (touchTimer > 0.5f)
                    {
                        var offsetX = Screen.width / 2;
                        var offsetY = Screen.height / 2;
                        mineIconPos.x = touch.position.x - offsetX;
                        mineIconPos.y = touch.position.y - offsetY;

                        

                        Mining(blockPosition + Vector3.right);// Кузичева 731,76
                    }
                }
                else// Иначе блокируем майнинг до нового тача
                {
                    touchWasMoved = true;
                }
                oldTouchPos = touch.position;

            }
            // Сброс логики нажатия на экран
            if (Input.touches.Length == 0)
            {
                touchWasMoved = false;
                touchDown = false;
                touchTimer = 0;
                mineTimer = 0;
            }
        }

        mineIcon.position = mineIconPos;
    }

    private void Mining(Vector3 blockPos)
    {
        mineTimer += Time.deltaTime;

        innerMineIcon.localScale = Vector3.one * mineTimer;

        if (mineTimer > mineTime)
        {
           
            //WorldGenerator.Inst.SetBlockAndUpdateChunck(blockPos, 14);
            WorldGenerator.Inst.MineBlock(blockPos);
            mineTimer = 0;
        }
    }

    private bool IsBlockRaycast(out RaycastHit raycastHit)
    {
        var maxDist = character.MineDistance;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out raycastHit))
        {
            var dist = Vector3.Distance(raycastHit.point, player.transform.position + Vector3.up);
            if (dist > maxDist)
                return false;

            if (raycastHit.collider.gameObject.layer == LayerMask.NameToLayer("NavChunck"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }
}
