using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
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
        [Serializable] sealed class TaskSnapshot { public TaskItem[] tasks; }
        [Serializable] sealed class TaskItem { public string session_id; public double expires_at; }
        public IReadOnlyList<string> ActiveTaskIds { get { PollTasks(); return activeTaskIds; } }

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
            if(!force && Time.unscaledTime-lastTaskPoll<.5f)return;
            lastTaskPoll=Time.unscaledTime;
            try {
                string path=Path.Combine(Application.persistentDataPath,"codex-tasks.json");
                var info=new FileInfo(path);
                string candidate=info.Exists&&info.Length<=256*1024?File.ReadAllText(path):"{\"tasks\":[]}";
                var snapshot=JsonUtility.FromJson<TaskSnapshot>(candidate);
                if(snapshot?.tasks==null)return;
                double now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()/1000.0;
                var ids=new List<string>();var seen=new HashSet<string>();
                foreach(var item in snapshot.tasks) {
                    if(item==null || !Guid.TryParse(item.session_id,out _) || item.expires_at<=now || !seen.Add(item.session_id))continue;
                    ids.Add(item.session_id);if(ids.Count==64)break;
                }
                tasksJson=candidate;activeTaskIds=ids.ToArray();
            } catch(IOException) { } // Keep the preceding complete atomic snapshot.
            catch(ArgumentException) { } // Partial/invalid JSON must not reassign pots.
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
