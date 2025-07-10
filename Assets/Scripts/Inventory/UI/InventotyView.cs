using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System;

public class InventotyView : ViewUI
{
    [SerializeField] List<InventorySlot> slots;
    [SerializeField] Transform location;
    [SerializeField] Button btnClose;

    public bool IsShowed { get; private set; }


    public void Init(Inventory inventory)
    {
        targetPosition = location.position;

        Init();

        foreach (var slot in slots)
        {
            slot.Init(SlotType.Main);
            slot.onSetItem.AddListener((s) => SlotItem_Seted(s, inventory));
            slot.onRemove.AddListener((s) => SlotItem_Removed(s, inventory));
            slot.onSetWithSwap.AddListener((s, oldItem) => SlotItemSwap_Seted(s, oldItem, inventory));
            inventory.onRemoveItem += slot.RemoveItem;
        }

        inventory.onTakeMain += Item_Taked;
        inventory.onUpdateItem += Item_Updated;
        inventory.onItemsSet += Items_Seted;
        inventory.onClose += Hide;
        inventory.onOpen += Show;

        btnClose.onClick.AddListener(inventory.Close);
    }

    private void SlotItemSwap_Seted(InventorySlot slot, Item oldItem, Inventory inventory)
    {
        print($"(Main) SlotItemSwap_Seted {slot.Item?.view} ### {oldItem?.view}");

        inventory.RemoveFullSlot(oldItem);

        var foundItem = inventory.main.Find(item => item == slot.Item);
        if (foundItem == null)
        {
            inventory.AddItemToMain(slot.Item);
            //print("добавлено");
        }
    }

    private void SlotItem_Removed(InventorySlot s, Inventory inventory)
    {
        //print($"{ItemsStorage.Singleton.GetItemData(s.Item.id).name}");
        inventory.RemoveFullSlot(s.Item);
    }

    private void SlotItem_Seted(InventorySlot slot, Inventory inventory)
    {
        var foundItem = inventory.main.Find(item => item == slot.Item);
        if (foundItem == null)
        {
            inventory.AddItemToMain(slot.Item);
            print("добавлено в основной инвентарь");
        }
    }

    private void Item_Updated(Item item)
    {
        var slot = slots.Find(s => s.Item == item);
        slot?.UpdateView();
    }

    private void Item_Taked(Item item)
    {
        var slot = slots.Find(s => s.Item == null);
        slot.SetItem(item);
    }

    private void Items_Seted(Inventory inventory)
    {
        foreach (var item in inventory.main)
        {
            Item_Taked(item);
        }
    }

    public override void Show()
    {
        IsShowed = true;

        InputLogic.ShowCursor();

        base.Show();
    }

    public override void Hide()
    {
        IsShowed = false;

        InputLogic.HideCursor();

        base.Hide();
    }

    public void ClearSlots()
    {
        foreach (var slot in slots)
        {
            slot.RemoveItem();
        }
    }
}
