using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] PlayerBehaviour playerPrefab;

    private IEnumerator Start()
    {
        var deb = GameObject.FindGameObjectWithTag("TxtDebugo").GetComponent<TMPro.TMP_Text>();
        deb.text += $"Ûûûûûû\n";

        PlayerPrefs.DeleteKey("inventory");

        yield return new WaitForSeconds(0.3f);

        var player = Instantiate(playerPrefab);
        deb.text += $"{player} ===\n";

        Destroy(player.GetComponent<NetworkPlayer>());

        WorldGenerator.Inst.AddPlayer(player.transform);

    }

    private void Update()
    {
        var deb = GameObject.FindGameObjectWithTag("TxtDebugo").GetComponent<TMPro.TMP_Text>();
        deb.text += $"{Time.deltaTime}\n";
    }
}
