using UnityEngine;

namespace DesktopFish.Editor {
    // Sparse alpha-visible transverse landmarks. Each strip is resampled as
    // a fresh regular surface; the reference topology and paint are not used.
    public static class FishRearHairGuides {
        static readonly Vector3[][] Rows = {
            new[] { // rear section family component-3
                new Vector3(-0.189260f,0.607501f,-0.234270f),new Vector3(-0.163814f,0.607501f,-0.254126f),new Vector3(-0.138060f,0.607501f,-0.273189f),new Vector3(-0.099599f,0.607501f,-0.259481f),new Vector3(-0.074103f,0.607501f,-0.279210f),
                new Vector3(-0.171000f,0.507501f,-0.266419f),new Vector3(-0.148010f,0.507501f,-0.290990f),new Vector3(-0.125855f,0.507501f,-0.317012f),new Vector3(-0.093751f,0.507501f,-0.325753f),new Vector3(-0.059875f,0.507501f,-0.331414f),
                new Vector3(-0.169816f,0.420000f,-0.348800f),new Vector3(-0.149563f,0.420000f,-0.373223f),new Vector3(-0.125968f,0.420000f,-0.386950f),new Vector3(-0.097507f,0.420000f,-0.385100f),new Vector3(-0.068860f,0.420000f,-0.382655f),
                new Vector3(-0.187191f,0.345001f,-0.394348f),new Vector3(-0.164179f,0.345001f,-0.404148f),new Vector3(-0.138349f,0.345001f,-0.385805f),new Vector3(-0.116596f,0.345001f,-0.408175f),new Vector3(-0.092276f,0.345001f,-0.404907f),
                new Vector3(-0.187349f,0.282500f,-0.400071f),new Vector3(-0.169098f,0.282500f,-0.406817f),new Vector3(-0.150806f,0.282500f,-0.412983f),new Vector3(-0.131812f,0.282500f,-0.409440f),new Vector3(-0.112783f,0.282500f,-0.405414f),
                new Vector3(-0.181667f,0.238750f,-0.391159f),new Vector3(-0.166579f,0.238750f,-0.396897f),new Vector3(-0.151471f,0.238750f,-0.401610f),new Vector3(-0.136189f,0.238750f,-0.397899f),new Vector3(-0.120873f,0.238750f,-0.392488f),
                new Vector3(-0.171465f,0.207500f,-0.387829f),new Vector3(-0.158789f,0.207500f,-0.389779f),new Vector3(-0.146233f,0.207500f,-0.390893f),new Vector3(-0.134524f,0.207500f,-0.386157f),new Vector3(-0.122907f,0.207500f,-0.380786f),
                new Vector3(-0.135176f,0.181200f,-0.396563f),new Vector3(-0.153296f,0.181200f,-0.404796f),new Vector3(-0.145522f,0.181200f,-0.419457f),new Vector3(-0.153316f,0.181200f,-0.430252f),new Vector3(-0.153948f,0.181200f,-0.442826f),
                new Vector3(-0.137416f,0.173700f,-0.409883f),new Vector3(-0.137416f,0.173700f,-0.409883f),new Vector3(-0.137416f,0.173700f,-0.409883f),new Vector3(-0.137416f,0.173700f,-0.409883f),new Vector3(-0.137416f,0.173700f,-0.409883f),
            },
            new[] { // rear section family component-7
                new Vector3(-0.144429f,0.607500f,-0.281526f),new Vector3(-0.117704f,0.607500f,-0.307703f),new Vector3(-0.091273f,0.607500f,-0.334499f),new Vector3(-0.053163f,0.607500f,-0.336725f),new Vector3(-0.014766f,0.607500f,-0.338349f),
                new Vector3(-0.133449f,0.507501f,-0.306098f),new Vector3(-0.110937f,0.507501f,-0.333773f),new Vector3(-0.089461f,0.507501f,-0.362920f),new Vector3(-0.043535f,0.507501f,-0.357282f),new Vector3(-0.014727f,0.507501f,-0.375999f),
                new Vector3(-0.126691f,0.420000f,-0.360172f),new Vector3(-0.111611f,0.420000f,-0.387413f),new Vector3(-0.096918f,0.420000f,-0.414999f),new Vector3(-0.069734f,0.420000f,-0.431440f),new Vector3(-0.030576f,0.420000f,-0.437195f),
                new Vector3(-0.130050f,0.345000f,-0.430809f),new Vector3(-0.112819f,0.345000f,-0.448121f),new Vector3(-0.095350f,0.345000f,-0.464936f),new Vector3(-0.070057f,0.345000f,-0.465482f),new Vector3(-0.044718f,0.345000f,-0.465933f),
                new Vector3(-0.131752f,0.282500f,-0.438513f),new Vector3(-0.117690f,0.282500f,-0.453531f),new Vector3(-0.102850f,0.282500f,-0.466820f),new Vector3(-0.079687f,0.282500f,-0.461618f),new Vector3(-0.062238f,0.282500f,-0.469110f),
                new Vector3(-0.128228f,0.238750f,-0.428065f),new Vector3(-0.119334f,0.238750f,-0.441786f),new Vector3(-0.108805f,0.238750f,-0.452824f),new Vector3(-0.089904f,0.238750f,-0.450130f),new Vector3(-0.077308f,0.238750f,-0.457779f),
                new Vector3(-0.118150f,0.209525f,-0.399895f),new Vector3(-0.115956f,0.209525f,-0.410442f),new Vector3(-0.112777f,0.209525f,-0.420081f),new Vector3(-0.102967f,0.209525f,-0.423610f),new Vector3(-0.092823f,0.209525f,-0.426831f),
                new Vector3(-0.116630f,0.207500f,-0.395999f),new Vector3(-0.115391f,0.207500f,-0.404839f),new Vector3(-0.113218f,0.207500f,-0.412931f),new Vector3(-0.105985f,0.207500f,-0.416969f),new Vector3(-0.096333f,0.207500f,-0.419068f),
                new Vector3(-0.111289f,0.202025f,-0.386976f),new Vector3(-0.111289f,0.202025f,-0.386976f),new Vector3(-0.111289f,0.202025f,-0.386976f),new Vector3(-0.111289f,0.202025f,-0.386976f),new Vector3(-0.111289f,0.202025f,-0.386976f),
            },
            new[] { // rear section family component-10
                new Vector3(-0.052817f,0.607500f,-0.322200f),new Vector3(-0.034563f,0.607500f,-0.335253f),new Vector3(-0.016475f,0.607500f,-0.349002f),new Vector3(0.006538f,0.607500f,-0.342032f),new Vector3(0.031342f,0.607500f,-0.327528f),
                new Vector3(-0.042206f,0.507500f,-0.348547f),new Vector3(-0.026239f,0.507500f,-0.363675f),new Vector3(-0.010261f,0.507500f,-0.378769f),new Vector3(0.011622f,0.507500f,-0.374779f),new Vector3(0.037633f,0.507500f,-0.357448f),
                new Vector3(-0.034776f,0.419999f,-0.412196f),new Vector3(-0.019867f,0.419999f,-0.428575f),new Vector3(-0.004968f,0.419999f,-0.445005f),new Vector3(0.014288f,0.419999f,-0.435943f),new Vector3(0.034719f,0.419999f,-0.420006f),
                new Vector3(-0.032282f,0.345000f,-0.467779f),new Vector3(-0.017686f,0.345000f,-0.477968f),new Vector3(-0.003180f,0.345000f,-0.485015f),new Vector3(0.010887f,0.345000f,-0.476792f),new Vector3(0.024928f,0.345000f,-0.467651f),
                new Vector3(-0.036005f,0.282500f,-0.476363f),new Vector3(-0.024844f,0.282500f,-0.484446f),new Vector3(-0.013650f,0.282500f,-0.491985f),new Vector3(-0.001578f,0.282500f,-0.485080f),new Vector3(0.010485f,0.282500f,-0.478321f),
                new Vector3(-0.033887f,0.238750f,-0.464113f),new Vector3(-0.025553f,0.238750f,-0.473305f),new Vector3(-0.017142f,0.238750f,-0.480398f),new Vector3(-0.008241f,0.238750f,-0.474240f),new Vector3(0.000654f,0.238750f,-0.468235f),
                new Vector3(-0.015925f,0.209689f,-0.449457f),new Vector3(-0.012039f,0.209689f,-0.444367f),new Vector3(-0.009588f,0.209689f,-0.438883f),new Vector3(-0.007382f,0.209689f,-0.433333f),new Vector3(-0.005693f,0.209689f,-0.427640f),
                new Vector3(-0.015545f,0.207500f,-0.445186f),new Vector3(-0.012763f,0.207500f,-0.439197f),new Vector3(-0.010133f,0.207500f,-0.433181f),new Vector3(-0.007989f,0.207500f,-0.427078f),new Vector3(-0.006195f,0.207500f,-0.420913f),
                new Vector3(-0.008615f,0.202189f,-0.404640f),new Vector3(-0.008615f,0.202189f,-0.404640f),new Vector3(-0.008615f,0.202189f,-0.404640f),new Vector3(-0.008615f,0.202189f,-0.404640f),new Vector3(-0.008615f,0.202189f,-0.404640f),
            },
            new[] { // rear section family component-6
                new Vector3(-0.005034f,0.607501f,-0.341920f),new Vector3(0.035749f,0.607501f,-0.323577f),new Vector3(0.086547f,0.607501f,-0.331938f),new Vector3(0.123077f,0.607501f,-0.302255f),new Vector3(0.159574f,0.607501f,-0.272487f),
                new Vector3(0.002048f,0.507501f,-0.381241f),new Vector3(0.046624f,0.507501f,-0.373687f),new Vector3(0.090564f,0.507501f,-0.364852f),new Vector3(0.122267f,0.507501f,-0.331402f),new Vector3(0.154140f,0.507501f,-0.298291f),
                new Vector3(0.020107f,0.419999f,-0.443464f),new Vector3(0.055162f,0.419999f,-0.422809f),new Vector3(0.086760f,0.419999f,-0.398081f),new Vector3(0.124151f,0.419999f,-0.380176f),new Vector3(0.145782f,0.419999f,-0.343708f),
                new Vector3(0.031404f,0.345001f,-0.471143f),new Vector3(0.062500f,0.345001f,-0.468394f),new Vector3(0.093052f,0.345001f,-0.464262f),new Vector3(0.113344f,0.345001f,-0.434073f),new Vector3(0.140191f,0.345001f,-0.420530f),
                new Vector3(0.040995f,0.282500f,-0.476028f),new Vector3(0.065375f,0.282500f,-0.474302f),new Vector3(0.089862f,0.282500f,-0.472871f),new Vector3(0.109635f,0.282500f,-0.458333f),new Vector3(0.129214f,0.282500f,-0.443253f),
                new Vector3(0.051246f,0.238750f,-0.459354f),new Vector3(0.069629f,0.238750f,-0.458337f),new Vector3(0.088389f,0.238750f,-0.458233f),new Vector3(0.102651f,0.238750f,-0.447239f),new Vector3(0.116726f,0.238750f,-0.435796f),
                new Vector3(0.064373f,0.209458f,-0.423652f),new Vector3(0.073738f,0.209458f,-0.427589f),new Vector3(0.083434f,0.209458f,-0.432560f),new Vector3(0.088565f,0.209458f,-0.423211f),new Vector3(0.094238f,0.209458f,-0.415560f),
                new Vector3(0.065838f,0.207500f,-0.418593f),new Vector3(0.072912f,0.207500f,-0.422902f),new Vector3(0.079896f,0.207500f,-0.426466f),new Vector3(0.085712f,0.207500f,-0.420256f),new Vector3(0.091432f,0.207500f,-0.413246f),
                new Vector3(0.072275f,0.201959f,-0.396878f),new Vector3(0.072275f,0.201959f,-0.396878f),new Vector3(0.072275f,0.201959f,-0.396878f),new Vector3(0.072275f,0.201959f,-0.396878f),new Vector3(0.072275f,0.201959f,-0.396878f),
            },
            new[] { // rear section family component-2
                new Vector3(0.070802f,0.607501f,-0.282547f),new Vector3(0.102522f,0.607501f,-0.279766f),new Vector3(0.121613f,0.607501f,-0.244388f),new Vector3(0.159987f,0.607501f,-0.258778f),new Vector3(0.185188f,0.607501f,-0.239170f),
                new Vector3(0.067867f,0.507501f,-0.316867f),new Vector3(0.098832f,0.507501f,-0.315078f),new Vector3(0.130099f,0.507501f,-0.314120f),new Vector3(0.153165f,0.507501f,-0.290619f),new Vector3(0.176307f,0.507501f,-0.267324f),
                new Vector3(0.083321f,0.420000f,-0.371437f),new Vector3(0.107125f,0.420000f,-0.356078f),new Vector3(0.140345f,0.420000f,-0.379590f),new Vector3(0.164021f,0.420000f,-0.363703f),new Vector3(0.187621f,0.420000f,-0.347507f),
                new Vector3(0.095580f,0.345001f,-0.397880f),new Vector3(0.119161f,0.345001f,-0.403846f),new Vector3(0.142709f,0.345001f,-0.411718f),new Vector3(0.166516f,0.345001f,-0.404888f),new Vector3(0.190304f,0.345001f,-0.399188f),
                new Vector3(0.105704f,0.282500f,-0.401273f),new Vector3(0.125236f,0.282500f,-0.408832f),new Vector3(0.144788f,0.282500f,-0.416174f),new Vector3(0.166376f,0.282500f,-0.400829f),new Vector3(0.185924f,0.282500f,-0.408213f),
                new Vector3(0.107856f,0.238750f,-0.387714f),new Vector3(0.124071f,0.238750f,-0.396106f),new Vector3(0.140342f,0.238750f,-0.404167f),new Vector3(0.158388f,0.238750f,-0.401819f),new Vector3(0.176387f,0.238750f,-0.399746f),
                new Vector3(0.114899f,0.207500f,-0.385237f),new Vector3(0.127681f,0.207500f,-0.392381f),new Vector3(0.140402f,0.207500f,-0.399732f),new Vector3(0.154965f,0.207500f,-0.400922f),new Vector3(0.169293f,0.207500f,-0.402895f),
                new Vector3(0.137084f,0.177161f,-0.399311f),new Vector3(0.149190f,0.177161f,-0.410151f),new Vector3(0.157199f,0.177161f,-0.423036f),new Vector3(0.162056f,0.177161f,-0.437497f),new Vector3(0.165803f,0.177161f,-0.452512f),
                new Vector3(0.150205f,0.169661f,-0.412458f),new Vector3(0.150205f,0.169661f,-0.412458f),new Vector3(0.150205f,0.169661f,-0.412458f),new Vector3(0.150205f,0.169661f,-0.412458f),new Vector3(0.150205f,0.169661f,-0.412458f),
            },
            new[] { // rear section family inner-0
                new Vector3(-0.227265f,0.607499f,-0.105668f),new Vector3(-0.213898f,0.607499f,-0.151656f),new Vector3(-0.183105f,0.607499f,-0.182078f),new Vector3(-0.162516f,0.607499f,-0.221615f),new Vector3(-0.111799f,0.607499f,-0.234239f),
                new Vector3(-0.217553f,0.507498f,-0.107215f),new Vector3(-0.203151f,0.507498f,-0.157788f),new Vector3(-0.174452f,0.507498f,-0.196090f),new Vector3(-0.139756f,0.507498f,-0.229243f),new Vector3(-0.091259f,0.507498f,-0.250550f),
                new Vector3(-0.227684f,0.420000f,-0.130470f),new Vector3(-0.214437f,0.420000f,-0.186943f),new Vector3(-0.171957f,0.420000f,-0.215903f),new Vector3(-0.142170f,0.420000f,-0.256810f),new Vector3(-0.098875f,0.420000f,-0.285002f),
                new Vector3(-0.250795f,0.345000f,-0.171504f),new Vector3(-0.238058f,0.345000f,-0.228667f),new Vector3(-0.198994f,0.345000f,-0.263339f),new Vector3(-0.157883f,0.345000f,-0.296264f),new Vector3(-0.116739f,0.345000f,-0.329159f),
                new Vector3(-0.260631f,0.282500f,-0.202364f),new Vector3(-0.244468f,0.282500f,-0.256097f),new Vector3(-0.206866f,0.282500f,-0.290750f),new Vector3(-0.168647f,0.282500f,-0.324854f),new Vector3(-0.129733f,0.282500f,-0.358338f),
                new Vector3(-0.259698f,0.238750f,-0.222689f),new Vector3(-0.244168f,0.238750f,-0.270290f),new Vector3(-0.211187f,0.238750f,-0.304177f),new Vector3(-0.178272f,0.238750f,-0.338116f),new Vector3(-0.140986f,0.238750f,-0.368619f),
                new Vector3(-0.249323f,0.207500f,-0.252764f),new Vector3(-0.232379f,0.207500f,-0.287422f),new Vector3(-0.206590f,0.207500f,-0.314604f),new Vector3(-0.181318f,0.207500f,-0.342224f),new Vector3(-0.152761f,0.207500f,-0.367066f),
                new Vector3(-0.187512f,0.164765f,-0.333687f),new Vector3(-0.186674f,0.164765f,-0.336541f),new Vector3(-0.185435f,0.164765f,-0.339069f),new Vector3(-0.182817f,0.164765f,-0.340484f),new Vector3(-0.180118f,0.164765f,-0.341832f),
                new Vector3(-0.188139f,0.157265f,-0.329426f),new Vector3(-0.188139f,0.157265f,-0.329426f),new Vector3(-0.188139f,0.157265f,-0.329426f),new Vector3(-0.188139f,0.157265f,-0.329426f),new Vector3(-0.188139f,0.157265f,-0.329426f),
            },
            new[] { // rear section family inner-1
                new Vector3(-0.014270f,0.607500f,-0.275946f),new Vector3(0.023094f,0.607500f,-0.264037f),new Vector3(0.066647f,0.607500f,-0.271142f),new Vector3(0.101004f,0.607500f,-0.249998f),new Vector3(0.135194f,0.607500f,-0.228340f),
                new Vector3(-0.012175f,0.507500f,-0.279775f),new Vector3(0.025148f,0.507500f,-0.278202f),new Vector3(0.062397f,0.507500f,-0.276313f),new Vector3(0.095446f,0.507500f,-0.256542f),new Vector3(0.128246f,0.507500f,-0.235711f),
                new Vector3(-0.002277f,0.419999f,-0.305999f),new Vector3(0.033407f,0.419999f,-0.306636f),new Vector3(0.069135f,0.419999f,-0.307421f),new Vector3(0.098631f,0.419999f,-0.287205f),new Vector3(0.127990f,0.419999f,-0.266523f),
                new Vector3(0.014612f,0.344999f,-0.358340f),new Vector3(0.047462f,0.344999f,-0.357349f),new Vector3(0.079288f,0.344999f,-0.353791f),new Vector3(0.104797f,0.344999f,-0.334403f),new Vector3(0.130447f,0.344999f,-0.315369f),
                new Vector3(0.026004f,0.282500f,-0.397777f),new Vector3(0.053782f,0.282500f,-0.395896f),new Vector3(0.074894f,0.282500f,-0.378035f),new Vector3(0.102197f,0.282500f,-0.375018f),new Vector3(0.123602f,0.282500f,-0.357859f),
                new Vector3(0.031743f,0.238750f,-0.415286f),new Vector3(0.052511f,0.238750f,-0.408599f),new Vector3(0.076796f,0.238750f,-0.411960f),new Vector3(0.093670f,0.238750f,-0.394145f),new Vector3(0.114101f,0.238750f,-0.386495f),
                new Vector3(0.035454f,0.207500f,-0.419420f),new Vector3(0.053698f,0.207500f,-0.419248f),new Vector3(0.069131f,0.207500f,-0.409805f),new Vector3(0.087193f,0.207500f,-0.409034f),new Vector3(0.102764f,0.207500f,-0.400049f),
                new Vector3(0.043190f,0.156730f,-0.408994f),new Vector3(0.047199f,0.156730f,-0.409573f),new Vector3(0.051141f,0.156730f,-0.409598f),new Vector3(0.054899f,0.156730f,-0.408091f),new Vector3(0.058646f,0.156730f,-0.406489f),
                new Vector3(0.046190f,0.149230f,-0.404850f),new Vector3(0.046190f,0.149230f,-0.404850f),new Vector3(0.046190f,0.149230f,-0.404850f),new Vector3(0.046190f,0.149230f,-0.404850f),new Vector3(0.046190f,0.149230f,-0.404850f),
            },
            new[] { // rear section family inner-2
                new Vector3(-0.125604f,0.607499f,-0.231321f),new Vector3(-0.096301f,0.607499f,-0.251590f),new Vector3(-0.060433f,0.607499f,-0.252141f),new Vector3(-0.027848f,0.607499f,-0.262555f),new Vector3(0.004736f,0.607499f,-0.272968f),
                new Vector3(-0.119215f,0.507500f,-0.238344f),new Vector3(-0.091499f,0.507500f,-0.257983f),new Vector3(-0.063693f,0.507500f,-0.277310f),new Vector3(-0.030239f,0.507500f,-0.277269f),new Vector3(0.003162f,0.507500f,-0.277412f),
                new Vector3(-0.118851f,0.420000f,-0.267448f),new Vector3(-0.094035f,0.420000f,-0.287584f),new Vector3(-0.069187f,0.420000f,-0.307612f),new Vector3(-0.034535f,0.420000f,-0.295798f),new Vector3(-0.006453f,0.420000f,-0.305326f),
                new Vector3(-0.123352f,0.345001f,-0.317381f),new Vector3(-0.101079f,0.345001f,-0.335639f),new Vector3(-0.079100f,0.345001f,-0.354783f),new Vector3(-0.050743f,0.345001f,-0.354740f),new Vector3(-0.022295f,0.345001f,-0.354426f),
                new Vector3(-0.124595f,0.282500f,-0.356739f),new Vector3(-0.105579f,0.282500f,-0.373228f),new Vector3(-0.086567f,0.282500f,-0.389726f),new Vector3(-0.061284f,0.282500f,-0.391906f),new Vector3(-0.035533f,0.282500f,-0.393019f),
                new Vector3(-0.121990f,0.238750f,-0.381266f),new Vector3(-0.104682f,0.238750f,-0.395760f),new Vector3(-0.087049f,0.238750f,-0.409433f),new Vector3(-0.064775f,0.238750f,-0.411420f),new Vector3(-0.042287f,0.238750f,-0.412870f),
                new Vector3(-0.114527f,0.207500f,-0.393964f),new Vector3(-0.098726f,0.207500f,-0.404809f),new Vector3(-0.082934f,0.207500f,-0.415679f),new Vector3(-0.063889f,0.207500f,-0.417487f),new Vector3(-0.044290f,0.207500f,-0.417753f),
                new Vector3(-0.070683f,0.156998f,-0.402607f),new Vector3(-0.067185f,0.156998f,-0.404761f),new Vector3(-0.063662f,0.156998f,-0.406841f),new Vector3(-0.059521f,0.156998f,-0.407090f),new Vector3(-0.055173f,0.156998f,-0.406728f),
                new Vector3(-0.060199f,0.149498f,-0.401686f),new Vector3(-0.060199f,0.149498f,-0.401686f),new Vector3(-0.060199f,0.149498f,-0.401686f),new Vector3(-0.060199f,0.149498f,-0.401686f),new Vector3(-0.060199f,0.149498f,-0.401686f),
            },
            new[] { // rear section family inner-3
                new Vector3(-0.044679f,0.607500f,-0.262402f),new Vector3(-0.012161f,0.607500f,-0.260156f),new Vector3(0.021458f,0.607500f,-0.277920f),new Vector3(0.053437f,0.607500f,-0.265860f),new Vector3(0.085418f,0.607500f,-0.253837f),
                new Vector3(-0.039901f,0.507500f,-0.272168f),new Vector3(-0.008976f,0.507500f,-0.280477f),new Vector3(0.021943f,0.507500f,-0.288656f),new Vector3(0.051334f,0.507500f,-0.266862f),new Vector3(0.081746f,0.507500f,-0.265100f),
                new Vector3(-0.036982f,0.420000f,-0.299125f),new Vector3(-0.008292f,0.420000f,-0.307092f),new Vector3(0.020391f,0.420000f,-0.314893f),new Vector3(0.048190f,0.420000f,-0.302409f),new Vector3(0.076006f,0.420000f,-0.290303f),
                new Vector3(-0.036354f,0.345001f,-0.357189f),new Vector3(-0.008867f,0.345001f,-0.364231f),new Vector3(0.014496f,0.345001f,-0.349160f),new Vector3(0.039882f,0.345001f,-0.344941f),new Vector3(0.065314f,0.345001f,-0.340962f),
                new Vector3(-0.036834f,0.282500f,-0.395813f),new Vector3(-0.014357f,0.282500f,-0.402057f),new Vector3(0.007948f,0.282500f,-0.406573f),new Vector3(0.028827f,0.282500f,-0.396708f),new Vector3(0.049601f,0.282500f,-0.385799f),
                new Vector3(-0.035279f,0.238750f,-0.414198f),new Vector3(-0.017505f,0.238750f,-0.419870f),new Vector3(0.000269f,0.238750f,-0.425519f),new Vector3(0.018223f,0.238750f,-0.419707f),new Vector3(0.036189f,0.238750f,-0.413113f),
                new Vector3(-0.029207f,0.207500f,-0.420587f),new Vector3(-0.014761f,0.207500f,-0.425047f),new Vector3(-0.000315f,0.207500f,-0.429590f),new Vector3(0.014034f,0.207500f,-0.425207f),new Vector3(0.028377f,0.207500f,-0.420313f),
                new Vector3(0.011954f,0.157095f,-0.412953f),new Vector3(0.015845f,0.157095f,-0.413407f),new Vector3(0.019736f,0.157095f,-0.413860f),new Vector3(0.023541f,0.157095f,-0.412783f),new Vector3(0.027303f,0.157095f,-0.410916f),
                new Vector3(0.026658f,0.149595f,-0.408019f),new Vector3(0.026658f,0.149595f,-0.408019f),new Vector3(0.026658f,0.149595f,-0.408019f),new Vector3(0.026658f,0.149595f,-0.408019f),new Vector3(0.026658f,0.149595f,-0.408019f),
            },
            new[] { // rear section family inner-4
                new Vector3(0.111920f,0.607500f,-0.234333f),new Vector3(0.135763f,0.607500f,-0.229206f),new Vector3(0.159832f,0.607500f,-0.224376f),new Vector3(0.171907f,0.607500f,-0.203778f),new Vector3(0.183044f,0.607500f,-0.181948f),
                new Vector3(0.091222f,0.507500f,-0.250614f),new Vector3(0.113923f,0.507500f,-0.245082f),new Vector3(0.135704f,0.507500f,-0.238623f),new Vector3(0.144203f,0.507500f,-0.218824f),new Vector3(0.152279f,0.507500f,-0.198599f),
                new Vector3(0.098928f,0.419999f,-0.284091f),new Vector3(0.121100f,0.419999f,-0.278307f),new Vector3(0.139905f,0.419999f,-0.269761f),new Vector3(0.144809f,0.419999f,-0.249805f),new Vector3(0.149941f,0.419999f,-0.230036f),
                new Vector3(0.117374f,0.344999f,-0.329245f),new Vector3(0.136813f,0.344999f,-0.324013f),new Vector3(0.153435f,0.344999f,-0.316291f),new Vector3(0.158412f,0.344999f,-0.298278f),new Vector3(0.163696f,0.344999f,-0.280536f),
                new Vector3(0.127266f,0.282500f,-0.360598f),new Vector3(0.143089f,0.282500f,-0.356575f),new Vector3(0.157137f,0.282500f,-0.350716f),new Vector3(0.162905f,0.282500f,-0.336288f),new Vector3(0.167680f,0.282500f,-0.320834f),
                new Vector3(0.135050f,0.238750f,-0.372963f),new Vector3(0.147267f,0.238750f,-0.370484f),new Vector3(0.159670f,0.238750f,-0.368235f),new Vector3(0.164966f,0.238750f,-0.357199f),new Vector3(0.170102f,0.238750f,-0.345966f),
                new Vector3(0.144028f,0.207500f,-0.373587f),new Vector3(0.153738f,0.207500f,-0.371521f),new Vector3(0.163321f,0.207500f,-0.369277f),new Vector3(0.168249f,0.207500f,-0.360447f),new Vector3(0.172413f,0.207500f,-0.350535f),
                new Vector3(0.169693f,0.165333f,-0.352045f),new Vector3(0.173077f,0.165333f,-0.349978f),new Vector3(0.176450f,0.165333f,-0.347900f),new Vector3(0.178998f,0.165333f,-0.344856f),new Vector3(0.180529f,0.165333f,-0.340622f),
                new Vector3(0.183363f,0.157832f,-0.334464f),new Vector3(0.183363f,0.157832f,-0.334464f),new Vector3(0.183363f,0.157832f,-0.334464f),new Vector3(0.183363f,0.157832f,-0.334464f),new Vector3(0.183363f,0.157832f,-0.334464f),
            },
            new[] { // rear section family inner-5
                new Vector3(0.164293f,0.607499f,-0.175564f),new Vector3(0.188973f,0.607499f,-0.163137f),new Vector3(0.214344f,0.607499f,-0.151417f),new Vector3(0.227259f,0.607499f,-0.126982f),new Vector3(0.239860f,0.607499f,-0.102226f),
                new Vector3(0.149178f,0.507500f,-0.175216f),new Vector3(0.174932f,0.507500f,-0.166067f),new Vector3(0.201047f,0.507500f,-0.157361f),new Vector3(0.215063f,0.507500f,-0.133806f),new Vector3(0.229456f,0.507500f,-0.110713f),
                new Vector3(0.155883f,0.420000f,-0.218482f),new Vector3(0.181774f,0.420000f,-0.208220f),new Vector3(0.207724f,0.420000f,-0.198027f),new Vector3(0.215667f,0.420000f,-0.166323f),new Vector3(0.236077f,0.420000f,-0.149512f),
                new Vector3(0.162546f,0.344999f,-0.281112f),new Vector3(0.189162f,0.344999f,-0.273703f),new Vector3(0.211929f,0.344999f,-0.261583f),new Vector3(0.225276f,0.344999f,-0.237934f),new Vector3(0.237426f,0.344999f,-0.212819f),
                new Vector3(0.159334f,0.282500f,-0.318567f),new Vector3(0.182291f,0.282500f,-0.316690f),new Vector3(0.202740f,0.282500f,-0.310410f),new Vector3(0.217782f,0.282500f,-0.294638f),new Vector3(0.231951f,0.282500f,-0.277334f),
                new Vector3(0.166955f,0.238750f,-0.331454f),new Vector3(0.180436f,0.238750f,-0.331642f),new Vector3(0.193970f,0.238750f,-0.331994f),new Vector3(0.204927f,0.238750f,-0.324300f),new Vector3(0.215898f,0.238750f,-0.316649f),
                new Vector3(0.182628f,0.207500f,-0.330894f),new Vector3(0.189850f,0.207500f,-0.330781f),new Vector3(0.197124f,0.207500f,-0.330843f),new Vector3(0.203352f,0.207500f,-0.327399f),new Vector3(0.209320f,0.207500f,-0.323085f),
                new Vector3(0.196676f,0.191525f,-0.324818f),new Vector3(0.199546f,0.191525f,-0.324652f),new Vector3(0.202403f,0.191525f,-0.324438f),new Vector3(0.204945f,0.191525f,-0.323073f),new Vector3(0.207349f,0.191525f,-0.321201f),
                new Vector3(0.204745f,0.184025f,-0.320332f),new Vector3(0.204745f,0.184025f,-0.320332f),new Vector3(0.204745f,0.184025f,-0.320332f),new Vector3(0.204745f,0.184025f,-0.320332f),new Vector3(0.204745f,0.184025f,-0.320332f),
            },
        };
        public static int Count => Rows.Length;
        public static float Top(int panel) => Rows[panel][0].y;
        public static float Bottom(int panel) => Rows[panel][Rows[panel].Length-1].y;
        public static float[] Stations(int panel) {
            var rows=Rows[panel];var ys=new System.Collections.Generic.List<float>();
            for(int i=0;i<rows.Length/5-1;i++) {
                float a=rows[i*5].y,b=rows[(i+1)*5].y;
                int steps=Mathf.CeilToInt((a-b)/(a>.90f?.018f:a<.28f?.026f:.045f));
                for(int j=0;j<steps;j++)ys.Add(Mathf.Lerp(a,b,j/(float)steps));
            }
            ys.Add(rows[rows.Length-1].y);return ys.ToArray();
        }
        public static Vector3 Point(int panel,float y,int col) {
            var rows=Rows[panel];int count=rows.Length/5;
            int row=0;while(row<count-2&&rows[(row+1)*5].y>y)row++;
            float h=rows[row*5].y-rows[(row+1)*5].y;
            float u=Mathf.Clamp01((rows[row*5].y-y)/h);
            Vector3 a=rows[row*5+col],b=rows[(row+1)*5+col];
            int prev=Mathf.Max(0,row-1),next=Mathf.Min(count-1,row+2);
            Vector3 ma=(b-rows[prev*5+col])/(rows[prev*5].y-rows[(row+1)*5].y);
            Vector3 mb=(rows[next*5+col]-a)/(rows[row*5].y-rows[next*5].y);
            var p=(2*u*u*u-3*u*u+1)*a+(u*u*u-2*u*u+u)*h*ma+(-2*u*u*u+3*u*u)*b+(u*u*u-u*u)*h*mb;
            // Shape-preserving interpolation prevents overshoot through a
            // neighboring lock when a sparse guide makes a tight tip turn.
            p.y=y;for(int k=0;k<3;k++)p[k]=Mathf.Clamp(p[k],Mathf.Min(a[k],b[k]),Mathf.Max(a[k],b[k]));
            return p;
        }
    }
}
