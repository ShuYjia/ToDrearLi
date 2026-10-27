using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Guards module reachability in the mainline 03 business scene. Modules 03 and
/// 04 stay soft-removed; 11流程测试 is reachable again as the temporary
/// three-topic process test. The modules keep their assets, their Config.xml
/// entries and their step-tree slots, so the eleven ordered slots must never be
/// trimmed.
/// </summary>
public sealed class TexCore_Singleplayer03SceneTests
{
    private const string ScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";
    private const string CourseName = "01货到人拣选电商仓储实施安装与调试实训系统";
    private const string BackpackName = "背包UI";
    private const string ProcessTestModuleName = "11流程测试";

    private static readonly string[] ObjectTreeNames = { "有交互物体", "无交互物体" };

    /// <summary>Modules offered by the 03 startup menu, in Config.xml order.</summary>
    private static readonly string[] VisibleModuleNames =
    {
        "01服务器网络搭建",
        "02服务器系统安装",
        "05划线贴码",
        "06货架安装",
        "07播种墙安装",
        "08充电桩安装",
        "09机器人入场",
        "10货架入场",
        "11流程测试"
    };

    /// <summary>Modules kept in the scene data but hidden from the flow.</summary>
    private static readonly string[] HiddenModuleNames =
    {
        "03RMS软件系统安装",
        "04RMS系统导入地图"
    };

    /// <summary>Backpack panel prefixes the hidden modules would own.</summary>
    private static readonly string[] HiddenBackpackPrefixes = { "03", "04" };

    /// <summary>临时 11 复用 09-03 / 09-05 / 10-02 后固定的三题顺序。</summary>
    private static readonly string[] ProcessTestTopicNames =
    {
        "01检查机器人",
        "02机器人推至二维码",
        "03机器人搬运至货架点"
    };

    private static readonly string[] ModuleStepTreeOrder =
    {
        "01服务器网络搭建",
        "02服务器系统安装",
        "03RMS软件系统安装",
        "04RMS系统导入地图",
        "05划线贴码",
        "06货架安装",
        "07播种墙安装",
        "08充电桩安装",
        "09机器人入场",
        "10货架入场",
        "11流程测试"
    };

    [Test]
    public void HeadMenu_OffersOnlyTheActiveModules()
    {
        WithScene(scene =>
        {
            TexCore_HeadMenu menu = FindSceneComponent<TexCore_HeadMenu>(scene);
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.node, Is.EqualTo(CourseName));

            SerializedProperty allowed = new SerializedObject(menu).FindProperty("allowedModuleNames");
            Assert.That(allowed, Is.Not.Null);
            Assert.That(allowed.arraySize, Is.EqualTo(VisibleModuleNames.Length),
                "03 场景菜单必须只放行当前上线模块。");

            List<string> serialized = Enumerable.Range(0, allowed.arraySize)
                .Select(index => allowed.GetArrayElementAtIndex(index).stringValue)
                .ToList();
            CollectionAssert.AreEqual(VisibleModuleNames, serialized);

            foreach (string module in VisibleModuleNames)
            {
                Assert.That(menu.IsModuleAllowed(module), Is.True, module + " 必须继续可从菜单进入。");
            }

            foreach (string module in HiddenModuleNames)
            {
                Assert.That(menu.IsModuleAllowed(module), Is.False, module + " 不得出现在菜单中。");
            }
        });
    }

    [Test]
    public void HiddenModules_AreDisabledInBothObjectTrees()
    {
        WithScene(scene =>
        {
            foreach (string treeName in ObjectTreeNames)
            {
                Transform course = CourseRoot(scene, treeName);
                foreach (string module in HiddenModuleNames)
                {
                    Transform moduleRoot = course.Find(module);
                    Assert.That(moduleRoot, Is.Not.Null,
                        treeName + "/" + CourseName + " 必须保留 " + module + " 根节点。");
                    Assert.That(moduleRoot.gameObject.activeSelf, Is.False,
                        treeName + "/" + CourseName + "/" + module + " 必须是禁用状态。");
                }
            }
        });
    }

    /// <summary>
    /// 11流程测试 重新回到可进入范围后，两个对象树的根节点必须同步启用；
    /// 运行期仍由 NodePanelBase 按当前节点重算激活状态。
    /// </summary>
    [Test]
    public void ProcessTestModule_IsEnabledInBothObjectTrees()
    {
        WithScene(scene =>
        {
            foreach (string treeName in ObjectTreeNames)
            {
                Transform course = CourseRoot(scene, treeName);
                Transform moduleRoot = course.Find(ProcessTestModuleName);
                Assert.That(moduleRoot, Is.Not.Null,
                    treeName + "/" + CourseName + " 必须保留 " + ProcessTestModuleName + " 根节点。");
                Assert.That(moduleRoot.gameObject.activeSelf, Is.True,
                    treeName + "/" + CourseName + "/" + ProcessTestModuleName + " 必须处于启用状态。");
            }
        });
    }

    /// <summary>
    /// 临时 11 只保留三个操作题，顺序固定为 01检查机器人 / 02机器人推至二维码 /
    /// 03机器人搬运至货架点，且每题内部的 stepIndex 从 0 连续递增。
    /// </summary>
    [Test]
    public void ProcessTestModule_HasOnlyTheThreeReusedOperationTopics()
    {
        WithScene(scene =>
        {
            TexCore_StepObj stepObjects = FindSceneComponent<TexCore_StepObj>(scene);
            Assert.That(stepObjects, Is.Not.Null);

            NodeObjs node = stepObjects.interacterObjs.Single(value =>
                value != null && value.nodePrefab != null &&
                value.nodePrefab.name == ProcessTestModuleName);
            Assert.That(node.nodeObjs, Has.Count.EqualTo(ProcessTestTopicNames.Length));

            for (int index = 0; index < ProcessTestTopicNames.Length; index++)
            {
                TopicObjs topic = node.nodeObjs[index];
                Assert.That(topic, Is.Not.Null, "11 第 " + index + " 个题目槽位丢失。");
                Assert.That(topic.topicPrefab, Is.Not.Null, "11 第 " + index + " 个题目没有 Prefab 引用。");
                Assert.That(topic.topicPrefab.name, Is.EqualTo(ProcessTestTopicNames[index]),
                    "11 题目顺序必须是 " + string.Join(" / ", ProcessTestTopicNames) + "。");

                List<StepObjs> steps = topic.topicObjs ?? new List<StepObjs>();
                Assert.That(steps, Is.Not.Empty, topic.topicPrefab.name + " 必须至少有一个步骤。");
                CollectionAssert.AreEqual(Enumerable.Range(0, steps.Count),
                    steps.Select(value => value.stepIndex).ToList(),
                    topic.topicPrefab.name + " 的 stepIndex 必须从 0 连续递增。");
                foreach (StepObjs step in steps)
                {
                    Assert.That(step.stepObjs, Is.Not.Null, topic.topicPrefab.name + " 的步骤对象列表为空引用。");
                    Assert.That(step.stepObjs, Has.None.Null, topic.topicPrefab.name + " 存在空步骤对象引用。");
                }
            }
        });
    }

    [Test]
    public void StepTree_KeepsAllElevenModulesInConfigOrder()
    {
        WithScene(scene =>
        {
            TexCore_StepObj stepObjects = FindSceneComponent<TexCore_StepObj>(scene);
            Assert.That(stepObjects, Is.Not.Null);
            Assert.That(stepObjects.interacterObjs, Has.Count.EqualTo(ModuleStepTreeOrder.Length),
                "步骤树不得删除 03/04/11，否则后续模块索引会错位。");

            for (int index = 0; index < ModuleStepTreeOrder.Length; index++)
            {
                NodeObjs node = stepObjects.interacterObjs[index];
                Assert.That(node, Is.Not.Null, "步骤树第 " + index + " 个节点丢失。");
                Assert.That(node.nodePrefab, Is.Not.Null, "步骤树第 " + index + " 个节点没有 Prefab 引用。");
                Assert.That(node.nodePrefab.name, Is.EqualTo(ModuleStepTreeOrder[index]),
                    "步骤树第 " + index + " 个节点必须仍是 " + ModuleStepTreeOrder[index] + "。");
                Assert.That(node.nodeObjs, Is.Not.Null,
                    "步骤树第 " + index + " 个节点的题目列表不得为空引用。");
            }
        });
    }

    [Test]
    public void Backpack_HasNoPanelForTheHiddenModules()
    {
        WithScene(scene =>
        {
            Transform backpack = FindSceneTransform(scene, BackpackName);
            Assert.That(backpack, Is.Not.Null, "03 场景缺少背包UI。");
            Assert.That(backpack.Find("01通用物品背包面板"), Is.Null,
                "01通用物品章节已下线，不得再出现在 03 背包中。");
            Assert.That(backpack.Find("02服务器系统安装背包面板"), Is.Not.Null,
                "背包默认章节必须保留 02服务器系统安装背包面板。");

            foreach (Transform child in backpack)
            {
                foreach (string prefix in HiddenBackpackPrefixes)
                {
                    Assert.That(child.name.StartsWith(prefix, StringComparison.Ordinal), Is.False,
                        "背包面板 " + child.name + " 属于已隐藏模块。");
                }
            }
        });
    }

    [Test]
    public void SceneAsset_StaysEnabledInBuildSettings()
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);

        EditorBuildSettingsScene entry = EditorBuildSettings.scenes
            .SingleOrDefault(scene => scene.path == ScenePath);
        Assert.That(entry, Is.Not.Null, "03 场景必须保留在 Build Settings 中。");
        Assert.That(entry.enabled, Is.True, "03 场景必须保持启用。");
        Assert.That(EditorBuildSettings.scenes
                .Single(scene => scene.path == "Assets/A_MRNet/Sence/04SingleplayerScene.unity").enabled,
            Is.False, "04SingleplayerScene 仍然保持禁用。");
    }

    [Test]
    public void Scene_HasNoMissingScripts()
    {
        WithScene(scene =>
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    Assert.That(component, Is.Not.Null, "03 场景不能保留 Missing Script：" + root.name);
                }
            }
        });
    }

    private static Transform CourseRoot(Scene scene, string treeName)
    {
        Transform tree = FindSceneTransform(scene, treeName);
        Assert.That(tree, Is.Not.Null, "03 场景缺少“" + treeName + "”根节点。");
        Transform course = tree.Find(CourseName);
        Assert.That(course, Is.Not.Null, treeName + " 缺少 " + CourseName + "。");
        return course;
    }

    private static void WithScene(Action<Scene> verify)
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool closeAfterTest = !scene.IsValid() || !scene.isLoaded;
        if (closeAfterTest)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        try
        {
            verify(scene);
        }
        finally
        {
            if (closeAfterTest && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    private static Transform FindSceneTransform(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == objectName)
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
