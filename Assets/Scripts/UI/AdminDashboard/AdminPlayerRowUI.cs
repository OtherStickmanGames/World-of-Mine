using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class AdminPlayerRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtUsername;
    [SerializeField] private TextMeshProUGUI txtSessions;
    [SerializeField] private TextMeshProUGUI txtTotalTime;
    [SerializeField] private TextMeshProUGUI txtMaxTime;
    [SerializeField] private TextMeshProUGUI txtAvgFps;
    [SerializeField] private TextMeshProUGUI txtMinFps;

    [SerializeField] private Image imgStatus;
    [SerializeField] private Button btnDelete;

    private PlayerDashboardStat currentStat;

    private void Awake()
    {
        if (txtUsername == null) throw new NullReferenceException("txtUsername is not assigned.");
        if (txtSessions == null) throw new NullReferenceException("txtSessions is not assigned.");
        if (txtTotalTime == null) throw new NullReferenceException("txtTotalTime is not assigned.");
        if (txtMaxTime == null) throw new NullReferenceException("txtMaxTime is not assigned.");
        if (txtAvgFps == null) throw new NullReferenceException("txtAvgFps is not assigned.");
        if (txtMinFps == null) throw new NullReferenceException("txtMinFps is not assigned.");
        if (btnDelete == null) throw new NullReferenceException("btnDelete is not assigned.");
        if (imgStatus == null) throw new NullReferenceException("imgStatus is not assigned.");

        btnDelete.onClick.AddListener(OnDeleteClicked);
    }

    public void Setup(PlayerDashboardStat stat)
    {
        currentStat = stat;
        txtUsername.text = stat.Nickname.ToString();
        txtSessions.text = stat.SessionCount.ToString();
        txtTotalTime.text = FormatTime(stat.TotalPlaytimeHours);
        txtMaxTime.text = FormatTime(stat.MaxSessionTimeHours);
        txtAvgFps.text = stat.AvgFps.ToString();
        txtMinFps.text = stat.MinFps.ToString();

        imgStatus.color = stat.IsOnline ? Color.green : new Color(0.7f, 0.7f, 0.7f, 1f);
    }

    private void OnDeleteClicked()
    {
        // Tell the main dashboard UI to show confirmation for this player
        AdminDashboardUI.Instance.ShowConfirmDelete(currentStat.Nickname.ToString(), currentStat.PlayerID.ToString());
    }

    private string FormatTime(float hours)
    {
        TimeSpan t = TimeSpan.FromHours(hours);
        return string.Format("{0:D2}h {1:D2}m", t.Hours + (t.Days * 24), t.Minutes);
    }
}
