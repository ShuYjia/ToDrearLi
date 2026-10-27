// Copyright (c) Mixed Reality Toolkit Contributors
// Licensed under the BSD 3-Clause

using MixedReality.Toolkit.Input;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MixedReality.Toolkit.Examples.Build
{
    /// <summary>
    /// A static class that provides functions for compiling the example scenes via command line.
    /// </summary>
    public static class BuildApp
    {
        private static string[] scenes =
        {
            "Assets/APP/MRTKSamples/Scenes/BoundsControlExamples.unity",
            "Assets/APP/MRTKSamples/Scenes/CanvasExample.unity",
            "Assets/APP/MRTKSamples/Scenes/CanvasUITearsheet.unity",
            "Assets/APP/MRTKSamples/Scenes/ClippingExamples.unity",
            "Assets/APP/MRTKSamples/Scenes/ClippingInstancedExamples.unity",
            "Assets/APP/MRTKSamples/Scenes/DiagnosticsDemo.unity",
            "Assets/APP/MRTKSamples/Scenes/DialogExample.unity",
            "Assets/APP/MRTKSamples/Scenes/DictationExample.unity",
            "Assets/APP/MRTKSamples/Scenes/DirectionalIndicatorExample(指引).unity",
            "Assets/APP/MRTKSamples/Scenes/DisableInteractorsExample.unity",
            "Assets/APP/MRTKSamples/Scenes/DwellExample.unity",
            "Assets/APP/MRTKSamples/Scenes/EyeGazeExample.unity",
            "Assets/APP/MRTKSamples/Scenes/FontIconExample.unity",
            "Assets/APP/MRTKSamples/Scenes/HandInteractionExamples.unity",
            "Assets/APP/MRTKSamples/Scenes/HandMenuExamples.unity",
            "Assets/APP/MRTKSamples/Scenes/InputFieldExamples.unity",
            "Assets/APP/MRTKSamples/Scenes/InteractableButtonExamples.unity",
            "Assets/APP/MRTKSamples/Scenes/LegacyConstraintsExample.unity",
            "Assets/APP/MRTKSamples/Scenes/MagicWindowExample.unity",
            "Assets/APP/MRTKSamples/Scenes/NearMenuExamples.unity",
            "Assets/APP/MRTKSamples/Scenes/NonCanvasDialogExample.unity",
            "Assets/APP/MRTKSamples/Scenes/NonCanvasObjectBarExample.unity",
            "Assets/APP/MRTKSamples/Scenes/NonCanvasUIBackplateExample.unity",
            "Assets/APP/MRTKSamples/Scenes/NonCanvasUITearSheet.unity",
            "Assets/APP/MRTKSamples/Scenes/OutlineExamples.unity",
            "Assets/APP/MRTKSamples/Scenes/PerformanceEvaluation.unity",
            "Assets/APP/MRTKSamples/Scenes/SeeItSayItExample.unity",
            "Assets/APP/MRTKSamples/Scenes/SlateDrawingExample.unity",
            "Assets/APP/MRTKSamples/Scenes/SolverExamples.unity",
            "Assets/APP/MRTKSamples/Scenes/SpatialMappingExample.unity",
            "Assets/APP/MRTKSamples/Scenes/SpeechInputExamples.unity",
            "Assets/APP/MRTKSamples/Scenes/TapToPlaceExample.unity",
            "Assets/APP/MRTKSamples/Scenes/TextPrefabExamples.unity",
            "Assets/APP/MRTKSamples/Scenes/TextToSpeechExamples.unity",
            "Assets/APP/MRTKSamples/Scenes/ToggleCollectionExample.unity",
            "Assets/APP/MRTKSamples/Scenes/TopNavigationExample.unity",
            "Assets/APP/MRTKSamples/Scenes/VanillaUGUIExample.unity",
            "Assets/APP/MRTKSamples/Scenes/Audio/AudioLoFiExample.unity",
            "Assets/APP/MRTKSamples/Scenes/Audio/AudioOcclusionExample.unity",
            "Assets/APP/MRTKSamples/Scenes/Experimental/NonNativeKeyboard.unity",
            "Assets/APP/MRTKSamples/Scenes/Experimental/ScrollingExample.unity",
            "Assets/APP/MRTKSamples/Scenes/Experimental/SpatialMouseSample.unity",
            "Assets/APP/MRTKSamples/Scenes/Experimental/VirtualizedScrollRectList.unity",
            "Assets/APP/MRTKSamples/Scenes/EyeTracking/EyeTrackingBasicSetupExample.unity",
            "Assets/APP/MRTKSamples/Scenes/EyeTracking/EyeTrackingExampleNavigationExample.unity",
            "Assets/APP/MRTKSamples/Scenes/EyeTracking/EyeTrackingTargetPositioningExample.unity",
            "Assets/APP/MRTKSamples/Scenes/EyeTracking/EyeTrackingTargetSelectionExample.unity",
            "Assets/APP/MRTKSamples/Scenes/EyeTracking/EyeTrackingVisualizerExample.unity"
        };

        private static string buildPath = "build";
        private static bool exportAndroidProject = false;
        private static bool hasSceneListOverride = false;
        private const string PureProbeDefine = "TEXCORE_PURE_PROBE_BUILD";
        private static readonly string[] PureProbeScenes =
        {
            "Assets/App/Shared/Scenes/Tests/PicoMrMinimal_20260610.unity"
        };

        public static void StartBpfCommandLineBuild()
        {
            ParseBuildCommandLine();

            PreparePicoMixedRealityBuild();

            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Debug.Log($"Starting BPF command line build for {EditorUserBuildSettings.activeBuildTarget}...");

            var enabledBuildScenes = GetEnabledBuildScenes();
            var buildScenes = GetRequestedBuildScenesOrDefault(enabledBuildScenes);
            if (buildScenes.Length == 0)
            {
                Debug.LogError("Build Failed! No enabled scenes found in EditorBuildSettings.");
                EditorApplication.Exit(1);
                return;
            }

            BuildPlayer(buildScenes);
        }

        private static void PreparePicoMixedRealityBuild()
        {
            Type configuratorType = Type.GetType("App.Editor.Pico.BpfPicoMixedRealityConfigurator, Assembly-CSharp-Editor");
            if (configuratorType == null)
            {
                Debug.LogWarning("PICO MR build preflight skipped: configurator type not found.");
                return;
            }

            var prepareMethod = configuratorType.GetMethod(
                "PrepareForAndroidBuild",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (prepareMethod == null)
            {
                Debug.LogWarning("PICO MR build preflight skipped: PrepareForAndroidBuild not found.");
                return;
            }

            prepareMethod.Invoke(null, null);
        }

        /// <summary>
        /// Build the Unity project's example scenes.
        /// </summary>
        public static void StartCommandLineBuild()
        {
            ParseBuildCommandLine();

            // We don't need stack traces on all our logs. Makes things a lot easier to read.
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Debug.Log($"Starting command line build for {EditorUserBuildSettings.activeBuildTarget}...");

            BuildPlayer(scenes);
        }

        public static void StartPureProbeCommandLineBuild()
        {
            ParseBuildCommandLine();

            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Debug.Log($"Starting pure probe command line build for {EditorUserBuildSettings.activeBuildTarget}...");

            PreparePicoMixedRealityBuild();
            var buildScenes = GetRequestedBuildScenesOrDefault(PureProbeScenes);
            BuildWithTemporaryDefine(PureProbeDefine, () => BuildPlayer(buildScenes));
        }

        /// <summary>
        /// Ensure that the Text Mesh Pro assets are included in the Unity project.
        /// </summary>
        /// <remarks>
        /// This is currently not functioning correctly. When running via command line,
        /// the assets are imported, but are not available in the built application.
        /// </remarks>
        public static void EnsureTMPro()
        {
            string assetsFullPath = Path.GetFullPath("Assets/TextMesh Pro");
            if (Directory.Exists(assetsFullPath))
            {
                Debug.Log("TMPro assets folder already imported. Skipping import.");
                return;
            }

            // Import the TMP Essential Resources package
            string packageFullPath = Path.GetFullPath("Packages/com.unity.textmeshpro");
            if (Directory.Exists(packageFullPath))
            {
                Debug.Log("Importing TextMesh Pro...");
                AssetDatabase.ImportPackage(packageFullPath + "/Package Resources/TMP Essential Resources.unitypackage", false);
            }
            else
            {
                Debug.LogError("Unable to locate the Text Mesh Pro package.");
            }
        }

        private static void ParseBuildCommandLine()
        {
            string[] arguments = Environment.GetCommandLineArgs();

            exportAndroidProject = false;
            hasSceneListOverride = false;

            for (int i = 0; i < arguments.Length; ++i)
            {
                switch (arguments[i])
                {
                    case "-sceneList":
                        scenes = SplitSceneList(arguments[++i]);
                        hasSceneListOverride = true;
                        break;
                    case "-buildOutput":
                        buildPath = arguments[++i];
                        break;
                    case "-debug":
                        // Add hand joints to hand visualization for debugging purposes
                        PatchDebugHands();
                        break;
                    case "-androidExportProject":
                        exportAndroidProject = true;
                        break;
                }
            }
        }

        private static string[] SplitSceneList(string sceneList)
        {
            return (from scene in sceneList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    select scene.Trim()).ToArray();
        }

        private static string[] GetEnabledBuildScenes()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
        }

        private static string[] GetRequestedBuildScenesOrDefault(string[] fallbackScenes)
        {
            return hasSceneListOverride ? scenes : fallbackScenes;
        }

        private static void BuildPlayer(string[] buildScenes)
        {
            BuildOptions buildOptions = BuildOptions.None;
            string locationPathName = buildPath;
            if (ShouldExportAndroidGradleProject())
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
                locationPathName = GetAndroidGradleProjectPath(buildPath);
                Debug.Log($"Exporting Android Gradle project to {locationPathName}");
            }
            else
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            }

            BuildPlayerOptions options = new BuildPlayerOptions()
            {
                scenes = buildScenes,
                locationPathName = locationPathName,
                targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup,
                target = EditorUserBuildSettings.activeBuildTarget,
                options = buildOptions,
            };

            bool success;
            try
            {
                BuildReport buildReport = BuildPipeline.BuildPlayer(options);
                success = buildReport != null && buildReport.summary.result == BuildResult.Succeeded;
            }
            catch (Exception e)
            {
                Debug.LogError($"Build Failed!\n{e.Message}\n{e.StackTrace}");
                success = false;
            }

            Debug.Log($"Finished build... Build success? {success}");
            if (success && ShouldExportAndroidGradleProject())
            {
                Debug.Log($"Android Gradle project ready at {locationPathName}");
            }

            EditorApplication.Exit(success ? 0 : 1);
        }

        private static void BuildWithTemporaryDefine(string define, Action buildAction)
        {
            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string originalDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            string updatedDefines = AppendDefine(originalDefines, define);

            try
            {
                if (!string.Equals(originalDefines, updatedDefines, StringComparison.Ordinal))
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, updatedDefines);
                }

                buildAction();
            }
            finally
            {
                if (!string.Equals(originalDefines, updatedDefines, StringComparison.Ordinal))
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, originalDefines);
                }
            }
        }

        private static string AppendDefine(string existingDefines, string define)
        {
            if (string.IsNullOrWhiteSpace(define))
            {
                return existingDefines ?? string.Empty;
            }

            var defineSet = new HashSet<string>(
                (existingDefines ?? string.Empty)
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim())
                    .Where(item => !string.IsNullOrEmpty(item)),
                StringComparer.Ordinal);

            defineSet.Add(define);
            return string.Join(";", defineSet);
        }

        private static bool ShouldExportAndroidGradleProject()
        {
            return exportAndroidProject && EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
        }

        private static string GetAndroidGradleProjectPath(string requestedOutputPath)
        {
            if (!Path.HasExtension(requestedOutputPath))
            {
                return requestedOutputPath;
            }

            string directory = Path.GetDirectoryName(requestedOutputPath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(requestedOutputPath);

            if (string.IsNullOrEmpty(directory))
            {
                directory = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Android");
            }

            if (string.IsNullOrEmpty(fileNameWithoutExtension))
            {
                fileNameWithoutExtension = "BpfPico";
            }

            return Path.Combine(directory, fileNameWithoutExtension + "_Gradle");
        }

        #region Hand Debug Patching

        private const string LeftHandVisualizerGuid = "2b468cc4fe6d2b44ebc53b958b38b91a";
        private const string RightHandVisualizerGuid = "da93d751ddc0f64468dfc02f18d02d00";
        private const string HandJointMaterialGuid = "f115122e8379c044faecfec013fda057";

        [MenuItem("Mixed Reality/MRTK3/Examples/Patch debug hand visualization...")]
        private static void PatchDebugHands() => PatchHands(true);

        [MenuItem("Mixed Reality/MRTK3/Examples/Patch debug hand visualization...", true)]
        private static bool ValidatePatchDebugHands() => !AreHandsPatched();

        [MenuItem("Mixed Reality/MRTK3/Examples/Unpatch debug hand visualization...")]
        private static void UnpatchDebugHands() => PatchHands(false);

        [MenuItem("Mixed Reality/MRTK3/Examples/Unpatch debug hand visualization...", true)]
        private static bool ValidateUnpatchDebugHands() => AreHandsPatched();

        /// <summary>
        /// Checks both hand prefabs for a <see cref="HandJointVisualizer"/>.
        /// </summary>
        /// <returns>Whether the left and right hands both have <see cref="HandJointVisualizer"/> scripts.</returns>
        private static bool AreHandsPatched()
        {
            bool isPatched = true;

            string rightHandPath = AssetDatabase.GUIDToAssetPath(RightHandVisualizerGuid);
            {
                GameObject rightHandVisualizer = PrefabUtility.LoadPrefabContents(rightHandPath);
                if (rightHandVisualizer != null)
                {
                    isPatched &= rightHandVisualizer.TryGetComponent<HandJointVisualizer>(out _);
                }
                PrefabUtility.UnloadPrefabContents(rightHandVisualizer);
            }

            string leftHandPath = AssetDatabase.GUIDToAssetPath(LeftHandVisualizerGuid);
            {
                GameObject leftHandVisualizer = PrefabUtility.LoadPrefabContents(leftHandPath);
                if (leftHandVisualizer != null)
                {
                    isPatched &= leftHandVisualizer.TryGetComponent<HandJointVisualizer>(out _);
                }
                PrefabUtility.UnloadPrefabContents(leftHandVisualizer);
            }

            return isPatched;
        }

        /// <summary>
        /// Updates both the left and right hand prefabs with <see cref="HandJointVisualizer"/> scripts.
        /// </summary>
        /// <param name="addDebug">If <see langword="true"/>, <see cref="HandJointVisualizer"/> will be added. If <see langword="false"/>, it'll be removed.</param>
        private static void PatchHands(bool addDebug)
        {
            string rightHandPath = AssetDatabase.GUIDToAssetPath(RightHandVisualizerGuid);
            {
                GameObject rightHandVisualizer = PrefabUtility.LoadPrefabContents(rightHandPath);
                if (rightHandVisualizer != null)
                {
                    if (addDebug)
                    {
                        HandJointVisualizer visualizer = rightHandVisualizer.EnsureComponent<HandJointVisualizer>();
                        visualizer.HandNode = UnityEngine.XR.XRNode.RightHand;
                        visualizer.JointMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(HandJointMaterialGuid));
                        visualizer.JointMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                    }
                    else if (rightHandVisualizer.TryGetComponent(out HandJointVisualizer handJointVisualizer))
                    {
                        UnityEngine.Object.DestroyImmediate(handJointVisualizer);
                    }
                    PrefabUtility.SaveAsPrefabAsset(rightHandVisualizer, rightHandPath);
                }
                PrefabUtility.UnloadPrefabContents(rightHandVisualizer);
            }

            string leftHandPath = AssetDatabase.GUIDToAssetPath(LeftHandVisualizerGuid);
            {
                GameObject leftHandVisualizer = PrefabUtility.LoadPrefabContents(leftHandPath);
                if (leftHandVisualizer != null)
                {
                    if (addDebug)
                    {
                        HandJointVisualizer visualizer = leftHandVisualizer.EnsureComponent<HandJointVisualizer>();
                        visualizer.HandNode = UnityEngine.XR.XRNode.LeftHand;
                        visualizer.JointMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(HandJointMaterialGuid));
                        visualizer.JointMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                    }
#if UNITY_6000_0_OR_NEWER
                    else
                    {
                        PrefabUtility.RemoveUnusedOverrides(new[] { leftHandVisualizer }, UnityEditor.InteractionMode.UserAction);
                    }
#endif
                    PrefabUtility.SaveAsPrefabAsset(leftHandVisualizer, leftHandPath);
                }
                PrefabUtility.UnloadPrefabContents(leftHandVisualizer);
            }
        }

        #endregion Hand Debug Patching
    }
}
