using UnityEngine;

namespace DesktopFish.Editor {
    // The broad anterior backing locks are separate from outer-hair slot15.
    // Sparse alpha-visible sections only; fresh eight-column loft topology.
    public static class FishChestBackingGuides {
        static readonly Vector3[][] Rows={
            new[]{
                new Vector3(-0.0576397f,0.9800000f,0.0430629f),new Vector3(-0.0576397f,0.9800000f,0.0430629f),new Vector3(-0.0576397f,0.9800000f,0.0430629f),new Vector3(-0.0576397f,0.9800000f,0.0430629f),
                new Vector3(-0.0576397f,0.9800000f,0.0430629f),new Vector3(-0.0576397f,0.9800000f,0.0430629f),new Vector3(-0.0576397f,0.9800000f,0.0430629f),new Vector3(-0.0576397f,0.9800000f,0.0430629f),
                new Vector3(-0.1457530f,0.9450000f,0.0338887f),new Vector3(-0.1089013f,0.9450000f,0.0545154f),new Vector3(-0.0720497f,0.9450000f,0.0637110f),new Vector3(-0.0351980f,0.9450000f,0.0658758f),
                new Vector3(0.0016536f,0.9450000f,0.0537148f),new Vector3(-0.0351980f,0.9450000f,0.0550047f),new Vector3(-0.0720497f,0.9450000f,0.0182900f),new Vector3(-0.1089013f,0.9450000f,-0.0004975f),
                new Vector3(-0.1945227f,0.8950000f,0.0298829f),new Vector3(-0.1809708f,0.8950000f,0.0412666f),new Vector3(-0.1674189f,0.8950000f,0.0524683f),new Vector3(-0.1538670f,0.8950000f,0.0638969f),
                new Vector3(-0.1403150f,0.8950000f,0.0188158f),new Vector3(-0.1538670f,0.8950000f,0.0063781f),new Vector3(-0.1674189f,0.8950000f,-0.0048203f),new Vector3(-0.1809708f,0.8950000f,-0.0159970f),
                new Vector3(-0.2195480f,0.8450000f,0.0169749f),new Vector3(-0.2049327f,0.8450000f,0.0334083f),new Vector3(-0.1903176f,0.8450000f,0.0447791f),new Vector3(-0.1757023f,0.8450000f,0.0570082f),
                new Vector3(-0.1610871f,0.8450000f,0.0678135f),new Vector3(-0.1757023f,0.8450000f,0.0011026f),new Vector3(-0.1903176f,0.8450000f,-0.0152100f),new Vector3(-0.2049327f,0.8450000f,-0.0218117f),
                new Vector3(-0.2319329f,0.7950000f,0.0088632f),new Vector3(-0.2160072f,0.7950000f,0.0282586f),new Vector3(-0.2000816f,0.7950000f,0.0400311f),new Vector3(-0.1841559f,0.7950000f,0.0516926f),
                new Vector3(-0.1682303f,0.7950000f,0.0629573f),new Vector3(-0.1841559f,0.7950000f,-0.0011382f),new Vector3(-0.2000816f,0.7950000f,-0.0237032f),new Vector3(-0.2160072f,0.7950000f,-0.0242088f),
                new Vector3(-0.2370802f,0.7450000f,0.0058055f),new Vector3(-0.2204491f,0.7450000f,0.0202298f),new Vector3(-0.2038180f,0.7450000f,0.0338132f),new Vector3(-0.1871869f,0.7450000f,0.0434763f),
                new Vector3(-0.1705558f,0.7450000f,0.0544069f),new Vector3(-0.1871869f,0.7450000f,-0.0031129f),new Vector3(-0.2038180f,0.7450000f,-0.0258847f),new Vector3(-0.2204491f,0.7450000f,-0.0248390f),
                new Vector3(-0.2335894f,0.6950000f,0.0037654f),new Vector3(-0.2172673f,0.6950000f,0.0194273f),new Vector3(-0.2009453f,0.6950000f,0.0348848f),new Vector3(-0.1846233f,0.6950000f,0.0382713f),
                new Vector3(-0.1683012f,0.6950000f,0.0473778f),new Vector3(-0.1846233f,0.6950000f,-0.0015792f),new Vector3(-0.2009453f,0.6950000f,-0.0222402f),new Vector3(-0.2172673f,0.6950000f,-0.0202769f),
                new Vector3(-0.2178894f,0.6450000f,0.0083681f),new Vector3(-0.2032320f,0.6450000f,0.0240509f),new Vector3(-0.1885745f,0.6450000f,0.0401043f),new Vector3(-0.1739171f,0.6450000f,0.0381507f),
                new Vector3(-0.1592596f,0.6450000f,0.0450483f),new Vector3(-0.1739171f,0.6450000f,0.0037923f),new Vector3(-0.1885745f,0.6450000f,-0.0129009f),new Vector3(-0.2032320f,0.6450000f,-0.0110585f),
                new Vector3(-0.2019147f,0.6075000f,-0.0095823f),new Vector3(-0.1884978f,0.6075000f,0.0351498f),new Vector3(-0.1750808f,0.6075000f,0.0512745f),new Vector3(-0.1616638f,0.6075000f,0.0410973f),
                new Vector3(-0.1482468f,0.6075000f,0.0472064f),new Vector3(-0.1616638f,0.6075000f,-0.0044709f),new Vector3(-0.1750808f,0.6075000f,0.0034371f),new Vector3(-0.1884978f,0.6075000f,-0.0030961f),
                new Vector3(-0.1913805f,0.5825000f,-0.0030865f),new Vector3(-0.1789388f,0.5825000f,0.0443161f),new Vector3(-0.1664971f,0.5825000f,0.0606549f),new Vector3(-0.1540554f,0.5825000f,0.0783808f),
                new Vector3(-0.1416136f,0.5825000f,0.0530675f),new Vector3(-0.1540554f,0.5825000f,0.0156208f),new Vector3(-0.1664971f,0.5825000f,0.0094986f),new Vector3(-0.1789388f,0.5825000f,0.0031402f),
                new Vector3(-0.1830241f,0.5575000f,0.0068047f),new Vector3(-0.1703198f,0.5575000f,0.0457826f),new Vector3(-0.1576154f,0.5575000f,0.0803038f),new Vector3(-0.1449111f,0.5575000f,0.1038550f),
                new Vector3(-0.1322067f,0.5575000f,0.0366769f),new Vector3(-0.1449111f,0.5575000f,0.0397404f),new Vector3(-0.1576154f,0.5575000f,0.0217523f),new Vector3(-0.1703198f,0.5575000f,0.0141891f),
                new Vector3(-0.1763183f,0.5325000f,0.0223126f),new Vector3(-0.1613303f,0.5325000f,0.0701605f),new Vector3(-0.1463422f,0.5325000f,0.1033509f),new Vector3(-0.1313541f,0.5325000f,0.1407686f),
                new Vector3(-0.1163661f,0.5325000f,0.0791113f),new Vector3(-0.1313541f,0.5325000f,0.0863735f),new Vector3(-0.1463422f,0.5325000f,0.0441183f),new Vector3(-0.1613303f,0.5325000f,0.0322219f),
                new Vector3(-0.1736424f,0.5075000f,0.0936984f),new Vector3(-0.1572758f,0.5075000f,0.1143140f),new Vector3(-0.1409091f,0.5075000f,0.1484077f),new Vector3(-0.1245425f,0.5075000f,0.1316490f),
                new Vector3(-0.1081759f,0.5075000f,0.1122270f),new Vector3(-0.1245425f,0.5075000f,0.1174750f),new Vector3(-0.1409091f,0.5075000f,0.1219715f),new Vector3(-0.1572758f,0.5075000f,0.0597467f),
                new Vector3(-0.1896522f,0.4825000f,0.1101108f),new Vector3(-0.1762602f,0.4825000f,0.1344806f),new Vector3(-0.1628682f,0.4825000f,0.1503256f),new Vector3(-0.1494762f,0.4825000f,0.1403757f),
                new Vector3(-0.1360842f,0.4825000f,0.1318370f),new Vector3(-0.1494762f,0.4825000f,0.1334039f),new Vector3(-0.1628682f,0.4825000f,0.1309244f),new Vector3(-0.1762602f,0.4825000f,0.1009297f),
                new Vector3(-0.1921647f,0.4637500f,0.1141067f),new Vector3(-0.1814094f,0.4637500f,0.1312170f),new Vector3(-0.1706541f,0.4637500f,0.1393195f),new Vector3(-0.1598989f,0.4637500f,0.1429948f),
                new Vector3(-0.1491436f,0.4637500f,0.1317455f),new Vector3(-0.1598989f,0.4637500f,0.1294563f),new Vector3(-0.1706541f,0.4637500f,0.1207260f),new Vector3(-0.1814094f,0.4637500f,0.1175443f),
                new Vector3(-0.1735401f,0.4450000f,0.1179021f),new Vector3(-0.1649123f,0.4450000f,0.1322963f),new Vector3(-0.1562845f,0.4450000f,0.1392897f),new Vector3(-0.1476567f,0.4450000f,0.1422097f),
                new Vector3(-0.1390289f,0.4450000f,0.1448581f),new Vector3(-0.1476567f,0.4450000f,0.1266180f),new Vector3(-0.1562845f,0.4450000f,0.1238882f),new Vector3(-0.1649123f,0.4450000f,0.1206322f),
                new Vector3(-0.1586482f,0.4325000f,0.1207066f),new Vector3(-0.1511726f,0.4325000f,0.1314827f),new Vector3(-0.1436971f,0.4325000f,0.1395646f),new Vector3(-0.1362216f,0.4325000f,0.1414508f),
                new Vector3(-0.1287460f,0.4325000f,0.1432020f),new Vector3(-0.1362216f,0.4325000f,0.1329344f),new Vector3(-0.1436971f,0.4325000f,0.1281418f),new Vector3(-0.1511726f,0.4325000f,0.1243579f),
                new Vector3(-0.1462239f,0.4262500f,0.1261222f),new Vector3(-0.1402176f,0.4262500f,0.1322412f),new Vector3(-0.1342112f,0.4262500f,0.1371448f),new Vector3(-0.1282049f,0.4262500f,0.1414895f),
                new Vector3(-0.1221985f,0.4262500f,0.1432067f),new Vector3(-0.1282049f,0.4262500f,0.1378711f),new Vector3(-0.1342112f,0.4262500f,0.1337437f),new Vector3(-0.1402176f,0.4262500f,0.1296528f),
                new Vector3(-0.1440689f,0.4257450f,0.1273545f),new Vector3(-0.1384630f,0.4257450f,0.1331049f),new Vector3(-0.1328572f,0.4257450f,0.1371798f),new Vector3(-0.1272513f,0.4257450f,0.1412335f),
                new Vector3(-0.1216454f,0.4257450f,0.1431991f),new Vector3(-0.1272513f,0.4257450f,0.1383523f),new Vector3(-0.1328572f,0.4257450f,0.1345571f),new Vector3(-0.1384630f,0.4257450f,0.1306136f),
                new Vector3(-0.1203421f,0.4238699f,0.1428004f),new Vector3(-0.1203421f,0.4238699f,0.1428004f),new Vector3(-0.1203421f,0.4238699f,0.1428004f),new Vector3(-0.1203421f,0.4238699f,0.1428004f),
                new Vector3(-0.1203421f,0.4238699f,0.1428004f),new Vector3(-0.1203421f,0.4238699f,0.1428004f),new Vector3(-0.1203421f,0.4238699f,0.1428004f),new Vector3(-0.1203421f,0.4238699f,0.1428004f),
            },
            new[]{
                new Vector3(0.0573932f,0.9800000f,0.0446972f),new Vector3(0.0573932f,0.9800000f,0.0446972f),new Vector3(0.0573932f,0.9800000f,0.0446972f),new Vector3(0.0573932f,0.9800000f,0.0446972f),
                new Vector3(0.0573932f,0.9800000f,0.0446972f),new Vector3(0.0573932f,0.9800000f,0.0446972f),new Vector3(0.0573932f,0.9800000f,0.0446972f),new Vector3(0.0573932f,0.9800000f,0.0446972f),
                new Vector3(-0.0020816f,0.9450000f,0.0646692f),new Vector3(0.0348299f,0.9450000f,0.0659821f),new Vector3(0.0717415f,0.9450000f,0.0596612f),new Vector3(0.1086531f,0.9450000f,0.0567289f),
                new Vector3(0.1455647f,0.9450000f,0.0346188f),new Vector3(0.1086531f,0.9450000f,0.0008112f),new Vector3(0.0717415f,0.9450000f,0.0206745f),new Vector3(0.0348299f,0.9450000f,0.0544318f),
                new Vector3(0.1365144f,0.8950000f,0.0154539f),new Vector3(0.1535824f,0.8950000f,0.0586983f),new Vector3(0.1706504f,0.8950000f,0.0462429f),new Vector3(0.1877183f,0.8950000f,0.0343900f),
                new Vector3(0.2047863f,0.8950000f,0.0235982f),new Vector3(0.1877183f,0.8950000f,-0.0216427f),new Vector3(0.1706504f,0.8950000f,-0.0094715f),new Vector3(0.1535824f,0.8950000f,0.0025941f),
                new Vector3(0.1637034f,0.8450000f,0.0086157f),new Vector3(0.1789852f,0.8450000f,0.0503488f),new Vector3(0.1942670f,0.8450000f,0.0390145f),new Vector3(0.2095488f,0.8450000f,0.0277913f),
                new Vector3(0.2248306f,0.8450000f,0.0135481f),new Vector3(0.2095488f,0.8450000f,-0.0253533f),new Vector3(0.1942670f,0.8450000f,-0.0204348f),new Vector3(0.1789852f,0.8450000f,-0.0041548f),
                new Vector3(0.1728295f,0.7950000f,0.0524139f),new Vector3(0.1878288f,0.7950000f,0.0439056f),new Vector3(0.2028281f,0.7950000f,0.0343953f),new Vector3(0.2178275f,0.7950000f,0.0249066f),
                new Vector3(0.2328268f,0.7950000f,0.0059469f),new Vector3(0.2178275f,0.7950000f,-0.0237993f),new Vector3(0.2028281f,0.7950000f,-0.0242762f),new Vector3(0.1878288f,0.7950000f,-0.0050821f),
                new Vector3(0.1746110f,0.7450000f,0.0463017f),new Vector3(0.1903097f,0.7450000f,0.0385049f),new Vector3(0.2060083f,0.7450000f,0.0294278f),new Vector3(0.2217069f,0.7450000f,0.0159241f),
                new Vector3(0.2374056f,0.7450000f,0.0025076f),new Vector3(0.2217069f,0.7450000f,-0.0226368f),new Vector3(0.2060083f,0.7450000f,-0.0255166f),new Vector3(0.1903097f,0.7450000f,-0.0051261f),
                new Vector3(0.1708558f,0.6950000f,0.0433268f),new Vector3(0.1865879f,0.6950000f,0.0357623f),new Vector3(0.2023200f,0.6950000f,0.0343421f),new Vector3(0.2180522f,0.6950000f,0.0189837f),
                new Vector3(0.2337843f,0.6950000f,0.0036763f),new Vector3(0.2180522f,0.6950000f,-0.0182460f),new Vector3(0.2023200f,0.6950000f,-0.0211815f),new Vector3(0.1865879f,0.6950000f,-0.0024358f),
                new Vector3(0.1603955f,0.6450000f,0.0436973f),new Vector3(0.1748872f,0.6450000f,0.0377681f),new Vector3(0.1893788f,0.6450000f,0.0424213f),new Vector3(0.2038705f,0.6450000f,0.0264370f),
                new Vector3(0.2183622f,0.6450000f,0.0105349f),new Vector3(0.2038705f,0.6450000f,-0.0104616f),new Vector3(0.1893788f,0.6450000f,-0.0085936f),new Vector3(0.1748872f,0.6450000f,-0.0184518f),
                new Vector3(0.1492060f,0.6075000f,0.0472620f),new Vector3(0.1620188f,0.6075000f,0.0421861f),new Vector3(0.1748316f,0.6075000f,0.0520297f),new Vector3(0.1876443f,0.6075000f,0.0363625f),
                new Vector3(0.2004571f,0.6075000f,0.0207311f),new Vector3(0.1876443f,0.6075000f,-0.0025281f),new Vector3(0.1748316f,0.6075000f,0.0029549f),new Vector3(0.1620188f,0.6075000f,-0.0037432f),
                new Vector3(0.1422049f,0.5825000f,0.0527327f),new Vector3(0.1541507f,0.5825000f,0.0796893f),new Vector3(0.1660965f,0.5825000f,0.0613173f),new Vector3(0.1780424f,0.5825000f,0.0451745f),
                new Vector3(0.1899882f,0.5825000f,-0.0016455f),new Vector3(0.1780424f,0.5825000f,0.0032943f),new Vector3(0.1660965f,0.5825000f,0.0088505f),new Vector3(0.1541507f,0.5825000f,0.0144187f),
                new Vector3(0.1329438f,0.5575000f,0.0619883f),new Vector3(0.1452971f,0.5575000f,0.1028533f),new Vector3(0.1576503f,0.5575000f,0.0792757f),new Vector3(0.1700035f,0.5575000f,0.0485195f),
                new Vector3(0.1823567f,0.5575000f,0.0073630f),new Vector3(0.1700035f,0.5575000f,0.0134383f),new Vector3(0.1576503f,0.5575000f,0.0196953f),new Vector3(0.1452971f,0.5575000f,0.0331325f),
                new Vector3(0.1157374f,0.5325000f,0.0776452f),new Vector3(0.1308113f,0.5325000f,0.1345315f),new Vector3(0.1458851f,0.5325000f,0.1034161f),new Vector3(0.1609590f,0.5325000f,0.0725265f),
                new Vector3(0.1760328f,0.5325000f,0.0223407f),new Vector3(0.1609590f,0.5325000f,0.0315230f),new Vector3(0.1458851f,0.5325000f,0.0415805f),new Vector3(0.1308113f,0.5325000f,0.0862480f),
                new Vector3(0.1077238f,0.5075000f,0.1107123f),new Vector3(0.1245523f,0.5075000f,0.1324613f),new Vector3(0.1413807f,0.5075000f,0.1493991f),new Vector3(0.1582092f,0.5075000f,0.1121449f),
                new Vector3(0.1750377f,0.5075000f,0.0952182f),new Vector3(0.1582092f,0.5075000f,0.0573640f),new Vector3(0.1413807f,0.5075000f,0.1200945f),new Vector3(0.1245523f,0.5075000f,0.1154882f),
                new Vector3(0.1353080f,0.4825000f,0.1319762f),new Vector3(0.1490792f,0.4825000f,0.1417085f),new Vector3(0.1628504f,0.4825000f,0.1524720f),new Vector3(0.1766215f,0.4825000f,0.1336660f),
                new Vector3(0.1903927f,0.4825000f,0.1108903f),new Vector3(0.1766215f,0.4825000f,0.1012950f),new Vector3(0.1628504f,0.4825000f,0.1330370f),new Vector3(0.1490792f,0.4825000f,0.1343032f),
                new Vector3(0.1469742f,0.4637500f,0.1317462f),new Vector3(0.1577822f,0.4637500f,0.1429775f),new Vector3(0.1685902f,0.4637500f,0.1398087f),new Vector3(0.1793982f,0.4637500f,0.1336229f),
                new Vector3(0.1902062f,0.4637500f,0.1159504f),new Vector3(0.1793982f,0.4637500f,0.1188057f),new Vector3(0.1685902f,0.4637500f,0.1220899f),new Vector3(0.1577822f,0.4637500f,0.1302990f),
                new Vector3(0.1415613f,0.4450000f,0.1434261f),new Vector3(0.1487594f,0.4450000f,0.1412723f),new Vector3(0.1559575f,0.4450000f,0.1389568f),new Vector3(0.1631557f,0.4450000f,0.1328517f),
                new Vector3(0.1703538f,0.4450000f,0.1197906f),new Vector3(0.1631557f,0.4450000f,0.1219855f),new Vector3(0.1559575f,0.4450000f,0.1242592f),new Vector3(0.1487594f,0.4450000f,0.1264618f),
                new Vector3(0.1296579f,0.4325000f,0.1426418f),new Vector3(0.1357640f,0.4325000f,0.1412628f),new Vector3(0.1418701f,0.4325000f,0.1389024f),new Vector3(0.1479762f,0.4325000f,0.1330666f),
                new Vector3(0.1540823f,0.4325000f,0.1236248f),new Vector3(0.1479762f,0.4325000f,0.1265295f),new Vector3(0.1418701f,0.4325000f,0.1298937f),new Vector3(0.1357640f,0.4325000f,0.1335689f),
                new Vector3(0.1224729f,0.4262500f,0.1430978f),new Vector3(0.1264244f,0.4262500f,0.1420319f),new Vector3(0.1303759f,0.4262500f,0.1387179f),new Vector3(0.1343274f,0.4262500f,0.1353131f),
                new Vector3(0.1382789f,0.4262500f,0.1316865f),new Vector3(0.1343274f,0.4262500f,0.1340197f),new Vector3(0.1303759f,0.4262500f,0.1366153f),new Vector3(0.1264244f,0.4262500f,0.1391110f),
                new Vector3(0.1219063f,0.4257844f,0.1431474f),new Vector3(0.1252797f,0.4257844f,0.1421743f),new Vector3(0.1286531f,0.4257844f,0.1392586f),new Vector3(0.1320265f,0.4257844f,0.1363254f),
                new Vector3(0.1353999f,0.4257844f,0.1333922f),new Vector3(0.1320265f,0.4257844f,0.1354867f),new Vector3(0.1286531f,0.4257844f,0.1376809f),new Vector3(0.1252797f,0.4257844f,0.1397754f),
                new Vector3(0.1203340f,0.4239094f,0.1428346f),new Vector3(0.1203340f,0.4239094f,0.1428346f),new Vector3(0.1203340f,0.4239094f,0.1428346f),new Vector3(0.1203340f,0.4239094f,0.1428346f),
                new Vector3(0.1203340f,0.4239094f,0.1428346f),new Vector3(0.1203340f,0.4239094f,0.1428346f),new Vector3(0.1203340f,0.4239094f,0.1428346f),new Vector3(0.1203340f,0.4239094f,0.1428346f),
            },
        };
        public static float[] Stations(int panel) {
            var p=Rows[panel];var ys=new System.Collections.Generic.List<float>();
            for(int row=0;row<p.Length/8;row++)ys.Add(p[row*8].y);return ys.ToArray();
        }
        static float Slope(float a,float b,float ha,float hb) {
            if(a*b<=0)return 0;float wa=2*hb+ha,wb=hb+2*ha;return (wa+wb)/(wa/a+wb/b);
        }
        static float Cubic(float a,float b,float ma,float mb,float h,float t) {
            float t2=t*t,t3=t2*t;return (2*t3-3*t2+1)*a+(t3-2*t2+t)*h*ma+(-2*t3+3*t2)*b+(t3-t2)*h*mb;
        }
        static Vector3 Column(int panel,float y,int col) {
            var p=Rows[panel];int count=p.Length/8,row=0;while(row<count-2&&p[(row+1)*8].y>y)row++;
            float h=p[row*8].y-p[(row+1)*8].y,t=Mathf.Clamp01((p[row*8].y-y)/h);
            Vector3 a=p[row*8+col],b=p[(row+1)*8+col],result=a;
            for(int k=0;k<3;k++) {
                if(k==1){result.y=y;continue;}
                float d=(b[k]-a[k])/h,ma=d,mb=d;
                if(row>0){float hp=p[(row-1)*8].y-p[row*8].y;ma=Slope((a[k]-p[(row-1)*8+col][k])/hp,d,hp,h);}
                if(row<count-2){float hn=p[(row+1)*8].y-p[(row+2)*8].y;mb=Slope(d,(p[(row+2)*8+col][k]-b[k])/hn,h,hn);}
                result[k]=Cubic(a[k],b[k],ma,mb,h,t);
            }
            return result;
        }
        public static Vector3 Surface(int panel,float y,float around) {
            float f=Mathf.Repeat(around,1)*8;int j=(int)f;float t=f-j;
            Vector3 a=Column(panel,y,j),b=Column(panel,y,(j+1)%8),before=Column(panel,y,(j+7)%8),after=Column(panel,y,(j+2)%8),p=a;
            for(int k=0;k<3;k++) {
                if(k==1){p.y=y;continue;}
                float d=b[k]-a[k];p[k]=Cubic(a[k],b[k],Slope(a[k]-before[k],d,1,1),Slope(d,after[k]-b[k],1,1),1,t);
            }
            return p;
        }
    }
}
