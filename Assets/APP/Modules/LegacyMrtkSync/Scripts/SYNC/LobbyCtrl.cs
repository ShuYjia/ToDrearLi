using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// 全自动跳板：主机秒进游戏并持续广播等候客机
public class LobbyCtrl : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // 只有主机（建房者）才执行全自动跳转
        if (IsServer)
        {
            Debug.Log("<color=yellow>主机抵达大厅，0.5秒后自动前往最终场景...</color>");

            // 延迟0.5秒，避免 NGO 底层刚初始化完就切场景导致 C++ 报错
            Invoke(nameof(AutoJumpToGame), 0.5f);
        }
    }

    private void AutoJumpToGame()
    {
        if (!IsServer) return;

        // ⚠️ 极其重要：这里绝对不能写 StopDiscovery()！
        // 因为主机要在 MRTKsync 场景里继续等客机，必须让局域网“灯塔”一直亮着！

        Debug.Log("<color=green>主机发车！全网使用 Single 模式加载最终场景！</color>");

        // 自动跳转到最终的游戏场景
        NetworkManager.Singleton.SceneManager.LoadScene("MRTKsync", LoadSceneMode.Single);
    }
}