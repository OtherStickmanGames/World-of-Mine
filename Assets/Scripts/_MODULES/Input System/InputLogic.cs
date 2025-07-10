using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Возиожно стоит переименовать в Input System
/// </summary>
public class InputLogic : MonoBehaviour
{
    public static InputLogic Single;
    public bool AvailableMouseScrollWorld { get; set; } = true;
    public bool AvailableMouseScrollUI { get; set; } = true;
    public bool AvailableMouseMoveWorld { get; set; } = true;
    public bool BlockPlayerControl { get; set; } = false;
    public bool DontHideCursor { get; set; } = false;

    PlayerBehaviour playerBehaviour;
    StarterAssetsInputs starterAssetsInputs;

    bool needUserGesture;

    private void Awake()
    {
        Single = this;

        PlayerBehaviour.onMineSpawn.AddListener(PlayerMine_Spawned);
        SaveBuildingView.onSaveBuildingClick.AddListener(SaveBuilding_Clicked);
        SaveBuildingView.onClose.AddListener(SaveBuilding_Closed);
    }

    private void Start()
    {
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
    }

    private void PlayerMine_Spawned(MonoBehaviour player)
    {
        playerBehaviour = player as PlayerBehaviour;
    }

    private void Update()
    {
        if (!BlockPlayerControl)
        {
            if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.E))
            {
                playerBehaviour?.OpenCloseInventory();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowCursor();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (Cursor.lockState is CursorLockMode.None)
            {
                if (playerBehaviour)
                {
                    if (!UI.ClickOnUI())
                    {
                        HideCursor();
                    }

#if UNITY_WEBGL

                    if (!Application.isMobilePlatform)
                    {
                        needUserGesture = true;
                        LockPlayerDigging();
                        print("*** нужен жест ***");
                        StartCoroutine(DelayedLockPlayerDigging());
                    }
                }
#endif
            }

        }

#if UNITY_WEBGL
        if (needUserGesture && !Application.isMobilePlatform)
        {
            if (Cursor.lockState is CursorLockMode.Locked)
            {
                if (Input.anyKeyDown)
                {
                    print("-= Уруруруур =-");
                    StartCoroutine(DelayedUnlockDigging());
                }
            }
        }
#endif
    }

    private void SaveBuilding_Clicked()
    {
        DontHideCursor = true;
    }

    private void SaveBuilding_Closed()
    {
        DontHideCursor = false;
    }

    public static void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Single.starterAssetsInputs.cursorInputForLook = false;
        Single.starterAssetsInputs.look = Vector2.zero;
        if (Single.playerBehaviour)
        {
            LockPlayerDigging();
        }
        print("----- Курсор Показан -----");
    }

    public static void HideCursor()
    {
        if (Single.DontHideCursor)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Single.starterAssetsInputs.cursorInputForLook = true;
        Single.starterAssetsInputs.look = Vector2.zero;

        if (!Single.needUserGesture)
        {
            Single.StartCoroutine(DelayedUnlockDigging());
        }

        print("======= Курсор Скрыт ========");
    }

    static IEnumerator DelayedUnlockDigging()
    {
        yield return null;

        if (Single.playerBehaviour)
        {
            UnlockPlayerDigging();
        }
    }

    static IEnumerator DelayedLockPlayerDigging()
    {
        if (Single.playerBehaviour)
        {
            yield return null;
            yield return null;
            yield return null;

            LockPlayerDigging();
        }
    }

    public static void LockPlayerDigging()
    {
        Single.playerBehaviour.allowDigging = false;
        print("=== заблокировал копат ===");
    }

    public static void UnlockPlayerDigging()
    {
        Single.playerBehaviour.allowDigging = true;
        print("+++ копат да +++");
    }
}
