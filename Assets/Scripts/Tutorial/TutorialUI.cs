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
using Cinemachine;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] bool testMobileInput;
    [SerializeField] int countBuildingsBlocks = 0;
    [SerializeField] float speedCamRot = 3f;
    [SerializeField] TMP_Text debugTexto;
    [SerializeField] QuickInventoryView quickInventoryView;
    [SerializeField] SaveBuildingView saveBuildingView;
    [SerializeField] Button btnSwitchCamera;
    [SerializeField] FixedTouchField touchField;
    [SerializeField] MobileInput mobileInput;
    [SerializeField] InteractableStateTracker btnJump;
    [SerializeField] float smoothTime = 1f;
    [SerializeField] float sensitivity = 3f;
    [SerializeField] TMP_InputField resolutionFactorInput;

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
    [SerializeField] GameObject placeBlockTutorial;
    [SerializeField] GameObject mineBlockTutorial;
    [SerializeField] GameObject switchCameraTutorial;
    [SerializeField] GameObject makeBuildingTutorial;
    [SerializeField] GameObject openSaveBuildingTutorial;
    [SerializeField] GameObject moveSaveCameraTutorial;
    [SerializeField] GameObject zoomSaveCameraTutorial;
    [SerializeField] GameObject leftCropHandleTutorial;
    [SerializeField] GameObject rightCropHandleTutorial;
    [SerializeField] GameObject horizontalPlaneAcceptZone;
    [SerializeField] GameObject verticalPlaneAcceptTutorial;
    [SerializeField] GameObject previewBuildingTutorial;
    [SerializeField] GameObject nameBuildingTutorial;
    [SerializeField] GameObject completeTutorial;
    [Space]
    [SerializeField] Transform highlightBlockTutorial;
    [SerializeField] CinemachineVirtualCamera tutorialPersonCamera;
    [SerializeField] Transform placeBlockPointer;
    [SerializeField] RectTransform mineTooltipPointer;
    [SerializeField] List<BlockData> buildingPoses;
    [SerializeField] Material mat;
    [SerializeField] Material highlightMakeBuildingMat;
    [SerializeField] Transform makeBuildingHighlightPrefab;
    [SerializeField] MovePointerTutorial leftTopMovePointerTutorial;
    [SerializeField] MovePointerTutorial rightBottomMovePointerTutorial;
    [SerializeField] Button btnComplete;
    [SerializeField] Button btnSkipTutor;
    [SerializeField] GameObject zoomButtons;

    Canvas canvasMine;
    Character mine;
    PlayerBehaviour playerBehaviour;
    ThirdPersonController thirdPersonController;
    Vector2 lookDirection;
    Vector2 currentVelocity;
    Vector3 oldCameraRotation;
    Vector3 oldCharacterPosition;
    Vector3 lookToPlaceBlock;
    Vector3 startPos = new(-125, 10, 1);
    Vector3 buildingPos;

    string debugStr;
    float sumCameraRotation;
    float sumCharacterMove;
    float ResolutionFactor => float.Parse(resolutionFactorInput.text);

  
    

    bool touchZoneComplete;
    bool lookZoneComplete;
    bool moveZoneComplete;
    bool jumpZoneComplete;
    bool selectSlotComplete;
    bool placeBlockComplete;
    bool mineBlockComplete;
    bool placeBlockTutorInited;
    bool mineBlockTutorialInited;
    bool switchCameraComplete;
    bool makeBuildingComplete;
    bool makeBuildingInited;
    bool openSaveBuildingComplete;
    bool moveSaveCameraComplete;
    bool zoomSaveCameraComplete;
    bool leftCropHandleMoveComplete;
    bool rightCropHandleMoveComplete;
    bool horizontalPlaneAcceptComplete;
    bool verticalPlaneAcceptComplete;
    bool previewBuildingComplete;
    bool nameBuildingComplete;
    bool needCameraLookToPlaceBlock;
    bool needCameraLookToBuilding;
    bool needSetPlayerStartPos;
    bool allowCheckMoveCamTutorial;

    AnimationCurve resolutionFactorCurve;


    private void Awake()
    {
        canvasMine = GetComponent<Canvas>();

        btnSwitchCamera.gameObject.SetActive(false);

        touchZonesTutorial.SetActive(true);
        lookZoneTutorial.SetActive(false);
        moveZoneTutorial.SetActive(false);
        jumpZoneTutorial.SetActive(false);
        selectSlotTutorial.SetActive(false);
        placeBlockTutorial.SetActive(false);
        mineBlockTutorial.SetActive(false);
        switchCameraTutorial.SetActive(false);
        makeBuildingTutorial.SetActive(false);
        openSaveBuildingTutorial.SetActive(false);
        moveSaveCameraTutorial.SetActive(false);
        zoomSaveCameraTutorial.SetActive(false);
        leftCropHandleTutorial.SetActive(false);
        rightCropHandleTutorial.SetActive(false);
        horizontalPlaneAcceptZone.SetActive(false);
        verticalPlaneAcceptTutorial.SetActive(false);
        previewBuildingTutorial.SetActive(false);
        nameBuildingTutorial.SetActive(false);
        completeTutorial.SetActive(false);

        btnSwitchCamera.onClick.AddListener(BtnSwitchCamera_Clicked);
        btnComplete.onClick.AddListener(Tutorial_Completed);
        btnSkipTutor.onClick.AddListener(BtnSkipTutor_Clicked);

        PlayerBehaviour.onMineSpawn.AddListener(PlayerMine_Spawned);
        PlayerBehaviour.onOwnerPositionSet.AddListener(PlayerPosition_Seted);
        WorldGenerator.onBlockPlace.AddListener(Block_Placed);

        canvasTutorial.gameObject.SetActive(true);
    }

    private void BtnSkipTutor_Clicked()
    {
        SetPlayerGamePosition();
        UnityEngine.SceneManagement.SceneManager.LoadScene("World");
        UserData.Owner.tutorialSkiped = true;
    }

    private void SetPlayerGamePosition()
    {
        playerBehaviour.transform.position += Vector3.up * 100;
        UserData.Owner.position = playerBehaviour.transform.position;
    }

    private void PlayerPosition_Seted(MonoBehaviour player)
    {
        LeanTween.delayedCall(0.1f, () => needSetPlayerStartPos = true);

    }

    private void Start()
    {
        quickInventoryView.gameObject.SetActive(false);
        saveBuildingView.Init();
        saveBuildingView.SetVisibleBtnSaveBuilding(false);
        mobileController.SetActive(false);
        mobileInput.gameObject.SetActive(false);

        resolutionFactorCurve = new();
        resolutionFactorCurve.AddKey(new(720, 1));
        resolutionFactorCurve.AddKey(new(1080, 1));

        SaveBuildingView.onSaveBuildingClick.AddListener(SaveBuilding_Clicked);
        SaveBuildingView.onBuildingSave.AddListener(Building_Saved);

        buildingPos = startPos + (Vector3.down * 24) + (Vector3.right * 1) + (Vector3.forward * 9);

        InputLogic.Single.DontHideCursor = false;
    }

    private void Building_Saved()
    {
        mobileController.SetActive(true);
    }

    private void SaveBuilding_Clicked()
    {
        mobileController.SetActive(false);
    }

    private void Update()
    {
        debugStr = string.Empty;

        if (Application.isMobilePlatform || testMobileInput)
        {
            var damping = resolutionFactorCurve.Evaluate(Screen.height) * Time.deltaTime;
            var value = touchField.TouchDist * sensitivity * damping;
            lookDirection = Vector2.SmoothDamp(lookDirection, value, ref currentVelocity, damping * smoothTime);
            VirtualLookInput(lookDirection);
        }

        if (Input.GetMouseButtonDown(0))
        {
            //UI.PrintCurrentUI();
        }

        TutorialLogic();
        //debugStr = $"{sensitivity * UI.ScaleFactor} ## {Screen.height} # {resolutionFactorCurve.Evaluate(Screen.height)}";
        debugTexto.text = debugStr;

        if (Input.GetKeyDown(KeyCode.L))
        {
            canvasTutorial.gameObject.SetActive(false);
        }
    }

    public void VirtualLookInput(Vector2 virtualLookDirection)
    {
        starterAssetsInputs.LookInput(virtualLookDirection);
    }

    private void BtnSwitchCamera_Clicked()
    {
        CameraStack.Instance.SwitchCamera();
    }

    private void PlayerMine_Spawned(MonoBehaviour player)
    {
        mobileInput.gameObject.SetActive(true);
        mobileInput.Init(player as PlayerBehaviour);
        playerBehaviour = player as PlayerBehaviour;
        thirdPersonController = player.GetComponent<ThirdPersonController>();

        btnSwitchCamera.gameObject.SetActive(true);

        saveBuildingView.gameObject.SetActive(true);

        mine = player.GetComponent<Character>();
        quickInventoryView.gameObject.SetActive(true);
        InitInventoryView(mine);

        if (Application.isMobilePlatform || testMobileInput)
        {
            touchField.gameObject.SetActive(true);
        }
        else
        {
            touchField.gameObject.SetActive(false);
        }

        
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
            //print(sumCameraRotation);
            debugStr += $" {sumCameraRotation}";

            if (sumCameraRotation > 500)
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

            if (sumCharacterMove > 8)
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
                quickInventoryView.SelectSlot(7);

                jumpZoneComplete = true;
                jumpZoneTutorial.SetActive(false);

                LeanTween.delayedCall(1, () =>
                {
                    selectSlotTutorial.SetActive(true);

                    canvasMine.sortingOrder = canvasTutorial.sortingOrder + 1;

                    var item = new Item() { id = 3 };
                    item.view = BlockItemSpawner.CreateDropedView(item.id);
                    item.count = 8;
                    mine.inventory.TakeItem(item);
                });
            }
        }

        if (!selectSlotComplete && jumpZoneComplete)
        {
            if (quickInventoryView.Selected == 0)
            {
                debugStr += $"����� �� ������";

                selectSlotComplete = true;

                selectSlotTutorial.SetActive(false);

                LeanTween.delayedCall(1, () =>
                {
                    placeBlockTutorial.SetActive(true);
                });
            }
        }

        if (!placeBlockComplete && selectSlotComplete)
        {
            var placeBlockPos = startPos + (Vector3.right * 3) - (Vector3.up * 24);

            highlightBlockTutorial.position = placeBlockPos;
            playerBehaviour.blockHighlight.position = Vector3.zero;

            var camRootLookDir = placeBlockPos - playerBehaviour.cameraTarget.position;

            var diffRotation = playerBehaviour.cameraTarget.rotation.eulerAngles - Quaternion.LookRotation(camRootLookDir).eulerAngles;

            thirdPersonController.AllowCameraRotation = false;

            if (!tutorialPersonCamera.Follow)
            {
                tutorialPersonCamera.Follow = playerBehaviour.cameraTarget;
            }

            if (!placeBlockTutorInited)
            {
                var offset = new Vector3(0.5f, 2, 0.5f);

                placeBlockPointer.position = placeBlockPos + offset;
                tutorialPersonCamera.Priority = 18;

                placeBlockTutorInited = true;
            }

            var checkingPos = placeBlockPos + Vector3.right;// + Vector3.up;

            needCameraLookToPlaceBlock = true;

            debugStr += $"��������� ����� {WorldGenerator.Inst.GetBlockID(checkingPos)}";

            if (WorldGenerator.Inst.GetBlockID(checkingPos) > 0)
            {
                placeBlockComplete = true;

                placeBlockTutorial.SetActive(false);

                ShowTutorial(mineBlockTutorial);
                
                //needCameraLookToPlaceBlock = false;
                //tutorialPersonCamera.Priority = 5;
                //thirdPersonController.AllowCameraRotation = true;
            }


        }

        if (!mineBlockComplete && placeBlockComplete)
        {
            var placeBlockPos = startPos + (Vector3.right * 3) - (Vector3.up * 24);

            highlightBlockTutorial.position = placeBlockPos;
            playerBehaviour.blockHighlight.position = Vector3.zero;

            if (!mineBlockTutorialInited)
            {
                var offset = new Vector3(0.5f, 2, 0.5f);

                placeBlockPointer.position = placeBlockPos + offset;

                mineBlockTutorialInited = true;
            }

            var screenPos = Camera.main.WorldToScreenPoint(placeBlockPos);
            screenPos.x -= Screen.width / 2;
            screenPos.y -= Screen.height / 2;
            //mineTooltipPointer.transform.position = screenPos;
            mineTooltipPointer.anchoredPosition = screenPos;
            var localPos = mineTooltipPointer.transform.localPosition;
            localPos.z = 0;
            mineTooltipPointer.transform.localPosition = localPos;

            var checkingPos = placeBlockPos + Vector3.right;

            if (WorldGenerator.Inst.GetBlockID(checkingPos) == 0)
            {
                mineBlockTutorial.SetActive(false);

                needCameraLookToPlaceBlock = false;
                tutorialPersonCamera.Priority = 5;
                thirdPersonController.AllowCameraRotation = true;

                placeBlockPointer.position = Vector3.zero;
                highlightBlockTutorial.gameObject.SetActive(false);

                mineBlockComplete = true;

                ShowTutorial(switchCameraTutorial);
            }
        }

        if (!switchCameraComplete && mineBlockComplete)
        {
            if (CameraStack.Instance.CurrentType is CameraStack.CameraType.First)
            {
                switchCameraComplete = true;
                switchCameraTutorial.SetActive(false);
                

                ShowTutorial(makeBuildingTutorial);

                LeanTween.delayedCall(1f, () =>
                {
                    needCameraLookToBuilding = true;
                    tutorialPersonCamera.Priority = 18;
                    thirdPersonController.AllowCameraRotation = false;
                });
            }
        }

        if (!makeBuildingComplete && switchCameraComplete)
        {
            if (!makeBuildingInited)
            {
                var hightLisghtPrefab = makeBuildingHighlightPrefab;// highlightBlockTutorial;
                var woodPrefab = BlockItemSpawner.CreateDropedView(BLOCKS.WOOD);
                woodPrefab.transform.localScale *= 3f;
                woodPrefab.GetComponent<MeshRenderer>().material = mat;
                var cobblestonePrefab = BlockItemSpawner.CreateDropedView(BLOCKS.COBBLESTONE);
                cobblestonePrefab.transform.localScale *= 3f;
                cobblestonePrefab.GetComponent<MeshRenderer>().material = mat;

                var blockOffset = Vector3.one * 0.5f;

                foreach (var blockData in buildingPoses)
                {
                    BuildingPointer buildingPointer = new BuildingPointer();
                    var pos = blockData.pos + buildingPos;
                    var highlight = Instantiate(hightLisghtPrefab, pos, Quaternion.identity);
                    highlight.gameObject.SetActive(true);
                    highlight.GetComponentInChildren<MeshRenderer>().material = highlightMakeBuildingMat;

                    buildingPointer.highlight = highlight.gameObject;

                    if (blockData.ID == BLOCKS.WOOD)
                    {
                        buildingPointer.supposedBlock = Instantiate(woodPrefab, pos + blockOffset, Quaternion.identity);
                    }
                    if (blockData.ID == BLOCKS.COBBLESTONE)
                    {
                        buildingPointer.supposedBlock = Instantiate(cobblestonePrefab, pos + blockOffset, Quaternion.identity);

                    }

                    makeBuildingPointers.Add(pos.ToIntPos(), buildingPointer);
                }

              
                //placeBlockPointer.gameObject.SetActive(false);
                placeBlockPointer.position = buildingPos + (Vector3.up * 3) + blockOffset;
                tutorialPersonCamera.LookAt = null;// placeBlockPointer;


                var item = new Item() { id = BLOCKS.COBBLESTONE };
                item.view = BlockItemSpawner.CreateDropedView(item.id);
                item.count = 8;
                mine.inventory.TakeItem(item);

                makeBuildingInited = true;
            }

            if (needCameraLookToBuilding)
            {
                thirdPersonController.AllowCameraRotation = touchField.Pressed;
            }

            if (makeBuildingPointers.Count == countBuildingsBlocks)
            {
                ShowTutorial(openSaveBuildingTutorial);
                saveBuildingView.SetVisibleBtnSaveBuilding(true);

                makeBuildingComplete = true;
                mobileController.SetActive(false);
                quickInventoryView.gameObject.SetActive(false);

                makeBuildingTutorial.SetActive(false);
            }
        }

        

        if (!openSaveBuildingComplete && makeBuildingComplete)
        {
            if (BuildingManager.Singleton.selectionStarted)
            {
                ShowTutorial(moveSaveCameraTutorial);

                saveBuildingView.HideBtnCancel();

                LeanTween.delayedCall(0.1f, SetCamPos);

                void SetCamPos()
                {
                    var camera = Camera.main;
                    var camPos = camera.transform.position;
                    var targetPos = buildingPos + (Vector3.right * 8);
                    targetPos.y = camPos.y;
                    LeanTween.value
                    (
                        gameObject,
                        pos =>
                        {
                            CameraStack.Instance.SetSaveBuildingCamPos(pos);
                            //print("�� ������ ????!!!!");
                        },
                        camPos,
                        targetPos,
                        1f
                    ).setEaseOutQuad().setOnComplete(() => 
                    {
                        allowCheckMoveCamTutorial = true;
                        //print($"{allowCheckMoveCamTutorial} ============================================");
                    });

                    saveBuildingView.SetVisibleBtnAccept(false);
                    saveBuildingView.GetLeftTopCropHandle().gameObject.SetActive(false);
                    saveBuildingView.GetRightBottomCropHandle().gameObject.SetActive(false);
                }

                openSaveBuildingTutorial.SetActive(false);
                canvasTutorial.sortingOrder = canvasMine.sortingOrder + 1;

                openSaveBuildingComplete = true;
            }
        }

        

        if (!moveSaveCameraComplete && openSaveBuildingComplete)
        {
            saveBuildingView.HideBtnCancel();
            var camPos = Camera.main.transform.position;
            var camPos2D = new Vector2(camPos.x, camPos.z);
            var buildingPos2D = new Vector2(buildingPos.x, buildingPos.z);
            //print($"{Vector2.Distance(camPos2D, buildingPos2D)} ### {allowCheckMoveCamTutorial}");
            if (Vector2.Distance(camPos2D, buildingPos2D) < 1.88f && allowCheckMoveCamTutorial)
            {
                moveSaveCameraTutorial.SetActive(false);
                ShowTutorial(zoomSaveCameraTutorial);

                moveSaveCameraComplete = true;
            }
        }

        if (!zoomSaveCameraComplete && moveSaveCameraComplete)
        {
            saveBuildingView.HideBtnCancel();
            var targetPos = buildingPos + (Vector3.one * 0.5f);
            var camPos = CameraStack.Instance.GetSaveBuildingCameraPosition();
            targetPos.y = camPos.y;
            targetPos.z += 0.5f;
            var moveCampPos = Vector3.MoveTowards(camPos, targetPos, Time.deltaTime * 3f);

            CameraStack.Instance.SetSaveBuildingCamPos(moveCampPos);

            if (CameraStack.Instance.GetSaveBuildingCamZoomValue() < 5f)
            {
                zoomSaveCameraTutorial.SetActive(false);
                ShowTutorial(leftCropHandleTutorial);
                saveBuildingView.GetLeftTopCropHandle().gameObject.SetActive(true);
                saveBuildingView.GetRightBottomCropHandle().gameObject.SetActive(true);

                zoomSaveCameraComplete = true;
            }
        }

        if (!leftCropHandleMoveComplete && zoomSaveCameraComplete)
        {
            saveBuildingView.HideBtnCancel();
            var leftHandle = saveBuildingView.GetLeftTopCropHandle();
            var startPos = (leftHandle.transform as RectTransform).anchoredPosition;
            var endPos = Camera.main.WorldToScreenPoint(buildingPos + new Vector3(-1.5f, 1, 2.5f)) * UI.ScaleFactor;
            leftTopMovePointerTutorial.SetCorners(startPos, endPos);

            CameraStack.Instance.SaveBuildingCamSetZoom(5f);

            var dist = leftTopMovePointerTutorial.distance;
            if (dist != null && dist < 35)
            {
                leftCropHandleTutorial.SetActive(false);

                ShowTutorial(rightCropHandleTutorial);

                leftCropHandleMoveComplete = true;
            }

            // ������� ���������� ��������� ����� ���������� ��������� ������ �������
            // �������� ����������� ������, ������� �� �� ��� ��������� ��������
            saveBuildingView.SetVisibleBtnAccept(false);
        }

        if (!rightCropHandleMoveComplete && leftCropHandleMoveComplete)
        {
            saveBuildingView.HideBtnCancel();
            var rightHandle = saveBuildingView.GetRightBottomCropHandle();
            var startPos = (rightHandle.transform as RectTransform).anchoredPosition;
            var endPos = Camera.main.WorldToScreenPoint(buildingPos + new Vector3(2.5f, 1, -1.5f)) * UI.ScaleFactor;
            rightBottomMovePointerTutorial.SetCorners(startPos, endPos);

            var dist = rightBottomMovePointerTutorial.distance;
            if (dist != null && dist < 58)
            {
                rightCropHandleTutorial.SetActive(false);

                rightCropHandleMoveComplete = true;

                saveBuildingView.SetVisibleBtnAccept(true);

                ShowTutorial(horizontalPlaneAcceptZone);
            }

            // ������� ���������� ��������� ����� ���������� ��������� ������ �������
            // �������� ����������� ������, ������� �� �� ��� ��������� ��������
            saveBuildingView.SetVisibleBtnAccept(false);
        }

        if (!horizontalPlaneAcceptComplete && rightCropHandleMoveComplete)
        {
            saveBuildingView.HideBtnCancel();
            if (saveBuildingView.CurSelectionMode == AcceptMode.Vertical)
            {
                horizontalPlaneAcceptZone.SetActive(false);
                horizontalPlaneAcceptComplete = true;
                ShowTutorial(verticalPlaneAcceptTutorial);
            }
        }

        if (!verticalPlaneAcceptComplete && horizontalPlaneAcceptComplete)
        {
            saveBuildingView.HideBtnCancel();
            if (saveBuildingView.CurSelectionMode == AcceptMode.Preview)
            {
                verticalPlaneAcceptTutorial.SetActive(false);
                verticalPlaneAcceptComplete = true;
                ShowTutorial(previewBuildingTutorial);
            }
        }

        if (!previewBuildingComplete && verticalPlaneAcceptComplete)
        {
            saveBuildingView.HideBtnCancel();
            if (saveBuildingView.CurSelectionMode == AcceptMode.Name)
            {
                saveBuildingView.SetBuildingName("��������� �..");
                saveBuildingView.SetVisibleBtnAccept(true);
                previewBuildingComplete = true;
                previewBuildingTutorial.SetActive(false);
                ShowTutorial(nameBuildingTutorial);
            }
        }

        if (!nameBuildingComplete && previewBuildingComplete)
        {
            saveBuildingView.HideBtnCancel();
            if (saveBuildingView.CurSelectionMode == AcceptMode.Save)
            {
                nameBuildingTutorial.SetActive(false);
                nameBuildingComplete = true;
                ShowTutorial(completeTutorial);
            }
        }

        if (nameBuildingComplete && !UserData.Owner.tutorialComplete)
        {
            saveBuildingView.HideBtnCancel();
            LeanTween.delayedCall(1f, () => saveBuildingView.SavedOk_Clicked());
            UserData.Owner.tutorialComplete = true;
            UserData.Owner.SaveData();

            print("� ��");
        }
    }

    private void Tutorial_Completed()
    {
        SetPlayerGamePosition();
        UnityEngine.SceneManagement.SceneManager.LoadScene("World");
    }

    Dictionary<Vector3Int, BuildingPointer> makeBuildingPointers = new Dictionary<Vector3Int, BuildingPointer>();

    private void Block_Placed(BlockData blockData)
    {
        if (!makeBuildingComplete && mineBlockComplete)
        {
            var pos = (blockData.pos - Vector3.right).ToIntPos();
            if (makeBuildingPointers.ContainsKey(pos))
            {
                var pointer = makeBuildingPointers[pos];
                Destroy(pointer.highlight);
                Destroy(pointer.supposedBlock);
                makeBuildingPointers.Remove(pos);
            }    
        }
    }

    private void ShowTutorial(GameObject tutorial)
    {
        LeanTween.delayedCall(1, () =>
        {
            tutorial.SetActive(true);
        });
    }

    float lookToBuildingTimer = 0;
    [SerializeField] int cameraHeightForPlaceMineBlockTutor = 10;
    private void LateUpdate()
    {
        if (needCameraLookToPlaceBlock)
        {
            var height = placeBlockComplete ? cameraHeightForPlaceMineBlockTutor - 1 : cameraHeightForPlaceMineBlockTutor;
            var placeBlockPos = startPos + (Vector3.right * 3) - (Vector3.up * height);
            var camRootLookDir = placeBlockPos - playerBehaviour.cameraTarget.position;
            playerBehaviour.cameraTarget.rotation = Quaternion.LookRotation(camRootLookDir);
        }

        if (needCameraLookToBuilding)
        {
            var camRootLookDir = buildingPos - playerBehaviour.cameraTarget.position + new Vector3(1, 0, -2);
            var currentCameraRotation = playerBehaviour.cameraTarget.rotation.eulerAngles;
            var targetCameraRotation = Quaternion.LookRotation(camRootLookDir).eulerAngles;
            var cameraRotation = Vector3.MoveTowards
            (
                currentCameraRotation,
                targetCameraRotation,
                Time.deltaTime * speedCamRot * Mathf.Clamp(Vector3.Distance(currentCameraRotation, targetCameraRotation), 0.1f, 80)
            );

            cameraRotation.y = ClampAngle(cameraRotation.y, float.MinValue, float.MaxValue);
            cameraRotation.x = ClampAngle(cameraRotation.x, -80, 80);

            playerBehaviour.cameraTarget.rotation = Quaternion.Euler(cameraRotation);
            if (!thirdPersonController.AllowCameraRotation)
            {
                var pitch = playerBehaviour.cameraTarget.rotation.eulerAngles.x - thirdPersonController.CameraAngleOverride;
                var yaw = playerBehaviour.cameraTarget.rotation.eulerAngles.y;
                thirdPersonController.SetPitchAndYaw(pitch, yaw);
                currentVelocity = default;
            }

            lookToBuildingTimer += Time.deltaTime;
            if (lookToBuildingTimer > 1.89f)
            {
                needCameraLookToBuilding = false;
                thirdPersonController.AllowCameraRotation = true;
                tutorialPersonCamera.Priority = 5;
            }
        }

        if (needSetPlayerStartPos)
        {
            mine.transform.position = startPos;
            
            needSetPlayerStartPos = false;
        }

        if (playerBehaviour && playerBehaviour.transform.position.y < -18)
        {
            playerBehaviour.transform.position = startPos;
        }
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    public class BuildingPointer
    {
        public GameObject highlight;
        public GameObject supposedBlock;
    }
}

public static class ExtXyext
{
    public static Vector3Int ToIntPos(this Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        return new Vector3Int(x, y, z);
    }
}
