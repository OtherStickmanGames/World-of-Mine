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
    ///  лиент запрашивает у сервера количество сохраненных построек
    /// —ервер их получает и отправл€ет обратно клиенту
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
}


[JsonObject]
public struct SaveBuildingData
{
    public UserChunckData blocksData;
    public DateTime createDate;
    public string nameBuilding;
}
