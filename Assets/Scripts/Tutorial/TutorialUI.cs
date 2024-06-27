using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using System;
using StarterAssets;
using TMPro;
using UnityEngine.EventSystems;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] TMP_Text debugTexto;
    [SerializeField] QuickInventoryView quickInventoryView;
    [SerializeField] Button btnSwitchCamera;
    [SerializeField] FixedTouchField touchField;
    [SerializeField] MobileInput mobileInput;
    [SerializeField] InteractableStateTracker btnJump;
    [SerializeField] float smoothTime = 1f;
    [SerializeField] float sensitivity = 3f;

    [Header("Output")]
    [SerializeField] StarterAssetsInputs starterAssetsInputs;
    [SerializeField] GameObject mobileController;

    [Header("Tutorial")]
    [SerializeField] Canvas canvasTutorial;
    [SerializeField] GameObject touchZonesTutorial;
    [SerializeField] InteractableStateTracker leftZone, rightZone;

    [Space(8)]

    [SerializeField] GameObject lookZoneTutorial;
    [SerializeField] GameObject moveZoneTutorial;
    [SerializeField] GameObject jumpZoneTutorial;
    [SerializeField] GameObject selectSlotTutorial;

    Canvas canvasMine;
    Character mine;
    Transform player;
    Vector2 lookDirection;
    Vector2 currentVelocity;
    Vector3 oldCameraRotation;
    Vector3 oldCharacterPosition;

    string debugStr;
    float sumCameraRotation, sumCharacterMove;

    bool touchZoneComplete, lookZoneComplete, moveZoneComplete, jumpZoneComplete;
    bool selectSlotComplete;

    bool needResetPlayerPosition;

    private void Awake()
    {
        canvasMine = GetComponent<Canvas>();

        btnSwitchCamera.gameObject.SetActive(false);
        lookZoneTutorial.SetActive(false);
        moveZoneTutorial.SetActive(false);
        jumpZoneTutorial.SetActive(false);
        selectSlotTutorial.SetActive(false);
        btnSwitchCamera.onClick.AddListener(BtnSwitchCamera_Clicked);

        PlayerBehaviour.onMineSpawn.AddListener(PlayerMine_Spawned);
    }


    private void Start()
    {
        quickInventoryView.gameObject.SetActive(false);
        mobileController.SetActive(false);
        mobileInput.gameObject.SetActive(false);
    }


    private void Update()
    {
        debugStr = string.Empty;

        if (Application.isMobilePlatform)
        {
            var value = touchField.TouchDist * sensitivity * UI.ScaleFactor;
            lookDirection = Vector2.SmoothDamp(lookDirection, value, ref currentVelocity, Time.deltaTime * smoothTime);
            VirtualLookInput(lookDirection);
        }

        TutorialLogic();

        debugTexto.text = debugStr;
    }

    public void VirtualLookInput(Vector2 virtualLookDirection)
    {
        starterAssetsInputs.LookInput(virtualLookDirection);
    }

    private void BtnSwitchCamera_Clicked()
    {
        var curType = CameraStack.Instance.CurrentType;
        if (curType == CameraStack.CameraType.First)
        {
            CameraStack.Instance.SwitchToThirdPerson();
        }
        else if (curType == CameraStack.CameraType.TopDown)
        {
            CameraStack.Instance.SwitchToFirstPerson();
        }
        else if (curType == CameraStack.CameraType.Third)
        {
            CameraStack.Instance.SwitchToTopDown();
        }
    }

    private void PlayerMine_Spawned(MonoBehaviour player)
    {
        mobileInput.gameObject.SetActive(true);
        mobileInput.Init(player as PlayerBehaviour);

        btnSwitchCamera.gameObject.SetActive(true);

        mine = player.GetComponent<Character>();
        quickInventoryView.gameObject.SetActive(true);
        InitInventoryView(mine);

        if (Application.isMobilePlatform)
        {
            //mobileController.SetActive(true);
            touchField.gameObject.SetActive(true);
        }
        else
        {
            touchField.gameObject.SetActive(false);
        }

        this.player = player.transform;
    }

    private void InitInventoryView(Character player)
    {
        //if (inventoryView)
        //{
        //    inventoryView.Init(player.inventory);
        //}
        quickInventoryView.Init(player.inventory);

        //onInventoryOpen.AddListener(player.inventory.Open);
        //onInventoryClose.AddListener(player.inventory.Close);
    }

    void TutorialLogic()
    {
        // Touch Zones
        if (!touchZoneComplete)
        {
            debugStr += $"{leftZone.Pressed} ### {rightZone.Pressed}";
            if (leftZone.Pressed && rightZone.Pressed)
            {
                touchZonesTutorial.SetActive(false);
                LeanTween.delayedCall(1, () => lookZoneTutorial.SetActive(true));

                oldCameraRotation = Camera.main.transform.rotation.eulerAngles;

                touchZoneComplete = true;
            }
        }

        if (!lookZoneComplete && touchZoneComplete)
        {
            var cameraRotation = Camera.main.transform.rotation.eulerAngles;
            var rotationDir = cameraRotation - oldCameraRotation;
            sumCameraRotation += rotationDir.magnitude;
            oldCameraRotation = cameraRotation;
            print(sumCameraRotation);
            debugStr += $" {sumCameraRotation}";

            if (sumCameraRotation > 1800)
            {
                lookZoneComplete = true;
                lookZoneTutorial.SetActive(false);

                LeanTween.delayedCall(1, () =>
                {
                    mobileController.SetActive(true);
                    moveZoneTutorial.SetActive(true);
                });
            }
        }

        if (!moveZoneComplete && lookZoneComplete)
        {
            if (mine.transform.position != Vector3.zero)
            {
                if (oldCharacterPosition == Vector3.zero)
                {
                    oldCharacterPosition = mine.transform.position;
                }
            }

            var charPos = mine.transform.position;
            var posDir = charPos - oldCharacterPosition;
            sumCharacterMove += posDir.magnitude;
            oldCharacterPosition = charPos;

            debugStr += $"  Pos {sumCharacterMove}";

            if (sumCharacterMove > 18)
            {
                moveZoneComplete = true;
                moveZoneTutorial.SetActive(false);

                LeanTween.delayedCall(1, () =>
                {
                    jumpZoneTutorial.SetActive(true);
                });
            }
        }

        if (!jumpZoneComplete && moveZoneComplete)
        {
            if (btnJump.Pressed)
            {
                jumpZoneComplete = true;

                jumpZoneTutorial.SetActive(false);

                LeanTween.delayedCall(1, () =>
                {
                    selectSlotTutorial.SetActive(true);

                    canvasMine.sortingOrder = canvasTutorial.sortingOrder + 1;

                    var item = new Item() { id = 1 };
                    item.view = BlockItemSpawner.CreateBlockGameObject(item.id);
                    item.count = 8;
                    mine.inventory.TakeItem(item);

                    quickInventoryView.SelectSlot(7);
                });
            }
        }

        if (!selectSlotComplete && jumpZoneComplete)
        {
            if (quickInventoryView.Selected == 0)
            {
                debugStr += $"≈бать ты машина";

                selectSlotComplete = true;

                selectSlotTutorial.SetActive(false);
            }
        }
    }
}
