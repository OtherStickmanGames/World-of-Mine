using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Config
{
    public SimulationConfig simulationConfig;
}

[Serializable]
public class SimulationConfig
{
    public List<SimulationBlockConfig> simulationBlockConfigs;
}

[Serializable]
public class SimulationBlockConfig
{
    public string name;
    public byte blockID;
    public float time;
    public float? maxTime; 
}
