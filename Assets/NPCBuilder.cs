using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Ururu
{
    // ���������, ����������� ������ ����� � ������� (��������� ������� � id �����)
    public struct BlockData
    {
        public Vector3 localPosition;
        public byte blockID;

        public BlockData(Vector3 pos, byte id)
        {
            localPosition = pos;
            blockID = id;
        }
    }

    public class NPCBuilder : MonoBehaviour
    {
        public float buildRange = 3.0f;       // ������������ ����������, �� ������� NPC ����� ���������� ����
        public float approachDistance = 1.0f; // ���������� ���������� ��� ������� � ����� ���������
        public byte scaffoldingBlockID = 1;   // ID ���������� ����� ��� ����� (scaffolding)
        public int verticalGapThreshold = 5;  // ���� ����� ������ ��� ����� ����� ������, ������ ������������ �������

        [SerializeField] TextAsset buildingData;

        PlayerBehaviour player;
        NavMeshAgent agent;
        List<BlockData> blueprint;

        private void Start()
        {
            blueprint = new List<BlockData>();
            var savedBuilding = JsonConvert.DeserializeObject<SaveBuildingData>(buildingData.text);
            foreach (var item in savedBuilding.blocksData.changedBlocks)
            {
                blueprint.Add(new BlockData() { blockID = item.blockId, localPosition = item.Pos });
            }
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
            // ������ ����� �������, ��� ����� ��������� ����� (���������� ����������)
            HashSet<Vector3> blueprintPositions = new HashSet<Vector3>();
            foreach (BlockData block in blueprint)
            {
                blueprintPositions.Add(basePosition + block.localPosition);
            }

            // ��������� ����� �� ������ (���������, ����� �����, ����� � �.�.)
            List<BlockData> orderedBlueprint = OrderBlueprint(blueprint);

            foreach (BlockData block in orderedBlueprint)
            {
                Vector3 globalPos = basePosition + block.localPosition;

                // 1. ���� �� ����� ��� ���� ����, �� ��������������� �������, ������� �����
                yield return StartCoroutine(ClearObstructionsAt(globalPos, block));

                // 2. ���� ���� �� ������ � ��� ��� ��� �����, ������������ ������
                if (block.blockID != 0 && !IsSupported(globalPos))
                {
                    yield return StartCoroutine(BuildSmartScaffolding(globalPos, blueprintPositions));
                }

                // 3. ������� ����� ������� ����� NavMesh � ������������ ����
                Vector3 approachPos = FindApproachPosition(globalPos);
                yield return StartCoroutine(MoveToPosition(approachPos));

                // 4. ���� NPC ���������� ������, ������������� ����
                if (Vector3.Distance(transform.position, globalPos) <= buildRange)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(globalPos, block.blockID);
                }
                else
                {
                    Debug.LogWarning("NPC �� ���� ������� ���������� ������ ��� ��������� �����: " + globalPos);
                }

                // �������� ��� ��������� �������������
                yield return new WaitForSeconds(0.2f);
            }

           
            // � ����� ������ BuildHouse, ����� ����� ���������� ��������� ���� ������:
            GetBuildingBounds(blueprint, basePosition, out Vector3 buildingCenter, out float buildingRadius);
            Vector3 exitPos = FindExitPoint(buildingCenter, buildingRadius, 5f);
            Debug.Log("������ ����� �� ����������: " + exitPos);
            // ���� NPC ��������� �� �����, �� ������� �������� �������� ��� ������:
            yield return StartCoroutine(EnsureDescentLadder(exitPos));
            // ����� ������������ � ��������� ����� ������:
            yield return StartCoroutine(MoveToPosition(exitPos));
        }

        // ���������� ������ �� ������ (�� ������ � ������)
        private List<BlockData> OrderBlueprint(List<BlockData> blueprint)
        {
            return blueprint.OrderBy(b => b.localPosition.y).ToList();
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

        // ��������: ������� �� ��������� ��� �������� ��������
        private bool IsSupported(Vector3 globalPos)
        {
            Vector3 belowPos = globalPos + Vector3.down;
            return WorldGenerator.Inst.GetBlockID(belowPos) != 0;
        }

        // ���������� ������������ ����� (���������� ������ ������) ��� �������� �� ��������� �����
        private int GetVerticalGap(Vector3 pos)
        {
            int gap = 0;
            Vector3 checkPos = pos + Vector3.down;
            // ������������ ����� 100 �������, ����� �������� ������������ �����
            while (WorldGenerator.Inst.GetBlockID(checkPos) == 0 && gap < 100)
            {
                gap++;
                checkPos += Vector3.down;
            }
            return gap;
        }

        // ���������� ������ �������� ���������� (scaffolding)
        // ���� ����� �������, �������� ������������ �������, ����� � ������������ ��������
        private IEnumerator BuildSmartScaffolding(Vector3 targetPos, HashSet<Vector3> blueprintPositions)
        {
            int gap = GetVerticalGap(targetPos);
            if (gap <= verticalGapThreshold)
            {
                // ������������ �������: ��� ���� �� targetPos
                Vector3 scaffoldPos = targetPos + Vector3.down;
                while (true)
                {
                    // ���� ������ ������� ������������� � �������, �� ������ scaffolding
                    if (blueprintPositions.Contains(scaffoldPos))
                        break;

                    WorldGenerator.Inst.SetBlockAndUpdateChunck(scaffoldPos, scaffoldingBlockID);
                    yield return null;

                    // ���� ��� ��������� ������ ��� ���� �����, ��������� ����������
                    if (WorldGenerator.Inst.GetBlockID(scaffoldPos + Vector3.down) != 0)
                        break;

                    scaffoldPos += Vector3.down;
                }
            }
            else
            {
                // ������������ ��������: �������� ����������� ����������� ��� ����������
                Vector3 chosenDir = DetermineStairDirection(targetPos, gap);
                if (chosenDir == Vector3.zero)
                {
                    chosenDir = Vector3.forward;
                }
                Vector3 scaffoldPos = targetPos;
                while (true)
                {
                    scaffoldPos += (chosenDir + Vector3.down);

                    // ���� ������� scaffoldPos ��������������� ��� ������� ���� � ���������� ������������� �����
                    if (blueprintPositions.Contains(scaffoldPos))
                        break;

                    WorldGenerator.Inst.SetBlockAndUpdateChunck(scaffoldPos, scaffoldingBlockID);
                    yield return null;

                    if (WorldGenerator.Inst.GetBlockID(scaffoldPos + Vector3.down) != 0)
                        break;
                }
            }
        }

        // ����������� ������������ ��������������� ����������� ��� ������������� ������������ ��������
        private Vector3 DetermineStairDirection(Vector3 startPos, int gap)
        {
            Vector3 bestDir = Vector3.zero;
            float bestScore = float.MaxValue;
            List<Vector3> directions = new List<Vector3>
            {
                Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
                (Vector3.forward + Vector3.right).normalized,
                (Vector3.forward + Vector3.left).normalized,
                (Vector3.back + Vector3.right).normalized,
                (Vector3.back + Vector3.left).normalized
            };

            foreach (Vector3 dir in directions)
            {
                Vector3 simPos = startPos;
                int steps = 0;
                // ��������� ������������� �� ��������� �� ���������� �����
                while (steps < gap)
                {
                    simPos += (dir + Vector3.down);
                    if (WorldGenerator.Inst.GetBlockID(simPos) != 0)
                    {
                        if (steps < bestScore)
                        {
                            bestScore = steps;
                            bestDir = dir;
                        }
                        break;
                    }
                    steps++;
                }
            }
            return bestDir;
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

        // ����������� NPC � �������� ������� � �������������� NavMeshAgent � ���������� � ������������
        private IEnumerator MoveToPosition(Vector3 destination)
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            agent.SetDestination(destination);
            float timeout = 5f;
            float timer = 0f;
            while (agent.pathPending || agent.remainingDistance > approachDistance)
            {
                timer += Time.deltaTime;
                if (timer > timeout)
                {
                    Debug.LogWarning("MoveToPosition: ������� ��� ������� ��������� �� " + destination);
                    // ����� �������� ����� ������ �� ���������� ��������� ��������
                    break;
                }
                yield return null;
            }
        }

        // ����� ��� ���������� ��������� ��������� (bounding box) � ������ ���������
        private void GetBuildingBounds(List<BlockData> blueprint, Vector3 basePosition, out Vector3 buildingCenter, out float buildingRadius)
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
            Vector3 size = maxPos - minPos;
            // ��� ������ ��� ���������� ������ �������������� �������
            buildingRadius = Mathf.Max(size.x, size.z) * 0.5f;
        }

        // ����� ����� ������ �� NavMesh �� ��������� ���������, ������ �� ������ � ������� ���������
        private Vector3 FindExitPoint(Vector3 buildingCenter, float buildingRadius, float safeDistance = 5f)
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
                    return hit.position;
                }
            }
            // ���� �� ����� ���������� �����, ���������� ����� ���������
            return buildingCenter;
        }

        // ����� �����, ������� ������ �������� ��� ������, ���� NPC ��������� ���� ����� ������:
        private IEnumerator EnsureDescentLadder(Vector3 exitPoint)
        {
            Debug.Log("�������� ������������� ������������� �������� ��� ������.");
            // ���� ������� �� ������ ������ 1 �����, ������ ���������:
            while (transform.position.y - exitPoint.y > 1f)
            {
                Vector3 nextStep = transform.position + Vector3.down;
                // ���� ��� NPC �����, ������ ��������� ���� ��� ��������:
                if (WorldGenerator.Inst.GetBlockID(nextStep) == 0)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(nextStep, scaffoldingBlockID);
                    Debug.Log("���������� ���� �������� �� " + nextStep);
                }
                // ���������� NPC �� ��������� ��� (� ��������� ��������� �����):
                yield return StartCoroutine(MoveToPosition(nextStep));
                yield return new WaitForSeconds(0.1f);
            }
            Debug.Log("�������� ��� ������ ���������, NPC ������ ����.");
            yield return null;
        }
    }
}

