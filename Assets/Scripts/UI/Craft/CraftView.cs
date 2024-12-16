using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
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
    [SerializeField] GameObject bottomArea;
    [SerializeField] TMP_Text labelNoIngridients;
    [SerializeField] Button btnCraft;
    [SerializeField] CountControllerView countCraftItems;

    [HideInInspector] public UnityEvent onClose;

    CraftableItemView selectedCraftableItem;
    Character player;

    public void Init(Character character)
    {
        Init();

        player = character;

        btnClose.onClick.AddListener(Close_Clicked);
        btnCraft.onClick.AddListener(Craft_Clicked);
        countCraftItems.Init();
    }

    private void Craft_Clicked()
    {
        var craftableData = selectedCraftableItem.Data;
        foreach (var resultData in craftableData.result)
        {
            var item = new Item()
            {
                id = resultData.GetItemData().GetID(),
                count = resultData.count,
            };
            var itemData = resultData.GetItemData();
            if (itemData.itemType is ItemType.MESH or ItemType.BLOCKABLE)
            {
                item.view = Instantiate(itemData.view);
            }

            player.inventory.TakeItem(item);
            RemoveIngridients(craftableData);
            Item_Clicked(selectedCraftableItem);// Птом добавить метод Update, сейчас это чисто для обновление
        }
        
    }

    public void Show(byte craftingItemID)
    {
        Show();

        labelCraftInfo.gameObject.SetActive(true);
        craftInfoArea.gameObject.SetActive(false);
        bottomArea.SetActive(false);
        labelNoIngridients.gameObject.SetActive(false);

        var itemsCraftable = ItemsStorage.Singleton.GetCraftableItems(craftingItemID);

        ClearCraftInfo();
        InitItems(itemsCraftable);
    }

    private void InitItems(ItemCraftableData[] itemCraftableDatas)
    {
        ClearCraftableItems();

        var storage = ItemsStorage.Singleton;
        foreach (var itemData in itemCraftableDatas)
        {
            var itemView = Instantiate(itemPrefab, itemsParent);
            itemView.Init(itemData);
            itemView.onClick.AddListener(Item_Clicked);
        }
    }

    private void Item_Clicked(CraftableItemView itemView)
    {
        selectedCraftableItem = itemView;
        var craftableData = itemView.Data;

        labelCraftInfo.gameObject.SetActive(false);
        craftInfoArea.gameObject.SetActive(true);

        var availableCraft = CheckAvailableCraft(craftableData);
        bottomArea.SetActive(availableCraft);
        labelNoIngridients.gameObject.SetActive(!availableCraft);

        ClearCraftInfo();

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

    private bool CheckAvailableCraft(ItemCraftableData craftableData)
    {
        foreach (var ingridientData in craftableData.ingredients)
        {
            var hasItems = player.inventory.GetItem(ingridientData.GetID());
            var counts = hasItems.Sum(i => i.count);
            if (counts < ingridientData.count)
            {
                return false;
            }
        }

        return true;
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

    private void RemoveIngridients(ItemCraftableData craftableData)
    {
        foreach (var ingridientData in craftableData.ingredients)
        {
            var hasItems = player.inventory.GetItem(ingridientData.GetID());
            //Debug.Log($"DKSFub {hasItems.Count}:");

            var countRemoved = 0;
            foreach (var item in hasItems)
            {
                for (int i = 0; i <= item.count; i++)
                {
                    if (countRemoved < ingridientData.count)
                    {
                        player.inventory.Remove(item);
                        countRemoved++;
                    }
                    else
                    {
                        //break;
                    }
                }
            }            
        }
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
