using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class SaveBuildingView : MonoBehaviour
{
    [SerializeField] Button btnSaveBuilding;
    [SerializeField] Button btnCancel;
    [SerializeField] Button btnAccept;
    [SerializeField] RectTransform selectionBox;
    [SerializeField] GameObject selectingArea;
    [SerializeField] Joystick moveCamJoystick;
    [SerializeField] InteractableStateTracker btnZoomPlus;
    [SerializeField] InteractableStateTracker btnZoomMinus;

    [Space]

    [SerializeField] RectTransform leftPlane;
    [SerializeField] RectTransform rightPlane;
    [SerializeField] RectTransform topPlane;
    [SerializeField] RectTransform bottomPlane;

    [Space]

    [SerializeField] InteractableStateTracker cropHandleLeftTop;
    [SerializeField] InteractableStateTracker cropHandleRightBottom;

    [Space]

    [SerializeField] float zoomValue = 0.1f;

    [Space]

    [SerializeField] Transform meshHolder;
    [SerializeField] GameObject panelPreview;
    [SerializeField] FixedTouchField buildingPreviewLook;

    [Space]

    [SerializeField] GameObject inputNameBuilding;
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] GameObject buildingSavedNotify;
    [SerializeField] Button btnSavedOk;

    [Space]

    public bool IsCurrentDeviceMouse;
    public float sensitivityMouseY = 1.5f;
    public float CameraAngleOverrideX = 0.0f;
    public float CameraAngleOverrideY = 0.0f;
    public float rotateSensitibity = 5;

    public static UnityEvent onSaveBuildingClick = new UnityEvent();
    public static UnityEvent onBuildingSave = new UnityEvent();
    public static UnityEvent onClose = new UnityEvent();

    public AcceptMode CurSelectionMode { get; set; }

    BuildPreviewData buildPreviewData;
    bool allowSelectBlocks;

    public void Init()
    {
        selectionBox.gameObject.SetActive(false);
        btnAccept.gameObject.SetActive(false);
        selectingArea.SetActive(false);
        panelPreview.SetActive(false);
        inputNameBuilding.SetActive(false);
        buildingSavedNotify.SetActive(false);
        btnCancel.gameObject.SetActive(false);

        btnSaveBuilding.onClick.AddListener(SaveBuilding_Clicked);
        btnCancel.onClick.AddListener(Cancel_Clicked);
        btnAccept.onClick.AddListener(Accept_Clicked);
        btnSavedOk.onClick.AddListener(SavedOk_Clicked);

        cropHandleLeftTop.onPointerUp.AddListener(CropHandle_Uped);
        cropHandleRightBottom.onPointerUp.AddListener(CropHandle_Uped);

        BuildingManager.Singleton.onCountBuildingsReceive.AddListener(CountBuildings_Received);
        BuildingManager.Singleton.onBuildSave.AddListener(Building_Saved);
        CameraStack.onCameraSwitch.AddListener(Camera_Switched);
    }

    private void Cancel_Clicked()
    {
        CloseSaveBuilding();
    }

    public void SavedOk_Clicked()
    {
        CloseSaveBuilding();
        onBuildingSave?.Invoke();
    }

    void CloseSaveBuilding()
    {
        panelPreview.SetActive(false);
        selectingArea.SetActive(false);
        btnAccept.gameObject.SetActive(false);
        btnCancel.gameObject.SetActive(false);
        CameraStack.Instance.SwitchToPreviousCamera();
        BuildingManager.Singleton.ClearHighlights();
        Destroy(buildPreviewData?.view);
        SetVisibleBtnSaveBuilding(true);
        onClose?.Invoke();
    }

    private void Building_Saved()
    {
        buildingSavedNotify.SetActive(true);
    }

    private void CountBuildings_Received(int countBuildings)
    {
        nameInput.text = $"Шедеврище - {countBuildings + 1}";
        btnAccept.gameObject.SetActive(true);
    }

    private void Camera_Switched(CameraStack.CameraType camType)
    {
        if (camType == CameraStack.CameraType.SaveBuilding)
        {
            if (CurSelectionMode == AcceptMode.Vertical)
            {
                StartCoroutine(Delay());
            }
        }

        IEnumerator Delay()
        {
            yield return null;

            var uiPos = Camera.main.WorldToScreenPoint(BuildingManager.Singleton.horizontalLeftTop);
            cropHandleLeftTop.SetPos(uiPos * UI.ScaleFactor);
            uiPos = Camera.main.WorldToScreenPoint(BuildingManager.Singleton.horizontalRightBottom);
            cropHandleRightBottom.SetPos(uiPos * UI.ScaleFactor);

            UpdateSelectionBackgroud();
            CropHandle_Uped();
        }
    }

    private void CropHandle_Uped()
    {
        var scaleFactor = UI.ScaleFactor;
        var startPos = Camera.main.ScreenToWorldPoint(cropHandleLeftTop.GetPos() / scaleFactor);
        var endPos   = Camera.main.ScreenToWorldPoint(cropHandleRightBottom.GetPos() / scaleFactor);

        if (CurSelectionMode == AcceptMode.Horizontal)
        {
            BuildingManager.Singleton.SelectionHorizontal(startPos, endPos);
        }
        if (CurSelectionMode == AcceptMode.Vertical)
        {
            BuildingManager.Singleton.SelectionVertical(startPos, endPos);
        }

        btnAccept.gameObject.SetActive(true);
    }

    private void SaveBuilding_Clicked()
    {
        CurSelectionMode = AcceptMode.Horizontal;
        BuildingManager.Singleton.StartSelection();

        allowSelectBlocks = true;
        meshHolder.rotation = Quaternion.identity;

        selectingArea.SetActive(true);
        inputNameBuilding.SetActive(false);
        buildingSavedNotify.SetActive(false);
        moveCamJoystick.gameObject.SetActive(true);
        btnCancel.gameObject.SetActive(true);

        cropHandleLeftTop.SetPos(new Vector2(Screen.width * UI.ScaleFactor * 0.2f, 830));
        cropHandleRightBottom.SetPos(new Vector2(Screen.width * UI.ScaleFactor * 0.8f, 200));

        SetVisibleBtnSaveBuilding(false);

        UpdateSelectionBackgroud();

        onSaveBuildingClick?.Invoke();
    }

    private void Accept_Clicked()
    {
        switch (CurSelectionMode)
        {
            case AcceptMode.Horizontal:
                CurSelectionMode = AcceptMode.Vertical;
                BuildingManager.Singleton.SwitchSelectionAxis();
                
                break;

            case AcceptMode.Vertical:
                ShowBuildingPreview();
                CurSelectionMode = AcceptMode.Preview;
                break;

            case AcceptMode.Preview:
                ShowInputBuildName();
                CurSelectionMode = AcceptMode.Name;
                break;

            case AcceptMode.Name:
                SaveBuilding();
                CurSelectionMode = AcceptMode.Save;
                break;
        }
    }

    private void SaveBuilding()
    {
        BuildingManager.Singleton.SaveBuilding(nameInput.text);
        btnAccept.gameObject.SetActive(false);
        // TO DO доделать в случае неудачи сейва
    }

    private void ShowInputBuildName()
    {
        inputNameBuilding.SetActive(true);
        btnAccept.gameObject.SetActive(false);

        BuildingManager.Singleton.InputNameBuilding_Showed();
        //print("Ебала в менеджер");
    }

    private void ShowBuildingPreview()
    {
        selectingArea.SetActive(false);
        panelPreview.SetActive(true);
        moveCamJoystick.gameObject.SetActive(false);

        allowSelectBlocks = false;

        var building = BuildingManager.Singleton.BuildPreview();
        building.view.layer = LayerMask.NameToLayer("UI");
        building.view.transform.SetParent(meshHolder);
        var widthScreenSpace = 1.5f;
        var scaleX = (Screen.width * UI.ScaleFactor) / (building.width * widthScreenSpace);
        var scaleY = (Screen.height * UI.ScaleFactor) / (building.height * 1.5f);
        var scaleZ = (Screen.width * UI.ScaleFactor) / (building.length * widthScreenSpace);
        building.view.transform.localScale = Vector3.one * Mathf.Min(scaleX, scaleY, scaleZ);
        building.ShiftPosition();
        buildPreviewData = building;
    }

    
    private void LateUpdate()
    {
        BuildingPreviewRotate();
                
    }

    private void Update()
    {
        
        if (allowSelectBlocks)
        {
            if (cropHandleLeftTop.Pressed)
            {
                UpdateLeftCropHandle(Input.mousePosition);
            }

            if (cropHandleRightBottom.Pressed)
            {
                UpdateRightCropHandle(Input.mousePosition);
            }
        }

        UpdateZoomButtons();
        CameraMove();
    }


    Vector3 camDir;
    private void CameraMove()
    {
        var joystickDir = moveCamJoystick.Direction;
        if (joystickDir != Vector2.zero)
        {
            if (CurSelectionMode == AcceptMode.Horizontal)
            {
                camDir.x = joystickDir.x;
                camDir.y = 0;
                camDir.z = joystickDir.y;
            }
            if (CurSelectionMode == AcceptMode.Vertical)
            {
                camDir.x = joystickDir.x;
                camDir.y = joystickDir.y;
                camDir.z = 0;
            }

            camDir *= CameraStack.Instance.GetSaveBuildingCamZoomValue();
            CameraStack.Instance.SaveBuildingCamMove(camDir);
        }
    }

    void UpdateZoomButtons()
    {
        if (btnZoomMinus.Pressed)
        {
            CameraStack.Instance.SaveBuildingCameraChangeZoom(zoomValue * 1.05f);
        }
        if (btnZoomPlus.Pressed)
        {
            CameraStack.Instance.SaveBuildingCameraChangeZoom(-zoomValue);
        }
    }

    void UpdateRightCropHandle(Vector2 mousePos)
    {
        var scaleFactor = UI.ScaleFactor;

        cropHandleRightBottom.SetPos(mousePos * UI.ScaleFactor);

        var size = rightPlane.sizeDelta;
        size.x = Screen.width - mousePos.x;
        rightPlane.sizeDelta = size * scaleFactor;

        var width = Screen.width - ((Screen.width - mousePos.x) + (leftPlane.sizeDelta.x / scaleFactor));
        size = topPlane.sizeDelta;
        size.x = width * scaleFactor;
        topPlane.sizeDelta = size;

        size = bottomPlane.sizeDelta;
        size.y = mousePos.y;
        size.x = width;
        bottomPlane.sizeDelta = size * scaleFactor;
    }

    void UpdateLeftCropHandle(Vector2 mousePos)
    {
        var scaleFactor = UI.ScaleFactor;

        cropHandleLeftTop.SetPos(mousePos * UI.ScaleFactor);

        var size = leftPlane.sizeDelta;
        size.x = mousePos.x * scaleFactor;
        leftPlane.sizeDelta = size;

        var width = Screen.width - (mousePos.x + (rightPlane.sizeDelta.x / scaleFactor));
        size = topPlane.sizeDelta;
        size.x = width;
        size.y = Screen.height - mousePos.y;
        topPlane.sizeDelta = size * scaleFactor;

        size = bottomPlane.sizeDelta;
        size.x = width * scaleFactor;
        bottomPlane.sizeDelta = size;

        var planePos = topPlane.anchoredPosition;
        planePos.x = mousePos.x;
        topPlane.anchoredPosition = planePos * scaleFactor;

        planePos = bottomPlane.anchoredPosition;
        planePos.x = mousePos.x;
        bottomPlane.anchoredPosition = planePos * scaleFactor;
    }

    void UpdateSelectionBackgroud()
    {
        var scaleFactor = UI.ScaleFactor;
        UpdateLeftCropHandle(cropHandleLeftTop.GetPos() / scaleFactor);
        UpdateRightCropHandle(cropHandleRightBottom.GetPos() / scaleFactor);
    }

    float _cinemachineTargetYaw;
    float _cinemachineTargetPitch;
    Vector2 prevMp, lookDirection, currentVelocity;
    void BuildingPreviewRotate()
    {
        if (panelPreview.activeSelf)
        {
            var look = buildingPreviewLook.TouchDist;
            if (!Application.isMobilePlatform)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    prevMp = Input.mousePosition;
                }
                if (Input.GetMouseButton(0))
                {
                    look   = (Vector2)Input.mousePosition - prevMp;
                    prevMp = (Vector2)Input.mousePosition;
                }
            }

            look.x *= -1;
            look   *= rotateSensitibity;
            lookDirection = Vector2.SmoothDamp(lookDirection, look, ref currentVelocity, Time.deltaTime * 1.38f);

            if (lookDirection.sqrMagnitude >= 0.01f)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += lookDirection.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += lookDirection.y * deltaTimeMultiplier * sensitivityMouseY;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, -90, 90);

            meshHolder.transform.rotation = Quaternion.Euler
            (
                _cinemachineTargetPitch + CameraAngleOverrideX,
                _cinemachineTargetYaw + CameraAngleOverrideY,
                0.0f
            );
        }
    }

    public void HideBtnCancel()
    {
        btnCancel.gameObject.SetActive(false); 
    }

    public void SetBuildingName(string value)
    {
        nameInput.text = value;
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    public void SetVisibleBtnAccept(bool value) => btnAccept.gameObject.SetActive(value);
    public void SetVisibleBtnSaveBuilding(bool value) => btnSaveBuilding.gameObject.SetActive(value);
    public InteractableStateTracker GetLeftTopCropHandle() => cropHandleLeftTop;
    public InteractableStateTracker GetRightBottomCropHandle() => cropHandleRightBottom;
}

public enum AcceptMode
{
    Horizontal,
    Vertical,
    Preview,
    Name,
    Save,
}
