using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine;
using System;

public class UI : MonoBehaviour
{
    [SerializeField] Button btnHost;
    [SerializeField] Button btnClient;

    private void Awake()
    {
        btnHost.onClick.AddListener(Host_Clicked);
        btnClient.onClick.AddListener(Client_Clicked);
    }

    private void Client_Clicked()
    {
        NetworkManager.Singleton.StartClient();
    }

    private void Host_Clicked()
    {
        NetworkManager.Singleton.StartHost();
    }
}
