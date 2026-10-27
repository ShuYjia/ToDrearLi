---
name: unity-intro-video-gate
description: Add or repair a skippable Unity introduction video that blocks a level, node, topic, quiz, or first gameplay step until the video finishes, is skipped, or fails. Use for Canvas or world-space Canvas video panels, VideoPlayer completion flow, close/skip buttons, MRTK PressableButton or StatefulInteractable wiring, first-topic gating, videos that disappear without continuing, or UI that becomes invisible after being reparented under a scene object.
---

# Unity Intro Video Gate

Build one reliable gate: show the introduction before the first actionable content, then continue exactly once through a shared completion path.

## Define success first

Require all of these outcomes:

1. Entering the owner level shows and plays the video.
2. The first topic or step does not start behind the video.
3. Skip/close hides the video and immediately starts the first content.
4. Natural playback completion starts the same content.
5. Playback failure does not block the level.
6. Re-entering, disabling, or destroying the owner does not leave listeners or throw missing-reference errors.

## Inspect before editing

1. Read repository rules and identify the owning scene, node, prefab, and runtime flow class.
2. Inspect the video panel's original Canvas, RectTransform, VideoPlayer, render target, and close-button hierarchy.
3. Inspect the close button's existing interaction component and persistent events.
4. Find the exact method that normally starts the first topic or step. Gate that method; do not create a parallel content-loading system.
5. Check the current Unity Editor log for collider ownership warnings, missing references, and compilation errors.

## Preserve UI rendering ownership

Keep a Canvas UI panel under the Canvas hierarchy where it renders correctly. Do not reparent it under a 3D business object merely to express logical ownership. Reparenting can change world/local transform interpretation, scale, sorting, raycasting, and visibility.

Represent logical ownership in code or serialized references:

- owner level/node controls when the panel becomes active;
- video panel remains a child of its working Canvas;
- prefer serialized references when editing through Unity;
- when runtime-loaded prefabs cannot serialize scene references, use a narrowly scoped scene lookup and validate the result.

For inactive scene objects, do not rely on `GameObject.Find`. A fallback lookup may use `Resources.FindObjectsOfTypeAll<Transform>()`, but require both the expected name and the active scene to avoid matching prefab assets or another loaded scene.

## Reuse the existing button

Never add a second collider, `Button`, `PressableButton`, or `StatefulInteractable` before inspecting descendants. MRTK button prefabs often place the visual wrapper and actual interactable on different transforms.

Use the event source already owning the button collider:

- Unity UI: reuse `UnityEngine.UI.Button.onClick`;
- MRTK: reuse the existing `StatefulInteractable.OnClicked` or `PressableButton`'s inherited interactable event;
- search children with inactive objects included when the named wrapper does not carry the component itself.

Adding another interactable to the wrapper can produce a warning like "collider ... already registered with another Interactable". The first registered component keeps the collider, so the new listener never receives the click even though the original close action still works.

If no valid interaction component exists, stop and choose one interaction system deliberately. Do not silently stack another system on the same collider.

## Gate the normal startup path

Delay the owner's normal first-content call until the introduction completes. For an inherited node flow, call the existing base startup only from a guarded continuation method.

Use one idempotent flag:

```csharp
private bool contentStarted;

private void StartContent()
{
    if (contentStarted || !isActiveAndEnabled)
        return;

    contentStarted = true;
    StartExistingFirstContent();
}
```

Do not preload the first quiz behind an opaque video unless the existing framework explicitly requires that lifecycle.

## Unify every exit

Route skip, natural completion, and playback error through the same method:

```csharp
private void FinishIntroduction()
{
    StopIntroduction();
    StartContent();
}
```

Wire these sources:

- close/skip click -> `FinishIntroduction()`;
- `VideoPlayer.loopPointReached` -> `FinishIntroduction()`;
- `VideoPlayer.errorReceived` -> log the error, then `FinishIntroduction()`.

Set `playOnAwake = false`, subscribe before calling `Play()`, and make the panel active before playback.

If the panel or VideoPlayer is missing, log a precise warning and start the content rather than permanently blocking the level.

## Clean up safely

Unsubscribe from every event that was subscribed, stop playback, and hide the panel when the owner is disabled or destroyed. Make cleanup safe to call more than once.

Use explicit Unity null checks before accessing scene objects because destroyed `UnityEngine.Object` instances can retain managed references while behaving as null:

```csharp
if (videoRoot != null)
    videoRoot.SetActive(false);
```

Do not let cleanup call the continuation path. Only explicit completion, skip, or playback error should advance content.

## Validate in layers

### Static validation

- Video panel remains under the intended Canvas.
- RectTransform, scale, anchors, render texture, and VideoPlayer source are preserved.
- Close button resolves to exactly one effective interactable.
- Owner startup no longer starts the first content before the gate.
- All three completion routes reach the guarded continuation.

### Unity validation

1. Let Unity recompile and confirm there are no C# errors.
2. Enter the level and verify the video is visible and playing.
3. Click skip early; verify the video hides and first content appears once.
4. Re-enter and let the video finish; verify the same result.
5. Temporarily test the missing/error fallback when proportionate to the change.
6. Exit Play Mode and inspect the Console for duplicate-interactable warnings, `MissingReferenceException`, and double-loaded content.

When Unity automation is unavailable, state that runtime verification remains for the user; do not present static inspection as Play Mode proof.

## Keep delivery scoped

Commit only the focused flow script, scene/prefab, and required `.meta` files. Preserve unrelated scene saves and generated configuration files unless the task explicitly owns them. Follow repository-specific fetch, Unity MCP, test, build, and PR rules before these generic instructions.

## BPF framework adaptation

When used in BPF-style node flows:

- keep the introduction panel under its working Canvas even if it logically belongs to a business node;
- gate the existing node `Start`/first `LoadChild` path rather than editing generated configuration;
- reuse the current `Aa_Node`/`NodePanelBase` first-topic loading behavior;
- never hand-edit generated `Assets/StreamingAssets/Config.xml` to add the video gate;
- verify the first configured child topic appears after both skip and natural completion.
