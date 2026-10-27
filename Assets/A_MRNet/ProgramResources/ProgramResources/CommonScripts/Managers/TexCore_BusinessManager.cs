using Bpf.Diagnostics;
/**
 * 脚本用途：处理项目中所有包含业务逻辑的交互事件
 * 挂载    ：静态可不挂载
 * **/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TexCore_BusinessManager : MonoBehaviour
{
    /// <summary>
    /// 业务点击拦截器（联机模块安装）。返回 true 表示网络层已接管，
    /// 本方法直接返回、不执行本地分发。单机时为 null，行为与改造前一致。
    /// </summary>
    public static System.Func<GameObject, string, bool> ClickInterceptor;

    public static void OnClick(GameObject interactionObj, string sceneName)
    {
        BpfLog.Info(BpfLogCategory.TexCore, "tex_core_business_manager.on_click", "TexCore_BusinessManager:触发：" + sceneName + "__" + interactionObj.name);

        if (interactionObj.name.Equals("工具")) return;

        // 联机时：Client 只发请求由 Host 裁决；Host 放行后仍走下面的本地分发。
        if (ClickInterceptor != null && ClickInterceptor(interactionObj, sceneName))
        {
            return;
        }

        switch (interactionObj.name.Split('_')[0])
        {
            // 选择题选项名为 “A.xxx”“B.xxx”，前缀正好落在 A/B 上。
            // 这两个分支原先只 break，点击不会贯穿到当前活动题目，
            // 答案写不进 Panel_Select.steps，点击“确定”必然判错。
            case "A":
            case "B":
                ForwardToActivePlane(interactionObj);
                break;
            case "J":
            case "Z":
                break;
            case "S":
                //TexCore_PlaneManager.self.activePlane.OnClick(interactionObj);
                break;
            case "T":
                switch (sceneName)
                {
                    case "MR多人协同系统":
                        TexCore_Const.startPanel = "04模式选择";
                        break;
                    default:
                        break;
                }
                TexCore_LoadScene.instance?.OnlyLoad("SampleScene");
                break;
            case "Y":
                BackgroundMusic.instance.OnClik(interactionObj);
                break;
            case "L1":
            case "L2":
                TexCore_HeadMenu.self?.OnClickMenu(interactionObj.name);
                return;
            case "Exam":
                TexCore_ExamData.self?.OnClick(interactionObj);
                break;
            case "s":
                TexCore_HeadMenu.self?.OnClick(interactionObj);
                return;
            case "m"://重载当前场景
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                return;
            case "w"://弹窗
                TexCore_WindowManager.self?.OnWindowClick(interactionObj);
                return;
            case "p"://目录
                //TexCore_PBSManager.self.OnClick(interactionObj);
                //My_PBSManager.self.OnClick(interactionObj);
                TexCore_EventCenter.Broadcast<string>(TexCore_EventType.点击目录, interactionObj.name);
                return;
            case "t":
                ToolsManager.self.OnClick(interactionObj);
                break;
            default:
                ForwardToActivePlane(interactionObj);
                break;
                //return;
                //case "n"://数字键盘
                //    TexCore_InputNumber.self.OnClick(interactionObj);
                //    return;
                //case "t"://工具
                //    ToolsManager.self.OnClick(interactionObj);
                //return;
        }
       
        
        switch (sceneName)
        {
            case "小地图":
                break;
            case "背包":
                break;
            //case "01StartPage"://登录场景
            //    Page01Start.instance.OnClick(interactionObj);
            //    break;
            //case "02NavigationPage"://UI场景
            //    Page02Navigation.instance.OnClick(interactionObj);
            //    break;
            //case "03OperatePage":
            //    TexCore_PlaneManager.self.activePlane.OnClick(interactionObj);
            //    break;
            case "12理论考核":
                SingTopicPage.self.OnClick(interactionObj);
                break;
        }

    }

    /// <summary>
    /// 把点击贯穿到当前活动平面（节点 → 活动题目）。活动平面尚未建立时直接忽略，
    /// 避免场景切换空档里点击选项或“确定”抛空引用。
    /// </summary>
    private static void ForwardToActivePlane(GameObject interactionObj)
    {
        if (TexCore_PlaneManager.self == null || TexCore_PlaneManager.self.activePlane == null)
        {
            return;
        }

        TexCore_PlaneManager.self.activePlane.OnClick(interactionObj);
    }

    public static void OnClick(GameObject interactionObj, string sceneName, string player)
    {
        BpfLog.Info(BpfLogCategory.TexCore, "tex_core_business_manager.on_click", "触发：" + sceneName + "__" + interactionObj.name + "_" + player);
        if (false/*TexCore_Const.isSinglePerson*/)
        {
            OnClick(interactionObj, sceneName);
        }
        else
        {
            interactionObj.name += "_" + player;
            TexCore_PlaneManager.self.activePlane.OnClick(interactionObj);
            interactionObj.name = interactionObj.name.Split('_')[0];
        }
    }
    public static void OnClick(GameObject interactionObj/*, string sceneName*/)
    {
        //print("协同触发：" /*+ sceneName + "__"*/ + interactionObj);
        
        TexCore_PlaneManager.self.activePlane.OnClick(interactionObj);
        //switch (sceneName)
        //{
        //    case "01LoginPage"://登录场景
        //        LoginPanel.instance.OnClick(interactionObj);
        //        break;
        //    case "02StartPage"://UI场景
        //        TexCore_PlaneManager.self.activePlane.OnClick(interactionObj);
        //        break;
        //    case "03OperatePage":
        //        TexCore_PlaneManager.self.activePlane.OnClick(interactionObj);
        //        break;
        //    default:
        //        TexCore_PlaneManager.self.activePlane.OnClick(interactionObj);
        //        break;
        //}
    }
}
