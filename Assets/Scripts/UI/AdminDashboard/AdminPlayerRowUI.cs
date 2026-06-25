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
    [SerializeField] private TextMeshProUGUI txtDevices;
    [SerializeField] private TextMeshProUGUI txtLastSeen;

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
        if (txtDevices == null) throw new NullReferenceException("txtDevices is not assigned.");
        if (txtLastSeen == null) throw new NullReferenceException("txtLastSeen is not assigned.");
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
        
        // Display max session time in minutes only
        int maxMinutes = Mathf.RoundToInt(stat.MaxSessionTimeHours * 60f);
        txtMaxTime.text = $"{maxMinutes}m";

        txtAvgFps.text = stat.AvgFps.ToString();
        txtMinFps.text = stat.MinFps.ToString();
        txtDevices.text = stat.Devices.ToString();
        
        txtLastSeen.text = GetRelativeTime(stat.LastSessionTicks);

        imgStatus.color = stat.IsOnline ? Color.green : new Color(0.7f, 0.7f, 0.7f, 1f);
    }

    private string GetRelativeTime(long ticks)
    {
        if (ticks <= 0) return "---";

        DateTime lastDate = new DateTime(ticks);
        TimeSpan span = DateTime.Now - lastDate;

        if (span.TotalSeconds < 60) return "Только что";
        if (span.TotalMinutes < 60) return $"{Mathf.FloorToInt((float)span.TotalMinutes)}м. назад";
        if (span.TotalHours < 24) return $"{Mathf.FloorToInt((float)span.TotalHours)}ч. назад";
        if (span.TotalDays < 2) return "Вчера";
        if (span.TotalDays < 7) return $"{Mathf.FloorToInt((float)span.TotalDays)}д. назад";

        return lastDate.ToString("dd.MM.yyyy");
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
