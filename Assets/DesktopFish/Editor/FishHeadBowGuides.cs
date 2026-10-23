using UnityEngine;
namespace DesktopFish.Editor {
    // Independent low-density curved bow patches and newly painted alpha edges.
    // Only sparse physical positions calibrate these projection frames/contours.
    public static class FishHeadBowGuides {
        public const int Rows=9,Columns=4;
        static readonly float[] Stations={0,.15f,.30f,.42f,.50f,.58f,.70f,.85f,1};
        static readonly Vector3[] Origins={new Vector3(0.228326935f,0.768150849f,0.079968150f),new Vector3(-0.291023982f,0.753571692f,0.001986086f)};
        static readonly Vector3[] Across={new Vector3(0.063725428f,-0.010712893f,-0.080625993f),new Vector3(0.060083177f,0.010953530f,0.073557786f)};
        static readonly Vector3[] Up={new Vector3(-0.003541399f,0.063647992f,-0.011256064f),new Vector3(0.001689468f,0.063608052f,-0.010851894f)};
        static readonly Vector3[] Front={new Vector3(0.785262048f,0.149931699f,0.600736260f),new Vector3(-0.777418792f,0.125789016f,0.616276801f)};
        static readonly Vector3[] Focus={new Vector3(0.261113215f,0.792375736f,0.031064968f),new Vector3(-0.261399616f,0.789192028f,0.032086046f)};
        static readonly float[][] DepthCoefficients={new[]{0.004001834f,0.000516400f,0.000116050f,0.001893743f,-0.006681801f,-0.000591553f,-0.000253686f,-0.000837131f},new[]{0.003561205f,0.000405061f,-0.001100796f,-0.000310816f,-0.005648263f,0.000145793f,0.000512018f,0.001044020f}};
        static readonly Vector3[][] Contours={
            new[]{
                new Vector3(0.000099999f,0.731880844f,0.739121027f),
                new Vector3(0.033426671f,0.523255047f,0.838288931f),
                new Vector3(0.066753344f,0.424628268f,0.858442063f),
                new Vector3(0.100080005f,0.334195584f,0.868519963f),
                new Vector3(0.133406689f,0.190695996f,0.876133134f),
                new Vector3(0.166733339f,0.100727099f,0.877702246f),
                new Vector3(0.200060012f,0.039744176f,0.874173587f),
                new Vector3(0.233386684f,0.001679732f,0.868442211f),
                new Vector3(0.266713357f,-0.000000509f,0.861155718f),
                new Vector3(0.300040029f,0.025456671f,0.848649964f),
                new Vector3(0.333366679f,0.051951389f,0.834632397f),
                new Vector3(0.366693352f,0.091309320f,0.807987751f),
                new Vector3(0.400020024f,0.142839006f,0.778514274f),
                new Vector3(0.433346697f,0.199293869f,0.746432197f),
                new Vector3(0.466673370f,0.249428311f,0.708697758f),
                new Vector3(0.500000042f,0.329188871f,0.703489108f),
                new Vector3(0.533326715f,0.354641973f,0.707816834f),
                new Vector3(0.566653342f,0.361085128f,0.710767887f),
                new Vector3(0.599980060f,0.361807307f,0.713888975f),
                new Vector3(0.633306688f,0.351215238f,0.779811597f),
                new Vector3(0.666633360f,0.298061414f,0.835298907f),
                new Vector3(0.699959988f,0.254027740f,0.880604655f),
                new Vector3(0.733286750f,0.221759162f,0.911605323f),
                new Vector3(0.766613333f,0.188914470f,0.940263134f),
                new Vector3(0.799940005f,0.155760552f,0.963051128f),
                new Vector3(0.833266678f,0.131321071f,0.980384923f),
                new Vector3(0.866593351f,0.115997112f,0.992939763f),
                new Vector3(0.899920023f,0.105704498f,1.000000437f),
                new Vector3(0.933246696f,0.108839622f,0.995970411f),
                new Vector3(0.966573368f,0.154906619f,0.968846629f),
                new Vector3(0.999900041f,0.753019075f,0.767491254f),
            },
            new[]{
                new Vector3(0.000100042f,0.710223158f,0.732971444f),
                new Vector3(0.033426739f,0.187635964f,0.961573450f),
                new Vector3(0.066753339f,0.115599355f,0.995054956f),
                new Vector3(0.100080036f,0.100182043f,0.999999948f),
                new Vector3(0.133406733f,0.109464861f,0.995715001f),
                new Vector3(0.166733333f,0.124147360f,0.984737377f),
                new Vector3(0.200060030f,0.143956847f,0.970818237f),
                new Vector3(0.233386727f,0.164710878f,0.950043095f),
                new Vector3(0.266713375f,0.192715019f,0.925212976f),
                new Vector3(0.300040024f,0.224086312f,0.895559674f),
                new Vector3(0.333366721f,0.267414678f,0.861105338f),
                new Vector3(0.366693370f,0.313663774f,0.821435697f),
                new Vector3(0.400020018f,0.353257070f,0.756225783f),
                new Vector3(0.433346715f,0.348292054f,0.702647571f),
                new Vector3(0.466673364f,0.343235888f,0.697573629f),
                new Vector3(0.500000012f,0.333487476f,0.692036397f),
                new Vector3(0.533326709f,0.292642266f,0.683637575f),
                new Vector3(0.566653358f,0.226407113f,0.696113723f),
                new Vector3(0.599980055f,0.172943246f,0.738685407f),
                new Vector3(0.633306655f,0.130823701f,0.770949158f),
                new Vector3(0.666633352f,0.090400770f,0.799711432f),
                new Vector3(0.699960000f,0.055860048f,0.824606402f),
                new Vector3(0.733286698f,0.033975531f,0.844678899f),
                new Vector3(0.766613322f,0.014362054f,0.862403865f),
                new Vector3(0.799940019f,-0.000000336f,0.875972778f),
                new Vector3(0.833266643f,0.001183746f,0.883351076f),
                new Vector3(0.866593340f,0.032454473f,0.888801067f),
                new Vector3(0.899919976f,0.101572920f,0.889417937f),
                new Vector3(0.933246674f,0.270558651f,0.886173485f),
                new Vector3(0.966573310f,0.473684293f,0.870914038f),
                new Vector3(0.999900001f,0.720471440f,0.729177584f),
            },
        };
        static int Index(int sign)=>sign>0?0:1;
        public static Vector3 Center(int sign)=>Focus[Index(sign)];
        public static Vector3 Normal(int sign)=>Front[Index(sign)];
        public static float Station(int row)=>Stations[row];
        public static Vector3 Point(int sign,float u,float v) {
            int side=Index(sign);float x=2*u-1,y=2*v-1,d=0,power=1;
            for(int i=0;i<4;i++){d+=power*(DepthCoefficients[side][i*2]+y*DepthCoefficients[side][i*2+1]);power*=x;}
            return Origins[side]+Across[side]*u+Up[side]*v+Front[side]*d;
        }
        static float Slope(float a,float b,float ha,float hb) {
            if(a*b<=0)return 0;return 3*(ha+hb)/((2*hb+ha)/a+(hb+2*ha)/b);
        }
        public static Vector2 Bounds(int sign,float u) {
            var row=Contours[Index(sign)];u=Mathf.Clamp01(u);int i=0;while(i<row.Length-2&&row[i+1].x<u)i++;
            float h=row[i+1].x-row[i].x,t=(u-row[i].x)/h,t2=t*t,t3=t2*t;var result=Vector2.zero;
            for(int k=1;k<3;k++) {
                float a=row[i][k],b=row[i+1][k],d=(b-a)/h,ma=d,mb=d;
                if(i>0){float hp=row[i].x-row[i-1].x;ma=Slope((a-row[i-1][k])/hp,d,hp,h);}
                if(i<row.Length-2){float hn=row[i+2].x-row[i+1].x;mb=Slope(d,(row[i+2][k]-b)/hn,h,hn);}
                result[k-1]=(2*t3-3*t2+1)*a+(t3-2*t2+t)*h*ma+(-2*t3+3*t2)*b+(t3-t2)*h*mb;
            }
            return result;
        }
        public static int TileX(int sign)=>sign>0?16:848;
        public const int TileY=1104,TileWidth=160,TileHeight=176;
        public static Vector2 UV(int sign,float u,float v)=>new Vector2((TileX(sign)+4+152*u)/1024f,(TileY+4+168*v-128)/1024f);
        public static bool InChart(Vector2 uv,int sign)=>uv.x>=(TileX(sign)+3f)/1024&&uv.x<=(TileX(sign)+157f)/1024&&uv.y>=(TileY+3f)/2048&&uv.y<=(TileY+173f)/2048;
    }
}
