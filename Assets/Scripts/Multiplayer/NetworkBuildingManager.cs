using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static ChunckData;
using Newtonsoft.Json;
using System;
#if !UNITY_WEBGL
using System.IO;
#endif

[RequireComponent(typeof(BuildingManager))]
public class NetworkBuildingManager : NetworkBehaviour
{
    public static string buildingsDirectory = $"{Application.dataPath}/Data/Buildings/";

    List<BuildingServerData> buildingsServerData = new List<BuildingServerData>();
    BuildingManager buildingManager;

    private void Awake()
    {
        buildingManager = GetComponent<BuildingManager>();

        buildingManager.onInputNameShow.AddListener(InputBuildingName_Showed);
        buildingManager.onSaveBuilding.AddListener(SaveBuilding_Clicked);
        buildingManager.onGetBuildings.AddListener(GetBuildings_Requested);
        buildingManager.onBuildingLike.AddListener(Building_Liked);

    }

    private void Start()
    {
        NetworkManager.OnServerStarted += Server_Started;
    }

    private void Server_Started()
    {
        UpdateBuildingsList();
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

        var guid = Guid.NewGuid().ToString();

        buildData.userName = NetworkUserManager.Instance.GetUserName(serverRpcParams.Receive.SenderClientId);
        SaveBuildingData data = new SaveBuildingData()
        {
            blocksData = buildData,
            createDate = DateTime.Now,
            nameBuilding = nameBuilding,
            guid = guid,
        };

        var json = JsonConvert.SerializeObject(data);
        var fileName = $"{nameBuilding.Trim()}_{guid}.json";
        var path = $"{buildingsDirectory}{fileName}";
        
        File.WriteAllText(path, json);
        ReceiveSaveBuildingSuccesClientRpc(GetTargetClientParams(serverRpcParams));
        Debug.Log($"Building will be saved by {data.blocksData.userName}");
        UpdateBuildingsList();
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
        //print("Отправил запрос на постройки");
    }

    [ServerRpc(RequireOwnership = false)]
    private void GetBuildingsServerRpc(int page, ServerRpcParams serverRpcParams = default)
    {
        if (!Directory.Exists(buildingsDirectory))
        {
            Directory.CreateDirectory(buildingsDirectory);
        }

        var files = Directory.GetFiles(buildingsDirectory).Where(f => f.Substring(f.Length - 4, 4) == "json").ToList();
        var playername = NetworkUserManager.Instance.GetUserName(serverRpcParams.Receive.SenderClientId);
        

        StartCoroutine(Async());

        IEnumerator Async()
        {
            List<SaveBuildingData> buildingsData = new List<SaveBuildingData>();
            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var data = JsonConvert.DeserializeObject<SaveBuildingData>(json);
                buildingsData.Add(data);
            }

            buildingsData = buildingsData.OrderBy(d => d.createDate).ToList();
            //FindObjectOfType<UI>().txtPizdos.text += $" #{buildingsData.Count}";

            foreach (var data in buildingsData)
            {
                var countLikes = data.playersLiked == null ? 0 : data.playersLiked.Count;
                BuildingServerData buildingData = new BuildingServerData
                {
                    positions = data.blocksData.changedBlocks.Select(b => b.Pos).ToArray(),
                    blockIDs = data.blocksData.changedBlocks.Select(b => b.blockId).ToArray(),
                    nameBuilding = data.nameBuilding,
                    authorName = data.blocksData.userName,
                    countLikes = countLikes,
                    liked = countLikes > 0 ? data.playersLiked.Contains(playername) : false,
                    guid = data.guid
                };

                //FindObjectOfType<UI>().txtPizdos.text += $" #{buildingData.nameBuilding}";
                //Debug.Log(buildingData.countLikes);
                ReceiveBuildingDataClientRpc(buildingData, GetTargetClientParams(serverRpcParams));
                //FindObjectOfType<UI>().txtPizdos.text += $" #{buildingData.nameBuilding}";
                yield return null;
            }
        }
        
    }

    private void UpdateBuildingsList()
    {
        if (!Directory.Exists(buildingsDirectory))
        {
            Directory.CreateDirectory(buildingsDirectory);
        }

        var files = Directory.GetFiles(buildingsDirectory).Where(f => f.Substring(f.Length - 4, 4) == "json").ToList();

        StartCoroutine(Async());

        IEnumerator Async()
        {
            List<SaveBuildingData> buildingsData = new List<SaveBuildingData>();
            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var data = JsonConvert.DeserializeObject<SaveBuildingData>(json);
                buildingsData.Add(data);
            }

            buildingsData = buildingsData.OrderBy(d => d.createDate).ToList();

            List<BuildingServerData> buildings = new List<BuildingServerData>();
            int idx = 0;
            foreach (var data in buildingsData)
            {
                BuildingServerData buildingData = new BuildingServerData
                {
                    positions = data.blocksData.changedBlocks.Select(b => b.Pos).ToArray(),
                    blockIDs = data.blocksData.changedBlocks.Select(b => b.blockId).ToArray(),
                    nameBuilding = data.nameBuilding,
                    authorName = data.blocksData.userName,
                    countLikes = data.playersLiked == null ? 0 : data.playersLiked.Count,
                    guid = data.guid
                };

                buildings.Add(buildingData);
                idx++;

                if (idx % 38 == 0)
                {
                    yield return null;
                }
            }

            buildingsServerData = buildings;
            //FindObjectOfType<UI>().txtPizdos.SetText($"{buildingsServerData.Count}");
            //Debug.Log($"{buildingsServerData.Count} -=-=-");
        }
    }


    [ClientRpc(RequireOwnership = false)]
    private void ReceiveBuildingDataClientRpc(BuildingServerData data, ClientRpcParams clientRpcParams = default)
    {
        //Debug.Log(data.guid);
        BuildingManager.Singleton.CreateBuildingPreview(data);
    }

    private void Building_Liked(string guid)
    {
        BuildingLikedServerRpc(guid);
    }

    [ServerRpc(RequireOwnership = false)]
    private void BuildingLikedServerRpc(string guid, ServerRpcParams serverRpcParams = default)
    {
        //Debug.Log(buildingsServerData.Count);
        var building = buildingsServerData.Find(b => b.guid == guid);

        var fileName = $"{building.nameBuilding}_{guid}.json";
        var path = $"{buildingsDirectory}{fileName}";
        var json = File.ReadAllText(path);
        var savedData = JsonConvert.DeserializeObject<SaveBuildingData>(json);
        var playername = NetworkUserManager.Instance.GetUserName(serverRpcParams.Receive.SenderClientId);
        // TO DO
        if (savedData.playersLiked == null)
        {
            savedData.playersLiked = new List<string>();
        }

        if (savedData.playersLiked.Contains(playername))
        {
            savedData.playersLiked.Remove(playername);
            Debug.Log($"{playername} DISLIKE !!! - {building.nameBuilding}");
        }
        else
        {
            savedData.playersLiked.Add(playername);
            Debug.Log($"{playername} Liked building - {building.nameBuilding}");
        }
        json = JsonConvert.SerializeObject(savedData);
        File.WriteAllText(path, json);
    }

}

/// <summary>
/// Хранится в Жасоне
/// </summary>
[JsonObject]
public struct SaveBuildingData
{
    public UserChunckData blocksData;
    public DateTime createDate;
    public string nameBuilding;
    public string guid;
    public List<string> playersLiked;
}

/// <summary>
/// Данные передающиеся по сети
/// </summary>
[Serializable]
public struct BuildingServerData : INetworkSerializable
{
    public Vector3[] positions;
    public byte[] blockIDs;
    public string nameBuilding;
    public string authorName;
    public string guid;
    public int countLikes;
    public bool liked;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref positions);
        serializer.SerializeValue(ref blockIDs);
        serializer.SerializeValue(ref nameBuilding);
        serializer.SerializeValue(ref authorName);
        serializer.SerializeValue(ref guid);
        serializer.SerializeValue(ref countLikes);
        serializer.SerializeValue(ref liked);
    }
}
