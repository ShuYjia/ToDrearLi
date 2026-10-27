using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MixedReality.Toolkit.SpatialManipulation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class TexCore_M08StepStateTests
{
    [Test]
    public void StaleReporterEnd_DoesNotClearCurrentBackpackStep()
    {
        TexCore_08BackpackController previousInstance =
            GetPrivateStaticField<TexCore_08BackpackController>(
                typeof(TexCore_08BackpackController), "instance");
        GameObject controllerObject = new GameObject("M08 Backpack Controller");
        GameObject panel = new GameObject("08充电桩安装背包面板");
        GameObject oldSource = new GameObject("Old Source");
        GameObject currentSource = new GameObject("Current Source");
        GameObject oldReporter = new GameObject("08_OldReporter");
        GameObject currentReporter = new GameObject("08_CurrentReporter");

        try
        {
            TexCore_08BackpackController controller =
                controllerObject.AddComponent<TexCore_08BackpackController>();
            SetPrivateStaticField(typeof(TexCore_08BackpackController),
                "instance", controller);
            controller.SetPanel(panel);
            controller.SetEntries(new List<TexCore_08BackpackController.Entry>
            {
                new TexCore_08BackpackController.Entry
                {
                    key = "旧物品",
                    label = "旧物品",
                    source = oldSource
                },
                new TexCore_08BackpackController.Entry
                {
                    key = "当前物品",
                    label = "当前物品",
                    source = currentSource
                }
            });

            TexCore_08BackpackController.NotifyStepStarted(oldReporter, oldSource);
            TexCore_08BackpackController.NotifyStepStarted(currentReporter, currentSource);
            TexCore_08BackpackController.NotifyStepEnded(oldReporter);

            Assert.That(GetPrivateField<GameObject>(controller, "activeReporter"),
                Is.SameAs(currentReporter));
            Assert.That(GetPrivateField<GameObject>(controller, "activeSource"),
                Is.SameAs(currentSource));
            Assert.That(GetPrivateField<string>(controller, "activeKey"),
                Is.EqualTo("当前物品"));
        }
        finally
        {
            SetPrivateStaticField(typeof(TexCore_08BackpackController),
                "instance", previousInstance);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(oldSource);
            Object.DestroyImmediate(currentSource);
            Object.DestroyImmediate(oldReporter);
            Object.DestroyImmediate(currentReporter);
        }
    }

    [Test]
    public void DuplicateUnderWay_DoesNotResetHeldSourcePose()
    {
        GameObject reporter = new GameObject("08_DuplicateUnderWay");
        GameObject source = new GameObject("Shared Drill");
        GameObject target = new GameObject("Drill Target");

        try
        {
            TexCore_OrderedPlacementStep step =
                reporter.AddComponent<TexCore_OrderedPlacementStep>();
            step.ConfigureSource(source);
            step.ConfigureAutoCompleteOnApproach(0.25f);
            step.Target = target.transform;
            step.UnderWay();

            Vector3 heldPose = new Vector3(0.7f, 1.2f, -0.4f);
            source.transform.position = heldPose;
            step.UnderWay();

            Assert.That(Vector3.Distance(source.transform.position, heldPose),
                Is.LessThan(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(reporter);
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void DuplicateFinish_DoesNotResnapSharedSource()
    {
        GameObject reporter = new GameObject("08_DuplicateFinish");
        GameObject source = new GameObject("Shared Drill");
        GameObject target = new GameObject("Completed Drill Target");

        try
        {
            target.transform.position = Vector3.one;
            TexCore_OrderedPlacementStep step =
                reporter.AddComponent<TexCore_OrderedPlacementStep>();
            step.ConfigureSource(source);
            step.ConfigureAutoCompleteOnApproach(0.25f);
            step.Target = target.transform;
            step.Finish();

            Vector3 laterStepPose = new Vector3(3f, 2f, 1f);
            source.transform.position = laterStepPose;
            step.Finish();

            Assert.That(Vector3.Distance(source.transform.position, laterStepPose),
                Is.LessThan(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(reporter);
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void CompletedDrill_RemainsGrabbableForFollowingHole()
    {
        GameObject reporter = new GameObject("08_FirstDrillReporter");
        GameObject drill = new GameObject("Shared Drill");
        GameObject target = new GameObject("First Drill Target");

        try
        {
            ObjectManipulator manipulator = drill.AddComponent<ObjectManipulator>();
            TexCore_OrderedPlacementStep step =
                reporter.AddComponent<TexCore_OrderedPlacementStep>();
            step.ConfigureSource(drill);
            step.ConfigureAutoCompleteOnApproach(0.25f);
            step.Target = target.transform;
            step.UnderWay();

            step.Finish();

            Assert.That(manipulator.enabled, Is.True,
                "首个钻孔完成后，共享电钻必须保持可抓取，供下一孔继续使用。");
        }
        finally
        {
            Object.DestroyImmediate(reporter);
            Object.DestroyImmediate(drill);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void PreviousDrillCleanup_DoesNotDisableActiveSharedDrill()
    {
        GameObject previousReporter = new GameObject("08_PreviousDrillReporter");
        GameObject currentReporter = new GameObject("08_CurrentDrillReporter");
        GameObject drill = new GameObject("Shared Drill");

        try
        {
            ObjectManipulator manipulator = drill.AddComponent<ObjectManipulator>();
            TexCore_OrderedPlacementStep previous =
                previousReporter.AddComponent<TexCore_OrderedPlacementStep>();
            TexCore_OrderedPlacementStep current =
                currentReporter.AddComponent<TexCore_OrderedPlacementStep>();
            previous.ConfigureSource(drill);
            current.ConfigureSource(drill);
            previous.ConfigureAutoCompleteOnApproach(0.25f);
            current.ConfigureAutoCompleteOnApproach(0.25f);

            current.UnderWay();
            previous.UnFinished();

            Assert.That(manipulator.enabled, Is.True,
                "上一颗螺丝清理时不能禁用当前步骤仍在使用的共享电钻。");
        }
        finally
        {
            Object.DestroyImmediate(previousReporter);
            Object.DestroyImmediate(currentReporter);
            Object.DestroyImmediate(drill);
        }
    }

    [Test]
    public void InitialUnpackObjects_RemainVisibleForAllM08Steps()
    {
        GameObject lidReporter = new GameObject("08_BoxLidReporter");
        GameObject stationReporter = new GameObject("08_StationReporter");
        GameObject boxLid = new GameObject("Box Lid");
        GameObject station = new GameObject("Charging Station In Box");

        try
        {
            TexCore_StepObjectState lidState =
                lidReporter.AddComponent<TexCore_StepObjectState>();
            TexCore_StepObjectState stationState =
                stationReporter.AddComponent<TexCore_StepObjectState>();
            MethodInfo configure = typeof(TexCore_0809SceneBuilder).GetMethod(
                "ConfigureInitiallyVisibleClickState",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(configure, Is.Not.Null);

            boxLid.SetActive(false);
            station.SetActive(false);
            configure.Invoke(null, new object[] { lidState, boxLid });
            configure.Invoke(null, new object[] { stationState, station });

            lidState.UnFinished();
            stationState.UnFinished();
            Assert.That(boxLid.activeSelf, Is.True,
                "介绍视频播放时，箱盖必须保持可见。");
            Assert.That(station.activeSelf, Is.True,
                "介绍视频播放时，充电桩必须保持在箱内可见。");

            lidState.Finish();
            stationState.Finish();
            Assert.That(boxLid.activeSelf, Is.True,
                "开箱题中尚未轮到箱盖时，TexCore 的前置 Finish 状态不能隐藏箱盖。");
            Assert.That(station.activeSelf, Is.True,
                "开箱题中尚未轮到充电桩时，TexCore 的前置 Finish 状态不能隐藏充电桩。");

            lidState.OnTrigger();
            stationState.OnTrigger();
            Assert.That(boxLid.activeSelf, Is.True,
                "玩家移开箱盖后，箱盖仍必须保持显示。");
            Assert.That(station.activeSelf, Is.True,
                "玩家取出充电桩后，箱内充电桩仍必须保持显示。");

            lidState.UnFinished();
            stationState.UnFinished();
            Assert.That(boxLid.activeSelf, Is.True,
                "后续任何状态更新都不能隐藏箱盖。");
            Assert.That(station.activeSelf, Is.True,
                "后续任何状态更新都不能隐藏箱内充电桩。");
        }
        finally
        {
            Object.DestroyImmediate(lidReporter);
            Object.DestroyImmediate(stationReporter);
            Object.DestroyImmediate(boxLid);
            Object.DestroyImmediate(station);
        }
    }

    [Test]
    public void SameObjectDrillScrew_RemainsVisibleUntilCompletion()
    {
        GameObject container = new GameObject("08 Drill Container");
        GameObject drill = new GameObject("08 Shared Drill");
        GameObject screw = new GameObject("08 Same Object Screw");

        try
        {
            MethodInfo create = typeof(TexCore_0809SceneBuilder).GetMethod(
                "CreateDrillScrewRemoval",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(create, Is.Not.Null);

            TexCore_InteracterBase step = (TexCore_InteracterBase)create.Invoke(
                null,
                new object[]
                {
                    container.transform, "08_SameObjectDrillScrew", drill, screw, screw, 0.25f
                });
            TexCore_StepObjectState state =
                step.GetComponent<TexCore_StepObjectState>();

            screw.SetActive(false);
            state.UnFinished();
            Assert.That(screw.activeSelf, Is.True,
                "08 同对象螺丝在未完成状态必须保持可见，不能被吸附点清理逻辑隐藏。");

            state.Finish();
            Assert.That(screw.activeSelf, Is.False,
                "08 同对象螺丝完成后才应隐藏。");
        }
        finally
        {
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(drill);
            Object.DestroyImmediate(screw);
        }
    }

    [Test]
    public void PreviousStationClick_DoesNotDisableCurrentPlacement()
    {
        GameObject previousReporter = new GameObject("08_StationTakeReporter");
        GameObject currentReporter = new GameObject("08_StationInstallReporter");
        GameObject station = new GameObject("Charging Station");
        GameObject target = new GameObject("Station Target");

        try
        {
            previousReporter.SetActive(false);
            ObjectManipulator manipulator = station.AddComponent<ObjectManipulator>();
            BoxCollider collider = station.AddComponent<BoxCollider>();
            TexCore_ClickObjectStep previous =
                previousReporter.AddComponent<TexCore_ClickObjectStep>();
            previous.Configure(false, false, true, true);
            previous.ConfigureSource(station);
            typeof(TexCore_ClickObjectStep).GetMethod("CacheSourceComponents",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(previous, null);
            previousReporter.SetActive(true);

            TexCore_OrderedPlacementStep current =
                currentReporter.AddComponent<TexCore_OrderedPlacementStep>();
            current.ConfigureSource(station);
            current.Target = target.transform;
            current.UnderWay();
            previous.UnFinished();

            Assert.That(manipulator.enabled, Is.True,
                "上一题的取桩步骤不能禁用当前安装步骤正在使用的充电桩抓取器。");
            Assert.That(collider.enabled, Is.True,
                "上一题的取桩步骤不能禁用当前安装步骤正在使用的充电桩碰撞体。");
        }
        finally
        {
            Object.DestroyImmediate(previousReporter);
            Object.DestroyImmediate(currentReporter);
            Object.DestroyImmediate(station);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void BoxLidCannotCompleteBeforeAllScrews()
    {
        GameObject lidReporter = new GameObject("08_BoxLidGateReporter");
        GameObject lid = new GameObject("08_BoxLid");
        GameObject screwReporter = new GameObject("08_ScrewReporter");

        try
        {
            ObjectManipulator manipulator = lid.AddComponent<ObjectManipulator>();
            Rigidbody body = lid.AddComponent<Rigidbody>();
            lid.AddComponent<BoxCollider>();
            GameObject lidChild = new GameObject("08_BoxLidChild");
            lidChild.transform.SetParent(lid.transform);
            ObjectManipulator childManipulator =
                lidChild.AddComponent<ObjectManipulator>();

            TexCore_ClickObjectStep lidStep =
                lidReporter.AddComponent<TexCore_ClickObjectStep>();
            lidStep.Configure(false, false, true, true);
            lidStep.ConfigureSource(lid);
            typeof(TexCore_ClickObjectStep).GetMethod("CacheSourceComponents",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(lidStep, null);

            TexCore_OrderedPlacementStep screwStep =
                screwReporter.AddComponent<TexCore_OrderedPlacementStep>();
            lidStep.ConfigurePrerequisites(
                new TexCore_InteracterBase[] { screwStep }, "请先完成 8 颗螺丝。");
            lidStep.UnderWay();

            Assert.That(manipulator.enabled, Is.False,
                "前 8 颗螺丝未完成时，箱盖必须保持不可抓取。");
            Assert.That(childManipulator.enabled, Is.False,
                "箱盖子物体上的抓取器也必须在前置未完成时保持禁用。");
            Assert.That(body.isKinematic, Is.True,
                "前置未完成时，箱盖刚体不能被碰撞或旧交互组件推动。");

            typeof(TexCore_ClickObjectStep).GetMethod("CompleteFromUser",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(lidStep, null);
            Assert.That(GetPrivateField<bool>(lidStep, "completed"), Is.False,
                "前置螺丝未完成时，箱盖步骤不能被完成。");
        }
        finally
        {
            Object.DestroyImmediate(lidReporter);
            Object.DestroyImmediate(lid);
            Object.DestroyImmediate(screwReporter);
        }
    }

    [Test]
    public void ScrewSequence_KeepsAllEightScrewsVisible()
    {
        GameObject sequenceObject = new GameObject("08_M08 Screw Sequence");
        List<GameObject> created = new List<GameObject> { sequenceObject };

        try
        {
            TexCore_0809M08ScrewSequence sequence =
                sequenceObject.AddComponent<TexCore_0809M08ScrewSequence>();
            List<TexCore_OrderedPlacementStep> steps = BuildScrewSteps(created, 8);
            SetScrewSteps(sequence, steps);

            // 已完成、当前、未完成三种状态同时存在。
            SetUserCompleted(steps[0], true);
            SetUserCompleted(steps[1], true);
            steps[2].UnFinished();
            steps[3].Finish();

            // 模拟动画曲线和步骤状态把 8 颗螺丝全部关掉。
            foreach (TexCore_OrderedPlacementStep step in steps)
            {
                step.Target.gameObject.SetActive(false);

                foreach (Renderer renderer in
                         step.Target.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = false;
                }
            }

            InvokePrivate(sequence, "ApplyScrewVisibility");

            for (int index = 0; index < steps.Count; index++)
            {
                GameObject screw = steps[index].Target.gameObject;
                Assert.That(screw.activeSelf, Is.True,
                    $"第 {index + 1} 颗螺丝在整个开箱步骤中都必须保持显示，包括已经拧下的螺丝。");

                foreach (Renderer renderer in
                         screw.GetComponentsInChildren<Renderer>(true))
                {
                    Assert.That(renderer.enabled, Is.True,
                        $"第 {index + 1} 颗螺丝的所有子级 Renderer 都必须保持启用。");
                }
            }
        }
        finally
        {
            DestroyAll(created);
        }
    }

    [Test]
    public void ScrewSequence_DisablesScrewAnimatorAutoPlayOnEntry()
    {
        GameObject sequenceObject = new GameObject("08_M08 Screw Sequence");
        List<GameObject> created = new List<GameObject> { sequenceObject };

        try
        {
            TexCore_0809M08ScrewSequence sequence =
                sequenceObject.AddComponent<TexCore_0809M08ScrewSequence>();
            List<TexCore_OrderedPlacementStep> steps =
                BuildScrewSteps(created, 8, true);
            SetScrewSteps(sequence, steps);

            foreach (TexCore_OrderedPlacementStep step in steps)
            {
                Assert.That(ScrewAnimator(step).enabled, Is.True,
                    "前置条件：场景里的螺丝动画默认是打开的，会在入场时自动播放。");
            }

            InvokePrivate(sequence, "SuppressScrewAnimatorAutoPlay");

            foreach (TexCore_OrderedPlacementStep step in steps)
            {
                Assert.That(ScrewAnimator(step).enabled, Is.False,
                    "进入关卡后 8 颗螺丝的动画都不能自动播放。");
            }

            InvokePrivate(sequence, "SuppressScrewAnimatorAutoPlay");

            foreach (TexCore_OrderedPlacementStep step in steps)
            {
                Assert.That(ScrewAnimator(step).enabled, Is.False,
                    "重复执行入场抑制不能把螺丝动画重新打开。");
            }
        }
        finally
        {
            DestroyAll(created);
        }
    }

    [Test]
    public void ScrewSequence_OnlyCurrentScrewAnimatorIsTriggeredOnce()
    {
        GameObject sequenceObject = new GameObject("08_M08 Screw Sequence");
        List<GameObject> created = new List<GameObject> { sequenceObject };

        try
        {
            TexCore_0809M08ScrewSequence sequence =
                sequenceObject.AddComponent<TexCore_0809M08ScrewSequence>();
            List<TexCore_OrderedPlacementStep> steps =
                BuildScrewSteps(created, 8, true);
            SetScrewSteps(sequence, steps);

            SetUserCompleted(steps[0], true);
            SetUserCompleted(steps[1], true);

            InvokePrivate(sequence, "ApplyScrewAnimators", 2);

            for (int index = 0; index < steps.Count; index++)
            {
                if (index == 2)
                {
                    Assert.That(ScrewAnimator(steps[index]).enabled, Is.True,
                        "当前第一颗未完成螺丝在步骤开始时显式触发一次拧下动画。");
                    continue;
                }

                Assert.That(ScrewAnimator(steps[index]).enabled, Is.False,
                    $"第 {index + 1} 颗螺丝的动画必须保持关闭，不能自动播放。");
            }

            // 同一个当前步骤只触发一次：手工关掉后再次应用也不会重播。
            ScrewAnimator(steps[2]).enabled = false;
            InvokePrivate(sequence, "ApplyScrewAnimators", 2);
            Assert.That(ScrewAnimator(steps[2]).enabled, Is.False,
                "同一个当前步骤不能重复触发动画。");

            // 轮到下一颗螺丝时，只有那一颗会被触发。
            SetUserCompleted(steps[2], true);
            InvokePrivate(sequence, "ApplyScrewAnimators", 3);
            Assert.That(ScrewAnimator(steps[3]).enabled, Is.True,
                "轮到第 4 颗螺丝时才显式触发它的动画。");
            Assert.That(ScrewAnimator(steps[2]).enabled, Is.False,
                "已经完成的螺丝动画必须关闭。");
        }
        finally
        {
            DestroyAll(created);
        }
    }

    [Test]
    public void ScrewSequence_RemovesVisibilityCurvesFromScrewAnimation()
    {
        AnimationClip clip = new AnimationClip();

        try
        {
            // 复刻场景里 8 段螺丝动画：旋转 + 结束时关闭 MeshRenderer。
            clip.SetCurve(string.Empty, typeof(Transform),
                "localEulerAnglesRaw.x",
                AnimationCurve.Linear(0f, 0f, 1.5f, 270f));
            clip.SetCurve(string.Empty, typeof(MeshRenderer), "m_Enabled",
                AnimationCurve.Linear(0f, 1f, 1f, 0f));
            clip.SetCurve(string.Empty, typeof(GameObject), "m_IsActive",
                AnimationCurve.Linear(0f, 1f, 1f, 0f));

            MethodInfo strip = typeof(TexCore_0809SceneBuilder).GetMethod(
                "StripVisibilityCurves",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(strip, Is.Not.Null);

            int removed = (int)strip.Invoke(null, new object[] { clip });

            Assert.That(removed, Is.EqualTo(2),
                "写 Renderer.enabled 和 GameObject.active 的曲线都必须移除。");

            EditorCurveBinding[] remaining =
                AnimationUtility.GetCurveBindings(clip);
            Assert.That(HasCurve(remaining, typeof(MeshRenderer), "m_Enabled"),
                Is.False,
                "动画不能再写 MeshRenderer.enabled，否则播完会把螺丝隐藏。");
            Assert.That(HasCurve(remaining, typeof(GameObject), "m_IsActive"),
                Is.False,
                "动画不能再写 GameObject.active。");
            Assert.That(HasCurve(remaining, typeof(Transform),
                    "localEulerAnglesRaw.x"),
                Is.True,
                "拧下螺丝的旋转曲线必须保留，交互动画反馈不能丢。");
        }
        finally
        {
            Object.DestroyImmediate(clip);
        }
    }

    private static bool HasCurve(EditorCurveBinding[] bindings,
        System.Type type, string propertyName)
    {
        return bindings.Any(binding =>
            binding.type == type && binding.propertyName == propertyName);
    }

    private static List<TexCore_OrderedPlacementStep> BuildScrewSteps(
        List<GameObject> created, int count, bool withAnimator = false)
    {
        List<TexCore_OrderedPlacementStep> steps =
            new List<TexCore_OrderedPlacementStep>(count);
        for (int index = 0; index < count; index++)
        {
            GameObject reporter = new GameObject("08_ScrewReporter" + (index + 1));
            GameObject screw = new GameObject("08_Screw" + (index + 1));
            screw.AddComponent<MeshRenderer>();
            GameObject part = new GameObject("08_Screw" + (index + 1) + "Part");
            part.transform.SetParent(screw.transform, false);
            part.AddComponent<MeshRenderer>();
            if (withAnimator)
            {
                screw.AddComponent<Animator>();
            }

            TexCore_OrderedPlacementStep step =
                reporter.AddComponent<TexCore_OrderedPlacementStep>();
            step.Target = screw.transform;
            steps.Add(step);
            created.Add(reporter);
            created.Add(screw);
        }

        return steps;
    }

    private static Animator ScrewAnimator(TexCore_OrderedPlacementStep step)
    {
        Animator animator = step.Target.GetComponent<Animator>();
        Assert.That(animator, Is.Not.Null, "测试螺丝必须带 Animator 组件。");
        return animator;
    }

    [Test]
    public void ScrewSequence_OnlyFirstIncompleteScrewIsInteractive()
    {
        GameObject sequenceObject = new GameObject("08_M08 Screw Sequence");
        List<GameObject> created = new List<GameObject> { sequenceObject };

        try
        {
            TexCore_0809M08ScrewSequence sequence =
                sequenceObject.AddComponent<TexCore_0809M08ScrewSequence>();
            List<TexCore_OrderedPlacementStep> steps = BuildScrewSteps(created, 8);
            SetScrewSteps(sequence, steps);

            SetUserCompleted(steps[0], true);
            SetUserCompleted(steps[1], true);

            InvokePrivate(sequence, "ApplyScrewInteractionStates", 2);

            Assert.That(steps[0].state, Is.EqualTo(FinishState.已完成));
            Assert.That(steps[1].state, Is.EqualTo(FinishState.已完成));
            Assert.That(steps[2].state, Is.EqualTo(FinishState.等待触发),
                "第 3 颗螺丝应成为当前可交互螺丝。");
            for (int index = 3; index < steps.Count; index++)
            {
                Assert.That(steps[index].state, Is.EqualTo(FinishState.未完成),
                    $"第 {index + 1} 颗螺丝未轮到，必须保持未完成，不能提前交互。");
            }
        }
        finally
        {
            DestroyAll(created);
        }
    }

    private static void SetScrewSteps(TexCore_0809M08ScrewSequence sequence,
        List<TexCore_OrderedPlacementStep> steps)
    {
        GetPrivateField<List<TexCore_OrderedPlacementStep>>(sequence, "screwSteps")
            .AddRange(steps);
    }
    private static void SetUserCompleted(TexCore_OrderedPlacementStep step,
        bool value)
    {
        SetPrivateField(step, "userCompleted", value);
    }

    private static void InvokePrivate(object target, string methodName,
        params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Missing private method: {methodName}");
        method.Invoke(target, arguments);
    }

    private static void DestroyAll(List<GameObject> objects)
    {
        foreach (GameObject value in objects)
        {
            if (value != null)
            {
                Object.DestroyImmediate(value);
            }
        }
    }

    private static void SetPrivateField(object target, string fieldName,
        object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing private field: {fieldName}");
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing private field: {fieldName}");
        return (T)field.GetValue(target);
    }

    private static T GetPrivateStaticField<T>(System.Type targetType,
        string fieldName)
    {
        FieldInfo field = targetType.GetField(fieldName,
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing private static field: {fieldName}");
        return (T)field.GetValue(null);
    }

    private static void SetPrivateStaticField(System.Type targetType,
        string fieldName, object value)
    {
        FieldInfo field = targetType.GetField(fieldName,
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing private static field: {fieldName}");
        field.SetValue(null, value);
    }
}
