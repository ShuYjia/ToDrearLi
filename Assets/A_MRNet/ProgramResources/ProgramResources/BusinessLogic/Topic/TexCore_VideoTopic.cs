using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Bpf.Diagnostics;

/// <summary>
/// A non-scored BPF topic that plays one video and then advances to the next topic.
/// </summary>
public class TexCore_VideoTopic : PanelTopicBase, IGetData
{
    public VideoPlayer videoPlayer;
    public RawImage videoSurface;
    public GameObject btn_Skip;

    private bool isCompleted;
    private RenderTexture runtimeVideoTexture;

    private void Awake()
    {
        ResolveReferences();
    }

    public override void Start()
    {
        if (datas != null && datas.selfDatas != null && datas.selfDatas.Count > 0)
        {
            topicData = datas.selfDatas[0] as DataClass_Topic;
        }
    }

    public override void OnEnable()
    {
        isCompleted = false;
        ResolveReferences();
        RegisterVideoEvents();

        if (datas != null)
        {
            TexCore_EventCenter.Broadcast<string>(TexCore_EventType.更新题目, datas.name);
        }

        TexCore_EventCenter.Broadcast(TexCore_EventType.关闭提示);

        if (videoSurface != null)
        {
            videoSurface.texture = null;
        }

        if (videoPlayer == null || videoPlayer.clip == null)
        {
            BpfLog.Warning(BpfLogCategory.Scene, "tex_core_video_topic.on_enable", "Video topic has no VideoClip: " + name);
            Invoke(nameof(CompleteTopic), 0f);
            return;
        }

        if (!ConfigureWorldSpacePlacement())
        {
            Invoke(nameof(CompleteTopic), 0f);
            return;
        }

        ConfigureVideoOutput();
        videoPlayer.Stop();
        videoPlayer.Prepare();
    }

    public override void OnDisable()
    {
        CancelInvoke(nameof(CompleteTopic));
        UnregisterVideoEvents();

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (videoSurface != null)
        {
            videoSurface.texture = null;
        }

        ReleaseVideoOutput();
    }

    public override void OnClick(GameObject obj)
    {
        if (obj != null && obj.name == "跳过")
        {
            Skip();
        }
    }

    public void Skip()
    {
        CompleteTopic();
    }

    private void ResolveReferences()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoSurface == null)
        {
            videoSurface = GetComponentInChildren<RawImage>(true);
        }

        if (btn_Skip == null)
        {
            Transform skip = transform.Find("跳过");
            if (skip != null)
            {
                btn_Skip = skip.gameObject;
            }
        }
    }

    private void RegisterVideoEvents()
    {
        if (videoPlayer == null)
        {
            return;
        }

        UnregisterVideoEvents();
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.frameReady += OnVideoFrameReady;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.sendFrameReadyEvents = true;
    }

    private void UnregisterVideoEvents()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.frameReady -= OnVideoFrameReady;
        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.sendFrameReadyEvents = false;
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        if (isCompleted || !isActiveAndEnabled)
        {
            return;
        }

        BindVideoTexture(source);
        source.Play();
    }

    private void OnVideoFrameReady(VideoPlayer source, long frameIndex)
    {
        BindVideoTexture(source);
    }

    private void BindVideoTexture(VideoPlayer source)
    {
        if (videoSurface == null)
        {
            return;
        }

        Texture texture = runtimeVideoTexture != null
            ? runtimeVideoTexture
            : source.texture;
        if (texture == null)
        {
            return;
        }

        videoSurface.texture = texture;
        source.sendFrameReadyEvents = false;
    }

    private void ConfigureVideoOutput()
    {
        ReleaseVideoOutput();

        int width = Mathf.Max(1, (int)videoPlayer.clip.width);
        int height = Mathf.Max(1, (int)videoPlayer.clip.height);
        runtimeVideoTexture = new RenderTexture(
            width,
            height,
            0,
            RenderTextureFormat.ARGB32);
        runtimeVideoTexture.name = name + "_VideoTexture";
        runtimeVideoTexture.Create();

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = runtimeVideoTexture;
        videoSurface.texture = runtimeVideoTexture;

        AspectRatioFitter fitter = videoSurface.GetComponent<AspectRatioFitter>();
        if (fitter != null)
        {
            fitter.aspectRatio = (float)width / height;
        }
    }

    private bool ConfigureWorldSpacePlacement()
    {
        Canvas canvas = GetComponent<Canvas>();
        RectTransform panel = transform as RectTransform;
        RectTransform anchor = FindLegacyVideoAnchor();

        if (canvas == null || panel == null || anchor == null)
        {
            BpfLog.Warning(BpfLogCategory.Scene, "tex_core_video_topic.configure_world_space_placement", "Video topic has no World Space placement anchor: " + name);
            return false;
        }

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        }

        MatchWorldRect(panel, anchor);

        VideoPlayer legacyPlayer = anchor.GetComponentInChildren<VideoPlayer>(true);
        if (legacyPlayer != null)
        {
            legacyPlayer.Stop();
        }
        anchor.gameObject.SetActive(false);

        return true;
    }

    private RectTransform FindLegacyVideoAnchor()
    {
        VideoPlayer[] scenePlayers = Resources.FindObjectsOfTypeAll<VideoPlayer>();
        for (int i = 0; i < scenePlayers.Length; i++)
        {
            VideoPlayer candidate = scenePlayers[i];
            if (candidate == videoPlayer || candidate.clip != videoPlayer.clip ||
                candidate.GetComponent<TexCore_VideoTopic>() != null ||
                !candidate.gameObject.scene.IsValid() || candidate.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            Canvas candidateCanvas = candidate.GetComponentInParent<Canvas>(true);
            if (candidateCanvas == null || candidateCanvas.renderMode != RenderMode.WorldSpace)
            {
                continue;
            }

            return candidate.transform.parent as RectTransform;
        }

        return null;
    }

    private void MatchWorldRect(RectTransform panel, RectTransform anchor)
    {
        Vector3[] anchorCorners = new Vector3[4];
        anchor.GetWorldCorners(anchorCorners);
        float anchorWidth = Vector3.Distance(anchorCorners[0], anchorCorners[3]);
        float anchorHeight = Vector3.Distance(anchorCorners[0], anchorCorners[1]);

        panel.position = anchor.TransformPoint(anchor.rect.center);
        panel.rotation = anchor.rotation;
        panel.localScale = Vector3.one;

        Canvas.ForceUpdateCanvases();
        Vector3[] panelCorners = new Vector3[4];
        panel.GetWorldCorners(panelCorners);
        float panelWidth = Vector3.Distance(panelCorners[0], panelCorners[3]);
        float panelHeight = Vector3.Distance(panelCorners[0], panelCorners[1]);
        float scaleX = panelWidth > 0f ? anchorWidth / panelWidth : 1f;
        float scaleY = panelHeight > 0f ? anchorHeight / panelHeight : 1f;
        panel.localScale = new Vector3(scaleX, scaleY, Mathf.Min(scaleX, scaleY));
    }

    private void ReleaseVideoOutput()
    {
        if (videoPlayer != null && videoPlayer.targetTexture == runtimeVideoTexture)
        {
            videoPlayer.targetTexture = null;
        }

        if (runtimeVideoTexture == null)
        {
            return;
        }

        runtimeVideoTexture.Release();
        Destroy(runtimeVideoTexture);
        runtimeVideoTexture = null;
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        CompleteTopic();
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        BpfLog.Error(BpfLogCategory.Scene, "tex_core_video_topic.on_video_error", "Video topic playback failed: " + message);
        CompleteTopic();
    }

    private void CompleteTopic()
    {
        if (isCompleted || !isActiveAndEnabled)
        {
            return;
        }

        isCompleted = true;
        UnregisterVideoEvents();

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (datas != null)
        {
            TexCore_EventCenter.Broadcast<string>(TexCore_EventType.完成题目, datas.name);
        }

        NodePanelBase node = controlPlane as NodePanelBase;
        if (node == null && controlPlane != null)
        {
            node = controlPlane.GetComponent<NodePanelBase>();
        }

        if (node != null)
        {
            node.NextTopic();
        }
        else
        {
            BpfLog.Error(BpfLogCategory.Scene, "tex_core_video_topic.complete_topic", "Video topic has no owning NodePanelBase: " + name);
        }
    }

    public TexCore_DataClass GetData()
    {
        datas.selfDatas = new List<SelfDataBase> { topicData };
        return datas;
    }
}
