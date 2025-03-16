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
        public float buildRange = 3.0f;       // Максимальное расстояние, на котором NPC может установить блок
        public float approachDistance = 1.0f; // Допустимое расстояние при подходе к точке установки
        public byte scaffoldingBlockID = 1;   // ID временного блока для опоры (scaffolding)
        public int verticalGapThreshold = 5;  // Если зазор меньше или равен этому порогу, строим вертикальную колонну

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
            Debug.Log("Блоков в чертеже: " + blueprint.Count);
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
                // Вычисляем точку, откуда начнём строительство (можно задать по логике игры)
                var playerNearPos = player.transform.position + player.transform.forward + Vector3.up;
                StartCoroutine(BuildHouse(playerNearPos, blueprint));
            }
        }

        // Главный метод строительства дома по чертежу (blueprint)
        public IEnumerator BuildHouse(Vector3 basePosition, List<BlockData> blueprint)
        {
            // Создаём набор позиций, где будут строиться блоки (глобальные координаты)
            HashSet<Vector3> blueprintPositions = new HashSet<Vector3>();
            foreach (BlockData block in blueprint)
            {
                blueprintPositions.Add(basePosition + block.localPosition);
            }

            // Сортируем блоки по высоте (фундамент, затем стены, крыша и т.д.)
            List<BlockData> orderedBlueprint = OrderBlueprint(blueprint);

            foreach (BlockData block in orderedBlueprint)
            {
                Vector3 globalPos = basePosition + block.localPosition;

                // 1. Если на месте уже есть блок, не соответствующий чертеже, очищаем место
                yield return StartCoroutine(ClearObstructionsAt(globalPos, block));

                //// 2. Если блок не пустой и под ним нет опоры, обеспечиваем доступ
                //if (block.blockID != 0 && !IsSupported(globalPos))
                //{
                //    yield return StartCoroutine(BuildSmartScaffolding(globalPos, blueprintPositions));
                //}

                // 3. Находим точку подхода через NavMesh и перемещаемся туда
                Vector3 approachPos = FindApproachPosition(globalPos);
                //if (approachPos == globalPos)
                //{
                    
                //    yield return StartCoroutine(BuildSmartScaffolding(globalPos, blueprintPositions));

                //}
                yield return StartCoroutine(MoveToPosition(approachPos));

                // 4. Если NPC достаточно близко, устанавливаем блок
                if (Vector3.Distance(transform.position, globalPos) <= buildRange)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(globalPos, block.blockID);
                }
                else
                {
                    
                    Debug.Log("NPC не смог подойти достаточно близко для установки блока: " + globalPos);
                }

                // Задержка для плавности строительства
                yield return new WaitForSeconds(0.2f);
            }


            // В конце метода BuildHouse, сразу после завершения установки всех блоков:
            //GetBuildingBounds(blueprint, basePosition, out Vector3 buildingCenter, out float buildingRadius);
            //Vector3 exitPos = FindExitPoint(buildingCenter, buildingRadius, 5f);
            //Debug.Log("Найден выход за постройкой: " + exitPos);
            //// Если NPC находится на крыше, то сначала построим лестницу для спуска:
            //yield return StartCoroutine(EnsureDescentLadder(exitPos));
            //// Затем переместимся к найденной точке выхода:
            //yield return StartCoroutine(MoveToPosition(exitPos));
        }

        // Если в целевой позиции уже есть блок, не входящий в чертеж, удаляем его
        private IEnumerator ClearObstructionsAt(Vector3 globalPos, BlockData targetBlock)
        {
            byte currentID = WorldGenerator.Inst.GetBlockID(globalPos);
            if (currentID != 0 && currentID != targetBlock.blockID)
            {
                WorldGenerator.Inst.SetBlockAndUpdateChunck(globalPos, 0);
                yield return null;
            }
        }

        // Поиск точки подхода на NavMesh, в пределах buildRange от целевой позиции
        private Vector3 FindApproachPosition(Vector3 targetPos)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, buildRange, NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return targetPos;
        }

        // Сортировка блоков по высоте (от низших к высшим)
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

            // Считаем, что движение началось
            Vector3 lastPosition = agent.transform.position;

            while (agent.pathPending || agent.remainingDistance > approachDistance)
            {
                // Проверяем, двигается ли агент
                float distanceMoved = Vector3.Distance(agent.transform.position, lastPosition);
                bool isMoving = distanceMoved > 0.01f; // Порог для определения "движения", можно настроить

                if (!isMoving)
                {
                    stuckTimer += Time.deltaTime;

                    if (stuckTimer > timeout)
                    {
                        Debug.Log("MoveToPosition: Агент застрял при попытке добраться до " + destination);
                        // Здесь можно добавить логику по постройке лестницы или обхода препятствия
                        break;
                    }
                }
                else
                {
                    stuckTimer = 0f; // Сброс таймера, если агент продолжает движение
                }

                lastPosition = agent.transform.position;

                yield return null;
            }
        }

        
    }
}
