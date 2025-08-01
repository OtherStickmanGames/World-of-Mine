using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Events;
#if !UNITY_WEBGL
using System.IO;
#endif

public class AudioClipSender : NetworkBehaviour
{
    public static string releaseNotesDirectory = $"{Application.dataPath}/Data/Sounds/ReleaseNotes/";

    public AudioFragmentHandler audioFragmentHandler;
    private string[] filters = { "*.mp3", "*.wav", ".ogg" }; // ������� ������ �������
    Dictionary<ulong, int> clientIdxAudioSending = new();
    public List<AudioClip> releaseNotesSounds;

    [HideInInspector] public UnityEvent<AudioClip> onVoiceReceive;
    [HideInInspector] public UnityEvent<AudioClip> onVoiceEndPlay;

    AudioSource audioSource;

    private void Awake()
    {
        releaseNotesSounds = new();
    }

    private void Start()
    {
        audioFragmentHandler.onDataReceive.AddListener(Data_Received);

        NetworkManager.OnServerStarted += Server_Started;

    }

    /// <summary>
    /// ����� �������� �������, ������ ����� ��������� ���� ������� ��������
    /// </summary>
    /// <param name="clienId"></param>
    public void StartSendNewsVoice(ulong clienId)
    {
        SendVoicesServerRpc(clienId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendVoicesServerRpc(ulong clienId, ServerRpcParams serverRpcParams = default)
    {
        if (releaseNotesSounds.Count > 0)
        {
            clientIdxAudioSending.Add(clienId, 0);

            SendAudioClip(releaseNotesSounds[0], clienId);
        }
        else
        {
            print("��� ����� ��� ��������");
        }
    }

    private void Server_Started()
    {
        LoadSound();
    }

    private void LoadSound()
    {
        #if !UNITY_WEBGL
        if (!Directory.Exists(releaseNotesDirectory))
        {
            Directory.CreateDirectory(releaseNotesDirectory);
        }

        var filePaths = filters.SelectMany(filter => Directory.GetFiles(releaseNotesDirectory, filter))
                .ToArray();

          
        StartCoroutine(LoadAudioFromFile(filePaths));
#endif
    }

#if !UNITY_WEBGL
    private IEnumerator LoadAudioFromFile(string[] filePaths)
    {
        foreach (var filePath in filePaths)
        {
            Debug.Log("Found Audio file: " + filePath);

            var uri = filePath;
#if UNITY_STANDALONE_LINUX
            uri = filePath.Insert(0, "file://");
#endif

            // ��������� ���������
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, GetAudioTypeFromFile(filePath)))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // �������� AudioClip �� ������
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
#endif

#if !UNITY_WEBGL
    private AudioType GetAudioTypeFromFile(string filePath)
    {
        byte[] fileHeader = new byte[4];
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            fs.Read(fileHeader, 0, fileHeader.Length);
        }

        // ���������� ���������
        if (fileHeader[0] == 0x52 && fileHeader[1] == 0x49 && fileHeader[2] == 0x46 && fileHeader[3] == 0x46)
            return AudioType.WAV;
        if (fileHeader[0] == 0xFF && (fileHeader[1] & 0xE0) == 0xE0) // MP3 ���������
            return AudioType.MPEG;
        if (fileHeader[0] == 0x4F && fileHeader[1] == 0x67 && fileHeader[2] == 0x67 && fileHeader[3] == 0x53)
            return AudioType.OGGVORBIS;

        return AudioType.UNKNOWN; // ����������� ������
    }
#endif

    public void PlayAudio(AudioClip clip)
    {
        // ���������, ��� ���� AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // ������������� ����������� ���� � �������������
        audioSource.clip = clip;
        audioSource.Play();

        startPlayingFlag = true;

        Debug.Log($"Playing audio: {clip.name}");
    }


    bool startPlayingFlag;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            if (IsServer)
            {

            }
        }

        if (startPlayingFlag && !audioSource.isPlaying)
        {
            startPlayingFlag = false;
            onVoiceEndPlay?.Invoke(audioSource.clip);
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
            NetTool.GetTargetClientParams(serverRpcParams)
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

        Debug.Log($"������� �������� {audioClip.name}");

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
        onVoiceReceive?.Invoke(receivedClip);
        //PlayAudio(receivedClip);
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

}
