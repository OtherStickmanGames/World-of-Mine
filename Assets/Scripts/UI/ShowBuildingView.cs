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
    [SerializeField] ScrollRect scrollRect;

    List<BuildingPreviewItem> buildingItems = new List<BuildingPreviewItem>();
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
        if (buildingItems.Find(i => i.Guid == serverData.guid))
            return;

        var previewItem = Instantiate(previewItemPrefab, parent);
        previewItem.Init(preview, serverData);
        previewItem.onLikeClick.AddListener(BuildingLike_Clicked);
        buildingItems.Add(previewItem);
    }

    private void BuildingLike_Clicked(BuildingPreviewItem buildingItem)
    {
        BuildingManager.Singleton.SetBuildingLike(buildingItem.Guid);
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
        BuildingManager.Singleton.InvokeBuildingListHide();
        InputLogic.Singleton.AvailableMouseScrollWorld = true;
    }

    private void ShowBuildings_Clicked()
    {
        buildingsView.SetActive(true);

        BuildingManager.Singleton.InvokeBuildingListShow();
        BuildingManager.Singleton.SendRequestGetBuildings(page);
        InputLogic.Singleton.AvailableMouseScrollWorld = false;
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

    private void Update()
    {
        var rotatingItem = buildingItems.Find(i => i.IsPreviewRotating);
        if (rotatingItem != null && scrollRect.enabled)
        {
            scrollRect.enabled = false;
        }
        if (!scrollRect.enabled && rotatingItem == null)
        {
            scrollRect.enabled = true;
        }
    }
}
