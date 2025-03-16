using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace NPCO
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

    public class NPCBuilder : MonoBehaviour
    {
        public float buildRange = 3.0f; // ������������ ����������, �� ������� NPC ����� ���������� ����
        public float approachDistance = 1.0f; // ����������, �� ������� NPC �������, ��� ������ ����� ���������
        public byte scaffoldingBlockID = 1; // ID ���������� ����� ��� ����� (����� ������ ����� ����������)

        // ������� ����� ������������� ����
        public IEnumerator BuildHouse(Vector3 basePosition, List<BlockData> blueprint)
        {
            // 1. ������������� ����� ���, ����� ������� ����� ����� (��������� -> ����� -> �����)
            List<BlockData> orderedBlueprint = OrderBlueprint(blueprint);

            // �������� �� ������� ����� �� �������
            foreach (BlockData block in orderedBlueprint)
            {
                // ��������� ���������� ������� �����: ������� ����� + ��������� ���������� �����
                Vector3 globalPos = basePosition + block.localPosition;

                // 2. ���� � ������� ������� ���� ������ ����� (�� �������� � �����������) � ������ ��
                yield return StartCoroutine(ClearObstructionsAt(globalPos, block));

                // 3. ���� ���� ������ ��������� � �������, ��������� ������� ���������
                if (!IsSupported(globalPos) && block.blockID != 0)
                {
                    // ���� ��� ������� �������� ��� �����, ������ ��������� �������� (scaffolding)
                    yield return StartCoroutine(BuildScaffolding(globalPos));
                }

                // 4. ���������� ����� �������: �������, ��������� �� NavMesh � ����������� � �������� buildRange �� globalPos
                Vector3 approachPos = FindApproachPosition(globalPos);
                // ���������� NPC � ���� �����
                yield return StartCoroutine(MoveToPosition(approachPos));

                // 5. ���� NPC ��������� ���������� ������ � ���������� �������, ������������� ����
                if (Vector3.Distance(transform.position, globalPos) <= buildRange)
                {
                    // ��������� ����� ����� ��������� ����
                    WorldGenerator.Inst.SetBlock(globalPos, block.blockID);
                }
                else
                {
                    Debug.LogWarning("NPC �� ���� ������� ���������� ������ ��� ��������� �����: " + globalPos);
                }

                // ������� �������� ����� ����������� (����� ��������� ��� �������� � ���������)
                yield return new WaitForSeconds(0.2f);
            }
        }

        // ���������� ������ �� ������ (������� ������ �����, ����� �������)
        private List<BlockData> OrderBlueprint(List<BlockData> blueprint)
        {
            return blueprint.OrderBy(b => b.localPosition.y).ToList();
        }

        // ��������: ���������� �� �������������� ���� ��� ������ ��������
        private bool IsSupported(Vector3 globalPos)
        {
            Vector3 belowPos = globalPos + Vector3.down;
            // ������������, ��� ����� ���� � id != 0 ������ ������
            return WorldGenerator.Inst.GetBlockID(belowPos) != 0;
        }

        // ���������� ��������� ���� �� ������, �� ������� ����� ���������� ������� ����
        private IEnumerator BuildScaffolding(Vector3 targetPos)
        {
            Vector3 scaffoldPos = targetPos;
            // ���� �����, ��������� ����, ���� �� ����� ����
            while (!IsSupported(scaffoldPos))
            {
                scaffoldPos += Vector3.down;
                // ������������� ��������� ���� �����
                WorldGenerator.Inst.SetBlock(scaffoldPos, scaffoldingBlockID);
                yield return null; // ��� ����� �� ���������� ����
            }
        }

        // ���� ������� ������� ��� ������ ������ ������, ������� �� ������������� �����, ������� ���
        private IEnumerator ClearObstructionsAt(Vector3 globalPos, BlockData targetBlock)
        {
            byte currentID = WorldGenerator.Inst.GetBlockID(globalPos);
            // ���� �� ����� ��������� ��� ���� ����, �������� �� �������� (��������, �����, ������ � �.�.)
            if (currentID != 0 && currentID != targetBlock.blockID)
            {
                // ������� (����������) �������� ����
                WorldGenerator.Inst.SetBlock(globalPos, 0);
                yield return null; // ��� ����� �� ���������� ����
            }
        }

        // ����� ����� �������, ��� NPC ����� ��������� ������, ����� ���������� ����
        private Vector3 FindApproachPosition(Vector3 targetPos)
        {
            NavMeshHit hit;
            // ������� ����� ��������� ����� �� NavMesh � �������� buildRange �� ������� �������
            if (NavMesh.SamplePosition(targetPos, out hit, buildRange, NavMesh.AllAreas))
            {
                return hit.position;
            }
            // ���� �� ������� � ���������� ���� ������� �������
            return targetPos;
        }

        // ����������� NPC � �������� ������� � �������������� NavMeshAgent
        private IEnumerator MoveToPosition(Vector3 destination)
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            agent.SetDestination(destination);
            // ���, ���� NPC �� ����������� � ����
            while (agent.pathPending || agent.remainingDistance > approachDistance)
            {
                yield return null;
            }
        }
    }
}
