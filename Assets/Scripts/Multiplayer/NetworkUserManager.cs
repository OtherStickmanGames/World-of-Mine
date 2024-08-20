using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class NetworkUserManager : NetworkBehaviour
{
    public Dictionary<ulong, string> users = new Dictionary<ulong, string>();

    [field:SerializeField]
    public bool UserRegistred { get; private set; } = false;

    public static NetworkUserManager Instance;
    public static UnityEvent onUserRegistred = new UnityEvent();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        NetworkManager.OnClientConnectedCallback  += Client_Connected;
        NetworkManager.OnClientDisconnectCallback += Client_Disconnected;
    }

    private void Client_Disconnected(ulong clientId)
    {
        if (NetworkManager.IsServer)
        {
            Debug.Log($"{users[clientId]}: Disconnected # Server Time:{DateTime.Now}");
            users.Remove(clientId);
        }
    }

    private void Client_Connected(ulong clientId)
    {
        if (NetworkManager.LocalClientId == clientId)
        {
            SendUserNameServerRpc(UserData.Owner.userName);
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
}
