using Bpf.Diagnostics;
/**
 * 管理单个场景多个面板的管理器
 *  
 *  包含一个从资源库中加载面板并实例化的功能
 *  新实例化出来的脚本会根据配置文件更改数据
 *  调用面板的预处理方法（跳转场景、更改相机）
 *  
 *  所有加载过的面板的缓存列表
 *  
 *  主场景（活动面板）activePlane(给BusinessManager调用点击事件的)
 * 
 * **/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using TMPro;

public class TexCore_PlaneManager : MonoBehaviour
{
    public bool isResourcesLoad = false;
    public static TexCore_PlaneManager self;

    /// <summary>
    /// 按资源路径缓存的 Prefab 资源：同一题目的 Resources.Load 只执行一次。
    /// 缓存的只是 Prefab 资源引用，不是运行时题目实例；
    /// 每次进入题目仍然执行新的 Instantiate，保证题目状态重新初始化、
    /// 交互物体状态不沿用上一次、OnEnable/Start 顺序与运行时绑定逻辑不变。
    /// </summary>
    private static readonly Dictionary<string, GameObject> planePrefabResourceCache =
        new Dictionary<string, GameObject>();

    /// <summary>
    /// 从缓存或 Resources 读取题目 Prefab 资源。只缓存加载成功的资源；
    /// 加载失败时保持原行为（下次仍尝试加载），不缓存 null。
    /// </summary>
    private static GameObject GetOrLoadPlanePrefab(string resourcePath)
    {
        if (planePrefabResourceCache.TryGetValue(resourcePath, out GameObject cached) &&
            cached != null)
        {
            return cached;
        }

        GameObject loaded = Resources.Load<GameObject>(resourcePath);
        if (loaded != null)
        {
            planePrefabResourceCache[resourcePath] = loaded;
        }

        return loaded;
    }


    private void Awake()
    {
        self = this;
    }

    public bool isSavePlane = false;
    public List<TexCore_PlaneBase> allPlane;//缓存池

    public List<AssetBundle> planeBundle;

    public TexCore_PlaneBase activePlane;

    public string prefabUrl = "BusinessPrefabs";

    public GameObject mainCamera;
    public Transform selfObjRoot;

    public bool isMain = false;
    public string startPlane;
    public TextMeshProUGUI result;

    public void StartLoad()
    {
        if (mainCamera == null) mainCamera = Camera.main.gameObject;
        if (string.IsNullOrEmpty(startPlane)) return;
#if UNITY_EDITOR
        TexCore_DataClass startNode = TexCore_ConfigManager.self.datas.datas.Find(startPlane);
        TexCore_PlaneManager.self.ShowPlane(startNode.name, startNode.GetUrl(), true);
#else
        try
        {
            TexCore_DataClass startNode = TexCore_ConfigManager.self.datas.datas.Find(startPlane);
            TexCore_PlaneManager.self.ShowPlane(startNode.name, startNode.GetUrl(), true);
            BpfLog.Info(BpfLogCategory.Scene, "tex_core_plane_manager.start_load", null, null, BpfLogField.Of("start_plane", startPlane));
        }
        catch (Exception ex)
        {
            result.text = ex.ToString();
            throw ex;
        }
#endif
    }

    public void ReturnPlane()
    {
        if (activePlane != null && activePlane.lastPlane != null)
        {
            activePlane.lastPlane.gameObject.SetActive(true);
            activePlane.gameObject.SetActive(false);
            activePlane = activePlane.lastPlane;
        }
    }

    

    /// <summary>
    /// 展示一个面板
    /// </summary>
    /// <param name="name">面板的名称</param>
    /// <returns></returns>
    public GameObject ShowPlane(string name, string path, bool isMain = false, Transform parent = null)
    {
        this.isMain = isMain;
        //TexCore_PlaneBase new_Plane = LoadPlane(name,path);

        if (isSavePlane)
        {
            foreach (TexCore_PlaneBase item in allPlane)//在加载过的面板中查找
            {
                if (item.datas.name.Equals(name))
                {
                    item.gameObject.SetActive(true);
                    if (isMain)
                    {
                        isMain = false;
                        CloseOldPlane(item);
                    }
                    return item.gameObject;
                }
            }
        }
        GameObject newPlaneObj;

        path = path[path.Length-1].Equals('/') ? path.Substring(0, path.Length - 1) : path;
        if (isResourcesLoad)//选择加载模式
        {
          newPlaneObj = Resources.Load<GameObject>(prefabUrl+"/" + path /*+ "/" + name*/);
            
            BpfLog.Info(BpfLogCategory.Scene, "tex_core_plane_manager.show_plane", prefabUrl + "/" + path /*+ "/" + name*/ + (newPlaneObj != null).ToString());
            if (activePlane != null && isMain && newPlaneObj != null) 
            {
                activePlane.gameObject.SetActive(false);
            }
            return BuildPlane(newPlaneObj, parent);
        }
        else
        {
            //Action<GameObject> back = BuildPlane;
            //TexCore_LoadAssets.self.LoadAssets(name, path, TexCore_ConfigManager.self.datas.datas.name, parent, action);
        }
        return null;
    }
    public GameObject BuildPlane(GameObject newPlaneObj, Transform parent, AssetBundle assetBundle = null)
    {
        if (newPlaneObj==null)
        {
            //全部面板做完
            //TopicPanel.instance.enter.SetActive(true);
            return null;
        }
        if (assetBundle != null)
        {
            planeBundle.Add(assetBundle);
        }
        //拿到预制体上的配置文件，判定是否需要加载场景
        TexCore_PlaneBase newPlane = newPlaneObj.GetComponentInChildren<TexCore_PlaneBase>();
        //newPlane.datas = TexCore_ConfigManager.self.datas.datas.Find(newPlane.datas.name);
        newPlane.datas = TexCore_ConfigManager.self.datas.datas.FindRoot(TexCore_HeadMenu.self.node).Find(newPlane.datas.name);
        //调用面板预加载
        IPanelInit init = newPlaneObj.GetComponent<IPanelInit>();
        if (init != null)
        {
            StartCoroutine(init.PanelInit());
        }

        //克隆面板对象
        newPlaneObj = Instantiate(newPlaneObj, parent == null ? transform : parent);
        newPlaneObj.name = newPlane.datas.name;
        // 03 场景的面板同样禁止双手缩放；其他场景该调用是空操作。
        TexCore_03NoScalePolicy.Enforce(newPlaneObj);
        //重置面板配置文件
        newPlane = newPlaneObj.GetComponentInChildren<TexCore_PlaneBase>();
        newPlane.datas = TexCore_ConfigManager.self.datas.datas.FindRoot(TexCore_HeadMenu.self.node).Find(newPlaneObj.name);



        allPlane.Add(newPlane);

        if (isMain)
        {
            isMain = false;
            CloseOldPlane(newPlane);
        }
        return newPlaneObj;
    }


    public void CloseOldPlane(TexCore_PlaneBase new_Plane)
    {
        if (new_Plane != null)//关闭前一个面板，展示新面板
        {
            if (activePlane != null)
            {
                new_Plane.lastPlane = activePlane;
                if (SceneManager.GetActiveScene().name.Equals("03OperatePage"))
                {
                    Destroy(activePlane.gameObject);
                }
                else
                {
                    activePlane.gameObject.SetActive(false);
                }
                
            }
            activePlane = new_Plane;
            new_Plane.gameObject.SetActive(true);

            //mainCamera.SetActive(true);
        }
    }

    public GameObject TexCoreBundlePlane(string bundleName, string planeName)
    {
        foreach (AssetBundle item in planeBundle)
        {
            if (item.name == bundleName)
            {
                return item.LoadAsset<GameObject>(planeName);
            }
        }
        return null;
    }

    /// <summary>
    /// 加载并实例化一个题目面板。
    /// <paramref name="planeData"/> 由调用方（节点）从自己的 childData 解析，
    /// 课程里多个模块存在同名题目时必须用它，不能用课程级 Find 兜底。
    /// </summary>
    public TexCore_PlaneBase LoadPlane(string planeName, string path, Transform parent = null,
        TexCore_DataClass planeData = null)
    {
        GameObject newPlaneObj;
        path = path[path.Length-1].Equals('/') ? path.Substring(0, path.Length - 1) : path;
        if (isResourcesLoad)
        {
            BpfLog.Info(BpfLogCategory.Scene, "tex_core_plane_manager.load_plane", prefabUrl + "/" + path + "/" + planeName);
            // Prefab 资源按路径缓存：同一题目的 Resources.Load 只执行一次，
            // 实例化语义保持不变（每次进入题目仍 Instantiate 新实例）。
            newPlaneObj = GetOrLoadPlanePrefab(prefabUrl + "/" + path + "/" + planeName);
        }
        else
        {
            newPlaneObj = TexCoreBundlePlane(planeName, path);
        }
        BpfLog.Info(BpfLogCategory.Scene, "tex_core_plane_manager.load_plane", prefabUrl + "/" + path + "/" + planeName + (newPlaneObj != null));
        TexCore_PlaneBase newPlane = newPlaneObj.GetComponentInChildren<TexCore_PlaneBase>();
        //string[] ps = path.Split("/");
        //TexCore_DataClass p = TexCore_ConfigManager.self.datas.datas.Find(planeName);
        newPlane.datas = ResolvePlaneData(planeName, planeData);

        //调用面板预加载
        if (newPlane.GetComponent<IPanelInit>() != null)
        {
            StartCoroutine(newPlane.GetComponent<IPanelInit>().PanelInit());
        }

        //克隆面板对象
        newPlaneObj = Instantiate(newPlaneObj, parent == null ? transform : parent);
        newPlaneObj.name = planeName;
        // 03 场景的面板同样禁止双手缩放；其他场景该调用是空操作。
        TexCore_03NoScalePolicy.Enforce(newPlaneObj);
        //重置面板配置文件
        newPlane = newPlaneObj.GetComponentInChildren<TexCore_PlaneBase>();
        newPlane.datas = ResolvePlaneData(planeName, planeData);
        return newPlane;
    }

    /// <summary>
    /// 解析题目数据：优先使用调用方从本节点 childData 解析好的数据。
    /// 课程里多个模块存在同名题目（05/08/09/11 都有“06完成整个模块”），
    /// 课程级 Find 会命中别的模块，导致题目读到别模块的 tasks/hintTexts，
    /// GetIndexSelf() 也返回错误的题目序号。
    /// </summary>
    private static TexCore_DataClass ResolvePlaneData(string planeName, TexCore_DataClass planeData)
    {
        if (planeData != null)
        {
            return planeData;
        }

        return TexCore_ConfigManager.self.datas.datas
            .FindRoot(TexCore_HeadMenu.self.node).Find(planeName);
    }



    public IEnumerator ShowPlaneLine(string planeName, string path, Transform parent = null, bool isAdd = false)
    {
        yield return null;

        TexCore_PlaneBase newPlane = null;
        foreach (TexCore_PlaneBase item in allPlane)//判定之前有没有加载过
        {
            if (item.datas.name.Equals(planeName))
            {
                newPlane = item;
            }
        }
        if (newPlane == null)
        {
            //GameObject newPlanePrefab;
            //yield return StartCoroutine(TexCore_LoadAssets.self.LoadAssets(planeName, path));

            GameObject newPlanePrefab = Resources.Load<GameObject>(prefabUrl + path + "/" + planeName);//加载plane

            newPlane = newPlanePrefab.GetComponentInChildren<TexCore_PlaneBase>();

            //调用面板预加载
            if (newPlane is IPanelInit)
            {
                yield return StartCoroutine(newPlane.GetComponent<IPanelInit>().PanelInit());
            }

            newPlanePrefab = Instantiate(newPlanePrefab, parent == null ? transform : parent);
            newPlane = newPlanePrefab.GetComponentInChildren<TexCore_PlaneBase>();
            newPlane.datas = TexCore_ConfigManager.self.datas.datas.Find(planeName);
            allPlane.Add(newPlane);

            newPlanePrefab.name = planeName;
        }
        if (isAdd == false)
        {
            if (newPlane != null)//关闭前一个面板，展示新面板
            {
                if (activePlane != null)
                {
                    newPlane.lastPlane = activePlane;
                    activePlane.gameObject.SetActive(false);
                }
                activePlane = newPlane;
                newPlane.gameObject.SetActive(true);
            }
        }
    }

    public void ResetObjPos(Transform obj,string _name)
    {
        obj.name = _name + "_" + obj.name;
        obj.transform.SetParent(selfObjRoot);
        obj.transform.position = Vector3.zero;
        obj.transform.localScale = Vector3.one;
        obj.transform.rotation = Quaternion.identity;
    }
}
