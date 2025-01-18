using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Данные передающиеся по сети
/// </summary>
[Serializable]
public struct BuildingServerData : INetworkSerializable
{
    public Vector3[] positions;
    public byte[] blockIDs;
    public string nameBuilding;
    public string authorName;
    public string guid;
    public int countLikes;
    public bool liked;

    // NO SENDABLE
    public List<string> playersLiked;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref positions);
        serializer.SerializeValue(ref blockIDs);
        serializer.SerializeValue(ref nameBuilding);
        serializer.SerializeValue(ref authorName);
        serializer.SerializeValue(ref guid);
        serializer.SerializeValue(ref countLikes);
        serializer.SerializeValue(ref liked);
    }
}

[Serializable]
public struct NetworkTurnedBlockData : INetworkSerializable
{
    public Vector3 worldBlockPos;
    public byte blockID;
    public NetworkTurnData[] turnsData;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref worldBlockPos);
        serializer.SerializeValue(ref blockID);
        serializer.SerializeValue(ref turnsData);
    }
}

public struct NetworkTurnData : INetworkSerializable
{
    public float angle;
    public RotationAxis axis;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref angle);
        serializer.SerializeValue(ref axis);
    }
}

[Serializable]
public struct NetworkNewsData : INetworkSerializable
{
    public string name;
    public string title;
    public string text;

    // Не передаю по сети
    public AudioClip voiceClip;
    public DateTime date;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref title);
        serializer.SerializeValue(ref text);
    }
}

public struct NetworkUserSessionData : INetworkSerializable
{
    //public string

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        //serializer.SerializeValue(ref angle);
        //serializer.SerializeValue(ref axis);
    }
}