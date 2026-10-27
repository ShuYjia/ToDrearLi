using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TexCore_BackpackPhysicsTests
{
    [Test]
    public void Stabilize_FreezesPhysicsAcrossTheSummonedObjectHierarchy()
    {
        GameObject root = new GameObject("Summoned Root");
        GameObject child = new GameObject("Summoned Child");

        try
        {
            child.transform.SetParent(root.transform);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            Rigidbody rootBody = root.AddComponent<Rigidbody>();
            Rigidbody childBody = child.AddComponent<Rigidbody>();
            ConstantForce force = child.AddComponent<ConstantForce>();
            PrepareDynamicBody(rootBody);
            PrepareDynamicBody(childBody);
            force.enabled = true;

            Type physicsType = typeof(TexCore_RackAssemblyController).Assembly
                .GetType("TexCore_BackpackPhysics");
            Assert.That(physicsType, Is.Not.Null,
                "Backpack-spawned objects need a dedicated physics stabilizer.");

            MethodInfo stabilize = physicsType.GetMethod(
                "Stabilize", BindingFlags.Public | BindingFlags.Static);
            Assert.That(stabilize, Is.Not.Null,
                "The stabilizer must expose a public entry point for backpack spawners.");

            stabilize.Invoke(null, new object[] { root });

            Assert.That(rootBody.isKinematic, Is.True);
            Assert.That(rootBody.useGravity, Is.False);
            Assert.That(rootBody.velocity, Is.EqualTo(Vector3.zero));
            Assert.That(rootBody.angularVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(childBody.isKinematic, Is.True);
            Assert.That(childBody.useGravity, Is.False);
            Assert.That(childBody.velocity, Is.EqualTo(Vector3.zero));
            Assert.That(childBody.angularVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(force.enabled, Is.False);
            Assert.That(collider.enabled, Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void M09DrillBootstrap_StabilizesThe09DrillInItsScene()
    {
        Scene scene = SceneManager.CreateScene("03SingleplayerScene");
        GameObject root = new GameObject("无交互物体");

        try
        {
            SceneManager.MoveGameObjectToScene(root, scene);
            Transform system = CreateChild(root.transform,
                "01货到人拣选电商仓储实施安装与调试实训系统");
            Transform module = CreateChild(system, "09机器人入场");
            Transform legacyRoot = CreateChild(module, "ChangJing-8");
            GameObject drill = CreateChild(legacyRoot, "ShouDianZuan-JQR").gameObject;
            Rigidbody body = drill.AddComponent<Rigidbody>();
            PrepareDynamicBody(body);

            Type bootstrapType = typeof(TexCore_BackpackPhysics).Assembly
                .GetType("TexCore_M09DrillPhysicsBootstrap");
            Assert.That(bootstrapType, Is.Not.Null,
                "M09 needs a runtime stabilizer for its separate drill source.");

            MethodInfo stabilize = bootstrapType.GetMethod(
                "StabilizeSceneDrill", BindingFlags.Public | BindingFlags.Static);
            Assert.That(stabilize, Is.Not.Null,
                "The M09 drill stabilizer must be callable for a loaded scene.");

            bool stabilized = (bool)stabilize.Invoke(null, new object[] { scene });

            Assert.That(stabilized, Is.True);
            Assert.That(body.isKinematic, Is.True);
            Assert.That(body.useGravity, Is.False);
            Assert.That(body.velocity, Is.EqualTo(Vector3.zero));
            Assert.That(body.angularVelocity, Is.EqualTo(Vector3.zero));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        return child.transform;
    }

    private static void PrepareDynamicBody(Rigidbody body)
    {
        body.isKinematic = false;
        body.useGravity = true;
        body.velocity = new Vector3(1f, 2f, 3f);
        body.angularVelocity = new Vector3(4f, 5f, 6f);
    }
}
