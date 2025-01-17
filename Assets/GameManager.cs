using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] public string serverAdress = "176.123.167.245";
    [SerializeField] public string hostName = "worldofmine.online";
    [SerializeField] Worker workerPrefab;
    [SerializeField] public Transform workersParent;
    [SerializeField] bool autoSpawn;
    [SerializeField] int countWorkers = 50;
    public List<Character> players = new();

    [Header("Глобальные Настройки Рабочих")]
    public float JumpTopThresold = 0.8f;
    public float JumpLowThresold = 0.5f;
    public float JumpForce = 30;
    public float JumpAvailabelDist = 1f;
    public float maxDiffrentHeight = 10;

    public static GameManager Inst;

    List<IUpdateble> updatables = new();
    PlayerBehaviour playerOwner;

    private void Awake()
    {
        Inst = this;

        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        //UserData.Owner.LoadData();

        PlayerBehaviour.onMineSpawn.AddListener(PlayerOwner_Spawned);

        //FindPathSystem.Instance.onPathComplete += FindPathBetweenBlocks_Completed;
        Character.onSpawn.AddListener(PlayerAny_Spawned);
    }

    private void PlayerOwner_Spawned(MonoBehaviour owner)
    {
        playerOwner = owner as PlayerBehaviour;
    }

    private IEnumerator Start()
    {
        updatables.Add(new BlockItemSpawner());

        yield return new WaitForEndOfFrame();

#if !UNITY_SERVER

#if UNITY_WEBGL && YG_PLUGIN_YANDEX_GAME
        while (!YG.YandexGame.SDKEnabled)
        {
            yield return new WaitForEndOfFrame();
        }
#endif
        if (Application.isMobilePlatform)
        {
            LoadTutorialScene();
        }
#endif


    }

    private void LoadTutorialScene()
    {
        var tutorialSceneName = "Tutorial";
        if (!UserData.Owner.tutorialComplete && SceneManager.GetActiveScene().name != tutorialSceneName)
        {
            SceneManager.LoadScene(tutorialSceneName);
        }
    }

    private void Update()
    {
        foreach (var item in updatables) item.Update();

        //if (Input.GetKeyDown(
        //))
        //{
        //    var item = new Item() { id = 9 };
        //    item.view = BlockItemSpawner.CreateBlockGameObject(item.id);
        //    item.count = 80;
        //    playerOwner.GetComponent<Character>().inventory.TakeItem(item);

        //    item = new Item() { id = 10 };
        //    item.view = BlockItemSpawner.CreateBlockGameObject(item.id);
        //    item.count = 80;
        //    playerOwner.GetComponent<Character>().inventory.TakeItem(item);
        //}
    }

    private void PlayerAny_Spawned(Character player)
    {
        players.Add(player);
    }

    public static void SetLayerByChild(GameObject go, int layer)
    {
        if (go.transform.childCount == 0)
            return;

        foreach (Transform t in go.transform)
        {
            t.gameObject.layer = layer;
            SetLayerByChild(t.gameObject, layer);
        }

    }

    public static void CheckPathBetweenBlock(Vector3 start, Vector3 end)
    {
        FindPathSystem.Instance.Find(start, end);
    }

    private void FindPathBetweenBlocks_Completed(FindPathSystem.PathDataResult data)
    {
        if (!data.found)
        {
            World.Instance.notMineable.AddRange(data.explored);
        }
    }

    private void OnDestroy()
    {
        
    }
}
