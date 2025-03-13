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
/// Этот скрипт сделан так, чтобы он мог работать и через сервер и локально для оффлайн режима
/// </summary>
public class WorldSimulation : MonoBehaviour
{
    public static WorldSimulation Single;
    public static UnityEvent<ChunckComponent, Vector3Int> onBlockMine = new();
    public static UnityEvent<ChunckComponent, Vector3Int, byte> onPlaceBlock = new();
    public static UnityEvent<Vector3, Vector3Int, byte> onBlockChanged = new();

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

        WorldGenerator.onBlockPick.AddListener(Block_Mined);
    }

    public void PlaceBlock(ChunckComponent chunk, Vector3 worldBlockPos, byte blockID)
    {
        // Сначала проверяем на Клиенте есть ли смысл передавать на сервер информацию
        // о поставленном блоке для симуляции мира
        if (blockID == DIRT)
        {
            var localBlockPos = WorldGenerator.Inst.ToLocalBlockPos(worldBlockPos);
            InvokePlaceSimulatableBlock(chunk, localBlockPos, blockID);

            //var bottomBlockPos = worldBlockPos + Vector3Int.down;
            //var bottomBlockID = WorldGenerator.Inst.GetBlockID(bottomBlockPos);
            //if (bottomBlockID is DIRT)
            //{
            //    var bottomChunk = WorldGenerator.Inst.GetChunk(bottomBlockPos);
            //    var bottomLocalPos = WorldGenerator.Inst.ToLocalBlockPos(bottomBlockPos);
            //    onBlockMine?.Invoke(bottomChunk, bottomLocalPos);
            //}
        }

        CheckBottomBlock(worldBlockPos, blockID);
    }

    void CheckBottomBlock(Vector3 worldBlockPos, byte blockID)
    {
        var bottomBlockPos = worldBlockPos + Vector3Int.down;
        var bottomBlockID = WorldGenerator.Inst.GetBlockID(bottomBlockPos);
        if (blockID is DIRT or GRASS)
        {
            if (bottomBlockID is GRASS)
            {
                WorldGenerator.Inst.PlaceBlock(bottomBlockPos, DIRT);
                WorldGenerator.Inst.SetBlockAndUpdateChunck(bottomBlockPos, DIRT);
            }
            if (bottomBlockID is DIRT)
            {
                var bottomChunk = WorldGenerator.Inst.GetChunk(bottomBlockPos);
                var bottomLocalPos = WorldGenerator.Inst.ToLocalBlockPos(bottomBlockPos);
                onBlockMine?.Invoke(bottomChunk, bottomLocalPos);
            }
        }
    }

    void InvokePlaceSimulatableBlock(ChunckComponent chunk, Vector3Int localBlockPos, byte blockID)
    {
        onPlaceBlock?.Invoke(chunk, localBlockPos, blockID);
    }

    public void SimalatebleBlockPlaced(Vector3 chunkPos, SimulatableBlockData data)
    {
        CheckDirectory();

        var fileName = GetChunkDataFileName(chunkPos);
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
                print("я хуй пойми как такая ситуация возможна, но я ремувнул");
            }
            else
            {
                print("бля, шо то вообще пошло не так 0_0");
            }
        }

        json = JsonConvert.SerializeObject(simulationChunkData);
        File.WriteAllText(path, json);
    }

    public void RemoveSimulatableBlockData(Vector3 chunkPos, Vector3Int localBlockPos)
    {
        CheckDirectory();

        var fileName = GetChunkDataFileName(chunkPos);
        var path = $"{simulationDataDirectory}{fileName}.json";

        SimulationChunkData simulationChunkData = null;
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
        }

        if (simulationChunkData != null)
        {
            SimulatableBlockData found = default;
            foreach (var block in simulationChunkData.simulatableBlocks)
            {
                if (block.localBlockPos == localBlockPos)
                {
                    found = block;
                    break;
                }
            }
            if (found.localBlockPos != default)
            {
                simulationChunkData.simulatableBlocks.Remove(found);

                json = JsonConvert.SerializeObject(simulationChunkData);
                File.WriteAllText(path, json);
            }
        }
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
        HashSet<SimulatableBlockData> needRemove = new();
        foreach (var blockData in simulationChunkData.simulatableBlocks)
        {
            var blockConfig = config.simulationBlockConfigs.Find(b => b.blockID == blockData.blockID);
            if (blockConfig != null)
            {
                var elapsed = DateTime.Now - blockData.changed;
                var seconds = elapsed.TotalSeconds;
                //print($"прошло времени {seconds}");
                if (seconds > blockConfig.time)
                {
                    InvokeBlockSimulation(blockData, simulationChunkData.ChunkPos, out var removeSimulationData);
                    if (removeSimulationData)
                    {
                        needRemove.Add(blockData);
                    }
                }
            }
            else
            {
                Debug.Log($"Э, хуйло добавь конфиг {blockData.blockID}");
            }
        }

        foreach (var expired in needRemove)
        {
            simulationChunkData.simulatableBlocks.Remove(expired);
        }

        if (needRemove.Count > 0)
        {
            SaveSimulationData(simulationChunkData);
        }
    }

    void InvokeBlockSimulation(SimulatableBlockData blockData, Vector3 chunkPos, out bool removeSimulationData)
    {
        removeSimulationData = false;
        switch (blockData.blockID)
        {
            case DIRT:
                var chunckData = WorldData.GetChunkData(chunkPos);
                var changedBlock = chunckData.changedBlocks.Find(b => b.Pos == blockData.localBlockPos);
                if (changedBlock != null)
                {
                    changedBlock.blockId = GRASS;
                }
                else
                {
                    changedBlock = new ChunckData.JsonBlockData()
                    {
                        blockId = GRASS,
                        posX = blockData.localBlockPos.x,
                        posY = blockData.localBlockPos.y,
                        posZ = blockData.localBlockPos.z
                    };
                    chunckData.changedBlocks.Add(changedBlock);
                }
                var x = blockData.localBlockPos.x;
                var y = blockData.localBlockPos.y;
                var z = blockData.localBlockPos.z;
                chunckData.blocks[x, y, z] = GRASS;

                onBlockChanged?.Invoke(chunkPos, blockData.localBlockPos, GRASS);

                //print($"Есть шо {changedBlock}");
                WorldData.SaveChunkData(chunckData, chunkPos);
                removeSimulationData = true;
                break;
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

    void SaveSimulationData(SimulationChunkData simulationChunkData)
    {
        var fileName = GetChunkDataFileName(simulationChunkData.ChunkPos);
        var path = $"{simulationDataDirectory}{fileName}.json";
        var json = JsonConvert.SerializeObject(simulationChunkData);
        File.WriteAllText(path, json);
    }

    string GetChunkDataFileName(Vector3 chunckPos)
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
                        time = 6 * 3,
                    }
                }
            };

            var json = JsonConvert.SerializeObject(config);
            File.WriteAllText(path, json);
        }
    }

    private void Block_Mined(BlockData blockData)
    {
        var isSimulatable = blockData.ID is DIRT;
        if (isSimulatable)
        {
            var chunk = WorldGenerator.Inst.GetChunk(blockData.pos);
            var localBlockPos = WorldGenerator.Inst.ToLocalBlockPos(blockData.pos);
            onBlockMine?.Invoke(chunk, localBlockPos);
        }

        var bottomBlockPos = blockData.pos + Vector3Int.down;
        var bottomBlockID = WorldGenerator.Inst.GetBlockID(bottomBlockPos);
        if (bottomBlockID is DIRT)
        {
            var chunk = WorldGenerator.Inst.GetChunk(bottomBlockPos);
            var localBlockPos = WorldGenerator.Inst.ToLocalBlockPos(bottomBlockPos);
            InvokePlaceSimulatableBlock(chunk, localBlockPos, bottomBlockID);
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




