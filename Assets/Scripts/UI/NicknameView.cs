using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NicknameView : MonoBehaviour
{
    [SerializeField] TMP_Text nickname;

    public void Init(NicknameData data)
    {
        nickname.SetText(data.nickname);
    }
}

[System.Serializable]
public struct NicknameData
{
    public string nickname;
}
