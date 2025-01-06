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

    int pageSize = 8;

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
        ShuffleBuildingList();
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
#if !UNITY_WEBGL
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
#endif
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
#if !UNITY_WEBGL
        if (!Directory.Exists(buildingsDirectory))
        {
            Directory.CreateDirectory(buildingsDirectory);
        }

        var countBuildings = Directory.GetFiles(buildingsDirectory).Length;

        SendCountBuildingsClientRpc(countBuildings, GetTargetClientParams(serverRpcParams));
#endif
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


    /// <summary>
    /// Сервер получает и отправляет список построек
    /// </summary>
    /// <param name="page"></param>
    /// <param name="serverRpcParams"></param>
    [ServerRpc(RequireOwnership = false)]
    private void GetBuildingsServerRpc(int page, ServerRpcParams serverRpcParams = default)
    {
        StartCoroutine(Async());

        IEnumerator Async()
        {
            var skip = page * pageSize;
            var buildingsPaged = buildingsServerData.Skip(skip).Take(pageSize).ToList();

            //foreach (var data in buildingsPaged)
            for (int i = 0; i < buildingsPaged.Count; i++)
            {
                var data = buildingsPaged[i];
                var username = NetworkUserManager.Instance.GetUserName(serverRpcParams.Receive.SenderClientId);
                data.liked = data.playersLiked != null && data.playersLiked.Find(p => p == username) != null;
                ReceiveBuildingDataClientRpc(data, GetTargetClientParams(serverRpcParams));
                //FindObjectOfType<UI>().txtPizdos.text += $" #{buildingData.nameBuilding}";
                yield return null;
            }

            if (skip + pageSize >= buildingsServerData.Count)
            {
                ReceiveEndOfPagesClientRpc(GetTargetClientParams(serverRpcParams));
            }
        }
    }

    private void UpdateBuildingsList()
    {
#if !UNITY_WEBGL
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
                    guid = data.guid,
                    // NOT SENDABLE
                    playersLiked = data.playersLiked,
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
#endif
    }

    private void ShuffleBuildingList()
    {
        StartCoroutine(Await());

        IEnumerator Await()
        {
            yield return new WaitForSeconds(38);

            var random = new System.Random();
            buildingsServerData = buildingsServerData.OrderBy(d => random.Next()).ToList();
            
            StartCoroutine(Await());
        } 
    }


    [ClientRpc(RequireOwnership = false)]
    private void ReceiveBuildingDataClientRpc(BuildingServerData data, ClientRpcParams clientRpcParams = default)
    {
        //Debug.Log($"{data.nameBuilding}  {data.guid}");
        BuildingManager.Singleton.CreateBuildingPreview(data);
    }

    /// <summary>
    /// Метод который вызыватся на клиенте, означающий конец списка построек
    /// </summary>
    /// <param name="clientRpcParams"></param>
    [ClientRpc(RequireOwnership = false)]
    private void ReceiveEndOfPagesClientRpc(ClientRpcParams clientRpcParams = default)
    {
        print("Конец списка");
        BuildingManager.Singleton.InvokeEndBuildingList();
    }

    private void Building_Liked(string guid)
    {
        BuildingLikedServerRpc(guid);
    }

    [ServerRpc(RequireOwnership = false)]
    private void BuildingLikedServerRpc(string guid, ServerRpcParams serverRpcParams = default)
    {
#if !UNITY_WEBGL
        //Debug.Log(buildingsServerData.Count);
        var building = buildingsServerData.Find(b => b.guid == guid);

        var fileName = $"{building.nameBuilding}_{guid}.json";
        var path = $"{buildingsDirectory}{fileName}";
        var json = File.ReadAllText(path);
        var savedData = JsonConvert.DeserializeObject<SaveBuildingData>(json);
        var playername = NetworkUserManager.Instance.GetUserName(serverRpcParams.Receive.SenderClientId);
        // TO DO По ID юзера
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

        UpdateBuildingsList();
#endif
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


