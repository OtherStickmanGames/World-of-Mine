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

    string chuncksDirectory = $"{Application.dataPath}/Data/Chuncks/";
    string serverDirectory = "Server/";
    string clientDirectory = "Client/";

    private void Awake()
    {
        WorldGenerator.onBlockPick.AddListener(Block_Mined);
    }

    private void Start()
    {
        worldGenerator = WorldGenerator.Inst;

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
            SaveChangeChunckServerRpc(data.pos);
            //SaveChangeChunckClientRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SaveChangeChunckServerRpc(Vector3 blockPos)
    {
        CheckDirectory(serverDirectory);

        var chunck = worldGenerator.GetChunk(blockPos);
        var chunckFileName = $"{chunck.pos.x}_{chunck.pos.y}_{chunck.pos.z}";
   
        ChunckData data = new(chunck);

        var json = JsonConvert.SerializeObject(data); //JsonUtility.ToJson(data);
        var path = $"{chuncksDirectory}{serverDirectory}{chunckFileName}.json";
        File.WriteAllText(path, json);
    }

    [ClientRpc]
    private void SaveChangeChunckClientRpc()
    {

    }

    void CheckDirectory(string subDirectory)
    {
        var path = $"{chuncksDirectory}{subDirectory}";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
