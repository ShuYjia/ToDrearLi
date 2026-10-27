using MixedReality.Toolkit.UX;
using Bpf.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

public enum UIPage
{
    Home,
    Rank,
    Study,
    UserCenter, 
    Bag, 
    Setting,
}

public class UIManager : MonoBehaviour
{
    [System.Serializable]
    public class PageEntry
    {
        public UIPage page;
        public GameObject root; // 对应Panel根节点
        public PressableButton tabButton;
    }

    [Header("Pages")]
    public UIPage defaultPage = UIPage.Home;
    public List<PageEntry> pages = new List<PageEntry>();

    private readonly Dictionary<UIPage, GameObject> _map = new();
    private readonly Stack<UIPage> _history = new();

    private UIPage? _current = null;

    private void Awake()
    {
        _map.Clear();
        foreach (var e in pages)
        {
            if (e.root == null) continue;
            _map[e.page] = e.root;


            if (e.tabButton != null)
            {
                var captured = e.page;
                e.tabButton.OnClicked.AddListener(() => Show(captured));
            }

        }

        // 启动先全隐藏
        foreach (var kv in _map) kv.Value.SetActive(false);
    }

    private void Start()
    {
        Show(defaultPage, pushHistory: false);
    }

    public void Show(UIPage page, object args = null, bool pushHistory = true)
    {
        if (!_map.TryGetValue(page, out var target) || target == null)
        {
            BpfLog.Error(BpfLogCategory.UI, "ui_manager.show", "UIManager: page not found: " + page);
            return;
        }

        if (_current.HasValue && _current.Value.Equals(page))
            return;

        // 记录历史
        if (pushHistory && _current.HasValue)
            _history.Push(_current.Value);

        // 隐藏当前
        if (_current.HasValue && _map.TryGetValue(_current.Value, out var curGo) && curGo != null)
        {
            var curPanel = curGo.GetComponent<UIPanel>();
            curPanel?.OnHide();
            curGo.SetActive(false);
        }

        // 显示目标
        _current = page;
        target.SetActive(true);
        var panel = target.GetComponent<UIPanel>();
        panel?.OnShow(args);
    }

    public void Back()
    {
        if (_history.Count == 0) return;
        var prev = _history.Pop();
        Show(prev, pushHistory: false);
    }

    // 如果你想做“弹窗叠加不隐藏底层”，可以用这个（可选）
    public void ShowOverlay(UIPage page, object args = null)
    {
        if (!_map.TryGetValue(page, out var target) || target == null) return;
        target.SetActive(true);
        target.GetComponent<UIPanel>()?.OnShow(args);
    }

    public void HideOverlay(UIPage page)
    {
        if (!_map.TryGetValue(page, out var target) || target == null) return;
        target.GetComponent<UIPanel>()?.OnHide();
        target.SetActive(false);
    }
}
