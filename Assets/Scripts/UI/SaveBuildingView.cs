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

    [Space]

    [SerializeField] RectTransform leftPlane;
    [SerializeField] RectTransform rightPlane;
    [SerializeField] RectTransform topPlane;
    [SerializeField] RectTransform bottomPlane;

    [Space]

    [SerializeField] InteractableStateTracker cropHandleLeftTop;
    [SerializeField] InteractableStateTracker cropHandleRightBottom;


    public SelectionMode CurSelectionMode { get; set; }

    Vector3 startPressPos;
    bool selectionBoxDisplayed;
    bool allowSelectBlocks;

    public void Init()
    {
        selectionBox.gameObject.SetActive(false);
        btnAccept.gameObject.SetActive(false);

        btnSaveBuilding.onClick.AddListener(SaveBuilding_Clicked);
        btnAccept.onClick.AddListener(Accept_Clicked);
    }

    private void SaveBuilding_Clicked()
    {
        BuildingManager.Singleton.StartSelection();

        allowSelectBlocks = true;
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
            var mousePos = Input.mousePosition;
            var scaleFactor = UI.ScaleFactor;

            if (cropHandleLeftTop.Pressed)
            {
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

            if (cropHandleRightBottom.Pressed)
            {
                cropHandleRightBottom.SetPos(mousePos * UI.ScaleFactor);

                var size = rightPlane.sizeDelta;
                size.x = Screen.width - mousePos.x;
                rightPlane.sizeDelta = size * scaleFactor;

                //var width = Screen.width - (mousePos.x + (rightPlane.sizeDelta.x / scaleFactor));
                //size = topPlane.sizeDelta;
                //size.x = width;
                //size.y = Screen.height - mousePos.y;
                //topPlane.sizeDelta = size * scaleFactor;

                //size = bottomPlane.sizeDelta;
                //size.x = width * scaleFactor;
                //bottomPlane.sizeDelta = size;

                //var planePos = topPlane.anchoredPosition;
                //planePos.x = mousePos.x;
                //topPlane.anchoredPosition = planePos * scaleFactor;

                //planePos = bottomPlane.anchoredPosition;
                //planePos.x = mousePos.x;
                //bottomPlane.anchoredPosition = planePos * scaleFactor;
            }
        }

        //if (allowSelectBlocks && !UI.ClickOnUI())
        //{
        //    if (Input.GetMouseButtonDown(0))
        //    {
        //        selectionBoxDisplayed = true;
        //        selectionBox.gameObject.SetActive(true);
        //        btnAccept.gameObject.SetActive(false);
        //        startPressPos = Input.mousePosition;
        //        selectionBox.anchoredPosition = startPressPos * UI.ScaleFactor;
        //    }

        //    if (selectionBoxDisplayed)
        //    {
        //        var scaleFactor = UI.ScaleFactor;
        //        var mousePos = Input.mousePosition;

        //        var minX = Mathf.Min(mousePos.x, startPressPos.x);
        //        var maxX = Mathf.Max(mousePos.x, startPressPos.x);
        //        var maxY = Mathf.Max(mousePos.y, startPressPos.y);
        //        var minY = Mathf.Min(mousePos.y, startPressPos.y);

        //        //var size = leftPlane.sizeDelta;


        //        //size = rightPlane.sizeDelta;
        //        //size.x = Screen.width - maxX;
        //        //rightPlane.sizeDelta = size * scaleFactor;

        //        //size = topPlane.sizeDelta;
        //        //size.x = maxX - minX;
        //        //size.y = Screen.height - maxY;
        //        //topPlane.sizeDelta = size * scaleFactor;

        //        //size = bottomPlane.sizeDelta;
        //        //size.x = maxX - minX;
        //        //size.y = minY;
        //        //bottomPlane.sizeDelta = size * scaleFactor;

        //        //var planePos = topPlane.anchoredPosition;
        //        //planePos.x = minX;
        //        //topPlane.anchoredPosition = planePos * scaleFactor;

        //        //planePos = bottomPlane.anchoredPosition;
        //        //planePos.x = minX;
        //        //bottomPlane.anchoredPosition = planePos * scaleFactor;

        //        var boxSize = Input.mousePosition - startPressPos;
        //        boxSize.y *= -1;
        //        selectionBox.sizeDelta = boxSize * UI.ScaleFactor;

                
        //    }

        //    if (Input.GetMouseButtonUp(0))
        //    {
        //        selectionBoxDisplayed = false;
        //        selectionBox.gameObject.SetActive(false);
        //        var startPos = Camera.main.ScreenToWorldPoint(startPressPos);
        //        var endPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //        BuildingManager.Singleton.SelectionHorizontal(startPos, endPos);

        //        btnAccept.gameObject.SetActive(true);
        //    }
        //}
    }
}

public enum SelectionMode
{
    Horizontal,
    Vertical
}
