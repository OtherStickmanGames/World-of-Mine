using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NetcodeStatusView : MonoBehaviour
{
    [SerializeField] TMP_Text txtNetcodeStatus;

    internal void Init()
    {
        HideStatus();
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
}
