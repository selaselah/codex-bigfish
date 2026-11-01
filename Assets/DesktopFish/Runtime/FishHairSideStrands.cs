using System;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using UnityEngine;

namespace DesktopFish {
    // Two complete side/back curls, each with its own carrier and rest curve.
    // The front locks are deliberately outside these connected physical domains.
    internal sealed class FishHairSideStrands {
        const int Nodes=7;
        readonly Transform[] bones;
        readonly Matrix4x4[] binds;
        readonly Vector3[,] rest=new Vector3[2,Nodes],position=new Vector3[2,Nodes],velocity=new Vector3[2,Nodes],offset=new Vector3[2,Nodes];
        readonly Matrix4x4[] previous=new Matrix4x4[2];
        readonly float[] top=new float[2],spacing=new float[2];
        float previousTime;
        bool initialized;
        public readonly int[] VertexStrands;
        public int DomainCount {get;private set;}
        public float MaximumOffset {get;private set;}
        public Vector3[] CaptureOffsets()=>Enumerable.Range(0,2*Nodes).Select(i=>offset[i/Nodes,i%Nodes]).ToArray();
        public FishHairSideStrands(Mesh mesh,Transform[] bones) {
            this.bones=bones;binds=mesh.bindposes;
            Vector3[] p=mesh.vertices;var tri=mesh.triangles;
            VertexStrands=Enumerable.Repeat(-1,p.Length).ToArray();
            // Authored complete outer/return domains, carried through the fixed
            // 5K reduction. Some of those domains now contain disconnected cells.
            using(var stream=new MemoryStream()) {
                using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true)) {
                    foreach(var v in p){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}foreach(int index in tri)writer.Write(index);
                }
                using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-","").ToLowerInvariant()!="be2aa114dcbdf94c4948903bc6972cdcbcb835132ffd329a3c3a0cd9b41b3b6c")throw new InvalidOperationException("Side-strand recipe does not match the neutral body.");
            }
            int[][] domains={new[]{730,731,732,733,734,735,736,737,738,739,740,741,742,743,744,745,746,747,748,749,750,751,752,753,754,755,756,757,758,759,760,761,762,763,764,765,766,767,768,769,770,771,772,773,774,775,776,777,778,779,780,781,782,783,784,785,786,787,788,789,790,791,792,793,794,795,796,797,798,799,800,801,802,803},new[]{1442,1443,1444,1445,1446,1447,1448,1449,1450,1451,1452,1453,1454,1455,1456,1457,1458,1459,1460,1461,1462,1463,1464,1465,1466,1467,1468,1469,1470,1471,1472,1473,1474,1475,1476,1477,1478,1479,1480,1481,1482,1483,1484,1485,1486,1487,1488,1489,1490,1491,1492,1493,1494,1495,1496,1497,1498,1499,1500,1501,1502,1503,1504,1505,1506,1507,1508,1509,1510,1511,1512,1513,1514,1515,1516,1517,1518,1519,1520,1521,1522,1523,1524,1525,1526,1527,1528,1529,1530}};
            for(int side=0;side<2;side++)foreach(int i in domains[side])VertexStrands[i]=side;
            DomainCount=4;
            for(int c=0;c<2;c++) {
                var indices=Enumerable.Range(0,p.Length).Where(i=>VertexStrands[i]==c).ToArray();
                top[c]=indices.Max(i=>p[i].y);spacing[c]=(top[c]-indices.Min(i=>p[i].y))/(Nodes-1);
                for(int node=0;node<Nodes;node++) {
                    float y=top[c]-node*spacing[c],total=0;Vector3 center=Vector3.zero;
                    foreach(int i in indices){float w=Mathf.Exp(-Mathf.Pow((p[i].y-y)/.045f,2));center+=p[i]*w;total+=w;}
                    rest[c,node]=new Vector3(center.x/total,y,center.z/total);
                }
            }
        }
        Matrix4x4 Carrier(int c){int bone=c==0?5:9;return bones[bone].localToWorldMatrix*binds[bone];}
        public float FreeWeight(int vertex,Vector3 p)=>VertexStrands[vertex]<0?0:Mathf.SmoothStep(0,1,Mathf.InverseLerp(top[VertexStrands[vertex]],top[VertexStrands[vertex]]-.16f,p.y));
        public void Tick(float time) {
            float elapsed=time-previousTime;
            bool reset=!initialized||elapsed<0||elapsed>.15f;
            for(int c=0;c<2;c++)if(initialized&&(Carrier(c).MultiplyPoint3x4(rest[c,0])-previous[c].MultiplyPoint3x4(rest[c,0])).magnitude>.30f)reset=true;
            int steps=reset?0:Mathf.CeilToInt(elapsed/(1f/120));float dt=steps>0?elapsed/steps:0;
            for(int c=0;c<2;c++) {
                Matrix4x4 carrier=Carrier(c);
                if(reset)for(int i=0;i<Nodes;i++){position[c,i]=carrier.MultiplyPoint3x4(rest[c,i]);velocity[c,i]=offset[c,i]=Vector3.zero;}
                for(int step=1;step<=steps;step++) {
                    float alpha=step/(float)steps;
                    position[c,0]=Vector3.Lerp(previous[c].MultiplyPoint3x4(rest[c,0]),carrier.MultiplyPoint3x4(rest[c,0]),alpha);
                    for(int i=1;i<Nodes;i++) {
                        Vector3 target=Vector3.Lerp(previous[c].MultiplyPoint3x4(rest[c,i]),carrier.MultiplyPoint3x4(rest[c,i]),alpha);
                        Vector3 parentTarget=Vector3.Lerp(previous[c].MultiplyPoint3x4(rest[c,i-1]),carrier.MultiplyPoint3x4(rest[c,i-1]),alpha);
                        Vector3 before=position[c,i];float u=(i-1)/(float)(Nodes-2);
                        velocity[c,i]+=(target-before)*Mathf.Lerp(65,20,u)*dt;
                        velocity[c,i]*=Mathf.Exp(-Mathf.Lerp(3.8f,2.5f,u)*dt);
                        Vector3 candidate=before+velocity[c,i]*dt,restDirection=target-parentTarget;
                        Vector3 direction=FishHairFollow.ConstrainDirection(candidate-position[c,i-1],restDirection);
                        position[c,i]=position[c,i-1]+direction*restDirection.magnitude;
                        velocity[c,i]=(position[c,i]-before)/dt;
                        MaximumOffset=Mathf.Max(MaximumOffset,(position[c,i]-target).magnitude);
                    }
                }
                for(int i=0;i<Nodes;i++)offset[c,i]=carrier.inverse.MultiplyVector(position[c,i]-carrier.MultiplyPoint3x4(rest[c,i]));
                for(int i=1;i<Nodes;i++) {
                    Vector3 d=offset[c,i]-offset[c,i-1];float h=spacing[c];
                    offset[c,i]=offset[c,i-1]+new Vector3(Mathf.Clamp(d.x,-h*.45f,h*.45f),Mathf.Clamp(d.y,-h*.24f,h*.24f),Mathf.Clamp(d.z,-h*.45f,h*.45f));
                }
                previous[c]=carrier;
            }
            initialized=true;previousTime=time;
        }
        public void Field(int c,Vector3 p,out Vector3 value,out Vector3 derivative) {
            float u=(top[c]-p.y)/spacing[c];
            if(u<=0){value=derivative=Vector3.zero;return;}
            if(u>=Nodes-1){value=offset[c,Nodes-1];derivative=Vector3.zero;return;}
            int i=Mathf.FloorToInt(u);float t=u-i,t2=t*t,t3=t2*t;
            Vector3 a=offset[c,i],b=offset[c,i+1],ma=i==0?Vector3.zero:(b-offset[c,i-1])*.5f,mb=i+1==Nodes-1?Vector3.zero:(offset[c,i+2]-a)*.5f;
            value=(2*t3-3*t2+1)*a+(t3-2*t2+t)*ma+(-2*t3+3*t2)*b+(t3-t2)*mb;
            derivative=-((6*t2-6*t)*a+(3*t2-4*t+1)*ma+(-6*t2+6*t)*b+(3*t2-2*t)*mb)/spacing[c];
        }
    }
}
