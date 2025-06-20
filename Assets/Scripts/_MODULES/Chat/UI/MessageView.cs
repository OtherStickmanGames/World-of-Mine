using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class MessageView : MonoBehaviour
{
    [SerializeField] TMP_Text txtMessage;
    [SerializeField] Image outline;

    public void Init(string username, string message)
    {
        var msg = $"<color=#DED24D>{username}</color>: {message}";
        txtMessage.SetText(msg);
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);

        LeanTween.value(gameObject, a => 
        {
            var c = outline.color;
            c.a = a;
            outline.color = c;
        }, 1, 0, 0.8f).setEaseOutQuad();
    }
}
