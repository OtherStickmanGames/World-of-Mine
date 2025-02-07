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
using Unity.Netcode.Transports.UTP;

public class UI : MonoBehaviour
{
    [SerializeField] bool testMobileInput;
    [SerializeField] GameObject serverStatePanel;
    [SerializeField] Button btnServer;
    [SerializeField] Button btnClient;
    [SerializeField] Button btnPlay;
    [SerializeField] NetcodeStatusView netcodeStatusView;
    [SerializeField] InventotyView inventoryView;
    [SerializeField] QuickInventoryView quickInventoryView;
    [SerializeField] CraftView craftView;
    [SerializeField] SaveBuildingView saveBuildingView;
    [SerializeField] ShowBuildingView showBuildingView;
    [SerializeField] Button btnSwitchCamera;
    [SerializeField] UserView userView;
    [SerializeField] ChatView chatView;
    [SerializeField] GameObject mobileController;
    [SerializeField] FixedTouchField touchField;
    [SerializeField] MobileInput mobileInput;
    [SerializeField] RectTransform touch1;
    [SerializeField] RectTransform touch2;
    [SerializeField] TMP_Text txtEbala;
    [SerializeField] public TMP_Text txtPizdos;
    [SerializeField] Button btnInventory;
    [SerializeField] Button btnCrafting;
    [SerializeField] ReleaseNotesView releaseNotesView;
    [SerializeField] NicknamesView nicknames;
    [SerializeField] TMP_Text positionInfo;

    [Space]

    [SerializeField] GameObject InventoryParent;
    [SerializeField] GameObject ReleaseNotesParent;
    [SerializeField] GameObject hideShowCursorInfo;

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

        btnPlay.onClick.AddListener(BtnPlay_Clicked);

        btnSwitchCamera.gameObject.SetActive(false);
        btnInventory.gameObject.SetActive(false);
        btnSwitchCamera.onClick.AddListener(BtnSwitchCamera_Clicked);

        btnReset.onClick.AddListener(BtnReset_Clicked);
        btnInventory.onClick.AddListener(Inventory_Clicked);
        btnCrafting.onClick.AddListener(Crafting_Clicked);
        craftView.onClose.AddListener(Craft_Closed);

        btnDisableAll.onClick.AddListener(DisableAll_Clicked);

#if !UNITY_SERVER
        netcodeStatusView.Init();
        netcodeStatusView.onConnectToReserveClick.AddListener(ConnectToReserve_Clicked);

        PlayerBehaviour.onMineSpawn.AddListener(PlayerMine_Spawned);
        NetworkManager.Singleton.OnConnectionEvent += ConnectionEvent_Invoked;
#endif

        serverStatePanel.SetActive(false);
        NetworkManager.Singleton.OnServerStarted += SERVER_STARTED;
#if !UNITY_STANDALONE
        btnServer.gameObject.SetActive(false);

#endif
    }

    int countTryConnection = 0;
    private void ConnectionEvent_Invoked(NetworkManager networkManager, ConnectionEventData eventData)
    {
        print($"{eventData.EventType} =-= {countTryConnection} -=-=-");

        if (eventData.EventType == ConnectionEvent.ClientConnected)
        {
            countTryConnection = 0;
            netcodeStatusView.HideStatus();
        }

        if (eventData.EventType == ConnectionEvent.ClientDisconnected)
        {
            if (countTryConnection < 3)
            {
                netcodeStatusView.ShowStatus("Пробую соедениться с сервером..");
                countTryConnection++;
                StartCoroutine(DelayConnect());
            }
            else
            {
                netcodeStatusView.ShowStatus("Не удалось соедениться с основным сервером, видимо он упал, попробуй позже :(");
#if !UNITY_WEBGL
                netcodeStatusView.ShowBtnConnect();
#endif
                
            }
        }

        IEnumerator DelayConnect()
        {
            yield return new WaitForSeconds(0.1f + (0.8f * countTryConnection));

            NetworkManager.Singleton.StartClient();
        }
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

        releaseNotesView.Init();

        nicknames.Init();

        quickInventoryView.gameObject.SetActive(false);
        mobileController.SetActive(false);
        mobileInput.gameObject.SetActive(false);

        inventoryView.gameObject.SetActive(false);
        craftView.gameObject.SetActive(false);

        ReleaseNotesParent.SetActive(false);
        saveBuildingView.gameObject.SetActive(false);
        showBuildingView.gameObject.SetActive(false);

        hideShowCursorInfo.SetActive(false);
        chatView.gameObject.SetActive(true);

        chatView.Hide();

        SaveBuildingView.onSaveBuildingClick.AddListener(SaveBuilding_Clicked);
        SaveBuildingView.onBuildingSave.AddListener(Building_Saved);
        BuildingManager.Singleton.onBuildingListShow.AddListener(BuildingList_Showed);
        BuildingManager.Singleton.onBuildingListHide.AddListener(BuildingList_Hided);

        InitResolutionCurveFactor();

        InitStartMenu();

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

        chatView.Init("Host");
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
        //var networkManager = NetworkManager.Singleton;
        //UnityTransport transport = (UnityTransport)networkManager.NetworkConfig.NetworkTransport;
        //transport.UseWebSockets = true;
        //transport.UseEncryption = false;
        //transport.SetConnectionData("127.0.0.1", 7777); // Имя хоста и порт

        StartCoroutine(ASYNC_START());

        IEnumerator ASYNC_START()
        {
            yield return null;

            NetworkManager.Singleton.StartServer();

            chatView.Init("SERVER");
        }
    }

    private void START_CLIENT()
    {
        StartCoroutine(ASYNC_START());

        IEnumerator ASYNC_START()
        {
            yield return new WaitForSeconds(0.358f);

            NetworkManager.Singleton.StartClient();
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
            var damping = resolutionFactorCurve.Evaluate(Screen.height) * Time.deltaTime;
            var value = touchField.TouchDist * sensitivity * damping;
            lookDirection = Vector2.SmoothDamp(lookDirection, value, ref currentVelocity, damping * smoothTime);
            VirtualLookInput(lookDirection);
        }

        
        if (Input.GetKeyDown(KeyCode.G))
        {
            PrintCurrentUI();
        }

        SetCurrentUIObject();

        if (player)
        {
            positionInfo.gameObject.SetActive(true);
            positionInfo.SetText($"X:{player.position.x:F0} Y:{player.position.y:F0} Z:{player.position.z:F0}");
        }
        else
        {
            positionInfo.gameObject.SetActive(false);
        }
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
        //var networkManager = NetworkManager.Singleton;
        //var m_Transport = (UnityTransport)networkManager.NetworkConfig.NetworkTransport;
        ////m_Transport.SetServerSecrets(MyGameServerCertificate, MyGameServerPrivateKey);
        ////networkManager.StartServer();
        //m_Transport.SetClientSecrets("devworldofmine.online");

        //var networkManager = NetworkManager.Singleton;
        //var m_Transport = (UnityTransport)networkManager.NetworkConfig.NetworkTransport;
        ////m_Transport.SetServerSecrets(MyGameServerCertificate, MyGameServerPrivateKey);

        var networkManager = NetworkManager.Singleton;
        UnityTransport transport = (UnityTransport)networkManager.NetworkConfig.NetworkTransport;
        transport.UseWebSockets = true;

        var hostname = "worldofmine.online";
        if (Application.absoluteURL.IndexOf("draft=true") > 0)
        {
            hostname = "devworldofmine.online";
        }
#if !UNITY_WEBGL || UNITY_EDITOR
        if (GameManager.Inst.useDevServer)
        {
            transport.SetConnectionData(GameManager.Inst.devServerAdress, 443);

            hostname = "devworldofmine.online";
        }
        else
        {
            transport.SetConnectionData(GameManager.Inst.serverAdress, 443);
        }
#endif


#if UNITY_WEBGL && !UNITY_EDITOR
        transport.UseEncryption = true; // Для HTTPS соединений
        transport.SetConnectionData(hostname, 443); // Имя хоста и порт
#endif

        transport.SetClientSecrets(hostname);

        btnClient.gameObject.SetActive(false);
        netcodeStatusView.ShowStatus();
        NetworkManager.Singleton.StartClient();
    }

    private void ConnectToReserve_Clicked()
    {
        var networkManager = NetworkManager.Singleton;
        UnityTransport transport = (UnityTransport)networkManager.NetworkConfig.NetworkTransport;
        var hostname = "devworldofmine.online";
        transport.SetClientSecrets(hostname);
        transport.SetConnectionData(GameManager.Inst.devServerAdress, 443);

        netcodeStatusView.HideBtnConnect();
        netcodeStatusView.ShowStatus("Пробую подключиться к резервному сервер О_о");

        NetworkManager.Singleton.StartClient();
    }

    private void PlayerMine_Spawned(MonoBehaviour player)
    {
#if !UNITY_SERVER
        var playerBeh = player as PlayerBehaviour;

        btnPlay.gameObject.SetActive(true);

        mobileInput.Init(playerBeh);

        btnClient.gameObject.SetActive(false);
        btnServer.gameObject.SetActive(false);

        ReleaseNotesParent.SetActive(true);

        mine = player.GetComponent<Character>();
        
        InitInventoryView(mine);

        this.player = player.transform;

        playerBeh.MobileTestINput = testMobileInput;
        playerBeh.onBlockInteract.AddListener(PlayerBlock_Interacted);

        Character.onDisable.AddListener(Character_Destroyed);

        var thirdController = playerBeh.GetComponent<ThirdPersonController>();
        thirdController.AllowCameraRotation = false;
        InputLogic.Singleton.AvailableMouseScrollWorld = false;

#endif
    }

    private void Character_Destroyed(Character character)
    {
        if (character == mine)
        {
            quickInventoryView.ClearSlots();
            inventoryView.ClearSlotst();
        }
    }

    private void BtnPlay_Clicked()
    {
        InventoryParent.SetActive(true);
        ReleaseNotesParent.SetActive(false);
        btnPlay.gameObject.SetActive(false);
        mobileInput.gameObject.SetActive(true);

        btnSwitchCamera.gameObject.SetActive(true);
        btnInventory.gameObject.SetActive(true);

        saveBuildingView.gameObject.SetActive(true);
        showBuildingView.gameObject.SetActive(true);

        quickInventoryView.gameObject.SetActive(true);

        netcodeStatusView.HideStatus();

        if (Application.isMobilePlatform || testMobileInput)
        {
            mobileController.SetActive(true);
            touchField.gameObject.SetActive(true);
            hideShowCursorInfo.SetActive(false);
        }
        else
        {
            touchField.gameObject.SetActive(false);
            hideShowCursorInfo.SetActive(true);
        }

        var thirdController = mine.GetComponent<ThirdPersonController>();
        thirdController.AllowCameraRotation = true;
        InputLogic.Singleton.AvailableMouseScrollWorld = true;

        chatView.Init(UserData.Owner.userName);
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
        resolutionFactorCurve.AddKey(new(720, 1));
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

    /// <summary>
    /// Метод в зависимости от платформы настраивает видимость 
    /// кнопок меню на старте
    /// </summary>
    private void InitStartMenu()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        btnClient.gameObject.SetActive(true);
        btnPlay.gameObject.SetActive(false);
#endif
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
        if (!EventSystem.current)
        {
            return;
        }

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

    private void OnDestroy()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.OnConnectionEvent -= ConnectionEvent_Invoked;
        }
    }

    public static float ScaleFactor => 1080f / (float) Screen.height;
}
