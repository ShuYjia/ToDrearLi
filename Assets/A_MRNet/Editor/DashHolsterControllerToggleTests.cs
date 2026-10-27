using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class DashHolsterControllerToggleTests
{
    [Test]
    public void LeftXToggle_ChangesVisibilityAndUsesAnUpwardControllerOffset()
    {
        Type runtimeType = AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Assembly-CSharp")
            .GetType("App.Runtime.HandMenu.DashHolsterHandMenuRuntime");
        MethodInfo toggleMethod = runtimeType.GetMethod(
            "ToggleControllerVisibility",
            BindingFlags.Public | BindingFlags.Static);
        MethodInfo offsetMethod = runtimeType.GetMethod(
            "GetControllerMenuOffset",
            BindingFlags.Public | BindingFlags.Static);

        Assert.That(toggleMethod, Is.Not.Null);
        Assert.That(offsetMethod, Is.Not.Null);
        Assert.That((bool)toggleMethod.Invoke(null, new object[] { false }), Is.True);
        Assert.That((bool)toggleMethod.Invoke(null, new object[] { true }), Is.False);
        Assert.That((Vector3)offsetMethod.Invoke(null, null),
            Is.EqualTo(new Vector3(0f, 0.18f, 0f)));
    }

    [Test]
    public void LeftXHold_FollowsOnlyAfterTheLongPressThreshold()
    {
        Type runtimeType = AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Assembly-CSharp")
            .GetType("App.Runtime.HandMenu.DashHolsterHandMenuRuntime");
        MethodInfo longPressMethod = runtimeType.GetMethod(
            "IsLongPress",
            BindingFlags.Public | BindingFlags.Static);
        MethodInfo followMethod = runtimeType.GetMethod(
            "ShouldFollowLeftHand",
            BindingFlags.Public | BindingFlags.Static);

        Assert.That(longPressMethod, Is.Not.Null);
        Assert.That(followMethod, Is.Not.Null);
        Assert.That((bool)longPressMethod.Invoke(null, new object[] { 0.34f }), Is.False);
        Assert.That((bool)longPressMethod.Invoke(null, new object[] { 0.35f }), Is.True);
        Assert.That((bool)followMethod.Invoke(null, new object[] { false, 1f }), Is.False);
        Assert.That((bool)followMethod.Invoke(null, new object[] { true, 0.35f }), Is.True);
    }
}
