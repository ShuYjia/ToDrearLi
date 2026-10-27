using System.Collections;
using MixedReality.Toolkit.UX;
using UnityEngine;
using Bpf.Diagnostics;

/// <summary>
/// Advances the active business topic when a legacy exit button is pressed
/// after its TexCore step was completed by the player.
/// </summary>
/// <remarks>
/// The legacy AGV 配网 "ESC" button only closed its own page hierarchy, so the
/// framework kept waiting for a manual L2 menu click. Attach this component to
/// that button and point <see cref="requiredStep"/> at the matching step to jump
/// straight to the next topic.
/// </remarks>
[DisallowMultipleComponent]
public sealed class TexCore_TopicAdvanceOnExitButton : MonoBehaviour
{
    /// <summary>
    /// 兜底跳题前的最大等待帧数。正常链路里 Panel_Operate 的计分协程在
    /// 下一帧就会提交分数并自行跳题，这里只是防止它没跳时把玩家卡住。
    /// </summary>
    private const int MaxAdvanceWaitFrames = 4;

    [SerializeField] private TexCore_PassiveUIEventStep requiredStep;

    /// <summary>
    /// 勾选后，即使 <see cref="requiredStep"/> 还没有被玩家做完，ESC 也会强制
    /// 跳到下一题。未结算的分数会被放弃，用于玩家卡在旧界面时脱困。
    /// 默认关闭，保证计分操作不会被误跳过。
    /// </summary>
    [Tooltip("即使配网序列没做完，点 ESC 也强制跳到下一题（放弃本题未结算的分数）。")]
    [SerializeField] private bool forceAdvanceWhenIncomplete;

    private PressableButton exitButton;
    private bool advancePending;

    private void Awake()
    {
        CacheExitButton();
    }

    private void OnEnable()
    {
        CacheExitButton();
        if (exitButton != null)
        {
            exitButton.OnClicked.AddListener(HandleExitButtonClicked);
        }
    }

    private void OnDisable()
    {
        if (exitButton != null)
        {
            exitButton.OnClicked.RemoveListener(HandleExitButtonClicked);
        }

        advancePending = false;
    }

    private void CacheExitButton()
    {
        if (exitButton == null)
        {
            exitButton = GetComponent<PressableButton>();
        }
    }

    private void HandleExitButtonClicked()
    {
        // The button itself no longer hides the owning scene root, so an early
        // press can never blank the scene.
        if (requiredStep == null || advancePending)
        {
            return;
        }

        // 没做完时默认既不关页面也不跳题：提前关页会让玩家无法继续计分操作。
        // 只有显式打开强制开关才允许放弃本题分数直接跳题。
        bool completedByUser = requiredStep.WasCompletedByUser;
        if (!completedByUser && !forceAdvanceWhenIncomplete)
        {
            return;
        }

        NodePanelBase node = ResolveActiveNodePanel();
        if (node == null)
        {
            BpfLog.Error(BpfLogCategory.Scene, "tex_core_topic_advance.no_node_panel", null, null,
                BpfLogField.Of("button", name));
            return;
        }

        BpfLog.Info(BpfLogCategory.Scene, "tex_core_topic_advance.next_topic", "进入下一步", null,
            BpfLogField.Of("button", name),
            BpfLogField.Of("completed_by_user", completedByUser));
        advancePending = true;
        StartCoroutine(AdvanceAfterFrameworkSubmit(node, node.activeChild, completedByUser));
    }

    /// <summary>
    /// 先离开点击事件调用栈，再按需等框架链路提交，最后跳题：
    /// ESC 同时也是配网步骤的最后一个检查点，同一帧直接 NextTopic() 会停掉
    /// Panel_Operate 的计分协程，丢掉这道操作题的分数与“完成题目”事件。
    /// 强制跳题时本题没有可结算的分数，只等一帧就跳；正常完成时最多再等
    /// <see cref="MaxAdvanceWaitFrames"/> 帧，通常就是下一帧。
    /// </summary>
    private IEnumerator AdvanceAfterFrameworkSubmit(
        NodePanelBase node, PanelTopicBase topic, bool waitForFrameworkSubmit)
    {
        yield return null;

        for (int frame = 0; waitForFrameworkSubmit && frame < MaxAdvanceWaitFrames; frame++)
        {
            // 计分协程或“下一步”按钮已经自行跳题，不再重复推进。
            if (node == null || node.activeChild != topic)
            {
                topic = null;
                break;
            }

            if (topic.isSubmit)
            {
                break;
            }

            yield return null;
        }

        advancePending = false;
        if (node != null && topic != null && node.activeChild == topic)
        {
            node.NextTopic();
        }

        CloseExitPage();
    }

    /// <summary>
    /// 关闭本步骤监听的旧界面（AGV 配网页）。只在真正完成序列后才调用，
    /// 提前关页会让玩家无法继续计分操作；也绝不碰承载场景的父节点。
    /// </summary>
    private void CloseExitPage()
    {
        if (requiredStep == null)
        {
            return;
        }

        GameObject page = requiredStep.InteractionSource;
        if (page != null && page != gameObject)
        {
            page.SetActive(false);
        }
    }

    private static NodePanelBase ResolveActiveNodePanel()
    {
        TexCore_PlaneManager manager = TexCore_PlaneManager.self;
        if (manager == null || manager.activePlane == null)
        {
            return null;
        }

        if (manager.activePlane is PanelTopicBase topic)
        {
            NodePanelBase owner = topic.controlPlane as NodePanelBase;
            if (owner != null)
            {
                return owner;
            }

            if (topic.controlPlane != null)
            {
                return topic.controlPlane.GetComponent<NodePanelBase>();
            }
        }

        return manager.activePlane as NodePanelBase;
    }
}
