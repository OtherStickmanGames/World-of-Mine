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
    [SerializeField] private TMP_Text labelPercent;
    [SerializeField] RectTransform bar;
    [SerializeField] GameObject markerNoSelect;
    [SerializeField] GameObject markerSelected;

    [HideInInspector] public UnityEvent<SurveyVariantItem> onClick;
    [HideInInspector] public UnityEvent<SurveyVariantItem> onDeselect;

    public State state;

    public int Votes { get; private set; }

    public void Init(string text)
    {
        title.SetText(text);

        NoChoose();

        btnItem.onClick.AddListener(Item_Clicked);

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }

    public void SetVotes(int value)
    {
        Votes = value;
    }

    private void Item_Clicked()
    {
        Votes++;
        onClick?.Invoke(this);
       
        Select();
    }

    public void NoChoose()
    {
        state = State.NoChoose;
        markerNoSelect.SetActive(true);
        markerSelected.SetActive(false);
        bar.gameObject.SetActive(false);
        labelPercent.gameObject.SetActive(false);
    }

    public void NoSelect()
    {
        if(state is State.Select)
        {
            Votes--;
            onDeselect?.Invoke(this);
        }
        state = State.NoSelect;
        markerNoSelect.SetActive(false);
        markerSelected.SetActive(false);
        bar.gameObject.SetActive(true);
        labelPercent.gameObject.SetActive(true);
    }

    public void Select()
    {
        state = State.Select;
        markerNoSelect.SetActive(false);
        markerSelected.SetActive(true);
        bar.gameObject.SetActive(true);
        labelPercent.gameObject.SetActive(true);
    }

    internal void SetVotesPercent(float percent)
    {
        labelPercent.SetText($"{percent:F0}%");

        SetWidthByPercentage(bar, percent);
    }

    /// <summary>
    /// ������������� ������ RectTransform � ��������� �� ������ ��� ��������.
    /// </summary>
    /// <param name="rectTransform">RectTransform, ������ �������� ���������� ��������.</param>
    /// <param name="percentage">������� �� ������ (100%) ������ �������� (�� 0 �� 100).</param>
    public static void SetWidthByPercentage(RectTransform rectTransform, float percentage)
    {
        // �������� ������� ������������� RectTransform
        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null)
        {
            Debug.LogError("RectTransform �� ����� �������� ��� �������� �� �������� RectTransform.");
            return;
        }

        // �������� ������ ��������
        float parentWidth = parentRect.rect.width;

        // ��������� ����� ������
        float newWidth = parentWidth * (percentage / 100f);

        // ������������� ����� ������, �������� ������� ������
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
    }

    public enum State
    {
        NoChoose,
        Select,
        NoSelect
    }
}
