using UnityEngine;

namespace DesktopFish.Editor {
    // Sparse alpha-visible iris outlines, independent from source UV/topology.
    // Depth is fitted to our own face; these points calibrate only the XY rim.
    public static class FishIrisGuides {
        static readonly Vector3[][] Rows={
            new[]{
                new Vector3(-0.0863332f,0.6644313f,0.1250925f),new Vector3(-0.0531224f,0.6644313f,0.1351388f),new Vector3(-0.0536247f,0.6709375f,0.1359810f),
                new Vector3(-0.0550747f,0.6773790f,0.1366527f),new Vector3(-0.0577815f,0.6835090f,0.1369897f),new Vector3(-0.0617137f,0.6890509f,0.1369846f),
                new Vector3(-0.0669990f,0.6933671f,0.1363885f),new Vector3(-0.0730668f,0.6964593f,0.1353466f),new Vector3(-0.0795627f,0.6984692f,0.1339431f),
                new Vector3(-0.0863332f,0.6991747f,0.1320126f),new Vector3(-0.0931496f,0.6986996f,0.1296014f),new Vector3(-0.0997719f,0.6968751f,0.1267634f),
                new Vector3(-0.1058570f,0.6936508f,0.1236997f),new Vector3(-0.1107519f,0.6888500f,0.1207338f),new Vector3(-0.1144850f,0.6832418f,0.1181101f),
                new Vector3(-0.1173178f,0.6772656f,0.1158795f),new Vector3(-0.1189478f,0.6709188f,0.1142177f),new Vector3(-0.1194634f,0.6644313f,0.1131695f),
                new Vector3(-0.1188978f,0.6579538f,0.1127491f),new Vector3(-0.1173488f,0.6515842f,0.1129098f),new Vector3(-0.1147163f,0.6454664f,0.1136436f),
                new Vector3(-0.1109015f,0.6398631f,0.1149499f),new Vector3(-0.1058853f,0.6351695f,0.1168358f),new Vector3(-0.0998613f,0.6317717f,0.1191785f),
                new Vector3(-0.0931667f,0.6300772f,0.1216484f),new Vector3(-0.0863332f,0.6295694f,0.1240094f),new Vector3(-0.0794649f,0.6299021f,0.1258042f),
                new Vector3(-0.0727720f,0.6316918f,0.1274437f),new Vector3(-0.0663716f,0.6345568f,0.1289947f),new Vector3(-0.0612924f,0.6393905f,0.1305186f),
                new Vector3(-0.0575110f,0.6451730f,0.1319165f),new Vector3(-0.0549834f,0.6514458f,0.1331425f),new Vector3(-0.0535528f,0.6579109f,0.1342042f),
            },
            new[]{
                new Vector3(0.0868537f,0.6644793f,0.1249399f),new Vector3(0.1200800f,0.6644793f,0.1129437f),new Vector3(0.1196324f,0.6709994f,0.1139781f),
                new Vector3(0.1182962f,0.6775031f,0.1155666f),new Vector3(0.1155845f,0.6836765f,0.1177906f),new Vector3(0.1115661f,0.6891916f,0.1205060f),
                new Vector3(0.1062361f,0.6934869f,0.1235178f),new Vector3(0.1001097f,0.6964819f,0.1265419f),new Vector3(0.0936246f,0.6985188f,0.1293904f),
                new Vector3(0.0868537f,0.6991991f,0.1318592f),new Vector3(0.0800441f,0.6987138f,0.1338525f),new Vector3(0.0734544f,0.6968281f,0.1353152f),
                new Vector3(0.0674350f,0.6935415f,0.1363043f),new Vector3(0.0625024f,0.6888306f,0.1367203f),new Vector3(0.0588419f,0.6831962f,0.1366404f),
                new Vector3(0.0560692f,0.6772306f,0.1363494f),new Vector3(0.0543472f,0.6709452f,0.1357818f),new Vector3(0.0537095f,0.6644793f,0.1349848f),
                new Vector3(0.0541917f,0.6579823f,0.1340384f),new Vector3(0.0555751f,0.6515232f,0.1329910f),new Vector3(0.0580075f,0.6452048f,0.1317884f),
                new Vector3(0.0618094f,0.6394349f,0.1303905f),new Vector3(0.0668949f,0.6346088f,0.1288681f),new Vector3(0.0731667f,0.6314357f,0.1273472f),
                new Vector3(0.0799609f,0.6298264f,0.1256741f),new Vector3(0.0868537f,0.6296132f,0.1238723f),new Vector3(0.0936844f,0.6301392f,0.1214662f),
                new Vector3(0.1003281f,0.6319493f,0.1189961f),new Vector3(0.1065366f,0.6350217f,0.1165824f),new Vector3(0.1114938f,0.6398392f,0.1147193f),
                new Vector3(0.1151799f,0.6455524f,0.1134690f),new Vector3(0.1179012f,0.6516190f,0.1127033f),new Vector3(0.1193872f,0.6580080f,0.1125670f),
            },
        };
        public static Vector3 Center(int sign)=>Rows[sign<0?0:1][0];
        public static Vector3 Rim(int sign,int column)=>Rows[sign<0?0:1][1+column%32];
    }
}
