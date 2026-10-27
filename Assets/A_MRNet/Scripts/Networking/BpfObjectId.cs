using Unity.Collections;
using Unity.Netcode;

namespace Bpf.Networking
{
    /// <summary>
    /// 业务对象的稳定标识：场景名 + 对象名。
    ///
    /// 业务侧点击分发本来就是按对象名前缀（"A.xxx" / "S.xxx" / "Exam.xxx"），
    /// 所以直接用名字做主键，不必给业务预制体加任何组件。
    /// </summary>
    public struct BpfObjectId : INetworkSerializable
    {
        public FixedString64Bytes sceneName;
        public FixedString64Bytes objectName;

        public BpfObjectId(string scene, string obj)
        {
            sceneName = new FixedString64Bytes(scene ?? string.Empty);
            objectName = new FixedString64Bytes(obj ?? string.Empty);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref sceneName);
            serializer.SerializeValue(ref objectName);
        }

        public override string ToString()
        {
            return sceneName.ToString() + "/" + objectName.ToString();
        }
    }

    /// <summary>业务对象在 Host 侧的状态（用于点击仲裁与晚加入快照）。</summary>
    public struct BpfObjectState : INetworkSerializable
    {
        public BpfObjectId id;
        public bool used;
        public ulong ownerClientId;
        public int value;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref id);
            serializer.SerializeValue(ref used);
            serializer.SerializeValue(ref ownerClientId);
            serializer.SerializeValue(ref value);
        }
    }
}
