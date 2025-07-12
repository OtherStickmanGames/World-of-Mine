using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [field:SerializeField]
    public bool DontHideCursor { get; set; } = true;

    PlayerBehaviour playerBehaviour;
    StarterAssetsInputs starterAssetsInputs;

    [ReadOnlyField] public bool needUserGesture;

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

            if (Input.GetKeyDown(KeyCode.X))
            {
                UI.Single.controlSettingsView.NoFall_Clicked();
            }
            if (Input.GetKeyDown(KeyCode.J))
            {
                UI.Single.controlSettingsView.AutoJump_Clicked();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //ShowCursor();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (Cursor.lockState is CursorLockMode.None)
            {
                if (playerBehaviour)
                {
                    if (!UI.ClickOnUI())
                    {
                        //LeanTween.delayedCall(0.5f, HideCursor);
                        HideCursor();
                        //DelayedHideCursor();
                    }
                    else
                    {

#if UNITY_WEBGL

                        if (!Application.isMobilePlatform)
                        {
                            needUserGesture = true;
                            LockPlayerDigging();
                            print("*** нужен жест ***");
                            StartCoroutine(DelayedLockPlayerDigging());
                        }
#endif
                    }
                }
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

        //var deb = GameObject.FindGameObjectWithTag("TxtDebugo").GetComponent<TMPro.TMP_Text>();
        //deb.text = $"{Cursor.lockState}";

        QuickSlotSwitcher();

    }

    void QuickSlotSwitcher()
    {
        if (CameraStack.Instance.CurrentType is CameraStack.CameraType.Third)
        {
            if (Input.GetKey(KeyCode.LeftControl))
                return;
        }

        var currentSlot = UI.Single.quickInventoryView.Selected;
        // TO DO на один раз при старте
        var slotCount = UI.Single.quickInventoryView.SlotCount;
        var scrollValue = Mouse.current.scroll.y.ReadValue();
        if (scrollValue < 0f)
            currentSlot = (currentSlot + 1) % slotCount;
        else if (scrollValue > 0f)
            currentSlot = (currentSlot - 1 + slotCount) % slotCount;

        if (scrollValue != 0f)
        {
            OnSlotChanged(currentSlot);
        }
    }

    void OnSlotChanged(int value)
    {
        UI.Single.quickInventoryView.SelectSlot(value);
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

    public void DelayedHideCursor()
    {
        StartCoroutine(Delay());

        static IEnumerator Delay()
        {
            yield return null;

            HideCursor();
        }
    }

    static IEnumerator DelayedUnlockDigging()
    {
        yield return null;

        if (Single.playerBehaviour)
        {
            UnlockPlayerDigging();
            Single.needUserGesture = false;
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

    private void OnApplicationFocus(bool focus)
    {
        print($"фокус {focus}");
    }

    private void OnApplicationPause(bool pause)
    {
        print($"пауза {pause}");
    }

    public void OnEscapePressed()
    {
        ShowCursor();
    }
}
