using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Guards the 08 backpack guidance against the shared-UI Canvas switch: the
/// chapter bar lives inside the 背包UI Canvas together with every chapter panel,
/// so a panel that is merely "active" can still be invisible. The guidance may
/// only ever be drawn on a target the player can actually see.
/// </summary>
public sealed class TexCore_08BackpackHighlightTests
{
    private const string BackpackRootName = "背包UI";
    private const string ChapterBarName = "Button Bar_Horizontal";
    private const string ChapterTabName = "08充电桩安装切换按钮";
    private const string PanelName = "08充电桩安装背包面板";
    private const string ItemKey = "充电桩";

    private GameObject backpackRoot;
    private GameObject panel;
    private GameObject itemButton;
    private TexCore_08BackpackController controller;

    [SetUp]
    public void SetUp()
    {
        backpackRoot = new GameObject(BackpackRootName, typeof(RectTransform), typeof(Canvas));

        // The scene opens on the Buttons Canvas, so the backpack starts hidden by
        // Canvas.enabled while every GameObject stays active.
        backpackRoot.GetComponent<Canvas>().enabled = false;

        GameObject chapterBar = new GameObject(ChapterBarName, typeof(RectTransform));
        chapterBar.transform.SetParent(backpackRoot.transform, false);
        CreateUiButton(chapterBar.transform, ChapterTabName);

        panel = new GameObject(PanelName, typeof(RectTransform));
        panel.transform.SetParent(backpackRoot.transform, false);

        GameObject grid = new GameObject("Grid", typeof(RectTransform));
        grid.transform.SetParent(panel.transform, false);
        itemButton = CreateUiButton(grid.transform, "08背包_" + ItemKey);

        // Awake runs EnsureRuntimeConfiguration and also rebinds the shared chapter
        // bar of whichever scene is open. The second effect would mutate the scene
        // in the Editor, so the root is briefly parked under a name that
        // GameObject.Find("背包UI") cannot match, then restored before the
        // assertions run.
        backpackRoot.name = BackpackRootName + " (test)";
        controller = backpackRoot.AddComponent<TexCore_08BackpackController>();
        backpackRoot.name = BackpackRootName;

        controller.SetPanel(panel);
        controller.SetEntries(new List<TexCore_08BackpackController.Entry>
        {
            new TexCore_08BackpackController.Entry
            {
                key = ItemKey,
                label = ItemKey,
                button = itemButton
            }
        });
        SetActiveEntry(controller, ItemKey);
    }

    /// <summary>
    /// The active entry is set by the step notification in the real flow; the test
    /// sets the same private field so the guidance step runs without a scene flow.
    /// </summary>
    private static void SetActiveEntry(TexCore_08BackpackController target, string key)
    {
        FieldInfo field = typeof(TexCore_08BackpackController).GetField("activeKey",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, "activeKey must stay the current backpack entry.");
        field.SetValue(target, key);
    }

    [TearDown]
    public void TearDown()
    {
        // The pooled guidance objects belong to a runtime-only controller (no
        // HideFlags on the root), so they are released with it and then swept.
        if (controller != null)
        {
            MethodInfo onDestroy = typeof(TexCore_08BackpackController).GetMethod(
                "OnDestroy", BindingFlags.NonPublic | BindingFlags.Instance);
            if (onDestroy != null)
            {
                onDestroy.Invoke(controller, null);
            }
        }

        RemoveGuidanceObjects();

        if (backpackRoot != null)
        {
            Object.DestroyImmediate(backpackRoot);
        }
    }

    private static void RemoveGuidanceObjects()
    {
        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate != null && candidate.name == "08BackpackActiveEntryGuidance")
            {
                Object.DestroyImmediate(candidate);
            }
        }
    }

    [Test]
    public void Guidance_StaysOffWhenBothThePanelAndTheCanvasAreHidden()
    {
        // VisibleUi.SingleMenu is in front: the whole backpack Canvas, chapter bar
        // included, is invisible while every GameObject stays active.
        SetCanvasEnabled(false);
        panel.SetActive(true);
        StepGuidance();

        Assert.That(panel.activeInHierarchy, Is.True,
            "The shared UI roots keep their GameObjects active across a switch.");
        Assert.That(GuidanceActive(), Is.False,
            "Nothing inside a hidden Canvas may be highlighted.");
    }

    [Test]
    public void Guidance_FollowsTheVisibleItemButtonInsideTheOpenBackpack()
    {
        GetCanvas().enabled = true;
        panel.SetActive(true);
        StepGuidance();

        Assert.That(GuidanceActive(), Is.True,
            "With the backpack open the current item button is the guidance target.");
    }

    [Test]
    public void Guidance_FallsBackToTheVisibleChapterTabWhileThePanelIsClosed()
    {
        GetCanvas().enabled = true;
        panel.SetActive(true);
        StepGuidance();
        Assert.That(GuidanceActive(), Is.True, "The open panel is the baseline state.");

        // Another chapter was selected, so the 08 panel closes while the shared
        // chapter bar stays on screen: the 08 entry is still reachable.
        panel.SetActive(false);
        StepGuidance();
        Assert.That(GuidanceActive(), Is.True,
            "A visible chapter tab may carry the guidance while the panel is closed.");

        // And once that Canvas goes away too, the guidance has to stop.
        SetCanvasEnabled(false);
        StepGuidance();
        Assert.That(GuidanceActive(), Is.False,
            "With the chapter tab invisible as well, no guidance may be drawn.");
    }

    [Test]
    public void Guidance_DisappearsWhenTheItemButtonCanvasIsSwitchedAway()
    {
        GetCanvas().enabled = true;
        panel.SetActive(true);
        StepGuidance();
        Assert.That(GuidanceActive(), Is.True);

        // VisibleUi.Backpack -> VisibleUi.SingleMenu: the Canvas is disabled and
        // the GameObject is left active, which is the exact stale-highlight case.
        SetCanvasEnabled(false);
        StepGuidance();

        Assert.That(itemButton.activeInHierarchy, Is.True,
            "The shared UI roots keep their GameObjects active across a switch.");
        Assert.That(GuidanceActive(), Is.False,
            "The 08 outline must not survive the switch to another Canvas.");
    }

    private Canvas GetCanvas()
    {
        return backpackRoot.GetComponent<Canvas>();
    }

    private void SetCanvasEnabled(bool enabled)
    {
        GetCanvas().enabled = enabled;
    }

    private bool GuidanceActive()
    {
        Transform guidance = backpackRoot.transform.Find("08BackpackActiveEntryGuidance");
        if (guidance == null)
        {
            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject candidate in all)
            {
                if (candidate != null && candidate.name == "08BackpackActiveEntryGuidance")
                {
                    return candidate.activeSelf;
                }
            }

            return false;
        }

        return guidance.gameObject.activeSelf;
    }

    private void StepGuidance()
    {
        MethodInfo update = typeof(TexCore_08BackpackController).GetMethod(
            "UpdateActiveEntryGuidance", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(update, Is.Not.Null,
            "UpdateActiveEntryGuidance must stay the per-frame guidance step.");
        update.Invoke(controller, null);
    }

    private static GameObject CreateUiButton(Transform parent, string name)
    {
        GameObject button = new GameObject(name, typeof(RectTransform));
        button.transform.SetParent(parent, false);
        return button;
    }
}
