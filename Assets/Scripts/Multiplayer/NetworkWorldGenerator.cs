using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;
using System.IO;
using System.Linq;

public class NetworkWorldGenerator : NetworkBehaviour
{
    WorldGenerator worldGenerator;

    public static string chuncksDirectory = $"{Application.dataPath}/Data/Chuncks/";
    public static string serverDirectory = "Server/";
    public static string clientDirectory = "Client/";

    List<ChunckComponent> offlineBlocksSeted = new List<ChunckComponent>();
    List<ChunckComponent> pendingChuncks = new List<ChunckComponent>();
    ChunckComponent currentPendingChunck;

    bool waitHandlingChunck;

    private void Awake()
    {
        WorldGenerator.onBlockPick.AddListener(Block_Mined);
        WorldGenerator.onBlockPlace.AddListener(Block_Placed);
        WorldGenerator.onTurnedBlockPlace.AddListener(TurnedBlock_Placed);
        ChunckComponent.onChunckInit.AddListener(Chunck_Inited);
        ChunckComponent.onBlocksSeted.AddListener(ChunckBlocks_Seted);
        NetworkUserManager.onUserRegistred.AddListener(UserOnServer_Registred);
    }

    private void ChunckBlocks_Seted(ChunckComponent chunckWithBlocks)
    {
        if (NetworkManager.IsConnectedClient)
        {
            if (IsClient)
            {
                SendChangedBlocksServerRpc(chunckWithBlocks.pos);
            }
        }
        else
        {
            if (!offlineBlocksSeted.Contains(chunckWithBlocks))
            {
                //print(chunckWithBlocks.pos);
                offlineBlocksSeted.Add(chunckWithBlocks);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendChangedBlocksServerRpc(Vector3 chunckPos, ServerRpcParams serverRpcParams = default)
    {
        var userName = NetworkUserManager.Instance.users[serverRpcParams.Receive.SenderClientId];
        var chunckDataFileName = GetChunckDataFileName(chunckPos);
        var path = $"{chuncksDirectory}{serverDirectory}{chunckDataFileName}.json";
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var chunckData = JsonConvert.DeserializeObject<ChunckData>(json);
            var changedBlocks = chunckData.changedBlocks;
            if (changedBlocks.Count > 0)
            {
                Vector3[] positions = changedBlocks.Select(b => b.Pos).ToArray();
                byte[] blockIDs = changedBlocks.Select(b => b.blockId).ToArray();
                ReceivePendingChunckBlocksDataClientRpc(positions, blockIDs, chunckPos, GetTargetClientParams(serverRpcParams));
            }
        }
        else
        {
            // ������ ���������� �������, ���������� � ���, ��� ���� �� �������
            ReceiveNoDataChunckBlocksClientRpc(chunckPos, GetTargetClientParams(serverRpcParams));
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void ReceiveNoDataChunckBlocksClientRpc(Vector3 chunckPos, ClientRpcParams clientRpcParams = default)
    {
        var chunck = worldGenerator.GetChunk(chunckPos);
        chunck.blocksLoaded = true;
    }

    private void Chunck_Inited(ChunckComponent emptyChunck)
    {
        var chunckFileName = GetChunckName(emptyChunck);
        var path = $"{chuncksDirectory}{serverDirectory}{chunckFileName}.json";
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);

            var chunckData = JsonConvert.DeserializeObject<ChunckData>(json, settings);
            emptyChunck.blocks = chunckData.blocks;
            emptyChunck.blocksLoaded = true;
        }

    }

    private IEnumerator Start()
    {
        worldGenerator = WorldGenerator.Inst;

        yield return null;
#if !UNITY_SERVER
        var chunck = worldGenerator.GetChunk(UserData.Owner.position.ToGlobalRoundBlockPos());
        offlineBlocksSeted.Add(chunck);
        //print(chunck.pos);
        chunck = worldGenerator.GetChunk(UserData.Owner.position.ToGlobalRoundBlockPos() + (Vector3.down * WorldGenerator.size));
        offlineBlocksSeted.Add(chunck);
        //print(chunck.pos);
#endif
    }

    private void Update()
    {
        if (IsClient)
        {
            if (!waitHandlingChunck && pendingChuncks.Count > 0)
            {// -16.00, 16.00, -16
                //print(pendingChuncks.Find(c => c.pos == new Vector3(-16, 16, -16)) != null);
                currentPendingChunck = pendingChuncks.First();
                //print(currentPendingChunck.pos);
                SendPendingChunckDataServerRpc(currentPendingChunck.pos);

                waitHandlingChunck = true;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendPendingChunckDataServerRpc(Vector3 chunckPos, ServerRpcParams serverRpcParams = default)
    {
        //var userName = NetworkUserManager.Instance.users[serverRpcParams.Receive.SenderClientId];
        var chunckDataFileName = GetChunckDataFileName(chunckPos);
        var path = $"{chuncksDirectory}{serverDirectory}{chunckDataFileName}.json";
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var chunckData = JsonConvert.DeserializeObject<ChunckData>(json);
            var changedBlocks = chunckData.changedBlocks;
            if(changedBlocks.Count > 0)
            {
                Vector3[] positions = changedBlocks.Select(b => b.Pos).ToArray();
                byte[] blockIDs = changedBlocks.Select(b => b.blockId).ToArray();
                ReceivePendingChunckBlocksDataClientRpc(positions, blockIDs, chunckPos, GetTargetClientParams(serverRpcParams));
            }
            else
            {
                SendNoChunckServerData(serverRpcParams.Receive.SenderClientId);
            }
        }
        else
        {
            SendNoChunckServerData(serverRpcParams.Receive.SenderClientId);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void ReceivePendingChunckBlocksDataClientRpc(Vector3[] positions, byte[] blockIDs, Vector3 chunckPos, ClientRpcParams clientRpcParams = default)
    {
        UpdateChunckMesh(positions, blockIDs, chunckPos);

        if (pendingChuncks.Count > 0)
        {
            pendingChuncks.Remove(currentPendingChunck);
        }
        waitHandlingChunck = false;

        // ���������
        StartCoroutine(Async());

        IEnumerator Async()
        {
            yield return new WaitForSeconds(0.5f);

            UpdateChunckMesh(positions, blockIDs, chunckPos);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void ReceiveChunckBlocksDataClientRpc(Vector3[] positions, byte[] blockIDs, Vector3 chunckPos, ClientRpcParams clientRpcParams = default)
    {
        UpdateChunckMesh(positions, blockIDs, chunckPos);
    }

    // ����������� �� �������
    private void UpdateChunckMesh(Vector3[] positions, byte[] blockIDs, Vector3 chunckPos)
    {
        //Debug.Break();

        StartCoroutine(Async());

        IEnumerator Async()
        {
            yield return null;

            var length = positions.Length;
            var chunck = worldGenerator.GetChunk(chunckPos);
            //print($"{chunckPos} ### {chunck.pos} ### {chunck.renderer.transform}");

            //yield return null;

            for (int i = 0; i < length; i++)
            {
                var pos = positions[i];
                var blockId = blockIDs[i];

                worldGenerator.SetBlock(pos, chunck, blockId);
            }

            worldGenerator.UpdateChunckMesh(chunck);
            chunck.renderer.gameObject.name = chunck.renderer.gameObject.name.Insert(0, $"{chunckPos} Srv Upd ");
            chunck.blocksLoaded = true;
        }
    }

    private void SendNoChunckServerData(ulong clientID)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { clientID };
        ReceiveNoServerChunckDataClientRpc(clientRpcParams);
    }

    [ClientRpc(RequireOwnership = false)]
    private void ReceiveNoServerChunckDataClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (pendingChuncks.Count > 0)
        {
            pendingChuncks.Remove(currentPendingChunck);
        }
        waitHandlingChunck = false;
    }

    private void Block_Placed(BlockData blockData)
    {
        if (IsClient)
        {
            SendBlockPlacedServerRpc(blockData.pos, blockData.ID);
        }
    }

    private void TurnedBlock_Placed(TurnedBlockData data)
    {
        if (IsClient)
        {
            NetworkTurnedBlockData networkData = new NetworkTurnedBlockData
            {
                pos = data.pos,
                blockID = data.ID,
                angle = data.angle,
                axis = data.axis
            };
            SendTurnedBlockPlacedServerRpc(networkData);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendTurnedBlockPlacedServerRpc(NetworkTurnedBlockData blockData, ServerRpcParams serverRpcParams = default)
    {
        print($"��� �� ������� {blockData.angle} ### {blockData.axis}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendBlockPlacedServerRpc(Vector3 blockPos, byte blockId, ServerRpcParams serverRpcParams = default)
    {
#if !UNITY_SERVER
        ChangeChunck(blockPos, blockId);
#endif
        SaveChangeChunck(blockPos, blockId, serverRpcParams);
        ReceiveBlockPlacedClientRpc(blockPos, blockId, serverRpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void ReceiveBlockPlacedClientRpc(Vector3 blockPos, byte blockID, ulong mineClientId)
    {
        if (mineClientId == NetworkManager.LocalClient.ClientId)
            return;
        // TO DO ��������, ����� �� ���������� ��� ������ ���� ��� ����� ����

        worldGenerator.SetBlockAndUpdateChunck(blockPos, blockID);
    }

    private void Block_Mined(BlockData data)
    {
        if (IsServer)
        {

        }
        else
        {
            //print("��������� ������ ������ �����");
            BlockMinedServerRpc(data.pos, data.ID);
            //SaveChangeChunckClientRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void BlockMinedServerRpc(Vector3 blockPos, byte blockID, ServerRpcParams serverRpcParams = default)
    {
        //print($"������ ���� {blockID} � {blockPos}");
#if !UNITY_SERVER
        ChangeChunck(blockPos, 0);
#endif
        SaveChangeChunck(blockPos, 0, serverRpcParams);
        ReceiveMinedBlockClientRpc(blockPos, serverRpcParams.Receive.SenderClientId);
    }

    private void SaveChangeChunck(Vector3 blockPos, byte blockID, ServerRpcParams serverRpcParams)
    {
        CheckDirectory(serverDirectory);

        var chunck = worldGenerator.GetChunk(blockPos);
        var chunckFileName = GetChunckName(chunck);
        var path = $"{chuncksDirectory}{serverDirectory}{chunckFileName}.json";

        ChunckData data = GetChunckData(path, chunck);

        ServerChangedDataBlocks(data, blockPos, blockID);

        var userName = NetworkUserManager.Instance.users[serverRpcParams.Receive.SenderClientId];
        var userChunckData = data.usersChangedBlocks.Find(u => u.userName == userName);
        if (userChunckData == null)
        {
            userChunckData = new ChunckData.UserChunckData
            {
                userName = userName
            };
            userChunckData.changedBlocks.Add(new(blockPos, blockID));
            data.usersChangedBlocks.Add(userChunckData);
        }
        else
        {
            var jsonBlockData = userChunckData.changedBlocks.Find(d => d.Pos == blockPos);
            if (jsonBlockData == null)
            {
                userChunckData.changedBlocks.Add(new(blockPos, blockID));
            }
            else
            {
                jsonBlockData.blockId = blockID;
            }
        }


        var json = JsonConvert.SerializeObject(data); //JsonUtility.ToJson(data);

        File.WriteAllText(path, json);
    }

    [ClientRpc]
    private void ReceiveMinedBlockClientRpc(Vector3 blockPos, ulong mineClientId)
    {
        if (mineClientId == NetworkManager.LocalClient.ClientId)
            return;
        // TO DO ��������, ����� �� ���������� ��� ������ ���� ��� ����� ����
        worldGenerator.SetBlockAndUpdateChunck(blockPos, 0);
    }

    private ChunckData GetChunckData(string path, ChunckComponent chunck)
    {
        ChunckData data;

        if (File.Exists(path))
        {
            var fileText = File.ReadAllText(path);
            data = JsonConvert.DeserializeObject<ChunckData>(fileText, settings);
            data.blocks = chunck.blocks;
        }
        else
        {
            data = new(chunck);
        }
        return data;
    }

    private void ServerChangedDataBlocks(ChunckData data, Vector3 blockPos, byte blockID)
    {
        var changedBlockData = data.changedBlocks.Find(d => d.Pos == blockPos);
        if (changedBlockData == null)
        {
            data.changedBlocks.Add(new ChunckData.JsonBlockData(blockPos, blockID));
        }
        else
        {
            changedBlockData.blockId = blockID;
        }
    }

    private void ChangeChunck(Vector3 blockPos, byte blockId)
    {
        worldGenerator.SetBlockAndUpdateChunck(blockPos, blockId);
    }

    void CheckDirectory(string subDirectory)
    {
        var path = $"{chuncksDirectory}{subDirectory}";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    string GetChunckName(ChunckComponent chunck)
    {
        return $"{chunck.pos.x}_{chunck.pos.y}_{chunck.pos.z}";
    }

    string GetChunckDataFileName(Vector3 chunckPos)
    {
        return $"{chunckPos.x}_{chunckPos.y}_{chunckPos.z}";
    }

    private void UserOnServer_Registred()
    {
        pendingChuncks.AddRange(offlineBlocksSeted);
        offlineBlocksSeted.Clear();
    }

    private ClientRpcParams GetTargetClientParams(ServerRpcParams serverRpcParams)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId };

        return clientRpcParams;
    }

    private static JsonSerializerSettings settings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        Error = (sender, eventArgs) =>
        {
            Debug.LogError(
                $"{eventArgs.ErrorContext.Error.Message} {eventArgs.ErrorContext.OriginalObject} {eventArgs.ErrorContext.Member}");
        }
    };
}
