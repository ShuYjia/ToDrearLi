using UnityEngine;

namespace ChangJing_8.ExportRuntime
{
    public class SceneItemSync : MonoBehaviour
    {
        public string uniqueID;
        public bool isTransformSync = false;
        public bool isActiveSync = true;

        private Vector3 lastLocalPos;
        private Quaternion lastLocalRot;
        private bool lastReportedActiveSelf;

        private bool isApplyingNetworkState = false;
        private bool isRegistered = false;
        private bool isQuitting = false;

        public void MarkAsRegistered()
        {
            isRegistered = true;
            lastLocalPos = transform.localPosition;
            lastLocalRot = transform.localRotation;
            lastReportedActiveSelf = gameObject.activeSelf;
        }

        private void Awake()
        {
            TryRegisterWithManager();
        }

        private void Start()
        {
            TryRegisterWithManager();
        }

        private void Update()
        {
            if (!isRegistered || GlobalSyncManager.Instance == null) return;

            if (isTransformSync && !isApplyingNetworkState)
            {
                if (Vector3.Distance(transform.localPosition, lastLocalPos) > 0.001f ||
                    Quaternion.Angle(transform.localRotation, lastLocalRot) > 0.1f)
                {
                    lastLocalPos = transform.localPosition;
                    lastLocalRot = transform.localRotation;
                    GlobalSyncManager.Instance.RequestSetTransform(uniqueID, lastLocalPos, lastLocalRot);
                }
            }
        }

        private void OnEnable()
        {
            TryRegisterWithManager();
            if (!isRegistered || !isActiveSync || isApplyingNetworkState || isQuitting) return;
            if (GlobalSyncManager.Instance == null) return;

            if (gameObject.activeSelf != lastReportedActiveSelf)
            {
                lastReportedActiveSelf = true;
                GlobalSyncManager.Instance.RequestSetActive(uniqueID, true);
            }
        }

        private void OnDisable()
        {
            if (!isRegistered || !isActiveSync || isApplyingNetworkState || isQuitting) return;
            if (GlobalSyncManager.Instance == null) return;

            if (!gameObject.activeSelf)
            {
                lastReportedActiveSelf = false;
                GlobalSyncManager.Instance.RequestSetActive(uniqueID, false);
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            if (!isRegistered || isQuitting) return;
            if (GlobalSyncManager.Instance != null)
            {
                GlobalSyncManager.Instance.UnregisterSceneItem(this);
            }
            isRegistered = false;
        }

        private void TryRegisterWithManager()
        {
            if (isRegistered || string.IsNullOrEmpty(uniqueID)) return;
            if (GlobalSyncManager.Instance != null)
            {
                GlobalSyncManager.Instance.RegisterSceneItem(this);
            }
        }

        public void ApplyNetworkTransform(Vector3 localPos, Quaternion localRot)
        {
            isApplyingNetworkState = true;
            transform.localPosition = localPos;
            transform.localRotation = localRot;
            lastLocalPos = localPos;
            lastLocalRot = localRot;
            isApplyingNetworkState = false;
        }

        public void ApplyNetworkActive(bool isActive)
        {
            if (gameObject.activeSelf == isActive) return;

            isApplyingNetworkState = true;
            gameObject.SetActive(isActive);
            lastReportedActiveSelf = isActive;
            isApplyingNetworkState = false;
        }
    }

}
