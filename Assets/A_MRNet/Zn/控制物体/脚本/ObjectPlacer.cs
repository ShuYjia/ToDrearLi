/****************************************************
    文件名：        ObjectPlacer.cs

    功能：          放置物体到玩家面前并跟随射线末端，处理物体选择、位置更新和交互
*****************************************************/

using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ObjectPlacer : MonoBehaviour
{
    /// <summary>
    /// 单例实例，确保全局只有一个 ObjectPlacer 实例
    /// </summary>
    private static ObjectPlacer _instance;
    public static ObjectPlacer Instance
    {
        get
        {
            // 如果没有实例，则从场景中查找或创建一个新的
            if (_instance == null)
            {
                _instance = FindObjectOfType<ObjectPlacer>();
            }
            return _instance;
        }
    }

    /// <summary>
    /// 当前相机，用于获取射线
    /// </summary>
    public Camera mainCamera;

    /// <summary>
    /// 当前显示的物体（玩家选择的物体）
    /// </summary>
    public GameObject displayedObject;

    /// <summary>
    /// 射线物体
    /// </summary>
    public GameObject rayObject;

    /// <summary>
    /// 父物体处理器，用于控制物体的放置和位置
    /// </summary>
    private ParentHandler _parentHandler;

    /// <summary>
    /// 射线的筛选层，用于限定射线检测的层
    /// </summary>
    public LayerMask layerMask;

    /// <summary>
    /// 可放置物体的资源路径，用于加载物体资源
    /// </summary>
    public string objectResourcePath;

    /// <summary>
    /// 射线是否击中物体，外部可以查询此状态
    /// </summary>
    public bool IsHit { get; private set; }

    /// <summary>
    /// 背包
    /// </summary>
    private TexCore_Interaction_UGUI bag;

    private void Start()
    {
        //bag = BagManager.Instance.bagUI;
    }

    private void Update()
    {
        if (displayedObject != null)
        {
            // 如果当前有物体被显示，则更新物体位置并处理按键操作
            UpdateObjectPosition();
            KeyYesObject();
        }
        else
        {
            // 如果没有显示的物体，则更新射线检测并处理按键操作
            UpdateObjectDetection();
            KeyNobject();
        }
        //if (Input.GetKeyDown(KeyCode.B) && ObjectUIManager.Instance.mainCanvas.activeSelf && !KnowledgeManager.self.transform.GetChild(0).gameObject.activeSelf) 
        //{
        //    bag.SetState(SelectState.Click);
        //}
        //if(Input.GetKeyDown(KeyCode.M))
        //{
        //    if(TexCore_Character.self.enableMove)
        //    {
        //        MapManager.instance.MapOnOff();
        //    }
        //}
    }

    /// <summary>
    /// 显示玩家选择的物体，并将其初始化
    /// </summary>
    /// <param name="selectedObject">玩家选择的物体</param>
    public void DisplaySelectedObject(string objectName)
    {
        GameObject objectPrefab = Resources.Load<GameObject>($"{objectResourcePath}{objectName}");

        if (objectPrefab == null) return;  // 如果资源加载失败，则直接返回

        // 如果当前没有显示的物体，则创建并初始化显示的物体
        if (displayedObject == null)
        {
            displayedObject = Instantiate(objectPrefab);
            displayedObject.name = objectName;
            _parentHandler = displayedObject.GetComponent<ParentHandler>();
            displayedObject.transform.SetParent(ObjectUIManager.Instance.objectRoot);
        }
        else
        {
            // 如果已有物体显示，则关闭当前物体并重新显示新物体
            CloseObj();
            DisplaySelectedObject(objectName);
        }
    }

    /// <summary>
    /// 更新物体位置，使其跟随射线末端
    /// </summary>
    private void UpdateObjectPosition()
    {
        // 从摄像机发射射线，获取鼠标指向的世界位置
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * 500, Color.green);  // 可视化射线

        // 检测射线是否击中物体
        if (Physics.Raycast(ray, out hit, 500, layerMask))
        {
            if (_parentHandler == null)
            {
                _parentHandler = hit.collider.GetComponent<ParentHandler>();
            }
            if (Vector3.Distance(_parentHandler.placementPoint.transform.position, hit.point) < 100)  
            {
                rayObject = hit.collider.gameObject;
                IsHit = true;
                _parentHandler.MoveTo(hit.point);  // 更新物体位置
            }
            else
            {
                rayObject = null;
                IsHit = false;
                _parentHandler.MoveTo(hit.point);  // 更新物体位置
            }
        }
        else
        {
            rayObject = null;
            IsHit = false;
            _parentHandler.MoveTo(ray.GetPoint(500));  // 如果没有击中物体，默认位置
        }
    }

    RaycastHit hitDetection;

    /// <summary>
    /// 更新物体检测状态，显示UI提示
    /// </summary>
    private void UpdateObjectDetection()
    {
        //if (BagManager.Instance.bag.activeInHierarchy || Handbook.instance.isHandbook)
        //{
        //    ObjectUIManager.Instance.HideHint();
        //    return;
        //}
            
        // 首先检测鼠标是否悬停在 UI 元素上
        if (IsPointerOverUIElement(out string uiElementName))
        {
            ObjectUIManager.Instance.HideHint();
            return;
        }

        // 从摄像机发射射线，检测是否有物体在射线范围内
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 2, Color.red);  // 可视化射线
        if (Physics.Raycast(ray, out hitDetection, 2, layerMask))
        {
            // 如果射线击中物体，则显示物体的悬停UI
            ObjectUIManager.Instance.OnHover(hitDetection.collider.gameObject);
        }
        else
        {
            // 如果射线未击中物体，则隐藏UI提示
            ObjectUIManager.Instance.HideHint();
        }
    }

    /// <summary>
    /// 检查鼠标是否悬停在 UI 元素上，并返回该元素的名称
    /// </summary>
    /// <param name="uiElementName">鼠标悬停的 UI 元素名称</param>
    /// <returns>是否悬停在 UI 上</returns>
    public bool IsPointerOverUIElement(out string uiElementName)
    {
        // 获取当前场景中的 EventSystem 和 GraphicRaycaster
        EventSystem eventSystem = EventSystem.current;
        GraphicRaycaster graphicRaycaster = FindObjectOfType<GraphicRaycaster>();

        // 创建一个空的 RaycastResult 列表用于存储结果
        List<RaycastResult> raycastResults = new List<RaycastResult>();

        // 设置事件数据，包含当前鼠标位置
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        // 使用 GraphicRaycaster 进行 UI 射线检测
        graphicRaycaster.Raycast(pointerData, raycastResults);

        // 如果检测结果中有 UI 元素，返回第一个元素的名称
        if (raycastResults.Count > 0)
        {
            uiElementName = raycastResults[0].gameObject.name;
            return true;
        }

        uiElementName = null;
        return false;
    }

    public Transform root;

    /// <summary>
    /// 处理与物体相关的按键操作，如放置物体、关闭物体等
    /// </summary>
    private void KeyYesObject()
    {
        // 按下空格键时放置物体
        if (Input.GetMouseButtonDown(0) && !_parentHandler.isFollowingRay) 
        {
            if (IsHit)
            {
                foreach (Transform item in root)
                {
                    if (item.name.Substring(2).Equals(_parentHandler.name))
                    {
                        item.GetComponent<TexCore_Interaction_UGUI>().enabled = false;
                        item.Find("J_前往").gameObject.SetActive(true);
                    }
                }

                _parentHandler.CreateObject();  // 生成物体
                CloseObj(false);  // 关闭当前显示物体
            }
        }

        // 按下鼠标中键时关闭当前显示的物体
        if (Input.GetMouseButtonDown(2))
        {
            CloseObj();
        }

        //ControlRotation();
    }

    /// <summary>
    /// 控制待生成物体的旋转
    /// </summary>
    private void ControlRotation()
    {
        // 获取旋转输入：E键左旋，Q键右旋
        float rotateInput = Input.GetKey(KeyCode.E) ? 1f : (Input.GetKey(KeyCode.Q) ? -1f : 0f);
        if (_parentHandler != null)
        {
            // 计算新的旋转角度并应用
            _parentHandler.empty.transform.eulerAngles += Vector3.up * rotateInput * 40 * Time.deltaTime;
            _parentHandler.real.transform.eulerAngles = _parentHandler.empty.transform.eulerAngles;
        }
            
    }

    /// <summary>
    /// 处理与物体交互的按键操作，如拾取物体
    /// </summary>
    private void KeyNobject()
    {

    }

    /// <summary>
    /// 关闭当前显示的物体，物体可以选择销毁或隐藏
    /// </summary>
    /// <param name="isDestroy">是否销毁物体</param>
    public void CloseObj(bool isDestroy = true)
    {
        if (isDestroy)
        {
            //GetChildPutBack(displayedObject);
            Destroy(displayedObject.gameObject);  // 销毁物体
        }
        displayedObject = null;  // 清空物体引用
        _parentHandler = null;  // 清空父物体处理器引用
    }


}
