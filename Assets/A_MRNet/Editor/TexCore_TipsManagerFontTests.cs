using System;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The shared title bar is the channel every guidance line and every rejected
/// input reason goes through. Its authored font only covers Latin, so Chinese
/// text used to reach the headset as a row of empty boxes.
/// </summary>
public sealed class TexCore_TipsManagerFontTests
{
    private const string ScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";

    /// <summary>
    /// A rule message followed by a guidance line: exactly what the title bar is
    /// asked to draw during a run.
    /// </summary>
    private const string SampleMessages =
        "密码长度必须为 8-64 位完成以下操作后，点击确定：选择中文（简体）作为安装语言。";

    [Test]
    public void EnsureTitleFont_MakesTheTitleBarDrawChineseHints()
    {
        WithScene(scene =>
        {
            TexCore_TipsManager manager = FindSceneComponent<TexCore_TipsManager>(scene);
            Assert.That(manager, Is.Not.Null, "主场景必须带提示管理器。");
            Assert.That(manager.TitleText, Is.Not.Null, "提示管理器必须挂接标题文本。");

            manager.EnsureTitleFont();

            TMP_FontAsset used = manager.TitleText.font;
            Assert.That(used, Is.Not.Null, "标题文本必须保留字体。");
            Assert.That(manager.TitleText.fontSharedMaterial, Is.EqualTo(used.material),
                "标题文本的材质必须跟随替换后的字体。");

            foreach (char character in SampleMessages)
            {
                Assert.That(used.HasCharacter(character), Is.True,
                    $"标题栏字体画不出“{character}”，中文提示仍会显示为方框。");
            }

            // A font that already draws the hints must stay in place.
            manager.EnsureTitleFont();
            Assert.That(manager.TitleText.font, Is.EqualTo(used),
                "已经能画中文的字体不允许被再次替换。");

            // The bar keeps publishing what it is given.
            manager.Title("密码长度必须为 8-64 位");
            Assert.That(manager.TitleText.text, Is.EqualTo("密码长度必须为 8-64 位"));
            Assert.That(manager.TitleText.gameObject.activeSelf, Is.True);
        });
    }

    private static void WithScene(Action<Scene> verify)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            verify(scene);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }
}
