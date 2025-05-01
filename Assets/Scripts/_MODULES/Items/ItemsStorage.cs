using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemsStorage : MonoBehaviour
{
    [SerializeField] ItemData[] itemsData;
    [Space(18)]
    [Header("«ƒ≈—‹ ¬—≈ –≈÷≈œ“€  –¿‘“Œ¬")]
    [SerializeField] ItemCraftableData[] itemsCraftableData;
    [Space(18)]
    [SerializeField] TurnableBlockData[] turnableBlockData;
    [Space(18)]
    [SerializeField] ItemID[] interactableBlocks;
    [Space(18)][Header("“”“ Œœ»—€¬¿… Õ¿  ¿ ŒÃ —“ŒÀ≈, ◊“Œ  –¿‘“»“—ﬂ")]
    [SerializeField] CraftingBundle[] craftingBundles;

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
                    Debug.LogError($"Õ›“ œ–›‘¿¡¿ ¬ –≈—¿’, ®œ“¿");
                }

                instance = Instantiate(storage);
            }

            return instance;
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
                if (item.colliderMesh)
                {
                    WorldGenerator.Inst.AddBlockableColliderMesh((byte)item.itemID, item.colliderMesh);
                }
            }
        }


        foreach (var item in turnableBlockData)
        {
            WorldGenerator.Inst.AddTurnableBlock((byte)item.itemID, item.rotationAxis);
        }

        //print("€€€€");
    }

    private IEnumerator Start()
    {

        yield return new WaitForEndOfFrame();

        
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

        Debug.LogError($"œÂ‰ÏÂÚ Ò ID “ËÔÓÏ {itemTypeID} ÕÂ Ì‡È‰ÂÌ !!!");

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

        Debug.LogError($"œÂ‰ÏÂÚ Ò ÔÓÒÚÓ ID {itemID} ÕÂ Ì‡È‰ÂÌ !!!");

        return default;
    }

    internal ItemData GetItemData(byte itemID)
    {
        return GetItemData((ItemID)itemID);
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

    public ItemCraftableData[] GetCratableItems()
    {
        return itemsCraftableData;
    }

    public ItemData[] GetItems()
    {
        return itemsData;
    }

    public CraftingBundle[] GetCraftingBundles()
    {
        return craftingBundles;
    }

    public bool HasCraftingBundle(byte itemID)
    {
        foreach (var bundle in craftingBundles)
        {
            if ((byte)bundle.craftingID == itemID)
            {
                return true;
            }
        }

        return false;
    }

    public CraftingBundle GetCraftingBundle(byte itemID)
    {
        foreach (var bundle in craftingBundles)
        {
            if((byte)bundle.craftingID == itemID)
            {
                return bundle; 
            }
        }

        Debug.LogError($"ÕÂÚ ÚÛÚ Ú‡ÍÓ„Ó „Ó‚Ì‡, Ú˚ ¯Ó, ˝Ô‡ÌÛÚ?");

        return default;
    }

    public ItemCraftableData[] GetCraftableItems(byte craftingTableID)
    {
        var bundle = GetCraftingBundle(craftingTableID);
        List<ItemCraftableData> result = new List<ItemCraftableData>();
        
        foreach (var bundleItem in bundle.items)
        {
            foreach (var itemCraftable in itemsCraftableData)
            {
                //print($"{itemCraftable.result[0].itemID}")
                if (itemCraftable.HasCraftResultItem(bundleItem))
                {
                    result.Add(itemCraftable);
                }
            }
        }

        return result.ToArray();
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
    public Mesh colliderMesh;

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

    public bool HasCraftResultItem(ItemID itemID)
    {
        foreach (var item in result)
        {
            if(item.itemID == itemID)
            {
                return true;
            }
        }

        return false;
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
#if UNITY_EDITOR
    public string name;
#endif
    public ItemID itemID;
    public ItemTypeID itemTypeID;
    public RotationAxis rotationAxis;
}

[System.Serializable]
public struct CraftingBundle
{
    public string name;
    public CraftingItemID craftingID;
    public ItemID[] items;
}

public enum ItemTypeID : byte
{
    NONE = 0,
    GRASS_BLOCK = 1,
    STONE = 2,
    COBBLESTONE = 3,
    DIRT = 4,
    WOOD = 9,
    LEAVES = 10,
    WOODEN_PLANK = 11,
    WOODEN_STAIR = 12,
}

public enum ItemID : byte
{
    NONE = 0,
    GRASS_BLOCK = 1,
    STONE = 2,
    COBBLESTONE = 3,
    DIRT = 4,
    WOOD = 9,
    LEAVES_BLOCK = 10,
    WOODEN_PLANK = 11,
    WOODEN_STAIR = 12,
    CLIFF = 14,
    COBBLESTONE_STAIR = 16,
    STONE_WORKBENCH = 50,
    SIMPLE_WOOD_WORKBENCH = 51,
    COLUMN_COBBLESTONE = 60,
    CLIFF_ROAD = 61,
    COBBLESTONE_WALL = 62,
    WALL_CLIFF = 63,
    SAND = 90,
    WOODEN_TIMBER = 92,
    INTERWOVEN_STONE = 94,
    WINDOW_SIMPLE_WOODEN = 120,
}

public enum CraftingItemID : byte
{
    SIMPLE_CRAFT = 0,
    STONE_WORKBENCH = 50,
    SIMPLE_WOOD_WORKBENCH = 51,
}

public enum ItemType : byte
{
    MESH, BLOCK, BLOCKABLE
}
