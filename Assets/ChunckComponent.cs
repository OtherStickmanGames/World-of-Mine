using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

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
