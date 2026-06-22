using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Ururu
{
    // Структура, описывающая данные блока в чертеже (локальная позиция и id блока)
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

                // 2. Если блок не пустой и под ним нет опоры, обеспечиваем доступ
                if (block.blockID != 0 && !IsSupported(globalPos))
                {
                    yield return StartCoroutine(BuildSmartScaffolding(globalPos, blueprintPositions));
                }

                // 3. Находим точку подхода через NavMesh и перемещаемся туда
                Vector3 approachPos = FindApproachPosition(globalPos);
                yield return StartCoroutine(MoveToPosition(approachPos));

                // 4. Если NPC достаточно близко, устанавливаем блок
                if (Vector3.Distance(transform.position, globalPos) <= buildRange)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(globalPos, block.blockID);
                }
                else
                {
                    Debug.LogWarning("NPC не смог подойти достаточно близко для установки блока: " + globalPos);
                }

                // Задержка для плавности строительства
                yield return new WaitForSeconds(0.2f);
            }

           
            // В конце метода BuildHouse, сразу после завершения установки всех блоков:
            GetBuildingBounds(blueprint, basePosition, out Vector3 buildingCenter, out float buildingRadius);
            Vector3 exitPos = FindExitPoint(buildingCenter, buildingRadius, 5f);
            Debug.Log("Найден выход за постройкой: " + exitPos);
            // Если NPC находится на крыше, то сначала построим лестницу для спуска:
            yield return StartCoroutine(EnsureDescentLadder(exitPos));
            // Затем переместимся к найденной точке выхода:
            yield return StartCoroutine(MoveToPosition(exitPos));
        }

        // Сортировка блоков по высоте (от низших к высшим)
        private List<BlockData> OrderBlueprint(List<BlockData> blueprint)
        {
            return blueprint.OrderBy(b => b.localPosition.y).ToList();
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

        // Проверка: имеется ли поддержка под заданной позицией
        private bool IsSupported(Vector3 globalPos)
        {
            Vector3 belowPos = globalPos + Vector3.down;
            return WorldGenerator.Inst.GetBlockID(belowPos) != 0;
        }

        // Возвращает вертикальный зазор (количество пустых блоков) под позицией до ближайшей опоры
        private int GetVerticalGap(Vector3 pos)
        {
            int gap = 0;
            Vector3 checkPos = pos + Vector3.down;
            // Ограничиваем поиск 100 блоками, чтобы избежать бесконечного цикла
            while (WorldGenerator.Inst.GetBlockID(checkPos) == 0 && gap < 100)
            {
                gap++;
                checkPos += Vector3.down;
            }
            return gap;
        }

        // Построение умного опорного сооружения (scaffolding)
        // Если зазор невелик, строится вертикальная колонна, иначе – диагональная лестница
        private IEnumerator BuildSmartScaffolding(Vector3 targetPos, HashSet<Vector3> blueprintPositions)
        {
            int gap = GetVerticalGap(targetPos);
            if (gap <= verticalGapThreshold)
            {
                // Вертикальная колонна: идём вниз от targetPos
                Vector3 scaffoldPos = targetPos + Vector3.down;
                while (true)
                {
                    // Если данная позиция запланирована в чертеже, не ставим scaffolding
                    if (blueprintPositions.Contains(scaffoldPos))
                        break;

                    WorldGenerator.Inst.SetBlockAndUpdateChunck(scaffoldPos, scaffoldingBlockID);
                    yield return null;

                    // Если под следующим блоком уже есть опора, завершаем построение
                    if (WorldGenerator.Inst.GetBlockID(scaffoldPos + Vector3.down) != 0)
                        break;

                    scaffoldPos += Vector3.down;
                }
            }
            else
            {
                // Диагональная лестница: выбираем оптимальное направление для построения
                Vector3 chosenDir = DetermineStairDirection(targetPos, gap);
                if (chosenDir == Vector3.zero)
                {
                    chosenDir = Vector3.forward;
                }
                Vector3 scaffoldPos = targetPos;
                while (true)
                {
                    scaffoldPos += (chosenDir + Vector3.down);

                    // Если позиция scaffoldPos зарезервирована под будущий блок – прекращаем строительство опоры
                    if (blueprintPositions.Contains(scaffoldPos))
                        break;

                    WorldGenerator.Inst.SetBlockAndUpdateChunck(scaffoldPos, scaffoldingBlockID);
                    yield return null;

                    if (WorldGenerator.Inst.GetBlockID(scaffoldPos + Vector3.down) != 0)
                        break;
                }
            }
        }

        // Определение оптимального горизонтального направления для строительства диагональной лестницы
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
                // Симуляция строительства по диагонали до достижения опоры
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

        // Перемещение NPC к заданной позиции с использованием NavMeshAgent с таймаутами и логированием
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
                    Debug.LogWarning("MoveToPosition: Таймаут при попытке добраться до " + destination);
                    // Можно добавить здесь логику по построению временной лестницы
                    break;
                }
                yield return null;
            }
        }

        // Метод для вычисления габаритов постройки (bounding box) и центра постройки
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
            // Для выхода нас интересуют только горизонтальные размеры
            buildingRadius = Mathf.Max(size.x, size.z) * 0.5f;
        }

        // Поиск точки выхода на NavMesh за пределами постройки, исходя из центра и радиуса постройки
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
            // Если не нашли подходящую точку, возвращаем центр постройки
            return buildingCenter;
        }

        // Новый метод, который строит лестницу для спуска, если NPC находится выше точки выхода:
        private IEnumerator EnsureDescentLadder(Vector3 exitPoint)
        {
            Debug.Log("Проверка необходимости строительства лестницы для спуска.");
            // Пока разница по высоте больше 1 блока, строим ступеньки:
            while (transform.position.y - exitPoint.y > 1f)
            {
                Vector3 nextStep = transform.position + Vector3.down;
                // Если под NPC пусто, ставим временный блок для лестницы:
                if (WorldGenerator.Inst.GetBlockID(nextStep) == 0)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(nextStep, scaffoldingBlockID);
                    Debug.Log("Установлен блок лестницы на " + nextStep);
                }
                // Перемещаем NPC на следующий шаг (к ближайшей доступной точке):
                yield return StartCoroutine(MoveToPosition(nextStep));
                yield return new WaitForSeconds(0.1f);
            }
            Debug.Log("Лестница для спуска построена, NPC теперь ниже.");
            yield return null;
        }
    }
}

