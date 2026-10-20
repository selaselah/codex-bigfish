using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Kirurobo;

namespace DesktopFish
{
    // This companion controls the original model's bones and morph targets directly.
    // It does not require the VRChat client, avatar descriptor, or FX controller.
    public sealed class FishCompanion : MonoBehaviour
    {
        public const int NativeWidth = 240, NativeHeight = 320;
        public Transform model;
        public SkinnedMeshRenderer face;
        public Camera view;
        FishMotion motion;
        readonly Dictionary<string, int> shapes = new Dictionary<string, int>();
        readonly Dictionary<string, float> expressions = new Dictionary<string, float>();
        UniWindowController window;
        Vector2 cursorStart, windowStart, menuPoint;
        bool dragging, moved, menu, menuActions;
        float nextBlink, blinkStart = -10, lastStatePoll;
        string state = "idle", statePath;
        string previousMotionState = "idle";
        string stateContent = "";
        double stateExpiresAt;
        double noticeUntil;
        long lastCommandId;
        int activeCount;
        string stateSource = "", notice = "";
        bool showStatus = true;
        Texture2D statusPill;
        GUIStyle statusStyle;
        Font menuFont;
        UniWindowController.MouseButton previousButtons;

        [Serializable] class StateMessage
        {
            public string state, action, source, notice;
            public double expires_at, notice_until;
            public long command_id;
            public int active_count;
        }
        [Serializable] class RuntimeStatus
        {
            public string state, source;
            public int active_count;
            public long command_id;
            public double observed_at;
        }

        void Start()
        {
            Application.targetFrameRate = 30;
            Application.runInBackground = true;
            menuFont = Font.CreateDynamicFontFromOSFont(new[] { "PingFang SC", "Arial Unicode MS", "Arial" }, 14);
            showStatus = PlayerPrefs.GetInt("showStatus", 1) != 0;
            motion = new FishMotion(model);
            if (face && face.sharedMesh)
                for (int i = 0; i < face.sharedMesh.blendShapeCount; i++)
                    shapes[face.sharedMesh.GetBlendShapeName(i)] = i;
            nextBlink = Time.time + 2.5f;
            statePath = Path.Combine(Application.persistentDataPath, "state.json");
            Debug.Log("Desktop Fish state file: " + statePath);
            window = UniWindowController.current;
            window.isTopmost = true;
            window.forceWindowed = true;
            // Use native desktop coordinates for both input and hit-testing on Retina.
            window.isHitTestEnabled = false;
            window.isTransparent = true;
            if (!Application.isEditor)
            {
                Screen.SetResolution(NativeWidth, NativeHeight, FullScreenMode.Windowed);
                StartCoroutine(PlaceWindow());
            }
        }

        IEnumerator PlaceWindow()
        {
            // Wait for the native window hook, and use its coordinate system on Retina.
            yield return new WaitForEndOfFrame();
            yield return null;
            window.windowSize = new Vector2(NativeWidth, NativeHeight);
            yield return null;
            yield return null;
            Rect monitor = UniWindowController.GetMonitorRect(0);
            Vector2 size = window.windowSize;
            if (monitor.width > 0 && size.x > 0)
                window.windowPosition = new Vector2(
                    Mathf.Max(monitor.xMin, monitor.xMax - size.x - 40), monitor.yMin + 60);
            Debug.Log("Desktop Fish window: " + window.windowSize + "; monitor: " + monitor +
                "; transparent: " + window.isTransparent + "; topmost: " + window.isTopmost);
        }

        void Update()
        {
            float t = Time.time;
            if (t - lastStatePoll > 0.5f)
            {
                lastStatePoll = t;
                PollState();
            }
            if (t > nextBlink)
            {
                blinkStart = t;
                nextBlink = t + UnityEngine.Random.Range(2.8f, 6f);
            }
            Vector2 desktopCursor = ScreenCursor();
            Vector2 nativeSize = window.windowSize;
            Vector2 localCursor = desktopCursor - window.windowPosition;
            Vector3 pointer = nativeSize.x > 0 && nativeSize.y > 0 ?
                new Vector3(localCursor.x / nativeSize.x * Screen.width,
                    localCursor.y / nativeSize.y * Screen.height, 0) : Input.mousePosition;
            bool inWindow = localCursor.x >= 0 && localCursor.y >= 0 &&
                localCursor.x < nativeSize.x && localCursor.y < nativeSize.y;
            bool hover = inWindow && Physics.Raycast(view.ScreenPointToRay(pointer), out RaycastHit _, 30);
            var buttons = UniWindowController.GetMouseButtons();
            bool left = (buttons & UniWindowController.MouseButton.Left) != 0;
            bool wasLeft = (previousButtons & UniWindowController.MouseButton.Left) != 0;
            window.isClickThrough = !hover && !dragging && !menu;
            float x = Mathf.Clamp((pointer.x / Mathf.Max(Screen.width, 1) - 0.5f) * 2, -1, 1);
            float y = Mathf.Clamp((pointer.y / Mathf.Max(Screen.height, 1) - 0.5f) * 2, -1, 1);

            if (left && !wasLeft && hover && !menu)
            {
                dragging = true;
                moved = false;
                cursorStart = ScreenCursor();
                windowStart = window.windowPosition;
                window.isClickThrough = false;
            }
            if (dragging && left)
            {
                Vector2 delta = ScreenCursor() - cursorStart;
                if (delta.sqrMagnitude > 16) moved = true;
                if (moved && !Application.isEditor) window.windowPosition = windowStart + delta;
            }
            if (dragging && !left && wasLeft)
            {
                dragging = false;
                if (!moved) motion.Click(t);
                else Debug.Log("Desktop Fish moved to: " + window.windowPosition);
            }
            if (state != previousMotionState)
            {
                if (state == "complete") motion.Play(FishMotion.Gesture.Cheer, t);
                previousMotionState = state;
            }
            motion.Tick(t, Time.deltaTime, x, y, state, dragging);
            float blinkTime = t - blinkStart;
            SetShape("Fcl_EYE_Close", Mathf.Max(motion.ClosedEyes, blinkTime < 0.19f ?
                Mathf.Sin(blinkTime / 0.19f * Mathf.PI) * 100 : 0));
            Expression("Fcl_ALL_Joy", motion.Joy);
            Expression("Fcl_ALL_Sorrow", state == "error" ? 70 : 0);
            Expression("Fcl_ALL_Surprised", dragging ? 35 : state == "waiting" ? 45 : 0);
            Expression("Fcl_ALL_Fun", state == "working" ? 25 : 0);
            if (Input.GetKeyDown(KeyCode.Escape)) CloseMenu();
            previousButtons = buttons;
        }

        Vector2 ScreenCursor()
        {
            // UniWindowController reports native desktop cursor coordinates, avoiding
            // feedback when dragging changes the origin of Input.mousePosition.
            return UniWindowController.GetCursorPosition();
        }

        void SetShape(string name, float weight)
        {
            if (face && shapes.TryGetValue(name, out int index)) face.SetBlendShapeWeight(index, weight);
        }

        void Expression(string name, float target)
        {
            expressions.TryGetValue(name, out float previous);
            float value = Mathf.Lerp(previous, target, 1 - Mathf.Exp(-7 * Time.deltaTime));
            expressions[name] = value;
            SetShape(name, value);
        }

        void PollState()
        {
            double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
            if (stateExpiresAt > 0 && now >= stateExpiresAt)
            {
                state = "idle";
                stateExpiresAt = 0;
                activeCount = 0;
                RecordStatus();
            }
            try
            {
                if (!File.Exists(statePath)) return;
                string text = File.ReadAllText(statePath);
                if (text == stateContent) return;
                var message = JsonUtility.FromJson<StateMessage>(text);
                if (message == null || !ValidState(message.state)) return;
                stateContent = text;
                stateSource = message.source ?? "manual";
                activeCount = message.active_count;
                notice = message.notice ?? "";
                noticeUntil = message.notice_until;
                stateExpiresAt = message.expires_at;
                if (stateExpiresAt > 0 && now >= stateExpiresAt)
                {
                    state = "idle";
                    stateExpiresAt = 0;
                    activeCount = 0;
                    RecordStatus();
                    return;
                }
                state = message.state;
                if (Enum.TryParse(message.action, true, out FishMotion.Gesture action) &&
                    action != FishMotion.Gesture.None && Enum.IsDefined(typeof(FishMotion.Gesture), action) &&
                    (message.command_id == 0 || message.command_id != lastCommandId))
                {
                    lastCommandId = message.command_id;
                    motion.Play(action, Time.time);
                    previousMotionState = state;
                    Debug.Log("Desktop Fish gesture: " + action);
                }
                RecordStatus();
            }
            catch (IOException) { } // Retry when an external writer finishes.
            catch (ArgumentException) { } // Ignore a partial/invalid JSON document.
        }

        static bool ValidState(string value) => value == "idle" || value == "working" ||
            value == "complete" || value == "waiting" || value == "error";

        void RecordStatus()
        {
            try
            {
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "runtime-status.json"),
                    JsonUtility.ToJson(new RuntimeStatus {
                        state = state, source = stateSource, active_count = activeCount,
                        command_id = lastCommandId,
                        observed_at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0
                    }));
            }
            catch (IOException) { }
        }

        void DrawStatus(float scale)
        {
            if (!showStatus) return;
            if (!statusPill)
            {
                statusPill = new Texture2D(64, 32, TextureFormat.RGBA32, false);
                for (int y = 0; y < 32; y++) for (int x = 0; x < 64; x++)
                {
                    float cx = Mathf.Clamp(x, 16, 47);
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(cx, 15.5f));
                    statusPill.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(16 - distance)));
                }
                statusPill.Apply();
                statusStyle = new GUIStyle(GUI.skin.label) {
                    font = menuFont, fontSize = 11, alignment = TextAnchor.MiddleCenter
                };
                statusStyle.normal.textColor = Color.white;
            }
            double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
            string label = state == "working" ? "工作中" : state == "waiting" ? "等你回复" :
                state == "complete" ? "本回合结束" : state == "error" ? "遇到问题" : "休息中";
            if (state == "working" || state == "waiting")
                label += activeCount > 0 ? " · " + activeCount + " 项" : "";
            if (state == "working") label += new string('.', (int)(Time.unscaledTime * 2) % 3 + 1);
            if (noticeUntil > now && !string.IsNullOrEmpty(notice)) label = notice;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            Rect rect = new Rect((Screen.width / scale - 170) * 0.5f, Screen.height / scale - 27, 170, 22);
            GUI.color = state == "complete" || noticeUntil > now ?
                new Color(0.10f, 0.38f, 0.37f, 0.92f) : new Color(0.10f, 0.16f, 0.30f, 0.88f);
            GUI.DrawTexture(rect, statusPill);
            GUI.color = Color.white;
            GUI.Label(rect, label, statusStyle);
        }

        void CloseMenu()
        {
            menu = false;
            menuActions = false;
            if (window) window.isHitTestEnabled = false;
        }

        void OnGUI()
        {
            // Handle the queued window event directly. Polling native button state
            // misses secondary clicks on macOS (including trackpad clicks).
            Event mouseEvent = Event.current;
            float scale = MenuScale();
            Vector2 menuPointer = mouseEvent.mousePosition / scale;
            if (mouseEvent.type == EventType.MouseDown && mouseEvent.button == 1)
            {
                menu = !menu;
                menuActions = false;
                menuPoint = new Vector2(Mathf.Clamp(menuPointer.x, 8, Screen.width / scale - 190),
                    Mathf.Clamp(menuPointer.y, 8, Screen.height / scale - 265));
                window.isClickThrough = false;
                Debug.Log("Desktop Fish menu: " + (menu ? "opened" : "closed"));
                mouseEvent.Use();
            }
            if (menu && mouseEvent.type == EventType.MouseDown && mouseEvent.button == 0 &&
                !MenuRect().Contains(menuPointer)) CloseMenu();
            if (!menu) { DrawStatus(scale); return; }
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            if (menuFont) GUI.skin.font = menuFont;
            GUILayout.BeginArea(MenuRect(), GUI.skin.box);
            GUILayout.Label(menuActions ? "大肥鱼 · 动作" : "蓝色大肥鱼");
            if (menuActions)
            {
                string[] actions = { "挥挥手", "女仆行礼", "打个小盹", "原地小碎步", "开心跳跃" };
                for (int i = 0; i < actions.Length; i++)
                    if (GUILayout.Button(actions[i], GUILayout.Height(25)))
                    {
                        motion.Play((FishMotion.Gesture)(i + 1), Time.time);
                        Debug.Log("Desktop Fish gesture: " + (FishMotion.Gesture)(i + 1));
                        CloseMenu();
                    }
                if (GUILayout.Button(motion.randomActions ? "随机小动作：开" : "随机小动作：关", GUILayout.Height(25)))
                    motion.randomActions = !motion.randomActions;
                if (GUILayout.Button("返回", GUILayout.Height(25))) menuActions = false;
                GUILayout.EndArea();
                return;
            }
            string[] states = { "idle", "working", "complete", "waiting", "error" };
            string[] labels = { "休息一下", "开始工作", "完成啦", "等你回复", "有点难过" };
            for (int i = 0; i < states.Length; i++)
                if (GUILayout.Button(labels[i], GUILayout.Height(25)))
                {
                    state = states[i];
                    stateExpiresAt = 0;
                    Debug.Log("Desktop Fish menu state: " + state);
                    CloseMenu();
                }
            if (GUILayout.Button("动作…", GUILayout.Height(25))) menuActions = true;
            if (GUILayout.Button(showStatus ? "状态提示：开" : "状态提示：关", GUILayout.Height(25)))
            {
                showStatus = !showStatus;
                PlayerPrefs.SetInt("showStatus", showStatus ? 1 : 0);
            }
            if (GUILayout.Button("退出", GUILayout.Height(25))) Application.Quit();
            GUILayout.EndArea();
        }

        float MenuScale() => window && !Application.isEditor && window.windowSize.x > 0 ?
            Mathf.Max(1, Screen.width / window.windowSize.x) : 1;

        Rect MenuRect() => new Rect(menuPoint.x, menuPoint.y, 180, 260);
    }
}
