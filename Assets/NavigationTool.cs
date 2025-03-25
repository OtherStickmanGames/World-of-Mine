using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class NavigationTool
{
    public static Vector3 FindApproachPositionOnBlock(Vector3 globalBlockPos, float distance = 1f)
    {
        //var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        //go.transform.position = globalBlockPos;
        //go.transform.localScale *= 0.3f;
        //go.name = "ИННЫА";

        globalBlockPos += (Vector3.one * 0.5f) + (Vector3.up * 0.51f) + Vector3.left;
        
        if (NavMesh.SamplePosition(globalBlockPos, out var hit, distance, NavMesh.AllAreas))
        {
            //Debug.Log($"Да, я возвращаю, эту хуйню {hit.position}");
            return hit.position;
        }

        Debug.Log("Не нашел точки на навмеше");
        return globalBlockPos;
    }

    public static Vector3 FindApproachPositionOnBlock(Vector3 globalBlockPos, out bool founded, float distance = 1f)
    {
        //var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        //go.transform.position = globalBlockPos;
        //go.transform.localScale *= 0.3f;
        //go.name = "ИННЫА";

        globalBlockPos += (Vector3.one * 0.5f) + (Vector3.up * 0.51f) + Vector3.left;

        if (NavMesh.SamplePosition(globalBlockPos, out var hit, distance, NavMesh.AllAreas))
        {
            //Debug.Log($"Да, я возвращаю, эту хуйню {hit.position}");
            founded = true;
            return hit.position;
        }

        Debug.Log("Не нашел точки на навмеше");
        founded = false;
        return globalBlockPos;
    }
}
