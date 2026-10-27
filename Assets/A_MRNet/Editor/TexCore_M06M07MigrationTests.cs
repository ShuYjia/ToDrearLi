using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Guards the 06/07 business closure that was migrated from the 0607 test scene
/// into the mainline 03 scene. The checks fail when a controller, a step, the
/// ordered QR target chain, a 07 guidance anchor, the frame screw material or a
/// backpack binding is lost again, and when another module's root disappears.
/// </summary>
public sealed class TexCore_M06M07MigrationTests
{
    private const string MainlineScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";
    private const string BusinessNode = "01货到人拣选电商仓储实施安装与调试实训系统";
    private const string RackModule = "06货架安装";
    private const string WallModule = "07播种墙安装";
    private const int ExpectedStepCount = 21;
    private const string QrPartKey = "货架二维码";
    private const string BasePartKey = "底座";
    private const string ScrewMaterialAsset = "M_ZhiJia";
    private const int MinimumScrewRendererCount = 8;

    private static readonly string[] InteractiveModules =
    {
        "01服务器网络搭建",
        "02服务器系统安装",
        "03RMS软件系统安装",
        "04RMS系统导入地图",
        "05划线贴码",
        RackModule,
        WallModule,
        "08充电桩安装",
        "09机器人入场",
        "10货架入场",
        "11流程测试"
    };

    [Test]
    public void MainlineScene_KeepsOneControllerForEachMigratedModule()
    {
        WithMainlineScene(scene =>
        {
            TexCore_RackAssemblyController[] controllers = FindInScene<TexCore_RackAssemblyController>(scene);
            Assert.That(controllers, Has.Length.EqualTo(2),
                "03 主场景必须同时保留 06 与 07 的货架装配控制器。");

            TexCore_RackAssemblyController rack = controllers.Single(value => PrefixOf(value) == "06");
            TexCore_RackAssemblyController wall = controllers.Single(value => PrefixOf(value) == "07");

            Assert.That(PathOf(rack.transform), Does.Contain(RackModule));
            Assert.That(PathOf(wall.transform), Does.Contain(WallModule));
            Assert.That(BackpackPanelOf(rack), Is.Not.Null, "06 控制器必须绑定背包面板。");
            Assert.That(BackpackPanelOf(wall), Is.Not.Null, "07 控制器必须绑定背包面板。");
            Assert.That(BackpackPanelOf(rack), Is.Not.SameAs(BackpackPanelOf(wall)),
                "06 与 07 必须各自拥有独立背包面板。");
        });
    }

    [Test]
    public void MigratedModules_KeepCompleteStepLists()
    {
        WithMainlineScene(scene =>
        {
            foreach (TexCore_RackAssemblyController controller in
                     FindInScene<TexCore_RackAssemblyController>(scene))
            {
                string prefix = PrefixOf(controller);
                List<SerializedProperty> steps = StepsOf(controller);
                Assert.That(steps, Has.Count.EqualTo(ExpectedStepCount),
                    $"{prefix} 关卡必须保留完整的 {ExpectedStepCount} 个步骤。");

                for (int index = 0; index < steps.Count; index++)
                {
                    SerializedProperty step = steps[index];
                    Assert.That(step.FindPropertyRelative("name").stringValue, Is.Not.Empty,
                        $"{prefix} 第 {index} 步缺少名称。");
                    Assert.That(step.FindPropertyRelative("kind").enumValueIndex,
                        Is.InRange(0, (int)RackAssemblyStepKind.MoveAssembly),
                        $"{prefix} 第 {index} 步的步骤类型越界。");

                    foreach (string field in new[]
                             {
                                 "blueTargets", "greenTargets", "resultObjects", "toolTargets",
                                 "guidanceAnchors"
                             })
                    {
                        SerializedProperty list = step.FindPropertyRelative(field);
                        for (int i = 0; i < list.arraySize; i++)
                        {
                            Object reference = list.GetArrayElementAtIndex(i).objectReferenceValue;
                            if (reference == null)
                            {
                                continue;
                            }

                            Assert.That(((Component)reference).gameObject.scene, Is.EqualTo(scene),
                                $"{prefix} 第 {index} 步的 {field}[{i}] 指向了 03 场景之外的对象。");
                        }
                    }
                }
            }
        });
    }

    [Test]
    public void RackModule_KeepsOrderedQrTargetChain()
    {
        WithMainlineScene(scene =>
        {
            TexCore_RackAssemblyController rack = FindInScene<TexCore_RackAssemblyController>(scene)
                .Single(value => PrefixOf(value) == "06");
            SerializedProperty orderedStep = StepsOf(rack)
                .Single(value => value.FindPropertyRelative("requireOrderedTargets").boolValue);

            Assert.That(orderedStep.FindPropertyRelative("spawnEulerOffset").vector3Value,
                Is.EqualTo(new Vector3(180f, 0f, 0f)),
                "06 底座步骤必须保留 180 度翻转姿态。");

            List<string> partKeys = StringList(orderedStep.FindPropertyRelative("targetPartKeys"));
            Assert.That(partKeys, Is.EqualTo(new List<string> { QrPartKey, BasePartKey }),
                "06 底座步骤必须按“先二维码、后底座”的顺序声明目标部件。");
            Assert.That(StringList(orderedStep.FindPropertyRelative("autoSpawnPartKeys")),
                Does.Contain(QrPartKey),
                "06 底座步骤必须自动生成货架二维码。");

            SerializedProperty blueTargets = orderedStep.FindPropertyRelative("blueTargets");
            Assert.That(blueTargets.arraySize, Is.EqualTo(2),
                "06 底座步骤必须同时保留二维码与底座的蓝色目标位。");
            for (int i = 0; i < blueTargets.arraySize; i++)
            {
                Assert.That(blueTargets.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
            }
        });
    }

    [Test]
    public void WallModule_KeepsGuidanceAnchors()
    {
        WithMainlineScene(scene =>
        {
            TexCore_RackAssemblyController wall = FindInScene<TexCore_RackAssemblyController>(scene)
                .Single(value => PrefixOf(value) == "07");
            List<SerializedProperty> anchored = StepsOf(wall)
                .Where(value => value.FindPropertyRelative("guidanceAnchors").arraySize > 0)
                .ToList();

            Assert.That(anchored, Has.Count.EqualTo(2),
                "07 必须保留电脑主机与键盘两处引导锚点。");

            foreach (SerializedProperty step in anchored)
            {
                // The migrated source leaves one optional slot empty on the
                // keyboard step, so a list only has to carry a usable anchor.
                SerializedProperty anchors = step.FindPropertyRelative("guidanceAnchors");
                List<Object> resolved = new List<Object>();
                for (int i = 0; i < anchors.arraySize; i++)
                {
                    Object anchor = anchors.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (anchor == null)
                    {
                        continue;
                    }

                    Assert.That(((Component)anchor).gameObject.scene, Is.EqualTo(scene));
                    resolved.Add(anchor);
                }

                Assert.That(resolved, Is.Not.Empty,
                    $"{step.FindPropertyRelative("name").stringValue} 的引导锚点全部为空。");
            }
        });
    }

    [Test]
    public void MigratedModules_KeepFrameScrewAndTemplateMaterials()
    {
        WithMainlineScene(scene =>
        {
            int screwRenderers = 0;
            foreach (string module in new[] { RackModule, WallModule })
            {
                Transform root = FindByPath(scene, $"无交互物体/{BusinessNode}/{module}");
                Assert.That(root, Is.Not.Null, $"{module} 的无交互物体根节点丢失。");

                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    // Three objects (XiangGai, ShuiPingYi, AGV1) already carried an
                    // empty material slot in the 0607 source, so this audit only
                    // guards the lists themselves plus the restored screw material.
                    Material[] materials = renderer.sharedMaterials;
                    Assert.That(materials, Is.Not.Empty,
                        $"{module}/{PathOf(renderer.transform)} 丢失了材质槽。");

                    if (module == WallModule && materials.Any(value => value != null &&
                            Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(value)) ==
                            ScrewMaterialAsset))
                    {
                        screwRenderers++;
                    }
                }
            }

            Assert.That(screwRenderers, Is.GreaterThanOrEqualTo(MinimumScrewRendererCount),
                $"07 支架螺丝必须继续使用 {ScrewMaterialAsset}，至少 {MinimumScrewRendererCount} 个。");
        });
    }

    [Test]
    public void InteractiveModules_KeepSingleRootPerModule()
    {
        WithMainlineScene(scene =>
        {
            Transform root = FindByPath(scene, "有交互物体");
            Assert.That(root, Is.Not.Null, "03 场景缺少“有交互物体”根节点。");
            Transform business = FindChild(root, BusinessNode);
            Assert.That(business, Is.Not.Null);

            foreach (string module in InteractiveModules)
            {
                int count = 0;
                for (int i = 0; i < business.childCount; i++)
                {
                    if (business.GetChild(i).name == module)
                    {
                        count++;
                    }
                }

                Assert.That(count, Is.EqualTo(1),
                    $"有交互物体/{BusinessNode} 下的 {module} 根节点数量应为 1，实际 {count}。");
            }
        });
    }

    private static void WithMainlineScene(System.Action<Scene> body)
    {
        Scene scene = SceneManager.GetSceneByPath(MainlineScenePath);
        bool closeSceneAfterTest = !scene.IsValid() || !scene.isLoaded;
        if (closeSceneAfterTest)
        {
            scene = EditorSceneManager.OpenScene(MainlineScenePath, OpenSceneMode.Additive);
        }

        try
        {
            body(scene);
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

    private static string PrefixOf(TexCore_RackAssemblyController controller)
    {
        return new SerializedObject(controller).FindProperty("spawnedObjectPrefix").stringValue;
    }

    private static GameObject BackpackPanelOf(TexCore_RackAssemblyController controller)
    {
        return new SerializedObject(controller).FindProperty("backpackPanel").objectReferenceValue as GameObject;
    }

    private static List<SerializedProperty> StepsOf(TexCore_RackAssemblyController controller)
    {
        SerializedProperty steps = new SerializedObject(controller).FindProperty("steps");
        List<SerializedProperty> result = new List<SerializedProperty>(steps.arraySize);
        for (int i = 0; i < steps.arraySize; i++)
        {
            result.Add(steps.GetArrayElementAtIndex(i));
        }

        return result;
    }

    private static List<string> StringList(SerializedProperty property)
    {
        List<string> result = new List<string>(property.arraySize);
        for (int i = 0; i < property.arraySize; i++)
        {
            result.Add(property.GetArrayElementAtIndex(i).stringValue);
        }

        return result;
    }

    private static Transform FindByPath(Scene scene, string path)
    {
        Transform current = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            string[] segments = path.Split('/');
            if (root.name != segments[0])
            {
                continue;
            }

            current = root.transform;
            for (int i = 1; i < segments.Length && current != null; i++)
            {
                current = FindChild(current, segments[i]);
            }

            if (current != null)
            {
                return current;
            }
        }

        return null;
    }

    private static Transform FindChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    private static string PathOf(Transform value)
    {
        List<string> parts = new List<string>();
        Transform current = value;
        while (current != null)
        {
            parts.Add(current.name);
            current = current.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }
}
