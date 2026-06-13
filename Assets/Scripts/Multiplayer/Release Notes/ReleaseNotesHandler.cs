using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using System.Linq;
using System.Globalization;
using Newtonsoft.Json;
#if !UNITY_WEBGL || UNITY_EDITOR
using System.IO;
#endif

public class ReleaseNotesHandler : NetworkBehaviour
{
    static string releaseNotesDirectory = $"{Application.dataPath}/Data/ReleaseNotes/News/";

    [SerializeField] AudioClipSender audioClipSender;

    [ReadOnlyField] public List<NetworkNewsData> newsData = new();
    [ReadOnlyField] public List<NetworkNewsData> clientNewsData = new();

    Dictionary<ulong, int> clientIdxNewsSending = new();
    HashSet<string> receivedAudioClipNames = new(); 

    public static UnityEvent<List<NetworkNewsData>> onNewsReceive = new();
    public static UnityEvent<NetworkNewsData> onNoteReceive = new();

    public static ReleaseNotesHandler Singleton;

    string format = "dd_MM_yyyy"; // ������ ������ ����
    CultureInfo provider = CultureInfo.InvariantCulture; // 

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        NetworkManager.OnServerStarted += Server_Started;
        audioClipSender.onVoiceReceive.AddListener(ClientNewsVoice_Received);
        audioClipSender.onVoiceEndPlay.AddListener(PlayVoice_Ended);

        //StartCoroutine(TestLoad());

        //CreateJsonTemplate();
    }


    IEnumerator TestLoad()
    {
        var url = "https://disk.yandex.ru/d/99GH9OoVb_lQOw";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            print($"{request.result}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                print($"{request.downloadHandler.text}");
            }
        }                
    }

    public void SurveySelect(string date, int idx)
    {
        SendSurveySelectServerRpc(date, idx);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendSurveySelectServerRpc(string date, int idx, ServerRpcParams serverRpcParams = default)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (!NetworkUserManager.Instance.playerIds.TryGetValue(clientId, out string playerId))
        {
            Debug.LogWarning($"[Survey] Client {clientId} tried to vote but has no PlayerID.");
            return;
        }

        date = date.Replace(".", "_");
        var found = newsData.Find(n => n.name == date);
        if (found.name != date || found.survey == null || idx < 0 || idx >= found.survey.Length) return;

        // Ensure list is initialized
        if (found.survey[idx].votedPlayerIds == null)
            found.survey[idx].votedPlayerIds = new List<string>();

        // Validation: Only add if player hasn't voted for this variant yet
        if (!found.survey[idx].votedPlayerIds.Contains(playerId))
        {
            found.survey[idx].votedPlayerIds.Add(playerId);
            found.survey[idx].votes = found.survey[idx].votedPlayerIds.Count;

            var json = JsonConvert.SerializeObject(found);
            var path = $"{releaseNotesDirectory}{date}.json";
            File.WriteAllText(path, json);
            Debug.Log($"[Survey] Player {playerId} voted for {found.name} option {idx}. Total: {found.survey[idx].votes}");
        }
#endif
    }

    internal void SurveyDeselect(string date, int idxSurvey)
    {
        SendSurveyDeselectServerRpc(date, idxSurvey);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendSurveyDeselectServerRpc(string date, int idx, ServerRpcParams serverRpcParams = default)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (!NetworkUserManager.Instance.playerIds.TryGetValue(clientId, out string playerId)) return;

        date = date.Replace(".", "_");
        var found = newsData.Find(n => n.name == date);
        if (found.name != date || found.survey == null || idx < 0 || idx >= found.survey.Length) return;

        if (found.survey[idx].votedPlayerIds != null && found.survey[idx].votedPlayerIds.Contains(playerId))
        {
            found.survey[idx].votedPlayerIds.Remove(playerId);
            found.survey[idx].votes = found.survey[idx].votedPlayerIds.Count;

            var json = JsonConvert.SerializeObject(found);
            var path = $"{releaseNotesDirectory}{date}.json";
            File.WriteAllText(path, json);
            Debug.Log($"[Survey] Player {playerId} removed vote from {found.name} option {idx}. Total: {found.survey[idx].votes}");
        }
#endif
    }

    private void PlayVoice_Ended(AudioClip audio)
    {
        if (audio.name != "Start_Voice")
        {
            if (clientNewsData.Count == 0)
            {
                print("��� ��������");
                return;
            }

            if (clientNewsData[0].voiceClip == null)
            {
                Debug.Log("��������� ������� �� ����� �������");
                return;
            }

            audioClipSender.PlayAudio(clientNewsData[0].voiceClip);
        }
    }

    private void ClientNewsVoice_Received(AudioClip audio)
    {
        var audioName = audio.name;
        if (audioName == "Start_Voice")
        {
            if (!receivedAudioClipNames.Contains(audioName))
            {
                audioClipSender.PlayAudio(audio);
                receivedAudioClipNames.Add(audioName);
            }
        }
        else
        {
            for (int i = 0; i < clientNewsData.Count; i++)
            {
                if (clientNewsData[i].name == audio.name)
                {
                    var data = clientNewsData[i];
                    data.voiceClip = audio;
                    clientNewsData[i] = data;

                    break;
                }
            }
        }
    }

    private void Server_Started()
    {
        NetworkManager.OnClientConnectedCallback += Client_Connected;

        LoadReleaseNotesOnServer();
    }

    

    private void LoadReleaseNotesOnServer()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (!Directory.Exists(releaseNotesDirectory))
        {
            Directory.CreateDirectory(releaseNotesDirectory);
        }

        StartCoroutine(LoadNoteFromFile());

        IEnumerator LoadNoteFromFile()
        {
            var filePaths = Directory.GetFiles(releaseNotesDirectory, "*.json");

            newsData.Clear();

            foreach (var filePath in filePaths)
            {
                Debug.Log("Found news file: " + filePath);

                var uri = filePath;
#if UNITY_STANDALONE_LINUX
                uri = filePath.Insert(0, "file://");
#endif
                using (UnityWebRequest request = UnityWebRequest.Get(uri))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var json = request.downloadHandler.text;
                        var name = Path.GetFileName(filePath);
                        
                        var data = JsonConvert.DeserializeObject<NetworkNewsData>(json, settings);
                        name = name.Substring(0, name.IndexOf("."));
                        data.name = name;
                        data.date = DateTime.ParseExact(name, format, provider);
                        data.survey ??= new NetworkSurveyData[] { };
                        newsData.Add(data);

                        //NetworkNewsData data = new();
                        //var news = request.downloadHandler.text;
                        ////print(news);
                        //var name = Path.GetFileName(filePath);
                        //name = name.Substring(0, name.IndexOf("."));
                        //data.name = name;
                        //var idxText = news.IndexOf("text:"); 
                        //data.title = news.Substring
                        //(
                        //    "title:".Length,
                        //    idxText - "text:".Length - 2
                        //);
                        //data.text = news[(idxText + 5)..];
                        
                        //data.date = DateTime.ParseExact(name, format, provider);
                        ////print(data.date);
                        //newsData.Add(data);
                    }
                    else
                    {
                        Debug.LogError($"Error loading json file: {request.error}");
                    }
                }
            }

            newsData = newsData.OrderByDescending(n => n.date).ToList();

            yield return new WaitForSeconds(1800);

            StartCoroutine(LoadNoteFromFile());
        }
#endif
    }

    private void Client_Connected(ulong clientID)
    {
        clientIdxNewsSending.Add(clientID, 0);

        // TODO �������, ����� �� �������� ������������ �������� � ������ � ��������
        StartCoroutine(Delay());

        IEnumerator Delay()
        {
            yield return new WaitForSeconds(1f);

            SendNews(clientID);
        }
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
        //newsData.date = DateTime.ParseExact(newsData.name, format, provider);
        clientNewsData.Add(newsData);
        onNoteReceive?.Invoke(newsData);
        //clientNewsData = clientNewsData.OrderByDescending(n => n.date).ToList();

        StartCoroutine(Delay());

        IEnumerator Delay()
        {
            yield return new WaitForSeconds(0.3f);

            ClientReceivedNewsServerRpc();
        }

        
    }

    [ServerRpc(RequireOwnership = false)]
    private void ClientReceivedNewsServerRpc(ServerRpcParams serverRpcParams = default)
    {
        SendNews(serverRpcParams.Receive.SenderClientId);
    }

    [ClientRpc(RequireOwnership = false)]
    private void AllNewsSendedClientRpc(ClientRpcParams clientRpcParams = default)
    {
        audioClipSender.StartSendNewsVoice(NetworkManager.LocalClientId);

        clientNewsData = clientNewsData.OrderByDescending(n => n.date).ToList();
        onNewsReceive?.Invoke(clientNewsData);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            audioClipSender?.PlayAudio(clientNewsData[0].voiceClip);
        }
    }

    private void CreateJsonTemplate()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        NetworkNewsData data = new();
        data.survey = new NetworkSurveyData[]
        {
            new NetworkSurveyData(),
            new NetworkSurveyData()
        };
        var json = JsonConvert.SerializeObject(data);
        var path = $"{releaseNotesDirectory}piso.json";
        File.WriteAllText(path, json);
#endif
    }

    public static readonly JsonSerializerSettings settings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        Error = (sender, eventArgs) =>
        {
            Debug.LogError(
                $"{eventArgs.ErrorContext.Error.Message} {eventArgs.ErrorContext.OriginalObject} {eventArgs.ErrorContext.Member}");
        }
    };
}


