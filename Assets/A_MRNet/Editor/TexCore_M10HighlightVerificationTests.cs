using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Guards the M10 rack-entry guidance: the topic/step wiring must stay
/// unchanged, and the runtime highlight must always keep a single focus.
/// </summary>
public sealed class TexCore_M10HighlightVerificationTests
{
    private const string BusinessRoot =
        "Assets/Resources/BusinessPrefabs/MR混合现实多人协作平台/01货到人拣选电商仓储实施安装与调试实训系统";
    private const string TopicRoot = BusinessRoot + "/10货架入场";
    private const string ScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";
    private const string VideoTopicName = "00介绍视频";
    private const string SelectTopicName = "01服务器设计主要目的";
    private const string OperateTopicName = "02机器人搬运至货架点";
    private const string CompletionTopicName = "03完成整个模块";
    private const string RackStepObjectName = "10_机器人搬运到指定货架点";
    private const string GateObjectName = "完成整个模块_10";
    private const int ExpectedTargetCount = 50;

    [TearDown]
    public void TearDown()
    {
        TexCore_FocusHighlight.Clear();
        TexCore_FocusHighlight highlight = TexCore_FocusHighlight.Instance;
        if (highlight != null)
        {
            Object.DestroyImmediate(highlight.gameObject);
        }
    }

    [Test]
    public void M10Topics_KeepNamesOperateBindingsAndScores()
    {
        GameObject video = LoadTopic(VideoTopicName);
        GameObject select = LoadTopic(SelectTopicName);
        GameObject operate = LoadTopic(OperateTopicName);
        GameObject completion = LoadTopic(CompletionTopicName);

        Assert.That(video, Is.Not.Null, "M10 must keep the 00介绍视频 topic prefab.");
        Assert.That(select, Is.Not.Null, "M10 must keep the 01服务器设计主要目的 topic prefab.");
        Assert.That(operate, Is.Not.Null, "M10 must keep the 02机器人搬运至货架点 topic prefab.");
        Assert.That(completion, Is.Not.Null, "M10 must keep the 03完成整个模块 topic prefab.");

        Panel_Operate operatePanel = operate.GetComponent<Panel_Operate>();
        Assert.That(operatePanel, Is.Not.Null,
            "02 still has to run through Panel_Operate so the scene step stays bound.");
        Assert.That(operatePanel.topicData.tasks,
            Is.EqualTo(new List<string> { RackStepObjectName }),
            "02 must still bind the 10_机器人搬运到指定货架点 scene step.");
        Assert.That(operatePanel.topicData.score, Is.EqualTo(10));

        Panel_Operate completionPanel = completion.GetComponent<Panel_Operate>();
        Assert.That(completionPanel, Is.Not.Null);
        Assert.That(completionPanel.topicData.tasks,
            Is.EqualTo(new List<string> { GateObjectName }),
            "03 must still bind the 完成整个模块_10 scene step.");
        Assert.That(completionPanel.topicData.score, Is.EqualTo(10));

        Panel_Select quiz = select.GetComponent<Panel_Select>();
        Assert.That(quiz, Is.Not.Null);
        Assert.That(quiz.topicData.score, Is.EqualTo(30),
            "M10 keeps the existing 30 + 10 + 10 score split.");
    }

    [Test]
    public void M10ProductionTopics_CarryTheFocusGuide()
    {
        TexCore_M10TopicGuidance videoGuide =
            LoadTopic(VideoTopicName).GetComponent<TexCore_M10TopicGuidance>();
        TexCore_M10TopicGuidance selectGuide =
            LoadTopic(SelectTopicName).GetComponent<TexCore_M10TopicGuidance>();

        Assert.That(videoGuide, Is.Not.Null, "The M10 intro video must carry the topic focus guide.");
        Assert.That(videoGuide.Mode, Is.EqualTo(TexCore_M10TopicGuidance.GuidanceMode.Video));

        Assert.That(selectGuide, Is.Not.Null, "The M10 knowledge topic must carry the topic focus guide.");
        Assert.That(selectGuide.Mode, Is.EqualTo(TexCore_M10TopicGuidance.GuidanceMode.Select));
    }

    [Test]
    public void Scene_KeepsFiftyTargetPointsAndTheM10GateWiring()
    {
        SceneStepSnapshot snapshot = ReadSceneStepSnapshot();

        Assert.That(snapshot.TargetPointCounts, Is.Not.Empty,
            "The scene must keep at least one rack workflow target point list.");
        Assert.That(snapshot.TargetPointCounts, Has.All.EqualTo(ExpectedTargetCount),
            "Every rack workflow step must keep its " + ExpectedTargetCount +
            " target points (10货架入场 and the temporary 11流程测试).");
        Assert.That(snapshot.FoundRackStepObject, Is.True,
            "The scene must keep the " + RackStepObjectName + " step object.");
        Assert.That(snapshot.FoundGateObject, Is.True,
            "The scene must keep the " + GateObjectName + " step object.");
        Assert.That(snapshot.GatesWithReturnTopic, Is.EqualTo(1),
            "Only the M10 completion gate may point its failure guidance back at a topic.");
        Assert.That(snapshot.M10GateReturnTopicFound, Is.True,
            "The M10 completion gate must return the player to " + OperateTopicName + ".");
    }

    [Test]
    public void FocusHighlight_KeepsOnlyOnePrimaryTarget()
    {
        GameObject first = new GameObject("FocusTargetA");
        GameObject second = new GameObject("FocusTargetB");
        GameObject secondary = new GameObject("FocusSecondary");
        try
        {
            first.AddComponent<BoxCollider>();
            second.AddComponent<BoxCollider>();
            secondary.AddComponent<BoxCollider>();

            TexCore_FocusHighlight.Show(first, "hint A");
            Assert.That(TexCore_FocusHighlight.Instance.PrimaryTarget, Is.EqualTo(first));

            TexCore_FocusHighlight.Show(second, "hint B", first, new[] { secondary });
            Assert.That(TexCore_FocusHighlight.Instance.PrimaryTarget, Is.EqualTo(second),
                "A new focus always replaces the previous primary target.");
            Assert.That(TexCore_FocusHighlight.Instance.ArrowSource, Is.EqualTo(first));
            Assert.That(TexCore_FocusHighlight.Instance.SecondaryCount, Is.EqualTo(1));

            TexCore_FocusHighlight.Hide();
            Assert.That(TexCore_FocusHighlight.Instance.PrimaryTarget, Is.Null);
            Assert.That(TexCore_FocusHighlight.Instance.IsActive, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(secondary);
        }
    }

    [Test]
    public void FocusHighlight_ClearForOnlyReleasesTheOwningComponent()
    {
        GameObject target = new GameObject("FocusTarget");
        GameObject owner = new GameObject("FocusOwner");
        GameObject otherOwner = new GameObject("OtherFocusOwner");
        try
        {
            target.AddComponent<BoxCollider>();

            TexCore_FocusHighlight.Show(target, "hint", null, null, owner);
            TexCore_FocusHighlight.ClearFor(otherOwner);
            Assert.That(TexCore_FocusHighlight.Instance.PrimaryTarget, Is.EqualTo(target),
                "A non-owner must not clear the active focus.");

            TexCore_FocusHighlight.ClearFor(owner);
            Assert.That(TexCore_FocusHighlight.Instance.PrimaryTarget, Is.Null,
                "The owning component releases the shared guide.");
            Assert.That(TexCore_FocusHighlight.Instance.IsActive, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(otherOwner);
        }
    }

    [Test]
    public void FocusHighlight_RejectsATargetUnderADisabledCanvas()
    {
        GameObject backdrop = CreateCanvas("Hidden Backdrop Canvas");
        GameObject itemButton = CreateUiButton(backdrop.transform, "08背包_充电桩");
        GameObject owner = new GameObject("BackpackOwner");
        try
        {
            backdrop.GetComponent<Canvas>().enabled = false;

            // 背包UI 隐藏时按钮依然 activeInHierarchy，所以光看这一项会误判可见。
            Assert.That(itemButton.activeInHierarchy, Is.True,
                "The scene keeps the shared UI roots active and hides them by Canvas.");

            TexCore_FocusHighlight.Show(itemButton, "请点击背包中的“充电桩”。", null, null, owner);

            Assert.That(TexCore_FocusHighlight.Instance.PrimaryTarget, Is.Null,
                "A button under a disabled Canvas is invisible and must not take the focus.");
            Assert.That(TexCore_FocusHighlight.Instance.IsActive, Is.False);
            Assert.That(TexCore_FocusHighlight.Instance.CurrentHint, Is.Null,
                "A hidden target must not broadcast an action hint either.");
        }
        finally
        {
            Object.DestroyImmediate(backdrop);
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void FocusHighlight_TakesTheItemButtonOnlyWhileItsCanvasIsVisible()
    {
        GameObject backdrop = CreateCanvas("Visible Backdrop Canvas");
        GameObject itemButton = CreateUiButton(backdrop.transform, "08背包_充电桩");
        try
        {
            TexCore_FocusHighlight.Show(itemButton, "请点击背包中的“充电桩”。");
            Assert.That(TexCore_FocusHighlight.Instance.PrimaryTarget, Is.EqualTo(itemButton),
                "A visible item button is a valid highlight target.");
            Assert.That(TexCore_FocusHighlight.Instance.IsActive, Is.True,
                "The visible item button has to draw its outline.");

            backdrop.GetComponent<Canvas>().enabled = false;
            TexCore_FocusHighlight.Instance.Revalidate();

            Assert.That(TexCore_FocusHighlight.Instance.PrimaryTarget, Is.Null,
                "The frame step has to drop the item button once its Canvas is disabled.");
            Assert.That(TexCore_FocusHighlight.Instance.IsActive, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(backdrop);
        }
    }

    [Test]
    public void FocusHighlight_DropsTheBackpackTargetWhenAnotherCanvasComesForward()
    {
        GameObject buttonsCanvas = CreateCanvas("Buttons Canvas");
        GameObject singleMenuCanvas = CreateCanvas("Single Menu Canvas");
        GameObject backpackCanvas = CreateCanvas("Backpack Canvas");
        GameObject backpackButton = CreateUiButton(backpackCanvas.transform, "08背包_充电桩");
        try
        {
            SetCanvasEnabled(buttonsCanvas, false);
            SetCanvasEnabled(singleMenuCanvas, false);
            SetCanvasEnabled(backpackCanvas, true);

            TexCore_FocusHighlight.Show(backpackButton, "请点击背包中的“充电桩”。");
            Assert.That(TexCore_FocusHighlight.Instance.PrimaryTarget, Is.EqualTo(backpackButton),
                "The open backpack owns the focus while the player is inside it.");

            // VisibleUi.Backpack -> VisibleUi.Buttons. The switch only disables the
            // Canvas, so the stale guide is dropped by the frame step, not by Show.
            SetCanvasEnabled(backpackCanvas, false);
            SetCanvasEnabled(buttonsCanvas, true);
            TexCore_FocusHighlight.Instance.Revalidate();
            Assert.That(TexCore_FocusHighlight.Instance.PrimaryTarget, Is.Null,
                "Switching to the Buttons Canvas has to drop the backpack focus at once. "
                + DescribeFocusState());
            Assert.That(LineRenderersAreHidden(), Is.True,
                "No stale outline or arrow may stay drawn after the Canvas switch: " + DescribeStaleLines());

            // VisibleUi.Buttons -> VisibleUi.SingleMenu
            SetCanvasEnabled(buttonsCanvas, false);
            SetCanvasEnabled(singleMenuCanvas, true);
            TexCore_FocusHighlight.Instance.Revalidate();
            Assert.That(TexCore_FocusHighlight.Instance.IsActive, Is.False,
                "The Single Menu Canvas must not inherit the leftover backpack guide.");

            // VisibleUi.SingleMenu -> VisibleUi.Backpack
            SetCanvasEnabled(singleMenuCanvas, false);
            SetCanvasEnabled(backpackCanvas, true);
            TexCore_FocusHighlight.Instance.Revalidate();
            Assert.That(TexCore_FocusHighlight.Instance.PrimaryTarget, Is.Null,
                "Reopening the backpack must not resurrect the stale target; the step has to re-show it.");
            Assert.That(LineRenderersAreHidden(), Is.True,
                "A Backpack -> Single Menu -> Backpack round trip leaves nothing behind.");
        }
        finally
        {
            Object.DestroyImmediate(buttonsCanvas);
            Object.DestroyImmediate(singleMenuCanvas);
            Object.DestroyImmediate(backpackCanvas);
        }
    }

    [Test]
    public void FocusHighlight_ClearsTheHiddenUiTargetOnTheNextLateUpdate()
    {
        GameObject backpackCanvas = CreateCanvas("Backpack Canvas");
        GameObject backpackButton = CreateUiButton(backpackCanvas.transform, "08背包_充电桩");
        GameObject rackBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject worldTarget = new GameObject("WorldTarget");
        try
        {
            // A world target needs real geometry: without bounds there is nothing
            // to outline, which is a different rule from the visibility one.
            rackBody.transform.SetParent(worldTarget.transform, false);

            TexCore_FocusHighlight.Show(backpackButton, "请点击背包中的“充电桩”。");
            Assert.That(TexCore_FocusHighlight.Instance.PrimaryTarget, Is.EqualTo(backpackButton));

            // A Canvas can also be disabled outside the highlight owner's control,
            // so the frame step has to re-check instead of trusting the Show-time
            // state. In play mode this runs from LateUpdate.
            backpackCanvas.GetComponent<Canvas>().enabled = false;
            TexCore_BackpackTick();
            Assert.That(TexCore_FocusHighlight.Instance.PrimaryTarget, Is.Null,
                "LateUpdate must release a target that went invisible after Show.");
            Assert.That(LineRenderersAreHidden(), Is.True);

            TexCore_FocusHighlight.Show(worldTarget, "请把充电桩放到机柜里。");
            Assert.That(TexCore_FocusHighlight.Instance.PrimaryTarget, Is.EqualTo(worldTarget),
                "A world target must be accepted while the backpack Canvas is hidden. "
                + DescribeFocusState());
            TexCore_BackpackTick();
            Assert.That(TexCore_FocusHighlight.Instance.PrimaryTarget, Is.EqualTo(worldTarget),
                "The world-object highlight is unaffected by the UI visibility rule. "
                + DescribeFocusState());
            Assert.That(TexCore_FocusHighlight.Instance.IsActive, Is.True,
                "A world object still draws its guide while the backpack is hidden: "
                + DescribeFocusState() + " bounds=" + DescribeBounds(worldTarget));
        }
        finally
        {
            Object.DestroyImmediate(backpackCanvas);
            Object.DestroyImmediate(worldTarget);
        }
    }

    [Test]
    public void FocusHighlight_IgnoresATargetWhoseCanvasGroupIsFadedOut()
    {
        GameObject canvasRoot = CreateCanvas("Faded Canvas");
        GameObject fadedRoot = CreateUiButton(canvasRoot.transform, "Faded Root");
        GameObject focusedButton = CreateUiButton(fadedRoot.transform, "05背包_划线");
        try
        {
            CanvasGroup group = fadedRoot.AddComponent<CanvasGroup>();
            Assert.That(TexCore_FocusHighlight.IsUiVisible(focusedButton), Is.True);

            group.alpha = 0f;
            Assert.That(TexCore_FocusHighlight.IsUiVisible(focusedButton), Is.False,
                "A fully transparent group is not a visible highlight target.");

            group.alpha = 1f;
            group.enabled = false;
            Assert.That(TexCore_FocusHighlight.IsUiVisible(focusedButton), Is.False,
                "A disabled CanvasGroup hides its subtree.");
        }
        finally
        {
            Object.DestroyImmediate(canvasRoot);
        }
    }

    [Test]
    public void RackEntryGuidance_FollowsReleaseAndAttach()
    {
        GameObject robot = new GameObject("AGV1");
        GameObject rack = new GameObject(RackStepObjectName);
        GameObject target = new GameObject("huojia1");
        GameObject spawnMarker = new GameObject("10_货架生成点");
        try
        {
            robot.AddComponent<BoxCollider>();
            rack.AddComponent<BoxCollider>();
            target.AddComponent<BoxCollider>();
            TexCore_RackEntryWorkflowStep step = rack.AddComponent<TexCore_RackEntryWorkflowStep>();
            step.Configure(robot, new[] { target }, spawnMarker.transform, 0.24f);

            Assert.That(step.CompletedCount, Is.EqualTo(0));
            Assert.That(step.RequiredCount, Is.EqualTo(1));

            // 02 is displayed by the framework, so the step is already active.
            SetPrivateField(step, "stepActive", true);

            step.SetGuidanceState(RackEntryGuidanceState.SelectRack);
            Assert.That(step.GuidanceState, Is.EqualTo(RackEntryGuidanceState.SelectRack));

            // ReleaseFromBackpack is the exact call the backpack item button makes.
            Assert.That(step.ReleaseFromBackpack(), Is.True);
            Assert.That(step.GuidanceState, Is.EqualTo(RackEntryGuidanceState.MoveRackToEntryQr),
                "Taking the rack out of the backpack moves the guide to the entry QR step.");

            InvokePrivate(step, "AttachToRobot", robot.transform);
            Assert.That(step.IsAttachedToRobot, Is.True);
            Assert.That(step.GuidanceState, Is.EqualTo(RackEntryGuidanceState.MoveToTarget),
                "Once the rack is docked the guide moves to the current target point.");
        }
        finally
        {
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(rack);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(spawnMarker);
        }
    }

    [Test]
    public void RackEntryGuidance_KeepsTheReusedRackVisible()
    {
        GameObject robot = new GameObject("AGV1");
        GameObject rack = new GameObject(RackStepObjectName);
        GameObject spawnMarker = new GameObject("10_货架生成点");
        try
        {
            rack.AddComponent<BoxCollider>();
            // A previously saved zero scale must never become the captured pose:
            // ResetRackToSpawn writes startScale back, so it would hide the rack
            // the player just took out of the backpack.
            rack.transform.localScale = Vector3.zero;
            TexCore_RackEntryWorkflowStep step = rack.AddComponent<TexCore_RackEntryWorkflowStep>();
            step.Configure(robot, new GameObject[0], spawnMarker.transform, 0.24f);
            SetPrivateField(step, "stepActive", true);

            Assert.That(step.ReleaseFromBackpack(), Is.True);
            Assert.That(rack.transform.localScale, Is.EqualTo(Vector3.one),
                "Taking the rack out of the backpack must leave it at a visible scale.");
        }
        finally
        {
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(rack);
            Object.DestroyImmediate(spawnMarker);
        }
    }

    [Test]
    public void RackEntryGuidance_LocksTheRackRotationForHandCarry()
    {
        GameObject robot = new GameObject("AGV1");
        GameObject rack = new GameObject(RackStepObjectName);
        GameObject spawnMarker = new GameObject("10_货架生成点");
        try
        {
            robot.AddComponent<BoxCollider>();
            BoxCollider rackTrigger = rack.AddComponent<BoxCollider>();
            rackTrigger.isTrigger = true;
            rack.AddComponent<Rigidbody>();
            MixedReality.Toolkit.SpatialManipulation.ObjectManipulator manipulator =
                rack.AddComponent<MixedReality.Toolkit.SpatialManipulation.ObjectManipulator>();
            rack.AddComponent<MixedReality.Toolkit.SpatialManipulation.ConstraintManager>();

            TexCore_RackEntryWorkflowStep step = rack.AddComponent<TexCore_RackEntryWorkflowStep>();
            step.Configure(robot, new GameObject[0], spawnMarker.transform, 0.24f);

            Assert.That(manipulator.AllowedManipulations,
                Is.EqualTo(MixedReality.Toolkit.TransformFlags.Move),
                "The rack may only translate while it is carried by hand.");
            Assert.That(manipulator.ApplyTorque, Is.False,
                "Torque control would let the carried rack swing about.");

            MixedReality.Toolkit.SpatialManipulation.RotationAxisConstraint rotation =
                rack.GetComponent<MixedReality.Toolkit.SpatialManipulation.RotationAxisConstraint>();
            Assert.That(rotation, Is.Not.Null,
                "The constraint manager has to lock rotation; the manipulation flag alone is not enough.");
            Assert.That(rotation.enabled, Is.True);
            Assert.That(rotation.ConstraintOnRotation,
                Is.EqualTo(MixedReality.Toolkit.AxisFlags.XAxis |
                           MixedReality.Toolkit.AxisFlags.YAxis |
                           MixedReality.Toolkit.AxisFlags.ZAxis),
                "All three axes have to be locked, or the rack can still be tipped over.");
        }
        finally
        {
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(rack);
            Object.DestroyImmediate(spawnMarker);
        }
    }

    [Test]
    public void RackEntryGuidance_KeepsTheCarriedRackOnTheGround()
    {
        GameObject robot = new GameObject("AGV1");
        GameObject rack = new GameObject(RackStepObjectName);
        GameObject spawnMarker = new GameObject("10_货架生成点");
        try
        {
            robot.AddComponent<BoxCollider>();
            BoxCollider rackTrigger = rack.AddComponent<BoxCollider>();
            rackTrigger.isTrigger = true;
            Rigidbody rackBody = rack.AddComponent<Rigidbody>();
            rack.AddComponent<MixedReality.Toolkit.SpatialManipulation.ObjectManipulator>();
            rack.AddComponent<MixedReality.Toolkit.SpatialManipulation.ConstraintManager>();

            spawnMarker.transform.position = new Vector3(0f, 1.15f, 1.35f);
            TexCore_RackEntryWorkflowStep step = rack.AddComponent<TexCore_RackEntryWorkflowStep>();
            step.Configure(robot, new GameObject[0], spawnMarker.transform, 0.24f);
            SetPrivateField(step, "stepActive", true);
            Assert.That(step.ReleaseFromBackpack(), Is.True);

            Assert.That(rackBody.constraints & RigidbodyConstraints.FreezePositionY,
                Is.EqualTo(RigidbodyConstraints.FreezePositionY),
                "A shelf stands on the floor, so the hand-carry may not lift it.");

            // The player drags the rack upward: the carry has to stay on the floor.
            rack.transform.position = spawnMarker.transform.position + Vector3.up * 1.5f;
            InvokePrivate(step, "LateUpdate");
            Assert.That(rack.transform.position.y,
                Is.EqualTo(spawnMarker.transform.position.y).Within(0.001f),
                "Carrying the rack is a slide across the floor, never a lift.");

            step.UnFinished();
            Assert.That(rackBody.constraints & RigidbodyConstraints.FreezePositionY,
                Is.EqualTo(RigidbodyConstraints.None),
                "The robot still has to be able to pick the rack up after the round.");
        }
        finally
        {
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(rack);
            Object.DestroyImmediate(spawnMarker);
        }
    }

    [Test]
    public void RackEntryGuidance_DrivesTheRobotAlongRightAnglesOnly()
    {
        // A destination that is offset on both ground axes, so a shortest-path
        // drive would have to cut the corner between the racks.
        Vector3 position = new Vector3(0f, 1.15f, 1.35f);
        Vector3 destination = new Vector3(4f, 1.15f, -3f);
        const float Step = 0.05f;

        bool movedAlongX = false;
        bool movedAlongZ = false;
        for (int i = 0; i < 2000; i++)
        {
            Vector3 next = TexCore_RackEntryWorkflowStep.GetAxisAlignedStep(
                position, destination, Step);
            bool stepX = Mathf.Abs(next.x - position.x) > 0.00001f;
            bool stepZ = Mathf.Abs(next.z - position.z) > 0.00001f;
            Assert.That(stepX && stepZ, Is.False,
                "The robot may only travel along one ground axis at a time, never diagonally.");
            Assert.That(next.y, Is.EqualTo(position.y),
                "The robot has to stay on its own floor height while it drives.");
            Assert.That(Vector3.Distance(next, position), Is.LessThanOrEqualTo(Step + 0.00001f),
                "The robot may not teleport further than one drive step.");

            movedAlongX |= stepX;
            movedAlongZ |= stepZ;
            position = next;

            if (new Vector2(position.x - destination.x, position.z - destination.z).magnitude
                < 0.0001f)
            {
                break;
            }
        }

        Assert.That(movedAlongX, Is.True, "The robot never closed the horizontal gap.");
        Assert.That(movedAlongZ, Is.True, "The robot never closed the depth gap.");
        Assert.That(new Vector2(position.x - destination.x, position.z - destination.z).magnitude,
            Is.LessThan(0.0001f),
            "An L-shaped drive still has to arrive at the highlighted target point.");
    }

    [Test]
    public void RackEntryGuidance_RoutesTheRobotAroundDeliveredRacks()
    {
        GameObject robot = new GameObject("AGV1");
        GameObject rack = new GameObject(RackStepObjectName);
        GameObject entryQr = new GameObject("10_货架入场二维码");
        GameObject blocked = new GameObject("huojia1");
        GameObject freeLane = new GameObject("huojia2");
        GameObject destination = new GameObject("huojia3");
        GameObject placed = new GameObject("10_已入场货架_huojia1");
        try
        {
            robot.AddComponent<BoxCollider>();
            rack.AddComponent<BoxCollider>();

            // A two-column rack grid with the entry corridor in front of it.
            entryQr.transform.position = new Vector3(2f, 0f, 1f);
            blocked.transform.position = new Vector3(3f, 0f, -1f);
            freeLane.transform.position = new Vector3(2f, 0f, -3f);
            destination.transform.position = new Vector3(3f, 0f, -3f);

            TexCore_RackEntryWorkflowStep step = rack.AddComponent<TexCore_RackEntryWorkflowStep>();
            step.Configure(robot,
                new[] { blocked, freeLane, destination }, entryQr.transform, 0.24f);
            step.SetEntryQr(entryQr.transform, null);

            Vector3 from = entryQr.transform.position;
            Vector3 to = destination.transform.position;
            List<Vector3> direct = step.BuildRoute(from, to);
            Assert.That(PassesThrough(direct, blocked.transform.position), Is.True,
                "While the cell is still free the direct lane runs through it.");

            // A rack is already standing on the cell that lane would use.
            placed.transform.position = blocked.transform.position;
            PlacedRacks(step).Add(placed);
            List<Vector3> detour = step.BuildRoute(from, to);

            Assert.That(PassesThrough(detour, blocked.transform.position), Is.False,
                "The robot may not drive through a cell that already holds a rack.");
            Assert.That(PassesThrough(detour, freeLane.transform.position), Is.True,
                "The robot has to use the free neighbouring lane instead.");
            Assert.That(detour[detour.Count - 1], Is.EqualTo(to),
                "The detour still has to arrive at the highlighted rack point.");

            for (int i = 1; i < detour.Count; i++)
            {
                Vector3 previous = detour[i - 1];
                Vector3 current = detour[i];
                Assert.That(Mathf.Abs(current.x - previous.x) < 0.0001f
                            || Mathf.Abs(current.z - previous.z) < 0.0001f, Is.True,
                    "Every leg of the route has to stay on one lane.");
                Assert.That(current.y, Is.EqualTo(previous.y),
                    "The route may not leave the robot's own floor height.");
            }
        }
        finally
        {
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(rack);
            Object.DestroyImmediate(entryQr);
            Object.DestroyImmediate(blocked);
            Object.DestroyImmediate(freeLane);
            Object.DestroyImmediate(destination);
            Object.DestroyImmediate(placed);
        }
    }

    [Test]
    public void RackEntryGuidance_ShowsNoHighlightWhileTheRobotDrivesItself()
    {
        GameObject robot = new GameObject("AGV1");
        GameObject rack = new GameObject(RackStepObjectName);
        GameObject target = new GameObject("huojia1");
        GameObject entryQr = new GameObject("10_货架入场二维码");
        try
        {
            robot.AddComponent<BoxCollider>();
            rack.AddComponent<BoxCollider>();
            target.AddComponent<BoxCollider>();

            TexCore_RackEntryWorkflowStep step = rack.AddComponent<TexCore_RackEntryWorkflowStep>();
            step.Configure(robot, new[] { target }, entryQr.transform, 0.24f);
            step.SetEntryQr(entryQr.transform, null);
            SetPrivateField(step, "stepActive", true);

            // Carrying the rack onto the entry code is the player's job.
            step.SetGuidanceState(RackEntryGuidanceState.MoveRackToEntryQr);
            Assert.That(TexCore_FocusHighlight.Instance.IsActive, Is.True,
                "Placing the rack on the entry code still needs its highlight.");

            // From there on the robot works on its own, so nothing is highlighted.
            step.SetGuidanceState(RackEntryGuidanceState.AttachRack);
            Assert.That(TexCore_FocusHighlight.Instance.IsActive, Is.False,
                "The robot drives under the rack by itself, so no highlight is needed.");

            step.SetGuidanceState(RackEntryGuidanceState.MoveToTarget);
            Assert.That(TexCore_FocusHighlight.Instance.IsActive, Is.False,
                "The robot drives to the rack point by itself, so no highlight is needed.");
        }
        finally
        {
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(rack);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(entryQr);
        }
    }

    [Test]
    public void RackEntryGuidance_GivesTheRackSomethingSolidToGrab()
    {
        GameObject robot = new GameObject("AGV1");
        GameObject rack = new GameObject(RackStepObjectName);
        GameObject spawnMarker = new GameObject("10_货架生成点");
        try
        {
            robot.AddComponent<BoxCollider>();

            // The rack's own box is a trigger, because that is what drives docking,
            // so the interactor had nothing solid to hit and MRTK registered no
            // collider at all. That is why the rack could not be moved by hand.
            BoxCollider rackTrigger = rack.AddComponent<BoxCollider>();
            rackTrigger.isTrigger = true;
            rackTrigger.size = new Vector3(0.5f, 2.2f, 0.5f);
            rack.AddComponent<Rigidbody>();
            MixedReality.Toolkit.SpatialManipulation.ObjectManipulator manipulator =
                rack.AddComponent<MixedReality.Toolkit.SpatialManipulation.ObjectManipulator>();

            TexCore_RackEntryWorkflowStep step = rack.AddComponent<TexCore_RackEntryWorkflowStep>();
            step.Configure(robot, new GameObject[0], spawnMarker.transform, 0.24f);

            Assert.That(manipulator.colliders.Count, Is.GreaterThan(0),
                "MRTK can only select an interactable through its registered colliders.");

            bool hasSolid = false;
            foreach (Collider value in manipulator.colliders)
            {
                if (value != null && !value.isTrigger)
                {
                    hasSolid = true;
                    break;
                }
            }

            Assert.That(hasSolid, Is.True,
                "The registered collider has to be solid; a trigger is not hittable.");
            Assert.That(rack.transform.Find("GrabCollider"), Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(rack);
            Object.DestroyImmediate(spawnMarker);
        }
    }

    [Test]
    public void RackEntryGuidance_GatesDockingOnTheEntryQrCode()
    {
        GameObject robot = new GameObject("AGV1");
        GameObject rack = new GameObject(RackStepObjectName);
        GameObject spawnMarker = new GameObject("10_货架生成点");
        GameObject qrRoot = new GameObject("10_货架入场二维码");
        GameObject qrAreaObject = new GameObject("Area");
        try
        {
            // IsRobot matches the jiqiren tag, so the probe robot needs it or the
            // gate under test would never even be reached.
            robot.tag = "jiqiren";
            robot.AddComponent<BoxCollider>();
            rack.AddComponent<BoxCollider>();

            BoxCollider area = qrAreaObject.AddComponent<BoxCollider>();
            area.isTrigger = true;
            area.size = new Vector3(0.8f, 0.2f, 0.8f);
            qrAreaObject.transform.SetParent(qrRoot.transform, false);

            TexCore_RackEntryWorkflowStep step = rack.AddComponent<TexCore_RackEntryWorkflowStep>();
            step.Configure(robot, new GameObject[0], spawnMarker.transform, 0.24f);
            step.SetEntryQr(qrRoot.transform, area);
            SetPrivateField(step, "stepActive", true);

            MethodInfo tick = typeof(TexCore_RackEntryWorkflowStep).GetMethod("Update",
                BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo dock = typeof(TexCore_RackEntryWorkflowStep).GetMethod("TryDockToRobot",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(tick, Is.Not.Null);
            Assert.That(dock, Is.Not.Null);

            step.SetGuidanceState(RackEntryGuidanceState.MoveRackToEntryQr);

            // 1) robot parked under a rack that is nowhere near the entry code
            rack.transform.SetPositionAndRotation(new Vector3(3f, 0f, 3f), Quaternion.identity);
            robot.transform.position = new Vector3(3f, 0f, 3f);
            tick.Invoke(step, null);
            dock.Invoke(step, new object[] { robot.transform });
            Assert.That(step.IsAttachedToRobot, Is.False,
                "The AGV must not dock while the rack is off the entry QR code.");
            Assert.That(step.GuidanceState, Is.EqualTo(RackEntryGuidanceState.MoveRackToEntryQr));

            // 2) 45 degrees off the code orientation
            rack.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 45f, 0f));
            tick.Invoke(step, null);
            Assert.That(step.IsRackOnEntryQrNow, Is.False, "A 45 degrees rack does not count as placed.");

            // 3) close enough for the distance, but outside the area collider
            area.size = new Vector3(0.2f, 0.2f, 0.2f);
            rack.transform.SetPositionAndRotation(new Vector3(0.18f, 0f, 0f), Quaternion.identity);
            tick.Invoke(step, null);
            Assert.That(step.IsRackOnEntryQrNow, Is.False,
                "Outside the entry area collider the rack does not count.");
            area.size = new Vector3(0.8f, 0.2f, 0.8f);

            // 4) centred and aligned: the placement passes and the state moves on
            rack.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            tick.Invoke(step, null);
            Assert.That(step.IsRackOnEntryQrNow, Is.True);
            Assert.That(step.GuidanceState, Is.EqualTo(RackEntryGuidanceState.AttachRack));

            // 5) only now may the AGV dock, and only once it is under the rack
            robot.transform.position = Vector3.zero;
            dock.Invoke(step, new object[] { robot.transform });
            Assert.That(step.IsAttachedToRobot, Is.True,
                "The AGV docks once the rack sits on the entry QR code.");
        }
        finally
        {
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(rack);
            Object.DestroyImmediate(spawnMarker);
            Object.DestroyImmediate(qrRoot);
        }
    }

    [Test]
    public void RackEntryGuidance_LeavesARackStandingOnEveryCompletedTarget()
    {
        GameObject robot = new GameObject("AGV1");
        GameObject rack = new GameObject(RackStepObjectName);
        GameObject firstTarget = new GameObject("huojia1");
        GameObject secondTarget = new GameObject("huojia2");
        GameObject spawnMarker = new GameObject("10_货架生成点");
        GameObject placed = null;
        try
        {
            robot.transform.position = Vector3.zero;
            robot.AddComponent<BoxCollider>();
            rack.AddComponent<BoxCollider>();
            rack.AddComponent<Rigidbody>();
            TexCore_RackEntryWorkflowStep step = rack.AddComponent<TexCore_RackEntryWorkflowStep>();
            step.Configure(robot, new[] { firstTarget, secondTarget }, spawnMarker.transform, 0.24f);
            SetPrivateField(step, "stepActive", true);
            SetPrivateField(step, "attachedToRobot", true);

            firstTarget.transform.position = new Vector3(1f, 1.15f, 1f);
            rack.transform.position = new Vector3(1f, 1.584f, 1f);

            InvokePrivate(step, "CompleteCurrentTarget", firstTarget);

            placed = GameObject.Find("10_已入场货架_huojia1");
            Assert.That(placed, Is.Not.Null,
                "A completed target point has to keep a rack standing on it (所有货架入场).");
            Assert.That(placed.activeSelf, Is.True);
            Assert.That(Vector3.Distance(placed.transform.position, firstTarget.transform.position), Is.LessThan(0.001f),
                "The placed rack belongs on the target point it filled.");
            Assert.That(placed.GetComponent<TexCore_RackEntryWorkflowStep>(), Is.Null,
                "The placed rack must not carry a second live workflow step.");
            Assert.That(placed.GetComponent<Rigidbody>(), Is.Null,
                "The placed rack must not keep physics.");
            Collider[] colliders = placed.GetComponentsInChildren<Collider>(true);
            Assert.That(colliders.Length == 0 || System.Array.TrueForAll(colliders, value => !value.enabled), Is.True,
                "The placed rack must not keep live colliders, or it would trigger the next cycle.");
        }
        finally
        {
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(rack);
            Object.DestroyImmediate(firstTarget);
            Object.DestroyImmediate(secondTarget);
            Object.DestroyImmediate(spawnMarker);
            if (placed != null)
            {
                Object.DestroyImmediate(placed);
            }
        }
    }

    [Test]
    public void RackEntryGuidance_CompletesATargetBelowTheDockedRack()
    {
        GameObject robot = new GameObject("AGV1");
        GameObject rack = new GameObject(RackStepObjectName);
        GameObject firstTarget = new GameObject("huojia1");
        GameObject secondTarget = new GameObject("huojia2");
        GameObject spawnMarker = new GameObject("10_货架生成点");
        try
        {
            robot.transform.position = Vector3.zero;
            robot.AddComponent<BoxCollider>();
            rack.AddComponent<BoxCollider>();
            TexCore_RackEntryWorkflowStep step = rack.AddComponent<TexCore_RackEntryWorkflowStep>();
            step.Configure(robot, new[] { firstTarget, secondTarget }, spawnMarker.transform, 0.24f);
            SetPrivateField(step, "stepActive", true);
            SetPrivateField(step, "attachedToRobot", true);

            // A docked rack stays on the floor at its carry height, so it can share
            // a Y with a target point. The tolerance is still judged on the ground
            // plane, which is what this test pins down.
            firstTarget.transform.position = new Vector3(1f, 1.15f, 1f);
            rack.transform.position = new Vector3(1f, 1.584f, 1f);

            InvokePrivate(step, "CompleteCurrentTarget", firstTarget);
            Assert.That(step.CompletedCount, Is.EqualTo(1),
                "A target directly below the docked rack still has to complete.");
        }
        finally
        {
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(rack);
            Object.DestroyImmediate(firstTarget);
            Object.DestroyImmediate(secondTarget);
            Object.DestroyImmediate(spawnMarker);
        }
    }

    [Test]
    public void RackEntryGuidance_DocksUnderTheRackWithoutLiftingIt()
    {
        GameObject robot = new GameObject("AGV1");
        GameObject rack = new GameObject(RackStepObjectName);
        GameObject spawnMarker = new GameObject("10_货架生成点");
        GameObject rackBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            robot.transform.position = Vector3.zero;
            robot.AddComponent<BoxCollider>();
            rack.AddComponent<BoxCollider>();
            rackBody.transform.SetParent(rack.transform, false);
            rackBody.transform.localScale = new Vector3(1f, 2f, 1f);

            TexCore_RackEntryWorkflowStep step = rack.AddComponent<TexCore_RackEntryWorkflowStep>();
            step.Configure(robot, new GameObject[0], spawnMarker.transform, 0.24f);
            SetPrivateField(step, "stepActive", true);

            MethodInfo underRack = typeof(TexCore_RackEntryWorkflowStep).GetMethod(
                "IsRobotUnderRack", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(underRack, Is.Not.Null);

            rack.transform.position = Vector3.zero;
            robot.transform.position = new Vector3(1f, 0f, 1f);
            Assert.That((bool)underRack.Invoke(step, null), Is.False,
                "Touching the rack bounds must not dock the robot any more.");

            robot.transform.position = new Vector3(0.1f, 0f, 0.1f);
            Assert.That((bool)underRack.Invoke(step, null), Is.True,
                "The robot docks once it is parked under the rack.");

            InvokePrivate(step, "AttachToRobot", robot.transform);
            Assert.That(step.IsAttachedToRobot, Is.True);
            Assert.That(rack.transform.position.x, Is.EqualTo(robot.transform.position.x).Within(0.001f));
            Assert.That(rack.transform.position.z, Is.EqualTo(robot.transform.position.z).Within(0.001f));

            float robotTop = robot.GetComponent<BoxCollider>().bounds.max.y;
            Assert.That(rack.transform.position.y, Is.EqualTo(0f).Within(0.001f),
                "The docked rack keeps its floor height; the robot passes underneath.");
            Assert.That(robotTop, Is.GreaterThan(rack.transform.position.y),
                "The robot body still reaches above the floor-standing rack.");
        }
        finally
        {
            Object.DestroyImmediate(robot);
            // rackBody is a child of rack, so destroying the rack removes both.
            Object.DestroyImmediate(rack);
            Object.DestroyImmediate(spawnMarker);
        }
    }

    [Test]
    public void CompletionGate_ReportsRemainingTargetProgress()
    {
        GameObject robot = new GameObject("AGV1");
        GameObject rack = new GameObject(RackStepObjectName);
        GameObject firstTarget = new GameObject("huojia1");
        GameObject secondTarget = new GameObject("huojia2");
        GameObject spawnMarker = new GameObject("10_货架生成点");
        GameObject gateObject = new GameObject(GateObjectName);
        try
        {
            rack.AddComponent<BoxCollider>();
            TexCore_RackEntryWorkflowStep step = rack.AddComponent<TexCore_RackEntryWorkflowStep>();
            step.Configure(robot, new[] { firstTarget, secondTarget }, spawnMarker.transform, 0.24f);

            TexCore_ModuleCompletionGateStep gate =
                gateObject.AddComponent<TexCore_ModuleCompletionGateStep>();
            gate.Configure(new TexCore_InteracterBase[] { step }, "请先完成前置步骤");
            gate.SetReturnTopicEntry(OperateTopicName);

            string message = (string)InvokePrivate(gate, "BuildIncompleteMessage");
            Assert.That(message, Is.EqualTo("当前完成 0/2，请返回02机器人搬运至货架点继续搬运。"));

            // Without a return topic the previous message-only behavior is kept.
            gate.SetReturnTopicEntry(string.Empty);
            Assert.That((string)InvokePrivate(gate, "BuildIncompleteMessage"),
                Is.EqualTo("请先完成前置步骤"));
        }
        finally
        {
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(rack);
            Object.DestroyImmediate(firstTarget);
            Object.DestroyImmediate(secondTarget);
            Object.DestroyImmediate(spawnMarker);
            Object.DestroyImmediate(gateObject);
        }
    }

    private static GameObject LoadTopic(string topicName)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(
            TopicRoot + "/" + topicName + ".prefab");
    }

    /// <summary>
    /// A root Canvas in the shape the shared UI uses: the GameObject stays active
    /// and visibility is carried by <c>Canvas.enabled</c>.
    /// </summary>
    private static GameObject CreateCanvas(string name)
    {
        GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas));
        canvasObject.GetComponent<Canvas>().enabled = true;
        return canvasObject;
    }

    private static GameObject CreateUiButton(Transform parent, string name)
    {
        GameObject button = new GameObject(name, typeof(RectTransform));
        button.transform.SetParent(parent, false);
        return button;
    }

    private static void SetCanvasEnabled(GameObject canvasObject, bool enabled)
    {
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        Assert.That(canvas, Is.Not.Null, canvasObject.name + " must stay a Canvas root.");
        canvas.enabled = enabled;
    }

    /// <summary>
    /// Runs the guide's own frame step without a play mode run. EditMode does not
    /// tick MonoBehaviour, so the test drives the same entry point LateUpdate uses.
    /// </summary>
    private static void TexCore_BackpackTick()
    {
        TexCore_FocusHighlight highlight = TexCore_FocusHighlight.Instance;
        Assert.That(highlight, Is.Not.Null, "The guide instance must exist once something is shown.");
        highlight.Revalidate();
    }

    private static string DescribeFocusState()
    {
        TexCore_FocusHighlight highlight = TexCore_FocusHighlight.Instance;
        if (highlight == null)
        {
            return "focus=no-instance";
        }

        GameObject target = highlight.PrimaryTarget;
        return string.Format(
            "focus target={0} active={1} owner={2} isActive={3} lines=[{4}]",
            target == null ? "null" : target.name,
            target == null ? "n/a" : target.activeInHierarchy.ToString(),
            highlight.OwnerId,
            highlight.IsActive,
            string.Join(",", EnabledGuideLines().ToArray()));
    }

    private static string DescribeBounds(GameObject target)
    {        if (target == null)
        {
            return "null";
        }

        Bounds bounds = default;
        bool found = false;
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found ? bounds.ToString() : "no-bounds";
    }

    private static bool LineRenderersAreHidden()
    {
        return EnabledGuideLines().Count == 0;
    }

    /// <summary>
    /// Names of the guide's own LineRenderers that are still being drawn. The
    /// message is carried into the assertion so a leak names itself.
    /// </summary>
    private static List<string> EnabledGuideLines()
    {
        List<string> enabled = new List<string>();
        TexCore_FocusHighlight highlight = TexCore_FocusHighlight.Instance;
        if (highlight == null)
        {
            return enabled;
        }

        foreach (LineRenderer line in highlight.GetComponentsInChildren<LineRenderer>(true))
        {
            if (line != null && line.enabled)
            {
                enabled.Add(line.name);
            }
        }

        return enabled;
    }

    private static string DescribeStaleLines()
    {
        List<string> enabled = EnabledGuideLines();
        if (enabled.Count == 0)
        {
            return "none";
        }

        return string.Join(", ", enabled.ToArray()) + " | stale=" + DescribeStalePositions();
    }

    private static string DescribeStalePositions()
    {        TexCore_FocusHighlight highlight = TexCore_FocusHighlight.Instance;
        if (highlight == null)
        {
            return "no-instance";
        }

        foreach (LineRenderer line in highlight.GetComponentsInChildren<LineRenderer>(true))
        {
            if (line == null || !line.enabled || line.positionCount == 0)
            {
                continue;
            }

            Vector3 first = line.GetPosition(0);
            Vector3 last = line.GetPosition(line.positionCount - 1);
            return string.Format("{0} count={1} first={2} last={3}", line.name, line.positionCount, first, last);
        }

        return "no-positions";
    }

    /// <summary>
    /// The racks the run has already delivered. The step keeps them so the robot
    /// can route around them, and the test needs to inject one without a full run.
    /// </summary>
    private static List<GameObject> PlacedRacks(TexCore_RackEntryWorkflowStep step)
    {
        FieldInfo field = typeof(TexCore_RackEntryWorkflowStep).GetField("placedRacks",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, "placedRacks must stay the record of delivered racks.");
        return (List<GameObject>)field.GetValue(step);
    }

    private static bool PassesThrough(List<Vector3> route, Vector3 cell)
    {
        foreach (Vector3 waypoint in route)
        {
            if (new Vector2(waypoint.x - cell.x, waypoint.z - cell.z).sqrMagnitude < 0.01f)
            {
                return true;
            }
        }

        return false;
    }

    private static object InvokePrivate(object instance, string methodName, params object[] arguments)
    {
        MethodInfo method = instance.GetType().GetMethod(methodName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null, methodName + " must stay available to the guidance flow.");
        return method.Invoke(instance, arguments);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, fieldName + " must stay available to the guidance flow.");
        field.SetValue(instance, value);
    }

    /// <summary>
    /// Streams the 65 MB scene instead of loading it. Unity writes non-ASCII
    /// object names as YAML escapes, so the needles are escaped the same way.
    /// </summary>
    private static SceneStepSnapshot ReadSceneStepSnapshot()
    {
        string escapedRackName = ToYamlEscaped(RackStepObjectName);
        string escapedGateName = ToYamlEscaped(GateObjectName);
        string escapedTopicName = ToYamlEscaped(OperateTopicName);

        SceneStepSnapshot snapshot = default;
        snapshot.TargetPointCounts = new List<int>();
        bool inTargetPoints = false;
        int currentTargetCount = 0;

        foreach (string line in File.ReadLines(ScenePath))
        {
            if (line.Contains(escapedRackName))
            {
                snapshot.FoundRackStepObject = true;
            }

            if (line.Contains(escapedGateName))
            {
                snapshot.FoundGateObject = true;
            }

            if (line.Contains("targetPoints:"))
            {
                inTargetPoints = true;
                currentTargetCount = 0;
                continue;
            }

            if (inTargetPoints)
            {
                if (line.TrimStart().StartsWith("- {fileID:"))
                {
                    currentTargetCount++;
                    continue;
                }

                inTargetPoints = false;
                snapshot.TargetPointCounts.Add(currentTargetCount);
            }

            int field = line.IndexOf("returnTopicEntryName:", System.StringComparison.Ordinal);
            if (field < 0 ||
                string.IsNullOrEmpty(line.Substring(field + "returnTopicEntryName:".Length).Trim()))
            {
                continue;
            }

            snapshot.GatesWithReturnTopic++;
            if (line.Contains(escapedTopicName))
            {
                snapshot.M10GateReturnTopicFound = true;
            }
        }

        return snapshot;
    }

    /// <summary>Escapes a value the way Unity serializes non-ASCII YAML strings.</summary>
    private static string ToYamlEscaped(string value)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        foreach (char character in value)
        {
            if (character < 128)
            {
                builder.Append(character);
            }
            else
            {
                builder.Append("\\u").Append(((int)character).ToString("X4"));
            }
        }

        return builder.ToString();
    }

    private struct SceneStepSnapshot
    {
        /// <summary>
        /// 每个货架搬运工作流的 targetPoints 条数。11流程测试 复用同一套搬运工作流，
        /// 所以场景里会同时存在 10 和 11 两条工作流，不能再把它们加总成一个数字。
        /// </summary>
        public List<int> TargetPointCounts;

        public bool FoundRackStepObject;
        public bool FoundGateObject;
        public int GatesWithReturnTopic;
        public bool M10GateReturnTopicFound;
    }
}
