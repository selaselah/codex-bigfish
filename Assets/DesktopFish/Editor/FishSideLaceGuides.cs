using UnityEngine;
namespace DesktopFish.Editor {
    // 190 alpha-visible section controls and two visible tip positions.
    // Ninety additional positions calibrate the recessed navy front bands.
    // Source topology, UV and color samples are not retained.
    public static class FishSideLaceGuides {
        static readonly Vector3[][] Panels={
            new[]{
                new Vector3(-0.0868160f,0.4101004f,0.0825235f),new Vector3(-0.0868160f,0.4101004f,0.0825235f),new Vector3(-0.0868160f,0.4101004f,0.0825235f),new Vector3(-0.0868160f,0.4101004f,0.0825235f),new Vector3(-0.0868160f,0.4101004f,0.0825235f),
                new Vector3(-0.0882982f,0.4140000f,0.0864331f),new Vector3(-0.0897188f,0.4140000f,0.0825756f),new Vector3(-0.0911393f,0.4140000f,0.0784843f),new Vector3(-0.0925598f,0.4140000f,0.0745150f),new Vector3(-0.0939803f,0.4140000f,0.0706712f),
                new Vector3(-0.0917481f,0.4200000f,0.0879589f),new Vector3(-0.0939710f,0.4200000f,0.0855809f),new Vector3(-0.0961940f,0.4200000f,0.0830791f),new Vector3(-0.0984169f,0.4200000f,0.0802925f),new Vector3(-0.1006399f,0.4200000f,0.0688775f),
                new Vector3(-0.0921211f,0.4260000f,0.0895574f),new Vector3(-0.0943417f,0.4260000f,0.0872436f),new Vector3(-0.0965622f,0.4260000f,0.0845836f),new Vector3(-0.0987826f,0.4260000f,0.0807658f),new Vector3(-0.1010032f,0.4260000f,0.0770094f),
                new Vector3(-0.0923115f,0.4320000f,0.0888703f),new Vector3(-0.0959786f,0.4320000f,0.0851170f),new Vector3(-0.0996457f,0.4320000f,0.0807298f),new Vector3(-0.1033128f,0.4320000f,0.0742786f),new Vector3(-0.1069799f,0.4320000f,0.0619238f),
                new Vector3(-0.0932080f,0.4380000f,0.0874651f),new Vector3(-0.0969179f,0.4380000f,0.0836213f),new Vector3(-0.1006279f,0.4380000f,0.0798094f),new Vector3(-0.1043378f,0.4380000f,0.0736481f),new Vector3(-0.1080477f,0.4380000f,0.0594411f),
                new Vector3(-0.0930069f,0.4440000f,0.0852969f),new Vector3(-0.0964454f,0.4440000f,0.0814820f),new Vector3(-0.0998839f,0.4440000f,0.0775740f),new Vector3(-0.1033223f,0.4440000f,0.0716512f),new Vector3(-0.1067608f,0.4440000f,0.0592965f),
                new Vector3(-0.0939485f,0.4500000f,0.0812268f),new Vector3(-0.0968053f,0.4500000f,0.0780643f),new Vector3(-0.0996621f,0.4500000f,0.0743582f),new Vector3(-0.1025190f,0.4500000f,0.0677033f),new Vector3(-0.1053758f,0.4500000f,0.0594759f),
                new Vector3(-0.0948328f,0.4560000f,0.0770806f),new Vector3(-0.0976889f,0.4560000f,0.0740900f),new Vector3(-0.1005449f,0.4560000f,0.0670343f),new Vector3(-0.1034010f,0.4560000f,0.0606925f),new Vector3(-0.1062570f,0.4560000f,0.0345561f),
                new Vector3(-0.0940412f,0.4620000f,0.0735513f),new Vector3(-0.0970019f,0.4620000f,0.0707341f),new Vector3(-0.0999627f,0.4620000f,0.0653012f),new Vector3(-0.1029234f,0.4620000f,0.0586965f),new Vector3(-0.1058841f,0.4620000f,0.0321363f),
                new Vector3(-0.0944606f,0.4680000f,0.0689980f),new Vector3(-0.0970621f,0.4680000f,0.0653354f),new Vector3(-0.0996636f,0.4680000f,0.0615710f),new Vector3(-0.1022652f,0.4680000f,0.0572355f),new Vector3(-0.1048667f,0.4680000f,0.0318734f),
                new Vector3(-0.0929644f,0.4740000f,0.0658396f),new Vector3(-0.0958585f,0.4740000f,0.0616640f),new Vector3(-0.0987525f,0.4740000f,0.0578163f),new Vector3(-0.1016465f,0.4740000f,0.0551252f),new Vector3(-0.1045405f,0.4740000f,0.0423171f),
                new Vector3(-0.0917855f,0.4800000f,0.0624090f),new Vector3(-0.0950013f,0.4800000f,0.0594470f),new Vector3(-0.0982170f,0.4800000f,0.0564849f),new Vector3(-0.1014327f,0.4800000f,0.0534382f),new Vector3(-0.1046484f,0.4800000f,0.0433482f),
                new Vector3(-0.0918917f,0.4860000f,0.0601284f),new Vector3(-0.0951095f,0.4860000f,0.0574274f),new Vector3(-0.0983273f,0.4860000f,0.0544608f),new Vector3(-0.1015451f,0.4860000f,0.0514093f),new Vector3(-0.1047629f,0.4860000f,0.0442762f),
                new Vector3(-0.0905233f,0.4920000f,0.0546227f),new Vector3(-0.0943441f,0.4920000f,0.0541798f),new Vector3(-0.0981650f,0.4920000f,0.0521610f),new Vector3(-0.1019858f,0.4920000f,0.0491160f),new Vector3(-0.1058066f,0.4920000f,0.0444465f),
                new Vector3(-0.0895038f,0.4980000f,0.0486578f),new Vector3(-0.0942980f,0.4980000f,0.0481150f),new Vector3(-0.0990923f,0.4980000f,0.0475579f),new Vector3(-0.1038866f,0.4980000f,0.0445287f),new Vector3(-0.1086808f,0.4980000f,0.0472282f),
                new Vector3(-0.0876897f,0.5040000f,0.0408889f),new Vector3(-0.0942359f,0.5040000f,0.0406477f),new Vector3(-0.1007822f,0.5040000f,0.0404019f),new Vector3(-0.1073284f,0.5040000f,0.0433467f),new Vector3(-0.1138747f,0.5040000f,0.0481199f),
                new Vector3(-0.0860458f,0.5100000f,0.0296653f),new Vector3(-0.0946734f,0.5100000f,0.0287070f),new Vector3(-0.1033009f,0.5100000f,0.0302458f),new Vector3(-0.1119285f,0.5100000f,0.0333918f),new Vector3(-0.1205561f,0.5100000f,0.0376263f),
                new Vector3(-0.0841926f,0.5160000f,0.0201911f),new Vector3(-0.0947391f,0.5160000f,0.0189810f),new Vector3(-0.1052855f,0.5160000f,0.0194544f),new Vector3(-0.1158320f,0.5160000f,0.0219971f),new Vector3(-0.1263785f,0.5160000f,0.0168779f),
                new Vector3(-0.0813538f,0.5220000f,0.0069317f),new Vector3(-0.0922214f,0.5220000f,0.0063648f),new Vector3(-0.1030890f,0.5220000f,0.0070130f),new Vector3(-0.1139565f,0.5220000f,0.0076612f),new Vector3(-0.1248241f,0.5220000f,0.0066617f),
            },
            new[]{
                new Vector3(0.0880549f,0.4103004f,0.0813075f),new Vector3(0.0880549f,0.4103004f,0.0813075f),new Vector3(0.0880549f,0.4103004f,0.0813075f),new Vector3(0.0880549f,0.4103004f,0.0813075f),new Vector3(0.0880549f,0.4103004f,0.0813075f),
                new Vector3(0.0896439f,0.4140000f,0.0851679f),new Vector3(0.0911155f,0.4140000f,0.0827232f),new Vector3(0.0925872f,0.4140000f,0.0802786f),new Vector3(0.0940588f,0.4140000f,0.0758027f),new Vector3(0.0955305f,0.4140000f,0.0703855f),
                new Vector3(0.0922972f,0.4200000f,0.0863126f),new Vector3(0.0943370f,0.4200000f,0.0844306f),new Vector3(0.0963767f,0.4200000f,0.0824631f),new Vector3(0.0984165f,0.4200000f,0.0803989f),new Vector3(0.1004563f,0.4200000f,0.0690256f),
                new Vector3(0.0931893f,0.4260000f,0.0867953f),new Vector3(0.0956878f,0.4260000f,0.0838421f),new Vector3(0.0981864f,0.4260000f,0.0808096f),new Vector3(0.1006850f,0.4260000f,0.0780902f),new Vector3(0.1031835f,0.4260000f,0.0754615f),
                new Vector3(0.0929648f,0.4320000f,0.0872867f),new Vector3(0.0962332f,0.4320000f,0.0831464f),new Vector3(0.0995016f,0.4320000f,0.0796069f),new Vector3(0.1027699f,0.4320000f,0.0758832f),new Vector3(0.1060383f,0.4320000f,0.0679810f),
                new Vector3(0.0947450f,0.4380000f,0.0849836f),new Vector3(0.0980617f,0.4380000f,0.0813589f),new Vector3(0.1013784f,0.4380000f,0.0768762f),new Vector3(0.1046950f,0.4380000f,0.0723824f),new Vector3(0.1080117f,0.4380000f,0.0595663f),
                new Vector3(0.0958250f,0.4440000f,0.0830051f),new Vector3(0.0985589f,0.4440000f,0.0788155f),new Vector3(0.1012929f,0.4440000f,0.0745212f),new Vector3(0.1040268f,0.4440000f,0.0702269f),new Vector3(0.1067608f,0.4440000f,0.0592965f),
                new Vector3(0.0962654f,0.4500000f,0.0789026f),new Vector3(0.0985430f,0.4500000f,0.0753407f),new Vector3(0.1008206f,0.4500000f,0.0718837f),new Vector3(0.1030981f,0.4500000f,0.0683218f),new Vector3(0.1053757f,0.4500000f,0.0594760f),
                new Vector3(0.0961371f,0.4560000f,0.0749946f),new Vector3(0.0987345f,0.4560000f,0.0716567f),new Vector3(0.1013320f,0.4560000f,0.0675692f),new Vector3(0.1039294f,0.4560000f,0.0595841f),new Vector3(0.1065268f,0.4560000f,0.0332155f),
                new Vector3(0.0953615f,0.4620000f,0.0716690f),new Vector3(0.0980237f,0.4620000f,0.0688648f),new Vector3(0.1006859f,0.4620000f,0.0646040f),new Vector3(0.1033481f,0.4620000f,0.0577883f),new Vector3(0.1060104f,0.4620000f,0.0311461f),
                new Vector3(0.0944983f,0.4680000f,0.0684261f),new Vector3(0.0971418f,0.4680000f,0.0656225f),new Vector3(0.0997854f,0.4680000f,0.0618894f),new Vector3(0.1024290f,0.4680000f,0.0569092f),new Vector3(0.1050725f,0.4680000f,0.0302622f),
                new Vector3(0.0939851f,0.4740000f,0.0647937f),new Vector3(0.0964711f,0.4740000f,0.0610233f),new Vector3(0.0989571f,0.4740000f,0.0576482f),new Vector3(0.1014432f,0.4740000f,0.0553775f),new Vector3(0.1039292f,0.4740000f,0.0321716f),
                new Vector3(0.0930573f,0.4800000f,0.0612242f),new Vector3(0.0958889f,0.4800000f,0.0586007f),new Vector3(0.0987205f,0.4800000f,0.0559771f),new Vector3(0.1015520f,0.4800000f,0.0533536f),new Vector3(0.1043836f,0.4800000f,0.0396543f),
                new Vector3(0.0924509f,0.4860000f,0.0597384f),new Vector3(0.0955289f,0.4860000f,0.0570885f),new Vector3(0.0986069f,0.4860000f,0.0542066f),new Vector3(0.1016849f,0.4860000f,0.0513247f),new Vector3(0.1047629f,0.4860000f,0.0442763f),
                new Vector3(0.0925555f,0.4920000f,0.0549663f),new Vector3(0.0958214f,0.4920000f,0.0537835f),new Vector3(0.0990873f,0.4920000f,0.0514557f),new Vector3(0.1023532f,0.4920000f,0.0487780f),new Vector3(0.1056191f,0.4920000f,0.0441793f),
                new Vector3(0.0900744f,0.4980000f,0.0488430f),new Vector3(0.0944905f,0.4980000f,0.0485910f),new Vector3(0.0989065f,0.4980000f,0.0477242f),new Vector3(0.1033226f,0.4980000f,0.0448826f),new Vector3(0.1077386f,0.4980000f,0.0502202f),
                new Vector3(0.0876244f,0.5040000f,0.0417647f),new Vector3(0.0940923f,0.5040000f,0.0413895f),new Vector3(0.1005603f,0.5040000f,0.0406430f),new Vector3(0.1070282f,0.5040000f,0.0431919f),new Vector3(0.1134961f,0.5040000f,0.0488779f),
                new Vector3(0.0857496f,0.5100000f,0.0305292f),new Vector3(0.0942740f,0.5100000f,0.0294734f),new Vector3(0.1027984f,0.5100000f,0.0294908f),new Vector3(0.1113227f,0.5100000f,0.0321795f),new Vector3(0.1198471f,0.5100000f,0.0378534f),
                new Vector3(0.0839950f,0.5160000f,0.0188034f),new Vector3(0.0948792f,0.5160000f,0.0182814f),new Vector3(0.1057633f,0.5160000f,0.0186287f),new Vector3(0.1166475f,0.5160000f,0.0223666f),new Vector3(0.1275316f,0.5160000f,0.0161488f),
                new Vector3(0.0812745f,0.5220000f,0.0058716f),new Vector3(0.0926589f,0.5220000f,0.0063947f),new Vector3(0.1040433f,0.5220000f,0.0070727f),new Vector3(0.1154276f,0.5220000f,0.0076006f),new Vector3(0.1268120f,0.5220000f,0.0055846f),
            },
        };
        static readonly Vector3[][] Straps={
            new[]{
                new Vector3(-0.0573401f,0.4160000f,0.1083824f),new Vector3(-0.0655032f,0.4160000f,0.1045008f),new Vector3(-0.0736664f,0.4160000f,0.0976483f),new Vector3(-0.0818296f,0.4160000f,0.0907752f),new Vector3(-0.0899928f,0.4160000f,0.0787725f),
                new Vector3(-0.0628106f,0.4280000f,0.1050250f),new Vector3(-0.0701884f,0.4280000f,0.1010991f),new Vector3(-0.0775662f,0.4280000f,0.0952539f),new Vector3(-0.0849440f,0.4280000f,0.0891598f),new Vector3(-0.0923218f,0.4280000f,0.0792131f),
                new Vector3(-0.0667639f,0.4400000f,0.0987592f),new Vector3(-0.0734205f,0.4400000f,0.0943448f),new Vector3(-0.0800771f,0.4400000f,0.0889274f),new Vector3(-0.0867338f,0.4400000f,0.0836042f),new Vector3(-0.0933904f,0.4400000f,0.0779746f),
                new Vector3(-0.0697064f,0.4520000f,0.0900172f),new Vector3(-0.0758779f,0.4520000f,0.0861939f),new Vector3(-0.0820493f,0.4520000f,0.0823705f),new Vector3(-0.0882208f,0.4520000f,0.0782334f),new Vector3(-0.0943923f,0.4520000f,0.0707502f),
                new Vector3(-0.0702674f,0.4640000f,0.0791326f),new Vector3(-0.0761842f,0.4640000f,0.0757800f),new Vector3(-0.0821011f,0.4640000f,0.0723652f),new Vector3(-0.0880180f,0.4640000f,0.0689505f),new Vector3(-0.0939348f,0.4640000f,0.0620409f),
                new Vector3(-0.0705107f,0.4760000f,0.0666789f),new Vector3(-0.0760018f,0.4760000f,0.0658994f),new Vector3(-0.0814929f,0.4760000f,0.0635309f),new Vector3(-0.0869841f,0.4760000f,0.0603769f),new Vector3(-0.0924752f,0.4760000f,0.0538969f),
                new Vector3(-0.0720759f,0.4880000f,0.0516271f),new Vector3(-0.0771380f,0.4880000f,0.0508035f),new Vector3(-0.0822002f,0.4880000f,0.0499894f),new Vector3(-0.0872623f,0.4880000f,0.0491655f),new Vector3(-0.0923244f,0.4880000f,0.0465703f),
                new Vector3(-0.0710803f,0.5000000f,0.0363298f),new Vector3(-0.0753530f,0.5000000f,0.0360803f),new Vector3(-0.0796257f,0.5000000f,0.0358308f),new Vector3(-0.0838984f,0.5000000f,0.0355739f),new Vector3(-0.0881711f,0.5000000f,0.0350669f),
                new Vector3(-0.0665894f,0.5120000f,0.0162608f),new Vector3(-0.0713906f,0.5120000f,0.0147531f),new Vector3(-0.0761919f,0.5120000f,0.0132077f),new Vector3(-0.0809931f,0.5120000f,0.0120336f),new Vector3(-0.0857944f,0.5120000f,0.0112862f),
            },
            new[]{
                new Vector3(0.0590946f,0.4160000f,0.1078560f),new Vector3(0.0671959f,0.4160000f,0.1022900f),new Vector3(0.0752971f,0.4160000f,0.0962897f),new Vector3(0.0833984f,0.4160000f,0.0888580f),new Vector3(0.0914996f,0.4160000f,0.0763321f),
                new Vector3(0.0652609f,0.4280000f,0.1040803f),new Vector3(0.0722900f,0.4280000f,0.0993019f),new Vector3(0.0793192f,0.4280000f,0.0944531f),new Vector3(0.0863483f,0.4280000f,0.0880451f),new Vector3(0.0933775f,0.4280000f,0.0775162f),
                new Vector3(0.0707817f,0.4400000f,0.0986296f),new Vector3(0.0769416f,0.4400000f,0.0937144f),new Vector3(0.0831016f,0.4400000f,0.0883501f),new Vector3(0.0892616f,0.4400000f,0.0824023f),new Vector3(0.0954215f,0.4400000f,0.0762163f),
                new Vector3(0.0720478f,0.4520000f,0.0883879f),new Vector3(0.0780234f,0.4520000f,0.0852490f),new Vector3(0.0839990f,0.4520000f,0.0820520f),new Vector3(0.0899746f,0.4520000f,0.0746357f),new Vector3(0.0959503f,0.4520000f,0.0666281f),
                new Vector3(0.0713821f,0.4640000f,0.0788846f),new Vector3(0.0772599f,0.4640000f,0.0757973f),new Vector3(0.0831377f,0.4640000f,0.0726517f),new Vector3(0.0890155f,0.4640000f,0.0671037f),new Vector3(0.0948933f,0.4640000f,0.0596660f),
                new Vector3(0.0716220f,0.4760000f,0.0665196f),new Vector3(0.0771345f,0.4760000f,0.0657401f),new Vector3(0.0826471f,0.4760000f,0.0630736f),new Vector3(0.0881596f,0.4760000f,0.0593727f),new Vector3(0.0936722f,0.4760000f,0.0528106f),
                new Vector3(0.0731818f,0.4880000f,0.0514463f),new Vector3(0.0780787f,0.4880000f,0.0506429f),new Vector3(0.0829757f,0.4880000f,0.0498522f),new Vector3(0.0878727f,0.4880000f,0.0490675f),new Vector3(0.0927697f,0.4880000f,0.0463268f),
                new Vector3(0.0732696f,0.5000000f,0.0354221f),new Vector3(0.0773270f,0.5000000f,0.0354192f),new Vector3(0.0813845f,0.5000000f,0.0354162f),new Vector3(0.0854420f,0.5000000f,0.0354133f),new Vector3(0.0894995f,0.5000000f,0.0345830f),
                new Vector3(0.0655886f,0.5120000f,0.0145446f),new Vector3(0.0704315f,0.5120000f,0.0127368f),new Vector3(0.0752744f,0.5120000f,0.0123812f),new Vector3(0.0801173f,0.5120000f,0.0123344f),new Vector3(0.0849602f,0.5120000f,0.0121621f),
            },
        };
        public static float Low(int sign)=>Panels[sign<0?0:1][0].y;
        public static float High(int sign)=>Panels[sign<0?0:1][95].y;
        static float Slope(float a,float b,float ha,float hb) {
            if(a*b<=0)return 0;float wa=2*hb+ha,wb=hb+2*ha;return (wa+wb)/(wa/a+wb/b);
        }
        static float Cubic(float a,float b,float ma,float mb,float h,float t) {
            float t2=t*t,t3=t2*t;return (2*t3-3*t2+1)*a+(t3-2*t2+t)*h*ma+(-2*t3+3*t2)*b+(t3-t2)*h*mb;
        }
        static Vector3 Column(Vector3[] p,float y,int col) {
            y=Mathf.Clamp(y,p[0].y,p[p.Length-1].y);
            int count=p.Length/5,row=0;while(row<count-2&&p[(row+1)*5].y<y)row++;
            Vector3 a=p[row*5+col],b=p[(row+1)*5+col],result=a;float h=b.y-a.y,t=Mathf.Clamp01((y-a.y)/h);
            for(int k=0;k<3;k++) {
                if(k==1){result.y=y;continue;}
                float d=(b[k]-a[k])/h,ma=d,mb=d;
                if(row>0){var before=p[(row-1)*5+col];float hp=a.y-before.y;ma=Slope((a[k]-before[k])/hp,d,hp,h);}
                if(row<count-2){var after=p[(row+2)*5+col];float hn=after.y-b.y;mb=Slope(d,(after[k]-b[k])/hn,h,hn);}
                result[k]=Cubic(a[k],b[k],ma,mb,h,t);
            }
            return result;
        }
        public static Vector3 Point(int sign,float y,float across)=>Surface(Panels[sign<0?0:1],y,across);
        public static Vector3 Strap(int sign,float y,float across)=>Surface(Straps[sign<0?0:1],y,across);
        static Vector3 Surface(Vector3[] data,float y,float across) {
            float f=Mathf.Clamp01(across)*4;int j=Mathf.Min(3,(int)f);float t=f-j;
            Vector3 a=Column(data,y,j),b=Column(data,y,j+1),p=a;
            for(int k=0;k<3;k++) {
                if(k==1){p.y=y;continue;}
                float d=b[k]-a[k],ma=d,mb=d;
                if(j>0)ma=Slope(a[k]-Column(data,y,j-1)[k],d,1,1);
                if(j<3)mb=Slope(d,Column(data,y,j+2)[k]-b[k],1,1);
                p[k]=Cubic(a[k],b[k],ma,mb,1,t);
            }
            return p;
        }
    }
}
