using UnityEngine;
using System.Diagnostics;
using System.IO;

namespace _syc_resource._my_scripts.GameCtrl
{
    public class FirewallManager : MonoBehaviour
    {
        // 你的游戏端口
        private const int GamePort = 7777; 
        // 防火墙规则名称（最好用英文，加上你的游戏名）
        private const string RuleName = "MyUnityGame_UDP_7777";

        void Start()
        {
            // 仅在 Windows 平台且非编辑器环境下运行
            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                // 简单检查是否可能需要添加规则（实际检测很复杂，这里建议每次启动尝试添加，或者写入注册表标记）
                // 为了不每次都弹窗，我们可以检查 PlayerPrefs
                if (PlayerPrefs.GetInt("FirewallSetup", 0) == 0)
                {
                    AddFirewallRule();
                }
            }
        }

        public void AddFirewallRule()
        {
            // netsh 命令：添加入站规则，允许 UDP 端口 7777
            string cmd = $"advfirewall firewall add rule name=\"{RuleName}\" dir=in action=allow protocol=UDP localport={GamePort}";

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "netsh";
            startInfo.Arguments = cmd;
        
            // 【关键】请求管理员权限
            startInfo.Verb = "runas"; 
        
            // 隐藏 CMD 窗口
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = true;

            try
            {
                Process process = Process.Start(startInfo);
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    UnityEngine.Debug.Log("防火墙规则添加成功！");
                    // 标记已添加，下次不再执行
                    PlayerPrefs.SetInt("FirewallSetup", 1);
                    PlayerPrefs.Save();
                }
                else
                {
                    UnityEngine.Debug.LogError("防火墙添加失败，可能是用户拒绝了权限。");
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError("无法执行防火墙命令: " + e.Message);
            }
        }
    }
}