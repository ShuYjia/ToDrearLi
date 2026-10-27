using System;
using System.Linq;
using System.Reflection;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public sealed class DashHolsterHandMenuRuntimeTests
{
    private GameObject holster;
    private GameObject leftInteractorObject;
    private GameObject rightInteractorObject;

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(holster);
        UnityEngine.Object.DestroyImmediate(leftInteractorObject);
        UnityEngine.Object.DestroyImmediate(rightInteractorObject);
    }

    [Test]
    public void Configure_TracksOnlyTheLeftInteractor()
    {
        holster = new GameObject("Dash Holster", typeof(Follow));
        leftInteractorObject = new GameObject("Left Interactor", typeof(XRRayInteractor));
        rightInteractorObject = new GameObject("Right Interactor", typeof(XRRayInteractor));

        MethodInfo configureMethod = FindRuntimeType().GetMethod(
            "Configure", BindingFlags.Public | BindingFlags.Static);
        bool configured = (bool)configureMethod.Invoke(null, new object[]
        {
            holster,
            leftInteractorObject.GetComponent<XRRayInteractor>(),
            rightInteractorObject.GetComponent<XRRayInteractor>()
        });

        SolverHandler solverHandler = holster.GetComponent<SolverHandler>();
        Assert.That(configured, Is.True);
        Assert.That(holster.GetComponent<Follow>().enabled, Is.False);
        Assert.That(solverHandler.TrackedTargetType, Is.EqualTo(TrackedObjectType.Interactor));
        Assert.That(solverHandler.TrackedHandedness, Is.EqualTo(Handedness.Left));
        Assert.That(solverHandler.LeftInteractor,
            Is.SameAs(leftInteractorObject.GetComponent<XRRayInteractor>()));
        Assert.That(solverHandler.RightInteractor, Is.Null);
    }

    [Test]
    public void Configure_RejectsRightOnlyController()
    {
        holster = new GameObject("Dash Holster");
        rightInteractorObject = new GameObject("Right Interactor", typeof(XRRayInteractor));

        MethodInfo configureMethod = FindRuntimeType().GetMethod(
            "Configure", BindingFlags.Public | BindingFlags.Static);
        bool configured = (bool)configureMethod.Invoke(null, new object[]
        {
            holster,
            null,
            rightInteractorObject.GetComponent<XRRayInteractor>()
        });

        Assert.That(configured, Is.False);
    }

    [Test]
    public void GestureRequiresPalmToFaceAngleAndDistance()
    {
        MethodInfo gestureMethod = FindRuntimeType().GetMethod(
            "IsGestureActive", BindingFlags.Public | BindingFlags.Static);
        GameObject camera = new GameObject("Main Camera");
        camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        HandJointPose facingPalm = new HandJointPose(
            new Vector3(0f, 0f, 0.35f),
            Quaternion.Euler(0f, 0f, 0f),
            0.01f);
        HandJointPose turnedPalm = new HandJointPose(
            facingPalm.Position,
            Quaternion.Euler(90f, 0f, 0f),
            0.01f);

        Assert.That((bool)gestureMethod.Invoke(null, new object[] { facingPalm, camera.transform }), Is.True);
        Assert.That((bool)gestureMethod.Invoke(null, new object[] { turnedPalm, camera.transform }), Is.False);
        UnityEngine.Object.DestroyImmediate(camera);
    }

    private static Type FindRuntimeType()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Assembly-CSharp")
            .GetType("App.Runtime.HandMenu.DashHolsterHandMenuRuntime");
    }
}
