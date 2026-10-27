using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GlobalSyncManager : NetworkBehaviour
{
    public static GlobalSyncManager Instance;

    // 户口本字典：记录场景里所有带身份证的物体
    private Dictionary<string, SceneItemSync> sceneItems = new Dictionary<string, SceneItemSync>();
    private Dictionary<string, bool> activeStateCache = new Dictionary<string, bool>();

    private void Awake()
    {
        Instance = this;

        // 💥 核心修复：管家主动出击！找出场景里所有的 SceneItemSync，包括处于隐藏状态(Inactive)的！
        SceneItemSync[] allItems = FindObjectsByType<SceneItemSync>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allItems)
        {
            if (!string.IsNullOrEmpty(item.uniqueID))
            {
                if (!sceneItems.ContainsKey(item.uniqueID))
                {
                    sceneItems.Add(item.uniqueID, item);
                    item.MarkAsRegistered(); // 告诉小弟：你已经被录入了
                }
                else
                {
                    Debug.LogError($"❌ 致命错误：发现重复的身份证号 [{item.uniqueID}]！请检查是否有同名层级！");
                }
            }
        }
        Debug.Log($"✅ 全局管家初始化完毕，成功强行收编了 {sceneItems.Count} 个场景物体！");
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) NetworkManager.Singleton.OnClientConnectedCallback += SyncFullStateToNewClient;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientConnectedCallback -= SyncFullStateToNewClient;
    }

    // ================== Active 自动显隐路由 ==================
    public void RequestSetActive(string id, bool isActive)
    {
        if (!IsSpawned) return;

        if (IsServer)
        {
            activeStateCache[id] = isActive;
            UpdateActiveClientRpc(id, isActive);
        }
        else
        {
            RequestActiveServerRpc(id, isActive);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestActiveServerRpc(string id, bool isActive)
    {
        activeStateCache[id] = isActive;
        UpdateActiveClientRpc(id, isActive);
    }

    [ClientRpc]
    private void UpdateActiveClientRpc(string id, bool isActive)
    {
        if (sceneItems.TryGetValue(id, out SceneItemSync item))
        {
            item.ApplyNetworkActive(isActive);
        }
        else
        {
            // 🔍 侦测雷达：精准爆出哪个物体同步失败！
            Debug.LogWarning($"⚠️ 同步失败：管家找不到身份证为 [{id}] 的物体！是不是改名字或者删掉了？");
        }
    }

    // ================== 中途加入玩家的全量同步补发 ==================
    private void SyncFullStateToNewClient(ulong clientId)
    {
        if (!IsServer || clientId == NetworkManager.ServerClientId) return;
        foreach (var kvp in activeStateCache)
        {
            ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } } };
            TargetSyncActiveClientRpc(kvp.Key, kvp.Value, rpcParams);
        }
    }

    [ClientRpc]
    private void TargetSyncActiveClientRpc(string id, bool isActive, ClientRpcParams clientRpcParams = default)
    {
        if (sceneItems.TryGetValue(id, out SceneItemSync item)) item.ApplyNetworkActive(isActive);
    }

    // ================== Transform 自动坐标路由 ==================
    public void RequestSetTransform(string id, Vector3 localPos, Quaternion localRot)
    {
        if (!IsSpawned) return;
        if (IsServer) UpdateTransformClientRpc(id, localPos, localRot);
        else RequestTransformServerRpc(id, localPos, localRot);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestTransformServerRpc(string id, Vector3 localPos, Quaternion localRot) => UpdateTransformClientRpc(id, localPos, localRot);

    [ClientRpc]
    private void UpdateTransformClientRpc(string id, Vector3 localPos, Quaternion localRot)
    {
        if (sceneItems.TryGetValue(id, out SceneItemSync item)) item.ApplyNetworkTransform(localPos, localRot);
    }
}