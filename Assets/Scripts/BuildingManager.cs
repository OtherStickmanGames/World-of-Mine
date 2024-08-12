using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BuildingManager : MonoBehaviour
{
    [SerializeField] Transform highlightBlockPrefab;

    public Vector3 horizontalLeftTop;
    public Vector3 horizontalRightBottom;

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

            horizontalLeftTop = startPos;
            horizontalRightBottom = endPos;

            int minX = Mathf.FloorToInt(Mathf.Min(startPos.x, endPos.x));
            int maxX = Mathf.FloorToInt(Mathf.Max(startPos.x, endPos.x));
            int minZ = Mathf.FloorToInt(Mathf.Min(startPos.z, endPos.z));
            int maxZ = Mathf.FloorToInt(Mathf.Max(startPos.z, endPos.z));

            Vector3 pos;
            for (int x = minX + 1; x <= maxX - 1; x++)
            {
                for (int z = minZ + 1; z <= maxZ - 1; z++)
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
        Vector3 camPos;
        var width = horizontalRightBottom.x - horizontalLeftTop.x;

        var heights = highlights.Select(h => h.position.y).ToList();
        heights.Add(playerBehaviour.transform.position.y);
        var minY = heights.Min();
        var maxY = heights.Max();
        var height = maxY - minY;

        camPos.x = horizontalLeftTop.x + (width / 2);
        camPos.z = horizontalRightBottom.z - 1;
        camPos.y = minY + (height / 2);

        horizontalLeftTop.y = maxY + 1.8f;
        horizontalRightBottom.y = minY - 1.8f;
        horizontalRightBottom.x++;

        var zoomByHeight = height / 1.18f;
        var zoomByWidth = width / 3.9f;
        var zoom = Mathf.Max(zoomByWidth, zoomByHeight);
        print($"{zoomByHeight} ### {zoomByWidth}");
        zoom = Mathf.Clamp(zoom, 3.5f, 888);
        CameraStack.Instance.SaveBuildingCamSetZoom(zoom);
        CameraStack.Instance.SaveBuilding(SelectionMode.Vertical, camPos);
    }
}


