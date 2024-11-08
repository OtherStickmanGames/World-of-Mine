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

public class UI : MonoBehaviour
{
    [SerializeField] bool testMobileInput;
    [SerializeField] GameObject serverStatePanel;
    [SerializeField] Button btnServer;
    [SerializeField] Button btnClient;
    [SerializeField] InventotyView inventoryView;
    [SerializeField] QuickInventoryView quickInventoryView;
    [SerializeField] CraftView craftView;
    [SerializeField] SaveBuildingView saveBuildingView;
    [SerializeField] ShowBuildingView showBuildingView;
    [SerializeField] Button btnSwitchCamera;
    [SerializeField] UserView userView;
    [SerializeField] GameObject mobileController;
    [SerializeField] FixedTouchField touchField;
    [SerializeField] MobileInput mobileInput;
    [SerializeField] RectTransform touch1;
    [SerializeField] RectTransform touch2;
    [SerializeField] TMP_Text txtEbala;
    [SerializeField] public TMP_Text txtPizdos;
    [SerializeField] Button btnInventory;
    [SerializeField] Button btnCrafting;
    

    [Header("Output")]
    public StarterAssetsInputs starterAssetsInputs;

    [SerializeField] Button btnReset;

    [Header("DEV POEBOTA")]
    [SerializeField] Button btnDisableAll;

    public static UnityEvent onInventoryOpen = new UnityEvent();
    public static UnityEvent onInventoryClose = new UnityEvent();

    Character mine;
    Transform player;
    AnimationCurve resolutionFactorCurve;

    bool needResetPlayerPosition;

    private void Awake()
    {
        btnClient.onClick.AddListener(BtnClient_Clicked);
        btnServer.onClick.AddListener(BtnServer_Clicked);

        btnSwitchCamera.gameObject.SetActive(false);
        btnInventory.gameObject.SetActive(false);
        btnSwitchCamera.onClick.AddListener(BtnSwitchCamera_Clicked);

        btnReset.onClick.AddListener(BtnReset_Clicked);
        btnInventory.onClick.AddListener(Inventory_Clicked);
        btnCrafting.onClick.AddListener(Crafting_Clicked);
        craftView.onClose.AddListener(Craft_Closed);

        btnDisableAll.onClick.AddListener(DisableAll_Clicked);

#if !UNITY_SERVER
        PlayerBehaviour.onMineSpawn.AddListener(PlayerMine_Spawned);
        PlayerBehaviour.onBlockInteract.AddListener(PlayerBlock_Interacted);
#endif

        serverStatePanel.SetActive(false);
        NetworkManager.Singleton.OnServerStarted += SERVER_STARTED;
#if !UNITY_STANDALONE
        btnServer.gameObject.SetActive(false);

#endif
    }

    private void PlayerBlock_Interacted(byte blockID)
    {
        mine.inventory.Open();
        btnCrafting.gameObject.SetActive(false);
        craftView.Show(blockID);
    }

    private void DisableAll_Clicked()
    {
        if (quickInventoryView.gameObject.activeInHierarchy)
        {
            quickInventoryView.gameObject.SetActive(false);
            btnInventory.gameObject.SetActive(false);
        }
        else
        {
            quickInventoryView.gameObject.SetActive(true);
            btnInventory.gameObject.SetActive(true);
        }
    }

    private void Start()
    {
#if UNITY_SERVER
        START_SERVER();
#else
        userView.Init();
        saveBuildingView.Init();
        showBuildingView.Init();

        quickInventoryView.gameObject.SetActive(false);
        mobileController.SetActive(false);
        mobileInput.gameObject.SetActive(false);

        inventoryView.gameObject.SetActive(false);
        craftView.gameObject.SetActive(false);

        SaveBuildingView.onSaveBuildingClick.AddListener(SaveBuilding_Clicked);
        SaveBuildingView.onBuildingSave.AddListener(Building_Saved);
        BuildingManager.Singleton.onBuildingListShow.AddListener(BuildingList_Showed);
        BuildingManager.Singleton.onBuildingListHide.AddListener(BuildingList_Hided);

        InitResolutionCurveFactor();

        txtEbala.text = $"{UserData.Owner.position}";
#endif
    }

    private void Craft_Closed()
    {
        btnCrafting.gameObject.SetActive(true);
    }

    private void Inventory_Clicked()
    {
        if (mine.inventory.IsOpen)
        {
            mine.inventory.Close();
        }
        else
        {
            mine.inventory.Open();
        }
    }

    private void Crafting_Clicked()
    {
        btnCrafting.gameObject.SetActive(false);
        craftView.Show(0);
    }

    private void SERVER_STARTED()
    {
        Debug.Log($"-= SERVER STARTED =-");
        serverStatePanel.SetActive(true);
    }

    private void BtnServer_Clicked()
    {
        NetworkManager.Singleton.StartServer();
    }

    private void BuildingList_Showed()
    {
        quickInventoryView.gameObject.SetActive(false);
        btnInventory.gameObject.SetActive(false);
    }

    private void BuildingList_Hided()
    {
        quickInventoryView.gameObject.SetActive(true);
        btnInventory.gameObject.SetActive(true);
    }

    private void START_SERVER()
    {
        StartCoroutine(ASYNC_START());

        IEnumerator ASYNC_START()
        {
            yield return new WaitForEndOfFrame();

            NetworkManager.Singleton.StartServer();
        }
    }

    private void Building_Saved()
    {
        mobileController.SetActive(true);
    }

    private void SaveBuilding_Clicked()
    {
        mobileController.SetActive(false);
    }

    private void BtnReset_Clicked()
    {
        needResetPlayerPosition = true;
    }

    private void BtnSwitchCamera_Clicked()
    {
        CameraStack.Instance.SwitchCamera();
    }

    Vector2 lookDirection;
    Vector2 currentVelocity;
    public float smoothTime = 1f;
    public float sensitivity = 3f;
    private void Update()
    {
        if (Application.isMobilePlatform || testMobileInput)
        {
            var value = touchField.TouchDist * sensitivity * resolutionFactorCurve.Evaluate(Screen.height);
            lookDirection = Vector2.SmoothDamp(lookDirection, value, ref currentVelocity, Time.deltaTime * smoothTime);
            VirtualLookInput(lookDirection);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            NetworkManager.Singleton.StartServer();
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            PrintCurrentUI();
        }

        SetCurrentUIObject();
    }

    void TouchUpdate()
    {
        touch1.position = Vector3.down * 100;
        touch2.position = Vector3.down * 100;
        var mospos = Input.mousePosition;

        var offsetX = Screen.width / 2;
        var offsetY = Screen.height / 2;
        //touch1.position = new Vector3(mospos.x - offsetX, mospos.y - offsetY, 300);

        if (Input.touches.Length > 0)
        {
            touch1.position = Input.touches[0].position - new Vector2(offsetX, offsetY);
            touch1.position += (Vector3.forward * 300);

            if (Input.touches.Length > 1)
            {
                touch2.position = Input.touches[1].position - new Vector2(offsetX, offsetY);
                touch2.position += (Vector3.forward * 300);
            }
        }

        txtEbala.text = $"{touch1.position}";
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
#if !UNITY_SERVER
        mobileInput.gameObject.SetActive(true);
        mobileInput.Init(player as PlayerBehaviour);

        btnClient.gameObject.SetActive(false);
        btnServer.gameObject.SetActive(false);

        btnSwitchCamera.gameObject.SetActive(true);
        btnInventory.gameObject.SetActive(true);

        mine = player.GetComponent<Character>();
        quickInventoryView.gameObject.SetActive(true);
        InitInventoryView(mine);

        if (Application.isMobilePlatform || testMobileInput)
        {
            mobileController.SetActive(true);
            touchField.gameObject.SetActive(true);
        }
        else
        {
            touchField.gameObject.SetActive(false);
        }

        this.player = player.transform;

        (player as PlayerBehaviour).MobileTestINput = testMobileInput;
#endif
    }

    private void InitInventoryView(Character player)
    {
        if (inventoryView)
        {
            inventoryView.Init(player.inventory);
        }
        quickInventoryView.Init(player.inventory);

        onInventoryOpen.AddListener(player.inventory.Open);
        onInventoryClose.AddListener(player.inventory.Close);

        craftView.Init(mine);
    }


    private void InitResolutionCurveFactor()
    {
        resolutionFactorCurve = new();
        resolutionFactorCurve.AddKey(new(720, 15));
        resolutionFactorCurve.AddKey(new(1080, 1));
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

    public void VirtualLookInput(Vector2 virtualLookDirection)
    {
        starterAssetsInputs.LookInput(virtualLookDirection);
    }

    static List<RaycastResult> results = new List<RaycastResult>();
    static PointerEventData pointer;
    public static bool ClickOnUI()
    {
        if (pointer == null)
        {
            pointer = new PointerEventData(EventSystem.current);
        }
        pointer.position = Input.mousePosition;

        EventSystem.current.RaycastAll(pointer, results);

        foreach (var item in results)
        {
            if (item.gameObject.layer == 5)
                return true;
        }

        return false;
    }

    public static bool ClickOnUI(List<GameObject> exclude)
    {
        if (pointer == null)
        {
            pointer = new PointerEventData(EventSystem.current);
        }
        pointer.position = Input.mousePosition;

        EventSystem.current.RaycastAll(pointer, results);

        foreach (var item in results)
        {
            if (exclude.Find(go => go == item.gameObject))
                continue;

            if (item.gameObject.layer == 5)
                return true;
        }

        return false;
    }

    public static void PrintCurrentUI()
    {
        if (pointer == null)
        {
            pointer = new PointerEventData(EventSystem.current);
        }
        pointer.position = Input.mousePosition;

        EventSystem.current.RaycastAll(pointer, results);
        if (results.Count > 0)
        {
            print(results[0].gameObject);
        }
        else
        {
            print("Там ничего неееет");
        }
    }

    public static GameObject CurrentUIObject { get; private set; }
    private static void SetCurrentUIObject()
    {
        if (pointer == null)
        {
            pointer = new PointerEventData(EventSystem.current);
        }
        pointer.position = Input.mousePosition;

        EventSystem.current.RaycastAll(pointer, results);

        if (results.Count > 0)
        {
            CurrentUIObject = results[0].gameObject;
        }
        else
        {
            CurrentUIObject = null;
        }
    }

    public static float ScaleFactor => 1080f / (float) Screen.height;
}
