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
                PrepareAndSendTurnedBlocksData(chunckData, chunckPos, GetTargetClientParams(serverRpcParams));

                Vector3[] positions = changedBlocks.Select(b => b.Pos).ToArray();
                byte[] blockIDs = changedBlocks.Select(b => b.blockId).ToArray();
                ReceivePendingChunckBlocksDataClientRpc(positions, blockIDs, chunckPos, GetTargetClientParams(serverRpcParams));
            }
        }
        else
        {
            // Сервер отправляет клиенту, информацию о том, что чанк не менялся
            ReceiveNoDataChunckBlocksClientRpc(chunckPos, GetTargetClientParams(serverRpcParams));
        }
    }

    /// <summary>
    /// Метод, который вызывается, чтобы сообщить клиенту
    /// что на сервере нет сохраненных изминений чанка 
    /// </summary>
    /// <param name="chunckPos"></param>
    /// <param name="clientRpcParams"></param>
    [ClientRpc(RequireOwnership = false)]
    private void ReceiveNoDataChunckBlocksClientRpc(Vector3 chunckPos, ClientRpcParams clientRpcParams = default)
    {
        var chunck = worldGenerator.GetChunk(chunckPos);
        chunck.blocksLoaded = true;
    }

    /// <summary>
    /// Вроде как здесь я чанку задаю блоки из json файла
    /// </summary>
    /// <param name="emptyChunck"></param>
    private void Chunck_Inited(ChunckComponent emptyChunck)
    {
        var chunckFileName = GetChunckName(emptyChunck);
        var path = $"{chuncksDirectory}{serverDirectory}{chunckFileName}.json";
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);

            var chunckData = JsonConvert.DeserializeObject<ChunckData>(json, settings);
            emptyChunck.blocks = chunckData.blocks;
            //emptyChunck.blocksLoaded = true;
        }

    }

    private IEnumerator Start()
    {
        worldGenerator = WorldGenerator.Inst;

        yield return null;
#if !UNITY_SERVER

#if UNITY_WEBGL && YG_PLUGIN_YANDEX_GAME
        while (!YG.YandexGame.SDKEnabled)
        {
            yield return new WaitForEndOfFrame();
        }
#endif

        //print(UserData.Owner);
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
                PrepareAndSendTurnedBlocksData(chunckData, chunckPos, GetTargetClientParams(serverRpcParams));
                
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

    private void PrepareAndSendTurnedBlocksData(ChunckData chunckData, Vector3 chunckPos, ClientRpcParams clientRpcParams)
    {
        if (chunckData.turnedBlocks == null || chunckData.turnedBlocks.Count is 0)
            return;

        var networkTurnedBlocksData = ToNetworkTurnedBlocksData(chunckData.turnedBlocks);
        ReceivePendingChunkTurnedBlocksDataClientRpc(networkTurnedBlocksData, chunckPos, clientRpcParams);
    }

    private NetworkTurnedBlockData[] ToNetworkTurnedBlocksData(List<ChunckData.JsonTurnedBlock> turnedBlocks)
    {
        var length = turnedBlocks.Count;
        NetworkTurnedBlockData[] result = new NetworkTurnedBlockData[length];
        for (int i = 0; i < length; i++)
        {
            var jsonData = turnedBlocks[i];
            result[i] = new NetworkTurnedBlockData()
            {
                worldBlockPos = jsonData.Pos,
                turnsData = new NetworkTurnData[jsonData.turnsBlockData.Length],
            };
            for (int j = 0; j < result[i].turnsData.Length; j++)
            {
                result[i].turnsData[j].angle = jsonData.turnsBlockData[j].angle;
                result[i].turnsData[j].axis = jsonData.turnsBlockData[j].axis;
            }

        }

        return result;
    }

    [ClientRpc(RequireOwnership = false)]
    private void ReceivePendingChunkTurnedBlocksDataClientRpc(NetworkTurnedBlockData[] turnedBlocks, Vector3 chunkPos, ClientRpcParams clientRpcParams)
    {
        if (turnedBlocks.Length is 0)
            return;

        //print($"хуль, я клиент и я получил данные о повертышах {turnedBlocks.Length}");
        var chunk = worldGenerator.GetChunk(chunkPos);
        foreach (var turnData in turnedBlocks)
        {
            var length = turnData.turnsData.Length;
            TurnBlockData[] turnsData = new TurnBlockData[length];
            for (int i = 0; i < length; i++)
            {
                turnsData[i].angle = turnData.turnsData[i].angle;
                turnsData[i].axis = turnData.turnsData[i].axis;
            }
            //print($"{turnData.worldBlockPos} ### {turnData.turnsData[0].angle}");
            chunk.AddTurnBlock
            (
                turnData.worldBlockPos.ToVecto3Int(),// бля.. в общем он тут
                turnsData                            // уже приходит в локальных координатах
            );
            
            //print($"{chunkPos} ### {turnData.worldBlockPos} ### {turnData.angle}");
        }

        //print("проверяю данные повернутых блоков у чанка");
        //foreach (var kv in chunk.turnedBlocks)
        //{
        //    print(kv.Key + " Позиция блока");
        //    foreach (var item in kv.Value)
        //    {
        //        print($"{item.angle} ^^^ {item.axis}");
        //    }
        //}
    }

    [ClientRpc(RequireOwnership = false)]
    private void ReceivePendingChunckBlocksDataClientRpc(Vector3[] positions, byte[] blockIDs, Vector3 chunckPos, ClientRpcParams clientRpcParams = default)
    {
        UpdateChunckMesh(positions, blockIDs, chunckPos, ActionOnComplete);

        void ActionOnComplete()
        {
            if (pendingChuncks.Count > 0)
            {
                pendingChuncks.Remove(currentPendingChunck);
            }

            waitHandlingChunck = false;

            //print("ЁБА ???");

            // КОСТЫЛИЩЕ
            //StartCoroutine(Async());
        }

        IEnumerator Async()
        {
            yield return new WaitForSeconds(0.5f);

            UpdateChunckMesh(positions, blockIDs, chunckPos);
        }
    }

    // Выполняется на клиенте
    private void UpdateChunckMesh(Vector3[] positions, byte[] blockIDs, Vector3 chunckPos, Action onComplete = null)
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
            //print("=========================");
            for (int i = 0; i < length; i++)
            {
                var pos = positions[i];
                var blockId = blockIDs[i];

                if (IsWorldPos(pos))
                {
                    pos = worldGenerator.ToLocalBlockPos(pos);
                }

                //int xIdx = (int)pos.x;
                //int yIdx = (int)pos.y;
                //int zIdx = (int)pos.z;
                //print($"{xIdx} # {yIdx} # {zIdx} # {chunck.blocks.Length}");
                chunck.SetBlock(pos, blockId);
            }

            //worldGenerator.UpdateChunckMesh(chunck);
            worldGenerator.UpdateChunkMeshAsync(chunck, LocalOnComplete);

            void LocalOnComplete()
            {
                chunck.renderer.gameObject.name = chunck.renderer.gameObject.name.Insert(0, $"{chunckPos} Srv Upd ");
                chunck.blocksLoaded = true;

                onComplete?.Invoke();
            }
            
        }
    }

    private bool IsWorldPos(Vector3 pos)
    {
        if (pos.x < 0 || pos.x > WorldGenerator.size - 1)
        {
            return true;
        }

        if (pos.y < 0 || pos.y > WorldGenerator.size - 1)
        {
            return true;
        }

        if (pos.z < 0 || pos.z > WorldGenerator.size - 1)
        {
            return true;
        }

        return false;
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
                worldBlockPos = data.pos,
                blockID = data.ID,
                turnsData = new NetworkTurnData[data.turnsBlockData.Length]
            };

            for (int i = 0; i < networkData.turnsData.Length; i++)
            {
                networkData.turnsData[i].angle = data.turnsBlockData[i].angle;
                networkData.turnsData[i].axis = data.turnsBlockData[i].axis;
            }

            SendTurnedBlockPlacedServerRpc(networkData);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendTurnedBlockPlacedServerRpc(NetworkTurnedBlockData blockData, ServerRpcParams serverRpcParams = default)
    {
        //print($"Еба че получил {blockData.angle} ### {blockData.axis}");
        SaveChangeChunck(blockData.worldBlockPos, blockData.blockID, serverRpcParams);
        SaveTurnedChangeChunck(blockData, serverRpcParams);
        ReceiveBlockPlacedClientRpc(blockData.worldBlockPos, blockData.blockID, serverRpcParams.Receive.SenderClientId);
        ReceiveTurnBlockPlacedClientRpc(blockData, serverRpcParams.Receive.SenderClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendBlockPlacedServerRpc(Vector3 blockPos, byte blockId, ServerRpcParams serverRpcParams = default)
    {
#if !UNITY_SERVER
        ChangeChunck(blockPos, blockId);
#endif
        SaveChangeChunck(blockPos, blockId, serverRpcParams);
        ReceiveBlockPlacedClientRpc(blockPos, blockId, serverRpcParams.Receive.SenderClientId);

        NetworkUserManager.Instance.AddPlacedBlock(serverRpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void ReceiveBlockPlacedClientRpc(Vector3 blockPos, byte blockID, ulong mineClientId)
    {
        if (mineClientId == NetworkManager.LocalClient.ClientId)
            return;
        // TO DO Доделать, чтобы не отправлять эти данные тому кто добыл блок

        worldGenerator.SetBlockAndUpdateChunck(blockPos, blockID);
    }

    [ClientRpc]
    private void ReceiveTurnBlockPlacedClientRpc(NetworkTurnedBlockData blockData, ulong mineClientId)
    {
        if (mineClientId == NetworkManager.LocalClient.ClientId)
            return;
        // TO DO Доделать, чтобы не отправлять эти данные тому кто добыл блок

        var chunk = worldGenerator.GetChunk(blockData.worldBlockPos);
        foreach (var turndata in blockData.turnsData)
        {
            chunk.AddTurnBlock
            (
                worldGenerator.ToLocalBlockPos(blockData.worldBlockPos),
                turndata.angle,
                turndata.axis
            );
        }

        //worldGenerator.UpdateChunckMesh(chunk);
        worldGenerator.UpdateChunkMeshAsync(chunk);
    }

    private void Block_Mined(BlockData data)
    {
        if (IsClient)
        {
            BlockMinedServerRpc(data.pos, data.ID);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void BlockMinedServerRpc(Vector3 blockPos, byte blockID, ServerRpcParams serverRpcParams = default)
    {
        //print($"Пришел блок {blockID} в {blockPos}");
#if !UNITY_SERVER
        ChangeChunck(blockPos, 0);
#endif
        RemoveTurnedBlockData(blockPos);
        SaveChangeChunck(blockPos, 0, serverRpcParams);
        ReceiveMinedBlockClientRpc(blockPos, serverRpcParams.Receive.SenderClientId);

        NetworkUserManager.Instance.AddMinedBlock(serverRpcParams.Receive.SenderClientId);
    }

    private void RemoveTurnedBlockData(Vector3 blockPos)
    {
        var chunck = worldGenerator.GetChunk(blockPos);
        print($"R.T.B.D: chunck {chunck} ###");
        var chunckFileName = GetChunckName(chunck);
        var path = $"{chuncksDirectory}{serverDirectory}{chunckFileName}.json";
        var localBlockPos = worldGenerator.ToLocalBlockPos(blockPos);

        ChunckData jsonChunkData = GetChunckData(path, chunck);
        int idx = 0;
        var hasTurnedData = false;
        print($"R.T.B.D: jsonChunkData {jsonChunkData} ###");
        foreach (var turnData in jsonChunkData.turnedBlocks)
        {
            if(turnData.Pos == localBlockPos)
            {
                hasTurnedData = true;
                break;
            }
            idx++;
        }

        if (hasTurnedData)
        {
            jsonChunkData.turnedBlocks.RemoveAt(idx);

            var json = JsonConvert.SerializeObject(jsonChunkData);
            File.WriteAllText(path, json);
            //print($"Жахнул поворото датас из чанкас {chunckFileName}");
        }
    }

    private void SaveChangeChunck(Vector3 worldBlockPos, byte blockID, ServerRpcParams serverRpcParams)
    {
        CheckDirectory(serverDirectory);

        var chunck = worldGenerator.GetChunk(worldBlockPos);
        var chunckFileName = GetChunckName(chunck);
        var path = $"{chuncksDirectory}{serverDirectory}{chunckFileName}.json";

        ChunckData jsonChunkData = GetChunckData(path, chunck);

        ServerChangedDataBlocks(jsonChunkData, worldBlockPos, blockID);

        var userName = NetworkUserManager.Instance.GetUserName(serverRpcParams.Receive.SenderClientId);
        var userChunckData = jsonChunkData.usersChangedBlocks.Find(u => u.userName == userName);
        if (userChunckData == null)
        {
            userChunckData = new ChunckData.UserChunckData
            {
                userName = userName
            };
            userChunckData.changedBlocks.Add(new(worldBlockPos, blockID));
            jsonChunkData.usersChangedBlocks.Add(userChunckData);
        }
        else
        {
            var jsonUserBlockData = userChunckData.changedBlocks.Find(d => d.Pos == worldBlockPos);
            if (jsonUserBlockData == null)
            {
                userChunckData.changedBlocks.Add(new(worldBlockPos, blockID));
            }
            else
            {
                jsonUserBlockData.blockId = blockID;
            }
        }

        var json = JsonConvert.SerializeObject(jsonChunkData); //JsonUtility.ToJson(data);

        File.WriteAllText(path, json);
    }

    
    private void SaveTurnedChangeChunck(NetworkTurnedBlockData blockData, ServerRpcParams serverRpcParams)
    {
        CheckDirectory(serverDirectory);

        var chunck = worldGenerator.GetChunk(blockData.worldBlockPos);
        var chunckFileName = GetChunckName(chunck);
        var path = $"{chuncksDirectory}{serverDirectory}{chunckFileName}.json";

        ChunckData jsonChunkData = GetChunckData(path, chunck);

        ServerChangedTurnedBlock(jsonChunkData, blockData);

        var json = JsonConvert.SerializeObject(jsonChunkData); //JsonUtility.ToJson(data);

        File.WriteAllText(path, json);
        //print($"Жасон савед - {chunckFileName}");
    }

    [ClientRpc]
    private void ReceiveMinedBlockClientRpc(Vector3 blockPos, ulong mineClientId)
    {
        if (mineClientId == NetworkManager.LocalClient.ClientId)
            return;
        // TO DO Доделать, чтобы не отправлять эти данные тому кто добыл блок
        worldGenerator.SetBlockAndUpdateChunck(blockPos, 0);
    }

    private ChunckData GetChunckData(string path, ChunckComponent chunck)
    {
        ChunckData data;

        if (File.Exists(path))
        {
            var fileText = File.ReadAllText(path);
            data = JsonConvert.DeserializeObject<ChunckData>(fileText, settings);
            data.blocks = chunck.blocks;// Я хз зачем я это делаю
        }
        else
        {
            data = new ChunckData(chunck);
        }
        return data;
    }

    private void ServerChangedDataBlocks(ChunckData jsonChunckData, Vector3 blockPos, byte blockID)
    {
        var blockLocalPos = worldGenerator.ToLocalBlockPos(blockPos);
        var changedBlockData = jsonChunckData.changedBlocks.Find(d => d.Pos == blockLocalPos);
        if (changedBlockData == null)
        {
            jsonChunckData.changedBlocks.Add(new ChunckData.JsonBlockData(blockLocalPos, blockID));
        }
        else
        {
            changedBlockData.blockId = blockID;
        }
    }

    private void ServerChangedTurnedBlock(ChunckData jsonChunckData, NetworkTurnedBlockData blockData)
    {
        if (jsonChunckData.turnedBlocks is null)
        {
            jsonChunckData.turnedBlocks = new List<ChunckData.JsonTurnedBlock>();
        }
        var localBlockPos = worldGenerator.ToLocalBlockPos(blockData.worldBlockPos);

        // Ищем в уже сохраненных данных информацию о повернутом блоке
        for (int i = 0; i < jsonChunckData.turnedBlocks.Count; i++)
        {
            var turnedBlock = jsonChunckData.turnedBlocks[i];
            if (turnedBlock.Pos == localBlockPos)
            {
                // не особо оптимизировано
                // мы пересоздаем массив вместе перезаписывания элементов
                // и при необходимости изминении размера массива
                var length = blockData.turnsData.Length;
                turnedBlock.turnsBlockData = new TurnBlockData[length];
                for (int j = 0; j < length; j++)
                {
                    turnedBlock.turnsBlockData[j].angle = blockData.turnsData[j].angle;
                    turnedBlock.turnsBlockData[j].axis = blockData.turnsData[j].axis;
                }

                return;
            }
        }

        // Если не находим, то создаем её
        TurnBlockData[] turns = new TurnBlockData[blockData.turnsData.Length];
        for (int j = 0; j < turns.Length; j++)
        {
            turns[j].angle = blockData.turnsData[j].angle;
            turns[j].axis = blockData.turnsData[j].axis;
        }
        var jsonTurnedBlock = new ChunckData.JsonTurnedBlock
        (
            localBlockPos,
            turns
        );
        jsonChunckData.turnedBlocks.Add(jsonTurnedBlock);
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

    public struct SendedChunkTrackingData
    {
        public Vector3 chunkPos;
        public ulong cliendId;
        public float lifeTime;
    }
}
