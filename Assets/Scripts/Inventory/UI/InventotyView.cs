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

        foreach (var slot in slots) slot.Init();

        inventory.onTakeMain += Item_Taked;
        inventory.onUpdateItem += Item_Updated;
        inventory.onClose += Hide;
        inventory.onOpen += Show;

        btnClose.onClick.AddListener(inventory.Close);
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

    public override void Show()
    {
        IsShowed = true;

        base.Show();
    }

    public override void Hide()
    {
        IsShowed = false;

        base.Hide();
    }
}
