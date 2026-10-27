using Bpf.Diagnostics;
/****************************************************
    文件名：        MultilevelMenu.cs

    功能：          菜单管理
*****************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

[Serializable]
public class MenuData
{
    public string menuName;

    public ShowDatas showDatas;

    public List<MenuData> datas;
}

[Serializable]
public class ShowDatas
{
    public List<ShowData> selfs;
}

[Serializable]
public class ShowData
{
    public enum DataEnem
    {
        纯文本,
        图片,
        视频
    }

    public DataEnem dataEnem;
    public string str;
}

public enum GenerationType
{
    正常,
    其他
}

public class MultilevelMenu : MonoBehaviour
{
    public List<MenuData> menuDatas;
    [Space()]
    public bool isPlay;

    public GenerationType generationType;

    [Header("左边")]
    public Transform prfRoot;
    public GameObject prf;
    public GameObject prf_1;
    public GameObject prf_2;

    [Header("右边")]
    public GameObject prf_Text;
    public GameObject prf_Image;
    public GameObject prf_Video;
    public Transform showRoot;
    public string path;

    private void OnEnable()
    {
        GenerateMenu(menuDatas, prfRoot);
        if (prfRoot.childCount > 0)
        {
            prfRoot.GetChild(0).GetComponent<TexCore_Interaction_UGUI>().SetState(SelectState.Click);
        }
    }

    public void OnClick(TexCore_Interaction_UGUI interaction_UGUI)
    {
        BpfLog.Info(BpfLogCategory.Asset, "multilevel_menu.on_click", null, null, BpfLogField.Of("name", interaction_UGUI.name));
        switch (generationType)
        {
            case GenerationType.正常:
                MenuData menuData = GetData(interaction_UGUI.name);
                if (menuData != null)
                {
                    GeneratedData(menuData.showDatas);
                }
                break;
            case GenerationType.其他:
                foreach (Transform data in showRoot)
                {
                    data.gameObject.SetActive(data.name.Equals(interaction_UGUI.name));
                }
                break;
        }
    }

    private void GenerateMenu(List<MenuData> sourceDatas, Transform trParent)
    {
        if (!isPlay || sourceDatas == null)
        {
            return;
        }

        for (int i = 0; i < sourceDatas.Count; i++)
        {
            if (sourceDatas[i].datas != null && sourceDatas[i].datas.Count > 0)
            {
                prf = prf_1;
            }
            else
            {
                prf = prf_2;
            }

            GameObject menuItem = Instantiate(prf, trParent);
            TexCore_Interaction_UGUI intUGUI = menuItem.GetComponentInChildren<TexCore_Interaction_UGUI>();
            intUGUI.itemParent = trParent;
            intUGUI.name = sourceDatas[i].menuName;
            intUGUI.GetComponentInChildren<TextMeshProUGUI>().text = sourceDatas[i].menuName;
            menuItem.SetActive(true);

            if (sourceDatas[i].datas != null && sourceDatas[i].datas.Count > 0)
            {
                GenerateMenu(sourceDatas[i].datas, intUGUI.clickChild.transform);
            }
        }

        isPlay = false;
    }

    public void GeneratedData(ShowDatas targetShowDatas)
    {
        foreach (Transform item in showRoot)
        {
            Destroy(item.gameObject);
        }

        if (targetShowDatas == null || targetShowDatas.selfs == null)
        {
            return;
        }

        foreach (ShowData item in targetShowDatas.selfs)
        {
            switch (item.dataEnem)
            {
                case ShowData.DataEnem.纯文本:
                    GameObject textObject = Instantiate(prf_Text, showRoot);
                    textObject.GetComponent<TextMeshProUGUI>().text = item.str;
                    textObject.SetActive(true);
                    break;
                case ShowData.DataEnem.图片:
                    GameObject imageObject = Instantiate(prf_Image, showRoot);
                    Image image = imageObject.GetComponent<Image>();
                    if (image != null)
                    {
                        StartCoroutine(LoadImageFromStreamingAssets(item.str, image));
                    }

                    imageObject.SetActive(true);
                    break;
                case ShowData.DataEnem.视频:
                    GameObject videoObject = Instantiate(prf_Video, showRoot);
                    VideoPlayer videoPlayer = videoObject.GetComponent<VideoPlayer>();
                    if (videoPlayer != null)
                    {
                        StartCoroutine(LoadVideoFromStreamingAssets(item.str, videoPlayer));
                    }

                    videoObject.SetActive(true);
                    break;
                default:
                    BpfLog.Warning(BpfLogCategory.Asset, "multilevel_menu.generated_data", "未处理的 ShowData 类型: " + item.dataEnem);
                    break;
            }
        }
    }

    private IEnumerator LoadImageFromStreamingAssets(string fileName, Image imgComponent)
    {
        string relativePath = CombineRelativePath(path, fileName);
        string resolvedUrl = relativePath;
        if (Application.platform == RuntimePlatform.Android)
        {
            yield return TexCore_RuntimeAssetService.ResolveStreamingAssetUrl(relativePath, url => resolvedUrl = url);
        }
        else
        {
            resolvedUrl = TexCore_RuntimeAssetService.GetStreamingAssetUrl(relativePath);
        }

        if (string.IsNullOrEmpty(resolvedUrl))
        {
            BpfLog.Error(BpfLogCategory.Asset, "multilevel_menu.load_image_from_streaming_assets", "无法加载图片: " + relativePath);
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(resolvedUrl))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                BpfLog.Error(BpfLogCategory.Asset, "multilevel_menu.load_image_from_streaming_assets", "无法加载图片: " + relativePath + " 错误: " + request.error);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null)
            {
                yield break;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            imgComponent.sprite = sprite;

            RectTransform rectTransform = imgComponent.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
            }
        }
    }

    private IEnumerator LoadVideoFromStreamingAssets(string fileName, VideoPlayer videoPlayer)
    {
        string resolvedPath = fileName;
        if (Application.platform == RuntimePlatform.Android)
        {
            yield return TexCore_RuntimeAssetService.ResolveStreamingAssetPath(fileName, pathValue => resolvedPath = pathValue);
        }
        else
        {
            resolvedPath = TexCore_RuntimeAssetService.GetStreamingAssetPath(fileName);
        }

        if (string.IsNullOrEmpty(resolvedPath))
        {
            BpfLog.Error(BpfLogCategory.Asset, "multilevel_menu.load_video_from_streaming_assets", "无法加载视频: " + fileName);
            yield break;
        }

        videoPlayer.url = resolvedPath;
        videoPlayer.Play();
    }

    private static string CombineRelativePath(string rootPath, string fileName)
    {
        if (string.IsNullOrEmpty(rootPath))
        {
            return fileName;
        }

        return rootPath.TrimEnd('/', '\\') + "/" + fileName.TrimStart('/', '\\');
    }

    public MenuData GetData(string name)
    {
        return FindDataByName(name, menuDatas);
    }

    private MenuData FindDataByName(string name, List<MenuData> datas)
    {
        if (datas == null)
        {
            return null;
        }

        foreach (MenuData data in datas)
        {
            if (data.menuName == name)
            {
                return data;
            }

            if (data.datas != null && data.datas.Count > 0)
            {
                MenuData childData = FindDataByName(name, data.datas);
                if (childData != null)
                {
                    return childData;
                }
            }
        }

        return null;
    }
}
