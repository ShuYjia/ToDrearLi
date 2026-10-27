using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps the 09 robot-entry drill stable without changing the shared 08 drill
/// or generic placement-step behavior.
/// </summary>
public static class TexCore_M09DrillPhysicsBootstrap
{
    private const string MainlineSceneName = "03SingleplayerScene";
    private const string Scene0809Name = "0809";
    private const string Scene09Name = "09";
    private const string FastenerStepPrefix = "09_01_手电钻拆除固定钉";
    private static readonly string[] DrillPath =
    {
        "无交互物体",
        "01货到人拣选电商仓储实施安装与调试实训系统",
        "09机器人入场",
        "ChangJing-8",
        "ShouDianZuan-JQR"
    };
    private static readonly string[] ModulePath =
    {
        "无交互物体",
        "01货到人拣选电商仓储实施安装与调试实训系统",
        "09机器人入场"
    };
    private static readonly string[] FastenerRootPath =
    {
        "无交互物体",
        "01货到人拣选电商仓储实施安装与调试实训系统",
        "09机器人入场",
        "ChangJing-8",
        "AGV",
        "XiangZi",
        "ZiGongSi"
    };
    private static readonly string[] FastenerNames =
    {
        "ZiGongSi",
        "ZiGongSi3",
        "ZiGongSi5",
        "ZiGongSi7",
        "ZiGongSi9",
        "ZiGongSi11",
        "ZiGongSi13",
        "ZiGongSi15"
    };
    private static VisibilityHost visibilityHost;
    private static readonly Dictionary<int, TexCore_OrderedPlacementStep[]> StepCache =
        new Dictionary<int, TexCore_OrderedPlacementStep[]>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SubscribeToSceneLoads()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureVisibilityHost();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StabilizeActiveSceneDrill()
    {
        StabilizeSceneDrill(SceneManager.GetActiveScene());
    }

    public static bool StabilizeSceneDrill(Scene scene)
    {
        if (!scene.IsValid() ||
            (scene.name != MainlineSceneName && scene.name != Scene0809Name &&
             scene.name != Scene09Name))
        {
            return false;
        }

        Transform drill = FindPath(scene, DrillPath);
        if (drill == null)
        {
            return false;
        }

        if (scene.name == Scene09Name)
        {
            Transform module = FindPath(scene, ModulePath);
            if (module != null)
            {
                module.gameObject.SetActive(true);
            }
        }

        TexCore_BackpackPhysics.Stabilize(drill.gameObject);
        SyncFastenerVisibility(scene);
        return true;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StepCache.Clear();
        StabilizeSceneDrill(scene);
    }

    private static void EnsureVisibilityHost()
    {
        if (visibilityHost != null)
        {
            return;
        }

        GameObject host = new GameObject("TexCore_M09FastenerVisibilityRuntime");
        Object.DontDestroyOnLoad(host);
        visibilityHost = host.AddComponent<VisibilityHost>();
    }

    private static void SyncFastenerVisibility(Scene scene)
    {
        if (!scene.IsValid() ||
            (scene.name != MainlineSceneName && scene.name != Scene09Name))
        {
            return;
        }

        Transform module = FindPath(scene, ModulePath);
        Transform fastenerRoot = FindPath(scene, FastenerRootPath);
        if (module == null || fastenerRoot == null)
        {
            return;
        }

        if (scene.name == Scene09Name)
        {
            // The standalone 09 scene only contains this module, so the module
            // itself always stays visible.
            module.gameObject.SetActive(true);
        }
        else if (!module.gameObject.activeInHierarchy)
        {
            // In the 03 mainline scene the TexCore step tree owns module node
            // visibility. Never reveal the whole module before M09 is entered,
            // and do not fight its show/hide logic while the node is hidden.
            // The fasteners keep their local active/render states; they simply
            // stay hidden behind the inactive module until M09 activates.
            return;
        }

        TexCore_OrderedPlacementStep[] steps = FindSteps(scene);

        for (int index = 0; index < FastenerNames.Length; index++)
        {
            Transform fastener = FindChild(fastenerRoot, FastenerNames[index]);
            if (fastener == null)
            {
                continue;
            }

            foreach (Animator animator in
                     fastener.GetComponentsInChildren<Animator>(true))
            {
                if (animator != null)
                {
                    animator.enabled = false;
                }
            }

            // 09 拆钉支持任意顺序后，运行期会把步骤的目标重新分配给“离钻头最近”
            // 的固定钉，所以固定钉显隐必须跟着“哪个步骤当前以它为目标”走，而不是
            // 原来的“步骤序号↔固定钉序号”映射。没有步骤认领的固定钉就是还没轮到
            // 拆的那一颗，保持可见。
            TexCore_OrderedPlacementStep step = FindStepByTargetName(
                scene, steps, FastenerNames[index]);
            bool visible = step == null || !step.WasCompletedByUser;
            fastener.gameObject.SetActive(visible);

            foreach (Renderer renderer in
                     fastener.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }
    }

    /// <summary>
    /// TexCore step reporters are scene objects created by the editor builders,
    /// so they can be resolved once per scene load instead of on every frame.
    /// Destroyed entries keep returning null through Unity's overloaded
    /// comparison, and the cache is dropped whenever any scene loads.
    /// </summary>
    private static TexCore_OrderedPlacementStep[] FindSteps(Scene scene)
    {
        if (StepCache.TryGetValue(scene.handle, out TexCore_OrderedPlacementStep[] cached) &&
            cached != null)
        {
            return cached;
        }

        TexCore_OrderedPlacementStep[] steps =
            Object.FindObjectsOfType<TexCore_OrderedPlacementStep>(true);
        StepCache[scene.handle] = steps;
        return steps;
    }

    /// <summary>
    /// 找到当前以 <paramref name="fastenerName"/> 这颗固定钉为目标的拆钉步骤。
    /// 09 拆钉支持任意顺序后，运行期会重新分配目标，所以不能再按步骤序号定位；
    /// 返回 null 表示这颗固定钉还没有被任何步骤认领，即还没轮到拆。
    /// </summary>
    private static TexCore_OrderedPlacementStep FindStepByTargetName(
        Scene scene,
        TexCore_OrderedPlacementStep[] steps,
        string fastenerName)
    {
        foreach (TexCore_OrderedPlacementStep step in steps)
        {
            if (step == null || step.gameObject.scene != scene ||
                !step.name.StartsWith(FastenerStepPrefix))
            {
                continue;
            }

            Transform target = step.Target;
            if (target != null && target.name == fastenerName)
            {
                return step;
            }
        }

        return null;
    }

    private static Transform FindChild(Transform parent, string childName)
    {
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static Transform FindPath(Scene scene, string[] path)
    {
        if (path == null || path.Length == 0)
        {
            return null;
        }

        Transform current = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == path[0])
            {
                current = root.transform;
                break;
            }
        }

        for (int index = 1; current != null && index < path.Length; index++)
        {
            Transform next = null;
            for (int child = 0; child < current.childCount; child++)
            {
                Transform candidate = current.GetChild(child);
                if (candidate.name == path[index])
                {
                    next = candidate;
                    break;
                }
            }

            current = next;
        }

        return current;
    }

    private sealed class VisibilityHost : MonoBehaviour
    {
        private void LateUpdate()
        {
            SyncFastenerVisibility(SceneManager.GetActiveScene());
        }
    }
}
