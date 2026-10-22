using UnityEngine;
namespace DesktopFish.Editor {
    // Sparse visible upper-eyeline edge controls from the local Ref review.
    // Independent strips follow the rebuilt skin, with new topology and morphs.
    public static class FishEyeContourGuides {
        static readonly Vector3[][] Upper={
            new[]{
                new Vector3(-0.0495683f,0.6843766f,0.1431663f),new Vector3(-0.0495683f,0.6843766f,0.1431663f),
                new Vector3(-0.0537500f,0.6964794f,0.1458079f),new Vector3(-0.0537500f,0.6985337f,0.1466853f),
                new Vector3(-0.0581250f,0.6906834f,0.1405924f),new Vector3(-0.0581250f,0.7014909f,0.1462856f),
                new Vector3(-0.0625000f,0.6926563f,0.1397064f),new Vector3(-0.0625000f,0.7040565f,0.1457589f),
                new Vector3(-0.0668750f,0.6946292f,0.1388204f),new Vector3(-0.0668750f,0.7065340f,0.1452444f),
                new Vector3(-0.0712500f,0.6957051f,0.1377322f),new Vector3(-0.0712500f,0.7078696f,0.1443850f),
                new Vector3(-0.0756250f,0.6966329f,0.1366105f),new Vector3(-0.0756250f,0.7091006f,0.1434940f),
                new Vector3(-0.0800000f,0.6975605f,0.1354888f),new Vector3(-0.0800000f,0.7100852f,0.1424907f),
                new Vector3(-0.0843750f,0.6983356f,0.1342264f),new Vector3(-0.0843750f,0.7111950f,0.1414935f),
                new Vector3(-0.0887500f,0.6985823f,0.1324770f),new Vector3(-0.0887500f,0.7117712f,0.1399158f),
                new Vector3(-0.0931250f,0.6988291f,0.1307276f),new Vector3(-0.0931250f,0.7120771f,0.1382150f),
                new Vector3(-0.0975000f,0.6988532f,0.1289055f),new Vector3(-0.0975000f,0.7120923f,0.1363823f),
                new Vector3(-0.1018750f,0.6982002f,0.1268623f),new Vector3(-0.1018750f,0.7118864f,0.1344703f),
                new Vector3(-0.1062500f,0.6975471f,0.1248190f),new Vector3(-0.1062500f,0.7115274f,0.1324981f),
                new Vector3(-0.1106250f,0.6964358f,0.1226412f),new Vector3(-0.1106250f,0.7115518f,0.1305991f),
                new Vector3(-0.1150000f,0.7117712f,0.1287464f),new Vector3(-0.1150000f,0.7141163f,0.1293546f),
                new Vector3(-0.1193750f,0.6922130f,0.1176981f),new Vector3(-0.1193750f,0.7098778f,0.1263335f),
                new Vector3(-0.1237500f,0.6892089f,0.1150420f),new Vector3(-0.1237500f,0.7118385f,0.1249200f),
                new Vector3(-0.1281250f,0.7139938f,0.1235519f),new Vector3(-0.1281250f,0.7145358f,0.1236921f),
                new Vector3(-0.1325000f,0.6824115f,0.1098054f),new Vector3(-0.1325000f,0.7019798f,0.1184546f),
                new Vector3(-0.1368750f,0.6779251f,0.1071798f),new Vector3(-0.1368750f,0.6991974f,0.1158577f),
                new Vector3(-0.1412500f,0.6582540f,0.1048341f),new Vector3(-0.1412500f,0.6958599f,0.1131425f),
                new Vector3(-0.1456250f,0.6625268f,0.1045596f),new Vector3(-0.1456250f,0.6950085f,0.1114017f),
                new Vector3(-0.1500000f,0.6956839f,0.1097930f),new Vector3(-0.1500000f,0.6962311f,0.1099170f),
                new Vector3(-0.1543750f,0.6679661f,0.1030427f),new Vector3(-0.1543750f,0.6802693f,0.1046127f),
                new Vector3(-0.1587500f,0.6711770f,0.1022059f),new Vector3(-0.1587500f,0.6767821f,0.1026075f),
                new Vector3(-0.1622360f,0.6744214f,0.1012829f),new Vector3(-0.1622360f,0.6744214f,0.1012829f),
            },
            new[]{
                new Vector3(0.0498651f,0.6845826f,0.1430530f),new Vector3(0.0498651f,0.6845826f,0.1430530f),
                new Vector3(0.0537500f,0.6964800f,0.1458080f),new Vector3(0.0537500f,0.6983631f,0.1466122f),
                new Vector3(0.0581250f,0.6906840f,0.1405924f),new Vector3(0.0581250f,0.7013194f,0.1462122f),
                new Vector3(0.0625000f,0.6926568f,0.1397065f),new Vector3(0.0625000f,0.7040569f,0.1457589f),
                new Vector3(0.0668750f,0.6946295f,0.1388205f),new Vector3(0.0668750f,0.7063652f,0.1451689f),
                new Vector3(0.0712500f,0.6957053f,0.1377322f),new Vector3(0.0712500f,0.7077599f,0.1443349f),
                new Vector3(0.0756250f,0.6966329f,0.1366106f),new Vector3(0.0756250f,0.7089360f,0.1434190f),
                new Vector3(0.0800000f,0.6975606f,0.1354889f),new Vector3(0.0800000f,0.7100853f,0.1424908f),
                new Vector3(0.0843750f,0.6983357f,0.1342265f),new Vector3(0.0843750f,0.7111950f,0.1414935f),
                new Vector3(0.0887500f,0.6985827f,0.1324771f),new Vector3(0.0887500f,0.7117713f,0.1399159f),
                new Vector3(0.0931250f,0.6988296f,0.1307277f),new Vector3(0.0931250f,0.7120773f,0.1382151f),
                new Vector3(0.0975000f,0.6988540f,0.1289056f),new Vector3(0.0975000f,0.7120926f,0.1363824f),
                new Vector3(0.1018750f,0.6982009f,0.1268624f),new Vector3(0.1018750f,0.7118868f,0.1344704f),
                new Vector3(0.1062500f,0.6975478f,0.1248191f),new Vector3(0.1062500f,0.7115279f,0.1324982f),
                new Vector3(0.1106250f,0.6964364f,0.1226413f),new Vector3(0.1106250f,0.7111933f,0.1305050f),
                new Vector3(0.1150000f,0.6943249f,0.1201698f),new Vector3(0.1150000f,0.7135755f,0.1292143f),
                new Vector3(0.1193750f,0.6922134f,0.1176982f),new Vector3(0.1193750f,0.7095179f,0.1262402f),
                new Vector3(0.1237500f,0.6892092f,0.1150422f),new Vector3(0.1237500f,0.7113001f,0.1247804f),
                new Vector3(0.1281250f,0.7118265f,0.1229913f),new Vector3(0.1281250f,0.7138136f,0.1235053f),
                new Vector3(0.1325000f,0.6824115f,0.1098056f),new Vector3(0.1325000f,0.7021523f,0.1185210f),
                new Vector3(0.1368750f,0.6779251f,0.1071800f),new Vector3(0.1368750f,0.6991974f,0.1158578f),
                new Vector3(0.1412500f,0.6569854f,0.1050244f),new Vector3(0.1412500f,0.6960361f,0.1132018f),
                new Vector3(0.1456250f,0.6615001f,0.1045276f),new Vector3(0.1456250f,0.6944683f,0.1112795f),
                new Vector3(0.1500000f,0.6944070f,0.1095038f),new Vector3(0.1500000f,0.6958664f,0.1098345f),
                new Vector3(0.1543750f,0.6670293f,0.1030247f),new Vector3(0.1543750f,0.6800895f,0.1045690f),
                new Vector3(0.1587500f,0.6698695f,0.1021592f),new Vector3(0.1587500f,0.6767294f,0.1025947f),
                new Vector3(0.1628296f,0.6737142f,0.1010919f),new Vector3(0.1628296f,0.6737142f,0.1010919f),
            },
        };
        public static int Columns(int sign)=>Upper[sign<0?0:1].Length/2;
        public static Vector3 Point(int sign,int col,bool upper)=>Upper[sign<0?0:1][col*2+(upper?1:0)];
    }
}
