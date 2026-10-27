using UnityEngine;

public class KeepRigAlive : MonoBehaviour
{
    void Awake()
    {
        // 让 HoloLens 的头显摄像机跨场景永不销毁！
        // 这样无论你怎么用 Single 模式切场景，物理原点永远锁定在游戏刚打开的那一刻！
        DontDestroyOnLoad(gameObject);
    }
}