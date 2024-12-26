using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using System.Collections.Generic;
#if !UNITY_WEBGL || UNITY_EDITOR
using System.IO;
#endif

public class AudioClipSender : NetworkBehaviour
{
    public static string releaseNotesDirectory = $"{Application.dataPath}/Data/Sounds/ReleaseNotes/";

    public AudioFragmentHandler audioFragmentHandler;
    private string[] filters = { "*.mp3", "*.wav" }; // Укажите нужные фильтры
    Dictionary<ulong, int> clientIdxAudioSending = new();
    public List<AudioClip> releaseNotesSounds;

    private void Awake()
    {
        releaseNotesSounds = new();
    }

    private void Start()
    {
        audioFragmentHandler.onDataReceive.AddListener(Data_Received);

        NetworkManager.OnServerStarted += Server_Started;

        
    }

    
    private void Client_Connected(ulong clienId)
    {
        clientIdxAudioSending.Add(clienId, 0);

        SendAudioClip(releaseNotesSounds[0], clienId);
    }

    private void Server_Started()
    {
        NetworkManager.OnClientConnectedCallback += Client_Connected;

        LoadSound();
    }

    private void LoadSound()
    {
        if (!Directory.Exists(releaseNotesDirectory))
        {
            Directory.CreateDirectory(releaseNotesDirectory);
        }

        var filePaths = filters.SelectMany(filter => Directory.GetFiles(releaseNotesDirectory, filter))
                .ToArray();

          
        StartCoroutine(LoadAudioFromFile(filePaths));
    }

    private IEnumerator LoadAudioFromFile(string[] filePaths)
    {
        foreach (var filePath in filePaths)
        {
            Debug.Log("Found Audio file: " + filePath); 

            // Загружаем аудиофайл
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(filePath, GetAudioTypeFromFile(filePath)))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Получаем AudioClip из ответа
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                    var name = Path.GetFileName(filePath);
                    name = name.Substring(0, name.IndexOf("."));
                    clip.name = name;
                    releaseNotesSounds.Add(clip);
                    //PlayAudio(clip);
                }
                else
                {
                    Debug.LogError($"Error loading audio file: {request.error}");
                }
            }
        }
    }

    private AudioType GetAudioTypeFromFile(string filePath)
    {
        byte[] fileHeader = new byte[4];
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            fs.Read(fileHeader, 0, fileHeader.Length);
        }

        // Сравниваем сигнатуры
        if (fileHeader[0] == 0x52 && fileHeader[1] == 0x49 && fileHeader[2] == 0x46 && fileHeader[3] == 0x46)
            return AudioType.WAV;
        if (fileHeader[0] == 0xFF && (fileHeader[1] & 0xE0) == 0xE0) // MP3 сигнатура
            return AudioType.MPEG;
        if (fileHeader[0] == 0x4F && fileHeader[1] == 0x67 && fileHeader[2] == 0x67 && fileHeader[3] == 0x53)
            return AudioType.OGGVORBIS;

        return AudioType.UNKNOWN; // Неизвестный формат
    }

    private void PlayAudio(AudioClip clip)
    {
        // Убедитесь, что есть AudioSource
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Устанавливаем загруженный клип и воспроизводим
        audioSource.clip = clip;
        audioSource.Play();

        Debug.Log($"Playing audio: {clip.name}");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            if (IsServer)
            {
                
            }
        }
    }

    byte[] currentReceivedAudioBytes;
    private void Data_Received(byte[] data)
    {
        currentReceivedAudioBytes = data;
        AudioClipBytesReceivedServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void AudioClipBytesReceivedServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        var clip = currentSendingClips[clientId];
        ReveiveAuidioClipInfoClientRpc
        (
            clip.channels,
            clip.frequency,
            clip.name,
            GetTargetClientParams(serverRpcParams)
        );

        clientIdxAudioSending[clientId]++;

        if (clientIdxAudioSending[clientId] < releaseNotesSounds.Count)
        {
            SendAudioClip(releaseNotesSounds[clientIdxAudioSending[clientId]], clientId);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void ReveiveAuidioClipInfoClientRpc(int channels, int frequency, string name, ClientRpcParams clientRpcParams = default)
    {
        AudioClipReceiveDone(currentReceivedAudioBytes, channels, frequency, name);
    }

    Dictionary<ulong, AudioClip> currentSendingClips = new();
    public void SendAudioClip(AudioClip audioClip, ulong clientID)
    {
        if (!currentSendingClips.ContainsKey(clientID))
        {
            currentSendingClips.Add(clientID, audioClip);
        }
        else
        {
            currentSendingClips[clientID] = audioClip;
        }

        byte[] audioData = AudioClipToByteArray(audioClip);

        audioFragmentHandler.SendLargeData(audioData, 0, clientID);
        //if (audioData.Length > 0)
        //{
        //    SendAudioServerRpc(audioData);
        //}
    }



    public Dictionary<string, AudioClip> clientReceivedAudios = new();
    private void AudioClipReceiveDone(byte[] audioData, int channels, int frequency, string name)
    {
        AudioClip receivedClip = ByteArrayToAudioClip(audioData, channels, frequency);
        receivedClip.name = name;
        clientReceivedAudios.Add(name, receivedClip);
        PlayAudio(receivedClip);
    }

    private byte[] AudioClipToByteArray(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] byteArray = new byte[samples.Length * sizeof(float)];
        Buffer.BlockCopy(samples, 0, byteArray, 0, byteArray.Length);

        return byteArray;
    }

    private AudioClip ByteArrayToAudioClip(byte[] byteArray, int channels, int frequency)
    {
        float[] samples = new float[byteArray.Length / sizeof(float)];
        Buffer.BlockCopy(byteArray, 0, samples, 0, byteArray.Length);

        AudioClip clip = AudioClip.Create("ReceivedClip", samples.Length / channels, channels, frequency, false);
        clip.SetData(samples, 0);

        return clip;
    }

    private ClientRpcParams GetTargetClientParams(ServerRpcParams serverRpcParams)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId };

        return clientRpcParams;
    }

    private ClientRpcParams GetTargetClientParams(ulong clientId)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { clientId };

        return clientRpcParams;
    }
}
