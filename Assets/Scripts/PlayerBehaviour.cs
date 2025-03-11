using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using StarterAssets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using static BLOCKS;

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
    public static UnityEvent<MonoBehaviour> onAnyPlayerSpawn = new();
    public UnityEvent<byte> onBlockInteract = new UnityEvent<byte>();

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

        onAnyPlayerSpawn?.Invoke(this);

        if (IsOwner)
        {
            onMineSpawn?.Invoke(this);
            
            WorldGenerator.Inst.AddPlayer(transform);
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

            if (player.inventory.mainSize == 0)// Сначала инвентаря не было, поэтому тем кто был без него надо его увеличить 
            {
                player.inventory.SetMainInventorySize(sizeMainInventory);
            }
        }
    }

    public void SetLoadedPosition()
    {
        var userDataPosition = UserData.Owner.position;
        //print($"{UserData.Owner.userName} ### {UserData.Owner.position}");
        if (userDataPosition == Vector3.zero)
        {
            transform.position += Vector3.one + Vector3.up * 38;
            print($"Загружена дефолтная позиция");
#if UNITY_ANDROID
            transform.position += Vector3.right * 888;
#endif
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

    float ebalaTimer, kostylTimer;
    private void CheckChuncksLoadedBlocks()
    {
        kostylTimer += Time.deltaTime;
        if(kostylTimer > 18)
        {
            thirdPersonController.AllowGravityLogic = true;
            print("СРАБОТАЛ КОСТЫЛЬ");
        }

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
                        if (!WorldGenerator.Inst.chuncks[key].blocksLoaded)
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
        if (ebalaTimer > 3.0f)
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

            hit.normal = VectorTools.GetDominantDirection(hit.normal);

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

            //print($"Ща блок: {WorldGenerator.Inst.GetBlockID(transform.position + (Vector3.down * 0.5f) + Vector3.right)}");
            var lookBlockID = WorldGenerator.Inst.GetBlockID(blockPosition + Vector3.right);
            if (ItemsStorage.Singleton.HasCraftingBundle(lookBlockID))
            {
                if (Input.GetMouseButtonDown(1) && !UI.ClickOnUI())
                {
                    onBlockInteract?.Invoke(lookBlockID);
                }
            }
            else
            {
                PlaceBlock(blockPosition + hit.normal, hit.normal);
            }
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
            //print(VectorTools.GetRoundedVector(rayDirection));
            // 1,1,0 и -1,1,0 направления поворота

            rayDirection = VectorTools.GetDominantDirection(rayDirection) * -1f;

            // Округляем направление до ближайшего кратного 90 градусов значения (-1, 0, 1)
            Vector3 roundedDirection = new Vector3(
                Mathf.Round(rayDirection.x),
                Mathf.Round(rayDirection.y),
                Mathf.Round(rayDirection.z)
            );
            roundedDirection.x *= -1f;

            //Quaternion rotation = Quaternion.LookRotation(roundedDirection);
            //rotation.ToAngleAxis(out var angle, out var axoso);
            ////Debug.Log("Ось вращения: " + axoso + ", Угол поворота: " + angle);

            //RotationAxis zaebis = RotationAxis.Y;
            //var turnBlockAngle = angle * axoso.y;
            //if (!(Mathf.Abs(roundedDirection.z - 1f) < 0.001f))
            //{
            //    if (Mathf.Abs(axoso.x) > 0)
            //    {
            //        zaebis = RotationAxis.X;
            //        turnBlockAngle = angle * axoso.x;
            //    }
            //}
            //print($"Май ось вращенька {zaebis}");

            var axis = WorldGenerator.Inst.turnableBlocks[(byte)ItemID.STONE_WORKBENCH];

            bool axisXY = (axis & (RotationAxis.X | RotationAxis.Y)) == (RotationAxis.X | RotationAxis.Y);
            
            if (player.inventory.CurrentSelectedItem != null)
            {
                var worldBlockPos = blockPosition + Vector3.right;

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

                List<TurnBlockData> turnsData = new List<TurnBlockData>();
                var isTurnableBlock = IsTurnableBlock(item.id);
                if (isTurnableBlock)
                {
                    turnsData = TurnBlockCalculation(item.id, chunck, new Vector3Int(xBlock, yBlock, zBlock));

                    //var availableAxis = WorldGenerator.Inst.turnableBlocks[item.id];
                    //if ((availableAxis & zaebis) == zaebis)
                    //{
                    //    chunck.AddTurnBlock
                    //    (
                    //        new Vector3Int(xBlock, yBlock, zBlock),
                    //        (int)turnBlockAngle,
                    //        zaebis
                    //    );
                    //    print($"зашли и вроде как повернули {turnBlockAngle} ### {zaebis}");
                    //}
                }

                var mesh = generator.UpdateMesh(chunck);//, (int)pos.x, (int)pos.y, (int)pos.z);
                chunck.meshFilter.mesh = mesh;
                //chunck.collider.sharedMesh = mesh;

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
                        //otherChunck.collider.sharedMesh = otherMesh;
                    }
                }

                if (isTurnableBlock)
                {
                    WorldGenerator.Inst.PlaceTurnedBlock
                    (
                        worldBlockPos,
                        item.id,
                        turnsData.ToArray()
                    );
                }
                else
                {
                    WorldGenerator.Inst.PlaceBlock(worldBlockPos, item.id);
                }

                WorldSimulation.Single.PlaceBlock(chunck, worldBlockPos, item.id);

                player.inventory.Remove(item);
            }
        }
    }
    

    public bool IsTurnableBlock(byte blockID)
    {
        return WorldGenerator.Inst.turnableBlocks.ContainsKey(blockID);
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

    public List<TurnBlockData> TurnBlockCalculation(byte blockID, ChunckComponent chunk, Vector3Int blockLocalPos)
    {
        Ray ray = CameraStack.Instance.Main.ScreenPointToRay(Input.mousePosition);
        Vector3 rayDirection = ray.direction;
        var roundedDir = VectorTools.GetRoundedVector(rayDirection);
        // 1,1,0 и -1,1,0 направления поворота
        //print(chunk.turnedBlocks.ContainsKey(blockLocalPos));
        //Quaternion roto = Quaternion.LookRotation(roundedDir);
        //roto.ToAngleAxis(out var agle, out var os);
        //Debug.Log($"{roundedDir}:: Ось вращения: " + os + ", Угол поворота: " + agle);

        var availableAxis = WorldGenerator.Inst.turnableBlocks[blockID];
        var dominantDirection = VectorTools.GetDominantDirection(rayDirection) * -1f;
        var roundedDominant = VectorTools.GetRoundedVector(dominantDirection);
        Debug.Log($"Зырь {roundedDir} &&& {roundedDominant}");
        
        TurnBlockData turnData = default;
        List<TurnBlockData> turns = new List<TurnBlockData>();

        var isXYTurn = (availableAxis & (RotationAxis.X | RotationAxis.Y)) == (RotationAxis.X | RotationAxis.Y);

        if (roundedDir.x > 0 && roundedDir.y > 0)
        {
            if (roundedDominant.x < 0 || roundedDominant.y < 0)
            {
                Debug.Log($"1: Смотрим вверх-вправо");
                if (isXYTurn)
                {
                    turnData.axis = RotationAxis.X;
                    turnData.angle = 90;
                    turns.Add(turnData);
                }

                turnData.axis = RotationAxis.Y;
                turns.Add(turnData);

                //Debug.Log($"Повернул {turnData.axis} :: {turnData.angle}");
            }
        }
        else
        if (roundedDir.x < 0 && roundedDir.y > 0)
        {
            if (roundedDominant.x > 0 || roundedDominant.y < 0)
            {
                if (isXYTurn)
                {
                    turnData.axis = RotationAxis.X;
                    turnData.angle = 90;
                    turns.Add(turnData);
                }

                turnData.axis = RotationAxis.Y;
                turnData.angle = -90;
                turns.Add(turnData);
                Debug.Log($"2: Смотрим вверх-влево");
            }
        }
        else
        if (roundedDir.y > 0 && roundedDir.z > 0)
        {
            if (roundedDominant.y < 0 || roundedDominant.z < 0)
            {
                turnData.axis = RotationAxis.X;
                turnData.angle = 180;
                turns.Add(turnData);
            }
        }
        else
        if (roundedDir.y > 0 && roundedDir.z < 0)
        {
            turnData.axis = RotationAxis.X;
            turnData.angle = 90;
            turns.Add(turnData);
            print("тыр тыр");
        }

        if (turns.Count == 0)
        {
            roundedDominant.x *= -1f;
            Quaternion rotation = Quaternion.LookRotation(roundedDominant);
            rotation.ToAngleAxis(out var angle, out var rotationAxis);

            RotationAxis turnAxis = RotationAxis.Y;
            var turnBlockAngle = angle * rotationAxis.y;
            if (!(Mathf.Abs(roundedDominant.z - 1f) < 0.001f))
            {
                if (Mathf.Abs(rotationAxis.x) > 0)
                {
                    turnAxis = RotationAxis.X;
                    turnBlockAngle = angle * rotationAxis.x;
                }
            }

            if ((availableAxis & turnAxis) == turnAxis)
            {
                turnData.axis = turnAxis;
                turnData.angle = turnBlockAngle;

                turns.Add(turnData);
                //chunk.AddTurnBlock
                //(
                //    blockLocalpos,
                //    (int)turnData.angle,
                //    turnData.axis
                //);
                //Debug.Log($"Повернул {turnData.axis} :: {turnData.angle}");
            }


            // Округляем направление до ближайшего кратного 90 градусов значения (-1, 0, 1)


        }

        foreach (var item in turns)
        {
            chunk.AddTurnBlock
                (
                    blockLocalPos,
                    (int)item.angle,
                    item.axis
                );
            //Debug.Log($"Повернул {item.axis} :: {item.angle}");
        }
        

        return turns;
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
            var itemData = ItemsStorage.Singleton.GetItemData(blockID);
            if (itemData.itemType != ItemType.BLOCKABLE)
            {
                transform.position += Vector3.up;
            }
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

        if (savePositionTimer < 1.5f)
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
        var item = player.inventory.main.Find(i => i.count < 1);
        if (item != null)
        {
            player.inventory.main.Remove(item);
            Debug.Log("Дропнул эбушку");
        }

        var jsonInventory = new JsonInventory(player.inventory);
        //print(jsonInventory.mainSize);
        //print(jsonInventory.main.Count);
        var json = JsonConvert.SerializeObject(jsonInventory);
        PlayerPrefs.SetString("inventory", json);
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        //onMineSpawn.RemoveAllListeners();
        //onOwnerPositionSet.RemoveAllListeners();
    }
}

public class PlayerOwnerSpawn : UnityEvent<MonoBehaviour> { }


