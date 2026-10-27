using System;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NetworkManager))]
public class ExampleNetworkDiscovery : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>
{
    [Serializable]
    public class ServerFoundEvent : UnityEvent<IPEndPoint, DiscoveryResponseData>
    {
    };

    NetworkManager m_NetworkManager;
    
    [SerializeField]
    [Tooltip("If true NetworkDiscovery will make the server visible and answer to client broadcasts as soon as netcode starts running as server.")]
    bool m_StartWithServer = true;

    public string ServerName = "EnterName";

    public ServerFoundEvent OnServerFound;
    
    private bool m_HasStartedWithServer = false;

    // 【新增】用来缓存当前房间的唯一序号
    private int _myRoomId; 

    public void Awake()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
        
        // 【新增】游戏启动时就固定好这个房间的序号，不要每次被搜都变
        _myRoomId = Random.Range(1000, 9999); 
    }

    public void Update()
    {
        if (m_StartWithServer && m_HasStartedWithServer == false && IsRunning == false)
        {
            if (m_NetworkManager.IsServer)
            {
                StartServer();
                m_HasStartedWithServer = true;
            }
        }
    }

    // 在 ExampleNetworkDiscovery.cs 中修改此方法
    protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response)
    {
        response = new DiscoveryResponseData()
        {
            Port = ((UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport).ConnectionData.Port,
            ServerName = this.ServerName, 
            HostName = "Player_" + NetworkManager.Singleton.LocalClientId, 
            
            // 【修改】使用缓存的固定 ID，删掉 Random.Range
            RoomId = _myRoomId, 
            
            CurrentPlayers = NetworkManager.Singleton.ConnectedClients.Count, 
            MaxPlayers = 4, 
            HasPassword = false 
        };
        return true;
    }

    protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response)
    {
        OnServerFound.Invoke(sender, response);
    }
}