using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] Transform holder;
    [SerializeField] Image itemIcon;
    [SerializeField] Image selectOutline;
    [SerializeField] Button button;
    [SerializeField] TMP_Text txtCount;
    [SerializeField] Color selectColor;
    [SerializeField] Transform itemParent;

    [Space(18)]

    public UnityEvent<InventorySlot> onClick;
    public UnityEvent<InventorySlot> onSetItem;
    public UnityEvent<InventorySlot> onRemove;
    public UnityEvent<InventorySlot, Item> onSetWithSwap;

    public Item Item { get; private set; }
    public SlotType SlotType { get; private set; }


    Color normalColor;

    public void Init(SlotType slotType = SlotType.None)
    {
        SlotType = slotType;

        Deselect();

        UpdateView();

        button.onClick.AddListener(Slot_Clicked);
    }

    private void Slot_Clicked()
    {
        onClick?.Invoke(this);

        Select();
    }

    public void Select()
    {
        selectOutline.gameObject.SetActive(true);
    }

    public void Deselect()
    {
        selectOutline.gameObject.SetActive(false);
    }

    public void SetItem(Item item)
    {
        Item = item;
        UpdateView();

        onSetItem?.Invoke(this);
    }

    public void SetItemWithoutUpdate(Item item)
    {
        Item = item;
        //print($"Item count {Item.count}");
        onSetItem?.Invoke(this);
    }

    public void SetItemToSwap(Item item)
    {
        var oldItem = Item;
        Item = item;

        onSetWithSwap?.Invoke(this, oldItem);
    }

    public void RemoveItem(Item item)
    {
        if (item == Item)
        {
            Item = null;
            UpdateView();
        }
    }

    public void RemoveItem()
    {
        onRemove?.Invoke(this);
        //print($"{transform.parent?.parent?.parent?.parent?.parent}");
        Item = null;
        UpdateView();
    }

    public void UpdateView()
    {
        //print($"Update: Count {Item?.count} && Nameee {Item?.view} && Idx Slot {transform.GetSiblingIndex()+1}:{SlotType}");
        if (Item != null)
        {
            if (Item.icon)
            {
                itemIcon.sprite = Item.icon;
                itemIcon.enabled = true;
            }
            else
            {
                SetGameObjectView();
            }
            txtCount.text = Item.count > 1 ? $"x{Item.count}" : string.Empty;
        }
        else
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
            txtCount.text = string.Empty;

            foreach (Transform view in itemParent)
            {
                Destroy(view.gameObject);
            }
        }
    }

    private void SetGameObjectView()
    {
        if (itemParent.childCount > 0)// я так понимаю типа уже есть иконка и мы просто +1 к количеству
            return;
        //print($"«ырим иконка {Item.view}");
        if (Item.view == null)
        {
            Item.view = BlockItemSpawner.CreateDropedView(Item.id);
        }
        else
        {
            //print("есть чЄ");
        }

        Item.view.transform.SetParent(itemParent, false);
        Item.view.transform.localPosition = Vector3.zero;
        Item.view.transform.localRotation = Quaternion.Euler(Rotation(Item.id));

        Item.view.SetActive(true);
        Item.view.layer = 5;

        foreach (var view in Item.view.GetComponentsInChildren<Transform>())
        {
            view.gameObject.layer = 5;
        }
    }

    Vector3 Rotation(byte id)
    {
        //switch (id)
        //{
        //    case ITEMS.INGOT_IRON:
        //        return new(1.327f, 95.58f, -33.715f);
        //    case ITEMS.STICK:
        //        return new(-51f, 39f, 3.189f);
        //    case ITEMS.AXE_WOODEN:
        //        return new(18.385f, 208.087f, -41.801f);
        //}

        return Vector3.zero;
    }
}

public enum SlotType
{
    None, Quick, Main
}
