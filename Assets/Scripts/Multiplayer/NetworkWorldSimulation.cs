using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkWorldSimulation : NetworkBehaviour
{
    List<MonoBehaviour> players = new();

    private void Start()
    {
        NetworkManager.OnClientStarted += Client_Started;
        NetworkManager.OnClientStopped += Client_Stopped;
        NetworkManager.OnServerStarted += Server_Started;
    }

    private void Server_Started()
    {
        WorldSimulation.Single.StartSimulation();
        WorldSimulation.onBlockChanged.AddListener(Block_Changed);
        PlayerBehaviour.onAnyPlayerSpawn.AddListener(AnyPlayer_Spawned);
    }

    private void AnyPlayer_Spawned(MonoBehaviour player)
    {
        players.Add(player);
    }

    private void Block_Changed(Vector3 chunkPos, Vector3Int localBlockPos, byte blockID)
    {
        List<ulong> clientIds = new();
        foreach (var player in players)
        {
            if (!player)
                continue;

            var dist = Vector3.Distance(chunkPos, player.transform.position);
            if (dist < WorldGenerator.size * 18)
            {
                var networkBeh = player.GetComponent<NetworkObject>();
                clientIds.Add(networkBeh.OwnerClientId);
            }
        }
        var crp = NetTool.GetTargetClientParams(clientIds.ToArray());
        //foreach (var item in crp.Send.TargetClientIds)
        //{
        //    print(item + " =-=-=-=-=-=-");
        //}
        ReceiveChangeBlockClientRpc(chunkPos, localBlockPos, blockID, crp);
    }

    [ClientRpc(RequireOwnership = false)]
    private void ReceiveChangeBlockClientRpc(Vector3 chunkPos, Vector3Int localBlockPos, byte blockID, ClientRpcParams clientRpcParams = default)
    {
        //print($"ебать че получил {chunkPos}");
        WorldGenerator.Inst.SetBlockAndUpdateChunck(chunkPos + localBlockPos, blockID);
    }

    private void Client_Started()
    {
        WorldSimulation.onPlaceBlock.AddListener(Block_Placed);
        WorldSimulation.onBlockMine.AddListener(Block_Mined);
    }

    private void Block_Mined(ChunckComponent chunk, Vector3Int localBlockPos)
    {
        SendRemoveSimulatableBlocServerRpc(chunk.pos, localBlockPos);
    }


    [ServerRpc(RequireOwnership = false)]
    private void SendRemoveSimulatableBlocServerRpc(Vector3 chunkPos, Vector3Int localBlockPos, ServerRpcParams serverRpcParams = default)
    {
        WorldSimulation.Single.RemoveSimulatableBlockData(chunkPos, localBlockPos);
    }

    private void Block_Placed(ChunckComponent chunk, Vector3Int blockLocalPos, byte blockId)
    {
        var data = new SimulatableBlockData()
        {
            blockID = blockId,
            localBlockPos = blockLocalPos,
            changed = DateTime.UtcNow
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
        WorldSimulation.onBlockMine.RemoveListener(Block_Mined);

    }
}
