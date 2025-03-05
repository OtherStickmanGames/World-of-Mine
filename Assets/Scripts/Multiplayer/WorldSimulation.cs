using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static BLOCKS;

public class WorldSimulation : MonoBehaviour
{
    public static WorldSimulation Single;
    public static UnityEvent<ChunckComponent, Vector3Int, byte> onPlaceBlock;

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
        if (blockID == DIRT)
        {
            var localBlockPos = WorldGenerator.Inst.ToLocalBlockPos(worldBlockPos);
            onPlaceBlock?.Invoke(chunk, localBlockPos, blockID);
        }
    }

    private void Block_Placed(BlockData blockData)
    {
        // TO DO, Вах надо переделать на получение    
    }
}

[Serializable]
public class SimulationChunkData
{
    public Vector3 chunkPos;
    public HashSet<SimulatableBlockData> simulatableBlocks;
}

[Serializable]
public struct SimulatableBlockData
{
    public Vector3Int localBlockPos;
    public byte blockID;
    public DateTime changed;
}


