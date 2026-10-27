using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Bpf.Diagnostics;

public class TexCore_RuntimeAssetService : MonoBehaviour
{
    public static TexCore_RuntimeAssetService Instance { get; private set; }

    [SerializeField]
    private List<string> startupFiles = new List<string>
    {
        "Config.xml",
        "Spaces/cover_image_small.png"
    };

    private readonly HashSet<string> copiedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private string runtimeRoot;
    private bool initialized;

    public static bool IsReady => Instance != null && Instance.initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
#if TEXCORE_PURE_PROBE_BUILD
        return;
#endif

        if (Instance != null)
        {
            return;
        }

        GameObject bootstrap = new GameObject("TexCoreRuntimeServices");
        bootstrap.AddComponent<TexCore_RuntimeAssetService>();
        bootstrap.AddComponent<TexCore_InputFieldBridgeInstaller>();
        bootstrap.AddComponent<TexCore_RuntimePlatformBootstrap>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        runtimeRoot = Application.platform == RuntimePlatform.Android ?
            Path.Combine(Application.persistentDataPath, "StreamingAssets") :
            Application.streamingAssetsPath;

        DontDestroyOnLoad(gameObject);
        StartCoroutine(InitializeRuntimeAssets());
    }

    public static string GetStreamingAssetPath(string relativePath)
    {
        string normalizedPath = NormalizeRelativePath(relativePath);
        return Path.Combine(InstanceOrThrow().runtimeRoot, normalizedPath);
    }

    public static string GetStreamingAssetUrl(string relativePath)
    {
        string fullPath = GetStreamingAssetPath(relativePath);
        return BuildFileUrl(fullPath);
    }

    public static IEnumerator EnsureStreamingAsset(string relativePath)
    {
        TexCore_RuntimeAssetService service = InstanceOrThrow();
        yield return service.EnsureFileCopied(normalizePath: NormalizeRelativePath(relativePath));
    }

    public static IEnumerator ResolveStreamingAssetUrl(string relativePath, Action<string> onResolved)
    {
        string normalizedPath = NormalizeRelativePath(relativePath);
        yield return EnsureStreamingAsset(normalizedPath);
        if (Application.platform == RuntimePlatform.Android && !HasStreamingAsset(normalizedPath))
        {
            onResolved?.Invoke(null);
            yield break;
        }

        onResolved?.Invoke(GetStreamingAssetUrl(normalizedPath));
    }

    public static IEnumerator ResolveStreamingAssetPath(string relativePath, Action<string> onResolved)
    {
        string normalizedPath = NormalizeRelativePath(relativePath);
        yield return EnsureStreamingAsset(normalizedPath);
        if (Application.platform == RuntimePlatform.Android && !HasStreamingAsset(normalizedPath))
        {
            onResolved?.Invoke(null);
            yield break;
        }

        onResolved?.Invoke(GetStreamingAssetPath(normalizedPath));
    }

    public static bool HasStreamingAsset(string relativePath)
    {
        return File.Exists(GetStreamingAssetPath(relativePath));
    }

    private IEnumerator InitializeRuntimeAssets()
    {
        if (initialized)
        {
            yield break;
        }

        if (Application.platform == RuntimePlatform.Android)
        {
            Directory.CreateDirectory(runtimeRoot);
            foreach (string relativePath in startupFiles)
            {
                yield return EnsureFileCopied(relativePath);
            }
        }

        initialized = true;
    }

    private IEnumerator EnsureFileCopied(string normalizePath)
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            yield break;
        }

        if (copiedFiles.Contains(normalizePath))
        {
            yield break;
        }

        string targetPath = Path.Combine(runtimeRoot, normalizePath);
        if (File.Exists(targetPath))
        {
            copiedFiles.Add(normalizePath);
            yield break;
        }

        string targetDirectory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        string sourceUrl = BuildStreamingAssetsSourceUrl(normalizePath);
        using (UnityWebRequest request = UnityWebRequest.Get(sourceUrl))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                BpfLog.Error(BpfLogCategory.TexCore, "tex_core_runtime_asset_service.ensure_file_copied", "Failed to prepare runtime asset: " + normalizePath + " | " + request.error);
                yield break;
            }

            File.WriteAllBytes(targetPath, request.downloadHandler.data);
            copiedFiles.Add(normalizePath);
        }
    }

    private string BuildStreamingAssetsSourceUrl(string normalizedPath)
    {
        string basePath = Application.streamingAssetsPath.Replace("\\", "/").TrimEnd('/');
        return basePath + "/" + normalizedPath.Replace("\\", "/");
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        return relativePath.Replace("\\", "/").TrimStart('/');
    }

    private static TexCore_RuntimeAssetService InstanceOrThrow()
    {
        if (Instance == null)
        {
            throw new InvalidOperationException("TexCore_RuntimeAssetService has not been initialized.");
        }

        return Instance;
    }

    private static string BuildFileUrl(string fullPath)
    {
        if (fullPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            return fullPath;
        }

        return new Uri(fullPath).AbsoluteUri;
    }
}
