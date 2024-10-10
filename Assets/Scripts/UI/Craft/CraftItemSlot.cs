using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CraftItemSlot : MonoBehaviour
{
    [SerializeField] InventorySlot slot;
    [SerializeField] TMP_Text title;

    internal void Init(ItemCraftableData.PropsData itemData)
    {
        ItemData data;
        if (itemData.itemID == ItemID.NONE)
        {
            data = ItemsStorage.Singleton.GetItemData(itemData.itemTypeID);
        }
        else
        {
            data = ItemsStorage.Singleton.GetItemData(itemData.itemID);
        }

        title.SetText(data.name);
        var itemID = itemData.itemID == ItemID.NONE ? (byte)itemData.itemTypeID : (byte)itemData.itemID;

        var item = new Item()
        {
            id = itemID,
            count = itemData.count,
        };

        if (data.itemType is ItemType.MESH or ItemType.BLOCKABLE)
        {
            //item.view = 
        }
        slot.SetItem(item);
    }
}
