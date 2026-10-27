using Unity.Netcode;
using UnityEngine;

// 注意：这里继承改成了 NetworkBehaviour
public class GameServerSpawner : NetworkBehaviour
{
    [Header("把你 Assets 里的蓝色图纸拖到这里")]
    public GameObject prefabToSpawn;

    // OnNetworkSpawn 是 NGO 的原生方法，只有在网络绝对安全建立后才会执行
    public override void OnNetworkSpawn()
    {
        // 只有主机有权力刷出全网同步的物体，客机只能乖乖看着
        if (IsServer)
        {
            Debug.Log("<color=green>网络底层安全建立！主机正在下达全网生成指令！</color>");

            // 1. 生成物理模型
            GameObject go = Instantiate(prefabToSpawn);

            // 2. 赋予网络灵魂（通电全网同步）
            go.GetComponent<NetworkObject>().Spawn();
        }
    }
}