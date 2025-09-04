using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using static ChunckData;

public static class BuildingBinarySerializer
{
    // версия формата — увеличь при изменениях
    private const int FORMAT_VERSION = 1;

    public static byte[] Serialize(Vector3[] positions, byte[] blockIDs, string nameBuilding, List<JsonTurnedBlock> turnedBlocks)
    {
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms, Encoding.UTF8, true))
        {
            // header: версия
            w.Write(FORMAT_VERSION);

            // positions
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

            // blockIDs
            w.Write(blockIDs != null ? blockIDs.Length : 0);
            if (blockIDs != null && blockIDs.Length > 0)
            {
                w.Write(blockIDs);
            }

            // nameBuilding (explicit UTF8 length + bytes)
            var nameBytes = Encoding.UTF8.GetBytes(nameBuilding ?? string.Empty);
            w.Write(nameBytes.Length);
            if (nameBytes.Length > 0) w.Write(nameBytes);

            // turnedBlocks
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
                            w.Write((int)arr[j].axis); // enum as int
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
                // можно обработать по-другому, пока бросим исключение — это явное несовпадение формата
                throw new InvalidOperationException($"Unsupported format version: {version}");
            }

            // positions
            int posCount = r.ReadInt32();
            positions = new Vector3[posCount];
            for (int i = 0; i < posCount; i++)
            {
                float x = r.ReadSingle();
                float y = r.ReadSingle();
                float z = r.ReadSingle();
                positions[i] = new Vector3(x, y, z);
            }

            // blockIDs
            int blockLen = r.ReadInt32();
            if (blockLen > 0)
            {
                blockIDs = r.ReadBytes(blockLen);
                if (blockIDs.Length != blockLen) throw new EndOfStreamException("Unexpected end when reading blockIDs");
            }
            else
            {
                blockIDs = Array.Empty<byte>();
            }

            // nameBuilding
            int nameLen = r.ReadInt32();
            if (nameLen > 0)
            {
                var nameBytes = r.ReadBytes(nameLen);
                if (nameBytes.Length != nameLen) throw new EndOfStreamException("Unexpected end when reading nameBuilding");
                nameBuilding = Encoding.UTF8.GetString(nameBytes);
            }
            else
            {
                nameBuilding = string.Empty;
            }

            // turnedBlocks
            int tbCount = r.ReadInt32();
            turnedBlocks = new List<JsonTurnedBlock>(tbCount);
            for (int i = 0; i < tbCount; i++)
            {
                JsonTurnedBlock tb = new JsonTurnedBlock();
                tb.posX = r.ReadSingle();
                tb.posY = r.ReadSingle();
                tb.posZ = r.ReadSingle();

                int innerCount = r.ReadInt32();
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
