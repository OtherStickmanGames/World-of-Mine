using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class UserView : MonoBehaviour
{
    [SerializeField] TMP_InputField inputUserName;


    public void Init()
    {
        inputUserName.onValueChanged.AddListener(Name_Changed);

        NetworkManager.Singleton.OnClientStarted += Client_Started;

        LeanTween.delayedCall(0.1f, InitUserName);
    }

    private void Client_Started()
    {
        inputUserName.interactable = false;
    }

    private void InitUserName()
    {
        var user = UserData.Owner;

        //Debug.Log(string.IsNullOrEmpty(user.userName));
        if (string.IsNullOrEmpty(user.userName))
        {
            inputUserName.text = $"‘–¿≈–Œ  {Random.Range(0, 9)}{Random.Range(0, 9)}{Random.Range(0, 9)}";
        }
        else
        {
            inputUserName.SetTextWithoutNotify(UserData.Owner.userName);
        }
    }

    private void Name_Changed(string value)
    {
        UserData.Owner.userName = value;
        UserData.Owner.SaveData();
    }

    
}
