using System;
using System.Linq;
using UnityEngine;

namespace DesktopFish {
    // Stateful, head-driven virtual spring chains. No clock/crank waveform is
    // used as a force. The existing skin/rig and neutral mesh remain untouched.
    public sealed class FishHairFollow : IDisposable {
        const int Chains=3,Nodes=5;
        const float RootY=.74f,Spacing=.125f,Width=.24f,MaxStep=1f/120,LocalMotion=.12f;
        sealed class Part {
            public SkinnedMeshRenderer renderer;
            public Mesh original,mesh;
            public Vector3[] positions,normals,animatedPositions,animatedNormals;
            public bool[] hair;
            public bool[] legacySide;
            public int[] triangles,activeFaces,strands,front;
            public Vector3[] restCross,beforeProjectionCross,normalCorrection;
            public float[] inverseMass,normalWeight;
        }
        readonly Part[] parts;
        readonly FishHairSideStrands sideStrands;
        readonly FishFrontHairFollow frontHair;
        public int SideDomainCount=>sideStrands?.DomainCount??0;
        public Vector3[] CaptureSideOffsets()=>sideStrands?.CaptureOffsets()??new Vector3[0];
        readonly SkinnedMeshRenderer body;
        readonly Transform[] bones;
        readonly Matrix4x4[] binds;
        Vector3 neckA,neckB,chestA,chestB;
        readonly Vector3[,] rest=new Vector3[Chains,Nodes],position=new Vector3[Chains,Nodes],velocity=new Vector3[Chains,Nodes],offset=new Vector3[Chains,Nodes];
        readonly Vector3[] coherent=new Vector3[Nodes];
        Matrix4x4 previousHead;
        float previousTime;
        bool initialized;
        readonly bool centralOnly;
        public float MaximumParticleOffset {get;private set;}
        public float MinimumJacobian {get;private set;}=1;
        public float MaximumLengthError {get;private set;}
        public int CollisionProjections {get;private set;}
        public int ResetCount {get;private set;}
        public float MaximumStrainCorrection {get;private set;}
        public float MaximumMeshCorrection {get;private set;}
        public int SafetyResets {get;private set;}
        public bool Active=>parts.Length!=0;
        public Vector3[] CaptureOffsets()=>Enumerable.Range(0,Chains*Nodes).Select(i=>offset[i/Nodes,i%Nodes]).ToArray();
        public FishHairFollow(Transform model,bool centralOnly=false) {
            this.centralOnly=centralOnly;
            var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();
            if(!skins.Any(s=>s.sharedMesh.name=="RearHairCards")){parts=new Part[0];return;}
            body=skins.Single(s=>s.sharedMesh.name=="Body");
            bones=body.bones;binds=body.sharedMesh.bindposes;
            if(!centralOnly){sideStrands=new FishHairSideStrands(body.sharedMesh,bones);frontHair=new FishFrontHairFollow(body.sharedMesh);}
            parts=skins.Where(s=>s.sharedMesh.name=="Body"||s.sharedMesh.name=="RearHairCards").Select(s=>{
                var source=s.sharedMesh;bool card=source.name=="RearHairCards";
                var part=new Part{renderer=s,original=source,mesh=UnityEngine.Object.Instantiate(source),positions=source.vertices,normals=source.normals};
                part.mesh.name=source.name;part.mesh.MarkDynamic();part.animatedPositions=(Vector3[])part.positions.Clone();part.animatedNormals=(Vector3[])part.normals.Clone();
                // Rear selection stays unchanged. New front/tuft domains are
                // selected separately; cards retain the accepted rear field.
                part.hair=source.boneWeights.Select((w,i)=>card?(!centralOnly||(i>=15&&i<30)):new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3}.Any(j=>centralOnly?j==7:j>=6&&j<=8)).ToArray();
                part.strands=card||sideStrands==null?Enumerable.Repeat(-1,part.positions.Length).ToArray():sideStrands.VertexStrands;
                part.front=card||frontHair==null?Enumerable.Repeat(-1,part.positions.Length).ToArray():frontHair.Groups;
                part.legacySide=source.boneWeights.Select((w,i)=>part.strands[i]<0&&!card&&new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3}.Any(j=>j==5||j==9)).ToArray();
                part.triangles=source.triangles;part.activeFaces=Enumerable.Range(0,part.triangles.Length/3).Where(f=>Enumerable.Range(0,3).Any(j=>(part.hair[part.triangles[3*f+j]]||part.strands[part.triangles[3*f+j]]>=0||part.front[part.triangles[3*f+j]]>=0))).ToArray();
                part.restCross=new Vector3[part.triangles.Length/3];part.beforeProjectionCross=new Vector3[part.restCross.Length];
                part.inverseMass=part.positions.Select((p,i)=>part.hair[i]?Mathf.SmoothStep(0,1,Mathf.InverseLerp(RootY,.30f,p.y)):(part.front[i]>=0?frontHair.FreeWeight(i,p):(!card&&sideStrands!=null?sideStrands.FreeWeight(i,p):0))).ToArray();
                part.normalCorrection=new Vector3[part.positions.Length];part.normalWeight=new float[part.positions.Length];
                foreach(int f in part.activeFaces){int a=part.triangles[3*f],b=part.triangles[3*f+1],c=part.triangles[3*f+2];part.restCross[f]=Vector3.Cross(part.positions[b]-part.positions[a],part.positions[c]-part.positions[a]);}
                s.sharedMesh=part.mesh;return part;
            }).ToArray();
            var support=parts.SelectMany(p=>p.positions.Where((q,i)=>p.hair[i]&&q.z<-.08f)).ToArray();
            for(int chain=0;chain<Chains;chain++)for(int node=0;node<Nodes;node++) {
                float x=(chain-1)*Width,y=RootY-node*Spacing,total=0,z=0;
                foreach(var p in support) {float w=Mathf.Exp(-Mathf.Pow((p.x-x)/.14f,2)-Mathf.Pow((p.y-y)/.11f,2));total+=w;z+=w*p.z;}
                if(total<1e-7f)throw new InvalidOperationException("Rear spring chain has no own-mesh support.");
                rest[chain,node]=new Vector3(x,y,z/total);
            }
        }
        Matrix4x4 HeadMatrix()=>bones[4].localToWorldMatrix*binds[4];
        void Reset(Matrix4x4 head,float time) {
            for(int c=0;c<Chains;c++)for(int i=0;i<Nodes;i++){position[c,i]=head.MultiplyPoint3x4(rest[c,i]);velocity[c,i]=offset[c,i]=Vector3.zero;}
            Array.Clear(coherent,0,coherent.Length);
            previousHead=head;previousTime=time;initialized=true;ResetCount++;
        }
        // Sphere/capsule proxies follow the head, neck and chest independently.
        Vector3 ProjectCollision(Vector3 p,Vector3 a,Vector3 b,float radius) {
            Vector3 ab=b-a;float t=ab.sqrMagnitude>1e-10f?Mathf.Clamp01(Vector3.Dot(p-a,ab)/ab.sqrMagnitude):0;
            Vector3 center=a+ab*t,d=p-center;float distance=d.magnitude;
            if(distance>=radius)return p;
            CollisionProjections++;return center+(distance>1e-7f?d/distance:Vector3.back)*radius;
        }
        Vector3 Collide(Vector3 p,Matrix4x4 head) {
            Vector3 center=head.MultiplyPoint3x4(new Vector3(0,.77f,-.04f));
            p=ProjectCollision(p,center,center,.19f);
            p=ProjectCollision(p,neckA,neckB,.074f);
            return ProjectCollision(p,chestA,chestB,.105f);
        }
        public void Tick(float time,float intensity,float sideWorkWeight=0) {
            if(parts.Length==0)return;
            var neck=bones[3].localToWorldMatrix*binds[3];var chest=bones[2].localToWorldMatrix*binds[2];
            neckA=neck.MultiplyPoint3x4(new Vector3(0,.52f,0));neckB=neck.MultiplyPoint3x4(new Vector3(0,.65f,0));
            chestA=chest.MultiplyPoint3x4(new Vector3(-.14f,.49f,0));chestB=chest.MultiplyPoint3x4(new Vector3(.14f,.49f,0));
            Matrix4x4 head=HeadMatrix();float elapsed=time-previousTime;
            if(!initialized||elapsed<0||elapsed>.15f||(head.MultiplyPoint3x4(rest[1,0])-previousHead.MultiplyPoint3x4(rest[1,0])).magnitude>.30f)Reset(head,time);
            else if(elapsed>0) {
                int steps=Mathf.CeilToInt(elapsed/MaxStep);float dt=elapsed/steps;
                for(int step=1;step<=steps;step++) {
                    float alpha=step/(float)steps;
                    for(int c=0;c<Chains;c++) {
                        position[c,0]=Vector3.Lerp(previousHead.MultiplyPoint3x4(rest[c,0]),head.MultiplyPoint3x4(rest[c,0]),alpha);
                        for(int i=1;i<Nodes;i++) {
                            Vector3 target=Vector3.Lerp(previousHead.MultiplyPoint3x4(rest[c,i]),head.MultiplyPoint3x4(rest[c,i]),alpha);
                            Vector3 parentTarget=Vector3.Lerp(previousHead.MultiplyPoint3x4(rest[c,i-1]),head.MultiplyPoint3x4(rest[c,i-1]),alpha);
                            Vector3 before=position[c,i];float stiffness=Mathf.Lerp(70,22,(i-1)/3f)*(1+(c-1)*.07f),drag=Mathf.Lerp(3.8f,2.6f,(i-1)/3f);
                            velocity[c,i]+=(target-position[c,i])*stiffness*dt;
                            velocity[c,i]*=Mathf.Exp(-drag*dt);
                            Vector3 p=before+velocity[c,i]*dt,restDirection=target-parentTarget;float length=restDirection.magnitude;
                            // Distance and bending constraints keep the cartoon lock
                            // intact while its intermediate sections flex independently.
                            for(int iteration=0;iteration<6;iteration++) {
                                Vector3 direction=ConstrainDirection(p-position[c,i-1],restDirection);
                                p=position[c,i-1]+direction*length;
                                p=Collide(p,head);
                            }
                            position[c,i]=p;velocity[c,i]=(p-before)/dt;
                            MaximumLengthError=Mathf.Max(MaximumLengthError,Mathf.Abs((p-position[c,i-1]).magnitude-length));
                            MaximumParticleOffset=Mathf.Max(MaximumParticleOffset,(p-target).magnitude);
                        }
                    }
                }
                var inverse=head.inverse;
                for(int c=0;c<Chains;c++)for(int i=0;i<Nodes;i++)offset[c,i]=inverse.MultiplyVector(position[c,i]-head.MultiplyPoint3x4(rest[c,i]));
                LimitTransferStrain();
                SynchronizeTransfer();
                previousHead=head;previousTime=time;
            }
            sideStrands?.Tick(time);
            if(frontHair!=null){Sample(-1,RootY-.30f,out Vector3 common,out Vector3 derivative);frontHair.Tick(time,head,common*.24f);}
            if(!Apply(Mathf.Clamp01(intensity),time,sideWorkWeight)) {SafetyResets++;Reset(head,time);Apply(0,time,sideWorkWeight);}
        }
        internal static Vector3 ConstrainDirection(Vector3 direction,Vector3 restDirection) {
            Vector3 rest=restDirection.normalized;direction=direction.normalized;
            const float cosine=.8480481f,sine=.5299193f;float dot=Vector3.Dot(rest,direction);
            // Keep small motions exactly; only project beyond the 32deg cone.
            if(dot>=cosine)return direction;
            Vector3 tangent=direction-rest*dot;
            if(tangent.sqrMagnitude<1e-12f)tangent=Vector3.Cross(rest,Mathf.Abs(rest.x)<.9f?Vector3.right:Vector3.up);
            return rest*cosine+tangent.normalized*sine;
        }
        void LimitTransferStrain() {
            // The physical proxies have more freedom than the old sparse mesh.
            // Limit local stretch/shear when transferring their motion to it.
            for(int sweep=0;sweep<4;sweep++) {
                for(int c=0;c<Chains;c++)for(int i=1;i<Nodes;i++) {
                    Vector3 old=offset[c,i],d=old-offset[c,i-1];
                    d=new Vector3(Mathf.Clamp(d.x,-Spacing*.4f,Spacing*.4f),Mathf.Clamp(d.y,-Spacing*.28f,Spacing*.28f),Mathf.Clamp(d.z,-Spacing*.45f,Spacing*.45f));
                    offset[c,i]=offset[c,i-1]+d;MaximumStrainCorrection=Mathf.Max(MaximumStrainCorrection,(old-offset[c,i]).magnitude);
                }
                for(int c=1;c<Chains;c++)for(int i=1;i<Nodes;i++) {
                    Vector3 old=offset[c,i],d=old-offset[c-1,i];
                    d=new Vector3(Mathf.Clamp(d.x,-Width*.22f,Width*.22f),Mathf.Clamp(d.y,-Width*.12f,Width*.12f),Mathf.Clamp(d.z,-Width*.22f,Width*.22f));
                    offset[c,i]=offset[c-1,i]+d;MaximumStrainCorrection=Mathf.Max(MaximumStrainCorrection,(old-offset[c,i]).magnitude);
                }
            }
        }
        void SynchronizeTransfer() {
            // A dominant shared mode keeps the complete hair mass together.
            // Local spring responses contribute only a small secondary motion.
            for(int i=0;i<Nodes;i++)coherent[i]=(offset[0,i]+2*offset[1,i]+offset[2,i])*.25f;
            for(int c=0;c<Chains;c++)for(int i=0;i<Nodes;i++)offset[c,i]=Vector3.Lerp(coherent[i],offset[c,i],LocalMotion);
        }
        Vector3 Control(int chain,int node)=>chain<0?coherent[node]:offset[chain,node];
        void SideField(int strand,Vector3 p,out Vector3 value,out Vector3 derivative) {
            sideStrands.Field(strand,p,out Vector3 local,out Vector3 localDerivative);
            float span=sideStrands.Span(strand),u=sideStrands.Progress(strand,p.y),mainSpan=Spacing*(Nodes-1);
            Sample(-1,RootY-u*mainSpan,out Vector3 common,out Vector3 commonDerivative);
            // Only displacement is shared. Each curl keeps its own root/rest
            // curve; no rear particle position is used as a side-hair target.
            value=Vector3.Lerp(common*(span/mainSpan),local,LocalMotion);
            derivative=Vector3.Lerp(commonDerivative,localDerivative,LocalMotion);
        }
        void Sample(int chain,float y,out Vector3 value,out Vector3 derivative) {
            if(y>=RootY){value=derivative=Vector3.zero;return;}
            float u=(RootY-y)/Spacing;
            if(u>=Nodes-1){value=Control(chain,Nodes-1);derivative=Vector3.zero;return;}
            int i=Mathf.FloorToInt(u);float t=u-i,t2=t*t,t3=t2*t;
            Vector3 a=Control(chain,i),b=Control(chain,i+1);
            Vector3 ma=i==0?Vector3.zero:(b-Control(chain,i-1))*.5f;
            Vector3 mb=i+1==Nodes-1?Vector3.zero:(Control(chain,i+2)-a)*.5f;
            value=(2*t3-3*t2+1)*a+(t3-2*t2+t)*ma+(-2*t3+3*t2)*b+(t3-t2)*mb;
            derivative=-((6*t2-6*t)*a+(3*t2-4*t+1)*ma+(-6*t2+6*t)*b+(3*t2-2*t)*mb)/Spacing;
        }
        void Field(Vector3 p,out Vector3 d,out Vector3 dx,out Vector3 dy) {
            if(centralOnly){Sample(1,p.y,out d,out dy);dx=Vector3.zero;return;}
            float u=Mathf.Clamp01((p.x<0?p.x+Width:p.x)/Width),s=u*u*(3-2*u),ds=6*u*(1-u)/Width;
            if(Mathf.Abs(p.x)>=Width)ds=0;
            int a=p.x<0?0:1,b=a+1;
            Sample(a,p.y,out Vector3 da,out Vector3 ya);Sample(b,p.y,out Vector3 db,out Vector3 yb);
            d=Vector3.Lerp(da,db,s);dx=(db-da)*ds;dy=Vector3.Lerp(ya,yb,s);
        }
        void FrontField(int vertex,Vector3 p,out Vector3 d,out Vector3 gradient) {
            if(frontHair.Groups[vertex]==6){d=frontHair.TuftDelta*frontHair.TuftWeight[vertex];gradient=frontHair.TuftGradient[vertex];return;}
            float u=frontHair.Progress(vertex,p.y),span=frontHair.Span(vertex);
            Sample(-1,RootY-u*Spacing*(Nodes-1),out d,out Vector3 dy);
            d*=.32f*span/(Spacing*(Nodes-1));dy*=.32f;
            FishFrontHairFollow.SoftLimit(ref d,ref dy,.018f);gradient=dy;
        }
        bool Apply(float intensity,float time,float sideWorkWeight) {
            foreach(var part in parts) {
                for(int i=0;i<part.positions.Length;i++) {
                    Vector3 p=part.positions[i],n=part.normals[i];
                    if(part.hair[i]&&p.y<RootY&&intensity>0) {
                        Field(p,out Vector3 d,out Vector3 dx,out Vector3 dy);d*=intensity;dx*=intensity;dy*=intensity;
                        Vector3 x=Vector3.right+dx,y=Vector3.up+dy,z=Vector3.forward;
                        float determinant=Vector3.Dot(x,Vector3.Cross(y,z));MinimumJacobian=Mathf.Min(MinimumJacobian,determinant);
                        if(determinant<=.15f||!Finite(d))return false;
                        p+=d;n=(Vector3.Cross(y,z)*n.x+Vector3.Cross(z,x)*n.y+Vector3.Cross(x,y)*n.z).normalized;
                    }
                    else if(part.strands[i]>=0&&intensity>0) {
                        SideField(part.strands[i],p,out Vector3 d,out Vector3 dy);d*=intensity;dy*=intensity;
                        float determinant=1+dy.y;MinimumJacobian=Mathf.Min(MinimumJacobian,determinant);
                        if(determinant<=.15f||!Finite(d))return false;
                        p+=d;n=new Vector3(n.x*determinant,n.y-dy.x*n.x-dy.z*n.z,n.z*determinant).normalized;
                    }
                    else if(part.front[i]>=0&&intensity>0) {
                        FrontField(i,p,out Vector3 d,out Vector3 gradient);d*=intensity;
                        Vector3 direction=part.front[i]==6?frontHair.TuftDelta*intensity:gradient*intensity;
                        if(part.front[i]!=6)gradient=Vector3.up;
                        float determinant=1+Vector3.Dot(direction,gradient);MinimumJacobian=Mathf.Min(MinimumJacobian,determinant);
                        if(determinant<=.15f||!Finite(d))return false;
                        p+=d;n=(n*determinant-gradient*Vector3.Dot(direction,n)).normalized;
                    }
                    else if(part.legacySide[i]&&p.y<.80f&&sideWorkWeight>.0001f) {
                        // Preserve the accepted212 side-curl response. Front locks
                        // need their own anchors/colliders before receiving physics.
                        float length=.80f-p.y,phase=FishWorkStation.CrankAngle(time)-.45f-1.8f*length;
                        float x=(.10f*Mathf.Sin(phase)+.016f*Mathf.Sin(2*phase-.3f))*sideWorkWeight,z=.045f*Mathf.Sin(phase-.55f)*sideWorkWeight;
                        float dxdy=-x+length*1.8f*(.10f*Mathf.Cos(phase)+.032f*Mathf.Cos(2*phase-.3f))*sideWorkWeight,dzdy=-z+length*1.8f*.045f*Mathf.Cos(phase-.55f)*sideWorkWeight;
                        p.x+=x*length;p.z+=z*length;n.y-=dxdy*n.x+dzdy*n.z;n.Normalize();
                    }
                    part.animatedPositions[i]=p;part.animatedNormals[i]=n;
                }
                ProjectMesh(part);
                part.mesh.vertices=part.animatedPositions;part.mesh.normals=part.animatedNormals;part.mesh.RecalculateBounds();
            }
            return true;
        }
        void ProjectMesh(Part part) {
            var p=part.animatedPositions;var t=part.triangles;
            foreach(int f in part.activeFaces)part.beforeProjectionCross[f]=Vector3.Cross(p[t[3*f+1]]-p[t[3*f]],p[t[3*f+2]]-p[t[3*f]]);
            // Sparse QEM triangles can span several control sections. A PBD
            // orientation/area constraint prevents such cells turning inside out.
            for(int iteration=0;iteration<16;iteration++) {
                bool corrected=false;
                foreach(int f in part.activeFaces) {
                    int a=t[3*f],b=t[3*f+1],c=t[3*f+2];Vector3 reference=part.restCross[f];float restArea=reference.magnitude;
                    if(restArea<1e-12f)continue;
                    Vector3 normal=reference/restArea;
                    for(int constraint=0;constraint<2;constraint++) {
                        Vector3 cross=Vector3.Cross(p[b]-p[a],p[c]-p[a]);float area=cross.magnitude;
                        float value=Vector3.Dot(cross,normal)-(constraint==0?restArea*.25f:area*.70f);
                        if(value>=0)continue;
                        Vector3 gradient=constraint==0?normal:normal-cross.normalized*.70f;
                        Vector3 ga=Vector3.Cross(p[b]-p[c],gradient),gb=Vector3.Cross(p[c]-p[a],gradient),gc=Vector3.Cross(p[a]-p[b],gradient);
                        float denominator=part.inverseMass[a]*ga.sqrMagnitude+part.inverseMass[b]*gb.sqrMagnitude+part.inverseMass[c]*gc.sqrMagnitude;
                        if(denominator<1e-18f)continue;
                        float lambda=-value/denominator;
                        Vector3 da=ga*(lambda*part.inverseMass[a]),db=gb*(lambda*part.inverseMass[b]),dc=gc*(lambda*part.inverseMass[c]);
                        p[a]+=da;p[b]+=db;p[c]+=dc;MaximumMeshCorrection=Mathf.Max(MaximumMeshCorrection,Mathf.Max(da.magnitude,Mathf.Max(db.magnitude,dc.magnitude)));corrected=true;
                    }
                }
                if(!corrected)break;
            }
            Array.Clear(part.normalCorrection,0,part.normalCorrection.Length);Array.Clear(part.normalWeight,0,part.normalWeight.Length);
            foreach(int f in part.activeFaces) {
                int a=t[3*f],b=t[3*f+1],c=t[3*f+2];Vector3 before=part.beforeProjectionCross[f],after=Vector3.Cross(p[b]-p[a],p[c]-p[a]);
                if(before.sqrMagnitude<1e-18f||after.sqrMagnitude<1e-18f)continue;
                before.Normalize();after.Normalize();Vector3 axis=Vector3.Cross(before,after);float sine=axis.magnitude;
                if(sine<1e-7f)continue;
                Vector3 rotation=axis*(Mathf.Atan2(sine,Vector3.Dot(before,after))/sine);
                for(int corner=0;corner<3;corner++){int i=t[3*f+corner],j=t[3*f+(corner+1)%3],k=t[3*f+(corner+2)%3];float angle=Vector3.Angle(p[j]-p[i],p[k]-p[i])*Mathf.Deg2Rad;part.normalCorrection[i]+=rotation*angle;part.normalWeight[i]+=angle;}
            }
            for(int i=0;i<p.Length;i++)if(part.inverseMass[i]>0&&part.normalWeight[i]>0) {
                Vector3 rotation=part.normalCorrection[i]/part.normalWeight[i];float angle=rotation.magnitude;
                if(angle>1e-7f)part.animatedNormals[i]=(Quaternion.AngleAxis(angle*Mathf.Rad2Deg,rotation/angle)*part.animatedNormals[i]).normalized;
            }
        }
        static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z);
        public void Dispose() {
            foreach(var part in parts) {
                if(part.renderer&&part.renderer.sharedMesh==part.mesh)part.renderer.sharedMesh=part.original;
                if(Application.isPlaying)UnityEngine.Object.Destroy(part.mesh);else UnityEngine.Object.DestroyImmediate(part.mesh);
            }
        }
    }
}
