using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorTools
{
    public static Vector3 GetRoundedVector(Vector3 vector)
    {
        vector.x = Mathf.Round(vector.x);
        vector.y = Mathf.Round(vector.y);
        vector.z = Mathf.Round(vector.z);

        return vector;
    }

    public static Vector3 GetDominantDirection(Vector3 direction)
    {
        // Сравниваем компоненты по их абсолютной величине
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y) && Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            return new Vector3(Mathf.Sign(direction.x), 0, 0); // Оставляем только X
        }
        else if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x) && Mathf.Abs(direction.y) > Mathf.Abs(direction.z))
        {
            return new Vector3(0, Mathf.Sign(direction.y), 0); // Оставляем только Y
        }
        else
        {
            return new Vector3(0, 0, Mathf.Sign(direction.z)); // Оставляем только Z
        }
    }
}
