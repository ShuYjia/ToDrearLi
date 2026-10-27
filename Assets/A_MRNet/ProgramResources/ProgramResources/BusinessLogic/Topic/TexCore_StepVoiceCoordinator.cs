using Bpf.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Single entry point for step narration. Panels report their step changes here
/// and the scene step tree reports its own activations through
/// <see cref="TexCore_EventType.设置物体状态"/>; both paths are deduplicated by
/// <see cref="StepVoiceKey"/> so one step is never announced twice.
/// </summary>
public class TexCore_StepVoiceCoordinator : MonoBehaviour
{
    public static TexCore_StepVoiceCoordinator self;

    private const string HostName = "TexCore_StepVoice";
    private const string OwnStepVoiceSourceName = "StepVoiceSource";

    private StepVoiceKey lastPlayedKey;
    private bool hasLastPlayedKey;
    private bool started;
    private AudioSource ownStepVoiceSource;

    /// <summary>
    /// Returns the shared host, creating it on demand at runtime so step
    /// narration works even in scenes that carry no
    /// <see cref="TexCore_AudioManager"/>. Outside play mode nothing is created:
    /// edit-mode tooling and tests must not spawn a persistent host, so callers
    /// null-check the result.
    /// </summary>
    public static TexCore_StepVoiceCoordinator Self
    {
        get
        {
            if (self != null)
            {
                return self;
            }

            self = FindObjectOfType<TexCore_StepVoiceCoordinator>(true);
            if (self != null || !Application.isPlaying)
            {
                return self;
            }

            GameObject host = new GameObject(HostName);
            self = host.AddComponent<TexCore_StepVoiceCoordinator>();
            return self;
        }
    }

    private void Awake()
    {
        if (self != null && self != this)
        {
            Destroy(this);
            return;
        }

        self = this;
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        if (!started)
        {
            started = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        TexCore_EventCenter.AddListener<int, int, int>(TexCore_EventType.设置物体状态, OnStepObjectsState);
    }

    private void OnDisable()
    {
        // TexCore_StepObj.OnDestroy calls TexCore_EventCenter.RemoveAllListener,
        // which can empty the shared table before this component unregisters.
        // Removing defensively keeps scene teardown and Play Mode exit quiet.
        try
        {
            TexCore_EventCenter.RemoveListener<int, int, int>(
                TexCore_EventType.设置物体状态, OnStepObjectsState);
        }
        catch (System.Exception)
        {
            // The table was already cleared; there is nothing left to remove.
        }
    }

    private void OnDestroy()
    {
        if (started)
        {
            started = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        if (self == this)
        {
            self = null;
        }
    }

    /// <summary>
    /// Reports a panel step change. Callers pass the panel that owns the step so
    /// the narration profile is read from the same object the player is on.
    /// </summary>
    public void NotifyStepChanged(PanelTopicBase panel, int nodeIndex, int topicIndex, int stepIndex)
    {
        if (panel == null)
        {
            return;
        }

        PlayStepVoice(panel, nodeIndex, topicIndex, stepIndex);
    }

    /// <summary>
    /// Reports a step activation that came from the scene step tree. The tree
    /// only carries indexes, so the owning panel is resolved from the currently
    /// active topic.
    /// </summary>
    public void OnStepObjectsState(int nodeIndex, int topicIndex, int stepIndex)
    {
        PanelTopicBase panel = ResolveActiveTopic(topicIndex);
        if (panel == null)
        {
            return;
        }

        PlayStepVoice(panel, nodeIndex, topicIndex, stepIndex);
    }

    /// <summary>
    /// Clears recall state so returning to the current step after leaving the
    /// topic replays its narration.
    /// </summary>
    public void ResetRecall()
    {
        hasLastPlayedKey = false;
    }

    /// <summary>
    /// Stops narration for topic switches, module exit and scene changes.
    /// </summary>
    public void Stop()
    {
        ResetRecall();
        StopStepChannel();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Stop();
    }

    /// <summary>
    /// True when the step must be (re)announced: a different step, or the same
    /// step after recall was cleared by leaving the topic. Repeat events for the
    /// step already playing return false so nothing stacks or restarts.
    /// </summary>
    public static bool ShouldPlay(bool hasLastPlayedKey, StepVoiceKey lastPlayedKey, StepVoiceKey candidate)
    {
        return !hasLastPlayedKey || !lastPlayedKey.Equals(candidate);
    }

    private void PlayStepVoice(PanelTopicBase panel, int nodeIndex, int topicIndex, int stepIndex)
    {
        TexCore_StepVoiceProfile profile = panel.GetComponent<TexCore_StepVoiceProfile>();
        StepVoiceKey key = new StepVoiceKey(
            SceneManager.GetActiveScene().name ?? string.Empty, nodeIndex, topicIndex, stepIndex);

        if (!ShouldPlay(hasLastPlayedKey, lastPlayedKey, key))
        {
            return;
        }

        hasLastPlayedKey = true;
        lastPlayedKey = key;

        AudioClip clip = profile == null ? null : profile.GetClip(stepIndex);
        if (clip == null)
        {
            // Still waiting for a recording. Silence is the intended behavior.
            StopStepChannel();
            return;
        }

        PlayOnStepChannel(clip, profile.volume);
    }

    /// <summary>
    /// Plays on the dedicated step channel. <see cref="TexCore_AudioManager"/>
    /// owns that channel when the scene provides one; otherwise this component
    /// carries its own source, so step narration still works in the mainline
    /// scenes, none of which currently host an audio manager.
    /// </summary>
    private void PlayOnStepChannel(AudioClip clip, float volume)
    {
        TexCore_AudioManager manager = TexCore_AudioManager.self;
        if (manager != null)
        {
            manager.PlayStepVoice(clip, volume * manager.stepVoiceVolume, lastPlayedKey);
            return;
        }

        AudioSource source = EnsureOwnStepVoiceSource();
        if (source == null)
        {
            return;
        }

        if (source.clip == clip && source.isPlaying)
        {
            return;
        }

        source.Stop();
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.Play();
    }

    private void StopStepChannel()
    {
        TexCore_AudioManager manager = TexCore_AudioManager.self;
        if (manager != null)
        {
            manager.StopStepVoice();
        }

        if (ownStepVoiceSource != null)
        {
            ownStepVoiceSource.Stop();
            ownStepVoiceSource.clip = null;
        }
    }

    private AudioSource EnsureOwnStepVoiceSource()
    {
        if (ownStepVoiceSource != null)
        {
            return ownStepVoiceSource;
        }

        Transform existing = transform.Find(OwnStepVoiceSourceName);
        if (existing != null)
        {
            ownStepVoiceSource = existing.GetComponent<AudioSource>();
        }

        if (ownStepVoiceSource == null)
        {
            GameObject channel = new GameObject(OwnStepVoiceSourceName);
            channel.transform.SetParent(transform, false);
            ownStepVoiceSource = channel.AddComponent<AudioSource>();
        }

        ownStepVoiceSource.playOnAwake = false;
        ownStepVoiceSource.loop = false;
        ownStepVoiceSource.spatialBlend = 0f;
        return ownStepVoiceSource;
    }

    private PanelTopicBase ResolveActiveTopic(int topicIndex)
    {
        TexCore_PlaneManager planeManager = TexCore_PlaneManager.self;
        if (planeManager == null || planeManager.activePlane == null)
        {
            return null;
        }

        MutilNodePanelBase multiNode = planeManager.activePlane as MutilNodePanelBase;
        if (multiNode != null)
        {
            if (multiNode.activeChild == null || multiNode.activeChild.Count == 0)
            {
                return null;
            }

            return multiNode.activeChild[0].active;
        }

        NodePanelBase node = planeManager.activePlane as NodePanelBase;
        if (node == null)
        {
            return null;
        }

        PanelTopicBase topic = node.activeChild;
        if (topic == null || topic.datas == null)
        {
            return null;
        }

        return topic.datas.GetIndexSelf() == topicIndex ? topic : null;
    }
}
