using System;
using System.Collections.Generic;
using System.Linq;
using MixedReality.Toolkit.UX;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps the shared backpack chapter bar as the single owner of which
/// backpack panel is visible in the supported business scenes.
/// </summary>
public static class TexCore_BackpackChapterTabs
{
    private static bool IsSupportedScene(string sceneName)
    {
        return sceneName == TexCore_LoadScene.BusinessSceneName ||
               sceneName == TexCore_LoadScene.Test0809SceneName ||
               sceneName == TexCore_LoadScene.Test0607SceneName ||
               sceneName == TexCore_LoadScene.Test09SceneName;
    }
    private const string BackpackRootName = "背包UI";
    private const string ChapterBarName = "Button Bar_Horizontal";
    // 01通用物品章节已从 03 场景下线。尚未迁移的场景仍然保留这个面板，
    // 继续在这里参与显隐，避免它被其它章节顶掉后仍留在屏幕上。
    private const string GenericPanelName = "01通用物品背包面板";
    private const string ServerInstallPanelName = "02服务器系统安装背包面板";
    private const string RackPanelName = "06货架安装背包面板";
    private const string SortingWallPanelName = "07播种墙安装背包面板";
    private const string ChargingStationPanelName = "08充电桩安装背包面板";
    private const string M05PanelName = "05划线贴码背包面板";
    private const string M10PanelName = "10货架入场背包面板";
    private const string M10SwitchButtonName = "10货架入场切换按钮";
    private static readonly HashSet<int> BoundButtonIds = new HashSet<int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureAfterSceneLoad()
    {
        EnsureInitialized();
    }

    public static void EnsureInitialized()
    {
        if (!IsSupportedScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        GameObject backpack = GameObject.Find(BackpackRootName);
        if (backpack == null)
        {
            return;
        }

        Transform chapterBar = backpack.transform.Find(ChapterBarName);
        if (chapterBar == null)
        {
            return;
        }

        EnsureM10ChapterButton(chapterBar);

        foreach (PressableButton button in chapterBar.GetComponentsInChildren<PressableButton>(true))
        {
            switch (button.name)
            {
                case "01通用物品切换按钮":
                    ConfigureButton(button, "01通用物品", GenericPanelName);
                    break;
                case "02服务器系统安装切换按钮":
                    ConfigureButton(button, "02服务器安装", ServerInstallPanelName);
                    break;
                case "05划线贴码切换按钮":
                    ConfigureButton(button, "05划线贴码", M05PanelName);
                    break;
                case "06货架安装切换按钮":
                    ConfigureButton(button, "06货架安装", RackPanelName);
                    break;
                case "07播种墙安装切换按钮":
                    ConfigureButton(button, "07播种墙安装", SortingWallPanelName);
                    break;
                case "08充电桩安装切换按钮":
                    ConfigureButton(button, "08充电桩安装", ChargingStationPanelName);
                    break;
                case M10SwitchButtonName:
                    ConfigureButton(button, "10货架入场", M10PanelName);
                    break;
            }
        }
    }

    /// <summary>
    /// 03 场景已经删除 01通用物品章节，背包的默认章节改为 02服务器系统安装。
    /// </summary>
    public static void ShowDefault()
    {
        ShowPanel(ServerInstallPanelName);
    }

    public static void ShowPanel(GameObject panel)
    {
        if (panel != null)
        {
            ShowPanel(panel.name);
        }
    }

    /// <summary>
    /// M05 has a permanent chapter-bar panel. Keep it mutually exclusive with
    /// the other shared panels when the workflow presents an allowed tool.
    /// </summary>
    public static void ShowM05Panel(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        Transform backpack = FindBackpackRoot(panel.transform);
        if (backpack == null)
        {
            return;
        }

        ShowPanel(backpack, M05PanelName);
        panel.SetActive(true);
    }

    /// <summary>
    /// M02 shares the chapter bar, and it also opens its own panel
    /// automatically while the U-disk step is active. The U-disk button keeps
    /// its own step gate.
    /// </summary>
    public static void ShowM02Panel(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        Transform backpack = FindBackpackRoot(panel.transform);
        if (backpack == null)
        {
            return;
        }

        ShowM02PanelFromBackpack(backpack, panel);
    }

    private static void ShowM02PanelFromBackpack(Transform backpack, GameObject panel)
    {
        if (backpack == null || panel == null)
        {
            return;
        }

        backpack.gameObject.SetActive(true);
        ShowPanel(backpack, ServerInstallPanelName);
        panel.SetActive(true);
    }

    public static void ShowPanel(string panelName)
    {
        if (!IsSupportedScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        GameObject backpack = GameObject.Find(BackpackRootName);
        if (backpack == null)
        {
            return;
        }

        ShowPanel(backpack.transform, panelName);
    }

    private static void ShowPanel(Transform backpack, string panelName)
    {
        if (backpack == null)
        {
            return;
        }

        SetPanelActive(backpack, GenericPanelName, panelName);
        SetPanelActive(backpack, ServerInstallPanelName, panelName);
        SetPanelActive(backpack, RackPanelName, panelName);
        SetPanelActive(backpack, SortingWallPanelName, panelName);
        SetPanelActive(backpack, ChargingStationPanelName, panelName);
        SetPanelActive(backpack, M05PanelName, panelName);
        SetPanelActive(backpack, M10PanelName, panelName);
        NotifyChapterPanelChanged(panelName);
    }

    /// <summary>
    /// Keeps the M10 rack-entry guidance in step with the visible chapter panel:
    /// the M10 item button is highlighted while M10 is open, and every other
    /// chapter clears that highlight.
    /// </summary>
    private static void NotifyChapterPanelChanged(string panelName)
    {
        if (string.Equals(panelName, M10PanelName, StringComparison.Ordinal))
        {
            TexCore_10BackpackController.OnM10PanelShown();
            return;
        }

        TexCore_10BackpackController.OnChapterPanelChanged();
    }

    /// <summary>
    /// Returns the runtime-created M10 chapter entry so guidance can highlight
    /// the real control instead of guessing a path.
    /// </summary>
    public static GameObject FindM10ChapterButton()
    {
        GameObject backpack = GameObject.Find(BackpackRootName);
        if (backpack == null)
        {
            return null;
        }

        Transform chapterBar = backpack.transform.Find(ChapterBarName);
        if (chapterBar == null)
        {
            return null;
        }

        PressableButton button = chapterBar.GetComponentsInChildren<PressableButton>(true)
            .FirstOrDefault(value => value.name == M10SwitchButtonName);
        return button != null ? button.gameObject : null;
    }

    /// <summary>
    /// Single highlight used while the player has switched away from the M10
    /// backpack but still has to take the rack out of it.
    /// </summary>
    public static void FocusM10ChapterButton(UnityEngine.Object owner)
    {
        GameObject button = FindM10ChapterButton();
        if (button == null || !button.activeInHierarchy)
        {
            return;
        }

        TexCore_FocusHighlight.ClearFor(owner);

        // 章节栏住在背包UI 的 Canvas 里：背包被 Buttons Canvas 或 Single Menu
        // Canvas 顶掉时按钮还在 activeInHierarchy，但玩家看不见，不能高亮。
        if (!TexCore_FocusHighlight.IsUiVisible(button))
        {
            return;
        }

        TexCore_FocusHighlight.Show(button, "请点击“10货架入场”打开背包并取出货架。",
            null, null, owner);
    }

    /// <summary>
    /// M10 owns a runtime-created panel because its only item is tied to the
    /// active rack-entry workflow. Clone the established M06 tab so that M10
    /// participates in the same chapter-bar navigation without serializing a
    /// duplicate into the scene.
    /// </summary>
    private static PressableButton EnsureM10ChapterButton(Transform chapterBar)
    {
        if (chapterBar == null)
        {
            return null;
        }

        PressableButton existing = chapterBar.GetComponentsInChildren<PressableButton>(true)
            .FirstOrDefault(button => button.name == M10SwitchButtonName);
        if (existing != null)
        {
            return existing;
        }

        PressableButton template = chapterBar.GetComponentsInChildren<PressableButton>(true)
            .FirstOrDefault(button => button.name == "06货架安装切换按钮");
        if (template == null)
        {
            return null;
        }

        GameObject tab = UnityEngine.Object.Instantiate(template.gameObject, chapterBar);
        tab.name = M10SwitchButtonName;
        tab.transform.SetAsLastSibling();
        tab.SetActive(true);
        return tab.GetComponent<PressableButton>() ??
               tab.GetComponentInChildren<PressableButton>(true);
    }

    private static void ConfigureButton(PressableButton button, string label, string panelName)
    {
        SetLabel(button, label);
        if (BoundButtonIds.Add(button.GetInstanceID()))
        {
            button.OnClicked.AddListener(() => ShowPanel(panelName));
        }
    }

    private static void SetLabel(PressableButton button, string label)
    {
        foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == "Text" ||
                (text.name == "Label" &&
                 !text.text.StartsWith("Say ", StringComparison.Ordinal)))
            {
                text.text = label;
            }
        }
    }

    private static void SetPanelActive(Transform root, string panelName, string selectedPanelName)
    {
        Transform panel = root.Find(panelName);
        if (panel != null)
        {
            panel.gameObject.SetActive(string.Equals(panelName, selectedPanelName, StringComparison.Ordinal));
        }
    }

    private static Transform FindBackpackRoot(Transform value)
    {
        for (Transform current = value; current != null; current = current.parent)
        {
            if (current.name == BackpackRootName)
            {
                return current;
            }
        }

        return null;
    }
}
