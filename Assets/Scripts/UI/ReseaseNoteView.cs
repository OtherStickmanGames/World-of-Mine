using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System;

public class ReseaseNoteView : MonoBehaviour
{
    [SerializeField] Button btnPlay;
    [SerializeField] Button btnPause;
    [SerializeField] TMP_Text txtTitle;
    [SerializeField] TMP_Text txtDate;
    [SerializeField] TMP_Text txtDescription;
    [SerializeField] SurveyVariantItem surveyVariantPrefab;
    [SerializeField] Transform surveyParent;

    public void Init()
    {
        btnPlay.onClick.AddListener(BtnPlay_Clicked);
        btnPause.onClick.AddListener(BtnPause_Clicked);
    }

    public void Fill(string title, string date, string description)
    {
        
        txtTitle.SetText(title);
        txtDate.SetText(date);
        txtDescription.SetText(description);

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }

    private void BtnPause_Clicked()
    {

    }

    private void BtnPlay_Clicked()
    {

    }
}
