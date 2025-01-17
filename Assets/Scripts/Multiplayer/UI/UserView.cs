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

        InitUserName();
    }

    private void Client_Started()
    {
        inputUserName.interactable = false;
    }

    private void InitUserName()
    {
        StartCoroutine(Delay());

        IEnumerator Delay()
        {
            yield return new WaitForSeconds(0.1f);

#if UNITY_WEBGL && YG_PLUGIN_YANDEX_GAME
            while (!YG.YandexGame.SDKEnabled)
            {
                yield return new WaitForEndOfFrame();
            }
#endif

            Debug.Log(UserData.Owner.userName + "  -=-==-=-=-=-");
            var userData = UserData.Owner;

            //Debug.Log(string.IsNullOrEmpty(userData.userName));
            if (string.IsNullOrEmpty(userData.userName))
            {
                inputUserName.text = $"Скиталец {new System.Random().Next(0, 9)}{new System.Random().Next(0, 9)}{new System.Random().Next(0, 9)}";
            }
            else
            {
                inputUserName.SetTextWithoutNotify(userData.userName);
            }
        }

        
    }

    bool needSave;
    float saveTimer;

    private void Name_Changed(string value)
    {
        UserData.Owner.userName = value;
        if (!needSave)
        {
            UserData.Owner.SaveData();
            needSave = true;
        }
        else
        {
            needSave = true;
            saveTimer = 0;
        }
    }

    private void Update()
    {
        if (needSave)
        {
            saveTimer += Time.deltaTime;

            if (saveTimer > 1.8f)
            {
                UserData.Owner.SaveData();
                needSave = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            
            UserData.LoadData();
            Debug.Log(UserData.Owner.userName + " +++++++++", this);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            print(UserData.Owner.userName + " ----------");

            YG.YandexGame.LoadProgress();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            YG.YandexGame.ResetSaveProgress();
            YG.YandexGame.SaveProgress();
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.OnClientStarted -= Client_Started;
        }
    }
}
