using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using Bpf.Diagnostics;

public class TexCore_VideoManager : MonoBehaviour
{
    public static TexCore_VideoManager self;

    /// <summary>
    /// 联机音视频同步拦截器（BpfSessionAVSync 安装，阶段 4）。
    /// 参数：事件类别 / 资产名 / 数值参数。观察者模式，不影响本地播放。
    /// 单机时为 null，行为与改造前一致。
    /// </summary>
    public static System.Action<string, string, double> AvPlayInterceptor;

    public VideoPlayer videoPlayer;

    public RenderTexture renderTexture;

    public GameObject texture;

    public bool isPlayOnAwake = false;

    public string videoPath;

    public bool isPlay = true;

    public float sourceValue = 1f;

    private void Awake()
    {
        self = this;
        videoPath = videoPath.Trim('/').Trim('\\');
        texture.SetActive(isPlayOnAwake);
        videoPlayer.playOnAwake = isPlayOnAwake;
    }

    public void SetSize(int w, int h)
    {
        if (renderTexture == null)
        {
            return;
        }

        renderTexture.Release();
        renderTexture.width = w;
        renderTexture.height = h;
        renderTexture.Create();
    }

    public void Play(string videoName)
    {
        AvPlayInterceptor?.Invoke("video", videoName, 0);

        StartCoroutine(PlayVideo(videoName));
    }

    private IEnumerator PlayVideo(string videoName)
    {
        string relativePath = BuildVideoAssetPath(videoName);
        string resolvedPath = relativePath;

        if (Application.platform == RuntimePlatform.Android)
        {
            yield return TexCore_RuntimeAssetService.ResolveStreamingAssetPath(relativePath, path => resolvedPath = path);
        }
        else
        {
            resolvedPath = TexCore_RuntimeAssetService.GetStreamingAssetPath(relativePath);
        }

        if (string.IsNullOrEmpty(resolvedPath))
        {
            BpfLog.Warning(BpfLogCategory.TexCore, "tex_core_video_manager.play_video", "Video asset not found: " + relativePath);
            yield break;
        }

        videoPlayer.enabled = true;
        videoPlayer.url = resolvedPath;
        videoPlayer.Play();
        videoPlayer.SetDirectAudioVolume(0, sourceValue);
        texture.SetActive(true);
    }

    private string BuildVideoAssetPath(string videoName)
    {
        return videoPath + "/" + videoName;
    }

    public void SetSize()
    {
        if (texture == null)
        {
            return;
        }

        Vector2 nomalSize = texture.GetComponent<RectTransform>().sizeDelta;
        float w = videoPlayer.width;
        float h = videoPlayer.height;
        if (w <= 0 || h <= 0)
        {
            return;
        }

        if (w > h)
        {
            float i = w / h;
            if (i < 1.3f)
            {
                float t = nomalSize.y / h;
                h = nomalSize.y * t;
            }
            else
            {
                float t = nomalSize.x / w;
                w = nomalSize.x * t;
            }
        }
        else
        {
            float t = nomalSize.y / h;
            w = nomalSize.x * t;
        }

        SetSize((int)w, (int)h);
    }

    public void Exit()
    {
        AvPlayInterceptor?.Invoke("videostop", string.Empty, 0);

        videoPlayer.enabled = false;
        texture.SetActive(false);
    }

    public void SetPlay()
    {
        // 联机广播取切换后的目标状态（此刻 isPlay 还是旧值）
        AvPlayInterceptor?.Invoke(isPlay ? "videopause" : "videoresume", string.Empty, 0);

        if (isPlay)
        {
            isPlay = false;
            videoPlayer.Pause();
        }
        else
        {
            isPlay = true;
            videoPlayer.Play();
        }
    }

    /// <summary>联机同步用：直接暂停（不带切换语义，避免状态错位）。</summary>
    public void PauseDirect()
    {
        isPlay = false;
        videoPlayer.Pause();
    }

    /// <summary>联机同步用：直接续播。</summary>
    public void ResumeDirect()
    {
        isPlay = true;
        videoPlayer.Play();
    }
}
