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
    [SerializeField] GameObject buildingInfoMessage;
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
        BuildingManager.Singleton.onBuildingListEnded.AddListener(BuildingList_Ended);

        buildingsView.SetActive(false);
        buildingInfoMessage.SetActive(false);

        
    }

    private void OnEnable()
    {
        UpdatePageBtnsView();
    }

    private void BuildingList_Ended()
    {
        btnNextPage.interactable = false;
    }

    private void LoadedPreview_Builded(BuildPreviewData preview, BuildingServerData serverData)
    {
        var loadedItem = buildingItems.Find(i => i.Guid == serverData.guid);
        if (loadedItem)
        {
            loadedItem.gameObject.SetActive(true);
        }
        else
        {
            var previewItem = Instantiate(previewItemPrefab, parent);
            previewItem.Init(preview, serverData);
            previewItem.onLikeClick.AddListener(BuildingLike_Clicked);
            previewItem.onBuildingClick.AddListener(Building_Clicked);
            buildingItems.Add(previewItem);
        }
    }

    private void Building_Clicked(BuildingPreviewItem buildingItem)
    {
        buildingInfoMessage.SetActive(true);
    }

    private void BuildingLike_Clicked(BuildingPreviewItem buildingItem)
    {
        BuildingManager.Singleton.SetBuildingLike(buildingItem.Guid);
    }

    private void PrevPage_Clicked()
    {
        page--;
        UpdatePageBtnsView();
        LoadItems();
        btnNextPage.interactable = true;
    }

    private void NextPage_Clicked()
    {
        page++;
        UpdatePageBtnsView();
        LoadItems();
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
        LoadItems();
        InputLogic.Singleton.AvailableMouseScrollWorld = false;
    }

    private void LoadItems()
    {
        ClearItems();
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

    private void ClearItems()
    {
        foreach (var item in buildingItems)
        {
            item.gameObject.SetActive(false);
        }
    }
}
