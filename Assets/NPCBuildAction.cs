using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using static ChunckData;

namespace Ururu
{
    public class NPCBuildAction : MonoBehaviour
    {
        public float buildRange = 3.0f;       // М����м�льно� р����оян��, н� �о�ором NPC мож�� ����но���ь бло�
        public float approachDistance = 1.0f; // Доп����мо� р����оян�� пр� по�хо�� � �оч�� ����но���
        public byte scaffoldingBlockID = 1;   // ID �р�м�нного бло�� �ля опоры (scaffolding)
        public int verticalGapThreshold = 5;  // Е�л� з�зор м�ньш� �л� р���н э�ом� порог�, ��ро�м ��р����льн�ю �олонн�

        [SerializeField] TextAsset buildingData;
        [SerializeField] AgentMove agentMove;

        PlayerBehaviour player;
        NavMeshAgent agent;
        List<BlockData> blueprint;
        List<JsonTurnedBlock> turnedBlocks;

        private Vector3 currentBuildingBasePosition;
        private HashSet<Vector3> currentBlueprintPositions;
        // Но�о� пр����но� пол� �ля хр�н�н�я поз�ц�й ч�р��ж�

        public bool ebobo;
        public bool withPause;


        private IEnumerator Start()
        {
            blueprint = new List<BlockData>();
            var savedBuilding = JsonConvert.DeserializeObject<SaveBuildingData>(buildingData.text);
            foreach (var item in savedBuilding.blocksData.changedBlocks)
            {
                blueprint.Add(new BlockData() { blockID = item.blockId, localPosition = item.Pos });
            }
            turnedBlocks = savedBuilding.turnedBlocks;
            //blueprint = BlockUtils.FillBoundingBox(blueprint);

            Debug.Log("Бло�о� � ч�р��ж�: " + blueprint.Count);

            while (player == null)
            {
                yield return null;
            }

            yield return new WaitForSeconds(3.5f);

            agent.enabled = false;
            transform.position = player.transform.position + (player.transform.forward * 3) + (Vector3.up * 3);
            yield return null;
            agent.enabled = true;

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
                // �ыч��ля�м �оч��, о����� н�чнём ��ро���ль���о (можно з����ь по лог��� �гры)
                var playerNearPos = player.transform.position + player.transform.forward + Vector3.up;
                playerNearPos.x = Mathf.FloorToInt(playerNearPos.x);
                playerNearPos.y = Mathf.FloorToInt(playerNearPos.y);
                playerNearPos.z = Mathf.FloorToInt(playerNearPos.z);

                StartCoroutine(BuildHouse(playerNearPos, blueprint));
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                isPaused = false;
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                ebobo = !ebobo;

                var agentIntPos = transform.position.ToIntPos();
                agentIntPos.x++;

                //WorldGenerator.Inst.SetBlockAndUpdateChunck(agentIntPos, 10);
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                withPause = !withPause;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                StartCoroutine(RestoreBuilding(currentBuildingBasePosition, blueprint));
            }
        }

        public IEnumerator RestoreBuilding(Vector3 basePosition, List<BlockData> blueprint)
        {
            HashSet<Vector3> blueprintPositions = new HashSet<Vector3>();
            foreach (BlockData block in blueprint)
            {
                blueprintPositions.Add(basePosition + block.localPosition);
            }

            agentMove.SetBlueprint(new
            (
                blueprintPositions,
                basePosition
            ));

            yield return StartCoroutine(CreateNavMeshFixableObjs(basePosition, blueprint));

            foreach (BlockData block in blueprint)
            {
                Vector3 globalPos = basePosition + block.localPosition;

                var blockID = WorldGenerator.Inst.GetBlockID(globalPos);

                if (blockID == block.blockID)
                    continue;

                //yield return StartCoroutine(agentMove.CheckMeshToFixNavError(globalPos));

                var offset = new Vector3(-0.5f, 0.1f, 0.5f);
                // 3. Н�хо��м �оч�� по�хо�� ч�р�з NavMesh � п�р�м�щ��м�я ����
                Vector3 approachPos = NavigationTool.FindApproachPositionOnBlock(globalPos, out var founded, 1.5f); //FindApproachPosition(globalPos + offset);

                if (!founded)
                {
                    approachPos.y--;
                }

                yield return StartCoroutine(agentMove.MoveToPosition(approachPos, true, 1.7f));

                // 4. Е�л� NPC �о����очно бл�з�о, ����н��л����м бло�
                if (Vector3.Distance(agent.transform.position, globalPos + offset) <= buildRange)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(globalPos, block.blockID);
                }
                else
                {
                    Debug.Log("NPC н� �мог по�ой�� �о����очно бл�з�о �ля �о����но�л�н�я бло��: " + globalPos);
                }

                yield return new WaitForSeconds(0.8f);
            }

            agentMove.SetBlueprint(null);
        }

        IEnumerator CreateNavMeshFixableObjs(Vector3 basePosition, List<BlockData> blueprint)
        {
            List<ChunckComponent> chunksToUpdate = new();
            List<GameObject> fixables = new();

            foreach (BlockData block in blueprint)
            {
                Vector3 globalPos = basePosition + block.localPosition;

                var pos1Up = globalPos + Vector3.up;
                var pos2Up = globalPos + (Vector3.up * 2);

                var blockId = WorldGenerator.Inst.GetBlockID(globalPos);
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

                    if (!chunksToUpdate.Contains(chunk))
                    {
                        chunksToUpdate.Add(chunk);
                    }

                    fixables.Add(fixGo);
                }
            }

            yield return new WaitForSeconds(0.1f);

            //yield return StartCoroutine(Pause());

            foreach (var chunk in chunksToUpdate)
            {
                WorldGenerator.Inst.UpdateMesh(chunk);
            }

            yield return new WaitForSeconds(0.1f);

            //yield return StartCoroutine(Pause());

            foreach (var item in fixables)
            {
                Destroy(item);
            }

            //yield return StartCoroutine(Pause());

            print("�ро�� ф����н�л");
        } 

        // Гл��ный м��о� ��ро���ль���� �ом� по ч�р��ж� (blueprint)
        public IEnumerator BuildHouse(Vector3 basePosition, List<BlockData> blueprint)
        {
            currentBuildingBasePosition = basePosition; // Сохр�ня�м б�зо��ю поз�ц�ю �ля р��чё�� г�б�р��о� по��рой��
            

            // Соз��ём н�бор поз�ц�й, г�� б���� ��ро��ь�я бло�� (глоб�льны� �оор��н��ы)
            HashSet<Vector3> blueprintPositions = new HashSet<Vector3>();
            foreach (BlockData block in blueprint)
            {
                blueprintPositions.Add(basePosition + block.localPosition);
            }
            currentBlueprintPositions = blueprintPositions; // �охр�ня�м �ля по���� п���

            agentMove.SetBlueprint(new
            (
                currentBlueprintPositions,
                basePosition
            ));


            // Сор��р��м бло�� по �ы�о�� (ф�н��м�н�, з���м ���ны, �рыш� � �.�.)
            List<BlockData> orderedBlueprint = OrderBlueprint(blueprint);

            foreach (BlockData block in orderedBlueprint)
            {
                Vector3 globalPos = basePosition + block.localPosition;

                // 1. Е�л� н� м���� �ж� ���ь бло�, н� �оо��������ющ�й ч�р��ж�, оч�щ��м м���о
                yield return StartCoroutine(ClearObstructionsAt(globalPos, block));

                //// 2. Е�л� бло� н� п���ой � по� н�м н�� опоры, об��п�ч����м �о���п
                //if (block.blockID != 0 && !IsSupported(globalPos))
                //{
                //    yield return StartCoroutine(BuildSmartScaffolding(globalPos, blueprintPositions));
                //}
                //var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                //go.transform.position = globalPos;
                //go.transform.localScale *= 0.38f;
                //go.name = "УССССС";

                var offset = new Vector3(-0.5f, 0.1f, 0.5f);
                // 3. Н�хо��м �оч�� по�хо�� ч�р�з NavMesh � п�р�м�щ��м�я ����

                // Е��ь пробл�м� пр� по��рой�� з��н�й, �г�н� пр� ��ро���ль���� �рыш� мож��
                // ��р�м���я �ой�� �о �оч�� н� по�о�онн���, � н� н� ��мой �рыш�, он� поч�м�-�о
                // пом�ч����я ��� бл�ж�йш�я, ���ь �мы�л пробо���ь �����ь р�зны� �оч�� � м��о�
                // FindApproachPosition
                Vector3 approachPos = FindApproachPosition(globalPos + offset);

                yield return StartCoroutine(agentMove.MoveToPosition(approachPos, true, 1.5f));

                // 4. Е�л� NPC �о����очно бл�з�о, ����н��л����м бло�
                if (Vector3.Distance(transform.position, globalPos + offset) <= buildRange)
                {
                    var hasTurned = turnedBlocks.Find(t => t.Pos == block.localPosition);

                    // По�ом п�р���л��ь н� �р�г�� м��о�ы, э�� н� �охр�няю� �нф� н� ��р���р�
                    if (hasTurned.turnsBlockData != null)
                    {
                        var chunk = WorldGenerator.Inst.GetChunk(globalPos);
                        var blockLocalPos = (globalPos - chunk.pos).ToVecto3Int();
                        chunk.AddTurnBlock(blockLocalPos, hasTurned.turnsBlockData);
                    }

                    WorldGenerator.Inst.SetBlockAndUpdateChunck(globalPos, block.blockID);
                }
                else
                {

                    Debug.Log("NPC н� �мог по�ой�� �о����очно бл�з�о �ля ����но��� бло��: " + globalPos);
                }

                // З���рж�� �ля пл��но��� ��ро���ль����
                yield return new WaitForSeconds(1.3f);
            }

            agentMove.SetBlueprint(null);

        }

        // Е�л� � ц�л��ой поз�ц�� �ж� ���ь бло�, н� �хо�ящ�й � ч�р��ж, ���ля�м �го
        private IEnumerator ClearObstructionsAt(Vector3 globalPos, BlockData targetBlock)
        {
            byte currentID = WorldGenerator.Inst.GetBlockID(globalPos);
            if (currentID != 0 && currentID != targetBlock.blockID)
            {
                print("�ы �б��ь");
                WorldGenerator.Inst.SetBlockAndUpdateChunck(globalPos, 0);
                yield return new WaitForSeconds(0.3f);
            }
        }

        // По��� �оч�� по�хо�� н� NavMesh, � пр���л�х buildRange о� ц�л��ой поз�ц��
        private Vector3 FindApproachPosition(Vector3 targetPos)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, buildRange - 0.5f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            Debug.Log("Н� н�ш�л �оч�� н� н��м�ш�");
            return targetPos;
        }

        // Сор��ро��� бло�о� по �ы�о�� (о� н�зш�х � �ы�ш�м)
        private List<BlockData> OrderBlueprint(List<BlockData> blueprint)
        {
            return blueprint.OrderBy(b => b.localPosition.y).ToList();
        }

        private IEnumerator BuildLadderForBlock(Vector3 destination)
        {
            // �ыч��ля�м г�б�р��ы по��рой�� н� о�но�� blueprint � б�зо�ой поз�ц��
            GetBuildingBounds(blueprint, currentBuildingBasePosition, out Vector3 buildingCenter, out float buildingRadius, out var size);

            var edge = GetClosestEdge(currentBuildingBasePosition, size, destination);

            print(edge + " =-=-=-=-=");

            // Ищ�м �оч�� �ыхо�� з� пр���л�м� по��рой�� (safeDistance = 1, ч�обы л���н�ц� был� «пр�л�пш�й» � по��рой��)
            Vector3 ladderBase = FindExitPoint(buildingCenter, buildingRadius, 1f);
            
            // О�р�гля�м �оор��н��ы б�зы л���н�цы (�о ц�лого зн�ч�н�я)
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

            Debug.Log("Л���н�ц� б���� ��ро��ь�я � �оч�� (о�р�гл�но): " + ladderBase);

            var isUpLadder = transform.position.y - 1 < ladderBase.y;
            scaffoldingBlockID = isUpLadder ? (byte)92 : (byte)61;

            // Н�ч�н��м ��ро��ь л���н�ц� �н�з� ���рх
            float startY = Mathf.Min(transform.position.y-1, ladderBase.y); // Б�р�м м�н�м�льный �ро��нь (н� �л�ч�й, ��л� н�жно ��ро��ь � �н�з)
            float endY = Mathf.Max(transform.position.y-1, ladderBase.y);  // Б�р�м м����м�льный �ро��нь (���� н�жно �обр��ь�я)

            bool placedAnyBlocks = false;

            float currentY = Mathf.Floor(startY); // С��р���м � бл�ж�йш�го н�жн�го ц�лого

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
                    Debug.Log("У���но�л�н бло� л���н�цы н� " + startLadderPos);
                    placedAnyBlocks = true;
                }

                // П�р�м�щ��м NPC � �л���ющ�м� ш�г� л���н�цы
                Vector3 nextStepPos = startLadderPos - dir;//new Vector3(ladderBase.x, currentY + 1f, ladderBase.z);
                yield return StartCoroutine(MoveToPosition(nextStepPos, false));
                yield return new WaitForSeconds(0.5f); // З���рж�� м�ж�� ш�г�м�

                startLadderPos -= dir;
            }

            if (placedAnyBlocks)
            {
                Debug.Log("Л���н�ц� по��ро�н�, NPC мож�� �обр��ь�я �о " + destination);
            }
            else
            {
                Debug.Log("Л���н�ц� н� �р�бо��л��ь: п��ь �ж� ��обо��н �л� NPC н� н�жной �ы�о��.");
            }

            yield return null;

            //WorldGenerator.Inst.SetBlockAndUpdateChunck(ladderBase, 61);
            //yield break;
        }

        public Edge GetClosestEdge(Vector3 buildingPosition, Vector3 size, Vector3 destination)
        {
            // Р�зм�ры по��рой��, пол�ч��м м�н�м�льны� � м����м�льны� �оор��н��ы по о�ям X, Y, Z
            Vector3 halfSize = size / 2;

            // Опр���ля�м гр�н�цы по��рой��
            Vector3 minBounds = buildingPosition;
            Vector3 maxBounds = buildingPosition + size;

            // �ыч��ля�м р����оян�я �о ��ж�ой гр�н�цы
            float distanceToLeft = Mathf.Abs(destination.x - minBounds.x);
            float distanceToRight = Mathf.Abs(destination.x - maxBounds.x);
            float distanceToFront = Mathf.Abs(destination.z - maxBounds.z);
            float distanceToBack = Mathf.Abs(destination.z - minBounds.z);
            float distanceToTop = Mathf.Abs(destination.y - maxBounds.y);
            float distanceToBottom = Mathf.Abs(destination.y - minBounds.y);

            // Н�хо��м м�н�м�льно� р����оян�� � �оз�р�щ��м �оо��������ющ�й �р�й
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
        //    // 1. Пол�ч��м гр�н�цы по��рой��
        //    GetBuildingBounds(blueprint, currentBuildingBasePosition, out Vector3 buildingCenter, out float buildingRadius);

        //    // 2. Ищ�м бл�ж�йш�ю �оч�� � по��рой�� (ladderBase), ч�обы л���н�ц� � н�й шл�
        //    Vector3 ladderBase = FindExitPoint(buildingCenter, buildingRadius, 1f);

        //    yield break;

        //    ladderBase = new Vector3(
        //        Mathf.Round(ladderBase.x),
        //        Mathf.Round(ladderBase.y),
        //        Mathf.Round(ladderBase.z)
        //    );

        //    Debug.Log($"Ц�ль л���н�цы (destination): {destination}, �оч�� � по��рой�� (ladderBase): {ladderBase}");

        //    // 3. �ыч��ля�м ���р�о��ю �оч��, �ч��ы��я, ч�о мы н�ч�н��м ��ро��ь л���н�ц� н�ж� ����щ�го полож�н�я �г�н��
        //    Vector3 currentPos = new Vector3(
        //        Mathf.Round(transform.position.x),
        //        Mathf.Round(transform.position.y) - 1, // н� 1 бло� н�ж�, ч�обы �п�р��ь�я � з�млю
        //        Mathf.Round(transform.position.z)
        //    );

        //    // 4. Е�л� �ж� н� м���� - н� ��ро�м
        //    if (currentPos == ladderBase)
        //    {
        //        Debug.Log("Л���н�ц� н� �р�б����я - �ж� н� поз�ц��.");
        //        yield break;
        //    }

        //    // 5. Ш�г��м � ��орон� ц�л�, о� н�жн�й �оч�� �о ladderBase
        //    int stepX = ladderBase.x > currentPos.x ? 1 : (ladderBase.x < currentPos.x ? -1 : 0);
        //    int stepZ = ladderBase.z > currentPos.z ? 1 : (ladderBase.z < currentPos.z ? -1 : 0);
        //    int stepY = ladderBase.y > currentPos.y ? 1 : -1;

        //    // 6. Про��ря�м гр�н�цы з��н�я � ��ро�м л���н�ц�
        //    bool placedAnyBlocks = false;

        //    while (currentPos.y != ladderBase.y)
        //    {
        //        // Н�хо��м ����щ�ю поз�ц�ю ���п�нь��
        //        Vector3 ladderBlockPos = new Vector3(
        //            Mathf.Round(currentPos.x),
        //            Mathf.Round(currentPos.y),
        //            Mathf.Round(currentPos.z)
        //        );

        //        // Про��ря�м, н� �ыхо��� л� ����щ�я �оч�� з� пр���лы з��н�я
        //        if (Mathf.Abs(ladderBlockPos.x - buildingCenter.x) > buildingRadius ||
        //            Mathf.Abs(ladderBlockPos.z - buildingCenter.z) > buildingRadius)
        //        {
        //            Debug.Log("С��п�нь�� �ыхо��� з� пр���лы з��н�я, пр��р�щ��м по��ро�н��");
        //            break;
        //        }

        //        // Е�л� бло� п���ой, �����м л���н�ц�
        //        if (WorldGenerator.Inst.GetBlockID(ladderBlockPos) == 0)
        //        {
        //            WorldGenerator.Inst.SetBlockAndUpdateChunck(ladderBlockPos, scaffoldingBlockID);
        //            Debug.Log($"По����л�н бло� л���н�цы н� {ladderBlockPos}");
        //            placedAnyBlocks = true;
        //        }

        //        // Д��г��м�я по л���н�ц�
        //        yield return StartCoroutine(MoveToPosition(ladderBlockPos, false));
        //        yield return new WaitForSeconds(0.1f);

        //        // По�н�м��м�я н� ш�г по Y
        //        currentPos.y += stepY;

        //        // Е�л� н�хо��м�я н�ж� target по �ы�о��, ���г��м�я по ���гон�л�
        //        if (Mathf.Abs(ladderBase.x - currentPos.x) > Mathf.Abs(ladderBase.z - currentPos.z))
        //        {
        //            currentPos.x += stepX;  // Ш�г��м по X
        //        }
        //        else
        //        {
        //            currentPos.z += stepZ;  // Ш�г��м по Z
        //        }

        //        // Корр��ц�я погр�шно���й
        //        if (Mathf.Abs(currentPos.y - ladderBase.y) < 0.1f) currentPos.y = ladderBase.y;
        //        if (Mathf.Abs(currentPos.x - ladderBase.x) < 0.1f) currentPos.x = ladderBase.x;
        //        if (Mathf.Abs(currentPos.z - ladderBase.z) < 0.1f) currentPos.z = ladderBase.z;
        //    }

        //    if (placedAnyBlocks)
        //    {
        //        Debug.Log($"Л���н�ц� ��п�шно по��ро�н� �о {ladderBase}");
        //    }
        //    else
        //    {
        //        Debug.Log("Л���н�ц� н� �р�бо��л��ь, п��ь ��обо��н.");
        //    }


        //}

        private IEnumerator MoveToPosition(Vector3 destination, bool canBuildLadder = true)
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();

            NavMeshPath path = new NavMeshPath();
            //agent.CalculatePath(destination, path);

            agent.SetDestination(destination);
            agent.isStopped = true;

            // Ж��м, по�� п��ь н� б���� �ыч��л�н:
            while (agent.pathPending)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            path = agent.path;
            // Т�п�рь можно пол�ч��ь agent.path �л� �ыполн��ь �ополн���льны� ��й����я
            // ...

            // Ког�� б����� го�о�ы, ч�обы �г�н� н�ч�л ���ж�н�� по �ыч��л�нном� п���:
            agent.isStopped = false;


            if (canBuildLadder && path.status != NavMeshPathStatus.PathComplete)
            {
                Debug.Log($"MoveToPosition: П��ь �о {destination} н� н�й��н ч�р�з NavMesh (PathComplete = {path.status}). З�п�����м по��ро�н�� scaffolding.");
                if(path.status is NavMeshPathStatus.PathInvalid)
                {
                    yield return StartCoroutine(Pause());
                }

                yield return StartCoroutine(MoveToPosition(destination, false));// Ч���о про��р��ь
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
                        Debug.Log($"MoveToPosition: Аг�н� ф�з�ч���� з���рял � {agent.transform.position}.");
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
                        Debug.Log($"MoveToPosition: Аг�н� н� пр�бл�ж����я � {destination}, ����щ�� р����оян�� = {currentDistanceToDest}");
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

        //    // Сн�ч�л� проб��м по��ро��ь п��ь
        //    NavMeshPath path = new NavMeshPath();
        //    agent.CalculatePath(destination, path);

        //    // Е�л� п��ь н� полный � мы �щё н� пробо��л� ��ро��ь л���н�ц� — ��ро�м
        //    if (canBuildLadder && path.status != NavMeshPathStatus.PathComplete)
        //    {
        //        Debug.Log($"MoveToPosition: П��ь �о {destination} н� н�й��н (PathComplete = {path.status}). Пы���м�я по��ро��ь л���н�ц�.");
        //        yield return StartCoroutine(BuildLadderForBlock(destination));

        //        // По�л� ��ро���ль���� л���н�цы проб��м �щё р�з, но �ж� б�з по��орного ��ро���ль����
        //        yield return StartCoroutine(MoveToPosition(destination, false));
        //        yield break;
        //    }

        //    // У���н��л����м п��ь
        //    agent.SetPath(path);

        //    // Ло��льны� п�р�м�нны� �ля про��р�� "з���р���н�я"
        //    float noMovementTimeout = 5f;       // �р�мя, по�л� �о�орого �ч����м, ч�о NPC «з���рял» ф�з�ч���� (н� ���г����я)
        //    float noProgressTimeout = 5f;       // �р�мя, по�л� �о�орого �ч����м, ч�о NPC «з���рял по прогр����» (���ж���я, но н� ���но����я бл�ж�)
        //    float stuckTimer = 0f;             // Счё�ч�� �ля ф�з�ч���ого з���р���н�я
        //    float progressTimer = 0f;          // Счё�ч�� �ля о��������я прогр����
        //    Vector3 lastPosition = agent.transform.position;
        //    float lastDistanceToDest = (path.corners.Length > 0)
        //        ? Vector3.Distance(agent.transform.position, path.corners[path.corners.Length - 1])
        //        : Vector3.Distance(agent.transform.position, destination);

        //    // Ц��л ож���н�я, по�� �г�н� н� �о���гн�� ц�л�
        //    while (agent.pathPending || agent.remainingDistance > approachDistance)
        //    {
        //        // 1) Про��р�� «ф�з�ч���ого» ���ж�н�я (н� ��о�� л� �г�н� н� м����)
        //        float distanceMoved = Vector3.Distance(agent.transform.position, lastPosition);
        //        bool isMoving = distanceMoved > 0.01f;
        //        if (!isMoving)
        //        {
        //            stuckTimer += Time.deltaTime;
        //            if (stuckTimer > noMovementTimeout)
        //            {
        //                Debug.Log($"MoveToPosition: Аг�н� ф�з�ч���� з���рял � {agent.transform.position}, н� ���г����я � {destination}.");

        //                // Е�л� мож�м ��ро��ь л���н�ц� — проб��м
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

        //        // 2) Про��р�� «прогр����» (�о�р�щ����я л� р����оян�� �о �он�чной �оч��)
        //        float currentDistanceToDest = agent.remainingDistance; // �л� �ыч��ля�ь по path.corners
        //        if (currentDistanceToDest >= lastDistanceToDest - 0.05f)
        //        {
        //            // Р����оян�� н� �м�ньш�ло�ь (�л� ��ж� ���л�ч�ло�ь)
        //            progressTimer += Time.deltaTime;
        //            if (progressTimer > noProgressTimeout)
        //            {
        //                Debug.Log($"MoveToPosition: Аг�н� н� пр�бл�ж����я � {destination}, ����щ�� р����оян�� = {currentDistanceToDest}");

        //                // Е�л� мож�м ��ро��ь л���н�ц� — проб��м
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
        //            // Е��ь прогр��� — �бр��ы���м ��йм�р
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
            yield return new WaitForSeconds(0.5f);

            // Пол�ч��м ц�лоч��л�нны� поз�ц�� �г�н�� � ц�л�
            Vector3Int agentPos = new Vector3Int(
                Mathf.FloorToInt(transform.position.x + 1),
                Mathf.FloorToInt(transform.position.y - 1.1f),
                Mathf.FloorToInt(transform.position.z)
            );
            Vector3Int destPos = new Vector3Int(
                Mathf.FloorToInt(destination.x),
                Mathf.FloorToInt(destination.y),// !!!!!!
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

            // См�щ��м об� поз�ц�� н� о��н бло� �н�з
            //agentPos.y -= 1;
            //destPos.y -= 1;

            List<Vector3Int> path = null;
            Debug.Log("�ы�о�ы о�л�ч�ю��я – �щ�м п��ь ���п�нь��м� ч�р�з AStarPath3D.");
            yield return StartCoroutine(AStarPath3DCoroutine(agentPos, destPos, currentBlueprintPositions, result => path = result));

            //if (agentPos.y != destPos.y)
            //{
            //    Debug.Log("�ы�о�ы о�л�ч�ю��я – �щ�м п��ь ���п�нь��м� ч�р�з AStarPath3D.");
            //    yield return StartCoroutine(AStarPath3DCoroutine(agentPos, destPos, currentBlueprintPositions, result => path = result));
            //}
            //else
            //{
            //    Debug.Log("�ы�о�ы �о�п���ю� – �щ�м гор�зон��льный п��ь �ля мо���.");
            //    yield return StartCoroutine(AStarPathCoroutine(agentPos, destPos, currentBlueprintPositions, result => path = result));
            //}

            if (path == null)
            {
                Debug.Log("Н� ���ло�ь н�й�� п��ь �ля scaffolding.");
                yield break;
            }

            Debug.Log("Н�й��н п��ь �ля scaffolding, �л�н�: " + path.Count);
            foreach (Vector3Int cell in path)
            {
                // Е�л� � яч�й�� п���о – �����м scaffolding-бло�
                if (WorldGenerator.Inst.GetBlockID(cell) == 0)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(cell, scaffoldingBlockID);
                    //Debug.Log("По����л�н scaffolding бло� н� " + cell);
                    //yield return StartCoroutine(MoveToPosition(cell, false));
                    yield return new WaitForSeconds(0.3f);
                }
                yield return null;
            }

            if (withPause)
            {
                yield return StartCoroutine(Pause());
            }

            yield return new WaitForSeconds(1.5f);

            // По�л� по��ро�н�я scaffolding, п�р�м�щ��м�я � ц�л�,
            // �м�щённой ���ж� н� о��н бло� �н�з
            Vector3 destinationOffset = destination + Vector3.down;
            yield return StartCoroutine(MoveToPosition(destinationOffset, false));
        }


        private IEnumerator BuildBridgeToPoint(Vector3Int start, Vector3Int goal)
        {
            // Для мо��� ф����р��м �ы�о�� start.y
            Vector3Int s = new Vector3Int(start.x, start.y, start.z);
            Vector3Int g = new Vector3Int(goal.x, start.y, goal.z);

            List<Vector3Int> path = null;
            yield return StartCoroutine(AStarPathCoroutine(s, g, currentBlueprintPositions, result => path = result));

            if (path == null)
            {
                Debug.Log("Н� ���ло�ь н�й�� гор�зон��льный п��ь �ля мо���.");
                yield break;
            }

            Debug.Log("Гор�зон��льный п��ь н�й��н �ля мо���, �л�н�: " + path.Count);
            foreach (Vector3Int cell in path)
            {
                if (WorldGenerator.Inst.GetBlockID(cell) == 0)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(cell, scaffoldingBlockID);
                    Debug.Log("По����л�н бло� мо��� н� " + cell);
                    yield return new WaitForSeconds(0.1f);
                }
                yield return null;
            }

            Debug.Log("Мо�� по��ро�н о� " + s + " �о " + g);
        }


        private IEnumerator BuildStairsToPoint(Vector3Int start, Vector3Int goal)
        {
            Vector3Int current = start;
            // Опр���ля�м н�пр��л�н�� по Y: ��л� �г�н� �ыш� ц�л�, н�жно �п�����ь�я, �н�ч� по�н�м��ь�я
            int verticalStep = (current.y > goal.y) ? -1 : 1;

            int maxSteps = 100;
            int steps = 0;

            while ((current.x != goal.x || current.z != goal.z || current.y != goal.y) && steps < maxSteps)
            {
                // �ыч��ля�м гор�зон��льно� н�пр��л�н�� о� current � goal
                int dx = goal.x - current.x;
                int dz = goal.z - current.z;
                int stepX = (dx == 0) ? 0 : (dx > 0 ? 1 : -1);
                int stepZ = (dz == 0) ? 0 : (dz > 0 ? 1 : -1);

                // Для ���п�н�� б���м пы���ь�я ���г��ь�я ���гон�льно: гор�зон��льно� �м�щ�н�� + ��р����льно� �зм�н�н��
                Vector3Int next = new Vector3Int(current.x + stepX, current.y + verticalStep, current.z + stepZ);

                // Е�л� по ���ой-л�бо о�� р�зн�ц� р��н� н�лю, о����ля�м б�з �м�щ�н�я
                if (dx == 0) next.x = current.x;
                if (dz == 0) next.z = current.z;

                // Е�л� �л���ющ�й ш�г �хо��� � яч�й�� по��рой��, попроб��м �оль�о гор�зон��льный ����г
                Vector3 nextF = new Vector3(next.x, next.y, next.z);
                if (currentBlueprintPositions.Contains(nextF))
                {
                    Vector3Int alt = new Vector3Int(current.x + stepX, current.y, current.z + stepZ);
                    next = alt;
                }

                // С����м бло�, ��л� яч�й�� п����
                if (WorldGenerator.Inst.GetBlockID(next) == 0)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(next, scaffoldingBlockID);
                    Debug.Log("У���но�л�н бло� ���п�нь�� н� " + next);
                    yield return new WaitForSeconds(0.1f);
                }

                current = next;
                steps++;
                yield return null;
            }

            Debug.Log("С��п�нь�� по��ро�ны о� " + start + " �о " + goal);
        }


        public List<Vector3Int> allowedDirections;

        /// <summary>
        /// Э�о� м��о� р��л�з��� �лгор��м A* �ля по���� п��� � 3D-�о���льном
        /// про��р�н���� � н��о�орым� о�об�нно��ям� �ля н�ш�й �гры. 
        /// �о� ��� он р�бо����, ш�г з� ш�гом:
        /// Опр���л�н�� �оп����мых н�пр��л�н�й:
        /// М��о� п�р�б�р��� ��� �омб�н�ц�� �зм�н�н�й по о�ям(dx, dy, dz)
        /// о� -1 �о 1, �ром� �л�ч�я, �ог�� ��� �зм�н�н�я р��ны н�лю
        /// (�о ���ь, �ог�� н�� ���ж�н�я). Кром� �ого, ���люч�ю��я
        /// «ч���о ��р����льны�» п�р�м�щ�н�я, �ог�� �зм�ня���я �оль�о 
        /// �ы�о��(dy ≠ 0, � dx � dz р��ны 0). Э�о н�жно, ч�обы �г�н� 
        /// н� ���г�л�я про��о ���рх �л� �н�з б�з гор�зон��льного �омпон�н��.
        /// � р�з�ль���� пол�ч����я �п��о� н�пр��л�н�й (���го �о 26 �озможных,
        /// но � ��р����льным� огр�н�ч�н�ям� – м�ньш�).
        /// Ин�ц��л�з�ц�я ��р����р по����:
        /// Соз��ю��я ��� ��р����ры:
        /// openSet – �ло��рь, г�� хр�ня��я �злы(яч�й��), �о�оры� �щё пр����о��
        /// обр�бо���ь.� н�ч�л� �ю�� �л��ё��я ���р�о��я яч�й�� (start) 
        /// � н�л��ой ��о�мо��ью п��� (gCost).
        /// closedSet – множ����о �ж� обр�бо��нных �зло�.
        /// О�но�ной ц��л по����:


        /// По�� openSet н� п���, м��о� �ыб�р��� �з�л � н��м�ньш�й ��мм�рной 
        /// ��о�мо��ью fCost (fCost = gCost + hCost, г�� hCost – э�р����ч����я
        /// оц�н�� р����оян�я �о ц�л�, з���ь ��польз����я м�нхэ���н��о� р����оян��).
        /// Е�л� �ыбр�нный �з�л �о�п����� � ц�л��ой яч�й�ой(goal), �о п��ь н�й��н.
        /// М��о� �о����н��л����� п��ь, н�ч�н�я о� ц�л� � ���г�я�ь по ро����ль���м
        /// �зл�м �о ���р��, п�р��ор�ч����� �го � п�р���ё� ч�р�з callback.
        /// Обр�бо��� �о���н�х яч���(р��ш�р�н�� ����щ�го �зл�) :
        /// Для ��ж�ого �з �оп����мых н�пр��л�н�й м��о� �ыч��ля�� поз�ц�ю
        /// �о����(current.position + dir). Е�л� э�� яч�й�� �ж� обр�бо��н�
        /// (н�хо����я � closedSet), �о � проп����ю�. 
        /// З���м �о���нюю яч�й�� п�р��о�я� � форм�� Vector3
        /// (ц�лоч��л�нны� �оор��н��ы) �ля �р��н�н�я � blueprintPositions.
        /// Е�л� �о���няя яч�й�� н� �хо��� � н�бор blueprintPositions � � н�й
        /// �ж� ��о�� бло� (�о ���ь он� з�ня��), �о он� проп�������я.
        /// Д�л�� про��ря���я, ч�о н�� э�ой яч�й�ой ��обо�но ��� яч�й�� – э�о н�жно,
        /// ч�обы �г�н� �ы�о�ой 2 бло�� мог прой�� по н�й
        /// (про��ряю��я �о���няя яч�й��, �о���няя ���рх� � �щё о��н �ро��нь ���рх�).
        /// Обно�л�н�� ��о�мо��� � �об��л�н�� � openSet:

        ///Е�л� �о���няя яч�й�� ��о�л���оря�� ���м ��ло��ям, р���ч��ы�����я tentativeG – ��о�мо��ь п��� �о �о���� ч�р�з ����щ�й �з�л(э�о про��о ����щ�я ��о�мо��ь + 1).
        ///Е�л� �о��� �ж� ���ь � openSet, �о про��ря���я, можно л� �л�чш��ь �го ��о�мо��ь(�.�.tentativeG м�ньш� �го ����щ�го gCost). Е�л� ��, �о обно�ляю��я gCost � ро����ль���й �з�л.
        ///Е�л� �о���� �щё н�� � openSet, �оз��ё��я но�ый �з�л � р���ч���нным� зн�ч�н�ям� gCost � hCost(э�р����ч����я оц�н�� �о ц�л�) � �об��ля���я � openSet.
        ///З���рш�н��:

        ///Е�л� openSet оп������ (�о ���ь п��ь н� н�й��н), м��о� �ызы���� callback � null.
        ///Т���м обр�зом, м��о� �щ�� оп��м�льный п��ь о� н�ч�льной яч�й�� �о ц�л�, �ч��ы��я, ч�о:

        ///Аг�н� мож�� ���г��ь�я по 3D-про��р�н����, но н� �о��рш��ь ч���о ��р����льны� п�р�м�щ�н�я.
        ///Е�л� яч�й�� з�ня�� бло�ом по��рой��, � ��ё р��но можно ��пользо���ь �ля прохо��, ��л� н�� н�й ���ь �о����очно ��обо�ного про��р�н����.
        ///С�о�мо��ь п��� р���ч��ы�����я н� о�но�� �ол�ч����� ш�го�, � э�р������ – н� о�но�� м�нхэ���н��ого р����оян�я.
        ///��� э�� ш�г� �ыполняю��я � ���� �ор���ны, ч�обы н� бло��ро���ь �ыполн�н�� �гры � ч�обы можно было о��л����ь з�ц��л���н�� (��л� ���р�ц�й ���но����я �л�ш�ом много, �ор���н� з���рш����я � пр���пр�ж��н��м).
        /// </summary>
        private IEnumerator AStarPath3DCoroutine(Vector3Int start, Vector3Int goal, HashSet<Vector3> blueprintPositions, System.Action<List<Vector3Int>> callback)
        {
            // Р�зр�ш��м ���ж�н�я, ���люч�я ���гон�льны� п�р�хо�ы � гор�зон��льной пло��о���.
            // Р�зр�ш��м �оль�о ���ж�н�я, � �о�орых л�бо dx == 0, л�бо dz == 0 (но н� об� н�н�л��ы�).
            // Т��ж� ���люч��м ч���о ��р����льны� хо�ы (�ог�� dx � dz р��ны 0, � dy н� р���н 0).
            allowedDirections = new List<Vector3Int>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        // Проп�����м о��������� ���ж�н�я.
                        if (dx == 0 && dy == 0 && dz == 0)
                            continue;
                        // И��люч��м ч���о ��р����льны� ���ж�н�я (�оль�о по Y).
                        if (dx == 0 && dz == 0 && dy != 0)
                            continue;
                        // И��люч��м ���гон�льны� хо�ы по гор�зон��л� (�ог�� � dx, � dz н�н�л��ы�).
                        if (dx != 0 && dz != 0)
                            continue;

                        allowedDirections.Add(new Vector3Int(dx, dy, dz));
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

            int iterations = 0;
            int maxIterations = 10000;
            while (openSet.Count > 0)
            {
                iterations++;
                if (iterations % 50 == 0)
                    yield return null; // ��ём �р�мя �ор���н�

                if (iterations > maxIterations)
                {
                    Debug.Log("AStarPath3DCoroutine: �о���гн�� м����м�м ���р�ц�й, �озможный ц��л.");
                    callback(null);
                    yield break;
                }

                // Б�рём �з�л � м�н�м�льным fCost
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

                    if (ebobo)
                    {
                        Debug.Log($"П��ь го�о� � �ы �ож�: И��р�ц�й {iterations}");

                        yield return StartCoroutine(Pause());

                        foreach (var item in ebos)
                        {
                            print("шо з� н�х");
                            WorldGenerator.Inst.SetBlockAndUpdateChunck(item, 0);
                        }
                        foreach (var item in path)
                        {
                            WorldGenerator.Inst.SetBlockAndUpdateChunck(item, 94);
                        }
                    }

                    callback(path);
                    yield break;
                }

                openSet.Remove(current.position);
                closedSet.Add(current.position);

                if (ebobo)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(current.position, 94);
                }

                foreach (var dir in allowedDirections)
                {
                    Vector3Int neighborPos = current.position + dir;
                    if (closedSet.Contains(neighborPos))
                        continue;

                    if (current == startNode)
                    {
                        var up3Pos = current.position + (Vector3Int.up * 3);
                        if (WorldGenerator.Inst.GetBlockID(up3Pos) != 0)
                        {
                            if (dir.y > 0)
                            {
                                Debug.Log("Н� �� �б��ь");
                                continue;
                            }
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
                        //Vector3 neighborF = new Vector3(neighborPos.x, neighborPos.y, neighborPos.z);
                        //// Е�л� яч�й�� н� �хо��� � blueprint � з�ня�� (н� п����), проп�����м �
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

                        // Про��ря�м, ч�о н�� яч�й�ой ��обо�но ��� яч�й��
                        if (upBlockID != 0 || up2BlockID != 0 || up3BlockID != 0)
                            continue;

                        // Но��я про��р��: ��л� �ж� � openSet �л� closedSet ���ь но�� н� ��� �л���� ���рх о� ��н������, проп�����м �го
                        Vector3Int aboveCandidate = neighborPos + Vector3Int.up * 2;
                        if (openSet.ContainsKey(aboveCandidate) || closedSet.Contains(aboveCandidate))
                        {
                            //Debug.Log("�озможно ��о�� �бр��ь э�� про��р��");
                            continue;
                        }


                        var agentIntPos = transform.position.ToIntPos();
                        agentIntPos.x++;

                        if (agentIntPos + Vector3Int.up == neighborPos || agentIntPos + (Vector3Int.up * 2) == neighborPos)
                        {
                            continue;
                        }
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
                        neighbor.hCost = ManhattanDistance(neighborPos, goal);
                        neighbor.parent = current;
                        openSet.Add(neighborPos, neighbor);

                        if (ebobo)
                        {
                            WorldGenerator.Inst.SetBlockAndUpdateChunck(neighborPos, 10);
                            ebos.Add(neighborPos);
                            yield return StartCoroutine(Pause());
                        }
                    }
                }

                //foreach (var dir in allowedDirections)
                //{
                //    Vector3Int neighborPos = current.position + dir;
                //    if (closedSet.Contains(neighborPos))
                //        continue;

                //    // Е�л� яч�й�� з�ня�� ч�р��жом, проп�����м �
                //    Vector3 neighborFloat = new Vector3(neighborPos.x, neighborPos.y, neighborPos.z);
                //    if (blueprintPositions.Contains(neighborFloat))
                //        continue;

                //    // Про��ря�м прохо��мо��ь: яч�й�� � яч�й�� ���рх� �олжны бы�ь п���ым�
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
            Debug.Log("П��ь н� н�й��н :(");
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
        //            yield return null; // ��ём �р�мя �ор���н�

        //        if (iterations > maxIterations)
        //        {
        //            Debug.LogWarning("AStarPathCoroutine: �о���гн�� м����м�м ���р�ц�й, �озможный ц��л.");
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
                    yield return null; // ��ём �р�мя �ор���н�

                if (iterations > maxIterations)
                {
                    Debug.Log("AStarPathCoroutine: �о���гн�� м����м�м ���р�ц�й, �озможный ц��л.");
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

                    // Е�л� яч�й�� �хо��� � blueprint, �ч����м � прохо��мой пр� ��ло���, ч�о н�� н�й ��обо�но 2 яч�й��
                    if (blueprintPositions.Contains(neighborF))
                    {
                        if (WorldGenerator.Inst.GetBlockID(neighborPos + Vector3Int.up) != 0 ||
                            WorldGenerator.Inst.GetBlockID(neighborPos + Vector3Int.up * 2) != 0)
                            continue;
                    }
                    else
                    {
                        // Е�л� яч�й�� н� �хо��� � blueprint, он� �олжн� бы�ь полно��ью п���ой,
                        // � н�� н�й – ��обо�но ��� яч�й��
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
            // Пр��о��м cell � Vector3 (ц�лоч��л�нный) � �р��н����м
            Vector3 cellF = new Vector3(cell.x, cell.y, cell.z);
            return blueprintPositions.Contains(cellF);
        }

        bool isPaused = false;
        private IEnumerator Pause(string msg = "")
        {
            isPaused = true;
            print($"{gameObject} п�з�з� ... {msg}");

            while (isPaused)
            {
                yield return null;
            }
        }


    }
}
