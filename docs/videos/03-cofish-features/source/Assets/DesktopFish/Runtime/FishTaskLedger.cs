using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DesktopFish
{
    [Serializable] public sealed class FishTaskItem
    {
        // A completion UUID acknowledges one exact turn. AppKit uses chat_id
        // as the stable card identity across working/completed transitions.
        public string session_id, chat_id, turn_id, title, summary, message, state, completion_key;
        public double expires_at, updated_at;
        public bool retained;
    }

    public sealed class FishTaskLedger
    {
        public static string StateDirectory { get; }=ResolveDirectory();
        static string ResolveDirectory()
        {
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="--cofish-state-dir")return Path.GetFullPath(args[i+1]);
            return Application.persistentDataPath;
        }
        [Serializable] sealed class Snapshot { public FishTaskItem[] tasks; }
        [Serializable] sealed class Progress { public string chat_id,turn_id,state; public double updated_at; }
        [Serializable] sealed class Saved { public int version=1; public FishTaskItem[] completed; public string[] acknowledged; public Progress[] progress; }
        readonly Dictionary<string,FishTaskItem> completed=new Dictionary<string,FishTaskItem>();
        readonly HashSet<string> acknowledged=new HashSet<string>();
        readonly Dictionary<string,Progress> progress=new Dictionary<string,Progress>();
        readonly string path;
        string lastSaved;
        List<FishTaskItem> active=new List<FishTaskItem>(), visibleCompleted=new List<FishTaskItem>();
        public bool HasSnapshot { get; private set; }
        public IReadOnlyList<FishTaskItem> Active => active;
        public IReadOnlyList<FishTaskItem> Completed => visibleCompleted;
        public string Json { get; private set; }="{\"tasks\":[]}";

        public FishTaskLedger(string storagePath=null)
        {
            path=storagePath;
            if(!string.IsNullOrEmpty(path))try {
                if(File.Exists(path) && new FileInfo(path).Length<=16*1024*1024) {
                    lastSaved=File.ReadAllText(path);var saved=JsonUtility.FromJson<Saved>(lastSaved);
                    if(saved?.version==1) {
                        foreach(string key in saved.acknowledged??Array.Empty<string>())if(!string.IsNullOrEmpty(key))acknowledged.Add(key);
                        foreach(var mark in saved.progress??Array.Empty<Progress>())
                            if(mark!=null&&Guid.TryParse(mark.chat_id,out _)&&Finite(mark.updated_at))progress[mark.chat_id]=mark;
                        foreach(var item in saved.completed??Array.Empty<FishTaskItem>())
                            if(item!=null && Guid.TryParse(item.session_id,out _) && Guid.TryParse(item.chat_id,out _) &&
                               item.retained && item.state=="complete" && !string.IsNullOrEmpty(item.completion_key) && !acknowledged.Contains(item.completion_key))
                                completed[item.completion_key]=item;
                    }
                }
            } catch(IOException e) { Debug.LogWarning("Completed task notices could not be read: "+e.Message); }
            catch(UnauthorizedAccessException e) { Debug.LogWarning("Completed task notices could not be read: "+e.Message); }
            catch(ArgumentException) { }
            // Migrate the old per-turn archive to at most one completion per
            // chat. Keep the newest, and remember older turns as already read.
            foreach(var group in completed.Values.ToArray().GroupBy(item=>item.chat_id)) {
                var newest=group.OrderByDescending(item=>item.updated_at).ThenBy(item=>item.completion_key).First();
                if(progress.TryGetValue(group.Key,out var mark) && (mark.updated_at>newest.updated_at ||
                    (mark.updated_at==newest.updated_at&&mark.state!="complete")))RetireChat(group.Key);
                else {
                    RetireChat(group.Key,newest.completion_key);
                    progress[group.Key]=new Progress {chat_id=group.Key,turn_id=newest.turn_id,state="complete",updated_at=newest.updated_at};
                }
            }
            Rebuild();Save();
        }

        static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        void RetireChat(string chat,string exceptKey=null)
        {
            foreach(var pair in completed.Where(pair=>pair.Value.chat_id==chat&&pair.Key!=exceptKey).ToArray()) {
                completed.Remove(pair.Key);acknowledged.Add(pair.Key);
            }
        }

        static string Clean(string text,int limit)
        {
            text=text??"";if(text.Length<=limit)return text;
            int length=limit;if(length>0&&char.IsHighSurrogate(text[length-1]))length--;
            return text.Substring(0,length);
        }
        static FishTaskItem Normalize(FishTaskItem source)
        {
            string chat=Guid.Parse(source.session_id).ToString();
            return new FishTaskItem {session_id=chat,chat_id=chat,turn_id=Clean(source.turn_id,256),
                title=Clean(source.title,160),summary=Clean(source.summary,120),message=Clean(source.message,600),
                state=source.state,expires_at=source.expires_at,updated_at=source.updated_at};
        }
        static string Key(FishTaskItem item) => item.session_id+"\n"+(!string.IsNullOrEmpty(item.turn_id)?item.turn_id:
            "legacy:"+item.updated_at.ToString("R",CultureInfo.InvariantCulture));

        public bool Update(string json,double now)
        {
            var snapshot=JsonUtility.FromJson<Snapshot>(json);
            if(snapshot?.tasks==null)return false;
            var next=new List<FishTaskItem>();var seen=new HashSet<string>();
            foreach(var source in snapshot.tasks) {
                if(seen.Count>=64)break;
                if(source==null || !Guid.TryParse(source.session_id,out _) || source.expires_at<=now ||
                   !Finite(source.expires_at) || !Finite(source.updated_at) ||
                   (source.state!="working"&&source.state!="waiting"&&source.state!="complete"))continue;
                var item=Normalize(source);if(!seen.Add(item.session_id))continue;
                string key=Key(item);
                bool stale=item.state=="complete"&&acknowledged.Contains(key);
                stale |= progress.TryGetValue(item.chat_id,out var priorProgress) &&
                    (item.updated_at<priorProgress.updated_at ||
                    (item.state!="complete"&&priorProgress.state=="complete"&&!string.IsNullOrEmpty(item.turn_id)&&item.turn_id==priorProgress.turn_id));
                if(stale) {
                    var current=active.FirstOrDefault(task=>task.chat_id==item.chat_id&&task.expires_at>now);
                    if(current!=null)next.Add(current);
                    continue;
                }
                progress[item.chat_id]=new Progress {chat_id=item.chat_id,turn_id=item.turn_id,state=item.state,updated_at=item.updated_at};
                // Continuing a chat implicitly reads its previous result. The
                // same card becomes working again; no parallel bowl remains.
                RetireChat(item.chat_id,item.state=="complete"?key:null);
                if(item.state=="complete") {
                    string id=completed.TryGetValue(key,out var prior)?prior.session_id:Guid.NewGuid().ToString();
                    item.chat_id=item.session_id;item.session_id=id;item.completion_key=key;
                    item.retained=true;item.expires_at=0;completed[key]=item;
                } else next.Add(item);
                if(seen.Count==64)break;
            }
            active=next;HasSnapshot=true;Rebuild();Save();return true;
        }

        public bool Acknowledge(string notificationId)
        {
            var found=completed.FirstOrDefault(pair=>pair.Value.session_id==notificationId);
            if(found.Value==null)return false;
            completed.Remove(found.Key);acknowledged.Add(found.Key);Rebuild();Save();return true;
        }

        void Rebuild()
        {
            // The archive is never timed out or dropped. A bounded visible
            // page keeps the existing native JSON budget; queued notices
            // surface as earlier ones are acknowledged.
            visibleCompleted=completed.Values.OrderByDescending(item=>item.updated_at).ThenBy(item=>item.session_id).Take(64).ToList();
            while(true) {
                Json=JsonUtility.ToJson(new Snapshot {tasks=active.Concat(visibleCompleted).ToArray()});
                if(Encoding.UTF8.GetByteCount(Json)<=256*1024)break;
                if(visibleCompleted.Count>0)visibleCompleted.RemoveAt(visibleCompleted.Count-1);
                else {foreach(var item in active)item.message="";Json=JsonUtility.ToJson(new Snapshot {tasks=active.ToArray()});break;}
            }
        }
        void Save()
        {
            if(string.IsNullOrEmpty(path))return;
            string json=JsonUtility.ToJson(new Saved {completed=completed.OrderBy(pair=>pair.Key).Select(pair=>pair.Value).ToArray(),acknowledged=acknowledged.OrderBy(key=>key).ToArray(),progress=progress.OrderBy(pair=>pair.Key).Select(pair=>pair.Value).ToArray()});
            if(json==lastSaved)return;
            string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(temporary,json);
                if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);
                lastSaved=json;
            } catch(IOException e) { Debug.LogWarning("Completed task notices could not be saved: "+e.Message); }
            catch(UnauthorizedAccessException e) { Debug.LogWarning("Completed task notices could not be saved: "+e.Message); }
            finally {try {if(File.Exists(temporary))File.Delete(temporary);}catch(IOException){}catch(UnauthorizedAccessException){}}
        }
    }
}
