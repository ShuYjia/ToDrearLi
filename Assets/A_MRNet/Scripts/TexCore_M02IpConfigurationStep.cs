using System.Collections.Generic;
using Bpf.Diagnostics;
using MixedReality.Toolkit.UX;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reports the original YouXian10 apply button to TexCore only after the
/// authored IP, subnet mask and gateway fields form a valid IPv4 configuration.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_M02IpConfigurationStep : TexCore_InteracterBase, ITexCoreUserCompletionStatus
{
    private InputField address;
    private InputField subnetMask;
    private InputField gateway;
    private PressableButton applyButton;
    private TexCore_ClickObjectStep guidance;
    private readonly List<TexCore_ClickObjectStep> inputGuidances =
        new List<TexCore_ClickObjectStep>();
    private readonly List<PressableButton> inputButtons = new List<PressableButton>();
    private bool stepActive;
    private bool completed;
    private bool userCompleted;

    public bool WasCompletedByUser => userCompleted;

    public void Configure(
        InputField addressField,
        InputField subnetMaskField,
        InputField gatewayField,
        PressableButton submitButton)
    {
        address = addressField;
        subnetMask = subnetMaskField;
        gateway = gatewayField;
        applyButton = submitButton;
        ListenToInputChanges();
        ApplyButtonState();
    }

    /// <summary>
    /// The apply button also carries a guidance-only reporter that draws the
    /// current step highlight. This step owns its lifecycle so the highlight
    /// follows the IP step state instead of the reporter's own state.
    /// </summary>
    public void SetGuidance(TexCore_ClickObjectStep value)
    {
        guidance = value;
    }

    /// <summary>
    /// The three field containers carry guidance-only reporters of their own.
    /// The player is pointed at the addresses first; the apply button is only
    /// highlighted once the whole configuration already passes its rule.
    /// </summary>
    public void AddInputGuidance(TexCore_ClickObjectStep value)
    {
        if (value == null || inputGuidances.Contains(value)) return;
        inputGuidances.Add(value);
        PressableButton button = value.GetComponent<PressableButton>();
        if (button != null && !inputButtons.Contains(button)) inputButtons.Add(button);
    }

    /// <summary>True when the live field text already forms a valid IPv4 setup.</summary>
    public bool InputsAreValid()
    {
        if (address == null || subnetMask == null || gateway == null) return false;
        return TexCore_ServerInstallIpValidation.TryValidate(
            address.text, subnetMask.text, gateway.text, out string _);
    }

    private void Awake()
    {
        applyButton = applyButton != null ? applyButton : GetComponent<PressableButton>();
    }

    public new void OnEnable()
    {
        base.OnEnable();
        applyButton = applyButton != null ? applyButton : GetComponent<PressableButton>();
        if (applyButton != null)
        {
            applyButton.OnClicked.RemoveListener(Submit);
            applyButton.OnClicked.AddListener(Submit);
        }
        ListenToInputChanges();
        RestoreInteraction();
    }

    private void OnDisable()
    {
        if (applyButton != null) applyButton.OnClicked.RemoveListener(Submit);
        RemoveInputListeners();
        base.OnDisabel();
    }

    public override void UnFinished()
    {
        completed = false;
        userCompleted = false;
        stepActive = false;
        ApplyButtonState();
        HideGuidance();
    }

    public override void UnderWay()
    {
        completed = false;
        stepActive = true;
        ApplyButtonState();
        RefreshGuidance();
    }

    public override void OnTrigger()
    {
        completed = true;
        stepActive = false;
        ApplyButtonState();
        HideGuidance();
    }

    public override void Finish()
    {
        completed = true;
        stepActive = false;
        ApplyButtonState();
        HideGuidance();
    }

    private void Submit()
    {
        if (!stepActive || completed) return;
        if (address == null || subnetMask == null || gateway == null)
        {
            ShowValidationMessage("IP配置输入框未找到");
            RefreshGuidance();
            return;
        }

        if (!TexCore_ServerInstallIpValidation.TryValidate(address.text, subnetMask.text, gateway.text, out string error))
        {
            // A wrong configuration keeps the step and its highlight active so
            // the player can correct the addresses.
            ShowValidationMessage(error);
            RefreshGuidance();
            return;
        }

        completed = true;
        userCompleted = true;
        stepActive = false;
        ApplyButtonState();
        HideGuidance();
        // The apply button is the only reporter that completes this step.
        TexCore_BusinessManager.OnClick(gameObject);
    }

    /// <summary>
    /// Keeps exactly one highlight visible: the three address fields while the
    /// configuration still needs work, the apply button once it is valid.
    /// </summary>
    private void RefreshGuidance()
    {
        // Without any field highlight to give, the apply button stays the target
        // so the step is never presented without a hint.
        bool applyReady = stepActive &&
                          (inputGuidances.Count == 0 || InputsAreValid());
        for (int index = 0; index < inputGuidances.Count; index++)
        {
            TexCore_ClickObjectStep inputGuide = inputGuidances[index];
            if (inputGuide == null) continue;
            if (applyReady) HideGuide(inputGuide);
            else ShowGuide(inputGuide);
        }

        if (guidance != null)
        {
            if (applyReady) ShowGuide(guidance);
            else HideGuide(guidance);
        }

        RestoreInteraction();
    }

    /// <summary>
    /// A guidance reporter also toggles the interactable it outlines, so the
    /// interaction state is restored right after the highlight moves: the fields
    /// must stay editable and the apply button must stay pressable even while
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

    private void ListenToInputChanges()
    {
        foreach (InputField field in InputFields())
        {
            field.onValueChanged.RemoveListener(OnInputChanged);
            field.onValueChanged.AddListener(OnInputChanged);
        }
    }

    private void RemoveInputListeners()
    {
        foreach (InputField field in InputFields())
        {
            field.onValueChanged.RemoveListener(OnInputChanged);
        }
    }

    private IEnumerable<InputField> InputFields()
    {
        if (address != null) yield return address;
        if (subnetMask != null) yield return subnetMask;
        if (gateway != null) yield return gateway;
    }

    /// <summary>The highlight follows the live values while the player types.</summary>
    private void OnInputChanged(string value)
    {
        RefreshGuidance();
    }

    private void ApplyButtonState()
    {
        if (applyButton != null) applyButton.enabled = stepActive && !completed;
    }

    /// <summary>
    /// The shared hint channel (显示提示) spawns a modal dialog in this flow,
    /// which covers the page the player has to correct. Show the reason in the
    /// non-modal title text instead and keep the diagnostic log.
    /// </summary>
    private void ShowValidationMessage(string message)
    {
        BpfLog.Warning(BpfLogCategory.UI, "tex_core_m02_ip_configuration_step.rejected", message, this);
        TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, message);
    }
}
