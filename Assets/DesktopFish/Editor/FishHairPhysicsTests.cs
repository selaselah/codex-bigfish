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
            public float sideImpulsePeak,sideResidualAfterStop,sideSettledResidual,sideRenderedMiddleAfterStop,sideRenderedTipAfterStop,sideRootMaximum,sideDerivativeMaximumError;public int sideDomains;
            public float[] frontGroupPeak=new float[6],frontGroupAfterStop=new float[6];
            public float frontRootMaximum,frontAfterStop,tuftRootMaximum,tuftPeak,tuftAfterStop,frontSettled,tuftSettled,frontDerivativeMaximumError;
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
                for(int frame=1;frame<=240;frame++){long start=System.Diagnostics.Stopwatch.GetTimestamp();sim.Tick(100+frame/60f,1);ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;count++;result.stationaryMaximum=Mathf.Max(result.stationaryMaximum,Mathf.Max(Magnitude(sim),sim.CaptureSideOffsets().Select(v=>v.magnitude).DefaultIfEmpty(0).Max()));}
                Require(result.stationaryMaximum<.0001f,"stationary character must not wave as the clock advances");
                result.sideDomains=sim.SideDomainCount;Require(result.sideDomains==4,"both complete outer/return side domains");
                var sideField=typeof(FishHairFollow).GetField("sideStrands",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(sim);
                int[] selected=(int[])sideField.GetType().GetField("VertexStrands").GetValue(sideField);
                var skin=c.model.GetComponentsInChildren<SkinnedMeshRenderer>().Single(x=>x.sharedMesh.name=="Body");var neutral=originals[Array.IndexOf(skins,skin)].vertices;
                float[] top=Enumerable.Range(0,2).Select(side=>Enumerable.Range(0,selected.Length).Where(i=>selected[i]==side).Max(i=>neutral[i].y)).ToArray();
                var front=typeof(FishHairFollow).GetField("frontHair",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(sim);
                var frontFree=front.GetType().GetMethod("FreeWeight");
                int[] groups=(int[])front.GetType().GetField("Groups").GetValue(front);
                float[] tuftWeight=(float[])front.GetType().GetField("TuftWeight").GetValue(front);
                Vector3 origin=c.model.localPosition;
                for(int frame=1;frame<=360;frame++) {
                    float t=frame/60f;c.model.localPosition=origin+Vector3.right*(.06f*Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.4f)));
                    sim.Tick(104+t,1);float displacement=Magnitude(sim);result.impulsePeak=Mathf.Max(result.impulsePeak,displacement);
                    if(frame==42)result.residualAfterStop=displacement;
                    if(frame==360)result.settledResidual=displacement;
                    float side=sim.CaptureSideOffsets().Max(v=>v.magnitude);result.sideImpulsePeak=Mathf.Max(result.sideImpulsePeak,side);
                    if(frame==42)result.sideResidualAfterStop=side;if(frame==360)result.sideSettledResidual=side;
                    var rendered=skin.sharedMesh.vertices;
                    for(int i=0;i<groups.Length;i++)if(groups[i]>=0) {
                        float d=(rendered[i]-neutral[i]).magnitude;
                        if(groups[i]==6){result.tuftPeak=Mathf.Max(result.tuftPeak,d);if(tuftWeight[i]==0)result.tuftRootMaximum=Mathf.Max(result.tuftRootMaximum,d);if(frame==42)result.tuftAfterStop=Mathf.Max(result.tuftAfterStop,d);if(frame==360)result.tuftSettled=Mathf.Max(result.tuftSettled,d);}
                        else {
                            int g=groups[i];result.frontGroupPeak[g]=Mathf.Max(result.frontGroupPeak[g],d);
                            float free=(float)frontFree.Invoke(front,new object[]{i,neutral[i]});
                            if(free==0)result.frontRootMaximum=Mathf.Max(result.frontRootMaximum,d);
                            if(frame==42){result.frontAfterStop=Mathf.Max(result.frontAfterStop,d);result.frontGroupAfterStop[g]=Mathf.Max(result.frontGroupAfterStop[g],d);}
                            if(frame==360)result.frontSettled=Mathf.Max(result.frontSettled,d);
                        }
                    }
                    foreach(var n in skin.sharedMesh.normals)Require(!float.IsNaN(n.x)&&Mathf.Abs(n.magnitude-1)<1e-4f,"finite unit transported normal");
                    for(int i=0;i<selected.Length;i++)if(selected[i]>=0) {
                        float d=(rendered[i]-neutral[i]).magnitude;
                        if(neutral[i].y==top[selected[i]])result.sideRootMaximum=Mathf.Max(result.sideRootMaximum,d);
                        if(frame==42&&neutral[i].y>.45f&&neutral[i].y<.65f)result.sideRenderedMiddleAfterStop=Mathf.Max(result.sideRenderedMiddleAfterStop,d);
                        if(frame==42&&neutral[i].y<.40f)result.sideRenderedTipAfterStop=Mathf.Max(result.sideRenderedTipAfterStop,d);
                    }
                }
                Require(result.frontGroupPeak.All(v=>v>.010f)&&result.frontGroupAfterStop.All(v=>v>.006f)&&result.tuftPeak>.015f&&result.tuftAfterStop>.006f,"every cheek-side domain and tuft must have perceptible head-driven inertia");
                Require(result.frontRootMaximum==0&&result.tuftRootMaximum==0&&result.frontAfterStop>.0005f&&result.tuftAfterStop>.0005f&&result.tuftPeak>.001f&&result.frontSettled<.002f&&result.tuftSettled<.002f,"new front/tuft inertia, fixed roots and settling");
                Require(result.impulsePeak>.005f&&result.residualAfterStop>.001f,"real motion must create a continuing inertial response after stop");
                Require(result.sideRootMaximum==0&&result.sideRenderedMiddleAfterStop>.002f&&result.sideRenderedTipAfterStop>.002f,"side middle/tip must bend after stop while both roots stay exact");
                Require(result.sideImpulsePeak>.005f&&result.sideResidualAfterStop>.001f&&result.sideSettledResidual<.002f,"side curls must have independent inertia and settle");
                Require(result.settledResidual<.002f,"a held pose must settle instead of looping indefinitely");
                int resets=sim.ResetCount;c.model.localPosition+=Vector3.right;sim.Tick(110+1f/60,1);result.teleportReset=sim.ResetCount==resets+1&&Magnitude(sim)<1e-6f&&sim.CaptureSideOffsets().All(v=>v.magnitude<1e-6f);Require(result.teleportReset,"teleport must reset safely");
                resets=sim.ResetCount;sim.Tick(112,1);result.longFrameReset=sim.ResetCount==resets+1&&Magnitude(sim)<1e-6f&&sim.CaptureSideOffsets().All(v=>v.magnitude<1e-6f);Require(result.longFrameReset,"long frame/pause must reset safely");
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
                    samples[run]=sim.CaptureOffsets().Concat(sim.CaptureSideOffsets()).Concat(c.model.GetComponentsInChildren<SkinnedMeshRenderer>().Single(x=>x.sharedMesh.name=="Body").sharedMesh.vertices).ToArray();Require(sim.SafetyResets==0,"frame-rate test fallback");
                    if(run==1) {
                        var sides=typeof(FishHairFollow).GetField("sideStrands",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(sim);
                        var sideField=typeof(FishHairFollow).GetMethod("SideField",BindingFlags.NonPublic|BindingFlags.Instance);
                        Func<int,Vector3,Vector3[]> sideEvaluate=(side,p)=>{object[] args={side,p,Vector3.zero,Vector3.zero};sideField.Invoke(sim,args);return new[]{(Vector3)args[2],(Vector3)args[3]};};
                        for(int side=0;side<2;side++)foreach(float y in new[]{.32f,.43f,.56f,.68f,.78f}) {
                            var p=new Vector3(side==0?-.28f:.28f,y,-.13f);const float e=.0001f;
                            var derivative=(sideEvaluate(side,p+Vector3.up*e)[0]-sideEvaluate(side,p-Vector3.up*e)[0])/(2*e);
                            result.sideDerivativeMaximumError=Mathf.Max(result.sideDerivativeMaximumError,(derivative-sideEvaluate(side,p)[1]).magnitude);
                        }
                        var frontField=typeof(FishHairFollow).GetMethod("FrontField",BindingFlags.NonPublic|BindingFlags.Instance);
                        Func<int,Vector3,Vector3[]> evaluateFront=(vertex,p)=>{object[] args={vertex,p,Vector3.zero,Vector3.zero};frontField.Invoke(sim,args);return new[]{(Vector3)args[2],(Vector3)args[3]};};
                        foreach(int vertex in new[]{804,847,876,1531,1577,1622})foreach(float y in new[]{.49f,.61f,.72f}) {
                            var p=new Vector3(0,y,0);const float e=.0001f;
                            var derivative=(evaluateFront(vertex,p+Vector3.up*e)[0]-evaluateFront(vertex,p-Vector3.up*e)[0])/(2*e);
                            result.frontDerivativeMaximumError=Mathf.Max(result.frontDerivativeMaximumError,(derivative-evaluateFront(vertex,p)[1]).magnitude);
                        }
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
            Require(result.frontDerivativeMaximumError<.002f,"actual sideburn derivative finite difference");Require(result.frameRateMaximumDifference<.006f,"30/60/120fps response diverged");Require(result.sideDerivativeMaximumError<.002f,"side normal transport derivative must match finite differences");Require(result.derivativeMaximumError<.002f,"field derivatives must match independent finite differences");
            result.averageTickMilliseconds=(float)(ticks*1000.0/System.Diagnostics.Stopwatch.Frequency/count);
            Directory.CreateDirectory("work/hair-front-217");File.WriteAllText("work/hair-front-217/Behavior-Tests.json",JsonUtility.ToJson(result,true));
            Debug.Log("Stateful hair behavior tests passed: "+JsonUtility.ToJson(result));
        }
    }
}
