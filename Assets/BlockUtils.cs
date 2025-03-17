using System.Collections.Generic;
using UnityEngine;

namespace Ururu
{
    public static class BlockUtils
    {
        public static List<BlockData> FillBoundingBox(List<BlockData> inputBlocks)
        {
            if (inputBlocks == null || inputBlocks.Count == 0)
                return new List<BlockData>(); // ���� ������ ���� - ���������� ������ ������

            // ��� 1: ���������� ������� Bounding Box
            Vector3 min = inputBlocks[0].localPosition;
            Vector3 max = inputBlocks[0].localPosition;

            foreach (var block in inputBlocks)
            {
                min = Vector3.Min(min, block.localPosition);
                max = Vector3.Max(max, block.localPosition);
            }

            // ���������� � �����, ���� ����� (���� �������������� ������ ������������� �������)
            Vector3Int minInt = Vector3Int.FloorToInt(min);
            Vector3Int maxInt = Vector3Int.CeilToInt(max);

            // ��� 2: ������� ������� ��� �������� ������ ������� �������
            HashSet<Vector3Int> existingPositions = new HashSet<Vector3Int>();
            foreach (var block in inputBlocks)
            {
                existingPositions.Add(Vector3Int.FloorToInt(block.localPosition));
            }

            // ��� 3: ��������� ��� ������� � Bounding Box
            List<BlockData> result = new List<BlockData>();

            for (int x = minInt.x; x <= maxInt.x; x++)
            {
                for (int y = minInt.y; y <= maxInt.y; y++)
                {
                    for (int z = minInt.z; z <= maxInt.z; z++)
                    {
                        Vector3Int pos = new Vector3Int(x, y, z);
                        if (existingPositions.Contains(pos))
                        {
                            // ������� ������������ ���� � ���� ����������
                            byte blockId = 0;
                            foreach (var block in inputBlocks)
                            {
                                if (Vector3Int.FloorToInt(block.localPosition) == pos)
                                {
                                    blockId = block.blockID;
                                    break;
                                }
                            }
                            result.Add(new BlockData(pos, blockId));
                        }
                        else
                        {
                            // ������ ������
                            result.Add(new BlockData(pos, 0));
                        }
                    }
                }
            }

            return result;
        }
    }
}
