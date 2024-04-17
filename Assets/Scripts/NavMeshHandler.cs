using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;

public class NavMeshHandler : MonoBehaviour
{
    [SerializeField] float distNavUpdate = 38;
    [SerializeField] NavMeshSurface navMeshSurface;

    WorldGenerator worldGenerator;
    Player player;

    private void Start()
    {
        worldGenerator = FindObjectOfType<WorldGenerator>();
        player = FindObjectOfType<Player>();
        WorldGenerator.onBlockPick.AddListener(Block_Picked);
    }

    private void Block_Picked(BlockData _)
    {
        if (navMeshSurface.navMeshData == null)
            navMeshSurface.BuildNavMesh();

        UpdateNavMesh();
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.B))
        //{
        //    navMeshSurface.BuildNavMesh();
        //}

        if (Input.GetKeyDown(KeyCode.U))
        {
            UpdateNavMesh();
            //CheckChuncks();
        }

        //CheckChuncks();
    }

    void UpdateNavMesh()
    {
        navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);

        //NavMeshBuilder.UpdateNavMeshDataAsync
    }

    void CheckChuncks()
    {
        var chuncks = worldGenerator.chuncks;
        foreach (var pair in chuncks)
        {
            var chunckSize = WorldGenerator.size;
            var offset = Vector3.one * (chunckSize / 2);
            var chunkPos = pair.Value.renderer.transform.position + offset;
            var dist = Vector3.Distance(chunkPos, player.transform.position);
            //print(dist);
            if(dist < distNavUpdate)
            {
                pair.Value.renderer.gameObject.layer = 7;
            }
            else
            {
                pair.Value.renderer.gameObject.layer = 0;
            }
        }
    }
}
