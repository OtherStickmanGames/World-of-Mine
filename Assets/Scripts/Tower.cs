using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower
{
    public int countResources;
    public GameObject trigger;

    public static Tower Instance;

    public Tower(GameObject trigger)
    {
        Instance = this;

        this.trigger = trigger;
    }
}
