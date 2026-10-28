using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DesktopFish
{
    // Runtime-only cartoon completion effect. The station owns all meshes.
    internal sealed class FishRiceBurst
    {
        public const float Duration=3.8f;
        public const int GrainCount=64;
        const int Sides=6,SeedVertices=20;
        const float RiceLift=.051f,ServingX=.020f,ServingZ=.045f;
        public readonly Transform Root;
        public readonly float Started;
        readonly Transform shell,lidPivot,puff,mound,bowl;
        readonly Mesh rice,puffMesh,shadow;
        readonly Vector3[] vertices=new Vector3[GrainCount*SeedVertices],normals=new Vector3[GrainCount*SeedVertices];
        readonly Vector3[] seedPoints=new Vector3[SeedVertices],seedNormals=new Vector3[SeedVertices];
        readonly Vector3[] starts=new Vector3[GrainCount],lands=new Vector3[GrainCount];
        readonly Quaternion[] turns=new Quaternion[GrainCount],rests=new Quaternion[GrainCount];
        readonly float[] heights=new float[GrainCount],sizes=new float[GrainCount];
        readonly Color[] puffColors,shadowColors;
        readonly float floor;

        static float Hash(int n) { unchecked {uint v=(uint)n;v^=v>>16;v*=0x7feb352d;v^=v>>15;v*=0x846ca68b;v^=v>>16;return(v&0xffffff)/16777215f;} }

        public FishRiceBurst(Transform cooker,Transform model,Transform lid,Material solid,Material vapor,Func<Mesh,Mesh> own,int seed,float time)
        {
            Started=time;Root=new GameObject("Completed rice burst "+seed).transform;
            Root.SetParent(cooker.parent,false);Root.localPosition=cooker.localPosition;Root.localRotation=cooker.localRotation;Root.localScale=cooker.localScale;
            Root.gameObject.SetActive(false);
            floor=(Mathf.Max(FishWorkStation.GeneratorFloorHeight+.004f,cooker.localPosition.y-.02f*cooker.localScale.y)-cooker.localPosition.y)/cooker.localScale.y;
            shell=model;shell.SetParent(Root,false);shell.localPosition=Vector3.zero;shell.localRotation=Quaternion.identity;
            var steam=shell.Find("Steam");if(steam)steam.gameObject.SetActive(false);
            lidPivot=new GameObject("Popping cooker lid").transform;lidPivot.SetParent(Root,false);lidPivot.localPosition=Vector3.up*.09f;
            lid.SetParent(lidPivot,false);lid.localPosition=Vector3.down*.09f;lid.localRotation=Quaternion.identity;

            seedPoints[0]=Vector3.down*.009f;seedPoints[19]=Vector3.up*.009f;
            for(int row=0;row<3;row++)for(int j=0;j<Sides;j++) {
                float a=j*Mathf.PI*2/Sides,r=row==1?.0029f:.0021f;
                seedPoints[1+row*Sides+j]=new Vector3(Mathf.Cos(a)*r,(row-1)*.0055f,Mathf.Sin(a)*r*.68f);
            }
            for(int i=0;i<SeedVertices;i++){var p=seedPoints[i];seedNormals[i]=new Vector3(p.x/(.0029f*.0029f),p.y/(.009f*.009f),p.z/(.0020f*.0020f)).normalized;}
            var triangles=new int[GrainCount*36*3];var colors=new Color[vertices.Length];int index=0;
            Action<int,int,int> face=(a,b,c)=>{triangles[index++]=a;triangles[index++]=b;triangles[index++]=c;};
            for(int grain=0;grain<GrainCount;grain++) {
                int key=seed*977+grain*31,n=grain*SeedVertices;
                float a=Hash(key)*Mathf.PI*2,r=Mathf.Sqrt(Hash(key+1))*.040f;
                starts[grain]=new Vector3((Hash(key+2)-.5f)*.026f,.060f+Hash(key+3)*.025f,(Hash(key+4)-.5f)*.025f);
                lands[grain]=new Vector3(ServingX+Mathf.Cos(a)*r,floor+RiceLift+.006f+.033f*Mathf.Pow(Mathf.Max(0,1-r*r/(.040f*.040f)),.7f)+Hash(key+5)*.006f,ServingZ+Mathf.Sin(a)*r*.72f);
                heights[grain]=.075f+Hash(key+6)*.075f;sizes[grain]=.78f+Hash(key+7)*.38f;
                turns[grain]=Quaternion.Euler(Hash(key+8)*360,Hash(key+9)*360,Hash(key+10)*360);
                rests[grain]=Quaternion.Euler(82+Hash(key+11)*16,Hash(key+12)*360,-8+Hash(key+13)*16);
                Color c=new Color(.985f+Hash(key+14)*.015f,.975f+Hash(key+15)*.025f,.94f+Hash(key+16)*.04f);
                for(int v=0;v<SeedVertices;v++)colors[n+v]=c;
                for(int j=0;j<Sides;j++)face(n,n+1+j,n+1+(j+1)%Sides);
                for(int row=0;row<2;row++)for(int j=0;j<Sides;j++) {
                    int a0=n+1+row*Sides+j,b0=n+1+row*Sides+(j+1)%Sides;
                    face(a0,a0+Sides,b0+Sides);face(a0,b0+Sides,b0);
                }
                for(int j=0;j<Sides;j++)face(n+19,n+13+(j+1)%Sides,n+13+j);
            }
            rice=own(new Mesh{name="Burst rice grains"});rice.MarkDynamic();rice.vertices=vertices;rice.triangles=triangles;rice.colors=colors;
            Part(Root,"Flying rice and settled pile",rice,solid);

            // A filled rice mound prevents the settled grains reading as confetti.
            const int moundSides=16;var mv=new Vector3[50];var mt=new int[96*3];var mc=new Color[mv.Length];int mi=0;
            mv[0]=Vector3.up*.033f;mv[49]=Vector3.up*.001f;
            for(int row=0;row<3;row++)for(int j=0;j<moundSides;j++) {
                float r=(row+1)/3f,a=j*Mathf.PI*2/moundSides;
                mv[1+row*moundSides+j]=new Vector3(Mathf.Cos(a)*.049f*r,.003f+.030f*Mathf.Pow(Mathf.Max(0,1-r*r),.7f),Mathf.Sin(a)*.036f*r);
            }
            Action<int,int,int> moundFace=(a,b,c)=>{mt[mi++]=a;mt[mi++]=b;mt[mi++]=c;};
            for(int j=0;j<moundSides;j++)moundFace(0,1+(j+1)%moundSides,1+j);
            for(int row=0;row<2;row++)for(int j=0;j<moundSides;j++) {
                int a=1+row*moundSides+j,b=1+row*moundSides+(j+1)%moundSides;
                moundFace(a,b,b+moundSides);moundFace(a,b+moundSides,a+moundSides);
            }
            for(int j=0;j<moundSides;j++)moundFace(49,33+j,33+(j+1)%moundSides);
            for(int j=0;j<mc.Length;j++)mc[j]=new Color(.94f,.93f,.87f);
            var moundMesh=own(new Mesh{name="Filled rice mound"});moundMesh.vertices=mv;moundMesh.triangles=mt;moundMesh.colors=mc;moundMesh.RecalculateNormals();moundMesh.RecalculateBounds();
            mound=Part(Root,"Rice mound",moundMesh,solid);

            // Thick rolled rim, open inner wall and a small porcelain foot.
            Vector2[] profile={new Vector2(.021f,.001f),new Vector2(.026f,.004f),new Vector2(.026f,.009f),new Vector2(.030f,.012f),new Vector2(.043f,.025f),new Vector2(.055f,.043f),new Vector2(.061f,.057f),new Vector2(.062f,.060f),new Vector2(.060f,.063f),new Vector2(.055f,.061f),new Vector2(.053f,.057f),new Vector2(.046f,.041f),new Vector2(.032f,.022f),new Vector2(.022f,.017f)};
            const int bowlSides=32;var bv=new Vector3[profile.Length*bowlSides+2];var bc=new Color[bv.Length];var bt=new int[((profile.Length-1)*bowlSides*2+bowlSides*2)*3];int bi=0;
            Color trim=new Color(.17f,.29f,.40f),porcelain=new Color(.88f,.94f,.98f),inside=new Color(.94f,.97f,1);
            for(int row=0;row<profile.Length;row++)for(int j=0;j<bowlSides;j++) {
                float a=j*Mathf.PI*2/bowlSides;int n=row*bowlSides+j;bv[n]=new Vector3(Mathf.Cos(a)*profile[row].x,profile[row].y,Mathf.Sin(a)*profile[row].x);
                bc[n]=row<3||(row>=6&&row<=9)?trim:row>=10?inside:porcelain;
            }
            Action<int,int,int> bowlFace=(a,b,c)=>{bt[bi++]=a;bt[bi++]=b;bt[bi++]=c;};
            for(int row=0;row<profile.Length-1;row++)for(int j=0;j<bowlSides;j++) {
                int a=row*bowlSides+j,b=row*bowlSides+(j+1)%bowlSides;bowlFace(a,a+bowlSides,b+bowlSides);bowlFace(a,b+bowlSides,b);
            }
            int bottom=bv.Length-2,interior=bv.Length-1;bv[bottom]=Vector3.up*profile[0].y;bc[bottom]=trim;bv[interior]=Vector3.up*profile[profile.Length-1].y;bc[interior]=inside;
            for(int j=0;j<bowlSides;j++){bowlFace(bottom,j,(j+1)%bowlSides);bowlFace(interior,(profile.Length-1)*bowlSides+(j+1)%bowlSides,(profile.Length-1)*bowlSides+j);}
            var bowlMesh=own(new Mesh{name="Serving rice bowl"});bowlMesh.vertices=bv;bowlMesh.triangles=bt;bowlMesh.colors=bc;bowlMesh.RecalculateNormals();bowlMesh.RecalculateBounds();
            bowl=Part(Root,"Rice serving bowl",bowlMesh,solid);bowl.localPosition=new Vector3(ServingX,floor,ServingZ);

            var pv=new Vector3[6*17];var pt=new int[6*16*3];puffColors=new Color[pv.Length];
            for(int k=0;k<6;k++) {
                int n=k*17;float angle=k*Mathf.PI*2/6;Vector3 center=new Vector3(Mathf.Cos(angle)*.022f,.075f+Mathf.Sin(angle)*.018f,.055f);
                pv[n]=center;
                for(int j=0;j<16;j++){float a=j*Mathf.PI*2/16;pv[n+1+j]=center+new Vector3(Mathf.Cos(a)*.022f,Mathf.Sin(a)*.020f,0);int t=(k*16+j)*3;pt[t]=n;pt[t+1]=n+1+j;pt[t+2]=n+1+(j+1)%16;}
            }
            puffMesh=own(new Mesh{name="Rice completion puff"});puffMesh.vertices=pv;puffMesh.triangles=pt;puffMesh.RecalculateNormals();puff=Part(Root,"Rice poof",puffMesh,vapor);
            var sv=new Vector3[25];var st=new int[24*3];sv[0]=new Vector3(ServingX,floor+.001f,ServingZ);shadowColors=new Color[sv.Length];
            for(int j=0;j<24;j++){float a=j*Mathf.PI*2/24;sv[j+1]=sv[0]+new Vector3(Mathf.Cos(a)*.061f,0,Mathf.Sin(a)*.044f);st[j*3]=0;st[j*3+1]=j+1;st[j*3+2]=(j+1)%24+1;}
            shadow=own(new Mesh{name="Rice pile soft shadow"});shadow.vertices=sv;shadow.triangles=st;shadow.RecalculateNormals();Part(Root,"Rice pile grounding",shadow,vapor);
            Tick(time);Root.gameObject.SetActive(true);
        }

        static Transform Part(Transform parent,string name,Mesh mesh,Material material) {
            var t=new GameObject(name).transform;t.SetParent(parent,false);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return t;
        }

        public void Tick(float time)
        {
            float age=Mathf.Max(0,time-Started),flight=Mathf.Clamp01((age-.055f)/.78f),fade=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((age-3.1f)/.7f));
            float shrink=Mathf.Max(.08f,fade);
            float servingScale=Mathf.Max(.08f,Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.43f)/.43f))*shrink);
            mound.gameObject.SetActive(age>.43f);mound.localScale=Vector3.one*servingScale;mound.localPosition=new Vector3(ServingX,floor+RiceLift*servingScale,ServingZ);
            bowl.gameObject.SetActive(age>.43f);bowl.localScale=Vector3.one*servingScale;
            shell.gameObject.SetActive(age<.15f);shell.localScale=new Vector3(1+.10f*Mathf.Sin(Mathf.Clamp01(age/.15f)*Mathf.PI),1-.10f*Mathf.Sin(Mathf.Clamp01(age/.15f)*Mathf.PI),1);
            lidPivot.gameObject.SetActive(age<.65f);float toss=Mathf.Clamp01(age/.65f);
            lidPivot.localPosition=new Vector3(-.023f*toss,.09f+.12f*Mathf.Sin(toss*Mathf.PI*.8f),0);
            lidPivot.localRotation=Quaternion.Euler(0,0,-110*toss);lidPivot.localScale=Vector3.one*Mathf.Max(.08f,1-toss*toss);
            for(int grain=0;grain<GrainCount;grain++) {
                Vector3 landing=new Vector3(ServingX+(lands[grain].x-ServingX)*shrink,floor+(lands[grain].y-floor)*shrink,ServingZ+(lands[grain].z-ServingZ)*shrink);
                Vector3 radial=new Vector3(lands[grain].x-ServingX,0,lands[grain].z-ServingZ);
                Vector3 center=Vector3.Lerp(starts[grain],landing,flight)+Vector3.up*(4*heights[grain]*flight*(1-flight))+radial.normalized*(.022f*Mathf.Sin(flight*Mathf.PI));
                Quaternion rotation=Quaternion.Slerp(turns[grain],rests[grain],flight)*Quaternion.Euler(0,0,170*flight*(1-flight));
                for(int v=0;v<SeedVertices;v++){int n=grain*SeedVertices+v;vertices[n]=center+rotation*seedPoints[v]*(sizes[grain]*shrink);normals[n]=rotation*seedNormals[v];}
            }
            rice.vertices=vertices;rice.normals=normals;rice.RecalculateBounds();
            puff.gameObject.SetActive(age<.36f);puff.localScale=Vector3.one*(.6f+Mathf.Clamp01(age/.36f)*1.25f);
            for(int i=0;i<puffColors.Length;i++)puffColors[i]=new Color(1,.98f,.90f,.65f*Mathf.Max(0,1-age/.36f));puffMesh.colors=puffColors;
            for(int i=0;i<shadowColors.Length;i++)shadowColors[i]=new Color(.64f,.59f,.47f,.18f*flight*fade);shadow.colors=shadowColors;
        }
    }
}
