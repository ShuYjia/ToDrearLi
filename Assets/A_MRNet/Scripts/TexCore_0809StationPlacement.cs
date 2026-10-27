using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// 0809 场景专用的“08_02_安装充电桩”放置体验修补。
///
/// 想要的效果：
///   1. 步骤进行期间目标位置不显示 ChongDianZhuang（它只是安装位）；
///   2. 但仍然有一个发光标记指出该把充电桩搬到哪里；
///   3. 玩家抱着 ChongDianZhuang_fuben 靠近标记就自动吸附，不需要精确对位、
///      也不需要刚好在容差内松手；
///   4. 吸附后位置、旋转、缩放与 ChongDianZhuang 完全一致；
///   5. 随后显示 ChongDianZhuang、隐藏 ChongDianZhuang_fuben，
///      让 08_03 及之后的步骤继续在正式充电桩上操作。
///
/// 另外处理「08_03_打开充电桩前盖」：前盖位置锁死、只允许绕 Z 轴掀开。
///
/// 只作用于 0809：共享框架、03 场景与 Config.xml 都不改。
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_0809StationPlacement : MonoBehaviour
{
    private const string SceneName = "0809";
    private const string StepName = "08_02_安装充电桩";
    private const string SourceName = "ChongDianZhuang_fuben";
    private const string TargetName = "ChongDianZhuang";
    private const string LidStepName = "08_03_打开充电桩前盖";
    private const string LogPrefix = "[0809Station] ";

    private const float ApproachTolerance = 0.25f;
    private const float FrontCoverMaxOpenAngle = 90f;
    private const float FallbackMarkerRadius = 0.5f;
    private const float MarkerLineWidth = 0.02f;
    private const int MarkerSegments = 48;

    private static readonly Color MarkerColor = new Color(0.05f, 0.95f, 1f, 1f);

    private static bool sceneCallbackRegistered;

    private TexCore_OrderedPlacementStep step;
    private GameObject source;
    private Transform target;
    private bool configured;
    private bool placed;
    private bool hintShown;
    private TexCore_ClickObjectStep lidStep;
    private Transform lidTransform;
    private Rigidbody lidBody;
    private ObjectManipulator lidManipulator;
    private Collider[] lidColliders;
    private Vector3 frontCoverHomePosition;
    private Vector3 frontCoverHomeWorldPosition;
    private Vector3 frontCoverOpenTangent = Vector3.up;
    private float frontCoverRadius = 1f;
    private float frontCoverAngle;
    private bool markerRadiusResolved;
    private float markerRadius = FallbackMarkerRadius;
    private GameObject markerRoot;
    private LineRenderer markerRing;
    private LineRenderer markerPole;
    private Material markerMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        if (sceneCallbackRegistered)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        sceneCallbackRegistered = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AddToInitialScene()
    {
        CreateForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CreateForScene(scene);
    }

    private static void CreateForScene(Scene scene)
    {
        if (!scene.IsValid() || scene.name != SceneName)
        {
            return;
        }

        TexCore_0809StationPlacement[] existing =
            Resources.FindObjectsOfTypeAll<TexCore_0809StationPlacement>();
        foreach (TexCore_0809StationPlacement value in existing)
        {
            if (value != null && value.gameObject.scene.handle == scene.handle)
            {
                return;
            }
        }

        GameObject host = new GameObject("0809ChargingStationPlacement");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<TexCore_0809StationPlacement>();
    }

    private void OnDestroy()
    {
        if (markerRoot != null)
        {
            if (Application.isPlaying)
            {
                Destroy(markerRoot);
            }
            else
            {
                DestroyImmediate(markerRoot);
            }
        }

        if (markerMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(markerMaterial);
            }
            else
            {
                DestroyImmediate(markerMaterial);
            }
        }
    }

    private void Awake()
    {
        // 必须在物理第一次步进之前捕获前盖的初始位姿：AddComponent 会同步执行
        // Awake，早于任何 FixedUpdate。若拖到 LateUpdate 才捕获，那个锚定世界
        // 坐标的 HingeJoint 可能已经把盖子拖离作者摆放的位置（实测差了约 0.24m）。
        ConfigureFrontCover();
    }

    private void LateUpdate()
    {
        if (SceneManager.GetActiveScene().name != SceneName)
        {
            return;
        }

        // 前盖与放置步骤互不依赖，单独处理。
        if (lidTransform == null)
        {
            ConfigureFrontCover();
        }

        EnforceFrontCover();

        if (!ResolveReferences())
        {
            return;
        }

        if (placed)
        {
            if (step.state == FinishState.未完成 && !step.WasCompletedByUser)
            {
                // 题目被重置（例如重新进入本题）：交还控制权，让框架重新隐藏
                // 正式充电桩，玩家可以再搬一次。
                placed = false;
                hintShown = false;
                markerRadiusResolved = false;
            }
            else
            {
                KeepPlacedState();
                return;
            }
        }

        bool stepIsCurrent =
            step.state == FinishState.等待触发 && !step.WasCompletedByUser;

        if (stepIsCurrent)
        {
            // TexCore_StepObjectState.showWhileActive 与 SetGuidanceVisible 都会把
            // 目标重新激活，所以在步骤进行期间必须逐帧压制。
            HideTarget();
            ShowMarker();
            ShowHintOnce();
        }
        else
        {
            HideMarker();
        }

        if (IsCompleted())
        {
            FinalizePlacement();
        }
    }

    /// <summary>
    /// 「08_03_打开充电桩前盖」的前盖处理：位置锁死，抓起来拉就能绕 Z 轴掀开。
    ///
    /// 前盖原本挂着一个 connectedBody 为空（锚定世界坐标）的 HingeJoint，
    /// 而 MRTK 是直接改 transform 的，物理求解会按关节锚点把盖子拉回去，
    /// 表现为「打开后又自动吸附回去」。
    ///
    /// 现在改成：
    ///   - Manipulator 只允许 Move（转手腕不生效），Rigidbody 设 kinematic，
    ///     移除 HingeJoint，约束完全由本脚本逐帧接管；
    ///   - 每帧把手（以及被它带动的盖子）相对初始位置的位移，投影到「绕 +Z
    ///     旋转时盖板运动的方向」上，换算成绕铰链轴的开启角，夹在
    ///     [0, 最大开启角]（对应原 HingeJoint 的 limits 0~90）；
    ///   - 位置每帧锁回初始值，所以盖子既拽不走，也不会平移；
    ///   - 角度只增不减，松手后停在当前角度。
    /// </summary>
    private void ConfigureFrontCover()
    {
        if (lidStep == null)
        {
            foreach (TexCore_ClickObjectStep candidate in
                     FindObjectsOfType<TexCore_ClickObjectStep>(true))
            {
                if (candidate != null && candidate.name == LidStepName)
                {
                    lidStep = candidate;
                    break;
                }
            }

            if (lidStep == null)
            {
                return;
            }
        }

        if (lidTransform == null)
        {
            GameObject lid = lidStep.InteractionSource;
            if (lid == null)
            {
                return;
            }

            lidTransform = lid.transform;
            frontCoverHomePosition = lidTransform.localPosition;

            lidBody = lid.GetComponent<Rigidbody>();
            if (lidBody != null)
            {
                lidBody.useGravity = false;
                lidBody.isKinematic = true;
            }

            // 约束由本脚本逐帧处理；关节留给物理求解只会把盖子拉回锚点。
            HingeJoint hinge = lid.GetComponent<HingeJoint>();
            if (hinge != null)
            {
                Destroy(hinge);
            }

            lidManipulator = lid.GetComponent<ObjectManipulator>();
            if (lidManipulator != null)
            {
                // 只允许 Move：玩家靠"抓起来拉"来开盖，不靠转手腕。
                lidManipulator.AllowedManipulations = TransformFlags.Move;
            }

            lidColliders = lid.GetComponentsInChildren<Collider>(true);

            frontCoverHomeWorldPosition = lidTransform.position;

            // 铰链轴 = 前盖自身的局部 Z 轴；力臂 = 从铰链指向盖板网格中心的方向。
            // 记录"绕 +Z 旋转时盖板运动的方向"，玩家把手往这个方向拉就是"掀开"。
            Vector3 hingeAxis = lidTransform.forward;
            Vector3 meshCenter = frontCoverHomeWorldPosition;
            bool hasBounds = false;
            Bounds bounds = default;
            foreach (Renderer renderer in lid.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (hasBounds)
            {
                meshCenter = bounds.center;
            }

            Vector3 radial = Vector3.ProjectOnPlane(
                meshCenter - frontCoverHomeWorldPosition, hingeAxis);
            frontCoverRadius = Mathf.Max(0.05f, radial.magnitude);
            frontCoverOpenTangent = Vector3.Cross(hingeAxis, radial).normalized;
            if (frontCoverOpenTangent.sqrMagnitude < 0.5f)
            {
                frontCoverOpenTangent = Vector3.Cross(hingeAxis, Vector3.up).normalized;
            }

            if (frontCoverOpenTangent.sqrMagnitude < 0.5f)
            {
                frontCoverOpenTangent = Vector3.Cross(hingeAxis, Vector3.right).normalized;
            }

            Debug.Log(LogPrefix + "前盖 " + lid.name + " 已改为「位置锁定 + 抓取拉动掀开 0~" +
                FrontCoverMaxOpenAngle + "°」，初始 localPos=" +
                frontCoverHomePosition.ToString("F4") + "，力臂=" +
                frontCoverRadius.ToString("F3") + "m，开启方向=" +
                frontCoverOpenTangent.ToString("F2") + "。");

            // 交叉校验：fuben 的前盖同款且没有 Rigidbody，不受物理影响，可作参照。
            GameObject fuben = GameObject.Find("ChongDianZhuang_fuben");
            Transform reference = fuben != null ? fuben.transform.Find("DingGai") : null;
            if (reference != null &&
                Vector3.Distance(reference.localPosition, frontCoverHomePosition) > 0.01f)
            {
                Debug.LogWarning(LogPrefix + "捕获到的前盖初始位置 " +
                    frontCoverHomePosition.ToString("F4") + " 与 fuben 前盖的 " +
                    reference.localPosition.ToString("F4") + " 不一致。");
            }
        }
    }

    /// <summary>
    /// 每帧执行前盖约束：
    ///   1. 按住盖子时，把手带动的位移换算成绕铰链轴的开启角（只增不减）；
    ///   2. 位置锁回初始值——盖子拽不走，也不会整体平移；
    ///   3. 旋转始终是纯 Z 轴、夹在 [0, 最大开启角]；
    ///   4. 08_03 之后框架会关掉盖子的交互，这里保持可用，随时能再拉开。
    /// </summary>
    private void EnforceFrontCover()
    {
        if (lidTransform == null)
        {
            return;
        }

        // MRTK 抓取时每帧直接摆放盖子的位置；把手部位移投影到"掀开方向"上
        // 即可换算成开启角。松手后位置被锁回原点，位移为 0，角度保持不变。
        Vector3 delta = lidTransform.position - frontCoverHomeWorldPosition;
        float arc = Vector3.Dot(delta, frontCoverOpenTangent);
        if (Mathf.Abs(arc) > 0.0005f)
        {
            float dragged = arc / frontCoverRadius * Mathf.Rad2Deg;

            // 只增不减：松手后停在当前角度，往回转也不会自己合上盖。
            frontCoverAngle = Mathf.Clamp(
                Mathf.Max(frontCoverAngle, dragged), 0f, FrontCoverMaxOpenAngle);
        }

        // 位置锁死：盖子不会被拽走，只按上面的角度绕 Z 轴转。
        lidTransform.localPosition = frontCoverHomePosition;
        lidTransform.localRotation = Quaternion.Euler(0f, 0f, frontCoverAngle);

        KeepFrontCoverInteractive();
    }

    /// <summary>
    /// 08_03 完成后 TexCore_ClickObjectStep 会关掉前盖的 Collider 与 Manipulator，
    /// 这里在步骤完成后把它们保持可用，让玩家之后随时还能把盖子拉开。
    /// </summary>
    private void KeepFrontCoverInteractive()
    {
        if (lidStep == null ||
            (!lidStep.WasCompletedByUser && lidStep.state != FinishState.已完成))
        {
            return;
        }

        if (lidManipulator != null && !lidManipulator.enabled)
        {
            lidManipulator.enabled = true;
        }

        if (lidColliders == null)
        {
            return;
        }

        foreach (Collider value in lidColliders)
        {
            if (value != null && !value.enabled)
            {
                value.enabled = true;
            }
        }
    }

    private bool ResolveReferences()
    {
        if (step != null && source != null && target != null)
        {
            return true;
        }

        if (step == null)
        {
            foreach (TexCore_OrderedPlacementStep candidate in
                     FindObjectsOfType<TexCore_OrderedPlacementStep>(true))
            {
                if (candidate != null && candidate.name == StepName)
                {
                    step = candidate;
                    break;
                }
            }

            if (step == null)
            {
                return false;
            }
        }

        source = step.InteractionSource;
        target = step.Target;

        if (source == null || source.name != SourceName ||
            target == null || target.name != TargetName)
        {
            Debug.LogError(LogPrefix + StepName + " 的绑定不符合预期：source=" +
                (source != null ? source.name : "null") + " target=" +
                (target != null ? target.name : "null") + "，本组件停止工作。");
            enabled = false;
            return false;
        }

        if (!configured)
        {
            // 与 08_04_钻孔 相同的公开入口：手持靠近即完成，不必精确松手。
            step.ConfigureAutoCompleteOnApproach(ApproachTolerance, false);
            configured = true;
            Debug.Log(LogPrefix + StepName + " 已启用靠近即吸附，容差 " +
                ApproachTolerance + " m。");
        }

        return true;
    }

    private bool IsCompleted()
    {
        return step.WasCompletedByUser ||
               step.state == FinishState.已完成 ||
               step.state == FinishState.触发;
    }

    private void HideTarget()
    {
        if (target != null && target.gameObject.activeSelf)
        {
            target.gameObject.SetActive(false);
        }
    }

    private void KeepPlacedState()
    {
        if (target != null && !target.gameObject.activeSelf)
        {
            target.gameObject.SetActive(true);
        }

        if (source != null && source.activeSelf)
        {
            source.SetActive(false);
        }

        HideMarker();
    }

    private void FinalizePlacement()
    {
        // 框架自身的 SnapToTarget 只复制位置与旋转，缩放要在这里补齐，
        // 否则 fuben 与正式充电桩外形不一致。
        if (target.parent == source.transform.parent)
        {
            source.transform.localPosition = target.localPosition;
            source.transform.localRotation = target.localRotation;
        }
        else
        {
            source.transform.SetPositionAndRotation(target.position, target.rotation);
        }

        source.transform.localScale = target.localScale;

        placed = true;

        if (target != null)
        {
            target.gameObject.SetActive(true);
        }

        if (source != null)
        {
            source.SetActive(false);
        }

        HideMarker();

        Debug.Log(LogPrefix + "充电桩已吸附到位：pos=" +
            source.transform.position.ToString("F4") + " rot=" +
            source.transform.rotation.eulerAngles.ToString("F2") + " scale=" +
            source.transform.lossyScale.ToString("F4"));

        TexCore_EventCenter.Broadcast<string, float>(
            TexCore_EventType.显示提示, "充电桩已安装到位", 1.5f);
    }

    private void ShowHintOnce()
    {
        if (hintShown)
        {
            return;
        }

        hintShown = true;

        TexCore_EventCenter.Broadcast<string, float>(
            TexCore_EventType.显示提示,
            "请将充电桩搬到发光标记处，靠近后会自动吸附",
            2f);

        Debug.Log(LogPrefix + "已提示玩家把充电桩搬到标记处。");
    }

    private void ShowMarker()
    {
        EnsureMarkerCreated();
        if (markerRoot == null || markerRing == null || target == null)
        {
            return;
        }

        ResolveMarkerRadius();

        markerRoot.SetActive(true);
        markerRing.enabled = true;
        markerPole.enabled = true;

        // 目标即使被禁用，读它的 transform 也是安全的。
        Vector3 center = target.position;
        float pulseRadius = markerRadius * (1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.06f);

        for (int index = 0; index < MarkerSegments; index++)
        {
            float angle = Mathf.PI * 2f * index / MarkerSegments;
            Vector3 point = center +
                (Vector3.right * Mathf.Cos(angle) + Vector3.forward * Mathf.Sin(angle)) *
                pulseRadius;
            markerRing.SetPosition(index, point);
        }

        markerPole.SetPosition(0, center);
        markerPole.SetPosition(1, center + Vector3.up * (markerRadius * 1.6f));
    }

    private void HideMarker()
    {
        if (markerRing != null)
        {
            markerRing.enabled = false;
        }

        if (markerPole != null)
        {
            markerPole.enabled = false;
        }

        if (markerRoot != null && markerRoot.activeSelf)
        {
            markerRoot.SetActive(false);
        }
    }

    private void ResolveMarkerRadius()
    {
        if (markerRadiusResolved || source == null || !source.activeInHierarchy)
        {
            return;
        }

        bool found = false;
        Bounds bounds = default;

        foreach (Renderer renderer in source.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!found)
        {
            return;
        }

        markerRadius = Mathf.Clamp(
            Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.15f, 0.25f, 1.2f);
        markerRadiusResolved = true;
    }

    private void EnsureMarkerCreated()
    {
        if (markerRoot != null)
        {
            return;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning(LogPrefix +
                "找不到 Sprites/Default，无法绘制目标位置标记。");
            return;
        }

        int layer = target != null ? target.gameObject.layer : 0;

        markerRoot = new GameObject("0809StationTargetMarker")
        {
            hideFlags = HideFlags.DontSave,
            layer = layer
        };
        markerMaterial = new Material(shader)
        {
            name = "0809StationTargetMarkerMaterial",
            hideFlags = HideFlags.DontSave
        };

        markerRing = CreateMarkerLine("Ring", true, MarkerSegments);
        markerPole = CreateMarkerLine("Pole", false, 2);
    }

    private LineRenderer CreateMarkerLine(string objectName, bool loop, int positionCount)
    {
        GameObject lineObject = new GameObject(objectName)
        {
            hideFlags = HideFlags.DontSave,
            layer = markerRoot.layer
        };
        lineObject.transform.SetParent(markerRoot.transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = markerMaterial;
        line.useWorldSpace = true;
        line.loop = loop;
        line.positionCount = positionCount;
        line.startWidth = MarkerLineWidth;
        line.endWidth = MarkerLineWidth;
        line.startColor = MarkerColor;
        line.endColor = MarkerColor;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.lightProbeUsage = LightProbeUsage.Off;
        line.reflectionProbeUsage = ReflectionProbeUsage.Off;
        line.sortingOrder = 60;
        return line;
    }
}
