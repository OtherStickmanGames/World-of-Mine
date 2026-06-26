using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using System;

public class UserView : MonoBehaviour
{
    [SerializeField] TMP_InputField inputUserName;


    public void Init()
    {
        var nav = inputUserName.navigation;
        nav.mode = Navigation.Mode.None;
        inputUserName.navigation = nav;

        inputUserName.onValueChanged.AddListener(Name_Changed);
        inputUserName.onSubmit.AddListener(Submited);
        inputUserName.onSelect.AddListener(Input_Selected);
        inputUserName.onDeselect.AddListener(Input_Deselected);

        NetworkManager.Singleton.OnClientStarted += Client_Started;

        InitUserName();
    }

    private void Submited(string text)
    {
        if (NetworkManager.Singleton && NetworkManager.Singleton.IsClient)
        {
            NetworkUserManager.Instance.UpdateUserNameServerRpc(text);
        }
        DelayShowCursor();
    }

    private void Input_Selected(string text)
    {
        if (InputLogic.Single != null)
        {
            InputLogic.Single.BlockPlayerControl = true;
        }
    }

    private void Input_Deselected(string text)
    {
        if (InputLogic.Single != null)
        {
            InputLogic.Single.BlockPlayerControl = false;
        }

        if (NetworkManager.Singleton && NetworkManager.Singleton.IsClient)
        {
            NetworkUserManager.Instance.UpdateUserNameServerRpc(text);
        }
    }

    private void Client_Started()
    {
        // Allow user to change name after connect
        // inputUserName.interactable = false;
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

            //Debug.Log(UserData.Owner.userName + "  -=-==-=-=-=-");
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

    private void DelayShowCursor()
    {
        if (!Application.isMobilePlatform)
        {
            StartCoroutine(Delay());
        }

        static IEnumerator Delay()
        {
            yield return null;

            Cursor.lockState = CursorLockMode.None;
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

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Y))
        {
            
            UserData.LoadData();
            Debug.Log(UserData.Owner.userName + " +++++++++", this);
        }
#endif
        //if (Input.GetKeyDown(KeyCode.H))
        //{
        //    print(UserData.Owner.userName + " ----------");

        //    YG.YandexGame.LoadProgress();
        //}

        //if (Input.GetKeyDown(KeyCode.R))
        //{
        //    YG.YandexGame.ResetSaveProgress();
        //    YG.YandexGame.SaveProgress();
        //}
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.OnClientStarted -= Client_Started;
        }
    }
}
