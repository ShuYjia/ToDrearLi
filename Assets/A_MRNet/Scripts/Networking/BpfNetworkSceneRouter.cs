using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bpf.Networking
{
    /// <summary>
    /// 网络场景路由：把 TexCore 的场景切换收口到 NGO 场景管理。
    ///
    /// 挂接方式（最小侵入）：
    ///   TexCore_LoadScene.SceneLoadInterceptor = HandleSceneLoad
    /// 单机（无会话）时拦截器返回 false，TexCore 行为与改造前逐字节一致；
    /// 会话进行中：
    ///   Host   → NetworkManager.SceneManager.LoadScene（NGO 驱动全员切换）；
    ///   Client → 本地什么都不做，等 Host 推送。
    ///
    /// 网络根是运行期生成的 DontDestroyOnLoad 对象，本组件随之常驻，
    /// 因此纯单机玩家（从未打开多人页）根本不会安装拦截器。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfNetworkSceneRouter : MonoBehaviour
    {
        private const string LogTag = "Bpf|SceneRouter";

        private BpfLanNetworkSettings settings;
        private BpfNetworkBootstrap bootstrap;

        public void Initialize(BpfLanNetworkSettings networkSettings, BpfNetworkBootstrap owner)
        {
            settings = networkSettings;
            bootstrap = owner;

            // 安装拦截器（幂等：重复赋值同一委托无害）。
            TexCore_LoadScene.SceneLoadInterceptor = HandleSceneLoad;
            BpfNetLog.Info(LogTag, "interceptor_installed", "场景切换拦截器已安装");
        }

        private void OnDestroy()
        {
            if (TexCore_LoadScene.SceneLoadInterceptor != null
                && TexCore_LoadScene.SceneLoadInterceptor.Equals((Func<string, bool>)HandleSceneLoad))
            {
                TexCore_LoadScene.SceneLoadInterceptor = null;
            }
        }

        /// <summary>
        /// TexCore 场景切换收口。返回 true = 网络层已接管，TexCore 不要本地加载。
        /// </summary>
        private bool HandleSceneLoad(string sceneName)
        {
            BpfNetworkBootstrap boot = bootstrap != null ? bootstrap : BpfNetworkBootstrap.Instance;
            if (boot == null || boot.RoomService == null || !boot.RoomService.IsInRoom)
            {
                return false; // 单机/无会话：不拦截
            }

            string resolved = TexCore_LoadScene.ResolveSceneName(sceneName);

            if (boot.NetworkManager == null || !boot.NetworkManager.IsListening)
            {
                return false;
            }

            if (boot.NetworkManager.IsHost)
            {
                if (boot.NetworkManager.SceneManager != null)
                {
                    BpfNetLog.Info(LogTag, "host_load_scene",
                        "Host 驱动场景切换：" + resolved);
                    boot.NetworkManager.SceneManager.LoadScene(resolved, LoadSceneMode.Single);
                }
            }
            else
            {
                BpfNetLog.Info(LogTag, "client_wait_scene",
                    "Client 等待 Host 推送场景：" + resolved);
            }

            return true;
        }

        /// <summary>
        /// 大厅"开始业务场景"按钮：仅 Host 可点，走 NGO 场景管理。
        /// </summary>
        public void RequestStartBusinessScene()
        {
            BpfNetworkBootstrap boot = bootstrap != null ? bootstrap : BpfNetworkBootstrap.Instance;
            if (boot == null || boot.RoomService == null || !boot.RoomService.IsInRoom)
            {
                BpfNetLog.Warning(LogTag, "start_denied_no_room", "不在房间内，无法开始");
                return;
            }

            if (boot.NetworkManager == null || !boot.NetworkManager.IsHost)
            {
                BpfNetLog.Warning(LogTag, "start_denied_not_host", "只有房主可以开始业务场景");
                return;
            }

            string target = settings != null && !string.IsNullOrEmpty(settings.businessSceneName)
                ? settings.businessSceneName
                : TexCore_LoadScene.BusinessSceneName;

            if (boot.NetworkManager.SceneManager != null)
            {
                BpfNetLog.Info(LogTag, "host_start_business", "Host 开始业务场景：" + target);
                boot.NetworkManager.SceneManager.LoadScene(target, LoadSceneMode.Single);
            }
        }
    }
}
