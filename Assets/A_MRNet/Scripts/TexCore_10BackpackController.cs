using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MixedReality.Toolkit.UX;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Bpf.Diagnostics;

/// <summary>
/// Runtime-only M10 backpack chapter. It reuses the existing shared backpack
/// visual and offers the single physical item required by rack entry: a rack.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_10BackpackController : MonoBehaviour
{
    private const string SceneName = "03SingleplayerScene";
    private const string PanelName = "10货架入场背包面板";
    private const string TemplatePanelName = "06货架安装背包面板";
    private const string RackLabel = "货架";
    private const string NotInStepHint = "请先进入“02机器人搬运至货架点”步骤，再取出货架。";
    private static readonly string[] SharedPanelNames =
    {
        "01通用物品背包面板",
        "02服务器系统安装背包面板",
        "05划线贴码背包面板",
        "06货架安装背包面板",
        "07播种墙安装背包面板",
        "08充电桩安装背包面板"
    };

    private GameObject backpackPanel;
    private GameObject rackButtonRoot;
    private PressableButton rackPressableButton;
    private Button rackUiButton;
    private TMP_Text statusLabel;
    private TexCore_RackEntryWorkflowStep workflow;
    private bool rackSummoned;
    private static TexCore_10BackpackController instance;
    private readonly List<GameObject> chapterFocusSecondary = new List<GameObject>();

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
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (rackPressableButton != null)
        {
            rackPressableButton.OnClicked.RemoveAllListeners();
        }
        if (rackUiButton != null)
        {
            rackUiButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// Opens the M10 panel and waits for the player to take the first rack.
    /// </summary>
    public static void Present(TexCore_RackEntryWorkflowStep value)
    {
        TexCore_10BackpackController controller = FindController();
        if (controller == null || value == null)
        {
            return;
        }

        if (controller.workflow != value)
        {
            controller.workflow = value;
            controller.rackSummoned = false;
        }

        controller.EnsurePanel();
        controller.Refresh();
        controller.ShowPanel();
        if (!controller.rackSummoned)
        {
            value.WithholdUntilSummoned();
        }

        controller.ShowRackEntryGuidance();
    }

    public static void HideFor(TexCore_RackEntryWorkflowStep value)
    {
        TexCore_10BackpackController controller = instance ?? FindController();
        if (controller == null || controller.workflow != value)
        {
            return;
        }

        controller.workflow = null;
        controller.rackSummoned = false;
        controller.Refresh();
        if (controller.backpackPanel != null)
        {
            controller.backpackPanel.SetActive(false);
        }

        TexCore_FocusHighlight.ClearFor(controller);
    }

    /// <summary>
    /// Highlights the M10 chapter entry and the rack item while the player still
    /// has to take the rack out of the backpack.
    /// </summary>
    public static void HighlightRackEntry()
    {
        TexCore_10BackpackController controller = instance ?? FindController();
        if (controller == null)
        {
            return;
        }

        controller.ShowRackEntryGuidance();
    }

    /// <summary>
    /// Keeps the M10 backpack guidance consistent when another chapter panel is
    /// selected. While the rack is still waiting, the M10 chapter entry becomes
    /// the single highlighted control so the player can come back to it.
    /// </summary>
    public static void OnChapterPanelChanged()
    {
        TexCore_10BackpackController controller = instance ?? FindController();
        if (controller == null || controller.workflow == null || controller.rackSummoned)
        {
            return;
        }

        TexCore_BackpackChapterTabs.FocusM10ChapterButton(controller);
    }

    /// <summary>
    /// Re-applies the rack item state and highlight after the M10 panel is shown
    /// again from the chapter bar. Opening the panel is not enough to take the
    /// rack out, so the item stays disabled until 02 actually runs.
    /// </summary>
    public static void OnM10PanelShown()
    {
        TexCore_10BackpackController controller = instance ?? FindController();
        if (controller == null)
        {
            return;
        }

        controller.EnsurePanel();
        controller.Refresh();
        controller.ShowRackEntryGuidance();
    }

    private static TexCore_10BackpackController FindController()
    {
        return FindController(SceneManager.GetActiveScene());
    }

    private static TexCore_10BackpackController FindController(Scene scene)
    {
        if (scene.name != SceneName && scene.name != TexCore_LoadScene.Test09SceneName)
        {
            return null;
        }

        if (instance != null && instance.gameObject.scene == scene)
        {
            return instance;
        }

        Transform backpack = Resources.FindObjectsOfTypeAll<Transform>()
            .FirstOrDefault(value => value != null &&
                                     value.gameObject.scene == scene &&
                                     value.name == "背包UI");
        if (backpack == null)
        {
            return null;
        }

        TexCore_10BackpackController controller = backpack.GetComponent<TexCore_10BackpackController>();
        instance = controller != null ? controller : backpack.gameObject.AddComponent<TexCore_10BackpackController>();
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
                    BpfLog.Warning(BpfLogCategory.Scene, "tex_core_10_backpack_controller.ensure_panel", "M10 backpack cannot find the shared 06 backpack panel template.", this);
                    return;
                }

                backpackPanel = Instantiate(source.gameObject, transform);
                backpackPanel.name = PanelName;
                backpackPanel.transform.SetSiblingIndex(source.GetSiblingIndex() + 1);
                backpackPanel.SetActive(false);
            }
        }

        if (rackButtonRoot == null)
        {
            BuildRackEntry();
        }
    }

    private void BuildRackEntry()
    {
        Transform grid = FindGrid(backpackPanel != null ? backpackPanel.transform : null);
        if (grid == null)
        {
            BpfLog.Warning(BpfLogCategory.Scene, "tex_core_10_backpack_controller.build_rack_entry", "M10 backpack panel has no button grid.", this);
            return;
        }

        List<Transform> slots = new List<Transform>();
        for (int i = 0; i < grid.childCount; i++)
        {
            Transform child = grid.GetChild(i);
            if (child.GetComponentInChildren<PressableButton>(true) != null ||
                child.GetComponentInChildren<Button>(true) != null)
            {
                slots.Add(child);
            }
        }

        if (slots.Count == 0)
        {
            BpfLog.Warning(BpfLogCategory.Scene, "tex_core_10_backpack_controller.build_rack_entry", "M10 backpack panel template has no reusable button.", this);
            return;
        }

        Transform slot = slots[0];
        rackButtonRoot = slot.gameObject;
        rackButtonRoot.name = "10背包_货架";
        rackButtonRoot.SetActive(true);
        SetLabel(rackButtonRoot, RackLabel);

        for (int i = 1; i < slots.Count; i++)
        {
            slots[i].gameObject.SetActive(false);
        }

        rackPressableButton = rackButtonRoot.GetComponentInChildren<PressableButton>(true);
        rackUiButton = rackButtonRoot.GetComponentInChildren<Button>(true);
        if (rackPressableButton != null)
        {
            // The M10 panel is cloned from the 06 rack-assembly panel, whose
            // button carries a serialized SpawnBottomBase listener. RemoveAll-
            // Listeners only clears runtime listeners, so the inherited one has
            // to be dropped explicitly or M10's 货架 button would call into M06.
            ClearInheritedListeners(rackPressableButton.OnClicked);
            rackPressableButton.OnClicked.AddListener(SummonRack);
        }
        if (rackUiButton != null)
        {
            ClearInheritedListeners(rackUiButton.onClick);
            rackUiButton.onClick.AddListener(SummonRack);
        }

        statusLabel = backpackPanel.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(value => value.GetComponentInParent<PressableButton>(true) == null &&
                                     value.GetComponentInParent<Button>(true) == null);
        ConfigurePanelLayout(grid);
    }

    /// <summary>
    /// Drops the serialized listeners a cloned template button carries, so the
    /// M10 item never re-enters another module's assembly flow. Reflection is
    /// required because Unity exposes no runtime API for persistent listeners.
    /// </summary>
    private static void ClearInheritedListeners(UnityEventBase unityEvent)
    {
        if (unityEvent == null)
        {
            return;
        }

        try
        {
            FieldInfo groupField = typeof(UnityEventBase).GetField("m_PersistentCalls",
                BindingFlags.Instance | BindingFlags.NonPublic);
            object group = groupField?.GetValue(unityEvent);
            MethodInfo clear = group?.GetType().GetMethod("Clear",
                BindingFlags.Instance | BindingFlags.Public);
            if (clear != null)
            {
                clear.Invoke(group, null);
            }
            else
            {
                BpfLog.Warning(BpfLogCategory.UI,
                    "tex_core_10_backpack_controller.clear_inherited_listeners",
                    "M10 backpack could not clear inherited button listeners.", null);
            }
        }
        catch (System.Exception exception)
        {
            BpfLog.Warning(BpfLogCategory.UI,
                "tex_core_10_backpack_controller.clear_inherited_listeners",
                exception.Message, null);
        }
    }

    private void SummonRack()
    {
        if (workflow == null)
        {
            // The chapter bar can open this panel, but it must never let the
            // player skip the 02 step, so fail loudly instead of silently.
            Refresh();
            TexCore_FocusHighlight.ClearFor(this);
            ShowHint(NotInStepHint);
            return;
        }

        if (rackSummoned)
        {
            // 重复点击时立刻退出“取出货架”的引导状态，避免继续显示错误高亮。
            TexCore_FocusHighlight.ClearFor(this);
            ShowHint("货架已经取出，请将它接驳到机器人。");
            return;
        }

        if (!workflow.ReleaseFromBackpack())
        {
            ShowHint("当前步骤不能取出货架。");
            return;
        }

        rackSummoned = true;
        Refresh();
        if (backpackPanel != null)
        {
            backpackPanel.SetActive(false);
        }

        // 切回默认背包并释放 M10 面板高亮；接驳阶段的高亮与提示由工作流统一发布。
        TexCore_BackpackChapterTabs.ShowDefault();
    }

    /// <summary>
    /// Focuses the rack item button, with the M10 chapter entry as a secondary
    /// marker. Nothing is highlighted before the panel exists or after the rack
    /// has already been taken out.
    /// </summary>
    private void ShowRackEntryGuidance()
    {
        if (workflow == null || rackSummoned || rackButtonRoot == null ||
            !rackButtonRoot.activeInHierarchy)
        {
            return;
        }

        chapterFocusSecondary.Clear();
        GameObject chapterButton = TexCore_BackpackChapterTabs.FindM10ChapterButton();
        if (chapterButton != null && TexCore_FocusHighlight.IsUiVisible(chapterButton))
        {
            chapterFocusSecondary.Add(chapterButton);
        }

        TexCore_FocusHighlight.Show(rackButtonRoot, TexCore_RackEntryWorkflowStep.GuideSelectRack,
            null, chapterFocusSecondary, this);
    }

    private void ShowPanel()
    {
        if (backpackPanel == null)
        {
            return;
        }

        gameObject.SetActive(true);
        foreach (string panelName in SharedPanelNames)
        {
            Transform panel = transform.Find(panelName);
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }
        }
        backpackPanel.SetActive(true);
    }

    private void Refresh()
    {
        bool inStep = workflow != null;
        bool available = inStep && !rackSummoned;
        if (rackPressableButton != null)
        {
            rackPressableButton.enabled = available;
        }
        if (rackUiButton != null)
        {
            rackUiButton.interactable = available;
        }
        if (statusLabel != null)
        {
            if (!inStep)
            {
                statusLabel.text = "10货架入场\n请先进入“02机器人搬运至货架点”步骤。";
            }
            else
            {
                statusLabel.text = available
                    ? "10货架入场\n点击货架，从背包取出待搬运货架。"
                    : "10货架入场\n货架已取出，请接驳机器人并搬运到目标点。";
            }
        }
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
        TMP_Text text = buttonRoot.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(value => value.name == "Label" || value.name == "Text");
        if (text == null)
        {
            text = buttonRoot.GetComponentInChildren<TMP_Text>(true);
        }
        if (text != null)
        {
            text.text = label;
        }
    }

    private static void ConfigurePanelLayout(Transform grid)
    {
        GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 1;
            layout.cellSize = new Vector2(92f, 44f);
            layout.spacing = Vector2.zero;
            layout.padding = new RectOffset(0, 0, 0, 0);
        }

        RectTransform panel = grid.parent != null ? grid.parent.GetComponent<RectTransform>() : null;
        if (panel != null)
        {
            panel.sizeDelta = new Vector2(224f, 120f);
        }

        Canvas.ForceUpdateCanvases();
    }

    private static void ShowHint(string hint)
    {
        TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, hint, 1.5f);
    }
}
