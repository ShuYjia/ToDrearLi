using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using Bpf.Diagnostics;

public class TexCore_LoadScene : MonoBehaviour
{
    public static TexCore_LoadScene instance;

    /// <summary>
    /// 场景切换拦截器（联机模块安装）。返回 true 表示网络层已接管，
    /// 本方法直接返回、不做本地加载。单机时为 null，行为与改造前一致。
    /// </summary>
    public static Func<string, bool> SceneLoadInterceptor;

    public const string StartupSceneName = "01StartUp";
    public const string BusinessSceneName = "03SingleplayerScene";
    public const string SingleLevelSceneName = "00SingleplayerScene";
    public const string Test0809SceneName = "0809";
    public const string Test0607SceneName = "0607";
    public const string Test09SceneName = "09";

    private static readonly HashSet<string> StartupAliases = new HashSet<string>
    {
        string.Empty,
        "00Navigation",
        "00LoginPage",
        "01LoginPage",
        "02StartPage",
        "1MainPage",
        "SampleScene"
    };

    private static readonly HashSet<string> BusinessAliases = new HashSet<string>
    {
        "03OperatePage",
        "缁冧範",
        "鑰冩牳"
    };

    public GameObject startCamera;

    public string secondScene = string.Empty;

    public List<GameObject> donotDestroy;

    public RawImage rawImage;

    public GameObject bg;

    public void Awake()
    {
        instance = this;

        if (donotDestroy != null)
        {
            foreach (GameObject item in donotDestroy)
            {
                if (item != null && ShouldPersistAcrossSceneLoad(item))
                {
                    DontDestroyOnLoad(item);
                }
            }
        }
    }

    public void ReturnHome()
    {
        if (secondScene != string.Empty)
        {
            BpfLog.Info(BpfLogCategory.Scene, "tex_core_load_scene.return_home", null, null, BpfLogField.Of("second_scene", secondScene));
            StartCoroutine(UnLoad(secondScene));
            secondScene = string.Empty;
        }
    }

    public void OnlyLoad(string sceneName)
    {
        // 联机会话中：场景切换交网络层（Host 走 NGO 场景管理，Client 只等待）。
        if (SceneLoadInterceptor != null && SceneLoadInterceptor(sceneName))
        {
            return;
        }

        string resolvedSceneName = ResolveSceneName(sceneName);
        if (resolvedSceneName == StartupSceneName)
        {
            foreach (GameObject item in donotDestroy)
            {
                if (item != null)
                {
                    SceneManager.MoveGameObjectToScene(item, SceneManager.GetActiveScene());
                }
            }
        }

        BpfLog.Info(BpfLogCategory.Scene, "tex_core_load_scene.only_load", sceneName + " => " + resolvedSceneName);
        if (resolvedSceneName != SceneManager.GetActiveScene().name)
        {
            PrepareForBusinessSceneLoad(resolvedSceneName);
            SceneManager.LoadScene(resolvedSceneName);
        }
    }

    public string sceneLoading;

    public void LoadScene(string sceneName, bool isOpen = false, bool isClose = false)
    {
        // 联机会话中：场景切换交网络层（Host 走 NGO 场景管理，Client 只等待）。
        if (SceneLoadInterceptor != null && SceneLoadInterceptor(sceneName))
        {
            return;
        }

        string resolvedSceneName = ResolveSceneName(sceneName);
        if (resolvedSceneName == StartupSceneName)
        {
            foreach (GameObject item in donotDestroy)
            {
                if (item != null)
                {
                    SceneManager.MoveGameObjectToScene(item, SceneManager.GetActiveScene());
                }
            }
        }

        if (!SceneManager.GetSceneByName(resolvedSceneName).isLoaded)
        {
            isStart = true;
            sceneLoading = resolvedSceneName;
            PrepareForBusinessSceneLoad(resolvedSceneName);
        }
    }

    public bool isStart = false;
    public bool isLoading = false;
    public bool isClose = false;
    public float startItem = 0;
    public float closeItem = 0;

    public void FixedUpdate()
    {
        if (isStart)
        {
            if (rawImage != null)
            {
                if (startItem < 1)
                {
                    startItem += 0.1f;
                    rawImage.color = Color.Lerp(Color.clear, Color.black, startItem);
                }
                else
                {
                    rawImage.color = Color.black;
                    isStart = false;
                    startItem = 0f;
                    if (SceneManager.GetActiveScene().name != sceneLoading)
                    {
                        SceneManager.LoadSceneAsync(sceneLoading);
                    }

                    isLoading = true;
                }
            }
        }

        if (isLoading && SceneManager.GetActiveScene().name == sceneLoading)
        {
            isLoading = false;
            isClose = true;
        }

        if (isClose)
        {
            if (isStart)
            {
                isStart = false;
                startItem = 0f;
            }

            if (rawImage != null)
            {
                if (closeItem < 1)
                {
                    closeItem += 0.04f;
                    rawImage.color = Color.Lerp(Color.black, Color.clear, closeItem);
                }
                else
                {
                    rawImage.color = Color.clear;
                    closeItem = 0f;
                    isClose = false;
                }
            }
        }
    }

    public void Loading(GameObject nomalObj)
    {
        isClose = true;
        isLoading = false;
        Instantiate(nomalObj, null);
    }

    public IEnumerator Load(string sceneName)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(ResolveSceneName(sceneName));
        yield return asyncOperation;
        if (bg != null && bg.activeSelf)
        {
            bg.SetActive(false);
        }
    }

    public IEnumerator Load(string sceneName, bool isOpen = true, bool isClose = true)
    {
        if (Camera.main != null)
        {
            Camera.main.transform.position = Vector3.one * 10;
            Camera.main.transform.rotation = Quaternion.identity;
        }

        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(ResolveSceneName(sceneName));
        asyncOperation.allowSceneActivation = true;

        yield return asyncOperation;
        this.isClose = isClose;

        BpfLog.Info(BpfLogCategory.Scene, "scene.transition_completed", "杞満瀹屾垚", null, BpfLogField.Of("scene", sceneName));
    }

    IEnumerator UnLoad(string scecneName)
    {
        yield return SceneManager.UnloadSceneAsync(ResolveSceneName(scecneName));
    }

    public static string ResolveSceneName(string sceneName)
    {
        if (StartupAliases.Contains(sceneName))
        {
            return StartupSceneName;
        }

        if (BusinessAliases.Contains(sceneName))
        {
            return BusinessSceneName;
        }

        return sceneName;
    }

    public static bool IsBusinessScene(string sceneName)
    {
        return sceneName == BusinessSceneName ||
               sceneName == SingleLevelSceneName ||
               sceneName == Test0809SceneName ||
               sceneName == Test0607SceneName ||
               sceneName == Test09SceneName;
    }

    private void PrepareForBusinessSceneLoad(string resolvedSceneName)
    {
        if (!IsBusinessScene(resolvedSceneName))
        {
            return;
        }

        if (rawImage != null)
        {
            rawImage.color = Color.clear;
            rawImage.enabled = false;
        }

        if (bg != null)
        {
            bg.SetActive(false);
        }

        if (donotDestroy == null)
        {
            return;
        }

        for (int i = donotDestroy.Count - 1; i >= 0; i--)
        {
            GameObject item = donotDestroy[i];
            if (item == null)
            {
                donotDestroy.RemoveAt(i);
                continue;
            }

            if (!ShouldPersistAcrossSceneLoad(item))
            {
                item.SetActive(false);
                donotDestroy.RemoveAt(i);
            }
        }
    }

    private static bool ShouldPersistAcrossSceneLoad(GameObject item)
    {
        if (item == null)
        {
            return false;
        }

        if (item.GetComponentInChildren<Camera>(true) != null)
        {
            return false;
        }

        if (item.GetComponentInChildren<XROrigin>(true) != null)
        {
            return false;
        }

        if (item.GetComponentInChildren<Canvas>(true) != null)
        {
            return false;
        }

        if (item.GetComponentInChildren<Graphic>(true) != null)
        {
            return false;
        }

        if (item.GetComponentInChildren<Renderer>(true) != null)
        {
            return false;
        }

        return true;
    }
}
