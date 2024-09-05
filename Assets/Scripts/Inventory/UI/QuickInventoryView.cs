using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuickInventoryView : MonoBehaviour
{
    [SerializeField] List<InventorySlot> slots;

    public int Selected { get; set; } = 0;

    Action<Item> setSelectedItem;

    Inventory inventory;

    public void Init(Inventory inventory)
    {
        inventory.onTakeQuick += Item_Taked;
        inventory.onUpdateItem += Item_Updated;
        inventory.onItemSets += Items_Seted;
        this.inventory = inventory;

        foreach (var slot in slots)
        {
            slot.Init();
            slot.onClick.AddListener((s) => Slot_Clicked(s, inventory));
            inventory.onRemoveItem += slot.RemoveItem;
        }

        setSelectedItem = i => inventory.CurrentSelectedItem = i;

        slots[0].Select();
    }


    public InventorySlot GetActiveSlot()
    {
        return slots[Selected];
    }

    private void Slot_Clicked(InventorySlot slot, Inventory inventory)
    {
        DeselectAllSlots();

        slot.Select();

        inventory.CurrentSelectedItem = slot.Item;

        Selected = slot.transform.GetSiblingIndex();
    }

    void DeselectAllSlots()
    {
        foreach (var slot in slots) slot.Deselect();
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
        var slotIdx = slots.IndexOf(slot);
        if (slotIdx == Selected)
        {
            inventory.CurrentSelectedItem = item;
        }
    }

    private void Items_Seted(Inventory inventory)
    {
        foreach (var item in inventory.quick)
        {
            Item_Taked(item);
        }
    }

    private void Update()
    {
        HotKeyInput();
    }

    public void SelectSlot(int idx)
    {
        Selected = idx;
        DeselectAllSlots();
        slots[idx].Select();
        setSelectedItem.Invoke(slots[idx].Item);
    }

    private void HotKeyInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectSlot(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectSlot(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectSlot(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SelectSlot(3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SelectSlot(4);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            SelectSlot(5);
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            SelectSlot(6);
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            SelectSlot(7);
        }
    }

    
}
