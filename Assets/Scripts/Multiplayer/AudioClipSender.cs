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
        foreach (var item in releaseNotesSounds)
        {
            SendAudioClip(item, clienId);
        }
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
            Debug.Log("Found file: " + filePath);

            // Загружаем аудиофайл
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(filePath, GetAudioTypeFromFile(filePath)))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Получаем AudioClip из ответа
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
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

    private void Data_Received(byte[] data)
    {
        print("чёт пришло");
    }

    public void SendAudioClip(AudioClip audioClip, ulong clientID)
    {
        byte[] audioData = AudioClipToByteArray(audioClip);

        audioFragmentHandler.SendLargeData(audioData, 0, clientID);
        //if (audioData.Length > 0)
        //{
        //    SendAudioServerRpc(audioData);
        //}
    }

    [ServerRpc]
    private void SendAudioServerRpc(byte[] audioData)
    {
        ReceiveAudioClientRpc(audioData);
    }

    [ClientRpc]
    private void ReceiveAudioClientRpc(byte[] audioData)
    {
        //AudioClip receivedClip = ByteArrayToAudioClip(audioData, audioClip.channels, audioClip.frequency);
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
