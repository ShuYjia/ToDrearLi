using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-topic step narration. One slot per real step, addressed by step index.
/// Slots are addressed strictly by index, so a missing recording stays
/// <c>null</c> instead of shifting later recordings onto the wrong step.
/// </summary>
public class TexCore_StepVoiceProfile : MonoBehaviour
{
    [Header("步骤语音（索引严格对应 stepIndex）")]
    public List<AudioClip> stepAudioClips = new List<AudioClip>();

    [Range(0f, 1f)]
    public float volume = 1f;

    /// <summary>
    /// Number of narration slots. Independent from the step count so a
    /// half-migrated prefab still reports what it actually owns.
    /// </summary>
    public int SlotCount
    {
        get { return stepAudioClips == null ? 0 : stepAudioClips.Count; }
    }

    /// <summary>
    /// Returns the clip for a step, or <c>null</c> when the step has no
    /// recording yet or the index is out of range.
    /// </summary>
    public AudioClip GetClip(int stepIndex)
    {
        if (stepAudioClips == null || stepIndex < 0 || stepIndex >= stepAudioClips.Count)
        {
            return null;
        }

        return stepAudioClips[stepIndex];
    }

    /// <summary>
    /// Resizes the slot list while preserving the clips already assigned to
    /// their step indexes. Used when a topic gains or loses steps.
    /// </summary>
    public void Resize(int stepCount)
    {
        if (stepCount < 0)
        {
            stepCount = 0;
        }

        if (stepAudioClips == null)
        {
            stepAudioClips = new List<AudioClip>();
        }

        while (stepAudioClips.Count < stepCount)
        {
            stepAudioClips.Add(null);
        }

        while (stepAudioClips.Count > stepCount)
        {
            stepAudioClips.RemoveAt(stepAudioClips.Count - 1);
        }
    }
}
