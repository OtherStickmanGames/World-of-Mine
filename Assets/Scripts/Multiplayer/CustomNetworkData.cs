using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Данные зданий слишком большие, поэтому это только часть данных
/// </summary>
[Serializable]
public struct NetworkHeaderBuildingData : INetworkSerializable
{
    public string nameBuilding;
    public string authorName;
    public string guid;
    public int countLikes;
    public bool liked;

    // NO SENDABLE
    public List<string> playersLiked;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref nameBuilding);
        serializer.SerializeValue(ref authorName);
        serializer.SerializeValue(ref guid);
        serializer.SerializeValue(ref countLikes);
        serializer.SerializeValue(ref liked);
    }
}



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

    private byte[] Serialize(BuildingServerData d)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            // positions
            if (d.positions != null)
            {
                bw.Write(d.positions.Length);
                foreach (var v in d.positions)
                {
                    bw.Write(v.x);
                    bw.Write(v.y);
                    bw.Write(v.z);
                }
            }
            else bw.Write(0);

            // blockIDs
            if (d.blockIDs != null)
            {
                bw.Write(d.blockIDs.Length);
                bw.Write(d.blockIDs);
            }
            else bw.Write(0);

            // strings (UTF8)
            WriteString(bw, d.nameBuilding);
            WriteString(bw, d.authorName);
            WriteString(bw, d.guid);

            bw.Write(d.countLikes);
            bw.Write(d.liked);

            bw.Flush();
            return ms.ToArray();
        }
    }

    private void WriteString(BinaryWriter bw, string s)
    {
        if (s == null) { bw.Write(0); return; }
        var bytes = System.Text.Encoding.UTF8.GetBytes(s);
        bw.Write(bytes.Length);
        bw.Write(bytes);
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
    public NetworkSurveyData[] survey;

    // Не передаю по сети
    [NonSerialized]
    public AudioClip voiceClip;
    //[NonSerialized]
    public DateTime date;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref title);
        serializer.SerializeValue(ref text);
        serializer.SerializeValue(ref survey);
    }
}

[Serializable]
public struct NetworkSurveyData : INetworkSerializable
{
    public string title;
    public int votes;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref title);
        serializer.SerializeValue(ref votes);
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