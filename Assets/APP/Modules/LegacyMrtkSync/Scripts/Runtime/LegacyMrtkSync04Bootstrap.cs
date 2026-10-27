using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace BPF.LegacyMrtkSync
{
    [DisallowMultipleComponent]
    public sealed class LegacyMrtkSync04Bootstrap : MonoBehaviour
    {
        [Header("Network")]
        [SerializeField] private ushort port = 7777;
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool autoStartHost;
        [SerializeField] private bool autoStartClient;
        [SerializeField] private string clientAddress = "127.0.0.1";

        private NetworkManager networkManager;
        private UnityTransport transport;
        private ExampleNetworkDiscovery discovery;
        private AutoJoinManager autoJoinManager;

        private void Awake()
        {
            networkManager = GetComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();
            discovery = GetComponent<ExampleNetworkDiscovery>();
            autoJoinManager = GetComponent<AutoJoinManager>();

            if (transport != null)
            {
                transport.SetConnectionData(clientAddress, port, "0.0.0.0");
            }

            if (networkManager != null && networkManager.NetworkConfig != null)
            {
                networkManager.NetworkConfig.EnableSceneManagement = true;
                networkManager.NetworkConfig.ClientConnectionBufferTimeout = 10;
            }

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            if (networkManager == null || networkManager.IsListening)
            {
                return;
            }

            if (autoStartHost)
            {
                StartHost();
            }
            else if (autoStartClient)
            {
                StartClient();
            }
        }

        public void StartHost()
        {
            if (networkManager == null || networkManager.IsListening)
            {
                return;
            }

            if (discovery != null)
            {
                discovery.StartServer();
            }

            networkManager.StartHost();
        }

        public void StartClient()
        {
            if (networkManager == null || networkManager.IsListening)
            {
                return;
            }

            if (autoJoinManager != null)
            {
                autoJoinManager.StartScanning();
                return;
            }

            networkManager.StartClient();
        }
    }
}