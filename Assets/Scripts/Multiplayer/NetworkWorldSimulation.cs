using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkWorldSimulation : NetworkBehaviour
{
    private void Start()
    {
        NetworkManager.OnClientStarted += Client_Started;
        NetworkManager.OnClientStopped += Client_Stopped;
    }

    private void Client_Started()
    {
        WorldSimulation.onPlaceBlock.AddListener(Block_Placed);
    }

    private void Block_Placed(ChunckComponent chunk, Vector3Int blockLocalPos, byte blockId)
    {
        var data = new SimulatableBlockData()
        {
            blockID = blockId,
            localBlockPos = blockLocalPos,
            changed = DateTime.Now
        };

        SendSimulatableBlockDataServerRpc(chunk.pos, data);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendSimulatableBlockDataServerRpc(Vector3 chunkPos, SimulatableBlockData data, ServerRpcParams serverRpcParams = default)
    {
        WorldSimulation.Single.SimalatebleBlockPlaced(chunkPos, data);
    }

    private void Client_Stopped(bool isHost_)
    {
        WorldSimulation.onPlaceBlock.RemoveListener(Block_Placed);
    }
}
