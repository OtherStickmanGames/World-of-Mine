using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkWorldGenerator : NetworkBehaviour
{
    WorldGenerator worldGenerator;

    private void Awake()
    {
        worldGenerator = WorldGenerator.Inst;

        WorldGenerator.onBlockPick.AddListener(Block_Mined);
    }

    private void Block_Mined(BlockData data)
    {
        
    }
}
