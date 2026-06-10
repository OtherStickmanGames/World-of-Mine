using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Newtonsoft.Json;
using Unity.Collections;

public class AdminDashboardManager : NetworkBehaviour
{
    public static AdminDashboardManager Instance { get; private set; }

    public event Action<PlayerDashboardStat[]> OnDataReceived;
    public event Action OnClearRequested;

    public NetworkVariable<int> ServerFps = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private float serverFpsTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (IsServer)
        {
            serverFpsTimer += Time.unscaledDeltaTime;
            if (serverFpsTimer >= 1f)
            {
                ServerFps.Value = Mathf.FloorToInt(1f / Time.unscaledDeltaTime);
                serverFpsTimer = 0;
            }
        }
    }

    public void RequestData()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            RequestDataServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDataServerRpc(ServerRpcParams rpcParams = default)
    {
        _ = ProcessAndSendDataAsync(rpcParams.Receive.SenderClientId);
    }

    private async Task ProcessAndSendDataAsync(ulong targetClient)
    {
        string dir = $"{Application.dataPath}/Data/Users/";
        var targetParams = GetTargetParams(targetClient);

        if (!Directory.Exists(dir))
        {
            ClearDashboardClientRpc(targetParams);
            return;
        }

        string[] files = Directory.GetFiles(dir, "*.json");
        List<PlayerDashboardStat> stats = new List<PlayerDashboardStat>();

        foreach (var file in files)
        {
            try
            {
                GlobalUserData data = await Task.Run(() => 
                {
                    string json = File.ReadAllText(file);
                    return JsonConvert.DeserializeObject<GlobalUserData>(json);
                });
                
                if (data != null && data.sessions != null && data.sessions.Count > 0)
                {
                    stats.Add(CalculateStat(data));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdminDashboard] Failed to process {file}: {e.Message}");
            }
        }

        // 1. Tell client to clear the table
        ClearDashboardClientRpc(targetParams);

        // 2. Send data in small chunks (10 players per RPC) to avoid network buffer overflow
        const int chunkSize = 10;
        for (int i = 0; i < stats.Count; i += chunkSize)
        {
            int count = Math.Min(chunkSize, stats.Count - i);
            var chunk = stats.GetRange(i, count);
            SendDataChunkClientRpc(chunk.ToArray(), targetParams);
            
            // Give the network a frame to breathe if there's a lot of data
            if (stats.Count > chunkSize) await Task.Yield();
        }
    }

    private PlayerDashboardStat CalculateStat(GlobalUserData data)
    {
        PlayerDashboardStat stat = new PlayerDashboardStat();
        stat.PlayerID = new FixedString64Bytes(data.playerID);
        stat.SessionCount = data.sessions.Count;
        
        string latestNickname = data.playerID; 
        float totalSec = 0;
        float maxSec = 0;
        int sumAvgFps = 0;
        int minFpsValue = int.MaxValue;
        int validFpsSessions = 0;

        // Check if this specific player is CURRENTLY connected to the server
        bool isCurrentlyOnline = false;
        if (!string.IsNullOrEmpty(data.playerID))
        {
            isCurrentlyOnline = NetworkUserManager.Instance.playerIds.ContainsValue(data.playerID);
        }

        for (int i = 0; i < data.sessions.Count; i++)
        {
            var session = data.sessions[i];
            bool isLast = (i == data.sessions.Count - 1);

            if (!string.IsNullOrEmpty(session.nickname))
                latestNickname = session.nickname;

            DateTime endTime;
            if (session.isActive && isLast && isCurrentlyOnline)
            {
                endTime = DateTime.Now;
            }
            else if (!session.isActive)
            {
                endTime = session.end.Year < 2000 ? session.start : session.end;
            }
            else
            {
                endTime = session.start;
            }

            if (endTime < session.start) endTime = session.start; 

            float duration = (float)(endTime - session.start).TotalSeconds;
            totalSec += duration;
            if (duration > maxSec) maxSec = duration;

            if (session.avgFps > 0)
            {
                sumAvgFps += session.avgFps;
                validFpsSessions++;
            }
            
            if (session.minFps > 0 && session.minFps < minFpsValue)
            {
                minFpsValue = session.minFps;
            }
        }

        stat.Nickname = new FixedString64Bytes(latestNickname);
        stat.TotalPlaytimeHours = totalSec / 3600f;
        stat.MaxSessionTimeHours = maxSec / 3600f;
        stat.AvgFps = validFpsSessions > 0 ? sumAvgFps / validFpsSessions : 0;
        stat.MinFps = minFpsValue == int.MaxValue ? 0 : minFpsValue;
        stat.IsOnline = isCurrentlyOnline;

        return stat;
    }

    [ClientRpc]
    private void ClearDashboardClientRpc(ClientRpcParams rpcParams = default)
    {
        OnClearRequested?.Invoke();
    }

    [ClientRpc]
    private void SendDataChunkClientRpc(PlayerDashboardStat[] stats, ClientRpcParams rpcParams = default)
    {
        OnDataReceived?.Invoke(stats);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeletePlayerDataServerRpc(string playerID, ServerRpcParams rpcParams = default)
    {
        _ = DeletePlayerDataAsync(playerID, rpcParams.Receive.SenderClientId);
    }

    private async Task DeletePlayerDataAsync(string playerID, ulong requesterId)
    {
#if !UNITY_WEBGL
        string cleanID = playerID.Replace("/", "_");
        string path = $"{Application.dataPath}/Data/Users/{cleanID}.json";

        try
        {
            if (File.Exists(path))
            {
                await Task.Run(() => File.Delete(path));
                Debug.Log($"[AdminDashboard] Player data deleted: {playerID}");
                await ProcessAndSendDataAsync(requesterId);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AdminDashboard] Failed to delete player data {playerID}: {e.Message}");
        }
#endif
    }

    private ClientRpcParams GetTargetParams(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };
    }
}

[Serializable]
public struct PlayerDashboardStat : INetworkSerializable
{
    public FixedString64Bytes PlayerID;
    public FixedString64Bytes Nickname;
    public int SessionCount;
    public float TotalPlaytimeHours;
    public float MaxSessionTimeHours;
    public int AvgFps;
    public int MinFps;
    public bool IsOnline;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerID);
        serializer.SerializeValue(ref Nickname);
        serializer.SerializeValue(ref SessionCount);
        serializer.SerializeValue(ref TotalPlaytimeHours);
        serializer.SerializeValue(ref MaxSessionTimeHours);
        serializer.SerializeValue(ref AvgFps);
        serializer.SerializeValue(ref MinFps);
        serializer.SerializeValue(ref IsOnline);
    }
}
