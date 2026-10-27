using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bpf.Networking
{
    /// <summary>
    /// 三点校准采集器（阶段 5 设备侧输入）。
    ///
    /// 背景：<see cref="BpfSessionCalibration.SubmitLocalPoints"/> 此前没有任何
    /// 调用者——设备上点了「开始校准」后无从提交三个物理标记点，校准永远
    /// 完不成。本组件补上采集入口：
    ///
    ///   - 校准阶段（PointHint &gt; 0）扣任一手柄扳机 = 记录该手当前位置为一个点；
    ///   - 三点齐了自动 <see cref="BpfSessionCalibration.SubmitLocalPoints"/>；
    ///   - Host 与 Client 走同一条路径（Host 的三个点即会话空间基准）；
    ///   - 每采完一点把提示推进到下一个索引（大厅面板的提示文字随之更新）。
    ///
    /// 扳机绑定沿用 BpfDiagnosticsOverlay 的 InputAction 模式
    /// （&lt;XRController&gt;{Hand}/trigger，PICO OpenXR 下左右手柄均可用）。
    /// 无 XR 环境（编辑器/验证台）可用 <see cref="CaptureNextPoint"/> 手动驱动。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfCalibrationCapture : MonoBehaviour
    {
        private const string LogTag = "Bpf|CalibCapture";

        [Tooltip("左手扳机绑定（新版 Input System）。")]
        [SerializeField] private string leftTriggerBinding = "<XRController>{LeftHand}/trigger";

        [Tooltip("右手扳机绑定（新版 Input System）。")]
        [SerializeField] private string rightTriggerBinding = "<XRController>{RightHand}/trigger";

        private InputAction leftTrigger;
        private InputAction rightTrigger;
        private bool wasLeftPressed;
        private bool wasRightPressed;

        private readonly Vector3[] points = new Vector3[3];
        private int captured;

        /// <summary>当前应采集第几点（1..3）；0 = 不在采集状态。</summary>
        private int awaiting;

        private void OnEnable()
        {
            BpfSessionCalibration.PointHint += HandlePointHint;
            leftTrigger = CreateAction(leftTriggerBinding);
            rightTrigger = CreateAction(rightTriggerBinding);
        }

        private void OnDisable()
        {
            BpfSessionCalibration.PointHint -= HandlePointHint;

            if (leftTrigger != null)
            {
                leftTrigger.Dispose();
                leftTrigger = null;
            }

            if (rightTrigger != null)
            {
                rightTrigger.Dispose();
                rightTrigger = null;
            }
        }

        private void Update()
        {
            if (awaiting <= 0 || leftTrigger == null || rightTrigger == null)
            {
                return;
            }

            // 边沿检测：按住不连续采集，避免一个点被重复记录
            bool leftNow = leftTrigger.IsPressed();
            bool rightNow = rightTrigger.IsPressed();
            bool leftEdge = leftNow && !wasLeftPressed;
            bool rightEdge = rightNow && !wasRightPressed;
            wasLeftPressed = leftNow;
            wasRightPressed = rightNow;

            if (leftEdge)
            {
                CaptureNextPoint(true);
            }
            else if (rightEdge)
            {
                CaptureNextPoint(false);
            }
        }

        /// <summary>
        /// 记录一个点（扳机入口或外部手动入口共用）。
        /// leftHand=true 取左手当前位置，否则右手。返回 false = 未记录
        /// （不在采集状态 / 该手未跟踪 / 会话未就绪）。
        /// </summary>
        public bool CaptureNextPoint(bool leftHand)
        {
            if (awaiting <= 0)
            {
                return false;
            }

            BpfSessionCalibration calibration = BpfSessionState.Instance != null
                ? BpfSessionState.Instance.Calibration
                : null;
            if (calibration == null)
            {
                BpfNetLog.Warning(LogTag, "no_calibration", "会话状态未就绪，无法采集");
                return false;
            }

            if (!BpfLocalRigPoseSource.TryCapturePose(out BpfPosePayload pose))
            {
                BpfNetLog.Warning(LogTag, "no_rig", "找不到本机 XR Rig（头/手），无法采集");
                return false;
            }

            bool tracked = leftHand ? pose.hasLeft : pose.hasRight;
            if (!tracked)
            {
                BpfNetLog.Warning(LogTag, "hand_untracked",
                    (leftHand ? "左手" : "右手") + "未跟踪，换一只手扣扳机");
                return false;
            }

            Vector3 point = leftHand
                ? new Vector3(pose.leftPosX, pose.leftPosY, pose.leftPosZ)
                : new Vector3(pose.rightPosX, pose.rightPosY, pose.rightPosZ);

            points[captured] = point;
            captured++;

            BpfNetLog.Info(LogTag, "point_captured",
                "已采集第 " + captured + " 个校准点：" + V3(point)
                + "（" + (leftHand ? "左手" : "右手") + "）");

            if (captured >= 3)
            {
                Submit(calibration);
                return true;
            }

            awaiting = captured + 1;
            BpfSessionCalibration.NotifyPointHint(awaiting);
            return true;
        }

        private void Submit(BpfSessionCalibration calibration)
        {
            calibration.SubmitLocalPoints(points[0], points[1], points[2]);
            captured = 0;
            awaiting = 0;
            BpfSessionCalibration.NotifyPointHint(0);
            BpfNetLog.Info(LogTag, "submitted", "三个校准点已上报，等待 Host 解算");
        }

        private void HandlePointHint(int index)
        {
            if (index <= 0)
            {
                // 校准结束或被重置：清空本机采集进度
                captured = 0;
                awaiting = 0;
                return;
            }

            if (index == 1)
            {
                captured = 0; // 重新开始一轮
            }

            awaiting = index;
        }

        private static InputAction CreateAction(string binding)
        {
            if (string.IsNullOrEmpty(binding))
            {
                return null;
            }

            InputAction action = new InputAction(name: "BpfCalibrationTrigger", type: InputActionType.Button);
            action.AddBinding(binding);
            action.Enable();
            return action;
        }

        private static string V3(Vector3 v)
        {
            return "(" + v.x.ToString("F2") + ", " + v.y.ToString("F2") + ", " + v.z.ToString("F2") + ")";
        }
    }
}
