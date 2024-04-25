using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;


[System.Serializable]
public class Item
{
    public byte id;
    public Sprite icon;
    public GameObject view;
    public bool stackable;
    public int count = 1;
    public int stackSize = 8;
}

[JsonObject]
public class JsonItem
{
    public byte id;
    public bool stackable;
    public int count;

    public JsonItem(Item item)
    {
        id = item.id;
        stackable = item.stackable;
        count = item.count;
    }

    public Item GetItem()
    {
        return new Item()
        {
            id = id,
            stackable = stackable,
            count = count
        };
    }

    [JsonConstructor]
    private JsonItem() { }
}
