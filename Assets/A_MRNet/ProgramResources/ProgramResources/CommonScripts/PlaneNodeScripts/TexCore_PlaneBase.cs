using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有面板的基类
/// </summary>
public class TexCore_PlaneBase : MonoBehaviour
{
    /// <summary>
    /// 面板通用的序列化数据类
    /// </summary>
    public TexCore_DataClass datas;

    /// <summary>
    /// 在这个面板之前显示的，用于返回前一个面板
    /// </summary>
    public TexCore_PlaneBase lastPlane;

    /// <summary>
    /// 必要的点击交互方法
    /// </summary>
    /// <param name="obj">点击的物体的对象</param>
    public virtual void OnClick(GameObject obj)
    {
        Print.Edit(":点击:" +obj.name);
    }
   
    /// <summary>
    /// 非必要的悬停交互方法
    /// </summary>
    /// <param name="obj">悬停的物体的对象</param>
    public virtual void OnHover(GameObject obj)
    {
        Print.Edit(":悬停:" + obj.name);
    }
   
}

public enum Character 
{   
    所有人,
    组长,
    观察员,
    记录员,
    管理员
}


/// <summary>
/// 获取配置文件数据的接口
/// </summary>
public interface IGetData
{
     TexCore_DataClass GetData();

}

public interface IPanelInit 
{
    IEnumerator PanelInit();
}


///// <summary>
///// 面板点击方法接口
///// </summary>
//public interface IOnClick
//{
//    public void OnClick(GameObject obj);
//}

///// <summary>
///// 面板点击方法接口
///// </summary>
//public interface IOnHover
//{
//    public void OnHover(GameObject obj);
//}
