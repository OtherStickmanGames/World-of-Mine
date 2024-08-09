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
        if (allowSelectBlocks && !UI.ClickOnUI())
        {
            if (Input.GetMouseButtonDown(0))
            {
                selectionBoxDisplayed = true;
                selectionBox.gameObject.SetActive(true);
                btnAccept.gameObject.SetActive(false);
                startPressPos = Input.mousePosition;
                selectionBox.anchoredPosition = startPressPos * UI.ScaleFactor;
            }

            if (selectionBoxDisplayed)
            {
                var boxSize = Input.mousePosition - startPressPos;
                boxSize.y *= -1;
                selectionBox.sizeDelta = boxSize * UI.ScaleFactor;
            }

            if (Input.GetMouseButtonUp(0))
            {
                selectionBoxDisplayed = false;
                selectionBox.gameObject.SetActive(false);
                var startPos = Camera.main.ScreenToWorldPoint(startPressPos);
                var endPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                BuildingManager.Singleton.SelectionHorizontal(startPos, endPos);

                btnAccept.gameObject.SetActive(true);
            }
        }
    }
}

public enum SelectionMode
{
    Horizontal,
    Vertical
}
