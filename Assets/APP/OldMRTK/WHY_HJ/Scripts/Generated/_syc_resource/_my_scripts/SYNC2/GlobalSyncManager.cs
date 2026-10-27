using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace WHY_HJ.ExportRuntime
{
    public class GlobalSyncManager : NetworkBehaviour
    {
        public static GlobalSyncManager Instance;

        private struct TransformSnapshot
        {
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;

            public TransformSnapshot(Vector3 localPosition, Quaternion localRotation)
            {
                LocalPosition = localPosition;
                LocalRotation = localRotation;
            }
        }

        private readonly Dictionary<string, SceneItemSync> sceneItems = new Dictionary<string, SceneItemSync>();
        private readonly Dictionary<string, bool> initialActiveState = new Dictionary<string, bool>();
        private readonly Dictionary<string, TransformSnapshot> initialTransformState = new Dictionary<string, TransformSnapshot>();
        private readonly Dictionary<string, bool> activeStateCache = new Dictionary<string, bool>();
        private readonly Dictionary<string, TransformSnapshot> transformStateCache = new Dictionary<string, TransformSnapshot>();

        private void Awake()
        {
            Instance = this;

            SceneItemSync[] allItems = FindObjectsByType<SceneItemSync>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (SceneItemSync item in allItems)
            {
                RegisterSceneItem(item);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterSceneItem(SceneItemSync item)
        {
            if (item == null || string.IsNullOrEmpty(item.uniqueID))
            {
                return;
            }

            if (sceneItems.TryGetValue(item.uniqueID, out SceneItemSync existingItem))
            {
                if (existingItem == item)
                {
                    item.MarkAsRegistered();
                    return;
                }

                Debug.LogError($"Duplicate SceneItemSync uniqueID detected: [{item.uniqueID}] on {item.name}.", item);
                return;
            }

            sceneItems.Add(item.uniqueID, item);
            item.MarkAsRegistered();
            CacheInitialState(item);
        }

        public void UnregisterSceneItem(SceneItemSync item)
        {
            if (item == null || string.IsNullOrEmpty(item.uniqueID))
            {
                return;
            }

            if (sceneItems.TryGetValue(item.uniqueID, out SceneItemSync existingItem) && existingItem == item)
            {
                sceneItems.Remove(item.uniqueID);
                initialActiveState.Remove(item.uniqueID);
                initialTransformState.Remove(item.uniqueID);
                activeStateCache.Remove(item.uniqueID);
                transformStateCache.Remove(item.uniqueID);
            }
        }

        private void CacheInitialState(SceneItemSync item)
        {
            if (item == null || string.IsNullOrEmpty(item.uniqueID))
            {
                return;
            }

            initialActiveState[item.uniqueID] = item.gameObject.activeSelf;
            initialTransformState[item.uniqueID] = new TransformSnapshot(item.transform.localPosition, item.transform.localRotation);
        }

        private void UpdateActiveReplayCache(string id, bool isActive)
        {
            if (initialActiveState.TryGetValue(id, out bool initialIsActive) && initialIsActive == isActive)
            {
                activeStateCache.Remove(id);
                return;
            }

            activeStateCache[id] = isActive;
        }

        private void UpdateTransformReplayCache(string id, Vector3 localPos, Quaternion localRot)
        {
            if (initialTransformState.TryGetValue(id, out TransformSnapshot initialState))
            {
                bool matchesInitialPosition = Vector3.Distance(initialState.LocalPosition, localPos) <= 0.001f;
                bool matchesInitialRotation = Quaternion.Angle(initialState.LocalRotation, localRot) <= 0.1f;
                if (matchesInitialPosition && matchesInitialRotation)
                {
                    transformStateCache.Remove(id);
                    return;
                }
            }

            transformStateCache[id] = new TransformSnapshot(localPos, localRot);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += SyncFullStateToNewClient;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= SyncFullStateToNewClient;
            }
        }

        public void RequestSetActive(string id, bool isActive)
        {
            if (!IsSpawned || !sceneItems.ContainsKey(id)) return;

            if (IsServer)
            {
                UpdateActiveReplayCache(id, isActive);
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
            if (!sceneItems.ContainsKey(id)) return;

            UpdateActiveReplayCache(id, isActive);
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
                Debug.LogWarning($"SceneItemSync active sync target was not found for id [{id}].");
            }
        }

        private void SyncFullStateToNewClient(ulong clientId)
        {
            if (!IsServer || clientId == NetworkManager.ServerClientId) return;
            try
            {
                KeyValuePair<string, bool>[] activeSnapshot = new List<KeyValuePair<string, bool>>(activeStateCache).ToArray();
                foreach (KeyValuePair<string, bool> kvp in activeSnapshot)
                {
                    ClientRpcParams rpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                    };
                    TargetSyncActiveClientRpc(kvp.Key, kvp.Value, rpcParams);
                }

                KeyValuePair<string, TransformSnapshot>[] transformSnapshot = new List<KeyValuePair<string, TransformSnapshot>>(transformStateCache).ToArray();
                foreach (KeyValuePair<string, TransformSnapshot> kvp in transformSnapshot)
                {
                    ClientRpcParams rpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                    };
                    TargetSyncTransformClientRpc(kvp.Key, kvp.Value.LocalPosition, kvp.Value.LocalRotation, rpcParams);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GlobalSyncManager failed to sync full state to client {clientId}: {ex}");
            }
        }

        [ClientRpc]
        private void TargetSyncActiveClientRpc(string id, bool isActive, ClientRpcParams clientRpcParams = default)
        {
            if (sceneItems.TryGetValue(id, out SceneItemSync item))
            {
                item.ApplyNetworkActive(isActive);
            }
        }

        [ClientRpc]
        private void TargetSyncTransformClientRpc(string id, Vector3 localPos, Quaternion localRot, ClientRpcParams clientRpcParams = default)
        {
            if (sceneItems.TryGetValue(id, out SceneItemSync item))
            {
                item.ApplyNetworkTransform(localPos, localRot);
            }
        }

        public void RequestSetTransform(string id, Vector3 localPos, Quaternion localRot)
        {
            if (!IsSpawned || !sceneItems.ContainsKey(id)) return;

            if (IsServer)
            {
                UpdateTransformReplayCache(id, localPos, localRot);
                UpdateTransformClientRpc(id, localPos, localRot);
            }
            else
            {
                RequestTransformServerRpc(id, localPos, localRot);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestTransformServerRpc(string id, Vector3 localPos, Quaternion localRot)
        {
            if (!sceneItems.ContainsKey(id)) return;

            UpdateTransformReplayCache(id, localPos, localRot);
            UpdateTransformClientRpc(id, localPos, localRot);
        }

        [ClientRpc]
        private void UpdateTransformClientRpc(string id, Vector3 localPos, Quaternion localRot)
        {
            if (sceneItems.TryGetValue(id, out SceneItemSync item))
            {
                item.ApplyNetworkTransform(localPos, localRot);
            }
        }
    }

}
