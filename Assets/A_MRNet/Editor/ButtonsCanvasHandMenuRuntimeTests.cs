using System;
using System.Linq;
using System.Reflection;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.UX;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public sealed class ButtonsCanvasHandMenuRuntimeTests
{
    private GameObject buttonsCanvas;
    private GameObject leftInteractorObject;
    private GameObject rightInteractorObject;
    private GameObject singleMenuCanvas;
    private GameObject backpackUi;

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(buttonsCanvas);
        UnityEngine.Object.DestroyImmediate(leftInteractorObject);
        UnityEngine.Object.DestroyImmediate(rightInteractorObject);
        UnityEngine.Object.DestroyImmediate(singleMenuCanvas);
        UnityEngine.Object.DestroyImmediate(backpackUi);
    }

    [Test]
    public void Configure_TracksOnlyTheLeftInteractorAndLeavesTheCanvasAtItsScenePose()
    {
        buttonsCanvas = new GameObject("Buttons Canvas", typeof(RectTransform), typeof(Canvas));
        leftInteractorObject = new GameObject("Left Interactor", typeof(XRRayInteractor));
        rightInteractorObject = new GameObject("Right Interactor", typeof(XRRayInteractor));

        Type runtimeType = AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Assembly-CSharp")
            .GetType("App.Runtime.HandMenu.ButtonsCanvasHandMenuRuntime");

        Assert.That(runtimeType, Is.Not.Null);

        MethodInfo configureMethod = runtimeType.GetMethod(
            "Configure",
            BindingFlags.Public | BindingFlags.Static);

        Assert.That(configureMethod, Is.Not.Null);

        bool configured = (bool)configureMethod.Invoke(
            null,
            new object[]
            {
                buttonsCanvas,
                leftInteractorObject.GetComponent<XRRayInteractor>(),
                rightInteractorObject.GetComponent<XRRayInteractor>()
            });

        Assert.That(configured, Is.True);

        SolverHandler solverHandler = buttonsCanvas.GetComponent<SolverHandler>();
        HandConstraint handConstraint = buttonsCanvas.GetComponent<HandConstraint>();

        Assert.That(solverHandler, Is.Not.Null);
        Assert.That(handConstraint, Is.Not.Null);
        Assert.That(buttonsCanvas.GetComponent<HandBounds>(), Is.Not.Null);
        Assert.That(solverHandler.TrackedTargetType, Is.EqualTo(TrackedObjectType.Interactor));
        Assert.That(solverHandler.TrackedHandedness, Is.EqualTo(Handedness.Left));
        Assert.That(solverHandler.PreferredTrackedHandedness, Is.EqualTo(Handedness.Left));
        Assert.That(solverHandler.LeftInteractor, Is.SameAs(leftInteractorObject.GetComponent<XRRayInteractor>()));
        Assert.That(solverHandler.RightInteractor, Is.Null);
        Assert.That(solverHandler.UpdateSolvers, Is.False);
        Assert.That(solverHandler.AdditionalOffset, Is.EqualTo(Vector3.zero));
        Assert.That(handConstraint.HideHandCursorsOnActivate, Is.False);

        bool configuredAgain = (bool)configureMethod.Invoke(
            null,
            new object[]
            {
                buttonsCanvas,
                leftInteractorObject.GetComponent<XRRayInteractor>(),
                rightInteractorObject.GetComponent<XRRayInteractor>()
            });

        Assert.That(configuredAgain, Is.True);
        Assert.That(buttonsCanvas.GetComponents<SolverHandler>(), Has.Length.EqualTo(1));
        Assert.That(buttonsCanvas.GetComponents<HandConstraint>(), Has.Length.EqualTo(1));
    }

    [Test]
    public void LeftX_UsesAShortPressToToggleAndAHoldToFollowTheLeftHand()
    {
        Type runtimeType = AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Assembly-CSharp")
            .GetType("App.Runtime.HandMenu.ButtonsCanvasHandMenuRuntime");
        FieldInfo keyboardBindingField = runtimeType.GetField(
            "KeyboardXBinding",
            BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo longPressMethod = runtimeType.GetMethod(
            "IsLongPress",
            BindingFlags.Public | BindingFlags.Static);
        MethodInfo followMethod = runtimeType.GetMethod(
            "ShouldFollowLeftHand",
            BindingFlags.Public | BindingFlags.Static);

        Assert.That(keyboardBindingField, Is.Not.Null);
        Assert.That(keyboardBindingField.GetValue(null), Is.EqualTo("<Keyboard>/x"));
        Assert.That(longPressMethod, Is.Not.Null);
        Assert.That(followMethod, Is.Not.Null);
        Assert.That((bool)longPressMethod.Invoke(null, new object[] { 0.34f }), Is.False);
        Assert.That((bool)longPressMethod.Invoke(null, new object[] { 0.35f }), Is.True);
        Assert.That((bool)followMethod.Invoke(null, new object[] { false, 1f }), Is.False);
        Assert.That((bool)followMethod.Invoke(null, new object[] { true, 0.35f }), Is.True);
    }

    [Test]
    public void ButtonsSwitchAmongMutuallyExclusiveUiAndReturnToButtonsCanvas()
    {
        buttonsCanvas = new GameObject("Buttons Canvas");
        Transform listItems = new GameObject("ListItems").transform;
        listItems.SetParent(buttonsCanvas.transform, false);
        Transform parentObject = new GameObject("ParentObject").transform;
        parentObject.SetParent(listItems, false);

        GameObject singleMenuButtonObject = new GameObject("List Item");
        singleMenuButtonObject.transform.SetParent(parentObject, false);
        PressableButton singleMenuButton = singleMenuButtonObject.AddComponent<PressableButton>();

        GameObject backpackButtonObject = new GameObject("List Item (3)");
        backpackButtonObject.transform.SetParent(parentObject, false);
        PressableButton backpackButton = backpackButtonObject.AddComponent<PressableButton>();

        singleMenuCanvas = new GameObject("Single Menu Canvas");
        backpackUi = new GameObject("背包UI");

        Type runtimeType = AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Assembly-CSharp")
            .GetType("App.Runtime.HandMenu.ButtonsCanvasHandMenuRuntime");
        MethodInfo configureButtonsMethod = runtimeType.GetMethod(
            "ConfigureSceneButtonActions",
            BindingFlags.Public | BindingFlags.Static);

        Assert.That(configureButtonsMethod, Is.Not.Null);
        bool configured = (bool)configureButtonsMethod.Invoke(
            null,
            new object[] { buttonsCanvas, singleMenuCanvas, backpackUi });

        Assert.That(configured, Is.True);
        Assert.That(buttonsCanvas.activeSelf, Is.True);
        Assert.That(singleMenuCanvas.activeSelf, Is.False);
        Assert.That(backpackUi.activeSelf, Is.False);

        singleMenuButton.OnClicked.Invoke();
        Assert.That(buttonsCanvas.activeSelf, Is.False);
        Assert.That(singleMenuCanvas.activeSelf, Is.True);
        Assert.That(backpackUi.activeSelf, Is.False);

        backpackButton.OnClicked.Invoke();
        Assert.That(buttonsCanvas.activeSelf, Is.False);
        Assert.That(singleMenuCanvas.activeSelf, Is.False);
        Assert.That(backpackUi.activeSelf, Is.True);

        MethodInfo showButtonsMethod = runtimeType.GetMethod(
            "ShowButtonsCanvas",
            BindingFlags.Public | BindingFlags.Static);
        Assert.That(showButtonsMethod, Is.Not.Null);
        showButtonsMethod.Invoke(
            null,
            new object[] { buttonsCanvas, singleMenuCanvas, backpackUi });

        Assert.That(buttonsCanvas.activeSelf, Is.True);
        Assert.That(singleMenuCanvas.activeSelf, Is.False);
        Assert.That(backpackUi.activeSelf, Is.False);
    }

    [Test]
    public void LinkedUiPosesShareButtonsCanvasOriginAndRotation()
    {
        buttonsCanvas = new GameObject("Buttons Canvas", typeof(RectTransform), typeof(Canvas));
        singleMenuCanvas = new GameObject("Single Menu Canvas");
        backpackUi = new GameObject("背包UI");
        singleMenuCanvas.transform.position = new Vector3(10f, 10f, 10f);
        backpackUi.transform.position = new Vector3(-10f, -10f, -10f);

        Type runtimeType = AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Assembly-CSharp")
            .GetType("App.Runtime.HandMenu.ButtonsCanvasHandMenuRuntime");
        MethodInfo applyPoseMethod = runtimeType.GetMethod(
            "ApplyLinkedPose",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[]
            {
                typeof(Transform),
                typeof(Transform),
                typeof(Vector3),
                typeof(Quaternion),
                typeof(Vector3)
            },
            null);
        FieldInfo singleOffsetField = runtimeType.GetField(
            "SingleMenuFollowOffset",
            BindingFlags.NonPublic | BindingFlags.Static);
        FieldInfo backpackOffsetField = runtimeType.GetField(
            "BackpackFollowOffset",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(applyPoseMethod, Is.Not.Null);
        Assert.That(singleOffsetField, Is.Not.Null);
        Assert.That(backpackOffsetField, Is.Not.Null);

        Transform source = buttonsCanvas.transform;
        Vector3 singleOffset = (Vector3)singleOffsetField.GetValue(null);
        Vector3 backpackOffset = (Vector3)backpackOffsetField.GetValue(null);
        source.position = new Vector3(2f, 3f, 4f);
        source.rotation = Quaternion.Euler(0f, 45f, 0f);

        applyPoseMethod.Invoke(
            null,
            new object[]
            {
                source,
                singleMenuCanvas.transform,
                Vector3.zero,
                Quaternion.identity,
                singleOffset
            });
        applyPoseMethod.Invoke(
            null,
            new object[]
            {
                source,
                backpackUi.transform,
                Vector3.zero,
                Quaternion.identity,
                backpackOffset
            });

        Assert.That(singleOffset, Is.EqualTo(Vector3.zero));
        Assert.That(backpackOffset, Is.EqualTo(Vector3.zero));
        Assert.That(singleMenuCanvas.transform.position, Is.EqualTo(source.position));
        Assert.That(backpackUi.transform.position, Is.EqualTo(source.position));
        Assert.That(singleMenuCanvas.transform.rotation, Is.EqualTo(source.rotation));
        Assert.That(backpackUi.transform.rotation, Is.EqualTo(source.rotation));
    }
}
