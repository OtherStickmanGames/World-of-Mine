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
/// Ётот скрипт сделан так, чтобы он мог работать и через сервер и локально дл€ оффлайн режима
/// </summary>
public class WorldSimulation : MonoBehaviour
{
    public static WorldSimulation Single;
    public static UnityEvent<ChunckComponent, Vector3Int, byte> onPlaceBlock = new();

    static string simulationDataDirectory = $"{Application.dataPath}/Data/Chuncks/Simulation/";

    public Dictionary<Vector3, SimulationChunkData> simulationsChunks = new();


    private void Awake()
    {
        Single = this;

        //WorldGenerator.onBlockPlace.AddListener(Block_Placed);
    }

    private void Start()
    {

    }

    public void PlaceBlock(ChunckComponent chunk, Vector3 worldBlockPos, byte blockID)
    {
        // —начала провер€ем на  лиенте есть ли смысл передавать на сервер информацию
        // о поставленном блоке дл€ симул€ции мира
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

        SimulationChunkData simulationData;

        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            simulationData = JsonConvert.DeserializeObject<SimulationChunkData>(json);
        }
        else
        {
            simulationData = new() { chunkPos = chunkPos, simulatableBlocks = new() };
        }

        if (!simulationData.simulatableBlocks.Contains(data))
        {
            print("нихуааа");
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
}

[Serializable]
public class SimulationChunkData
{
    public Vector3 chunkPos;
    public HashSet<SimulatableBlockData> simulatableBlocks;
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


