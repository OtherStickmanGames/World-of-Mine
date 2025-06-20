using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class MessageView : MonoBehaviour
{
    [SerializeField] TMP_Text txtMessage;
    [SerializeField] Image[] showEffects;

    public void Init(string username, string message)
    {
        var msg = $"<color=#DED24D>{username}</color>: {message}";
        txtMessage.SetText(msg);
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);

        var seq = LeanTween.sequence();

        seq.append(LeanTween.value(gameObject, a =>
        {
            foreach (var image in showEffects)
            {
                var c = image.color;
                c.a = a;
                image.color = c;
            }
        }, 0, 1, 0.15f).setEaseOutQuad());

        seq.append(LeanTween.value(gameObject, a => 
        {
            foreach (var image in showEffects)
            {
                var c = image.color;
                c.a = a;
                image.color = c;
            }
        }, 1, 0, 0.8f).setEaseOutQuad());
    }
}
