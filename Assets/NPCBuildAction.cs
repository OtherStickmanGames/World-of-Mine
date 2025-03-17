using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Ururu
{
    public class NPCBuildAction : MonoBehaviour
    {
        public float buildRange = 3.0f;       // ������������ ����������, �� ������� NPC ����� ���������� ����
        public float approachDistance = 1.0f; // ���������� ���������� ��� ������� � ����� ���������
        public byte scaffoldingBlockID = 1;   // ID ���������� ����� ��� ����� (scaffolding)
        public int verticalGapThreshold = 5;  // ���� ����� ������ ��� ����� ����� ������, ������ ������������ �������

        [SerializeField] TextAsset buildingData;

        PlayerBehaviour player;
        NavMeshAgent agent;
        List<BlockData> blueprint;

        private Vector3 currentBuildingBasePosition;
        private HashSet<Vector3> currentBlueprintPositions;
        // ����� ��������� ���� ��� �������� ������� �������





        private void Start()
        {
            blueprint = new List<BlockData>();
            var savedBuilding = JsonConvert.DeserializeObject<SaveBuildingData>(buildingData.text);
            foreach (var item in savedBuilding.blocksData.changedBlocks)
            {
                blueprint.Add(new BlockData() { blockID = item.blockId, localPosition = item.Pos });
            }
            blueprint = BlockUtils.FillBoundingBox(blueprint);

            Debug.Log("������ � �������: " + blueprint.Count);
        }

        private void Update()
        {
            player ??= FindObjectOfType<PlayerBehaviour>();
            agent ??= GetComponent<NavMeshAgent>();

            if (Input.GetKeyDown(KeyCode.J))
            {
                StartCoroutine(Async());

                IEnumerator Async()
                {
                    agent.enabled = false;
                    transform.position = player.transform.position + (player.transform.forward * 3) + (Vector3.up * 3);
                    yield return null;
                    agent.enabled = true;
                }
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                agent.SetDestination(player.transform.position + player.transform.forward);
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                // ��������� �����, ������ ����� ������������� (����� ������ �� ������ ����)
                var playerNearPos = player.transform.position + player.transform.forward + Vector3.up;
                StartCoroutine(BuildHouse(playerNearPos, blueprint));
            }
        }

        // ������� ����� ������������� ���� �� ������� (blueprint)
        public IEnumerator BuildHouse(Vector3 basePosition, List<BlockData> blueprint)
        {
            currentBuildingBasePosition = basePosition; // ��������� ������� ������� ��� ������� ��������� ���������


            // ������ ����� �������, ��� ����� ��������� ����� (���������� ����������)
            HashSet<Vector3> blueprintPositions = new HashSet<Vector3>();
            foreach (BlockData block in blueprint)
            {
                blueprintPositions.Add(basePosition + block.localPosition);
            }
            currentBlueprintPositions = blueprintPositions; // ��������� ��� ������ ����

           

            // ��������� ����� �� ������ (���������, ����� �����, ����� � �.�.)
            List<BlockData> orderedBlueprint = OrderBlueprint(blueprint);

            foreach (BlockData block in orderedBlueprint)
            {
                Vector3 globalPos = basePosition + block.localPosition;

                // 1. ���� �� ����� ��� ���� ����, �� ��������������� �������, ������� �����
                yield return StartCoroutine(ClearObstructionsAt(globalPos, block));

                //// 2. ���� ���� �� ������ � ��� ��� ��� �����, ������������ ������
                //if (block.blockID != 0 && !IsSupported(globalPos))
                //{
                //    yield return StartCoroutine(BuildSmartScaffolding(globalPos, blueprintPositions));
                //}

                // 3. ������� ����� ������� ����� NavMesh � ������������ ����
                Vector3 approachPos = FindApproachPosition(globalPos);
                //if (approachPos == globalPos)
                //{
                    
                //    yield return StartCoroutine(BuildSmartScaffolding(globalPos, blueprintPositions));

                //}
                yield return StartCoroutine(MoveToPosition(approachPos));

                // 4. ���� NPC ���������� ������, ������������� ����
                if (Vector3.Distance(transform.position, globalPos) <= buildRange)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(globalPos, block.blockID);
                }
                else
                {
                    
                    Debug.Log("NPC �� ���� ������� ���������� ������ ��� ��������� �����: " + globalPos);
                }

                // �������� ��� ��������� �������������
                yield return new WaitForSeconds(0.2f);
            }

        }

        // ���� � ������� ������� ��� ���� ����, �� �������� � ������, ������� ���
        private IEnumerator ClearObstructionsAt(Vector3 globalPos, BlockData targetBlock)
        {
            byte currentID = WorldGenerator.Inst.GetBlockID(globalPos);
            if (currentID != 0 && currentID != targetBlock.blockID)
            {
                WorldGenerator.Inst.SetBlockAndUpdateChunck(globalPos, 0);
                yield return null;
            }
        }

        // ����� ����� ������� �� NavMesh, � �������� buildRange �� ������� �������
        private Vector3 FindApproachPosition(Vector3 targetPos)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, buildRange, NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return targetPos;
        }

        // ���������� ������ �� ������ (�� ������ � ������)
        private List<BlockData> OrderBlueprint(List<BlockData> blueprint)
        {
            return blueprint.OrderBy(b => b.localPosition.y).ToList();
        }

        private IEnumerator BuildLadderForBlock(Vector3 destination)
        {
            // ��������� �������� ��������� �� ������ blueprint � ������� �������
            GetBuildingBounds(blueprint, currentBuildingBasePosition, out Vector3 buildingCenter, out float buildingRadius, out var size);

            var edge = GetClosestEdge(currentBuildingBasePosition, size, destination);

            print(edge + " =-=-=-=-=");

            // ���� ����� ������ �� ��������� ��������� (safeDistance = 1, ����� �������� ���� ���������� � ���������)
            Vector3 ladderBase = FindExitPoint(buildingCenter, buildingRadius, 1f);
            
            // ��������� ���������� ���� �������� (�� ������ ��������)
            ladderBase = new Vector3(
                Mathf.FloorToInt(ladderBase.x),
                Mathf.FloorToInt(ladderBase.y),
                Mathf.FloorToInt(ladderBase.z)
            );

            switch (edge)
            {
                case Edge.Left:
                    ladderBase = destination + Vector3.left;
                    break;
                case Edge.Right:
                    ladderBase = destination + Vector3.right;
                    break;
                case Edge.Front:
                    ladderBase = destination + Vector3.forward;
                    break;
                case Edge.Back:
                    ladderBase = destination + Vector3.back;
                    break;
            }

            ladderBase += Vector3.down;

            ladderBase.x = Mathf.FloorToInt(ladderBase.x);
            ladderBase.y = Mathf.FloorToInt(ladderBase.y);
            ladderBase.z = Mathf.FloorToInt(ladderBase.z);

            Debug.Log("�������� ����� ��������� � ����� (���������): " + ladderBase);

            var isUpLadder = transform.position.y - 1 < ladderBase.y;
            scaffoldingBlockID = isUpLadder ? (byte)92 : (byte)61;

            // �������� ������� �������� ����� �����
            float startY = Mathf.Min(transform.position.y-1, ladderBase.y); // ����� ����������� ������� (�� ������, ���� ����� ������� � ����)
            float endY = Mathf.Max(transform.position.y-1, ladderBase.y);  // ����� ������������ ������� (���� ����� ���������)

            bool placedAnyBlocks = false;

            float currentY = Mathf.Floor(startY); // �������� � ���������� ������� ������

            var height = Mathf.RoundToInt(endY - currentY);
            Vector3 startLadderPos = new Vector3(0, isUpLadder ? currentY : endY, 0);
            Vector3 dir = Vector3.forward;

            if (edge is Edge.Left or Edge.Right)
            {
                if (isUpLadder)
                {
                    startLadderPos.x = ladderBase.x;
                    startLadderPos.z = ladderBase.z + height;
                }
                else
                {
                    startLadderPos.x = ladderBase.x;
                    startLadderPos.z = ladderBase.z;
                    ladderBase.z += height;
                }
            }
            if (edge is Edge.Front or Edge.Back)
            {
                dir = Vector3.right;
                if (isUpLadder)
                {
                    startLadderPos.x = ladderBase.x + height;
                    startLadderPos.z = ladderBase.z;
                }
                else
                {
                    startLadderPos.x = ladderBase.x;
                    startLadderPos.z = ladderBase.z;
                    ladderBase.x += height;
                }
            }

            dir.y = (transform.position.y - 1) < ladderBase.y ? -1 : 1;

            while (Vector3.Distance(ladderBase, startLadderPos) > 0.3f)
            {
                if (WorldGenerator.Inst.GetBlockID(startLadderPos) == 0)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(startLadderPos, scaffoldingBlockID);
                    Debug.Log("���������� ���� �������� �� " + startLadderPos);
                    placedAnyBlocks = true;
                }

                // ���������� NPC � ���������� ���� ��������
                Vector3 nextStepPos = startLadderPos - dir;//new Vector3(ladderBase.x, currentY + 1f, ladderBase.z);
                yield return StartCoroutine(MoveToPosition(nextStepPos, false));
                yield return new WaitForSeconds(0.5f); // �������� ����� ������

                startLadderPos -= dir;
            }

            if (placedAnyBlocks)
            {
                Debug.Log("�������� ���������, NPC ����� ��������� �� " + destination);
            }
            else
            {
                Debug.Log("�������� �� �����������: ���� ��� �������� ��� NPC �� ������ ������.");
            }

            yield return null;

            //WorldGenerator.Inst.SetBlockAndUpdateChunck(ladderBase, 61);
            //yield break;
        }

        public Edge GetClosestEdge(Vector3 buildingPosition, Vector3 size, Vector3 destination)
        {
            // ������� ���������, �������� ����������� � ������������ ���������� �� ���� X, Y, Z
            Vector3 halfSize = size / 2;

            // ���������� ������� ���������
            Vector3 minBounds = buildingPosition;
            Vector3 maxBounds = buildingPosition + size;

            // ��������� ���������� �� ������ �������
            float distanceToLeft = Mathf.Abs(destination.x - minBounds.x);
            float distanceToRight = Mathf.Abs(destination.x - maxBounds.x);
            float distanceToFront = Mathf.Abs(destination.z - maxBounds.z);
            float distanceToBack = Mathf.Abs(destination.z - minBounds.z);
            float distanceToTop = Mathf.Abs(destination.y - maxBounds.y);
            float distanceToBottom = Mathf.Abs(destination.y - minBounds.y);

            // ������� ����������� ���������� � ���������� ��������������� ����
            //float minDistance = Mathf.Min(distanceToLeft, distanceToRight, distanceToFront, distanceToBack, distanceToTop, distanceToBottom);
            float minDistance = Mathf.Min(distanceToLeft, distanceToRight, distanceToFront, distanceToBack);


            if (FloatEquels(minDistance, distanceToLeft))
                return Edge.Left;
            else if (FloatEquels(minDistance, distanceToRight))
                return Edge.Right;
            else if (FloatEquels(minDistance, distanceToFront))
                return Edge.Front;
            else// if (FloatEquels(minDistance, distanceToBack))
                return Edge.Back;
            //else if (FloatEquels(minDistance, distanceToTop))
            //    return Edge.Top;
            //else
            //    return Edge.Bottom;
        }

        public bool FloatEquels(float a, float b)
        {
            return Mathf.Abs(a - b) < 0.001f;
        }
            

        public enum Edge
        {
            Left,
            Right,
            Front,
            Back,
            Top,
            Bottom
        }

        //private IEnumerator BuildLadderForBlock(Vector3 destination)
        //{
        //    // 1. �������� ������� ���������
        //    GetBuildingBounds(blueprint, currentBuildingBasePosition, out Vector3 buildingCenter, out float buildingRadius);

        //    // 2. ���� ��������� ����� � ��������� (ladderBase), ����� �������� � ��� ���
        //    Vector3 ladderBase = FindExitPoint(buildingCenter, buildingRadius, 1f);

        //    yield break;

        //    ladderBase = new Vector3(
        //        Mathf.Round(ladderBase.x),
        //        Mathf.Round(ladderBase.y),
        //        Mathf.Round(ladderBase.z)
        //    );

        //    Debug.Log($"���� �������� (destination): {destination}, ����� � ��������� (ladderBase): {ladderBase}");

        //    // 3. ��������� ��������� �����, ��������, ��� �� �������� ������� �������� ���� �������� ��������� ������
        //    Vector3 currentPos = new Vector3(
        //        Mathf.Round(transform.position.x),
        //        Mathf.Round(transform.position.y) - 1, // �� 1 ���� ����, ����� ��������� � �����
        //        Mathf.Round(transform.position.z)
        //    );

        //    // 4. ���� ��� �� ����� - �� ������
        //    if (currentPos == ladderBase)
        //    {
        //        Debug.Log("�������� �� ��������� - ��� �� �������.");
        //        yield break;
        //    }

        //    // 5. ������ � ������� ����, �� ������ ����� �� ladderBase
        //    int stepX = ladderBase.x > currentPos.x ? 1 : (ladderBase.x < currentPos.x ? -1 : 0);
        //    int stepZ = ladderBase.z > currentPos.z ? 1 : (ladderBase.z < currentPos.z ? -1 : 0);
        //    int stepY = ladderBase.y > currentPos.y ? 1 : -1;

        //    // 6. ��������� ������� ������ � ������ ��������
        //    bool placedAnyBlocks = false;

        //    while (currentPos.y != ladderBase.y)
        //    {
        //        // ������� ������� ������� ���������
        //        Vector3 ladderBlockPos = new Vector3(
        //            Mathf.Round(currentPos.x),
        //            Mathf.Round(currentPos.y),
        //            Mathf.Round(currentPos.z)
        //        );

        //        // ���������, �� ������� �� ������� ����� �� ������� ������
        //        if (Mathf.Abs(ladderBlockPos.x - buildingCenter.x) > buildingRadius ||
        //            Mathf.Abs(ladderBlockPos.z - buildingCenter.z) > buildingRadius)
        //        {
        //            Debug.Log("��������� ������� �� ������� ������, ���������� ����������");
        //            break;
        //        }

        //        // ���� ���� ������, ������ ��������
        //        if (WorldGenerator.Inst.GetBlockID(ladderBlockPos) == 0)
        //        {
        //            WorldGenerator.Inst.SetBlockAndUpdateChunck(ladderBlockPos, scaffoldingBlockID);
        //            Debug.Log($"��������� ���� �������� �� {ladderBlockPos}");
        //            placedAnyBlocks = true;
        //        }

        //        // ��������� �� ��������
        //        yield return StartCoroutine(MoveToPosition(ladderBlockPos, false));
        //        yield return new WaitForSeconds(0.1f);

        //        // ����������� �� ��� �� Y
        //        currentPos.y += stepY;

        //        // ���� ��������� ���� target �� ������, ��������� �� ���������
        //        if (Mathf.Abs(ladderBase.x - currentPos.x) > Mathf.Abs(ladderBase.z - currentPos.z))
        //        {
        //            currentPos.x += stepX;  // ������ �� X
        //        }
        //        else
        //        {
        //            currentPos.z += stepZ;  // ������ �� Z
        //        }

        //        // ��������� ������������
        //        if (Mathf.Abs(currentPos.y - ladderBase.y) < 0.1f) currentPos.y = ladderBase.y;
        //        if (Mathf.Abs(currentPos.x - ladderBase.x) < 0.1f) currentPos.x = ladderBase.x;
        //        if (Mathf.Abs(currentPos.z - ladderBase.z) < 0.1f) currentPos.z = ladderBase.z;
        //    }

        //    if (placedAnyBlocks)
        //    {
        //        Debug.Log($"�������� ������� ��������� �� {ladderBase}");
        //    }
        //    else
        //    {
        //        Debug.Log("�������� �� �����������, ���� ��������.");
        //    }


        //}

        private IEnumerator MoveToPosition(Vector3 destination, bool canBuildLadder = true)
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();

            NavMeshPath path = new NavMeshPath();
            agent.CalculatePath(destination, path);

            if (canBuildLadder && path.status != NavMeshPathStatus.PathComplete)
            {
                Debug.Log($"MoveToPosition: ���� �� {destination} �� ������ ����� NavMesh (PathComplete = {path.status}). ��������� ���������� scaffolding.");
                yield return StartCoroutine(BuildPathScaffolding(destination));
                yield return StartCoroutine(MoveToPosition(destination, false));
                yield break;
            }

            agent.SetPath(path);

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
        }


        //private IEnumerator MoveToPosition(Vector3 destination, bool canBuildLadder = true)
        //{
        //    NavMeshAgent agent = GetComponent<NavMeshAgent>();

        //    // ������� ������� ��������� ����
        //    NavMeshPath path = new NavMeshPath();
        //    agent.CalculatePath(destination, path);

        //    // ���� ���� �� ������ � �� ��� �� ��������� ������� �������� � ������
        //    if (canBuildLadder && path.status != NavMeshPathStatus.PathComplete)
        //    {
        //        Debug.Log($"MoveToPosition: ���� �� {destination} �� ������ (PathComplete = {path.status}). �������� ��������� ��������.");
        //        yield return StartCoroutine(BuildLadderForBlock(destination));

        //        // ����� ������������� �������� ������� ��� ���, �� ��� ��� ���������� �������������
        //        yield return StartCoroutine(MoveToPosition(destination, false));
        //        yield break;
        //    }

        //    // ������������� ����
        //    agent.SetPath(path);

        //    // ��������� ���������� ��� �������� "�����������"
        //    float noMovementTimeout = 5f;       // �����, ����� �������� �������, ��� NPC �������� ��������� (�� ���������)
        //    float noProgressTimeout = 5f;       // �����, ����� �������� �������, ��� NPC �������� �� ��������� (��������, �� �� ���������� �����)
        //    float stuckTimer = 0f;             // ������� ��� ����������� �����������
        //    float progressTimer = 0f;          // ������� ��� ���������� ���������
        //    Vector3 lastPosition = agent.transform.position;
        //    float lastDistanceToDest = (path.corners.Length > 0)
        //        ? Vector3.Distance(agent.transform.position, path.corners[path.corners.Length - 1])
        //        : Vector3.Distance(agent.transform.position, destination);

        //    // ���� ��������, ���� ����� �� ��������� ����
        //    while (agent.pathPending || agent.remainingDistance > approachDistance)
        //    {
        //        // 1) �������� ������������ �������� (�� ����� �� ����� �� �����)
        //        float distanceMoved = Vector3.Distance(agent.transform.position, lastPosition);
        //        bool isMoving = distanceMoved > 0.01f;
        //        if (!isMoving)
        //        {
        //            stuckTimer += Time.deltaTime;
        //            if (stuckTimer > noMovementTimeout)
        //            {
        //                Debug.Log($"MoveToPosition: ����� ��������� ������� � {agent.transform.position}, �� ��������� � {destination}.");

        //                // ���� ����� ������� �������� � �������
        //                if (canBuildLadder)
        //                {
        //                    yield return StartCoroutine(BuildLadderForBlock(destination));
        //                    yield return StartCoroutine(MoveToPosition(destination, false));
        //                }
        //                yield break;
        //            }
        //        }
        //        else
        //        {
        //            stuckTimer = 0f;
        //        }

        //        // 2) �������� ���������� (����������� �� ���������� �� �������� �����)
        //        float currentDistanceToDest = agent.remainingDistance; // ��� ��������� �� path.corners
        //        if (currentDistanceToDest >= lastDistanceToDest - 0.05f)
        //        {
        //            // ���������� �� ����������� (��� ���� �����������)
        //            progressTimer += Time.deltaTime;
        //            if (progressTimer > noProgressTimeout)
        //            {
        //                Debug.Log($"MoveToPosition: ����� �� ������������ � {destination}, ������� ���������� = {currentDistanceToDest}");

        //                // ���� ����� ������� �������� � �������
        //                if (canBuildLadder)
        //                {
        //                    yield return StartCoroutine(BuildLadderForBlock(destination));
        //                    yield return StartCoroutine(MoveToPosition(destination, false));
        //                }
        //                yield break;
        //            }
        //        }
        //        else
        //        {
        //            // ���� �������� � ���������� ������
        //            progressTimer = 0f;
        //        }
        //        lastDistanceToDest = currentDistanceToDest;
        //        lastPosition = agent.transform.position;

        //        yield return null;
        //    }
        //}


        private void GetBuildingBounds(List<BlockData> blueprint, Vector3 basePosition, out Vector3 buildingCenter, out float buildingRadius, out Vector3 size)
        {
            Vector3 minPos = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxPos = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var block in blueprint)
            {
                Vector3 globalPos = basePosition + block.localPosition;
                minPos = Vector3.Min(minPos, globalPos);
                maxPos = Vector3.Max(maxPos, globalPos);
            }
            buildingCenter = (minPos + maxPos) * 0.5f;
            size = (maxPos + Vector3.one) - minPos;
            buildingRadius = Mathf.Max(size.x, size.z) * 0.5f;
        }

        private Vector3 FindExitPoint(Vector3 buildingCenter, float buildingRadius, float safeDistance)
        {
            const int tries = 16;
            float stepAngle = 360f / tries;
            float searchRadius = buildingRadius + safeDistance;
            for (int i = 0; i < tries; i++)
            {
                float angle = stepAngle * i;
                Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad));
                Vector3 candidate = buildingCenter + dir * searchRadius;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(candidate, out hit, safeDistance, NavMesh.AllAreas))
                {
                    return hit.position + Vector3.right;
                }
            }
            return buildingCenter;
        }

        private IEnumerator BuildPathScaffolding(Vector3 destination)
        {
            // �������� ������������� ������� ������ � ����
            Vector3Int agentPos = new Vector3Int(
                Mathf.FloorToInt(transform.position.x),
                Mathf.FloorToInt(transform.position.y-1.1f),
                Mathf.FloorToInt(transform.position.z)
            );
            Vector3Int destPos = new Vector3Int(
                Mathf.FloorToInt(destination.x),
                Mathf.FloorToInt(destination.y),// !!!!!!
                Mathf.FloorToInt(destination.z)
            );

            // ������� ��� ������� �� ���� ���� ����
            //agentPos.y -= 1;
            //destPos.y -= 1;

            List<Vector3Int> path = null;
            if (agentPos.y != destPos.y)
            {
                Debug.Log("������ ���������� � ���� ���� ����������� ����� AStarPath3D.");
                yield return StartCoroutine(AStarPath3DCoroutine(agentPos, destPos, currentBlueprintPositions, result => path = result));
            }
            else
            {
                Debug.Log("������ ��������� � ���� �������������� ���� ��� �����.");
                yield return StartCoroutine(AStarPathCoroutine(agentPos, destPos, currentBlueprintPositions, result => path = result));
            }

            if (path == null)
            {
                Debug.Log("�� ������� ����� ���� ��� scaffolding.");
                yield break;
            }

            Debug.Log("������ ���� ��� scaffolding, �����: " + path.Count);
            foreach (Vector3Int cell in path)
            {
                // ���� � ������ ����� � ������ scaffolding-����
                if (WorldGenerator.Inst.GetBlockID(cell) == 0)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(cell, scaffoldingBlockID);
                    Debug.Log("��������� scaffolding ���� �� " + cell);
                    //yield return StartCoroutine(MoveToPosition(cell, false));
                    yield return new WaitForSeconds(0.1f);
                }
                yield return null;
            }

            yield return new WaitForSeconds(1f);

            // ����� ���������� scaffolding, ������������ � ����,
            // ��������� ����� �� ���� ���� ����
            Vector3 destinationOffset = destination + Vector3.down;
            yield return StartCoroutine(MoveToPosition(destinationOffset, false));
        }


        private IEnumerator BuildBridgeToPoint(Vector3Int start, Vector3Int goal)
        {
            // ��� ����� ��������� ������ start.y
            Vector3Int s = new Vector3Int(start.x, start.y, start.z);
            Vector3Int g = new Vector3Int(goal.x, start.y, goal.z);

            List<Vector3Int> path = null;
            yield return StartCoroutine(AStarPathCoroutine(s, g, currentBlueprintPositions, result => path = result));

            if (path == null)
            {
                Debug.Log("�� ������� ����� �������������� ���� ��� �����.");
                yield break;
            }

            Debug.Log("�������������� ���� ������ ��� �����, �����: " + path.Count);
            foreach (Vector3Int cell in path)
            {
                if (WorldGenerator.Inst.GetBlockID(cell) == 0)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(cell, scaffoldingBlockID);
                    Debug.Log("��������� ���� ����� �� " + cell);
                    yield return new WaitForSeconds(0.1f);
                }
                yield return null;
            }

            Debug.Log("���� �������� �� " + s + " �� " + g);
        }


        private IEnumerator BuildStairsToPoint(Vector3Int start, Vector3Int goal)
        {
            Vector3Int current = start;
            // ���������� ����������� �� Y: ���� ����� ���� ����, ����� ����������, ����� �����������
            int verticalStep = (current.y > goal.y) ? -1 : 1;

            int maxSteps = 100;
            int steps = 0;

            while ((current.x != goal.x || current.z != goal.z || current.y != goal.y) && steps < maxSteps)
            {
                // ��������� �������������� ����������� �� current � goal
                int dx = goal.x - current.x;
                int dz = goal.z - current.z;
                int stepX = (dx == 0) ? 0 : (dx > 0 ? 1 : -1);
                int stepZ = (dz == 0) ? 0 : (dz > 0 ? 1 : -1);

                // ��� �������� ����� �������� ��������� �����������: �������������� �������� + ������������ ���������
                Vector3Int next = new Vector3Int(current.x + stepX, current.y + verticalStep, current.z + stepZ);

                // ���� �� �����-���� ��� ������� ����� ����, ��������� ��� ��������
                if (dx == 0) next.x = current.x;
                if (dz == 0) next.z = current.z;

                // ���� ��������� ��� ������ � ������ ���������, ��������� ������ �������������� �����
                Vector3 nextF = new Vector3(next.x, next.y, next.z);
                if (currentBlueprintPositions.Contains(nextF))
                {
                    Vector3Int alt = new Vector3Int(current.x + stepX, current.y, current.z + stepZ);
                    next = alt;
                }

                // ������ ����, ���� ������ �����
                if (WorldGenerator.Inst.GetBlockID(next) == 0)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(next, scaffoldingBlockID);
                    Debug.Log("���������� ���� ��������� �� " + next);
                    yield return new WaitForSeconds(0.1f);
                }

                current = next;
                steps++;
                yield return null;
            }

            Debug.Log("��������� ��������� �� " + start + " �� " + goal);
        }

        private IEnumerator AStarPath3DCoroutine(Vector3Int start, Vector3Int goal, HashSet<Vector3> blueprintPositions, System.Action<List<Vector3Int>> callback)
        {
            // ����������� �����������: ��� ���������� dx,dy,dz �� {-1,0,1}, ����� (0,0,0)
            // � ��������� ����� ������������ ���� (dy != 0, dx==0 � dz==0), ����� ����� �� �������� ������ �� ���������.
            List<Vector3Int> allowedDirections = new List<Vector3Int>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0)
                            continue;
                        if (dy != 0 && dx == 0 && dz == 0)
                            continue;
                        allowedDirections.Add(new Vector3Int(dx, dy, dz));
                    }
                }
            }

            Dictionary<Vector3Int, Node> openSet = new Dictionary<Vector3Int, Node>();
            HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();

            Node startNode = new Node(start);
            startNode.gCost = 0;
            startNode.hCost = ManhattanDistance(start, goal);
            openSet.Add(start, startNode);

            int iterations = 0;
            int maxIterations = 10000;
            while (openSet.Count > 0)
            {
                iterations++;
                if (iterations % 50 == 0)
                    yield return null; // ��� ����� ��������

                if (iterations > maxIterations)
                {
                    Debug.LogWarning("AStarPath3DCoroutine: ��������� �������� ��������, ��������� ����.");
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
                    callback(path);
                    yield break;
                }

                openSet.Remove(current.position);
                closedSet.Add(current.position);

                foreach (var dir in allowedDirections)
                {
                    Vector3Int neighborPos = current.position + dir;
                    if (closedSet.Contains(neighborPos))
                        continue;

                    Vector3 neighborF = new Vector3(neighborPos.x, neighborPos.y, neighborPos.z);
                    // ���� ������ �� ������ � blueprint � ������ (�� �����), ���������� �
                    if (!blueprintPositions.Contains(neighborF) && WorldGenerator.Inst.GetBlockID(neighborPos) != 0)
                        continue;

                    // ���������, ��� ��� ������� �������� ��� ������
                    if (WorldGenerator.Inst.GetBlockID(neighborPos + Vector3Int.up) != 0 ||
                        WorldGenerator.Inst.GetBlockID(neighborPos + Vector3Int.up * 2) != 0)
                        continue;

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


                //foreach (var dir in allowedDirections)
                //{
                //    Vector3Int neighborPos = current.position + dir;
                //    if (closedSet.Contains(neighborPos))
                //        continue;

                //    // ���� ������ ������ ��������, ���������� �
                //    Vector3 neighborFloat = new Vector3(neighborPos.x, neighborPos.y, neighborPos.z);
                //    if (blueprintPositions.Contains(neighborFloat))
                //        continue;

                //    // ��������� ������������: ������ � ������ ������ ������ ���� �������
                //    if (WorldGenerator.Inst.GetBlockID(neighborPos) != 0 ||
                //        WorldGenerator.Inst.GetBlockID(neighborPos + Vector3Int.up) != 0
                //        || WorldGenerator.Inst.GetBlockID(neighborPos + (Vector3Int.up *2)) != 0)
                //        continue;

                //    float tentativeG = current.gCost + 1f;
                //    Node neighbor;
                //    if (openSet.TryGetValue(neighborPos, out neighbor))
                //    {
                //        if (tentativeG < neighbor.gCost)
                //        {
                //            neighbor.gCost = tentativeG;
                //            neighbor.parent = current;
                //        }
                //    }
                //    else
                //    {
                //        neighbor = new Node(neighborPos);
                //        neighbor.gCost = tentativeG;
                //        neighbor.hCost = ManhattanDistance(neighborPos, goal);
                //        neighbor.parent = current;
                //        openSet.Add(neighborPos, neighbor);
                //    }
                //}
            }
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


        //private class Node
        //{
        //    public Vector3Int position;
        //    public float gCost;
        //    public float hCost;
        //    public float fCost { get { return gCost + hCost; } }
        //    public Node parent;

        //    public Node(Vector3Int pos) { position = pos; }
        //}

        //private IEnumerator AStarPathCoroutine(Vector3Int start, Vector3Int goal, HashSet<Vector3> blueprintPositions, System.Action<List<Vector3Int>> callback)
        //{
        //    List<Vector3Int> directions = new List<Vector3Int>
        //    {
        //        new Vector3Int(1, 0, 0),
        //        new Vector3Int(-1, 0, 0),
        //        new Vector3Int(0, 0, 1),
        //        new Vector3Int(0, 0, -1)
        //    };

        //    Dictionary<Vector3Int, Node> openSet = new Dictionary<Vector3Int, Node>();
        //    HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();

        //    Node startNode = new Node(start);
        //    startNode.gCost = 0;
        //    startNode.hCost = Vector3Int.Distance(start, goal);
        //    openSet.Add(start, startNode);

        //    int iterations = 0;
        //    int maxIterations = 10000;

        //    while (openSet.Count > 0)
        //    {
        //        iterations++;
        //        if (iterations % 50 == 0)
        //            yield return null; // ��� ����� ��������

        //        if (iterations > maxIterations)
        //        {
        //            Debug.LogWarning("AStarPathCoroutine: ��������� �������� ��������, ��������� ����.");
        //            callback(null);
        //            yield break;
        //        }

        //        Node current = openSet.Values.OrderBy(n => n.fCost).First();
        //        if (current.position == goal)
        //        {
        //            List<Vector3Int> path = new List<Vector3Int>();
        //            while (current != null)
        //            {
        //                path.Add(current.position);
        //                current = current.parent;
        //            }
        //            path.Reverse();
        //            callback(path);
        //            yield break;
        //        }

        //        openSet.Remove(current.position);
        //        closedSet.Add(current.position);

        //        foreach (var dir in directions)
        //        {
        //            Vector3Int neighborPos = current.position + dir;
        //            if (closedSet.Contains(neighborPos))
        //                continue;
        //            Vector3 neighborF = new Vector3(neighborPos.x, neighborPos.y, neighborPos.z);
        //            if (blueprintPositions.Contains(neighborF))
        //                continue;
        //            if (WorldGenerator.Inst.GetBlockID(neighborPos) != 0)
        //                continue;

        //            float tentativeG = current.gCost + 1f;
        //            Node neighbor;
        //            if (openSet.TryGetValue(neighborPos, out neighbor))
        //            {
        //                if (tentativeG < neighbor.gCost)
        //                {
        //                    neighbor.gCost = tentativeG;
        //                    neighbor.parent = current;
        //                }
        //            }
        //            else
        //            {
        //                neighbor = new Node(neighborPos);
        //                neighbor.gCost = tentativeG;
        //                neighbor.hCost = Vector3Int.Distance(neighborPos, goal);
        //                neighbor.parent = current;
        //                openSet.Add(neighborPos, neighbor);
        //            }
        //        }
        //    }
        //    callback(null);
        //    yield break;
        //}

        private IEnumerator AStarPathCoroutine(Vector3Int start, Vector3Int goal, HashSet<Vector3> blueprintPositions, System.Action<List<Vector3Int>> callback)
        {
            List<Vector3Int> directions = new List<Vector3Int>
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(0, 0, -1)
            };

            Dictionary<Vector3Int, Node> openSet = new Dictionary<Vector3Int, Node>();
            HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();

            Node startNode = new Node(start);
            startNode.gCost = 0;
            startNode.hCost = Vector3Int.Distance(start, goal);
            openSet.Add(start, startNode);

            int iterations = 0;
            int maxIterations = 10000;

            while (openSet.Count > 0)
            {
                iterations++;
                if (iterations % 50 == 0)
                    yield return null; // ��� ����� ��������

                if (iterations > maxIterations)
                {
                    Debug.LogWarning("AStarPathCoroutine: ��������� �������� ��������, ��������� ����.");
                    callback(null);
                    yield break;
                }

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
                    callback(path);
                    yield break;
                }

                openSet.Remove(current.position);
                closedSet.Add(current.position);

                foreach (var dir in directions)
                {
                    Vector3Int neighborPos = current.position + dir;
                    if (closedSet.Contains(neighborPos))
                        continue;

                    Vector3 neighborF = new Vector3(neighborPos.x, neighborPos.y, neighborPos.z);

                    // ���� ������ ������ � blueprint, ������� � ���������� ��� �������, ��� ��� ��� �������� 2 ������
                    if (blueprintPositions.Contains(neighborF))
                    {
                        if (WorldGenerator.Inst.GetBlockID(neighborPos + Vector3Int.up) != 0 ||
                            WorldGenerator.Inst.GetBlockID(neighborPos + Vector3Int.up * 2) != 0)
                            continue;
                    }
                    else
                    {
                        // ���� ������ �� ������ � blueprint, ��� ������ ���� ��������� ������,
                        // � ��� ��� � �������� ��� ������
                        if (WorldGenerator.Inst.GetBlockID(neighborPos) != 0)
                            continue;
                        if (WorldGenerator.Inst.GetBlockID(neighborPos + Vector3Int.up) != 0 ||
                            WorldGenerator.Inst.GetBlockID(neighborPos + Vector3Int.up * 2) != 0)
                            continue;
                    }

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
                        neighbor.hCost = Vector3Int.Distance(neighborPos, goal);
                        neighbor.parent = current;
                        openSet.Add(neighborPos, neighbor);
                    }
                }
            }
            callback(null);
            yield break;
        }



        private bool IsBlueprintCell(Vector3Int cell, HashSet<Vector3> blueprintPositions)
        {
            // �������� cell � Vector3 (�������������) � ����������
            Vector3 cellF = new Vector3(cell.x, cell.y, cell.z);
            return blueprintPositions.Contains(cellF);
        }


    }
}
