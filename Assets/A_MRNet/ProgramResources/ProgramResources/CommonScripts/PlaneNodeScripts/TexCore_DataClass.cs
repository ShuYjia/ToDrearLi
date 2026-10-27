
//  XmlAnyAttribute     获取或设置要替代的 XmlAnyAttributeAttribute。

//  XmlAnyElements	    获取要替代的 XmlAnyElementAttribute 对象集合。

//  XmlArray	        设置数组的名称

//  XmlArrayItems	    设置数组中元素的名称

//  XmlAttribute	    设置元素名

//  XmlChoiceIdentifier	获取或设置一个对象，该对象允许你区分一组选择。

//  XmlDefaultValue	    获取或设置 XML 元素或属性的默认值。

//  XmlElement	        设置类名

//  XmlElements	        获取一个对象集合，该集合指定 XmlSerializer 将公共字段或读/写属性串行化为 XML 元素的方式。

//  XmlEnum             获取或设置一个对象，该对象指定 XmlSerializer 串行化枚举成员的方式。

//  XmlIgnore	        获取或设置一个值，该值指定 XmlSerializer 是否串行化公共字段或公共读/写属性。

//  Xmlns	            获取或设置一个值，该值指定当替代包含返回 XmlSerializerNamespaces 对象的成员的对象时，是否保留所有的命名空间声明。

//  XmlRoot	            获取或设置一个对象，该对象指定 XmlSerializer 将类串行化为 XML 根元素的方式。

//  XmlText	            获取或设置一个对象，该对象指示 XmlSerializer 将公共字段或公共读/写属性串行化为 XML 文本。

//  XmlType	            获取或设置一个对象，该对象指定 XmlSerializer 串行化已对其应用 XmlTypeAttribute 的类的方式。

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;                                                    
using UnityEngine;
using Bpf.Diagnostics;
             

/// <summary>
/// 用于直接写入配置文件的类
/// </summary>
[Serializable]
public class ConfigleData
{
    [XmlElement("根节点")]
    public TexCore_DataClass datas;

}

/// <summary>
/// 每个面板的通用配置文件类
/// </summary>
[Serializable]
public class TexCore_DataClass
{
    /// <summary>
    /// 面板的名称
    /// </summary>
    [XmlAttribute("标题")]
    public string name;
    /// <summary>
    /// 面板的私有节点
    /// </summary>
    //[XmlElement("特殊数据")]
    [XmlArray("数据组")]
    [XmlArrayItem("数据类")]
    public List<SelfDataBase> selfDatas;

    /// <summary>
    /// 面板的子面板
    /// </summary>
    [XmlArray("子面板")]
    [XmlArrayItem("面板配置数据")]
    public List<TexCore_DataClass> childData;

    [NonSerialized]
    [XmlIgnore]
    public TexCore_DataClass parent;

    ///// <summary>
    ///// 该界面的附加面板，没有则直接为空
    ///// </summary>
    //[XmlElement("场景数据")]
    //public SceneData sceneData;

    ///// <summary>
    ///// 面板的文本
    ///// </summary>
    //[XmlArray("文本")]
    //[XmlArrayItem("文本内容")]
    //public List<string> texts;

    ///// <summary>
    ///// 面板的图片
    ///// </summary>
    //[XmlArray("图片")]
    //[XmlArrayItem("图片名称")]
    //public List<string> pictures;



    /// <summary>
    /// 给所有的子节点设置父节点
    /// </summary>
    public void SetParent()
    {
        if (childData == null) return;
        foreach (TexCore_DataClass item in childData)
        {
            item.parent = this;
            item.SetParent();
        }
    }

    #region 顺序查找功能
    public TexCore_DataClass NextData()
    {

        //寻找弟弟们有没有题
        int n = GetIndexSelf();//获取自身的索引
        if (n != -1) //不是根节点
        {
            for (int i = n + 1; i < parent.childData.Count; i++)
            {
                TexCore_DataClass topic = parent.childData[i].GetData();
                if (topic != null)
                {
                    return topic;
                }
            }

            //找叔叔们有没有题
            int y = parent.GetIndexSelf();

            if (y != -1)
            {
                for (int i = y; i < parent.parent.childData.Count; i++)
                {
                    TexCore_DataClass topic = parent.parent.childData[i].NextData();
                    if (topic != null)
                    {
                        return topic;
                    }
                }
                return parent.parent.childData[y].NextData();
            }
        }
        return null;
    }
    public TexCore_DataClass GetData()
    {
        BpfLog.Info(BpfLogCategory.Scene, "tex_core_data_class.get_data", null, null, BpfLogField.Of("name", name));
        if (childData == null || childData.Count == 0)  //查自己的题目
        {
            return this;
        }
        if (selfDatas!=null)
        {
            return this;
        }
        foreach (TexCore_DataClass item in childData)
        {
            TexCore_DataClass topic = item.GetData();
            if (topic != null)
            {
                return topic;
            }
        }
        return null;
    }
    public int GetIndexSelf()
    {
        if (parent == null) return -1;
        for (int i = 0; i < parent.childData.Count; i++)
        {
            if (parent.childData[i].name.Equals(name))
            {
                //Debug.Log(标题 + "/自身索引为：" + i);
                return i;
            }
        }
        return -1;
    }
    public void SetNode(string name,TexCore_DataClass TexCore_Data)//目录查找功能，一层一层往下查找
    {
        if (this.name.Equals(name))
        {
            selfDatas = TexCore_Data.selfDatas;
            return;
        }
        if (childData != null)
        {
            for (int i = 0; i < childData.Count; i++)
            {
                if (childData[i].name.Equals(name))
                {
                    childData[i].selfDatas = TexCore_Data.selfDatas;
                    return;
                }
                else
                {
                    childData[i].SetNode(name,TexCore_Data);
                }
            }
        }
    }

    public TexCore_DataClass GetData(string url)
    {
        if (name.Equals(url)) return this;
        string[] paths = url.Split("/");
        foreach (TexCore_DataClass item in childData)
        {
            if (item.name.Equals(paths[0]))
            {
                return item.Find(url.Substring(paths[0].Length));
            }
        }
        return null;
    }
    public TexCore_DataClass Find(string name)//目录查找功能，一层一层往下查找
    {
        if (this.name == name) return this;
        if (childData != null)
        {
            for (int i = 0; i < childData.Count; i++)
            {
                TexCore_DataClass item = childData[i].Find(name);
                if (item != null)
                {
                    return item;
                }
            }
            return null;
        }
        else
        {
            return null;
        }
    }

    //新增
    public TexCore_DataClass FindRoot(string name)
    {
        if (this.name == name) return this;
        if (childData != null)
        {
            for (int i = 0; i < childData.Count; i++)
            {
                if (name == childData[i].name)
                {
                    return childData[i];
                }
            }
            return null;
        }
        else
        {
            return null;
        }
    }


    #endregion

    public string GetUrl(string url="")
    {
        if (parent!=null)
        {
            return parent.GetUrl(name + "/" + url);
        }
        else 
        {
            return name + "/" + url;
        }
    }
}
/// <summary>
/// 场景数据
/// </summary>
[Serializable]
public class SceneData:ISerializationCallbackReceiver
{
    [XmlAttribute("场景名称")]
    public string sceneName;

    [XmlAttribute("是否使用默认相机")]
    public bool userNewCamera;

    [XmlIgnore]
    public Vector3 pos;
    [XmlIgnore]
    public Vector3 rot;

    [XmlAttribute("镜头位置X")] public float posX { get; set; }
    [XmlAttribute("镜头位置Y")] public float posY { get; set; }
    [XmlAttribute("镜头位置Z")] public float posZ { get; set; }

    [XmlAttribute("镜头角度X")] public float rotX { get; set; }
    [XmlAttribute("镜头角度Y")] public float rotY { get; set; }
    [XmlAttribute("镜头角度Z")] public float rotZ { get; set; }

    public void ReadTrans()
    {
        pos = new Vector3(posX, posY, posZ);
        rot = new Vector3(rotX, rotY, rotZ);
    }

    public void OnAfterDeserialize()
    {
        posX = pos.x;
        posY = pos.y;
        posZ = pos.z;
        rotX = rot.x;
        rotY = rot.y;
        rotZ = rot.z;
    }

    public void OnBeforeSerialize()
    {
        posX = pos.x;
        posY = pos.y;
        posZ = pos.z;
        rotX = rot.x;
        rotY = rot.y;
        rotZ = rot.z;
    }


    //[XmlAttribute("相机旋转")]
    //public string cameraRot;
    //[XmlAttribute("相机位置")]
    //public string cameraPos;

}

/// <summary>
/// 特殊面板的配置文件基类
/// </summary>
[XmlInclude(typeof(ModelDate))]
[XmlInclude(typeof(DataClass_Topic))]
[XmlInclude(typeof(DataPanelNode))]
[XmlInclude(typeof(DataClass_Input))]
[XmlInclude(typeof(DataClass_Option))]
[XmlInclude(typeof(DataClass_Dialogue))]
[XmlInclude(typeof(DataClass_Drawing))]
[XmlInclude(typeof(DataClass))]
[Serializable]
public class SelfDataBase 
{
    

}

[Serializable]
public class V3
{
    [XmlAttribute("x")]
    public float x;
    [XmlAttribute("y")]
    public float y;
    [XmlAttribute("z")]
    public float z;
}
