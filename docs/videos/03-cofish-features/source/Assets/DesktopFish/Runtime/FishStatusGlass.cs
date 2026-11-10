using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace DesktopFish
{
    // AppKit owns the live desktop backdrop. The editor/other platforms use a
    // lightweight illustration of the same capsule, with the same state text.
    sealed class FishStatusGlass : IDisposable
    {
        Texture2D fallback;
        GUIStyle text;
        int textureWidth;
        string lastLabel, lastTone;
        float lastNativeUpdate = -10;
        bool nativeFailed, nativeAttempted, nativeVisible;
        string tasksJson="{\"tasks\":[]}";
        float lastTaskPoll=-10;
        public bool NativePointerEvents { get; private set; }
        string[] activeTaskIds=Array.Empty<string>();
        readonly FishTaskLedger taskLedger=new FishTaskLedger(Path.Combine(FishTaskLedger.StateDirectory,"codex-completed-tasks.json"));
        readonly byte[] acknowledgedTask=new byte[65];
        public IReadOnlyList<string> ActiveTaskIds { get { PollTasks(); return activeTaskIds; } }
        public IReadOnlyList<FishTaskItem> ActiveTasks { get { PollTasks();return taskLedger.Active; } }
        public IReadOnlyList<FishTaskItem> CompletedTasks { get { PollTasks();return taskLedger.Completed; } }
        public bool HasTaskSnapshot => taskLedger.HasSnapshot;

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        [DllImport("FishGlassStatus")] static extern int FishGlassStatusIsReady();
        [DllImport("FishGlassStatus")] static extern int FishGlassStatusUpdate(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string tone);
        [DllImport("FishGlassStatus")] static extern void FishGlassStatusSetVisible(int visible);
        [DllImport("FishGlassStatus")] static extern void FishGlassStatusDestroy();
        [DllImport("FishGlassStatus")] static extern void FishGlassStatusUpdateTasks(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string json);
        [DllImport("FishGlassStatus")] static extern void FishGlassStatusShowTasks();
        [DllImport("FishGlassStatus")] static extern void FishGlassStatusShowTask(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string sessionId);
        [DllImport("FishGlassStatus")] static extern void FishGlassStatusFinishDrag();
        [DllImport("FishGlassStatus")] static extern int FishGlassStatusPollAvatarPointer(out float x,out float y,out float screenX,out float screenY);
        [DllImport("FishGlassStatus")] static extern int FishGlassStatusPollCompletedAcknowledgement([Out] byte[] utf8,int capacity);
        [DllImport("FishGlassStatus")] static extern void FishGlassStatusAcknowledgeCompletion([MarshalAs(UnmanagedType.LPUTF8Str)] string notificationId);
#endif
        public void Draw(string label, string tone, float scale, Font font)
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if (!nativeFailed)
            {
                try
                {
                    nativeAttempted = true;
                    // A small heartbeat also reconnects after a native window recreation.
                    if (lastLabel != label || lastTone != tone || Time.unscaledTime-lastNativeUpdate > 1)
                    {
                        FishGlassStatusUpdate(label, tone);
                        PollTasks();
                        FishGlassStatusUpdateTasks(tasksJson);
                        lastLabel = label; lastTone = tone; lastNativeUpdate = Time.unscaledTime;
                    }
                    if (FishGlassStatusIsReady() > 0)
                    {
                        if (!nativeVisible) FishGlassStatusSetVisible(1);
                        nativeVisible = true;
                        return;
                    }
                }
                catch (DllNotFoundException) { nativeFailed = true; }
                catch (EntryPointNotFoundException) { nativeFailed = true; }
            }
#endif
            DrawFallback(label, tone, scale, font);
        }

        void PollTasks(bool force=false)
        {
            PollAcknowledgements();
            if(!force && Time.unscaledTime-lastTaskPoll<.5f)return;
            lastTaskPoll=Time.unscaledTime;
            try {
                string path=Path.Combine(FishTaskLedger.StateDirectory,"codex-tasks.json");
                var info=new FileInfo(path);
                if(info.Exists) {
                    if(info.Length>256*1024)return;
                    string candidate=File.ReadAllText(path);
                    double now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()/1000.0;
                    if(!taskLedger.Update(candidate,now))return;
                }
                var ids=new List<string>();foreach(var item in taskLedger.Active)ids.Add(item.session_id);
                activeTaskIds=ids.ToArray();tasksJson=taskLedger.Json;
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
                if(!nativeFailed) {nativeAttempted=true;FishGlassStatusUpdateTasks(tasksJson);PollAcknowledgements();}
#endif
            } catch(IOException) { } // Keep the preceding complete atomic snapshot.
            catch(ArgumentException) { } // Partial/invalid JSON must not reassign pots.
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            catch(DllNotFoundException){nativeFailed=true;}
            catch(EntryPointNotFoundException){nativeFailed=true;}
#endif
        }

        void PollAcknowledgements()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if(!nativeFailed)try {
                for(int i=0;i<128;i++) {
                    int count=FishGlassStatusPollCompletedAcknowledgement(acknowledgedTask,acknowledgedTask.Length);
                    if(count<=0)break;
                    taskLedger.Acknowledge(Encoding.UTF8.GetString(acknowledgedTask,0,count));tasksJson=taskLedger.Json;
                }
            } catch(DllNotFoundException){nativeFailed=true;}
            catch(EntryPointNotFoundException){nativeFailed=true;}
#endif
        }

        public bool AcknowledgeCompletion(string notificationId)
        {
            if(!taskLedger.Acknowledge(notificationId))return false;
            tasksJson=taskLedger.Json;
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if(!nativeFailed)try {
                FishGlassStatusAcknowledgeCompletion(notificationId);FishGlassStatusUpdateTasks(tasksJson);
            } catch(DllNotFoundException){nativeFailed=true;}
            catch(EntryPointNotFoundException){nativeFailed=true;}
#endif
            return true;
        }

        public bool ShowTask(string sessionId)
        {
            PollTasks(true);
            if(string.IsNullOrEmpty(sessionId) || Array.IndexOf(activeTaskIds,sessionId)<0)return false;
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if(!nativeFailed)try {
                nativeAttempted=true;
                FishGlassStatusUpdateTasks(tasksJson);FishGlassStatusShowTask(sessionId);return true;
            }
            catch(DllNotFoundException){nativeFailed=true;}
            catch(EntryPointNotFoundException){nativeFailed=true;}
#endif
            return false;
        }

        public int ReadAvatarPointer(out Vector2 viewport,out Vector2 desktop)
        {
            viewport=desktop=Vector2.zero;
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if(!nativeFailed)try {
                if(FishGlassStatusIsReady()>0) {
                    int phase=FishGlassStatusPollAvatarPointer(out float x,out float y,out float sx,out float sy);
                    NativePointerEvents=true;viewport=new Vector2(x,y);desktop=new Vector2(sx,sy);return phase;
                }
            }
            catch(DllNotFoundException){nativeFailed=true;}
            catch(EntryPointNotFoundException){nativeFailed=true;}
#endif
            return 0;
        }

        public void ShowTasks()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if(!nativeFailed)try { PollTasks();FishGlassStatusUpdateTasks(tasksJson);FishGlassStatusShowTasks(); }
            catch(DllNotFoundException){nativeFailed=true;}
            catch(EntryPointNotFoundException){nativeFailed=true;}
#endif
        }

        public void FinishDrag()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if (!nativeFailed) try { nativeAttempted=true; FishGlassStatusFinishDrag(); }
            catch (DllNotFoundException) { nativeFailed=true; }
            catch (EntryPointNotFoundException) { nativeFailed=true; }
#endif
        }

        public void Hide()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if (nativeVisible && !nativeFailed) FishGlassStatusSetVisible(0);
#endif
            nativeVisible = false;
            lastLabel = null;
        }

        void DrawFallback(string label, string tone, float scale, Font font)
        {
            if (text == null)
            {
                text = new GUIStyle(GUI.skin.label) {
                    fontSize = 13, alignment = TextAnchor.MiddleCenter,
                    clipping = TextClipping.Clip, padding = new RectOffset()
                };
                text.normal.textColor = new Color(.12f,.17f,.25f,.96f);
            }
            text.font = font;
            int width = Mathf.CeilToInt(Mathf.Clamp(text.CalcSize(new GUIContent(label)).x+48,140,212));
            if (!fallback || textureWidth != width) CreateFallback(width);
            var oldMatrix = GUI.matrix; var oldColor = GUI.color;
            GUI.matrix = Matrix4x4.Scale(Vector3.one*scale);
            Rect rect = new Rect((Screen.width/scale-width)*.5f,Screen.height/scale-45,width,40);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(rect.x-6,rect.y-6,width+12,52),fallback);
            Color dot = tone == "complete" ? new Color(.18f,.66f,.50f) : tone == "error" ?
                new Color(.91f,.33f,.39f) : tone == "waiting" ? new Color(.91f,.64f,.22f) :
                tone == "idle" ? new Color(.48f,.56f,.67f) : new Color(.29f,.55f,.95f);
            GUI.DrawTexture(new Rect(rect.x+16,rect.y+17,6,6),Texture2D.whiteTexture,
                ScaleMode.StretchToFill,true,0,dot,0,3);
            GUI.Label(new Rect(rect.x+32,rect.y,width-48,40),label,text);
            GUI.color = oldColor; GUI.matrix = oldMatrix;
        }

        void CreateFallback(int width)
        {
            ReleaseTexture(); textureWidth = width;
            fallback = new Texture2D((width+12)*2,104,TextureFormat.RGBA32,false) {
                hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[fallback.width*fallback.height];
            for (int y=0;y<fallback.height;y++) for (int x=0;x<fallback.width;x++)
            {
                float px=(x+.5f)*.5f-6, py=(y+.5f)*.5f-6;
                float cx=Mathf.Clamp(px,20,width-20);
                float distance=new Vector2(px-cx,py-20).magnitude-20;
                float coverage=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.3f,.3f,distance));
                float shadowDistance=new Vector2(px-cx,py-18.5f).magnitude-20;
                float shadow=.12f*Mathf.Exp(-Mathf.Max(0,shadowDistance)*.7f)*(1-coverage);
                float rim=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.2f,1.0f,Mathf.Abs(distance+.5f))))*.4f*coverage;
                float fill=coverage*Mathf.Lerp(.26f,.40f,Mathf.Clamp01(py/40));
                Color glass=Color.Lerp(new Color(.82f,.89f,.98f),Color.white,Mathf.Clamp01(py/40));
                float alpha=fill+shadow*(1-fill);
                Color colour=(glass*fill+new Color(.05f,.08f,.14f)*shadow*(1-fill))/Mathf.Max(.0001f,alpha);
                colour=Color.Lerp(colour,Color.white,rim);
                colour.a=Mathf.Clamp01(alpha+rim*(1-alpha)); pixels[y*fallback.width+x]=colour;
            }
            fallback.SetPixels(pixels); fallback.Apply(false,false);
        }

        void ReleaseTexture()
        {
            if (!fallback) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(fallback);
            else UnityEngine.Object.DestroyImmediate(fallback);
            fallback = null;
        }
        public void Dispose()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if (nativeAttempted && !nativeFailed) FishGlassStatusDestroy();
#endif
            ReleaseTexture();
        }
    }
}
