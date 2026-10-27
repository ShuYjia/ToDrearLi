using UnityEngine;

public class SceneItemSync : MonoBehaviour
{
    public string uniqueID;
    public bool isTransformSync = false;
    public bool isActiveSync = true;

    private Vector3 lastLocalPos;
    private Quaternion lastLocalRot;

    private bool isApplyingNetworkState = false;
    private bool isRegistered = false; // 改为由管家来赋予
    private bool isQuitting = false;

    // 供管家在 Awake 时强制调用
    public void MarkAsRegistered()
    {
        isRegistered = true;
        lastLocalPos = transform.localPosition;
        lastLocalRot = transform.localRotation;
    }

    private void Update()
    {
        if (!isRegistered || !isTransformSync || isApplyingNetworkState || GlobalSyncManager.Instance == null) return;

        if (Vector3.Distance(transform.localPosition, lastLocalPos) > 0.001f ||
            Quaternion.Angle(transform.localRotation, lastLocalRot) > 0.1f)
        {
            lastLocalPos = transform.localPosition;
            lastLocalRot = transform.localRotation;
            GlobalSyncManager.Instance.RequestSetTransform(uniqueID, lastLocalPos, lastLocalRot);
        }
    }

    private void OnEnable()
    {
        if (!isRegistered || !isActiveSync || isApplyingNetworkState || isQuitting) return;
        if (GlobalSyncManager.Instance != null) GlobalSyncManager.Instance.RequestSetActive(uniqueID, true);
    }

    private void OnDisable()
    {
        if (!isRegistered || !isActiveSync || isApplyingNetworkState || isQuitting) return;
        if (GlobalSyncManager.Instance != null) GlobalSyncManager.Instance.RequestSetActive(uniqueID, false);
    }

    private void OnApplicationQuit() { isQuitting = true; }

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
        isApplyingNetworkState = false;
    }
}