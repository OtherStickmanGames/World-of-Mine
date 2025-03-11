using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.Events;

public class ReseaseNoteView : MonoBehaviour
{
    [SerializeField] Button btnPlay;
    [SerializeField] Button btnPause;
    [SerializeField] TMP_Text txtTitle;
    [SerializeField] TMP_Text txtDate;
    [SerializeField] TMP_Text txtDescription;
    [SerializeField] SurveyVariantItem surveyVariantPrefab;
    [SerializeField] Transform surveyParent;

    [HideInInspector] public UnityEvent<int> onSurveyClick;

    List<SurveyVariantItem> surveyItems = new();

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

        ClearSurvey();

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }

    internal void FillSurvey(NetworkSurveyData[] survey)
    {
        foreach (var surveyItem in survey)
        {
            var view = Instantiate(surveyVariantPrefab, surveyParent);
            view.Init(surveyItem.title);
            view.SetVotes(surveyItem.votes);
            view.onClick.AddListener(Survey_Clicked);

            surveyItems.Add(view);
        }
    }

    private void Survey_Clicked(SurveyVariantItem surveyView)
    {
        var votes = surveyItems.Select(s => s.Votes).ToArray();
        var votesPercentge = CalculatePercentageDistribution(votes);

        var idx = 0;
        foreach (var view in surveyItems)
        {
            view.SetVotesPercent(votesPercentge[idx]);
            idx++;

            if (view == surveyView)
                continue;

            view.NoSelect();
        }

        var idxSurvey = surveyView.transform.GetSiblingIndex();
        ReleaseNotesHandler.Singleton.SurveySelect(txtDate.text, idxSurvey);
        //onSurveyClick?.Invoke(surveyView.transform.GetSiblingIndex());
    }

    private void BtnPause_Clicked()
    {

    }

    private void BtnPlay_Clicked()
    {

    }

    public static float[] CalculatePercentageDistribution(int[] votes)
    {
        // Суммируем все голоса
        int totalVotes = 0;
        foreach (int vote in votes)
        {
            totalVotes += vote;
        }

        // Создаем массив для хранения процентных значений
        float[] percentages = new float[votes.Length];

        // Если сумма равна 0, то возвращаем массив с нулевыми значениями, чтобы избежать деления на 0
        if (totalVotes == 0)
        {
            return percentages;
        }

        // Рассчитываем процент для каждого элемента
        for (int i = 0; i < votes.Length; i++)
        {
            percentages[i] = (float)votes[i] / totalVotes * 100f;
        }

        return percentages;
    }


    private void ClearSurvey()
    {
        foreach (Transform item in surveyParent)
        {
            Destroy(item.gameObject);
        }
    }
}
