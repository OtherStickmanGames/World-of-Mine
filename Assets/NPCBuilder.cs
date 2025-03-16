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

        // ������� ����� ������������� ���� �� ������� (blueprint)
        public IEnumerator BuildHouse(Vector3 basePosition, List<BlockData> blueprint)
        {
            // ��������� ����� �� ������ (������� ���������, ����� �����, ����� � �.�.)
            List<BlockData> orderedBlueprint = OrderBlueprint(blueprint);

            foreach (BlockData block in orderedBlueprint)
            {
                // ��������� ���������� ������� �����: ������� ����� + ��������� ����������
                Vector3 globalPos = basePosition + block.localPosition;

                // 1. ���� �� ����� ��������� ��� ���� ����, ������� �� ������������� �������, ������� �����
                yield return StartCoroutine(ClearObstructionsAt(globalPos, block));

                // 2. ���� ���� �� ������ � ��� ��� ��� �����, ����� ���������� ������:
                if (block.blockID != 0 && !IsSupported(globalPos))
                {
                    // ������ ����� ������� ���������� (���� ����� ������� � ��������, ����� � ������������ �������)
                    yield return StartCoroutine(BuildSmartScaffolding(globalPos));
                }

                // 3. ������� ���������� ����� ��� ������� � ������� ��������� � ������� NavMesh
                Vector3 approachPos = FindApproachPosition(globalPos);
                yield return StartCoroutine(MoveToPosition(approachPos));

                // 4. ���� NPC ���������� ������, ������������� ����
                if (Vector3.Distance(transform.position, globalPos) <= buildRange)
                {
                    WorldGenerator.Inst.SetBlock(globalPos, block.blockID);
                }
                else
                {
                    Debug.LogWarning("NPC �� ���� ������� ���������� ������ ��� ��������� �����: " + globalPos);
                }

                // ��������� �������� ����� ���������� ������ ��� ���������
                yield return new WaitForSeconds(0.2f);
            }
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
                WorldGenerator.Inst.SetBlock(globalPos, 0);
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
        private IEnumerator BuildSmartScaffolding(Vector3 targetPos)
        {
            int gap = GetVerticalGap(targetPos);
            if (gap <= verticalGapThreshold)
            {
                // ������ ������������ �������: ������ ����� ��������������� ��� ������� �� ����������� �����
                Vector3 scaffoldPos = targetPos + Vector3.down;
                while (!IsSupported(scaffoldPos))
                {
                    WorldGenerator.Inst.SetBlock(scaffoldPos, scaffoldingBlockID);
                    yield return null; // ���� ���� ����� ����������
                    scaffoldPos += Vector3.down;
                }
            }
            else
            {
                // ������ ������������ �������� (������� �����), ����� NPC ��� ��������� � ����������
                // ���������� ����������� ����������� ��� ������������� ��������
                Vector3 chosenDir = DetermineStairDirection(targetPos, gap);
                if (chosenDir == Vector3.zero)
                {
                    // ���� �� ������� ��������� �����������, ���������� ����������� (�����)
                    chosenDir = Vector3.forward;
                }
                Vector3 scaffoldPos = targetPos;
                // ������ �� ��� ���, ���� �� ��������� ���������
                while (!IsSupported(scaffoldPos))
                {
                    scaffoldPos += (chosenDir + Vector3.down);
                    WorldGenerator.Inst.SetBlock(scaffoldPos, scaffoldingBlockID);
                    yield return null;
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
                        // "���������" ����������� � ���������� ����� �� �����
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

        // ����������� NPC � �������� ������� � �������������� NavMeshAgent
        private IEnumerator MoveToPosition(Vector3 destination)
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            agent.SetDestination(destination);
            while (agent.pathPending || agent.remainingDistance > approachDistance)
            {
                yield return null;
            }
        }
    }
}
