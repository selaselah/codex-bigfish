using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DesktopFish.Editor {
// Independently authored sparse structural replacement. The unchanged base is
// reduced first; these complete neck/cloth parts retain every authored triangle.
// The base .003 QEM bound is never used as an error claim for the new parts.
public static class FishNeckCollarAssemblyGuides {
static readonly Vector4[] Cloth={new Vector4(0.000000000f,0.519880295f,0.037572600f,0.940000000f),
new Vector4(0.000000000f,0.515880295f,0.039272600f,0.900000000f),
new Vector4(0.013428828f,0.524615288f,0.026163096f,0.940000000f),
new Vector4(0.013860410f,0.520615288f,0.027807400f,0.900000000f),
new Vector4(0.023194017f,0.531874239f,0.015173211f,0.940000000f),
new Vector4(0.024044017f,0.527874239f,0.016645454f,0.900000000f),
new Vector4(0.029628452f,0.536111325f,0.004749838f,0.940000000f),
new Vector4(0.030828074f,0.532111325f,0.005954374f,0.900000000f),
new Vector4(0.034957144f,0.539432406f,-0.004817485f,0.940000000f),
new Vector4(0.036429388f,0.535432406f,-0.003967485f,0.900000000f),
new Vector4(0.039439034f,0.542913258f,-0.014489739f,0.940000000f),
new Vector4(0.041081704f,0.538913258f,-0.014051978f,0.900000000f),
new Vector4(0.042362023f,0.545984566f,-0.025000002f,0.940000000f),
new Vector4(0.044062023f,0.541984566f,-0.025000002f,0.900000000f),
new Vector4(0.043254703f,0.548529446f,-0.036753070f,0.940000000f),
new Vector4(0.044895221f,0.544529446f,-0.037198827f,0.900000000f),
new Vector4(0.041457899f,0.549949646f,-0.048935734f,0.940000000f),
new Vector4(0.042930142f,0.545949646f,-0.049785734f,0.900000000f),
new Vector4(0.036353730f,0.550378740f,-0.061603509f,0.940000000f),
new Vector4(0.037551689f,0.546378740f,-0.062809699f,0.900000000f),
new Vector4(0.027760033f,0.550046861f,-0.073081776f,0.940000000f),
new Vector4(0.028610033f,0.546046861f,-0.074554020f,0.900000000f),
new Vector4(0.015148124f,0.548514128f,-0.081718162f,0.940000000f),
new Vector4(0.015586780f,0.544514128f,-0.083360593f,0.900000000f),
new Vector4(-0.000000006f,0.547588527f,-0.086766697f,0.940000000f),
new Vector4(-0.000000006f,0.543588527f,-0.088466697f,0.900000000f),
new Vector4(-0.015157797f,0.548510194f,-0.081759665f,0.940000000f),
new Vector4(-0.015596415f,0.544510194f,-0.083402107f,0.900000000f),
new Vector4(-0.027774662f,0.550070584f,-0.073107123f,0.940000000f),
new Vector4(-0.028624662f,0.546070584f,-0.074579367f,0.900000000f),
new Vector4(-0.036698518f,0.550827503f,-0.061916407f,0.940000000f),
new Vector4(-0.037897037f,0.546827503f,-0.063122041f,0.900000000f),
new Vector4(-0.041860282f,0.550382793f,-0.049168039f,0.940000000f),
new Vector4(-0.043332526f,0.546382793f,-0.050018039f,0.900000000f),
new Vector4(-0.043350160f,0.548706442f,-0.036780257f,0.940000000f),
new Vector4(-0.044990666f,0.544706442f,-0.037226059f,0.900000000f),
new Vector4(-0.042404078f,0.546078086f,-0.024999999f,0.940000000f),
new Vector4(-0.044104078f,0.542078086f,-0.024999998f,0.900000000f),
new Vector4(-0.040376239f,0.543833673f,-0.014153296f,0.940000000f),
new Vector4(-0.042018028f,0.539833673f,-0.013712244f,0.900000000f),
new Vector4(-0.036644340f,0.540572107f,-0.003843387f,0.940000000f),
new Vector4(-0.038116583f,0.536572107f,-0.002993387f,0.900000000f),
new Vector4(-0.031062447f,0.536506832f,0.006173277f,0.940000000f),
new Vector4(-0.032262386f,0.532506832f,0.007377498f,0.900000000f),
new Vector4(-0.023390278f,0.532159686f,0.015513131f,0.940000000f),
new Vector4(-0.024240279f,0.528159686f,0.016985374f,0.900000000f),
new Vector4(-0.013314443f,0.525144398f,0.025760302f,0.940000000f),
new Vector4(-0.013745762f,0.521144398f,0.027404676f,0.900000000f),
new Vector4(0.000000000f,0.519880295f,0.037572600f,0.940000000f),
new Vector4(0.000000000f,0.515880295f,0.039272600f,0.900000000f),
new Vector4(0.000000000f,0.519880295f,0.037572600f,0.940000000f),
new Vector4(0.000000000f,0.520541789f,0.040218249f,1.000000000f),
new Vector4(-0.000000000f,0.515530078f,0.046407637f,0.985000000f),
new Vector4(-0.000000001f,0.511564633f,0.046832904f,0.910000000f),
new Vector4(0.009759679f,0.522557974f,0.030349890f,0.940000000f),
new Vector4(0.010217865f,0.522829139f,0.034048386f,1.000000000f),
new Vector4(0.011993332f,0.516304902f,0.044317557f,0.985000000f),
new Vector4(0.012623336f,0.511802753f,0.046190488f,0.910000000f),
new Vector4(0.017097976f,0.526672602f,0.021976301f,0.940000000f),
new Vector4(0.018928941f,0.524960391f,0.027991741f,1.000000000f),
new Vector4(0.026023929f,0.510750574f,0.047239070f,0.985000000f),
new Vector4(0.028541505f,0.503521284f,0.052297800f,0.910000000f),
new Vector4(0.023194017f,0.531874239f,0.015173211f,0.940000000f),
new Vector4(0.026439048f,0.529082231f,0.021608516f,1.000000000f),
new Vector4(0.039013546f,0.510688198f,0.042482822f,0.985000000f),
new Vector4(0.043475464f,0.501974186f,0.048118866f,0.910000000f),
new Vector4(0.027724706f,0.535090983f,0.008041019f,0.940000000f),
new Vector4(0.032562374f,0.532325839f,0.014434926f,1.000000000f),
new Vector4(0.051308337f,0.514035908f,0.035148816f,0.985000000f),
new Vector4(0.057960130f,0.505358836f,0.040727939f,0.910000000f),
new Vector4(0.031532198f,0.537131667f,0.001458657f,0.940000000f),
new Vector4(0.037445450f,0.534866039f,0.006929959f,1.000000000f),
new Vector4(0.060359300f,0.518511730f,0.024068757f,0.985000000f),
new Vector4(0.068490021f,0.510521492f,0.028379298f,0.910000000f),
new Vector4(0.034957144f,0.539432406f,-0.004817485f,0.940000000f),
new Vector4(0.041680713f,0.537747261f,-0.000456528f,1.000000000f),
new Vector4(0.067734543f,0.523642322f,0.012379680f,0.985000000f),
new Vector4(0.076979451f,0.516450247f,0.015163495f,0.910000000f),
new Vector4(0.037902698f,0.541658878f,-0.011204544f,0.940000000f),
new Vector4(0.044988012f,0.540616624f,-0.008047508f,1.000000000f),
new Vector4(0.072443603f,0.529002887f,0.000123506f,0.985000000f),
new Vector4(0.082185909f,0.522694787f,0.001251931f,0.910000000f),
new Vector4(0.040975370f,0.544167638f,-0.017774934f,0.940000000f),
new Vector4(0.046693844f,0.543521711f,-0.015905688f,1.000000000f),
new Vector4(0.068852932f,0.533443745f,-0.012724860f,0.985000000f),
new Vector4(0.076715834f,0.527680596f,-0.013367147f,0.910000000f),
new Vector4(0.042362023f,0.545984566f,-0.025000002f,0.940000000f),
new Vector4(0.048134878f,0.545411224f,-0.024014093f,1.000000000f),
new Vector4(0.070504690f,0.535614521f,-0.024256195f,0.985000000f),
new Vector4(0.078442365f,0.529951175f,-0.026113069f,0.910000000f),
new Vector4(0.000000000f,0.519880295f,0.037572600f,0.940000000f),
new Vector4(0.000000000f,0.520541789f,0.040218249f,1.000000000f),
new Vector4(-0.000000000f,0.515530078f,0.046407637f,0.985000000f),
new Vector4(-0.000000001f,0.511564633f,0.046832904f,0.910000000f),
new Vector4(-0.009699166f,0.523124576f,0.030006617f,0.940000000f),
new Vector4(-0.010152930f,0.523383018f,0.033680035f,1.000000000f),
new Vector4(-0.011911264f,0.516809484f,0.043852027f,0.985000000f),
new Vector4(-0.012535189f,0.512289842f,0.045690476f,0.910000000f),
new Vector4(-0.016929720f,0.527164221f,0.021513987f,0.940000000f),
new Vector4(-0.018678897f,0.525415541f,0.027275522f,1.000000000f),
new Vector4(-0.025456957f,0.511064409f,0.045538971f,0.985000000f),
new Vector4(-0.027862076f,0.503784975f,0.050248581f,0.910000000f),
new Vector4(-0.023390278f,0.532159686f,0.015513131f,0.940000000f),
new Vector4(-0.026608838f,0.529337665f,0.021900849f,1.000000000f),
new Vector4(-0.039080756f,0.510827335f,0.042590756f,0.985000000f),
new Vector4(-0.043506276f,0.502072057f,0.048161368f,0.910000000f),
new Vector4(-0.028973443f,0.535289407f,0.009529190f,0.940000000f),
new Vector4(-0.033666714f,0.532595751f,0.015728513f,1.000000000f),
new Vector4(-0.051853138f,0.514582833f,0.035688387f,0.985000000f),
new Vector4(-0.058306385f,0.506004056f,0.040999956f,0.910000000f),
new Vector4(-0.033151451f,0.537724257f,0.002817364f,0.940000000f),
new Vector4(-0.039129554f,0.535535456f,0.008261615f,1.000000000f),
new Vector4(-0.062294699f,0.519478856f,0.025295587f,0.985000000f),
new Vector4(-0.070514590f,0.511594256f,0.029568932f,0.910000000f),
new Vector4(-0.036644340f,0.540572107f,-0.003843387f,0.940000000f),
new Vector4(-0.043431685f,0.538867039f,0.000478190f,1.000000000f),
new Vector4(-0.069732648f,0.524684901f,0.013161798f,0.985000000f),
new Vector4(-0.079065248f,0.517465433f,0.015891466f,0.910000000f),
new Vector4(-0.039727375f,0.543307126f,-0.010540422f,0.940000000f),
new Vector4(-0.046520738f,0.542001151f,-0.007489648f,1.000000000f),
new Vector4(-0.072845020f,0.529365501f,0.000269601f,0.985000000f),
new Vector4(-0.082185894f,0.522694787f,0.001251915f,0.910000000f),
new Vector4(-0.041025102f,0.544360220f,-0.017766170f,0.940000000f),
new Vector4(-0.046735616f,0.543683461f,-0.015898328f,1.000000000f),
new Vector4(-0.068863856f,0.533486021f,-0.012722941f,0.985000000f),
new Vector4(-0.076715812f,0.527680477f,-0.013367159f,0.910000000f),
new Vector4(-0.042404078f,0.546078086f,-0.024999999f,0.940000000f),
new Vector4(-0.048170199f,0.545489752f,-0.024014090f,1.000000000f),
new Vector4(-0.070513919f,0.535634956f,-0.024256195f,0.985000000f),
new Vector4(-0.078442335f,0.529950997f,-0.026113071f,0.910000000f),
new Vector4(0.042362023f,0.545984566f,-0.025000002f,0.940000000f),
new Vector4(0.048725321f,0.541592590f,-0.025143461f,0.970000000f),
new Vector4(0.053931655f,0.535817337f,-0.025260836f,0.945000000f),
new Vector4(0.042531282f,0.547367573f,-0.032499414f,0.940000000f),
new Vector4(0.048893983f,0.543732587f,-0.033570692f,0.970000000f),
new Vector4(0.054099830f,0.538576689f,-0.034447193f,0.945000000f),
new Vector4(0.043978125f,0.549691319f,-0.041006725f,0.940000000f),
new Vector4(0.048496455f,0.545169488f,-0.042455515f,0.970000000f),
new Vector4(0.052193271f,0.539287990f,-0.043640889f,0.945000000f),
new Vector4(0.041457899f,0.549949646f,-0.048935734f,0.940000000f),
new Vector4(0.046660607f,0.544026684f,-0.051439953f,0.970000000f),
new Vector4(0.050917368f,0.536998806f,-0.053488859f,0.945000000f),
new Vector4(0.038117696f,0.550082088f,-0.056984544f,0.940000000f),
new Vector4(0.046451988f,0.538982221f,-0.062727564f,0.970000000f),
new Vector4(0.053270955f,0.527718694f,-0.067426398f,0.945000000f),
new Vector4(0.034589764f,0.550675392f,-0.066222474f,0.940000000f),
new Vector4(0.038446176f,0.538355607f,-0.069445397f,0.970000000f),
new Vector4(0.041601422f,0.526093964f,-0.072082334f,0.945000000f),
new Vector4(0.027760033f,0.550046861f,-0.073081776f,0.940000000f),
new Vector4(0.029099468f,0.538532491f,-0.074172707f,0.970000000f),
new Vector4(0.030195370f,0.526929824f,-0.075065287f,0.945000000f),
new Vector4(0.019967880f,0.548830092f,-0.079861283f,0.940000000f),
new Vector4(0.020041874f,0.537290400f,-0.079023248f,0.970000000f),
new Vector4(0.020102414f,0.525667015f,-0.078337582f,0.945000000f),
new Vector4(0.010328367f,0.548198164f,-0.083575040f,0.940000000f),
new Vector4(0.009972482f,0.536636685f,-0.081338713f,0.970000000f),
new Vector4(0.009681303f,0.524995475f,-0.079508990f,0.945000000f),
new Vector4(-0.000000006f,0.547588527f,-0.086766697f,0.940000000f),
new Vector4(-0.000000005f,0.536320543f,-0.084120762f,0.970000000f),
new Vector4(-0.000000005f,0.524919465f,-0.081955906f,0.945000000f),
new Vector4(-0.010338562f,0.548162818f,-0.083632886f,0.940000000f),
new Vector4(-0.009932421f,0.536701302f,-0.081329546f,0.970000000f),
new Vector4(-0.009600124f,0.525141879f,-0.079444994f,0.945000000f),
new Vector4(-0.019977033f,0.548857570f,-0.079886444f,0.940000000f),
new Vector4(-0.019666509f,0.537150126f,-0.078960359f,0.970000000f),
new Vector4(-0.019412444f,0.525389490f,-0.078202653f,0.945000000f),
new Vector4(-0.027774662f,0.550070584f,-0.073107123f,0.940000000f),
new Vector4(-0.028135423f,0.537978416f,-0.073312897f,0.970000000f),
new Vector4(-0.028430592f,0.525903006f,-0.073481257f,0.945000000f),
new Vector4(-0.034723539f,0.550827920f,-0.066381902f,0.940000000f),
new Vector4(-0.037910414f,0.538171561f,-0.069094864f,0.970000000f),
new Vector4(-0.040517858f,0.525634540f,-0.071314561f,0.945000000f),
new Vector4(-0.038673498f,0.550827086f,-0.057450913f,0.940000000f),
new Vector4(-0.046681780f,0.539346197f,-0.062922769f,0.970000000f),
new Vector4(-0.053234011f,0.527770925f,-0.067399742f,0.945000000f),
new Vector4(-0.041860282f,0.550382793f,-0.049168039f,0.940000000f),
new Vector4(-0.046643449f,0.544307797f,-0.051448021f,0.970000000f),
new Vector4(-0.050556949f,0.537155528f,-0.053313460f,0.945000000f),
new Vector4(-0.044088539f,0.549857497f,-0.041046910f,0.940000000f),
new Vector4(-0.048485153f,0.545267845f,-0.042454282f,0.970000000f),
new Vector4(-0.052082382f,0.539330856f,-0.043605769f,0.945000000f),
new Vector4(-0.042611782f,0.547555387f,-0.032513604f,0.940000000f),
new Vector4(-0.048690816f,0.544089704f,-0.033556753f,0.970000000f),
new Vector4(-0.053664571f,0.539072327f,-0.034410238f,0.945000000f),
new Vector4(-0.042404078f,0.546078086f,-0.024999999f,0.940000000f),
new Vector4(-0.048443097f,0.541979251f,-0.025107638f,0.970000000f),
new Vector4(-0.053384113f,0.536443841f,-0.025195707f,0.945000000f),
new Vector4(0.053931655f,0.535817337f,-0.025260836f,0.945000000f),
new Vector4(0.071579366f,0.532457701f,-0.025961444f,0.990000000f),
new Vector4(0.078442365f,0.531151175f,-0.025713069f,0.910000000f),
new Vector4(0.054099830f,0.538576689f,-0.034447193f,0.945000000f),
new Vector4(0.073043134f,0.534540440f,-0.038170060f,0.990000000f),
new Vector4(0.080409974f,0.532137454f,-0.038825683f,0.910000000f),
new Vector4(0.052193271f,0.539287990f,-0.043640889f,0.945000000f),
new Vector4(0.071757554f,0.532745448f,-0.050596495f,0.990000000f),
new Vector4(0.079365887f,0.528757751f,-0.052246213f,0.910000000f),
new Vector4(0.050917368f,0.536998806f,-0.053488859f,0.945000000f),
new Vector4(0.071736838f,0.525146406f,-0.064891453f,0.990000000f),
new Vector4(0.079833299f,0.518870473f,-0.068023711f,0.910000000f),
new Vector4(0.053270955f,0.527718694f,-0.067426398f,0.945000000f),
new Vector4(0.065018834f,0.493329097f,-0.079810020f,0.990000000f),
new Vector4(0.069587454f,0.478511989f,-0.083100684f,0.910000000f),
new Vector4(0.041601422f,0.526093964f,-0.072082334f,0.945000000f),
new Vector4(0.046590324f,0.488025090f,-0.080092430f,0.990000000f),
new Vector4(0.048530452f,0.472387195f,-0.081489690f,0.910000000f),
new Vector4(0.030195370f,0.526929824f,-0.075065287f,0.945000000f),
new Vector4(0.033219094f,0.488365603f,-0.081067210f,0.990000000f),
new Vector4(0.034394987f,0.473368406f,-0.081527293f,0.910000000f),
new Vector4(0.020102414f,0.525667015f,-0.078337582f,0.945000000f),
new Vector4(0.019921397f,0.482758792f,-0.079408527f,0.990000000f),
new Vector4(0.019851001f,0.466905594f,-0.077835903f,0.910000000f),
new Vector4(0.009681303f,0.524995475f,-0.079508990f,0.945000000f),
new Vector4(0.009337112f,0.481120187f,-0.078674910f,0.990000000f),
new Vector4(0.009203260f,0.465500951f,-0.076290950f,0.910000000f),
new Vector4(-0.000000005f,0.524919465f,-0.081955906f,0.945000000f),
new Vector4(0.000071586f,0.480677231f,-0.078339960f,0.990000000f),
new Vector4(0.000099426f,0.465138584f,-0.074850425f,0.910000000f),
new Vector4(-0.009600124f,0.525141879f,-0.079444994f,0.945000000f),
new Vector4(-0.009159295f,0.481427084f,-0.078297060f,0.990000000f),
new Vector4(-0.008987861f,0.465870261f,-0.075791046f,0.910000000f),
new Vector4(-0.019412444f,0.525389490f,-0.078202653f,0.945000000f),
new Vector4(-0.019369635f,0.482990182f,-0.079136075f,0.990000000f),
new Vector4(-0.019352987f,0.467334896f,-0.077509969f,0.910000000f),
new Vector4(-0.028430592f,0.525903006f,-0.073481257f,0.945000000f),
new Vector4(-0.033091223f,0.488412169f,-0.081199396f,0.990000000f),
new Vector4(-0.034903690f,0.473832399f,-0.082326896f,0.910000000f),
new Vector4(-0.040517858f,0.525634540f,-0.071314561f,0.945000000f),
new Vector4(-0.046270164f,0.487824740f,-0.079862707f,0.990000000f),
new Vector4(-0.048507173f,0.472287595f,-0.081469208f,0.910000000f),
new Vector4(-0.053234011f,0.527770925f,-0.067399742f,0.945000000f),
new Vector4(-0.064243932f,0.492271374f,-0.079084738f,0.990000000f),
new Vector4(-0.068525568f,0.477022618f,-0.082103714f,0.910000000f),
new Vector4(-0.050556949f,0.537155528f,-0.053313460f,0.945000000f),
new Vector4(-0.071635873f,0.525190288f,-0.064842314f,0.990000000f),
new Vector4(-0.079833232f,0.518870473f,-0.068023674f,0.910000000f),
new Vector4(-0.052082382f,0.539330856f,-0.043605769f,0.945000000f),
new Vector4(-0.071726473f,0.532757451f,-0.050586656f,0.990000000f),
new Vector4(-0.079365842f,0.528757751f,-0.052246206f,0.910000000f),
new Vector4(-0.053664571f,0.539072327f,-0.034410238f,0.945000000f),
new Vector4(-0.072921245f,0.534679132f,-0.038159713f,0.990000000f),
new Vector4(-0.080409952f,0.532137334f,-0.038825683f,0.910000000f),
new Vector4(-0.053384113f,0.536443841f,-0.025195707f,0.945000000f),
new Vector4(-0.071426033f,0.532632993f,-0.025943209f,0.990000000f),
new Vector4(-0.078442335f,0.531150997f,-0.025713071f,0.910000000f)};
static readonly int[] ClothTriangles={0,2,1,1,2,3,2,4,3,3,4,5,4,6,5,5,6,7,6,8,7,7,8,9,8,10,9,9,10,11,10,12,11,11,12,13,12,14,13,13,14,15,14,16,15,15,16,17,16,18,17,17,18,19,18,20,19,19,20,21,20,22,21,21,22,23,22,24,23,23,24,25,24,26,25,25,26,27,26,28,27,27,28,29,28,30,29,29,30,31,30,32,31,31,32,33,32,34,33,33,34,35,34,36,35,35,36,37,36,38,37,37,38,39,38,40,39,39,40,41,40,42,41,41,42,43,42,44,43,43,44,45,44,46,45,45,46,47,46,48,47,47,48,49,50,54,51,51,54,55,51,55,52,52,55,56,52,56,53,53,56,57,54,58,55,55,58,59,55,59,56,56,59,60,56,60,57,57,60,61,58,62,59,59,62,63,59,63,60,60,63,64,60,64,61,61,64,65,62,66,63,63,66,67,63,67,64,64,67,68,64,68,65,65,68,69,66,70,67,67,70,71,67,71,68,68,71,72,68,72,69,69,72,73,70,74,71,71,74,75,71,75,72,72,75,76,72,76,73,73,76,77,74,78,75,75,78,79,75,79,76,76,79,80,76,80,77,77,80,81,78,82,79,79,82,83,79,83,80,80,83,84,80,84,81,81,84,85,82,86,83,83,86,87,83,87,84,84,87,88,84,88,85,85,88,89,90,94,91,91,94,95,91,95,92,92,95,96,92,96,93,93,96,97,94,98,95,95,98,99,95,99,96,96,99,100,96,100,97,97,100,101,98,102,99,99,102,103,99,103,100,100,103,104,100,104,101,101,104,105,102,106,103,103,106,107,103,107,104,104,107,108,104,108,105,105,108,109,106,110,107,107,110,111,107,111,108,108,111,112,108,112,109,109,112,113,110,114,111,111,114,115,111,115,112,112,115,116,112,116,113,113,116,117,114,118,115,115,118,119,115,119,116,116,119,120,116,120,117,117,120,121,118,122,119,119,122,123,119,123,120,120,123,124,120,124,121,121,124,125,122,126,123,123,126,127,123,127,124,124,127,128,124,128,125,125,128,129,130,133,131,131,133,134,131,134,132,132,134,135,133,136,134,134,136,137,134,137,135,135,137,138,136,139,137,137,139,140,137,140,138,138,140,141,139,142,140,140,142,143,140,143,141,141,143,144,142,145,143,143,145,146,143,146,144,144,146,147,145,148,146,146,148,149,146,149,147,147,149,150,148,151,149,149,151,152,149,152,150,150,152,153,151,154,152,152,154,155,152,155,153,153,155,156,154,157,155,155,157,158,155,158,156,156,158,159,157,160,158,158,160,161,158,161,159,159,161,162,160,163,161,161,163,164,161,164,162,162,164,165,163,166,164,164,166,167,164,167,165,165,167,168,166,169,167,167,169,170,167,170,168,168,170,171,169,172,170,170,172,173,170,173,171,171,173,174,172,175,173,173,175,176,173,176,174,174,176,177,175,178,176,176,178,179,176,179,177,177,179,180,178,181,179,179,181,182,179,182,180,180,182,183,181,184,182,182,184,185,182,185,183,183,185,186,187,190,188,188,190,191,188,191,189,189,191,192,190,193,191,191,193,194,191,194,192,192,194,195,193,196,194,194,196,197,194,197,195,195,197,198,196,199,197,197,199,200,197,200,198,198,200,201,199,202,200,200,202,203,200,203,201,201,203,204,202,205,203,203,205,206,203,206,204,204,206,207,205,208,206,206,208,209,206,209,207,207,209,210,208,211,209,209,211,212,209,212,210,210,212,213,211,214,212,212,214,215,212,215,213,213,215,216,214,217,215,215,217,218,215,218,216,216,218,219,217,220,218,218,220,221,218,221,219,219,221,222,220,223,221,221,223,224,221,224,222,222,224,225,223,226,224,224,226,227,224,227,225,225,227,228,226,229,227,227,229,230,227,230,228,228,230,231,229,232,230,230,232,233,230,233,231,231,233,234,232,235,233,233,235,236,233,236,234,234,236,237,235,238,236,236,238,239,236,239,237,237,239,240,238,241,239,239,241,242,239,242,240,240,242,243};
static bool Unpainted(Vector2 uv)=>Mathf.Abs(uv.x-.995f)<.000001f;
static bool Neck(int i,BoneWeight[] w,Vector2[] uv,Color[] c,int neck)=>w[i].boneIndex0==neck&&Unpainted(uv[i])&&c[i].r-c[i].g>.025f;
static bool Collar(int i,BoneWeight[] w,Vector2[] uv,Color[] c,int chest)=>w[i].boneIndex0==chest&&w[i].weight0>.999f&&Unpainted(uv[i])&&Mathf.Min(c[i].r,Mathf.Min(c[i].g,c[i].b))>.75f;
static Vector3 Unwarp(Vector3 p) {
 // Reuse the author inverse; remove only the obsolete physical neck offset.
 float oldY=FishRebuiltModel.AuthorPoint(p).y;
 float t=Mathf.InverseLerp(.520f,.590f,oldY);p.z+=.025f*Mathf.Sin(t*Mathf.PI);
 if(p.y<.520f)return new Vector3(0,.495f,.032f);
 if(p.y<.540f){float center=(-.0657069f+.0042306f)*.5f,radius=(.0042306f+.0657069f)*.5f;return new Vector3(p.x/.0421489f*.059f,.525f,(-.06554f+.00665f)*.5f+(p.z-center)/radius*(.00665f+.06554f)*.5f);}
 return p;
}
static BoneWeight NeckWeights(Vector3 point,BoneWeight old,int chest,int neck){
 if(point.y>=.545f)return old;
 float neckShare=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.495f,.545f,point.y));
 return new BoneWeight{boneIndex0=chest,boneIndex1=neck,weight0=1-neckShare,weight1=neckShare};
}
public static Mesh CaptureNeck(Mesh authored,int neck,int chest){
 var positions=authored.vertices;var weights=authored.boneWeights;var colors=authored.colors;var uv=authored.uv;var tri=authored.triangles;
 var selected=Enumerable.Range(0,positions.Length).Select(i=>Neck(i,weights,uv,colors,neck)).ToArray();
 var remap=new Dictionary<int,int>();var merged=new Dictionary<string,int>();var p=new List<Vector3>();var w=new List<BoneWeight>();var c=new List<Color>();var st=new List<Vector2>();var indices=new List<int>();
 foreach(int i in tri.Where(i=>selected[i]).Distinct()){var q=Unwarp(positions[i]);var weight=NeckWeights(q,weights[i],chest,neck);string key=Vector3Int.RoundToInt(q*10000000).ToString()+","+weight.boneIndex0+","+weight.boneIndex1+","+weight.weight0.ToString("R");if(!merged.TryGetValue(key,out int j)){j=p.Count;merged[key]=j;p.Add(q);w.Add(weight);c.Add(colors[i]);st.Add(uv[i]);}remap[i]=j;}
 for(int k=0;k<tri.Length;k+=3){int a=tri[k],b=tri[k+1],d=tri[k+2];bool on=selected[a]||selected[b]||selected[d];if(!on)continue;if(!(selected[a]&&selected[b]&&selected[d]))throw new InvalidOperationException("135 mixed native neck boundary");int ia=remap[a],ib=remap[b],ic=remap[d];if(ia==ib||ib==ic||ic==ia)continue;indices.AddRange(new[]{ia,ib,ic});}
 var mesh=new Mesh{name="Independent complete neck135"};mesh.vertices=p.ToArray();mesh.colors=c.ToArray();mesh.uv=st.ToArray();mesh.boneWeights=w.ToArray();mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
}
static Matrix4x4[] DefaultMatrices(Transform root,Transform[] bones,Matrix4x4[] bind){
 var all=root.GetComponentsInChildren<Transform>(true);var p=all.Select(t=>t.localPosition).ToArray();var q=all.Select(t=>t.localRotation).ToArray();var scales=all.Select(t=>t.localScale).ToArray();
 var existing=root.GetComponent<FishRigProfile>();if(existing)throw new InvalidOperationException("135 capture expects bind-root before profile");var profile=root.gameObject.AddComponent<FishRigProfile>();profile.articulatedHandRig=true;
 Matrix4x4[] matrices;try{new FishMotion(root){randomActions=false}.Tick(0,1,0,0,"idle",false);matrices=bones.Select((b,i)=>root.worldToLocalMatrix*b.localToWorldMatrix*bind[i]).ToArray();}
 finally{for(int i=0;i<all.Length;i++){all[i].localPosition=p[i];all[i].localRotation=q[i];all[i].localScale=scales[i];}UnityEngine.Object.DestroyImmediate(profile);}return matrices;
}
static BoneWeight ClothWeights(Vector3 p,int chest,int neck,int left,int right){
 float x=Mathf.Abs(p.x),arm=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.070f,.180f,x));
 if(arm>0)return new BoneWeight{boneIndex0=chest,boneIndex1=p.x<0?left:right,weight0=1-arm,weight1=arm};
 float neckShare=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.515f,.550f,p.y))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.045f,.065f,x)))*.72f;
 return new BoneWeight{boneIndex0=chest,boneIndex1=neck,weight0=1-neckShare,weight1=neckShare};
}
static Matrix4x4 Blend(Matrix4x4 a,Matrix4x4 b,float t){var result=Matrix4x4.zero;for(int i=0;i<16;i++)result[i]=a[i]*(1-t)+b[i]*t;return result;}
public static Mesh Apply(Mesh reduced,Mesh completeNeck,Transform root,Transform[] bones,int chest,int neck,int left,int right){
 var oldP=reduced.vertices;var oldN=reduced.normals;var oldUV=reduced.uv;var oldC=reduced.colors;var oldW=reduced.boneWeights;var oldT=reduced.triangles;
 var selected=Enumerable.Range(0,oldP.Length).Select(i=>Neck(i,oldW,oldUV,oldC,neck)||Collar(i,oldW,oldUV,oldC,chest)).ToArray();var keep=new List<int>();int removed=0;for(int k=0;k<oldT.Length;k+=3){var ids=new[]{oldT[k],oldT[k+1],oldT[k+2]};if(ids.Any(i=>selected[i])){if(!ids.All(i=>selected[i]))throw new InvalidOperationException("135 mixed reduced assembly boundary");removed++;}else keep.AddRange(ids);}
 var used=keep.Distinct().OrderBy(i=>i).ToArray();var map=new Dictionary<int,int>();for(int i=0;i<used.Length;i++)map[used[i]]=i;
 var p=used.Select(i=>oldP[i]).ToList();var n=used.Select(i=>oldN[i]).ToList();var uv=used.Select(i=>oldUV[i]).ToList();var colors=used.Select(i=>oldC[i]).ToList();var weights=used.Select(i=>oldW[i]).ToList();var tri=keep.Select(i=>map[i]).ToList();
 int neckStart=p.Count;p.AddRange(completeNeck.vertices);n.AddRange(completeNeck.normals);uv.AddRange(completeNeck.uv);colors.AddRange(completeNeck.colors);weights.AddRange(completeNeck.boneWeights);tri.AddRange(completeNeck.triangles.Select(i=>i+neckStart));
 var matrices=DefaultMatrices(root,bones,reduced.bindposes);var clothP=new Vector3[Cloth.Length];var clothW=new BoneWeight[Cloth.Length];var clothC=new Color[Cloth.Length];ColorUtility.TryParseHtmlString("#FFFCF3",out Color white);float maxResidual=0;
 for(int i=0;i<Cloth.Length;i++){Vector3 target=new Vector3(Cloth[i].x,Cloth[i].y,Cloth[i].z);var w=ClothWeights(target,chest,neck,left,right);var m=Blend(matrices[w.boneIndex0],matrices[w.boneIndex1],w.weight1);if(Mathf.Abs(m.determinant)<.000001f)throw new InvalidOperationException("135 singular own binding");clothP[i]=m.inverse.MultiplyPoint3x4(target);clothW[i]=w;clothC[i]=white*Cloth[i].w;clothC[i].a=1;maxResidual=Mathf.Max(maxResidual,Vector3.Distance(m.MultiplyPoint3x4(clothP[i]),target));}
 var clothTri=(int[])ClothTriangles.Clone();int[] starts={0,48*3,102*3,156*3,228*3,300*3};Vector3[] directions={Vector3.zero,new Vector3(0,1,.4f),new Vector3(0,1,.4f),Vector3.up,new Vector3(0,.1f,-1)};
 for(int part=0;part<5;part++){float score=0;for(int k=starts[part];k<starts[part+1];k+=3){var a=clothP[clothTri[k]];var b=clothP[clothTri[k+1]];var c=clothP[clothTri[k+2]];var middle=(a+b+c)/3;Vector3 desired=part==0?new Vector3(middle.x,0,middle.z+.025f):directions[part];score+=Vector3.Dot(Vector3.Cross(b-a,c-a),desired);}if(score<0)for(int k=starts[part];k<starts[part+1];k+=3){int tmp=clothTri[k+1];clothTri[k+1]=clothTri[k+2];clothTri[k+2]=tmp;}}
 var clothMesh=new Mesh();clothMesh.vertices=clothP;clothMesh.triangles=clothTri;clothMesh.RecalculateNormals();int clothStart=p.Count;p.AddRange(clothP);n.AddRange(clothMesh.normals);uv.AddRange(Enumerable.Repeat(new Vector2(.995f,(.995f*1024+128)/2048),Cloth.Length));colors.AddRange(clothC);weights.AddRange(clothW);tri.AddRange(clothTri.Select(i=>i+clothStart));
 var mesh=new Mesh{name=reduced.name,indexFormat=p.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};mesh.SetVertices(p);mesh.SetNormals(n);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.boneWeights=weights.ToArray();mesh.bindposes=reduced.bindposes;mesh.SetTriangles(tri,0);mesh.RecalculateBounds();if(mesh.triangles.Length/3+800>20000)throw new InvalidOperationException("135 exact assembly exceeds actual budget");if(maxResidual>2e-7f)throw new InvalidOperationException("135 inverse own default LBS");
 File.WriteAllText("work/rebuilt-assembly.json","{\"method\":\"unchanged base QEM, exact unreduced independent sparse neck and layered collar replacement\",\"outside_actual_vertices\":"+used.Length+",\"removed_actual_triangles\":"+removed+",\"full_new_neck_triangles\":"+completeNeck.triangles.Length/3+",\"new_cloth_triangles\":"+clothTri.Length/3+",\"final_body_triangles\":"+tri.Count/3+",\"new_component_reduction_error\":0,\"actual_UV_kept_exact\":true,\"own_two_slot_default_residual\":"+maxResidual.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
 UnityEngine.Object.DestroyImmediate(reduced);UnityEngine.Object.DestroyImmediate(completeNeck);UnityEngine.Object.DestroyImmediate(clothMesh);return mesh;
}
}
}
