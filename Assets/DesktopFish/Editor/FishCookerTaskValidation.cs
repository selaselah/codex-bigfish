using System;
using System.Linq;
using UnityEngine;

namespace DesktopFish.Editor
{
    // Production prop/collider regression in an isolated editor process.
    public static class FishCookerTaskValidation
    {
        static void Require(bool value,string message) { if(!value)throw new InvalidOperationException(message); }
        public static void Run()
        {
            var parent=new GameObject("Cooker task fixture");
            var model=new GameObject("Model").transform;model.SetParent(parent.transform,false);
            var ids=new[]{"00000000-0000-4000-8000-000000000001","00000000-0000-4000-8000-000000000002","00000000-0000-4000-8000-000000000003"};
            try { using(var station=new FishWorkStation(model,Shader.Find("DesktopFish/WorkProps"),121,0)) {
                station.Tick(1,1,3,true);station.BindTaskIds(ids);Physics.SyncTransforms();
                var original=Enumerable.Range(0,3).Select(station.CookerHitAreaAt).ToArray();
                station.BindTaskIds(new[]{ids[2],ids[0],ids[1]});
                for(int i=0;i<3;i++)Require(station.CookerTaskIdAt(i)==ids[i],"Recency reorder must preserve cooker identity");
                var styles=Enumerable.Range(0,3).Select(station.CookerStyleAt).ToArray();
                for(int frame=0;frame<8;frame++) {
                    station.Tick(1+frame*.17f,1,3,true);Physics.SyncTransforms();
                    for(int i=0;i<3;i++) {
                        var box=(BoxCollider)station.CookerHitAreaAt(i);
                        Vector3 center=box.transform.TransformPoint(box.center);
                        var ray=new Ray(center+Vector3.forward,Vector3.back);
                        Require(station.TryGetCookerTask(ray,out string task)&&task==ids[i],"Actual wobbling pot ray must resolve its own task");
                    }
                }
                // A complete snapshot identifies the removed task, not just the last pot.
                station.CompleteCookers(1,3,new[]{ids[0],ids[2]});
                station.Tick(3,1,2,true);station.BindTaskIds(new[]{ids[2],ids[0]});Physics.SyncTransforms();
                Require(station.CookerCount==2&&station.CookerTaskIdAt(0)==ids[0]&&station.CookerTaskIdAt(1)==ids[2],"Completion keeps the surviving task IDs");
                Require(station.CookerHitAreaAt(0)==original[0]&&station.CookerHitAreaAt(1)==original[2],"Completion preserves surviving cookers and styles");
                station.BindTaskIds(new[]{ids[2]});Require(station.CookerTaskIdAt(0)==null&&station.CookerTaskIdAt(1)==ids[2],"Expired IDs cannot remain clickable as another task");
                station.Tick(4,0,0);Physics.SyncTransforms();
                Require(!station.TryGetCookerTask(new Ray(Vector3.forward,Vector3.back),out _),"Hidden/finished cookers have no live hit target");
            }} finally { UnityEngine.Object.DestroyImmediate(parent); }
            Debug.Log("PASS: cooker task stable IDs/reorder/animated ray hits/specific completion/survivor identity/expiry/hidden hit targets");
        }
    }
}
