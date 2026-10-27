using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class PicoLeftJoystickLocomotionRuntimeTests
{
    [Test]
    public void GetPlanarMoveDirection_UsesHeadYawAndPreservesAnalogMagnitude()
    {
        MethodInfo moveDirectionMethod = GetRuntimeType().GetMethod(
            "GetPlanarMoveDirection",
            BindingFlags.Public | BindingFlags.Static);

        Assert.That(moveDirectionMethod, Is.Not.Null);

        Vector3 forward = (Vector3)moveDirectionMethod.Invoke(
            null,
            new object[] { new Vector2(0f, 0.5f), Vector3.right });
        Vector3 right = (Vector3)moveDirectionMethod.Invoke(
            null,
            new object[] { new Vector2(1f, 0f), Vector3.forward });

        Assert.That(forward, Is.EqualTo(Vector3.right * 0.5f));
        Assert.That(right, Is.EqualTo(Vector3.right));
    }

    [Test]
    public void GetPlanarMoveDirection_IgnoresVerticalHeadPitch()
    {
        MethodInfo moveDirectionMethod = GetRuntimeType().GetMethod(
            "GetPlanarMoveDirection",
            BindingFlags.Public | BindingFlags.Static);

        Vector3 moveDirection = (Vector3)moveDirectionMethod.Invoke(
            null,
            new object[] { Vector2.up, new Vector3(0f, -0.8f, 0.6f) });

        Assert.That(moveDirection, Is.EqualTo(Vector3.forward));
    }

    private static Type GetRuntimeType()
    {
        Type runtimeType = AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Assembly-CSharp")
            .GetType("App.Runtime.Locomotion.PicoLeftJoystickLocomotionRuntime");

        Assert.That(runtimeType, Is.Not.Null);
        return runtimeType;
    }
}
