using System;
using System.Collections;
using System.Collections.Generic;
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
                    EndUserSession(playerIds[clientId]);
                    playerIds.Remove(clientId);
                }
            }
            else
            {
                Debug.Log($"Надо поразбираться шо за шляпа с дисконектом без конекта: client id {clientId}");
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
            Debug.Log($"Нет юзера {ownerID}");
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
#if !UNITY_WEBGL
        if (!Directory.Exists(usersDataDirectory))
        {
            Directory.CreateDirectory(usersDataDirectory);
        }

        playerId = playerId.Replace("/", "_");
        
        if(!playerIds.TryAdd(serverRpcParams.Receive.SenderClientId, playerId))
        {
            print($"Почему-то не добавился player ID {playerId}");
        }

        var fileName = $"{playerId}.json";
        var path = $"{usersDataDirectory}{fileName}";
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var userData = JsonConvert.DeserializeObject<GlobalUserData>(json);
            var session = new SessionData()
            {
                nickname = nickname,
                start = DateTime.Now,
                isActive = true,
                clientID = serverRpcParams.Receive.SenderClientId,
            };
            userData.sessions.Add(session);

            json = JsonConvert.SerializeObject(userData);
            File.WriteAllText(path, json);
        }
        else
        {
            var userData = new GlobalUserData() { playerID = playerId };
            var session = new SessionData()
            {
                nickname = nickname,
                start = DateTime.Now,
                isActive = true,
                clientID = serverRpcParams.Receive.SenderClientId,
            };
            userData.sessions = new();
            userData.sessions.Add(session);

            var json = JsonConvert.SerializeObject(userData);
            File.WriteAllText(path, json);
        }
#endif
    }

    // TODO переделать, чтобы держать в оперативе все данные юзеров
    public void AddMinedBlock(ulong clienID)
    {
#if !UNITY_WEBGL
        if (!playerIds.ContainsKey(clienID))
            return;

        var playerId = playerIds[clienID];
        var fileName = $"{playerId}.json";
        var path = $"{usersDataDirectory}{fileName}";
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var userData = JsonConvert.DeserializeObject<GlobalUserData>(json);
            var idxLastSession = userData.sessions.Count - 1;
            var session = userData.sessions[idxLastSession];

            session.countMineBlock++;
            userData.sessions[idxLastSession] = session;

            json = JsonConvert.SerializeObject(userData);
            File.WriteAllText(path, json);
        }
#endif
    }

    // TODO переделать, чтобы держать в оперативе все данные юзеров
    public void AddPlacedBlock(ulong clienID)
    {
#if !UNITY_WEBGL
        if (!playerIds.ContainsKey(clienID))
            return;

        var playerId = playerIds[clienID];
        var fileName = $"{playerId}.json";
        var path = $"{usersDataDirectory}{fileName}";
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var userData = JsonConvert.DeserializeObject<GlobalUserData>(json);
            var idxLastSession = userData.sessions.Count - 1;
            var session = userData.sessions[idxLastSession];

            session.countPlacedBlock++;
            userData.sessions[idxLastSession] = session;

            json = JsonConvert.SerializeObject(userData);
            File.WriteAllText(path, json);
        }
#endif
    }

    private void EndUserSession(string playerId)
    {
#if !UNITY_WEBGL
        playerId = playerId.Replace("/", "_");
        var fileName = $"{playerId}.json";
        var path = $"{usersDataDirectory}{fileName}";
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var userData = JsonConvert.DeserializeObject<GlobalUserData>(json);
            var idxLastSession = userData.sessions.Count - 1;
            var session = userData.sessions[idxLastSession];
            session.end = DateTime.Now;
            session.isActive = false;
            userData.sessions[idxLastSession] = session;

            json = JsonConvert.SerializeObject(userData);
            File.WriteAllText(path, json);
        }
        else
        {
            print("чё за хуйня блять...");
        }
#endif
    }

    private GlobalUserData GetUserData(string playerId)
    {
#if !UNITY_WEBGL
        playerId = playerId.Replace("/", "_");
        var fileName = $"{playerId}.json";
        var path = $"{usersDataDirectory}{fileName}";
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<GlobalUserData>(json);
        }
        else
        {
            print($"чё за хуйня блять... нет данных юзера {playerId}");
            
        }
#endif
        return null;
    }

    private SessionData GetLastSession(GlobalUserData userData)
    {
        var idxLastSession = userData.sessions.Count - 1;
        return userData.sessions[idxLastSession];
    }

    private void SetLastSession(GlobalUserData userData, SessionData sessionData)
    {
        var idxLastSession = userData.sessions.Count - 1;
        userData.sessions[idxLastSession] = sessionData;
    }

    private void SaveUserData(GlobalUserData userData)
    {
#if !UNITY_WEBGL
        var fileName = $"{userData.playerID}.json";
        var path = $"{usersDataDirectory}{fileName}";
        var json = JsonConvert.SerializeObject(userData);
        File.WriteAllText(path, json);
#endif
    }


    [Header("Буфер последних значений FPS")]
    [Tooltip("Сколько последних кадров учитывать при расчёте среднего")]
    [SerializeField] private int bufferSize = 100;

    private Queue<float> fpsBuffer = new Queue<float>();
    private Queue<float> fpsMinBuffer = new Queue<float>();
    private float sumFps = 0f;
    private float sumMinFps = 0f;
    private float sendTimer;
    private float minFpsTimer;
    private int minFps = 60;

    void Update()
    {
        if (NetworkManager.IsConnectedClient)
        {
            // 1) Текущее значение FPS
            float currentFps = 1f / Time.deltaTime;

            // 2) Добавляем в буфер и обновляем сумму
            fpsBuffer.Enqueue(currentFps);
            sumFps += currentFps;

            

            // 3) Если превысили размер буфера — убираем самое старое
            if (fpsBuffer.Count > bufferSize)
                sumFps -= fpsBuffer.Dequeue();

            // 4) Считаем среднее по тому, что осталось в буфере
            float averageFps = sumFps / fpsBuffer.Count;


            // 5) Вывод / отправка
            sendTimer += Time.deltaTime;
            if (sendTimer > 10)
            {
                SendAverageFps(averageFps, minFps);
                sendTimer = 0;
            }

            minFpsTimer += Time.deltaTime;
            if (minFpsTimer > 3 && appFocus)
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
        // Вставьте сюда ваш код отправки на сервер или в аналитику
        //Debug.Log($"Скользящее среднее FPS (последние {fpsBuffer.Count} кадров): {avgFps:F1} min: {minFps}");
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
        var data = GetUserData(playerID);
        // ЕСТЬ ЯВНАЯ ПРОБЛЕМОС !!!!!!
        if (data != null)
        {
            var session = GetLastSession(data);

            // TO DO Отправлять девайс тип вместе со стартом сессии
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
                session.minFps = fps;
            }

            if (needSave)
            {
                SetLastSession(data, session);
                SaveUserData(data);
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

    private bool appFocus;
    private void OnApplicationFocus(bool focus)
    {
        appFocus = focus;
        minFpsTimer = 0.5f;
        fpsMinBuffer.Clear();
        //print($"фокус {appFocus}");
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


