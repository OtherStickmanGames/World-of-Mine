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

        Keyframe start = new Keyframe(0, 0);
        Keyframe end = new Keyframe(mineTime, 1);
        mineCurve = new AnimationCurve(new Keyframe[] { start, end });
    }

    private void Update()
    {
        scaleFactor = UI.ScaleFactor;

        MineInputHandler();
    }

    private void MineInputHandler()
    {
        mineIconPos.x = -888;
        mineIconPos.y = -888;
        //mineIconPos.z = 300;

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
                var touch = Input.touches[0];

                // ���� ���� ������� ������ ���
                if (!touchDown)
                {
                    touchDown = true;
                    oldTouchPos = touch.position;
                }
                // ��������� ���� �� �������� ����
                var dir = touch.position - oldTouchPos;
                // ���� ��� �������� ������ 0,5 ���, �� ���������� �������
                lastBlockRaycast = IsBlockRaycast(out raycastHit);
                if (dir.magnitude < 0.88f * scaleFactor && lastBlockRaycast)
                {
                    touchTimer += Time.deltaTime;

                    Vector3 normalPos = raycastHit.point - (raycastHit.normal / 2);

                    int x = Mathf.FloorToInt(normalPos.x);
                    int y = Mathf.FloorToInt(normalPos.y);
                    int z = Mathf.FloorToInt(normalPos.z);

                    blockPosition = new(x, y, z);

                    player.blockHighlight.position = blockPosition;

                    if (touchTimer > 0.5f)
                    {
                        var scaleFactor = (float)1080 / (float)Screen.height;
                        mineIconPos.x = touch.position.x * scaleFactor;
                        mineIconPos.y = touch.position.y * scaleFactor;

                        Mining(blockPosition + Vector3.right);// �������� 731,76
                    }
                }
                else// ����� ��������� ������� �� ������ ����
                {
                    touchWasMoved = true;
                }
                oldTouchPos = touch.position;

            }
            // ����� ������ ������� �� �����
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
