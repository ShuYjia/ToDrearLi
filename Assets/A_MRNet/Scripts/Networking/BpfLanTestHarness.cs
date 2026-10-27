using MixedReality.Toolkit.SpatialManipulation;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// 第一阶段验证台（OnGUI，编辑器 Play / 开发构建可用）。
    ///
    /// 挂在验证场景 BpfLanTest.unity，提供最小操作面：
    /// 建房 / 扫描 / 加入 / 离开 + 实时房间与状态显示。
    /// 不依赖任何场景 UI 与 EventSystem，双击即可跑通 Gate B。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfLanTestHarness : MonoBehaviour
    {
        private const string LogTag = "Bpf|Harness";

        private string roomName = "BPF验证房间";
        private string hostAddress = "127.0.0.1";
        private string status = "点击「初始化」开始";
        private Vector2 roomScroll;
        private bool subscribed;

        private BpfNetworkBootstrap bootstrap;
        private BpfInteractiveObject testInteractive;

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (bootstrap == null)
            {
                return;
            }

            if (bootstrap.RoomService != null)
            {
                bootstrap.RoomService.StatusChanged -= HandleStatus;
                bootstrap.RoomService.Event -= HandleEvent;
            }

            if (bootstrap.Discovery != null)
            {
                bootstrap.Discovery.RoomDiscovered -= HandleRoomDiscovered;
            }

            subscribed = false;
        }

        private void TrySubscribe()
        {
            if (subscribed)
            {
                return;
            }

            bootstrap = BpfNetworkBootstrap.Instance;
            if (bootstrap == null)
            {
                return;
            }

            if (bootstrap.RoomService != null)
            {
                bootstrap.RoomService.StatusChanged += HandleStatus;
                bootstrap.RoomService.Event += HandleEvent;
            }

            if (bootstrap.Discovery != null)
            {
                bootstrap.Discovery.RoomDiscovered += HandleRoomDiscovered;
            }

            subscribed = true;
            status = "网络根已就绪";
        }

        private void HandleStatus(string message)
        {
            status = message;
        }

        private void HandleEvent(BpfSessionEvent evt)
        {
            status = evt.message;
        }

        private void HandleRoomDiscovered(BpfDiscoveredRoom room)
        {
            status = "发现房间：" + room.Describe();
        }

        // ------------------------------------------------------------ UI

        private void OnGUI()
        {
            TrySubscribe();

            GUILayout.BeginArea(new Rect(16f, 16f, 460f, 620f));
            GUILayout.BeginVertical("box");
            GUILayout.Label("BPF 局域网联机验证台（阶段 0）", GUI.skin.label);
            GUILayout.Space(6f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("房间名", GUILayout.Width(56f));
            roomName = GUILayout.TextField(roomName, GUILayout.Width(200f));
            if (GUILayout.Button("创建房间", GUILayout.Width(90f)))
            {
                EnsureRoot();
                bootstrap.RoomService.LocalPlayerName = "HostHarness";
                bootstrap.RoomService.CreateRoom(roomName);
            }

            if (GUILayout.Button("离开", GUILayout.Width(60f)))
            {
                if (bootstrap != null)
                {
                    bootstrap.RoomService.LeaveRoom();
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Host IP", GUILayout.Width(56f));
            hostAddress = GUILayout.TextField(hostAddress, GUILayout.Width(200f));
            if (GUILayout.Button("加入", GUILayout.Width(90f)))
            {
                EnsureRoot();
                bootstrap.RoomService.LocalPlayerName = "ClientHarness";
                bootstrap.RoomService.JoinRoom(hostAddress, roomName);
            }

            if (GUILayout.Button("扫描", GUILayout.Width(60f)))
            {
                EnsureRoot();
                bootstrap.Discovery.StartScan();
                status = "扫描中…";
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // 阶段 4 抓取平滑验证：生成带 ObjectManipulator 的测试物，
            // 用公开 API 驱动抓取/释放（无 XR 硬件也能验仲裁与位姿流）。
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("生成抓取物", GUILayout.Width(90f)))
            {
                SpawnTestInteractive();
            }

            if (GUILayout.Button("抓取/释放", GUILayout.Width(90f)))
            {
                ToggleTestGrab();
            }

            if (GUILayout.Button("阶段4自测", GUILayout.Width(90f)))
            {
                BpfStage4SelfTest.Run();
                status = "阶段 4 自测已启动（看控制台 Bpf|Stage4Test）";
            }

            if (GUILayout.Button("联机预检", GUILayout.Width(90f)))
            {
                BpfPreflightSelfTest.Run();
                status = "联机预检已启动（看控制台 Bpf|Preflight）";
            }

            if (GUILayout.Button("客户端路径自测", GUILayout.Width(110f)))
            {
                BpfClientPathSelfTest.Run();
                status = "客户端路径自测已启动（看控制台 Bpf|ClientPathTest）";
            }

            GUILayout.EndHorizontal();
            GUILayout.Label("抓取物：" + (testInteractive != null
                ? testInteractive.name + (testInteractive.LocalHeld ? "（本机持有）" : "（空闲）")
                : "（未生成）"));

            GUILayout.Space(6f);
            GUILayout.Label("状态：" + status);
            if (bootstrap != null)
            {
                GUILayout.Label("会话阶段：" + bootstrap.SessionPhase
                    + "  NetworkManager监听=" + (bootstrap.NetworkManager != null && bootstrap.NetworkManager.IsListening)
                    + "  LocalClientId=" + (bootstrap.NetworkManager != null ? bootstrap.NetworkManager.LocalClientId.ToString() : "-"));
            }

            GUILayout.Space(6f);
            GUILayout.Label("房间列表：");
            roomScroll = GUILayout.BeginScrollView(roomScroll, GUILayout.Height(200f));
            if (bootstrap != null && bootstrap.Discovery != null)
            {
                IReadOnlyList<BpfDiscoveredRoom> rooms = bootstrap.Discovery.Rooms;
                if (rooms.Count == 0)
                {
                    GUILayout.Label("（空）");
                }

                foreach (BpfDiscoveredRoom room in rooms)
                {
                    GUILayout.BeginHorizontal("box");
                    GUILayout.Label(room.Describe());
                    if (GUILayout.Button("加入", GUILayout.Width(50f)))
                    {
                        bootstrap.RoomService.LocalPlayerName = "ClientHarness";
                        bootstrap.RoomService.JoinRoom(room.hostAddress, room.roomName);
                    }

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void EnsureRoot()
        {
            if (bootstrap == null)
            {
                bootstrap = BpfNetworkBootstrap.EnsureRuntimeRoot();
                TrySubscribe();
            }
        }

        // ------------------------------------------------------------ 抓取验证

        private void SpawnTestInteractive()
        {
            EnsureRoot();

            if (testInteractive != null)
            {
                Destroy(testInteractive.gameObject);
                testInteractive = null;
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "BPFTestGrab";
            go.transform.position = new Vector3(0f, 1.2f, 1.5f);
            go.transform.localScale = Vector3.one * 0.15f;

            // MRTK3 ObjectManipulator：挂上即接本地抓取输入；
            // 挂载失败不影响手动 API 验证。
            try
            {
                go.AddComponent<ObjectManipulator>();
            }
            catch (System.Exception e)
            {
                status = "ObjectManipulator 挂载失败：" + e.Message;
            }

            testInteractive = go.AddComponent<BpfInteractiveObject>();
            status = "已生成测试抓取物（在房间内且校准后点「抓取/释放」）";
        }

        private void ToggleTestGrab()
        {
            if (testInteractive == null)
            {
                status = "请先生成抓取物";
                return;
            }

            if (testInteractive.LocalHeld)
            {
                testInteractive.EndLocalRelease();
                status = "已释放";
                return;
            }

            bool accepted = testInteractive.BeginLocalGrab();
            status = accepted ? "抓取已受理" : "抓取被拒绝（不在房间或未校准）";
        }
    }
}
