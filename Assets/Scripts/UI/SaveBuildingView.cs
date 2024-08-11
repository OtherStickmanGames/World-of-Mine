using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveBuildingView : MonoBehaviour
{
    [SerializeField] Button btnSaveBuilding;
    [SerializeField] Button btnAccept;
    [SerializeField] RectTransform selectionBox;
    [SerializeField] GameObject selectingArea;
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


    public SelectionMode CurSelectionMode { get; set; }

    Vector3 startPressPos;
    bool selectionBoxDisplayed;
    bool allowSelectBlocks;

    public void Init()
    {
        selectionBox.gameObject.SetActive(false);
        btnAccept.gameObject.SetActive(false);
        selectingArea.SetActive(false);

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
            if (CurSelectionMode == SelectionMode.Vertical)
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
        }
    }

    private void CropHandle_Uped()
    {
        var scaleFactor = UI.ScaleFactor;
        var startPos = Camera.main.ScreenToWorldPoint(cropHandleLeftTop.GetPos() / scaleFactor);
        var endPos = Camera.main.ScreenToWorldPoint(cropHandleRightBottom.GetPos() / scaleFactor);

        BuildingManager.Singleton.SelectionHorizontal(startPos, endPos);

        btnAccept.gameObject.SetActive(true);
    }

    private void SaveBuilding_Clicked()
    {
        BuildingManager.Singleton.StartSelection();

        allowSelectBlocks = true;
        selectingArea.SetActive(true);

        UpdateSelectionBackgroud();
    }

    private void Accept_Clicked()
    {
        CurSelectionMode = SelectionMode.Vertical;
        BuildingManager.Singleton.SwitchSelection();
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
    }

    void UpdateZoomButtons()
    {
        if (btnZoomMinus.Pressed)
        {
            CameraStack.Instance.SaveBuildingCameraZoom(zoomValue * 1.05f);
        }
        if (btnZoomPlus.Pressed)
        {
            CameraStack.Instance.SaveBuildingCameraZoom(-zoomValue);
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
}

public enum SelectionMode
{
    Horizontal,
    Vertical
}
