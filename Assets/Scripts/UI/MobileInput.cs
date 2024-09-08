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
    [SerializeField] Button btnNoFall;
    [SerializeField] GameObject noFallOutline;
    [SerializeField] Color noFallActiveColor;
    [SerializeField] TMP_Text noFallTitle;
    [SerializeField] LayerMask layer;
    [SerializeField] TMP_Text txtDebuga;

    AnimationCurve mineCurve;
    ThirdPersonController thirdPersonController;
    PlayerBehaviour player;
    Character character;
    RaycastHit raycastHit;
    Vector3 mineIconPos;
    Vector2 oldTouchPos;
    Vector3 blockPosition;
    float scaleFactor;
    float touchTimer;
    float mineTimer;
    bool touchDown;
    bool touchWasMoved;
    bool lastBlockRaycast;

    public void Init(PlayerBehaviour player)
    {
        this.player = player;
        character = player.GetComponent<Character>();
        thirdPersonController = player.GetComponent<ThirdPersonController>();

        if (btnNoFall)
        {
            btnNoFall.onClick.AddListener(NoFall_Clicked);
            UpdateBtnNoFallView();
        }

        Keyframe start = new Keyframe(0, 0);
        Keyframe end = new Keyframe(mineTime, 1);
        mineCurve = new AnimationCurve(new Keyframe[] { start, end });
    }

    public void NoFall_Clicked()
    {
        thirdPersonController.NoFall = !thirdPersonController.NoFall;
        UpdateBtnNoFallView();
    }

    private void UpdateBtnNoFallView()
    {
        if (thirdPersonController.NoFall)
        {
            noFallOutline.SetActive(true);
            noFallTitle.color = noFallActiveColor;
        }
        else
        {
            noFallOutline.SetActive(false);
            noFallTitle.color = Color.white;
        }
    }

    private void Update()
    {
#if !UNITY_SERVER
        scaleFactor = UI.ScaleFactor;

        MineInputHandler();
#endif
    }

    private void MineInputHandler()
    {
        mineIconPos.x = -888;
        mineIconPos.y = -888;

        if (thirdPersonController.CurrentSpeed > 0)
        {

        }
        else
        {
            if (Input.touches.Length == 1)
            {
                //txtDebuga.text = $"Mos Pos {Input.mousePosition}\nTach Poso {Input.touches[0].position}";

                //var offsetX = Screen.width / 2;
                //var offsetY = Screen.height / 2;
                //var offset = new Vector2(offsetX, offsetY);
                //TouchTest.position = Input.touches[0].position;// - offset;

                //var localPos = TouchTest.localPosition;
                //localPos.z = 0;
                //TouchTest.localPosition = localPos;
                
                TouchTest.anchoredPosition = Input.touches[0].position * scaleFactor;
            }

            var exclude = new List<GameObject>() { lookTouch.gameObject };
            if (Input.touches.Length == 1 && !touchWasMoved && !UI.ClickOnUI(exclude))
            {
                //print("зашли в тач");
                var touch = Input.touches[0];

                // Если было нажатие первый раз
                if (!touchDown)
                {
                    //print("==============================================");
                    touchDown = true;
                    oldTouchPos = touch.position;
                }
                // Вычисляем было ли движение тача
                var dir = touch.position - oldTouchPos;
                // Если нет движения больше 0,5 сек, то активируем майнинг
                lastBlockRaycast = IsBlockRaycast(out raycastHit);
                //print($"{oldTouchPos} ??? {touch.position} ### {dir.magnitude < 1.88f * scaleFactor}");
                if (dir.magnitude < 3.8f * scaleFactor && lastBlockRaycast)
                {
                    touchTimer += Time.deltaTime;

                    Vector3 normalPos = raycastHit.point - (raycastHit.normal / 2);

                    int x = Mathf.FloorToInt(normalPos.x);
                    int y = Mathf.FloorToInt(normalPos.y);
                    int z = Mathf.FloorToInt(normalPos.z);

                    blockPosition = new(x, y, z);

                    player.blockHighlight.position = blockPosition;
                    //print(touchTimer);
                    if (touchTimer > 0.5f)
                    {
                        mineIconPos.x = touch.position.x * scaleFactor;
                        mineIconPos.y = touch.position.y * scaleFactor;

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
                if (touchTimer > 0 && touchTimer < 0.3f && lastBlockRaycast && !touchWasMoved && character.inventory.CurrentSelectedItem != null)
                {
                    PlaceBlock();
                }

                touchWasMoved = false;
                touchDown = false;
                touchTimer = 0;
                mineTimer = 0;
            }
        }

        mineIcon.anchoredPosition = mineIconPos;
    }

    private void Mining(Vector3 blockPos)
    {
        mineTimer += Time.deltaTime;

        innerMineIcon.localScale = Vector3.one * mineCurve.Evaluate(mineTimer);

        if (mineTimer > mineTime)
        {
            WorldGenerator.Inst.MineBlock(blockPos);
            mineTimer = 0;
        }
    }

    private void PlaceBlock()
    {
        
        var pos = blockPosition + raycastHit.normal + Vector3.right;
        var item = character.inventory.CurrentSelectedItem;
        var blockID = item.id;
        WorldGenerator.Inst.SetBlockAndUpdateChunck(pos, blockID);
        WorldGenerator.Inst.PlaceBlock(pos, blockID);
        character.inventory.Remove(item);
    }

    private bool IsBlockRaycast(out RaycastHit raycastHit)
    {
        var maxDist = character.MineDistance;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out raycastHit, maxDist * 1.3f, layer))
        {
            //print($"{LayerMask.LayerToName(raycastHit.collider.gameObject.layer)} =-=-=-=-=-=-=-");
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
