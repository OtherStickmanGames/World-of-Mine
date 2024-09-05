using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using StarterAssets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(108)]
public class PlayerBehaviour : MonoBehaviour
{
    [SerializeField] public Transform cameraTarget;
    [SerializeField] Transform blockHighlightPrefab;
    [SerializeField] LayerMask layerMask;
    [SerializeField] SkinnedMeshRenderer[] skinnedMeshRenderers;
    [SerializeField] public bool allowDigging;
    [SerializeField] int sizeMainInventory = 0;

    [ReadOnlyField] public Transform blockHighlight;

    public bool IsOwner { get; set; } = true;

    public static UnityEvent<MonoBehaviour> onMineSpawn = new UnityEvent<MonoBehaviour>();
    public static UnityEvent<MonoBehaviour> onOwnerPositionSet = new UnityEvent<MonoBehaviour>();

    ThirdPersonController thirdPersonController;
    Character player;
    float defaultBottomClamp;
    float defaultTopClamp;
    float deltaTime;

    private void Start()
    {
        blockHighlight = Instantiate(blockHighlightPrefab, Vector3.zero, Quaternion.identity);

        player = GetComponent<Character>();
        thirdPersonController = GetComponent<ThirdPersonController>();

        defaultBottomClamp = thirdPersonController.BottomClamp;
        defaultTopClamp = thirdPersonController.TopClamp;

        if (IsOwner)
        {
            onMineSpawn?.Invoke(this);
            EventsHolder.playerSpawnedMine?.Invoke(player);
            CameraStack.onCameraSwitch.AddListener(Camera_Switched);

            var sai = FindObjectOfType<StarterAssetsInputs>();
            var pi = FindObjectOfType<PlayerInput>();
            thirdPersonController.SetInput(sai, pi);

            var userDataPosition = UserData.Owner.position;
            print($"{UserData.Owner.userName} ### {UserData.Owner.position}");
            if (userDataPosition == Vector3.zero)
            {
                transform.position += Vector3.one + Vector3.up * 180;
                print($"soeiofsoefbiosebf");
            }
            else
            {
                WorldGenerator.Inst.GetChunk(userDataPosition.ToGlobalRoundBlockPos());
                transform.position = userDataPosition;// + (Vector3.up * 5);
            }

            onOwnerPositionSet?.Invoke(this);

            player.inventory.onTakeItem += Item_TakedUpdated;
            player.inventory.onUpdateItem += Item_TakedUpdated;

            InitSizeMainInventory();
            LoadInventory();
        }


        //FindPathSystem.Instance.onPathComplete += FindPath_Completed;
    }

    private void Item_TakedUpdated(Item item)
    {
        SaveInventory();
    }

    void InitSizeMainInventory()
    {
        player.inventory.mainSize = sizeMainInventory;
    }

    

    private void FindPath_Completed(FindPathSystem.PathDataResult data)
    {
        if (!data.found)
        {
            foreach (var item in data.explored)
            {
                WorldGenerator.Inst.SetBlockAndUpdateChunck(item, 66);
            }
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        deltaTime = Time.deltaTime;

        SavePlayerPosition();

        if (allowDigging && !Application.isMobilePlatform)
        {
            BlockRaycast();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            CameraStack.Instance.SwitchCamera();
        }

        //if (Input.GetKeyDown(KeyCode.X))
        //{
        //    var pos = transform.position + Vector3.right + Vector3.up + transform.forward;
        //    pos = pos.ToGlobalBlockPos();
        //    WorldGenerator.Inst.SetBlockAndUpdateChunck(pos, 8);
        //    targetPos = pos;
        //    print(pos);
        //    print(World.Instance.towerPos.position.ToGlobalBlockPos());
        //}

        if (Input.GetKeyDown(KeyCode.P))
        {
            //print(targetPos);
            FindPathSystem.Instance.Find(transform.position.ToGlobalBlockPos(), targetPos);
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            transform.position += Vector3.up * 80;
        }

        if (!thirdPersonController.AllowGravityLogic)
        {
            CheckChuncksLoadedBlocks();
        }
        
    }

    float ebalaTimer;
    private void CheckChuncksLoadedBlocks()
    {
        var pos = transform.position.ToGlobalRoundBlockPos();
        var viewDistance = 2;
        var size = WorldGenerator.size;
        for (float x = -viewDistance + pos.x; x < viewDistance + pos.x; x += size)
        {
            for (float y = -viewDistance + pos.y; y < viewDistance + pos.y; y += size)
            {
                for (float z = -viewDistance + pos.z; z < viewDistance + pos.z; z += size)
                {
                    var worldPos = new Vector3(x, y, z);
                    if (WorldGenerator.Inst.HasChunck(worldPos, out var key))
                    {
                        if (!WorldGenerator.Inst.GetChunk(key).blocksLoaded)
                        {
                            print("эсть не загруженные блоки");
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }
        
        ebalaTimer += Time.deltaTime;
        if (ebalaTimer > 1f)
        {
            thirdPersonController.AllowGravityLogic = true;
        }
    }

    Vector3 targetPos;

    void BlockRaycast()
    {
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, 8f, layerMask))
        {
            blockHighlight.position = Vector3.zero;

            Vector3 normalPos = hit.point - (hit.normal / 2);

            int x = Mathf.FloorToInt(normalPos.x);
            int y = Mathf.FloorToInt(normalPos.y);
            int z = Mathf.FloorToInt(normalPos.z);

            Vector3 blockPosition = new(x, y, z);

            blockHighlight.position = blockPosition;
            //blockHighlight.forward = Vector3.forward;

            if (Input.GetMouseButtonDown(0) && !UI.ClickOnUI())
            {
                WorldGenerator.Inst.MineBlock(blockPosition + Vector3.right);
            }

            PlaceBlock(blockPosition + hit.normal);


            //if (Input.GetMouseButtonDown(1))
            //{
            //    // зачем-то нужно прибавлять 1 по оси X, хз почему так, но именно так работает
            //    ref var chunck = ref Service<World>.Get().GetChunk(blockPosition + Vector3.right);
            //    var pos = chunck.renderer.transform.position;

            //    // зачем-то нужно прибавлять 1 по оси X, хз почему так, но именно так работает
            //    int xBlock = x - Mathf.FloorToInt(pos.x) + 1;
            //    int yBlock = y - Mathf.FloorToInt(pos.y);
            //    int zBlock = z - Mathf.FloorToInt(pos.z);
            //    byte hitBlockID = chunck.blocks[xBlock, yBlock, zBlock];

            //    if (hitBlockID == 100 || hitBlockID == 101 || hitBlockID == 102)
            //    {
            //        GlobalEvents.interactBlockHited.Invoke(hitBlockID, new(x + 1, y, z));
            //    }
            //    else
            //    {
            //        int idx = 0;
            //        foreach (var entity in filter)
            //        {
            //            if (idx == InputHandler.Instance.quickSlotID - 1)
            //            {
            //                var poolItems = ecsWorld.GetPool<InventoryItem>();
            //                ref var item = ref poolItems.Get(entity);

            //                if (item.itemType == ItemType.Block)
            //                {
            //                    var e = godcraft.EcsWorld.NewEntity();

            //                    var pool = godcraft.EcsWorld.GetPool<ChunckHitEvent>();
            //                    pool.Add(e);
            //                    ref var component = ref pool.Get(e);
            //                    component.collider = hit.collider;
            //                    component.position = blockPosition + hit.normal;
            //                    component.blockId = item.blockID;

            //                    onChunkHit?.Invoke(new Entity { id = e }, component);
            //                    GlobalEvents.onBlockPlaced?.Invoke(item.blockID, blockPosition + hit.normal);

            //                    // HOT FIX вынести в отдельную систему
            //                    item.count--;
            //                    if (item.count == 0)
            //                    {
            //                        Destroy(item.view);
            //                        ecsWorld.DelEntity(entity);
            //                    }

            //                    StartCoroutine(Delay());

            //                    //-----------------------------------
            //                }
            //                else
            //                {
            //                    ref var used = ref ecsWorld.GetPool<ItemUsed>().Add(entity);
            //                    used.entity = entity;
            //                    used.id = item.blockID;

            //                    StartCoroutine(Delay());
            //                }

            //                IEnumerator Delay()
            //                {
            //                    yield return null;

            //                    GlobalEvents.itemUsing?.Invoke(entity);
            //                }

            //                break;
            //            }
            //            idx++;
            //        }


            //    }
            //}
        }
        else
        {
            blockHighlight.position = default;
        }
    }

    void PlaceBlock(Vector3 blockPosition)
    {
        if (Input.GetMouseButtonDown(1) && !UI.ClickOnUI())
        {
            //print("kjdnsfjksdf");
            if (player.inventory.CurrentSelectedItem != null)
            {

                var item = player.inventory.CurrentSelectedItem;
                // зачем-то нужно прибавлять 1 по оси X, хз почему так, но именно так работает
                var generator = WorldGenerator.Inst;
                var chunck = generator.GetChunk(blockPosition + Vector3.right);
                var pos = chunck.renderer.transform.position;

                int xBlock = (int)(blockPosition.x - pos.x) + 1;
                int yBlock = (int)(blockPosition.y - pos.y);
                int zBlock = (int)(blockPosition.z - pos.z);
                // зачем-то нужно прибавлять 1 по оси X, хз почему так, но именно так работает
                byte hitBlockID = chunck.blocks[xBlock, yBlock, zBlock];

                chunck.blocks[xBlock, yBlock, zBlock] = item.id;

                var mesh = generator.UpdateMesh(chunck);//, (int)pos.x, (int)pos.y, (int)pos.z);
                chunck.meshFilter.mesh = mesh;
                chunck.collider.sharedMesh = mesh;

                for (int p = 0; p < 6; p++)
                {
                    var blockPos = new Vector3(xBlock, yBlock, zBlock);

                    Vector3 checkingBlockPos = blockPos + World.faceChecks[p];
                    var blockInOtherChunckPos = checkingBlockPos + pos;

                    if (!IsBlockChunk((int)checkingBlockPos.x, (int)checkingBlockPos.y, (int)checkingBlockPos.z))
                    {
                        var otherChunck = generator.GetChunk(checkingBlockPos + pos);

                        var otherMesh = generator.UpdateMesh(otherChunck);
                        otherChunck.meshFilter.mesh = otherMesh;
                        otherChunck.collider.sharedMesh = otherMesh;
                    }
                }

                WorldGenerator.Inst.PlaceBlock(blockPosition + Vector3.right, item.id);

                player.inventory.Remove(item);
            }
        }
    }

    bool IsBlockChunk(int x, int y, int z)
    {
        var size = WorldGenerator.size;
        if (x < 0 || x > size - 1 || y < 0 || y > size - 1 || z < 0 || z > size - 1)
            return false;
        else
            return true;
    }

    

    private void Camera_Switched(CameraStack.CameraType cameraType)
    {
        if(cameraType == CameraStack.CameraType.First)
        {
            SetVisibleMesh(false);
            thirdPersonController.BottomClamp = -80f;
            thirdPersonController.TopClamp = 87f;
        }
        else
        {
            SetVisibleMesh(true);
            thirdPersonController.BottomClamp = defaultBottomClamp;
            thirdPersonController.TopClamp = defaultTopClamp;
        }
    }

    private void SetVisibleMesh(bool value)
    {
        var shadowMode = value ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        foreach (var item in skinnedMeshRenderers)
        {
            item.shadowCastingMode = shadowMode;
        }
    }

    private void LateUpdate()
    {
        CheckPosition();
    }

    void CheckPosition()
    {
        if (!thirdPersonController.AllowGravityLogic)
            return;

        var blockablePos = transform.position + Vector3.up + Vector3.right;
        var blockID = WorldGenerator.Inst.GetBlockID(blockablePos);
        if (blockID > 0)
        {
            transform.position += Vector3.up;
        }
        if (transform.position.y < -888)
        {
            var pos = transform.position;
            pos.y = 300;
            transform.position = pos;
        }
    }

    float savePositionTimer;
    void SavePlayerPosition()
    {
        savePositionTimer += deltaTime;

        if (savePositionTimer < 1)
            return;

        savePositionTimer = 0;

        UserData.Owner.position = transform.position;
        UserData.Owner.SaveData();
    }

    void LoadInventory()
    {
        if (PlayerPrefs.HasKey("inventory"))
        {
            var json = PlayerPrefs.GetString("inventory");
            var jsonInventory = JsonConvert.DeserializeObject<JsonInventory>(json);
            jsonInventory.SetInventoryData(player.inventory);
            //print(jsonInventory);
            //print(jsonInventory.quick[0].count);
        }
    }

    void SaveInventory()
    {
        var jsonInventory = new JsonInventory(player.inventory);
        var json = JsonConvert.SerializeObject(jsonInventory);
        PlayerPrefs.SetString("inventory", json);
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        onMineSpawn.RemoveAllListeners();
        onOwnerPositionSet.RemoveAllListeners();
    }
}
