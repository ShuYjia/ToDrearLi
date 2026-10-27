using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Bpf.Diagnostics
{
    /// <summary>
    /// Development-only diagnostics overlay. In the Editor it renders an IMGUI
    /// window toggled with <see cref="BpfLogOptions.OverlayToggleKey"/>; on a
    /// headset it renders a world-space panel toggled with the left controller
    /// menu button. A release player never creates the panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BpfDiagnosticsOverlay : MonoBehaviour
    {
        private const int WindowId = 0x4250464C;
        private const int MaxDrawnRows = 200;
        private const float ScrollRepeatSeconds = 0.2f;
        private const float ScrollDeadZone = 0.5f;

        private static BpfDiagnosticsOverlay instance;

        private readonly BpfDiagnosticsPanel panel = new BpfDiagnosticsPanel();
        private readonly List<InputAction> actions = new List<InputAction>();

        private InputAction toggleKeyAction;
        private InputAction toggleControllerAction;
        private InputAction scrollAction;
        private InputAction levelAction;
        private InputAction clearAction;
        private InputAction pauseAction;

        private Rect windowRect = new Rect(24f, 24f, 820f, 560f);
        private Vector2 recordScroll;
        private Vector2 categoryScroll;
        private string keywordDraft = string.Empty;
        private string exportMessage = string.Empty;
        private GUIStyle recordStyle;
        private bool showFilePath = true;
        private bool visible;
        private float scrollTimer;
        private BpfDiagnosticsDevicePanel devicePanel;

        /// <summary>Live overlay instance, or null when it was never created.</summary>
        public static BpfDiagnosticsOverlay Instance
        {
            get { return instance; }
        }

        public static bool IsVisible
        {
            get { return instance != null && instance.visible; }
        }

        /// <summary>Shows or hides the overlay. Used by tests and hotkeys.</summary>
        public static void SetVisible(bool value)
        {
            if (instance != null)
            {
                instance.visible = value;
            }
        }

        public static void Toggle()
        {
            if (instance != null)
            {
                instance.visible = !instance.visible;
            }
        }

        /// <summary>Filter and export state, exposed for tests and editors.</summary>
        public BpfDiagnosticsPanel Panel
        {
            get { return panel; }
        }

        private void Awake()
        {
            if (!BpfLogOptions.IsDevelopmentBuild)
            {
                Destroy(this);
                return;
            }

            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            CreateActions();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            if (devicePanel != null)
            {
                devicePanel.Destroy();
                devicePanel = null;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                actions[i].Disable();
                actions[i].Dispose();
            }

            actions.Clear();
        }

        private void Update()
        {
            if (WasPressed(toggleKeyAction) || WasPressed(toggleControllerAction))
            {
                visible = !visible;
            }

            if (UseDevicePanel())
            {
                UpdateDevicePanel();
                return;
            }

            if (devicePanel != null && devicePanel.IsVisible)
            {
                devicePanel.SetVisible(false);
            }
        }

        private void UpdateDevicePanel()
        {
            if (devicePanel == null)
            {
                devicePanel = new BpfDiagnosticsDevicePanel(panel);
            }

            if (devicePanel.IsVisible != visible)
            {
                devicePanel.SetVisible(visible);
            }

            if (!visible)
            {
                return;
            }

            HandleAxisScroll(scrollAction != null ? scrollAction.ReadValue<Vector2>().y : 0f);

            if (WasPressed(levelAction))
            {
                panel.CycleLevelFilter(1);
            }

            if (WasPressed(clearAction))
            {
                panel.Clear();
            }

            if (WasPressed(pauseAction))
            {
                panel.TogglePause();
            }

            devicePanel.Tick(Camera.main, Time.unscaledDeltaTime);
        }

        private void HandleAxisScroll(float axis)
        {
            scrollTimer -= Time.unscaledDeltaTime;
            if (Mathf.Abs(axis) < ScrollDeadZone)
            {
                scrollTimer = 0f;
                return;
            }

            if (scrollTimer > 0f)
            {
                return;
            }

            scrollTimer = ScrollRepeatSeconds;
            if (devicePanel != null)
            {
                devicePanel.Scroll(axis > 0f ? -1 : 1);
            }
        }

        private void OnGUI()
        {
            if (!visible || UseDevicePanel())
            {
                return;
            }

            windowRect = GUILayout.Window(WindowId, windowRect, DrawWindow, "BPF Diagnostics");
        }

        private void DrawWindow(int windowId)
        {
            if (recordStyle == null)
            {
                recordStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                    richText = false,
                    fontSize = 11,
                };
            }

            DrawLevelFilter();
            DrawCategoryFilter();
            DrawKeywordFilter();
            DrawActions();
            DrawCounters();
            DrawRecords();
            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 22f));
        }

        private void DrawLevelFilter()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Level", GUILayout.Width(52f));
            BpfLogLevel[] levels = BpfLogLevelExtensions.All;
            for (int i = 0; i < levels.Length; i++)
            {
                bool selected = panel.MinimumLevel == levels[i];
                Color previous = GUI.backgroundColor;
                if (selected)
                {
                    GUI.backgroundColor = new Color(0.25f, 0.55f, 0.95f);
                }

                if (GUILayout.Button(levels[i].ToTag(), GUILayout.Width(78f)))
                {
                    panel.SetLevelFilter(levels[i]);
                }

                GUI.backgroundColor = previous;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawCategoryFilter()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Category", GUILayout.Width(52f));
            categoryScroll = GUILayout.BeginScrollView(categoryScroll, GUILayout.Height(30f));
            GUILayout.BeginHorizontal();
            string[] categories = BpfDiagnosticsPanel.CategoryOptions;
            for (int i = 0; i < categories.Length; i++)
            {
                string category = categories[i];
                bool selected = string.Equals(panel.Category, category, System.StringComparison.OrdinalIgnoreCase);
                Color previous = GUI.backgroundColor;
                if (selected)
                {
                    GUI.backgroundColor = new Color(0.25f, 0.55f, 0.95f);
                }

                string label = string.IsNullOrEmpty(category) ? "All" : category;
                if (GUILayout.Button(label, GUILayout.Width(110f)))
                {
                    panel.SetCategory(category);
                }

                GUI.backgroundColor = previous;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
        }

        private void DrawKeywordFilter()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Keyword", GUILayout.Width(52f));
            keywordDraft = GUILayout.TextField(keywordDraft ?? string.Empty, GUILayout.Width(260f));
            if (!string.Equals(keywordDraft, panel.Keyword, System.StringComparison.Ordinal))
            {
                panel.SetKeyword(keywordDraft);
            }

            if (GUILayout.Button("Reset", GUILayout.Width(64f)))
            {
                keywordDraft = string.Empty;
                panel.SetKeyword(string.Empty);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawActions()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(panel.IsPaused ? "Resume" : "Pause", GUILayout.Width(84f)))
            {
                panel.TogglePause();
            }

            if (GUILayout.Button("Clear", GUILayout.Width(84f)))
            {
                panel.Clear();
                exportMessage = string.Empty;
            }

            if (GUILayout.Button("Export", GUILayout.Width(84f)))
            {
                string path = panel.ExportRecent();
                exportMessage = path == null ? "export: nothing to export" : "export: " + path;
            }

            showFilePath = GUILayout.Toggle(showFilePath, "Path", GUILayout.Width(60f));
            if (GUILayout.Button("Close", GUILayout.Width(70f)))
            {
                visible = false;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawCounters()
        {
            GUILayout.Label(
                "records " + panel.TotalRecords +
                " | errors " + panel.ErrorCount +
                " | warnings " + panel.WarningCount +
                " | rate-limited " + panel.RateLimitedCount +
                " | session " + BpfLog.SessionId +
                " | scene " + BpfLog.CurrentScene +
                (panel.IsPaused ? " | PAUSED" : string.Empty));

            if (showFilePath)
            {
                GUILayout.Label("log: " + (panel.LogFilePath ?? "(not initialized)"));
            }

            if (!string.IsNullOrEmpty(exportMessage))
            {
                GUILayout.Label(exportMessage);
            }
        }

        private void DrawRecords()
        {
            List<BpfLogRecord> records = panel.GetRecords();
            recordScroll = GUILayout.BeginScrollView(recordScroll, GUILayout.ExpandHeight(true));
            if (records.Count == 0)
            {
                GUILayout.Label("(no records)");
            }
            else
            {
                int start = Mathf.Max(0, records.Count - MaxDrawnRows);
                for (int i = start; i < records.Count; i++)
                {
                    BpfLogRecord record = records[i];
                    Color previous = GUI.contentColor;
                    GUI.contentColor = ColorFor(record.Level);
                    GUILayout.Label(record.ToConsoleLine(), recordStyle);
                    GUI.contentColor = previous;
                }
            }

            GUILayout.EndScrollView();
        }

        private static Color ColorFor(BpfLogLevel level)
        {
            switch (level)
            {
                case BpfLogLevel.Debug:
                    return new Color(0.6f, 0.6f, 0.6f);
                case BpfLogLevel.Warning:
                    return new Color(1f, 0.85f, 0.3f);
                case BpfLogLevel.Error:
                case BpfLogLevel.Exception:
                case BpfLogLevel.Fatal:
                    return new Color(1f, 0.45f, 0.45f);
                default:
                    return Color.white;
            }
        }

        private static bool WasPressed(InputAction action)
        {
            return action != null && action.WasPressedThisFrame();
        }

        private static bool UseDevicePanel()
        {
            return !Application.isEditor && XRSettings.isDeviceActive;
        }

        private void CreateActions()
        {
            toggleKeyAction = CreateAction("BpfOverlayToggleKey", InputActionType.Button, BpfLogOptions.OverlayToggleBinding);
            toggleControllerAction = CreateAction("BpfOverlayToggleController", InputActionType.Button, BpfLogOptions.OverlayControllerToggleBinding);
            scrollAction = CreateAction("BpfOverlayScroll", InputActionType.Value, "<XRController>{RightHand}/primary2DAxis");
            levelAction = CreateAction("BpfOverlayLevel", InputActionType.Button, "<XRController>{RightHand}/primaryButton");
            clearAction = CreateAction("BpfOverlayClear", InputActionType.Button, "<XRController>{RightHand}/secondaryButton");
            pauseAction = CreateAction("BpfOverlayPause", InputActionType.Button, "<XRController>{LeftHand}/primaryButton");
        }

        private InputAction CreateAction(string name, InputActionType type, string binding)
        {
            InputAction action = new InputAction(name, type, binding);
            action.Enable();
            actions.Add(action);
            return action;
        }
    }
}
