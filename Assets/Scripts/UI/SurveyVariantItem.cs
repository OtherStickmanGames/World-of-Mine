using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using System;

public class SurveyVariantItem : MonoBehaviour
{
    [SerializeField] private Button btnItem;
    [SerializeField] private TMP_Text title;
    [SerializeField] RectTransform bar;
    [SerializeField] GameObject markerNoSelect;
    [SerializeField] GameObject markerSelected;

    [HideInInspector] public UnityEvent<SurveyVariantItem> onClick;

    public void Init(string text)
    {
        title.SetText(text);

        NoChoose();

        btnItem.onClick.AddListener(Item_Clicked);

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }

    private void Item_Clicked()
    {
        onClick?.Invoke(this);
    }

    public void NoChoose()
    {
        markerNoSelect.SetActive(true);
        markerSelected.SetActive(false);
        bar.gameObject.SetActive(false);
    }

    public void NoSelect()
    {
        markerNoSelect.SetActive(false);
        markerSelected.SetActive(false);
        bar.gameObject.SetActive(true);
    }

    public void Select()
    {
        markerNoSelect.SetActive(false);
        markerSelected.SetActive(true);
        bar.gameObject.SetActive(true);
    }
}
