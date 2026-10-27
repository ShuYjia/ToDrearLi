using Unity.Netcode;
using Unity.Collections;

// 客户端广播寻找房间时发送的数据（目前为空即可，如果要实现服务端过滤可以加字段）
public struct DiscoveryBroadcastData : INetworkSerializable
{
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter { }
}

// 服务端响应房间信息的数据
public struct DiscoveryResponseData : INetworkSerializable
{
    public ushort Port;
    public FixedString64Bytes ServerName; // 房间名称
    public FixedString64Bytes HostName;   // 房主名称
    public int RoomId;                    // 房间序号
    public int CurrentPlayers;            // 当前人数
    public int MaxPlayers;                // 最大人数
    public bool HasPassword;              // 是否有密码

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Port);
        serializer.SerializeValue(ref ServerName);
        serializer.SerializeValue(ref HostName);
        serializer.SerializeValue(ref RoomId);
        serializer.SerializeValue(ref CurrentPlayers);
        serializer.SerializeValue(ref MaxPlayers);
        serializer.SerializeValue(ref HasPassword);
    }
}