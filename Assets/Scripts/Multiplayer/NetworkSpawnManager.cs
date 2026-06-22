using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages safe spawn positions for players, maintaining a pool of pre-calculated safe spots
/// and providing on-demand safe spot searching using the world generator's data.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[DefaultExecutionOrder(100)]
public class NetworkSpawnManager : NetworkBehaviour
{
    public static NetworkSpawnManager Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private NetworkWorldGenerator worldGenerator;

    [Header("Settings")]
    [SerializeField] private int maxPoolSize = 5;
    [SerializeField] private int maxSpiralRadius = 500;
    [SerializeField] private float strictSearchDuration = 3.0f;
    [SerializeField] private float searchTimeout = 10.0f;

    private Queue<Vector3> safeSpawnPool = new Queue<Vector3>();
    private int currentSpiralRadius = 0;
    private int currentSpiralX = 0;
    private int currentSpiralZ = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _ = ReplenishSpawnPoolAsync();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSafeSpawnServerRpc(Vector3 startPos = default, ServerRpcParams rpcParams = default)
    {
        var clientId = rpcParams.Receive.SenderClientId;
        _ = ProvideSafeSpawnAsync(clientId, startPos);
    }

    private async Task ProvideSafeSpawnAsync(ulong clientId, Vector3 startSearchPos)
    {
        Vector3 spawnPos;
        
        // Use pool for center spawns if available
        if (Vector3.Distance(startSearchPos, Vector3.zero) < 10f && safeSpawnPool.Count > 0)
        {
            spawnPos = safeSpawnPool.Dequeue();
            _ = ReplenishSpawnPoolAsync(); // Refill center pool
        }
        else
        {
            // Find a specific safe spot near startSearchPos
            spawnPos = await FindSafeSpotNearAsync(startSearchPos);
        }

        if (spawnPos != Vector3.zero)
        {
            var clientParams = GetTargetClientParams(clientId);
            SetSafeSpawnClientRpc(spawnPos, clientParams);
        }
    }

    private async Task<Vector3> FindSafeSpotNearAsync(Vector3 startPos)
    {
        float startTime = Time.time;
        int chunkSize = WorldGenerator.size;
        int startX = Mathf.RoundToInt(startPos.x / chunkSize);
        int startZ = Mathf.RoundToInt(startPos.z / chunkSize);
        int chunksChecked = 0;

        for (int r = 0; r < maxSpiralRadius; r++)
        {
            bool isStrict = (Time.time - startTime) < strictSearchDuration;

            for (int x = startX - r; x <= startX + r; x++)
            {
                for (int z = startZ - r; z <= startZ + r; z++)
                {
                    if (Math.Abs(x - startX) != r && Math.Abs(z - startZ) != r) continue;

                    for (int y = 3; y >= -2; y--)
                    {
                        Vector3 chunkPos = new Vector3(x * chunkSize, y * chunkSize, z * chunkSize);
                        var (isSafe, surfaceY) = await worldGenerator.CheckChunkSafetyAsync(chunkPos, isStrict);
                        if (isSafe)
                        {
                            return new Vector3(chunkPos.x + (chunkSize / 2), surfaceY + 1.5f, chunkPos.z + (chunkSize / 2));
                        }
                    }

                    chunksChecked++;
                    if (chunksChecked % 10 == 0)
                    {
                        await Task.Yield();
                    }
                }
            }

            if (Time.time - startTime > searchTimeout)
            {
                Debug.LogWarning($"[Safe Spawn] Search timeout near {startPos}. Using fallback altitude.");
                break;
            }
        }

        return new Vector3(startPos.x, 100f, startPos.z);
    }

    [ClientRpc]
    private void SetSafeSpawnClientRpc(Vector3 spawnPos, ClientRpcParams rpcParams = default)
    {
        if (NetworkManager.LocalClient != null && NetworkManager.LocalClient.PlayerObject != null)
        {
            NetworkManager.LocalClient.PlayerObject.transform.position = spawnPos;
            Debug.Log($"[Safe Spawn] Teleported to: {spawnPos}");
        }
    }

    private async Task ReplenishSpawnPoolAsync()
    {
        int maxAttempts = 100;
        int attempts = 0;
        int chunkSize = WorldGenerator.size;

        while (safeSpawnPool.Count < maxPoolSize && attempts < maxAttempts)
        {
            attempts++;
            
            bool foundNext = false;
            for (int r = currentSpiralRadius; r < 20 && !foundNext; r++)
            {
                for (int x = -r; x <= r && !foundNext; x++)
                {
                    for (int z = -r; z <= r && !foundNext; z++)
                    {
                        if (Math.Abs(x) != r && Math.Abs(z) != r) continue;
                        
                        for (int y = 3; y >= -2; y--)
                        {
                            Vector3 chunkPos = new Vector3(x * chunkSize, y * chunkSize, z * chunkSize);
                            var (isSafe, surfaceY) = await worldGenerator.CheckChunkSafetyAsync(chunkPos);
                            if (isSafe)
                            {
                                Vector3 spawnPos = new Vector3(chunkPos.x + (chunkSize / 2), surfaceY + 1.5f, chunkPos.z + (chunkSize / 2));
                                if (!safeSpawnPool.Contains(spawnPos))
                                {
                                    safeSpawnPool.Enqueue(spawnPos);
                                    foundNext = true;
                                    currentSpiralX = x;
                                    currentSpiralZ = z;
                                    currentSpiralRadius = r;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (foundNext) break;
            }
            if (!foundNext) break; 
        }
    }

    private ClientRpcParams GetTargetClientParams(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
    }
}
