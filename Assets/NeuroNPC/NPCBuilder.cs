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
        public float buildRange = 3.0f; // Максимальное расстояние, на котором NPC может установить блок
        public float approachDistance = 1.0f; // Расстояние, на котором NPC считает, что достиг точки установки
        public byte scaffoldingBlockID = 1; // ID временного блока для опоры (можно задать любой подходящий)

        // Главный метод строительства дома
        public IEnumerator BuildHouse(Vector3 basePosition, List<BlockData> blueprint)
        {
            // 1. Отсортировать блоки так, чтобы строить снизу вверх (фундамент -> стены -> крыша)
            List<BlockData> orderedBlueprint = OrderBlueprint(blueprint);

            // Проходим по каждому блоку из чертежа
            foreach (BlockData block in orderedBlueprint)
            {
                // Вычисляем глобальную позицию блока: базовая точка + локальные координаты блока
                Vector3 globalPos = basePosition + block.localPosition;

                // 2. Если в целевой позиции есть другие блоки (не входящие в конструкцию) – убрать их
                yield return StartCoroutine(ClearObstructionsAt(globalPos, block));

                // 3. Если блок должен ставиться в воздухе, проверяем наличие поддержки
                if (!IsSupported(globalPos) && block.blockID != 0)
                {
                    // Если под целевой позицией нет опоры, строим временную подпорку (scaffolding)
                    yield return StartCoroutine(BuildScaffolding(globalPos));
                }

                // 4. Определяем точку подхода: позиция, доступная по NavMesh и находящаяся в пределах buildRange от globalPos
                Vector3 approachPos = FindApproachPosition(globalPos);
                // Перемещаем NPC к этой точке
                yield return StartCoroutine(MoveToPosition(approachPos));

                // 5. Если NPC находится достаточно близко к глобальной позиции, устанавливаем блок
                if (Vector3.Distance(transform.position, globalPos) <= buildRange)
                {
                    // Установка блока через генератор мира
                    WorldGenerator.Inst.SetBlock(globalPos, block.blockID);
                }
                else
                {
                    Debug.LogWarning("NPC не смог подойти достаточно близко для установки блока: " + globalPos);
                }

                // Краткая задержка между установками (можно настроить для анимации и плавности)
                yield return new WaitForSeconds(0.2f);
            }
        }

        // Сортировка блоков по высоте (сначала нижние блоки, потом верхние)
        private List<BlockData> OrderBlueprint(List<BlockData> blueprint)
        {
            return blueprint.OrderBy(b => b.localPosition.y).ToList();
        }

        // Проверка: существует ли поддерживающий блок под данной позицией
        private bool IsSupported(Vector3 globalPos)
        {
            Vector3 belowPos = globalPos + Vector3.down;
            // Предполагаем, что любой блок с id != 0 служит опорой
            return WorldGenerator.Inst.GetBlockID(belowPos) != 0;
        }

        // Построение временных опор до уровня, на котором можно установить целевой блок
        private IEnumerator BuildScaffolding(Vector3 targetPos)
        {
            Vector3 scaffoldPos = targetPos;
            // Ищем опору, спускаясь вниз, пока не найдём блок
            while (!IsSupported(scaffoldPos))
            {
                scaffoldPos += Vector3.down;
                // Устанавливаем временный блок опоры
                WorldGenerator.Inst.SetBlock(scaffoldPos, scaffoldingBlockID);
                yield return null; // даём время на обновление мира
            }
        }

        // Если целевая позиция уже занята другим блоком, который не соответствует плану, удаляем его
        private IEnumerator ClearObstructionsAt(Vector3 globalPos, BlockData targetBlock)
        {
            byte currentID = WorldGenerator.Inst.GetBlockID(globalPos);
            // Если на месте установки уже есть блок, отличный от целевого (например, земля, камень и т.п.)
            if (currentID != 0 && currentID != targetBlock.blockID)
            {
                // Удаляем (выкапываем) мешающий блок
                WorldGenerator.Inst.SetBlock(globalPos, 0);
                yield return null; // даём время на обновление мира
            }
        }

        // Поиск точки подхода, где NPC может безопасно встать, чтобы установить блок
        private Vector3 FindApproachPosition(Vector3 targetPos)
        {
            NavMeshHit hit;
            // Пробуем найти ближайшую точку на NavMesh в пределах buildRange от целевой позиции
            if (NavMesh.SamplePosition(targetPos, out hit, buildRange, NavMesh.AllAreas))
            {
                return hit.position;
            }
            // Если не найдено – возвращаем саму целевую позицию
            return targetPos;
        }

        // Перемещение NPC к заданной позиции с использованием NavMeshAgent
        private IEnumerator MoveToPosition(Vector3 destination)
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            agent.SetDestination(destination);
            // Ждём, пока NPC не приблизится к цели
            while (agent.pathPending || agent.remainingDistance > approachDistance)
            {
                yield return null;
            }
        }
    }
}
