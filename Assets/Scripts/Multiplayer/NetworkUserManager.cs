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


public class NetworkUserManager : NetworkBehaviour
{
    public Dictionary<ulong, string> users = new Dictionary<ulong, string>();
    public Dictionary<ulong, string> playerIds = new Dictionary<ulong, string>();

    [field: SerializeField]
    public bool UserRegistred { get; private set; } = false;

    public static NetworkUserManager Instance;
    public static UnityEvent onUserRegistred = new UnityEvent();

    static string usersDataDirectory = $"{Application.dataPath}/Data/Users/";


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
                Debug.Log($"Надо поразбираться шо за шляпа с дисконектом без конекта");
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
            SendYGUserConnectedServerRpc(userData.userName, YG.YandexGame.playerId);
#endif

#if UNITY_STANDALONE && !UNITY_SERVER
            SendYGUserConnectedServerRpc(userData.userName, "878sdf78sd78f5");
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

        ReceiveNicknameClientRpc(users[ownerID], crp);
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

        playerIds.Add(serverRpcParams.Receive.SenderClientId, playerId);

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

    private void EndUserSession(string playerId)
    {
#if !UNITY_WEBGL
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
}

[Serializable]
public class GlobalUserData
{
    public string playerID;

    public List<SessionData> sessions;
}


