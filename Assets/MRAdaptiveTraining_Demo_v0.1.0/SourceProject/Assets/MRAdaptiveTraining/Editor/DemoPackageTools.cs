using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MRAdaptiveTraining.Editor
{
    public static class DemoPackageTools
    {
        private const string Root = "Assets/MRAdaptiveTraining";
        private const string ScenePath = Root + "/Demo/AdaptiveTrainingDemo.unity";

        [MenuItem("Tools/MR 教学/打开独立演示场景")]
        public static void OpenDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) CreateDemo();
            else EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Tools/MR 教学/检查当前场景接入")]
        public static void CheckScene()
        {
            AdaptiveTrainingController[] controllers = UnityEngine.Object.FindObjectsOfType<AdaptiveTrainingController>(true);
            if (controllers.Length == 0) Debug.LogWarning("[MR 教学] 当前场景没有 AdaptiveTrainingController。");
            foreach (AdaptiveTrainingController c in controllers)
            {
                string issue = c.policy == null ? "缺少任务规则资产" : c.policy.rules.Validate();
                if (issue != null) Debug.LogError("[MR 教学] " + c.name + "：" + issue, c);
                else Debug.Log("[MR 教学] 规则配置有效：" + c.taskId, c);
                if (!c.errorTrackingAvailable) Debug.LogWarning("[MR 教学] 错误采集尚未标记完整，将产生待核实状态。", c);
                if (!c.useDemoStorage && (c.onRepeatRequested.GetPersistentEventCount() == 0 || c.onContinueRequested.GetPersistentEventCount() == 0))
                    Debug.LogWarning("[MR 教学] Inspector 未看到重练或继续回调；如通过代码订阅，请人工核实。", c);
            }
        }

        public static void CreateDemo()
        {
            // Invoked explicitly only. Importing this package never edits the user's scene.
            TrainingPolicyAsset rules = AssetDatabase.LoadAssetAtPath<TrainingPolicyAsset>(Root + "/Demo/DemoPolicy.asset");
            if (rules == null)
            {
                rules = ScriptableObject.CreateInstance<TrainingPolicyAsset>();
                AssetDatabase.CreateAsset(rules, Root + "/Demo/DemoPolicy.asset");
            }
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Demo Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.065f, 0.10f, 0.14f);
            GameObject root = new GameObject("MR Adaptive Training Demo");
            AdaptiveTrainingController controller = root.AddComponent<AdaptiveTrainingController>();
            controller.policy = rules;
            controller.useDemoStorage = true;
            controller.errorTrackingAvailable = true;
            TeacherDashboard dashboard = root.AddComponent<TeacherDashboard>();
            dashboard.controller = controller;
            AdaptiveTrainingDemo demo = root.AddComponent<AdaptiveTrainingDemo>();
            demo.controller = controller;
            demo.dashboard = dashboard;
            EditorSceneManager.SaveScene(scene, ScenePath);
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/Demo/AdaptiveTrainingDemo.prefab");
            AssetDatabase.SaveAssets();
        }

        // Build in the isolated source project, never in the recipient's production project.
        public static void BuildDelivery()
        {
            string output = Environment.GetEnvironmentVariable("MR_TRAINING_OUTPUT");
            if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("MR_TRAINING_OUTPUT is required.");
            Directory.CreateDirectory(output);
            CreateDemo();
            RunSmokeChecks();
            AssetDatabase.ExportPackage(Root, Path.Combine(output, "MRAdaptiveTraining_Demo_v0.1.0.unitypackage"), ExportPackageOptions.Recurse);
            File.WriteAllText(Path.Combine(output, "UnityValidation.txt"), "Unity " + Application.unityVersion + "\nCompile, generated scene/prefab, JSON roundtrip and package export passed.\nHeadset, XR UI and production scene integration require manual verification.\n");
            Debug.Log("MR_TRAINING_EXPORT_OK");
        }

        private static void RunSmokeChecks()
        {
            TrainingSession session = new TrainingSession("validation-student", "validation-task", new TrainingPolicy(), true);
            session.BeginAttempt(true, DateTime.UtcNow);
            AttemptRecord first = session.CompleteAttempt(true, 12, DateTime.UtcNow);
            if (first.decision.action != TrainingAction.Repeat) throw new Exception("Baseline repeat failed.");
            session.BeginAttempt(true, DateTime.UtcNow);
            AttemptRecord second = session.CompleteAttempt(true, 14, DateTime.UtcNow);
            AttemptRecord roundtrip = JsonUtility.FromJson<AttemptRecord>(JsonUtility.ToJson(second));
            if (roundtrip.decision.state != LearningState.Proficient || roundtrip.decision.action != TrainingAction.Continue || roundtrip.policySnapshot.policyVersion != "demo-v1")
                throw new Exception("JSON / mastery smoke check failed.");
            string temporary = Path.Combine(Application.temporaryCachePath, "MRTrainingValidation_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                File.WriteAllText(temporary, JsonUtility.ToJson(roundtrip));
                if (JsonUtility.FromJson<AttemptRecord>(File.ReadAllText(temporary)).attemptId != second.attemptId)
                    throw new Exception("File roundtrip failed.");
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }

            string testDirectory = Path.Combine(Application.temporaryCachePath, "MRTrainingStoreTest_" + Guid.NewGuid().ToString("N"));
            LocalTrainingStore testStore = new LocalTrainingStore(testDirectory);
            try
            {
                testStore.Save(second);
                testStore.Save(second); // An identical retry must not duplicate a record.
                File.WriteAllText(Path.Combine(testDirectory, "broken.json"), "{bad json");
                if (testStore.LoadAll().Count != 1 || testStore.InvalidFileCount != 1)
                    throw new Exception("Store corruption / idempotent retry test failed.");
                string csv = testStore.ExportCsv();
                if (!File.ReadAllText(csv).Contains(second.attemptId)) throw new Exception("CSV export failed.");
                string originalId = second.attemptId;
                second.attemptId = "../invalid";
                bool rejected = false;
                try { testStore.Save(second); } catch (InvalidDataException) { rejected = true; }
                second.attemptId = originalId;
                if (!rejected) throw new Exception("Invalid record id was accepted.");
            }
            finally
            {
                // This unique directory is created solely by this test; never recurse into user records.
                if (Directory.Exists(testDirectory))
                {
                    foreach (string file in Directory.GetFiles(testDirectory)) File.Delete(file);
                    Directory.Delete(testDirectory);
                }
            }
        }
    }
}
