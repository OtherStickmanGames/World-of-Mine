using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using StarterAssets;
using System;

public class ControlSettingsView : MonoBehaviour
{
    [SerializeField] Button btnNoFall;
    [SerializeField] GameObject noFallOutline;
    [SerializeField] TMP_Text noFallTitle;
    [SerializeField] Color noFallActiveColor;

    [SerializeField] Button btnAutoJump;
    [SerializeField] GameObject autoJumpOutline;
    [SerializeField] TMP_Text autoJumpTitle;


    ThirdPersonController thirdPersonController;

    public void Init(PlayerBehaviour player)
    {
        thirdPersonController = player.GetComponent<ThirdPersonController>();


        if (btnNoFall)
        {
            btnNoFall.onClick.RemoveAllListeners();
            btnNoFall.onClick.AddListener(NoFall_Clicked);
            UpdateBtnNoFallView();
        }

        btnAutoJump.onClick.AddListener(AutoJump_Clicked);
        UpdateBtnAutoJumpView();
    }

    public void AutoJump_Clicked()
    {
        thirdPersonController.AutoJump = !thirdPersonController.AutoJump;
        UpdateBtnAutoJumpView();
    }

    public void NoFall_Clicked()
    {
        thirdPersonController.NoFall = !thirdPersonController.NoFall;
        UpdateBtnNoFallView();
    }

    private void UpdateBtnNoFallView()
    {
        if (thirdPersonController.NoFall)
        {
            noFallOutline.SetActive(true);
            noFallTitle.color = noFallActiveColor;
        }
        else
        {
            noFallOutline.SetActive(false);
            noFallTitle.color = Color.white;
        }
    }

    private void UpdateBtnAutoJumpView()
    {
        if (thirdPersonController.AutoJump)
        {
            autoJumpOutline.SetActive(true);
            autoJumpTitle.color = noFallActiveColor;
        }
        else
        {
            autoJumpOutline.SetActive(false);
            autoJumpTitle.color = Color.white;
        }
    }

}
