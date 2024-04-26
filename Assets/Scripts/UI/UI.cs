using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using System;

public class UI : MonoBehaviour
{
    [SerializeField] bool testMobileInput;
    [SerializeField] Button btnHost;
    [SerializeField] Button btnClient;
    [SerializeField] InventotyView inventotyView;
    [SerializeField] QuickInventoryView quickInventoryView;
    [SerializeField] Button btnSwitchCamera;
    [SerializeField] UserView userView;
    [SerializeField] GameObject mobileInput;

    [SerializeField] Button btnReset;

    public static UnityEvent onInventoryOpen = new UnityEvent();
    public static UnityEvent onInventoryClose = new UnityEvent();

    Character mine;
    Transform player;

    bool needResetPlayerPosition;

    private void Awake()
    {
        btnClient.onClick.AddListener(BtnClient_Clicked);
        btnHost.onClick.AddListener(BtnHost_Clicked);

        btnSwitchCamera.onClick.AddListener(BtnSwitchCamera_Clicked);

        btnReset.onClick.AddListener(BtnReset_Clicked);

        PlayerBehaviour.onMineSpawn.AddListener(PlayerMine_Spawned);
    }

    private void BtnReset_Clicked()
    {
        needResetPlayerPosition = true;
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

    private void Start()
    {
        userView.Init();

        quickInventoryView.gameObject.SetActive(false);
        mobileInput.SetActive(false);
        
    }

    private void BtnHost_Clicked()
    {
        NetworkManager.Singleton.StartHost();
    }

    private void BtnClient_Clicked()
    {
        NetworkManager.Singleton.StartClient();

    }

    private void PlayerMine_Spawned(MonoBehaviour player)
    {
        mine = player.GetComponent<Character>();
        quickInventoryView.gameObject.SetActive(true);
        InitInventoryView(mine);

        if (Application.isMobilePlatform || testMobileInput)
        {
            mobileInput.SetActive(true);
        }

        this.player = player.transform;
    }

    private void InitInventoryView(Character player)
    {
        if (inventotyView)
        {
            inventotyView.Init(player.inventory);
        }
        quickInventoryView.Init(player.inventory);

        onInventoryOpen.AddListener(player.inventory.Open);
        onInventoryClose.AddListener(player.inventory.Close);
    }

    private void LateUpdate()
    {
        if (needResetPlayerPosition)
        {
            var pos = player.position;
            pos.y = 180;
            player.position = pos;
            needResetPlayerPosition = false;
        }
    }

    public static void ClearParent(Transform parent)
    {
        foreach (Transform item in parent)
        {
            Destroy(item.gameObject);
        }
    }
}
