using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine;
#if !UNITY_WEBGL
using System.IO;
#endif


[Preserve]
public class UserData
{
    public string userName;
  
    public Vector3 position;

    public bool tutorialComplete;
    public bool tutorialSkiped;

    static string userDataPath = $"{DataPath}/Data/UserData.json";

#if UNITY_WEBGL && YG_PLUGIN_YANDEX_GAME
    public static bool dataIsLoaded = false;
#endif

    static string DataPath
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Application.persistentDataPath;
#endif
            return Application.dataPath;
        }
    }

    public static UserData Owner
    {
        get
        {
            if (owner == null)
            {
                owner = new();

#if !UNITY_WEBGL
                if (File.Exists(userDataPath))
                {
                    var json = File.ReadAllText(userDataPath);
                    owner = JsonUtility.FromJson<UserData>(json);
                }
#endif

#if UNITY_WEBGL

#if YG_PLUGIN_YANDEX_GAME
//                if (YG.YandexGame.savesData.userData != null)
//                {
//                    owner = YG.YandexGame.savesData.userData;
//                }
                LoadData();
#endif

#endif

            }

            return owner;
        }
    }

    private static UserData owner;

    public UserData()
    {
        //LoadData();
    }

    public static void LoadData()
    {
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

#if !UNITY_WEBGL
        if (!File.Exists(userDataPath))
            return;

        var json = File.ReadAllText(userDataPath);
        owner = JsonUtility.FromJson<UserData>(json);
#endif

#if UNITY_WEBGL
        owner.userName = YG.YandexGame.savesData.nickname;
        owner.tutorialComplete = YG.YandexGame.savesData.tutorialComplete;
        owner.tutorialSkiped = YG.YandexGame.savesData.tutorialSkiped;
        owner.position = YG.YandexGame.savesData.position;
#endif
        //Debug.Log(userData.userName);
    }

    public void SaveData()
    {
#if !UNITY_WEBGL
        Directory.CreateDirectory($"{DataPath}/Data/");
        var json = JsonUtility.ToJson(this);
        File.WriteAllText(userDataPath, json);
#endif

#if YG_PLUGIN_YANDEX_GAME
        YG.YandexGame.savesData.nickname = owner.userName;
        YG.YandexGame.savesData.tutorialComplete = owner.tutorialComplete;
        YG.YandexGame.savesData.tutorialSkiped = owner.tutorialSkiped;
        YG.YandexGame.savesData.position = owner.position;
        YG.YandexGame.SaveProgress();
#endif

#if UNITY_EDITOR
        //UnityEditor.AssetDatabase.Refresh();
#endif
    }
}
