using Unity.Netcode;
using UnityEngine;

namespace Bpf.Networking
{
    /// <summary>
    /// 命名消息集中注册与分发点。
    ///
    /// 只在 NetworkManager 完成 StartHost/StartClient 之后再注册——
    /// CustomMessagingManager 随会话创建/销毁，早注册会丢。
    /// 会话停止后自动复位，下次会话重新注册。
    /// </summary>
    [DisallowMultipleComponent]
    public class BpfNetMessageRouter : MonoBehaviour
    {
        private const string LogTag = "Bpf|MessageRouter";

        private bool registered;

        private void Update()
        {
            NetworkManager manager = ResolveManager();
            if (manager == null)
            {
                return;
            }

            if (!manager.IsListening)
            {
                registered = false;
                return;
            }

            if (registered)
            {
                return;
            }

            CustomMessagingManager messaging = manager.CustomMessagingManager;
            if (messaging == null)
            {
                return;
            }

            messaging.RegisterNamedMessageHandler(BpfNetMessage.PlayerPose, BpfPlayerPoseSync.HandlePoseMessage);
            messaging.RegisterNamedMessageHandler(
                BpfNetMessage.CalibrationReport, BpfSessionCalibration.HandleCalibrationReport);
            messaging.RegisterNamedMessageHandler(
                BpfNetMessage.CalibrationComplete, BpfSessionCalibration.HandleCalibrationResult);
            messaging.RegisterNamedMessageHandler(
                BpfNetMessage.InteractionRequest, BpfTexCoreNetworkBridge.HandleInteractionRequest);
            messaging.RegisterNamedMessageHandler(
                BpfNetMessage.TopicState, BpfTexCoreNetworkBridge.HandleTopicState);
            messaging.RegisterNamedMessageHandler(
                BpfNetMessage.ObjectSnapshot, BpfTexCoreNetworkBridge.HandleObjectSnapshot);
            messaging.RegisterNamedMessageHandler(
                BpfNetMessage.InteractiveRequest, BpfInteractiveObject.HandleInteractiveRequest);
            messaging.RegisterNamedMessageHandler(
                BpfNetMessage.InteractiveResult, BpfInteractiveObject.HandleInteractiveResult);
            messaging.RegisterNamedMessageHandler(
                BpfNetMessage.InteractivePose, BpfInteractiveObject.HandleInteractivePose);
            messaging.RegisterNamedMessageHandler(
                BpfNetMessage.InteractiveSnapshot, BpfInteractiveRegistry.HandleInteractiveSnapshot);
            messaging.RegisterNamedMessageHandler(
                BpfNetMessage.AvEvent, BpfSessionAVSync.HandleAvEvent);

            registered = true;
            BpfNetLog.Info(LogTag, "handlers_registered",
                "命名消息处理器已注册（isServer=" + manager.IsServer + "）");
        }

        private static NetworkManager ResolveManager()
        {
            if (BpfNetworkBootstrap.Exists)
            {
                return BpfNetworkBootstrap.Instance.NetworkManager;
            }

            return NetworkManager.Singleton;
        }
    }
}
