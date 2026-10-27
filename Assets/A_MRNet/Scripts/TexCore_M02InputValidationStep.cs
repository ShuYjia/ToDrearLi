using System.Collections.Generic;
using Bpf.Diagnostics;
using MixedReality.Toolkit.UX;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// M02 install step reporter for the authored pages that ask the player to
/// type values. Every bound input must pass its own rule and the whole page
/// only completes once, so one page still maps to exactly one TexCore step.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_M02InputValidationStep : TexCore_InteracterBase, ITexCoreUserCompletionStatus
{
    /// <summary>
    /// The shared title channel draws with a Latin-only font, so a Chinese
    /// reason reaches the player as a row of empty boxes. The page's own input
    /// text uses a CJK font, so the reason is repeated on the page itself.
    /// </summary>
    private static readonly Color MessageColor = new Color(1f, 0.42f, 0.35f);

    private const string MessageLabelName = "M02_InputMessage";

    /// <summary>
    /// The raster size the reason is drawn at. The label is scaled down to the
    /// page, so this is the glyph resolution, not the on-screen text size.
    /// </summary>
    private const int MessageFontSize = 48;

    private PressableButton submitButton;
    private InputField[] inputFields = new InputField[0];
    private M02InputValidationMode mode;
    private TexCore_ClickObjectStep guidance;
    private readonly List<TexCore_ClickObjectStep> inputGuidances =
        new List<TexCore_ClickObjectStep>();
    private readonly List<PressableButton> inputButtons = new List<PressableButton>();
    private Transform pageRoot;
    private Text messageLabel;
    private bool stepActive;
    private bool completed;
    private bool userCompleted;

    public bool WasCompletedByUser => userCompleted;

    public M02InputValidationMode Mode => mode;

    public int InputFieldCount => inputFields != null ? inputFields.Length : 0;

    /// <summary>The reason shown for the last rejected submit, if any.</summary>
    public string ValidationMessage { get; private set; }

    /// <summary>The on-page reason label. It only exists after a rejection.</summary>
    public Text MessageLabel => messageLabel;

    public bool HasVisibleMessage =>
        messageLabel != null && messageLabel.gameObject.activeSelf && !string.IsNullOrEmpty(messageLabel.text);

    public void Configure(
        PressableButton submitButton,
        InputField[] inputFields,
        M02InputValidationMode mode,
        Transform pageRoot = null)
    {
        this.submitButton = submitButton;
        this.inputFields = inputFields ?? new InputField[0];
        this.mode = mode;
        this.pageRoot = pageRoot != null
            ? pageRoot
            : submitButton != null ? submitButton.transform.parent : null;
        ListenToInputChanges();
        ApplyButtonState();
    }

    /// <summary>
    /// The submit button also carries a guidance-only reporter that draws the
    /// current step highlight. This step owns its lifecycle.
    /// </summary>
    public void SetGuidance(TexCore_ClickObjectStep value)
    {
        guidance = value;
    }

    /// <summary>
    /// The input containers carry guidance-only reporters of their own. The
    /// player is pointed at the fields first; the submit button is only
    /// highlighted once every value already passes its rule.
    /// </summary>
    public void AddInputGuidance(TexCore_ClickObjectStep value)
    {
        if (value == null || inputGuidances.Contains(value)) return;
        inputGuidances.Add(value);
        PressableButton button = value.GetComponent<PressableButton>();
        if (button != null && !inputButtons.Contains(button)) inputButtons.Add(button);
    }

    /// <summary>True when the live field text already passes the page rule.</summary>
    public bool InputsAreValid()
    {
        return TexCore_M02InputRules.TryValidate(mode, ReadValues(), out string _);
    }

    private void Awake()
    {
        submitButton = submitButton != null ? submitButton : GetComponent<PressableButton>();
    }

    public new void OnEnable()
    {
        base.OnEnable();
        submitButton = submitButton != null ? submitButton : GetComponent<PressableButton>();
        if (submitButton != null)
        {
            submitButton.OnClicked.RemoveListener(Submit);
            submitButton.OnClicked.AddListener(Submit);
        }

        ListenToInputChanges();
        RestoreInteraction();
    }

    private void OnDisable()
    {
        if (submitButton != null) submitButton.OnClicked.RemoveListener(Submit);
        if (inputFields != null)
        {
            for (int index = 0; index < inputFields.Length; index++)
            {
                if (inputFields[index] != null) inputFields[index].onValueChanged.RemoveListener(OnInputChanged);
            }
        }

        base.OnDisabel();
    }

    public override void UnFinished()
    {
        completed = false;
        userCompleted = false;
        stepActive = false;
        ClearMessage();
        ApplyButtonState();
        HideGuidance();
    }

    public override void UnderWay()
    {
        // Every presentation re-reads the live InputField text, so a previous
        // error message can never be reused as a cached result.
        completed = false;
        stepActive = true;
        ClearMessage();
        ApplyButtonState();
        RefreshGuidance();
    }

    public override void OnTrigger()
    {
        completed = true;
        stepActive = false;
        ClearMessage();
        ApplyButtonState();
        HideGuidance();
    }

    public override void Finish()
    {
        completed = true;
        stepActive = false;
        ClearMessage();
        ApplyButtonState();
        HideGuidance();
    }

    private void Submit()
    {
        if (!stepActive || completed) return;

        if (!TexCore_M02InputRules.TryValidate(mode, ReadValues(), out string error))
        {
            // The page, the typed text and the highlight all stay in place.
            ShowValidationMessage(error);
            RefreshGuidance();
            return;
        }

        completed = true;
        userCompleted = true;
        stepActive = false;
        ClearMessage();
        ApplyButtonState();
        HideGuidance();
        // The submit button is the only reporter that completes this step.
        TexCore_BusinessManager.OnClick(gameObject);
    }

    private string[] ReadValues()
    {
        string[] values = new string[inputFields != null ? inputFields.Length : 0];
        for (int index = 0; index < values.Length; index++)
        {
            InputField field = inputFields[index];
            values[index] = field != null ? field.text : string.Empty;
        }

        return values;
    }

    /// <summary>
    /// Keeps exactly one highlight visible: the fields while the values still
    /// need work, the submit button once every value passes its rule.
    /// </summary>
    private void RefreshGuidance()
    {
        // Without any field highlight to give, the submit button stays the target
        // so the step is never presented without a hint.
        bool submitReady = stepActive &&
                           (inputGuidances.Count == 0 || InputsAreValid());
        for (int index = 0; index < inputGuidances.Count; index++)
        {
            TexCore_ClickObjectStep inputGuide = inputGuidances[index];
            if (inputGuide == null) continue;
            if (submitReady) HideGuide(inputGuide);
            else ShowGuide(inputGuide);
        }

        if (guidance != null)
        {
            if (submitReady) ShowGuide(guidance);
            else HideGuide(guidance);
        }

        RestoreInteraction();
    }

    /// <summary>
    /// A guidance reporter also toggles the interactable it outlines, so the
    /// interaction state is restored right after the highlight moves: the fields
    /// must stay editable and the submit button must stay pressable even while
    /// its highlight is off.
    /// </summary>
    private void RestoreInteraction()
    {
        ApplyButtonState();
        for (int index = 0; index < inputButtons.Count; index++)
        {
            if (inputButtons[index] != null)
            {
                inputButtons[index].enabled = stepActive && !completed;
            }
        }
    }

    private static void ShowGuide(TexCore_ClickObjectStep guide)
    {
        if (guide.state == FinishState.等待触发) return;
        guide.SetState(FinishState.等待触发);
    }

    private static void HideGuide(TexCore_ClickObjectStep guide)
    {
        if (guide.state == FinishState.未完成) return;
        guide.SetState(FinishState.未完成);
    }

    private void HideGuidance()
    {
        for (int index = 0; index < inputGuidances.Count; index++)
        {
            if (inputGuidances[index] != null) HideGuide(inputGuidances[index]);
        }

        if (guidance != null) HideGuide(guidance);
    }

    private void ApplyButtonState()
    {
        if (submitButton != null) submitButton.enabled = stepActive && !completed;
    }

    private void ListenToInputChanges()
    {
        if (inputFields == null) return;
        for (int index = 0; index < inputFields.Length; index++)
        {
            InputField field = inputFields[index];
            if (field == null) continue;
            field.onValueChanged.RemoveListener(OnInputChanged);
            field.onValueChanged.AddListener(OnInputChanged);
        }
    }

    /// <summary>
    /// Typing again is the player answering the message, so a stale reason must
    /// not stay on the page while the value is already being corrected. The
    /// highlight also follows the live values, so it moves to the submit button
    /// as soon as the page is complete.
    /// </summary>
    private void OnInputChanged(string value)
    {
        ClearMessage();
        RefreshGuidance();
    }

    /// <summary>
    /// The shared hint channel (显示提示) spawns a modal dialog in this flow,
    /// which covers the page the player has to correct. Keep the diagnostic log,
    /// keep the non-modal title text, and show the reason on the page itself,
    /// where the player is actually looking.
    /// </summary>
    private void ShowValidationMessage(string message)
    {
        BpfLog.Warning(BpfLogCategory.UI, "tex_core_m02_input_validation_step.rejected", message, this);
        TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, message);
        ShowPageMessage(message);
    }

    private void ShowPageMessage(string message)
    {
        ValidationMessage = message;
        Text label = EnsureMessageLabel();
        if (label == null) return;
        label.text = message;
        label.gameObject.SetActive(true);
    }

    private void ClearMessage()
    {
        ValidationMessage = null;
        if (messageLabel != null) messageLabel.gameObject.SetActive(false);
    }

    /// <summary>
    /// Creates the reason label once, as the last child of the page so it draws
    /// above the page, but with the raycast target off so it can never steal a
    /// poke from the input fields or the submit button. It is a plain page-local
    /// text: nothing about the authored page hierarchy, size or scale changes.
    /// </summary>
    private Text EnsureMessageLabel()
    {
        if (messageLabel != null) return messageLabel;

        RectTransform page = pageRoot as RectTransform;
        if (page == null) return null;

        Text reference = FindFieldText();
        if (reference == null)
        {
            BpfLog.Warning(BpfLogCategory.UI,
                "tex_core_m02_input_validation_step.message_font_missing",
                "M02 输入页面没有可复用的中文字体，校验原因只写入日志。",
                this);
            return null;
        }

        GameObject holder = new GameObject(MessageLabelName, typeof(RectTransform), typeof(Text));
        holder.transform.SetParent(page, false);
        holder.transform.SetAsLastSibling();

        Text label = holder.GetComponent<Text>();
        label.font = reference.font;
        label.fontStyle = reference.fontStyle;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = MessageColor;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;

        // A legacy text rasterizes its glyphs at fontSize and the transform then
        // scales that bitmap, so a page-sized font size is drawn as a blur. Ask
        // for a readable raster and scale the whole label down instead.
        float visualHeight = page.rect.height * 0.055f;
        float visualWidth = page.rect.width - 10f;
        float scale = visualHeight / MessageFontSize;
        label.fontSize = MessageFontSize;

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = new Vector3(scale, scale, 1f);
        rect.sizeDelta = new Vector2(visualWidth / scale, visualHeight * 2f / scale);
        // The lower band of the page is free of the two input plates, the submit
        // button and the pop-up keyboard.
        rect.anchoredPosition = new Vector2(0f, -page.rect.height * 0.22f);

        messageLabel = label;
        return label;
    }

    private Text FindFieldText()
    {
        if (inputFields == null) return null;
        for (int index = 0; index < inputFields.Length; index++)
        {
            InputField field = inputFields[index];
            if (field == null) continue;
            if (field.textComponent != null && field.textComponent.font != null) return field.textComponent;
            if (field.placeholder is Text placeholder && placeholder.font != null) return placeholder;
        }

        return null;
    }
}
