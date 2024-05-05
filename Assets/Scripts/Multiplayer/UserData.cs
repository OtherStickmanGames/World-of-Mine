using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine;
using System.IO;


[Preserve]
public class UserData
{
    public string userName;
  
    public Vector3 position;

    static string userDataPath = $"{DataPath}/Data/UserData.json";

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
                if (File.Exists(userDataPath))
                {
                    var json = File.ReadAllText(userDataPath);
                    owner = JsonUtility.FromJson<UserData>(json);
                }
                else
                {
                    owner = new();
                }
            }

            return owner;
        }
    }

    private static UserData owner;

    public UserData()
    {
        //LoadData();
    }

    public void LoadData()
    {
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
        if (!File.Exists(userDataPath))
            return;

        var json = File.ReadAllText(userDataPath);
        owner = JsonUtility.FromJson<UserData>(json);
        
        //Debug.Log(userData.userName);
    }

    public void SaveData()
    {
        Directory.CreateDirectory($"{DataPath}/Data/");
        var json = JsonUtility.ToJson(this);
        File.WriteAllText(userDataPath, json);
#if UNITY_EDITOR
        //UnityEditor.AssetDatabase.Refresh();
#endif
    }
}
