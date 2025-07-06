using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using StarterAssets;

public class testosettings : MonoBehaviour
{
    [SerializeField] Button useSprint;
    [SerializeField] Slider autoJumpSpeed;
    [SerializeField] TMP_Text txtJumpSpeed;

    ThirdPersonController thirdPersonController;

    private void Start()
    {
        PlayerBehaviour.onMineSpawn.AddListener(Mine_Spawned);
        autoJumpSpeed.onValueChanged.AddListener(JumpSpeed_Changed);
        useSprint.onClick.AddListener(UseSprint_Clicked);
    }

    private void UseSprint_Clicked()
    {
        thirdPersonController.useSprint = !thirdPersonController.useSprint;
        UpdateBtnView();

    }

    private void JumpSpeed_Changed(float value)
    {
        txtJumpSpeed.SetText($"{value:F3}");
        thirdPersonController.autoJumpSpeed = value;
    }

    private void Mine_Spawned(MonoBehaviour player)
    {
        thirdPersonController = player.GetComponent<ThirdPersonController>();
        UpdateBtnView();
        txtJumpSpeed.SetText($"{autoJumpSpeed.value}");
        thirdPersonController.autoJumpSpeed = autoJumpSpeed.value;

    }

    void UpdateBtnView()
    {
        useSprint.image.color = thirdPersonController.useSprint ? Color.yellow : Color.white;
    }
}
