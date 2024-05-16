using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using System;
using StarterAssets;
using TMPro;
using UnityEngine.EventSystems;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] QuickInventoryView quickInventoryView;
    [SerializeField] Button btnSwitchCamera;
    [SerializeField] FixedTouchField touchField;
    [SerializeField] MobileInput mobileInput;
    [SerializeField] float smoothTime = 1f;
    [SerializeField] float sensitivity = 3f;

    [Header("Output")]
    [SerializeField] StarterAssetsInputs starterAssetsInputs;
    [SerializeField] GameObject mobileController;

    Character mine;
    Transform player;
    Vector2 lookDirection;
    Vector2 currentVelocity;
    

    bool needResetPlayerPosition;

    private void Awake()
    {
       
        btnSwitchCamera.gameObject.SetActive(false);
        btnSwitchCamera.onClick.AddListener(BtnSwitchCamera_Clicked);


        PlayerBehaviour.onMineSpawn.AddListener(PlayerMine_Spawned);
    }

    private void Start()
    {
        quickInventoryView.gameObject.SetActive(false);
        mobileController.SetActive(false);
        mobileInput.gameObject.SetActive(false);
    }

    
    private void Update()
    {
        if (Application.isMobilePlatform)
        {
            var value = touchField.TouchDist * sensitivity * UI.ScaleFactor;
            lookDirection = Vector2.SmoothDamp(lookDirection, value, ref currentVelocity, Time.deltaTime * smoothTime);
            VirtualLookInput(lookDirection);
        }
    }

    public void VirtualLookInput(Vector2 virtualLookDirection)
    {
        starterAssetsInputs.LookInput(virtualLookDirection);
    }

    private void BtnSwitchCamera_Clicked()
    {
        var curType = CameraStack.Instance.CurrentType;
        if (curType == CameraStack.CameraType.First)
        {
            CameraStack.Instance.SwitchToThirdPerson();
        }
        else if (curType == CameraStack.CameraType.TopDown)
        {
            CameraStack.Instance.SwitchToFirstPerson();
        }
        else if (curType == CameraStack.CameraType.Third)
        {
            CameraStack.Instance.SwitchToTopDown();
        }
    }

    private void PlayerMine_Spawned(MonoBehaviour player)
    {
        mobileInput.gameObject.SetActive(true);
        mobileInput.Init(player as PlayerBehaviour);

        btnSwitchCamera.gameObject.SetActive(true);

        mine = player.GetComponent<Character>();
        quickInventoryView.gameObject.SetActive(true);
        InitInventoryView(mine);

        if (Application.isMobilePlatform)
        {
            mobileController.SetActive(true);
            touchField.gameObject.SetActive(true);
        }
        else
        {
            touchField.gameObject.SetActive(false);
        }

        this.player = player.transform;
    }

    private void InitInventoryView(Character player)
    {
        //if (inventoryView)
        //{
        //    inventoryView.Init(player.inventory);
        //}
        quickInventoryView.Init(player.inventory);

        //onInventoryOpen.AddListener(player.inventory.Open);
        //onInventoryClose.AddListener(player.inventory.Close);
    }
}
