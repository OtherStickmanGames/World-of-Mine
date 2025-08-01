using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] public bool useDevServer;
    [SerializeField] public bool isLocalhost;
    [SerializeField] public string serverAdress = "176.123.167.245";
    [SerializeField] public string devServerAdress = "176.123.165.242";
    [SerializeField] public string hostName = "worldofmine.online";
    [SerializeField] public string yandexMetricCounter = "99583935";
    [SerializeField] Worker workerPrefab;
    [SerializeField] public Transform workersParent;
    [SerializeField] bool autoSpawn;
    [SerializeField] int countWorkers = 50;
    public List<Character> players = new();

    [Space]

    [SerializeField] public string tutorialSceneName = "Tutorial";

    [Header("���������� ��������� �������")]
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

#if !UNITY_SERVER
        if (!Application.isMobilePlatform)
        {
#if !UNITY_EDITOR
            QualitySettings.SetQualityLevel(1);
#endif
        }
#endif
    }

    private void PlayerOwner_Spawned(MonoBehaviour owner)
    {
        playerOwner = owner as PlayerBehaviour;
    }

    public UniversalRenderPipelineAsset withShadow;
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
        if (!Application.isMobilePlatform)
        {
            WorldGenerator.Inst.SetDistanceViewChunk(8);
        }

        //var blocksData = new FantasyTreeGenerator().GenerateTree();
        //var mesh = MeshGenerator.Single.GenerateMesh(blocksData);
        //var building = new GameObject($"���������� ���������");
        //var renderer = building.AddComponent<MeshRenderer>();
        //var meshFilter = building.AddComponent<MeshFilter>();
        //var collider = building.AddComponent<MeshCollider>();
        //renderer.material = WorldGenerator.Inst.mat;
        //meshFilter.mesh = mesh;
        //collider.sharedMesh = mesh;
    }

    private void LoadTutorialScene()
    {
        if (!IsTutorialScene())
        {
            if (!UserData.Owner.tutorialComplete && !UserData.Owner.tutorialSkiped)
            {
                SceneManager.LoadScene(tutorialSceneName);
            }
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

    bool? isTutorial = null;
    public static bool IsTutorialScene()
    {
        if (Inst.isTutorial == null)
        {
            Inst.isTutorial = SceneManager.GetActiveScene().name == Inst.tutorialSceneName;
        }

        return Inst.isTutorial.Value;
    }

    private void OnDestroy()
    {
        
    }
}
