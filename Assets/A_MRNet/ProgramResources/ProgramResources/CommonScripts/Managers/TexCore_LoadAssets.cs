using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Bpf.Diagnostics;

public class TexCore_LoadAssets : MonoBehaviour
{
    public string assetsPath;

    public static TexCore_LoadAssets self;

    public Dictionary<string, AssetBundle> bundels;

    public Image bg;
    public TextMeshProUGUI text;
    public Slider slider;
    public TextMeshProUGUI log;
    public List<TMP_FontAsset> fonts;
    public  bool isloading;

    public void Awake()
    {
        self = this;
        bundels = new Dictionary<string, AssetBundle>();
    }

    public void LoadAssets(string name,string bundlename,string path="",Action<GameObject> action=null)
    {
        if (bundels.ContainsKey(bundlename))
        {
            if (action!=null)
            {
               // Debug.Log("加载已下载资源：" + bundlename + "/"  + name);
                action( bundels[bundlename].LoadAsset<GameObject>(name));
            }
        }
        else
        {
            BpfLog.Info(BpfLogCategory.Asset, "asset.load_started", "开始下载资源："+ assetsPath + "/" + (path.Equals("") ? "" :path )+ "/"+name);
            StartCoroutine(DownLoadAB(assetsPath + "/" + (path.Equals("") ? "" :path ), name, bundlename, true, action));
        }
    }

    public IEnumerator DownLoadAB(string path, string name,string bundlename, bool isLoading = false, Action<GameObject> action = null)
    {
        yield return null;
        //UnityWebRequest获取ab包
        UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(path + "/" + bundlename + ".ab");
        BpfLog.Info(BpfLogCategory.Asset, "asset.download_url", "资源下载地址："+www.url);
        UnityWebRequestAsyncOperation hodel = www.SendWebRequest();

        BpfLog.Info(BpfLogCategory.Asset, "asset.download_started", "正在下载资源包");
        
        if (isLoading)
        {
            float t = 0;
            bg.gameObject.SetActive(true);
            text.gameObject.SetActive(true);
            slider.gameObject.SetActive(true);
            log.gameObject.SetActive(true);
            log.text = "正在下载资源:"+bundlename;
            //Debug.Log((hodel.progress < 0.97f) + "---" + (hodel.isDone == false));

            while (hodel.progress < 0.97f && hodel.isDone == false)
            {
                t = hodel.progress * 0.8f;
                //Debug.Log("t=" + t + "----" + slider.value);
                if (hodel.progress < 0.97f)
                {
                    text.text = (t * 100f).ToString("0") + "%";
                    slider.value = t;
                    yield return new WaitForFixedUpdate();
                }
            }
            t = 0.8f;
            while (t <= 0.98f&& hodel.isDone==false)
            {
                t += 0.01f;
                text.text = (t * 100f).ToString("0") + "%";
                slider.value = t;
                yield return new WaitForFixedUpdate();
            }
        }
        yield return hodel;
        BpfLog.Info(BpfLogCategory.Asset, "asset.download_completed", "资源包下载完成");
        if (www.isNetworkError || www.isHttpError)
        {
            BpfLog.Info(BpfLogCategory.Asset, "asset.download_failed", null, null, BpfLogField.Of("error", www.error));
        }
        else
        {
            BpfLog.Info(BpfLogCategory.Asset, "asset.extract_started", "正在解析资源包");
            
            log.text = "正在解压:"+bundlename;
            //获取ab包内容
            yield return new WaitForFixedUpdate();
            AssetBundle ab = DownloadHandlerAssetBundle.GetContent(www);
            yield return new WaitForFixedUpdate();
            BpfLog.Info(BpfLogCategory.Asset, "asset.extract_completed", "解析资源包完成");

            bundels.Add(bundlename, ab);
            //创建指定名称的ab包模型，其中cube代表ab包中模型的名称
            GameObject newObj = ab.LoadAsset<GameObject>(name);
            if (action != null)
            {
                ReloadFont(newObj);//替换字体资源
                action(newObj);  
            }
            StartCoroutine(HodelClose());
        }
        isloading = false;

    }
    IEnumerator HodelClose()
    {
        yield return new WaitForSeconds(1f);
        if (isloading == false)
        {
            bg.gameObject.SetActive(false);
            text.gameObject.SetActive(false);
            slider.gameObject.SetActive(false);
            log.gameObject.SetActive(false);
        }
    }

    public void OnDestroy()
    {
        foreach (AssetBundle item in bundels.Values)
        {
            item.Unload(true);
        }
        bundels = null;
    }

    public void ReloadFont(GameObject gameObject)
    {
        TextMeshProUGUI[] texts= gameObject.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (TextMeshProUGUI item in texts)
        {
            item.font = fonts[0];
        }
    }
}
