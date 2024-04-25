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
        if (AvailableSpace(item))
        {
            UnityEngine.Object.DestroyImmediate(item.view);

            var matched = ExistMatchInQuick(item);
            if (matched != null)
            {
                matched.count += item.count;
                //UnityEngine.Object.Destroy(item.view);
                onUpdateItem?.Invoke(matched);
            }
            else if (quick.Count < quickSize)
            {
                quick.Add(item);
                onTakeQuick?.Invoke(item);
            }
            else
            {
                matched = ExistMatchInMain(item);
                if (matched != null)
                {
                    matched.count += item.count;
                    onUpdateItem?.Invoke(matched);
                }
                else
                {
                    main.Add(item);
                    onTakeQuick?.Invoke(item);
                }
            }

        }
    }

    public void Remove(Item item)
    {
        item.count--;
        onUpdateItem?.Invoke(item);
        
        if (item.count > 0)
            return;

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
        if (ExistMatchInQuick(item) != null || ExistMatchInMain(item) != null)
        {
            return true;
        }
        else 
        {
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
}

[JsonObject]
public class JsonInventory
{
    public List<JsonItem> quick;
    public List<JsonItem> main;
    public int quickSize = 8;
    public int mainSize = 15;

    public JsonInventory(Inventory inventory)
    {
        quick = inventory.quick.Select(i => new JsonItem(i)).ToList();
        main = inventory.main.Select(i => new JsonItem(i)).ToList();

        quickSize = inventory.quickSize;
        mainSize = inventory.mainSize;
    }
}

