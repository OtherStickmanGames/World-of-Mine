using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Needs Card", menuName = "NPC/Needs Card")]
public class NeedsCard : ScriptableObject
{
    public string title;
    public ProfType profType;
    public List<Requirement> requirements;
}

public enum ProfType
{
    None,
    GuardianOfCreation,
}
