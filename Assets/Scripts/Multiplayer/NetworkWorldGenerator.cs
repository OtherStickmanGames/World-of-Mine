using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;
using System.IO;

public class NetworkWorldGenerator : NetworkBehaviour
{
    WorldGenerator worldGenerator;

    public static string chuncksDirectory = $"{Application.dataPath}/Data/Chuncks/";
    public static string serverDirectory = "Server/";
    public static string clientDirectory = "Client/";

    List<ChunckComponent> offlineBlocksSeted = new List<ChunckComponent>();

    private void Awake()
    {
        WorldGenerator.onBlockPick.AddListener(Block_Mined);
        ChunckComponent.onChunckInit.AddListener(Chunck_Inited);
        ChunckComponent.onBlocksSeted.AddListener(ChunckBlocks_Seted);
    }

    private void ChunckBlocks_Seted(ChunckComponent chunckWithBlocks)
    {
        if (NetworkManager.IsConnectedClient){

            if (IsClient)
            {
                SendChangedBlocksServerRpc();
            }
        }
        else
        {
            offlineBlocksSeted.Add(chunckWithBlocks);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendChangedBlocksServerRpc(ServerRpcParams serverRpcParams = default)
    {
        //print(serverRpcParams.Receive.SenderClientId);
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
        worldGenerator.GetChunk(UserData.Owner.position.ToGlobalRoundBlockPos());
        worldGenerator.GetChunk(UserData.Owner.position.ToGlobalRoundBlockPos() + (Vector3.down * WorldGenerator.size));

    }

    private void Block_Mined(BlockData data)
    {
        if (IsServer)
        {

        }
        else
        {
            print("отправляем запрос");
            BlockMinedServerRpc(data.pos, data.ID);
            //SaveChangeChunckClientRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void BlockMinedServerRpc(Vector3 blockPos, byte blockID)
    {
        //print($"Пришел блок {blockID} в {blockPos}");
        ChangeChunck(blockPos);
        SaveChangeChunck(blockPos, blockID);
    }

    private void SaveChangeChunck(Vector3 blockPos, byte blockID)
    {
        CheckDirectory(serverDirectory);

        var chunck = worldGenerator.GetChunk(blockPos);
        var chunckFileName = GetChunckName(chunck);

        ChunckData data = new(chunck);

        var json = JsonConvert.SerializeObject(data); //JsonUtility.ToJson(data);
        var path = $"{chuncksDirectory}{serverDirectory}{chunckFileName}.json";
        File.WriteAllText(path, json);
    }

    [ClientRpc]
    private void SaveChangeChunckClientRpc()
    {

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
