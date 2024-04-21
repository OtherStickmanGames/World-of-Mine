using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using Newtonsoft;
using Newtonsoft.Json;

public class ChunckComponent
{
    public MeshRenderer renderer;
    public MeshFilter meshFilter;
    public MeshCollider collider;
    public Vector3 pos;

    public byte[,,] blocks;

    public NavMeshModifier navMeshModifier;
    public NavMeshSurface meshSurface;

    public Dictionary<Vector3, NavMeshLink> links = new Dictionary<Vector3, NavMeshLink>();


    /// <summary>
    /// Не забываем перед вызовом сделать +1 по оси X
    /// </summary>
    /// <param name="blockPosition"></param>
    /// <returns></returns>
    public byte GetBlockID(Vector3 blockPosition)
    {
        var pos = renderer.transform.position;

        int xBlock = (int)(blockPosition.x - pos.x);
        int yBlock = (int)(blockPosition.y - pos.y);
        int zBlock = (int)(blockPosition.z - pos.z);

        //Debug.Log($"{xBlock}|{yBlock}|{zBlock}");
        byte blockID = blocks[xBlock, yBlock, zBlock];
        //blocks[xBlock, yBlock, zBlock] = 0;

        //var mesh = WorldGenerator.Inst.UpdateMesh(this);//, (int)pos.x, (int)pos.y, (int)pos.z);
        //meshFilter.mesh = mesh;
        //collider.sharedMesh = mesh;

        return blockID;
    }
}

[JsonObject]
public class ChunckData
{
    //public Vector3 pos;
    //[JsonProperty]
    //public List<List<byte>> blocks;
    [JsonProperty]
    public byte[,,] blocks;

    public ChunckData(ChunckComponent chunck)
    {
        blocks = chunck.blocks;
        //blocks = new List<List<byte>>();

        //for (int x = 0; x < WorldGenerator.size; x++)
        //{
        //    for (int y = 0; y < WorldGenerator.size; y++)
        //    {
        //        if (x < blocks.Count)
        //        {
        //            if (y < blocks[x].Count)
        //            {

        //            }
        //            else
        //            {
        //                blocks[x].Add(chunck.blocks[x, y, 0]);
        //            }
        //        }
        //        else
        //        {
        //            blocks.Add(new List<byte>());
        //            blocks[x].Add(chunck.blocks[x, y, 0]);
        //        }
        //    }
        //}
    }
}
