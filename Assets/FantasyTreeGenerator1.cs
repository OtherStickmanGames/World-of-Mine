using System;
using System.Collections.Generic;
using UnityEngine;

namespace TonkoTree
{
    public class FantasyTreeGenerator
    {
        // Параметры генерации
        private int sizeX = 256, sizeY = 512, sizeZ = 256;
        private int minHeight = 50, maxHeight = 80;
        private int woodVariants = 2;  // количество вариаций ствола
        private int leafVariants = 2;  // количество вариаций листвы
        private System.Random rnd = new System.Random();

        // Генерирует и возвращает список блоков дерева
        public List<BlockData> GenerateTree()
        {
            var blocks = new List<BlockData>();
            int cx = sizeX / 2;
            int cz = sizeZ / 2;

            // Определяем высоту ствола и вариант ID ствола
            int height = rnd.Next(minHeight, maxHeight + 1);
            byte woodId = (byte)(5 + rnd.Next(0, woodVariants));

            // Рисуем ствол
            for (int y = 0; y < height; y++)
            {
                var block = new BlockData();
                block.ID = woodId;
                block.pos = new Vector3Int(cx, y, cz);
                blocks.Add(block);
            }

            // Генерируем ветви
            int branchCount = rnd.Next(3, 6);
            for (int i = 0; i < branchCount; i++)
            {
                float angle = (float)(rnd.NextDouble() * 2 * Math.PI);
                int by = rnd.Next(height / 2, height - 2);
                int branchLength = rnd.Next(4, 8);
                DrawBranch(blocks, cx, by, cz, angle, branchLength);
            }

            // Добавляем крону на вершине
            DrawLeavesSphere(blocks, cx, height, cz, rnd.Next(4, 6));

            return blocks;
        }

        private void DrawBranch(List<BlockData> blocks, int startX, int startY, int startZ, float angle, int length)
        {
            byte woodId = (byte)(5 + rnd.Next(0, woodVariants));
            float dx = Mathf.Cos(angle);
            float dz = Mathf.Sin(angle);
            int x = startX;
            int y = startY;
            int z = startZ;

            for (int i = 0; i < length; i++)
            {
                x = Mathf.Clamp(x + Mathf.RoundToInt(dx), 0, sizeX - 1);
                z = Mathf.Clamp(z + Mathf.RoundToInt(dz), 0, sizeZ - 1);
                y = Mathf.Min(startY + i / 3, sizeY - 1);

                var pos = new Vector3Int(x, y, z);
                if (!blocks.Exists(b => b.pos == pos))
                {
                    var block = new BlockData();
                    block.ID = woodId;
                    block.pos = pos;
                    blocks.Add(block);
                }
            }

            // Крона на конце ветки
            DrawLeavesSphere(blocks, x, y, z, rnd.Next(3, 5));
        }

        private void DrawLeavesSphere(List<BlockData> blocks, int cx, int cy, int cz, int radius)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
                for (int y = cy - radius; y <= cy + radius; y++)
                    for (int z = cz - radius; z <= cz + radius; z++)
                    {
                        if (x < 0 || x >= sizeX || y < 0 || y >= sizeY || z < 0 || z >= sizeZ)
                            continue;

                        int dx = x - cx;
                        int dy = y - cy;
                        int dz = z - cz;
                        if (dx * dx + dy * dy + dz * dz <= radius * radius)
                        {
                            if (rnd.NextDouble() < 0.8)
                            {
                                byte leafId = (byte)(6 + rnd.Next(0, leafVariants));
                                var pos = new Vector3Int(x, y, z);
                                if (!blocks.Exists(b => b.pos == pos))
                                {
                                    var block = new BlockData();
                                    block.ID = leafId;
                                    block.pos = pos;
                                    blocks.Add(block);
                                }
                            }
                        }
                    }
        }
    }
}
