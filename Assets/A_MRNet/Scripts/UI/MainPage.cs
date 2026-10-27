using UnityEngine;
using UnityEngine.UI;
using MixedReality.Toolkit.UX;
public enum GameSubPageId
{
    SoloPractice,
    Coop,
}

public class MainPage : UIPanel
{
    public PressableButton btnSolo;
    public PressableButton btnSolo2;
    public PressableButton btnSolo3;
    public PressableButton btnCoop;
    public PressableButton btnSoloTab;

    public GameObject soloRoot;
    public GameObject coopRoot;

    public GameSubPageId defaultSub = GameSubPageId.SoloPractice;
    public string soloScene = TexCore_LoadScene.BusinessSceneName;

    private GameSubPageId? _currentSub;

    private void Awake()
    {
        if (btnSolo != null) btnSolo.OnClicked.AddListener(EnterSoloScene);
        if (btnCoop != null) btnCoop.OnClicked.AddListener(() => ShowSub(GameSubPageId.Coop));

        if (soloRoot != null) soloRoot.SetActive(false);
        if (coopRoot != null) coopRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (btnSoloTab != null)
        {
            btnSoloTab.OnClicked.RemoveListener(ShowSoloPractice);
            btnSoloTab.OnClicked.AddListener(ShowSoloPractice);
        }

        // 回到游戏页时保证显示一个子页
        ShowSub(defaultSub);
    }

    private void ShowSoloPractice()
    {
        ShowSub(GameSubPageId.SoloPractice);
    }

    public void ShowSub(GameSubPageId id)
    {
        if (_currentSub.HasValue && _currentSub.Value.Equals(id))
            return;

        if (soloRoot != null) soloRoot.SetActive(id == GameSubPageId.SoloPractice);
        if (coopRoot != null) coopRoot.SetActive(id == GameSubPageId.Coop);

        _currentSub = id;

        if (id == GameSubPageId.Coop)
        {
            EnterCoopLobby();
        }
        else if (Bpf.Networking.BpfLanLobbyPanel.ActivePanel != null)
        {
            // 切出协同子页：收起联机面板（切回时再显示）
            Bpf.Networking.BpfLanLobbyPanel.ActivePanel.SetVisible(false);
        }
    }

    /// <summary>
    /// 多人子页首次打开：确保联机网络根存在，并搭出大厅面板。
    /// 面板是自带 DontDestroyOnLoad 世界空间画布的运行期对象（阶段 5 起），
    /// 不依赖 coopRoot 的 Canvas——业务场景切换后仍存活，校准因此可用。
    /// </summary>
    private void EnterCoopLobby()
    {
        Bpf.Networking.BpfNetworkBootstrap.EnsureRuntimeRoot();
        Bpf.Networking.BpfLanLobbyPanel panel = Bpf.Networking.BpfLanLobbyPanel.EnsureFor();
        if (panel != null)
        {
            panel.SetVisible(true);
        }
    }

    private void EnterSoloScene()
    {
        if (TexCore_LoadScene.instance == null)
        {
            ShowSub(GameSubPageId.SoloPractice);
            return;
        }

        string targetScene = string.IsNullOrEmpty(soloScene)
            ? TexCore_LoadScene.BusinessSceneName
            : soloScene;

        TexCore_LoadScene.instance.OnlyLoad(targetScene);
    }

    public override void OnShow(object args = null)
    {
        // 刷新数据/播放动画等
    }
    public override void OnHide()
    {
        // 停止协程/回收等
    }


}

