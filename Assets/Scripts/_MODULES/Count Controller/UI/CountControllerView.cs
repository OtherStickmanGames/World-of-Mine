using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System;

public class CountControllerView : MonoBehaviour
{
    [SerializeField] TMP_Text labelCount;
    [SerializeField] Button btnDecrease;
    [SerializeField] Button btnIncrease;

    public int Value { get; set; } = 1;

    public void Init()
    {
        btnDecrease.onClick.AddListener(Decrease_Clicked);
        btnIncrease.onClick.AddListener(Increase_Clicked);

        UpdateCountView();
    }

    private void Increase_Clicked()
    {
        Value++;
        UpdateCountView();
    }

    private void Decrease_Clicked()
    {
        Value--;
        UpdateCountView();
    }

    public void UpdateCountView()
    {
        labelCount.SetText($"{Value}");

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(labelCount.transform.parent as RectTransform);
    }
}
