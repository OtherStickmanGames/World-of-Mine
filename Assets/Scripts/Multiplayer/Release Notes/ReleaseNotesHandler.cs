using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
#if !UNITY_WEBGL || UNITY_EDITOR
using System.IO;
#endif

public class ReleaseNotesHandler : NetworkBehaviour
{
    static string releaseNotesDirectory = $"{Application.dataPath}/Data/ReleaseNotes/News/";

    public List<NetworkNewsData> newsData = new();
    public List<NetworkNewsData> clientNewsData = new();

    Dictionary<ulong, int> clientIdxNewsSending = new();

    public static UnityEvent<List<NetworkNewsData>> onNewsReceive = new();

    public static ReleaseNotesHandler Singleton;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        NetworkManager.OnServerStarted += Server_Started;
    }

    private void Server_Started()
    {
        NetworkManager.OnClientConnectedCallback += Client_Connected;

        LoadReleaseNotes();
    }

    private void LoadReleaseNotes()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (!Directory.Exists(releaseNotesDirectory))
        {
            Directory.CreateDirectory(releaseNotesDirectory);
        }

        var filePaths = Directory.GetFiles(releaseNotesDirectory, "*.txt");


        StartCoroutine(LoadAudioFromFile(filePaths));

        IEnumerator LoadAudioFromFile(string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                Debug.Log("Found news file: " + filePath);

                // Загружаем аудиофайл
                using (UnityWebRequest request = UnityWebRequest.Get(filePath))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        NetworkNewsData data = new();
                        var news = request.downloadHandler.text;
                        //print(news);
                        var name = Path.GetFileName(filePath);
                        name = name.Substring(0, name.IndexOf("."));
                        data.name = name;
                        var idxText = news.IndexOf("text:"); 
                        data.title = news.Substring
                        (
                            "title:".Length,
                            idxText - "text:".Length - 2
                        );
                        data.text = news[(idxText + 5)..];
                        data.date = DateTime.Parse(name.Replace("_", "/"));
                        newsData.Add(data);
                    }
                    else
                    {
                        Debug.LogError($"Error loading audio file: {request.error}");
                    }
                }
            }
        }
#endif
    }

    private void Client_Connected(ulong clientID)
    {
        clientIdxNewsSending.Add(clientID, 0);

        SendNews(clientID);
    }

    private void SendNews(ulong clientID)
    {
        var idx = clientIdxNewsSending[clientID];

        var rpcParams = NetTool.GetTargetClientParams(clientID);
        
        if (idx < newsData.Count)
        {
            ReceiveNewsDataClientRpc(newsData[idx], rpcParams);
            clientIdxNewsSending[clientID]++;
        }
        else
        {
            AllNewsSendedClientRpc(rpcParams);
        }
    }

    
    [ClientRpc(RequireOwnership = false)]
    private void ReceiveNewsDataClientRpc(NetworkNewsData newsData, ClientRpcParams clientRpcParams = default)
    {
        clientNewsData.Add(newsData);
        ClientReceivedNewsServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ClientReceivedNewsServerRpc(ServerRpcParams serverRpcParams = default)
    {
        SendNews(serverRpcParams.Receive.SenderClientId);
    }

    [ClientRpc(RequireOwnership = false)]
    private void AllNewsSendedClientRpc(ClientRpcParams clientRpcParams = default)
    {
        onNewsReceive?.Invoke(clientNewsData);
    }
}


