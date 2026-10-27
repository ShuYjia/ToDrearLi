using UnityEngine;

/// <summary>
/// Topic-level highlight for the M10 non-operation topics. It focuses only the
/// control that is valid right now, and never reveals the correct answer: the
/// option area is highlighted as a whole before the player picks anything.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_M10TopicGuidance : MonoBehaviour
{
    public enum GuidanceMode
    {
        Video,
        Select
    }

    [SerializeField] private GuidanceMode mode = GuidanceMode.Select;
    [SerializeField] private string chooseOptionHint = "请点击上方选项选择答案。";
    [SerializeField] private string confirmHint = "请点击“确定”提交答案。";
    [SerializeField] private string nextHint = "请点击“下一步”继续。";
    [SerializeField] private string videoHint = "正在播放介绍视频，可点击“跳过”直接进入下一步。";

    private Panel_Select selectPanel;
    private GameObject optionsRoot;
    private GameObject confirmButton;
    private GameObject nextButton;
    private GameObject skipButton;
    private GameObject focusedTarget;
    private bool active;

    /// <summary>Control that is highlighted right now, or null.</summary>
    public GameObject FocusedTarget => focusedTarget;

    public GuidanceMode Mode => mode;

    /// <summary>
    /// Selects what this topic highlights and drops the resolved references so
    /// they are looked up again on the next activation.
    /// </summary>
    public void Configure(GuidanceMode value)
    {
        mode = value;
        selectPanel = null;
        optionsRoot = null;
        confirmButton = null;
        nextButton = null;
        skipButton = null;
        focusedTarget = null;
    }

    private void OnEnable()
    {
        active = true;
        focusedTarget = null;
        ResolveReferences();
    }

    private void OnDisable()
    {
        active = false;
        focusedTarget = null;
        TexCore_FocusHighlight.ClearFor(this);
    }

    private void LateUpdate()
    {
        if (!active)
        {
            return;
        }

        if (mode == GuidanceMode.Video)
        {
            UpdateVideoGuidance();
        }
        else
        {
            UpdateSelectGuidance();
        }
    }

    private void ResolveReferences()
    {
        if (mode == GuidanceMode.Video)
        {
            skipButton = FindChildByName("跳过");
            return;
        }

        if (selectPanel == null)
        {
            selectPanel = GetComponent<Panel_Select>();
        }

        if (selectPanel == null)
        {
            return;
        }

        if (optionsRoot == null && selectPanel.itemParent != null)
        {
            optionsRoot = selectPanel.itemParent.gameObject;
        }

        if (confirmButton == null && selectPanel.btn_Confirm != null)
        {
            confirmButton = selectPanel.btn_Confirm;
        }

        if (nextButton == null && selectPanel.btn_Next != null)
        {
            nextButton = selectPanel.btn_Next;
        }
    }

    private void UpdateVideoGuidance()
    {
        if (skipButton == null)
        {
            ResolveReferences();
        }

        Focus(skipButton, videoHint);
    }

    private void UpdateSelectGuidance()
    {
        if (selectPanel == null)
        {
            ResolveReferences();
        }

        if (selectPanel == null)
        {
            ClearFocus();
            return;
        }

        if (selectPanel.isSubmit)
        {
            Focus(nextButton, nextHint);
            return;
        }

        if (selectPanel.steps != null && selectPanel.steps.Count > 0)
        {
            Focus(confirmButton, confirmHint);
            return;
        }

        Focus(optionsRoot, chooseOptionHint);
    }

    private void Focus(GameObject target, string hint)
    {
        if (target == null || !target.activeInHierarchy)
        {
            ClearFocus();
            return;
        }

        if (focusedTarget == target)
        {
            return;
        }

        focusedTarget = target;
        TexCore_FocusHighlight.Show(target, hint, null, null, this);
    }

    private void ClearFocus()
    {
        if (focusedTarget == null)
        {
            return;
        }

        focusedTarget = null;
        TexCore_FocusHighlight.ClearFor(this);
    }

    private GameObject FindChildByName(string childName)
    {
        Transform value = transform.Find(childName);
        if (value != null)
        {
            return value.gameObject;
        }

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child.name == childName)
            {
                return child.gameObject;
            }
        }

        return null;
    }
}
