using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class AdminDashboardUI : MonoBehaviour
{
    public static AdminDashboardUI Instance { get; private set; }

    [SerializeField] private GameObject dashboardPanel;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private AdminPlayerRowUI rowPrefab;
    [SerializeField] private Button btnPlayersTab;

    [SerializeField] private TextMeshProUGUI txtServerFps;

    [Header("Confirmation Dialog")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TextMeshProUGUI confirmText;
    [SerializeField] private Button btnConfirmYes;
    [SerializeField] private Button btnConfirmNo;

    private RectTransform contentRect;
    private List<PlayerDashboardStat> cachedStats = new List<PlayerDashboardStat>();
    private List<AdminPlayerRowUI> rowPool = new List<AdminPlayerRowUI>();
    private const float ROW_HEIGHT = 40f;
    private const int POOL_SIZE = 25; // Enough to cover the visible area + buffer

    private string pendingDeleteID;

    private void Awake()
    {
        Instance = this;

        if (dashboardPanel == null) throw new NullReferenceException("DashboardPanel is not assigned.");
        if (scrollRect == null) throw new NullReferenceException("ScrollRect is not assigned.");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        scrollRect.scrollSensitivity *= 30f; // Significantly increase sensitivity for WebGL browsers
#endif

        if (rowPrefab == null) throw new NullReferenceException("RowPrefab is not assigned.");
        if (btnPlayersTab == null) throw new NullReferenceException("BtnPlayersTab is not assigned.");
        
        if (confirmPanel == null) throw new NullReferenceException("ConfirmPanel is not assigned.");
        if (confirmText == null) throw new NullReferenceException("ConfirmText is not assigned.");
        if (btnConfirmYes == null) throw new NullReferenceException("BtnConfirmYes is not assigned.");
        if (btnConfirmNo == null) throw new NullReferenceException("BtnConfirmNo is not assigned.");

        contentRect = scrollRect.content;
        if (contentRect == null) throw new NullReferenceException("ScrollRect.content is missing.");

        dashboardPanel.SetActive(false);
        confirmPanel.SetActive(false);

        btnPlayersTab.onClick.AddListener(OnPlayersTabClicked);
        btnConfirmYes.onClick.AddListener(OnDeleteConfirmed);
        btnConfirmNo.onClick.AddListener(() => confirmPanel.SetActive(false));

        scrollRect.onValueChanged.AddListener((_) => RefreshView());

        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < POOL_SIZE; i++)
        {
            var row = Instantiate(rowPrefab, contentRect);
            row.gameObject.SetActive(false);
            rowPool.Add(row);
        }
    }

    public void ShowConfirmDelete(string nickname, string playerID)
    {
        pendingDeleteID = playerID;
        confirmText.text = $"Удалить все данные игрока <b>{nickname}</b>?\n<color=red>Это действие необратимо!</color>";
        confirmPanel.SetActive(true);
    }

    private void OnDeleteConfirmed()
    {
        confirmPanel.SetActive(false);
        if (!string.IsNullOrEmpty(pendingDeleteID))
        {
            AdminDashboardManager.Instance.DeletePlayerDataServerRpc(pendingDeleteID);
        }
    }

    private void Start()
    {
        AdminDashboardManager.Instance.OnDataReceived += AppendToTable;
        AdminDashboardManager.Instance.OnClearRequested += ClearTable;
        AdminDashboardManager.Instance.ServerFps.OnValueChanged += OnServerFpsChanged;
        
        OnServerFpsChanged(0, AdminDashboardManager.Instance.ServerFps.Value);
    }

    private void OnDestroy()
    {
        AdminDashboardManager.Instance.OnDataReceived -= AppendToTable;
        AdminDashboardManager.Instance.OnClearRequested -= ClearTable;
        AdminDashboardManager.Instance.ServerFps.OnValueChanged -= OnServerFpsChanged;
    }

    private void OnServerFpsChanged(int previousValue, int newValue)
    {
        txtServerFps.text = $"Server FPS: <color=yellow>{newValue}</color>";
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.G) && Input.GetKeyDown(KeyCode.M))
        {
            ToggleDashboard();
        }
    }

    private void ToggleDashboard()
    {
        bool isActive = !dashboardPanel.activeSelf;
        dashboardPanel.SetActive(isActive);

        if (isActive)
        {
            OnPlayersTabClicked();
        }
    }

    private void OnPlayersTabClicked()
    {
        AdminDashboardManager.Instance.RequestData();
    }

    private void AppendToTable(PlayerDashboardStat[] stats)
    {
        cachedStats.AddRange(stats);
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, cachedStats.Count * ROW_HEIGHT);
        RefreshView();
    }

    private void ClearTable()
    {
        cachedStats.Clear();
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, 0);
        contentRect.anchoredPosition = Vector2.zero;
        RefreshView();
    }

    private void RefreshView()
    {
        if (cachedStats.Count == 0)
        {
            foreach (var row in rowPool) row.gameObject.SetActive(false);
            return;
        }

        // Calculate visible range
        float scrollPos = contentRect.anchoredPosition.y;
        int startIndex = Mathf.Max(0, Mathf.FloorToInt(scrollPos / ROW_HEIGHT));
        
        for (int i = 0; i < rowPool.Count; i++)
        {
            int dataIndex = startIndex + i;
            var row = rowPool[i];

            if (dataIndex < cachedStats.Count)
            {
                row.gameObject.SetActive(true);
                row.Setup(cachedStats[dataIndex]);
                
                // Position row manually
                var rect = row.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(0, -dataIndex * ROW_HEIGHT);
            }
            else
            {
                row.gameObject.SetActive(false);
            }
        }
    }
}
