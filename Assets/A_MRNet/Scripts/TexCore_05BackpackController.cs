using System;
using System.Collections.Generic;
using System.Linq;
using MixedReality.Toolkit.UX;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Bpf.Diagnostics;

/// <summary>
/// Runtime-only M05 tool backpack.  It deliberately has its own controller so
/// the 05 tools do not leak into the 08 charging-station backpack.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_05BackpackController : MonoBehaviour
{
    private const string SceneName = "03SingleplayerScene";
    private const string PanelName = "05划线贴码背包面板";
    private const string TemplatePanelName = "01通用物品背包面板";

    /// <summary>
    /// Testing shortcut: skips the laser, ray and marking stages and drops the
    /// step straight into the ink-line phase.
    /// </summary>
    private const string JumpToDrawAction = "跳到弹线";

    private static readonly string[] Actions =
    {
        "激光水平仪",
        "开启射线",
        "记号笔",
        "墨斗",
        "二维码",
        JumpToDrawAction
    };

    private sealed class Entry
    {
        public string action;
        public GameObject root;
        public PressableButton button;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private GameObject backpackPanel;
    private TMP_Text statusLabel;
    private TexCore_M05DrawStep workflow;
    private TexCore_M05QrPasteStep qrWorkflow;
    private TexCore_M05DrawStep drawSource;
    private TexCore_M05QrPasteStep qrSource;
    private string activeAction;
    private bool statusLogged;
    private static TexCore_05BackpackController instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadHandler()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneName || scene.name == TexCore_LoadScene.Test09SceneName)
        {
            FindController(scene);
        }
    }

    private void Awake()
    {
        instance = this;
        EnsurePanel();
        Refresh();
    }

    private void OnDestroy()
    {
        TexCore_FocusHighlight.ClearFor(this);
        if (instance == this)
        {
            instance = null;
        }

        UnbindButtons();
    }

    /// <summary>
    /// Called by the physical M05 step whenever the required tool changes.
    /// </summary>
    public static void Present(TexCore_M05DrawStep value)
    {
        if (value == null)
        {
            return;
        }

        TexCore_05BackpackController controller = FindController(value.gameObject.scene);
        if (controller == null)
        {
            return;
        }

        controller.workflow = value;
        controller.qrWorkflow = null;
        controller.activeAction = value.RequiredBackpackAction;
        controller.ShowPanel();
        controller.Refresh();
    }

    /// <summary>
    /// Shows the final M05 backpack action.  The QR objects remain hidden and
    /// non-interactable until this action has been used.
    /// </summary>
    public static void Present(TexCore_M05QrPasteStep value)
    {
        if (value == null)
        {
            return;
        }

        TexCore_05BackpackController controller = FindController(value.gameObject.scene);
        if (controller == null)
        {
            return;
        }

        controller.workflow = null;
        controller.qrWorkflow = value;
        controller.activeAction = value.RequiredBackpackAction;
        controller.ShowPanel();
        controller.Refresh();
    }

    public static void RefreshFor(TexCore_M05DrawStep value)
    {
        if (instance != null && instance.workflow == value)
        {
            instance.activeAction = value.RequiredBackpackAction;
            instance.Refresh();
        }
    }

    public static void RefreshFor(TexCore_M05QrPasteStep value)
    {
        if (instance != null && instance.qrWorkflow == value)
        {
            instance.activeAction = value.RequiredBackpackAction;
            instance.Refresh();
        }
    }

    public static void HideFor(TexCore_M05DrawStep value)
    {
        if (instance == null || instance.workflow != value)
        {
            return;
        }

        instance.workflow = null;
        instance.activeAction = string.Empty;
        if (instance.backpackPanel != null)
        {
            instance.backpackPanel.SetActive(false);
        }
    }

    public static void HideFor(TexCore_M05QrPasteStep value)
    {
        if (instance == null || instance.qrWorkflow != value)
        {
            return;
        }

        instance.qrWorkflow = null;
        instance.activeAction = string.Empty;
        if (instance.backpackPanel != null)
        {
            instance.backpackPanel.SetActive(false);
        }
    }

    private static TexCore_05BackpackController FindController(Scene scene)
    {
        if (instance != null && instance.gameObject.scene == scene)
        {
            return instance;
        }

        Transform backpackRoot = Resources.FindObjectsOfTypeAll<Transform>()
            .FirstOrDefault(value => value != null &&
                                     value.gameObject.scene == scene &&
                                     value.name == "背包UI");
        if (backpackRoot == null)
        {
            return null;
        }

        GameObject backpack = backpackRoot.gameObject;
        TexCore_05BackpackController controller = backpack.GetComponent<TexCore_05BackpackController>();
        instance = controller != null ? controller : backpack.AddComponent<TexCore_05BackpackController>();
        return instance;
    }

    private void EnsurePanel()
    {
        if (backpackPanel == null)
        {
            Transform configured = transform.Find(PanelName);
            if (configured != null)
            {
                backpackPanel = configured.gameObject;
            }
            else
            {
                Transform source = transform.Find(TemplatePanelName);
                if (source == null)
                {
                    BpfLog.Warning(BpfLogCategory.Scene, "tex_core_05_backpack_controller.ensure_panel", "M05 backpack cannot find the shared backpack panel template.", this);
                    return;
                }

                backpackPanel = Instantiate(source.gameObject, transform);
                backpackPanel.name = PanelName;
                backpackPanel.transform.SetSiblingIndex(source.GetSiblingIndex() + 1);
                backpackPanel.SetActive(false);
            }
        }

        if (entries.Count == 0)
        {
            BuildEntries();
        }
    }

    private void BuildEntries()
    {
        Transform grid = FindGrid(backpackPanel != null ? backpackPanel.transform : null);
        if (grid == null)
        {
            BpfLog.Warning(BpfLogCategory.Scene, "tex_core_05_backpack_controller.build_entries", "M05 backpack panel has no button grid.", this);
            return;
        }

        List<Transform> slots = new List<Transform>();
        for (int i = 0; i < grid.childCount; i++)
        {
            Transform child = grid.GetChild(i);
            if (child.GetComponentInChildren<PressableButton>(true) != null)
            {
                slots.Add(child);
            }
        }

        if (slots.Count == 0)
        {
            return;
        }

        Transform template = slots[0];
        while (slots.Count < Actions.Length)
        {
            slots.Add(Instantiate(template, grid));
        }

        UnbindButtons();
        for (int i = 0; i < Actions.Length; i++)
        {
            Transform slot = slots[i];
            string action = Actions[i];
            slot.name = $"05背包_{action}";
            SetLabel(slot.gameObject, action);
            PressableButton button = slot.GetComponentInChildren<PressableButton>(true);
            if (button != null)
            {
                button.OnClicked.RemoveAllListeners();
                string captured = action;
                button.OnClicked.AddListener(() => Use(captured));
            }
            entries.Add(new Entry { action = action, root = slot.gameObject, button = button });
        }

        for (int i = Actions.Length; i < slots.Count; i++)
        {
            slots[i].gameObject.SetActive(false);
        }

        statusLabel = backpackPanel.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(value => value.GetComponentInParent<PressableButton>(true) == null &&
                                     value.GetComponentInParent<Button>(true) == null);
        ConfigurePanelLayout(grid);
    }

    private void Use(string action)
    {
        if (string.Equals(action, JumpToDrawAction, StringComparison.Ordinal))
        {
            workflow?.JumpToInkDrawing();
            RefreshFor(workflow);
            return;
        }

        if (workflow != null)
        {
            workflow.UseBackpackAction(action);
            RefreshFor(workflow);
            return;
        }

        if (qrWorkflow == null)
        {
            return;
        }

        qrWorkflow.UseBackpackAction(action);
        RefreshFor(qrWorkflow);
    }

    private void ShowPanel()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        EnsurePanel();
        if (backpackPanel == null)
        {
            return;
        }

        TexCore_BackpackChapterTabs.ShowM05Panel(backpackPanel);
    }

    /// <summary>
    /// Gives the panel a stable view of both M05 workflows so every counter can
    /// be rendered even while only one of them owns the active phase.  Dispatch
    /// still goes through the single active workflow.
    /// </summary>
    public static void RegisterWorkflows(Scene scene, TexCore_M05DrawStep draw, TexCore_M05QrPasteStep qr)
    {
        TexCore_05BackpackController controller = FindController(scene);
        if (controller == null)
        {
            return;
        }

        controller.drawSource = draw;
        controller.qrSource = qr;
        controller.Refresh();
    }

    private void Refresh()
    {
        if (backpackPanel == null)
        {
            return;
        }

        ITexCoreM05StepStatus status = workflow != null ? workflow : (ITexCoreM05StepStatus)qrWorkflow;
        string phaseName = status != null ? status.PhaseName : "未开始";
        string hint = status != null ? status.PhaseHint : string.Empty;
        if (statusLabel != null)
        {
            statusLabel.text =
                "05 划线贴码\n" +
                $"当前步骤：{phaseName}\n" +
                $"基准点：{Count(drawSource?.MarkedDatumCount)}/{Count(drawSource?.RequiredDatumMarkCount)}\n" +
                $"划线：{Count(drawSource?.CompletedSegmentCount)}/{Count(drawSource?.RequiredSegmentCount)}\n" +
                $"二维码：{Count(qrSource?.PlacedCount)}/{Count(qrSource?.PairCount)}\n" +
                hint;
        }

        LogStatusOnce();
        UpdateButtonFocus();

        foreach (Entry entry in entries)
        {
            bool enabled = string.Equals(entry.action, JumpToDrawAction, StringComparison.Ordinal)
                ? workflow != null
                : (workflow != null || qrWorkflow != null) &&
                  string.Equals(entry.action, activeAction, StringComparison.Ordinal);
            if (entry.root != null)
            {
                entry.root.SetActive(true);
            }
            if (entry.button != null)
            {
                entry.button.enabled = enabled;
            }
        }
    }

    private static int Count(int? value) => value ?? 0;

    /// <summary>
    /// Points the shared focus at the backpack button the player has to press
    /// next, using the same TexCore_FocusHighlight the other guided topics use.
    /// Only the summon phases take the focus: while a world target owns it the
    /// panel clears its own claim instead of competing for the single primary.
    /// </summary>
    private void UpdateButtonFocus()
    {
        ITexCoreM05StepStatus status = workflow != null ? workflow : (ITexCoreM05StepStatus)qrWorkflow;
        if (status == null || !status.FocusesBackpackButton)
        {
            TexCore_FocusHighlight.ClearFor(this);
            return;
        }

        Entry active = entries.FirstOrDefault(value =>
            value != null && string.Equals(value.action, activeAction, StringComparison.Ordinal));
        if (active?.root == null)
        {
            TexCore_FocusHighlight.ClearFor(this);
            return;
        }

        TexCore_FocusHighlight.Show(active.root, $"请点击背包中的“{activeAction}”。", null, null, this);
    }

    /// <summary>
    /// One-shot report of what the panel actually resolved, so a missing or
    /// undersized status label can be told apart from a panel that never
    /// refreshed.
    /// </summary>
    private void LogStatusOnce()
    {
        if (statusLogged)
        {
            return;
        }

        statusLogged = true;
        RectTransform panelRect = backpackPanel.transform as RectTransform;
        BpfLog.Info(BpfLogCategory.UI, "tex_core_05_backpack.status_panel",
            $"panel={backpackPanel.name} panelSize={(panelRect != null ? panelRect.rect.size.ToString() : "n/a")} slots={entries.Count} statusLabel={(statusLabel != null ? statusLabel.name : "null")} labelSize={(statusLabel != null ? statusLabel.rectTransform.rect.size.ToString() : "-")} active={(workflow != null ? "draw" : qrWorkflow != null ? "qr" : "none")}",
            this);
    }

    private void UnbindButtons()
    {
        foreach (Entry entry in entries)
        {
            if (entry?.button != null)
            {
                entry.button.OnClicked.RemoveAllListeners();
            }
        }
        entries.Clear();
    }

    private static Transform FindGrid(Transform panel)
    {
        if (panel == null)
        {
            return null;
        }

        Transform named = panel.Find("Grid");
        if (named != null)
        {
            return named;
        }

        return panel.GetComponentsInChildren<GridLayoutGroup>(true)
            .Select(value => value.transform)
            .FirstOrDefault();
    }

    private static void SetLabel(GameObject buttonRoot, string label)
    {
        TMP_Text[] texts = buttonRoot.GetComponentsInChildren<TMP_Text>(true);
        bool wroteActionText = false;
        foreach (TMP_Text text in texts)
        {
            if (text == null ||
                (text.name != "Text" &&
                 (text.name != "Label" || text.text.StartsWith("Say ", StringComparison.Ordinal))))
            {
                continue;
            }

            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.enableAutoSizing = true;
            text.fontSizeMin = 6f;
            text.fontSizeMax = 16f;
            ApplyLegibleFont(text, label);
            wroteActionText = true;
        }

        if (!wroteActionText)
        {
            TMP_Text text = buttonRoot.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = label;
                text.alignment = TextAlignmentOptions.Center;
                text.enableWordWrapping = true;
                text.enableAutoSizing = true;
                text.fontSizeMin = 6f;
                text.fontSizeMax = 16f;
                ApplyLegibleFont(text, label);
            }
        }
    }

    private static TMP_FontAsset sceneFont;

    /// <summary>
    /// The backpack template ships a font whose atlas misses characters such as
    /// 墨 and 笔, and TMP then draws those glyphs as blanks. Probe the label
    /// against the fonts the loaded scene already provides and swap in one that
    /// can draw all of it, the same way TexCore_TipsManager handles its title.
    /// </summary>
    private static void ApplyLegibleFont(TMP_Text text, string required)
    {
        if (text == null || string.IsNullOrEmpty(required) || CanDraw(text.font, required))
        {
            return;
        }

        TMP_FontAsset replacement = ResolveLegibleFont(required);
        if (replacement == null)
        {
            BpfLog.Warning(BpfLogCategory.UI, "tex_core_05_backpack.label_font_missing",
                $"No scene font can draw “{required}”, so those glyphs stay blank.", text);
            return;
        }

        text.font = replacement;
        text.fontSharedMaterial = replacement.material;
    }

    /// <summary>
    /// Asks the font itself whether it can produce every character, which also
    /// lets a dynamic font add a missing glyph to its atlas. Fallback tables are
    /// ignored on purpose: the label needs a font whose own atlas covers it.
    /// </summary>
    private static bool CanDraw(TMP_FontAsset font, string required)
    {
        if (font == null)
        {
            return false;
        }

        for (int index = 0; index < required.Length; index++)
        {
            if (!font.HasCharacter(required[index], false, true))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The replacement comes from a text object of the loaded scene, so it is a
    /// font this scene already ships with instead of a new dependency.
    /// </summary>
    private static TMP_FontAsset ResolveLegibleFont(string required)
    {
        if (CanDraw(sceneFont, required))
        {
            return sceneFont;
        }

        foreach (TMP_Text candidate in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (candidate == null || candidate.font == null || !candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            if (CanDraw(candidate.font, required))
            {
                sceneFont = candidate.font;
                return sceneFont;
            }
        }

        return null;
    }

    private void ConfigurePanelLayout(Transform grid)
    {
        GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.cellSize = new Vector2(70f, 44f);
            layout.spacing = Vector2.zero;
            layout.padding = new RectOffset(5, 5, 0, 0);
        }

        RectTransform panel = grid.parent != null ? grid.parent.GetComponent<RectTransform>() : null;
        if (panel != null)
        {
            // Taller than the original single-hint panel so the six status
            // lines fit above the button grid.
            panel.sizeDelta = new Vector2(224f, 236f);
        }

        ConfigureStatusLabel();
        Canvas.ForceUpdateCanvases();
    }

    private void ConfigureStatusLabel()
    {
        if (statusLabel == null)
        {
            return;
        }

        // Six short lines have to stay readable inside the fixed panel width.
        statusLabel.alignment = TextAlignmentOptions.TopLeft;
        statusLabel.enableWordWrapping = false;
        statusLabel.enableAutoSizing = true;
        statusLabel.fontSizeMin = 5f;
        statusLabel.fontSizeMax = 12f;
        ApplyLegibleFont(statusLabel, statusLabelProbe);
    }

    /// <summary>Characters the status block uses, probed against the font.</summary>
    private const string statusLabelProbe = "05 划线贴码当前步骤基准点二维码已贴附激活请操作下一个";
}
