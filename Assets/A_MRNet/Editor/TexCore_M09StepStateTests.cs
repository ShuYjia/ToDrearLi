using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class TexCore_M09StepStateTests
{
    [Test]
    public void DrillStep_MustLeaveTargetBeforeReturningToComplete()
    {
        GameObject reporter = new GameObject("09 Drill Reporter");
        GameObject drill = new GameObject("09 Drill");
        GameObject target = new GameObject("09 Nail Target");

        try
        {
            TexCore_OrderedPlacementStep step =
                reporter.AddComponent<TexCore_OrderedPlacementStep>();
            step.ConfigureSource(drill);
            step.Target = target.transform;
            step.ConfigureAutoCompleteOnApproach(0.25f, true);
            step.UnderWay();

            Assert.That(CanAutoComplete(step), Is.False,
                "激活时已贴在当前钉位，不能自动完成下一颗钉。");

            drill.transform.position = Vector3.right;
            UpdateApproachArm(step);
            drill.transform.position = target.transform.position;

            Assert.That(CanAutoComplete(step), Is.True,
                "离开当前钉位后重新进入，才允许本颗钉完成。");
        }
        finally
        {
            Object.DestroyImmediate(reporter);
            Object.DestroyImmediate(drill);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void LaterSharedAgvStep_DoesNotResetCompletedUnboxPose()
    {
        GameObject unpackReporter = new GameObject("09_10_推出机器人");
        GameObject moveReporter = new GameObject("09_15_机器人移动至二维码");
        GameObject agv = new GameObject("Shared AGV");
        GameObject unboxTarget = new GameObject("Unbox Target");
        GameObject qrTarget = new GameObject("QR Target");

        try
        {
            unboxTarget.transform.position = Vector3.right;
            qrTarget.transform.position = Vector3.right * 3f;

            TexCore_OrderedPlacementStep unpack =
                unpackReporter.AddComponent<TexCore_OrderedPlacementStep>();
            unpack.ConfigureSource(agv);
            unpack.Target = unboxTarget.transform;
            unpack.UnderWay();
            unpack.Finish();

            TexCore_OrderedPlacementStep move =
                moveReporter.AddComponent<TexCore_OrderedPlacementStep>();
            move.ConfigureSource(agv);
            move.Target = qrTarget.transform;
            move.UnFinished();
            move.UnderWay();

            Assert.That(Vector3.Distance(agv.transform.position, unboxTarget.transform.position),
                Is.LessThan(0.0001f),
                "二维码步骤未激活或激活时，不得把已经推出的 AGV 重置回箱内。");
            Assert.That(move.Target, Is.SameAs(qrTarget.transform));
        }
        finally
        {
            Object.DestroyImmediate(unpackReporter);
            Object.DestroyImmediate(moveReporter);
            Object.DestroyImmediate(agv);
            Object.DestroyImmediate(unboxTarget);
            Object.DestroyImmediate(qrTarget);
        }
    }

    [Test]
    public void FutureDrillStep_DoesNotHideSharedDrill()
    {
        GameObject reporter = new GameObject("09 Future Drill Reporter");
        GameObject drill = new GameObject("09 Drill");
        GameObject target = new GameObject("09 Future Nail Target");
        GameObject screw = new GameObject("09 Future Nail");

        try
        {
            TexCore_StepObjectState state = reporter.AddComponent<TexCore_StepObjectState>();
            state.Configure(
                new[] { drill, target, screw },
                null,
                null,
                new[] { target, screw },
                new[] { screw },
                new[] { target });

            state.UnFinished();

            Assert.That(drill.activeSelf, Is.True,
                "未来钉子步骤不得隐藏当前步骤共用的电钻。");
            Assert.That(target.activeSelf, Is.False,
                "未来钉子的吸附点只能在轮到该钉时显示。");
            Assert.That(screw.activeSelf, Is.True,
                "未钻除的未来钉子必须保留显示。");
        }
        finally
        {
            Object.DestroyImmediate(reporter);
            Object.DestroyImmediate(drill);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(screw);
        }
    }

    [Test]
    public void BoxLidAndChargingStation_RemainVisibleAcrossM09States()
    {
        GameObject reporter = new GameObject("09 Box Lid Reporter");
        GameObject boxLid = new GameObject("09 Box Lid");
        GameObject chargingStation = new GameObject("09 Charging Station");

        try
        {
            TexCore_StepObjectState state = reporter.AddComponent<TexCore_StepObjectState>();
            state.Configure(
                new[] { boxLid, chargingStation },
                null,
                new[] { boxLid, chargingStation },
                null,
                new[] { boxLid, chargingStation },
                null);

            state.UnFinished();
            Assert.That(boxLid.activeSelf, Is.True,
                "钻钉阶段必须保留箱盖显示。");
            Assert.That(chargingStation.activeSelf, Is.True,
                "钻钉阶段必须保留充电桩显示。");

            state.UnderWay();
            Assert.That(boxLid.activeSelf, Is.True,
                "箱盖移除步骤激活后必须保持可见。");
            Assert.That(chargingStation.activeSelf, Is.True,
                "箱盖移除步骤激活后必须保持充电桩可见。");

            state.Finish();
            Assert.That(boxLid.activeSelf, Is.True,
                "箱盖移开后必须继续显示其被移开的模型。");
            Assert.That(chargingStation.activeSelf, Is.True,
                "后续机器人步骤必须保持充电桩显示。");
        }
        finally
        {
            Object.DestroyImmediate(reporter);
            Object.DestroyImmediate(boxLid);
            Object.DestroyImmediate(chargingStation);
        }
    }

    [Test]
    public void FloorQrTarget_ComparesHorizontallyAndKeepsReleaseHeight()
    {
        GameObject reporter = new GameObject("09 QR Step Reporter");
        GameObject robot = new GameObject("09 QR Robot");
        GameObject decal = new GameObject("09 QR Decal");

        try
        {
            decal.transform.position = new Vector3(1.2f, 0.01f, -0.15f);
            robot.transform.position = new Vector3(0f, 0.12f, 0f);

            TexCore_OrderedPlacementStep step =
                reporter.AddComponent<TexCore_OrderedPlacementStep>();
            step.ConfigureSource(robot);
            step.Target = decal.transform;
            step.PositionTolerance = 0.2f;
            step.RotationTolerance = 180f;
            step.IgnoreVerticalForTolerance = true;
            step.UnderWay();

            Assert.That(step.IsWithinTargetTolerance(), Is.False,
                "机器人尚未推到二维码上时不得判定完成。");

            robot.transform.position = new Vector3(1.2f, 0.12f, -0.15f);
            Assert.That(step.IsWithinTargetTolerance(), Is.True,
                "机器人原点高于地面二维码贴片时必须只比较水平距离。");

            InvokePrivate(step, "SnapToTarget");

            Assert.That(new Vector2(robot.transform.position.x - 1.2f,
                    robot.transform.position.z + 0.15f).magnitude, Is.LessThan(0.0001f),
                "完成后必须水平对齐到二维码贴片。");
            Assert.That(robot.transform.position.y, Is.EqualTo(0.12f).Within(0.0001f),
                "完成后不得把机器人压到地面二维码贴片的高度。");
        }
        finally
        {
            Object.DestroyImmediate(reporter);
            Object.DestroyImmediate(robot);
            Object.DestroyImmediate(decal);
        }
    }

    private static bool CanAutoComplete(TexCore_OrderedPlacementStep step)
    {
        MethodInfo method = typeof(TexCore_OrderedPlacementStep).GetMethod(
            "CanAutoCompleteOnApproach", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        return (bool)method.Invoke(step, null);
    }

    private static void InvokePrivate(TexCore_OrderedPlacementStep step, string methodName)
    {
        MethodInfo method = typeof(TexCore_OrderedPlacementStep).GetMethod(
            methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "找不到私有方法：" + methodName);
        method.Invoke(step, null);
    }

    private static void UpdateApproachArm(TexCore_OrderedPlacementStep step)
    {
        MethodInfo method = typeof(TexCore_OrderedPlacementStep).GetMethod(
            "UpdateAutoCompleteApproachArm", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(step, null);
    }
}
