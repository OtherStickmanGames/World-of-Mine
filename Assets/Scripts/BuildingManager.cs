using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

public class BuildingManager : MonoBehaviour
{
    [SerializeField] Material mat;
    [SerializeField] Transform highlightBlockPrefab;

    public Vector3 horizontalLeftTop;
    public Vector3 horizontalRightBottom;
    public Vector3 verticalLeftTop;
    public Vector3 verticalRightBottom;

    public UnityEvent onInputNameShow;

    public static BuildingManager Singleton;

    List<Transform> highlights = new List<Transform>();
    List<BlockData> blocksData = new List<BlockData>();
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

    public void SelectionVertical(Vector3 startPos, Vector3 endPos)
    {
        StartCoroutine(Async());

        IEnumerator Async()
        {
            yield return null;

            ClearHighlights();

            blocksData.Clear();

            startPos = startPos.ToGlobalBlockPos();
            endPos = endPos.ToGlobalBlockPos();

            int minX = Mathf.FloorToInt(Mathf.Min(startPos.x, endPos.x));
            int maxX = Mathf.FloorToInt(Mathf.Max(startPos.x, endPos.x));
            int minY = Mathf.FloorToInt(Mathf.Min(startPos.y, endPos.y));
            int maxY = Mathf.FloorToInt(Mathf.Max(startPos.y, endPos.y));
            int minZ = Mathf.FloorToInt(Mathf.Min(horizontalLeftTop.z, horizontalRightBottom.z));
            int maxZ = Mathf.FloorToInt(Mathf.Max(horizontalLeftTop.z, horizontalRightBottom.z));

            Vector3 pos;
            for (int x = minX + 1; x < maxX; x++)
            {
                for (int y = minY + 1; y < maxY; y++)
                {
                    for (int z = minZ + 1; z < maxZ; z++)
                    {
                        pos.x = x;
                        pos.y = y;
                        pos.z = z;

                        var blockID = WorldGenerator.Inst.GetBlockID(pos + Vector3.right);
                        if (blockID == 0)
                        {
                            continue;
                        }

                        BlockData blockData = new BlockData
                        {
                            pos = pos,
                            ID = blockID
                        };
                        blocksData.Add(blockData);

                        var highlight = Instantiate(highlightBlockPrefab, pos, Quaternion.identity);
                        highlights.Add(highlight);
                    }
                }

                yield return null;
            }
        }
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

    internal BuildPreviewData BuildPreview()
    {
        ClearHighlights();

        MeshGenerator.NormalizeBlocksPositions(blocksData);

        var mesh = MeshGenerator.Single.GenerateMesh(blocksData);
        var building = new GameObject($"TESTO MESH");
        var renderer = building.AddComponent<MeshRenderer>();
        var meshFilter = building.AddComponent<MeshFilter>();
        var collider = building.AddComponent<MeshCollider>();
        renderer.material = mat;
        meshFilter.mesh = mesh;
        collider.sharedMesh = mesh;

        BuildPreviewData data = new BuildPreviewData
        {
            view   = building,
            width  = blocksData.Max(b => b.pos.x) + 1,
            height = blocksData.Max(b => b.pos.y) + 1,
            length = blocksData.Max(b => b.pos.z) + 1
        };

        return data;
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
        CameraStack.Instance.SaveBuilding(AcceptMode.Horizontal, camPos);
    }

    public void SwitchSelectionAxis()
    {
        Vector3 camPos;
        var width = horizontalRightBottom.x - horizontalLeftTop.x;

        var heights = highlights.Select(h => h.position.y).ToList();
        //heights.Add(playerBehaviour.transform.position.y);
        var minY = heights.Min();
        var maxY = heights.Max();
        var height = maxY - minY;

        camPos.x = horizontalLeftTop.x + (width / 2) + 1;
        camPos.z = horizontalRightBottom.z - 1;
        camPos.y = minY + (height / 2);

        horizontalLeftTop.y = maxY + 1.8f;
        horizontalLeftTop.x += 0.3f;
        horizontalRightBottom.y = minY - 1f;
        horizontalRightBottom.x += 0.7f;

        var zoomByHeight = height / 1.0f;
        var zoomByWidth = width / 3.0f;
        var zoom = Mathf.Max(zoomByWidth, zoomByHeight);
        zoom = Mathf.Clamp(zoom, 3.5f, 888);
        CameraStack.Instance.SaveBuildingCamSetZoom(zoom);
        CameraStack.Instance.SaveBuilding(AcceptMode.Vertical, camPos);
    }

    public void InputNameBuilding_Showed()
    {
        onInputNameShow?.Invoke();
    }
}


public class BuildPreviewData
{
    public GameObject view;
    public float width;
    public float length;
    public float height;

    public void SetScale(Vector2 scale)
    {

    }

    public void ShiftPosition()
    {
        Vector3 localPos;
        localPos.x = -((width / 2) - 1) * view.transform.localScale.x;
        localPos.y = -(height / 2) * view.transform.localScale.y;
        localPos.z = -(length / 2) * view.transform.localScale.z;

        view.transform.localPosition = localPos;
    }
}
