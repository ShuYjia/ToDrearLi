/****************************************************
    文件名：        ObjectUIManager.cs
    编写者：        Zn
    功能：          管理游戏中的物体与提示UI
*****************************************************/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 管理游戏中的物体和UI提示，包含添加、移除和提示显示的功能
/// </summary>
public class ObjectUIManager : MonoBehaviour
{
    /// <summary>
    /// 提示类型枚举，定义不同的提示状态
    /// </summary>
    private enum HintType
    {
        None,       // 无提示
        PressF,     // 按F操作提示
        PressR      // 按R拾取提示
    }

    private static ObjectUIManager _instance;

    /// <summary>
    /// 单例属性，用于获取唯一实例
    /// </summary>
    public static ObjectUIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("ObjectUIManager");
                _instance = obj.AddComponent<ObjectUIManager>();
            }
            return _instance;
        }
    }

    /// <summary>
    /// 存储所有生成的物体
    /// </summary>
    public HashSet<GameObject> ObjectSet { get; private set; } = new HashSet<GameObject>();
    [Header("UI Elements")]
    /// <summary>
    /// 主界面画布对象，用于显示主UI元素。
    /// </summary>
    public GameObject mainCanvas;

    /// <summary>
    /// 悬停提示的UI元素，用于显示鼠标悬浮提示。
    /// </summary>
    public GameObject hoverHintUI;

    /// <summary>
    /// 全站仪面板
    /// </summary>
    public GameObject totalStationPanel;
    /// <summary>
    /// 按键操作
    /// </summary>
    public GameObject keysOperationPanel;


    /// <summary>
    /// 圆水准器定位
    /// </summary>
    public RectTransform yszqdw;
    /// <summary>
    /// 管水准器定位
    /// </summary>
    public RectTransform gszqdw;

    /// <summary>
    /// 提示文本组件，用于动态显示操作提示信息。
    /// </summary>
    public TextMeshProUGUI hintText;

    /// <summary>
    /// 背景模糊效果的图像组件，用于在提示时模糊背景。
    /// </summary>
    public Image backgroundImage;

    [Header("3D Objects")]
    /// <summary>
    /// 存放3D物体的父级对象，用于组织场景中的物体层次结构。
    /// </summary>
    public Transform objectRoot;

    /// <summary>
    /// 当前显示的提示类型，记录当前显示的UI提示状态。
    /// </summary>
    private HintType currentHintType = HintType.None;

    public GameObject btn;//自动漫游

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// 添加物体到管理器，确保唯一性，并清除已丢失的物体
    /// </summary>
    /// <param name="obj">要添加的物体</param>
    public void AddObject(GameObject obj)
    {
        // 清除丢失或空的物体
        ObjectSet.RemoveWhere(existingObj => existingObj == null);

        // 若物体未在集合中，则添加
        if (!ObjectSet.Contains(obj))
        {
            ObjectSet.Add(obj);
        }

        if (ObjectSet.Count == 5)
        {
            btn.SetActive(true);
        }
    }

    /// <summary>
    /// 查找物体
    /// </summary>
    /// <param name="obj"></param>
    public Transform FindObj(string objName)
    {
        foreach (GameObject obj in ObjectSet)
        {
            if (obj.name.Equals(objName)) 
            {
                return obj.transform;
            }
        }
        return null;
    }

    /// <summary>
    /// 从管理器中移除物体
    /// </summary>
    /// <param name="obj">要移除的物体</param>
    public void RemoveObject(GameObject obj)
    {
        ObjectSet.Remove(obj);
        HideHint();
    }

    /// <summary>
    /// 物体点击事件处理
    /// </summary>
    /// <param name="obj">被点击的物体</param>
    public void OnClick(GameObject obj)
    {
        // 此处可以加入点击事件逻辑
        switch (obj.name.Split('_')[0])
        {
            case "O":
                    //TotalStation._instance.OnClick(obj);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 处理物体悬浮事件，显示相应的提示
    /// </summary>
    /// <param name="obj">悬浮的物体</param>
    public void OnHover(GameObject obj)
    {
        string nameSuffix = "";
        if (obj.name.Length>1)
        {
            nameSuffix = obj.name.Substring(2);
        }


        if (nameSuffix == "全站仪" || nameSuffix == "手簿") 
        {
            UpdateHint(HintType.PressF, "按F操作");
        }
        else if (obj.name.StartsWith("O"))
        {
            UpdateHint(HintType.PressR, "按R拾取");
        }
        else
        {
            HideHint();
        }
    }

    /// <summary>
    /// 更新提示的显示内容和位置
    /// </summary>
    /// <param name="hintType">提示类型</param>
    /// <param name="message">提示内容</param>
    private void UpdateHint(HintType hintType, string message)
    {
        currentHintType = hintType;
        hintText.text = message;
        hoverHintUI.SetActive(true);
        hoverHintUI.transform.position = Input.mousePosition;
    }

    /// <summary>
    /// 隐藏当前显示的提示
    /// </summary>
    public void HideHint()
    {
        hoverHintUI.SetActive(false);
        currentHintType = HintType.None;
    }

    /// <summary>
    /// 根据物体名称查找物体
    /// </summary>
    /// <param name="objectName">物体名称</param>
    /// <returns>找到的物体，未找到返回null</returns>
    public GameObject FindObjectByName(string objectName)
    {
        foreach (GameObject obj in ObjectSet)
        {
            if (objectName.Equals(obj.name))
            {
                return obj;
            }
        }
        return null;
    }

    /// <summary>
    /// 切换主画布和全站仪专属的显示状态
    /// </summary>
    public void ToggleCanvasVisibility()
    {
        ToggleGameObject(mainCanvas.transform.GetChild(0).transform.Find("isHide").gameObject);
        ToggleGameObject(totalStationPanel);
    }

    /// <summary>
    /// 切换给定物体的显示状态
    /// </summary>
    /// <param name="obj">需要切换的物体</param>
    private void ToggleGameObject(GameObject obj)
    {
        obj.SetActive(!obj.activeSelf);
    }

    public void SetUIPos(Vector2 vector2, Vector2 vector21)
    {
        yszqdw.anchoredPosition = vector2;
        gszqdw.anchoredPosition = new Vector2(vector21.x, gszqdw.anchoredPosition.y);
    }
}
