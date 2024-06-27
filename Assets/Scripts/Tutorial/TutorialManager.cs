using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] PlayerBehaviour playerPrefab;

    private IEnumerator Start()
    {
        PlayerPrefs.DeleteKey("inventory");

        yield return new WaitForSeconds(0.1f);

        var player = Instantiate(playerPrefab);
        Destroy(player.GetComponent<NetworkPlayer>());

        WorldGenerator.Inst.AddPlayer(player.transform);


    }
}
