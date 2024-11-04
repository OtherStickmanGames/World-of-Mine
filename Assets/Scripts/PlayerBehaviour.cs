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
    [SerializeField] MeshRenderer[] meshRenderers;
    [SerializeField] public bool allowDigging;
    [SerializeField] int sizeMainInventory = 0;

    [ReadOnlyField] public Transform blockHighlight;

    public Character Character => player;
    public bool IsOwner { get; set; } = true;
    public bool MobileTestINput { get; set; } = false;

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

            SetLoadedPosition();

            onOwnerPositionSet?.Invoke(this);

            player.inventory.onTakeItem += Item_TakedUpdated;
            player.inventory.onUpdateItem += Item_TakedUpdated;

            InitSizeMainInventory();
            LoadInventory();
        }
    }

    public void SetLoadedPosition()
    {
        var userDataPosition = UserData.Owner.position;
        //print($"{UserData.Owner.userName} ### {UserData.Owner.position}");
        if (userDataPosition == Vector3.zero)
        {
            transform.position += Vector3.one + Vector3.up * 180;
            print($"soeiofsoefbiosebf");
        }
        else
        {
            WorldGenerator.Inst.GetChunk(userDataPosition.ToGlobalRoundBlockPos());
            transform.position = userDataPosition;// + (Vector3.up * 5);
            //print($"{transform.position} ===---+++");
        }
    }

    private void Item_TakedUpdated(Item item)
    {
        SaveInventory();
    }

    void InitSizeMainInventory()
    {
        player.inventory.mainSize = sizeMainInventory;
    }


    private void Update()
    {
        if (!IsOwner)
            return;

        deltaTime = Time.deltaTime;

        SavePlayerPosition();

        if (allowDigging && !Application.isMobilePlatform && !MobileTestINput && !UI.ClickOnUI())
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

        if (Input.GetKey(KeyCode.LeftControl))
        {
            thirdPersonController.NoFall = true;
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            thirdPersonController.NoFall = false;
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            FindObjectOfType<MobileInput>().NoFall_Clicked();
        }

        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.E))
        {
            if (player.inventory.IsOpen)
            {
                player.inventory.Close();
            }
            else
            {
                player.inventory.Open();
            }
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
                            //print("эсть не загруженные блоки");
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
            print("ЧАНКИ ЗАГРУЖЕНЫ");
        }
    }

    Vector3 targetPos;

    void BlockRaycast()
    {
        var camPos = Camera.main.transform.position;
        var dist = Vector3.Distance(camPos, transform.position);
        dist += player.MineDistance;
        if (Physics.Raycast(camPos, Camera.main.transform.forward, out RaycastHit hit, dist, layerMask))
        {
            blockHighlight.position = Vector3.zero;

            hit.normal = GetDominantDirection(hit.normal);

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

            PlaceBlock(blockPosition + hit.normal, hit.normal);


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

    void PlaceBlock(Vector3 blockPosition, Vector3 hitNormal)
    {
        if (Input.GetMouseButtonDown(1) && !UI.ClickOnUI())
        {
            //print($"{hitNormal} - hit normal");
            //print($"Dot right {Vector3.Dot(Vector3.right, hitNormal)}");
            //print($"Dot Left {Vector3.Dot(Vector3.left, hitNormal)}");

            Ray ray = CameraStack.Instance.Main.ScreenPointToRay(Input.mousePosition);
            // Получаем направление луча
            Vector3 rayDirection = ray.direction;

            rayDirection = GetDominantDirection(rayDirection) * -1f;

            // Округляем направление до ближайшего кратного 90 градусов значения (-1, 0, 1)
            Vector3 roundedDirection = new Vector3(
                Mathf.Round(rayDirection.x),
                Mathf.Round(rayDirection.y),
                Mathf.Round(rayDirection.z)
            );

            Quaternion rotation = Quaternion.LookRotation(roundedDirection);
            rotation.ToAngleAxis(out var angle, out var axoso);
            //Debug.Log("Ось вращения: " + axoso + ", Угол поворота: " + angle);

            RotationAxis zaebis = RotationAxis.Y;
            var turnBlockAngle = angle * axoso.y;
            if (!(Mathf.Abs(roundedDirection.z - 1f) < 0.001f))
            {
                if (Mathf.Abs(axoso.x) > 0)
                {
                    zaebis = RotationAxis.X;
                    turnBlockAngle = angle * axoso.x;
                }
            }
            //print($"Май ось вращенька {zaebis}");

            var axis = WorldGenerator.Inst.turnableBlocks[(byte)ItemID.STONE_WORKBENCH];

            bool axisXY = (axis & (RotationAxis.X | RotationAxis.Y)) == (RotationAxis.X | RotationAxis.Y);
            var chicko = WorldGenerator.Inst.GetChunk(blockPosition + Vector3.right);

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

                var isTurnableBlock = IsTurnableBlock(item.id);
                if (isTurnableBlock)
                {
                    var availableAxis = WorldGenerator.Inst.turnableBlocks[item.id];
                    if ((availableAxis & zaebis) == zaebis)
                    {
                        chunck.AddTurnBlock
                        (
                            new Vector3Int(xBlock, yBlock, zBlock),
                            (int)turnBlockAngle,
                            zaebis
                        );
                        print($"зашли и вроде как повернули {turnBlockAngle} ### {zaebis}");
                    }
                }

                var mesh = generator.UpdateMesh(chunck);//, (int)pos.x, (int)pos.y, (int)pos.z);
                chunck.meshFilter.mesh = mesh;
                chunck.collider.sharedMesh = mesh;

                var blockLocalPos = new Vector3(xBlock, yBlock, zBlock);

                for (int p = 0; p < 6; p++)
                {

                    Vector3 checkingBlockPos = blockLocalPos + World.faceChecks[p];
                    var blockInOtherChunckPos = checkingBlockPos + pos;

                    if (!IsBlockChunk((int)checkingBlockPos.x, (int)checkingBlockPos.y, (int)checkingBlockPos.z))
                    {
                        var otherChunck = generator.GetChunk(checkingBlockPos + pos);

                        var otherMesh = generator.UpdateMesh(otherChunck);
                        otherChunck.meshFilter.mesh = otherMesh;
                        otherChunck.collider.sharedMesh = otherMesh;
                    }
                }


                if (isTurnableBlock)
                {
                    WorldGenerator.Inst.PlaceTurnedBlock
                    (
                        blockPosition + Vector3.right,
                        item.id,
                        (int)turnBlockAngle,
                        zaebis
                    );
                }
                else
                {
                    WorldGenerator.Inst.PlaceBlock(blockPosition + Vector3.right, item.id);
                }

                player.inventory.Remove(item);
            }
        }
    }

    private bool IsTurnableBlock(byte blockID)
    {
        return WorldGenerator.Inst.turnableBlocks.ContainsKey(blockID);
    }

    private Vector3 GetDominantDirection(Vector3 direction)
    {
        // Сравниваем компоненты по их абсолютной величине
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y) && Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            return new Vector3(Mathf.Sign(direction.x), 0, 0); // Оставляем только X
        }
        else if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x) && Mathf.Abs(direction.y) > Mathf.Abs(direction.z))
        {
            return new Vector3(0, Mathf.Sign(direction.y), 0); // Оставляем только Y
        }
        else
        {
            return new Vector3(0, 0, Mathf.Sign(direction.z)); // Оставляем только Z
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
        foreach (var item in meshRenderers)
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
