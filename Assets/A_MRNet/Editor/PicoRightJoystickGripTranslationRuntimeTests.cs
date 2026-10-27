using App.Runtime.GripTranslation;
using NUnit.Framework;
using UnityEngine;

public sealed class PicoRightJoystickGripTranslationRuntimeTests
{
    [Test]
    public void GetTranslationDelta_UsesRightStickVerticalAxisAlongRayDirection()
    {
        Vector3 delta = PicoRightJoystickGripTranslationRuntime.GetTranslationDelta(
            0.5f,
            Vector3.forward,
            2f,
            0.25f);

        Assert.That(delta, Is.EqualTo(Vector3.forward * 0.25f));
    }

    [Test]
    public void GetTranslationDelta_PullsBackForNegativeInputAndNormalizesRayDirection()
    {
        Vector3 delta = PicoRightJoystickGripTranslationRuntime.GetTranslationDelta(
            -1f,
            new Vector3(0f, 0.6f, 0.8f),
            1.5f,
            1f);

        Assert.That(delta.x, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(delta.y, Is.EqualTo(-0.9f).Within(0.0001f));
        Assert.That(delta.z, Is.EqualTo(-1.2f).Within(0.0001f));
    }

    [Test]
    public void GetTranslationDelta_ReturnsZeroForNoRayDirection()
    {
        Vector3 delta = PicoRightJoystickGripTranslationRuntime.GetTranslationDelta(
            1f,
            Vector3.zero,
            1.5f,
            1f);

        Assert.That(delta, Is.EqualTo(Vector3.zero));
    }
}
