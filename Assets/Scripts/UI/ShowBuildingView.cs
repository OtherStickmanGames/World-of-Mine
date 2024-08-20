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
    [SerializeField] Transform parent;
    [SerializeField] BuildingPreviewItem previewItemPrefab;

    int page;

    public void Init()
    {
        btnShowBuildings.onClick.AddListener(ShowBuildings_Clicked);
        btnClose.onClick.AddListener(Close_Clicked);
        btnNextPage.onClick.AddListener(NextPage_Clicked);
        btnPrevPage.onClick.AddListener(PrevPage_Clicked);

        BuildingManager.Singleton.onLoadedPreviewBuild.AddListener(LoadedPreview_Builded);

        gameObject.SetActive(true);
        buildingsView.SetActive(false);

        UpdatePageBtnsView();
    }

    private void LoadedPreview_Builded(BuildPreviewData preview, BuildingServerData serverData)
    {
        var previewItem = Instantiate(previewItemPrefab, parent);
        previewItem.Init(preview, serverData);

        preview.view.layer = LayerMask.NameToLayer("UI");
        preview.view.transform.SetParent(parent);

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
