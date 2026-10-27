using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode coverage for the M05 ink-line assist: black completed lines pinned
/// to the grid endpoints, dedupe/reset behaviour and the single black JiZhun
/// copy that assists the QR paste phase.
/// The core release seam <see cref="TexCore_M05DrawStep"/> CompleteInkRelease
/// is invoked through reflection so no live XR select-exit event is needed.
/// </summary>
public sealed class TexCore_M05DrawAssistTests
{
    private GameObject huaXianRoot;
    private Transform jiZhun;
    private TexCore_M05DrawStep drawStep;
    private TexCore_M05QrPasteStep qrStep;

    [SetUp]
    public void SetUp()
    {
        huaXianRoot = new GameObject("HuaXian");

        Transform moDou = new GameObject("MoDou").transform;
        moDou.SetParent(huaXianRoot.transform, false);
        GameObject template = new GameObject("PlaneLine");
        template.transform.SetParent(moDou, false);
        template.AddComponent<LineRenderer>();

        Transform ends = new GameObject("Ends").transform;
        ends.SetParent(huaXianRoot.transform, false);
        Transform firstEnd = new GameObject("First").transform;
        firstEnd.SetParent(ends, false);
        Transform secondEnd = new GameObject("Second").transform;
        secondEnd.SetParent(ends, false);

        jiZhun = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        jiZhun.name = "JiZhun";
        jiZhun.SetParent(huaXianRoot.transform, false);
        jiZhun.position = new Vector3(1f, 2f, 3f);
        jiZhun.localScale = Vector3.one * 2f;

        GameObject drawObject = new GameObject("M05_DrawStep");
        drawObject.transform.SetParent(huaXianRoot.transform, false);
        drawStep = drawObject.AddComponent<TexCore_M05DrawStep>();
        drawStep.Configure(firstEnd, firstEnd, secondEnd, null);
        drawStep.ConfigurePreparation(huaXianRoot.transform, null, null, null, null, null, null);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(huaXianRoot);
        drawStep = null;
        qrStep = null;
    }

    [Test]
    public void CorrectRelease_CreatesOneBlackLinePinnedToGridEndpoints()
    {
        InvokeRelease(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 4f));

        Assert.That(drawStep.CompletedSegmentCount, Is.EqualTo(1));
        Transform line = huaXianRoot.transform.Find("M05_CompletedLine_0");
        Assert.That(line, Is.Not.Null);
        // The line is an independent object under HuaXian, not a child that
        // follows or hides with the MoDou.
        Assert.That(line.parent, Is.EqualTo(huaXianRoot.transform));

        LineRenderer renderer = line.GetComponent<LineRenderer>();
        Assert.That(renderer.useWorldSpace, Is.True);
        Assert.That(renderer.startColor, Is.EqualTo(Color.black));
        Assert.That(renderer.endColor, Is.EqualTo(Color.black));
        Assert.That(renderer.widthMultiplier, Is.EqualTo(0.004f).Within(0.0001f));
        Assert.That(renderer.GetPosition(0), Is.EqualTo(new Vector3(0f, 0f, 0f)));
        Assert.That(renderer.GetPosition(1), Is.EqualTo(new Vector3(0f, 0f, 4f)));
    }

    [Test]
    public void WrongRelease_DoesNotCreateAnyPersistentLine()
    {
        InvokeRelease(new Vector3(9f, 9f, 9f), new Vector3(9f, 9f, 10f));

        Assert.That(drawStep.CompletedSegmentCount, Is.EqualTo(0));
        Assert.That(CompletedLineCount(), Is.EqualTo(0));
    }

    [Test]
    public void RepeatReleaseOnSameSegment_DoesNotDuplicateLine()
    {
        InvokeRelease(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 4f));
        InvokeRelease(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 4f));

        Assert.That(drawStep.CompletedSegmentCount, Is.EqualTo(1));
        Assert.That(CompletedLineCount(), Is.EqualTo(1));
    }

    [Test]
    public void TenCorrectLines_CreateExactlyOneBlackJiZhunClone()
    {
        CompleteAllSegments();

        Assert.That(drawStep.CompletedSegmentCount, Is.EqualTo(10));
        Assert.That(CompletedLineCount(), Is.EqualTo(10));

        GameObject clone = drawStep.JiZhunClone;
        Assert.That(clone, Is.Not.Null);
        Assert.That(clone.activeSelf, Is.True);
        Assert.That(clone.name, Is.EqualTo("M05_JiZhun_黑色复制体"));
        Assert.That(clone.transform.parent, Is.EqualTo(huaXianRoot.transform));
        Assert.That(clone.transform.position, Is.EqualTo(jiZhun.position));
        Assert.That(clone.transform.lossyScale, Is.EqualTo(Vector3.one * 2f));
        Assert.That(
            clone.GetComponentsInChildren<Collider>(true),
            Has.All.Matches<Collider>(value => !value.enabled),
            "复制体必须关闭全部碰撞，避免阻挡二维码交互。");
        Assert.That(
            clone.GetComponentsInChildren<MeshRenderer>(true).First().material.color,
            Is.EqualTo(Color.black));
        // The original JiZhun keeps its colliders; only the copy is inert.
        Assert.That(jiZhun.GetComponentsInChildren<Collider>(true), Has.All.Matches<Collider>(value => value.enabled));

        MethodInfo create = typeof(TexCore_M05DrawStep).GetMethod(
            "CreateJiZhunClone", BindingFlags.Instance | BindingFlags.NonPublic);
        create.Invoke(drawStep, null);
        Assert.That(drawStep.JiZhunClone, Is.SameAs(clone), "复制体只允许创建一次。");
    }

    [Test]
    public void QrPhase_KeepsCloneVisible_UntilCompletionOrReset()
    {
        MethodInfo create = typeof(TexCore_M05DrawStep).GetMethod(
            "CreateJiZhunClone", BindingFlags.Instance | BindingFlags.NonPublic);
        create.Invoke(drawStep, null);
        GameObject clone = drawStep.JiZhunClone;
        Assert.That(clone, Is.Not.Null);

        qrStep = new GameObject("M05_QrPasteStep").AddComponent<TexCore_M05QrPasteStep>();
        qrStep.transform.SetParent(huaXianRoot.transform, false);
        qrStep.Configure(null, null);
        qrStep.BindDrawStep(drawStep);

        qrStep.UnderWay();
        Assert.That(clone.activeSelf, Is.True, "二维码阶段复制体必须保持显示。");

        qrStep.Finish();
        Assert.That(drawStep.JiZhunClone, Is.Null);
        Assert.IsTrue(clone == null, "二维码完成后复制体必须销毁。");

        create.Invoke(drawStep, null);
        GameObject secondClone = drawStep.JiZhunClone;
        Assert.That(secondClone, Is.Not.Null);
        qrStep.UnFinished();
        Assert.That(drawStep.JiZhunClone, Is.Null);
        Assert.IsTrue(secondClone == null, "模块重置时复制体必须销毁。");
    }

    [Test]
    public void Reset_ClearsAllLinesAndSegments()
    {
        CompleteAllSegments();
        Assert.That(CompletedLineCount(), Is.EqualTo(10));

        drawStep.UnFinished();

        Assert.That(drawStep.CompletedSegmentCount, Is.EqualTo(0));
        Assert.That(CompletedLineCount(), Is.EqualTo(0));
        Assert.That(drawStep.JiZhunClone, Is.Null);
    }

    private void CompleteAllSegments()
    {
        for (int i = 0; i <= 4; i++)
        {
            InvokeRelease(new Vector3(i, 0f, 0f), new Vector3(i, 0f, 4f));
            InvokeRelease(new Vector3(0f, 0f, i), new Vector3(4f, 0f, i));
        }
    }

    private int CompletedLineCount()
    {
        return huaXianRoot.GetComponentsInChildren<LineRenderer>(true)
            .Count(value => value.gameObject.name.StartsWith("M05_CompletedLine_"));
    }

    private void InvokeRelease(Vector3 start, Vector3 end)
    {
        MethodInfo method = typeof(TexCore_M05DrawStep).GetMethod(
            "CompleteInkRelease", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(drawStep, new object[] { start, end });
    }
}
