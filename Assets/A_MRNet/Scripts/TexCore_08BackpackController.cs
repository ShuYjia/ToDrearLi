using System;
using System.Collections.Generic;
using System.Linq;
using MixedReality.Toolkit.UX;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Backpack chapter for 08 charging-station installation.
/// It reuses the scene's existing MRTK button visual and activates the real
/// source object already referenced by the TexCore step, so the step tree and
/// the backpack cannot drift apart.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_08BackpackController : MonoBehaviour
{
    [Serializable]
    public sealed class Entry
    {
        public string key;
        public string label;
        public GameObject source;
        public GameObject button;
        /// <summary>
        /// true 时步骤启动后对象自动显示，不等待背包按钮召唤；
        /// false 时保持原有逻辑，等待玩家点击背包按钮。
        /// </summary>
        public bool keepSourceVisible;
    }

    [SerializeField] private GameObject backpackPanel;
    [SerializeField] private List<Entry> entries = new List<Entry>();
    [Header("当前物品指引")]
    [SerializeField] private bool showActiveEntryGuidance = true;
    [SerializeField] private Color activeEntryGuidanceColor = new Color(1f, 0.78f, 0.05f, 1f);
    [SerializeField, Range(0f, 0.5f)] private float activeEntryPulseAmount = 0.18f;
    [SerializeField, Min(0f)] private float activeEntryPulseSpeed = 3f;

    private readonly List<PressableButton> pressableButtons = new List<PressableButton>();
    private readonly List<Button> uiButtons = new List<Button>();
    private string activeKey;
    private GameObject activeSource;
    private GameObject activeReporter;
    private bool waitingForSummon;
    private bool sourceWasSummoned;
    private GameObject lastCompletedSource;
    private static TexCore_08BackpackController instance;
    private GameObject entryGuidanceRoot;
    private LineRenderer entryGuidanceOutline;
    private LineRenderer entryGuidanceArrow;
    private LineRenderer entryGuidanceArrowHead;
    private Material entryGuidanceMaterial;

    public IReadOnlyList<Entry> Entries => entries;

    /// <summary>
    /// The chapter panel this controller drives. Exposed so scene migration
    /// verification can confirm 03 keeps a single backpack system on the shared
    /// 背包UI root and still points at the migrated 08 chapter panel.
    /// </summary>
    public GameObject BackpackPanel => backpackPanel;

    private void Awake()
    {
        instance = this;
        EnsureRuntimeConfiguration();
        BindButtons();
        TexCore_BackpackChapterTabs.EnsureInitialized();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureSceneController()
    {
        GameObject backpack = GameObject.Find("背包UI");
        if (backpack != null && backpack.GetComponent<TexCore_08BackpackController>() == null)
        {
            backpack.AddComponent<TexCore_08BackpackController>();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
        UnbindButtons();
        if (entryGuidanceRoot != null)
        {
            if (Application.isPlaying) Destroy(entryGuidanceRoot);
            else DestroyImmediate(entryGuidanceRoot);
        }
        if (entryGuidanceMaterial != null)
        {
            if (Application.isPlaying) Destroy(entryGuidanceMaterial);
            else DestroyImmediate(entryGuidanceMaterial);
        }
    }

    private void Update()
    {
        Entry activeEntry = entries.FirstOrDefault(value => value != null && value.key == activeKey);

        // 自动显示的条目既不参与“等待召唤”的隐藏，也不会在后续帧被
        // 其他步骤的可见性刷新连父级一起关掉。
        if (waitingForSummon &&
            (activeEntry == null || !activeEntry.keepSourceVisible) &&
            activeSource != null && activeSource.activeSelf)
        {
            activeSource.SetActive(false);
        }

        if (activeEntry != null && activeEntry.keepSourceVisible &&
            activeSource != null && !activeSource.activeInHierarchy)
        {
            ActivateSource(activeSource);
        }

        UpdateActiveEntryGuidance();
    }

    public void SetPanel(GameObject value)
    {
        backpackPanel = value;
    }

    public void SetEntries(List<Entry> values)
    {
        entries = values ?? new List<Entry>();
    }

    public static void NotifyStepStarted(GameObject reporter, GameObject source)
    {
        TexCore_08BackpackController controller = FindController();
        if (controller == null)
        {
            return;
        }
        controller.HandleStepStarted(reporter, source);
    }

    public static void NotifyStepEnded(GameObject reporter)
    {
        TexCore_08BackpackController controller = FindController();
        if (controller == null)
        {
            return;
        }
        controller.HandleStepEnded(reporter);
    }

    public static void HideChapter()
    {
        if (instance != null)
        {
            instance.HideChapterInternal();
        }
    }

    public void Show08BackpackChapter()
    {
        ConfigurePanelLayout();
        RefreshEntryLabels();
        TexCore_BackpackChapterTabs.ShowPanel(backpackPanel);
        RebuildPanelLayout();
        ApplyButtonState();
    }

    public void SummonQRCode() => Summon("二维码");
    public void SummonChargingStation() => Summon("充电桩");
    public void SummonDrill() => Summon("手电钻");
    public void SummonExpansionTube() => Summon("膨胀管");
    public void SummonHammer() => Summon("羊角锤");
    public void SummonExpansionScrew() => Summon("膨胀螺丝");
    public void SummonWrench() => Summon("扳手");
    public void SummonWifiAntenna() => Summon("WiFi天线");
    public void SummonPowerPlug() => Summon("电源插头");

    public void Summon(string key)
    {
        if (!string.Equals(activeKey, key, StringComparison.Ordinal))
        {
            ShowHint($"当前步骤不需要{key}");
            return;
        }

        Entry entry = entries.FirstOrDefault(value => value != null && value.key == key);
        GameObject source = activeSource != null ? activeSource : entry?.source;
        if (entry == null || source == null)
        {
            ShowHint($"未配置{key}取件对象");
            return;
        }

        waitingForSummon = false;
        sourceWasSummoned = true;
        ActivateSource(source);
        ShowHint($"已从背包取出{key}");
    }

    private void HandleStepStarted(GameObject reporter, GameObject source)
    {
        if (reporter == null || !Is08Reporter(reporter))
        {
            if (reporter != null && reporter.name.StartsWith("09_", StringComparison.Ordinal))
            {
                activeReporter = null;
                activeKey = string.Empty;
                activeSource = null;
                waitingForSummon = false;
                sourceWasSummoned = false;
                lastCompletedSource = null;
                ApplyButtonState();
                TexCore_BackpackChapterTabs.ShowDefault();
            }
            return;
        }

        Entry entry = entries.FirstOrDefault(value => value != null && value.source == source);
        if (entry == null && source != null)
        {
            entry = entries.FirstOrDefault(value => value != null &&
                value.source != null && value.source.name == source.name);
        }
        if (entry == null && source != null)
        {
            string key = GetKeyForSourceName(source.name);
            entry = entries.FirstOrDefault(value => value != null && value.key == key);
        }
        if (entry == null)
        {
            activeReporter = null;
            activeKey = string.Empty;
            activeSource = null;
            waitingForSummon = false;
            sourceWasSummoned = false;
            lastCompletedSource = null;
            ApplyButtonState();
            return;
        }

        bool reuseSource = source != null && source.activeSelf &&
                           ((lastCompletedSource != null &&
                             lastCompletedSource == source) ||
                            (sourceWasSummoned && activeSource == source));
        lastCompletedSource = null;
        activeReporter = reporter;
        activeSource = source;
        activeKey = entry.key;
        sourceWasSummoned = reuseSource;
        waitingForSummon = activeSource != null && !reuseSource && !entry.keepSourceVisible;
        if (entry.keepSourceVisible && activeSource != null)
        {
            ActivateSource(activeSource);
        }
        Show08BackpackChapter();
    }

    private void HandleStepEnded(GameObject reporter)
    {
        if (reporter == null || !Is08Reporter(reporter))
        {
            return;
        }

        if (activeReporter != null && reporter != activeReporter)
        {
            return;
        }

        if (activeSource != null && activeSource.activeSelf)
        {
            lastCompletedSource = activeSource;
        }

        activeReporter = null;
        activeKey = string.Empty;
        activeSource = null;
        waitingForSummon = false;
        sourceWasSummoned = false;
        ApplyButtonState();
    }

    private void HideChapterInternal()
    {
        activeReporter = null;
        activeKey = string.Empty;
        activeSource = null;
        waitingForSummon = false;
        sourceWasSummoned = false;
        lastCompletedSource = null;
        if (backpackPanel != null)
        {
            backpackPanel.SetActive(false);
        }
        ApplyButtonState();
    }

    private void BindButtons()
    {
        UnbindButtons();
        if (entries == null)
        {
            return;
        }

        foreach (Entry entry in entries)
        {
            if (entry == null || entry.button == null)
            {
                continue;
            }

            SetEntryLabel(entry.button, entry.label);

            PressableButton pressable = entry.button.GetComponentInChildren<PressableButton>(true);
            if (pressable != null)
            {
                string key = entry.key;
                pressable.OnClicked.AddListener(() => Summon(key));
                pressableButtons.Add(pressable);
            }

            Button button = entry.button.GetComponentInChildren<Button>(true);
            if (button != null)
            {
                string key = entry.key;
                button.onClick.AddListener(() => Summon(key));
                uiButtons.Add(button);
            }
        }
        ApplyButtonState();
    }

    private void EnsureRuntimeConfiguration()
    {
        if (backpackPanel == null)
        {
            Transform root = transform;
            Transform configured = root.Find("08充电桩安装背包面板");
            if (configured != null)
            {
                backpackPanel = configured.gameObject;
            }
            else
            {
                Transform sourcePanel = root.Find("06货架安装背包面板");
                if (sourcePanel != null)
                {
                    backpackPanel = Instantiate(sourcePanel.gameObject, root);
                    backpackPanel.name = "08充电桩安装背包面板";
                    backpackPanel.SetActive(false);
                    BuildRuntimeEntries(sourcePanel, backpackPanel.transform);
                }
            }
        }

        EnsureSharedPanelTemplate();

        if (entries.Count == 0)
        {
            BuildRuntimeEntries(null, backpackPanel != null ? backpackPanel.transform : null);
        }

        ConfigurePanelLayout();
    }

    private void EnsureSharedPanelTemplate()
    {
        if (backpackPanel == null || UsesSharedPanelTemplate(backpackPanel))
        {
            return;
        }

        Transform sourcePanel = transform.Find("06货架安装背包面板");
        if (sourcePanel == null)
        {
            return;
        }

        Dictionary<string, GameObject> sourcesByKey = entries
            .Where(entry => entry != null && !string.IsNullOrEmpty(entry.key))
            .GroupBy(entry => entry.key)
            .ToDictionary(group => group.Key, group => group.First().source);
        List<Entry> configuredEntries = entries
            .Where(entry => entry != null && !string.IsNullOrEmpty(entry.key))
            .Select(entry => new Entry
            {
                key = entry.key,
                label = entry.label,
                source = entry.source,
                keepSourceVisible = entry.keepSourceVisible
            })
            .ToList();

        GameObject previousPanel = backpackPanel;
        GameObject clonedPanel = Instantiate(sourcePanel.gameObject, transform);
        clonedPanel.name = previousPanel.name;
        clonedPanel.transform.SetSiblingIndex(previousPanel.transform.GetSiblingIndex());
        clonedPanel.SetActive(false);
        backpackPanel = clonedPanel;

        if (configuredEntries.Count == 0)
        {
            BuildRuntimeEntries(sourcePanel, clonedPanel.transform);
        }
        else
        {
            entries = configuredEntries;
            BindEntriesToSharedButtons(sourcesByKey);
        }

        if (Application.isPlaying)
        {
            Destroy(previousPanel);
        }
        else
        {
            DestroyImmediate(previousPanel);
        }
    }

    private static bool UsesSharedPanelTemplate(GameObject panel)
    {
        return panel != null && panel.transform.Find("Grid") != null && panel.transform.childCount >= 5;
    }

    private void BindEntriesToSharedButtons(IReadOnlyDictionary<string, GameObject> sourcesByKey)
    {
        Transform grid = FindButtonGrid(backpackPanel != null ? backpackPanel.transform : null);
        if (grid == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            GameObject source;
            if (sourcesByKey != null && sourcesByKey.TryGetValue(entry.key, out source))
            {
                entry.source = source;
            }

            entry.button = i < grid.childCount ? grid.GetChild(i).gameObject : null;
            if (entry.button != null)
            {
                entry.button.name = $"08背包_{entry.key}";
                entry.button.SetActive(true);
                SetEntryLabel(entry.button, entry.label);
            }
        }

        for (int i = entries.Count; i < grid.childCount; i++)
        {
            grid.GetChild(i).gameObject.SetActive(false);
        }
    }

    private void ConfigurePanelLayout()
    {
        if (backpackPanel == null)
        {
            return;
        }

        RectTransform panelRect = backpackPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            // The cloned panel inherits a ContentSizeFitter whose min-size
            // calculation sees the stretched grid as zero and collapses the
            // whole panel. Keep the framework Canvas, but give this chapter a
            // stable 5-column button surface, matching the other backpack
            // chapters (the 9 entries occupy two rows).
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(224f, 100f);
        }

        ContentSizeFitter fitter = backpackPanel.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.enabled = false;
        }

        VerticalLayoutGroup vertical = backpackPanel.GetComponent<VerticalLayoutGroup>();
        if (vertical != null)
        {
            vertical.enabled = false;
        }

        Transform gridTransform = FindButtonGrid(backpackPanel.transform);
        if (gridTransform == null)
        {
            return;
        }

        RectTransform gridRect = gridTransform.GetComponent<RectTransform>();
        if (gridRect != null)
        {
            // Match the existing 06/07 backpack Grid child exactly.
            gridRect.anchorMin = new Vector2(0f, 1f);
            gridRect.anchorMax = new Vector2(0f, 1f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.sizeDelta = new Vector2(220f, 88f);
            gridRect.anchoredPosition = new Vector2(112f, -50f);
        }

        GridLayoutGroup grid = gridTransform.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.enabled = true;
            grid.cellSize = new Vector2(44f, 44f);
            grid.spacing = Vector2.zero;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.childAlignment = TextAnchor.UpperLeft;
        }
    }

    private void RebuildPanelLayout()
    {
        if (backpackPanel == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        RectTransform panelRect = backpackPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        }

        foreach (RectTransformColliderFitter fitter in
                 backpackPanel.GetComponentsInChildren<RectTransformColliderFitter>(true))
        {
            if (fitter == null)
            {
                continue;
            }

            fitter.enabled = false;
            fitter.enabled = true;
        }
        Canvas.ForceUpdateCanvases();
    }

    private static void SetEntryLabel(GameObject button, string value)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text visibleText = texts.FirstOrDefault(text =>
            text != null && text.gameObject.activeSelf &&
            (text.name == "Text" || text.name == "Label"));
        if (visibleText == null)
        {
            visibleText = texts.FirstOrDefault(text => text != null && text.gameObject.activeSelf);
        }
        if (visibleText != null)
        {
            visibleText.text = value ?? string.Empty;
        }
    }

    private void RefreshEntryLabels()
    {
        foreach (Entry entry in entries)
        {
            if (entry != null)
            {
                SetEntryLabel(entry.button, entry.label);
            }
        }
    }

    private void BuildRuntimeEntries(Transform sourcePanel, Transform panel)
    {
        if (panel == null)
        {
            return;
        }
        Transform grid = FindButtonGrid(panel);
        if (grid == null)
        {
            return;
        }

        List<Transform> buttons = new List<Transform>();
        for (int i = 0; i < grid.childCount; i++)
        {
            buttons.Add(grid.GetChild(i));
        }
        Transform template = buttons.FirstOrDefault(value => value.GetComponentInChildren<PressableButton>(true) != null);
        if (template == null && sourcePanel != null)
        {
            template = sourcePanel.GetComponentsInChildren<PressableButton>(true)
                .Select(value => value.transform).FirstOrDefault();
        }
        string[] keys = { "二维码", "充电桩", "手电钻", "膨胀管", "羊角锤", "膨胀螺丝", "扳手", "WiFi天线", "电源插头" };
        for (int i = buttons.Count; i < keys.Length && template != null; i++)
        {
            Transform clone = Instantiate(template, grid);
            clone.name = $"08背包_{keys[i]}";
            buttons.Add(clone);
        }
        if (entries.Count > 0)
        {
            return;
        }

        string[] labels = { "二维码", "充电桩", "手电钻", "膨胀管", "羊角锤", "膨胀螺丝", "扳手", "WiFi天线", "电源插头" };
        string[] sourceNames = { "ErWeiMa", "ChongDianZhuang_fuben", "ShouDianZuan-CDZ", "PengZhangLuoSi (1)", "YangJiaoChui", "luosi（1）", "banshou_2", "tianxian1", "Chazuo" };
        bool[] keepSourceVisible = { false, true, false, true, false, false, false, false, false };
        for (int i = 0; i < keys.Length; i++)
        {
            GameObject source = FindSceneObject(sourceNames[i], "changjing-2");
            entries.Add(new Entry
            {
                key = keys[i],
                label = labels[i],
                source = source,
                button = i < buttons.Count ? buttons[i].gameObject : null,
                keepSourceVisible = keepSourceVisible[i]
            });
        }

        BindEntriesToSharedButtons(null);
    }

    private static Transform FindButtonGrid(Transform panel)
    {
        if (panel == null)
        {
            return null;
        }

        Transform sharedGrid = panel.Find("Grid");
        return sharedGrid != null ? sharedGrid : panel.Find("08背包按钮网格");
    }

    private void UnbindButtons()
    {
        foreach (PressableButton button in pressableButtons)
        {
            if (button != null)
            {
                button.OnClicked.RemoveAllListeners();
            }
        }
        foreach (Button button in uiButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }
        pressableButtons.Clear();
        uiButtons.Clear();
    }

    private void ApplyButtonState()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            bool interactable = backpackPanel != null && backpackPanel.activeSelf &&
                                !string.IsNullOrEmpty(activeKey) && entry != null &&
                                entry.key == activeKey;
            if (entry?.button == null)
            {
                continue;
            }
            PressableButton pressable = entry.button.GetComponentInChildren<PressableButton>(true);
            if (pressable != null)
            {
                pressable.enabled = interactable;
            }
            Button button = entry.button.GetComponentInChildren<Button>(true);
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
        UpdateActiveEntryGuidance();
    }

    private void UpdateActiveEntryGuidance()
    {
        Entry entry = entries.FirstOrDefault(value => value != null && value.key == activeKey);
        if (!showActiveEntryGuidance || backpackPanel == null || entry == null || entry.button == null)
        {
            SetEntryGuidanceVisible(false);
            return;
        }

        // 背包UI 与 Buttons Canvas、Single Menu Canvas 共用同一位置，切换时只关
        // Canvas 而保留 activeInHierarchy，因此这里必须判断目标是否真的可见：
        // 面板可见就高亮当前物品按钮，面板不可见就退回到可见的章节切换按钮，
        // 两者都不可见时整块指引隐藏，绝不能画到别的 Canvas 上。
        GameObject guidanceTarget = backpackPanel.activeInHierarchy
            ? entry.button : FindChargingStationChapterTab();
        if (guidanceTarget == null || !TexCore_FocusHighlight.IsUiVisible(guidanceTarget))
        {
            SetEntryGuidanceVisible(false);
            return;
        }

        Bounds bounds;
        if (!TryGetGuidanceBounds(guidanceTarget, out bounds))
        {
            SetEntryGuidanceVisible(false);
            return;
        }

        EnsureEntryGuidanceCreated();
        if (entryGuidanceRoot == null)
        {
            return;
        }

        entryGuidanceRoot.SetActive(true);
        Camera activeCamera = Camera.main;
        Vector3 center = bounds.center;
        Vector3 right = activeCamera != null ? activeCamera.transform.right : Vector3.right;
        Vector3 up = activeCamera != null ? activeCamera.transform.up : Vector3.up;
        Vector3 normal = activeCamera != null
            ? (activeCamera.transform.position - center).normalized : Vector3.forward;
        float size = Mathf.Max(0.02f, bounds.extents.magnitude);
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * activeEntryPulseSpeed) * activeEntryPulseAmount;
        float halfWidth = size * 0.85f * pulse;
        float halfHeight = size * 0.6f * pulse;
        Vector3 offset = normal * Mathf.Max(0.002f, size * 0.04f);
        Vector3[] corners =
        {
            center - right * halfWidth - up * halfHeight + offset,
            center - right * halfWidth + up * halfHeight + offset,
            center + right * halfWidth + up * halfHeight + offset,
            center + right * halfWidth - up * halfHeight + offset
        };
        float lineWidth = Mathf.Max(0.001f, size * 0.07f);
        entryGuidanceOutline.startWidth = lineWidth;
        entryGuidanceOutline.endWidth = lineWidth;
        entryGuidanceOutline.startColor = activeEntryGuidanceColor;
        entryGuidanceOutline.endColor = activeEntryGuidanceColor;
        for (int i = 0; i < corners.Length; i++)
        {
            entryGuidanceOutline.SetPosition(i, corners[i]);
        }

        Vector3 tip = center + up * (halfHeight + size * 0.1f) + offset;
        float arrowLength = Mathf.Max(size * 0.9f, 0.04f);
        float arrowWidth = arrowLength * 0.35f;
        Vector3 tail = tip + up * arrowLength;
        entryGuidanceArrow.startWidth = lineWidth;
        entryGuidanceArrow.endWidth = lineWidth;
        entryGuidanceArrow.startColor = activeEntryGuidanceColor;
        entryGuidanceArrow.endColor = activeEntryGuidanceColor;
        entryGuidanceArrow.SetPosition(0, tail);
        entryGuidanceArrow.SetPosition(1, tip);
        Vector3 headBase = tip + up * (arrowLength * 0.35f);
        entryGuidanceArrowHead.startWidth = lineWidth;
        entryGuidanceArrowHead.endWidth = lineWidth;
        entryGuidanceArrowHead.startColor = activeEntryGuidanceColor;
        entryGuidanceArrowHead.endColor = activeEntryGuidanceColor;
        entryGuidanceArrowHead.SetPosition(0, headBase + right * arrowWidth);
        entryGuidanceArrowHead.SetPosition(1, tip);
        entryGuidanceArrowHead.SetPosition(2, headBase - right * arrowWidth);
    }

    private GameObject FindChargingStationChapterTab()
    {
        Transform backpackRoot = backpackPanel != null ? backpackPanel.transform.parent : null;
        Transform chapterBar = backpackRoot != null ? backpackRoot.Find("Button Bar_Horizontal") : null;
        if (chapterBar == null)
        {
            return null;
        }

        Transform configured = chapterBar.Find("08充电桩安装切换按钮");
        if (configured != null)
        {
            return configured.gameObject;
        }

        PressableButton[] buttons = chapterBar.GetComponentsInChildren<PressableButton>(true);
        return buttons.Length > 3 ? buttons[3].gameObject : null;
    }

    private void SetEntryGuidanceVisible(bool visible)
    {
        if (entryGuidanceRoot != null)
        {
            entryGuidanceRoot.SetActive(visible);
        }
    }

    private void EnsureEntryGuidanceCreated()
    {
        if (entryGuidanceRoot != null)
        {
            return;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            showActiveEntryGuidance = false;
            return;
        }

        entryGuidanceRoot = new GameObject("08BackpackActiveEntryGuidance")
        {
            hideFlags = HideFlags.DontSave,
            layer = gameObject.layer
        };
        entryGuidanceMaterial = new Material(shader)
        {
            name = "08BackpackActiveEntryGuidanceMaterial",
            hideFlags = HideFlags.DontSave
        };
        entryGuidanceOutline = CreateEntryGuideLine("Outline", true, 4, 54);
        entryGuidanceArrow = CreateEntryGuideLine("Arrow", false, 2, 52);
        entryGuidanceArrowHead = CreateEntryGuideLine("ArrowHead", false, 3, 53);
    }

    private LineRenderer CreateEntryGuideLine(string objectName, bool loop, int positionCount, int sortingOrder)
    {
        GameObject lineObject = new GameObject(objectName)
        {
            hideFlags = HideFlags.DontSave,
            layer = gameObject.layer
        };
        lineObject.transform.SetParent(entryGuidanceRoot.transform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = entryGuidanceMaterial;
        line.useWorldSpace = true;
        line.loop = loop;
        line.positionCount = positionCount;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.lightProbeUsage = LightProbeUsage.Off;
        line.reflectionProbeUsage = ReflectionProbeUsage.Off;
        line.sortingOrder = sortingOrder;
        return line;
    }

    private static bool TryGetGuidanceBounds(GameObject source, out Bounds bounds)
    {
        bool found = false;
        bounds = default;
        foreach (Collider collider in source.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null) continue;
            if (!found) { bounds = collider.bounds; found = true; }
            else bounds.Encapsulate(collider.bounds);
        }
        if (found) return true;

        foreach (Renderer renderer in source.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (found) return true;

        Vector3[] corners = new Vector3[4];
        foreach (RectTransform rectTransform in source.GetComponentsInChildren<RectTransform>(true))
        {
            if (rectTransform == null || rectTransform.rect.width <= 0.0001f ||
                rectTransform.rect.height <= 0.0001f)
            {
                continue;
            }

            rectTransform.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                if (!found)
                {
                    bounds = new Bounds(corners[i], Vector3.zero);
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(corners[i]);
                }
            }
        }
        return found;
    }

    private static TexCore_08BackpackController FindController()
    {
        if (instance != null)
        {
            return instance;
        }
        instance = FindObjectOfType<TexCore_08BackpackController>(true);
        return instance;
    }

    private static bool Is08Reporter(GameObject reporter)
    {
        return reporter.name.StartsWith("08_", StringComparison.Ordinal) ||
               reporter.transform.parent != null && reporter.transform.parent.name == "TexCore_08Steps";
    }

    private static void ActivateParentChain(Transform value)
    {
        Transform current = value;
        while (current != null)
        {
            current.gameObject.SetActive(true);
            current = current.parent;
        }
    }

    private static void ActivateSource(GameObject source)
    {
        if (source == null)
        {
            return;
        }

        // 必须连同父级一起激活，否则 changjing-2 等父节点被禁用时
        // 子物体单独 SetActive(true) 仍然不可见。
        ActivateParentChain(source.transform);
        source.SetActive(true);
        TexCore_BackpackPhysics.Stabilize(source);

        foreach (Collider value in source.GetComponentsInChildren<Collider>(true))
        {
            if (value != null)
            {
                value.enabled = true;
            }
        }
    }

    private static GameObject FindSceneObject(string objectName, string ancestorName)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject value in objects)
        {
            if (value == null || value.name != objectName || !value.scene.IsValid())
            {
                continue;
            }

            if (string.IsNullOrEmpty(ancestorName))
            {
                return value;
            }

            Transform current = value.transform.parent;
            while (current != null)
            {
                if (current.name == ancestorName || current.name == "08充电桩安装")
                {
                    return value;
                }
                current = current.parent;
            }
        }
        return null;
    }

    private static string GetKeyForSourceName(string sourceName)
    {
        if (string.IsNullOrEmpty(sourceName)) return string.Empty;
        if (sourceName.StartsWith("ErWeiMa", StringComparison.Ordinal)) return "二维码";
        if (sourceName.StartsWith("ChongDianZhuang", StringComparison.Ordinal)) return "充电桩";
        if (sourceName.StartsWith("ShouDianZuan-CDZ", StringComparison.Ordinal)) return "手电钻";
        if (sourceName.StartsWith("PengZhangLuoSi", StringComparison.Ordinal)) return "膨胀管";
        if (sourceName.StartsWith("YangJiaoChui", StringComparison.Ordinal)) return "羊角锤";
        if (sourceName.StartsWith("luosi", StringComparison.Ordinal)) return "膨胀螺丝";
        if (sourceName.StartsWith("banshou", StringComparison.Ordinal)) return "扳手";
        if (sourceName.StartsWith("tianxian", StringComparison.Ordinal)) return "WiFi天线";
        if (sourceName.StartsWith("Chazuo", StringComparison.Ordinal)) return "电源插头";
        return string.Empty;
    }

    private static void ShowHint(string text)
    {
        TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, text, 1.5f);
    }
}
