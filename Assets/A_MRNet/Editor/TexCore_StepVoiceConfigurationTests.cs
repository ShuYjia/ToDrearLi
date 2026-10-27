using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Guards the step-voice configuration introduced for modules 01-11: every topic
/// owns a profile, slot counts follow the real step list, and only the recorded
/// 08/09 alignment is bound. Audio order must follow task order, and a missing
/// recording must stay silent rather than shift a later clip onto the step.
/// </summary>
public sealed class TexCore_StepVoiceConfigurationTests
{
    private const string BusinessRoot = TexCore_StepVoiceMigration.BusinessRoot;
    private const string StepVoiceRoot = TexCore_StepVoiceMigration.StepVoiceRoot;

    private static GameObject LoadTopic(string relativePath)
    {
        string path = BusinessRoot + "/" + relativePath + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab, Is.Not.Null, "缺少题目 Prefab：" + path);
        return prefab;
    }

    private static IEnumerable<string> TopicPrefabPaths()
    {
        string projectRoot = Directory.GetCurrentDirectory();
        string absoluteRoot = Path.GetFullPath(BusinessRoot);
        return Directory
            .GetFiles(absoluteRoot, "*.prefab", SearchOption.AllDirectories)
            .Select(value => value.Replace('\\', '/'))
            .Where(value => value.Substring(absoluteRoot.Length + 1).Contains("/"))
            .Select(value => value.Substring(projectRoot.Length + 1).Replace('\\', '/'))
            .OrderBy(value => value, System.StringComparer.Ordinal);
    }

    [Test]
    public void EveryTopic_OwnsAStepVoiceProfile()
    {
        List<string> missing = new List<string>();
        int topics = 0;
        foreach (string path in TopicPrefabPaths())
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                continue;
            }

            PanelTopicBase panel = prefab.GetComponent<PanelTopicBase>();
            if (panel == null || panel.topicData == null || panel.topicData.tasks == null)
            {
                continue;
            }

            topics++;
            if (prefab.GetComponent<TexCore_StepVoiceProfile>() == null)
            {
                missing.Add(path);
            }
        }

        Assert.That(topics, Is.GreaterThan(0), "没有扫到任何题目 Prefab。");
        Assert.That(missing, Is.Empty, "以下题目缺少 TexCore_StepVoiceProfile：\n" + string.Join("\n", missing));
    }

    [Test]
    public void EveryTopic_SlotCountMatchesItsStepCount()
    {
        List<string> problems = new List<string>();
        foreach (string path in TopicPrefabPaths())
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                continue;
            }

            PanelTopicBase panel = prefab.GetComponent<PanelTopicBase>();
            TexCore_StepVoiceProfile profile = prefab.GetComponent<TexCore_StepVoiceProfile>();
            if (panel == null || panel.topicData == null || panel.topicData.tasks == null || profile == null)
            {
                continue;
            }

            if (profile.SlotCount != panel.topicData.tasks.Count)
            {
                problems.Add($"{Path.GetFileName(path)}: slots={profile.SlotCount} steps={panel.topicData.tasks.Count}");
            }
        }

        Assert.That(problems, Is.Empty, "语音槽位数量与步骤数量不一致：\n" + string.Join("\n", problems));
    }

    [Test]
    public void StepIndexes_AreContiguousAndUniquePerTopic()
    {
        GameObject operate = LoadTopic("08充电桩安装/02安装膨胀螺丝");
        Panel_Operate panel = operate.GetComponent<Panel_Operate>();
        TexCore_StepVoiceProfile profile = operate.GetComponent<TexCore_StepVoiceProfile>();

        Assert.That(panel.topicData.tasks.Count, Is.EqualTo(16),
            "02安装膨胀螺丝 仍然是 16 个真实步骤。");
        Assert.That(profile.SlotCount, Is.EqualTo(panel.topicData.tasks.Count));

        // Slot i must answer for step index i and nothing else.
        for (int i = 0; i < profile.SlotCount; i++)
        {
            Assert.That(profile.GetClip(i), Is.SameAs(profile.stepAudioClips[i]),
                $"槽位 {i} 必须严格对应 stepIndex {i}。");
        }

        Assert.That(profile.GetClip(-1), Is.Null, "越界索引必须返回 null。");
        Assert.That(profile.GetClip(profile.SlotCount), Is.Null, "越界索引必须返回 null。");
    }

    [Test]
    public void MissingRecording_StaysNullInsteadOfShiftingLaterClips()
    {
        // 8—11.MP3 is bound to step 15, so steps 12..14 must stay empty even
        // though a clip exists later in the list.
        GameObject operate = LoadTopic("08充电桩安装/02安装膨胀螺丝");
        TexCore_StepVoiceProfile profile = operate.GetComponent<TexCore_StepVoiceProfile>();

        Assert.That(profile.GetClip(12), Is.Null, "step 12 无录音，必须为 null。");
        Assert.That(profile.GetClip(13), Is.Null, "step 13 无录音，必须为 null。");
        Assert.That(profile.GetClip(14), Is.Null, "step 14 无录音，必须为 null。");
        Assert.That(profile.GetClip(15), Is.Not.Null, "step 15 绑定了 8—11.MP3。");
    }

    [Test]
    public void BoundClips_ReproduceTheAuthoredBindingTableExactly()
    {
        List<string> problems = new List<string>();
        int checkedTopics = 0;

        foreach (string key in TexCore_StepVoiceBindings.AuthoredKeys)
        {
            GameObject prefab = LoadTopic(key);
            PanelTopicBase panel = prefab.GetComponent<PanelTopicBase>();
            TexCore_StepVoiceProfile profile = prefab.GetComponent<TexCore_StepVoiceProfile>();

            Assert.That(panel, Is.Not.Null, key + " 必须是题目。");
            Assert.That(profile, Is.Not.Null, key + " 缺少语音配置。");
            Assert.That(profile.SlotCount, Is.EqualTo(panel.topicData.tasks.Count),
                key + " 的槽位数必须等于步骤数。");
            checkedTopics++;

            for (int i = 0; i < profile.SlotCount; i++)
            {
                bool expected = TexCore_StepVoiceBindings.HasAuthoredSlot(key, i);
                bool actual = profile.GetClip(i) != null;
                if (expected != actual)
                {
                    problems.Add($"{key}[{i}] 期望绑定={expected} 实际={actual}");
                }
            }
        }

        Assert.That(checkedTopics, Is.EqualTo(28), "有授权语音的题目应为 28 个。");
        Assert.That(problems, Is.Empty, "以下槽位与授权绑定表不一致：\n" + string.Join("\n", problems));
    }

    [Test]
    public void BoundClipTotal_MatchesTheAuthoredBaselineOf53()
    {
        // 53 bindings is what the project carries on Panel_Operate.stepAudioClips
        // after narration moved to the profile. It was 54 until the 09
        // "06机器人加入系统" topic was removed from the flow together with its
        // 9—6.MP3 binding; a further drop here means authored narration was
        // silently dropped again.
        int bound = 0;
        foreach (string path in TopicPrefabPaths())
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            TexCore_StepVoiceProfile profile = prefab == null
                ? null
                : prefab.GetComponent<TexCore_StepVoiceProfile>();
            if (profile == null)
            {
                continue;
            }

            for (int i = 0; i < profile.SlotCount; i++)
            {
                if (profile.GetClip(i) != null)
                {
                    bound++;
                }
            }
        }

        Assert.That(TexCore_StepVoiceBindings.CountAllAuthoredSlots(), Is.EqualTo(53),
            "授权绑定表本身应为 53 条。");
        Assert.That(bound, Is.EqualTo(53),
            "迁移后的绑定总数必须保持 53 条，不能少于项目剩余的授权语音数量。");
    }

    [Test]
    public void TopicsWithoutAuthoredNarration_StaySilent()
    {
        // These topics have no authored step narration at all, so every slot must
        // stay empty rather than borrowing a clip from a neighbouring step.
        foreach (string topic in new[]
                 {
                     "01服务器网络搭建/04机柜电源线连接地插供电",
                     "01服务器网络搭建/16完成整个模块",
                     "05划线贴码/04划线完成",
                     "06货架安装/09安装完五层货架",
                     "07播种墙安装/12完成整个模块",
                     "10货架入场/02机器人搬运至货架点"
                 })
        {
            GameObject prefab = LoadTopic(topic);
            TexCore_StepVoiceProfile profile = prefab.GetComponent<TexCore_StepVoiceProfile>();
            Assert.That(profile, Is.Not.Null, topic + " 缺少语音配置。");
            Assert.That(TexCore_StepVoiceBindings.CountAuthoredSlots(topic), Is.Zero,
                topic + " 不应出现在授权绑定表里。");
            Assert.That(profile.stepAudioClips.Count(value => value != null), Is.Zero,
                topic + " 没有授权语音，全部槽位必须为空。");
        }
    }

    [Test]
    public void LegacyHintAudioStrings_AreNoLongerUsed()
    {
        List<string> problems = new List<string>();
        foreach (string path in TopicPrefabPaths())
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                continue;
            }

            PanelTopicBase panel = prefab.GetComponent<PanelTopicBase>();
            if (panel == null || panel.topicData == null || panel.topicData.hintAudios == null)
            {
                continue;
            }

            if (panel.topicData.hintAudios.Any(value => !string.IsNullOrEmpty(value)))
            {
                problems.Add(Path.GetFileName(path));
            }
        }

        Assert.That(problems, Is.Empty,
            "以下题目仍在用 hintAudios 字符串承载步骤语音：\n" + string.Join("\n", problems));
    }

    [Test]
    public void BoundClips_AreRealAudioAssetsInsideTheProject()
    {
        List<string> problems = new List<string>();
        int bound = 0;
        foreach (string path in TopicPrefabPaths())
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                continue;
            }

            TexCore_StepVoiceProfile profile = prefab.GetComponent<TexCore_StepVoiceProfile>();
            if (profile == null)
            {
                continue;
            }

            for (int i = 0; i < profile.SlotCount; i++)
            {
                AudioClip clip = profile.GetClip(i);
                if (clip == null)
                {
                    continue;
                }

                bound++;
                string assetPath = AssetDatabase.GetAssetPath(clip);
                if (string.IsNullOrEmpty(assetPath))
                {
                    problems.Add($"{Path.GetFileName(path)}[{i}] 的 AudioClip 没有资产路径");
                }
                else if (!assetPath.StartsWith(StepVoiceRoot, System.StringComparison.Ordinal))
                {
                    problems.Add($"{Path.GetFileName(path)}[{i}] 引用了项目外/旧目录资源：{assetPath}");
                }
            }
        }

        Assert.That(problems, Is.Empty, string.Join("\n", problems));
        Assert.That(bound, Is.EqualTo(53),
            "全部 53 条授权绑定都必须指向项目内的真实 AudioClip。");
    }

    [Test]
    public void SceneBuildersAndMigration_ShareOneBindingSource()
    {
        // The scene builders and the migration must read the same authored table,
        // otherwise one of them silently drops narration the other keeps.
        Assert.That(TexCore_StepVoiceBindings.HasAuthoredSlot("08充电桩安装/01粘贴二维码", 0),
            Is.True, "08/01 的第 0 步有 8—2.MP3。");
        Assert.That(TexCore_StepVoiceBindings.HasAuthoredSlot("09机器人入场/05机器人推至二维码", 0),
            Is.False, "05 没有录音，不能凭空产生绑定。");
        Assert.That(TexCore_StepVoiceBindings.HasAuthoredSlot("08充电桩安装/02安装膨胀螺丝", 3),
            Is.False, "第 3 步没有录音，必须保持空槽。");

        // 07/03 installs eight parts by alternating two recordings.
        Assert.That(TexCore_StepVoiceBindings.CountAuthoredSlots("07播种墙安装/03安装层板和横梁"),
            Is.EqualTo(8));

        // Both entry points must agree for every authored topic.
        foreach (string key in TexCore_StepVoiceBindings.AuthoredKeys)
        {
            Assert.That(TexCore_StepVoiceMigration.ResolveStepAudioClips(key, 0), Is.Not.Null,
                key + " 的解析入口必须共用同一张表。");
        }
    }
}
