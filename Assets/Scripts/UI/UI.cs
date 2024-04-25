using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using System;

public class UI : MonoBehaviour
{
    [SerializeField] Button btnHost;
    [SerializeField] Button btnClient;
    [SerializeField] InventotyView inventotyView;
    [SerializeField] QuickInventoryView quickInventoryView;
    [SerializeField] Button btnSwitchCamera;
    [SerializeField] UserView userView;

    public static UnityEvent onInventoryOpen = new UnityEvent();
    public static UnityEvent onInventoryClose = new UnityEvent();

    Character mine;


    private void Awake()
    {
        btnClient.onClick.AddListener(BtnClient_Clicked);
        btnHost.onClick.AddListener(BtnHost_Clicked);

        PlayerBehaviour.onMineSpawn.AddListener(PlayerMine_Spawned);
    }

    private void Start()
    {
        userView.Init();

        quickInventoryView.gameObject.SetActive(false);
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

    public static void ClearParent(Transform parent)
    {
        foreach (Transform item in parent)
        {
            Destroy(item.gameObject);
        }
    }
}
