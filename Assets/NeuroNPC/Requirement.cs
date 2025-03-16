using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Requirement", menuName = "NPC/Requirement")]
public class Requirement : ScriptableObject
{
    public RequirementType requirementType;
}

public enum RequirementType
{
    Tavern,
    CollectResources,
    Patrol,
    RestoreBuilding,
}
