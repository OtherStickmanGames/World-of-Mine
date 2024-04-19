using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class UserView : MonoBehaviour
{
    [SerializeField] TMP_InputField inputUserName;


    public void Init()
    {
        inputUserName.onValueChanged.AddListener(Name_Changed);

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            UserData.Owner.LoadData();
        }
    }
}
