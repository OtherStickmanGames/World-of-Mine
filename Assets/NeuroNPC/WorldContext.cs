using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ChunckData;

public class WorldContext : MonoBehaviour
{
    public List<AssignedBuildingData> assignedBuildings;
    public Vector3 buildPoint;
}

[Serializable]
public class AssignedBuildingData
{
    public string name;
    public BuildingType buildingType;
    public UserChunckData blocksData;
    public DateTime assignedDate;
    public string guid;
    public string characterID;
    public float posX;
    public float posY;
    public float posZ;

    public void SetPos(Vector3 pos)
    {
        posX = pos.x;
        posY = pos.y;
        posZ = pos.z;
    }
}
