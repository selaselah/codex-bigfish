using UnityEngine;
namespace DesktopFish.Editor {
    // Sparse alpha-visible outer-curl sections. All surfaces and topology are authored.
    public static class FishSideCurlGuides {
        static readonly Vector3[][] Rows={
            new[]{
                new Vector3(-0.0456525f,0.9811083f,-0.0086811f),new Vector3(-0.0456525f,0.9811083f,-0.0086811f),new Vector3(-0.0456525f,0.9811083f,-0.0086811f),new Vector3(-0.0456525f,0.9811083f,-0.0086811f),new Vector3(-0.0456525f,0.9811083f,-0.0086811f),new Vector3(-0.0456525f,0.9811083f,-0.0086811f),new Vector3(-0.0456525f,0.9811083f,-0.0086811f),new Vector3(-0.0456525f,0.9811083f,-0.0086811f),
                new Vector3(-0.0843700f,0.9762472f,-0.0161003f),new Vector3(-0.0645317f,0.9762472f,-0.0198660f),new Vector3(-0.0457372f,0.9762472f,-0.0182360f),new Vector3(-0.0281056f,0.9762472f,-0.0105939f),new Vector3(-0.0105290f,0.9762472f,-0.0026674f),new Vector3(-0.0295525f,0.9762472f,-0.0031139f),new Vector3(-0.0485474f,0.9762472f,-0.0037084f),new Vector3(-0.0669451f,0.9762472f,-0.0073899f),
                new Vector3(-0.1262882f,0.9637471f,-0.0259742f),new Vector3(-0.1036579f,0.9637471f,-0.0385128f),new Vector3(-0.0880588f,0.9637471f,-0.0312587f),new Vector3(-0.0733627f,0.9637471f,-0.0214625f),new Vector3(-0.0583687f,0.9637471f,-0.0125048f),new Vector3(-0.0792911f,0.9637471f,-0.0047742f),new Vector3(-0.0966144f,0.9637471f,-0.0071748f),new Vector3(-0.1116388f,0.9637471f,-0.0160467f),
                new Vector3(-0.1787752f,0.9324981f,-0.0420090f),new Vector3(-0.1536304f,0.9324981f,-0.0611693f),new Vector3(-0.1460237f,0.9324981f,-0.0511608f),new Vector3(-0.1383247f,0.9324981f,-0.0413058f),new Vector3(-0.1302356f,0.9324981f,-0.0320998f),new Vector3(-0.1536184f,0.9324981f,-0.0158700f),new Vector3(-0.1620855f,0.9324981f,-0.0244474f),new Vector3(-0.1705526f,0.9324981f,-0.0330249f),
                new Vector3(-0.2140873f,0.8825019f,-0.0553031f),new Vector3(-0.2035595f,0.8825019f,-0.0672491f),new Vector3(-0.1915094f,0.8825019f,-0.0789715f),new Vector3(-0.1852244f,0.8825019f,-0.0645557f),new Vector3(-0.1783643f,0.8825019f,-0.0500555f),new Vector3(-0.1889013f,0.8825019f,-0.0381108f),new Vector3(-0.1997142f,0.8825019f,-0.0262066f),new Vector3(-0.2073236f,0.8825019f,-0.0408170f),
                new Vector3(-0.2357521f,0.8200003f,-0.0664853f),new Vector3(-0.2241743f,0.8200003f,-0.0787284f),new Vector3(-0.2124026f,0.8200003f,-0.0909244f),new Vector3(-0.2067634f,0.8200003f,-0.0744977f),new Vector3(-0.2019504f,0.8200003f,-0.0582718f),new Vector3(-0.2138589f,0.8200003f,-0.0461090f),new Vector3(-0.2256987f,0.8200003f,-0.0339296f),new Vector3(-0.2316803f,0.8200003f,-0.0504395f),
                new Vector3(-0.2463117f,0.7574976f,-0.0735312f),new Vector3(-0.2334878f,0.7574976f,-0.0855262f),new Vector3(-0.2208212f,0.7574976f,-0.0975755f),new Vector3(-0.2153069f,0.7574976f,-0.0792486f),new Vector3(-0.2127875f,0.7574976f,-0.0619557f),new Vector3(-0.2255809f,0.7574976f,-0.0499502f),new Vector3(-0.2382871f,0.7574976f,-0.0379146f),new Vector3(-0.2437198f,0.7574976f,-0.0562134f),
                new Vector3(-0.2537714f,0.6949978f,-0.0801033f),new Vector3(-0.2399899f,0.6949978f,-0.0920979f),new Vector3(-0.2261875f,0.6949978f,-0.1040842f),new Vector3(-0.2213933f,0.6949978f,-0.0847269f),new Vector3(-0.2197812f,0.6949978f,-0.0666308f),new Vector3(-0.2333914f,0.6949978f,-0.0545683f),new Vector3(-0.2471675f,0.6949978f,-0.0425715f),new Vector3(-0.2522231f,0.6949978f,-0.0620325f),
                new Vector3(-0.2572865f,0.6324982f,-0.0872598f),new Vector3(-0.2425150f,0.6324982f,-0.0990980f),new Vector3(-0.2277551f,0.6324982f,-0.1109415f),new Vector3(-0.2244598f,0.6324982f,-0.0907576f),new Vector3(-0.2240720f,0.6324982f,-0.0719169f),new Vector3(-0.2386815f,0.6324982f,-0.0600039f),new Vector3(-0.2536056f,0.6324982f,-0.0482363f),new Vector3(-0.2571379f,0.6324982f,-0.0685296f),
                new Vector3(-0.2543494f,0.5700018f,-0.0953726f),new Vector3(-0.2387644f,0.5700018f,-0.1070691f),new Vector3(-0.2231342f,0.5700018f,-0.1187436f),new Vector3(-0.2210849f,0.5700018f,-0.0985127f),new Vector3(-0.2211592f,0.5700018f,-0.0793095f),new Vector3(-0.2368438f,0.5700018f,-0.0676613f),new Vector3(-0.2522229f,0.5700018f,-0.0558653f),new Vector3(-0.2547263f,0.5700018f,-0.0763160f),
                new Vector3(-0.2538821f,0.5074998f,-0.1045004f),new Vector3(-0.2377292f,0.5074998f,-0.1154631f),new Vector3(-0.2218782f,0.5074998f,-0.1265770f),new Vector3(-0.2200223f,0.5074998f,-0.1065989f),new Vector3(-0.2201668f,0.5074998f,-0.0876222f),new Vector3(-0.2350972f,0.5074998f,-0.0760474f),new Vector3(-0.2514585f,0.5074998f,-0.0651890f),new Vector3(-0.2533206f,0.5074998f,-0.0851702f),
                new Vector3(-0.2825182f,0.4450011f,-0.1206889f),new Vector3(-0.2727843f,0.4450011f,-0.1356152f),new Vector3(-0.2527303f,0.4450011f,-0.1490751f),new Vector3(-0.2459696f,0.4450011f,-0.1318052f),new Vector3(-0.2408950f,0.4450011f,-0.1147748f),new Vector3(-0.2517084f,0.4450011f,-0.1000019f),new Vector3(-0.2721505f,0.4450011f,-0.0865971f),new Vector3(-0.2767567f,0.4450011f,-0.1035609f),
                new Vector3(-0.3040967f,0.4074986f,-0.1376750f),new Vector3(-0.3090147f,0.4074986f,-0.1533746f),new Vector3(-0.2873188f,0.4074986f,-0.1764850f),new Vector3(-0.2775679f,0.4074986f,-0.1621311f),new Vector3(-0.2705782f,0.4074986f,-0.1470084f),new Vector3(-0.2640286f,0.4074986f,-0.1317631f),new Vector3(-0.2928978f,0.4074986f,-0.1066552f),new Vector3(-0.2971570f,0.4074986f,-0.1225383f),
                new Vector3(-0.3334270f,0.3700010f,-0.1693548f),new Vector3(-0.3402232f,0.3700010f,-0.1863546f),new Vector3(-0.3172885f,0.3700010f,-0.2073526f),new Vector3(-0.3089475f,0.3700010f,-0.1905606f),new Vector3(-0.3008451f,0.3700010f,-0.1737364f),new Vector3(-0.2984823f,0.3700010f,-0.1561404f),new Vector3(-0.3167650f,0.3700010f,-0.1357679f),new Vector3(-0.3254734f,0.3700010f,-0.1525106f),
                new Vector3(-0.3576782f,0.3324990f,-0.1955011f),new Vector3(-0.3562226f,0.3324990f,-0.2095954f),new Vector3(-0.3431689f,0.3324990f,-0.2165900f),new Vector3(-0.3287270f,0.3324990f,-0.2227346f),new Vector3(-0.3297346f,0.3324990f,-0.2083662f),new Vector3(-0.3284565f,0.3324990f,-0.1925984f),new Vector3(-0.3414140f,0.3324990f,-0.1855450f),new Vector3(-0.3543775f,0.3324990f,-0.1784952f),
                new Vector3(-0.3657918f,0.2949995f,-0.2117583f),new Vector3(-0.3604481f,0.2949995f,-0.2227300f),new Vector3(-0.3488656f,0.2949995f,-0.2264930f),new Vector3(-0.3362495f,0.2949995f,-0.2290615f),new Vector3(-0.3408904f,0.2949995f,-0.2172778f),new Vector3(-0.3455961f,0.2949995f,-0.2055688f),new Vector3(-0.3577733f,0.2949995f,-0.2024931f),new Vector3(-0.3699932f,0.2949995f,-0.1994667f),
                new Vector3(-0.3597064f,0.2575001f,-0.2213519f),new Vector3(-0.3494505f,0.2575001f,-0.2274471f),new Vector3(-0.3369258f,0.2575001f,-0.2266800f),new Vector3(-0.3246904f,0.2575001f,-0.2267880f),new Vector3(-0.3334099f,0.2575001f,-0.2160460f),new Vector3(-0.3441942f,0.2575001f,-0.2115493f),new Vector3(-0.3562122f,0.2575001f,-0.2107833f),new Vector3(-0.3678070f,0.2575001f,-0.2087378f),
                new Vector3(-0.2792951f,0.2199998f,-0.1980480f),new Vector3(-0.2769442f,0.2199998f,-0.1970673f),new Vector3(-0.2746373f,0.2199998f,-0.1959703f),new Vector3(-0.2723304f,0.2199998f,-0.1948733f),new Vector3(-0.2756224f,0.2199998f,-0.1933642f),new Vector3(-0.2781350f,0.2199998f,-0.1939170f),new Vector3(-0.2806312f,0.2199998f,-0.1945131f),new Vector3(-0.2829518f,0.2199998f,-0.1955737f),
                new Vector3(-0.2781084f,0.2195289f,-0.1971121f),new Vector3(-0.2759479f,0.2195289f,-0.1962872f),new Vector3(-0.2738467f,0.2195289f,-0.1952969f),new Vector3(-0.2717540f,0.2195289f,-0.1942834f),new Vector3(-0.2747073f,0.2195289f,-0.1929012f),new Vector3(-0.2769689f,0.2195289f,-0.1934449f),new Vector3(-0.2792427f,0.2195289f,-0.1939546f),new Vector3(-0.2813564f,0.2195289f,-0.1949099f),
                new Vector3(-0.2685849f,0.2157789f,-0.1896346f),new Vector3(-0.2685849f,0.2157789f,-0.1896346f),new Vector3(-0.2685849f,0.2157789f,-0.1896346f),new Vector3(-0.2685849f,0.2157789f,-0.1896346f),new Vector3(-0.2685849f,0.2157789f,-0.1896346f),new Vector3(-0.2685849f,0.2157789f,-0.1896346f),new Vector3(-0.2685849f,0.2157789f,-0.1896346f),new Vector3(-0.2685849f,0.2157789f,-0.1896346f),
            },
            new[]{
                new Vector3(0.0577975f,0.9815709f,-0.0097479f),new Vector3(0.0577975f,0.9815709f,-0.0097479f),new Vector3(0.0577975f,0.9815709f,-0.0097479f),new Vector3(0.0577975f,0.9815709f,-0.0097479f),new Vector3(0.0577975f,0.9815709f,-0.0097479f),new Vector3(0.0577975f,0.9815709f,-0.0097479f),new Vector3(0.0577975f,0.9815709f,-0.0097479f),new Vector3(0.0577975f,0.9815709f,-0.0097479f),
                new Vector3(0.0102952f,0.9762475f,-0.0023486f),new Vector3(0.0286608f,0.9762475f,-0.0127703f),new Vector3(0.0475258f,0.9762475f,-0.0202827f),new Vector3(0.0676254f,0.9762475f,-0.0205998f),new Vector3(0.0885368f,0.9762475f,-0.0161868f),new Vector3(0.0698468f,0.9762475f,-0.0076553f),new Vector3(0.0505173f,0.9762475f,-0.0028504f),new Vector3(0.0303647f,0.9762475f,-0.0028413f),
                new Vector3(0.0612434f,0.9637492f,-0.0129370f),new Vector3(0.0747484f,0.9637492f,-0.0227586f),new Vector3(0.0889698f,0.9637492f,-0.0306868f),new Vector3(0.1031312f,0.9637492f,-0.0387740f),new Vector3(0.1252983f,0.9637492f,-0.0257068f),new Vector3(0.1119318f,0.9637492f,-0.0155196f),new Vector3(0.0979669f,0.9637492f,-0.0069132f),new Vector3(0.0822161f,0.9637492f,-0.0030261f),
                new Vector3(0.1334656f,0.9324987f,-0.0368621f),new Vector3(0.1417885f,0.9324987f,-0.0461583f),new Vector3(0.1503658f,0.9324987f,-0.0553326f),new Vector3(0.1598529f,0.9324987f,-0.0640709f),new Vector3(0.1778449f,0.9324987f,-0.0421660f),new Vector3(0.1694724f,0.9324987f,-0.0328936f),new Vector3(0.1610998f,0.9324987f,-0.0236212f),new Vector3(0.1521711f,0.9324987f,-0.0146153f),
                new Vector3(0.1795110f,0.8824987f,-0.0549254f),new Vector3(0.1873803f,0.8824987f,-0.0693447f),new Vector3(0.1930194f,0.8824987f,-0.0835247f),new Vector3(0.2057050f,0.8824987f,-0.0713115f),new Vector3(0.2153127f,0.8824987f,-0.0587680f),new Vector3(0.2079463f,0.8824987f,-0.0444026f),new Vector3(0.2009064f,0.8824987f,-0.0300723f),new Vector3(0.1890311f,0.8824987f,-0.0423725f),
                new Vector3(0.2025936f,0.8199995f,-0.0651622f),new Vector3(0.2071295f,0.8199995f,-0.0814842f),new Vector3(0.2129982f,0.8199995f,-0.0981546f),new Vector3(0.2244822f,0.8199995f,-0.0860191f),new Vector3(0.2361812f,0.8199995f,-0.0739398f),new Vector3(0.2326343f,0.8199995f,-0.0578762f),new Vector3(0.2267985f,0.8199995f,-0.0412145f),new Vector3(0.2147729f,0.8199995f,-0.0532084f),
                new Vector3(0.2138623f,0.7575028f,-0.0686274f),new Vector3(0.2161530f,0.7575028f,-0.0858940f),new Vector3(0.2218184f,0.7575028f,-0.1043986f),new Vector3(0.2342840f,0.7575028f,-0.0925453f),new Vector3(0.2465451f,0.7575028f,-0.0806171f),new Vector3(0.2444634f,0.7575028f,-0.0634271f),new Vector3(0.2388433f,0.7575028f,-0.0449391f),new Vector3(0.2262593f,0.7575028f,-0.0567489f),
                new Vector3(0.2205714f,0.6949985f,-0.0715435f),new Vector3(0.2224977f,0.6949985f,-0.0895080f),new Vector3(0.2273001f,0.6949985f,-0.1085382f),new Vector3(0.2411113f,0.6949985f,-0.0964053f),new Vector3(0.2547450f,0.6949985f,-0.0842066f),new Vector3(0.2527097f,0.6949985f,-0.0662017f),new Vector3(0.2478130f,0.6949985f,-0.0471365f),new Vector3(0.2342667f,0.6949985f,-0.0593676f),
                new Vector3(0.2253781f,0.6325021f,-0.0732189f),new Vector3(0.2260178f,0.6325021f,-0.0920577f),new Vector3(0.2290412f,0.6325021f,-0.1119882f),new Vector3(0.2439420f,0.6325021f,-0.1002675f),new Vector3(0.2585080f,0.6325021f,-0.0883935f),new Vector3(0.2583684f,0.6325021f,-0.0697838f),new Vector3(0.2544778f,0.6325021f,-0.0494561f),new Vector3(0.2399460f,0.6325021f,-0.0613458f),
                new Vector3(0.2241336f,0.5699986f,-0.0778699f),new Vector3(0.2238490f,0.5699986f,-0.0968830f),new Vector3(0.2262184f,0.5699986f,-0.1172052f),new Vector3(0.2418567f,0.5699986f,-0.1057649f),new Vector3(0.2570611f,0.5699986f,-0.0941106f),new Vector3(0.2570398f,0.5699986f,-0.0749466f),new Vector3(0.2550643f,0.5699986f,-0.0548187f),new Vector3(0.2395990f,0.5699986f,-0.0663443f),
                new Vector3(0.2249097f,0.5075008f,-0.0841309f),new Vector3(0.2249091f,0.5075008f,-0.1032893f),new Vector3(0.2270930f,0.5075008f,-0.1236192f),new Vector3(0.2420354f,0.5075008f,-0.1124735f),new Vector3(0.2583071f,0.5075008f,-0.1020406f),new Vector3(0.2584650f,0.5075008f,-0.0829665f),new Vector3(0.2572677f,0.5075008f,-0.0631657f),new Vector3(0.2409902f,0.5075008f,-0.0735955f),
                new Vector3(0.2473707f,0.4450002f,-0.1149748f),new Vector3(0.2526507f,0.4450002f,-0.1308942f),new Vector3(0.2600435f,0.4450002f,-0.1468324f),new Vector3(0.2837801f,0.4450002f,-0.1311713f),new Vector3(0.2886077f,0.4450002f,-0.1153419f),new Vector3(0.2832653f,0.4450002f,-0.0994220f),new Vector3(0.2779230f,0.4450002f,-0.0835020f),new Vector3(0.2515052f,0.4450002f,-0.0991392f),
                new Vector3(0.2778945f,0.4074997f,-0.1459127f),new Vector3(0.2873949f,0.4074997f,-0.1613903f),new Vector3(0.2976905f,0.4074997f,-0.1766665f),new Vector3(0.3174557f,0.4074997f,-0.1537785f),new Vector3(0.3131091f,0.4074997f,-0.1369959f),new Vector3(0.3066720f,0.4074997f,-0.1207427f),new Vector3(0.3002578f,0.4074997f,-0.1044837f),new Vector3(0.2716707f,0.4074997f,-0.1296055f),
                new Vector3(0.3112492f,0.3699989f,-0.1758412f),new Vector3(0.3195824f,0.3699989f,-0.1909074f),new Vector3(0.3249674f,0.3699989f,-0.2071000f),new Vector3(0.3479036f,0.3699989f,-0.1800884f),new Vector3(0.3407337f,0.3699989f,-0.1645777f),new Vector3(0.3327352f,0.3699989f,-0.1493836f),new Vector3(0.3245662f,0.3699989f,-0.1342546f),new Vector3(0.3039408f,0.3699989f,-0.1603834f),
                new Vector3(0.3356339f,0.3325004f,-0.2111945f),new Vector3(0.3334610f,0.3325004f,-0.2253965f),new Vector3(0.3502694f,0.3325004f,-0.2180593f),new Vector3(0.3599959f,0.3325004f,-0.2074003f),new Vector3(0.3615909f,0.3325004f,-0.1929273f),new Vector3(0.3588051f,0.3325004f,-0.1763994f),new Vector3(0.3451816f,0.3325004f,-0.1852305f),new Vector3(0.3323118f,0.3325004f,-0.1944152f),
                new Vector3(0.3363584f,0.2950000f,-0.2215762f),new Vector3(0.3326852f,0.2950000f,-0.2352560f),new Vector3(0.3455564f,0.2950000f,-0.2308970f),new Vector3(0.3583686f,0.2950000f,-0.2264782f),new Vector3(0.3628651f,0.2950000f,-0.2136325f),new Vector3(0.3670268f,0.2950000f,-0.2004476f),new Vector3(0.3548007f,0.2950000f,-0.2054604f),new Vector3(0.3418706f,0.2950000f,-0.2097598f),
                new Vector3(0.3206052f,0.2574994f,-0.2260008f),new Vector3(0.3094211f,0.2574994f,-0.2347889f),new Vector3(0.3231719f,0.2574994f,-0.2359732f),new Vector3(0.3366495f,0.2574994f,-0.2360958f),new Vector3(0.3486836f,0.2574994f,-0.2306105f),new Vector3(0.3580735f,0.2574994f,-0.2148513f),new Vector3(0.3454935f,0.2574994f,-0.2182159f),new Vector3(0.3325828f,0.2574994f,-0.2202957f),
                new Vector3(0.2576296f,0.2199999f,-0.2121112f),new Vector3(0.2532613f,0.2199999f,-0.2151734f),new Vector3(0.2579457f,0.2199999f,-0.2157652f),new Vector3(0.2626413f,0.2199999f,-0.2164867f),new Vector3(0.2671056f,0.2199999f,-0.2145340f),new Vector3(0.2714952f,0.2199999f,-0.2117180f),new Vector3(0.2668679f,0.2199999f,-0.2117868f),new Vector3(0.2622498f,0.2199999f,-0.2119614f),
                new Vector3(0.2504901f,0.2161933f,-0.2113846f),new Vector3(0.2479942f,0.2161933f,-0.2130283f),new Vector3(0.2504113f,0.2161933f,-0.2133425f),new Vector3(0.2528285f,0.2161933f,-0.2136567f),new Vector3(0.2552981f,0.2161933f,-0.2126661f),new Vector3(0.2577852f,0.2161933f,-0.2112406f),new Vector3(0.2553564f,0.2161933f,-0.2112161f),new Vector3(0.2529233f,0.2161933f,-0.2113003f),
                new Vector3(0.2442962f,0.2124430f,-0.2106342f),new Vector3(0.2442962f,0.2124430f,-0.2106342f),new Vector3(0.2442962f,0.2124430f,-0.2106342f),new Vector3(0.2442962f,0.2124430f,-0.2106342f),new Vector3(0.2442962f,0.2124430f,-0.2106342f),new Vector3(0.2442962f,0.2124430f,-0.2106342f),new Vector3(0.2442962f,0.2124430f,-0.2106342f),new Vector3(0.2442962f,0.2124430f,-0.2106342f),
            },
        };
        public static float Top(int panel)=>Rows[panel][0].y;
        public static float Bottom(int panel)=>Rows[panel][Rows[panel].Length-1].y;
        static float Slope(float a,float b,float ha,float hb) {
            if(a*b<=0)return 0;float wa=2*hb+ha,wb=hb+2*ha;return (wa+wb)/(wa/a+wb/b);
        }
        static float Cubic(float a,float b,float ma,float mb,float h,float t) {
            float t2=t*t,t3=t2*t;return (2*t3-3*t2+1)*a+(t3-2*t2+t)*h*ma+(-2*t3+3*t2)*b+(t3-t2)*h*mb;
        }
        public static float[] Stations(int panel) {
            var p=Rows[panel];var ys=new System.Collections.Generic.List<float>();
            for(int row=0;row<p.Length/8-1;row++) {
                float a=p[row*8].y,b=p[(row+1)*8].y;int steps=Mathf.Max(1,Mathf.CeilToInt((a-b)/.035f));
                for(int j=0;j<steps;j++)ys.Add(Mathf.Lerp(a,b,j/(float)steps));
            }
            ys.Add(Bottom(panel));
            // Hidden root stations lie inside the rebuilt scalp. Spend their
            // geometry on the visible S bend instead of dense concealed rings.
            var visible=ys.FindAll(y=>y<=.86f||Mathf.Abs(y-Top(panel))<.00001f);
            visible.Add(.90f);visible.Add(.86f);visible.Sort((a,b)=>b.CompareTo(a));
            return visible.ToArray();
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
