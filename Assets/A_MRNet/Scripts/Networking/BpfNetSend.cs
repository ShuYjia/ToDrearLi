using Unity.Collections;
using Unity.Netcode;

namespace Bpf.Networking
{
    /// <summary>
    /// 命名消息发送助手：统一处理 NGO「Client 只能发给 Host」的限制。
    ///
    /// NGO 1.5.2 的 CustomMessagingManager.SendNamedMessageToAll 内部走
    /// SendNamedMessage(name, IReadOnlyList&lt;ulong&gt;, ...) 重载，该重载在
    /// 非 Server 上直接抛 InvalidOperationException
    /// （"Can not send unnamed messages to multiple users as a client"）。
    /// 因此 Client origin 的消息必须用 ServerClientId 单发给 Host；
    /// Host/Server origin 的消息仍走全员广播（Host 本机客户端本地直送）。
    ///
    /// 阶段 1~3 的客户端发送点（姿态广播、校准上报、交互请求）此前直接用
    /// SendNamedMessageToAll，在真实 Client 上会抛异常被 try/catch 吞掉——
    /// 单机实测只有 Host 在发，所以没暴露。阶段 4 起统一走本助手。
    /// </summary>
    public static class BpfNetSend
    {
        /// <summary>
        /// Server/Host：广播给全员；纯 Client：单发给 Host（ServerClientId）。
        /// </summary>
        public static void ToHostOrAll(
            NetworkManager manager, string messageName,
            in FastBufferWriter writer, NetworkDelivery delivery)
        {
            if (manager == null || string.IsNullOrEmpty(messageName))
            {
                return;
            }

            CustomMessagingManager messaging = manager.CustomMessagingManager;
            if (messaging == null)
            {
                return;
            }

            if (manager.IsServer)
            {
                messaging.SendNamedMessageToAll(messageName, writer, delivery);
            }
            else
            {
                // ServerClientId 是 const（NGO 里恒为 0），必须用类型名访问。
                messaging.SendNamedMessage(messageName, NetworkManager.ServerClientId, writer, delivery);
            }
        }
    }
}
