using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bpf.Diagnostics;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot migration that introduces <see cref="TexCore_StepVoiceProfile"/> on
/// every topic Prefab under <c>Assets/Resources/BusinessPrefabs</c>.
///
/// Scope of this migration:
///   - create one narration slot per real step (slot count == topicData.tasks.Count)
///   - bind the only step -> narration mapping recorded in this repository, the
///     08/09 audio arrays owned by <c>TexCore_0809SceneBuilder</c>
///   - leave every other slot <c>null</c> and clear the legacy hintAudios strings
///
/// It deliberately does not guess bindings from legacy file names. Legacy assets
/// stay untouched and read-only under Assets/APP/OldMRTK; the clips that are
/// actually bound get copied into Assets/A_MRNet/Audio/StepVoice so Android can
/// load them.
/// </summary>
internal static class TexCore_StepVoiceMigration
{
    internal const string BusinessRoot =
        "Assets/Resources/BusinessPrefabs/MR混合现实多人协作平台/01货到人拣选电商仓储实施安装与调试实训系统";

    internal const string StepVoiceRoot = "Assets/A_MRNet/Audio/StepVoice";

    [MenuItem("BPF/TexCore/Migrate Step Voice")]
    private static void MigrateFromMenu()
    {
        MigrateAll();
    }

    /// <summary>
    /// Creates or refreshes the step-voice profile on every topic Prefab and
    /// reports what changed.
    /// </summary>
    internal static void MigrateAll()
    {
        string[] prefabPaths = Directory
            .GetFiles(BusinessRoot, "*.prefab", SearchOption.AllDirectories)
            .Select(value => value.Replace('\\', '/'))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        int migrated = 0;
        int slots = 0;
        int bound = 0;
        List<string> skipped = new List<string>();

        foreach (string prefabPath in prefabPaths)
        {
            string relative = prefabPath.Substring(BusinessRoot.Length + 1);
            string[] segments = relative.Split('/');
            if (segments.Length < 2)
            {
                // Module node Prefab, not a topic.
                continue;
            }

            string module = segments[0];
            string topic = Path.GetFileNameWithoutExtension(segments[segments.Length - 1]);
            string key = module + "/" + topic;

            try
            {
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    PanelTopicBase panel = root.GetComponent<PanelTopicBase>();
                    if (panel == null || panel.topicData == null || panel.topicData.tasks == null)
                    {
                        skipped.Add(relative);
                        continue;
                    }

                    int stepCount = panel.topicData.tasks.Count;
                    TexCore_StepVoiceProfile profile = root.GetComponent<TexCore_StepVoiceProfile>();
                    if (profile == null)
                    {
                        profile = root.AddComponent<TexCore_StepVoiceProfile>();
                    }

                    profile.stepAudioClips = TexCore_StepVoiceBindings.ResolveStepAudioClips(key, stepCount);
                    profile.volume = 1f;
                    ClearLegacyHintAudios(panel);

                    EditorUtility.SetDirty(profile);
                    EditorUtility.SetDirty(panel);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    migrated++;
                    slots += stepCount;
                    bound += profile.stepAudioClips.Count(value => value != null);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            catch (Exception exception)
            {
                BpfLog.Error(BpfLogCategory.Diagnostics, "tex_core_step_voice_migration.migrate_all",
                    "步骤语音迁移失败：" + relative + " | " + exception.Message);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        BpfLog.Info(BpfLogCategory.Diagnostics, "tex_core_step_voice_migration.migrate_all",
            $"步骤语音迁移完成：题目 {migrated} 个，槽位 {slots} 个，已绑定 {bound} 个。跳过 {skipped.Count} 个非题目 Prefab。");
    }

    /// <summary>
    /// Builds the narration slot list for one topic. The authored step -> audio
    /// binding lives only in <see cref="TexCore_StepVoiceBindings"/> so the scene
    /// builders and this migration can never disagree about it.
    /// </summary>
    internal static List<AudioClip> ResolveStepAudioClips(string key, int stepCount)
    {
        return TexCore_StepVoiceBindings.ResolveStepAudioClips(key, stepCount);
    }

    private static void ClearLegacyHintAudios(PanelTopicBase panel)
    {
        if (panel.topicData.hintAudios == null)
        {
            panel.topicData.hintAudios = new List<string>();
            return;
        }

        for (int i = 0; i < panel.topicData.hintAudios.Count; i++)
        {
            panel.topicData.hintAudios[i] = string.Empty;
        }
    }
}
