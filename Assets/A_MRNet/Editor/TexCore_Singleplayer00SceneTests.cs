using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TexCore_Singleplayer00SceneTests
{
    private const string StartupScenePath = "Assets/A_MRNet/Sence/01StartUp.unity";
    private const string ScenePath = "Assets/A_MRNet/Sence/00SingleplayerScene.unity";
    private const string FullScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";
    private const string CourseName = "01货到人拣选电商仓储实施安装与调试实训系统";
    private const string ModuleName = "01服务器网络搭建";

    private static readonly string[] RemovedModuleNames =
    {
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

    private static readonly string[] RemovedBackpackChildren =
    {
        "02服务器系统安装背包面板",
        "05划线贴码背包面板",
        "06货架安装背包面板",
        "07播种墙安装背包面板",
        "08充电桩安装背包面板",
        "10货架入场背包面板",
        "Button Bar_Horizontal"
    };

    [Test]
    public void SceneAsset_HasIndependentGuid_AndIsEnabledAfterStartup()
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
        Assert.That(AssetDatabase.AssetPathToGUID(ScenePath), Is.Not.Empty);
        Assert.That(AssetDatabase.AssetPathToGUID(ScenePath),
            Is.Not.EqualTo(AssetDatabase.AssetPathToGUID(FullScenePath)));

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        int startupIndex = Array.FindIndex(scenes, value => value.path == StartupScenePath);
        Assert.That(startupIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(scenes[startupIndex + 1].path, Is.EqualTo(ScenePath));
        Assert.That(scenes[startupIndex + 1].enabled, Is.True);
        Assert.That(scenes.Single(value => value.path == FullScenePath).enabled, Is.True);
        Assert.That(scenes.Single(value => value.path == "Assets/A_MRNet/Sence/04SingleplayerScene.unity").enabled,
            Is.False);
    }

    [Test]
    public void StepTree_ContainsOnlyServerNetworkModuleWith17Topics()
    {
        WithScene(scene =>
        {
            TexCore_StepObj stepObjects = FindSceneComponent<TexCore_StepObj>(scene);
            Assert.That(stepObjects, Is.Not.Null);
            Assert.That(stepObjects.interacterObjs, Has.Count.EqualTo(1));

            NodeObjs module = stepObjects.interacterObjs[0];
            Assert.That(module.nodePrefab, Is.Not.Null);
            Assert.That(module.nodePrefab.name, Is.EqualTo(ModuleName));
            Assert.That(module.nodeObjs, Has.Count.EqualTo(17));

            foreach (TopicObjs topic in module.nodeObjs)
            {
                Assert.That(topic, Is.Not.Null);
                Assert.That(topic.topicPrefab, Is.Not.Null);
                Assert.That(topic.topicObjs, Is.Not.Null);

                List<int> indices = topic.topicObjs.Select(value => value.stepIndex).OrderBy(value => value).ToList();
                CollectionAssert.AreEqual(Enumerable.Range(0, indices.Count), indices);
                foreach (StepObjs step in topic.topicObjs)
                {
                    Assert.That(step.stepObjs, Is.Not.Null);
                    Assert.That(step.stepObjs, Has.None.Null);
                }
            }
        });
    }

    [Test]
    public void Scene_HasNoOtherModuleRootsOrModuleBackpackContent()
    {
        WithScene(scene =>
        {
            foreach (string containerName in new[] { "有交互物体", "无交互物体" })
            {
                Transform container = FindSceneTransform(scene, containerName);
                Assert.That(container, Is.Not.Null);
                Transform course = container.Find(CourseName);
                Assert.That(course, Is.Not.Null);
                Assert.That(course.Find(ModuleName), Is.Not.Null);
                foreach (string removedModule in RemovedModuleNames)
                {
                    Assert.That(course.Find(removedModule), Is.Null, containerName + " 不得包含 " + removedModule);
                }
            }

            Transform backpack = FindSceneTransform(scene, "背包UI");
            Assert.That(backpack, Is.Not.Null);
            Assert.That(backpack.Find("01通用物品背包面板"), Is.Not.Null);
            foreach (string removedChild in RemovedBackpackChildren)
            {
                Assert.That(backpack.Find(removedChild), Is.Null, "背包不得包含 " + removedChild);
            }
        });
    }

    [Test]
    public void SceneLocalPolicies_Restrict00ToTheFirstModuleAndKeepBaseRuntimeEnabled()
    {
        WithScene(scene =>
        {
            TexCore_HeadMenu menu = FindSceneComponent<TexCore_HeadMenu>(scene);
            SerializedProperty allowedModules = new SerializedObject(menu).FindProperty("allowedModuleNames");
            Assert.That(allowedModules, Is.Not.Null);
            Assert.That(allowedModules.arraySize, Is.EqualTo(1));
            Assert.That(allowedModules.GetArrayElementAtIndex(0).stringValue, Is.EqualTo(ModuleName));
            Assert.That(menu.IsModuleAllowed(ModuleName), Is.True);
            Assert.That(menu.IsModuleAllowed("02服务器系统安装"), Is.False);

            TexCore_03SceneFlowPolicy policy = FindSceneComponent<TexCore_03SceneFlowPolicy>(scene);
            SerializedProperty targetScene = new SerializedObject(policy).FindProperty("targetSceneName");
            Assert.That(targetScene.stringValue, Is.EqualTo("00SingleplayerScene"));
            Assert.That(policy.IsTargetScene("00SingleplayerScene"), Is.True);
            Assert.That(policy.IsTargetScene("03SingleplayerScene"), Is.False);
        });

        Assert.That(TexCore_LoadScene.IsBusinessScene("00SingleplayerScene"), Is.True);
        Assert.That(App.Runtime.HandMenu.ButtonsCanvasHandMenuRuntime.IsButtonsCanvasScene("00SingleplayerScene"),
            Is.True);
        Assert.That(App.Runtime.Locomotion.PicoLeftJoystickLocomotionRuntime.IsSingleplayerScene(
            "00SingleplayerScene"), Is.True);
        Assert.That(App.Runtime.GripTranslation.PicoRightJoystickGripTranslationRuntime.IsSingleplayerScene(
            "00SingleplayerScene"), Is.True);
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
                    Assert.That(component, Is.Not.Null,
                        "00 场景不能保留 Missing Script：" + root.name);
                }
            }
        });
    }

    private static void WithScene(Action<Scene> verify)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            verify(scene);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
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
