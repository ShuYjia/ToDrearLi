using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Net.NetworkInformation;

[DisallowMultipleComponent]
public abstract class NetworkDiscovery<TBroadCast, TResponse> : MonoBehaviour
    where TBroadCast : INetworkSerializable, new()
    where TResponse : INetworkSerializable, new()
{
    private enum MessageType : byte
    {
        BroadCast = 0,
        Response = 1,
    }

    UdpClient m_Client;

    [SerializeField] ushort m_Port = 47777;

    // This is long because unity inspector does not like ulong.
    // 硬编码唯一ID，确保所有打包出去的设备绝对一致
    [SerializeField]
    long m_UniqueApplicationId = 20260304123456L; 

    // 把 OnValidate 方法直接删掉或者注释掉，不让它乱改 ID
    /*
    void OnValidate() { ... }
    */

    /// <summary>
    /// Gets a value indicating whether the discovery is running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets whether the discovery is in server mode.
    /// </summary>
    public bool IsServer { get; private set; }

    /// <summary>
    /// Gets whether the discovery is in client mode.
    /// </summary>
    public bool IsClient { get; private set; }

    public void OnApplicationQuit()
    {
        StopDiscovery();
    }
    

    public void ClientBroadcast(TBroadCast broadCast)
    {
        if (!IsClient)
        {
            throw new InvalidOperationException("Cannot send client broadcast while not running in client mode.");
        }

        using (FastBufferWriter writer = new FastBufferWriter(1024, Unity.Collections.Allocator.Temp, 1024 * 64))
        {
            WriteHeader(writer, MessageType.BroadCast);
            writer.WriteNetworkSerializable(broadCast);
            var data = writer.ToArray();

            try
            {
                // 1. 发送默认全局广播 (兼容性保留)
                m_Client.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, m_Port));

                // 2. 【核心修复】遍历所有网卡，计算正确的子网广播地址并发送
                foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        foreach (var ip in networkInterface.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip.Address))
                            {
                                byte[] addressBytes = ip.Address.GetAddressBytes();
                                byte[] subnetBytes = ip.IPv4Mask.GetAddressBytes();
                                
                                if (subnetBytes.Length == 4)
                                {
                                    byte[] broadcastBytes = new byte[4];
                                    for (int i = 0; i < 4; i++)
                                    {
                                        broadcastBytes[i] = (byte)(addressBytes[i] | (subnetBytes[i] ^ 255));
                                    }
                                    IPAddress broadcastIP = new IPAddress(broadcastBytes);
                                    m_Client.SendAsync(data, data.Length, new IPEndPoint(broadcastIP, m_Port));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    /// <summary>
    /// Starts the discovery in server mode which will respond to client broadcasts searching for servers.
    /// </summary>
    public void StartServer()
    {
        StartDiscovery(true);
    }

    /// <summary>
    /// Starts the discovery in client mode. <see cref="ClientBroadcast"/> can be called to send out broadcasts to servers and the client will actively listen for responses.
    /// </summary>
    public void StartClient()
    {
        StartDiscovery(false);
    }

    public void StopDiscovery()
    {
        IsClient = false;
        IsServer = false;
        IsRunning = false;

        if (m_Client != null)
        {
            try
            {
                m_Client.Close();
            }
            catch (Exception)
            {
                // We don't care about socket exception here. Socket will always be closed after this.
            }

            m_Client = null;
        }
    }

    /// <summary>
    /// Gets called whenever a broadcast is received. Creates a response based on the incoming broadcast data.
    /// </summary>
    /// <param name="sender">The sender of the broadcast</param>
    /// <param name="broadCast">The broadcast data which was sent</param>
    /// <param name="response">The response to send back</param>
    /// <returns>True if a response should be sent back else false</returns>
    protected abstract bool ProcessBroadcast(IPEndPoint sender, TBroadCast broadCast, out TResponse response);

    /// <summary>
    /// Gets called when a response to a broadcast gets received
    /// </summary>
    /// <param name="sender">The sender of the response</param>
    /// <param name="response">The value of the response</param>
    protected abstract void ResponseReceived(IPEndPoint sender, TResponse response);

    void StartDiscovery(bool isServer)
    {
        StopDiscovery();

        IsServer = isServer;
        IsClient = !isServer;

        // If we are not a server we use the 0 port (let udp client assign a free port to us)
        var port = isServer ? m_Port : 0;

        // 强制绑定 0.0.0.0 (IPv4 Any)，确保不会监听到 IPv6 导致错失数据
        m_Client = new UdpClient(new IPEndPoint(IPAddress.Any, port)) { EnableBroadcast = true, MulticastLoopback = false };

        // ⚠️ 极其关键的时序修复：必须先告诉雷达“正在运行”，再开启耳朵！
        IsRunning = true;
        _ = ListenAsync(isServer ? ReceiveBroadcastAsync : new Func<Task>(ReceiveResponseAsync));
    }

    async Task ListenAsync(Func<Task> onReceiveTask)
    {
        // 🛠️ 官方 Bug 修复 1：把无脑的 while(true) 改成受控的 while(IsRunning)
        while (IsRunning)
        {
            try
            {
                await onReceiveTask();
            }
            catch (ObjectDisposedException)
            {
                // socket has been closed
                break;
            }
            catch (Exception)
            {
                // 🛠️ 官方 Bug 修复 2：如果端口被置空，强行打断死循环，绝不吃干 CPU！
                if (m_Client == null || !IsRunning)
                {
                    break;
                }
            }
        }
    }

    async Task ReceiveResponseAsync()
    {
        UdpReceiveResult udpReceiveResult = await m_Client.ReceiveAsync();

        var segment = new ArraySegment<byte>(udpReceiveResult.Buffer, 0, udpReceiveResult.Buffer.Length);
        using var reader = new FastBufferReader(segment, Allocator.Persistent);

        try
        {
            if (ReadAndCheckHeader(reader, MessageType.Response) == false)
            {
                return;
            }
            
            reader.ReadNetworkSerializable(out TResponse receivedResponse);
            ResponseReceived(udpReceiveResult.RemoteEndPoint, receivedResponse);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    async Task ReceiveBroadcastAsync()
    {
        UdpReceiveResult udpReceiveResult = await m_Client.ReceiveAsync();

        var segment = new ArraySegment<byte>(udpReceiveResult.Buffer, 0, udpReceiveResult.Buffer.Length);
        using var reader = new FastBufferReader(segment, Allocator.Persistent);

        try
        {
            if (ReadAndCheckHeader(reader, MessageType.BroadCast) == false)
            {
                return;
            }
            
            reader.ReadNetworkSerializable(out TBroadCast receivedBroadcast);

            if (ProcessBroadcast(udpReceiveResult.RemoteEndPoint, receivedBroadcast, out TResponse response))
            {
                using var writer = new FastBufferWriter(1024, Allocator.Persistent, 1024 * 64);
                WriteHeader(writer, MessageType.Response);

                writer.WriteNetworkSerializable(response);
                var data = writer.ToArray();

                await m_Client.SendAsync(data, data.Length, udpReceiveResult.RemoteEndPoint);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    
    private void WriteHeader(FastBufferWriter writer, MessageType messageType)
    {
        // Serialize unique application id to make sure packet received is from same application.
        writer.WriteValueSafe(m_UniqueApplicationId);

        // Write a flag indicating whether this is a broadcast
        writer.WriteByteSafe((byte) messageType);
    }

    private bool ReadAndCheckHeader(FastBufferReader reader, MessageType expectedType)
    {
        reader.ReadValueSafe(out long receivedApplicationId);
        if (receivedApplicationId != m_UniqueApplicationId)
        {
            return false;
        }

        reader.ReadByteSafe(out byte messageType);
        if (messageType != (byte) expectedType)
        {
            return false;
        }

        return true;
    }
}