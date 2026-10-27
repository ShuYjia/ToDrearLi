using System.Collections.Generic;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// 本机 XR Rig 姿态采样（头 + 左手 + 右手）。
    ///
    /// 挂在网络根对象上（DontDestroyOnLoad），业务场景切换后自动重新
    /// 绑定新场景的 Rig。寻址策略（按优先级）：
    ///   1. 编辑器里序列化引用的 Transform；
    ///   2. 头部：Camera.main（XR 头显相机即主相机）；
    ///   3. 手部：在相机所在 Rig 根下按名字匹配
    ///      （Left/Right + Controller/Hand/Ray/Interactor）。
    /// 找不到的手部以 hasLeft/hasRight=false 上报，头像只显示已跟踪的部位。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfLocalRigPoseSource : MonoBehaviour
    {
        private const string LogTag = "Bpf|RigPose";

        [Tooltip("头部 Transform（留空则用 Camera.main）。")]
        [SerializeField] private Transform headOverride;

        [Tooltip("左手 Transform（留空则自动查找）。")]
        [SerializeField] private Transform leftHandOverride;

        [Tooltip("右手 Transform（留空则自动查找）。")]
        [SerializeField] private Transform rightHandOverride;

        private static BpfLocalRigPoseSource instance;

        /// <summary>静态采样入口；网络根不存在时返回 false。</summary>
        public static BpfLocalRigPoseSource Instance => instance;

        private Transform head;
        private Transform leftHand;
        private Transform rightHand;
        private float nextRescanTime;

        private void Awake()
        {
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            // 场景切换后 Rig 会换：周期性重扫，直到三类目标都找到。
            if (head == null || leftHand == null || rightHand == null)
            {
                if (Time.time >= nextRescanTime)
                {
                    nextRescanTime = Time.time + 2f;
                    ResolveRig();
                }
            }
        }

        private void ResolveRig()
        {
            head = headOverride != null ? headOverride : (Camera.main != null ? Camera.main.transform : null);
            leftHand = leftHandOverride;
            rightHand = rightHandOverride;

            if (head == null)
            {
                return;
            }

            if (leftHand == null || rightHand == null)
            {
                Transform rigRoot = head.root;
                List<Transform> candidates = new List<Transform>();
                Collect(rigRoot, candidates);

                foreach (Transform candidate in candidates)
                {
                    string name = candidate.name.ToLowerInvariant();
                    if (leftHand == null && name.Contains("left")
                        && (name.Contains("controller") || name.Contains("hand") || name.Contains("ray") || name.Contains("interactor")))
                    {
                        leftHand = candidate;
                    }
                    else if (rightHand == null && name.Contains("right")
                        && (name.Contains("controller") || name.Contains("hand") || name.Contains("ray") || name.Contains("interactor")))
                    {
                        rightHand = candidate;
                    }
                }
            }

            BpfNetLog.Info(LogTag, "rig_resolved",
                "head=" + (head != null ? head.name : "-")
                + " left=" + (leftHand != null ? leftHand.name : "-")
                + " right=" + (rightHand != null ? rightHand.name : "-"));
        }

        private static void Collect(Transform root, List<Transform> output)
        {
            if (root == null)
            {
                return;
            }

            output.Add(root);
            for (int i = 0; i < root.childCount; i++)
            {
                Collect(root.GetChild(i), output);
            }
        }

        /// <summary>
        /// 采样当前姿态。返回 false 表示连头部都找不到（不应发生）。
        /// </summary>
        public static bool TryCapturePose(out BpfPosePayload pose)
        {
            pose = default;
            if (instance == null)
            {
                return false;
            }

            return instance.Capture(out pose);
        }

        private bool Capture(out BpfPosePayload pose)
        {
            pose = default;
            if (head == null)
            {
                return false;
            }

            pose.headPosX = head.position.x;
            pose.headPosY = head.position.y;
            pose.headPosZ = head.position.z;
            pose.headRotX = head.rotation.x;
            pose.headRotY = head.rotation.y;
            pose.headRotZ = head.rotation.z;
            pose.headRotW = head.rotation.w;

            if (leftHand != null)
            {
                pose.hasLeft = true;
                pose.leftPosX = leftHand.position.x;
                pose.leftPosY = leftHand.position.y;
                pose.leftPosZ = leftHand.position.z;
                pose.leftRotX = leftHand.rotation.x;
                pose.leftRotY = leftHand.rotation.y;
                pose.leftRotZ = leftHand.rotation.z;
                pose.leftRotW = leftHand.rotation.w;
            }

            if (rightHand != null)
            {
                pose.hasRight = true;
                pose.rightPosX = rightHand.position.x;
                pose.rightPosY = rightHand.position.y;
                pose.rightPosZ = rightHand.position.z;
                pose.rightRotX = rightHand.rotation.x;
                pose.rightRotY = rightHand.rotation.y;
                pose.rightRotZ = rightHand.rotation.z;
                pose.rightRotW = rightHand.rotation.w;
            }

            return true;
        }

        /// <summary>当前头部 Transform（编辑器调试/校准流程用）。</summary>
        public Transform Head => head;
    }
}
