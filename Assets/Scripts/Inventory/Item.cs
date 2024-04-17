using System.Collections;
using System.Collections.Generic;
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
