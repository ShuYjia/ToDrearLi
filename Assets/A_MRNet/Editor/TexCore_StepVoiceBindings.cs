#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Bpf.Diagnostics;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Authored per-step narration bindings, keyed by "module/topic" and then by
/// step index. These are the bindings the project already carried on
/// <c>Panel_Operate.stepAudioClips</c> before step narration moved to
/// <see cref="TexCore_StepVoiceProfile"/>; they were read back out of that
/// serialized data rather than guessed from legacy file names.
///
/// This is the single source of truth for both the scene builders and the
/// migration. A legacy file is only referenced here when the project itself
/// already bound it to that step.
/// </summary>
internal static class TexCore_StepVoiceBindings
{
    /// <summary>Legacy narration root per module, used to resolve file names.</summary>
    private static readonly Dictionary<string, string> LegacyRoots =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "01", "Assets/APP/OldMRTK/NetWork-03_2/Support/GameMain/Res/Scenes/Music/服务器网络搭建" },
            { "02", "Assets/APP/OldMRTK/WHY_FuWuQi-6/Support/GameMain/Res/Scenes/Music/服务器系统搭建" },
            { "05", "Assets/APP/OldMRTK/HuaXian/Support/GameMain/Sounds/5画线贴码" },
            { "06", "Assets/APP/OldMRTK/WHY_HJ/Support/GameMain/Res/Scenes/Music/货架安装" },
            { "07", "Assets/APP/OldMRTK/WHY_BZQ/Support/GameMain/Res/Scenes/Music/播种墙安装" },
            { "08", "Assets/APP/OldMRTK/changjing-2/Support/GameMain/Res/Scenes/Music/充电桩安装" },
            { "09", "Assets/APP/OldMRTK/ChangJing-8/Support/GameMain/Sounds/9机器人入场" },
            { "10", "Assets/APP/OldMRTK/changjing-4/Support/GameMain/Res/Scenes/Music/货架入场" },
        };

    /// <summary>
    /// Step index -> legacy narration file name, per "module/topic". A topic
    /// absent from this table has no authored narration at all.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<int, string>> Authored =
        new Dictionary<string, Dictionary<int, string>>(StringComparer.Ordinal)
        {
            // 01服务器网络搭建
            {
                "01服务器网络搭建/05服务器电源线连接机柜插座供电",
                new Dictionary<int, string>
                {
                    { 0, "1—2.MP3" },
                }
            },
            {
                "01服务器网络搭建/10机柜网线另一端连接AC",
                new Dictionary<int, string>
                {
                    { 0, "1—5.MP3" },
                }
            },
            {
                "01服务器网络搭建/12AP连接交换机",
                new Dictionary<int, string>
                {
                    { 0, "1—6.MP3" },
                }
            },
            {
                "01服务器网络搭建/13服务器网线连接交换机",
                new Dictionary<int, string>
                {
                    { 0, "1—4.MP3" },
                }
            },
            {
                "01服务器网络搭建/14显示器VGA连接",
                new Dictionary<int, string>
                {
                    { 0, "1—7.MP3" },
                }
            },
            // 06货架安装
            {
                "06货架安装/02翻倒底座粘贴二维码",
                new Dictionary<int, string>
                {
                    { 0, "6—2.MP3" },
                }
            },
            {
                "06货架安装/03安装4根立柱",
                new Dictionary<int, string>
                {
                    { 0, "6—4.MP3" },
                }
            },
            {
                "06货架安装/04固定4个螺丝且用胶水固定",
                new Dictionary<int, string>
                {
                    { 0, "6—5.MP3" },
                }
            },
            {
                "06货架安装/05安装完一层货架",
                new Dictionary<int, string>
                {
                    { 0, "6—6.MP3" },
                }
            },
            {
                "06货架安装/06安装完二层货架",
                new Dictionary<int, string>
                {
                    { 0, "6—7.MP3" },
                    { 2, "6—8.MP3" },
                }
            },
            {
                "06货架安装/07安装完三层货架",
                new Dictionary<int, string>
                {
                    { 0, "6—9.MP3" },
                    { 2, "6—10.MP3" },
                }
            },
            {
                "06货架安装/08安装完四层货架",
                new Dictionary<int, string>
                {
                    { 0, "6—11.MP3" },
                    { 2, "6—12.MP3" },
                }
            },
            {
                "06货架安装/10完成整个模块",
                new Dictionary<int, string>
                {
                    { 0, "6—13.MP3" },
                    { 1, "6—14.MP3" },
                }
            },
            // 07播种墙安装
            {
                "07播种墙安装/02固定立柱",
                new Dictionary<int, string>
                {
                    { 0, "7—1.MP3" },
                    { 1, "7—2.MP3" },
                    { 2, "7—3.MP3" },
                }
            },
            {
                "07播种墙安装/03安装层板和横梁",
                new Dictionary<int, string>
                {
                    { 0, "7—3.MP3" },
                    { 1, "7—4 1.MP3" },
                    { 2, "7—3.MP3" },
                    { 3, "7—4 1.MP3" },
                    { 4, "7—3.MP3" },
                    { 5, "7—4 1.MP3" },
                    { 6, "7—3.MP3" },
                    { 7, "7—4 1.MP3" },
                }
            },
            {
                "07播种墙安装/05安装固定扫描枪",
                new Dictionary<int, string>
                {
                    { 0, "7—5.MP3" },
                }
            },
            {
                "07播种墙安装/07安装电脑",
                new Dictionary<int, string>
                {
                    { 0, "7—6.MP3" },
                    { 1, "7—7.MP3" },
                    { 2, "7—8.MP3" },
                    { 3, "7—9.MP3" },
                    { 4, "7—10 1.MP3" },
                }
            },
            {
                "07播种墙安装/08卡槽安装",
                new Dictionary<int, string>
                {
                    { 0, "7—11.MP3" },
                }
            },
            {
                "07播种墙安装/10安装端子",
                new Dictionary<int, string>
                {
                    { 0, "7—12.MP3" },
                }
            },
            {
                "07播种墙安装/11电子标签供电",
                new Dictionary<int, string>
                {
                    { 0, "7—13.MP3" },
                }
            },
            // 08充电桩安装
            {
                "08充电桩安装/01粘贴二维码",
                new Dictionary<int, string>
                {
                    { 0, "8—2.MP3" },
                }
            },
            {
                "08充电桩安装/02安装膨胀螺丝",
                new Dictionary<int, string>
                {
                    { 0, "8—3.MP3" },
                    { 1, "8—4.MP3" },
                    { 2, "8—6.MP3" },
                    { 6, "8—7.MP3" },
                    { 10, "8—9.MP3" },
                    { 11, "8—10.MP3" },
                    { 15, "8—11.MP3" },
                }
            },
            {
                "08充电桩安装/03安装WiFi天线",
                new Dictionary<int, string>
                {
                    { 0, "8—5.MP3" },
                }
            },
            {
                "08充电桩安装/04连接电源网线并开机",
                new Dictionary<int, string>
                {
                    { 0, "8—12.MP3" },
                }
            },
            {
                "08充电桩安装/05充电站配网",
                new Dictionary<int, string>
                {
                    { 0, "8—13.MP3" },
                }
            },
            // 09机器人入场
            {
                "09机器人入场/02从货箱取出机器人",
                new Dictionary<int, string>
                {
                    { 0, "9—1.MP3" },
                    { 8, "9—2.MP3" },
                }
            },
            {
                "09机器人入场/03检查机器人",
                new Dictionary<int, string>
                {
                    { 0, "9—3.MP3" },
                    { 2, "9—4.MP3" },
                }
            },
            {
                "09机器人入场/04AGV配网",
                new Dictionary<int, string>
                {
                    { 0, "9—5.MP3" },
                }
            },
        };

    /// <summary>
    /// Builds the narration slot list for one topic. Slots the project never
    /// bound stay <c>null</c> so a missing recording cannot shift a later clip
    /// onto the wrong step.
    /// </summary>
    internal static List<AudioClip> ResolveStepAudioClips(string key, int stepCount)
    {
        List<AudioClip> clips = new List<AudioClip>(stepCount);
        for (int i = 0; i < stepCount; i++)
        {
            clips.Add(null);
        }

        if (!Authored.TryGetValue(key, out Dictionary<int, string> slots))
        {
            return clips;
        }

        foreach (KeyValuePair<int, string> slot in slots)
        {
            if (slot.Key < 0 || slot.Key >= stepCount)
            {
                BpfLog.Warning(BpfLogCategory.Diagnostics,
                    "tex_core_step_voice_bindings.resolve_step_audio_clips",
                    "步骤语音槽位越界，已忽略：" + key + " slot=" + slot.Key + " steps=" + stepCount);
                continue;
            }

            clips[slot.Key] = ImportLegacyClip(key, slot.Key, slot.Value);
        }

        return clips;
    }

    /// <summary>
    /// Number of slots this topic has authored narration for. Used by tests and
    /// reports to describe the migrated configuration.
    /// </summary>
    internal static int CountAuthoredSlots(string key)
    {
        return Authored.TryGetValue(key, out Dictionary<int, string> slots) ? slots.Count : 0;
    }

    /// <summary>All topics that carry authored narration.</summary>
    internal static IEnumerable<string> AuthoredKeys
    {
        get { return Authored.Keys; }
    }

    /// <summary>True when the project bound narration to this exact step.</summary>
    internal static bool HasAuthoredSlot(string key, int stepIndex)
    {
        return Authored.TryGetValue(key, out Dictionary<int, string> slots) && slots.ContainsKey(stepIndex);
    }

    /// <summary>
    /// Total number of authored narration bindings across every topic. This is
    /// the number the migrated profiles must reproduce: 53 bindings over 28
    /// topics, recovered from the pre-migration serialized data.
    /// </summary>
    internal static int CountAllAuthoredSlots()
    {
        int total = 0;
        foreach (Dictionary<int, string> slots in Authored.Values)
        {
            total += slots.Count;
        }

        return total;
    }

    /// <summary>
    /// Copies a legacy narration file into the project and returns the loaded
    /// clip. The legacy tree itself stays read-only.
    /// </summary>
    private static AudioClip ImportLegacyClip(string key, int stepIndex, string legacyFileName)
    {
        string moduleName = key.Substring(0, key.IndexOf('/'));
        string prefix = moduleName.Substring(0, 2);
        if (!LegacyRoots.TryGetValue(prefix, out string legacyRoot))
        {
            BpfLog.Warning(BpfLogCategory.Diagnostics,
                "tex_core_step_voice_bindings.import_legacy_clip",
                "没有该模块的旧语音根目录：" + moduleName);
            return null;
        }

        string source = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            legacyRoot.Replace('/', Path.DirectorySeparatorChar),
            legacyFileName);
        string destination = TexCore_StepVoiceMigration.StepVoiceRoot + "/" + moduleName + "/"
            + SanitizeName(key) + "_" + stepIndex + ".mp3";

        if (!File.Exists(source))
        {
            BpfLog.Warning(BpfLogCategory.Diagnostics,
                "tex_core_step_voice_bindings.import_legacy_clip",
                "旧语音不存在：" + source);
            return null;
        }

        EnsureFolder(TexCore_StepVoiceMigration.StepVoiceRoot + "/" + moduleName);

        string absoluteDestination = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            destination.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(absoluteDestination))
        {
            File.Copy(source, absoluteDestination);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
        }

        return AssetDatabase.LoadAssetAtPath<AudioClip>(destination);
    }

    private static string SanitizeName(string value)
    {
        return value.Replace("/", "_").Replace("\\", "_");
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        int slash = folder.LastIndexOf('/');
        EnsureFolder(folder.Substring(0, slash));
        AssetDatabase.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
    }
}
#endif
