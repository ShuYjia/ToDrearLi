using System;
using System.Collections.Generic;
using System.Linq;
using MixedReality.Toolkit.UX;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Bpf.Diagnostics;

public static class TexCore_M02BackpackSceneSetup
{
    public const string ScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";
    public const string BackpackRootName = "背包UI";
    public const string ServerInstallPanelName = "02服务器系统安装背包面板";
    public const string RackPanelName = "06货架安装背包面板";
    public const string SortingWallPanelName = "07播种墙安装背包面板";
    public const string ChargingStationPanelName = "08充电桩安装背包面板";
    public const string ChapterBarName = "Button Bar_Horizontal";
    public const string UsbButtonName = "02_U盘";
    public const string ServerInstallTabName = "02服务器系统安装切换按钮";

    private static readonly string[] ChapterButtonNames =
    {
        ServerInstallTabName,
        "05划线贴码切换按钮",
        "06货架安装切换按钮",
        "07播种墙安装切换按钮",
        "08充电桩安装切换按钮"
    };

    private static readonly string[] ChapterButtonLabels =
    {
        "02服务器安装",
        "05划线贴码",
        "06货架安装",
        "07播种墙安装",
        "08充电桩安装"
    };

    [MenuItem("BPF/03 Server Install/Configure M02 Backpack Panel")]
    public static void ConfigureActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Open '{ScenePath}' before configuring the M02 backpack panel.");
        }

        Transform backpack = FindSceneTransform(scene, BackpackRootName);
        if (backpack == null)
        {
            throw new InvalidOperationException($"Scene is missing '{BackpackRootName}'.");
        }

        Transform rackPanel = RequireDirectChild(backpack, RackPanelName);
        Transform sortingWallPanel = RequireDirectChild(backpack, SortingWallPanelName);
        Transform chargingStationPanel = RequireDirectChild(backpack, ChargingStationPanelName);

        rackPanel.name = RackPanelName;
        sortingWallPanel.name = SortingWallPanelName;
        chargingStationPanel.name = ChargingStationPanelName;

        Transform serverInstallPanel = FindDirectChild(backpack, ServerInstallPanelName);
        if (serverInstallPanel == null)
        {
            GameObject clone = UnityEngine.Object.Instantiate(chargingStationPanel.gameObject, backpack, false);
            clone.name = ServerInstallPanelName;
            serverInstallPanel = clone.transform;
        }

        // 01通用物品章节已下线，02 现在是背包的第一块面板。
        serverInstallPanel.SetSiblingIndex(0);
        ConfigureServerInstallPanel(serverInstallPanel.gameObject);

        Transform chapterBar = RequireDirectChild(backpack, ChapterBarName);
        ConfigureChapterBar(chapterBar);
        serverInstallPanel.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException($"Could not save '{ScenePath}'.");
        }

        List<string> errors = ValidateScene(scene);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException("M02 backpack validation failed:\n- " + string.Join("\n- ", errors));
        }

        BpfLog.Info(BpfLogCategory.Diagnostics, "tex_core_m02_backpack_scene_setup.configure_active_scene", "[BPF-M02-BACKPACK] Configured fixed 02 panel, numbered panels and five chapter tabs.");
    }

    public static List<string> ValidateScene(Scene scene)
    {
        List<string> errors = new List<string>();
        Transform backpack = FindSceneTransform(scene, BackpackRootName);
        if (backpack == null)
        {
            errors.Add($"Missing '{BackpackRootName}'.");
            return errors;
        }

        if (FindDirectChild(backpack, "MenuDemo") != null)
        {
            errors.Add("Legacy default panel name 'MenuDemo' still exists.");
        }

        foreach (string panelName in new[]
                 {
                     ServerInstallPanelName, RackPanelName,
                     SortingWallPanelName, ChargingStationPanelName
                 })
        {
            if (FindDirectChild(backpack, panelName) == null)
            {
                errors.Add($"Missing backpack panel '{panelName}'.");
            }
        }

        Transform serverInstallPanel = FindDirectChild(backpack, ServerInstallPanelName);
        if (serverInstallPanel != null)
        {
            PressableButton[] usbButtons = serverInstallPanel.GetComponentsInChildren<PressableButton>(true);
            if (usbButtons.Length != 1)
            {
                errors.Add($"'{ServerInstallPanelName}' has {usbButtons.Length} buttons; expected 1.");
            }
            else
            {
                if (!string.Equals(usbButtons[0].name, UsbButtonName, StringComparison.Ordinal))
                {
                    errors.Add($"M02 button is named '{usbButtons[0].name}'; expected '{UsbButtonName}'.");
                }
                if (!string.Equals(GetButtonLabel(usbButtons[0]), "U盘", StringComparison.Ordinal))
                {
                    errors.Add("M02 button label is not 'U盘'.");
                }
            }
        }

        Transform chapterBar = FindDirectChild(backpack, ChapterBarName);
        if (chapterBar == null)
        {
            errors.Add($"Missing '{ChapterBarName}'.");
            return errors;
        }

        PressableButton[] chapterButtons = chapterBar.GetComponentsInChildren<PressableButton>(true);
        if (chapterButtons.Length != ChapterButtonNames.Length)
        {
            errors.Add($"Chapter bar has {chapterButtons.Length} buttons; expected {ChapterButtonNames.Length}.");
            return errors;
        }

        for (int index = 0; index < ChapterButtonNames.Length; index++)
        {
            if (!string.Equals(chapterButtons[index].name, ChapterButtonNames[index], StringComparison.Ordinal))
            {
                errors.Add($"Chapter button {index} is '{chapterButtons[index].name}'; expected '{ChapterButtonNames[index]}'.");
            }
            if (!string.Equals(GetButtonLabel(chapterButtons[index]), ChapterButtonLabels[index], StringComparison.Ordinal))
            {
                errors.Add($"Chapter button '{ChapterButtonNames[index]}' has the wrong label.");
            }
        }

        return errors;
    }

    private static void ConfigureServerInstallPanel(GameObject panel)
    {
        PressableButton[] buttons = panel.GetComponentsInChildren<PressableButton>(true);
        if (buttons.Length == 0)
        {
            throw new InvalidOperationException($"'{ServerInstallPanelName}' has no PressableButton template.");
        }

        PressableButton keep = buttons[0];
        for (int index = buttons.Length - 1; index > 0; index--)
        {
            UnityEngine.Object.DestroyImmediate(buttons[index].gameObject);
        }

        keep.gameObject.name = UsbButtonName;
        ClearButtonEvents(keep);
        SetButtonLabel(keep, "U盘");
        EditorUtility.SetDirty(keep.gameObject);
    }

    private static void ConfigureChapterBar(Transform chapterBar)
    {
        PressableButton[] buttons = chapterBar.GetComponentsInChildren<PressableButton>(true);
        PressableButton serverInstallTab = buttons.FirstOrDefault(button => button.name == ServerInstallTabName);
        if (serverInstallTab == null)
        {
            throw new InvalidOperationException($"'{ChapterBarName}' is missing '{ServerInstallTabName}'.");
        }

        PressableButton[] ordered = ChapterButtonNames
            .Select(name => chapterBar.GetComponentsInChildren<PressableButton>(true)
                .FirstOrDefault(button => button.name == name))
            .ToArray();
        if (ordered.Any(button => button == null))
        {
            throw new InvalidOperationException("Could not resolve all five numbered chapter buttons.");
        }

        for (int index = 0; index < ordered.Length; index++)
        {
            ordered[index].transform.SetSiblingIndex(index);
            SetButtonLabel(ordered[index], ChapterButtonLabels[index]);
            EditorUtility.SetDirty(ordered[index].gameObject);
        }
    }

    private static void ClearButtonEvents(PressableButton button)
    {
        if (button == null) return;
        for (int index = button.OnClicked.GetPersistentEventCount() - 1; index >= 0; index--)
        {
            UnityEventTools.RemovePersistentListener(button.OnClicked, index);
        }
        button.OnClicked.RemoveAllListeners();
    }

    private static void SetButtonLabel(PressableButton button, string label)
    {
        TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text target = labels.FirstOrDefault(text => text.name == "Label" || text.name == "Text") ??
                          labels.FirstOrDefault();
        if (target == null)
        {
            throw new InvalidOperationException($"Button '{button.name}' has no TMP label.");
        }
        target.text = label;
        EditorUtility.SetDirty(target);
    }

    private static string GetButtonLabel(PressableButton button)
    {
        TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text target = labels.FirstOrDefault(text => text.name == "Label" || text.name == "Text") ??
                          labels.FirstOrDefault();
        return target != null ? target.text : string.Empty;
    }

    private static Transform RequireDirectChild(Transform parent, string childName)
    {
        Transform child = FindDirectChild(parent, childName);
        if (child == null)
        {
            throw new InvalidOperationException($"'{parent.name}' is missing direct child '{childName}'.");
        }
        return child;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null) return null;
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (string.Equals(child.name, childName, StringComparison.Ordinal)) return child;
        }
        return null;
    }

    private static Transform FindSceneTransform(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => string.Equals(candidate.name, objectName, StringComparison.Ordinal));
            if (match != null) return match;
        }
        return null;
    }
}
