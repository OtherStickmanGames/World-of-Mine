using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AdminDashboardUIGenerator
{
    private const int DEFAULT_LAYER = 0; // Matching the scene's manual setup
    private const int UI_LAYER = 5;

    [MenuItem("Tools/Noob Online/Generate Admin Dashboard UI")]
    public static void GenerateUI()
    {
        // 1. Find or Create Dedicated Admin Canvas
        GameObject canvasGo = GameObject.Find("AdminCanvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("AdminCanvas");
            canvasGo.layer = DEFAULT_LAYER;
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; 
            
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 600);
            scaler.matchWidthOrHeight = 1.0f;
            
            canvasGo.AddComponent<GraphicRaycaster>();
        }
        else
        {
            // Clean up existing UI if we are regenerating
            var oldUI = canvasGo.transform.Find("AdminDashboardUI");
            if (oldUI != null) GameObject.DestroyImmediate(oldUI.gameObject);
        }

        Canvas rootCanvas = canvasGo.GetComponent<Canvas>();

        // Ensure EventSystem exists (using FindAnyObjectByType for better reliability)
        if (GameObject.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.layer = DEFAULT_LAYER;
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            
            // Default to New Input System module
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // 2. Dashboard Manager (Singleton)
        AdminDashboardManager manager = GameObject.FindObjectOfType<AdminDashboardManager>();
        if (manager == null)
        {
            GameObject managerGo = new GameObject("AdminDashboardManager");
            managerGo.AddComponent<AdminDashboardManager>();
            managerGo.AddComponent<Unity.Netcode.NetworkObject>();
        }

        // 3. Dashboard UI Root
        GameObject dashboardGo = new GameObject("AdminDashboardUI");
        dashboardGo.layer = DEFAULT_LAYER;
        dashboardGo.transform.SetParent(rootCanvas.transform, false);
        var dashboardRect = dashboardGo.AddComponent<RectTransform>();
        dashboardRect.anchorMin = Vector2.zero;
        dashboardRect.anchorMax = Vector2.one;
        dashboardRect.offsetMin = Vector2.zero;
        dashboardRect.offsetMax = Vector2.zero;
        var uiScript = dashboardGo.AddComponent<AdminDashboardUI>();

        // 4. Panel
        GameObject panelGo = new GameObject("DashboardPanel");
        panelGo.layer = DEFAULT_LAYER;
        panelGo.transform.SetParent(dashboardGo.transform, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.1f);
        panelRect.anchorMax = new Vector2(0.9f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // 5. Tabs Area
        GameObject tabsGo = new GameObject("TabsArea");
        tabsGo.layer = DEFAULT_LAYER;
        tabsGo.transform.SetParent(panelGo.transform, false);
        var tabsRect = tabsGo.AddComponent<RectTransform>();
        tabsRect.anchorMin = new Vector2(0, 0.9f);
        tabsRect.anchorMax = new Vector2(1, 1);
        tabsRect.offsetMin = Vector2.zero;
        tabsRect.offsetMax = Vector2.zero;
        var tabsLayout = tabsGo.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.childAlignment = TextAnchor.MiddleLeft;
        tabsLayout.padding = new RectOffset(10, 10, 10, 10);
        tabsLayout.spacing = 10;
        tabsLayout.childForceExpandWidth = false;

        GameObject btnPlayersGo = CreateButton("Btn_PlayersTab", tabsGo.transform, "Игроки");

        // 5.1 Server FPS Display
        GameObject srvFpsGo = new GameObject("Txt_ServerFPS");
        srvFpsGo.layer = UI_LAYER;
        srvFpsGo.transform.SetParent(panelGo.transform, false);
        var srvFpsRect = srvFpsGo.AddComponent<RectTransform>();
        srvFpsRect.anchorMin = new Vector2(1, 1);
        srvFpsRect.anchorMax = new Vector2(1, 1);
        srvFpsRect.pivot = new Vector2(1, 1);
        srvFpsRect.anchoredPosition = new Vector2(-20, -15);
        srvFpsRect.sizeDelta = new Vector2(200, 30);
        var srvFpsTmp = srvFpsGo.AddComponent<TextMeshProUGUI>();
        srvFpsTmp.text = "Server FPS: ---";
        srvFpsTmp.alignment = TextAlignmentOptions.Right;
        srvFpsTmp.fontSize = 18;
        srvFpsTmp.fontStyle = FontStyles.Bold;

        // 6. Content Area (Scroll View)
        GameObject scrollGo = new GameObject("Scroll View");
        scrollGo.layer = DEFAULT_LAYER;
        scrollGo.transform.SetParent(panelGo.transform, false);
        var scrollRectTr = scrollGo.AddComponent<RectTransform>();
        scrollRectTr.anchorMin = new Vector2(0, 0);
        scrollRectTr.anchorMax = new Vector2(1, 0.85f); // Leave room for header
        scrollRectTr.offsetMin = new Vector2(10, 10);
        scrollRectTr.offsetMax = new Vector2(-10, -10);
        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.scrollSensitivity = 0.3f; // Adjusted sensitivity for better balance
        scrollGo.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);

        GameObject viewportGo = new GameObject("Viewport");
        viewportGo.layer = DEFAULT_LAYER;
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var viewportRect = viewportGo.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportGo.AddComponent<RectMask2D>();
        scrollRect.viewport = viewportRect;

        GameObject contentGo = new GameObject("Content");
        contentGo.layer = DEFAULT_LAYER;
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        
        // NO LAYOUT GROUPS HERE - Manual positioning for virtualization
        scrollRect.content = contentRect;

        // 7. Header Row (Static, above ScrollView)
        GameObject headerGo = CreateRow("HeaderRow", panelGo.transform, true);
        var headerRect = headerGo.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 0.85f);
        headerRect.anchorMax = new Vector2(1, 0.9f);
        headerRect.offsetMin = new Vector2(10, 0);
        headerRect.offsetMax = new Vector2(-10, 0);

        // 8. Prefab Row (Temp construction)
        GameObject rowPrefabGo = CreateRow("PlayerRowPrefab", null, false);
        var rowScript = rowPrefabGo.AddComponent<AdminPlayerRowUI>();
        
        // Add Online Status Indicator (as first child)
        GameObject statusGo = new GameObject("Img_Status");
        statusGo.layer = UI_LAYER;
        statusGo.transform.SetParent(rowPrefabGo.transform, false);
        statusGo.transform.SetAsFirstSibling();
        var statusImg = statusGo.AddComponent<Image>();
        var statusRect = statusGo.GetComponent<RectTransform>();
        statusRect.sizeDelta = new Vector2(statusRect.sizeDelta.x, 30); // Matching correct height
        var statusLayout = statusGo.AddComponent<LayoutElement>();
        statusLayout.minWidth = 15;
        statusLayout.minHeight = 15;
        statusLayout.preferredWidth = 15;
        statusLayout.preferredHeight = 15;

        // Add Delete Button to Row
        GameObject btnDelGo = CreateButton("Btn_Delete", rowPrefabGo.transform, "X");
        btnDelGo.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 1f);
        var delRect = btnDelGo.GetComponent<RectTransform>();
        delRect.sizeDelta = new Vector2(delRect.sizeDelta.x, 30); // Matching correct height
        var delLayout = btnDelGo.GetComponent<LayoutElement>();
        delLayout.minWidth = 40;
        delLayout.preferredWidth = 40;

        // Link prefab fields via SerializedObject
        var texts = rowPrefabGo.GetComponentsInChildren<TextMeshProUGUI>();
        var serializedObject = new SerializedObject(rowScript);
        serializedObject.FindProperty("txtUsername").objectReferenceValue = texts[0];
        serializedObject.FindProperty("txtSessions").objectReferenceValue = texts[1];
        serializedObject.FindProperty("txtTotalTime").objectReferenceValue = texts[2];
        serializedObject.FindProperty("txtMaxTime").objectReferenceValue = texts[3];
        serializedObject.FindProperty("txtAvgFps").objectReferenceValue = texts[4];
        serializedObject.FindProperty("txtMinFps").objectReferenceValue = texts[5];
        serializedObject.FindProperty("txtDevices").objectReferenceValue = texts[6];
        serializedObject.FindProperty("txtLastSeen").objectReferenceValue = texts[7];
        serializedObject.FindProperty("btnDelete").objectReferenceValue = btnDelGo.GetComponent<Button>();
        serializedObject.FindProperty("imgStatus").objectReferenceValue = statusImg;
        serializedObject.ApplyModifiedProperties();

        // Save Prefab
        if (!System.IO.Directory.Exists("Assets/Prefabs/UI")) System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
        
        string prefabPath = "Assets/Prefabs/UI/AdminPlayerRowPrefab.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rowPrefabGo, prefabPath);
        GameObject.DestroyImmediate(rowPrefabGo);

        // 9. Confirmation Dialog
        GameObject confirmPanelGo = new GameObject("ConfirmPanel");
        confirmPanelGo.layer = DEFAULT_LAYER;
        confirmPanelGo.transform.SetParent(dashboardGo.transform, false);
        var confirmRect = confirmPanelGo.AddComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.3f, 0.3f);
        confirmRect.anchorMax = new Vector2(0.7f, 0.7f);
        confirmRect.offsetMin = Vector2.zero;
        confirmRect.offsetMax = Vector2.zero;
        confirmPanelGo.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var shadow = confirmPanelGo.AddComponent<Outline>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(2, -2);

        GameObject confirmTxtGo = new GameObject("Text_Prompt");
        confirmTxtGo.transform.SetParent(confirmPanelGo.transform, false);
        var ctRect = confirmTxtGo.AddComponent<RectTransform>();
        ctRect.anchorMin = new Vector2(0.1f, 0.4f);
        ctRect.anchorMax = new Vector2(0.9f, 0.9f);
        ctRect.offsetMin = Vector2.zero;
        ctRect.offsetMax = Vector2.zero;
        var ctTmp = confirmTxtGo.AddComponent<TextMeshProUGUI>();
        ctTmp.text = "Confirm?";
        ctTmp.alignment = TextAlignmentOptions.Center;
        ctTmp.fontSize = 20;

        GameObject btnYesGo = CreateButton("Btn_Yes", confirmPanelGo.transform, "ДА");
        var byRect = btnYesGo.GetComponent<RectTransform>();
        byRect.anchorMin = new Vector2(0.1f, 0.1f);
        byRect.anchorMax = new Vector2(0.45f, 0.35f);
        byRect.offsetMin = Vector2.zero;
        byRect.offsetMax = Vector2.zero;
        btnYesGo.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f, 1f);

        GameObject btnNoGo = CreateButton("Btn_No", confirmPanelGo.transform, "НЕТ");
        var bnRect = btnNoGo.GetComponent<RectTransform>();
        bnRect.anchorMin = new Vector2(0.55f, 0.1f);
        bnRect.anchorMax = new Vector2(0.9f, 0.35f);
        bnRect.offsetMin = Vector2.zero;
        bnRect.offsetMax = Vector2.zero;

        // 10. Assign UI Script fields
        var uiSerializedObj = new SerializedObject(uiScript);
        uiSerializedObj.FindProperty("dashboardPanel").objectReferenceValue = panelGo;
        uiSerializedObj.FindProperty("scrollRect").objectReferenceValue = scrollRect;
        uiSerializedObj.FindProperty("rowPrefab").objectReferenceValue = prefab.GetComponent<AdminPlayerRowUI>();
        uiSerializedObj.FindProperty("btnPlayersTab").objectReferenceValue = btnPlayersGo.GetComponent<Button>();
        uiSerializedObj.FindProperty("txtServerFps").objectReferenceValue = srvFpsTmp;
        
        uiSerializedObj.FindProperty("confirmPanel").objectReferenceValue = confirmPanelGo;
        uiSerializedObj.FindProperty("confirmText").objectReferenceValue = ctTmp;
        uiSerializedObj.FindProperty("btnConfirmYes").objectReferenceValue = btnYesGo.GetComponent<Button>();
        uiSerializedObj.FindProperty("btnConfirmNo").objectReferenceValue = btnNoGo.GetComponent<Button>();
        uiSerializedObj.ApplyModifiedProperties();

        EditorUtility.SetDirty(uiScript);
        Debug.Log("Admin Dashboard UI Generated Successfully! Don't forget to save the scene.");
    }

    private static GameObject CreateButton(string name, Transform parent, string text)
    {
        GameObject btnGo = new GameObject(name);
        btnGo.layer = UI_LAYER;
        btnGo.transform.SetParent(parent, false);
        btnGo.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
        var btn = btnGo.AddComponent<Button>();
        var layout = btnGo.AddComponent<LayoutElement>();
        layout.minWidth = 150;
        layout.minHeight = 40;

        GameObject txtGo = new GameObject("Text");
        txtGo.layer = UI_LAYER;
        txtGo.transform.SetParent(btnGo.transform, false);
        var rect = txtGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 18;

        return btnGo;
    }

    private static GameObject CreateRow(string name, Transform parent, bool isHeader)
    {
        GameObject rowGo = new GameObject(name);
        rowGo.layer = UI_LAYER;
        if (parent != null) rowGo.transform.SetParent(parent, false);
        
        var rect = rowGo.AddComponent<RectTransform>();
        // Set anchors to Top-Horizontal stretch
        rect.anchorMin = new Vector2(0, 1f);
        rect.anchorMax = new Vector2(1, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0, 40);
        
        var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.spacing = 10;
        
        var layoutElem = rowGo.AddComponent<LayoutElement>();
        layoutElem.minHeight = 40;

        if (isHeader)
        {
            rowGo.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);
            
            // Add padding for header to align with rows that have a status dot
            // The dot is 15px + 10px spacing = 25px offset
            GameObject spacer = new GameObject("Header_Spacer");
            spacer.transform.SetParent(rowGo.transform, false);
            var sl = spacer.AddComponent<LayoutElement>();
            sl.minWidth = 15;
            sl.preferredWidth = 15;
        }

        string[] columns = { "Имя", "Сессии", "Общее �ремя", "Макс. �ремя", "Ср. FPS", "Мин. FPS", "Устройст�о", "Был � сети" };
        foreach (var col in columns)
        {
            GameObject colGo = new GameObject(col);
            colGo.layer = UI_LAYER;
            colGo.transform.SetParent(rowGo.transform, false);
            var tmp = colGo.AddComponent<TextMeshProUGUI>();
            tmp.text = isHeader ? col : "---";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 8; // Starting font size as seen in prefab
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 8;
            tmp.fontSizeMax = 18;
            if (isHeader) tmp.fontStyle = FontStyles.Bold;
        }

        if (isHeader)
        {
            // Spacer for the delete button in header
            GameObject delSpacer = new GameObject("Header_DelSpacer");
            delSpacer.transform.SetParent(rowGo.transform, false);
            var dsl = delSpacer.AddComponent<LayoutElement>();
            dsl.minWidth = 40;
            dsl.preferredWidth = 40;
        }

        return rowGo;
    }
}
