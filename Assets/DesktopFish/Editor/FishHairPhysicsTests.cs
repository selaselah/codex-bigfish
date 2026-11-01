using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace DesktopFish.Editor {
    public static class FishHairPhysicsTests {
        [Serializable] sealed class Report {
            public float stationaryMaximum,impulsePeak,residualAfterStop,settledResidual,frameRateMaximumDifference,derivativeMaximumError,averageTickMilliseconds;
            public bool teleportReset,longFrameReset,meshLifecycleRestored,colliderProjection;
        }
        static FishCompanion Scene() {EditorSceneManager.OpenScene("Assets/Scenes/DesktopFish.unity");return UnityEngine.Object.FindObjectOfType<FishCompanion>();}
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException("Hair physics: "+message);}
        static float Magnitude(FishHairFollow sim)=>sim.CaptureOffsets().Max(v=>v.magnitude);
        public static void Run() {
            var result=new Report();long ticks=0,count=0;
            var c=Scene();var skins=c.model.GetComponentsInChildren<SkinnedMeshRenderer>();var originals=skins.Select(s=>s.sharedMesh).ToArray();
            using(var sim=new FishHairFollow(c.model)) {
                sim.Tick(100,1);
                for(int frame=1;frame<=240;frame++){long start=System.Diagnostics.Stopwatch.GetTimestamp();sim.Tick(100+frame/60f,1);ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;count++;result.stationaryMaximum=Mathf.Max(result.stationaryMaximum,Magnitude(sim));}
                Require(result.stationaryMaximum<.0001f,"stationary character must not wave as the clock advances");
                Vector3 origin=c.model.localPosition;
                for(int frame=1;frame<=360;frame++) {
                    float t=frame/60f;c.model.localPosition=origin+Vector3.right*(.06f*Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.4f)));
                    sim.Tick(104+t,1);float displacement=Magnitude(sim);result.impulsePeak=Mathf.Max(result.impulsePeak,displacement);
                    if(frame==42)result.residualAfterStop=displacement;
                    if(frame==360)result.settledResidual=displacement;
                }
                Require(result.impulsePeak>.005f&&result.residualAfterStop>.001f,"real motion must create a continuing inertial response after stop");
                Require(result.settledResidual<.002f,"a held pose must settle instead of looping indefinitely");
                int resets=sim.ResetCount;c.model.localPosition+=Vector3.right;sim.Tick(110+1f/60,1);result.teleportReset=sim.ResetCount==resets+1&&Magnitude(sim)<1e-6f;Require(result.teleportReset,"teleport must reset safely");
                resets=sim.ResetCount;sim.Tick(112,1);result.longFrameReset=sim.ResetCount==resets+1&&Magnitude(sim)<1e-6f;Require(result.longFrameReset,"long frame/pause must reset safely");
                var project=typeof(FishHairFollow).GetMethod("ProjectCollision",BindingFlags.NonPublic|BindingFlags.Instance);
                Vector3 sphere=(Vector3)project.Invoke(sim,new object[]{new Vector3(.01f,0,0),Vector3.zero,Vector3.zero,.1f});
                Vector3 capsule=(Vector3)project.Invoke(sim,new object[]{new Vector3(.01f,.4f,0),Vector3.zero,Vector3.up,.1f});
                result.colliderProjection=Mathf.Abs(sphere.magnitude-.1f)<1e-6f&&Mathf.Abs(new Vector2(capsule.x,capsule.z).magnitude-.1f)<1e-6f;Require(result.colliderProjection,"sphere/capsule contact projection");
                Require(sim.SafetyResets==0,"normal/impulse profiles must not use unsafe-field fallback");
            }
            result.meshLifecycleRestored=skins.Select((s,i)=>s.sharedMesh==originals[i]).All(x=>x);Require(result.meshLifecycleRestored,"dispose must restore original mesh objects");
            Vector3[][] samples=new Vector3[3][];int[] rates={30,60,120};
            for(int run=0;run<3;run++) {
                c=Scene();Vector3 origin=c.model.localPosition;
                using(var sim=new FishHairFollow(c.model)) {
                    sim.Tick(100,1);
                    for(int frame=1;frame<=2*rates[run];frame++) {float t=frame/(float)rates[run];c.model.localPosition=origin+Vector3.right*(.04f*Mathf.Sin(t*3.5f));sim.Tick(100+t,1);}
                    samples[run]=sim.CaptureOffsets();Require(sim.SafetyResets==0,"frame-rate test fallback");
                    if(run==1) {
                        var field=typeof(FishHairFollow).GetMethod("Field",BindingFlags.Instance|BindingFlags.NonPublic);
                        Func<Vector3,Vector3[]> evaluate=p=>{object[] args={p,Vector3.zero,Vector3.zero,Vector3.zero};field.Invoke(sim,args);return new[]{(Vector3)args[1],(Vector3)args[2],(Vector3)args[3]};};
                        foreach(float x in new[]{-.19f,-.08f,.04f,.18f})foreach(float y in new[]{.29f,.43f,.58f,.73f}) {
                            Vector3 p=new Vector3(x,y,-.25f);var q=evaluate(p);const float e=.0001f;
                            Vector3 dx=(evaluate(p+Vector3.right*e)[0]-evaluate(p-Vector3.right*e)[0])/(2*e),dy=(evaluate(p+Vector3.up*e)[0]-evaluate(p-Vector3.up*e)[0])/(2*e);
                            result.derivativeMaximumError=Mathf.Max(result.derivativeMaximumError,Mathf.Max((dx-q[1]).magnitude,(dy-q[2]).magnitude));
                        }
                    }
                }
            }
            for(int run=0;run<2;run++)for(int i=0;i<samples[run].Length;i++)result.frameRateMaximumDifference=Mathf.Max(result.frameRateMaximumDifference,(samples[run][i]-samples[2][i]).magnitude);
            Require(result.frameRateMaximumDifference<.006f,"30/60/120fps response diverged");Require(result.derivativeMaximumError<.002f,"field derivatives must match independent finite differences");
            result.averageTickMilliseconds=(float)(ticks*1000.0/System.Diagnostics.Stopwatch.Frequency/count);
            Directory.CreateDirectory("work/hair-physics-213");File.WriteAllText("work/hair-physics-213/Behavior-Tests.json",JsonUtility.ToJson(result,true));
            Debug.Log("Stateful hair behavior tests passed: "+JsonUtility.ToJson(result));
        }
    }
}
