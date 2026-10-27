using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Bpf.Diagnostics;

/// <summary>
/// Identity of one step narration slot. Two events carrying the same key refer
/// to the same step, which is what makes repeat suppression and re-entry after
/// a rollback possible.
/// </summary>
public struct StepVoiceKey
{
    public string sceneName;
    public int nodeIndex;
    public int topicIndex;
    public int stepIndex;

    public StepVoiceKey(string sceneName, int nodeIndex, int topicIndex, int stepIndex)
    {
        this.sceneName = sceneName;
        this.nodeIndex = nodeIndex;
        this.topicIndex = topicIndex;
        this.stepIndex = stepIndex;
    }

    public bool IsValid
    {
        get { return !string.IsNullOrEmpty(sceneName) && stepIndex >= 0; }
    }

    public bool Equals(StepVoiceKey other)
    {
        return string.Equals(sceneName, other.sceneName) &&
            nodeIndex == other.nodeIndex &&
            topicIndex == other.topicIndex &&
            stepIndex == other.stepIndex;
    }

    public override bool Equals(object obj)
    {
        return obj is StepVoiceKey && Equals((StepVoiceKey)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = sceneName == null ? 0 : sceneName.GetHashCode();
            hash = (hash * 397) ^ nodeIndex;
            hash = (hash * 397) ^ topicIndex;
            hash = (hash * 397) ^ stepIndex;
            return hash;
        }
    }

    public override string ToString()
    {
        return sceneName + "#" + nodeIndex + "/" + topicIndex + "/" + stepIndex;
    }
}

public class TexCore_AudioManager : MonoBehaviour
{
    public static TexCore_AudioManager self;

    /// <summary>
    /// 联机音视频同步拦截器（BpfSessionAVSync 安装，阶段 4）。
    /// 参数：事件类别 / 资产名 / 数值参数（步骤语音时为 stepIndex）。
    /// 观察者模式：不影响本地播放，仅由网络层决定是否广播。
    /// 单机时为 null，行为与改造前一致。
    /// </summary>
    public static System.Action<string, string, double> AvPlayInterceptor;

    public List<AudioClip> audioClips;

    public AudioSource audioSource;

    public AudioSource wordSourcce;

    public AudioSource bgmSource;

    /// <summary>
    /// Dedicated channel for per-step narration. Kept separate from
    /// <see cref="audioSource"/> so dialogue, UI and step guidance never share
    /// one AudioSource.
    /// </summary>
    public AudioSource stepVoiceSource;

    public string audioPath = "Audio";

    public float bgmSourceValue = 0.7f;
    public float nomalSourceValue = 1f;

    [Range(0f, 1f)]
    public float stepVoiceVolume = 1f;

    private const string StepVoiceSourceName = "StepVoiceSource";

    private void Awake()
    {
        self = this;
        audioPath = audioPath.Trim('/').Trim('\\');
        audioSource = GetComponent<AudioSource>();
        EnsureStepVoiceSource();
    }

    private void Start()
    {
        if (bgmSource != null)
        {
            StartCoroutine(IELoadExternalAudioWWW(BuildAudioAssetPath("背景音乐"), bgmSource, bgmSourceValue));
        }
    }

    public void OnEnable()
    {
        TexCore_EventCenter.AddListener<string>((TexCore_EventType)4, PlayAudio);
    }

    public void OnDisable()
    {
        audioSource.enabled = false;
        StopStepVoice();
        TexCore_EventCenter.RemoveListener<string>((TexCore_EventType)4, PlayAudio);
    }

    public void Exit()
    {
        audioSource.enabled = false;
        StopStepVoice();
    }

    public void PlayBGM(string audioName)
    {
        AvPlayInterceptor?.Invoke("bgm", audioName, 0);

        bgmSource.enabled = true;
        foreach (AudioClip item in audioClips)
        {
            if (item.name.Equals(audioName))
            {
                bgmSource.clip = item;
                bgmSource.Play();
                bgmSource.volume = bgmSourceValue;
                return;
            }
        }

        StartCoroutine(IELoadExternalAudioWWW(BuildAudioAssetPath(audioName), bgmSource, bgmSourceValue));
    }

    public void PlayAudio(AudioClip clip)
    {
        audioSource.enabled = true;
        audioSource.clip = clip;
        audioSource.Play();
        audioSource.volume = nomalSourceValue;
    }

    public void PlayWordAudio(AudioClip clip)
    {
        wordSourcce.clip = clip;
        wordSourcce.Play();
    }

    public void StopWordAudio()
    {
        wordSourcce.Stop();
    }

    /// <summary>
    /// Plays the narration for one step on the dedicated step-voice channel.
    /// Re-entering the same step is a no-op so duplicate step events cannot
    /// restart or stack the clip. A null clip means the step is still waiting
    /// for a recording and stays silent without logging an error.
    /// </summary>
    public void PlayStepVoice(AudioClip clip, float volume, StepVoiceKey key)
    {
        if (clip == null)
        {
            StopStepVoice();
            return;
        }

        EnsureStepVoiceSource();
        if (stepVoiceSource == null)
        {
            return;
        }

        if (stepVoiceSource.clip == clip && stepVoiceSource.isPlaying)
        {
            return;
        }

        // 联机广播放在"确实要播"之后：重复进入同一步骤不会重启对端播放。
        AvPlayInterceptor?.Invoke("stepvoice", clip.name, key.stepIndex);

        stepVoiceSource.Stop();
        stepVoiceSource.clip = clip;
        stepVoiceSource.volume = Mathf.Clamp01(volume);
        stepVoiceSource.Play();        BpfLog.Info(BpfLogCategory.TexCore, "tex_core_audio_manager.play_step_voice",
            "步骤语音：" + key, null, BpfLogField.Of("clip", clip.name));
    }

    /// <summary>
    /// Stops and releases the step-voice channel. Called on topic switch,
    /// module exit and scene transition.
    /// </summary>
    public void StopStepVoice()
    {
        AvPlayInterceptor?.Invoke("stepvoicestop", string.Empty, 0);

        if (stepVoiceSource == null)
        {
            return;
        }

        stepVoiceSource.Stop();
        stepVoiceSource.clip = null;
    }

    private void EnsureStepVoiceSource()
    {
        if (stepVoiceSource != null)
        {
            return;
        }

        Transform existing = transform.Find(StepVoiceSourceName);
        if (existing != null)
        {
            stepVoiceSource = existing.GetComponent<AudioSource>();
        }

        if (stepVoiceSource == null)
        {
            GameObject channel = new GameObject(StepVoiceSourceName);
            channel.transform.SetParent(transform, false);
            stepVoiceSource = channel.AddComponent<AudioSource>();
        }

        stepVoiceSource.playOnAwake = false;
        stepVoiceSource.loop = false;
        stepVoiceSource.spatialBlend = 0f;
    }

    public void PlayAudio(string audioName)
    {
        AvPlayInterceptor?.Invoke("audio", audioName, 0);

        audioSource.enabled = true;
        foreach (AudioClip item in audioClips)
        {
            if (item.name.Equals(audioName))
            {
                audioSource.clip = item;
                audioSource.Play();
                audioSource.volume = nomalSourceValue;
                return;
            }
        }

        StartCoroutine(IELoadExternalAudioWWW(BuildAudioAssetPath(audioName), audioSource, nomalSourceValue));
    }

    private string BuildAudioAssetPath(string audioName)
    {
        string fileName = audioName.EndsWith(".wav") ? audioName : audioName + ".wav";
        return audioPath + "/" + fileName;
    }

    private IEnumerator IELoadExternalAudioWWW(string relativePath, AudioSource targetSource, float value, AudioType audioType = AudioType.WAV)
    {
        string audioUrl = relativePath;
        if (Application.platform == RuntimePlatform.Android)
        {
            string resolvedUrl = null;
            yield return TexCore_RuntimeAssetService.ResolveStreamingAssetUrl(relativePath, url => resolvedUrl = url);
            audioUrl = resolvedUrl;
        }
        else if (!relativePath.StartsWith("http"))
        {
            audioUrl = TexCore_RuntimeAssetService.GetStreamingAssetUrl(relativePath);
        }

        if (string.IsNullOrEmpty(audioUrl))
        {
            BpfLog.Warning(BpfLogCategory.TexCore, "tex_core_audio_manager.ie_load_external_audio_www", "Audio asset not found: " + relativePath);
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(audioUrl, audioType))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                BpfLog.Info(BpfLogCategory.TexCore, "tex_core_audio_manager.ie_load_external_audio_www", request.error + audioUrl);
                yield break;
            }

            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
            targetSource.clip = audioClip;
            targetSource.Play();
            targetSource.volume = value;
            BpfLog.Info(BpfLogCategory.TexCore, "tex_core_audio_manager.ie_load_external_audio_www", "鎴愬姛鎾斁闊抽" + audioUrl);
        }
    }
}
