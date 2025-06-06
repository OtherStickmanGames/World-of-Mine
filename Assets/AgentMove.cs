using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using System;
using Unity.AI.Navigation;

public class AgentMove : MonoBehaviour
{
    [SerializeField] NavMeshAgent agent;

    //[SerializeField] float approachDistance = 1.0f; // ���������� ���������� ��� ������� � ����� ���������
    [SerializeField] byte scaffoldingBlockID = 1;   // ID ���������� ����� ��� ����� (scaffolding)
    [SerializeField] int verticalGapThreshold = 5;  // ���� ����� ������ ��� ����� ����� ������, ������ ������������ �������
    [SerializeField] ItemID[] excludePathfindingBlocks;
    [SerializeField] GameObject markerPrefab;

    public bool skipContine;
    public bool withDebug;
    public bool withPause;
    public List<Vector3Int> allowedDirections;
    public List<Vector3Int> allScaffoldingPositions = new();

    List<GameObject> markers = new List<GameObject>();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            isPaused = false;
        }
    }

    

    public IEnumerator MoveToPosition(Vector3 destination, bool canBuildLadder = true, float approachDistance = .3f)
    {
        var distance = Vector3.Distance(agent.transform.position + Vector3.down, destination);
        if (distance < approachDistance - .1f)
        {
            print("��� �� �����");
            yield break;
        }

        agent.SetDestination(destination);
        agent.isStopped = true;

        // ����, ���� ���� �� ����� ��������:
        while (agent.pathPending)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        NavMeshPath path = agent.path;
        
        agent.isStopped = false;
        agent.stoppingDistance = approachDistance - 0.05f;

        var lastCorner = path.corners[path.corners.Length - 1];
        var lastCornerEnough = false;
        if (path.corners.Length > 0)
        {
            var distLastCornerToDest = Vector3.Distance(destination, lastCorner);
            if (distLastCornerToDest - 0.05f < approachDistance)
            {
                lastCornerEnough = true;
            }
        }

        if (canBuildLadder && path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.Log($"MoveToPosition: ���� �� {destination} �� ������ ����� NavMesh (PathComplete = {path.status}). ��������� ���������� scaffolding. lastCornerEnough = {lastCornerEnough}");
            if (path.status is NavMeshPathStatus.PathInvalid)
            {
                yield return StartCoroutine(Pause());
            }

            var scaffoldingDestination = destination;
            if (lastCornerEnough)
            {
                scaffoldingDestination = lastCorner;
            }
            yield return StartCoroutine(MoveToPosition(destination, false));// ����� ���������
            yield return StartCoroutine(BuildPathScaffolding(scaffoldingDestination));
            yield return StartCoroutine(MoveToPosition(destination, false));
            yield break;
        }

        //agent.SetPath(path);

        float noMovementTimeout = 5f;
        float noProgressTimeout = 5f;
        float stuckTimer = 0f;
        float progressTimer = 0f;
        Vector3 lastPosition = agent.transform.position;
        float lastDistanceToDest = (path.corners.Length > 0)
            ? Vector3.Distance(agent.transform.position, path.corners[path.corners.Length - 1])
            : Vector3.Distance(agent.transform.position, destination);

        while (agent.remainingDistance > approachDistance /*|| agent.pathPending/**/ )
        {
            yield return StartCoroutine(CheckScaffoldingToRemove());

            float distanceMoved = Vector3.Distance(agent.transform.position, lastPosition);
            bool isMoving = distanceMoved > 0.01f;
            if (!isMoving)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > noMovementTimeout)
                {
                    Debug.Log($"MoveToPosition: ����� ��������� ������� � {agent.transform.position}.");
                    if (canBuildLadder)
                    {
                        yield return StartCoroutine(BuildPathScaffolding(destination));
                        yield return StartCoroutine(MoveToPosition(destination, false));
                    }
                    yield break;
                }
            }
            else
            {
                stuckTimer = 0f;
            }

            float currentDistanceToDest = agent.remainingDistance;
            if (currentDistanceToDest >= lastDistanceToDest - 0.05f)
            {
                progressTimer += Time.deltaTime;
                if (progressTimer > noProgressTimeout)
                {
                    Debug.Log($"MoveToPosition: ����� �� ������������ � {destination}, ������� ���������� = {currentDistanceToDest}");
                    if (canBuildLadder)
                    {
                        yield return StartCoroutine(BuildPathScaffolding(destination));
                        yield return StartCoroutine(MoveToPosition(destination, false));
                    }
                    yield break;
                }
            }
            else
            {
                progressTimer = 0f;
            }
            lastDistanceToDest = currentDistanceToDest;
            lastPosition = agent.transform.position;
            yield return null;
        }

        yield return StartCoroutine(StopAgent());
    }

    WaitForSeconds wait01 = new(0.1f);

    public IEnumerator SimpleMoveToPosition(Vector3 destination, float approachDistance = 0.1f)
    {
        var distance = Vector3.Distance(transform.position, destination);
        if (distance < approachDistance)
        {
            yield break;
        }

        agent.SetDestination(destination);
        agent.isStopped = true;

        // ����, ���� ���� �� ����� ��������:
        while (agent.pathPending)
        {
            yield return null;
        }

        yield return wait01;

        NavMeshPath path = agent.path;

        agent.isStopped = false;

        float noMovementTimeout = 5f;
        float noProgressTimeout = 5f;
        float stuckTimer = 0f;
        float progressTimer = 0f;
        Vector3 lastPosition = agent.transform.position;
        float lastDistanceToDest = (path.corners.Length > 0)
            ? Vector3.Distance(agent.transform.position, path.corners[path.corners.Length - 1])
            : Vector3.Distance(agent.transform.position, destination);

        while (agent.pathPending || agent.remainingDistance > approachDistance)
        {
            float distanceMoved = Vector3.Distance(agent.transform.position, lastPosition);
            bool isMoving = distanceMoved > 0.01f;
            if (!isMoving)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > noMovementTimeout)
                {
                    Debug.Log($"MoveToPosition: ����� ��������� ������� � {agent.transform.position}.");
                    
                    yield break;
                }
            }
            else
            {
                stuckTimer = 0f;
            }

            float currentDistanceToDest = agent.remainingDistance;
            if (currentDistanceToDest >= lastDistanceToDest - 0.05f)
            {
                progressTimer += Time.deltaTime;
                if (progressTimer > noProgressTimeout)
                {
                    Debug.Log($"MoveToPosition: ����� �� ������������ � {destination}, ������� ���������� = {currentDistanceToDest}");
                   
                    yield break;
                }
            }
            else
            {
                progressTimer = 0f;
            }
            lastDistanceToDest = currentDistanceToDest;
            lastPosition = agent.transform.position;
            yield return null;
        }

        yield return StartCoroutine(StopAgent());
    }

    

    private IEnumerator BuildPathScaffolding(Vector3 destination)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.transform.position = destination;
        go.transform.localScale *= 0.3f;
        go.name = "Destination ��� ����";
        Destroy(go.GetComponent<Collider>());

        yield return new WaitForSeconds(0.5f);

        //yield return StartCoroutine(Pause());

        // �������� ������������� ������� ������ � ����
        Vector3Int agentPos = new Vector3Int(
            Mathf.FloorToInt(transform.position.x + 1),
            Mathf.FloorToInt(transform.position.y - 1.1f),
            Mathf.FloorToInt(transform.position.z)
        );
        Vector3Int destPos = new Vector3Int(
            Mathf.FloorToInt(destination.x + 1),
            Mathf.FloorToInt(destination.y - 0.9f),// !!!!!!
            Mathf.FloorToInt(destination.z)
        );

        if (WorldGenerator.Inst.GetBlockID(destPos + Vector3Int.up) != 0)
            destPos.y++;

        if (withPause)
        {
            WorldGenerator.Inst.SetBlockAndUpdateChunck(agentPos, 90);
            WorldGenerator.Inst.SetBlockAndUpdateChunck(destPos, 61);

            yield return StartCoroutine(Pause());
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.transform.position = destPos;
        go.transform.localScale *= 0.3f;
        go.name = "��������������� Destionation";
        Destroy(go.GetComponent<Collider>());

        List<Vector3Int> path = null;
        Debug.Log("������ ���������� � ���� ���� ����������� ����� AStarPath3D.");
        yield return StartCoroutine(AStarPath3DCoroutine(agentPos, destPos, result => path = result));


        if (path == null)
        {
            Debug.Log("�� ������� ����� ���� ��� scaffolding.");
            yield break;
        }

        List<GameObject> toAfterDestroy = new();

        var startItemId = (ItemID)WorldGenerator.Inst.GetBlockID(agentPos);
        
        if (excludePathfindingBlocks.Contains(startItemId))
        {
            var pathLink = CreatePathNavMeshLink(agentPos, path[1]);
            toAfterDestroy.Add(pathLink);
        }

        yield return new WaitForSeconds(0.5f);

        Debug.Log("������ ���� ��� scaffolding, �����: " + path.Count);
        List<Vector3Int> scaffoldingPositions = new();
        foreach (Vector3Int cell in path)
        {
            // ���� � ������ ����� � ������ scaffolding-����
            if (WorldGenerator.Inst.GetBlockID(cell) == 0)
            {
                WorldGenerator.Inst.SetBlockAndUpdateChunck(cell, scaffoldingBlockID);
                yield return new WaitForSeconds(0.3f);

                //var pathPart = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //pathPart.transform.position = cell + new Vector3(-0.5f, 0.3f, 0.5f);
                //pathPart.transform.localScale *= 0.5f;
                //pathPart.name = "-= ����� ���� =-";

                var approachCellPos = NavigationTool.FindApproachPositionOnBlock(cell);

                yield return StartCoroutine(SimpleMoveToPosition(approachCellPos));

                print("����������� �����");

                scaffoldingPositions.Add(cell);

                var scaffoldingsCount = scaffoldingPositions.Count;
                if (scaffoldingsCount > 1)
                {
                    var blocks = blueprint.blockPositions;
                    var start = blueprint.startPosition;
                    var prevIdx = scaffoldingsCount - 2;
                    var prevScaffolding = scaffoldingPositions[prevIdx];
                    //if (IsPositionInsideBuilding(blocks, prevScaffolding, start))
                    {
                        print("����");
                        WorldGenerator.Inst.SetBlockAndUpdateChunck(prevScaffolding, 0);
                        scaffoldingPositions.RemoveAt(prevIdx);

                        yield return null;
                    }
                }

                yield return StartCoroutine(Pause());

                //Debug.Log("��������� scaffolding ���� �� " + cell);
                //yield return StartCoroutine(MoveToPosition(cell, false));
                //yield return new WaitForSeconds(0.3f);
            }
            yield return null;
        }

        if (withPause)
        {
            yield return StartCoroutine(Pause());
        }

        yield return new WaitForSeconds(0.5f);

        allScaffoldingPositions.AddRange(scaffoldingPositions);
        // ����� ���������� scaffolding, ������������ � ����,
        // ��������� ����� �� ���� ���� ����
        //Vector3 destinationOffset = destination + Vector3.down;
        yield return StartCoroutine(MoveToPosition(destination, false));

        Destroy(go);
        foreach (var item in toAfterDestroy)
        {
            Destroy(item);
        }
    }

    GameObject CreatePathNavMeshLink(Vector3 start, Vector3 end)
    {
        var linkGo = new GameObject("-= PATH LINK =-");
        var link = linkGo.AddComponent<NavMeshLink>();
        link.transform.position = start + new Vector3(-0.5f, 1f, 0.5f);
        link.width = 0.97f;
        link.autoUpdate = true;

        var dir = end - start;

        if ((int)dir.x != 0)
        {
            linkGo.transform.rotation = Quaternion.Euler(0, 90 * dir.x, 0);
        }
        if ((int)dir.z != 0 && dir.z < 0)
        {
            linkGo.transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        link.endPoint = new Vector3(0, dir.y, 1);
        link.startPoint = Vector3.zero;
        return linkGo;
    }

    public IEnumerator StopAgent()
    {
        agent.updatePosition = false;
        agent.SetDestination(transform.position);

        while (agent.pathPending)
        {
            yield return null;
        }

        agent.ResetPath();
        agent.updatePosition = true;
    }

    private IEnumerator AStarPath3DCoroutine(Vector3Int start, Vector3Int goal, Action<List<Vector3Int>> callback)
    {
        print($"Id goal ����� {(ItemID)WorldGenerator.Inst.GetBlockID(goal)}");
        // ��������� ��������, �������� ������������ �������� � �������������� ���������.
        // ��������� ������ ��������, � ������� ���� dx == 0, ���� dz == 0 (�� �� ��� ���������).
        // ����� ��������� ����� ������������ ���� (����� dx � dz ����� 0, � dy �� ����� 0).
        allowedDirections = new List<Vector3Int>();
        //if (!allowedDirections.Any())
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        // ���������� ���������� ��������.
                        if (dx == 0 && dy == 0 && dz == 0)
                            continue;
                        // ��������� ����� ������������ �������� (������ �� Y).
                        if (dx == 0 && dz == 0 && dy != 0)
                            continue;
                        // ��������� ������������ ���� �� ����������� (����� � dx, � dz ���������).
                        if (dx != 0 && dz != 0)
                            continue;

                        allowedDirections.Add(new Vector3Int(dx, dy, dz));
                    }
                }
            }
        }

        Dictionary<Vector3Int, Node> openSet = new Dictionary<Vector3Int, Node>();
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();

        List<Vector3Int> ebos = new();

        Node startNode = new Node(start);
        startNode.gCost = 0;
        startNode.hCost = ManhattanDistance(start, goal);
        openSet.Add(start, startNode);

        print($"{(ItemID)WorldGenerator.Inst.GetBlockID(startNode.position)} ��������� ---");

        int iterations = 0;
        int maxIterations = 10000;
        while (openSet.Count > 0)
        {
            iterations++;
            if (iterations % 50 == 0)
                yield return null; // ��� ����� ��������

            if (iterations > maxIterations)
            {
                Debug.Log("AStarPath3DCoroutine: ��������� �������� ��������, ��������� ����.");
                callback(null);
                yield break;
            }

            // ���� ���� � ����������� fCost
            Node current = openSet.Values.OrderBy(n => n.fCost).First();
            if (current.position == goal)
            {
                List<Vector3Int> path = new List<Vector3Int>();
                while (current != null)
                {
                    path.Add(current.position);
                    current = current.parent;
                }
                path.Reverse();

                print($"���� ������, �������� {iterations}");

                callback(path);
                yield break;
            }

            openSet.Remove(current.position);
            closedSet.Add(current.position);

            
            // --=-- ������� �������� ����� --=--
            foreach (var dir in allowedDirections)
            {
                Vector3Int neighborPos = current.position + dir;
                if (closedSet.Contains(neighborPos))
                    continue;

                var neighborId = WorldGenerator.Inst.GetBlockID(neighborPos);


                // ��������, ������� ��������� ����, ���� ����� ����� ��� �������
                // � ���� ����� �����, ����� �� ������ ����� ������ ������
                if (current == startNode && dir.y > 0)
                {
                    var up3Pos = current.position + (Vector3Int.up * 3);
                    if (WorldGenerator.Inst.GetBlockID(up3Pos) != 0)
                    {
                        continue;
                    }
                }

                if (neighborPos != goal)
                {
                    var upBlockID = WorldGenerator.Inst.GetBlockID(neighborPos + Vector3Int.up);
                    var up2BlockID = WorldGenerator.Inst.GetBlockID(neighborPos + Vector3Int.up * 2);
                    var up3BlockID = WorldGenerator.Inst.GetBlockID(neighborPos + Vector3Int.up * 3);

                    if (up2BlockID == 10 || up2BlockID == 94)
                        up2BlockID = 0;
                    if (upBlockID == 10 || upBlockID == 94)
                        upBlockID = 0;
                    if (up3BlockID == 10 || up3BlockID == 94)
                        up3BlockID = 0;

                    //Vector3 neighborF = new Vector3(neighborPos.x, neighborPos.y, neighborPos.z);
                    //// ���� ������ �� ������ � blueprint � ������ (�� �����), ���������� �
                    //if (!blueprintPositions.Contains(neighborF) && WorldGenerator.Inst.GetBlockID(neighborPos) != 0)
                    //    continue;

                    if (upBlockID == scaffoldingBlockID)
                    {
                        WorldGenerator.Inst.SetBlockAndUpdateChunck(neighborPos + Vector3Int.up, 0);
                        upBlockID = 0;
                    }
                    if (up2BlockID == scaffoldingBlockID)
                    {
                        WorldGenerator.Inst.SetBlockAndUpdateChunck(neighborPos + (Vector3Int.up * 2), 0);
                        up2BlockID = 0;
                    }
                    if (up3BlockID == scaffoldingBlockID)
                    {
                        WorldGenerator.Inst.SetBlockAndUpdateChunck(neighborPos + (Vector3Int.up * 3), 0);
                        up3BlockID = 0;
                    }

                    // ���������, ��� ��� ������� �������� ��� ������
                    if (upBlockID != 0 || up2BlockID != 0 || up3BlockID != 0)
                    {
                        if (withDebug)
                        {
                            var offset = new Vector3(-0.5f, 0.5f, 0.5f);
                            var marker = Instantiate(markerPrefab, neighborPos + offset, Quaternion.identity);
                            markers.Add(marker);

                            yield return StartCoroutine(Pause($"������� �� ������ {upBlockID != 0} || {up2BlockID != 0} || {up3BlockID != 0}"));
                        }

                        continue;
                    }

                    //if (dir.y > 0)
                    //{
                    //    // ���������, ��� ��� ������� �������� ��� ������
                    //    if (upBlockID != 0 || up2BlockID != 0 || up3BlockID != 0)
                    //        continue;
                    //}
                    //else
                    //{// ���������, ��� ��� ������� �������� ��� ������
                    //    if (upBlockID != 0 || up2BlockID != 0)
                    //        continue;
                    //}

                    // ���� ��� � openSet ��� closedSet ���� ���� �� ��� ������ ����� �� ���������, ���������� ���
                    Vector3Int aboveCandidate = neighborPos + (Vector3Int.up * 2);
                    //if (aboveCandidate != start && (openSet.ContainsKey(aboveCandidate) || closedSet.Contains(aboveCandidate)))
                    if (IsAboveInPath(current, aboveCandidate))
                    {
                        if (withDebug)
                        {
                            var offset = new Vector3(-0.5f, 0.5f, 0.5f);
                            var marker = Instantiate(markerPrefab, neighborPos + offset, Quaternion.identity);
                            markers.Add(marker);

                            yield return StartCoroutine(Pause($"������� �� ������ {openSet.ContainsKey(aboveCandidate)} || {closedSet.Contains(aboveCandidate)}"));
                        }
                        //Debug.Log("�������� ����� ������ ��� ��������");
                        continue;
                    }

                    bool IsAboveInPath(Node node, Vector3Int aboveCandidate)
                    {
                        Node current = node;
                        while (current != null)
                        {
                            if (current.position == aboveCandidate)
                                return true;
                            current = current.parent;
                        }
                        return false;
                    }

                    if (excludePathfindingBlocks.Contains((ItemID)neighborId))
                    {
                        //print("=-=-=-=-=-=-=-");
                        continue;
                    }

                    var neighborDownId = WorldGenerator.Inst.GetBlockID(neighborPos + Vector3Int.down);
                    if (excludePathfindingBlocks.Contains((ItemID)neighborDownId))
                    {
                        continue;
                    }

                    // ���� ��� � openSet ��� closedSet ���� ���� ����
                    // �� ���������, ���������� �
                    var belowCandidate = neighborPos + Vector3Int.down + Vector3Int.left;
                    if (openSet.ContainsKey(belowCandidate) || closedSet.Contains(belowCandidate))
                    {
                        //continue;
                    }


                    var agentIntPos = transform.position.ToIntPos();
                    agentIntPos.x++;

                    if (agentIntPos + Vector3Int.up == neighborPos || agentIntPos + (Vector3Int.up * 2) == neighborPos)
                    {
                        if (withDebug)
                        {
                            var offset = new Vector3(-0.5f, 0.5f, 0.5f);
                            var marker = Instantiate(markerPrefab, neighborPos + offset, Quaternion.identity);
                            markers.Add(marker);

                            yield return StartCoroutine(Pause($"������� �� third "));
                        }

                        if (!skipContine)
                            continue;
                    }
                }
                else
                {
                    print($"-=-=-= ���� ���� =-=-=-");
                }

                //if (withDebug)
                //{
                //    //WorldGenerator.Inst.SetBlockAndUpdateChunck(neighborPos, 94);

                //    var offset = new Vector3(-0.5f, 0.5f, 0.5f);
                //    var marker = Instantiate(markerPrefab, neighborPos + offset, Quaternion.identity);
                //    markers.Add(marker);

                //    yield return StartCoroutine(Pause($"{dir}"));
                //}


                float tentativeG = current.gCost + 1f;
                Node neighbor;
                if (openSet.TryGetValue(neighborPos, out neighbor))
                {
                    if (tentativeG < neighbor.gCost)
                    {
                        neighbor.gCost = tentativeG;
                        neighbor.parent = current;
                    }
                }
                else
                {
                    neighbor = new Node(neighborPos);
                    neighbor.gCost = tentativeG;
                    neighbor.hCost = ManhattanDistance(neighborPos, goal);
                    neighbor.parent = current;
                    openSet.Add(neighborPos, neighbor);
                }
            }
        }

        Debug.Log("���� �� ������ :(");
        callback(null);
        yield break;
    }

    private float ManhattanDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
    }

    private class Node
    {
        public Vector3Int position;
        public float gCost;
        public float hCost;
        public float fCost { get { return gCost + hCost; } }
        public Node parent;
        public Node(Vector3Int pos) { position = pos; }
    }

    private IEnumerator CheckScaffoldingToRemove()
    {
        var offset = new Vector3(-0.5f, 1, 0.5f);
        var idxOffset = 0;
        for (int i = 0; i < allScaffoldingPositions.Count; i++)
        {
            var pos = allScaffoldingPositions[i - idxOffset];
            var dist = Vector3.Distance(agent.transform.position, pos + offset);
            if (dist > 1.1f)
            {
                agent.isStopped = true;
                print("��������");
                
                yield return wait01;
                // TO DO, ������ ��� ����� ����, ��� ��� ��� � ����� ����������
                // ��������, �� �� �������� �������� ����� ��� ��������� ����, 
                // � ������ ��������� ����� �� �������, �� ������ NPC ������ 
                // ������� �������� �� ��� ����� ��� ������ ���� ����,
                // ����� ������ ����, � ����� ������� ������ ����, ��� ��� ������,
                // ��� ��� ����� ��������. 
                // ��������: ���� ��������� ������ ����, ���� ���� 1,
                // �� �� ������� ��������, ���� ���������� ����� � �������
                // ������ ���� ����
                // ���� ������ ��������, ����� ��������� ���� ������� ���������
                if (WorldGenerator.Inst.GetBlockID(pos) == scaffoldingBlockID)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(pos, 0);
                }

                if (!allScaffoldingPositions.Remove(pos))
                {
                    print($"��� ����� ������, �� �� ������� ������� {pos}");
                }
                else
                {
                    idxOffset++;
                }

                yield return wait01;

                agent.isStopped = false;
            }
        }
    }


    bool isPaused = false;
    private IEnumerator Pause(string msg = "")
    {
        isPaused = true;
        print($"{gameObject} �� �����... {msg}");

        while (isPaused)
        {
            yield return null;
        }
    }

    /// <summary>
    /// ���������, ��������� �� ������� (globalPos) ������ Bounding Box ���������.
    /// ������ buildingBlockLocalPositions �������� ��������� ���������� ������ (��� ��������),
    /// � ���������� ������� ����� ���������� ��� buildingBlockLocalPosition + buildingCenter.
    /// </summary>
    /// <param name="buildingBlockLocalPositions">������ ��������� ������� ������ ���������.</param>
    /// <param name="globalPos">���������� ������� ��� �������� (��������, ������� ������������� ��������).</param>
    /// <param name="buildintStartPos">����� ���������, ������� ����������� � ��������� ����������� ������.</param>
    /// <returns>True, ���� globalPos ��������� ������ Bounding Box ���������, ����� false.</returns>
    public bool IsPositionInsideBuilding(HashSet<Vector3> buildingBlockLocalPositions, Vector3Int globalPos, Vector3 buildintStartPos)
    {
        if (buildingBlockLocalPositions == null || buildingBlockLocalPositions.Count == 0)
            return false;

        // ��������� ���������� ���������� ��� ������� �����
        Vector3 firstGlobal = buildintStartPos + buildingBlockLocalPositions.First();
        int minX = Mathf.FloorToInt(firstGlobal.x), maxX = Mathf.FloorToInt(firstGlobal.x);
        int minY = Mathf.FloorToInt(firstGlobal.y), maxY = Mathf.FloorToInt(firstGlobal.y);
        int minZ = Mathf.FloorToInt(firstGlobal.z), maxZ = Mathf.FloorToInt(firstGlobal.z);

        // ������� ��� ����� � ������� ���������� AABB
        foreach (var localPos in buildingBlockLocalPositions)
        {
            Vector3 globalBlockPos = buildintStartPos + localPos;
            int gx = Mathf.FloorToInt(globalBlockPos.x);
            int gy = Mathf.FloorToInt(globalBlockPos.y);
            int gz = Mathf.FloorToInt(globalBlockPos.z);

            if (gx < minX) minX = gx;
            if (gx > maxX) maxX = gx;
            if (gy < minY) minY = gy;
            if (gy > maxY) maxY = gy;
            if (gz < minZ) minZ = gz;
            if (gz > maxZ) maxZ = gz;
        }

        int posX = globalPos.x;
        int posY = globalPos.y;
        int posZ = globalPos.z;

        bool inside = (posX >= minX && posX <= maxX) &&
                      (posY >= minY && posY <= maxY) &&
                      (posZ >= minZ && posZ <= maxZ);

        return inside;
    }

    public Blueprint blueprint;

    public void SetBlueprint(Blueprint value)
    {
        blueprint = value;
    }

    public class Blueprint
    {
        public HashSet<Vector3> blockPositions;
        public Vector3 startPosition;

        public Blueprint(HashSet<Vector3> positions, Vector3 startPosition)
        {
            this.startPosition = startPosition;
            this.blockPositions = positions;
        }
    }

    /// <summary>
    /// Hot Fix, ��� ��� ������ ��������� �������� ���� � ��� ���� ��������� 
    /// � ������� ���\��� �����, ��������� ������ ����� ������ �����
    /// </summary>
    /// <param name="blockPos"></param>
    public IEnumerator CheckMeshToFixNavError(Vector3 blockPos)
    {
        var pos1Up = blockPos + Vector3.up;
        var pos2Up = blockPos + (Vector3.up * 2);

        var blockId = WorldGenerator.Inst.GetBlockID(blockPos);
        var id1Up = WorldGenerator.Inst.GetBlockID(pos1Up);
        var id2Up = WorldGenerator.Inst.GetBlockID(pos2Up);

        if (blockId == 0 && id1Up != 0 && id2Up != 0)
        {
            var fixGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fixGo.transform.position = pos1Up + new Vector3(-0.5f, 0.3f, 0.5f);
            fixGo.transform.localScale *= .7f;

            var chunk = WorldGenerator.Inst.GetChunk(pos1Up + new Vector3(-0.5f, -0.1f, 0.5f));
            fixGo.transform.SetParent(chunk.renderer.transform);
            fixGo.layer = chunk.renderer.gameObject.layer;

            yield return wait01;

            WorldGenerator.Inst.UpdateMesh(chunk);
            //yield return StartCoroutine(WorldGenerator.Inst.DelayableUpdateNavMesh(chunk));

            //yield return wait01;
            yield return wait01;
            yield return new WaitForSeconds(1.5f);

            Destroy(fixGo);
        }
    }
}
