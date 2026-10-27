using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MixedReality.Toolkit.UX;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TexCore_M02BackpackSceneTests
{
    private static readonly string[] SharedPanelNames =
    {
        "02服务器系统安装背包面板",
        "05划线贴码背包面板",
        "06货架安装背包面板",
        "07播种墙安装背包面板",
        "08充电桩安装背包面板",
        "10货架入场背包面板"
    };

    [Test]
    public void Scene_HasFixedNumberedM02BackpackPanelAndChapterTab()
    {
        Scene scene = EditorSceneManager.OpenScene(
            TexCore_M02BackpackSceneSetup.ScenePath,
            OpenSceneMode.Additive);
        try
        {
            List<string> errors = TexCore_M02BackpackSceneSetup.ValidateScene(scene);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void BackpackChapterBar_ShowsTheServerInstallTabAndSwitchesExclusively()
    {
        Scene previousScene = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(TexCore_M02BackpackSceneSetup.ScenePath);
        bool closeSceneAfterTest = !scene.IsValid() || !scene.isLoaded;
        if (closeSceneAfterTest)
        {
            scene = EditorSceneManager.OpenScene(
                TexCore_M02BackpackSceneSetup.ScenePath,
                OpenSceneMode.Additive);
        }

        Transform backpack = null;
        GameObject m10TabBefore = null;
        Dictionary<string, bool> panelStates = new Dictionary<string, bool>();
        try
        {
            SceneManager.SetActiveScene(scene);
            backpack = FindBackpack(scene);
            Assert.That(backpack, Is.Not.Null, "场景缺少背包UI。");

            // EnsureInitialized adds the runtime M10 chapter tab and switches
            // panels, so remember what the shared scene looked like.
            m10TabBefore = FindChapterTabObject(backpack, "10货架入场切换按钮");
            foreach (string panelName in SharedPanelNames)
            {
                Transform panel = backpack.Find(panelName);
                if (panel != null)
                {
                    panelStates[panelName] = panel.gameObject.activeSelf;
                }
            }

            TexCore_BackpackChapterTabs.EnsureInitialized();

            PressableButton serverTab = FindChapterTab(
                backpack, TexCore_M02BackpackSceneSetup.ServerInstallTabName);
            Assert.That(serverTab, Is.Not.Null, "章节栏必须保留“02服务器安装”按钮。");
            Assert.That(serverTab.gameObject.activeSelf, Is.True,
                "02 章节按钮不允许再被运行时隐藏。");
            Assert.That(GetButtonLabel(serverTab), Is.EqualTo("02服务器安装"));

            int boundBefore = CountBoundChapterButtons();

            serverTab.OnClicked.Invoke();
            Assert.That(
                IsPanelActive(backpack, TexCore_M02BackpackSceneSetup.ServerInstallPanelName),
                Is.True,
                "点击 02 章节按钮后必须显示 02 背包面板。");
            Assert.That(CountActiveSharedPanels(backpack), Is.EqualTo(1),
                "同一时刻只允许显示一个背包面板。");

            PressableButton rackTab = FindChapterTab(backpack, "06货架安装切换按钮");
            Assert.That(rackTab, Is.Not.Null, "其他背包章节按钮必须仍然存在。");
            rackTab.OnClicked.Invoke();
            Assert.That(
                IsPanelActive(backpack, TexCore_M02BackpackSceneSetup.RackPanelName),
                Is.True,
                "其他背包章节必须仍可正常切换。");
            Assert.That(
                IsPanelActive(backpack, TexCore_M02BackpackSceneSetup.ServerInstallPanelName),
                Is.False);
            Assert.That(CountActiveSharedPanels(backpack), Is.EqualTo(1));

            TexCore_BackpackChapterTabs.EnsureInitialized();
            Assert.That(CountBoundChapterButtons(), Is.EqualTo(boundBefore),
                "重复初始化不允许重复注册章节按钮监听。");
            Assert.That(CountChapterTabs(backpack, "10货架入场切换按钮"), Is.EqualTo(1),
                "重复初始化不允许创建重复的 10货架入场 章节按钮。");
        }
        finally
        {
            if (backpack != null)
            {
                RestoreBackpackState(backpack, panelStates, m10TabBefore);
            }

            if (previousScene.IsValid() && previousScene.isLoaded && previousScene != scene)
            {
                SceneManager.SetActiveScene(previousScene);
            }

            if (closeSceneAfterTest)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    /// <summary>
    /// Chapter tabs are a single shared set per backpack root, so a test that
    /// initializes them must not leave a duplicate M10 tab or a switched panel
    /// behind for the other scene tests.
    /// </summary>
    private static void RestoreBackpackState(
        Transform backpack,
        Dictionary<string, bool> panelStates,
        GameObject m10TabBefore)
    {
        GameObject m10Tab = FindChapterTabObject(backpack, "10货架入场切换按钮");
        if (m10Tab != null && m10TabBefore == null)
        {
            Object.DestroyImmediate(m10Tab);
        }

        foreach (KeyValuePair<string, bool> state in panelStates)
        {
            Transform panel = backpack.Find(state.Key);
            if (panel != null)
            {
                panel.gameObject.SetActive(state.Value);
            }
        }
    }

    private static GameObject FindChapterTabObject(Transform backpack, string tabName)
    {
        PressableButton tab = FindChapterTab(backpack, tabName);
        return tab != null ? tab.gameObject : null;
    }

    private static int CountChapterTabs(Transform backpack, string tabName)
    {
        Transform chapterBar = backpack.Find(TexCore_M02BackpackSceneSetup.ChapterBarName);
        if (chapterBar == null)
        {
            return 0;
        }

        return chapterBar.GetComponentsInChildren<PressableButton>(true)
            .Count(value => value.name == tabName);
    }

    private static Transform FindBackpack(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(
                value => value.name == TexCore_M02BackpackSceneSetup.BackpackRootName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static PressableButton FindChapterTab(Transform backpack, string tabName)
    {
        Transform chapterBar = backpack.Find(TexCore_M02BackpackSceneSetup.ChapterBarName);
        if (chapterBar == null)
        {
            return null;
        }

        return chapterBar.GetComponentsInChildren<PressableButton>(true)
            .FirstOrDefault(value => value.name == tabName);
    }

    private static bool IsPanelActive(Transform backpack, string panelName)
    {
        Transform panel = backpack.Find(panelName);
        return panel != null && panel.gameObject.activeSelf;
    }

    private static int CountActiveSharedPanels(Transform backpack)
    {
        return SharedPanelNames.Count(panelName => IsPanelActive(backpack, panelName));
    }

    private static string GetButtonLabel(PressableButton button)
    {
        TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text target = labels.FirstOrDefault(text => text.name == "Label" || text.name == "Text") ??
                          labels.FirstOrDefault();
        return target != null ? target.text : string.Empty;
    }

    private static int CountBoundChapterButtons()
    {
        FieldInfo field = typeof(TexCore_BackpackChapterTabs).GetField(
            "BoundButtonIds", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        HashSet<int> bound = field.GetValue(null) as HashSet<int>;
        return bound != null ? bound.Count : 0;
    }
}
