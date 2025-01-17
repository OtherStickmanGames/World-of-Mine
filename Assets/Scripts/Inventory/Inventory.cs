using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Newtonsoft.Json;

public class Inventory
{
    public List<Item> quick = new();
    public List<Item> main = new();
    public int quickSize = 8;
    public int mainSize = 15;

    public Action onOpen;
    public Action onClose;
    public Action<Item> onRemoveItem;
    public Action<Item> onUpdateItem;
    public Action<Item> onTakeQuick;
    public Action<Item> onTakeMain;
    public Action<Item> onTakeItem;
    public Action<Inventory> onItemsSet;
    public Item CurrentSelectedItem { get; set; }
    public bool IsOpen { get; private set; }

    Character player;

    public Inventory(Character owner)
    {
        player = owner;
    }

    public void Open()
    {
        IsOpen = true;
        onOpen?.Invoke();
    }

    public void Close()
    {
        IsOpen = false;
        onClose?.Invoke();
    }

    public void TakeItem(Item item)
    {
        //Debug.Log("да ну на ");
        if (AvailableSpace(item))
        {
            // хер знает, попробую убрать
            //UnityEngine.Object.DestroyImmediate(item.view);

            var matched = ExistMatchInQuick(item);
            if (matched != null)
            {
                matched.count += item.count;
                UnityEngine.Object.DestroyImmediate(item.view);
                onUpdateItem?.Invoke(matched);
            }
            else if (quick.Count < quickSize)
            {

                quick.Add(item);
                onTakeQuick?.Invoke(item);
                onTakeItem?.Invoke(item);
            }
            else
            {
                matched = ExistMatchInMain(item);
                if (matched != null)
                {
                    matched.count += item.count;
                    UnityEngine.Object.DestroyImmediate(item.view);
                    onUpdateItem?.Invoke(matched);
                }
                else
                {
                    main.Add(item);
                    onTakeMain?.Invoke(item);
                    onTakeItem?.Invoke(item);
                }
            }

        }
    }

    public void Remove(Item item)
    {
        Debug.Log($"Remove {item.view}:{item.count}");
        item.count--;
        //onUpdateItem?.Invoke(item);

        if (item.count == 0)
        {
            onRemoveItem?.Invoke(item);

            if (item == CurrentSelectedItem)
            {
                CurrentSelectedItem = null;
            }

            if (quick.Contains(item))
            {
                quick.Remove(item);
            }
            else
            {
                main.Remove(item);
            }
        }

        onUpdateItem?.Invoke(item);
    }

    public void RemoveFullSlot(Item item)
    {
        if (item == CurrentSelectedItem)
        {
            CurrentSelectedItem = null;
        }
        //Debug.Log($"{quick.Count} =-=- {main.Count}");
        //Debug.Log($"{quick.Contains(item)} ### {ItemsStorage.Singleton.GetItemData(item.id).name}");
        if (quick.Contains(item))
        {
            quick.Remove(item);
        }
        else
        {
            main.Remove(item);
            //Debug.Log($"Ремувнул {item.id} {ItemsStorage.Singleton?.GetItemData(item.id).name}");
        }
        //Debug.Log($"After opta {quick.Count} =-=- {main.Count}");
    }

    Item ExistMatchInQuick(Item item)
    {
        var matched = quick.Find(i => i.id == item.id && i.count < i.stackSize);

        return matched;
    }

    Item ExistMatchInMain(Item item)
    {
        var matched = main.Find(i => i.id == item.id && i.count < i.stackSize);

        return matched;
    }

    public bool AvailableSpace(Item item)
    {
        //Debug.Log($"{ExistMatchInQuick(item) != null} ### {ExistMatchInMain(item) != null}");
        if (ExistMatchInQuick(item) != null || ExistMatchInMain(item) != null)
        {
            return true;
        }
        else 
        {
            //Debug.Log($"{main.Count} ### {mainSize}");
            if (quick.Count < quickSize || main.Count < mainSize)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool AvailableSpace(byte blockID)
    {
        var item = new Item() { id = blockID };
        return AvailableSpace(item);
    }

    List<Item> foundResult = new();
    public List<Item> GetItem(byte id)
    {
        foundResult.Clear();
        foreach (var item in quick)
        {
            if (item.id == id)
            {
                foundResult.Add(item);
            }
        }
        foreach (var item in main)
        {
            if (item.id == id)
            {
                foundResult.Add(item);
            }
        }

        return foundResult;
    }

    public void AddItemToQuick(Item item)
    {
        quick.Add(item);
        SaveInventory();
    }

    public void AddItemToMain(Item item)
    {
        main.Add(item);
        SaveInventory();
    }

    /// <summary>
    /// Метод вызывается когда спискам инвентаря присвоены
    /// значения из сохраненного Json-а 
    /// </summary>
    public void InvokeItemsSeted()
    {
        onItemsSet?.Invoke(this);
    }

    public void SetMainInventorySize(int value)
    {
        mainSize = value;
    }

    public void SaveInventory()
    {
        var item = main.Find(i => i.count < 1);
        if (item != null)
        {
            main.Remove(item);
            Debug.Log("Дропнул эбушку");
        }

        var jsonInventory = new JsonInventory(this);
        var json = JsonConvert.SerializeObject(jsonInventory);
        PlayerPrefs.SetString("inventory", json);
        PlayerPrefs.Save();
    }
}

[JsonObject]
public class JsonInventory
{
    public List<JsonItem> quick;
    public List<JsonItem> main;
    public int quickSize = 8;
    public int mainSize = 15;

    [JsonConstructor]
    private JsonInventory() { }

    public JsonInventory(Inventory inventory)
    {
        quick = inventory.quick.Select(i => new JsonItem(i)).ToList();
        main = inventory.main.Select(i => new JsonItem(i)).ToList();

        quickSize = inventory.quickSize;
        mainSize = inventory.mainSize;
    }

    public void SetInventoryData(Inventory inventory)
    {
        inventory.quick = quick.Select(i => i.GetItem()).ToList();
        inventory.main = main.Select(i => i.GetItem()).ToList();

        inventory.quickSize = quickSize;
        inventory.mainSize = mainSize;

        SetItemViews(inventory);

        inventory.InvokeItemsSeted();
    }

    private void SetItemViews(Inventory inventory)
    {
        //foreach (var item in inventory.quick)
        //{
        //    var itemData = ItemsStorage.Singleton.GetItemData(item.id);
        //    if (itemData.itemType is ItemType.MESH or ItemType.BLOCKABLE)
        //    {
        //        item.view = GameObject.Instantiate(itemData.view);
        //    }
        //}

        InitViews(inventory.quick);
        InitViews(inventory.main);

        void InitViews(List<Item> items)
        {
            foreach (var item in items)
            {
                var itemData = ItemsStorage.Singleton.GetItemData(item.id);
                if (itemData.itemType is ItemType.MESH or ItemType.BLOCKABLE)
                {
                    item.view = GameObject.Instantiate(itemData.view);
                }
            }
        }
    }

}

