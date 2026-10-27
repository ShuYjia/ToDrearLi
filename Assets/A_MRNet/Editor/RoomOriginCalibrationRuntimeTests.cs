using System.Reflection;
using App.Runtime.RoomCalibration;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.UX;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public sealed class RoomOriginCalibrationRuntimeTests
{
    private static readonly Vector3 StoredPosition = new Vector3(1.25f, 0f, -2.5f);
    private const float StoredYaw = 37f;
    private GameObject createdButtonsCanvas;

    [SetUp]
    public void SetUp()
    {
        createdButtonsCanvas = null;
        DestroyRuntimeObjects();
        RoomOriginCalibrationSession.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        DestroyRuntimeObjects();
        if (createdButtonsCanvas != null)
        {
            Object.DestroyImmediate(createdButtonsCanvas);
        }

        RoomOriginCalibrationSession.Clear();
    }

    [Test]
    public void BeginCalibration_PreservesExistingSessionUntilConfirmation()
    {
        RoomOriginCalibrationSession.Store(StoredPosition, StoredYaw);
        RoomOriginCalibrationManager manager = CreateManager();

        manager.BeginCalibration();

        Assert.That(RoomOriginCalibrationSession.IsCalibrated, Is.True);
        Assert.That(RoomOriginCalibrationSession.OriginPosition, Is.EqualTo(StoredPosition));
        Assert.That(RoomOriginCalibrationSession.OriginYawDegrees, Is.EqualTo(StoredYaw));
    }

    [Test]
    public void StartupCalibration_UsesAVisibleDelayBeforeAutoTrigger()
    {
        FieldInfo delayField = typeof(RoomOriginCalibrationManager).GetField(
            "startupCalibrationDelaySeconds",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(delayField, Is.Not.Null);
        float delaySeconds = (float)delayField.GetValue(CreateManager());
        Assert.That(delaySeconds, Is.GreaterThanOrEqualTo(2f));
    }

    [Test]
    public void BeginCalibration_CentersConfirmButtonInsideSafePanelBounds()
    {
        RoomOriginCalibrationManager manager = CreateManager();
        manager.BeginCalibration();

        GameObject panelObject = FindRuntimeObject("Room Origin Calibration Panel");
        Assert.That(panelObject, Is.Not.Null);

        Transform confirmTransform = panelObject.transform.Find("Confirm Calibration Button");
        Assert.That(confirmTransform, Is.Not.Null);

        RectTransform rect = confirmTransform.GetComponent<RectTransform>();
        Assert.That(rect, Is.Not.Null);
        Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(rect.anchoredPosition.x, Is.EqualTo(0f));
        Assert.That(rect.sizeDelta.x, Is.GreaterThanOrEqualTo(360f));
    }

    [Test]
    public void BeginCalibration_UsesWorldSpaceCanvasWithHeadFollowAndTrackedRaycaster()
    {
        RoomOriginCalibrationManager manager = CreateManager();
        manager.BeginCalibration();

        GameObject canvasObject = FindRuntimeObject("Room Origin Calibration Canvas");
        Assert.That(canvasObject, Is.Not.Null);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        Assert.That(canvas, Is.Not.Null);
        Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.WorldSpace));
        Assert.That(canvasObject.GetComponent<TrackedDeviceGraphicRaycaster>(), Is.Not.Null);

        SolverHandler solverHandler = canvasObject.GetComponent<SolverHandler>();
        Assert.That(solverHandler, Is.Not.Null);
        Assert.That(solverHandler.TrackedTargetType, Is.EqualTo(TrackedObjectType.Head));
        Assert.That(solverHandler.enabled, Is.False);

        Follow follow = canvasObject.GetComponent<Follow>();
        Assert.That(follow, Is.Not.Null);
        Assert.That(follow.MinDistance, Is.EqualTo(0.9f));
        Assert.That(follow.DefaultDistance, Is.EqualTo(1.1f));
        Assert.That(follow.MaxDistance, Is.EqualTo(1.4f));
        Assert.That(follow.enabled, Is.False);
    }

    [Test]
    public void BeginCalibration_UsesTransparentPanelWithCenteredReticleAndGuidanceText()
    {
        RoomOriginCalibrationManager manager = CreateManager();
        manager.BeginCalibration();

        GameObject panelObject = FindRuntimeObject("Room Origin Calibration Panel");
        Assert.That(panelObject, Is.Not.Null);
        Assert.That(panelObject.GetComponent<Image>(), Is.Null);

        Transform reticleObject = panelObject.transform.Find("Calibration Reticle");
        Assert.That(reticleObject, Is.Not.Null);

        RectTransform reticleRect = reticleObject.GetComponent<RectTransform>();
        Assert.That(reticleRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(reticleRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(reticleRect.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(reticleObject.Find("Horizontal"), Is.Not.Null);
        Assert.That(reticleObject.Find("Vertical"), Is.Not.Null);
        Assert.That(reticleObject.Find("Horizontal").GetComponent<Image>().raycastTarget, Is.False);
        Assert.That(reticleObject.Find("Vertical").GetComponent<Image>().raycastTarget, Is.False);

        Assert.That(panelObject.transform.Find("站到地面标记位置，并面向箭头方向。"), Is.Not.Null);
        Assert.That(panelObject.transform.Find("等待头显追踪..."), Is.Not.Null);
    }

    [Test]
    public void BusinessScene_BindsRecalibrationToButtonsCanvasListItem6()
    {
        Assert.That(SceneManager.GetActiveScene().name, Is.Not.EqualTo(TexCore_LoadScene.StartupSceneName));
        EnsureButtonsCanvasFixture();

        RoomOriginCalibrationManager manager = CreateManager();
        InvokePrivate(manager, "HandleActiveSceneChanged");

        GameObject buttonObject = FindRuntimeObject("List Item (6)");
        Assert.That(buttonObject, Is.Not.Null);
        Assert.That(buttonObject.activeInHierarchy, Is.True);
        Assert.That(buttonObject.transform.parent.name, Is.EqualTo("ParentObject"));
        Assert.That(buttonObject.transform.parent.parent.name, Is.EqualTo("ListItems"));
        Assert.That(buttonObject.transform.parent.parent.parent.name, Is.EqualTo("Buttons Canvas"));
        Assert.That(buttonObject.GetComponent<PressableButton>(), Is.Not.Null);

        TMP_Text label = FindButtonLabel(buttonObject);
        Assert.That(label, Is.Not.Null);
        Assert.That(label.text, Does.Contain("重新校准原点"));
    }

    [Test]
    public void BusinessScene_EnsuresRuntimeAndCreatesRecalibrationButtonFromButtonsCanvasEntryPoint()
    {
        EnsureButtonsCanvasFixture();

        MethodInfo ensureRuntimeMethod = typeof(RoomOriginCalibrationManager).GetMethod(
            "EnsureRuntimeForActiveScene",
            BindingFlags.Public | BindingFlags.Static);

        Assert.That(ensureRuntimeMethod, Is.Not.Null);

        RoomOriginCalibrationManager manager =
            (RoomOriginCalibrationManager)ensureRuntimeMethod.Invoke(null, null);
        Assert.That(manager, Is.Not.Null);

        GameObject buttonObject = FindRuntimeObject("List Item (6)");
        Assert.That(buttonObject, Is.Not.Null);
        Assert.That(buttonObject.transform.parent.name, Is.EqualTo("ParentObject"));
        Assert.That(buttonObject.activeInHierarchy, Is.True);
        Assert.That(FindButtonLabel(buttonObject).text, Does.Contain("重新校准原点"));

        buttonObject.GetComponent<PressableButton>().OnClicked.Invoke();

        GameObject panelObject = FindRuntimeObject("Room Origin Calibration Panel");
        Assert.That(panelObject, Is.Not.Null);
        Assert.That(panelObject.activeSelf, Is.True);
    }

    [Test]
    public void BusinessScene_RebindsRecalibrationButtonAfterSceneRestore()
    {
        EnsureButtonsCanvasFixture();
        RoomOriginCalibrationManager manager = CreateManager();
        InvokePrivate(manager, "HandleActiveSceneChanged");

        GameObject buttonObject = FindRuntimeObject("List Item (6)");
        PressableButton button = buttonObject.GetComponent<PressableButton>();
        button.OnClicked.RemoveAllListeners();

        InvokePrivate(manager, "HandleActiveSceneChanged");
        button.OnClicked.Invoke();

        GameObject panelObject = FindRuntimeObject("Room Origin Calibration Panel");
        Assert.That(panelObject, Is.Not.Null);
        Assert.That(panelObject.activeSelf, Is.True);
    }

    private void EnsureButtonsCanvasFixture()
    {
        GameObject existingCanvas = FindRuntimeObject("Buttons Canvas");
        if (existingCanvas != null && existingCanvas.scene == SceneManager.GetActiveScene())
        {
            return;
        }

        createdButtonsCanvas = new GameObject("Buttons Canvas", typeof(RectTransform), typeof(Canvas));
        createdButtonsCanvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

        GameObject listItems = new GameObject("ListItems", typeof(RectTransform));
        listItems.transform.SetParent(createdButtonsCanvas.transform, false);

        GameObject parentObject = new GameObject("ParentObject", typeof(RectTransform));
        parentObject.transform.SetParent(listItems.transform, false);

        GameObject template = new GameObject("List Item (6)", typeof(RectTransform));
        template.transform.SetParent(parentObject.transform, false);
        template.AddComponent<PressableButton>();

        GameObject labelObject = new GameObject("Text", typeof(RectTransform));
        labelObject.transform.SetParent(template.transform, false);
        labelObject.AddComponent<TextMeshProUGUI>().text = "Template";
    }

    [Test]
    public void CancelCalibration_KeepsPreviousSessionAndHidesCalibrationPanel()
    {
        RoomOriginCalibrationSession.Store(StoredPosition, StoredYaw);
        RoomOriginCalibrationManager manager = CreateManager();
        manager.BeginCalibration();

        MethodInfo cancelMethod = typeof(RoomOriginCalibrationManager).GetMethod(
            "CancelCalibration",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.That(cancelMethod, Is.Not.Null);
        cancelMethod.Invoke(manager, null);

        Assert.That(RoomOriginCalibrationSession.IsCalibrated, Is.True);
        Assert.That(RoomOriginCalibrationSession.OriginPosition, Is.EqualTo(StoredPosition));
        Assert.That(RoomOriginCalibrationSession.OriginYawDegrees, Is.EqualTo(StoredYaw));

        GameObject panelObject = FindRuntimeObject("Room Origin Calibration Panel");
        Assert.That(panelObject, Is.Not.Null);
        Assert.That(panelObject.activeSelf, Is.False);
    }

    private static RoomOriginCalibrationManager CreateManager()
    {
        GameObject managerObject = new GameObject("Room Origin Calibration Test Manager");
        return managerObject.AddComponent<RoomOriginCalibrationManager>();
    }

    private static void InvokePrivate(RoomOriginCalibrationManager manager, string methodName)
    {
        MethodInfo method = typeof(RoomOriginCalibrationManager).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        method.Invoke(manager, null);
    }

    private static GameObject FindRuntimeObject(string objectName)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];
            if (candidate != null &&
                candidate.name == objectName &&
                candidate.scene.IsValid())
            {
                return candidate;
            }
        }

        return null;
    }

    private static TMP_Text FindButtonLabel(GameObject buttonObject)
    {
        TMP_Text[] labels = buttonObject.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null && labels[i].gameObject.name == "Text")
            {
                return labels[i];
            }
        }

        return null;
    }

    private static void DestroyRuntimeObjects()
    {
        RoomOriginCalibrationManager[] managers =
            Resources.FindObjectsOfTypeAll<RoomOriginCalibrationManager>();
        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] != null && managers[i].gameObject.scene.IsValid())
            {
                Object.DestroyImmediate(managers[i].gameObject);
            }
        }

        DestroyNamedObject("Room Origin Calibration Canvas");
        DestroyNamedObject("Room Origin Calibration Panel");
        DestroyNamedObject("Room Origin Recalibration Button");
    }

    private static void DestroyNamedObject(string objectName)
    {
        GameObject target = FindRuntimeObject(objectName);
        if (target != null)
        {
            Object.DestroyImmediate(target);
        }
    }
}
