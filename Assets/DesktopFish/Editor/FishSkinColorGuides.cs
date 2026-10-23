using UnityEngine;
namespace DesktopFish.Editor {
    // Position-only cheek hue-boundary guides. Independent soft distance bands
    // use the rebuilt palette; no source RGB texels, UVs or palette are stored.
    public static class FishSkinColorGuides {
        static readonly Vector3[] Positions={
            new Vector3(-0.1023000f,0.5900000f,0.1268269f),
            new Vector3(-0.1179461f,0.5900000f,0.1049313f),
            new Vector3(-0.0917229f,0.5950000f,0.1361887f),
            new Vector3(-0.1238774f,0.5950000f,0.1001021f),
            new Vector3(-0.0849007f,0.6000000f,0.1399150f),
            new Vector3(-0.1266852f,0.6000000f,0.1000681f),
            new Vector3(-0.0785337f,0.6050000f,0.1429583f),
            new Vector3(-0.1294097f,0.6050000f,0.1000603f),
            new Vector3(-0.0765052f,0.6100000f,0.1437552f),
            new Vector3(-0.1317154f,0.6100000f,0.1000054f),
            new Vector3(-0.0738534f,0.6150000f,0.1449894f),
            new Vector3(-0.1331603f,0.6150000f,0.1000972f),
            new Vector3(-0.0752410f,0.6200000f,0.1435660f),
            new Vector3(-0.1345940f,0.6200000f,0.1000552f),
            new Vector3(-0.0769219f,0.6250000f,0.1412065f),
            new Vector3(-0.1360799f,0.6250000f,0.1000979f),
            new Vector3(-0.0807612f,0.6300000f,0.1306339f),
            new Vector3(-0.1376912f,0.6300000f,0.1000426f),
            new Vector3(-0.1234179f,0.6350000f,0.1106036f),
            new Vector3(-0.1399200f,0.6350000f,0.1000477f),
            new Vector3(-0.1289926f,0.6400000f,0.1080883f),
            new Vector3(-0.1427124f,0.6400000f,0.1015502f),
            new Vector3(-0.1325926f,0.6450000f,0.1066239f),
            new Vector3(-0.1453164f,0.6450000f,0.1000872f),
            new Vector3(-0.1451695f,0.6500000f,0.1029996f),
            new Vector3(-0.1472881f,0.6500000f,0.1000599f),
            new Vector3(0.0991075f,0.5900000f,0.1294189f),
            new Vector3(0.1175936f,0.5900000f,0.1057169f),
            new Vector3(0.0881872f,0.5950000f,0.1377665f),
            new Vector3(0.1239024f,0.5950000f,0.1000183f),
            new Vector3(0.0815997f,0.6000000f,0.1412801f),
            new Vector3(0.1266841f,0.6000000f,0.1000602f),
            new Vector3(0.0757534f,0.6050000f,0.1441155f),
            new Vector3(0.1294091f,0.6050000f,0.1000584f),
            new Vector3(0.0728422f,0.6100000f,0.1451743f),
            new Vector3(0.1317154f,0.6100000f,0.1000054f),
            new Vector3(0.0699854f,0.6150000f,0.1462596f),
            new Vector3(0.1331602f,0.6150000f,0.1000972f),
            new Vector3(0.0711119f,0.6200000f,0.1451264f),
            new Vector3(0.1345940f,0.6200000f,0.1000552f),
            new Vector3(0.0727294f,0.6250000f,0.1432985f),
            new Vector3(0.1360799f,0.6250000f,0.1000978f),
            new Vector3(0.0766355f,0.6300000f,0.1317990f),
            new Vector3(0.1376912f,0.6300000f,0.1000427f),
            new Vector3(0.1234182f,0.6350000f,0.1106035f),
            new Vector3(0.1399200f,0.6350000f,0.1000478f),
            new Vector3(0.1289928f,0.6400000f,0.1080883f),
            new Vector3(0.1427124f,0.6400000f,0.1015502f),
            new Vector3(0.1325927f,0.6450000f,0.1066239f),
            new Vector3(0.1453164f,0.6450000f,0.1000873f),
            new Vector3(-0.0971920f,0.6000000f,0.1337282f),
            new Vector3(-0.1245318f,0.6000000f,0.1046263f),
            new Vector3(-0.0932848f,0.6050000f,0.1360993f),
            new Vector3(-0.1294097f,0.6050000f,0.1000603f),
            new Vector3(-0.0855233f,0.6100000f,0.1399671f),
            new Vector3(-0.1317154f,0.6100000f,0.1000054f),
            new Vector3(-0.0859462f,0.6150000f,0.1393363f),
            new Vector3(-0.1331603f,0.6150000f,0.1000972f),
            new Vector3(-0.0842210f,0.6200000f,0.1392110f),
            new Vector3(-0.1345940f,0.6200000f,0.1000552f),
            new Vector3(-0.0883818f,0.6250000f,0.1311081f),
            new Vector3(-0.1360799f,0.6250000f,0.1000979f),
            new Vector3(-0.0896181f,0.6300000f,0.1268668f),
            new Vector3(-0.1376912f,0.6300000f,0.1000426f),
            new Vector3(-0.1234179f,0.6350000f,0.1106036f),
            new Vector3(-0.1399200f,0.6350000f,0.1000477f),
            new Vector3(-0.1289926f,0.6400000f,0.1080883f),
            new Vector3(-0.1427124f,0.6400000f,0.1015502f),
            new Vector3(0.0944097f,0.6000000f,0.1353499f),
            new Vector3(0.1231832f,0.6000000f,0.1067160f),
            new Vector3(0.0873172f,0.6050000f,0.1389915f),
            new Vector3(0.1294091f,0.6050000f,0.1000584f),
            new Vector3(0.0818193f,0.6100000f,0.1416744f),
            new Vector3(0.1317154f,0.6100000f,0.1000054f),
            new Vector3(0.0790682f,0.6150000f,0.1427367f),
            new Vector3(0.1331602f,0.6150000f,0.1000972f),
            new Vector3(0.0817204f,0.6200000f,0.1405856f),
            new Vector3(0.1345940f,0.6200000f,0.1000552f),
            new Vector3(0.0821620f,0.6250000f,0.1385620f),
            new Vector3(0.1360799f,0.6250000f,0.1000978f),
            new Vector3(0.0843173f,0.6300000f,0.1295304f),
            new Vector3(0.1376912f,0.6300000f,0.1000427f),
            new Vector3(0.1234182f,0.6350000f,0.1106035f),
            new Vector3(0.1396675f,0.6350000f,0.1004793f),
            new Vector3(0.1289928f,0.6400000f,0.1080883f),
            new Vector3(0.1305317f,0.6400000f,0.1078782f),
            new Vector3(-0.1034238f,0.6050000f,0.1296598f),
            new Vector3(-0.1222281f,0.6050000f,0.1120502f),
            new Vector3(-0.0962437f,0.6100000f,0.1338845f),
            new Vector3(-0.1289073f,0.6100000f,0.1049647f),
            new Vector3(-0.0941025f,0.6150000f,0.1347298f),
            new Vector3(-0.1331603f,0.6150000f,0.1000972f),
            new Vector3(-0.0954243f,0.6200000f,0.1323070f),
            new Vector3(-0.1335396f,0.6200000f,0.1020299f),
            new Vector3(-0.0979516f,0.6250000f,0.1266967f),
            new Vector3(-0.1360799f,0.6250000f,0.1000979f),
            new Vector3(-0.1001069f,0.6300000f,0.1217472f),
            new Vector3(-0.1366478f,0.6300000f,0.1018883f),
            new Vector3(-0.1234179f,0.6350000f,0.1106036f),
            new Vector3(-0.1281970f,0.6350000f,0.1098983f),
            new Vector3(0.1000162f,0.6050000f,0.1319041f),
            new Vector3(0.1198435f,0.6050000f,0.1158086f),
            new Vector3(0.0935708f,0.6100000f,0.1354748f),
            new Vector3(0.1271070f,0.6100000f,0.1081036f),
            new Vector3(0.0909321f,0.6150000f,0.1366655f),
            new Vector3(0.1312883f,0.6150000f,0.1036046f),
            new Vector3(0.0928192f,0.6200000f,0.1342712f),
            new Vector3(0.1329537f,0.6200000f,0.1031270f),
            new Vector3(0.0936768f,0.6250000f,0.1286648f),
            new Vector3(0.1343578f,0.6250000f,0.1031438f),
            new Vector3(0.0937395f,0.6300000f,0.1248349f),
            new Vector3(0.1346275f,0.6300000f,0.1071838f),
            new Vector3(0.1234182f,0.6350000f,0.1106035f),
            new Vector3(0.1254654f,0.6350000f,0.1103307f),
        };
        static Vector2[] Contour(int start,int count) {
            int rows=count/2;var polygon=new Vector2[rows*2+2];
            Vector3 first=Positions[start],last=Positions[start+count-2];
            polygon[0]=new Vector2((Mathf.Abs(first.x)+Mathf.Abs(Positions[start+1].x))*.5f,first.y-.0025f);
            for(int row=0;row<rows;row++) {
                Vector3 p=Positions[start+row*2+1];polygon[row+1]=new Vector2(Mathf.Abs(p.x),p.y);
            }
            polygon[rows+1]=new Vector2((Mathf.Abs(last.x)+Mathf.Abs(Positions[start+count-1].x))*.5f,last.y+.0025f);
            for(int row=0;row<rows;row++) {
                Vector3 p=Positions[start+(rows-1-row)*2];polygon[rows+2+row]=new Vector2(Mathf.Abs(p.x),p.y);
            }
            return polygon;
        }
        static readonly Vector2[][] Contours={
            Contour(0,26),
            Contour(26,24),
            Contour(50,18),
            Contour(68,18),
            Contour(86,14),
            Contour(100,14),
        };
        static float Distance(Vector2[] polygon,Vector2 point) {
            bool inside=false;float distance=float.PositiveInfinity;
            for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++) {
                Vector2 a=polygon[j],b=polygon[i],ab=b-a;
                Vector2 near=a+ab*Mathf.Clamp01(Vector2.Dot(point-a,ab)/Mathf.Max(1e-12f,ab.sqrMagnitude));
                distance=Mathf.Min(distance,(point-near).magnitude);
                if((a.y>point.y)!=(b.y>point.y)&&point.x<(b.x-a.x)*(point.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
            }
            return inside?distance:-distance;
        }
        static readonly float[] Weights={.24f,.26f,.28f};
        public static float Blush(Vector3 point) {
            int side=point.x<0?0:1;Vector2 p=new Vector2(Mathf.Abs(point.x),point.y);
            float amount=0;
            for(int level=0;level<3;level++) {
                float d=Distance(Contours[level*2+side],p);
                amount+=Weights[level]*Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.007f,.007f,d));
                if(level==2)amount+=.10f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.012f,d));
            }
            return Mathf.Clamp01(amount);
        }
    }
}
