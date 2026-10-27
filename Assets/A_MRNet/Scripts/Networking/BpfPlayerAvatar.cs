using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// 远端玩家头像（Host 通过 SpawnWithOwnership 生成，所有权归对应客户端）。
    ///
    /// 可视化：头（球体）+ 左手/右手（小立方体）+ 头顶名牌（世界空间 TMP）。
    /// 阶段 1 全部运行期生成，不依赖预制体里的子节点；
    /// 头/手 Transform 由 <see cref="BpfPlayerPoseSync"/> 驱动。
    ///
    /// 自己的头像（IsOwner）贴在本人 Rig 上；他人头像走网络插值。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfPlayerAvatar : NetworkBehaviour
    {
        private readonly NetworkVariable<FixedString32Bytes> playerName =
            new NetworkVariable<FixedString32Bytes>(
                new FixedString32Bytes("玩家"),
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        /// <summary>按 ClientId 索引的在线头像表（各端本地视图，主线程使用）。</summary>
        public static readonly Dictionary<ulong, BpfPlayerAvatar> Active = new Dictionary<ulong, BpfPlayerAvatar>();

        private Transform headVisual;
        private Transform leftVisual;
        private Transform rightVisual;
        private Transform nameplate;
        private TextMeshPro nameplateText;
        private BpfPlayerPoseSync poseSync;

        public string Nickname => playerName.Value.ToString();

        public Color32 Color => BpfPlayerPalette.GetColor((int)OwnerClientId);

        public BpfPlayerPoseSync PoseSync => poseSync;

        public override void OnNetworkSpawn()
        {
            Active[OwnerClientId] = this;
            BpfPlayerRegistry.Register(OwnerClientId, this);

            playerName.OnValueChanged += HandleNameChanged;

            poseSync = GetComponent<BpfPlayerPoseSync>();
            if (poseSync == null)
            {
                poseSync = gameObject.AddComponent<BpfPlayerPoseSync>();
            }

            EnsureVisuals();
            poseSync.BindVisuals(headVisual, leftVisual, rightVisual);

            BpfNetLog.Info("Bpf|Avatar", "spawned",
                "玩家头像已同步：clientId=" + OwnerClientId + " 昵称=" + Nickname
                + " isOwner=" + IsOwner);
        }

        public override void OnNetworkDespawn()
        {
            playerName.OnValueChanged -= HandleNameChanged;
            if (Active.TryGetValue(OwnerClientId, out BpfPlayerAvatar self) && self == this)
            {
                Active.Remove(OwnerClientId);
            }

            BpfPlayerRegistry.Unregister(OwnerClientId, this);
        }

        /// <summary>Host 专用：写入审批阶段登记的昵称。</summary>
        public void ApplyProfile(BpfConnectionProfile profile)
        {
            if (!IsServer)
            {
                return;
            }

            playerName.Value = new FixedString32Bytes(profile.playerName);
        }

        // ------------------------------------------------------------ 可视化

        /// <summary>运行期生成头/手/名牌（预制体留空时自建，均挂到本头像下随之销毁）。</summary>
        private void EnsureVisuals()
        {
            if (headVisual == null)
            {
                headVisual = CreateVisual("HeadVisual", PrimitiveType.Sphere, 0.16f, Color);
                headVisual.SetParent(transform, true);
            }

            if (leftVisual == null)
            {
                leftVisual = CreateVisual("LeftHandVisual", PrimitiveType.Cube, 0.07f, Color);
                leftVisual.SetParent(transform, true);
            }

            if (rightVisual == null)
            {
                rightVisual = CreateVisual("RightHandVisual", PrimitiveType.Cube, 0.07f, Color);
                rightVisual.SetParent(transform, true);
            }

            if (nameplate == null)
            {
                nameplate = CreateNameplate(Nickname, Color);
                if (nameplate != null)
                {
                    nameplate.SetParent(transform, true);
                    nameplateText = nameplate.GetComponentInChildren<TextMeshPro>();
                }
            }
        }

        private static Transform CreateVisual(string name, PrimitiveType type, float size, Color32 color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                // 可视化副本不参与物理
                Object.Destroy(collider);
            }

            go.transform.localScale = Vector3.one * size;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                // URP 工程里 Standard 不存在，按管线逐级回退
                Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                                ?? Shader.Find("Standard")
                                ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    renderer.material = new Material(shader);
                    renderer.material.color = color;
                }
            }

            return go.transform;
        }

        private static Transform CreateNameplate(string nickname, Color32 color)
        {
            GameObject go = new GameObject("Nameplate");
            TextMeshPro text = go.AddComponent<TextMeshPro>();
            text.text = string.IsNullOrEmpty(nickname) ? "玩家" : nickname;
            text.fontSize = 4f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font != null)
            {
                text.font = font;
            }

            // 世界空间文本：默认 1 单位=1 米太大，缩到挂牌尺度
            go.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            return go.transform;
        }

        private void Update()
        {
            // 名牌跟随头顶并面向相机
            if (nameplate != null && headVisual != null)
            {
                nameplate.position = headVisual.position + Vector3.up * 0.25f;
                Camera cam = Camera.main;
                if (cam != null)
                {
                    nameplate.forward = cam.transform.forward;
                }
            }
        }

        private void HandleNameChanged(FixedString32Bytes previous, FixedString32Bytes current)
        {
            if (nameplateText != null)
            {
                nameplateText.text = current.ToString();
            }

            BpfNetLog.Info("Bpf|Avatar", "name_synced", "clientId=" + OwnerClientId + " 昵称=" + current);
        }
    }
}
