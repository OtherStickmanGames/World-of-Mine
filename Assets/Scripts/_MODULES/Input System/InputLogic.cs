using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputLogic : MonoBehaviour
{
    public static InputLogic Singleton;
    public bool AvailableMouseScrollWorld { get; set; }
    public bool AvailableMouseScrollUI { get; set; } = true;
    public bool AvailableMouseMoveWorld { get; set; } = true;

    private void Awake()
    {
        Singleton = this;
    }
}
