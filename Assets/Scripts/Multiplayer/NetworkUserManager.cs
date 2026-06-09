using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;
#if !UNITY_WEBGL
using System.IO;
#endif
#if UNITY_WEBGL && YG_PLUGIN_YANDEX_GAME
using YG;
#endif


public class NetworkUserManager : NetworkBehaviour
{
    public Dictionary<ulong, string> users = new Dictionary<ulong, string>();
    public Dictionary<ulong, string> playerIds = new Dictionary<ulong, string>();
    private Dictionary<string, GlobalUserData> userDataCache = new Dictionary<string, GlobalUserData>();

    [field: SerializeField]
    public bool UserRegistred { get; private set; } = false;

    public static NetworkUserManager Instance;
    public static UnityEvent onUserRegistred = new UnityEvent();

    static string usersDataDirectory = $"{Application.dataPath}/Data/Users/";

    string ygPlayerID = "COMPUCTER";

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        NetworkManager.OnClientConnectedCallback += Client_Connected;
        NetworkManager.OnClientDisconnectCallback += Client_Disconnected;
    }

    private void Client_Disconnected(ulong clientId)
    {
        if (NetworkManager.IsServer)
        {
            if (users.ContainsKey(clientId))
            {
                Debug.Log($"{users[clientId]}: Disconnected # Server Time:{DateTime.Now}");
                users.Remove(clientId);

                if (playerIds.ContainsKey(clientId))
                {
                    _ = EndUserSessionAsync(playerIds[clientId]);
                    playerIds.Remove(clientId);
                }
            }
            else
            {
                Debug.Log($"Этот пользователь не в списке и подключился как гость: client id {clientId}");
            }
        }
    }

    private void Client_Connected(ulong clientId)
    {
        if (NetworkManager.LocalClientId == clientId)
        {
            var userData = UserData.Owner;
            SendUserNameServerRpc(userData.userName);

#if UNITY_WEBGL && YG_PLUGIN_YANDEX_GAME
            SendYGUserConnectedServerRpc(userData.userName, ygPlayerID);
#endif

#if UNITY_STANDALONE && !UNITY_SERVER
            SendYGUserConnectedServerRpc(userData.userName, "878sdf78sd78f5");
#endif

#if UNITY_ANDROID
            SendYGUserConnectedServerRpc(userData.userName, SystemInfo.deviceUniqueIdentifier);
#endif
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void SendUserNameServerRpc(string userName, ServerRpcParams serverRpcParams = default)
    {
        users.Add(serverRpcParams.Receive.SenderClientId, userName);

        ClientRpcParams rpcParams = default;
        rpcParams.Send.TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId };

        UserRegistredClientRpc(rpcParams);
        Debug.Log($"{userName} Client ID {serverRpcParams.Receive.SenderClientId} # Server Time:{DateTime.Now}");
    }


    [ClientRpc(RequireOwnership = false)]
    private void UserRegistredClientRpc(ClientRpcParams clientRpcParams = default)
    {
        UserRegistred = true;
        onUserRegistred?.Invoke();
    }

    public string GetUserName(ulong userId)
    {
        return users[userId];
    }

    Action<string> nicknameRequest;
    public void GetNicknameRequest(ulong ownerID, Action<string> requestResult)
    {
        nicknameRequest = requestResult;
        SendNicknameServerRpc(ownerID);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendNicknameServerRpc(ulong ownerID, ServerRpcParams serverRpcParams = default)
    {
        var crp = NetTool.GetTargetClientParams(serverRpcParams);

        if (users.TryGetValue(ownerID, out var user))
        {
            ReceiveNicknameClientRpc(user, crp);
        }
        else
        {
            Debug.Log($"  {ownerID}");
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void ReceiveNicknameClientRpc(string nickname, ClientRpcParams clientRpcParams = default)
    {
        nicknameRequest?.Invoke(nickname);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendYGUserConnectedServerRpc(string nickname, string playerId, ServerRpcParams serverRpcParams = default)
    {
        _ = SendYGUserConnectedAsync(nickname, playerId, serverRpcParams.Receive.SenderClientId);
    }

    private async Task SendYGUserConnectedAsync(string nickname, string playerId, ulong senderClientId)
    {
#if !UNITY_WEBGL
        if (!Directory.Exists(usersDataDirectory))
        {
            Directory.CreateDirectory(usersDataDirectory);
        }

        playerId = playerId.Replace("/", "_");
        
        if(!playerIds.TryAdd(senderClientId, playerId))
        {
            print($"-   player ID {playerId}");
        }

        var fileName = $"{playerId}.json";
        var path = $"{usersDataDirectory}{fileName}";
        GlobalUserData userData;

        if (File.Exists(path))
        {
            var json = await Task.Run(() => File.ReadAllText(path));
            userData = JsonConvert.DeserializeObject<GlobalUserData>(json);
            var session = new SessionData()
            {
                nickname = nickname,
                start = DateTime.Now,
                isActive = true,
                clientID = senderClientId,
            };
            userData.sessions.Add(session);
        }
        else
        {
            userData = new GlobalUserData() { playerID = playerId };
            var session = new SessionData()
            {
                nickname = nickname,
                start = DateTime.Now,
                isActive = true,
                clientID = senderClientId,
            };
            userData.sessions = new();
            userData.sessions.Add(session);

            // Need to set playerID for new user
            userData.playerID = playerId;
        }

        userDataCache[playerId] = userData;
        await SaveUserDataAsync(userData);
#endif
    }

    // TODO ,       
    public void AddMinedBlock(ulong clienID)
    {
        _ = AddMinedBlockAsync(clienID);
    }

    private async Task AddMinedBlockAsync(ulong clienID)
    {
#if !UNITY_WEBGL
        if (!playerIds.ContainsKey(clienID))
            return;

        var playerId = playerIds[clienID];
        GlobalUserData userData = await GetUserDataAsync(playerId);

        if (userData != null)
        {
            var idxLastSession = userData.sessions.Count - 1;
            var session = userData.sessions[idxLastSession];

            session.countMineBlock++;
            userData.sessions[idxLastSession] = session;

            await SaveUserDataAsync(userData);
        }
#endif
    }

    // TODO ,       
    public void AddPlacedBlock(ulong clienID)
    {
        _ = AddPlacedBlockAsync(clienID);
    }

    private async Task AddPlacedBlockAsync(ulong clienID)
    {
#if UNITY_WEBGL
        await Task.CompletedTask;
#endif
#if !UNITY_WEBGL
        if (!playerIds.ContainsKey(clienID))
            return;

        var playerId = playerIds[clienID];
        GlobalUserData userData = await GetUserDataAsync(playerId);

        if (userData != null)
        {
            var idxLastSession = userData.sessions.Count - 1;
            var session = userData.sessions[idxLastSession];

            session.countPlacedBlock++;
            userData.sessions[idxLastSession] = session;

            await SaveUserDataAsync(userData);
        }
#endif
    }

    private async Task EndUserSessionAsync(string playerId)
    {
#if !UNITY_WEBGL
        playerId = playerId.Replace("/", "_");
        GlobalUserData userData = await GetUserDataAsync(playerId);

        if (userData != null)
        {
            var idxLastSession = userData.sessions.Count - 1;
            var session = userData.sessions[idxLastSession];
            session.end = DateTime.Now;
            session.isActive = false;
            userData.sessions[idxLastSession] = session;

            await SaveUserDataAsync(userData);
            userDataCache.Remove(playerId); // Optional: clear cache on disconnect
        }
        else
        {
            print("   ...");
        }
#endif
    }

    private async Task<GlobalUserData> GetUserDataAsync(string playerId)
    {
#if !UNITY_WEBGL
        if (userDataCache.TryGetValue(playerId, out var cachedData))
        {
            return cachedData;
        }

        playerId = playerId.Replace("/", "_");
        var fileName = $"{playerId}.json";
        var path = $"{usersDataDirectory}{fileName}";
        if (File.Exists(path))
        {
            var json = await Task.Run(() => File.ReadAllText(path));
            var userData = JsonConvert.DeserializeObject<GlobalUserData>(json);
            userDataCache[playerId] = userData;
            return userData;
        }
        else
        {
            print($"   ...    {playerId}");
        }
#endif
        return null;
    }

    private async Task SaveUserDataAsync(GlobalUserData userData)
    {
#if !UNITY_WEBGL
        var fileName = $"{userData.playerID}.json";
        var path = $"{usersDataDirectory}{fileName}";
        var json = JsonConvert.SerializeObject(userData);
        await Task.Run(() => File.WriteAllText(path, json));
#endif
    }


    [Header("   FPS")]
    [Tooltip("      ")]
    [SerializeField] private int bufferSize = 100;

    private Queue<float> fpsBuffer = new Queue<float>();
    private Queue<float> fpsMinBuffer = new Queue<float>();
    private float sumFps = 0f;
    private float sumMinFps = 0f;
    private float sendTimer;
    private float minFpsTimer;
    private int minFps = int.MaxValue;

    void Update()
    {
        if (NetworkManager.IsConnectedClient && appFocus)
        {
            // 1)   FPS (using unscaledDeltaTime for real performance)
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0) dt = 0.016f; // Safety check
            float currentFps = 1f / dt;

            // 2)      
            fpsBuffer.Enqueue(currentFps);
            sumFps += currentFps;

            // 3)        
            if (fpsBuffer.Count > bufferSize)
                sumFps -= fpsBuffer.Dequeue();

            // 4)    ,    
            float averageFps = sumFps / fpsBuffer.Count;


            // 5)  / 
            sendTimer += Time.deltaTime;
            if (sendTimer > 10)
            {
                SendAverageFps(averageFps, minFps);
                sendTimer = 0;
            }

            minFpsTimer += Time.deltaTime;
            if (minFpsTimer > 3)
            {
                fpsMinBuffer.Enqueue(currentFps);
                sumMinFps += currentFps;

                if (fpsMinBuffer.Count > 8)
                {
                    sumMinFps -= fpsMinBuffer.Dequeue();
                    var avgMinFps = sumMinFps / fpsMinBuffer.Count;

                    if (minFps > avgMinFps)
                    { 
                        minFps = Mathf.FloorToInt(avgMinFps);
                    }

                }
            }
        }
    }

    private void SendAverageFps(float avgFps, int minFps)
    {
        //Debug.Log($"  FPS ( {fpsBuffer.Count} ): {avgFps:F1} min: {minFps}");
#if UNITY_WEBGL && YG_PLUGIN_YANDEX_GAME
        DeviceType deviceType = DeviceType.Desktopo;
        if (YandexGame.EnvironmentData.isMobile)
        {
            deviceType = DeviceType.Mobilo;
        }
        else
        if (YandexGame.EnvironmentData.isTablet)
        {
            deviceType = DeviceType.Tableto;
        }

        SendAvgFpsServerRpc
        (
            ygPlayerID,
            Mathf.RoundToInt(avgFps),
            minFps,
            deviceType
        );
#endif
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendAvgFpsServerRpc(string playerID, int fps, int minFps, DeviceType deviceType, ServerRpcParams serverRpcParams = default)
    {
        //print($"avg: {fps} min: {minFps} ### {deviceType} : {playerID} #");
        _ = SendAvgFpsAsync(playerID, fps, minFps, deviceType);
    }

    private async Task SendAvgFpsAsync(string playerID, int fps, int minFps, DeviceType deviceType)
    {
        var data = await GetUserDataAsync(playerID);
        //    !!!!!!
        if (data != null)
        {
            var idxLastSession = data.sessions.Count - 1;
            var session = data.sessions[idxLastSession];

            // TO DO
            session.deviceType = deviceType;

            var needSave = false;
            if (session.avgFps != fps)
            {
                session.avgFps = fps;
                needSave = true;
            }
            if (session.minFps != minFps)
            {
                needSave = true;
                session.minFps = minFps;
            }

            if (needSave)
            {
                data.sessions[idxLastSession] = session;
                await SaveUserDataAsync(data);
            }
        }
    }

    /// <summary>
    /// From Index.Shatal
    /// </summary>
    /// <param name="value"></param>
    public void SetYGPlayerID(string value)
    {
        ygPlayerID = value;
    }

    private bool appFocus = true;
    private void OnApplicationFocus(bool focus)
    {
        appFocus = focus;
        minFpsTimer = 0.5f;
        fpsMinBuffer.Clear();
        fpsBuffer.Clear();
        sumFps = 0;
        sumMinFps = 0;
        //print($" {appFocus}");
    }

    public enum DeviceType
    {
        Desktopo,
        Mobilo,
        Tableto
    }
}

[Serializable]
public struct SessionData
{
    public string nickname;

    public DateTime start;
    public DateTime end;
    public int countPlacedBlock;
    public int countMineBlock;
    public int countTakedBlock;
    public bool isActive;
    public ulong clientID;
    public NetworkUserManager.DeviceType deviceType;
    public int avgFps;
    public int minFps;
}

[Serializable]
public class GlobalUserData
{
    public string playerID;

    public List<SessionData> sessions;
}
