using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using System;

public class UI : MonoBehaviour
{
    [SerializeField] InventotyView inventotyView;
    [SerializeField] QuickInventoryView quickInventoryView;
    [SerializeField] Button btnSwitchCamera;

    public static UnityEvent onInventoryOpen = new UnityEvent();
    public static UnityEvent onInventoryClose = new UnityEvent();

    Player mine;


    private void Awake()
    {
        PlayerBehaviour.onMineSpawn.AddListener(PlayerMine_Spawned);
    }

    private void PlayerMine_Spawned(Player player)
    {
        mine = player;

        InitInventoryView(player);
    }

    private void InitInventoryView(Player player)
    {
        inventotyView.Init(player.inventory);
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
