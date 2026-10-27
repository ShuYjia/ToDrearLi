using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

[InitializeOnLoad]
public static class ImportCheck
{
    static ImportCheck() { EditorApplication.update += AwaitImport; }

    private static void AwaitImport()
    {
        if (!SessionState.GetBool("MRImportCheck.Pending", false)) return;
        long ticks;
        long.TryParse(SessionState.GetString("MRImportCheck.Started", "0"), out ticks);
        if (DateTime.UtcNow.Ticks - ticks > TimeSpan.FromMinutes(3).Ticks)
        {
            SessionState.SetBool("MRImportCheck.Pending", false);
            Debug.LogError("Import timed out.");
            EditorApplication.Exit(1);
            return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/MRAdaptiveTraining/Runtime/Unity/AdaptiveTrainingController.cs");
        if (script == null || script.GetClass() == null) return;
        SessionState.SetBool("MRImportCheck.Pending", false);
        try { Run(); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    public static void Import()
    {
        string package = Environment.GetEnvironmentVariable("MR_TRAINING_PACKAGE");
        if (string.IsNullOrEmpty(package)) throw new Exception("MR_TRAINING_PACKAGE required.");
        SessionState.SetString("MRImportCheck.Started", DateTime.UtcNow.Ticks.ToString());
        SessionState.SetBool("MRImportCheck.Pending", true);
        AssetDatabase.ImportPackage(package, false);
        Debug.Log("MR_TRAINING_IMPORT_REQUESTED");
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/MRAdaptiveTraining/Demo/AdaptiveTrainingDemo.unity");
        MonoBehaviour controller = null;
        MonoBehaviour demo = null;
        foreach (MonoBehaviour component in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
        {
            if (component.GetType().FullName == "MRAdaptiveTraining.AdaptiveTrainingController") controller = component;
            if (component.GetType().FullName == "MRAdaptiveTraining.AdaptiveTrainingDemo") demo = component;
        }
        if (controller == null || demo == null) throw new Exception("Imported scene components missing.");
        foreach (Transform item in UnityEngine.Object.FindObjectsOfType<Transform>())
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0) throw new Exception("Missing script in imported scene.");
        SerializedObject demoObject = new SerializedObject(demo);
        if (demoObject.FindProperty("controller").objectReferenceValue != controller || demoObject.FindProperty("dashboard").objectReferenceValue == null)
            throw new Exception("Scene hookups missing.");
        if (new SerializedObject(controller).FindProperty("policy").objectReferenceValue == null) throw new Exception("Policy reference missing.");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MRAdaptiveTraining/Demo/AdaptiveTrainingDemo.prefab");
        if (prefab == null || GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab) != 0) throw new Exception("Prefab invalid.");

        Type type = controller.GetType();
        string temp = Path.Combine(Application.temporaryCachePath, "MRImportCheck_" + Guid.NewGuid().ToString("N"));
        Type storeType = type.Assembly.GetType("MRAdaptiveTraining.LocalTrainingStore");
        object isolatedStore = Activator.CreateInstance(storeType, new object[] { temp });
        type.GetField("store", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, isolatedStore);
        bool continued = false;
        ((UnityEvent)type.GetField("onRepeatRequested").GetValue(controller)).AddListener(() => type.GetMethod("BeginNextAttempt").Invoke(controller, null));
        ((UnityEvent)type.GetField("onContinueRequested").GetValue(controller)).AddListener(() => continued = true);
        try
        {
            if (!(bool)type.GetMethod("BeginTraining").Invoke(controller, new object[] { "import-test" })) throw new Exception("Begin failed.");
            type.GetMethod("CompleteAttempt").Invoke(controller, new object[] { true });
            type.GetMethod("ApplyDecision").Invoke(controller, null);
            if (!(bool)type.GetProperty("HasActiveAttempt").GetValue(controller, null)) throw new Exception("Repeat event failed.");
            type.GetMethod("CompleteAttempt").Invoke(controller, new object[] { true });
            type.GetMethod("ApplyDecision").Invoke(controller, null);
            if (!continued) throw new Exception("Continue event failed.");
            ICollection records = (ICollection)type.GetMethod("LoadRecords").Invoke(controller, null);
            if (records.Count != 2) throw new Exception("Controller did not save exactly two attempts.");
            if (type.GetProperty("LastError").GetValue(controller, null) != null) throw new Exception("Controller reported an error.");
        }
        finally
        {
            if (Directory.Exists(temp))
            {
                foreach (string file in Directory.GetFiles(temp)) File.Delete(file);
                Directory.Delete(temp);
            }
        }
        Debug.Log("MR_TRAINING_FRESH_IMPORT_OK: scene + prefab + references + controller repeat/continue + saved records.");
    }
}
