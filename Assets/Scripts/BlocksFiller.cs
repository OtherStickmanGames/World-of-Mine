using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BLOCKS;

public class BlocksFiller : MonoBehaviour
{
    [SerializeField] Player player;
    [SerializeField] BlockName[] blocksNames;

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                var item = new Item() { id = (byte)blocksNames[0] };
                item.view = BlockItemSpawner.CreateBlockGameObject(item.id);
                item.count = 8;
                player.inventory.TakeItem(item);
            }
        }
    }
}
