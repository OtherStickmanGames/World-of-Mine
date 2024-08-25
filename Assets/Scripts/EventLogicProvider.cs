using System;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

public class EventLogicProvider : MonoBehaviour
{
    PlayerBehaviour playerOwner;
    ThirdPersonController ownerPersonController;

    private void Awake()
    {
        PlayerBehaviour.onMineSpawn.AddListener(PlayerOwner_Spawned);

    }

    private void Start()
    {
        BuildingManager.Singleton.onBuildingListShow.AddListener(BuildingList_Showed);
        BuildingManager.Singleton.onBuildingListHide.AddListener(BuildingList_Hideed);
    }

    private void BuildingList_Hideed()
    {
        ownerPersonController.LockCameraPosition = false;
    }

    private void BuildingList_Showed()
    {
        ownerPersonController.LockCameraPosition = true;
    }

    private void PlayerOwner_Spawned(MonoBehaviour owner)
    {
        playerOwner = owner as PlayerBehaviour;
        ownerPersonController = owner.GetComponent<ThirdPersonController>();
    }
}
