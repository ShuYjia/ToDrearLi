using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;

namespace _syc_resource._my_scripts
{
    public class RoomBrowserCtrl : MonoBehaviour
    {
        private ExampleNetworkDiscovery networkDiscovery;

        [Header("建房 UI 引用 (可选)")]
        public TMP_InputField createRoomNameInput;

        private void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                networkDiscovery = NetworkManager.Singleton.GetComponent<ExampleNetworkDiscovery>();
            }
        }

        // ==========================================
        // 主机建房逻辑（保持不变）
        // ==========================================
        public void CreateRoom()
        {
            string roomName = "新房间";
            if (createRoomNameInput != null && !string.IsNullOrEmpty(createRoomNameInput.text))
            {
                roomName = createRoomNameInput.text;
            }

            networkDiscovery.ServerName = roomName;

            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
            if (transport != null)
            {
                transport.SetConnectionData(transport.ConnectionData.Address, transport.ConnectionData.Port, "0.0.0.0");
            }

            NetworkManager.Singleton.StartHost();
            networkDiscovery.StartServer();

            Debug.Log("房间创建成功，准备跳转大厅跳板...");

            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene("Lobby", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

        // ==========================================
        // 客机加入逻辑（核心修复：把活交给不死的雷达去干）
        // ==========================================
        public void QuickJoin()
        {
            // 在场景中找到挂在 NetworkManager 上的不死雷达
            AutoJoinManager autoJoin = FindObjectOfType<AutoJoinManager>();

            if (autoJoin != null)
            {
                Debug.Log("UI 发送指令：呼叫不死雷达开始扫描...");
                autoJoin.StartScanning(); // 开始扫描，后续的跳转由它全权接管！
            }
            else
            {
                Debug.LogError("致命错误：找不到 AutoJoinManager！请务必把它挂在 NetworkManager 所在的物体上！");
            }
        }
    }
}