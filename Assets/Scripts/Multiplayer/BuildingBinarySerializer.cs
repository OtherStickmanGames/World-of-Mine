using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using static ChunckData;

public static class BuildingBinarySerializer
{
    private const int FORMAT_VERSION = 1;
    private const int MAX_POSITIONS = 100000; // Р›РёРјРёС‚ Р±Р»РѕРєРѕРІ // Р›РёРјРёС‚ Р±Р»РѕРєРѕРІ
    private const int MAX_NAME_LENGTH = 256; // Р›РёРјРёС‚ РЅР°Р·РІР°РЅРёСЏ // Р›РёРјРёС‚ РЅР°Р·РІР°РЅРёСЏ
    private const int MAX_TURNED_BLOCKS = 100000;

    public static byte[] Serialize(Vector3[] positions, byte[] blockIDs, string nameBuilding, List<JsonTurnedBlock> turnedBlocks)
    {
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms, Encoding.UTF8, true))
        {
            w.Write(FORMAT_VERSION);

            w.Write(positions != null ? positions.Length : 0);
            if (positions != null)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    w.Write(positions[i].x);
                    w.Write(positions[i].y);
                    w.Write(positions[i].z);
                }
            }

            w.Write(blockIDs != null ? blockIDs.Length : 0);
            if (blockIDs != null && blockIDs.Length > 0)
            {
                w.Write(blockIDs);
            }

            var nameBytes = Encoding.UTF8.GetBytes(nameBuilding ?? string.Empty);
            w.Write(nameBytes.Length);
            if (nameBytes.Length > 0) w.Write(nameBytes);

            w.Write(turnedBlocks != null ? turnedBlocks.Count : 0);
            if (turnedBlocks != null)
            {
                for (int i = 0; i < turnedBlocks.Count; i++)
                {
                    var tb = turnedBlocks[i];
                    w.Write(tb.posX);
                    w.Write(tb.posY);
                    w.Write(tb.posZ);

                    var arr = tb.turnsBlockData;
                    w.Write(arr != null ? arr.Length : 0);
                    if (arr != null)
                    {
                        for (int j = 0; j < arr.Length; j++)
                        {
                            w.Write(arr[j].angle);
                            w.Write((int)arr[j].axis);
                        }
                    }
                }
            }

            w.Flush();
            return ms.ToArray();
        }
    }

    public static void Deserialize(byte[] data, out Vector3[] positions, out byte[] blockIDs, out string nameBuilding, out List<JsonTurnedBlock> turnedBlocks)
    {
        positions = Array.Empty<Vector3>();
        blockIDs = Array.Empty<byte>();
        nameBuilding = string.Empty;
        turnedBlocks = new List<JsonTurnedBlock>();

        using (var ms = new MemoryStream(data))
        using (var r = new BinaryReader(ms, Encoding.UTF8, true))
        {
            int version = r.ReadInt32();
            if (version != FORMAT_VERSION)
            {
                throw new InvalidOperationException($"РќРµРїРѕРґРґРµСЂР¶РёРІР°РµРјР°СЏ РІРµСЂСЃРёСЏ С„РѕСЂРјР°С‚Р°: {version}");
            }

            int posCount = r.ReadInt32();
            if (posCount < 0 || posCount > MAX_POSITIONS)
            {
                throw new InvalidDataException($"РќРµРґРѕРїСѓСЃС‚РёРјРѕРµ РєРѕР»РёС‡РµСЃС‚РІРѕ РїРѕР·РёС†РёР№: {posCount}");
            }

            positions = new Vector3[posCount];
            for (int i = 0; i < posCount; i++)
            {
                float x = r.ReadSingle();
                float y = r.ReadSingle();
                float z = r.ReadSingle();
                positions[i] = new Vector3(x, y, z);
            }

            int blockLen = r.ReadInt32();
            if (blockLen < 0 || blockLen > MAX_POSITIONS)
            {
                throw new InvalidDataException($"РќРµРґРѕРїСѓСЃС‚РёРјРѕРµ РєРѕР»РёС‡РµСЃС‚РІРѕ ID Р±Р»РѕРєРѕРІ: {blockLen}");
            }

            if (blockLen > 0)
            {
                blockIDs = r.ReadBytes(blockLen);
                if (blockIDs.Length != blockLen) throw new EndOfStreamException("РќРµРѕР¶РёРґР°РЅРЅС‹Р№ РєРѕРЅРµС† С„Р°Р№Р»Р° РїСЂРё С‡С‚РµРЅРёРё ID Р±Р»РѕРєРѕРІ");
            }
            else
            {
                blockIDs = Array.Empty<byte>();
            }

            int nameLen = r.ReadInt32();
            if (nameLen < 0 || nameLen > MAX_NAME_LENGTH)
            {
                throw new InvalidDataException($"РќРµРґРѕРїСѓСЃС‚РёРјР°СЏ РґР»РёРЅР° РЅР°Р·РІР°РЅРёСЏ РїРѕСЃС‚СЂРѕР№РєРё: {nameLen}");
            }

            if (nameLen > 0)
            {
                var nameBytes = r.ReadBytes(nameLen);
                if (nameBytes.Length != nameLen) throw new EndOfStreamException("РќРµРѕР¶РёРґР°РЅРЅС‹Р№ РєРѕРЅРµС† С„Р°Р№Р»Р° РїСЂРё С‡С‚РµРЅРёРё РЅР°Р·РІР°РЅРёСЏ");
                nameBuilding = Encoding.UTF8.GetString(nameBytes);
            }
            else
            {
                nameBuilding = string.Empty;
            }

            int tbCount = r.ReadInt32();
            if (tbCount < 0 || tbCount > MAX_TURNED_BLOCKS)
            {
                throw new InvalidDataException($"РќРµРґРѕРїСѓСЃС‚РёРјРѕРµ РєРѕР»РёС‡РµСЃС‚РІРѕ РїРѕРІРµСЂРЅСѓС‚С‹С… Р±Р»РѕРєРѕРІ: {tbCount}");
            }

            turnedBlocks = new List<JsonTurnedBlock>(tbCount);
            for (int i = 0; i < tbCount; i++)
            {
                JsonTurnedBlock tb = new JsonTurnedBlock();
                tb.posX = r.ReadSingle();
                tb.posY = r.ReadSingle();
                tb.posZ = r.ReadSingle();

                int innerCount = r.ReadInt32();
                if (innerCount < 0 || innerCount > 100) 
                {
                    throw new InvalidDataException($"РќРµРґРѕРїСѓСЃС‚РёРјРѕРµ РєРѕР»РёС‡РµСЃС‚РІРѕ РІРЅСѓС‚СЂРµРЅРЅРёС… РїРѕРІРѕСЂРѕС‚РѕРІ: {innerCount}");
                }

                if (innerCount > 0)
                {
                    var inner = new TurnBlockData[innerCount];
                    for (int j = 0; j < innerCount; j++)
                    {
                        inner[j].angle = r.ReadSingle();
                        inner[j].axis = (RotationAxis)r.ReadInt32();
                    }
                    tb.turnsBlockData = inner;
                }
                else
                {
                    tb.turnsBlockData = Array.Empty<TurnBlockData>();
                }

                turnedBlocks.Add(tb);
            }
        }
    }
}