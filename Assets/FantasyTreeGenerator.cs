using System;
using System.Collections.Generic;
using UnityEngine;

public class FantasyTreeGenerator
{
    // Настройки генерации деревьев
    private int sizeX = 160, sizeY = 240, sizeZ = 160;
    private int minHeight = 50, maxHeight = 80;
    private int baseTrunkRadius = 6;        // максимальный радиус внизу
    private int minTrunkRadius = 2;         // минимальный радиус вверху
    private int leafVariants = 2;
    private float leafFlatten = 0.5f;
    private float noiseScale = 0.5f;
    private float crackThreshold = 0.3f;
    private float trunkNoiseScale = 0.05f;
    private int trunkBendAmplitude = 10;
    private float maxTiltAngle = 15f;
    private System.Random rnd = new System.Random();

    public List<BlockData> GenerateTree()
    {
        var blocks = new List<BlockData>();
        int height = rnd.Next(minHeight, maxHeight + 1);

        DrawTrunk(blocks, height);
        DrawBranches(blocks, height);
        DrawLeavesSphereNoise(blocks, new Vector3Int(sizeX / 2, height, sizeZ / 2), 20, leafFlatten);
        return blocks;
    }

    private void DrawTrunk(List<BlockData> blocks, int height)
    {
        byte woodId = 5;
        int baseX = sizeX / 2, baseZ = sizeZ / 2;

        // Случайный наклон задаем один раз
        float tiltAngle = (float)(rnd.NextDouble() * 2 * Math.PI);
        float tiltRadians = maxTiltAngle * Mathf.Deg2Rad;
        float tiltDX = Mathf.Cos(tiltAngle) * Mathf.Tan(tiltRadians);
        float tiltDZ = Mathf.Sin(tiltAngle) * Mathf.Tan(tiltRadians);

        for (int y = 0; y < height; y++)
        {
            // Текущий радиус ствола
            float t = (float)y / height;
            int currentRadius = Mathf.RoundToInt(Mathf.Lerp(baseTrunkRadius, minTrunkRadius, t));

            // Изгиб по шуму
            float nx = Mathf.PerlinNoise(y * trunkNoiseScale, 0);
            float nz = Mathf.PerlinNoise(0, y * trunkNoiseScale);
            int bendX = Mathf.RoundToInt((nx * 2 - 1) * trunkBendAmplitude);
            int bendZ = Mathf.RoundToInt((nz * 2 - 1) * trunkBendAmplitude);

            // Наклон (линейно по высоте)
            int tiltX = Mathf.RoundToInt(tiltDX * y);
            int tiltZ = Mathf.RoundToInt(tiltDZ * y);

            int cx = Mathf.Clamp(baseX + bendX + tiltX, currentRadius, sizeX - currentRadius - 1);
            int cz = Mathf.Clamp(baseZ + bendZ + tiltZ, currentRadius, sizeZ - currentRadius - 1);

            // Рисуем кольцо ствола
            for (int dx = -currentRadius; dx <= currentRadius; dx++)
                for (int dz = -currentRadius; dz <= currentRadius; dz++)
                    if (dx * dx + dz * dz <= currentRadius * currentRadius)
                        TryAdd(blocks, woodId, new Vector3Int(cx + dx, y, cz + dz));
        }
    }

    private void DrawBranches(List<BlockData> blocks, int height)
    {
        int branchCount = rnd.Next(7, 12);
        int baseX = sizeX / 2, baseZ = sizeZ / 2;

        // Используем те же смещения, что и для ствола, чтобы ветки вылезали корректно
        // Предварительно запоминаем массив центров по y
        var centers = new Vector2Int[height];
        for (int y = 0; y < height; y++)
        {
            float nx = Mathf.PerlinNoise(y * trunkNoiseScale, 0);
            float nz = Mathf.PerlinNoise(0, y * trunkNoiseScale);
            int bendX = Mathf.RoundToInt((nx * 2 - 1) * trunkBendAmplitude);
            int bendZ = Mathf.RoundToInt((nz * 2 - 1) * trunkBendAmplitude);
            centers[y] = new Vector2Int(baseX + bendX, baseZ + bendZ);
        }

        for (int i = 0; i < branchCount; i++)
        {
            int by = rnd.Next(height / 3, height - 10);
            var center = centers[by];
            int tr = Mathf.RoundToInt(Mathf.Lerp(baseTrunkRadius, minTrunkRadius, (float)by / height));
            float angle = (float)(rnd.NextDouble() * 2 * Math.PI);
            int sx = center.x + Mathf.RoundToInt(Mathf.Cos(angle) * tr);
            int sz = center.y + Mathf.RoundToInt(Mathf.Sin(angle) * tr);
            DrawBranch(blocks, sx, by, sz, angle, rnd.Next(30, 50));
        }
    }

    private void DrawBranch(List<BlockData> blocks, int sx, int sy, int sz, float angle, int length)
    {
        byte woodId = 5;
        float dx = Mathf.Cos(angle), dz = Mathf.Sin(angle);
        int x = sx, y = sy, z = sz;
        int baseR = 3;
        for (int i = 0; i < length; i++)
        {
            x = Mathf.Clamp(x + Mathf.RoundToInt(dx), 0, sizeX - 1);
            z = Mathf.Clamp(z + Mathf.RoundToInt(dz), 0, sizeZ - 1);
            y = Mathf.Clamp(sy + i / 5, 0, sizeY - 1);
            float t = (float)i / length;
            int r = Mathf.Max(1, Mathf.RoundToInt(baseR * (1 - t)));
            for (int bx = -r; bx <= r; bx++)
                for (int bz = -r; bz <= r; bz++)
                    if (bx * bx + bz * bz <= r * r)
                        TryAdd(blocks, woodId, new Vector3Int(x + bx, y, z + bz));
            if (i > length * 0.6f && rnd.NextDouble() < 0.1)
                DrawLeavesSphereNoise(blocks, new Vector3Int(x, y, z), 5, leafFlatten);
        }
    }

    private void DrawLeavesSphereNoise(List<BlockData> blocks, Vector3Int center, int radius, float flatten)
    {
        int y0 = center.y;
        for (int xi = center.x - radius; xi <= center.x + radius; xi++)
            for (int zi = center.z - radius; zi <= center.z + radius; zi++)
            {
                int dx = xi - center.x, dz = zi - center.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz) / radius;
                if (d > 1) continue;
                int h = Mathf.RoundToInt(radius * flatten * (1 - d * d));
                for (int dy = -h; dy <= h; dy++)
                {
                    var pos = new Vector3Int(xi, y0 + dy, zi);
                    if (!InBounds(pos)) continue;
                    float n = Mathf.PerlinNoise((xi + y0 + dy) * noiseScale, (zi + y0 + dy) * noiseScale);
                    if (n < crackThreshold) continue;
                    TryAdd(blocks, (byte)(6 + rnd.Next(leafVariants)), pos);
                }
            }
    }

    private void TryAdd(List<BlockData> blocks, byte id, Vector3Int pos)
    {
        if (pos.x < 0 || pos.y < 0 || pos.z < 0 || pos.x >= sizeX || pos.y >= sizeY || pos.z >= sizeZ) return;
        if (!blocks.Exists(b => b.pos == pos)) blocks.Add(new BlockData { ID = id, pos = pos });
    }

    private bool InBounds(Vector3Int p) => p.x >= 0 && p.y >= 0 && p.z >= 0 && p.x < sizeX && p.y < sizeY && p.z < sizeZ;
}
