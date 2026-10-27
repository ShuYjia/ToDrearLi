using System.Collections.Generic;
using Unity.Netcode;

namespace Bpf.Networking
{
    /// <summary>
    /// 玩家注册表：clientId → 头像实例。各端本地视图，主线程使用。
    ///
    /// 注册/反注册都发生在 NGO 回调（主线程），不需要加锁。
    /// 断线时由 Host 侧与本地 OnNetworkDespawn 双向清理。
    /// </summary>
    public static class BpfPlayerRegistry
    {
        private static readonly Dictionary<ulong, BpfPlayerAvatar> Avatars =
            new Dictionary<ulong, BpfPlayerAvatar>();

        /// <summary>有玩家头像加入（本机视角）。</summary>
        public static event System.Action<ulong> PlayerJoined;

        /// <summary>有玩家头像离开（本机视角）。</summary>
        public static event System.Action<ulong> PlayerLeft;

        public static int Count => Avatars.Count;

        public static void Register(ulong clientId, BpfPlayerAvatar avatar)
        {
            if (avatar == null)
            {
                return;
            }

            Avatars[clientId] = avatar;
            PlayerJoined?.Invoke(clientId);
        }

        public static void Unregister(ulong clientId, BpfPlayerAvatar avatar)
        {
            if (Avatars.TryGetValue(clientId, out BpfPlayerAvatar existing) && existing == avatar)
            {
                Avatars.Remove(clientId);
                PlayerLeft?.Invoke(clientId);
            }
        }

        /// <summary>某个客户端掉线：清理其头像并通知。</summary>
        public static void NotifyDisconnected(ulong clientId)
        {
            if (Avatars.Remove(clientId))
            {
                PlayerLeft?.Invoke(clientId);
            }
        }

        public static bool TryFind(ulong clientId, out BpfPlayerAvatar avatar)
        {
            return Avatars.TryGetValue(clientId, out avatar);
        }

        public static void Clear()
        {
            Avatars.Clear();
        }
    }
}
