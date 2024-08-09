using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [SerializeField] Transform highlightBlockPrefab;

    public static BuildingManager Singleton;


    List<Transform> highlights = new List<Transform>();

    PlayerBehaviour playerBehaviour;
    bool selectionStarted;

    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        Singleton = this;

        PlayerBehaviour.onMineSpawn.AddListener(OwnerPlayer_Spawned);
    }

    private void OwnerPlayer_Spawned(MonoBehaviour owner)
    {
        playerBehaviour = owner.GetComponent<PlayerBehaviour>();

    }

    private void Update()
    {
        if (!selectionStarted)
            return;

    }

    public void SelectionHorizontal(Vector3 startPos, Vector3 endPos)
    {
        StartCoroutine(Async());

        IEnumerator Async()
        {
            ClearHighlights();

            startPos = startPos.ToGlobalBlockPos();
            endPos = endPos.ToGlobalBlockPos();

            int minX = Mathf.FloorToInt(Mathf.Min(startPos.x, endPos.x));
            int maxX = Mathf.FloorToInt(Mathf.Max(startPos.x, endPos.x));
            int minZ = Mathf.FloorToInt(Mathf.Min(startPos.z, endPos.z));
            int maxZ = Mathf.FloorToInt(Mathf.Max(startPos.z, endPos.z));

            Vector3 pos;
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    pos.x = x;
                    pos.y = startPos.y;
                    pos.z = z;

                    while (WorldGenerator.Inst.GetBlockID(pos + Vector3.right) == 0)
                    {
                        pos.y--;
                    }

                    var highlight = Instantiate(highlightBlockPrefab, pos, Quaternion.identity);
                    highlights.Add(highlight);
                }

                yield return null;
            }
        }

    }

    public void ClearHighlights()
    {
        foreach (var item in highlights)
        {
            Destroy(item.gameObject);
        }

        highlights.Clear();
    }

    public void StartSelection()
    {
        selectionStarted = true;
        playerBehaviour.allowDigging = false;
        var camPos = playerBehaviour.transform.position + (playerBehaviour.transform.up * 88);
        CameraStack.Instance.SaveBuilding(SelectionMode.Horizontal, camPos);
    }

    public void SwitchSelection()
    {
        var camPos = playerBehaviour.transform.position - (playerBehaviour.transform.forward * 5);
        CameraStack.Instance.SaveBuilding(SelectionMode.Vertical, camPos);
    }
}


