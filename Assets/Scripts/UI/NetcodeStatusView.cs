using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class NetcodeStatusView : MonoBehaviour
{
    [SerializeField] TMP_Text txtNetcodeStatus;
    [SerializeField] Button btnConnectReserve;

    [HideInInspector] public UnityEvent onConnectToReserveClick;

    internal void Init()
    {
        HideStatus();
        HideBtnConnect();

        btnConnectReserve.onClick.AddListener(ConnectToReserve_Clicked);
    }

    private void ConnectToReserve_Clicked()
    {
        onConnectToReserveClick?.Invoke();
    }

    public void HideStatus()
    {
        txtNetcodeStatus.gameObject.SetActive(false);
    }

    public void ShowStatus()
    {
        txtNetcodeStatus.gameObject.SetActive(true);
    }

    public void ShowStatus(string status)
    {
        txtNetcodeStatus.SetText(status);
        ShowStatus();
    }

    public void HideBtnConnect()
    {
        btnConnectReserve.gameObject.SetActive(false);
    }

    public void ShowBtnConnect()
    {
        btnConnectReserve.gameObject.SetActive(true);
    }
}
