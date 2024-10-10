using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using TMPro;


public class CraftView : ViewUI
{
    [SerializeField] Button btnClose;
    [SerializeField] CraftableItemView itemPrefab;
    [SerializeField] Transform itemsParent;
    [SerializeField] TMP_Text labelCraftInfo;
    [SerializeField] Transform craftInfoArea;
    [SerializeField] CraftItemSlot itemSlotPrefab;
    [SerializeField] Transform resultItemsParent;
    [SerializeField] Transform ingridientsParent;
    [SerializeField] TMP_Text labelDescription;

    [HideInInspector] public UnityEvent onClose;

    public override void Init()
    {
        base.Init();

        btnClose.onClick.AddListener(Close_Clicked);
    }

    public override void Show()
    {
        base.Show();

        labelCraftInfo.gameObject.SetActive(true);
        craftInfoArea.gameObject.SetActive(false);

        ClearCraftInfo();
        InitItems();
    }

    private void InitItems()
    {
        ClearCraftableItems();

        var storage = ItemsStorage.Singleton;
        foreach (var itemData in storage.GetCratableItems())
        {
            var itemView = Instantiate(itemPrefab, itemsParent);
            itemView.Init(itemData);
            itemView.onClick.AddListener(Item_Clicked);
        }
    }

    private void Item_Clicked(CraftableItemView itemView)
    {
        labelCraftInfo.gameObject.SetActive(false);
        craftInfoArea.gameObject.SetActive(true);

        ClearCraftInfo();

        var craftableData = itemView.Data;
        foreach (var itemData in craftableData.result)
        {
            var slotView = Instantiate(itemSlotPrefab, resultItemsParent);
            slotView.Init(itemData);

            LayoutRebuilder.ForceRebuildLayoutImmediate(resultItemsParent as RectTransform);
        }

        foreach (var itemData in craftableData.ingredients)
        {
            var slotView = Instantiate(itemSlotPrefab, ingridientsParent);
            slotView.Init(itemData);
        }

        var description = string.IsNullOrEmpty(craftableData.description) ? craftableData.GetResultItemData().description : craftableData.description;
        labelDescription.SetText(description);
    }

    private void ClearCraftInfo()
    {
        foreach (Transform item in resultItemsParent)
        {
            Destroy(item.gameObject);
        }
        foreach (Transform item in ingridientsParent)
        {
            Destroy(item.gameObject);
        }
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(resultItemsParent as RectTransform);
    }

    private void ClearCraftableItems()
    {
        foreach (Transform item in itemsParent)
        {
            Destroy(item.gameObject);
        }
    }

    private void Close_Clicked()
    {
        Hide();
        onClose?.Invoke();
    }
}
