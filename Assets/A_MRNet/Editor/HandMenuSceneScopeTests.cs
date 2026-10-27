using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class HandMenuSceneScopeTests
{
    [Test]
    public void DashAndButtonsMenus_AreScopedToTheirOwnScenes()
    {
        Assert.That(InvokeScenePredicate(
            "App.Runtime.HandMenu.DashHolsterHandMenuRuntime",
            "IsDashHolsterScene",
            "01StartUp"), Is.True);
        Assert.That(InvokeScenePredicate(
            "App.Runtime.HandMenu.DashHolsterHandMenuRuntime",
            "IsDashHolsterScene",
            "03SingleplayerScene"), Is.False);
        Assert.That(InvokeScenePredicate(
            "App.Runtime.HandMenu.ButtonsCanvasHandMenuRuntime",
            "IsButtonsCanvasScene",
            "03SingleplayerScene"), Is.True);
        Assert.That(InvokeScenePredicate(
            "App.Runtime.HandMenu.ButtonsCanvasHandMenuRuntime",
            "IsButtonsCanvasScene",
            "01StartUp"), Is.False);
    }

    private static bool InvokeScenePredicate(string typeName, string methodName, string sceneName)
    {
        Type runtimeType = AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Assembly-CSharp")
            .GetType(typeName);
        MethodInfo method = runtimeType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);
        return (bool)method.Invoke(null, new object[] { sceneName });
    }
}
