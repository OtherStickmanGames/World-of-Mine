using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockItemSpawner : IUpdateble
{
    DropedBlockGenerator blockGenerator = new();
    List<SpawnedBlockData> spawnedBlocks = new();
    List<SpawnedBlockData> removable = new();

    public static BlockItemSpawner Instance;

    public BlockItemSpawner()
    {
        Instance = this;
        
        WorldGenerator.onBlockPick.AddListener(Block_Picked);
    }

    public void Update()
    {
        foreach (var block in spawnedBlocks)
        {
            block.view.transform.Rotate(Vector3.up, 1f);
            block.lifetime += Time.deltaTime;

            foreach (var player in GameManager.Inst.players)
            {
                CheckDistanceToPlayer(player, block);
            }
        }

        foreach (var item in removable)
        {
            spawnedBlocks.Remove(item);
            //Object.Destroy(item.view);
        }
        removable.Clear();
    }

    void CheckDistanceToPlayer(Character player, SpawnedBlockData data)
    {
        if (data.lifetime > 1f)
        {
            if (!data.view)
                return;

            if (!player)
                return;

            var dist = Vector3.Distance(player.transform.position, data.view.transform.position);

            if (dist < 5 && player.inventory.AvailableSpace(data.ID))
            {
                var dir = player.transform.position - data.view.transform.position;
                dir.Normalize();

                if (dist < 0.18f)
                {
                    TakeBlock(player, data);   
                }
                else
                {
                    data.view.transform.position += (5 / dist) * Time.deltaTime * dir;
                }
            }
        }
    }

    void TakeBlock(Character player, SpawnedBlockData block)
    {
        var item = new Item()
        {
            id = block.ID,
            view = block.view,
        };

        player.inventory.TakeItem(item);
        removable.Add(block);

        EventsHolder.onItemTaked?.Invoke(player, block);
    }

    private void Block_Picked(BlockData data)
    {
        if (data.ID == 2)
        {
            data.ID = 3;
        }
        
        var dropedView = CreateDropedView(data.ID);

        float offsetRandomX = Random.Range(0.3f, 0.57f) - 1;
        float offsetRandomY = Random.Range(0.3f, 0.57f);
        float offsetRandomZ = Random.Range(0.3f, 0.57f);

        dropedView.transform.position = data.pos + new Vector3(offsetRandomX, offsetRandomY, offsetRandomZ);

        spawnedBlocks.Add(new() { ID = data.ID, view = dropedView });
    }

    public static GameObject CreateDropedView(byte ID)
    {
        GameObject view;
        var itemData = ItemsStorage.Singleton.GetItemData(ID);
        if (itemData.itemType is ItemType.MESH or ItemType.BLOCKABLE)
        {
            view = GameObject.Instantiate(itemData.view);
            view.layer = 0;
        }
        else
        {
            view = new GameObject($"Droped Block - {ID}");
            view.AddComponent<MeshRenderer>().material = WorldGenerator.Inst.mat;
            view.AddComponent<MeshFilter>().mesh = Instance.blockGenerator.GenerateBlockMesh(ID);
            view.transform.localScale /= 3f;
        }

        return view;
    }
  
}

public class SpawnedBlockData
{
    public byte ID;
    public GameObject view;
    public float lifetime;
}


