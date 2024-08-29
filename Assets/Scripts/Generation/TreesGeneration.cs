using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;
using static BLOCKS;

public class TreesGeneration : MonoBehaviour
{
    [SerializeField] ProceduralGeneration proceduralGeneration;

    [Header("Настройки генерации")]
    [SerializeField] float yCorrect = 1f;
    [SerializeField] float noiseScale = 50;
    [SerializeField] float landThresold = 11f;
    [SerializeField] float landHeight = 1f;
    [SerializeField] float landBump = 30f;
    [SerializeField] float landHeightSlice = 8f;
    [SerializeField] float randomFactor = 888;

    [Header("Деревья")]
    [SerializeField] TreeData[] treeDatas;

    Random random = new Random(888);

    private void Awake()
    {
        ChunckComponent.onBlocksSeted.AddListener(ChunckBlocks_Seted);
    }

    private void ChunckBlocks_Seted(ChunckComponent chunckComponent)
    {
        GenerateBlockIdSettings settings = new GenerateBlockIdSettings()
        {
            yCorrect = yCorrect,
            noiseScale = noiseScale,
            landThresold = landThresold,
            landHeight = landHeight,
            landBump = landBump,
            landHeightSlice = landHeightSlice,
            randomFactor = randomFactor,
        };

        int chunckSize = chunckComponent.size;
       
        foreach (var grassPos in chunckComponent.grassBlocks)
        {
            var id = proceduralGeneration.GetBlockID
            (
                grassPos.x,
                0,
                grassPos.z,
                settings
            );

            if (id > 0)
            {
                int randValue = random.Next(0, 180);
                for (int i = 0; i < treeDatas.Length; i++)
                {
                    if (randValue < treeDatas[i].spawnChance)
                    {
                        var json = treeDatas[i].treeJson.text;
                        var data = JsonConvert.DeserializeObject<SaveBuildingData>(json);

                        var blocks = data.blocksData.changedBlocks;
                        int grassLocalX = grassPos.x - (int)chunckComponent.pos.x;
                        int grassLocalY = grassPos.y - (int)chunckComponent.pos.y;
                        int grassLocalZ = grassPos.z - (int)chunckComponent.pos.z;

                        chunckComponent.blocks[grassLocalX, grassLocalY, grassLocalZ] = DIRT;


                        foreach (var block in blocks)
                        {
                            int treeBlockX = grassLocalX + (int)block.posX;
                            int treeBlockY = grassLocalY + (int)block.posY;
                            int treeBlockZ = grassLocalZ + (int)block.posZ;

                            if (treeBlockX < chunckSize && treeBlockY < chunckSize & treeBlockZ < chunckSize)
                            {
                                chunckComponent.blocks[treeBlockX, treeBlockY, treeBlockZ] = block.blockId;
                            }
                        }

                        break;
                    }
                }
                
            }
        }

        
    }

    [Serializable]
    public struct TreeData
    {
        public string name;
        public TextAsset treeJson;
        public int spawnChance;
    }
}
