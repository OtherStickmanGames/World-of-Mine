using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour
{
    [SerializeField] CharacterAction characterAction;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            characterAction.Execute(transform);
        }
    }
}
