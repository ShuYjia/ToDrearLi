using System;
using System.Collections.Generic;
using System.Linq;
using MixedReality.Toolkit.UX;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Reports an authored sequence of UI actions to TexCore without taking
/// ownership of the UI objects or their parent-page visibility.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_PassiveUIEventStep : TexCore_InteracterBase,
    ITexCoreUserCompletionStatus
{
    [SerializeField] private GameObject interactionSource;
    [SerializeField] private List<GameObject> orderedSources = new List<GameObject>();
    [SerializeField] private List<bool> textInputCheckpoints = new List<bool>();

    private readonly List<UnityAction> buttonHandlers = new List<UnityAction>();
    private readonly List<UnityAction<string>> inputHandlers = new List<UnityAction<string>>();
    private bool listenersAttached;
    private int completedCheckpointCount;
    private bool stepActive;
    private bool completed;
    private bool userCompleted;

    public bool WasCompletedByUser => userCompleted;

    public GameObject InteractionSource => interactionSource != null
        ? interactionSource
        : gameObject;

    public int CheckpointCount => orderedSources.Count;

    public int CompletedCheckpointCount => completedCheckpointCount;

    public GameObject GetCheckpointSource(int index)
    {
        return index >= 0 && index < orderedSources.Count
            ? orderedSources[index]
            : null;
    }

    public bool IsTextInputCheckpoint(int index)
    {
        return index >= 0 && index < textInputCheckpoints.Count &&
            textInputCheckpoints[index];
    }

    public void ConfigureSequence(GameObject source, IEnumerable<GameObject> sources,
        IEnumerable<bool> textInputs)
    {
        List<GameObject> configuredSources = sources == null
            ? new List<GameObject>()
            : sources.ToList();
        List<bool> configuredTextInputs = textInputs == null
            ? new List<bool>()
            : textInputs.ToList();

        if (source == null || configuredSources.Count == 0 ||
            configuredSources.Count != configuredTextInputs.Count)
        {
            throw new ArgumentException("09 AGV 配网检查点配置无效。");
        }

        if (configuredSources.Any(value => value == null) ||
            configuredSources.Distinct().Count() != configuredSources.Count)
        {
            throw new ArgumentException("09 AGV 配网检查点必须是唯一的原始 UI 控件。");
        }

        for (int index = 0; index < configuredSources.Count; index++)
        {
            if (configuredTextInputs[index])
            {
                if (configuredSources[index].GetComponent<InputField>() == null)
                {
                    throw new ArgumentException("09 AGV 配网输入检查点缺少 InputField。");
                }
            }
            else if (configuredSources[index].GetComponent<PressableButton>() == null)
            {
                throw new ArgumentException("09 AGV 配网按钮检查点缺少 PressableButton。");
            }
        }

        DetachListeners();
        interactionSource = source;
        orderedSources = configuredSources;
        textInputCheckpoints = configuredTextInputs;
        completedCheckpointCount = 0;
        AttachListeners();
    }

    private void Awake()
    {
        AttachListeners();
    }

    public new void OnEnable()
    {
        base.OnEnable();
        AttachListeners();
    }

    private void OnDisable()
    {
        DetachListeners();
        base.OnDisabel();
    }

    public override void UnFinished()
    {
        completedCheckpointCount = 0;
        completed = false;
        userCompleted = false;
        stepActive = false;
    }

    public override void UnderWay()
    {
        if (!stepActive)
        {
            completedCheckpointCount = 0;
        }

        completed = false;
        userCompleted = false;
        stepActive = true;
    }

    public override void OnTrigger()
    {
        completed = true;
        stepActive = false;
    }

    public override void Finish()
    {
        completed = true;
        stepActive = false;
    }

    private void AttachListeners()
    {
        if (!Application.isPlaying || listenersAttached ||
            orderedSources.Count != textInputCheckpoints.Count)
        {
            return;
        }

        buttonHandlers.Clear();
        inputHandlers.Clear();
        for (int index = 0; index < orderedSources.Count; index++)
        {
            int checkpointIndex = index;
            GameObject source = orderedSources[index];
            if (source == null)
            {
                buttonHandlers.Add(null);
                inputHandlers.Add(null);
                continue;
            }

            if (textInputCheckpoints[index])
            {
                InputField inputField = source.GetComponent<InputField>();
                UnityAction<string> handler = value => HandleInputEndEdit(checkpointIndex, value);
                buttonHandlers.Add(null);
                inputHandlers.Add(handler);
                if (inputField != null)
                {
                    inputField.onEndEdit.AddListener(handler);
                }
            }
            else
            {
                PressableButton button = source.GetComponent<PressableButton>();
                UnityAction handler = () => HandleButtonClick(checkpointIndex);
                buttonHandlers.Add(handler);
                inputHandlers.Add(null);
                if (button != null)
                {
                    button.OnClicked.AddListener(handler);
                }
            }
        }

        listenersAttached = true;
    }

    private void DetachListeners()
    {
        if (!listenersAttached)
        {
            return;
        }

        for (int index = 0; index < orderedSources.Count; index++)
        {
            GameObject source = orderedSources[index];
            if (source == null)
            {
                continue;
            }

            if (index < inputHandlers.Count && inputHandlers[index] != null)
            {
                InputField inputField = source.GetComponent<InputField>();
                if (inputField != null)
                {
                    inputField.onEndEdit.RemoveListener(inputHandlers[index]);
                }
            }

            if (index < buttonHandlers.Count && buttonHandlers[index] != null)
            {
                PressableButton button = source.GetComponent<PressableButton>();
                if (button != null)
                {
                    button.OnClicked.RemoveListener(buttonHandlers[index]);
                }
            }
        }

        buttonHandlers.Clear();
        inputHandlers.Clear();
        listenersAttached = false;
    }

    private void HandleInputEndEdit(int checkpointIndex, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            CompleteCheckpoint(checkpointIndex);
        }
    }

    private void HandleButtonClick(int checkpointIndex)
    {
        CompleteCheckpoint(checkpointIndex);
    }

    private void CompleteCheckpoint(int checkpointIndex)
    {
        if (!stepActive || completed || checkpointIndex != completedCheckpointCount)
        {
            return;
        }

        completedCheckpointCount++;
        if (completedCheckpointCount < orderedSources.Count)
        {
            return;
        }

        completed = true;
        userCompleted = true;
        stepActive = false;
        TexCore_BusinessManager.OnClick(gameObject);
    }
}
