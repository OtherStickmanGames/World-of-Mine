using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuickInventoryView : MonoBehaviour
{
    [SerializeField] List<InventorySlot> slots;

    public int Selected { get; set; } = 0;

    Action<Item> setSelectedItem;

    public void Init(Inventory inventory)
    {
        inventory.onTakeQuick += Item_Taked;
        inventory.onUpdateItem += Item_Updated;
        inventory.onItemSets += Items_Seted;

        foreach (var slot in slots)
        {
            slot.Init();
            slot.onClick.AddListener(Slot_Clicked);
            inventory.onRemoveItem += slot.RemoveItem;
        }

        setSelectedItem = i => inventory.CurrentSelectedItem = i;

        slots[0].Select();
    }


    public InventorySlot GetActiveSlot()
    {
        return slots[Selected];
    }

    private void Slot_Clicked(InventorySlot slot)
    {
        DeselectAllSlots();

        slot.Select();

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

    private void HotKeyInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            setSelectedItem.Invoke(slots[0].Item);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            setSelectedItem.Invoke(slots[1].Item);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            setSelectedItem.Invoke(slots[2].Item);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            setSelectedItem.Invoke(slots[3].Item);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            setSelectedItem.Invoke(slots[4].Item);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            setSelectedItem.Invoke(slots[5].Item);
        }
    }

    
}
