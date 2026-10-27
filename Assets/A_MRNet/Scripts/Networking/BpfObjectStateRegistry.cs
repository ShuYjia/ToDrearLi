using System.Collections.Generic;

namespace Bpf.Networking
{
    /// <summary>
    /// Host 侧业务对象状态表（点击仲裁 + 晚加入快照）。
    ///
    /// 只在 Host 上写入：Client 的点击一律先发请求，由 Host 决定是否放行，
    /// 因此"分数只由 Host 计算"这一条由本表 + 点击拦截共同保证。
    ///
    /// 各端本地视图只读；非 Host 上本表为空，状态靠 0.5s 镜像推送。
    /// </summary>
    public static class BpfObjectStateRegistry
    {
        private static readonly Dictionary<BpfObjectId, BpfObjectState> States =
            new Dictionary<BpfObjectId, BpfObjectState>();

        public static int Count => States.Count;

        /// <summary>取当前状态（不存在视为未占用）。</summary>
        public static bool TryGet(BpfObjectId id, out BpfObjectState state)
        {
            return States.TryGetValue(id, out state);
        }

        /// <summary>Host 专用：占用/更新一个对象。</summary>
        public static void Set(BpfObjectId id, bool used, ulong ownerClientId, int value)
        {
            States[id] = new BpfObjectState
            {
                id = id,
                used = used,
                ownerClientId = ownerClientId,
                value = value,
            };
        }

        /// <summary>Host 专用：快照（晚加入的 Client 用它对齐）。</summary>
        public static List<BpfObjectState> Snapshot()
        {
            List<BpfObjectState> snapshot = new List<BpfObjectState>();
            foreach (KeyValuePair<BpfObjectId, BpfObjectState> pair in States)
            {
                snapshot.Add(pair.Value);
            }

            return snapshot;
        }

        /// <summary>把 Host 广播来的快照写进本地视图（Client 用）。</summary>
        public static void ApplySnapshot(List<BpfObjectState> snapshot)
        {
            States.Clear();
            if (snapshot == null)
            {
                return;
            }

            foreach (BpfObjectState state in snapshot)
            {
                States[state.id] = state;
            }
        }

        /// <summary>会话结束/离开房间时清理。</summary>
        public static void Clear()
        {
            States.Clear();
        }
    }
}
