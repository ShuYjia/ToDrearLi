using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Networking;
using Bpf.Diagnostics;

public class TexCore_ConfigManager : MonoBehaviour
{
    public static TexCore_ConfigManager self;

    public bool isSaveFile = false;
    public bool isLoadFile = false;
    public ConfigleData datas;

    public string savePath;
    public string prefabsPath = "BusinessPrefabs";
    public string configName = "/Config.xml";
    public string interfaceName = "/Interface.xml";

    public bool isSaveInterface = false;
    public bool isLoadInterface = false;

    private void Awake()
    {
        self = this;
        if (datas != null && datas.datas != null)
        {
            datas.datas.SetParent();
        }
        StartCoroutine(InitializeConfig());
    }

    private IEnumerator InitializeConfig()
    {
        while (!TexCore_RuntimeAssetService.IsReady)
        {
            yield return null;
        }

        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
                savePath = TexCore_RuntimeAssetService.GetStreamingAssetPath(string.Empty);

                if (isSaveFile)
                {
                    SaveConfig(savePath + configName);
                }

                if (isSaveInterface)
                {
                    SaveSimpleFile(interfaceName, BackInterface.Self);
                }

                if (isLoadInterface)
                {
                    StartCoroutine(LoadConfigOnWebgl(TexCore_RuntimeAssetService.GetStreamingAssetUrl(interfaceName), file =>
                    {
                        BackInterface.Self = (BackInterface)XmlSerializer.LoadFromByte(file, typeof(BackInterface));
                    }));
                }

                if (isLoadFile)
                {
                    StartCoroutine(LoadConfigOnWebgl(TexCore_RuntimeAssetService.GetStreamingAssetUrl(configName), ConfigleLoadSuccess));
                }

                break;
            case RuntimePlatform.WebGLPlayer:
                savePath = GetIP();

                if (isLoadFile)
                {
                    StartCoroutine(LoadConfigOnWebgl(savePath + configName, ConfigleLoadSuccess));
                }

                if (isLoadInterface)
                {
                    StartCoroutine(LoadConfigOnWebgl(savePath + interfaceName, file =>
                    {
                        BackInterface.Self = (BackInterface)XmlSerializer.LoadFromByte(file, typeof(BackInterface));
                    }));
                }

                break;
            case RuntimePlatform.Android:
                savePath = TexCore_RuntimeAssetService.GetStreamingAssetPath(string.Empty);

                if (isLoadInterface)
                {
                    StartCoroutine(LoadConfigOnWebgl(TexCore_RuntimeAssetService.GetStreamingAssetUrl(interfaceName), file =>
                    {
                        if (file != null)
                        {
                            BackInterface.Self = (BackInterface)XmlSerializer.LoadFromByte(file, typeof(BackInterface));
                        }
                    }));
                }

                if (isLoadFile)
                {
                    StartCoroutine(LoadConfigOnWebgl(TexCore_RuntimeAssetService.GetStreamingAssetUrl(configName), ConfigleLoadSuccess));
                }

                break;
        }
    }

    public void ConfigleLoadSuccess(byte[] file)
    {
        datas = (ConfigleData)XmlSerializer.LoadFromByte(file, typeof(ConfigleData));
        datas.datas.SetParent();

        if (TexCore_PBSManager.self != null)
        {
            TexCore_PBSManager.self.InitPBS();
        }

        if (TexCore_PlaneManager.self != null)
        {
            TexCore_PlaneManager.self.StartLoad();
        }

        if (Page03Data.instance != null)
        {
            Page03Data.instance.StartPanel();
        }

        if (TexCore_HeadMenu.self != null)
        {
            TexCore_HeadMenu.self.InitBtns();
        }
    }

    public void ReName(TexCore_DataClass texCoreData, string id)
    {
        if (texCoreData != null && !string.IsNullOrEmpty(texCoreData.name))
        {
            texCoreData.name = id + texCoreData.name;
            if (texCoreData.childData != null && texCoreData.childData.Count > 0)
            {
                for (int i = 0; i < texCoreData.childData.Count; i++)
                {
                    ReName(texCoreData.childData[i], id + i.ToString("D2") + "_");
                }
            }
        }
    }

    public string GetIP()
    {
        var uri = new Uri(Path.Combine(TexCore_RuntimeAssetService.GetStreamingAssetPath(string.Empty)));
        return uri.ToString();
    }

    public void AddData(TexCore_DataClass newData)
    {
        if (datas.datas.childData == null)
        {
            datas.datas.childData = new List<TexCore_DataClass>();
        }

        datas.datas.childData.Add(newData);
    }

    public void SaveSimpleFile(string fileName, object newData)
    {
        XmlSerializer.SaveToXml(savePath + fileName, newData);
    }

    public void SaveConfig(string url)
    {
        if (!isSaveFile)
        {
            return;
        }

        ReadDataFromPrefabSingle(datas.datas, prefabsPath);
        XmlSerializer.SaveToXml(url, datas);
    }

    private void LoadConfig(string url)
    {
        datas = (ConfigleData)XmlSerializer.LoadFromXml(url, typeof(ConfigleData));
        datas.datas.SetParent();
    }

    public void LoadConfigSimpleFile(string url, Type type)
    {
        XmlSerializer.LoadFromXml(url, type);
    }

    public IEnumerator LoadConfigOnWebgl(string path, Action<byte[]> success)
    {
        if (Application.platform == RuntimePlatform.Android &&
            !path.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            path = TexCore_RuntimeAssetService.GetStreamingAssetUrl(path);
        }

        UnityWebRequest www = UnityWebRequest.Get(path);
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.Success)
        {
            success?.Invoke(www.downloadHandler.data);
        }
        else
        {
            BpfLog.Warning(BpfLogCategory.Scene, "tex_core_config_manager.load_config_on_webgl", "Load config failed: " + path + " | " + www.error);
        }
    }

    public void ReadDataFromPrefab()
    {
        string fileUrl = prefabsPath + "/";
        GameObject[] objs = Resources.LoadAll<GameObject>(fileUrl);

        if (datas == null)
        {
            datas = new ConfigleData();
        }

        if (datas.datas == null)
        {
            datas.datas = new TexCore_DataClass();
        }

        foreach (GameObject item in objs)
        {
            IGetData newClassData = item.GetComponentInChildren<IGetData>();
            if (newClassData != null)
            {
                if (datas.datas.childData == null)
                {
                    datas.datas.childData = new List<TexCore_DataClass>();
                }

                datas.datas.childData.Add(newClassData.GetData());
            }
        }
    }

    public void ReadDataFromPrefab(string url)
    {
        GameObject[] objs = Resources.LoadAll<GameObject>(url);

        foreach (GameObject item in objs)
        {
            IGetData newClassData = item.GetComponent<IGetData>();
            if (newClassData != null)
            {
                TexCore_DataClass newData = newClassData.GetData();
                datas.datas.SetNode(newData.name, newData);
            }
        }
    }

    public void ReadDataFromPrefabSingle(TexCore_DataClass node, string url)
    {
        GameObject obj = Resources.Load(url + "/" + node.name) as GameObject;
        if (obj != null)
        {
            IGetData newClassData = obj.GetComponent<IGetData>();
            TexCore_DataClass newData = newClassData != null ? newClassData.GetData() : obj.GetComponent<TexCore_PlaneBase>().datas;

            if (newData == null)
            {
                return;
            }

            node.selfDatas = newData.selfDatas;
            node.childData = newData.childData;
        }

        if (node.childData == null)
        {
            return;
        }

        foreach (TexCore_DataClass item in node.childData)
        {
            ReadDataFromPrefabSingle(item, url + "/" + node.name);
        }
    }

    public void ReadDataFromPrefab(TexCore_DataClass node, string url)
    {
        string fileUrl = url + "/" + node.name;
        GameObject[] objs = Resources.LoadAll<GameObject>(fileUrl);

        TexCore_PlaneBase texCoreData = new TexCore_PlaneBase();
        foreach (GameObject item in objs)
        {
            if (!item.name.Equals(node.name))
            {
                continue;
            }

            IGetData newClassData = item.GetComponent<IGetData>();
            node = newClassData != null ? newClassData.GetData() : item.GetComponent<TexCore_PlaneBase>().datas;
            texCoreData = item.GetComponent<TexCore_PlaneBase>();
            datas.datas.SetNode(node.name, node);
            break;
        }

        List<string> childs = new List<string>();
        if (texCoreData != null && texCoreData.datas != null && texCoreData.datas.childData != null)
        {
            foreach (TexCore_DataClass item in texCoreData.datas.childData)
            {
                childs.Add(item.name);
            }
        }

        foreach (GameObject item in objs)
        {
            if (!childs.Contains(item.name))
            {
                continue;
            }

            TexCore_PlaneBase newClassData = item.GetComponentInChildren<TexCore_PlaneBase>();
            if (newClassData != null)
            {
                ReadDataFromPrefab(newClassData.datas, url + "/" + node.name);
            }
        }
    }
}

public static class XmlSerializer
{
    public static void SaveToXml(string filePath, object sourceObj, Type type = null, string xmlRootName = "")
    {
        if (string.IsNullOrWhiteSpace(filePath) || sourceObj == null)
        {
            return;
        }

        type = type ?? sourceObj.GetType();

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            System.Xml.Serialization.XmlSerializer xmlSerializer = string.IsNullOrWhiteSpace(xmlRootName) ?
                new System.Xml.Serialization.XmlSerializer(type) :
                new System.Xml.Serialization.XmlSerializer(type, new XmlRootAttribute(xmlRootName));
            xmlSerializer.Serialize(writer, sourceObj);
        }
    }

    public static object LoadFromXml(string filePath, Type type)
    {
        object result = null;

        if (File.Exists(filePath))
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(type);
                result = xmlSerializer.Deserialize(reader);
            }
        }

        return result;
    }

    public static object LoadFromByte(byte[] stream, Type type)
    {
        object result = null;

        using (MemoryStream reader = new MemoryStream(stream))
        {
            System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(type);
            result = xmlSerializer.Deserialize(reader);
        }

        return result;
    }
}
