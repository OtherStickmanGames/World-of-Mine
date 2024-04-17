using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;
using System.Linq;

public class Worker : MonoBehaviour
{
    [SerializeField] Transform indicator;
    [SerializeField] Transform destionationIndicator;
    [SerializeField] Transform mineDirIndicator;
    [SerializeField] NavMeshAgent meshAgentPrefab;
    [SerializeField] Player player;
    [SerializeField] float verticalSpeed = 5;
    [SerializeField] float moveSpeed = 2f;

    [Header("Debug Info")]
    [SerializeField] [TextArea(1,5)] List<string> sequenceAction;
    [SerializeField] float distanceToMineBlock;
    [SerializeField] int lastPosCounter;


    NavMeshAgent navMeshAgent;
    MoveComponent moveComponent;

    [field: SerializeField]
    State currentState;

    public State CurrentState {
        get => currentState;
        set
        {
            currentState = value;
            lastPosCounter = 0;
            mineTimer = 0;
            logicTimeout = 0;
            currentStateTime = 0;
        } 
    }
    float defaultStopDistance;
    float lifetime;
    public bool inTowerArea;
    bool needFixPos;
    string hashId;

    private void Start()
    {
        moveComponent = GetComponent<MoveComponent>();

        hashId = GetHashCode().ToString();
        gameObject.name = gameObject.name.Insert(0, $"{hashId} ");

        TriggerSystem.onTriggerEnter += Trigger_Entered;
        TriggerSystem.onTriggerExit += Trigger_Exit;
        //navMeshAgent = Instantiate(meshAgentPrefab, transform.position, Quaternion.identity);
        
    }


    private void Update()
    {
        lifetime += Time.deltaTime;

        if (!navMeshAgent)
        {
            if (moveComponent.Grounded && lifetime > 3f)
            {
                navMeshAgent = Instantiate(meshAgentPrefab, transform.position, Quaternion.identity);
                navMeshAgent.name = navMeshAgent.name.Insert(0, $"{hashId} ");
                navMeshAgent.transform.SetParent(GameManager.Inst.workersParent);
                defaultStopDistance = navMeshAgent.stoppingDistance;
            }
        }
        else
        {
            FollowToNavAgent();
            UpdateDebugInfo();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            //WorldGenerator.Inst.SetBlockAndUpdateChunck(transform.position.ToGlobalBlockPos(), 11);
            //print(World.Instance.notMineable.Length);
            //var blockData = FindBlockSystem.Instance.GetNearBlockByUpPlane(transform.position, World.Instance.notMineable);
            //WorldGenerator.Inst.SetBlockAndUpdateChunck(blockData.pos, 8);
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            var player = FindObjectOfType<PlayerBehaviour>().transform;
            navMeshAgent.SetDestination(player.position);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            print(navMeshAgent.hasPath);
            print(navMeshAgent.pathPending);
            print(navMeshAgent.isPathStale);
            print(navMeshAgent.pathStatus);
            //print(navMeshAgent.currentOffMeshLinkData.activated);
            //print(navMeshAgent.currentOffMeshLinkData.valid);
            //print(navMeshAgent.currentOffMeshLinkData.linkType);
            //print(navMeshAgent.currentOffMeshLinkData.startPos);
            print(navMeshAgent.velocity);
            print(navMeshAgent.desiredVelocity);
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            print("запущ трианглик");
            NavMesh.CalculateTriangulation();
        }

        if (navMeshAgent)
        {
            Logic();
        }
    }

    Vector3 dirToNavMesh;

    void FollowToNavAgent()
    {
        dirToNavMesh = navMeshAgent.transform.position - transform.position;
        var correctedDir = dirToNavMesh;
        correctedDir.y = 0;
        //dir.y = 0;
        var dist = correctedDir.magnitude;
        if (dist > 0.3f)
        {
            var agentVel = navMeshAgent.velocity;
            var vel = new Vector2(dirToNavMesh.x, dirToNavMesh.z);
            //vel = new Vector2(agentVel.x, agentVel.z);
            moveComponent.MoveSpeed = Mathf.Clamp(dist * moveSpeed, 0, 10);
            moveComponent.Movenment = vel;

            if(dist > 3 || navMeshAgent.transform.position.y - transform.position.y > 10)
            {
                needFixPos = true;
                //transform.position = navMeshAgent.transform.position;
                //print($"какая-то жопа {gameObject}");
            }
        }
        else
        {
            moveComponent.Movenment = Vector2.zero;
        }
    }

    float logicTimeout;
    float currentStateTime;
    float checkLastPosTimer;
    int waitLootDuration;
    Vector3 lastNavMeshPos;
    public BlockData mineableData;
    List<Vector3> excludeBlocks = new List<Vector3>();
    bool findingNearBlock = false;

    void Logic()
    {
        logicTimeout += Time.deltaTime;
        currentStateTime += Time.deltaTime;
        checkLastPosTimer += Time.deltaTime;
        mineDirIndicator.forward = transform.forward;

        if (lifetime < 3 || logicTimeout < 1)
            return;

        if (CurrentState == State.Idle)
        {
            excludeBlocks.Clear();
            excludeBlocks.AddRange(World.Instance.notMineable);
            excludeBlocks.AddRange(World.Instance.notAvailable);
            if (!findingNearBlock)
            {
                sequenceAction.Add("запустил поиск блока");
                findingNearBlock = true;
                FindBlockSystem.Instance.GetNearUpperBlock(transform.position, excludeBlocks, NearBlock_Received);
            }

            void NearBlock_Received(BlockData result)
            {
                findingNearBlock = false;
                WorldGenerator.Inst.SetBlockAndUpdateChunck(result.pos, 8);
                mineableData = result;
                World.Instance.AddNotAvailable(result.pos);

                CurrentState = State.Mine;
                currentStateTime = 0;
                logicTimeout = 0;
                mineTimer = 0;
                sequenceAction.Add("нашел блок");
            }

        }
        if (CurrentState == State.Mine)
        {
            distanceToMineBlock = Vector3.Distance(mineableData.pos, transform.position);

            navMeshAgent.stoppingDistance = defaultStopDistance;
            SetDestination(mineableData.pos - (Vector3.right * 0.5f) + (Vector3.forward * 0.5f) + (Vector3.up * 0.5f));
            //navMeshAgent.SetDestination(mineableData.pos - (Vector3.right * 0.5f) + (Vector3.forward * 0.5f) + (Vector3.up * 0.5f));

            CheckDistanceToMineBlockByDestination();
            if (CheckExistBlock())
            {
                if (IsPathNotComplete && currentStateTime > 60 && navMeshAgent.velocity == Vector3.zero && mineTimer < 0.001f)
                {
                    sequenceAction.Add("Первая глоб. проверка майна");
                    World.Instance.AddNotAvailable(mineableData.pos);
                    if (player.inventory.quick.Count > 0)
                    {
                        CurrentState = State.DropRes;
                    }
                    else
                    if (CheckCountMatchLastPos())
                    {
                        if (transform.position.y - World.Instance.towerPos.position.y > 1.8f)
                        {
                            var blockPos = navMeshAgent.transform.position + Vector3.right;
                            WorldGenerator.Inst.MineBlock(blockPos.ToGlobalBlockPos());
                            StartCoroutine(DisableDelayEnableAgent());
                            print("mdooooooo");
                        }
                    }
                    else
                    {
                        CurrentState = State.Idle;
                    }
                    currentStateTime = 0;
                    logicTimeout = 0;
                    //print($"нууууу, такое происходит {navMeshAgent.pathStatus}");
                }
                else
                if (IsPathNotComplete && CheckCountMatchLastPos(15) && mineTimer < 0.001f)
                {
                    sequenceAction.Add("Вторая глоб. проверка майна");
                    World.Instance.AddNotAvailable(mineableData.pos);
                    CurrentState = State.Idle;
                    currentStateTime = 0;
                    logicTimeout = 0;
                    print("пригодилось");
                    GameManager.CheckPathBetweenBlock(mineableData.pos, World.Instance.towerPos.position.ToGlobalBlockPos());
                }
            }

            if (navMeshAgent.velocity == Vector3.zero && distanceToMineBlock < 5 && mineableData != null)
            {
                Mine();
            }
        }
        if (CurrentState == State.WaitTakeLoot)
        {
            if (player.inventory.quick.Count > 0)
            {
                CurrentState = State.DropRes;
            }

            waitLootDuration++;

            if (waitLootDuration > 5)
            {
                CurrentState = State.Idle;
            }

            logicTimeout = 0;
        }
        if (CurrentState == State.DropRes)
        {
            navMeshAgent.stoppingDistance = 0;
            SetDestination(World.Instance.towerPos.position);
            //navMeshAgent.SetDestination(World.Instance.towerPos.position);

            if (inTowerArea)
            {
                Tower.Instance.countResources++;
                player.inventory.quick.Clear();

                CurrentState = State.Idle;
                var randomOffset = (Random.insideUnitCircle * 3);
                var randomPos = transform.position + new Vector3(randomOffset.x, 1, randomOffset.y);
                navMeshAgent.SetDestination(randomPos);
                sequenceAction.Clear();
            }
            else
            if (IsPathNotComplete && CheckCountMatchLastPos())
            {
                if (CheckAvailableMineHeight())
                {
                    var blockPos = (navMeshAgent.transform.position + Vector3.right).ToGlobalBlockPos();
                    var bottomID = WorldGenerator.Inst.GetBlockID(blockPos);
                    if (bottomID == 0)
                    {
                        blockPos = (transform.position + Vector3.right).ToGlobalBlockPos();
                    }
                    
                    WorldGenerator.Inst.MineBlock(blockPos);
                    StartCoroutine(DisableDelayEnableAgent());
                    print("чет застрял");
                }
                else
                {
                    currentState = State.Idle;
                    sequenceAction.Add("я хз шо мне делать");
                }
            }

            logicTimeout = 0;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            print(navMeshAgent.transform.position.ToGlobalBlockPos());
        }


    }

    float checkExistBlockTimer;
    private bool CheckExistBlock()
    {
        checkExistBlockTimer += Time.deltaTime;

        if (checkExistBlockTimer < 1)
            return true;

        if (WorldGenerator.Inst.GetBlockID(mineableData.pos) == 0)
        {
            checkExistBlockTimer = 0;

            CurrentState = State.Idle;
            waitLootDuration = 0;
            mineableData = null;
            sequenceAction.Add("Обнаружил, что блока не существует");
            return false;
        }

        return true;
    }

    private void CheckDistanceToMineBlockByDestination()
    {
        var destinationPos = navMeshAgent.destination.ToGlobalBlockPos();

        if (destinationPos + Vector3.down == mineableData.pos)
            return;

        var destinationBlockPos = destinationPos + Vector3.down + Vector3.right;

        if (World.Instance.notMineable.Contains(destinationBlockPos))
            return;

        if (Vector3.Distance(navMeshAgent.destination, mineableData.pos) > 3.5f && CheckAvailableMineHeight(navMeshAgent.destination))
        {
            var id = WorldGenerator.Inst.GetBlockID(destinationBlockPos);

            //WorldGenerator.Inst.SetBlockAndUpdateChunck(destinationPos + Vector3.down + Vector3.right, 14);

            if (id != 0)
            {
                mineableData.pos = destinationBlockPos;
                mineableData.ID = id;
                World.Instance.AddNotAvailable(destinationBlockPos);
                sequenceAction.Add($"Дестинатион смена блока {Vector3.Distance(navMeshAgent.destination, mineableData.pos)}\n{navMeshAgent.destination} = Дестинат\n{mineableData.pos} = Блок пос");
            }
        }
    }

    [SerializeField] float mineTimer;
    //GameObject ebos;
    void Mine()
    {
        mineTimer += Time.deltaTime;

        var playerPos = transform.position + Vector3.up;
        var dir = (mineableData.pos - playerPos).normalized;
        var rayPos = (playerPos + (dir * 2f)).ToGlobalRoundBlockPos();

        //if (!ebos)
        //{
        //    ebos = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //    ebos.transform.localScale = Vector3.one * 0.58f;
        //    Destroy(ebos.GetComponent<Collider>());
        //}

        //ebos.transform.position = rayPos;
        //WorldGenerator.Inst.SetBlockAndUpdateChunck(rayPos, 14);

        mineDirIndicator.forward = dir;

        var blockID = WorldGenerator.Inst.GetBlockID(rayPos);
        if (blockID != 0)
        {
            if (rayPos != mineableData.pos)
            {
                if (!World.Instance.notMineable.Contains(rayPos))
                {
                    sequenceAction.Add("сменил блок по лучу");
                    WorldGenerator.Inst.SetBlock(rayPos, 11);
                    SetMineableData(rayPos, blockID);
                    //print("нашел блок ближе");
                }
            }
        }

        if (WorldGenerator.Inst.GetBlockID(mineableData.pos) == 0)
        {
            CurrentState = State.Idle;
            waitLootDuration = 0;
            mineableData = null;
            sequenceAction.Add("Пока ебашил, обнаружил, что блока не существует");
            //print("Странная хуйня.....");
            return;
        }

        if (World.Instance.notMineable.Contains(mineableData.pos))
        {
            Debug.LogError("ПИЗДЭС");
        }

        if (mineTimer > 3)
        {
            mineTimer = 0;
            WorldGenerator.Inst.MineBlock(mineableData.pos);
            World.Instance.notAvailable.Remove(mineableData.pos);

            logicTimeout = 0;
            waitLootDuration = 0;
            mineableData = null;
            CurrentState = State.WaitTakeLoot;
            lastPosCounter = 0;
        }
    }

    public NavMeshPathStatus CurrentPathStatus;
    bool IsPathNotComplete => CurrentPathStatus != NavMeshPathStatus.PathComplete;

    bool CheckCountMatchLastPos(int matchesThresold = 10)
    {
        if (checkLastPosTimer < 1)
            return false;

        checkLastPosTimer = 0;

        var dir = navMeshAgent.transform.position - lastNavMeshPos;
        if (dir.sqrMagnitude > 0.5)
        {
            lastPosCounter = 0;
            lastNavMeshPos = navMeshAgent.transform.position;
            return false;
        }
        else
        {
            lastPosCounter++;
            if (lastPosCounter > matchesThresold)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    bool CheckAvailableMineHeight()
    {
        var agentPos = navMeshAgent.transform.position.ToGlobalBlockPos();
        var towerPos = World.Instance.towerPos.position.ToGlobalBlockPos();
        if (Vector3.Distance(agentPos, towerPos) > 10)
            return true;

        return agentPos.y > towerPos.y;
    }

    bool CheckAvailableMineHeight(Vector3 target)
    {
        var agentPos = target.ToGlobalBlockPos();
        var towerPos = World.Instance.towerPos.position.ToGlobalBlockPos();
        if (Vector3.Distance(agentPos, towerPos) > 30)
            return true;

        return agentPos.y > towerPos.y;
    }

    void SetMineableData(Vector3 pos, byte id)
    {
        World.Instance.notAvailable.Remove(mineableData.pos);
        mineableData.pos = pos;
        mineableData.ID = id;
        World.Instance.AddNotAvailable(pos);
    }

    Vector3 lastDestination;
    void SetDestination(Vector3 pos)
    {
        if (pos == lastDestination)
            return;

        navMeshAgent.SetDestination(pos);
        lastDestination = pos;
        string str = $"{pos} Назанчен";
        if (pos == World.Instance.towerPos.position)
        {
            str = "Идем к башне";
        }
        sequenceAction.Add(str);
    }

    IEnumerator DisableDelayEnableAgent()
    {
        navMeshAgent.enabled = false;
        CurrentState = State.Sleep;
        navMeshAgent.transform.position = transform.position;

        yield return new WaitForSeconds(8f);

        navMeshAgent.transform.position = transform.position;
        yield return null;
        navMeshAgent.enabled = true;
        CurrentState = State.Idle;
    }

    public Vector3 agentVelocity;
    public bool hasPath;
    public bool isPathStale;
    public Vector3[] corners;
    void UpdateDebugInfo()
    {
        CurrentPathStatus = navMeshAgent.pathStatus;
        agentVelocity = navMeshAgent.velocity;
        hasPath = navMeshAgent.hasPath;
        isPathStale = navMeshAgent.isPathStale;
        var path = navMeshAgent.path;
        corners = path.corners;
    }

    private void LateUpdate()
    {
        var offset = Mathf.Abs(dirToNavMesh.x) + Mathf.Abs(dirToNavMesh.z);
        if (dirToNavMesh.y > GameManager.Inst.JumpLowThresold && offset > GameManager.Inst.JumpAvailabelDist && navMeshAgent.velocity != Vector3.zero)
        {
            var currentPos = transform.position;
            var targetPos = currentPos;
            targetPos.y += dirToNavMesh.y;
            var k = Mathf.Clamp(dirToNavMesh.y, 0, GameManager.Inst.JumpTopThresold);
            moveComponent._verticalVelocity = GameManager.Inst.JumpForce * k; // * Time.deltaTime;
            //transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * verticalSpeed);
            //print("потуги");
        }

        if (needFixPos)
        {
            var newPos = transform.position;
            newPos.x = navMeshAgent.transform.position.x;
            newPos.y = navMeshAgent.transform.position.y + 0.8f;
            newPos.z = navMeshAgent.transform.position.z;
            transform.position = newPos;

            needFixPos = false;
        }

        if (navMeshAgent && currentState != State.Sleep && IsPathNotComplete)
        {
            var diffrentHeight = navMeshAgent.transform.position.y - transform.position.y;
            if (diffrentHeight > GameManager.Inst.maxDiffrentHeight)
            {
                StartCoroutine(DisableDelayEnableAgent());
                //print("ебат кроват");
            }
        }

        if (currentState == State.Mine)
        {
            indicator.position = mineableData.pos - (Vector3.right * 0.5f) + (Vector3.forward * 0.5f) + (Vector3.up * 0.5f);
        }
        else
        {
            indicator.position = Vector3.zero;
        }

        if (navMeshAgent && !float.IsInfinity(navMeshAgent.destination.x))
        {
            destionationIndicator.position = navMeshAgent.destination;
        }

        var playerPos = transform.position + Vector3.up;
        mineDirIndicator.position = playerPos;
    }

    private void Trigger_Entered(GameObject entered, GameObject trigger)
    {
        if (inTowerArea)
            return;

        if (entered != gameObject)
            return;

        if (trigger != Tower.Instance.trigger)
            return;

        inTowerArea = true;
    }

    private void Trigger_Exit(GameObject exited, GameObject trigger)
    {
        if (!inTowerArea)
            return;

        if (exited != gameObject)
            return;

        if (trigger != Tower.Instance.trigger)
            return;

        inTowerArea = false;
    }

    public enum State
    {
        Idle,
        Mine,
        Sleep,
        DropRes,
        WaitTakeLoot,
    }
}
