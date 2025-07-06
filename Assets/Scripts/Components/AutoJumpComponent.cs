using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoJumpComponent : MonoBehaviour
{
    public float stepHeight = 1f;
    public float stepCheckDistance = 0.5f;
    public float autoJumpSpeed = 5f;
    public float checkHeight = 0.5f;
    public LayerMask blockLayer;
    public Transform marker;
    public Transform marker1;
    public float timeoutThreshold = 0.3f;

    float timeout;

    public bool HandleAutoJump(Vector3 movementDirection)
    {
        timeout += Time.deltaTime;

        //if (timeout < timeoutThreshold && timeout > 0.15f)
        //{
        //    return false;
        //}

        movementDirection.y = 0;
        Vector3 footPosition = transform.position + Vector3.up * checkHeight;
        Vector3 blockCheckPosition = footPosition + movementDirection.normalized * stepCheckDistance;

        // Проверка препятствия прямо перед игроком
        bool isObstacle = Physics.Raycast(footPosition, movementDirection, stepCheckDistance, blockLayer);

        if (!isObstacle)
            return false;

        // Проверка свободного места прямо над препятствием
        Vector3 upperCheckPosition = blockCheckPosition + Vector3.up * stepHeight;
        //bool isFreeAbove = !Physics.Raycast(upperCheckPosition, Vector3.down, 0.1f, blockLayer);

        bool isFreeAbove = Physics.OverlapBox
        (
            upperCheckPosition,
            Vector3.one * 0.3f,
            Quaternion.identity,
            blockLayer
        ).Length == 0;

        marker.position = upperCheckPosition;
        marker1.position = footPosition;

        if (isFreeAbove)
        {
            timeout = 0;
            return true;
        }
        else
        {
            return false;
        }

        //if (isObstacle && isFreeAbove)
        //{
        //    // Триггерим автопрыжок
        //    //VerticalSpeed = autoJumpSpeed;
        //    print("йоба");
        //}

    }
 
}
