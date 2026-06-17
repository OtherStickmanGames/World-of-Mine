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

    public float savingProgress = 0f;

    private void SaveBuilding_Clicked(List<BlockData> blocksData, List<JsonTurnedBlock> turnedBlocks, string nameBuilding)
    {
        Vector3[] positions = blocksData.Select(b => b.pos).ToArray();
        byte[] blockIDs = blocksData.Select(b => b.ID).ToArray();

        //var networkTurnedBlockData = NetworkWorldGenerator.ToNetworkTurnedBlocksData(turnedBlocks);
        //SaveBuildingServerRpc(positions, blockIDs, networkTurnedBlockData, nameBuilding);
        
        var binaryBuildingData = BuildingBinarySerializer.Serialize(positions, blockIDs, nameBuilding, turnedBlocks);

        StartCoroutine(FragmentableSending());

        IEnumerator FragmentableSending()
        {
            int ChunkSize = 512;
            var allCount = binaryBuildingData.Length;
            int total = (allCount + ChunkSize - 1) / ChunkSize;
            print($"Количество частей постройки {total}");
            for (int i = 0; i < total; i++)
            {
                int offset = i * ChunkSize;
                int len = Math.Min(ChunkSize, allCount - offset);
                byte[] frag = new byte[len];
                Array.Copy(binaryBuildingData, offset, frag, 0, len);
                var sendId = idGenerator.GenerateId();
                timers[sendId] = 0f;
                // Отправляем фрагмент серверу
                SaveBuildingServerRpc(sendId, i, total, frag);

                yield return new WaitForSeconds(0.1f);

                BuildingManager.Singleton.savingProgress = (float)i / total;

                //while (true)
                //{
                //    if (!timers.ContainsKey(sendId))
                //        break;

                //    if (timers[sendId] > 3)
                //    {
                //        yield break;
                //    }
                //    else
                //    {
                //        yield return null;
                //    }
                //}
            }
        }
        
        
    }

    Dictionary<ulong, Dictionary<int, byte[]>> receivedFragmentsBinaryBuildings = new();

    [ServerRpc(RequireOwnership = false)]
    private void SaveBuildingServerRpc(ulong sendId, int fragmentIndex, int total, byte[] frag, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (!receivedFragmentsBinaryBuildings.ContainsKey(clientId)) receivedFragmentsBinaryBuildings[clientId] = new Dictionary<int, byte[]>();
        var clientFragments = receivedFragmentsBinaryBuildings[clientId];

        if(fragmentIndex == 0)
        {
            clientFragments.Clear();
        }

        clientFragments[fragmentIndex] = frag;
        print($"Получил {fragmentIndex + 1} часть постройки из {total}");
                if (clientFragments.Count == total)
        {
            int fullSize = 0;
            for (int i = 0; i < total; i++)
                if (clientFragments.ContainsKey(i))
                    fullSize += clientFragments[i].Length;

            var binaryBuildingData = new byte[fullSize];
            int pos = 0;
            for (int i = 0; i < total; i++)
            {
                if (clientFragments.TryGetValue(i, out var part))
                {
                    Buffer.BlockCopy(part, 0, binaryBuildingData, pos, part.Length);
                    pos += part.Length;
                }
            }
            receivedFragmentsBinaryBuildings.Remove(clientId);

            BuildingBinarySerializer.Deserialize
            (
                binaryBuildingData,
                out Vector3[] outPositions,
                out byte[] outBlockIDs,
                out string outName,
                out List<JsonTurnedBlock> outTurned
            );
            var networkTurnedBlockData = NetworkWorldGenerator.ToNetworkTurnedBlocksData(outTurned);
            // TODO: Куча лишних ненужных преобразований данных
            SaveBuilding(outPositions, outBlockIDs, networkTurnedBlockData, outName, serverRpcParams);

        }

    }

    private void SaveBuilding(Vector3[] positions, byte[] blockIDs, NetworkTurnedBlockData[] turnedBlocks, string nameBuilding, ServerRpcParams serverRpcParams = default)
    {
        var jsonTurnedBlocks = ConvertTools.ToJsonTurnedBlock(turnedBlocks);

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
            turnedBlocks = jsonTurnedBlocks,
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
    /// Просто сообщаем клиенту, что сохранили постройку
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

    long rpcId = 0;
    Dictionary<long, float> rpcTimeouts = new();
    Dictionary<ulong, float> timers = new();
    IdGenerator idGenerator = new();
    Coroutine sendBuildingsRoutine;
    Coroutine sendBuildingFragments;
    /// <summary>
    /// Сервер получает и отправляет список построек
    /// </summary>
    /// <param name="page"></param>
    /// <param name="serverRpcParams"></param>
    [ServerRpc(RequireOwnership = false)]
    private void GetBuildingsServerRpc(int page, ServerRpcParams serverRpcParams = default)
    {
        if (sendBuildingsRoutine != null)
            StopCoroutine(sendBuildingsRoutine);

        if (sendBuildingFragments != null)
            StopCoroutine(sendBuildingFragments);

        sendBuildingsRoutine = StartCoroutine(Async());

        IEnumerator Async()
        {
            var skip = page * pageSize;
            var buildingsPaged = buildingsServerData.Skip(skip).Take(pageSize).ToList();
            var username = NetworkUserManager.Instance.GetUserName(serverRpcParams.Receive.SenderClientId);

            bool needBreak = false;
            for (int i = 0; i < buildingsPaged.Count; i++)
            {
                rpcId++;
                var data = buildingsPaged[i];
                print(data.positions.Length + " count blocks");
                data.liked = data.playersLiked != null && data.playersLiked.Find(p => p == username) != null;
                var id = rpcId;

                rpcTimeouts[id] = 0;

                yield return sendBuildingFragments = StartCoroutine(StartSendFragmentableBuildingData(id, data, GetTargetClientParams(serverRpcParams)));
                

                //ReceiveBuildingDataClientRpc(rpcId, data, GetTargetClientParams(serverRpcParams));

                while (true)
                {
                    if (!rpcTimeouts.ContainsKey(id))
                    {
                        break;
                    }

                    if (rpcTimeouts[id] < 3)
                    {
                        yield return null;
                        //print($"ждем {id}");
                    }
                    else
                    {
                        rpcTimeouts.Remove(id);
                        needBreak = true;
                        print("таймаут вышел");
                        break;
                    }
                }

                if (needBreak)
                {
                    break;
                }

                yield return new WaitForSeconds(0.1f);
                //FindObjectOfType<UI>().txtPizdos.text += $" #{buildingData.nameBuilding}";

            }

            print($"Клиент получил все постройки на странице {page + 1} из {(buildingsServerData.Count + pageSize - 1) / pageSize} всего построек {buildingsServerData.Count}");

            if (skip + pageSize >= buildingsServerData.Count)
            {
                ReceiveEndOfPagesClientRpc(GetTargetClientParams(serverRpcParams));
            }

            sendBuildingsRoutine = null;
        }
    }

    Dictionary<long, BuildingServerData> receivedBuildings = new();
    BuildingFragments receivedFragments;

    private IEnumerator StartSendFragmentableBuildingData(long rpcId, BuildingServerData data, ClientRpcParams clientRpcParams)
    {
        NetworkHeaderBuildingData mainFragment = new()
        {
            nameBuilding = data.nameBuilding,
            authorName = data.authorName,
            guid = data.guid,
            countLikes = data.countLikes,
            liked = data.liked
        };

        var sendId = idGenerator.GenerateId();
        timers[sendId] = 0f;

        ReceiveBuildingMainFragmentClientRpc(sendId, mainFragment, clientRpcParams);

        while (true)
        {
            if (!timers.ContainsKey(sendId))
                break;

            if (timers[sendId] > 3)
            {
                yield break;
            }
            else
            {
                yield return null;
            }
        }

        print("Збс, клиент получил первую часть");

        int ChunkSize = 188;//128;
        var allCount = data.blockIDs.Length;
        int total = (allCount + ChunkSize - 1) / ChunkSize;
        print($"Количество частей блоков {total}");
        for (int i = 0; i < total; i++)
        {
            int offset = i * ChunkSize;
            int len = Math.Min(ChunkSize, allCount - offset);
            byte[] frag = new byte[len];
            Array.Copy(data.blockIDs, offset, frag, 0, len);
            sendId = idGenerator.GenerateId();
            timers[sendId] = 0f;
            // Отправляем фрагмент клиенту
            BuildingFragmentBlocksClientRpc(sendId, i, total, frag, clientRpcParams);

            while (true)
            {
                if (!timers.ContainsKey(sendId))
                    break;

                if (timers[sendId] > 3)
                {
                    yield break;
                }
                else
                {
                    yield return null;
                }
            }
        }

        ChunkSize /= 3;
        allCount = data.positions.Length;
        total = (allCount + ChunkSize - 1) / ChunkSize;
        print($"Количество частей позиций {total}");
        for (int i = 0; i < total; i++)
        {
            int offset = i * ChunkSize;
            int len = Math.Min(ChunkSize, allCount - offset);
            Vector3[] frag = new Vector3[len];
            Array.Copy(data.positions, offset, frag, 0, len);
            sendId = idGenerator.GenerateId();
            timers[sendId] = 0f;
            // Отправляем фрагмент клиенту
            BuildingFragmentPositionsClientRpc(sendId, i, total, frag, clientRpcParams);

            while (true)
            {
                if (!timers.ContainsKey(sendId))
                    break;

                if (timers[sendId] > 3)
                {
                    yield break;
                }
                else
                {
                    yield return null;
                }
            }
        }

        print("Збс, клиент получил позиции блоков");

        rpcTimeouts.Remove(rpcId);

        sendBuildingFragments = null;
    }

    [ClientRpc(RequireOwnership = false)]
    private void BuildingFragmentPositionsClientRpc(ulong messageId, int fragmentIndex, int totalFragments, Vector3[] fragmentData, ClientRpcParams clientRpcParams = default)
    {
        receivedFragments.partsPositions[fragmentIndex] = fragmentData;
        receivedFragments.lastReceivedTime = DateTime.UtcNow;
        print($"Получил позиции {fragmentIndex + 1} из {totalFragments}");

        if (totalFragments == receivedFragments.CountPartsPositions)
        {
            print("Ебать! Я получил все данные");

            // Собираем все в порядке индексов
            int fullSize = 0;
            for (int i = 0; i < receivedFragments.CountPartsBlocks; i++) 
                fullSize += receivedFragments.partsBlocks[i].Length;

            var allBlocks = new byte[fullSize];
            int pos = 0;
            for (int i = 0; i < receivedFragments.CountPartsBlocks; i++)
            {
                var partBlocks = receivedFragments.partsBlocks[i];
                Buffer.BlockCopy(partBlocks, 0, allBlocks, pos, partBlocks.Length);
                pos += partBlocks.Length;
            }

            fullSize = 0;
            for (int i = 0; i < receivedFragments.CountPartsPositions; i++)
                fullSize += receivedFragments.partsPositions[i].Length;

            var allPositions = new Vector3[fullSize];
            var allPosesList = new List<Vector3>();
            pos = 0;
            for (int i = 0; i < receivedFragments.CountPartsPositions; i++)
            {
                var partPositions = receivedFragments.partsPositions[i];
                //Buffer.BlockCopy(partPositions, 0, allPositions, pos, partPositions.Length);
                //pos += partPositions.Length;
                allPosesList.AddRange(partPositions);
            }

            currentReceiving.blockIDs = allBlocks;
            //currentReceiving.positions = allPositions;
            currentReceiving.positions = allPosesList.ToArray();

            BuildingManager.Singleton.CreateBuildingPreview(currentReceiving);
        }

        
        AckReceivedServerRpc(messageId);
    }

    [ClientRpc(RequireOwnership = false)]
    public void BuildingFragmentBlocksClientRpc(ulong messageId, int fragmentIndex, int totalFragments, byte[] fragmentData, ClientRpcParams clientRpcParams = default)
    {
        // Количество полученных частей всегда должно быть равно
        // индексу следующей части
        if (receivedFragments.partsBlocks.Count == fragmentIndex)
        {
            receivedFragments.partsBlocks[fragmentIndex] = fragmentData;
        receivedFragments.lastReceivedTime = DateTime.UtcNow;
        }
        else
        {
            print("ebat'");
        }

        AckReceivedServerRpc(messageId);
        print($"Получил блоки {fragmentIndex + 1} из {totalFragments}");
    }


    BuildingServerData currentReceiving;

    [ClientRpc(RequireOwnership = false)]
    private void ReceiveBuildingMainFragmentClientRpc(ulong sendId, NetworkHeaderBuildingData mainFragment, ClientRpcParams clientRpcParams = default)
    {
        receivedFragments = new();
        currentReceiving = new()
        {
            nameBuilding = mainFragment.nameBuilding,
            authorName = mainFragment.authorName,
            guid = mainFragment.guid,
            countLikes = mainFragment.countLikes,
            liked = mainFragment.liked
        };

        AckReceivedServerRpc(sendId);
        print($"Получил хидер постройки {mainFragment.nameBuilding}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void AckReceivedServerRpc(ulong sendId, ServerRpcParams serverRpcParams = default)
    {
        timers.Remove(sendId);
    }

        private List<long> _rpcKeysToRemove = new List<long>();
    private List<ulong> _timerKeysToRemove = new List<ulong>();

        private void Update()
    {
        float dt = Time.deltaTime;

        if (IsServer || IsHost)
        {
            if (rpcTimeouts.Count > 0)
            {
                _rpcKeysToRemove.Clear();
                foreach (var kvp in rpcTimeouts)
                {
                    long id = kvp.Key;
                    rpcTimeouts[id] = kvp.Value + dt;
                    if (rpcTimeouts[id] > 300)
                    {
                        _rpcKeysToRemove.Add(id);
                    }
                }

                foreach (var id in _rpcKeysToRemove)
                {
                    if (rpcTimeouts.Remove(id))
                    {
                        print("Таймаут RPC удален");
                    }
                }
            }

            if (timers.Count > 0)
            {
                _timerKeysToRemove.Clear();
                foreach (var kvp in timers)
                {
                    ulong id = kvp.Key;
                    timers[id] = kvp.Value + dt;
                    if (timers[id] > 10)
                    {
                        _timerKeysToRemove.Add(id);
                    }
                }

                foreach (var id in _timerKeysToRemove)
                {
                    if (timers.Remove(id))
                    {
                        print("Таймаут таймера удален");
                    }
                }
            }
        }

        if (IsClient && !IsServer)
        {
            if (receivedFragments != null && (DateTime.UtcNow - receivedFragments.lastReceivedTime).TotalSeconds > 30)
            {
                receivedFragments = null;
                print("Клиент: Таймаут передачи постройки, фрагменты очищены.");
            }
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void ReceiveBuildingDataClientRpc(long rpcId, BuildingServerData data, ClientRpcParams clientRpcParams = default)
    {
        //Debug.Log($"{data.nameBuilding}  {data.guid}");
        BuildingManager.Singleton.CreateBuildingPreview(data);

        AckClientReceivedBuildingServerRpc(rpcId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AckClientReceivedBuildingServerRpc(long rpcId, ServerRpcParams serverRpcParams = default)
    {
        rpcTimeouts.Remove(rpcId);
        Debug.Log($"клиент уебатор {rpcId}");
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
            yield return new WaitForSeconds(380);

            var random = new System.Random();
            buildingsServerData = buildingsServerData.OrderBy(d => random.Next()).ToList();

            StartCoroutine(Await());
        }
    }


    

    /// <summary>
    /// Метод который вызывается на клиенте, означающий конец списка построек
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
        // TODO: По ID юзера
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
    public List<JsonTurnedBlock> turnedBlocks;
}

class Pending
{
    public int Total;
    public Dictionary<int, byte[]> Parts = new Dictionary<int, byte[]>();
    public DateTime FirstReceived = DateTime.UtcNow;
    public int ReceivedCount => Parts.Count;
}

public class BuildingFragments
{
    public DateTime lastReceivedTime = DateTime.UtcNow;
    public Dictionary<int, byte[]> partsBlocks = new();
    public Dictionary<int, Vector3[]> partsPositions = new();

    public int CountPartsBlocks => partsBlocks.Count;
    public int CountPartsPositions => partsPositions.Count;
}


