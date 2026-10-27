using Unity.Netcode;
using UnityEngine;

public class MRTKNetTransform : NetworkBehaviour
{
    // 允许抓取者（拥有者）修改坐标
    private NetworkVariable<Vector3> netPos = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<Quaternion> netRot = new NetworkVariable<Quaternion>(writePerm: NetworkVariableWritePermission.Owner);

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        // 别人生成的物体，本地强行关闭物理，防止物理引擎打架导致抽搐
        if (!IsOwner && rb != null) rb.isKinematic = true;
    }

    public override void OnGainedOwnership()
    {
        // 当你伸手抓住它（获得权限），恢复物理
        if (rb != null) rb.isKinematic = false;
    }

    public override void OnLostOwnership()
    {
        // 松手或被别人抢走，关闭物理
        if (rb != null) rb.isKinematic = true;
    }

    private void Update()
    {
        if (!IsSpawned) return;

        if (IsOwner)
        {
            // 我抓着它，向网络实时发送我的绝对世界坐标（无视父子层级）
            netPos.Value = transform.position;
            netRot.Value = transform.rotation;
        }
        else
        {
            // 别人抓着，我平滑插值跟随
            transform.position = Vector3.Lerp(transform.position, netPos.Value, Time.deltaTime * 15f);
            transform.rotation = Quaternion.Slerp(transform.rotation, netRot.Value, Time.deltaTime * 15f);
        }
    }
}