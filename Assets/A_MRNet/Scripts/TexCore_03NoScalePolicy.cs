using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 03 业务场景专用策略：禁止物体通过双手拉近/拉远改变尺寸。
/// 移动、旋转、抓取、拖放、吸附和步骤完成仍由各步骤脚本原有逻辑控制。
/// 策略只对 03SingleplayerScene 中的对象生效，其他场景调用是空操作，
/// 因此不会改动共享业务 Prefab 资源本身。
/// </summary>
public static class TexCore_03NoScalePolicy
{
    /// <summary>03 业务场景名，与 <see cref="TexCore_LoadScene.BusinessSceneName"/> 保持一致。</summary>
    public const string BusinessSceneName = "03SingleplayerScene";

    /// <summary>判断场景名是否属于 03 业务场景。</summary>
    public static bool IsBusinessScene(string sceneName)
    {
        return string.Equals(sceneName, BusinessSceneName, System.StringComparison.Ordinal);
    }

    /// <summary>清除单个操纵器的缩放权限，保留它的移动与旋转权限。</summary>
    public static void Enforce(ObjectManipulator manipulator)
    {
        if (manipulator == null || !IsEnforceable(manipulator.gameObject))
        {
            return;
        }

        ClearScale(manipulator);
    }

    /// <summary>关闭边界控制的缩放手柄，保留旋转手柄位。</summary>
    public static void Enforce(BoundsControl boundsControl)
    {
        if (boundsControl == null || !IsEnforceable(boundsControl.gameObject))
        {
            return;
        }

        ClearScale(boundsControl);
    }

    /// <summary>
    /// 对整棵子树执行一次策略，供运行时新建、克隆或绑定对象后立刻调用。
    /// </summary>
    public static void Enforce(GameObject root)
    {
        if (root == null || !IsEnforceable(root))
        {
            return;
        }

        foreach (ObjectManipulator manipulator in
                 root.GetComponentsInChildren<ObjectManipulator>(true))
        {
            if (manipulator != null)
            {
                ClearScale(manipulator);
            }
        }

        foreach (BoundsControl boundsControl in
                 root.GetComponentsInChildren<BoundsControl>(true))
        {
            if (boundsControl != null)
            {
                ClearScale(boundsControl);
            }
        }
    }

    /// <summary>
    /// 对目标场景里已经加载的对象执行一次性检查。只允许在场景策略初始化、
    /// 场景加载和物体状态刷新时调用，禁止放进 Update 逐帧扫描。
    /// </summary>
    public static void EnforceScene(Scene scene)
    {
        if (!scene.IsValid() || !IsBusinessScene(scene.name))
        {
            return;
        }

        foreach (ObjectManipulator manipulator in
                 Object.FindObjectsOfType<ObjectManipulator>(true))
        {
            if (manipulator != null && manipulator.gameObject.scene == scene)
            {
                ClearScale(manipulator);
            }
        }

        foreach (BoundsControl boundsControl in
                 Object.FindObjectsOfType<BoundsControl>(true))
        {
            if (boundsControl != null && boundsControl.gameObject.scene == scene)
            {
                ClearScale(boundsControl);
            }
        }
    }

    private static bool IsEnforceable(GameObject target)
    {
        // 场景外的对象（其他场景、DontDestroyOnLoad、Prefab 资源）一律不改动。
        return target.scene.IsValid() && IsBusinessScene(target.scene.name);
    }

    private static void ClearScale(ObjectManipulator manipulator)
    {
        manipulator.AllowedManipulations &= ~TransformFlags.Scale;
    }

    private static void ClearScale(BoundsControl boundsControl)
    {
        boundsControl.EnabledHandles &= ~HandleType.Scale;
    }
}
