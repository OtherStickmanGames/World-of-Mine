using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ConvertTools
{
    public static NetworkTurnedBlockData[] ToNetworkTurnedBlocksData(List<ChunckData.JsonTurnedBlock> turnedBlocks)
    {
        var length = turnedBlocks.Count;
        NetworkTurnedBlockData[] result = new NetworkTurnedBlockData[length];
        for (int i = 0; i < length; i++)
        {
            var jsonData = turnedBlocks[i];
            result[i] = new NetworkTurnedBlockData()
            {
                worldBlockPos = jsonData.Pos,
                turnsData = new NetworkTurnData[jsonData.turnsBlockData.Length],
            };
            for (int j = 0; j < result[i].turnsData.Length; j++)
            {
                result[i].turnsData[j].angle = jsonData.turnsBlockData[j].angle;
                result[i].turnsData[j].axis = jsonData.turnsBlockData[j].axis;
            }

        }

        return result;
    }

    public static List<ChunckData.JsonTurnedBlock> ToJsonTurnedBlock(NetworkTurnedBlockData[] turnedBlocks)
    {
        List<ChunckData.JsonTurnedBlock> result = new();

        var length = turnedBlocks.Length;

        for (int i = 0; i < length; i++)
        {
            var networkData = turnedBlocks[i];
            ChunckData.JsonTurnedBlock jsonData = new();

            jsonData.posX = networkData.worldBlockPos.x;
            jsonData.posY = networkData.worldBlockPos.y;
            jsonData.posZ = networkData.worldBlockPos.z;
            jsonData.turnsBlockData = new TurnBlockData[networkData.turnsData.Length];

            for (int j = 0; j < networkData.turnsData.Length; j++)
            {
                jsonData.turnsBlockData[j].angle = networkData.turnsData[j].angle;
                jsonData.turnsBlockData[j].axis = networkData.turnsData[j].axis;
            }

            result.Add(jsonData);
        }

        return result;
    }
}
