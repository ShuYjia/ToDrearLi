using UnityEngine;
using UnityEngine.SceneManagement; // 别忘了引入场景管理命名空间

// 极简版 GameManager（恢复了开局自动跳转 StartMain 的能力）
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    void Awake()
    {
        // 标准的单例模式 + 跨场景不销毁
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 【恢复这里】游戏刚启动时，自动跳转到输入 IP 的主界面
        // 加了一个安全判断，防止如果你已经在 StartMain 场景里直接点运行会导致无限死循环重载
        if (SceneManager.GetActiveScene().name != "UDPLobby")
        {
            SceneManager.LoadScene("UDPLobby");
        }
    }
}