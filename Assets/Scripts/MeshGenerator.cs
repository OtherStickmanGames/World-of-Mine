using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MeshGenerator : MonoBehaviour
{
    float TextureOffset = 1f / 16f;

    Dictionary<BlockSide, List<Vector3>> blockVerticesSet;
    Dictionary<BlockSide, List<int>> blockTrianglesSet;

    readonly List<Vector3> vertices = new();
    readonly List<int> triangulos = new();
    readonly List<Vector2> uvs = new();

    public static MeshGenerator Single;

    private void Awake()
    {
        Single = this;
    }

    private void Start()
    {
        blockVerticesSet = new Dictionary<BlockSide, List<Vector3>>();
        blockTrianglesSet = new Dictionary<BlockSide, List<int>>();

        DictionaryInits();
        InitTriangulos();
    }

    public static void NormalizeBlocksPositions(List<BlockData> blocksData, List<ChunckData.JsonTurnedBlock> turnedBlocks)
    {
        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var minZ = float.MaxValue;

        foreach (var blockData in blocksData)
        {
            if (blockData.pos.x < minX)
            {
                minX = blockData.pos.x;
            }
            if (blockData.pos.y < minY)
            {
                minY = blockData.pos.y;
            }
            if (blockData.pos.z < minZ)
            {
                minZ = blockData.pos.z;
            }
        }

        foreach (var blockData in blocksData)
        {
            blockData.pos.x -= minX;
            blockData.pos.y -= minY;
            blockData.pos.z -= minZ;
        }

        for (int i = 0; i < turnedBlocks.Count; i++)
        {
            var turnData = turnedBlocks[i];
            turnData.posX -= minX;
            turnData.posY -= minY;
            turnData.posZ -= minZ;
            turnedBlocks[i] = turnData;
        }
    }

    public Mesh GenerateMesh(List<BlockData> normalizedBlocksData)
    {
        Mesh mesh = new();
        mesh.Clear();

        triangulos.Clear();
        vertices.Clear();
        uvs.Clear();

        var maxX = Mathf.FloorToInt(normalizedBlocksData.Max(b => b.pos.x));
        var maxY = Mathf.FloorToInt(normalizedBlocksData.Max(b => b.pos.y));
        var maxZ = Mathf.FloorToInt(normalizedBlocksData.Max(b => b.pos.z));

        var dictionaryBlocks = normalizedBlocksData.ToDictionary
        (
            b => new Vector3Int
            (
                Mathf.FloorToInt(b.pos.x),
                Mathf.FloorToInt(b.pos.y),
                Mathf.FloorToInt(b.pos.z)
            ),
            b => b.ID
        );

        Vector3Int pos = Vector3Int.zero;
        BlockUVS b;

        foreach (var blockData in normalizedBlocksData)
        {
            b = BlockUVS.GetBlock(blockData.ID);

            pos.x = Mathf.FloorToInt(blockData.pos.x);
            pos.y = Mathf.FloorToInt(blockData.pos.y);
            pos.z = Mathf.FloorToInt(blockData.pos.z);

            if (pos.z >= maxZ || !dictionaryBlocks.ContainsKey(pos + Vector3Int.forward))
            {
                CreateBlockSide(BlockSide.Front, pos.x, pos.y, pos.z, b);
            }
            if (pos.z == 0 || !dictionaryBlocks.ContainsKey(pos + Vector3Int.back))
            {
                CreateBlockSide(BlockSide.Back, pos.x, pos.y, pos.z, b);
            }
            if (pos.x >= maxX || !dictionaryBlocks.ContainsKey(pos + Vector3Int.right))
            {
                CreateBlockSide(BlockSide.Right, pos.x, pos.y, pos.z, b);
            }
            if (pos.x == 0 || !dictionaryBlocks.ContainsKey(pos + Vector3Int.left))
            {
                CreateBlockSide(BlockSide.Left, pos.x, pos.y, pos.z, b);
            }
            if (pos.y >= maxY || !dictionaryBlocks.ContainsKey(pos + Vector3Int.up))
            {
                CreateBlockSide(BlockSide.Top, pos.x, pos.y, pos.z, b);
            }
            if (pos.y == 0 || !dictionaryBlocks.ContainsKey(pos + Vector3Int.down))
            {
                CreateBlockSide(BlockSide.Bottom, pos.x, pos.y, pos.z, b);
            }
        }

        if (vertices.Count > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        else
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangulos.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.OptimizeReorderVertexBuffer();
        mesh.Optimize();

        return mesh;
    }

    void DictionaryInits()
    {
        List<Vector3> verticesFront = new List<Vector3>
            {
                new Vector3( 0, 0, 1 ),
                new Vector3(-1, 0, 1 ),
                new Vector3(-1, 1, 1 ),
                new Vector3( 0, 1, 1 ),
            };
        List<Vector3> verticesBack = new List<Vector3>
            {
                new Vector3( 0, 0, 0 ),
                new Vector3(-1, 0, 0 ),
                new Vector3(-1, 1, 0 ),
                new Vector3( 0, 1, 0 ),
            };
        List<Vector3> verticesRight = new List<Vector3>
            {
                new Vector3( 0, 0, 0 ),
                new Vector3( 0, 0, 1 ),
                new Vector3( 0, 1, 1 ),
                new Vector3( 0, 1, 0 ),
            };
        List<Vector3> verticesLeft = new List<Vector3>
            {
                new Vector3(-1, 0, 0 ),
                new Vector3(-1, 0, 1 ),
                new Vector3(-1, 1, 1 ),
                new Vector3(-1, 1, 0 ),
            };
        List<Vector3> verticesTop = new List<Vector3>
            {
                new Vector3( 0, 1, 0 ),
                new Vector3(-1, 1, 0 ),
                new Vector3(-1, 1, 1 ),
                new Vector3( 0, 1, 1 ),
            };
        List<Vector3> verticesBottom = new List<Vector3>
            {
                new Vector3( 0, 0, 0 ),
                new Vector3(-1, 0, 0 ),
                new Vector3(-1, 0, 1 ),
                new Vector3( 0, 0, 1 ),
            };

        blockVerticesSet.Add(BlockSide.Front, null);
        blockVerticesSet.Add(BlockSide.Back, null);
        blockVerticesSet.Add(BlockSide.Right, null);
        blockVerticesSet.Add(BlockSide.Left, null);
        blockVerticesSet.Add(BlockSide.Top, null);
        blockVerticesSet.Add(BlockSide.Bottom, null);

        blockVerticesSet[BlockSide.Front]  = verticesFront;//.ToNativeArray(Allocator.Persistent);
        blockVerticesSet[BlockSide.Back]   = verticesBack;//.ToNativeArray(Allocator.Persistent);
        blockVerticesSet[BlockSide.Right]  = verticesRight;//.ToNativeArray(Allocator.Persistent);
        blockVerticesSet[BlockSide.Left]   = verticesLeft;//.ToNativeArray(Allocator.Persistent);
        blockVerticesSet[BlockSide.Top]    = verticesTop;//.ToNativeArray(Allocator.Persistent);
        blockVerticesSet[BlockSide.Bottom] = verticesBottom;
    }

    void InitTriangulos()
    {
        List<int> trianglesFront = new List<int> { 3, 2, 1, 4, 3, 1 };
        List<int> trianglesBack = new List<int> { 1, 2, 3, 1, 3, 4 };
        List<int> trianglesRight = new List<int> { 1, 3, 2, 4, 3, 1 };
        List<int> trianglesLeft = new List<int> { 2, 3, 1, 1, 3, 4 };
        List<int> trianglesTop = new List<int> { 1, 2, 3, 1, 3, 4 };
        List<int> trianglesBottom = new List<int> { 3, 2, 1, 4, 3, 1 };

        blockTrianglesSet.Add(BlockSide.Front, trianglesFront);
        blockTrianglesSet.Add(BlockSide.Back, trianglesBack);
        blockTrianglesSet.Add(BlockSide.Right, trianglesRight);
        blockTrianglesSet.Add(BlockSide.Left, trianglesLeft);
        blockTrianglesSet.Add(BlockSide.Top, trianglesTop);
        blockTrianglesSet.Add(BlockSide.Bottom, trianglesBottom);
    }

    Vector3 vertexPos;
    void CreateBlockSide(BlockSide side, int x, int y, int z, BlockUVS b)
    {
        List<Vector3> vrtx = blockVerticesSet[side];
        List<int> trngls = blockTrianglesSet[side];
        int offset = 1;

        triangulos.Add(trngls[0] - offset + vertices.Count);
        triangulos.Add(trngls[1] - offset + vertices.Count);
        triangulos.Add(trngls[2] - offset + vertices.Count);

        triangulos.Add(trngls[3] - offset + vertices.Count);
        triangulos.Add(trngls[4] - offset + vertices.Count);
        triangulos.Add(trngls[5] - offset + vertices.Count);

        vertexPos.x = x + vrtx[0].x;
        vertexPos.y = y + vrtx[0].y;
        vertexPos.z = z + vrtx[0].z;
        vertices.Add(vertexPos); // 1

        vertexPos.x = x + vrtx[1].x;
        vertexPos.y = y + vrtx[1].y;
        vertexPos.z = z + vrtx[1].z;
        vertices.Add(vertexPos); // 2

        vertexPos.x = x + vrtx[2].x;
        vertexPos.y = y + vrtx[2].y;
        vertexPos.z = z + vrtx[2].z;
        vertices.Add(vertexPos); // 3

        vertexPos.x = x + vrtx[3].x;
        vertexPos.y = y + vrtx[3].y;
        vertexPos.z = z + vrtx[3].z;
        vertices.Add(vertexPos); // 4

        AddUVS(side, b);
    }

    void AddUVS(BlockSide side, BlockUVS b)
    {
        switch (side)
        {
            case BlockSide.Front:
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, (TextureOffset * b.TextureYSide) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, (TextureOffset * b.TextureYSide) + TextureOffset));
                break;
            case BlockSide.Back:
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, (TextureOffset * b.TextureYSide) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, (TextureOffset * b.TextureYSide) + TextureOffset));
                break;
            case BlockSide.Right:
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, (TextureOffset * b.TextureYSide) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, (TextureOffset * b.TextureYSide) + TextureOffset));

                break;
            case BlockSide.Left:
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, (TextureOffset * b.TextureYSide) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, (TextureOffset * b.TextureYSide) + TextureOffset));

                break;
            case BlockSide.Top:
                uvs.Add(new Vector2(TextureOffset * b.TextureX, TextureOffset * b.TextureY));
                uvs.Add(new Vector2((TextureOffset * b.TextureX) + TextureOffset, TextureOffset * b.TextureY));
                uvs.Add(new Vector2((TextureOffset * b.TextureX) + TextureOffset, (TextureOffset * b.TextureY) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureX, (TextureOffset * b.TextureY) + TextureOffset));

                break;
            case BlockSide.Bottom:
                uvs.Add(new Vector2(TextureOffset * b.TextureXBottom, TextureOffset * b.TextureYBottom));
                uvs.Add(new Vector2((TextureOffset * b.TextureXBottom) + TextureOffset, TextureOffset * b.TextureYBottom));
                uvs.Add(new Vector2((TextureOffset * b.TextureXBottom) + TextureOffset, (TextureOffset * b.TextureYBottom) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureXBottom, (TextureOffset * b.TextureYBottom) + TextureOffset));

                break;

        }

    }
}
