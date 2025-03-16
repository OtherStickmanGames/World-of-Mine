using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Find Building Place", menuName = "NPC/Actions/Find Building Place")]
public class FindBuildingPlace : CharacterAction
{
    public int distanceRange = 100;

    public override void Execute(Transform character)
    {
        isDone = false;

        var rand = new System.Random();
        int x = (int)character.transform.position.x + rand.Next(-distanceRange, distanceRange);
        int y = (int)character.transform.position.y + rand.Next(-distanceRange, distanceRange);
        int z = (int)character.transform.position.z + rand.Next(-distanceRange, distanceRange);

        var allegedBuildingPos = new Vector3Int(x, y, z);

        var monobeh = character.GetComponent<MonoBehaviour>();
        monobeh.StartCoroutine(Async());

        IEnumerator Async()
        {
            int iteration = 0;
            bool inverted = false;

            while (!(GetBlockID(allegedBuildingPos) == 0
                && GetBlockID(allegedBuildingPos + Vector3Int.up) == 0
                && GetBlockID(allegedBuildingPos + Vector3Int.down) > 0))
            {
                if (iteration < distanceRange * 2)
                {
                    allegedBuildingPos += Vector3Int.up;
                }
                else
                {
                    if (!inverted)
                    {
                        inverted = true;

                        allegedBuildingPos += Vector3Int.down * (distanceRange * 2);
                    }

                    allegedBuildingPos += Vector3Int.down;
                }

                iteration++;

                Debug.Log(allegedBuildingPos);
                yield return null;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PISKA";
            go.transform.position = allegedBuildingPos;

            isDone = true;
        }
    }

    byte GetBlockID(Vector3Int worldBlockPos)
    {
        return WorldGenerator.Inst.GetBlockID(worldBlockPos);
    }


}
