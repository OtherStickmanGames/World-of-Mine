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
        public float buildRange = 3.0f;       // Максимальное расстояние, на котором NPC может установить блок
        public float approachDistance = 1.0f; // Допустимое расстояние при подходе к точке установки
        public byte scaffoldingBlockID = 1;   // ID временного блока для опоры (scaffolding)
        public int verticalGapThreshold = 5;  // Если зазор меньше или равен этому порогу, строим вертикальную колонну

        [SerializeField] TextAsset buildingData;

        PlayerBehaviour player;
        NavMeshAgent agent;
        List<BlockData> blueprint;

        private Vector3 currentBuildingBasePosition;


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
            currentBuildingBasePosition = basePosition; // Сохраняем базовую позицию для расчёта габаритов постройки


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

        private IEnumerator BuildLadderForBlock(Vector3 destination)
        {
            // Вычисляем габариты постройки на основе blueprint и базовой позиции
            GetBuildingBounds(blueprint, currentBuildingBasePosition, out Vector3 buildingCenter, out float buildingRadius, out var size);

            var edge = GetClosestEdge(currentBuildingBasePosition, size, destination);

            print(edge + " =-=-=-=-=");

            // Ищем точку выхода за пределами постройки (safeDistance = 1, чтобы лестница была «прилипшей» к постройке)
            Vector3 ladderBase = FindExitPoint(buildingCenter, buildingRadius, 1f);
            
            // Округляем координаты базы лестницы (до целого значения)
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

            Debug.Log("Лестница будет строиться в точке (округлено): " + ladderBase);

            var isUpLadder = transform.position.y - 1 < ladderBase.y;
            scaffoldingBlockID = isUpLadder ? (byte)92 : (byte)61;

            // Начинаем строить лестницу снизу вверх
            float startY = Mathf.Min(transform.position.y-1, ladderBase.y); // Берем минимальный уровень (на случай, если нужно строить и вниз)
            float endY = Mathf.Max(transform.position.y-1, ladderBase.y);  // Берем максимальный уровень (куда нужно добраться)

            bool placedAnyBlocks = false;

            float currentY = Mathf.Floor(startY); // Стартуем с ближайшего нижнего целого

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


            WorldGenerator.Inst.SetBlockAndUpdateChunck(ladderBase, 61);

            WorldGenerator.Inst.SetBlockAndUpdateChunck(startLadderPos, 92);
            yield return new WaitForSeconds(1f); // Задержка между шагами

            Debug.LogError(height);

            yield break;

            while (Vector3.Distance(ladderBase, startLadderPos) > 0.3f)
            {
                if (WorldGenerator.Inst.GetBlockID(startLadderPos) == 0)
                {
                    WorldGenerator.Inst.SetBlockAndUpdateChunck(startLadderPos, scaffoldingBlockID);
                    Debug.Log("Установлен блок лестницы на " + startLadderPos);
                    placedAnyBlocks = true;
                }

                // Перемещаем NPC к следующему шагу лестницы
                Vector3 nextStepPos = startLadderPos - dir;//new Vector3(ladderBase.x, currentY + 1f, ladderBase.z);
                yield return StartCoroutine(MoveToPosition(nextStepPos, false));
                yield return new WaitForSeconds(0.5f); // Задержка между шагами

                startLadderPos -= dir;
            }

            if (placedAnyBlocks)
            {
                Debug.Log("Лестница построена, NPC может добраться до " + destination);
            }
            else
            {
                Debug.Log("Лестница не требовалась: путь уже свободен или NPC на нужной высоте.");
            }

            yield return null;

            //WorldGenerator.Inst.SetBlockAndUpdateChunck(ladderBase, 61);
            //yield break;
        }

        public Edge GetClosestEdge(Vector3 buildingPosition, Vector3 size, Vector3 destination)
        {
            // Размеры постройки, получаем минимальные и максимальные координаты по осям X, Y, Z
            Vector3 halfSize = size / 2;

            // Определяем границы постройки
            Vector3 minBounds = buildingPosition;
            Vector3 maxBounds = buildingPosition + size;

            // Вычисляем расстояния до каждой границы
            float distanceToLeft = Mathf.Abs(destination.x - minBounds.x);
            float distanceToRight = Mathf.Abs(destination.x - maxBounds.x);
            float distanceToFront = Mathf.Abs(destination.z - maxBounds.z);
            float distanceToBack = Mathf.Abs(destination.z - minBounds.z);
            float distanceToTop = Mathf.Abs(destination.y - maxBounds.y);
            float distanceToBottom = Mathf.Abs(destination.y - minBounds.y);

            // Находим минимальное расстояние и возвращаем соответствующий край
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
        //    // 1. Получаем границы постройки
        //    GetBuildingBounds(blueprint, currentBuildingBasePosition, out Vector3 buildingCenter, out float buildingRadius);

        //    // 2. Ищем ближайшую точку у постройки (ladderBase), чтобы лестница к ней шла
        //    Vector3 ladderBase = FindExitPoint(buildingCenter, buildingRadius, 1f);

        //    yield break;

        //    ladderBase = new Vector3(
        //        Mathf.Round(ladderBase.x),
        //        Mathf.Round(ladderBase.y),
        //        Mathf.Round(ladderBase.z)
        //    );

        //    Debug.Log($"Цель лестницы (destination): {destination}, точка у постройки (ladderBase): {ladderBase}");

        //    // 3. Вычисляем стартовую точку, учитывая, что мы начинаем строить лестницу ниже текущего положения агента
        //    Vector3 currentPos = new Vector3(
        //        Mathf.Round(transform.position.x),
        //        Mathf.Round(transform.position.y) - 1, // на 1 блок ниже, чтобы упереться в землю
        //        Mathf.Round(transform.position.z)
        //    );

        //    // 4. Если уже на месте - не строим
        //    if (currentPos == ladderBase)
        //    {
        //        Debug.Log("Лестница не требуется - уже на позиции.");
        //        yield break;
        //    }

        //    // 5. Шагаем в сторону цели, от нижней точки до ladderBase
        //    int stepX = ladderBase.x > currentPos.x ? 1 : (ladderBase.x < currentPos.x ? -1 : 0);
        //    int stepZ = ladderBase.z > currentPos.z ? 1 : (ladderBase.z < currentPos.z ? -1 : 0);
        //    int stepY = ladderBase.y > currentPos.y ? 1 : -1;

        //    // 6. Проверяем границы здания и строим лестницу
        //    bool placedAnyBlocks = false;

        //    while (currentPos.y != ladderBase.y)
        //    {
        //        // Находим текущую позицию ступеньки
        //        Vector3 ladderBlockPos = new Vector3(
        //            Mathf.Round(currentPos.x),
        //            Mathf.Round(currentPos.y),
        //            Mathf.Round(currentPos.z)
        //        );

        //        // Проверяем, не выходит ли текущая точка за пределы здания
        //        if (Mathf.Abs(ladderBlockPos.x - buildingCenter.x) > buildingRadius ||
        //            Mathf.Abs(ladderBlockPos.z - buildingCenter.z) > buildingRadius)
        //        {
        //            Debug.Log("Ступенька выходит за пределы здания, прекращаем построение");
        //            break;
        //        }

        //        // Если блок пустой, ставим лестницу
        //        if (WorldGenerator.Inst.GetBlockID(ladderBlockPos) == 0)
        //        {
        //            WorldGenerator.Inst.SetBlockAndUpdateChunck(ladderBlockPos, scaffoldingBlockID);
        //            Debug.Log($"Поставлен блок лестницы на {ladderBlockPos}");
        //            placedAnyBlocks = true;
        //        }

        //        // Двигаемся по лестнице
        //        yield return StartCoroutine(MoveToPosition(ladderBlockPos, false));
        //        yield return new WaitForSeconds(0.1f);

        //        // Поднимаемся на шаг по Y
        //        currentPos.y += stepY;

        //        // Если находимся ниже target по высоте, двигаемся по диагонали
        //        if (Mathf.Abs(ladderBase.x - currentPos.x) > Mathf.Abs(ladderBase.z - currentPos.z))
        //        {
        //            currentPos.x += stepX;  // Шагаем по X
        //        }
        //        else
        //        {
        //            currentPos.z += stepZ;  // Шагаем по Z
        //        }

        //        // Коррекция погрешностей
        //        if (Mathf.Abs(currentPos.y - ladderBase.y) < 0.1f) currentPos.y = ladderBase.y;
        //        if (Mathf.Abs(currentPos.x - ladderBase.x) < 0.1f) currentPos.x = ladderBase.x;
        //        if (Mathf.Abs(currentPos.z - ladderBase.z) < 0.1f) currentPos.z = ladderBase.z;
        //    }

        //    if (placedAnyBlocks)
        //    {
        //        Debug.Log($"Лестница успешно построена до {ladderBase}");
        //    }
        //    else
        //    {
        //        Debug.Log("Лестница не требовалась, путь свободен.");
        //    }


        //}




        private IEnumerator MoveToPosition(Vector3 destination, bool canBuildLadder = true)
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();

            // Сначала пробуем построить путь
            NavMeshPath path = new NavMeshPath();
            agent.CalculatePath(destination, path);

            // Если путь не полный и мы ещё не пробовали строить лестницу — строим
            if (canBuildLadder && path.status != NavMeshPathStatus.PathComplete)
            {
                Debug.Log($"MoveToPosition: Путь до {destination} не найден (PathComplete = {path.status}). Пытаемся построить лестницу.");
                yield return StartCoroutine(BuildLadderForBlock(destination));

                // После строительства лестницы пробуем ещё раз, но уже без повторного строительства
                yield return StartCoroutine(MoveToPosition(destination, false));
                yield break;
            }

            // Устанавливаем путь
            agent.SetPath(path);

            // Локальные переменные для проверки "застревания"
            float noMovementTimeout = 5f;       // Время, после которого считаем, что NPC «застрял» физически (не двигается)
            float noProgressTimeout = 5f;       // Время, после которого считаем, что NPC «застрял по прогрессу» (движется, но не становится ближе)
            float stuckTimer = 0f;             // Счётчик для физического застревания
            float progressTimer = 0f;          // Счётчик для отсутствия прогресса
            Vector3 lastPosition = agent.transform.position;
            float lastDistanceToDest = (path.corners.Length > 0)
                ? Vector3.Distance(agent.transform.position, path.corners[path.corners.Length - 1])
                : Vector3.Distance(agent.transform.position, destination);

            // Цикл ожидания, пока агент не достигнет цели
            while (agent.pathPending || agent.remainingDistance > approachDistance)
            {
                // 1) Проверка «физического» движения (не стоит ли агент на месте)
                float distanceMoved = Vector3.Distance(agent.transform.position, lastPosition);
                bool isMoving = distanceMoved > 0.01f;
                if (!isMoving)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > noMovementTimeout)
                    {
                        Debug.Log($"MoveToPosition: Агент физически застрял у {agent.transform.position}, не двигается к {destination}.");

                        // Если можем строить лестницу — пробуем
                        if (canBuildLadder)
                        {
                            yield return StartCoroutine(BuildLadderForBlock(destination));
                            yield return StartCoroutine(MoveToPosition(destination, false));
                        }
                        yield break;
                    }
                }
                else
                {
                    stuckTimer = 0f;
                }

                // 2) Проверка «прогресса» (сокращается ли расстояние до конечной точки)
                float currentDistanceToDest = agent.remainingDistance; // или вычислять по path.corners
                if (currentDistanceToDest >= lastDistanceToDest - 0.05f)
                {
                    // Расстояние не уменьшилось (или даже увеличилось)
                    progressTimer += Time.deltaTime;
                    if (progressTimer > noProgressTimeout)
                    {
                        Debug.Log($"MoveToPosition: Агент не приближается к {destination}, текущее расстояние = {currentDistanceToDest}");

                        // Если можем строить лестницу — пробуем
                        if (canBuildLadder)
                        {
                            yield return StartCoroutine(BuildLadderForBlock(destination));
                            yield return StartCoroutine(MoveToPosition(destination, false));
                        }
                        yield break;
                    }
                }
                else
                {
                    // Есть прогресс — сбрасываем таймер
                    progressTimer = 0f;
                }
                lastDistanceToDest = currentDistanceToDest;
                lastPosition = agent.transform.position;

                yield return null;
            }
        }


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

    }
}
