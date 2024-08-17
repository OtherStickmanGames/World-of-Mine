using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System;

public class ShowBuildingView : MonoBehaviour
{
    [SerializeField] Button btnShowBuildings;
    [SerializeField] GameObject buildingsView;
    [SerializeField] Button btnClose;
    [SerializeField] Button btnNextPage;
    [SerializeField] Button btnPrevPage;
    [SerializeField] TMP_Text labelPage;

    int page;

    public void Init()
    {
        btnShowBuildings.onClick.AddListener(ShowBuildings_Clicked);
        btnClose.onClick.AddListener(Close_Clicked);
        btnNextPage.onClick.AddListener(NextPage_Clicked);
        btnPrevPage.onClick.AddListener(PrevPage_Clicked);

        gameObject.SetActive(true);
        buildingsView.SetActive(false);

        UpdatePageBtnsView();
    }

    private void PrevPage_Clicked()
    {
        page--;
        UpdatePageBtnsView();
    }

    private void NextPage_Clicked()
    {
        page++;
        UpdatePageBtnsView();
    }

    private void Close_Clicked()
    {
        buildingsView.SetActive(false);
    }

    private void ShowBuildings_Clicked()
    {
        buildingsView.SetActive(true);

        BuildingManager.Singleton.SendRequestGetBuildings(page);
    }

    void UpdatePageBtnsView()
    {
        btnPrevPage.interactable = page != 0;

        labelPage.SetText($"Страница {page + 1}");

        StartCoroutine(Async());

        IEnumerator Async()
        {
            yield return null;

            LayoutRebuilder.ForceRebuildLayoutImmediate(labelPage.transform.parent as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(labelPage.transform as RectTransform);

        }
    }
}
