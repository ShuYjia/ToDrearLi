using System.Net;
using System.Collections; // 必须引入，用于协程
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(ExampleNetworkDiscovery))]
public class AutoJoinManager : MonoBehaviour
{
    private ExampleNetworkDiscovery _discovery;
    private bool _isScanning = false;

    // ==========================================
    // 跨线程通讯变量（防止死锁的核心）
    // ==========================================
    private bool _needToConnect = false;
    private string _targetIp = "";
    private ushort _targetPort = 0;

    void Awake()
    {
        _discovery = GetComponent<ExampleNetworkDiscovery>();
        _discovery.OnServerFound.AddListener(OnServerFoundCallback);
    }

    public void StartScanning()
    {
        if (_isScanning) return;

        _isScanning = true;
        Debug.Log("<color=yellow>客机雷达开启：正在扫描局域网房间...</color>");

        _discovery.StartClient();
        _discovery.ClientBroadcast(new DiscoveryBroadcastData());
    }

    // ==========================================
    // 后台 UDP 线程：只记录数据，绝对不碰 NetworkManager！
    // ==========================================
    private void OnServerFoundCallback(IPEndPoint sender, DiscoveryResponseData response)
    {
        if (!_isScanning || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;

        // 记录扫到的 IP 和端口
        _targetIp = sender.Address.ToString();
        _targetPort = response.Port;
        _isScanning = false;

        // 举起小旗子，呼叫 Unity 主线程接手干活
        _needToConnect = true;
    }

    // ==========================================
    // 主线程守护神：Update 永远在安全的主线程执行
    // ==========================================
    private void Update()
    {
        if (_needToConnect)
        {
            _needToConnect = false; // 放下旗子
            StartCoroutine(ConnectWithDelay()); // 开启安全跃迁协程
        }
    }

    // ==========================================
    // 延迟连接：给底层端口释放的喘息时间
    // ==========================================
    private IEnumerator ConnectWithDelay()
    {
        Debug.Log($"<color=yellow>🎯 发现主机！正在关闭雷达并释放端口，0.5秒后发起连接...</color>");

        // 1. 先关闭局域网雷达
        _discovery.StopDiscovery();

        // 2. ⚠️ 核心修复：硬性等待 0.5 秒！（彻底避免端口和场景加载死锁）
        yield return new WaitForSeconds(0.5f);

        // 3. 自动配置目标 IP 和 端口
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        transport.SetConnectionData(_targetIp, _targetPort);

        // 4. 防超时安全设置 (10秒)
        NetworkManager.Singleton.NetworkConfig.ClientConnectionBufferTimeout = 10000;

        Debug.Log($"<color=green>🚀 端口释放完毕，安全发起连接！目标 IP: {_targetIp}</color>");

        // 5. 安全启动客机！
        NetworkManager.Singleton.StartClient();
    }
}