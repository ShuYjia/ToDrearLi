using System.Collections.Generic;
using System.IO;
using System.Linq;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 03 业务场景禁止双手缩放的回归测试：场景序列化数据、运行时动态对象
/// 和“只影响 03”的边界必须同时成立。
/// </summary>
public sealed class TexCore_03NoScalePolicyTests
{
    private const string BusinessScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";
    private const string ScaleCapablePrefabPath = "Assets/Resources/TopicPrefabs/视频题.prefab";
    private const string ProbeSourceScenePath =
        "Assets/APP/MRTKSamples/Scenes/EmptyScene/SampleEmptyMRTKScene.unity";
    private const string ProbeFolder = "Assets/__TexCore03NoScalePolicyProbe";
    private const string ProbeScenePath = ProbeFolder + "/03SingleplayerScene.unity";
    private const string OtherProbeScenePath = ProbeFolder + "/OtherSceneProbe.unity";

    /// <summary>
    /// 03 场景当前合法的操纵权限分布，改动场景后需要同步更新。
    /// 147 = 场景序列化的 145 个 ObjectManipulator + 2 个 TexCore_M10RackCarrier
    /// 预制体实例各 1 个（10货架入场 与 临时 11流程测试）。
    /// 145 个里有 11 个来自临时 11流程测试 复制进来的 09 机器人与 10 机器人；
    /// 这两个机器人只允许水平平移，所以旋转计数比它们仍可旋转时少 1。
    /// </summary>
    private const int ExpectedManipulatorCount = 147;
    private const int ExpectedMoveCount = 145;
    private const int ExpectedRotateCount = 91;

    private Scene probeScene;
    private Scene otherProbeScene;
    private Scene previousActiveScene;

    [OneTimeSetUp]
    public void CreateProbeScene()
    {
        previousActiveScene = SceneManager.GetActiveScene();
        if (!AssetDatabase.IsValidFolder(ProbeFolder))
        {
            AssetDatabase.CreateFolder("Assets", "__TexCore03NoScalePolicyProbe");
        }

        probeScene = CreateProbeScene(ProbeScenePath);
        otherProbeScene = CreateProbeScene(OtherProbeScenePath);
    }

    [OneTimeTearDown]
    public void DeleteProbeScene()
    {
        CloseProbeScene(probeScene);
        CloseProbeScene(otherProbeScene);
        if (AssetDatabase.IsValidFolder(ProbeFolder))
        {
            AssetDatabase.DeleteAsset(ProbeFolder);
        }

        if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(previousActiveScene);
        }
    }

    [Test]
    public void Scene_ObjectManipulators_DropScaleAndKeepMoveRotate()
    {
        WithBusinessScene(scene =>
        {
            // 场景策略在缓存失效时走的就是这个入口，它必须能在真实 03 场景上重复执行。
            Assert.DoesNotThrow(() => TexCore_03NoScalePolicy.EnforceScene(scene));

            ObjectManipulator[] manipulators = FindInScene<ObjectManipulator>(scene);
            Assert.That(manipulators.Length, Is.EqualTo(ExpectedManipulatorCount),
                "03 场景的 ObjectManipulator 数量已变化，请重新确认权限分布。");

            int moveCount = 0;
            int rotateCount = 0;
            foreach (ObjectManipulator manipulator in manipulators)
            {
                string owner = PathOf(manipulator.transform);
                Assert.That(manipulator.AllowedManipulations & TransformFlags.Scale,
                    Is.EqualTo(TransformFlags.None),
                    owner + " 仍然允许双手缩放。");
                Assert.That(
                    manipulator.AllowedManipulations & ~(TransformFlags.Move | TransformFlags.Rotate),
                    Is.EqualTo(TransformFlags.None),
                    owner + " 出现了移动/旋转之外的未知权限位。");

                if ((manipulator.AllowedManipulations & TransformFlags.Move) != 0) moveCount++;
                if ((manipulator.AllowedManipulations & TransformFlags.Rotate) != 0) rotateCount++;
            }

            Assert.That(moveCount, Is.EqualTo(ExpectedMoveCount),
                "只有 1 个物体被有意设为不可操作，其余物体必须保留移动权限。");
            Assert.That(rotateCount, Is.EqualTo(ExpectedRotateCount),
                "旋转权限必须保持原有的 1 个纯旋转 + 90 个移动旋转配置。");
        });
    }

    [Test]
    public void Scene_BoundsControls_DropScaleHandles()
    {
        WithBusinessScene(scene =>
        {
            BoundsControl[] boundsControls = FindInScene<BoundsControl>(scene);
            Assert.That(boundsControls.Length, Is.GreaterThan(0));

            foreach (BoundsControl boundsControl in boundsControls)
            {
                Assert.That(boundsControl.EnabledHandles & HandleType.Scale,
                    Is.EqualTo(HandleType.None),
                    PathOf(boundsControl.transform) + " 仍然开放缩放手柄。");
            }
        });
    }

    [TestCase(7, 3, TestName = "Enforce_ClearsOnlyScale_MoveRotateScale_BecomesMoveRotate")]
    [TestCase(5, 1, TestName = "Enforce_ClearsOnlyScale_MoveScale_BecomesMove")]
    [TestCase(1, 1, TestName = "Enforce_ClearsOnlyScale_MoveStaysMove")]
    [TestCase(2, 2, TestName = "Enforce_ClearsOnlyScale_RotateStaysRotate")]
    [TestCase(0, 0, TestName = "Enforce_ClearsOnlyScale_NoneStaysNone")]
    public void Enforce_ClearsOnlyTheScaleBit(int allowed, int expected)
    {
        GameObject probe = CreateProbeObject("No Scale Policy Probe");
        try
        {
            ObjectManipulator manipulator = probe.AddComponent<ObjectManipulator>();
            manipulator.AllowedManipulations = (TransformFlags)allowed;

            TexCore_03NoScalePolicy.Enforce(manipulator);

            Assert.That((int)manipulator.AllowedManipulations, Is.EqualTo(expected));
        }
        finally
        {
            Object.DestroyImmediate(probe);
        }
    }

    [Test]
    public void Enforce_BoundsControl_DropsScaleAndKeepsRotation()
    {
        GameObject probe = CreateProbeObject("Bounds Control Probe");
        try
        {
            // 保持未激活，避免 BoundsControl.Awake 在 EditMode 里生成手柄视觉。
            probe.SetActive(false);
            BoundsControl boundsControl = probe.AddComponent<BoundsControl>();
            boundsControl.EnabledHandles = HandleType.Rotation | HandleType.Scale;

            TexCore_03NoScalePolicy.Enforce(boundsControl);

            Assert.That(boundsControl.EnabledHandles, Is.EqualTo(HandleType.Rotation));

            boundsControl.EnabledHandles = HandleType.Scale;
            TexCore_03NoScalePolicy.Enforce(boundsControl);
            Assert.That(boundsControl.EnabledHandles, Is.EqualTo(HandleType.None));
        }
        finally
        {
            Object.DestroyImmediate(probe);
        }
    }

    [Test]
    public void RuntimeInstantiatedScalePrefab_LosesScaleOnlyInside03()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScaleCapablePrefabPath);
        Assert.That(prefab, Is.Not.Null, "缺少带默认缩放能力的 Prefab 样本：" + ScaleCapablePrefabPath);
        ObjectManipulator prefabManipulator = prefab.GetComponentInChildren<ObjectManipulator>(true);
        Assert.That(prefabManipulator, Is.Not.Null, "样本 Prefab 必须自带 ObjectManipulator。");
        Assert.That(prefabManipulator.AllowedManipulations & TransformFlags.Scale,
            Is.Not.EqualTo(TransformFlags.None),
            "样本 Prefab 必须默认允许缩放，否则本测试没有意义。");

        GameObject instance = Object.Instantiate(prefab);
        try
        {
            SceneManager.MoveGameObjectToScene(instance, probeScene);
            ObjectManipulator[] manipulators = instance.GetComponentsInChildren<ObjectManipulator>(true);
            Assert.That(manipulators.Length, Is.GreaterThan(0));
            Assert.That(manipulators.Any(value => (value.AllowedManipulations & TransformFlags.Scale) != 0),
                Is.True, "实例化后的对象必须仍然带着 Prefab 默认的缩放能力。");

            TexCore_03NoScalePolicy.Enforce(instance);

            foreach (ObjectManipulator manipulator in manipulators)
            {
                Assert.That(manipulator.AllowedManipulations & TransformFlags.Scale,
                    Is.EqualTo(TransformFlags.None),
                    PathOf(manipulator.transform) + " 在 03 场景实例化后仍可缩放。");
                Assert.That(manipulator.AllowedManipulations & TransformFlags.Move,
                    Is.Not.EqualTo(TransformFlags.None),
                    PathOf(manipulator.transform) + " 丢失了移动权限。");
            }

            // 共享业务 Prefab 资源本身不允许被改写。
            Assert.That(prefabManipulator.AllowedManipulations & TransformFlags.Scale,
                Is.Not.EqualTo(TransformFlags.None),
                "共享业务 Prefab 的默认缩放能力必须保持不变。");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void Enforce_LeavesObjectsOutside03Untouched()
    {
        GameObject probe = CreateProbeObject("Other Scene Probe", otherProbeScene);
        try
        {
            ObjectManipulator manipulator = probe.AddComponent<ObjectManipulator>();
            manipulator.AllowedManipulations =
                TransformFlags.Move | TransformFlags.Rotate | TransformFlags.Scale;

            TexCore_03NoScalePolicy.Enforce(probe);

            Assert.That(manipulator.AllowedManipulations,
                Is.EqualTo(TransformFlags.Move | TransformFlags.Rotate | TransformFlags.Scale),
                "03 之外的场景不允许被本策略改动。");
            Assert.That(TexCore_03NoScalePolicy.IsBusinessScene(otherProbeScene.name), Is.False);
            Assert.That(TexCore_03NoScalePolicy.IsBusinessScene(string.Empty), Is.False);
            Assert.That(TexCore_03NoScalePolicy.IsBusinessScene(
                TexCore_03NoScalePolicy.BusinessSceneName), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(probe);
        }
    }

    [Test]
    public void SceneFlowPolicy_EnforcesNoScaleOutsideUpdate()
    {
        string policySource = ReadScript("TexCore_03NoScalePolicy.cs");
        Assert.That(policySource, Does.Not.Contain("void Update("),
            "禁止缩放策略必须是静态工具类，不能自己逐帧扫描。");

        string flowSource = ReadScript("TexCore_03SceneFlowPolicy.cs");
        string update = ExtractMethod(flowSource, "private void Update()");
        Assert.That(update, Does.Not.Contain("TexCore_03NoScalePolicy"));
        Assert.That(update, Does.Not.Contain("FindObjectsOfType"));
        Assert.That(flowSource, Does.Contain("TexCore_03NoScalePolicy.EnforceScene(policyScene)"),
            "03 场景策略必须在缓存失效时执行一次性全场景限制。");
    }

    private GameObject CreateProbeObject(string name)
    {
        return CreateProbeObject(name, probeScene);
    }

    private static GameObject CreateProbeObject(string name, Scene scene)
    {
        GameObject probe = new GameObject(name);
        SceneManager.MoveGameObjectToScene(probe, scene);
        return probe;
    }

    /// <summary>
    /// 复制一个极小的现有场景得到探针场景：场景名来自资源文件名，所以目标文件必须
    /// 叫 03SingleplayerScene.unity。EditMode 下 Runner 会保留一个未保存的空场景，
    /// 无法再用 NewScene(Additive) 新建场景，因此这里改用复制 + 加性打开。
    /// </summary>
    private static Scene CreateProbeScene(string destinationPath)
    {
        AssetDatabase.CopyAsset(ProbeSourceScenePath, destinationPath);
        return EditorSceneManager.OpenScene(destinationPath, OpenSceneMode.Additive);
    }

    private static void CloseProbeScene(Scene scene)
    {
        if (scene.IsValid() && scene.isLoaded)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void WithBusinessScene(System.Action<Scene> verify)
    {
        Scene scene = SceneManager.GetSceneByPath(BusinessScenePath);
        bool closeSceneAfterTest = !scene.IsValid() || !scene.isLoaded;
        if (closeSceneAfterTest)
        {
            scene = EditorSceneManager.OpenScene(BusinessScenePath, OpenSceneMode.Additive);
        }

        try
        {
            verify(scene);
        }
        finally
        {
            if (closeSceneAfterTest && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static T[] FindInScene<T>(Scene scene) where T : Component
    {
        return Object.FindObjectsOfType<T>(true)
            .Where(value => value != null && value.gameObject.scene == scene)
            .ToArray();
    }

    private static string PathOf(Transform value)
    {
        List<string> parts = new List<string>();
        for (Transform current = value; current != null; current = current.parent)
        {
            parts.Add(current.name);
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private static string ReadScript(string fileName)
    {
        return File.ReadAllText(Path.Combine(Application.dataPath, "A_MRNet", "Scripts", fileName));
    }

    private static string ExtractMethod(string source, string signature)
    {
        int start = source.IndexOf(signature);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), signature);
        int bodyStart = source.IndexOf('{', start);
        int depth = 0;
        for (int index = bodyStart; index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            else if (source[index] == '}' && --depth == 0)
                return source.Substring(bodyStart, index - bodyStart + 1);
        }

        Assert.Fail("方法体未闭合：" + signature);
        return string.Empty;
    }
}
