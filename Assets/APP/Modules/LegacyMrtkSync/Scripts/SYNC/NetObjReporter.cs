using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetObjReporter : MonoBehaviour
{
    private NetworkObject netObj;

    private void Awake()
    {
        netObj = GetComponent<NetworkObject>();
        // 删除了所有 SetParent 相关的代码！
    }

    //private void OnEnable() { if (IsHostReady()) GlobalSyncManager.Instance.HandleNetObjState(netObj, true); }
    //private void OnDisable() { if (IsHostReady()) GlobalSyncManager.Instance.HandleNetObjState(netObj, false); }

    private bool IsHostReady() { return GlobalSyncManager.Instance != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer; }
}