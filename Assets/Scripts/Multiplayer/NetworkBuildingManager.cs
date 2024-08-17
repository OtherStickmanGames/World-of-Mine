using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static ChunckData;
using Newtonsoft.Json;
using System;
#if UNITY_STANDALONE || UNITY_EDITOR
using System.IO;
#endif

[RequireComponent(typeof(BuildingManager))]
public class NetworkBuildingManager : NetworkBehaviour
{
    public static string buildingsDirectory = $"{Application.dataPath}/Data/Buildings/";

    BuildingManager buildingManager;

    private void Awake()
    {
        buildingManager = GetComponent<BuildingManager>();

        buildingManager.onInputNameShow.AddListener(InputBuildingName_Showed);
        buildingManager.onSaveBuilding.AddListener(SaveBuilding_Clicked);
        buildingManager.onGetBuildings.AddListener(GetBuildings_Requested);
    }


    private void SaveBuilding_Clicked(List<BlockData> blocksData, string nameBuilding)
    {
        Vector3[] positions = blocksData.Select(b => b.pos).ToArray();
        byte[] blockIDs = blocksData.Select(b => b.ID).ToArray();

        SaveBuildingServerRpc(positions, blockIDs, nameBuilding);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SaveBuildingServerRpc(Vector3[] positions, byte[] blockIDs, string nameBuilding, ServerRpcParams serverRpcParams = default )
    {
        UserChunckData buildData = new UserChunckData()
        {
            changedBlocks = new List<JsonBlockData>()
        };

        var length = positions.Length;
        for (int i = 0; i < length; i++)
        {
            var jsonBlockData = new JsonBlockData(positions[i], blockIDs[i]);
            buildData.changedBlocks.Add(jsonBlockData);
        }

        buildData.userName = NetworkUserManager.Instance.GetUserName(serverRpcParams.Receive.SenderClientId);
        SaveBuildingData data = new SaveBuildingData()
        {
            blocksData = buildData,
            createDate = DateTime.Now,
            nameBuilding = nameBuilding
        };

        var json = JsonConvert.SerializeObject(data);
        var fileName = $"{nameBuilding.Trim()}_{Guid.NewGuid()}.json";
        var path = $"{buildingsDirectory}{fileName}";
        
        File.WriteAllText(path, json);
        ReceiveSaveBuildingSuccesClientRpc(GetTargetClientParams(serverRpcParams));
        Debug.Log($"Building will be saved by {data.blocksData.userName}");
    }

    /// <summary>
    /// Просто сoобщаем клиенту, что сохранили постройку
    /// </summary>
    /// <param name="clientRpcParams"></param>
    [ClientRpc(RequireOwnership = false)]
    private void ReceiveSaveBuildingSuccesClientRpc(ClientRpcParams clientRpcParams = default)
    {
        BuildingManager.Singleton.Building_Saved();
    }  

    private void InputBuildingName_Showed()
    {
        GetCountSavedBuildingsServerRpc();
    }

    /// <summary>
    /// Клиент запрашивает у сервера количество сохраненных построек
    /// Сервер их получает и отправляет обратно клиенту
    /// </summary>
    /// <param name="rpcParams"></param>
    [ServerRpc(RequireOwnership = false)]
    private void GetCountSavedBuildingsServerRpc(ServerRpcParams serverRpcParams = default)
    {
        if (!Directory.Exists(buildingsDirectory))
        {
            Directory.CreateDirectory(buildingsDirectory);
        }

        var countBuildings = Directory.GetFiles(buildingsDirectory).Length;

        SendCountBuildingsClientRpc(countBuildings, GetTargetClientParams(serverRpcParams));
    }

    [ClientRpc(RequireOwnership = false)]
    private void SendCountBuildingsClientRpc(int count, ClientRpcParams clientRpcParams = default)
    {
        buildingManager.CountBuildings_Received(count);
    }

    private ClientRpcParams GetTargetClientParams(ServerRpcParams serverRpcParams)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId };

        return clientRpcParams;
    }


    /// <summary>
    /// Клиент запрашивает список построек по страницам
    /// </summary>
    /// <param name="page"></param>
    private void GetBuildings_Requested(int page)
    {
        GetBuildingsServerRpc(page);
    }

    [ServerRpc(RequireOwnership = false)]
    private void GetBuildingsServerRpc(int page, ServerRpcParams serverRpcParams = default)
    {
        if (!Directory.Exists(buildingsDirectory))
        {
            Directory.CreateDirectory(buildingsDirectory);
        }

        var files = Directory.GetFiles(buildingsDirectory).Where(f => f.Substring(f.Length - 4, 4) == "json").ToList();

        BuildingServerData buildingData = new BuildingServerData();
        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var data = JsonConvert.DeserializeObject<SaveBuildingData>(json);

            buildingData.positions = data.blocksData.changedBlocks.Select(b => b.Pos).ToArray();
            buildingData.blockIDs = data.blocksData.changedBlocks.Select(b => b.blockId).ToArray();
            buildingData.nameBuilding = data.nameBuilding;
            buildingData.authorName = data.blocksData.userName;

            ReceiveBuildingDataClientRpc(buildingData, GetTargetClientParams(serverRpcParams));
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void ReceiveBuildingDataClientRpc(BuildingServerData data, ClientRpcParams clientRpcParams = default)
    {
        BuildingManager.Singleton.CreateBuildingPreview(data);
    }
}


[JsonObject]
public struct SaveBuildingData
{
    public UserChunckData blocksData;
    public DateTime createDate;
    public string nameBuilding;
}

[Serializable]
public struct BuildingServerData : INetworkSerializable
{
    public Vector3[] positions;
    public byte[] blockIDs;
    public string nameBuilding;
    public string authorName;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref positions);
        serializer.SerializeValue(ref blockIDs);
        serializer.SerializeValue(ref nameBuilding);
        serializer.SerializeValue(ref authorName);
    }
}
