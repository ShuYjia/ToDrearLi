using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Bpf.Diagnostics
{
    /// <summary>
    /// World-space development log panel. It is created only for development
    /// players on a headset, because the IMGUI overlay of
    /// <see cref="BpfDiagnosticsOverlay"/> is not readable inside the headset.
    /// </summary>
    internal sealed class BpfDiagnosticsDevicePanel
    {
        private const string RootName = "Bpf Diagnostics Device Panel";
        private const int VisibleRows = 20;
        private const float DistanceMeters = 1.6f;
        private const float RefreshIntervalSeconds = 0.4f;

        private static readonly Vector2 PanelSize = new Vector2(1400f, 900f);
        private const float PanelScale = 0.0012f;

        private readonly BpfDiagnosticsPanel panel;
        private GameObject root;
        private Text body;
        private Camera camera;
        private float refreshTimer;
        private int scrollBack;

        public BpfDiagnosticsDevicePanel(BpfDiagnosticsPanel panel)
        {
            this.panel = panel;
        }

        public bool IsVisible
        {
            get { return root != null && root.activeSelf; }
        }

        /// <summary>Creates the panel on first use and shows or hides it.</summary>
        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                if (root != null)
                {
                    root.SetActive(false);
                }

                return;
            }

            if (root == null)
            {
                Create();
            }

            root.SetActive(true);
            scrollBack = 0;
            Refresh(true);
        }

        /// <summary>Scrolls the record window, positive direction moves to newer rows.</summary>
        public void Scroll(int direction)
        {
            if (root == null)
            {
                return;
            }

            int count = panel.GetRecords().Count;
            int maximum = Mathf.Max(0, count - VisibleRows);
            scrollBack = Mathf.Clamp(scrollBack - direction, 0, maximum);
            Refresh(true);
        }

        /// <summary>Keeps the panel in front of the camera and refreshes the text.</summary>
        public void Tick(Camera activeCamera, float deltaTime)
        {
            if (root == null || !root.activeSelf || activeCamera == null)
            {
                return;
            }

            camera = activeCamera;
            Transform cameraTransform = camera.transform;
            Vector3 forward = cameraTransform.forward;
            root.transform.position = cameraTransform.position + (forward * DistanceMeters);
            root.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            refreshTimer += deltaTime;
            if (refreshTimer >= RefreshIntervalSeconds)
            {
                refreshTimer = 0f;
                Refresh(false);
            }
        }

        public void Destroy()
        {
            if (root == null)
            {
                return;
            }

            Object.Destroy(root);
            root = null;
            body = null;
        }

        private void Create()
        {
            root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(Image));
            Object.DontDestroyOnLoad(root);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = short.MaxValue;

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = PanelSize;
            rootRect.localScale = Vector3.one * PanelScale;

            Image background = root.GetComponent<Image>();
            background.color = new Color(0.04f, 0.05f, 0.07f, 0.88f);

            GameObject bodyObject = new GameObject("Body", typeof(RectTransform), typeof(Text));
            bodyObject.transform.SetParent(root.transform, false);
            body = bodyObject.GetComponent<Text>();
            body.font = ResolveFont();
            body.fontSize = 20;
            body.alignment = TextAnchor.UpperLeft;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            body.supportRichText = false;
            body.color = new Color(0.9f, 0.94f, 0.98f, 1f);

            RectTransform bodyRect = body.rectTransform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(24f, 24f);
            bodyRect.offsetMax = new Vector2(-24f, -24f);
        }

        private void Refresh(bool force)
        {
            if (body == null)
            {
                return;
            }

            List<BpfLogRecord> records = panel.GetRecords();
            int end = Mathf.Clamp(records.Count - scrollBack, 0, records.Count);
            int start = Mathf.Max(0, end - VisibleRows);

            StringBuilder builder = new StringBuilder(4096);
            builder.Append("BPF 诊断面板  level>=").Append(panel.MinimumLevel.ToTag());
            builder.Append("  category=").Append(string.IsNullOrEmpty(panel.Category) ? "ALL" : panel.Category);
            builder.Append("  error=").Append(panel.ErrorCount);
            builder.Append("  warning=").Append(panel.WarningCount);
            builder.Append("  ").Append(panel.IsPaused ? "PAUSED" : "LIVE");
            builder.Append('\n');
            builder.Append("菜单键 关闭 | 右手摇杆 上下 滚动 | A 级别 | B 清空 | 左手 X 暂停");
            builder.Append('\n');
            builder.Append("显示 ").Append(start).Append('-').Append(end).Append(" / ").Append(records.Count);
            builder.Append("  ").Append(panel.LogFilePath);
            builder.Append('\n');
            builder.Append("------------------------------------------------------------\n");

            if (records.Count == 0)
            {
                builder.Append("(暂无日志)");
            }
            else
            {
                for (int i = start; i < end; i++)
                {
                    builder.Append(records[i].ToConsoleLine());
                    builder.Append('\n');
                }
            }

            body.text = builder.ToString();
            if (force)
            {
                refreshTimer = 0f;
            }
        }

        private static Font ResolveFont()
        {
            string[] candidates =
            {
                "Noto Sans CJK SC",
                "Source Han Sans CN",
                "Droid Sans Fallback",
                "Microsoft YaHei",
                "SimHei",
                "Arial",
            };

            try
            {
                Font osFont = Font.CreateDynamicFontFromOSFont(candidates, 20);
                if (osFont != null)
                {
                    return osFont;
                }
            }
            catch (System.Exception)
            {
                // Fall through to the built-in font below.
            }

            try
            {
                return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch (System.Exception)
            {
                return Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        /// <summary>Camera used for billboarding, exposed for diagnostics.</summary>
        public Camera Camera
        {
            get { return camera; }
        }
    }
}
