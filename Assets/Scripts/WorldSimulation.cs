using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static BLOCKS;
using Newtonsoft.Json;
#if !UNITY_WEBGL
using System.IO;
#endif


/// <summary>
/// ���� ������ ������ ���, ����� �� ��� �������� � ����� ������ � �������� ��� ������� ������
/// </summary>
public class WorldSimulation : MonoBehaviour
{
    public static WorldSimulation Single;
    public static UnityEvent<ChunckComponent, Vector3Int, byte> onPlaceBlock = new();

    static string simulationDataDirectory = $"{Application.dataPath}/Data/Chuncks/Simulation/";

    public Dictionary<Vector3, SimulationChunkData> simulationsChunks = new();

    HashSet<SimulationChunkData> queueToAddSimulationChunks = new();
    [ReadOnlyField] public SimulationConfig config;

    private void Awake()
    {
        Single = this;

        //WorldGenerator.onBlockPlace.AddListener(Block_Placed);
    }

    private void Start()
    {
        LoadOrCreateSimulationConfig();
    }

    

    public void PlaceBlock(ChunckComponent chunk, Vector3 worldBlockPos, byte blockID)
    {
        // ������� ��������� �� ������� ���� �� ����� ���������� �� ������ ����������
        // � ������������ ����� ��� ��������� ����
        if (blockID == DIRT)
        {
            var localBlockPos = WorldGenerator.Inst.ToLocalBlockPos(worldBlockPos);
            onPlaceBlock?.Invoke(chunk, localBlockPos, blockID);
        }
    }

    public void SimalatebleBlockPlaced(Vector3 chunkPos, SimulatableBlockData data)
    {
        CheckDirectory();

        var fileName = GetChunckDataFileName(chunkPos);
        var path = $"{simulationDataDirectory}{fileName}.json";

        SimulationChunkData simulationChunkData;
        string json;

        if (simulationsChunks.ContainsKey(chunkPos))
        {
            simulationChunkData = simulationsChunks[chunkPos];
        }
        else
        {
            if (File.Exists(path))
            {
                json = File.ReadAllText(path);
                simulationChunkData = JsonConvert.DeserializeObject<SimulationChunkData>(json);
            }
            else
            {
                simulationChunkData = new() { ChunkPos = chunkPos, simulatableBlocks = new() };
            }
            queueToAddSimulationChunks.Add(simulationChunkData);
        }

        
        SimulatableBlockData found = default;
        foreach (var block in simulationChunkData.simulatableBlocks)
        {
            if(block.localBlockPos == data.localBlockPos)
            {
                found = block;
                break;
            }
        }
        if (found.localBlockPos == default) 
        {
            simulationChunkData.simulatableBlocks.Add(data);
        }
        else
        {
            if (simulationChunkData.simulatableBlocks.Remove(found))
            {
                print("� ��� ����� ��� ����� �������� ��������, �� � ��������");
            }
            else
            {
                print("���, �� �� ������ ����� �� ��� 0_0");
            }
        }

        json = JsonConvert.SerializeObject(simulationChunkData);
        File.WriteAllText(path, json);
    }

    public void StartSimulation()
    {
        StartCoroutine(Async());

        IEnumerator Async()
        {
            var chunkPoses = simulationsChunks.Keys;

            foreach (var chunkPos in chunkPoses)
            {
                var simulationData = simulationsChunks[chunkPos];

                SimulationChunk(simulationData);

                yield return null;
            }

            yield return null;

            CheckQueyeToAddSimulationChunks();

            StartCoroutine(Async());
        }
    }

    void SimulationChunk(SimulationChunkData simulationChunkData)
    {
        foreach (var blockData in simulationChunkData.simulatableBlocks)
        {
            var blockConfig = config.simulationBlockConfigs.Find(b => b.blockID == blockData.blockID);
            if(blockConfig != null)
            {
                var elapsed = DateTime.Now - blockData.changed;
                var seconds = elapsed.TotalSeconds;
                print($"������ ������� {seconds}");
                if(seconds > blockConfig.time)
                {

                }
            }
            else
            {
                Debug.Log($"�, ����� ������ ������ {blockData.blockID}");
            }
        }
    }

    void CheckQueyeToAddSimulationChunks()
    {
        if (queueToAddSimulationChunks.Count > 0)
        {
            foreach (var item in queueToAddSimulationChunks)
            {
                simulationsChunks.Add(item.ChunkPos, item);
            }

            queueToAddSimulationChunks.Clear();
        }
    }

    string GetChunckDataFileName(Vector3 chunckPos)
    {
        return $"{chunckPos.x}_{chunckPos.y}_{chunckPos.z}";
    }

    private void CheckDirectory()
    {
#if !UNITY_WEBGL
        if (!Directory.Exists(simulationDataDirectory))
        {
            Directory.CreateDirectory(simulationDataDirectory);
        }
#endif
    }

    private void LoadOrCreateSimulationConfig()
    {
        CheckDirectory();
        var fileName = "SimulationConfig.json";
        var path = $"{Application.dataPath}/Data/Chuncks/{fileName}";
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            config = JsonConvert.DeserializeObject<SimulationConfig>(json);
        }
        else
        {
            config = new()
            {
                simulationBlockConfigs = new List<SimulationBlockConfig>()
                {
                    new SimulationBlockConfig()
                    {
                        name = "DIRT",
                        blockID = 4,
                        time = 60 * 3,
                    }
                }
            };

            var json = JsonConvert.SerializeObject(config);
            File.WriteAllText(path, json);
        }
    }
}

[Serializable]
public class SimulationChunkData
{
    public float x, y, z;
    public HashSet<SimulatableBlockData> simulatableBlocks;

    [JsonIgnore]
    public Vector3 ChunkPos
    {
        get => new(x, y, z);
        set
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }
    }

}

[Serializable]
public struct SimulatableBlockData : INetworkSerializable
{
    public Vector3Int localBlockPos;
    public byte blockID;
    public DateTime changed;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref localBlockPos);
        serializer.SerializeValue(ref blockID);
        serializer.SerializeValue(ref changed);
    }
}


