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

    bool waitHandlingChunck;

    private void Awake()
    {
        WorldGenerator.onBlockPick.AddListener(Block_Mined);
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
            offlineBlocksSeted.Add(chunckWithBlocks);
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
            var userChunckData = chunckData.usersChangedBlocks.Find(u => u.userName == userName);
            if (userChunckData != null)
            {
                Vector3[] positions = userChunckData.changedBlocks.Select(b => b.Pos).ToArray();
                byte[] blockIDs = userChunckData.changedBlocks.Select(b => b.blockId).ToArray();
                ReceiveChunckBlocksDataClientRpc(positions, blockIDs, chunckPos, GetTargetClientParams(serverRpcParams));
            }
        }
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
        offlineBlocksSeted.Add(worldGenerator.GetChunk(UserData.Owner.position.ToGlobalRoundBlockPos()));
        offlineBlocksSeted.Add(worldGenerator.GetChunk(UserData.Owner.position.ToGlobalRoundBlockPos() + (Vector3.down * WorldGenerator.size)));

    }

    private void Update()
    {
        if (IsClient)
        {
            if (!waitHandlingChunck && pendingChuncks.Count > 0)
            {
                var chunck = pendingChuncks.First();

                SendPendingChunckDataServerRpc(chunck.pos);

                waitHandlingChunck = true;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendPendingChunckDataServerRpc(Vector3 chunckPos, ServerRpcParams serverRpcParams = default)
    {
        var userName = NetworkUserManager.Instance.users[serverRpcParams.Receive.SenderClientId];
        var chunckDataFileName = GetChunckDataFileName(chunckPos);
        var path = $"{chuncksDirectory}{serverDirectory}{chunckDataFileName}.json";
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var chunckData = JsonConvert.DeserializeObject<ChunckData>(json);
            var userChunckData = chunckData.usersChangedBlocks.Find(u => u.userName == userName);
            if (userChunckData == null)
            {
                SendNoChunckServerData(serverRpcParams.Receive.SenderClientId);
            }
            else
            {
                Vector3[] positions = userChunckData.changedBlocks.Select(b => b.Pos).ToArray();
                byte[] blockIDs = userChunckData.changedBlocks.Select(b => b.blockId).ToArray();
                ReceivePendingChunckBlocksDataClientRpc(positions, blockIDs, chunckPos, GetTargetClientParams(serverRpcParams));
            }
        }
        else
        {
            SendNoChunckServerData(serverRpcParams.Receive.SenderClientId);
        }

        print("ёлы палы");
    }

    [ClientRpc(RequireOwnership = false)]
    private void ReceivePendingChunckBlocksDataClientRpc(Vector3[] positions, byte[] blockIDs, Vector3 chunckPos, ClientRpcParams clientRpcParams = default)
    {
        UpdateChunckMesh(positions, blockIDs, chunckPos);

        waitHandlingChunck = false;
    }

    [ClientRpc(RequireOwnership = false)]
    private void ReceiveChunckBlocksDataClientRpc(Vector3[] positions, byte[] blockIDs, Vector3 chunckPos, ClientRpcParams clientRpcParams = default)
    {
        UpdateChunckMesh(positions, blockIDs, chunckPos);
    }

    // Выполняется на клиенте
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
            chunck.renderer.gameObject.name = chunck.renderer.gameObject.name.Insert(0, $"{chunckPos} ^^^ ");
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
            pendingChuncks.RemoveAt(0);
        }
        waitHandlingChunck = false;
    }

    private void Block_Mined(BlockData data)
    {
        if (IsServer)
        {

        }
        else
        {
            //print("отправили запрос добычи блока");
            BlockMinedServerRpc(data.pos, data.ID);
            //SaveChangeChunckClientRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void BlockMinedServerRpc(Vector3 blockPos, byte blockID, ServerRpcParams serverRpcParams = default)
    {
        //print($"Пришел блок {blockID} в {blockPos}");
        ChangeChunck(blockPos);
        SaveChangeChunck(blockPos, 0, serverRpcParams);
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
    private void SaveChangeChunckClientRpc()
    {

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

    private void ChangeChunck(Vector3 blockPos)
    {
        worldGenerator.SetBlockAndUpdateChunck(blockPos, 0);
    }

    void CheckDirectory(string subDirectory)
    {
        var path = $"{chuncksDirectory}{subDirectory}";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerSpawnedServerRpc(string userName)
    {
        print(userName);
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
        ReceiveNoServerChunckDataClientRpc(clientRpcParams);

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
