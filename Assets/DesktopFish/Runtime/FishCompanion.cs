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
        // Keep the original character pixel scale while giving the work
        // equipment its own surrounding transparent space.
        public const int NativeWidth = 312, NativeHeight = 416;
        public Transform model;
        public SkinnedMeshRenderer face;
        public Camera view;
        public Shader workPropShader;
        FishMotion motion;
        FishWorkStation workStation;
        float restingViewSize;
        Vector3 restingViewPosition;
        sealed class ShapeBinding { public SkinnedMeshRenderer renderer; public int index; }
        readonly Dictionary<string, List<ShapeBinding>> shapes = new Dictionary<string, List<ShapeBinding>>();
        readonly Dictionary<string, float> expressions = new Dictionary<string, float>();
        UniWindowController window;
        Vector2 cursorStart, windowStart, menuPoint;
        bool dragging, moved, menu, menuActions;
        bool pressedCooker, cookerEventDrag;
        string pressedCookerTaskId;
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
        bool nativeWindowPlaced;
        FishStatusGlass statusGlass;
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
            workStation = new FishWorkStation(model, workPropShader);
            restingViewSize=view.orthographicSize;restingViewPosition=view.transform.position;
            foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                for(int i=0;i<renderer.sharedMesh.blendShapeCount;i++) {
                    string name=renderer.sharedMesh.GetBlendShapeName(i);
                    if(!shapes.TryGetValue(name,out var bindings))shapes[name]=bindings=new List<ShapeBinding>();
                    bindings.Add(new ShapeBinding{renderer=renderer,index=i});
                }
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
            nativeWindowPlaced = true;
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
            Ray pointerRay=view.ScreenPointToRay(pointer);
            string cookerTaskId=null;
            bool overCooker=inWindow && workStation.TryGetCookerTask(pointerRay,out cookerTaskId);
            bool hover = overCooker || (inWindow && Physics.Raycast(pointerRay, out RaycastHit _, 30));
            var buttons = UniWindowController.GetMouseButtons();
            bool left = (buttons & UniWindowController.MouseButton.Left) != 0;
            bool wasLeft = (previousButtons & UniWindowController.MouseButton.Left) != 0;
            if(statusGlass!=null)for(int i=0;i<128;i++) {
                int phase=statusGlass.ReadAvatarPointer(out Vector2 viewport,out Vector2 desktop);
                if(phase==0)break;
                HandleCookerPointer(phase,view.ViewportPointToRay(viewport),desktop);
            }
            window.isClickThrough = !hover && !dragging && !menu;
            float x = Mathf.Clamp((pointer.x / Mathf.Max(Screen.width, 1) - 0.5f) * 2, -1, 1);
            float y = Mathf.Clamp((pointer.y / Mathf.Max(Screen.height, 1) - 0.5f) * 2, -1, 1);

            if (left && !wasLeft && hover && !menu && !cookerEventDrag && !(overCooker && statusGlass!=null && statusGlass.NativePointerEvents))
            {
                dragging = true;
                moved = false;
                pressedCooker=overCooker;pressedCookerTaskId=cookerTaskId;
                cursorStart = ScreenCursor();
                windowStart = window.windowPosition;
                window.isClickThrough = false;
            }
            if (dragging && left && !cookerEventDrag)
            {
                Vector2 delta = ScreenCursor() - cursorStart;
                if (delta.sqrMagnitude > 16) moved = true;
                if (moved && !Application.isEditor) window.windowPosition = windowStart + delta;
            }
            if (dragging && !left && wasLeft && !cookerEventDrag) FinishPointerDrag(t,overCooker,cookerTaskId);
            if (state != previousMotionState)
            {
                if (state == "complete") {workStation.CompleteCookers(workStation.CookerCount,t);motion.Play(FishMotion.Gesture.Cheer, t);}
                previousMotionState = state;
            }
            motion.Tick(t, Time.deltaTime, x, y, state, dragging);
            workStation.Tick(t, motion.WorkWeight, state == "working" ? activeCount : 0,
                state == "working" && activeCount > 0 && !dragging);
            if(statusGlass==null)statusGlass=new FishStatusGlass();
            workStation.BindTaskIds(statusGlass.ActiveTaskIds);
            FishWorkStation.FrameWorkingCamera(view,restingViewSize,restingViewPosition,motion.WorkWeight);
            float blinkTime = t - blinkStart;
            float visibleJoy=Expression("Fcl_ALL_Joy", motion.Joy);
            // ALL_Joy already closes the eyes. Add only the requested excess.
            float extraEyes=Expression("Fcl_EYE_Joy",Mathf.Max(0,motion.SmileEyes-motion.Joy));
            float visibleSmile=Mathf.Clamp(visibleJoy+extraEyes,0,100);
            float blink=blinkTime<.19f?Mathf.Sin(blinkTime/.19f*Mathf.PI)*100:0;
            SetShape("Fcl_EYE_Close",Mathf.Max(motion.ClosedEyes,blink)*(1-visibleSmile*.01f));
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

        void FinishPointerDrag(float time,bool overCooker,string cookerTaskId)
        {
            dragging=false;cookerEventDrag=false;
            if(!moved) {
                if(pressedCooker) {
                    if(overCooker && !string.IsNullOrEmpty(pressedCookerTaskId) && cookerTaskId==pressedCookerTaskId) {
                        if(statusGlass==null)statusGlass=new FishStatusGlass();
                        if(statusGlass.ShowTask(pressedCookerTaskId)) {
                            showStatus=true;
                            Debug.Log("Desktop Fish cooker task selected: "+pressedCookerTaskId);
                        }
                    }
                } else motion.Click(time);
            } else {
                if(statusGlass==null)statusGlass=new FishStatusGlass();
                statusGlass.FinishDrag();
                Debug.Log("Desktop Fish drag released at: "+window.windowPosition);
            }
            pressedCooker=false;pressedCookerTaskId=null;
        }

        void HandleCookerEvent(Event e)
        {
            if(e.button!=0 || menu || workStation==null || !view || !window)return;
            if(statusGlass!=null && statusGlass.NativePointerEvents)return;
            if(e.type!=EventType.MouseDown && e.type!=EventType.MouseDrag && e.type!=EventType.MouseUp)return;
            // Queue-based window events also catch brief/trackpad clicks that
            // can fall entirely between native button-state polling frames.
            Vector2 point=new Vector2(e.mousePosition.x,Screen.height-e.mousePosition.y);
            Vector2 desktop=window.windowPosition+Vector2.Scale(new Vector2(point.x/Mathf.Max(1,Screen.width),point.y/Mathf.Max(1,Screen.height)),window.windowSize);
            int phase=e.type==EventType.MouseDown?1:e.type==EventType.MouseDrag?2:3;
            if(HandleCookerPointer(phase,view.ScreenPointToRay(point),desktop))e.Use();
        }

        bool HandleCookerPointer(int phase,Ray ray,Vector2 desktop)
        {
            if(phase==4) { cookerEventDrag=false;dragging=false;pressedCooker=false;pressedCookerTaskId=null;return false; }
            if(menu || workStation==null || !window)return false;
            bool hit=workStation.TryGetCookerTask(ray,out string id);
            if(phase==1 && hit) {
                cookerEventDrag=true;dragging=true;moved=false;pressedCooker=true;pressedCookerTaskId=id;
                cursorStart=desktop;windowStart=window.windowPosition;window.isClickThrough=false;return true;
            } else if(cookerEventDrag && phase==2) {
                Vector2 delta=desktop-cursorStart;if(delta.sqrMagnitude>16)moved=true;
                if(moved&&!Application.isEditor)window.windowPosition=windowStart+delta;return true;
            } else if(cookerEventDrag && phase==3) {
                if((desktop-cursorStart).sqrMagnitude>16)moved=true;
                FinishPointerDrag(Time.time,hit,id);return true;
            }
            return false;
        }

        void OnApplicationFocus(bool focused)
        {
            if(!focused && cookerEventDrag) { cookerEventDrag=false;dragging=false;pressedCooker=false;pressedCookerTaskId=null; }
        }

        void SetShape(string name, float weight)
        {
            if(shapes.TryGetValue(name,out var bindings))foreach(var binding in bindings)binding.renderer.SetBlendShapeWeight(binding.index,weight);
        }

        float Expression(string name, float target)
        {
            expressions.TryGetValue(name, out float previous);
            float value = Mathf.Lerp(previous, target, 1 - Mathf.Exp(-7 * Time.deltaTime));
            expressions[name] = value;
            SetShape(name, value);
            return value;
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
                int previousActiveCount=activeCount;
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
                    if(action==FishMotion.Gesture.Cheer&&(state=="complete"||(noticeUntil>now&&!string.IsNullOrEmpty(notice))))
                        workStation.CompleteCookers(state=="complete"&&activeCount==0?workStation.CookerCount:Mathf.Max(1,previousActiveCount-activeCount),Time.time,statusGlass?.ActiveTaskIds);
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
            // Create glass after UniWinC finishes the final native size and
            // position. A splash-sized/offscreen window can leave stale clipping.
            if (!Application.isEditor && !nativeWindowPlaced) return;
            if (!showStatus) { statusGlass?.Hide(); return; }
            if (statusGlass == null) statusGlass = new FishStatusGlass();
            double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
            string label = state == "working" ? "工作中" : state == "waiting" ? "等你回复" :
                state == "complete" ? "本回合结束" : state == "error" ? "遇到问题" : "休息中";
            if (state == "working" || state == "waiting")
                label += activeCount > 0 ? " · " + activeCount + " 项" : "";
            if (noticeUntil > now && !string.IsNullOrEmpty(notice)) label = notice;
            statusGlass.Draw(label, noticeUntil > now ? "complete" : state, scale, menuFont);
        }

        void OnDestroy() { statusGlass?.Dispose(); workStation?.Dispose(); motion?.Dispose(); }

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
            HandleCookerEvent(mouseEvent);
            float scale = MenuScale();
            Vector2 menuPointer = mouseEvent.mousePosition / scale;
            if (mouseEvent.type == EventType.MouseDown && mouseEvent.button == 1)
            {
                menu = !menu;
                menuActions = false;
                menuPoint = new Vector2(Mathf.Clamp(menuPointer.x, 8, Screen.width / scale - 190),
                    Mathf.Clamp(menuPointer.y, 8, Screen.height / scale - 295));
                window.isClickThrough = false;
                Debug.Log("Desktop Fish menu: " + (menu ? "opened" : "closed"));
                mouseEvent.Use();
            }
            if (menu && mouseEvent.type == EventType.MouseDown && mouseEvent.button == 0 &&
                !MenuRect().Contains(menuPointer)) CloseMenu();
            if (!menu) { DrawStatus(scale); return; }
            statusGlass?.Hide();
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
            if (GUILayout.Button("查看任务消息", GUILayout.Height(25))) {
                CloseMenu();
                showStatus=true; // Reveal the entry point for this view; do not change the saved preference.
                if(statusGlass==null)statusGlass=new FishStatusGlass();
                statusGlass.ShowTasks();
            }
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

        Rect MenuRect() => new Rect(menuPoint.x, menuPoint.y, 180, 290);
    }
}
