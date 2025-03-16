using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;


namespace Ururasf
{
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


            // � ����� ������ BuildHouse, ����� ����� ���������� ��������� ���� ������:
            //GetBuildingBounds(blueprint, basePosition, out Vector3 buildingCenter, out float buildingRadius);
            //Vector3 exitPos = FindExitPoint(buildingCenter, buildingRadius, 5f);
            //Debug.Log("������ ����� �� ����������: " + exitPos);
            //// ���� NPC ��������� �� �����, �� ������� �������� �������� ��� ������:
            //yield return StartCoroutine(EnsureDescentLadder(exitPos));
            //// ����� ������������ � ��������� ����� ������:
            //yield return StartCoroutine(MoveToPosition(exitPos));
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

        private IEnumerator MoveToPosition(Vector3 destination)
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            agent.SetDestination(destination);

            float timeout = 5f;
            float stuckTimer = 0f;

            // �������, ��� �������� ��������
            Vector3 lastPosition = agent.transform.position;

            while (agent.pathPending || agent.remainingDistance > approachDistance)
            {
                // ���������, ��������� �� �����
                float distanceMoved = Vector3.Distance(agent.transform.position, lastPosition);
                bool isMoving = distanceMoved > 0.01f; // ����� ��� ����������� "��������", ����� ���������

                if (!isMoving)
                {
                    stuckTimer += Time.deltaTime;

                    if (stuckTimer > timeout)
                    {
                        Debug.Log("MoveToPosition: ����� ������� ��� ������� ��������� �� " + destination);
                        // ����� ����� �������� ������ �� ��������� �������� ��� ������ �����������
                        break;
                    }
                }
                else
                {
                    stuckTimer = 0f; // ����� �������, ���� ����� ���������� ��������
                }

                lastPosition = agent.transform.position;

                yield return null;
            }
        }

        
    }
}
