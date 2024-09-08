using System;
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
        thirdPersonController.AllowGravityLogic = false;

        Client_Started();
    }

    Coroutine delay;

    private void Client_Started()
    {
        // КОСТЫЛИЩЕ, я задолбался разбираться, почему перс иногда спавнится на старте

        delay = StartCoroutine(Async());

        IEnumerator Async()
        {
            yield return null;

            player.SetLoadedPosition();
            delay = null; 
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if(delay != null)
        {
            StopCoroutine(delay);
        }
    }
}
