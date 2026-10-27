using System;
using System.Linq;
using System.Reflection;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using NUnit.Framework;
using UnityEngine;

public sealed class DashHolsterHandMenuGestureRuntimeTests
{
    [Test]
    public void Configure_AllowsLeftPalmGestureWhenNoControllersAreAvailable()
    {
        GameObject holster = new GameObject("Dash Holster");
        try
        {
            MethodInfo configureMethod = FindRuntimeType().GetMethod(
                "Configure",
                BindingFlags.Public | BindingFlags.Static);

            bool configured = (bool)configureMethod.Invoke(
                null,
                new object[] { holster, null, null });

            SolverHandler solverHandler = holster.GetComponent<SolverHandler>();
            Assert.That(configured, Is.True);
            Assert.That(solverHandler, Is.Not.Null);
            Assert.That(solverHandler.TrackedTargetType, Is.EqualTo(TrackedObjectType.HandJoint));
            Assert.That(solverHandler.TrackedHandedness, Is.EqualTo(MixedReality.Toolkit.Handedness.Left));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(holster);
        }
    }

    private static Type FindRuntimeType()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Assembly-CSharp")
            .GetType("App.Runtime.HandMenu.DashHolsterHandMenuRuntime");
    }
}
