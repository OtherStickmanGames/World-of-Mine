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

        // Главный метод строительства дома по чертежу (blueprint)
        public IEnumerator BuildHouse(Vector3 basePosition, List<BlockData> blueprint)
        {
            // Сортируем блоки по высоте (сначала фундамент, затем стены, крыша и т.д.)
            List<BlockData> orderedBlueprint = OrderBlueprint(blueprint);

            foreach (BlockData block in orderedBlueprint)
            {
                // Вычисляем глобальную позицию блока: базовая точка + локальные координаты
                Vector3 globalPos = basePosition + block.localPosition;

                // 1. Если на месте установки уже есть блок, который не соответствует чертежу, очищаем место
                yield return StartCoroutine(ClearObstructionsAt(globalPos, block));

                // 2. Если блок не пустой и под ним нет опоры, нужно обеспечить доступ:
                if (block.blockID != 0 && !IsSupported(globalPos))
                {
                    // Строим умное опорное сооружение (если зазор большой – лестницу, иначе – вертикальную колонну)
                    yield return StartCoroutine(BuildSmartScaffolding(globalPos));
                }

                // 3. Находим подходящую точку для подхода к позиции установки с помощью NavMesh
                Vector3 approachPos = FindApproachPosition(globalPos);
                yield return StartCoroutine(MoveToPosition(approachPos));

                // 4. Если NPC достаточно близко, устанавливаем блок
                if (Vector3.Distance(transform.position, globalPos) <= buildRange)
                {
                    WorldGenerator.Inst.SetBlock(globalPos, block.blockID);
                }
                else
                {
                    Debug.LogWarning("NPC не смог подойти достаточно близко для установки блока: " + globalPos);
                }

                // Небольшая задержка между установкой блоков для плавности
                yield return new WaitForSeconds(0.2f);
            }
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
                WorldGenerator.Inst.SetBlock(globalPos, 0);
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
        private IEnumerator BuildSmartScaffolding(Vector3 targetPos)
        {
            int gap = GetVerticalGap(targetPos);
            if (gap <= verticalGapThreshold)
            {
                // Строим вертикальную колонну: ставим блоки непосредственно под целевым до обнаружения опоры
                Vector3 scaffoldPos = targetPos + Vector3.down;
                while (!IsSupported(scaffoldPos))
                {
                    WorldGenerator.Inst.SetBlock(scaffoldPos, scaffoldingBlockID);
                    yield return null; // даем миру время обновиться
                    scaffoldPos += Vector3.down;
                }
            }
            else
            {
                // Строим диагональную лестницу (широкую опору), чтобы NPC мог подъехать и спуститься
                // Определяем оптимальное направление для строительства лестницы
                Vector3 chosenDir = DetermineStairDirection(targetPos, gap);
                if (chosenDir == Vector3.zero)
                {
                    // Если не удалось подобрать направление, используем стандартное (вперёд)
                    chosenDir = Vector3.forward;
                }
                Vector3 scaffoldPos = targetPos;
                // Строим до тех пор, пока не достигнем поддержки
                while (!IsSupported(scaffoldPos))
                {
                    scaffoldPos += (chosenDir + Vector3.down);
                    WorldGenerator.Inst.SetBlock(scaffoldPos, scaffoldingBlockID);
                    yield return null;
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
                        // "Стоимость" направления – количество шагов до опоры
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

        // Перемещение NPC к заданной позиции с использованием NavMeshAgent
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
