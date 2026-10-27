using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime "物体标签" for backpack-spawned rack parts. The authored 06/07 item
/// templates only ship a label on the drill, so the rack assembly controller
/// clones that label for the other parts. This component keeps the clone
/// readable (billboard above the part) while a ray or grab hovers the item.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_ObjectNameLabel : MonoBehaviour
{
    private const float HeightPadding = 0.05f;
    private const float DefaultWorldScale = 0.0003f;

    private GameObject labelRoot;
    private Transform labelOrigin;
    private float worldScale = DefaultWorldScale;
    private bool hoverWired;

    public void Initialize(GameObject labelTemplate, string displayName)
    {
        if (labelRoot != null || labelTemplate == null)
        {
            return;
        }

        worldScale = labelTemplate.transform.lossyScale.x;
        labelRoot = Instantiate(labelTemplate, transform, false);
        labelRoot.name = "物体标签UI";

        // The label is display-only: it must never steal the controller ray
        // from the part it describes.
        GraphicRaycaster raycaster = labelRoot.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            Destroy(raycaster);
        }
        foreach (Graphic graphic in labelRoot.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        Text label = labelRoot.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.text = displayName;
        }

        labelRoot.SetActive(false);
        // The authored label is a callout. Newer templates can expose an
        // explicit "Origin"; the current 06 template stores the tip as the
        // far lower-right Image child instead.
        labelOrigin = FindCalloutTip(labelRoot.transform);
        ApplyWorldScale();
        WireHover();
    }

    private void OnDisable()
    {
        SetVisible(false);
    }

    private void OnDestroy()
    {
        UnwireHover();
        if (labelRoot != null)
        {
            Destroy(labelRoot);
            labelRoot = null;
        }
    }

    private void LateUpdate()
    {
        if (labelRoot == null || !labelRoot.activeSelf)
        {
            return;
        }

        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            return;
        }

        Vector3 anchor = GetLabelAnchor();
        labelRoot.transform.position = anchor;
        Vector3 toCamera = activeCamera.transform.position - anchor;
        if (toCamera.sqrMagnitude > 0.000001f)
        {
            // A world-space Canvas reads from its -Z side, so +Z has to point
            // away from the camera. Looking at the camera with +Z showed the
            // text mirrored (the 胶水 / 橡胶锤 / 二维码 labels), hence the 180°
            // flip around Y.
            labelRoot.transform.rotation = Quaternion.LookRotation(-toCamera, Vector3.up);
        }
        ApplyWorldScale();

        // Put the callout tip on the anchor: the panel then hangs beside the
        // part the way the authored 电钻 label does, instead of centring the
        // canvas on it and leaving the tip far away.
        if (labelOrigin != null)
        {
            labelRoot.transform.position += anchor - labelOrigin.position;
        }
    }

    private static Transform FindCalloutTip(Transform root)
    {
        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (candidate != root && candidate.name == "Origin")
            {
                return candidate;
            }
        }

        RectTransform rootRect = root as RectTransform;
        Transform bestCandidate = null;
        float bestDistance = 0f;
        foreach (RectTransform candidate in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (candidate == rootRect ||
                candidate.parent != root ||
                candidate.GetComponent<Image>() == null)
            {
                continue;
            }

            Vector2 anchoredPosition = candidate.anchoredPosition;
            float distance = anchoredPosition.sqrMagnitude;
            if (bestCandidate == null || distance > bestDistance)
            {
                bestDistance = distance;
                bestCandidate = candidate;
            }
        }

        return bestCandidate;
    }

    private Vector3 GetLabelAnchor()
    {
        bool found = false;
        Bounds bounds = default;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(false))
        {
            if (renderer == null || !renderer.enabled ||
                renderer.transform.IsChildOf(labelRoot.transform))
            {
                continue;
            }

            // Only the part's drawn mesh may move the label. A particle, trail
            // or line renderer (the 胶水 pour for example) reports bounds far
            // larger than the part and would float the label away from it.
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found
            ? new Vector3(bounds.center.x, bounds.max.y + HeightPadding, bounds.center.z)
            : transform.position + Vector3.up * HeightPadding;
    }

    private void ApplyWorldScale()
    {
        if (labelRoot == null)
        {
            return;
        }

        float parentScale = transform.lossyScale.x;
        float scale = Mathf.Approximately(parentScale, 0f) ? worldScale : worldScale / parentScale;
        labelRoot.transform.localScale = Vector3.one * scale;
    }

    private void SetVisible(bool visible)
    {
        if (labelRoot != null)
        {
            labelRoot.SetActive(visible);
        }
    }

    private void WireHover()
    {
        if (hoverWired)
        {
            return;
        }

        ObjectManipulator manipulator = GetComponent<ObjectManipulator>();
        if (manipulator == null)
        {
            return;
        }

        AddHoverListeners(manipulator.IsRayHovered);
        AddHoverListeners(manipulator.IsGazeHovered);
        AddHoverListeners(manipulator.IsGrabHovered);
        AddHoverListeners(manipulator.IsPokeHovered);
        hoverWired = true;
    }

    private void UnwireHover()
    {
        if (!hoverWired)
        {
            return;
        }

        ObjectManipulator manipulator = GetComponent<ObjectManipulator>();
        if (manipulator != null)
        {
            RemoveHoverListeners(manipulator.IsRayHovered);
            RemoveHoverListeners(manipulator.IsGazeHovered);
            RemoveHoverListeners(manipulator.IsGrabHovered);
            RemoveHoverListeners(manipulator.IsPokeHovered);
        }
        hoverWired = false;
    }

    private void AddHoverListeners(MixedReality.Toolkit.TimedFlag flag)
    {
        flag.OnEntered.AddListener(HandleHoverEntered);
        flag.OnExited.AddListener(HandleHoverExited);
    }

    private void RemoveHoverListeners(MixedReality.Toolkit.TimedFlag flag)
    {
        flag.OnEntered.RemoveListener(HandleHoverEntered);
        flag.OnExited.RemoveListener(HandleHoverExited);
    }

    private void HandleHoverEntered(float _)
    {
        SetVisible(true);
    }

    private void HandleHoverExited(float _)
    {
        SetVisible(false);
    }
}
