using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkUserManager : NetworkBehaviour
{
    public Dictionary<int, string> users = new Dictionary<int, string>();

    public static NetworkUserManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        NetworkManager.OnClientConnectedCallback += Client_Connected;
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
        print($"{userName} ### {serverRpcParams.Receive.SenderClientId}");
    }
}
