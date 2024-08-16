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


    public bool IsCurrentDeviceMouse;
    public float sensitivityMouseY = 1.5f;
    public float CameraAngleOverrideX = 0.0f;
    public float CameraAngleOverrideY = 0.0f;
    public float rotateSensitibity = 5;



    public static UnityEvent onSaveBuildingClick = new UnityEvent();

    public AcceptMode CurSelectionMode { get; set; }

    Vector3 startPressPos;
    bool selectionBoxDisplayed;
    bool allowSelectBlocks;

    public void Init()
    {
        selectionBox.gameObject.SetActive(false);
        btnAccept.gameObject.SetActive(false);
        selectingArea.SetActive(false);
        panelPreview.SetActive(false);
        inputNameBuilding.SetActive(false);

        btnSaveBuilding.onClick.AddListener(SaveBuilding_Clicked);
        btnAccept.onClick.AddListener(Accept_Clicked);

        cropHandleLeftTop.onPointerUp.AddListener(CropHandle_Uped);
        cropHandleRightBottom.onPointerUp.AddListener(CropHandle_Uped);

        CameraStack.onCameraSwitch.AddListener(Camera_Switched);
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
        var endPos = Camera.main.ScreenToWorldPoint(cropHandleRightBottom.GetPos() / scaleFactor);

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
        BuildingManager.Singleton.StartSelection();

        allowSelectBlocks = true;
        selectingArea.SetActive(true);

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
                moveCamJoystick.gameObject.SetActive(false);
                break;

            case AcceptMode.Vertical:
                ShowBuildingPreview();
                CurSelectionMode = AcceptMode.Preview;
                break;

            case AcceptMode.Preview:
                ShowInputBuildName();
                break;
        }
    }

    private void ShowInputBuildName()
    {
        inputNameBuilding.SetActive(true);

        BuildingManager.Singleton.InputNameBuilding_Showed();   
    }

    private void ShowBuildingPreview()
    {
        selectingArea.SetActive(false);
        panelPreview.SetActive(true);

        var building = BuildingManager.Singleton.BuildPreview();
        building.view.layer = LayerMask.NameToLayer("UI");
        building.view.transform.SetParent(meshHolder);
        var widthScreenSpace = 1.38f;
        var scaleX = (Screen.width * UI.ScaleFactor) / (building.width * widthScreenSpace);
        var scaleY = (Screen.height * UI.ScaleFactor) / (building.height * 1.3f);
        var scaleZ = (Screen.width * UI.ScaleFactor) / (building.length * widthScreenSpace);
        building.view.transform.localScale = Vector3.one * Mathf.Min(scaleX, scaleY, scaleZ);
        building.ShiftPosition();
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

    private void CameraMove()
    {
        var joystickDir = moveCamJoystick.Direction;
        if (joystickDir != Vector2.zero)
        {
            var dir = new Vector3(joystickDir.x, 0, joystickDir.y);
            dir *= 5;
            CameraStack.Instance.SaveBuildingCamMove(dir);
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

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}

public enum AcceptMode
{
    Horizontal,
    Vertical,
    Preview
}
