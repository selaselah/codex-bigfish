using UnityEngine;

namespace DesktopFish.Editor {
    // Separate branch contours retain the fork opening. Only sparse section
    // controls are stored; all surface samples/topology are generated anew.
    public static class FishRearForkGuides {
        static readonly Vector3[][] Rows = {
            new[] { // component 0, trunk
                new Vector3(0.1511571f,0.8075001f,-0.2681970f),new Vector3(0.1880076f,0.8075001f,-0.2351530f),new Vector3(0.1906199f,0.8075001f,-0.1861833f),new Vector3(0.2331103f,0.8075001f,-0.1557627f),new Vector3(0.2334216f,0.8075001f,-0.1057226f),
                new Vector3(0.1556311f,0.7074974f,-0.2815806f),new Vector3(0.1916220f,0.7074974f,-0.2509752f),new Vector3(0.2203541f,0.7074974f,-0.2167857f),new Vector3(0.2386515f,0.7074974f,-0.1774440f),new Vector3(0.2422377f,0.7074974f,-0.1308386f),
                new Vector3(0.143897f,0.607500f,-0.288979f),new Vector3(0.180902f,0.607500f,-0.260721f),new Vector3(0.211690f,0.607500f,-0.229355f),new Vector3(0.226452f,0.607500f,-0.189974f),new Vector3(0.228741f,0.607500f,-0.144357f),
                new Vector3(0.139431f,0.507499f,-0.294786f),new Vector3(0.180415f,0.507499f,-0.268736f),new Vector3(0.211276f,0.507499f,-0.237547f),new Vector3(0.225907f,0.507499f,-0.198117f),new Vector3(0.227378f,0.507499f,-0.152007f),
                new Vector3(0.168279f,0.420000f,-0.312679f),new Vector3(0.206911f,0.420000f,-0.289489f),new Vector3(0.206331f,0.420000f,-0.244924f),new Vector3(0.249282f,0.420000f,-0.224090f),new Vector3(0.248976f,0.420000f,-0.179674f),
            },
            new[] { // component 0, branch 0
                new Vector3(0.168279f,0.420000f,-0.312679f),new Vector3(0.187595f,0.420000f,-0.301084f),new Vector3(0.206911f,0.420000f,-0.289489f),new Vector3(0.206621f,0.420000f,-0.267207f),new Vector3(0.206331f,0.420000f,-0.244924f),
                new Vector3(0.176960f,0.395000f,-0.326973f),new Vector3(0.201207f,0.395000f,-0.305421f),new Vector3(0.221755f,0.395000f,-0.282021f),new Vector3(0.226966f,0.395000f,-0.250960f),new Vector3(0.232177f,0.395000f,-0.219899f),
                new Vector3(0.185447f,0.370001f,-0.341870f),new Vector3(0.209841f,0.370001f,-0.320010f),new Vector3(0.228753f,0.370001f,-0.295605f),new Vector3(0.233362f,0.370001f,-0.264560f),new Vector3(0.237837f,0.370001f,-0.233453f),
                new Vector3(0.192899f,0.345001f,-0.355931f),new Vector3(0.216300f,0.345001f,-0.334143f),new Vector3(0.234951f,0.345001f,-0.310145f),new Vector3(0.239741f,0.345001f,-0.279696f),new Vector3(0.244079f,0.345001f,-0.249036f),
                new Vector3(0.197504f,0.320001f,-0.366941f),new Vector3(0.220543f,0.320001f,-0.346489f),new Vector3(0.239626f,0.320001f,-0.324037f),new Vector3(0.245332f,0.320001f,-0.294827f),new Vector3(0.250108f,0.320001f,-0.265147f),
                new Vector3(0.199070f,0.294999f,-0.373899f),new Vector3(0.221775f,0.294999f,-0.355433f),new Vector3(0.241523f,0.294999f,-0.335262f),new Vector3(0.248253f,0.294999f,-0.307588f),new Vector3(0.254467f,0.294999f,-0.279617f),
                new Vector3(0.197160f,0.270000f,-0.376687f),new Vector3(0.219889f,0.270000f,-0.360363f),new Vector3(0.240029f,0.270000f,-0.342371f),new Vector3(0.247489f,0.270000f,-0.316203f),new Vector3(0.254950f,0.270000f,-0.290035f),
                new Vector3(0.189175f,0.245000f,-0.373112f),new Vector3(0.213537f,0.245000f,-0.360971f),new Vector3(0.234216f,0.245000f,-0.345964f),new Vector3(0.242572f,0.245000f,-0.321366f),new Vector3(0.250910f,0.245000f,-0.296755f),
                new Vector3(0.162323f,0.220000f,-0.345755f),new Vector3(0.191219f,0.220000f,-0.349987f),new Vector3(0.216608f,0.220000f,-0.348316f),new Vector3(0.228995f,0.220000f,-0.324764f),new Vector3(0.240777f,0.220000f,-0.300195f),
                new Vector3(0.149568f,0.195000f,-0.340751f),new Vector3(0.167485f,0.195000f,-0.332169f),new Vector3(0.185294f,0.195000f,-0.323428f),new Vector3(0.199738f,0.195000f,-0.309731f),new Vector3(0.214238f,0.195000f,-0.296115f),
                new Vector3(0.151349f,0.176250f,-0.351620f),new Vector3(0.163339f,0.176250f,-0.341675f),new Vector3(0.171503f,0.176250f,-0.329444f),new Vector3(0.176765f,0.176250f,-0.315478f),new Vector3(0.183632f,0.176250f,-0.302472f),
                new Vector3(0.162558f,0.163750f,-0.358502f),new Vector3(0.169428f,0.163750f,-0.353745f),new Vector3(0.173051f,0.163750f,-0.347176f),new Vector3(0.174871f,0.163750f,-0.339601f),new Vector3(0.176692f,0.163750f,-0.332027f),
                new Vector3(0.178813f,0.154605f,-0.374522f),new Vector3(0.178813f,0.154605f,-0.374522f),new Vector3(0.178813f,0.154605f,-0.374522f),new Vector3(0.178813f,0.154605f,-0.374522f),new Vector3(0.178813f,0.154605f,-0.374522f),
            },
            new[] { // component 0, branch 1
                new Vector3(0.206331f,0.420000f,-0.244924f),new Vector3(0.227806f,0.420000f,-0.234507f),new Vector3(0.249282f,0.420000f,-0.224090f),new Vector3(0.249129f,0.420000f,-0.201882f),new Vector3(0.248976f,0.420000f,-0.179674f),
                new Vector3(0.217582f,0.394999f,-0.297372f),new Vector3(0.241457f,0.394999f,-0.275302f),new Vector3(0.259988f,0.394999f,-0.251411f),new Vector3(0.258763f,0.394999f,-0.220786f),new Vector3(0.257486f,0.394999f,-0.190143f),
                new Vector3(0.230386f,0.370001f,-0.306797f),new Vector3(0.253764f,0.370001f,-0.285358f),new Vector3(0.271179f,0.370001f,-0.261955f),new Vector3(0.270538f,0.370001f,-0.232601f),new Vector3(0.269470f,0.370001f,-0.203107f),
                new Vector3(0.242680f,0.345000f,-0.315133f),new Vector3(0.263967f,0.345000f,-0.294330f),new Vector3(0.280796f,0.345000f,-0.271991f),new Vector3(0.281044f,0.345000f,-0.243940f),new Vector3(0.280652f,0.345000f,-0.215668f),
                new Vector3(0.250757f,0.319999f,-0.320367f),new Vector3(0.270227f,0.319999f,-0.300603f),new Vector3(0.286242f,0.319999f,-0.279545f),new Vector3(0.287212f,0.319999f,-0.252859f),new Vector3(0.288182f,0.319999f,-0.226174f),
                new Vector3(0.250613f,0.294999f,-0.320463f),new Vector3(0.268919f,0.294999f,-0.302337f),new Vector3(0.284876f,0.294999f,-0.283200f),new Vector3(0.287164f,0.294999f,-0.258183f),new Vector3(0.289487f,0.294999f,-0.233180f),
                new Vector3(0.243905f,0.269999f,-0.316190f),new Vector3(0.262662f,0.269999f,-0.300522f),new Vector3(0.278868f,0.269999f,-0.283636f),new Vector3(0.281431f,0.269999f,-0.260237f),new Vector3(0.284242f,0.269999f,-0.236957f),
                new Vector3(0.233086f,0.245001f,-0.308980f),new Vector3(0.251680f,0.245001f,-0.295903f),new Vector3(0.267693f,0.245001f,-0.281462f),new Vector3(0.270172f,0.245001f,-0.259867f),new Vector3(0.272898f,0.245001f,-0.238403f),
                new Vector3(0.219782f,0.220000f,-0.299415f),new Vector3(0.236385f,0.220000f,-0.288761f),new Vector3(0.250899f,0.220000f,-0.276894f),new Vector3(0.253129f,0.220000f,-0.257901f),new Vector3(0.255800f,0.220000f,-0.239164f),
                new Vector3(0.205422f,0.195000f,-0.288452f),new Vector3(0.219049f,0.195000f,-0.280253f),new Vector3(0.230737f,0.195000f,-0.270861f),new Vector3(0.232931f,0.195000f,-0.255629f),new Vector3(0.234721f,0.195000f,-0.240149f),
                new Vector3(0.202903f,0.176250f,-0.283382f),new Vector3(0.209610f,0.176250f,-0.274903f),new Vector3(0.214986f,0.176250f,-0.265960f),new Vector3(0.216063f,0.176250f,-0.255523f),new Vector3(0.217155f,0.176250f,-0.245090f),
                new Vector3(0.214904f,0.163750f,-0.284018f),new Vector3(0.219474f,0.163750f,-0.279090f),new Vector3(0.221997f,0.163750f,-0.273678f),new Vector3(0.221984f,0.163750f,-0.267665f),new Vector3(0.221835f,0.163750f,-0.261619f),
                new Vector3(0.233671f,0.155133f,-0.294208f),new Vector3(0.233671f,0.155133f,-0.294208f),new Vector3(0.233671f,0.155133f,-0.294208f),new Vector3(0.233671f,0.155133f,-0.294208f),new Vector3(0.233671f,0.155133f,-0.294208f),
            },
            new[] { // component 1, trunk
                new Vector3(-0.2311303f,0.8074980f,-0.1128539f),new Vector3(-0.2298963f,0.8074980f,-0.1593747f),new Vector3(-0.2012161f,0.8074980f,-0.1941760f),new Vector3(-0.1880246f,0.8074980f,-0.2355909f),new Vector3(-0.1493680f,0.8074980f,-0.2661321f),
                new Vector3(-0.2405792f,0.7075013f,-0.1329610f),new Vector3(-0.2372544f,0.7075013f,-0.1792455f),new Vector3(-0.2196755f,0.7075013f,-0.2178990f),new Vector3(-0.1916759f,0.7075013f,-0.2509738f),new Vector3(-0.1535375f,0.7075013f,-0.2786209f),
                new Vector3(-0.228900f,0.607498f,-0.144540f),new Vector3(-0.226228f,0.607498f,-0.189055f),new Vector3(-0.200294f,0.607498f,-0.222216f),new Vector3(-0.181900f,0.607498f,-0.259057f),new Vector3(-0.144329f,0.607498f,-0.286539f),
                new Vector3(-0.228452f,0.507498f,-0.152947f),new Vector3(-0.226144f,0.507498f,-0.198944f),new Vector3(-0.210940f,0.507498f,-0.237272f),new Vector3(-0.181314f,0.507498f,-0.267019f),new Vector3(-0.139670f,0.507498f,-0.289619f),
                new Vector3(-0.248752f,0.419999f,-0.178936f),new Vector3(-0.217984f,0.419999f,-0.209307f),new Vector3(-0.206991f,0.419999f,-0.251217f),new Vector3(-0.192250f,0.419999f,-0.290940f),new Vector3(-0.153850f,0.419999f,-0.316858f),
            },
            new[] { // component 1, branch 0
                new Vector3(-0.248752f,0.419999f,-0.178936f),new Vector3(-0.233368f,0.419999f,-0.194122f),new Vector3(-0.217984f,0.419999f,-0.209307f),new Vector3(-0.212487f,0.419999f,-0.230262f),new Vector3(-0.206991f,0.419999f,-0.251217f),
                new Vector3(-0.256575f,0.395001f,-0.189258f),new Vector3(-0.256938f,0.395001f,-0.219685f),new Vector3(-0.257112f,0.395001f,-0.250051f),new Vector3(-0.239355f,0.395001f,-0.274572f),new Vector3(-0.216535f,0.395001f,-0.297443f),
                new Vector3(-0.265684f,0.370000f,-0.200592f),new Vector3(-0.266043f,0.370000f,-0.230564f),new Vector3(-0.266232f,0.370000f,-0.260474f),new Vector3(-0.248317f,0.370000f,-0.283894f),new Vector3(-0.225832f,0.370000f,-0.305676f),
                new Vector3(-0.275094f,0.344999f,-0.212104f),new Vector3(-0.275069f,0.344999f,-0.240742f),new Vector3(-0.275175f,0.344999f,-0.269424f),new Vector3(-0.258184f,0.344999f,-0.292363f),new Vector3(-0.236507f,0.344999f,-0.313727f),
                new Vector3(-0.283484f,0.319999f,-0.223300f),new Vector3(-0.282724f,0.319999f,-0.250695f),new Vector3(-0.282167f,0.319999f,-0.278164f),new Vector3(-0.265647f,0.319999f,-0.299772f),new Vector3(-0.245897f,0.319999f,-0.320193f),
                new Vector3(-0.290216f,0.295000f,-0.233672f),new Vector3(-0.288779f,0.295000f,-0.259587f),new Vector3(-0.287045f,0.295000f,-0.285387f),new Vector3(-0.271975f,0.295000f,-0.306059f),new Vector3(-0.253278f,0.295000f,-0.325338f),
                new Vector3(-0.290363f,0.270001f,-0.241332f),new Vector3(-0.288955f,0.270001f,-0.266028f),new Vector3(-0.287136f,0.270001f,-0.290543f),new Vector3(-0.270746f,0.270001f,-0.308631f),new Vector3(-0.252332f,0.270001f,-0.325827f),
                new Vector3(-0.285242f,0.245001f,-0.247353f),new Vector3(-0.281904f,0.245001f,-0.269808f),new Vector3(-0.278213f,0.245001f,-0.292076f),new Vector3(-0.262658f,0.245001f,-0.308073f),new Vector3(-0.241695f,0.245001f,-0.321211f),
                new Vector3(-0.266937f,0.220000f,-0.249429f),new Vector3(-0.263336f,0.220000f,-0.270080f),new Vector3(-0.259648f,0.220000f,-0.290669f),new Vector3(-0.242758f,0.220000f,-0.301863f),new Vector3(-0.223783f,0.220000f,-0.311571f),
                new Vector3(-0.218108f,0.195000f,-0.241960f),new Vector3(-0.221196f,0.195000f,-0.257522f),new Vector3(-0.224547f,0.195000f,-0.273183f),new Vector3(-0.210516f,0.195000f,-0.282317f),new Vector3(-0.197703f,0.195000f,-0.291908f),
                new Vector3(-0.210770f,0.176250f,-0.248566f),new Vector3(-0.212054f,0.176250f,-0.259002f),new Vector3(-0.213376f,0.176250f,-0.269450f),new Vector3(-0.206781f,0.176250f,-0.277453f),new Vector3(-0.199185f,0.176250f,-0.285148f),
                new Vector3(-0.222953f,0.163750f,-0.263698f),new Vector3(-0.223953f,0.163750f,-0.269109f),new Vector3(-0.224953f,0.163750f,-0.274520f),new Vector3(-0.220919f,0.163750f,-0.278641f),new Vector3(-0.216406f,0.163750f,-0.282639f),
                new Vector3(-0.237710f,0.156313f,-0.280053f),new Vector3(-0.237710f,0.156313f,-0.280053f),new Vector3(-0.237710f,0.156313f,-0.280053f),new Vector3(-0.237710f,0.156313f,-0.280053f),new Vector3(-0.237710f,0.156313f,-0.280053f),
            },
            new[] { // component 1, branch 1
                new Vector3(-0.206991f,0.419999f,-0.251217f),new Vector3(-0.199620f,0.419999f,-0.271079f),new Vector3(-0.192250f,0.419999f,-0.290940f),new Vector3(-0.173050f,0.419999f,-0.303899f),new Vector3(-0.153850f,0.419999f,-0.316858f),
                new Vector3(-0.217723f,0.394999f,-0.236446f),new Vector3(-0.211319f,0.394999f,-0.264572f),new Vector3(-0.204900f,0.394999f,-0.292687f),new Vector3(-0.184026f,0.394999f,-0.311688f),new Vector3(-0.159299f,0.394999f,-0.328261f),
                new Vector3(-0.223116f,0.370000f,-0.250259f),new Vector3(-0.217272f,0.370000f,-0.278050f),new Vector3(-0.211692f,0.370000f,-0.305999f),new Vector3(-0.191630f,0.370000f,-0.325311f),new Vector3(-0.166827f,0.370000f,-0.341795f),
                new Vector3(-0.230067f,0.345001f,-0.265901f),new Vector3(-0.224621f,0.345001f,-0.293195f),new Vector3(-0.219042f,0.345001f,-0.320411f),new Vector3(-0.200081f,0.345001f,-0.339893f),new Vector3(-0.175627f,0.345001f,-0.356201f),
                new Vector3(-0.236977f,0.320001f,-0.281721f),new Vector3(-0.230766f,0.320001f,-0.307876f),new Vector3(-0.224292f,0.320001f,-0.333875f),new Vector3(-0.205296f,0.320001f,-0.352404f),new Vector3(-0.182620f,0.320001f,-0.368738f),
                new Vector3(-0.240867f,0.294999f,-0.294796f),new Vector3(-0.233575f,0.294999f,-0.319690f),new Vector3(-0.225795f,0.294999f,-0.344263f),new Vector3(-0.206829f,0.294999f,-0.361456f),new Vector3(-0.184868f,0.294999f,-0.376672f),
                new Vector3(-0.239761f,0.270000f,-0.304077f),new Vector3(-0.232136f,0.270000f,-0.328095f),new Vector3(-0.223678f,0.270000f,-0.351499f),new Vector3(-0.204222f,0.270000f,-0.366788f),new Vector3(-0.182123f,0.270000f,-0.380128f),
                new Vector3(-0.234434f,0.245000f,-0.311053f),new Vector3(-0.226092f,0.245000f,-0.333555f),new Vector3(-0.217003f,0.245000f,-0.355413f),new Vector3(-0.196846f,0.245000f,-0.367737f),new Vector3(-0.174789f,0.245000f,-0.378424f),
                new Vector3(-0.222910f,0.220000f,-0.313778f),new Vector3(-0.213090f,0.220000f,-0.333469f),new Vector3(-0.203507f,0.220000f,-0.353421f),new Vector3(-0.182516f,0.220000f,-0.360762f),new Vector3(-0.160753f,0.220000f,-0.367248f),
                new Vector3(-0.185654f,0.195000f,-0.299083f),new Vector3(-0.181944f,0.195000f,-0.315774f),new Vector3(-0.178850f,0.195000f,-0.332873f),new Vector3(-0.164302f,0.195000f,-0.342407f),new Vector3(-0.149035f,0.195000f,-0.351465f),
                new Vector3(-0.173867f,0.176250f,-0.306438f),new Vector3(-0.172109f,0.176250f,-0.318994f),new Vector3(-0.169587f,0.176250f,-0.331188f),new Vector3(-0.162447f,0.176250f,-0.341197f),new Vector3(-0.152021f,0.176250f,-0.349651f),
                new Vector3(-0.175991f,0.163750f,-0.330570f),new Vector3(-0.175055f,0.163750f,-0.338496f),new Vector3(-0.173528f,0.163750f,-0.346158f),new Vector3(-0.170491f,0.163750f,-0.353144f),new Vector3(-0.162748f,0.163750f,-0.358024f),
                new Vector3(-0.178727f,0.154601f,-0.374539f),new Vector3(-0.178727f,0.154601f,-0.374539f),new Vector3(-0.178727f,0.154601f,-0.374539f),new Vector3(-0.178727f,0.154601f,-0.374539f),new Vector3(-0.178727f,0.154601f,-0.374539f),
            },
        };
        public static int Count => Rows.Length;
        public static int Columns(int panel) => 4;
        public static int BoneGroup(int panel) => panel<3?4:2;
        public static float[] Stations(int panel) {
            var rows=Rows[panel];var result=new System.Collections.Generic.List<float>();
            for(int row=0;row<rows.Length/5-1;row++) {
                float a=rows[row*5].y,b=rows[(row+1)*5].y;
                int steps=Mathf.Max(1,Mathf.CeilToInt((a-b)/(a<.23f?.030f:.055f)));
                for(int step=0;step<steps;step++)result.Add(Mathf.Lerp(a,b,step/(float)steps));
            }
            result.Add(rows[rows.Length-1].y);return result.ToArray();
        }
        public static Vector3 Point(int panel,float y,int col) {
            var rows=Rows[panel];int count=rows.Length/5,row=0;
            while(row<count-2&&rows[(row+1)*5].y>y)row++;
            float h=rows[row*5].y-rows[(row+1)*5].y,u=Mathf.Clamp01((rows[row*5].y-y)/h);
            Vector3 a=rows[row*5+col],b=rows[(row+1)*5+col];
            int prev=Mathf.Max(0,row-1),next=Mathf.Min(count-1,row+2);
            Vector3 ma=(b-rows[prev*5+col])/(rows[prev*5].y-rows[(row+1)*5].y);
            Vector3 mb=(rows[next*5+col]-a)/(rows[row*5].y-rows[next*5].y);
            var p=(2*u*u*u-3*u*u+1)*a+(u*u*u-2*u*u+u)*h*ma+(-2*u*u*u+3*u*u)*b+(u*u*u-u*u)*h*mb;
            for(int k=0;k<3;k++)p[k]=Mathf.Clamp(p[k],Mathf.Min(a[k],b[k]),Mathf.Max(a[k],b[k]));
            p.y=y;return p;
        }
    }
}
