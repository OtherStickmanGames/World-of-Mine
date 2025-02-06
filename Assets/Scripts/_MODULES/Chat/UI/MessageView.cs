using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class MessageView : MonoBehaviour
{
    [SerializeField] TMP_Text txtMessage;

    public void Init(string username, string message)
    {
        var msg = $"<color=#DED24D>{username}</color>: {message}";
        txtMessage.SetText(msg);
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }
}
