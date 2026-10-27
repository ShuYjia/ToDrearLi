using UnityEngine;

public abstract class UIPanel : MonoBehaviour
{
    // 可在子类覆盖做动画/刷新数据等
    public virtual void OnShow(object args = null) { }
    public virtual void OnHide() { }
}
