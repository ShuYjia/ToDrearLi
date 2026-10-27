using UnityEngine;
using Bpf.Diagnostics;

/// <summary>
/// 09 机器人“只允许平移”策略：任何时刻都锁死刚体的三轴旋转，
/// 并清掉残余角速度，保证手柄抓取移动时只发生平移。
/// </summary>
/// <remarks>
/// 场景里 <c>AGV/AGV1</c> 的 Rigidbody 已经设置 FreezeRotation，这里再做一层
/// 运行期兜底，原因是 MRTK 的 <c>ObjectManipulator.SetupRigidbodyManipulation()</c>
/// 在 VelocityTracking 抓取期间会把刚体切成非运动学（动态），一旦旋转自由度
/// 没被锁住，与货箱/地面的接触力矩就会让机器人边移动边打转。
/// 位置自由度完全不改：吸附、推出、推至二维码都照旧。
/// </remarks>
[DisallowMultipleComponent]
public sealed class TexCore_M09RobotTranslationOnly : MonoBehaviour
{
    private const RigidbodyConstraints RotationLock = RigidbodyConstraints.FreezeRotation;

    [Tooltip("被限制为只能平移的刚体，留空则取本物体上的 Rigidbody。")]
    [SerializeField] private Rigidbody body;

    private bool reportedCorrection;

    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void FixedUpdate()
    {
        Apply();
    }

    private void Apply()
    {
        if (body == null)
        {
            return;
        }

        bool corrected = false;
        if ((body.constraints & RotationLock) != RotationLock)
        {
            body.constraints |= RotationLock;
            corrected = true;
        }

        if (body.angularVelocity != Vector3.zero)
        {
            body.angularVelocity = Vector3.zero;
            corrected = true;
        }

        if (!corrected || reportedCorrection)
        {
            return;
        }

        // 只在第一次纠正时记录一次，便于确认兜底真的生效过。
        reportedCorrection = true;
        BpfLog.Info(BpfLogCategory.Scene, "tex_core_m09_robot_translation_only.enforced",
            "09 机器人旋转已锁定，只允许平移", gameObject,
            BpfLogField.Of("constraints", body.constraints));
    }
}
