using Bpf.Diagnostics;
/****************************************************
    文件名：        KnowledgeManager.cs
    功能：          知识库管理
*****************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

public class KnowledgeManager : MonoBehaviour
{
    private static KnowledgeManager self;

    public static KnowledgeManager Self
    {
        get
        {
            if (self == null)
            {
                self = FindObjectOfType<KnowledgeManager>();
            }

            return self;
        }
        set => self = value;
    }

    public TextMeshProUGUI headText;

    public GameObject textTemplate;
    public GameObject imageTemplate;
    public GameObject videoTemplate;

    public Transform trxtRoot;

    public TMP_InputField findInput;

    public List<Node> knowledges;

    public Node activeNode;

    public bool isDestroy = true;

    private int currentMatchIndex = 0;
    private readonly List<TMP_Text> matchTexts = new List<TMP_Text>();
    private string lastSearchText = string.Empty;
    private readonly Dictionary<TMP_Text, string> originalTexts = new Dictionary<TMP_Text, string>();

    public void SetNode(string name, Node node)
    {
        headText.text = name;
        if (isDestroy)
        {
            ClearChildren();
        }

        if (node != null)
        {
            activeNode = node;
            GetData(node.data, trxtRoot);
        }
    }

    public void SetNode(string name)
    {
        headText.text = name.Length > 2 ? name.Substring(2) : name;
        if (isDestroy)
        {
            ClearChildren();
        }

        activeNode = FindNodeName(name);
        if (activeNode != null)
        {
            GetData(activeNode.data, trxtRoot);
        }
    }

    public void Recover()
    {
        if (activeNode == null)
        {
            return;
        }

        SetNode(activeNode.name);
        findInput.text = string.Empty;

        foreach (Transform item in trxtRoot)
        {
            TMP_Text textComponent = item.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                string originalText = textComponent.text.Replace("<color=#00FF00>", string.Empty).Replace("</color>", string.Empty);
                textComponent.text = originalText;
            }
        }

        matchTexts.Clear();
        currentMatchIndex = 0;
        lastSearchText = string.Empty;
    }

    public Node FindNodeName(string name)
    {
        foreach (Node item in knowledges)
        {
            if (item.name == name)
            {
                return item;
            }
        }

        return null;
    }

    public void FindNode()
    {
        string findName = findInput.text;
        if (string.IsNullOrEmpty(findName))
        {
            return;
        }

        headText.text = findName;
        ClearChildren();

        List<Node> result = new List<Node>();
        foreach (Node item in knowledges)
        {
            if (item.name.Contains(findName))
            {
                result.Add(item);
                continue;
            }

            foreach (Data data in item.data)
            {
                if (data.data.Contains(findName))
                {
                    result.Add(item);
                    break;
                }
            }
        }

        foreach (Node item in result)
        {
            GetData(item.data, trxtRoot);
        }
    }

    public void FindLocalNode()
    {
        string findName = findInput.text;
        if (string.IsNullOrEmpty(findName))
        {
            return;
        }

        matchTexts.RemoveAll(item => item == null);
        if (lastSearchText != findName)
        {
            ClearAllHighlights();
            lastSearchText = findName;
            currentMatchIndex = 0;
            matchTexts.Clear();

            foreach (Transform item in trxtRoot)
            {
                TMP_Text textComponent = item.GetComponent<TextMeshProUGUI>();
                if (textComponent != null && textComponent.text.Contains(findName))
                {
                    if (!originalTexts.ContainsKey(textComponent))
                    {
                        originalTexts[textComponent] = textComponent.text;
                    }

                    matchTexts.Add(textComponent);
                    HighlightText(textComponent, findName);
                }
            }
        }

        if (matchTexts.Count == 0)
        {
            BpfLog.Info(BpfLogCategory.TexCore, "knowledge_manager.find_local_node", "没有找到匹配的内容");
            return;
        }

        TMP_Text currentText = matchTexts[currentMatchIndex];
        ScrollToText(currentText);
        currentMatchIndex = (currentMatchIndex + 1) % matchTexts.Count;
    }

    private void HighlightText(TMP_Text textComponent, string keyword)
    {
        string originalText = originalTexts.ContainsKey(textComponent) ? originalTexts[textComponent] : textComponent.text;
        textComponent.text = originalText.Replace(keyword, $"<color=#00FF00>{keyword}</color>");
    }

    private void ClearAllHighlights()
    {
        foreach (KeyValuePair<TMP_Text, string> kvp in originalTexts)
        {
            if (kvp.Key != null)
            {
                kvp.Key.text = kvp.Value;
            }
        }

        originalTexts.Clear();
    }

    private void ScrollToText(TMP_Text textComponent)
    {
        Canvas.ForceUpdateCanvases();

        RectTransform textRect = textComponent.GetComponent<RectTransform>();
        ScrollRect scrollRect = trxtRoot.GetComponentInParent<ScrollRect>();
        RectTransform scrollContent = scrollRect.content;

        Vector2 viewportLocalPosition = scrollRect.viewport.InverseTransformPoint(textRect.position);
        Vector2 contentLocalPosition = scrollContent.anchoredPosition;
        contentLocalPosition.y -= viewportLocalPosition.y;

        scrollContent.anchoredPosition = contentLocalPosition;
    }

    private void GetData(List<Data> data, Transform parent)
    {
        foreach (Data item in data)
        {
            switch (item.type)
            {
                case DataType.纯文本:
                    TextMeshProUGUI textComponent = Instantiate(textTemplate, parent).GetComponentInChildren<TextMeshProUGUI>();
                    textComponent.gameObject.SetActive(true);
                    textComponent.text = item.data.Replace("\\n", "\n");
                    textComponent.transform.parent.gameObject.SetActive(true);
                    break;
                case DataType.图片:
                    GameObject pic = Instantiate(imageTemplate, parent);
                    pic.SetActive(true);
                    StartCoroutine(LoadTexture(item.data, pic.GetComponentInChildren<RawImage>()));
                    break;
                case DataType.视频:
                    GameObject vie = Instantiate(videoTemplate, parent);
                    vie.SetActive(true);
                    StartCoroutine(LoadVideo(item.data, vie.GetComponent<VideoPlayer>()));
                    break;
            }
        }
    }

    private IEnumerator LoadVideo(string path, VideoPlayer videoPlayer)
    {
        string resolvedPath = path;
        if (Application.platform == RuntimePlatform.Android)
        {
            yield return TexCore_RuntimeAssetService.ResolveStreamingAssetPath(path, value => resolvedPath = value);
        }
        else
        {
            resolvedPath = TexCore_RuntimeAssetService.GetStreamingAssetPath(path);
        }

        if (string.IsNullOrEmpty(resolvedPath))
        {
            BpfLog.Error(BpfLogCategory.TexCore, "knowledge_manager.load_video", "无法加载视频: " + path);
            yield break;
        }

        videoPlayer.url = resolvedPath;
        videoPlayer.Play();
    }

    public IEnumerator LoadTexture(string path, RawImage rawImage)
    {
        string resolvedUrl = path;
        if (Application.platform == RuntimePlatform.Android)
        {
            yield return TexCore_RuntimeAssetService.ResolveStreamingAssetUrl(path, value => resolvedUrl = value);
        }
        else
        {
            resolvedUrl = TexCore_RuntimeAssetService.GetStreamingAssetUrl(path);
        }

        if (string.IsNullOrEmpty(resolvedUrl))
        {
            BpfLog.Error(BpfLogCategory.TexCore, "knowledge_manager.load_texture", "无法加载图片: " + path);
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(resolvedUrl))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                BpfLog.Error(BpfLogCategory.TexCore, "knowledge_manager.load_texture", null, null, BpfLogField.Of("error", request.error));
                yield break;
            }

            Texture texture = DownloadHandlerTexture.GetContent(request);
            rawImage.texture = texture;

            RectTransform rectTransform = rawImage.rectTransform;
            rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
        }
    }

    private void ClearChildren()
    {
        foreach (Transform item in trxtRoot)
        {
            Destroy(item.gameObject);
        }
    }
}

[Serializable]
public class Node
{
    public string name;
    public List<Data> data;
}

public enum DataType
{
    纯文本,
    图片,
    视频
}

[Serializable]
public class Data
{
    [XmlAttribute]
    public DataType type;

    [XmlAttribute]
    public string data;
}
