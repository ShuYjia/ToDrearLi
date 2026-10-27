using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class TexCore_RackAssemblyControllerTests
{
    [Test]
    public void InstantiatePreservingWorldScale_KeepsTemplateWorldSizeAfterReparenting()
    {
        GameObject sourceParent = new GameObject("Source Parent");
        GameObject destinationParent = new GameObject("Destination Parent");

        try
        {
            sourceParent.transform.localScale = Vector3.one * 10f;
            destinationParent.transform.localScale = Vector3.one;

            GameObject templateObject = new GameObject("Template");
            Transform template = templateObject.transform;
            template.SetParent(sourceParent.transform, false);
            template.localPosition = new Vector3(0.2f, 0.3f, 0.4f);
            template.localRotation = Quaternion.Euler(10f, 20f, 30f);
            template.localScale = Vector3.one;

            MethodInfo method = typeof(TexCore_RackAssemblyController).GetMethod(
                "InstantiatePreservingWorldScale",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null,
                "The rack assembly spawner must preserve the template world scale when changing parents.");

            GameObject spawned = (GameObject)method.Invoke(
                null,
                new object[] { template, destinationParent.transform });

            Assert.That(Vector3.Distance(spawned.transform.position, template.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(spawned.transform.rotation, template.rotation), Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(spawned.transform.lossyScale, template.lossyScale), Is.LessThan(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(sourceParent);
            Object.DestroyImmediate(destinationParent);
        }
    }

    [Test]
    public void DisplayWallStep_CreatesUnsavedGuidanceForActivePlacementTarget()
    {
        string targetName = $"DisplayWallTarget_{System.Guid.NewGuid():N}";
        GameObject controllerObject = new GameObject("DisplayWallController");
        GameObject targetObject = new GameObject(targetName);

        try
        {
            targetObject.AddComponent<BoxCollider>();
            TexCore_RackAssemblyController controller = controllerObject.AddComponent<TexCore_RackAssemblyController>();
            controller.SetSpawnedObjectPrefix("07");
            controller.SetStepDefinitions(new List<RackAssemblyStepDefinition>
            {
                new RackAssemblyStepDefinition
                {
                    kind = RackAssemblyStepKind.PlacePart,
                    partKey = "测试部件",
                    blueTargets = new List<Transform> { targetObject.transform }
                }
            });

            controller.ActivateStep(0, null);

            GameObject guidance = FindGuidanceObjectForTarget(targetName);
            Assert.That(guidance, Is.Not.Null);
            Assert.That(guidance.activeInHierarchy, Is.True);
            Assert.That(guidance.hideFlags & HideFlags.DontSave, Is.EqualTo(HideFlags.DontSave));
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(targetObject);
        }
    }

    [Test]
    public void RackStep_CreatesGuidanceFor06Controller()
    {
        string targetName = $"RackTarget_{System.Guid.NewGuid():N}";
        GameObject controllerObject = new GameObject("RackController");
        GameObject targetObject = new GameObject(targetName);

        try
        {
            targetObject.AddComponent<BoxCollider>();
            TexCore_RackAssemblyController controller = controllerObject.AddComponent<TexCore_RackAssemblyController>();
            controller.SetSpawnedObjectPrefix("06");
            controller.SetStepDefinitions(new List<RackAssemblyStepDefinition>
            {
                new RackAssemblyStepDefinition
                {
                    kind = RackAssemblyStepKind.PlacePart,
                    partKey = "测试部件",
                    blueTargets = new List<Transform> { targetObject.transform }
                }
            });

            controller.ActivateStep(0, null);

            GameObject guidance = FindGuidanceObjectForTarget(targetName);
            Assert.That(guidance, Is.Not.Null);
            Assert.That(guidance.activeInHierarchy, Is.True);
            Assert.That(guidance.hideFlags & HideFlags.DontSave, Is.EqualTo(HideFlags.DontSave));
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(targetObject);
        }
    }

    [Test]
    public void RackMoveStep_CreatesGuidanceFor06Controller()
    {
        string targetName = $"RackMoveTarget_{System.Guid.NewGuid():N}";
        GameObject controllerObject = new GameObject("RackMoveController");
        GameObject targetObject = new GameObject(targetName);

        try
        {
            targetObject.AddComponent<BoxCollider>();
            TexCore_RackAssemblyController controller = controllerObject.AddComponent<TexCore_RackAssemblyController>();
            controller.SetSpawnedObjectPrefix("06");
            controller.SetStepDefinitions(new List<RackAssemblyStepDefinition>
            {
                new RackAssemblyStepDefinition
                {
                    kind = RackAssemblyStepKind.MoveAssembly,
                    moveTarget = targetObject.transform
                }
            });

            controller.ActivateStep(0, null);

            GameObject guidance = FindGuidanceObjectForTarget(targetName);
            Assert.That(guidance, Is.Not.Null);
            Assert.That(guidance.activeInHierarchy, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(targetObject);
        }
    }

    [Test]
    public void RackStep_DoesNotCreateGuidanceForNon0607Controller()
    {
        string targetName = $"OtherRackTarget_{System.Guid.NewGuid():N}";
        GameObject controllerObject = new GameObject("OtherRackController");
        GameObject targetObject = new GameObject(targetName);

        try
        {
            targetObject.AddComponent<BoxCollider>();
            TexCore_RackAssemblyController controller = controllerObject.AddComponent<TexCore_RackAssemblyController>();
            controller.SetSpawnedObjectPrefix("08");
            controller.SetStepDefinitions(new List<RackAssemblyStepDefinition>
            {
                new RackAssemblyStepDefinition
                {
                    kind = RackAssemblyStepKind.PlacePart,
                    partKey = "测试部件",
                    blueTargets = new List<Transform> { targetObject.transform }
                }
            });

            controller.ActivateStep(0, null);

            Assert.That(FindGuidanceObjectForTarget(targetName), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(targetObject);
        }
    }

    [Test]
    public void ApplyTargetCompletion_UsesSpawnedPartMaterialsForTheResultObject()
    {
        GameObject controllerObject = new GameObject("RackMaterialController");
        GameObject sourceRoot = new GameObject("SpawnedPart");
        GameObject resultRoot = new GameObject("CompletedPart");
        GameObject blueTarget = new GameObject("BlueTarget");
        Material sourceMaterial = null;
        Material resultMaterial = null;

        try
        {
            GameObject sourceRendererObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sourceRendererObject.name = "Body";
            sourceRendererObject.transform.SetParent(sourceRoot.transform, false);
            GameObject resultRendererObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            resultRendererObject.name = "CompletedBody";
            resultRendererObject.transform.SetParent(resultRoot.transform, false);

            Shader shader = Shader.Find("Standard");
            Assert.That(shader, Is.Not.Null);
            sourceMaterial = new Material(shader) { color = Color.green };
            resultMaterial = new Material(shader) { color = Color.red };
            sourceRendererObject.GetComponent<Renderer>().sharedMaterial = sourceMaterial;
            resultRendererObject.GetComponent<Renderer>().sharedMaterial = resultMaterial;
            resultRoot.SetActive(false);

            RackAssemblyStepDefinition step = new RackAssemblyStepDefinition
            {
                kind = RackAssemblyStepKind.PlacePart,
                blueTargets = new List<Transform> { blueTarget.transform },
                resultObjects = new List<Transform> { resultRoot.transform }
            };
            TexCore_RackAssemblyController controller = controllerObject.AddComponent<TexCore_RackAssemblyController>();
            MethodInfo method = typeof(TexCore_RackAssemblyController).GetMethod(
                "ApplyTargetCompletion",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            method.Invoke(controller, new object[] { step, 0, sourceRoot.transform });

            Assert.That(step.blueTargets[0].gameObject.activeSelf, Is.False);
            Assert.That(resultRoot.activeSelf, Is.True);
            Assert.That(resultRendererObject.GetComponent<Renderer>().sharedMaterial,
                Is.SameAs(sourceMaterial));

        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(sourceRoot);
            Object.DestroyImmediate(resultRoot);
            Object.DestroyImmediate(blueTarget);
            Object.DestroyImmediate(sourceMaterial);
            Object.DestroyImmediate(resultMaterial);
        }
    }

    private static GameObject FindGuidanceObjectForTarget(string targetName)
    {
        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate != null && candidate.name.StartsWith($"{targetName}_") &&
                candidate.name.EndsWith("TargetGuidance"))
            {
                return candidate;
            }
        }

        return null;
    }
}
