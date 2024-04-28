using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
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

    private void Awake()
    {
        Inst = this;

        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        UserData.Owner.LoadData();

        PlayerBehaviour.onMineSpawn.AddListener(PlayerOwner_Spawned);

        //FindPathSystem.Instance.onPathComplete += FindPathBetweenBlocks_Completed;
        Character.onSpawn.AddListener(PlayerAny_Spawned);
    }

    private void PlayerOwner_Spawned(MonoBehaviour owner)
    {
        
    }

    private IEnumerator Start()
    {
        updatables.Add(new BlockItemSpawner());

        if (autoSpawn)
        {
            for (int i = 0; i < countWorkers; i++)
            {
                yield return new WaitForSeconds(8);

                var worker = Instantiate(workerPrefab);
                worker.transform.SetParent(workersParent);
            }
        }

        
    }

    private void Update()
    {
        foreach (var item in updatables) item.Update();
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

    public bool ClickOnUI()
    {
        List<RaycastResult> results = new List<RaycastResult>();
        PointerEventData pointer = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition,
        };
        EventSystem.current.RaycastAll(pointer, results);

        foreach (var item in results)
        {
            if (item.gameObject.layer == 5)
                return true;
        }

        return false;
    }
}
