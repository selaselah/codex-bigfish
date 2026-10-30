using System;
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

        void PollTasks()
        {
            if(Time.unscaledTime-lastTaskPoll<.5f)return;
            lastTaskPoll=Time.unscaledTime;
            try {
                string path=Path.Combine(Application.persistentDataPath,"codex-tasks.json");
                var info=new FileInfo(path);
                tasksJson=info.Exists&&info.Length<=256*1024?File.ReadAllText(path):"{\"tasks\":[]}";
            } catch(IOException) { } // Keep the preceding complete atomic snapshot.
        }

        public void ShowTasks()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if(!nativeFailed)try { PollTasks();FishGlassStatusUpdateTasks(tasksJson);FishGlassStatusShowTasks(); }
            catch(DllNotFoundException){nativeFailed=true;}
            catch(EntryPointNotFoundException){nativeFailed=true;}
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
