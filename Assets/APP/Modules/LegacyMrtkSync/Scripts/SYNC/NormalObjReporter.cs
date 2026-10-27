using Unity.Netcode;
using UnityEngine;

public class NormalObjReporter : MonoBehaviour
{
    [HideInInspector]
    public int registryIndex = -1; // 控制中心发给我的身份证号

    [HideInInspector] public bool defaultActive;

    private void OnEnable()
    {
        if (IsHostReady() && registryIndex != -1)
        {
            // 向控制中心报告：几号普通物体亮了！
            //GlobalSyncManager.Instance.HandleNormalObjState(registryIndex, true);
        }
    }

    private void OnDisable()
    {
        if (IsHostReady() && registryIndex != -1)
        {
            // 向控制中心报告：几号普通物体灭了！
            //GlobalSyncManager.Instance.HandleNormalObjState(registryIndex, false);
        }
    }

    private bool IsHostReady()
    {
        return GlobalSyncManager.Instance != null &&
               NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening &&
               NetworkManager.Singleton.IsServer;
    }
}