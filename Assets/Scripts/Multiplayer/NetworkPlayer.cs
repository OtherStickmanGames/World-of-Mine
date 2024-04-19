using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerBehaviour))]
[DefaultExecutionOrder(103)]
public class NetworkPlayer : NetworkBehaviour
{
    PlayerBehaviour player;
    ThirdPersonController thirdPersonController;

    private void Awake()
    {
        player = GetComponent<PlayerBehaviour>();
        thirdPersonController = GetComponent<ThirdPersonController>();
    }

    private void Start()
    {
        player.IsOwner = IsOwner;
        thirdPersonController.IsOwner = IsOwner;

        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            print(IsOwner);
        }
    }
}
