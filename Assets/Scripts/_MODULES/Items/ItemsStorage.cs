using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemsStorage : MonoBehaviour
{
    [SerializeField] ItemData[] itemsData;
    [Space(18)]
    [SerializeField] ItemCraftableData[] itemsCraftableData;
    [Space(18)]
    [SerializeField] TurnableBlockData[] turnableBlockData;

    static ItemsStorage instance;
    public static ItemsStorage Singleton
    {
        get
        {
            if (!instance)
            {
                var storage = Resources.Load<ItemsStorage>("Items Storage");
                if (!storage)
                {
                    Debug.LogError($"НЭТ ПРЭФАБА В РЕСАХ, ЁПТА");
                }

                instance = Instantiate(storage);
            }

            return instance;
        }
    }

    ItemData foundResult;
    internal ItemData GetItemData(ItemTypeID itemTypeID)
    {
        var length = itemsData.Length;
        for (int i = 0; i < length; i++)
        {
            foundResult = itemsData[i];
            if (foundResult.itemTypeID == itemTypeID)
            {
                return foundResult;
            }
        }

        Debug.LogError($"Предмет с ID Типом {itemTypeID} Не найден !!!");

        return default;
    }

    internal ItemData GetItemData(ItemID itemID)
    {
        var length = itemsData.Length;
        for (int i = 0; i < length; i++)
        {
            foundResult = itemsData[i];
            if (foundResult.itemID == itemID)
            {
                return foundResult;
            }
        }

        Debug.LogError($"Предмет с просто ID {itemID} Не найден !!!");

        return default;
    }

    internal ItemData GetItemData(ItemCraftableData.PropsData props)
    {
        if (props.itemID is ItemID.NONE)
        {
            return GetItemData(props.itemTypeID);
        }
        else
        {
            return GetItemData(props.itemID);
        }
    }
    

    private void Awake()
    {
        instance = instance != null ? instance : this;

        WorldGenerator.onReady.AddListener(WG_Inited);

    }

    private void WG_Inited()
    {
        foreach (var item in itemsData)
        {
            if (item.itemType is ItemType.BLOCKABLE)
            {
                WorldGenerator.Inst.AddBlockableMesh((byte)item.itemID, item.view.transform);
            }
        }


        foreach (var item in turnableBlockData)
        {
            WorldGenerator.Inst.AddTurnableBlock((byte)item.itemID, item.rotationAxis);
        }

        //print("ЫЫЫЫ");
    }

    private IEnumerator Start()
    {

        yield return new WaitForEndOfFrame();

        
    }

    public ItemCraftableData[] GetCratableItems()
    {
        return itemsCraftableData;
    }

    public ItemData[] GetItems()
    {
        return itemsData;
    }
}

[System.Serializable]
public struct ItemData
{
    public string name;
    public ItemID itemID;
    public ItemTypeID itemTypeID;
    public ItemType itemType;
    [TextArea(1, 18)]
    public string description;
    public GameObject view;

    public byte GetID()
    {
        if (itemID is ItemID.NONE)
        {
            return (byte)itemTypeID;
        }
        else
        {
            return (byte)itemID;
        }
    }
    
}

[System.Serializable]
public struct ItemCraftableData
{
    public string name;
    [TextArea(1, 18)]
    public string description;
    public PropsData[] result;
    public PropsData[] ingredients;
    public float timeCrafting;

    public ItemData GetResultItemData(int idx = 0)
    {
        return ItemsStorage.Singleton.GetItemData(result[idx]);
    }


    [System.Serializable]
    public struct PropsData
    {
        public ItemID itemID;
        public ItemTypeID itemTypeID;
        public int count;


        public byte GetID()
        {
            if (itemID is ItemID.NONE)
            {
                return (byte)itemTypeID;
            }
            else
            {
                return (byte)itemID;
            }
        }

        public ItemData GetItemData()
        {
            return ItemsStorage.Singleton.GetItemData(this);
        }
    }
}


[System.Serializable]
public struct TurnableBlockData
{
    public ItemID itemID;
    public ItemTypeID itemTypeID;
    public RotationAxis rotationAxis;
}

public enum ItemTypeID : byte
{
    NONE = 0,
    GRASS = 1,
    STONE = 2,
    COBBLESTONE = 3,
    DIRT = 4,
    WOOD = 9,
    LEAVES = 10,
    WOODEN_PLANK = 11,
}

public enum ItemID : byte
{
    NONE = 0,
    STONE = 2,
    COBBLESTONE = 3,
    STONE_WORKBENCH = 50,
}

public enum ItemType : byte
{
    MESH, BLOCK, BLOCKABLE
}
