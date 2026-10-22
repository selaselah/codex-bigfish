using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesktopFish.Editor {
    // Sparse section guides measured from the local review, registered to the
    // rebuilt coordinate system. Curves between them generate new topology.
    public static class FishModelGuides {
        static readonly Vector4[] HairEnvelope = {
            new Vector4(0.530000f,1.000000f,1.000000f,0.000000f),
            new Vector4(0.632500f,0.976623f,1.032812f,-0.006522f),
            new Vector4(0.670000f,0.924562f,1.035522f,-0.005851f),
            new Vector4(0.720000f,0.912071f,1.027201f,-0.004146f),
            new Vector4(0.770000f,0.953721f,1.013866f,0.002719f),
            new Vector4(0.820000f,0.970487f,1.007982f,0.007829f),
            new Vector4(0.870000f,0.991852f,1.005432f,0.009197f),
            new Vector4(0.907500f,1.046844f,1.024193f,0.005488f),
            new Vector4(0.932500f,1.188661f,1.066403f,-0.000005f),
            new Vector4(0.957500f,1.326461f,1.197358f,-0.011695f),
            new Vector4(0.980000f,1.330000f,1.170000f,-0.045000f),
            new Vector4(1.100000f,1.000000f,1.000000f,-0.045000f)
        };
        // Registered alpha-visible rear/side section extrema, compared with
        // the previous generated crown. The sparse correction is confined to
        // the rear hemisphere; measured frontal strips are left independent.
        static readonly Vector4[] RearCrownCorrection = {
            new Vector4(.8700f,1.000f,0,0),
            new Vector4(.9075f,.995f,.0036f,0),
            new Vector4(.9325f,1.039f,.0075f,0),
            new Vector4(.9575f,1.005f,.0086f,0),
            new Vector4(.9700f,.943f,.0125f,0),
            new Vector4(.97625f,.958f,.0008f,0),
            new Vector4(.9825f,.881f,-.0088f,0),
            new Vector4(.9900f,1f,0,0)
        };
        static readonly Vector4[] SkinWidth = {
            new Vector4(0.540000f,1.000000f,0.000000f,0.000000f),
            new Vector4(0.551250f,1.002454f,0.000000f,0.000000f),
            new Vector4(0.570000f,1.004872f,0.000000f,0.000000f),
            new Vector4(0.595000f,1.001500f,0.000000f,0.000000f),
            new Vector4(0.632500f,0.992715f,0.000000f,0.000000f),
            new Vector4(0.670000f,0.995824f,0.000000f,0.000000f),
            new Vector4(0.720000f,0.990292f,0.000000f,0.000000f),
            new Vector4(0.770000f,0.961596f,0.000000f,0.000000f),
            new Vector4(0.820000f,0.937400f,0.000000f,0.000000f),
            new Vector4(0.870000f,0.918147f,0.000000f,0.000000f),
            new Vector4(0.907500f,0.804229f,0.000000f,0.000000f),
            new Vector4(0.960000f,0.800000f,0.000000f,0.000000f)
        };

        public static Vector3 Headband(float t,bool outer) => FishHeadbandGuides.Point(t,outer?FishHeadbandGuides.Edge(t):0);
        // Sparse transverse landmarks capture each front strip's asymmetric
        // curvature, rather than inflating every section into an identical arch.
        static readonly Vector3[][] FrontPanelRows = {
            new[]{ // slot-10-component-0
                new Vector3(0.035981f,0.975132f,0.088588f),new Vector3(0.036081f,0.975132f,0.088588f),new Vector3(0.036181f,0.975132f,0.088588f),new Vector3(0.036281f,0.975132f,0.088588f),new Vector3(0.036381f,0.975132f,0.088588f),
                new Vector3(0.024467f,0.973750f,0.080145f),new Vector3(0.028784f,0.973750f,0.087866f),new Vector3(0.033101f,0.973750f,0.091733f),new Vector3(0.037418f,0.973750f,0.092467f),new Vector3(0.041735f,0.973750f,0.093201f),
                new Vector3(0.010419f,0.970000f,0.084335f),new Vector3(0.021369f,0.970000f,0.099241f),new Vector3(0.032319f,0.970000f,0.101223f),new Vector3(0.043270f,0.970000f,0.102589f),new Vector3(0.054220f,0.970000f,0.103286f),
                new Vector3(-0.002675f,0.966250f,0.086725f),new Vector3(0.013757f,0.966250f,0.099096f),new Vector3(0.030189f,0.966250f,0.107710f),new Vector3(0.046621f,0.966250f,0.108775f),new Vector3(0.063053f,0.966250f,0.106066f),
                new Vector3(-0.005728f,0.963750f,0.082245f),new Vector3(0.012939f,0.963750f,0.091439f),new Vector3(0.031607f,0.963750f,0.111721f),new Vector3(0.050274f,0.963750f,0.113043f),new Vector3(0.068941f,0.963750f,0.104548f),
                new Vector3(-0.011356f,0.957500f,0.073272f),new Vector3(0.012387f,0.957500f,0.079105f),new Vector3(0.036130f,0.957500f,0.121289f),new Vector3(0.059873f,0.957500f,0.121534f),new Vector3(0.083617f,0.957500f,0.098254f),
                new Vector3(-0.019160f,0.945000f,0.061199f),new Vector3(0.011866f,0.945000f,0.059962f),new Vector3(0.042893f,0.945000f,0.136941f),new Vector3(0.073919f,0.945000f,0.135468f),new Vector3(0.104945f,0.945000f,0.107147f),
                new Vector3(-0.029633f,0.920000f,0.046009f),new Vector3(0.011471f,0.920000f,0.035241f),new Vector3(0.052574f,0.920000f,0.158397f),new Vector3(0.093678f,0.920000f,0.155512f),new Vector3(0.134781f,0.920000f,0.125203f),
                new Vector3(0.096934f,0.882500f,0.177085f),new Vector3(0.113824f,0.882500f,0.173611f),new Vector3(0.130714f,0.882500f,0.170219f),new Vector3(0.147603f,0.882500f,0.162038f),new Vector3(0.164493f,0.882500f,0.140709f),
                new Vector3(0.119787f,0.832500f,0.189021f),new Vector3(0.136813f,0.832500f,0.184927f),new Vector3(0.153839f,0.832500f,0.180167f),new Vector3(0.170866f,0.832500f,0.170375f),new Vector3(0.187892f,0.832500f,0.149564f),
                new Vector3(0.132469f,0.782500f,0.191338f),new Vector3(0.148777f,0.782500f,0.186783f),new Vector3(0.165086f,0.782500f,0.182021f),new Vector3(0.181394f,0.782500f,0.172644f),new Vector3(0.197702f,0.782500f,0.150916f),
                new Vector3(0.140369f,0.732500f,0.187371f),new Vector3(0.155454f,0.732500f,0.182682f),new Vector3(0.170540f,0.732500f,0.178054f),new Vector3(0.185626f,0.732500f,0.169128f),new Vector3(0.200712f,0.732500f,0.148111f),
                new Vector3(0.143260f,0.695000f,0.181763f),new Vector3(0.156482f,0.695000f,0.177802f),new Vector3(0.169703f,0.695000f,0.173841f),new Vector3(0.182924f,0.695000f,0.166147f),new Vector3(0.196146f,0.695000f,0.146959f),
                new Vector3(0.138561f,0.657500f,0.175783f),new Vector3(0.148813f,0.657500f,0.173045f),new Vector3(0.159065f,0.657500f,0.170200f),new Vector3(0.169318f,0.657500f,0.164136f),new Vector3(0.179570f,0.657500f,0.150420f),
                new Vector3(0.122517f,0.620000f,0.168531f),new Vector3(0.127368f,0.620000f,0.167839f),new Vector3(0.132219f,0.620000f,0.166842f),new Vector3(0.137071f,0.620000f,0.164111f),new Vector3(0.141922f,0.620000f,0.158641f),
                new Vector3(0.105496f,0.604742f,0.166471f),new Vector3(0.105556f,0.604742f,0.166471f),new Vector3(0.105616f,0.604742f,0.166471f),new Vector3(0.105676f,0.604742f,0.166471f),new Vector3(0.105736f,0.604742f,0.166471f)
            },
            new[]{ // slot-10-component-3
                new Vector3(0.012104f,0.960741f,0.082081f),new Vector3(0.012204f,0.960741f,0.082081f),new Vector3(0.012304f,0.960741f,0.082081f),new Vector3(0.012404f,0.960741f,0.082081f),new Vector3(0.012504f,0.960741f,0.082081f),
                new Vector3(-0.008375f,0.957500f,0.088181f),new Vector3(0.006592f,0.957500f,0.095895f),new Vector3(0.021558f,0.957500f,0.095187f),new Vector3(0.036525f,0.957500f,0.087377f),new Vector3(0.051491f,0.957500f,0.078048f),
                new Vector3(-0.006965f,0.945000f,0.104501f),new Vector3(0.020254f,0.945000f,0.105021f),new Vector3(0.047474f,0.945000f,0.102316f),new Vector3(0.074693f,0.945000f,0.099600f),new Vector3(0.101913f,0.945000f,0.076700f),
                new Vector3(0.094324f,0.920000f,0.110939f),new Vector3(0.107527f,0.920000f,0.108189f),new Vector3(0.120729f,0.920000f,0.105392f),new Vector3(0.133932f,0.920000f,0.102161f),new Vector3(0.147135f,0.920000f,0.079549f),
                new Vector3(0.140401f,0.882500f,0.116412f),new Vector3(0.152341f,0.882500f,0.113079f),new Vector3(0.164282f,0.882500f,0.109038f),new Vector3(0.176222f,0.882500f,0.104997f),new Vector3(0.188163f,0.882500f,0.079194f),
                new Vector3(0.174605f,0.832500f,0.117341f),new Vector3(0.186090f,0.832500f,0.112499f),new Vector3(0.197574f,0.832500f,0.107906f),new Vector3(0.209058f,0.832500f,0.102485f),new Vector3(0.220543f,0.832500f,0.074438f),
                new Vector3(0.189303f,0.782500f,0.115382f),new Vector3(0.200554f,0.782500f,0.110580f),new Vector3(0.211805f,0.782500f,0.105298f),new Vector3(0.223056f,0.782500f,0.100370f),new Vector3(0.234307f,0.782500f,0.070157f),
                new Vector3(0.193097f,0.732500f,0.110681f),new Vector3(0.203923f,0.732500f,0.106229f),new Vector3(0.214748f,0.732500f,0.101219f),new Vector3(0.225573f,0.732500f,0.096601f),new Vector3(0.236398f,0.732500f,0.067531f),
                new Vector3(0.189472f,0.695000f,0.106525f),new Vector3(0.199780f,0.695000f,0.102494f),new Vector3(0.210087f,0.695000f,0.098060f),new Vector3(0.220394f,0.695000f,0.093879f),new Vector3(0.230702f,0.695000f,0.066794f),
                new Vector3(0.180346f,0.657500f,0.104205f),new Vector3(0.189329f,0.657500f,0.100226f),new Vector3(0.198312f,0.657500f,0.096011f),new Vector3(0.207296f,0.657500f,0.090140f),new Vector3(0.216279f,0.657500f,0.069014f),
                new Vector3(0.160493f,0.620000f,0.110575f),new Vector3(0.166831f,0.620000f,0.108230f),new Vector3(0.173169f,0.620000f,0.105533f),new Vector3(0.179508f,0.620000f,0.097021f),new Vector3(0.185846f,0.620000f,0.087409f),
                new Vector3(0.148687f,0.601250f,0.113796f),new Vector3(0.149147f,0.601250f,0.113796f),new Vector3(0.149607f,0.601250f,0.113558f),new Vector3(0.150066f,0.601250f,0.113172f),new Vector3(0.150526f,0.601250f,0.112531f),
                new Vector3(0.148660f,0.600767f,0.113425f),new Vector3(0.148720f,0.600767f,0.113425f),new Vector3(0.148780f,0.600767f,0.113425f),new Vector3(0.148840f,0.600767f,0.113425f),new Vector3(0.148900f,0.600767f,0.113425f)
            },
            new[]{ // slot-10-component-4
                new Vector3(0.011763f,0.979167f,0.050132f),new Vector3(0.011863f,0.979167f,0.050132f),new Vector3(0.011963f,0.979167f,0.050132f),new Vector3(0.012063f,0.979167f,0.050132f),new Vector3(0.012163f,0.979167f,0.050132f),
                new Vector3(-0.007977f,0.957500f,0.085488f),new Vector3(0.021341f,0.957500f,0.085515f),new Vector3(0.050658f,0.957500f,0.082056f),new Vector3(0.079976f,0.957500f,0.078281f),new Vector3(0.109294f,0.957500f,0.046916f),
                new Vector3(0.029615f,0.945000f,0.090895f),new Vector3(0.055086f,0.945000f,0.087890f),new Vector3(0.080556f,0.945000f,0.084235f),new Vector3(0.106027f,0.945000f,0.079059f),new Vector3(0.131498f,0.945000f,0.047633f),
                new Vector3(0.102018f,0.920000f,0.090845f),new Vector3(0.117485f,0.920000f,0.086711f),new Vector3(0.132952f,0.920000f,0.082332f),new Vector3(0.148419f,0.920000f,0.077615f),new Vector3(0.163886f,0.920000f,0.047386f),
                new Vector3(0.146450f,0.882500f,0.090694f),new Vector3(0.159050f,0.882500f,0.086111f),new Vector3(0.171650f,0.882500f,0.080980f),new Vector3(0.184251f,0.882500f,0.075849f),new Vector3(0.196851f,0.882500f,0.045079f),
                new Vector3(0.181580f,0.832500f,0.087192f),new Vector3(0.191943f,0.832500f,0.082045f),new Vector3(0.202306f,0.832500f,0.076909f),new Vector3(0.212669f,0.832500f,0.071773f),new Vector3(0.223032f,0.832500f,0.040692f),
                new Vector3(0.199238f,0.782500f,0.082986f),new Vector3(0.207894f,0.782500f,0.078183f),new Vector3(0.216549f,0.782500f,0.072957f),new Vector3(0.225205f,0.782500f,0.068385f),new Vector3(0.233860f,0.782500f,0.039730f),
                new Vector3(0.204850f,0.732500f,0.078165f),new Vector3(0.212074f,0.732500f,0.074125f),new Vector3(0.219298f,0.732500f,0.069507f),new Vector3(0.226522f,0.732500f,0.064890f),new Vector3(0.233747f,0.732500f,0.040429f),
                new Vector3(0.202445f,0.695000f,0.075022f),new Vector3(0.208427f,0.695000f,0.071763f),new Vector3(0.214408f,0.695000f,0.067531f),new Vector3(0.220390f,0.695000f,0.064005f),new Vector3(0.226371f,0.695000f,0.044497f),
                new Vector3(0.192399f,0.657500f,0.073986f),new Vector3(0.196715f,0.657500f,0.071336f),new Vector3(0.201030f,0.657500f,0.068375f),new Vector3(0.205346f,0.657500f,0.065420f),new Vector3(0.209661f,0.657500f,0.062435f),
                new Vector3(0.171467f,0.620000f,0.076507f),new Vector3(0.174139f,0.620000f,0.075143f),new Vector3(0.176811f,0.620000f,0.073072f),new Vector3(0.179483f,0.620000f,0.071196f),new Vector3(0.182154f,0.620000f,0.069320f),
                new Vector3(0.153576f,0.601250f,0.081911f),new Vector3(0.154229f,0.601250f,0.081866f),new Vector3(0.154882f,0.601250f,0.081421f),new Vector3(0.155535f,0.601250f,0.080931f),new Vector3(0.156188f,0.601250f,0.080161f),
                new Vector3(0.150788f,0.597867f,0.081751f),new Vector3(0.150848f,0.597867f,0.081751f),new Vector3(0.150908f,0.597867f,0.081751f),new Vector3(0.150968f,0.597867f,0.081751f),new Vector3(0.151028f,0.597867f,0.081751f)
            },
            new[]{ // slot-10-component-6
                new Vector3(0.011576f,0.953411f,0.086774f),new Vector3(0.011676f,0.953411f,0.086774f),new Vector3(0.011776f,0.953411f,0.086774f),new Vector3(0.011876f,0.953411f,0.086774f),new Vector3(0.011976f,0.953411f,0.086774f),
                new Vector3(-0.007486f,0.945000f,0.101248f),new Vector3(0.010541f,0.945000f,0.101815f),new Vector3(0.028568f,0.945000f,0.100893f),new Vector3(0.046596f,0.945000f,0.100790f),new Vector3(0.064623f,0.945000f,0.088340f),
                new Vector3(0.086116f,0.920000f,0.111886f),new Vector3(0.096369f,0.920000f,0.110606f),new Vector3(0.106622f,0.920000f,0.109326f),new Vector3(0.116875f,0.920000f,0.107786f),new Vector3(0.127128f,0.920000f,0.090965f),
                new Vector3(0.140389f,0.882500f,0.113625f),new Vector3(0.148923f,0.882500f,0.111812f),new Vector3(0.157458f,0.882500f,0.110032f),new Vector3(0.165993f,0.882500f,0.108066f),new Vector3(0.174527f,0.882500f,0.089086f),
                new Vector3(0.175964f,0.832500f,0.112581f),new Vector3(0.184135f,0.832500f,0.110451f),new Vector3(0.192305f,0.832500f,0.108384f),new Vector3(0.200476f,0.832500f,0.106317f),new Vector3(0.208647f,0.832500f,0.085027f),
                new Vector3(0.192513f,0.782500f,0.111598f),new Vector3(0.200750f,0.782500f,0.109599f),new Vector3(0.208987f,0.782500f,0.107368f),new Vector3(0.217224f,0.782500f,0.105257f),new Vector3(0.225461f,0.782500f,0.082932f),
                new Vector3(0.198730f,0.732500f,0.110370f),new Vector3(0.206769f,0.732500f,0.108532f),new Vector3(0.214808f,0.732500f,0.106694f),new Vector3(0.222847f,0.732500f,0.104695f),new Vector3(0.230886f,0.732500f,0.083040f),
                new Vector3(0.197177f,0.695000f,0.111583f),new Vector3(0.204845f,0.695000f,0.109912f),new Vector3(0.212514f,0.695000f,0.108240f),new Vector3(0.220182f,0.695000f,0.106548f),new Vector3(0.227851f,0.695000f,0.086286f),
                new Vector3(0.189480f,0.657500f,0.115007f),new Vector3(0.196256f,0.657500f,0.113519f),new Vector3(0.203032f,0.657500f,0.111967f),new Vector3(0.209807f,0.657500f,0.110475f),new Vector3(0.216583f,0.657500f,0.093108f),
                new Vector3(0.172524f,0.620000f,0.126304f),new Vector3(0.177521f,0.620000f,0.124979f),new Vector3(0.182519f,0.620000f,0.123308f),new Vector3(0.187516f,0.620000f,0.119875f),new Vector3(0.192513f,0.620000f,0.108819f),
                new Vector3(0.159515f,0.601965f,0.135570f),new Vector3(0.159575f,0.601965f,0.135570f),new Vector3(0.159635f,0.601965f,0.135570f),new Vector3(0.159695f,0.601965f,0.135570f),new Vector3(0.159755f,0.601965f,0.135570f)
            },
            new[]{ // slot-10-component-8
                new Vector3(0.017427f,0.945152f,0.128574f),new Vector3(0.017527f,0.945152f,0.128574f),new Vector3(0.017627f,0.945152f,0.128574f),new Vector3(0.017727f,0.945152f,0.128574f),new Vector3(0.017827f,0.945152f,0.128574f),
                new Vector3(0.015479f,0.945000f,0.128405f),new Vector3(0.016533f,0.945000f,0.128555f),new Vector3(0.017588f,0.945000f,0.128641f),new Vector3(0.018642f,0.945000f,0.128683f),new Vector3(0.019696f,0.945000f,0.128700f),
                new Vector3(0.041045f,0.920000f,0.118692f),new Vector3(0.060038f,0.920000f,0.140147f),new Vector3(0.079031f,0.920000f,0.140848f),new Vector3(0.098024f,0.920000f,0.140376f),new Vector3(0.117017f,0.920000f,0.139307f),
                new Vector3(0.101993f,0.882500f,0.121157f),new Vector3(0.115991f,0.882500f,0.153149f),new Vector3(0.129989f,0.882500f,0.151451f),new Vector3(0.143988f,0.882500f,0.149550f),new Vector3(0.157986f,0.882500f,0.147181f),
                new Vector3(0.135612f,0.832500f,0.124963f),new Vector3(0.148567f,0.832500f,0.158950f),new Vector3(0.161522f,0.832500f,0.155816f),new Vector3(0.174478f,0.832500f,0.152199f),new Vector3(0.187433f,0.832500f,0.148494f),
                new Vector3(0.149583f,0.782500f,0.121159f),new Vector3(0.162836f,0.782500f,0.114240f),new Vector3(0.176089f,0.782500f,0.154552f),new Vector3(0.189342f,0.782500f,0.150350f),new Vector3(0.202594f,0.782500f,0.145778f),
                new Vector3(0.156916f,0.732500f,0.119838f),new Vector3(0.169596f,0.732500f,0.156210f),new Vector3(0.182277f,0.732500f,0.151332f),new Vector3(0.194958f,0.732500f,0.146453f),new Vector3(0.207639f,0.732500f,0.141563f),
                new Vector3(0.158550f,0.695000f,0.120514f),new Vector3(0.170331f,0.695000f,0.153496f),new Vector3(0.182112f,0.695000f,0.149379f),new Vector3(0.193893f,0.695000f,0.144233f),new Vector3(0.205675f,0.695000f,0.139651f),
                new Vector3(0.155469f,0.657500f,0.122625f),new Vector3(0.165974f,0.657500f,0.151238f),new Vector3(0.176480f,0.657500f,0.147058f),new Vector3(0.186986f,0.657500f,0.142703f),new Vector3(0.197492f,0.657500f,0.138581f),
                new Vector3(0.141619f,0.620000f,0.126759f),new Vector3(0.150048f,0.620000f,0.144035f),new Vector3(0.158477f,0.620000f,0.141590f),new Vector3(0.166905f,0.620000f,0.139141f),new Vector3(0.175334f,0.620000f,0.136787f),
                new Vector3(0.126394f,0.601250f,0.138746f),new Vector3(0.132891f,0.601250f,0.147107f),new Vector3(0.139387f,0.601250f,0.144641f),new Vector3(0.145884f,0.601250f,0.142584f),new Vector3(0.152381f,0.601250f,0.140713f),
                new Vector3(0.106032f,0.588750f,0.157897f),new Vector3(0.111798f,0.588750f,0.155662f),new Vector3(0.117564f,0.588750f,0.153179f),new Vector3(0.123329f,0.588750f,0.150596f),new Vector3(0.129095f,0.588750f,0.140229f),
                new Vector3(0.116143f,0.585269f,0.146276f),new Vector3(0.116203f,0.585269f,0.146276f),new Vector3(0.116263f,0.585269f,0.146276f),new Vector3(0.116323f,0.585269f,0.146276f),new Vector3(0.116383f,0.585269f,0.146276f)
            },
            new[]{ // slot-10-component-10
                new Vector3(0.012678f,0.960170f,0.087242f),new Vector3(0.012778f,0.960170f,0.087242f),new Vector3(0.012878f,0.960170f,0.087242f),new Vector3(0.012978f,0.960170f,0.087242f),new Vector3(0.013078f,0.960170f,0.087242f),
                new Vector3(-0.007834f,0.957500f,0.099171f),new Vector3(0.004297f,0.957500f,0.099583f),new Vector3(0.016429f,0.957500f,0.099756f),new Vector3(0.028560f,0.957500f,0.098647f),new Vector3(0.040691f,0.957500f,0.097723f),
                new Vector3(0.041434f,0.945000f,0.094796f),new Vector3(0.054324f,0.945000f,0.103527f),new Vector3(0.067214f,0.945000f,0.102459f),new Vector3(0.080105f,0.945000f,0.100970f),new Vector3(0.092995f,0.945000f,0.099134f),
                new Vector3(0.105551f,0.920000f,0.109333f),new Vector3(0.113163f,0.920000f,0.107871f),new Vector3(0.120774f,0.920000f,0.106410f),new Vector3(0.128386f,0.920000f,0.104646f),new Vector3(0.135998f,0.920000f,0.102994f),
                new Vector3(0.148594f,0.882500f,0.113553f),new Vector3(0.155169f,0.882500f,0.111351f),new Vector3(0.161744f,0.882500f,0.109150f),new Vector3(0.168320f,0.882500f,0.106842f),new Vector3(0.174895f,0.882500f,0.103225f),
                new Vector3(0.180259f,0.832500f,0.112646f),new Vector3(0.186667f,0.832500f,0.109796f),new Vector3(0.193074f,0.832500f,0.106945f),new Vector3(0.199481f,0.832500f,0.104262f),new Vector3(0.205889f,0.832500f,0.101443f),
                new Vector3(0.201039f,0.782500f,0.109742f),new Vector3(0.207418f,0.782500f,0.106891f),new Vector3(0.213798f,0.782500f,0.103684f),new Vector3(0.220177f,0.782500f,0.100712f),new Vector3(0.226557f,0.782500f,0.097523f),
                new Vector3(0.215715f,0.732500f,0.095233f),new Vector3(0.221717f,0.732500f,0.102486f),new Vector3(0.227719f,0.732500f,0.099522f),new Vector3(0.233721f,0.732500f,0.096557f),new Vector3(0.239722f,0.732500f,0.093098f),
                new Vector3(0.222336f,0.695000f,0.101316f),new Vector3(0.227668f,0.695000f,0.099045f),new Vector3(0.233001f,0.695000f,0.096016f),new Vector3(0.238333f,0.695000f,0.093367f),new Vector3(0.243665f,0.695000f,0.090735f),
                new Vector3(0.222105f,0.657500f,0.098198f),new Vector3(0.226288f,0.657500f,0.099098f),new Vector3(0.230471f,0.657500f,0.096909f),new Vector3(0.234654f,0.657500f,0.094675f),new Vector3(0.238837f,0.657500f,0.092440f),
                new Vector3(0.212624f,0.620000f,0.108202f),new Vector3(0.214846f,0.620000f,0.107251f),new Vector3(0.217068f,0.620000f,0.106067f),new Vector3(0.219291f,0.620000f,0.104855f),new Vector3(0.221513f,0.620000f,0.099030f),
                new Vector3(0.206575f,0.606807f,0.112394f),new Vector3(0.206635f,0.606807f,0.112394f),new Vector3(0.206695f,0.606807f,0.112394f),new Vector3(0.206755f,0.606807f,0.112394f),new Vector3(0.206815f,0.606807f,0.112394f)
            },
            new[]{ // slot-10-component-13
                new Vector3(0.014446f,0.963478f,0.093757f),new Vector3(0.014546f,0.963478f,0.093757f),new Vector3(0.014646f,0.963478f,0.093757f),new Vector3(0.014746f,0.963478f,0.093757f),new Vector3(0.014846f,0.963478f,0.093757f),
                new Vector3(-0.006452f,0.957500f,0.096854f),new Vector3(0.009733f,0.957500f,0.091438f),new Vector3(0.025919f,0.957500f,0.096010f),new Vector3(0.042104f,0.957500f,0.094775f),new Vector3(0.058289f,0.957500f,0.093630f),
                new Vector3(0.072127f,0.945000f,0.092256f),new Vector3(0.078303f,0.945000f,0.098314f),new Vector3(0.084479f,0.945000f,0.097507f),new Vector3(0.090654f,0.945000f,0.096611f),new Vector3(0.096830f,0.945000f,0.095594f),
                new Vector3(0.121241f,0.920000f,0.098467f),new Vector3(0.126422f,0.920000f,0.102787f),new Vector3(0.131603f,0.920000f,0.101722f),new Vector3(0.136784f,0.920000f,0.100809f),new Vector3(0.141964f,0.920000f,0.099797f),
                new Vector3(0.170974f,0.882500f,0.108594f),new Vector3(0.175713f,0.882500f,0.107567f),new Vector3(0.180453f,0.882500f,0.106129f),new Vector3(0.185192f,0.882500f,0.104897f),new Vector3(0.189931f,0.882500f,0.103664f),
                new Vector3(0.214361f,0.832500f,0.109424f),new Vector3(0.218289f,0.832500f,0.108242f),new Vector3(0.222217f,0.832500f,0.106527f),new Vector3(0.226145f,0.832500f,0.104937f),new Vector3(0.230072f,0.832500f,0.103189f),
                new Vector3(0.243145f,0.782500f,0.104161f),new Vector3(0.246166f,0.782500f,0.103200f),new Vector3(0.249187f,0.782500f,0.101931f),new Vector3(0.252209f,0.782500f,0.100452f),new Vector3(0.255230f,0.782500f,0.099108f),
                new Vector3(0.259033f,0.732500f,0.096909f),new Vector3(0.260674f,0.732500f,0.096559f),new Vector3(0.262314f,0.732500f,0.095769f),new Vector3(0.263955f,0.732500f,0.094891f),new Vector3(0.265596f,0.732500f,0.094186f),
                new Vector3(0.257244f,0.695000f,0.101067f),new Vector3(0.258025f,0.695000f,0.100988f),new Vector3(0.258805f,0.695000f,0.100629f),new Vector3(0.259585f,0.695000f,0.100251f),new Vector3(0.260366f,0.695000f,0.099873f),
                new Vector3(0.241054f,0.665134f,0.117615f),new Vector3(0.241114f,0.665134f,0.117615f),new Vector3(0.241174f,0.665134f,0.117615f),new Vector3(0.241234f,0.665134f,0.117615f),new Vector3(0.241294f,0.665134f,0.117615f)
            },
            new[]{ // slot-11-component-8
                new Vector3(0.002020f,0.960744f,0.089730f),new Vector3(0.002120f,0.960744f,0.089730f),new Vector3(0.002220f,0.960744f,0.089730f),new Vector3(0.002320f,0.960744f,0.089730f),new Vector3(0.002420f,0.960744f,0.089730f),
                new Vector3(0.002012f,0.957500f,0.095813f),new Vector3(0.004011f,0.957500f,0.096381f),new Vector3(0.006010f,0.957500f,0.096898f),new Vector3(0.008010f,0.957500f,0.096251f),new Vector3(0.010009f,0.957500f,0.095051f),
                new Vector3(0.003905f,0.945000f,0.116115f),new Vector3(0.011326f,0.945000f,0.117654f),new Vector3(0.018747f,0.945000f,0.118690f),new Vector3(0.026168f,0.945000f,0.115833f),new Vector3(0.033590f,0.945000f,0.112424f),
                new Vector3(0.010485f,0.920000f,0.146274f),new Vector3(0.023242f,0.920000f,0.146632f),new Vector3(0.035999f,0.920000f,0.147164f),new Vector3(0.048756f,0.920000f,0.142050f),new Vector3(0.061513f,0.920000f,0.135373f),
                new Vector3(0.025353f,0.882500f,0.173230f),new Vector3(0.041043f,0.882500f,0.173305f),new Vector3(0.056733f,0.882500f,0.172890f),new Vector3(0.072422f,0.882500f,0.166197f),new Vector3(0.088112f,0.882500f,0.157882f),
                new Vector3(0.049169f,0.832500f,0.192809f),new Vector3(0.064952f,0.832500f,0.190595f),new Vector3(0.080735f,0.832500f,0.188382f),new Vector3(0.096518f,0.832500f,0.179984f),new Vector3(0.112300f,0.832500f,0.169624f),
                new Vector3(0.072861f,0.782500f,0.199746f),new Vector3(0.087494f,0.782500f,0.195598f),new Vector3(0.102128f,0.782500f,0.191384f),new Vector3(0.116761f,0.782500f,0.181829f),new Vector3(0.131394f,0.782500f,0.169094f),
                new Vector3(0.104021f,0.732500f,0.188428f),new Vector3(0.116911f,0.732500f,0.183212f),new Vector3(0.129801f,0.732500f,0.176793f),new Vector3(0.142691f,0.732500f,0.167069f),new Vector3(0.155581f,0.732500f,0.151594f),
                new Vector3(0.145147f,0.695000f,0.152553f),new Vector3(0.152218f,0.695000f,0.146783f),new Vector3(0.159290f,0.695000f,0.140076f),new Vector3(0.166361f,0.695000f,0.133161f),new Vector3(0.173433f,0.695000f,0.123066f),
                new Vector3(0.177889f,0.684302f,0.114151f),new Vector3(0.177949f,0.684302f,0.114151f),new Vector3(0.178009f,0.684302f,0.114151f),new Vector3(0.178069f,0.684302f,0.114151f),new Vector3(0.178129f,0.684302f,0.114151f)
            },
            new[]{ // slot-11-component-4
                new Vector3(0.001046f,0.960635f,0.092828f),new Vector3(0.001146f,0.960635f,0.092828f),new Vector3(0.001246f,0.960635f,0.092828f),new Vector3(0.001346f,0.960635f,0.092828f),new Vector3(0.001446f,0.960635f,0.092828f),
                new Vector3(-0.005182f,0.957500f,0.100394f),new Vector3(-0.003621f,0.957500f,0.101543f),new Vector3(-0.002061f,0.957500f,0.102073f),new Vector3(-0.000500f,0.957500f,0.100981f),new Vector3(0.001060f,0.957500f,0.099649f),
                new Vector3(-0.023894f,0.945000f,0.122206f),new Vector3(-0.018306f,0.945000f,0.125611f),new Vector3(-0.012718f,0.945000f,0.127819f),new Vector3(-0.007130f,0.945000f,0.125565f),new Vector3(-0.001543f,0.945000f,0.122416f),
                new Vector3(-0.040905f,0.920000f,0.148796f),new Vector3(-0.032592f,0.920000f,0.154254f),new Vector3(-0.024279f,0.920000f,0.158465f),new Vector3(-0.015966f,0.920000f,0.156269f),new Vector3(-0.007653f,0.920000f,0.154224f),
                new Vector3(-0.049845f,0.882500f,0.181328f),new Vector3(-0.039536f,0.882500f,0.186365f),new Vector3(-0.029227f,0.882500f,0.190761f),new Vector3(-0.018918f,0.882500f,0.187918f),new Vector3(-0.008609f,0.882500f,0.185458f),
                new Vector3(-0.056577f,0.832500f,0.204030f),new Vector3(-0.045742f,0.832500f,0.208795f),new Vector3(-0.034908f,0.832500f,0.212691f),new Vector3(-0.024073f,0.832500f,0.210240f),new Vector3(-0.013238f,0.832500f,0.208005f),
                new Vector3(-0.053640f,0.782500f,0.214726f),new Vector3(-0.043532f,0.782500f,0.218711f),new Vector3(-0.033423f,0.782500f,0.222058f),new Vector3(-0.023315f,0.782500f,0.219457f),new Vector3(-0.013206f,0.782500f,0.216670f),
                new Vector3(-0.042697f,0.732500f,0.217115f),new Vector3(-0.034420f,0.732500f,0.220303f),new Vector3(-0.026143f,0.732500f,0.223051f),new Vector3(-0.017867f,0.732500f,0.221361f),new Vector3(-0.009590f,0.732500f,0.219666f),
                new Vector3(-0.015160f,0.695000f,0.214717f),new Vector3(-0.009230f,0.695000f,0.215587f),new Vector3(-0.003301f,0.695000f,0.216597f),new Vector3(0.002628f,0.695000f,0.215900f),new Vector3(0.008558f,0.695000f,0.214329f),
                new Vector3(0.022922f,0.680728f,0.210138f),new Vector3(0.022982f,0.680728f,0.210138f),new Vector3(0.023042f,0.680728f,0.210138f),new Vector3(0.023102f,0.680728f,0.210138f),new Vector3(0.023162f,0.680728f,0.210138f)
            },
            new[]{ // slot-11-component-6
                new Vector3(-0.001214f,0.962164f,0.089695f),new Vector3(-0.001114f,0.962164f,0.089695f),new Vector3(-0.001014f,0.962164f,0.089695f),new Vector3(-0.000914f,0.962164f,0.089695f),new Vector3(-0.000814f,0.962164f,0.089695f),
                new Vector3(-0.007203f,0.957500f,0.099035f),new Vector3(-0.004244f,0.957500f,0.103571f),new Vector3(-0.001285f,0.957500f,0.105905f),new Vector3(0.001674f,0.957500f,0.103053f),new Vector3(0.004633f,0.957500f,0.100460f),
                new Vector3(-0.019161f,0.945000f,0.120480f),new Vector3(-0.010715f,0.945000f,0.128017f),new Vector3(-0.002269f,0.945000f,0.132951f),new Vector3(0.006178f,0.945000f,0.128839f),new Vector3(0.014624f,0.945000f,0.124592f),
                new Vector3(-0.030148f,0.920000f,0.150422f),new Vector3(-0.017017f,0.920000f,0.159068f),new Vector3(-0.003887f,0.920000f,0.166049f),new Vector3(0.009244f,0.920000f,0.161468f),new Vector3(0.022374f,0.920000f,0.156964f),
                new Vector3(-0.037332f,0.882500f,0.182398f),new Vector3(-0.021854f,0.882500f,0.191545f),new Vector3(-0.006376f,0.882500f,0.198639f),new Vector3(0.009102f,0.882500f,0.194685f),new Vector3(0.024580f,0.882500f,0.190771f),
                new Vector3(-0.037370f,0.832500f,0.206849f),new Vector3(-0.021598f,0.832500f,0.214092f),new Vector3(-0.005826f,0.832500f,0.220817f),new Vector3(0.009946f,0.832500f,0.217596f),new Vector3(0.025718f,0.832500f,0.214113f),
                new Vector3(-0.029778f,0.782500f,0.216796f),new Vector3(-0.016163f,0.782500f,0.222753f),new Vector3(-0.002549f,0.782500f,0.228046f),new Vector3(0.011066f,0.782500f,0.225081f),new Vector3(0.024681f,0.782500f,0.221968f),
                new Vector3(-0.021041f,0.732500f,0.218956f),new Vector3(-0.013039f,0.732500f,0.222689f),new Vector3(-0.005037f,0.732500f,0.225247f),new Vector3(0.002965f,0.732500f,0.223659f),new Vector3(0.010967f,0.732500f,0.222070f),
                new Vector3(-0.017068f,0.696342f,0.214770f),new Vector3(-0.017008f,0.696342f,0.214770f),new Vector3(-0.016948f,0.696342f,0.214770f),new Vector3(-0.016888f,0.696342f,0.214770f),new Vector3(-0.016828f,0.696342f,0.214770f)
            },
            new[]{ // slot-11-component-7
                new Vector3(-0.000312f,0.962389f,0.089237f),new Vector3(-0.000212f,0.962389f,0.089237f),new Vector3(-0.000112f,0.962389f,0.089237f),new Vector3(-0.000012f,0.962389f,0.089237f),new Vector3(0.000088f,0.962389f,0.089237f),
                new Vector3(-0.003163f,0.957500f,0.099275f),new Vector3(0.000220f,0.957500f,0.103004f),new Vector3(0.003602f,0.957500f,0.106740f),new Vector3(0.006984f,0.957500f,0.104232f),new Vector3(0.010366f,0.957500f,0.100626f),
                new Vector3(-0.007555f,0.945000f,0.121699f),new Vector3(0.001156f,0.945000f,0.126931f),new Vector3(0.009866f,0.945000f,0.132004f),new Vector3(0.018577f,0.945000f,0.127803f),new Vector3(0.027288f,0.945000f,0.121700f),
                new Vector3(-0.007160f,0.920000f,0.155368f),new Vector3(0.005837f,0.920000f,0.160613f),new Vector3(0.018835f,0.920000f,0.164362f),new Vector3(0.031832f,0.920000f,0.157826f),new Vector3(0.044830f,0.920000f,0.148974f),
                new Vector3(-0.004745f,0.882500f,0.190966f),new Vector3(0.010589f,0.882500f,0.193785f),new Vector3(0.025923f,0.882500f,0.196722f),new Vector3(0.041257f,0.882500f,0.189324f),new Vector3(0.056591f,0.882500f,0.179935f),
                new Vector3(-0.002999f,0.832500f,0.212712f),new Vector3(0.012742f,0.832500f,0.215997f),new Vector3(0.028484f,0.832500f,0.218984f),new Vector3(0.044225f,0.832500f,0.212571f),new Vector3(0.059966f,0.832500f,0.204527f),
                new Vector3(-0.001516f,0.782500f,0.219648f),new Vector3(0.012435f,0.782500f,0.223157f),new Vector3(0.026386f,0.782500f,0.226908f),new Vector3(0.040337f,0.782500f,0.221494f),new Vector3(0.054288f,0.782500f,0.215748f),
                new Vector3(-0.000842f,0.732500f,0.220831f),new Vector3(0.008206f,0.732500f,0.222952f),new Vector3(0.017253f,0.732500f,0.224444f),new Vector3(0.026301f,0.732500f,0.220878f),new Vector3(0.035348f,0.732500f,0.217471f),
                new Vector3(-0.009504f,0.695696f,0.215457f),new Vector3(-0.009444f,0.695696f,0.215457f),new Vector3(-0.009384f,0.695696f,0.215457f),new Vector3(-0.009324f,0.695696f,0.215457f),new Vector3(-0.009264f,0.695696f,0.215457f)
            },
            new[]{ // slot-11-component-9
                new Vector3(-0.000821f,0.965685f,0.091388f),new Vector3(-0.000721f,0.965685f,0.091388f),new Vector3(-0.000621f,0.965685f,0.091388f),new Vector3(-0.000521f,0.965685f,0.091388f),new Vector3(-0.000421f,0.965685f,0.091388f),
                new Vector3(-0.002365f,0.957500f,0.108298f),new Vector3(0.001461f,0.957500f,0.111652f),new Vector3(0.005287f,0.957500f,0.114944f),new Vector3(0.009113f,0.957500f,0.114103f),new Vector3(0.012939f,0.957500f,0.108750f),
                new Vector3(-0.000926f,0.945000f,0.130794f),new Vector3(0.006026f,0.945000f,0.135177f),new Vector3(0.012978f,0.945000f,0.139737f),new Vector3(0.019930f,0.945000f,0.137087f),new Vector3(0.026882f,0.945000f,0.129192f),
                new Vector3(0.003397f,0.920000f,0.161959f),new Vector3(0.012598f,0.920000f,0.166110f),new Vector3(0.021798f,0.920000f,0.170231f),new Vector3(0.030998f,0.920000f,0.165851f),new Vector3(0.040198f,0.920000f,0.156413f),
                new Vector3(0.007230f,0.882500f,0.195562f),new Vector3(0.017562f,0.882500f,0.198560f),new Vector3(0.027894f,0.882500f,0.201457f),new Vector3(0.038226f,0.882500f,0.195659f),new Vector3(0.048558f,0.882500f,0.186716f),
                new Vector3(0.010624f,0.832500f,0.221508f),new Vector3(0.021046f,0.832500f,0.224176f),new Vector3(0.031467f,0.832500f,0.226476f),new Vector3(0.041888f,0.832500f,0.221635f),new Vector3(0.052310f,0.832500f,0.213876f),
                new Vector3(0.011054f,0.782500f,0.229167f),new Vector3(0.021172f,0.782500f,0.232074f),new Vector3(0.031289f,0.782500f,0.235190f),new Vector3(0.041407f,0.782500f,0.230724f),new Vector3(0.051525f,0.782500f,0.224432f),
                new Vector3(0.007347f,0.732500f,0.227535f),new Vector3(0.016163f,0.732500f,0.230386f),new Vector3(0.024979f,0.732500f,0.233073f),new Vector3(0.033794f,0.732500f,0.229584f),new Vector3(0.042610f,0.732500f,0.224833f),
                new Vector3(-0.007607f,0.695000f,0.222742f),new Vector3(-0.001140f,0.695000f,0.224735f),new Vector3(0.005328f,0.695000f,0.226013f),new Vector3(0.011795f,0.695000f,0.224457f),new Vector3(0.018263f,0.695000f,0.222042f),
                new Vector3(-0.025612f,0.679075f,0.216027f),new Vector3(-0.025552f,0.679075f,0.216027f),new Vector3(-0.025492f,0.679075f,0.216027f),new Vector3(-0.025432f,0.679075f,0.216027f),new Vector3(-0.025372f,0.679075f,0.216027f)
            },
            new[]{ // slot-11-component-10
                new Vector3(0.000681f,0.965497f,0.091862f),new Vector3(0.000781f,0.965497f,0.091862f),new Vector3(0.000881f,0.965497f,0.091862f),new Vector3(0.000981f,0.965497f,0.091862f),new Vector3(0.001081f,0.965497f,0.091862f),
                new Vector3(-0.005324f,0.957500f,0.107764f),new Vector3(-0.001354f,0.957500f,0.110620f),new Vector3(0.002617f,0.957500f,0.113477f),new Vector3(0.006587f,0.957500f,0.111137f),new Vector3(0.010558f,0.957500f,0.106746f),
                new Vector3(-0.009171f,0.945000f,0.129476f),new Vector3(-0.002239f,0.945000f,0.133641f),new Vector3(0.004694f,0.945000f,0.138325f),new Vector3(0.011626f,0.945000f,0.134330f),new Vector3(0.018558f,0.945000f,0.126981f),
                new Vector3(-0.009949f,0.920000f,0.161993f),new Vector3(-0.000527f,0.920000f,0.165626f),new Vector3(0.008896f,0.920000f,0.169646f),new Vector3(0.018318f,0.920000f,0.165231f),new Vector3(0.027741f,0.920000f,0.158269f),
                new Vector3(-0.006148f,0.882500f,0.195503f),new Vector3(0.004193f,0.882500f,0.198933f),new Vector3(0.014533f,0.882500f,0.202216f),new Vector3(0.024873f,0.882500f,0.197142f),new Vector3(0.035213f,0.882500f,0.189985f),
                new Vector3(-0.002421f,0.832500f,0.221855f),new Vector3(0.007619f,0.832500f,0.224661f),new Vector3(0.017659f,0.832500f,0.227544f),new Vector3(0.027699f,0.832500f,0.222762f),new Vector3(0.037739f,0.832500f,0.216721f),
                new Vector3(-0.005079f,0.782500f,0.231252f),new Vector3(0.004560f,0.782500f,0.234388f),new Vector3(0.014198f,0.782500f,0.237242f),new Vector3(0.023837f,0.782500f,0.233457f),new Vector3(0.033476f,0.782500f,0.228718f),
                new Vector3(-0.014497f,0.732500f,0.228560f),new Vector3(-0.006596f,0.732500f,0.231711f),new Vector3(0.001306f,0.732500f,0.234229f),new Vector3(0.009207f,0.732500f,0.231672f),new Vector3(0.017109f,0.732500f,0.228797f),
                new Vector3(-0.029168f,0.695000f,0.220228f),new Vector3(-0.025443f,0.695000f,0.221613f),new Vector3(-0.021718f,0.695000f,0.221953f),new Vector3(-0.017994f,0.695000f,0.221934f),new Vector3(-0.014269f,0.695000f,0.221342f),
                new Vector3(-0.037873f,0.685938f,0.216376f),new Vector3(-0.037813f,0.685938f,0.216376f),new Vector3(-0.037753f,0.685938f,0.216376f),new Vector3(-0.037693f,0.685938f,0.216376f),new Vector3(-0.037633f,0.685938f,0.216376f)
            },
            new[]{ // slot-11-component-11
                new Vector3(0.001766f,0.963264f,0.089314f),new Vector3(0.001866f,0.963264f,0.089314f),new Vector3(0.001966f,0.963264f,0.089314f),new Vector3(0.002066f,0.963264f,0.089314f),new Vector3(0.002166f,0.963264f,0.089314f),
                new Vector3(-0.008064f,0.957500f,0.105212f),new Vector3(-0.005588f,0.957500f,0.108443f),new Vector3(-0.003113f,0.957500f,0.109057f),new Vector3(-0.000637f,0.957500f,0.106671f),new Vector3(0.001838f,0.957500f,0.103371f),
                new Vector3(-0.019839f,0.945000f,0.123638f),new Vector3(-0.015077f,0.945000f,0.129510f),new Vector3(-0.010315f,0.945000f,0.130929f),new Vector3(-0.005553f,0.945000f,0.128226f),new Vector3(-0.000791f,0.945000f,0.124825f),
                new Vector3(-0.036498f,0.920000f,0.151435f),new Vector3(-0.029516f,0.920000f,0.158806f),new Vector3(-0.022534f,0.920000f,0.161451f),new Vector3(-0.015552f,0.920000f,0.159071f),new Vector3(-0.008571f,0.920000f,0.156195f),
                new Vector3(-0.051991f,0.882500f,0.182633f),new Vector3(-0.043808f,0.882500f,0.190012f),new Vector3(-0.035625f,0.882500f,0.194177f),new Vector3(-0.027442f,0.882500f,0.191726f),new Vector3(-0.019259f,0.882500f,0.189243f),
                new Vector3(-0.062928f,0.832500f,0.208179f),new Vector3(-0.054705f,0.832500f,0.215634f),new Vector3(-0.046482f,0.832500f,0.219486f),new Vector3(-0.038258f,0.832500f,0.217792f),new Vector3(-0.030035f,0.832500f,0.216001f),
                new Vector3(-0.068639f,0.782500f,0.218659f),new Vector3(-0.061488f,0.782500f,0.224349f),new Vector3(-0.054338f,0.782500f,0.227605f),new Vector3(-0.047187f,0.782500f,0.226606f),new Vector3(-0.040037f,0.782500f,0.225480f),
                new Vector3(-0.060797f,0.732500f,0.218848f),new Vector3(-0.056229f,0.732500f,0.222766f),new Vector3(-0.051661f,0.732500f,0.224798f),new Vector3(-0.047093f,0.732500f,0.224484f),new Vector3(-0.042526f,0.732500f,0.223868f),
                new Vector3(-0.041042f,0.695000f,0.215433f),new Vector3(-0.040170f,0.695000f,0.215958f),new Vector3(-0.039298f,0.695000f,0.215958f),new Vector3(-0.038427f,0.695000f,0.215897f),new Vector3(-0.037555f,0.695000f,0.215812f),
                new Vector3(-0.037128f,0.689347f,0.213992f),new Vector3(-0.037068f,0.689347f,0.213992f),new Vector3(-0.037008f,0.689347f,0.213992f),new Vector3(-0.036948f,0.689347f,0.213992f),new Vector3(-0.036888f,0.689347f,0.213992f)
            },
            new[]{ // slot-11-component-12
                new Vector3(0.001680f,0.951139f,0.110682f),new Vector3(0.001780f,0.951139f,0.110682f),new Vector3(0.001880f,0.951139f,0.110682f),new Vector3(0.001980f,0.951139f,0.110682f),new Vector3(0.002080f,0.951139f,0.110682f),
                new Vector3(0.004096f,0.945000f,0.120732f),new Vector3(0.005559f,0.945000f,0.121348f),new Vector3(0.007022f,0.945000f,0.121973f),new Vector3(0.008486f,0.945000f,0.121743f),new Vector3(0.009949f,0.945000f,0.120299f),
                new Vector3(0.014871f,0.920000f,0.153610f),new Vector3(0.019915f,0.920000f,0.154717f),new Vector3(0.024959f,0.920000f,0.155814f),new Vector3(0.030003f,0.920000f,0.153842f),new Vector3(0.035047f,0.920000f,0.150329f),
                new Vector3(0.027692f,0.882500f,0.184079f),new Vector3(0.034396f,0.882500f,0.184954f),new Vector3(0.041101f,0.882500f,0.186070f),new Vector3(0.047806f,0.882500f,0.183631f),new Vector3(0.054511f,0.882500f,0.179481f),
                new Vector3(0.037639f,0.832500f,0.206766f),new Vector3(0.044639f,0.832500f,0.207681f),new Vector3(0.051640f,0.832500f,0.208343f),new Vector3(0.058641f,0.832500f,0.205567f),new Vector3(0.065642f,0.832500f,0.201209f),
                new Vector3(0.042557f,0.782500f,0.215484f),new Vector3(0.048787f,0.782500f,0.215827f),new Vector3(0.055016f,0.782500f,0.216130f),new Vector3(0.061246f,0.782500f,0.213460f),new Vector3(0.067476f,0.782500f,0.209903f),
                new Vector3(0.043043f,0.732500f,0.217001f),new Vector3(0.047355f,0.732500f,0.217310f),new Vector3(0.051667f,0.732500f,0.217536f),new Vector3(0.055979f,0.732500f,0.215698f),new Vector3(0.060291f,0.732500f,0.213305f),
                new Vector3(0.034326f,0.695000f,0.212349f),new Vector3(0.035346f,0.695000f,0.212519f),new Vector3(0.036366f,0.695000f,0.212672f),new Vector3(0.037385f,0.695000f,0.212672f),new Vector3(0.038405f,0.695000f,0.212054f),
                new Vector3(0.031915f,0.687772f,0.210924f),new Vector3(0.031975f,0.687772f,0.210924f),new Vector3(0.032035f,0.687772f,0.210924f),new Vector3(0.032095f,0.687772f,0.210924f),new Vector3(0.032155f,0.687772f,0.210924f)
            },
            new[]{ // measured underlayer, slot-11-component-0
                new Vector3(-0.001735f,0.955554f,0.108675f),new Vector3(-0.001635f,0.955554f,0.108675f),new Vector3(-0.001535f,0.955554f,0.108675f),new Vector3(-0.001435f,0.955554f,0.108675f),new Vector3(-0.001335f,0.955554f,0.108675f),
                new Vector3(0.023833f,0.945000f,0.122821f),new Vector3(0.033105f,0.945000f,0.123964f),new Vector3(0.042377f,0.945000f,0.123939f),new Vector3(0.051648f,0.945000f,0.120405f),new Vector3(0.060920f,0.945000f,0.113327f),
                new Vector3(0.063495f,0.920000f,0.137551f),new Vector3(0.074734f,0.920000f,0.136164f),new Vector3(0.085972f,0.920000f,0.134557f),new Vector3(0.097210f,0.920000f,0.129955f),new Vector3(0.108448f,0.920000f,0.116230f),
                new Vector3(0.105424f,0.882500f,0.148797f),new Vector3(0.116151f,0.882500f,0.145231f),new Vector3(0.126878f,0.882500f,0.141672f),new Vector3(0.137605f,0.882500f,0.134400f),new Vector3(0.148332f,0.882500f,0.117806f),
                new Vector3(0.134555f,0.832500f,0.151576f),new Vector3(0.144837f,0.832500f,0.147668f),new Vector3(0.155119f,0.832500f,0.143534f),new Vector3(0.165401f,0.832500f,0.136058f),new Vector3(0.175683f,0.832500f,0.117316f),
                new Vector3(0.153194f,0.782500f,0.151445f),new Vector3(0.161037f,0.782500f,0.147159f),new Vector3(0.168880f,0.782500f,0.142754f),new Vector3(0.176723f,0.782500f,0.138348f),new Vector3(0.184566f,0.782500f,0.117192f),
                new Vector3(0.161675f,0.732500f,0.148208f),new Vector3(0.167552f,0.732500f,0.145027f),new Vector3(0.173430f,0.732500f,0.141688f),new Vector3(0.179307f,0.732500f,0.138350f),new Vector3(0.185185f,0.732500f,0.122644f),
                new Vector3(0.160452f,0.695000f,0.142765f),new Vector3(0.163343f,0.695000f,0.141515f),new Vector3(0.166235f,0.695000f,0.139987f),new Vector3(0.169127f,0.695000f,0.138262f),new Vector3(0.172018f,0.695000f,0.131399f),
                new Vector3(0.159237f,0.674401f,0.137919f),new Vector3(0.159297f,0.674401f,0.137919f),new Vector3(0.159357f,0.674401f,0.137919f),new Vector3(0.159417f,0.674401f,0.137919f),new Vector3(0.159477f,0.674401f,0.137919f),
            },
            new[]{ // measured underlayer, slot-11-component-2
                new Vector3(-0.002290f,0.957440f,0.097541f),new Vector3(-0.002190f,0.957440f,0.097541f),new Vector3(-0.002090f,0.957440f,0.097541f),new Vector3(-0.001990f,0.957440f,0.097541f),new Vector3(-0.001890f,0.957440f,0.097541f),
                new Vector3(0.005646f,0.945000f,0.117864f),new Vector3(0.017083f,0.945000f,0.121931f),new Vector3(0.028520f,0.945000f,0.124763f),new Vector3(0.039957f,0.945000f,0.120268f),new Vector3(0.051394f,0.945000f,0.114925f),
                new Vector3(0.026055f,0.920000f,0.144520f),new Vector3(0.041571f,0.920000f,0.145627f),new Vector3(0.057088f,0.920000f,0.145490f),new Vector3(0.072605f,0.920000f,0.138122f),new Vector3(0.088121f,0.920000f,0.128138f),
                new Vector3(0.053848f,0.882500f,0.167287f),new Vector3(0.070716f,0.882500f,0.164925f),new Vector3(0.087585f,0.882500f,0.162441f),new Vector3(0.104454f,0.882500f,0.152652f),new Vector3(0.121323f,0.882500f,0.138084f),
                new Vector3(0.088966f,0.832500f,0.179462f),new Vector3(0.104559f,0.832500f,0.175119f),new Vector3(0.120152f,0.832500f,0.170150f),new Vector3(0.135746f,0.832500f,0.159236f),new Vector3(0.151339f,0.832500f,0.142364f),
                new Vector3(0.118883f,0.782500f,0.177234f),new Vector3(0.131130f,0.782500f,0.172020f),new Vector3(0.143377f,0.782500f,0.166653f),new Vector3(0.155624f,0.782500f,0.156845f),new Vector3(0.167871f,0.782500f,0.140505f),
                new Vector3(0.148966f,0.732500f,0.158529f),new Vector3(0.155523f,0.732500f,0.154709f),new Vector3(0.162081f,0.732500f,0.150465f),new Vector3(0.168639f,0.732500f,0.145582f),new Vector3(0.175197f,0.732500f,0.132782f),
                new Vector3(0.179679f,0.709260f,0.122491f),new Vector3(0.179739f,0.709260f,0.122491f),new Vector3(0.179799f,0.709260f,0.122491f),new Vector3(0.179859f,0.709260f,0.122491f),new Vector3(0.179919f,0.709260f,0.122491f),
            },
        };
        static Vector3 FrontPanelRaw(int panel,int sign,float t,float across) {
            return SamplePanel(FrontPanelRows[panel],sign,t,across);
        }
        static Vector3 SamplePanel(Vector3[] rows,int sign,float t,float across) {
            return SamplePanelAtHeight(rows,sign,Mathf.Lerp(rows[0].y,rows[rows.Length-1].y,t),across);
        }
        internal static Vector3 SamplePanelAtHeight(Vector3[] rows,int sign,float y,float across) {
            int count=rows.Length/5;
            int row=0;while(row<count-2&&rows[(row+1)*5].y>y)row++;
            float h=rows[row*5].y-rows[(row+1)*5].y;
            float u=Mathf.Clamp01((rows[row*5].y-y)/h);
            Vector3[] points=new Vector3[5];
            for(int col=0;col<5;col++) {
                Vector3 a=rows[row*5+col],b=rows[(row+1)*5+col];
                int prev=Mathf.Max(0,row-1),next=Mathf.Min(count-1,row+2);
                Vector3 ma=(b-rows[prev*5+col])/(rows[prev*5].y-rows[(row+1)*5].y);
                Vector3 mb=(rows[next*5+col]-a)/(rows[row*5].y-rows[next*5].y);
                var p=(2*u*u*u-3*u*u+1)*a+(u*u*u-2*u*u+u)*h*ma+(-2*u*u*u+3*u*u)*b+(u*u*u-u*u)*h*mb;
                p.y=y;for(int k=0;k<3;k++)p[k]=Mathf.Clamp(p[k],Mathf.Min(a[k],b[k]),Mathf.Max(a[k],b[k]));
                points[col]=p;
            }
            float f=(sign>0?across:1-across)*4;int c=Mathf.Min(3,(int)f);float v=f-c;
            Vector3 value=Vector3.Lerp(points[c],points[c+1],v);value.x*=sign;return value;
        }

        public static Vector3 FrontPanel(int panel,int sign,float t,float across) => FrontPanelRaw(panel,sign,t,across);



        // The outer cheek lock turns back toward its inner edge. Its frontmost
        // depth profile alone omits the visible side return in oblique views.
        static readonly Vector4[][] CheekReturnRows={
            new[]{
                new Vector4(0.588750f,0.111798f,0.149395f,0),
                new Vector4(0.601250f,0.132891f,0.131021f,0),
                new Vector4(0.620000f,0.150048f,0.120225f,0),
                new Vector4(0.657500f,0.165974f,0.115720f,0),
                new Vector4(0.695000f,0.170331f,0.113394f,0),
                new Vector4(0.732500f,0.169596f,0.112269f,0),
                new Vector4(0.782500f,0.162836f,0.114028f,0),
                new Vector4(0.832500f,0.148567f,0.116106f,0),
                new Vector4(0.882500f,0.115991f,0.114808f,0),
                new Vector4(0.920000f,0.060038f,0.113058f,0),
                new Vector4(0.945000f,0.016533f,0.126425f,0)
            },
            new[]{
                new Vector4(0.588750f,0.117564f,0.144358f,0),
                new Vector4(0.601250f,0.139387f,0.127266f,0),
                new Vector4(0.620000f,0.158477f,0.116364f,0),
                new Vector4(0.657500f,0.176480f,0.110965f,0),
                new Vector4(0.695000f,0.182112f,0.108510f,0),
                new Vector4(0.732500f,0.182277f,0.107241f,0),
                new Vector4(0.782500f,0.176089f,0.109577f,0),
                new Vector4(0.832500f,0.161522f,0.112647f,0),
                new Vector4(0.882500f,0.129989f,0.112078f,0),
                new Vector4(0.920000f,0.079031f,0.110334f,0),
                new Vector4(0.945000f,0.017588f,0.125794f,0)
            },
            new[]{
                new Vector4(0.588750f,0.123329f,0.140359f,0),
                new Vector4(0.601250f,0.145884f,0.123443f,0),
                new Vector4(0.620000f,0.166905f,0.112130f,0),
                new Vector4(0.657500f,0.186986f,0.106805f,0),
                new Vector4(0.695000f,0.193893f,0.103626f,0),
                new Vector4(0.732500f,0.194958f,0.102715f,0),
                new Vector4(0.782500f,0.189342f,0.104918f,0),
                new Vector4(0.832500f,0.174478f,0.108827f,0),
                new Vector4(0.882500f,0.143988f,0.109704f,0),
                new Vector4(0.920000f,0.098024f,0.107310f,0),
                new Vector4(0.945000f,0.018642f,0.126278f,0)
            }
        };
        public static Vector3 CheekReturn(float t,int quarter) {
            Vector3 front=FrontPanel(4,1,t,quarter/4f);
            var rows=CheekReturnRows[quarter-1];Vector4 g=Sample(rows,front.y);
            float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(FrontPanelRows[4][0].y,rows[rows.Length-1].x,front.y))*
                Mathf.SmoothStep(0,1,Mathf.InverseLerp(FrontPanelRows[4][FrontPanelRows[4].Length-1].y,rows[0].x,front.y));
            return Vector3.Lerp(front,new Vector3(g.y,front.y,g.z),blend);
        }

        static readonly Vector3[][] RibbonTailRows={
            new[]{new Vector3(0.005248f,0.475000f,0.086062f),new Vector3(0.006481f,0.475000f,0.086237f),new Vector3(0.007714f,0.475000f,0.086446f),new Vector3(0.008947f,0.475000f,0.086655f),new Vector3(0.010179f,0.475000f,0.086865f)},
            new[]{new Vector3(0.005386f,0.480000f,0.081941f),new Vector3(0.012632f,0.480000f,0.083115f),new Vector3(0.019878f,0.480000f,0.083734f),new Vector3(0.027123f,0.480000f,0.081425f),new Vector3(0.034369f,0.480000f,0.078682f)},
            new[]{new Vector3(0.005011f,0.485000f,0.077629f),new Vector3(0.013852f,0.485000f,0.077775f),new Vector3(0.022694f,0.485000f,0.077984f),new Vector3(0.031536f,0.485000f,0.074387f),new Vector3(0.040378f,0.485000f,0.071011f)},
            new[]{new Vector3(0.005076f,0.490000f,0.072882f),new Vector3(0.012998f,0.490000f,0.072819f),new Vector3(0.020920f,0.490000f,0.072617f),new Vector3(0.028843f,0.490000f,0.070056f),new Vector3(0.036765f,0.490000f,0.066752f)},
            new[]{new Vector3(0.005500f,0.498000f,0.064000f),new Vector3(0.011250f,0.498000f,0.063250f),new Vector3(0.017000f,0.498000f,0.062500f),new Vector3(0.022750f,0.498000f,0.061750f),new Vector3(0.028500f,0.498000f,0.061000f)},
            new[]{new Vector3(0.006000f,0.509000f,0.055000f),new Vector3(0.008500f,0.509000f,0.054500f),new Vector3(0.011000f,0.509000f,0.054000f),new Vector3(0.013500f,0.509000f,0.053500f),new Vector3(0.016000f,0.509000f,0.053000f)},
        };
        static readonly Vector3[][] CollarRows={
            new[]{new Vector3(0.003750f,0.513243f,0.046839f),new Vector3(0.003750f,0.515029f,0.045360f),new Vector3(0.003750f,0.523543f,0.031884f),new Vector3(0.003750f,0.530867f,0.019467f),new Vector3(0.003750f,0.537000f,0.009000f)},
            new[]{new Vector3(0.010000f,0.514431f,0.045258f),new Vector3(0.010000f,0.516827f,0.042274f),new Vector3(0.010000f,0.525149f,0.026435f),new Vector3(0.010000f,0.531873f,0.014216f),new Vector3(0.010000f,0.537000f,0.007000f)},
            new[]{new Vector3(0.018750f,0.509017f,0.050326f),new Vector3(0.018750f,0.513386f,0.045871f),new Vector3(0.018750f,0.523837f,0.026703f),new Vector3(0.018750f,0.531375f,0.010886f),new Vector3(0.018750f,0.536000f,0.003000f)},
            new[]{new Vector3(0.031250f,0.504274f,0.052344f),new Vector3(0.031250f,0.512194f,0.041927f),new Vector3(0.031250f,0.525409f,0.021577f),new Vector3(0.031250f,0.533345f,0.005350f),new Vector3(0.031250f,0.536000f,0.002500f)},
            new[]{new Vector3(0.043750f,0.502713f,0.049045f),new Vector3(0.043750f,0.510536f,0.039804f),new Vector3(0.043750f,0.518359f,0.029409f),new Vector3(0.043750f,0.526183f,0.011514f),new Vector3(0.043750f,0.534006f,-0.010585f)},
            new[]{new Vector3(0.056250f,0.505672f,0.042618f),new Vector3(0.056250f,0.512162f,0.033712f),new Vector3(0.056250f,0.518652f,0.022822f),new Vector3(0.056250f,0.525142f,0.010132f),new Vector3(0.056250f,0.531632f,-0.010420f)},
            new[]{new Vector3(0.063750f,0.508599f,0.036064f),new Vector3(0.063750f,0.514108f,0.026890f),new Vector3(0.063750f,0.519617f,0.017717f),new Vector3(0.063750f,0.525126f,0.006544f),new Vector3(0.063750f,0.530635f,-0.010610f)},
            new[]{new Vector3(0.065625f,0.509137f,0.034539f),new Vector3(0.065625f,0.514439f,0.025806f),new Vector3(0.065625f,0.519740f,0.016617f),new Vector3(0.065625f,0.525042f,0.006581f),new Vector3(0.065625f,0.530344f,-0.010508f)},
            new[]{new Vector3(0.081250f,0.518861f,0.012859f),new Vector3(0.081250f,0.521349f,0.008083f),new Vector3(0.081250f,0.523836f,0.002511f),new Vector3(0.081250f,0.526324f,-0.003230f),new Vector3(0.081250f,0.528812f,-0.010925f)},
            new[]{new Vector3(0.083000f,0.526000f,-0.018000f),new Vector3(0.083000f,0.526000f,-0.018000f),new Vector3(0.083000f,0.526000f,-0.018000f),new Vector3(0.083000f,0.526000f,-0.018000f),new Vector3(0.083000f,0.526000f,-0.018000f)},
        };

        static Vector3 SectionPatch(Vector3[][] rows,float t,float v) {
            float x=Mathf.Lerp(rows[0][0].x,rows[rows.Length-1][0].x,t);int i=0;
            while(i<rows.Length-2&&x>rows[i+1][0].x)i++;
            float u=Mathf.InverseLerp(rows[i][0].x,rows[i+1][0].x,x);
            float f=Mathf.Clamp01(v)*4;int j=Mathf.Min(3,(int)f);
            Vector3 a=Vector3.Lerp(rows[i][j],rows[i][j+1],f-j),b=Vector3.Lerp(rows[i+1][j],rows[i+1][j+1],f-j);
            return Vector3.Lerp(a,b,u);
        }
        public static Vector3 CollarFold(float t,float v) => SectionPatch(CollarRows,t,v);
        public static Vector3 RibbonTail(float t,float v) {
            float y=Mathf.Lerp(RibbonTailRows[0][0].y,RibbonTailRows[RibbonTailRows.Length-1][0].y,t);int i=0;
            while(i<RibbonTailRows.Length-2&&y>RibbonTailRows[i+1][0].y)i++;
            float u=Mathf.InverseLerp(RibbonTailRows[i][0].y,RibbonTailRows[i+1][0].y,y),f=v*4;int j=Mathf.Min(3,(int)f);
            return Vector3.Lerp(Vector3.Lerp(RibbonTailRows[i][j],RibbonTailRows[i][j+1],f-j),Vector3.Lerp(RibbonTailRows[i+1][j],RibbonTailRows[i+1][j+1],f-j),u);
        }

        // Visible cloth-color boundary and depth sections, sampled locally.
        static readonly Vector4[] BibRows={
            new Vector4(0.416000f,0.056000f,0.117000f,0.108000f),
            new Vector4(0.420000f,0.061212f,0.117633f,0.107114f),
            new Vector4(0.432500f,0.066164f,0.119163f,0.102272f),
            new Vector4(0.445000f,0.069259f,0.113152f,0.095558f),
            new Vector4(0.457500f,0.069695f,0.099108f,0.084820f),
            new Vector4(0.470000f,0.070750f,0.083402f,0.074138f),
            new Vector4(0.482500f,0.071299f,0.064929f,0.058138f),
            new Vector4(0.495000f,0.072530f,0.049739f,0.043448f),
            new Vector4(0.507500f,0.068711f,0.031602f,0.022747f),
            new Vector4(0.514000f,0.061000f,0.025000f,0.017000f)
        };
        public static Vector3 Bib(float y,float u) {
            Vector4 g=Sample(BibRows,y);float a=Mathf.Abs(u);
            float fold=.0012f*Mathf.Sin(a*Mathf.PI*3)*Mathf.Sin(Mathf.InverseLerp(.416f,.514f,y)*Mathf.PI);
            return new Vector3(u*g.y,y,Mathf.Lerp(g.z,g.w,a*a)+fold);
        }
        static readonly Vector3[][] BibLaceRows={
            new[]{new Vector3(0.091000f,0.414000f,0.084000f),new Vector3(0.092580f,0.420000f,0.086045f),new Vector3(0.093235f,0.432500f,0.086937f),new Vector3(0.096056f,0.445000f,0.082075f),new Vector3(0.096228f,0.457500f,0.073862f),new Vector3(0.094514f,0.470000f,0.067018f),new Vector3(0.093103f,0.482500f,0.060389f),new Vector3(0.092733f,0.495000f,0.052299f),new Vector3(0.043920f,0.507500f,0.042944f),new Vector3(0.045179f,0.520000f,0.025591f)},
            new[]{new Vector3(0.094000f,0.414000f,0.084000f),new Vector3(0.096296f,0.420000f,0.082527f),new Vector3(0.099784f,0.432500f,0.079273f),new Vector3(0.101243f,0.445000f,0.073971f),new Vector3(0.101445f,0.457500f,0.066543f),new Vector3(0.099427f,0.470000f,0.060396f),new Vector3(0.098692f,0.482500f,0.055182f),new Vector3(0.099719f,0.495000f,0.049208f),new Vector3(0.087224f,0.507500f,0.035323f),new Vector3(0.086077f,0.520000f,0.010338f)},
            new[]{new Vector3(0.097000f,0.414000f,0.084000f),new Vector3(0.100455f,0.420000f,0.067488f),new Vector3(0.106206f,0.432500f,0.054663f),new Vector3(0.106530f,0.445000f,0.059065f),new Vector3(0.106612f,0.457500f,0.031948f),new Vector3(0.104563f,0.470000f,0.031107f),new Vector3(0.104643f,0.482500f,0.041824f),new Vector3(0.106829f,0.495000f,0.047547f),new Vector3(0.113749f,0.507500f,0.041010f),new Vector3(0.126952f,0.520000f,0.009248f)},
        };
        public static Vector3 BibLace(float y,float across) {
            var points=new Vector3[3];
            for(int col=0;col<3;col++) {
                // Above this height the measured material also contains the
                // collar. Continue the vertical strip into that overlap rather
                // than lofting across both disjoint islands as a broad flap.
                var rows=BibLaceRows[col];float sampleY=Mathf.Min(y,.495f);int i=0;while(i<rows.Length-2&&sampleY>rows[i+1].y)i++;
                float t=Mathf.InverseLerp(rows[i].y,rows[i+1].y,sampleY);points[col]=Vector3.Lerp(rows[i],rows[i+1],t);
                float top=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.495f,.514f,y));
                points[col].x-=.010f*top*(1-col/2f);points[col].z-=.019f*top;points[col].y=y;
            }
            float u=across*2;int index=Mathf.Min(1,(int)u);return Vector3.Lerp(points[index],points[index+1],u-index);
        }

        // Sparse visible artwork/edge probes, projected onto the local reference
        // surface with alpha rejection and registered to the rebuilt review space.
        public static readonly Vector3[] ApronDetailAnchors={
            new Vector3(0.054833f,0.329581f,0.207203f), // whale-head
            new Vector3(0.018667f,0.358761f,0.191567f), // whale-crown
            new Vector3(-0.007778f,0.341639f,0.211625f), // whale-shoulder
            new Vector3(-0.005444f,0.315137f,0.236345f), // whale-flank
            new Vector3(0.023333f,0.305506f,0.242747f), // whale-bottom
            new Vector3(0.046667f,0.318413f,0.223655f), // belly-start
            new Vector3(0.028000f,0.321875f,0.227914f), // belly-upper
            new Vector3(0.007778f,0.308917f,0.241499f), // belly-end
            new Vector3(0.031500f,0.306871f,0.240298f), // belly-bottom
            new Vector3(-0.042778f,0.349542f,0.193369f), // tail-tip
            new Vector3(-0.022556f,0.353638f,0.195803f), // tail-root
            new Vector3(-0.029167f,0.333879f,0.217037f), // tail-turn
            new Vector3(-0.040833f,0.300526f,0.243896f), // boat-left
            new Vector3(-0.062611f,0.325198f,0.206052f), // boat-top
            new Vector3(-0.066500f,0.309326f,0.217343f), // boat-right
            new Vector3(-0.052889f,0.304630f,0.231796f), // boat-notch
            new Vector3(-0.044333f,0.317809f,0.225852f), // spray-0
            new Vector3(-0.035389f,0.315250f,0.232876f), // spray-1
            new Vector3(0.011278f,0.370928f,0.177848f), // spout-0
            new Vector3(0.000778f,0.367107f,0.183732f), // spout-1
            new Vector3(0.073889f,0.357458f,0.146021f), // edge-left-upper
            new Vector3(0.070389f,0.290712f,0.228509f), // edge-left-lower
            new Vector3(0.000000f,0.268667f,0.274807f), // edge-bottom
            new Vector3(-0.073889f,0.293027f,0.223311f), // edge-right-lower
            new Vector3(-0.073889f,0.354574f,0.153866f), // edge-right-upper
            new Vector3(0.046667f,0.347954f,0.193029f), // whale-upper-left
            new Vector3(-0.000389f,0.352067f,0.200549f), // whale-upper-right
            new Vector3(0.052889f,0.316995f,0.220553f), // whale-lower-left
            new Vector3(0.041611f,0.308860f,0.235910f), // whale-lower-arc
            new Vector3(0.033444f,0.331303f,0.218966f), // eye
            new Vector3(0.050167f,0.317866f,0.221683f), // belly-left
            new Vector3(0.034222f,0.307570f,0.239283f), // belly-lower
        };
        static readonly Vector4[] ApronUpperDepth={
            new Vector4(.370000f,.180550f,0,0),
            new Vector4(.395000f,.138948f,0,0),
            new Vector4(.400000f,.130263f,0,0)
        };
        public static float ApronDepth(float y) => Sample(ApronUpperDepth,y).y;
        // Refit a smooth cloth sheet to the sparse visible 3D probes. The same
        // curve is used for lace and the seam, preserving their relative lift.
        static readonly double[] ApronSheetFit=FitApronSheet();
        static double[] ApronTerms(double x,double y) {
            x/=.1;y=(y-.32)/.1;return new[]{1d,x,y,x*x,x*y,y*y};
        }
        static double[] FitApronSheet() {
            var points=new System.Collections.Generic.List<Vector3>(ApronDetailAnchors);
            foreach(var p in ApronUpperDepth)points.Add(new Vector3(0,p.x,p.y));
            var m=new double[6,7];
            foreach(var p in points) {
                var row=ApronTerms(p.x,p.y);
                for(int i=0;i<6;i++) {
                    for(int j=0;j<6;j++)m[i,j]+=row[i]*row[j];m[i,6]+=row[i]*p.z;
                }
            }
            for(int i=0;i<6;i++) {
                int pivot=i;for(int k=i+1;k<6;k++)if(System.Math.Abs(m[k,i])>System.Math.Abs(m[pivot,i]))pivot=k;
                for(int j=0;j<7;j++){double temp=m[i,j];m[i,j]=m[pivot,j];m[pivot,j]=temp;}
                double scale=m[i,i];if(System.Math.Abs(scale)<1e-12)throw new System.InvalidOperationException("Singular apron sheet fit");
                for(int j=0;j<7;j++)m[i,j]/=scale;
                for(int k=0;k<6;k++)if(k!=i){double factor=m[k,i];for(int j=0;j<7;j++)m[k,j]-=factor*m[i,j];}
            }
            return new[]{m[0,6],m[1,6],m[2,6],m[3,6],m[4,6],m[5,6]};
        }
        public static float ApronSheetDepth(float x,float y) {
            var row=ApronTerms(x,y);double z=0;for(int i=0;i<6;i++)z+=row[i]*ApronSheetFit[i];return (float)z;
        }


        public static readonly Vector3[] BeltStuds={
            new Vector3(-0.033360f,0.398500f,0.112992f),
            new Vector3(-0.031634f,0.406231f,0.115756f),
            new Vector3(0.034308f,0.398507f,0.112603f),
            new Vector3(0.034262f,0.406692f,0.115217f)
        };

        // Visible shoe edges include alpha-cut instep holes. These sparse
        // measured rings feed a new loft, independent of reference topology.
        // Two sparse alpha-visible garment boundaries, measured on radial
        // cuts; the independent loft supplies the gathered fabric between them.
        static readonly Vector3[] NavyHem = {
            new Vector3(0.000000f,0.184421f,0.260688f),
            new Vector3(0.068149f,0.185757f,0.254335f),
            new Vector3(0.128508f,0.184327f,0.222582f),
            new Vector3(0.162032f,0.187844f,0.162032f),
            new Vector3(0.192172f,0.190928f,0.110951f),
            new Vector3(0.208748f,0.197523f,0.055934f),
            new Vector3(0.216107f,0.199699f,-0.000000f),
            new Vector3(0.212157f,0.205372f,-0.056847f),
            new Vector3(0.207256f,0.197807f,-0.119660f),
            new Vector3(0.181620f,0.193829f,-0.181620f),
            new Vector3(0.150423f,0.192890f,-0.260541f),
            new Vector3(0.082751f,0.186698f,-0.308832f),
            new Vector3(-0.000000f,0.184207f,-0.317808f),
            new Vector3(-0.082752f,0.186697f,-0.308836f),
            new Vector3(-0.150895f,0.191820f,-0.261358f),
            new Vector3(-0.181620f,0.193829f,-0.181619f),
            new Vector3(-0.207863f,0.196589f,-0.120010f),
            new Vector3(-0.212157f,0.205372f,-0.056847f),
            new Vector3(-0.215416f,0.201193f,0.000000f),
            new Vector3(-0.208780f,0.196936f,0.055942f),
            new Vector3(-0.192172f,0.190928f,0.110951f),
            new Vector3(-0.162242f,0.187333f,0.162242f),
            new Vector3(-0.128826f,0.183003f,0.223133f),
            new Vector3(-0.068149f,0.185758f,0.254335f),
        };
        static readonly Vector3[] WhiteHem = {
            new Vector3(-0.000000f,0.157707f,0.269852f),
            new Vector3(0.071279f,0.156515f,0.266018f),
            new Vector3(0.133086f,0.156655f,0.230512f),
            new Vector3(0.165402f,0.160321f,0.165402f),
            new Vector3(0.194577f,0.164709f,0.112339f),
            new Vector3(0.211074f,0.172364f,0.056557f),
            new Vector3(0.218104f,0.177101f,-0.000000f),
            new Vector3(0.220332f,0.173695f,-0.059038f),
            new Vector3(0.211548f,0.175802f,-0.122137f),
            new Vector3(0.187320f,0.172126f,-0.187320f),
            new Vector3(0.158224f,0.170685f,-0.274051f),
            new Vector3(0.087621f,0.167344f,-0.327007f),
            new Vector3(-0.000000f,0.166564f,-0.335408f),
            new Vector3(-0.086779f,0.169650f,-0.323865f),
            new Vector3(-0.158878f,0.169479f,-0.275185f),
            new Vector3(-0.187320f,0.172126f,-0.187320f),
            new Vector3(-0.209538f,0.179143f,-0.120977f),
            new Vector3(-0.218705f,0.176319f,-0.058602f),
            new Vector3(-0.219578f,0.174528f,0.000000f),
            new Vector3(-0.210643f,0.172836f,0.056442f),
            new Vector3(-0.193410f,0.167333f,0.111665f),
            new Vector3(-0.164535f,0.162460f,0.164535f),
            new Vector3(-0.133650f,0.154309f,0.231489f),
            new Vector3(-0.071423f,0.155729f,0.266554f),
        };
        public static Vector3 HemEdge(float t,bool white) => ClosedFootCurve(white?WhiteHem:NavyHem,t);
        static readonly Vector3[] ShoeRim={
            new Vector3(0.111125f,0.124703f,-0.015000f),
            new Vector3(0.109153f,0.117690f,0.012892f),
            new Vector3(0.093750f,0.111044f,0.072500f),
            new Vector3(0.090781f,0.111373f,0.073645f),
            new Vector3(0.068750f,0.113039f,0.087027f),
            new Vector3(0.046997f,0.110959f,0.072525f),
            new Vector3(0.043750f,0.110962f,0.070000f),
            new Vector3(0.029110f,0.118063f,0.012366f),
            new Vector3(0.025259f,0.123863f,-0.015000f),
            new Vector3(0.027438f,0.127145f,-0.043520f),
            new Vector3(0.037269f,0.127852f,-0.067469f),
            new Vector3(0.053132f,0.128316f,-0.077843f),
            new Vector3(0.068750f,0.128670f,-0.079734f),
            new Vector3(0.083570f,0.128440f,-0.074631f),
            new Vector3(0.096298f,0.128510f,-0.060914f),
            new Vector3(0.106425f,0.126955f,-0.041009f)
        };
        static readonly Vector3[] ShoeWide={
            new Vector3(0.111170f,0.095000f,0.026941f),
            new Vector3(0.112367f,0.095000f,0.044124f),
            new Vector3(0.113482f,0.095000f,0.069401f),
            new Vector3(0.103803f,0.095000f,0.106411f),
            new Vector3(0.071035f,0.095000f,0.128966f),
            new Vector3(0.037640f,0.095000f,0.107466f),
            new Vector3(0.028292f,0.095000f,0.069319f),
            new Vector3(0.029787f,0.095000f,0.043825f),
            new Vector3(0.030273f,0.095000f,0.026973f),
            new Vector3(0.032071f,0.095000f,0.010953f),
            new Vector3(0.033689f,0.095000f,-0.010600f),
            new Vector3(0.037501f,0.095000f,-0.054027f),
            new Vector3(0.070871f,0.095000f,-0.071973f),
            new Vector3(0.098013f,0.095000f,-0.038039f),
            new Vector3(0.105278f,0.095000f,-0.007272f),
            new Vector3(0.108834f,0.095000f,0.011218f)
        };
        static readonly Vector3[] ShoeBottom={
            new Vector3(0.109013f,0.082500f,0.027752f),
            new Vector3(0.113236f,0.082500f,0.044658f),
            new Vector3(0.114628f,0.082500f,0.070478f),
            new Vector3(0.103896f,0.082500f,0.105522f),
            new Vector3(0.071555f,0.082500f,0.121310f),
            new Vector3(0.038091f,0.082500f,0.108024f),
            new Vector3(0.027763f,0.082500f,0.071545f),
            new Vector3(0.030006f,0.082500f,0.044814f),
            new Vector3(0.033150f,0.082500f,0.027463f),
            new Vector3(0.035270f,0.082500f,0.012528f),
            new Vector3(0.037237f,0.082500f,-0.006943f),
            new Vector3(0.038915f,0.082500f,-0.050877f),
            new Vector3(0.071616f,0.082500f,-0.068651f),
            new Vector3(0.095353f,0.082500f,-0.029396f),
            new Vector3(0.101571f,0.082500f,-0.002319f),
            new Vector3(0.105466f,0.082500f,0.013569f)
        };
        static readonly Vector3[] SockAnkle={
            new Vector3(0.108849f,0.145000f,-0.019613f),
            new Vector3(0.106193f,0.145000f,-0.004560f),
            new Vector3(0.100240f,0.145000f,0.010720f),
            new Vector3(0.088418f,0.145000f,0.025996f),
            new Vector3(0.069279f,0.145000f,0.033456f),
            new Vector3(0.050514f,0.145000f,0.026882f),
            new Vector3(0.039094f,0.145000f,0.011649f),
            new Vector3(0.033056f,0.145000f,-0.003985f),
            new Vector3(0.030924f,0.145000f,-0.019533f),
            new Vector3(0.032371f,0.145000f,-0.034952f),
            new Vector3(0.038171f,0.145000f,-0.050862f),
            new Vector3(0.050495f,0.145000f,-0.066076f),
            new Vector3(0.069669f,0.145000f,-0.070860f),
            new Vector3(0.087272f,0.145000f,-0.062023f),
            new Vector3(0.099512f,0.145000f,-0.049778f),
            new Vector3(0.105180f,0.145000f,-0.033890f)
        };
        // The outer two stations connect to the wall; alpha sampling there
        // finds the side panel instead of the narrow strap, so continue the arch.
        static readonly Vector3[] ShoeStrap={
            new Vector3(0.031250f,0.121000f,0.046875f),
            new Vector3(0.043750f,0.124211f,0.051875f),
            new Vector3(0.056250f,0.126312f,0.056875f),
            new Vector3(0.068750f,0.128513f,0.061875f),
            new Vector3(0.081250f,0.126977f,0.056875f),
            new Vector3(0.093750f,0.124982f,0.051875f),
            new Vector3(0.106250f,0.121700f,0.046875f)
        };
        static Vector3 ClosedFootCurve(Vector3[] ring,float t) {
            float f=Mathf.Repeat(t,1)*ring.Length;int i=(int)f;float u=f-i;
            Vector3 p0=ring[(i+ring.Length-1)%ring.Length],p1=ring[i],p2=ring[(i+1)%ring.Length],p3=ring[(i+2)%ring.Length];
            return .5f*((2*p1)+(-p0+p2)*u+(2*p0-5*p1+4*p2-p3)*u*u+(-p0+3*p1-3*p2+p3)*u*u*u);
        }
        public static Vector3 FootRing(float t,int row) {
            Vector3 rim=ClosedFootCurve(ShoeRim,t),wide=ClosedFootCurve(ShoeWide,t),bottom=ClosedFootCurve(ShoeBottom,t);
            if(row==0)return rim;
            float front=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI*2)),2);
            if(row==1||row==2) {
                Vector3 p=Vector3.Lerp(rim,wide,row==1?.57f:.84f);
                p.z+=(row==1?.006f:.003f)*front;p.y+=(row==1?.003f:.005f)*front;
                return p;
            }
            if(row==3)return wide;
            bottom.y=row==4?.0785f:.071f;return bottom;
        }
        public static Vector3 SockRing(float t,int row) {
            Vector3 ankle=ClosedFootCurve(SockAnkle,t),bottom=ClosedFootCurve(ShoeBottom,t);
            if(row==0){ankle.y=.205f;ankle.x=.065f+(ankle.x-.069f)*.8f;ankle.z*=.65f;return ankle;}
            if(row==1){ankle.y=.156f;return ankle;}
            if(row==2)return ankle;
            if(row==3){Vector3 p=ClosedFootCurve(ShoeRim,t),c=new Vector3(.069f,p.y,.005f);p=c+(p-c)*.88f;p.y-=.006f;return p;}
            bottom=ClosedFootCurve(ShoeWide,t);bottom.x=.069f+(bottom.x-.069f)*.88f;bottom.z=.02f+(bottom.z-.02f)*.88f;bottom.y=.083f;return bottom;
        }
        public static Vector3 FootStrap(float t) {
            float f=t*(ShoeStrap.Length-1);int i=Mathf.Min(ShoeStrap.Length-2,(int)f);
            float u=f-i;Vector3 p0=ShoeStrap[Mathf.Max(0,i-1)],p1=ShoeStrap[i],p2=ShoeStrap[i+1],p3=ShoeStrap[Mathf.Min(ShoeStrap.Length-1,i+2)];
            return .5f*((2*p1)+(-p0+p2)*u+(2*p0-5*p1+4*p2-p3)*u*u+(-p0+3*p1-3*p2+p3)*u*u*u);
        }

        // Sparse positional sections measured through alpha, not the transparent mesh padding.
        static readonly Vector3[][] FinShellRows={
            new[]{ // alpha-visible open shell 22
                new Vector3(0.1750000f,0.7503579f,0.0898083f),new Vector3(0.1750000f,0.7503579f,0.0898083f),new Vector3(0.1750000f,0.7536685f,0.0885269f),new Vector3(0.1750000f,0.7538079f,0.0883365f),new Vector3(0.1750000f,0.7463393f,-0.0186869f),new Vector3(0.1750000f,0.7404342f,-0.0222056f),new Vector3(0.1750000f,0.7181797f,-0.0320876f),new Vector3(0.1750000f,0.6950029f,-0.0316762f),new Vector3(0.1750000f,0.6709382f,-0.0237294f),
                new Vector3(0.1875000f,0.7374113f,0.0890923f),new Vector3(0.1875000f,0.7397315f,0.0896331f),new Vector3(0.1875000f,0.7642129f,0.0712250f),new Vector3(0.1875000f,0.7762885f,0.0410418f),new Vector3(0.1875000f,0.7709561f,0.0097023f),new Vector3(0.1875000f,0.7545513f,-0.0125470f),new Vector3(0.1875000f,0.7332290f,-0.0292711f),new Vector3(0.1875000f,0.7034682f,-0.0349535f),new Vector3(0.1875000f,0.6732172f,-0.0243738f),
                new Vector3(0.2000000f,0.7269552f,0.0872671f),new Vector3(0.2000000f,0.7378284f,0.0884455f),new Vector3(0.2000000f,0.7613182f,0.0700978f),new Vector3(0.2000000f,0.7735995f,0.0406831f),new Vector3(0.2000000f,0.7692768f,0.0093047f),new Vector3(0.2000000f,0.7531354f,-0.0135177f),new Vector3(0.2000000f,0.7315710f,-0.0292828f),new Vector3(0.2000000f,0.7025220f,-0.0345071f),new Vector3(0.2000000f,0.6731654f,-0.0193770f),
                new Vector3(0.2250000f,0.7118571f,0.0834583f),new Vector3(0.2250000f,0.7289253f,0.0826547f),new Vector3(0.2250000f,0.7528891f,0.0682651f),new Vector3(0.2250000f,0.7647158f,0.0403236f),new Vector3(0.2250000f,0.7602695f,0.0109066f),new Vector3(0.2250000f,0.7463759f,-0.0130055f),new Vector3(0.2250000f,0.7236002f,-0.0264012f),new Vector3(0.2250000f,0.6955338f,-0.0284034f),new Vector3(0.2250000f,0.6666229f,-0.0160744f),
                new Vector3(0.2500000f,0.7031798f,0.0791300f),new Vector3(0.2500000f,0.7143737f,0.0774454f),new Vector3(0.2500000f,0.7378625f,0.0659119f),new Vector3(0.2500000f,0.7489150f,0.0411323f),new Vector3(0.2500000f,0.7469212f,0.0144940f),new Vector3(0.2500000f,0.7323344f,-0.0070745f),new Vector3(0.2500000f,0.7093554f,-0.0174735f),new Vector3(0.2500000f,0.6830437f,-0.0178765f),new Vector3(0.2500000f,0.6555142f,-0.0046291f),
                new Vector3(0.2750000f,0.6791515f,0.0712041f),new Vector3(0.2750000f,0.6982151f,0.0701589f),new Vector3(0.2750000f,0.7135363f,0.0604081f),new Vector3(0.2750000f,0.7228853f,0.0423285f),new Vector3(0.2750000f,0.7224550f,0.0206902f),new Vector3(0.2750000f,0.7111696f,0.0039802f),new Vector3(0.2750000f,0.6948706f,-0.0040481f),new Vector3(0.2750000f,0.6763173f,-0.0045603f),new Vector3(0.2750000f,0.6571665f,0.0032208f),
                new Vector3(0.3000000f,0.6654441f,0.0631302f),new Vector3(0.3000000f,0.6775837f,0.0626518f),new Vector3(0.3000000f,0.6865103f,0.0555371f),new Vector3(0.3000000f,0.6912577f,0.0432284f),new Vector3(0.3000000f,0.6904884f,0.0296577f),new Vector3(0.3000000f,0.6855408f,0.0171888f),new Vector3(0.3000000f,0.6754671f,0.0124955f),new Vector3(0.3000000f,0.6646151f,0.0126627f),new Vector3(0.3000000f,0.6533702f,0.0184953f),
                new Vector3(0.3250000f,0.6597684f,0.0520987f),new Vector3(0.3250000f,0.6629055f,0.0514463f),new Vector3(0.3250000f,0.6659539f,0.0489753f),new Vector3(0.3250000f,0.6673499f,0.0444579f),new Vector3(0.3250000f,0.6672429f,0.0395111f),new Vector3(0.3250000f,0.6657447f,0.0346258f),new Vector3(0.3250000f,0.6622411f,0.0328473f),new Vector3(0.3250000f,0.6584826f,0.0330460f),new Vector3(0.3250000f,0.6546623f,0.0351420f),
                new Vector3(0.3404552f,0.6584740f,0.0454307f),new Vector3(0.3404552f,0.6584740f,0.0454307f),new Vector3(0.3404552f,0.6584740f,0.0454307f),new Vector3(0.3404552f,0.6584740f,0.0454307f),new Vector3(0.3404552f,0.6584740f,0.0454307f),new Vector3(0.3404552f,0.6584740f,0.0454307f),new Vector3(0.3404552f,0.6584740f,0.0454307f),new Vector3(0.3404552f,0.6584740f,0.0454307f),new Vector3(0.3404552f,0.6584740f,0.0454307f),
            },
            new[]{ // alpha-visible open shell 23
                new Vector3(0.1750000f,0.7149928f,-0.0249922f),new Vector3(0.1750000f,0.7149928f,-0.0249922f),new Vector3(0.1750000f,0.7149928f,-0.0249922f),new Vector3(0.1750000f,0.7149928f,-0.0249922f),new Vector3(0.1750000f,0.7149928f,-0.0249922f),new Vector3(0.1750000f,0.7093848f,-0.0257477f),new Vector3(0.1750000f,0.6987413f,-0.0249976f),new Vector3(0.1750000f,0.6946406f,-0.0230122f),new Vector3(0.1750000f,0.6922823f,-0.0217352f),
                new Vector3(0.1875000f,0.7432228f,0.0809849f),new Vector3(0.1875000f,0.7432228f,0.0809849f),new Vector3(0.1875000f,0.7484713f,0.0755976f),new Vector3(0.1875000f,0.7510097f,0.0721623f),new Vector3(0.1875000f,0.7458191f,-0.0105035f),new Vector3(0.1875000f,0.7380670f,-0.0171637f),new Vector3(0.1875000f,0.7197864f,-0.0274406f),new Vector3(0.1875000f,0.6993856f,-0.0288144f),new Vector3(0.1875000f,0.6786611f,-0.0213383f),
                new Vector3(0.2000000f,0.7308481f,0.0800853f),new Vector3(0.2000000f,0.7343619f,0.0812320f),new Vector3(0.2000000f,0.7550769f,0.0640045f),new Vector3(0.2000000f,0.7655435f,0.0374799f),new Vector3(0.2000000f,0.7613236f,0.0098056f),new Vector3(0.2000000f,0.7473447f,-0.0104055f),new Vector3(0.2000000f,0.7287087f,-0.0253681f),new Vector3(0.2000000f,0.7029708f,-0.0295210f),new Vector3(0.2000000f,0.6770302f,-0.0198257f),
                new Vector3(0.2250000f,0.7134823f,0.0771957f),new Vector3(0.2250000f,0.7259783f,0.0755637f),new Vector3(0.2250000f,0.7471404f,0.0621991f),new Vector3(0.2250000f,0.7578708f,0.0371888f),new Vector3(0.2250000f,0.7537898f,0.0106495f),new Vector3(0.2250000f,0.7409512f,-0.0103201f),new Vector3(0.2250000f,0.7211852f,-0.0238542f),new Vector3(0.2250000f,0.6961203f,-0.0248197f),new Vector3(0.2250000f,0.6699305f,-0.0131084f),
                new Vector3(0.2500000f,0.7041252f,0.0734281f),new Vector3(0.2500000f,0.7120282f,0.0711648f),new Vector3(0.2500000f,0.7326604f,0.0604073f),new Vector3(0.2500000f,0.7427081f,0.0383813f),new Vector3(0.2500000f,0.7411444f,0.0143711f),new Vector3(0.2500000f,0.7281928f,-0.0052557f),new Vector3(0.2500000f,0.7075883f,-0.0149057f),new Vector3(0.2500000f,0.6840683f,-0.0146710f),new Vector3(0.2500000f,0.6592456f,-0.0023757f),
                new Vector3(0.2750000f,0.6803902f,0.0666266f),new Vector3(0.2750000f,0.6950824f,0.0645631f),new Vector3(0.2750000f,0.7082140f,0.0557430f),new Vector3(0.2750000f,0.7165498f,0.0402036f),new Vector3(0.2750000f,0.7166129f,0.0210194f),new Vector3(0.2750000f,0.7068029f,0.0058926f),new Vector3(0.2750000f,0.6922408f,-0.0008987f),new Vector3(0.2750000f,0.6761239f,-0.0010587f),new Vector3(0.2750000f,0.6592268f,0.0061786f),
                new Vector3(0.3000000f,0.6661530f,0.0581379f),new Vector3(0.3000000f,0.6755306f,0.0566826f),new Vector3(0.3000000f,0.6813810f,0.0509621f),new Vector3(0.3000000f,0.6846304f,0.0415291f),new Vector3(0.3000000f,0.6845599f,0.0305196f),new Vector3(0.3000000f,0.6812053f,0.0202806f),new Vector3(0.3000000f,0.6741917f,0.0172031f),new Vector3(0.3000000f,0.6668444f,0.0166764f),new Vector3(0.3000000f,0.6591746f,0.0190495f),
                new Vector3(0.3250000f,0.6608377f,0.0464401f),new Vector3(0.3250000f,0.6609348f,0.0464227f),new Vector3(0.3250000f,0.6618964f,0.0450350f),new Vector3(0.3250000f,0.6625429f,0.0432699f),new Vector3(0.3250000f,0.6620917f,0.0412613f),new Vector3(0.3250000f,0.6616544f,0.0395482f),new Vector3(0.3250000f,0.6607084f,0.0386960f),new Vector3(0.3250000f,0.6595729f,0.0386034f),new Vector3(0.3250000f,0.6582440f,0.0382659f),
                new Vector3(0.3310885f,0.6588512f,0.0423248f),new Vector3(0.3310885f,0.6588512f,0.0423248f),new Vector3(0.3310885f,0.6588512f,0.0423248f),new Vector3(0.3310885f,0.6588512f,0.0423248f),new Vector3(0.3310885f,0.6588512f,0.0423248f),new Vector3(0.3310885f,0.6588512f,0.0423248f),new Vector3(0.3310885f,0.6588512f,0.0423248f),new Vector3(0.3310885f,0.6588512f,0.0423248f),new Vector3(0.3310885f,0.6588512f,0.0423248f),
            },
            new[]{ // alpha-visible open shell 24
                new Vector3(-0.1750000f,0.7685469f,0.0473257f),new Vector3(-0.1750000f,0.7685469f,0.0473257f),new Vector3(-0.1750000f,0.7693268f,0.0449579f),new Vector3(-0.1750000f,0.7611542f,0.0137788f),new Vector3(-0.1750000f,0.7496163f,-0.0027079f),new Vector3(-0.1750000f,0.7383332f,-0.0154190f),new Vector3(-0.1750000f,0.7201448f,-0.0274904f),new Vector3(-0.1750000f,0.6886837f,-0.0347156f),new Vector3(-0.1750000f,0.6556733f,-0.0266810f),
                new Vector3(-0.1875000f,0.7312105f,0.1000446f),new Vector3(-0.1875000f,0.7312105f,0.1000446f),new Vector3(-0.1875000f,0.7545892f,0.0796018f),new Vector3(-0.1875000f,0.7686226f,0.0473233f),new Vector3(-0.1875000f,0.7623585f,0.0128761f),new Vector3(-0.1875000f,0.7455074f,-0.0123094f),new Vector3(-0.1875000f,0.7222717f,-0.0291287f),new Vector3(-0.1875000f,0.6908442f,-0.0360375f),new Vector3(-0.1875000f,0.6589921f,-0.0301100f),
                new Vector3(-0.2000000f,0.7216656f,0.0975506f),new Vector3(-0.2000000f,0.7298928f,0.0977914f),new Vector3(-0.2000000f,0.7544988f,0.0782500f),new Vector3(-0.2000000f,0.7682748f,0.0463116f),new Vector3(-0.2000000f,0.7615437f,0.0121761f),new Vector3(-0.2000000f,0.7452665f,-0.0121848f),new Vector3(-0.2000000f,0.7232983f,-0.0281238f),new Vector3(-0.2000000f,0.6933974f,-0.0363770f),new Vector3(-0.2000000f,0.6627139f,-0.0265170f),
                new Vector3(-0.2250000f,0.7063336f,0.0882652f),new Vector3(-0.2250000f,0.7276645f,0.0865990f),new Vector3(-0.2250000f,0.7520706f,0.0729591f),new Vector3(-0.2250000f,0.7649914f,0.0434180f),new Vector3(-0.2250000f,0.7578115f,0.0122756f),new Vector3(-0.2250000f,0.7426646f,-0.0099918f),new Vector3(-0.2250000f,0.7219894f,-0.0233719f),new Vector3(-0.2250000f,0.6942694f,-0.0310918f),new Vector3(-0.2250000f,0.6656368f,-0.0247785f),
                new Vector3(-0.2500000f,0.7035758f,0.0809780f),new Vector3(-0.2500000f,0.7177984f,0.0791867f),new Vector3(-0.2500000f,0.7418101f,0.0677368f),new Vector3(-0.2500000f,0.7532638f,0.0408582f),new Vector3(-0.2500000f,0.7477118f,0.0132127f),new Vector3(-0.2500000f,0.7319828f,-0.0056623f),new Vector3(-0.2500000f,0.7122367f,-0.0166355f),new Vector3(-0.2500000f,0.6861869f,-0.0237989f),new Vector3(-0.2500000f,0.6593405f,-0.0161192f),
                new Vector3(-0.2750000f,0.6842648f,0.0733250f),new Vector3(-0.2750000f,0.7031626f,0.0747784f),new Vector3(-0.2750000f,0.7208632f,0.0642968f),new Vector3(-0.2750000f,0.7292145f,0.0412544f),new Vector3(-0.2750000f,0.7229128f,0.0186087f),new Vector3(-0.2750000f,0.7137995f,0.0010909f),new Vector3(-0.2750000f,0.6989092f,-0.0085948f),new Vector3(-0.2750000f,0.6795625f,-0.0137277f),new Vector3(-0.2750000f,0.6598672f,-0.0086258f),
                new Vector3(-0.3000000f,0.6785711f,0.0679795f),new Vector3(-0.3000000f,0.6867773f,0.0680978f),new Vector3(-0.3000000f,0.6987856f,0.0600950f),new Vector3(-0.3000000f,0.7036752f,0.0418207f),new Vector3(-0.3000000f,0.6993802f,0.0248456f),new Vector3(-0.3000000f,0.6930281f,0.0125239f),new Vector3(-0.3000000f,0.6838930f,0.0042123f),new Vector3(-0.3000000f,0.6711951f,0.0000209f),new Vector3(-0.3000000f,0.6582662f,0.0026329f),
                new Vector3(-0.3250000f,0.6679476f,0.0532223f),new Vector3(-0.3250000f,0.6761500f,0.0550794f),new Vector3(-0.3250000f,0.6817800f,0.0508019f),new Vector3(-0.3250000f,0.6838099f,0.0399733f),new Vector3(-0.3250000f,0.6825521f,0.0297827f),new Vector3(-0.3250000f,0.6789739f,0.0232681f),new Vector3(-0.3250000f,0.6747740f,0.0187045f),new Vector3(-0.3250000f,0.6691966f,0.0166763f),new Vector3(-0.3250000f,0.6631318f,0.0163147f),
                new Vector3(-0.3437500f,0.6709099f,0.0406364f),new Vector3(-0.3437500f,0.6723816f,0.0407321f),new Vector3(-0.3437500f,0.6739622f,0.0394069f),new Vector3(-0.3437500f,0.6741873f,0.0360255f),new Vector3(-0.3437500f,0.6735838f,0.0335443f),new Vector3(-0.3437500f,0.6728848f,0.0319927f),new Vector3(-0.3437500f,0.6719450f,0.0312383f),new Vector3(-0.3437500f,0.6708151f,0.0308218f),new Vector3(-0.3437500f,0.6687831f,0.0290054f),
                new Vector3(-0.3520465f,0.6717754f,0.0353947f),new Vector3(-0.3520465f,0.6717754f,0.0353947f),new Vector3(-0.3520465f,0.6717754f,0.0353947f),new Vector3(-0.3520465f,0.6717754f,0.0353947f),new Vector3(-0.3520465f,0.6717754f,0.0353947f),new Vector3(-0.3520465f,0.6717754f,0.0353947f),new Vector3(-0.3520465f,0.6717754f,0.0353947f),new Vector3(-0.3520465f,0.6717754f,0.0353947f),new Vector3(-0.3520465f,0.6717754f,0.0353947f),
            },
            new[]{ // alpha-visible open shell 25
                new Vector3(-0.1750000f,0.7298367f,-0.0126520f),new Vector3(-0.1750000f,0.7298367f,-0.0126520f),new Vector3(-0.1750000f,0.7293315f,-0.0129713f),new Vector3(-0.1750000f,0.7196500f,-0.0190906f),new Vector3(-0.1750000f,0.7154407f,-0.0217511f),new Vector3(-0.1750000f,0.7105739f,-0.0240573f),new Vector3(-0.1750000f,0.7007454f,-0.0260069f),new Vector3(-0.1750000f,0.6858061f,-0.0263879f),new Vector3(-0.1750000f,0.6739597f,-0.0214204f),
                new Vector3(-0.1875000f,0.7583305f,0.0483427f),new Vector3(-0.1875000f,0.7583305f,0.0483427f),new Vector3(-0.1875000f,0.7595633f,0.0443386f),new Vector3(-0.1875000f,0.7563172f,0.0162091f),new Vector3(-0.1875000f,0.7462592f,-0.0004130f),new Vector3(-0.1875000f,0.7362218f,-0.0130687f),new Vector3(-0.1875000f,0.7199401f,-0.0240989f),new Vector3(-0.1875000f,0.6930421f,-0.0311941f),new Vector3(-0.1875000f,0.6652180f,-0.0255034f),
                new Vector3(-0.2000000f,0.7274015f,0.0872317f),new Vector3(-0.2000000f,0.7274015f,0.0872317f),new Vector3(-0.2000000f,0.7491154f,0.0690337f),new Vector3(-0.2000000f,0.7609403f,0.0407883f),new Vector3(-0.2000000f,0.7555740f,0.0109603f),new Vector3(-0.2000000f,0.7404503f,-0.0097541f),new Vector3(-0.2000000f,0.7212672f,-0.0242147f),new Vector3(-0.2000000f,0.6945490f,-0.0315142f),new Vector3(-0.2000000f,0.6670630f,-0.0252684f),
                new Vector3(-0.2250000f,0.7094817f,0.0800643f),new Vector3(-0.2250000f,0.7255440f,0.0776237f),new Vector3(-0.2250000f,0.7476530f,0.0655884f),new Vector3(-0.2250000f,0.7587449f,0.0388582f),new Vector3(-0.2250000f,0.7526384f,0.0112166f),new Vector3(-0.2250000f,0.7392134f,-0.0088251f),new Vector3(-0.2250000f,0.7205283f,-0.0213529f),new Vector3(-0.2250000f,0.6955215f,-0.0276493f),new Vector3(-0.2250000f,0.6696916f,-0.0210114f),
                new Vector3(-0.2500000f,0.7048231f,0.0737436f),new Vector3(-0.2500000f,0.7156316f,0.0720034f),new Vector3(-0.2500000f,0.7370143f,0.0615898f),new Vector3(-0.2500000f,0.7476659f,0.0376565f),new Vector3(-0.2500000f,0.7427313f,0.0126046f),new Vector3(-0.2500000f,0.7293104f,-0.0056105f),new Vector3(-0.2500000f,0.7107415f,-0.0152506f),new Vector3(-0.2500000f,0.6870980f,-0.0204586f),new Vector3(-0.2500000f,0.6630410f,-0.0127925f),
                new Vector3(-0.2750000f,0.6853207f,0.0678989f),new Vector3(-0.2750000f,0.7009641f,0.0680095f),new Vector3(-0.2750000f,0.7161146f,0.0588380f),new Vector3(-0.2750000f,0.7237256f,0.0387669f),new Vector3(-0.2750000f,0.7189765f,0.0183021f),new Vector3(-0.2750000f,0.7108272f,0.0022698f),new Vector3(-0.2750000f,0.6973659f,-0.0057141f),new Vector3(-0.2750000f,0.6806197f,-0.0098164f),new Vector3(-0.2750000f,0.6633378f,-0.0055291f),
                new Vector3(-0.3000000f,0.6784895f,0.0624326f),new Vector3(-0.3000000f,0.6838559f,0.0623009f),new Vector3(-0.3000000f,0.6936770f,0.0552863f),new Vector3(-0.3000000f,0.6978768f,0.0399772f),new Vector3(-0.3000000f,0.6946245f,0.0252131f),new Vector3(-0.3000000f,0.6892397f,0.0144495f),new Vector3(-0.3000000f,0.6814432f,0.0074426f),new Vector3(-0.3000000f,0.6709302f,0.0043447f),new Vector3(-0.3000000f,0.6602199f,0.0070668f),
                new Vector3(-0.3250000f,0.6686025f,0.0489500f),new Vector3(-0.3250000f,0.6732354f,0.0490859f),new Vector3(-0.3250000f,0.6769180f,0.0458261f),new Vector3(-0.3250000f,0.6784938f,0.0385271f),new Vector3(-0.3250000f,0.6775633f,0.0314514f),new Vector3(-0.3250000f,0.6753282f,0.0264091f),new Vector3(-0.3250000f,0.6722527f,0.0232532f),new Vector3(-0.3250000f,0.6683495f,0.0220606f),new Vector3(-0.3250000f,0.6642230f,0.0221758f),
                new Vector3(-0.3432720f,0.6705561f,0.0351227f),new Vector3(-0.3432720f,0.6705561f,0.0351227f),new Vector3(-0.3432720f,0.6705561f,0.0351227f),new Vector3(-0.3432720f,0.6705561f,0.0351227f),new Vector3(-0.3432720f,0.6705561f,0.0351227f),new Vector3(-0.3432720f,0.6705561f,0.0351227f),new Vector3(-0.3432720f,0.6705561f,0.0351227f),new Vector3(-0.3432720f,0.6705561f,0.0351227f),new Vector3(-0.3432720f,0.6705561f,0.0351227f),
            },
        };
        // Monotone Hermite segments retain each measured extremum without
        // the flat spots introduced by clamping a Catmull-Rom overshoot.
        static Vector3 FinCurve(Vector3 p0,Vector3 a,Vector3 b,Vector3 p3,float t) {
            Vector3 result=Vector3.zero;
            for(int k=0;k<3;k++) {
                float before=a[k]-p0[k],delta=b[k]-a[k],after=p3[k]-b[k];
                float m0=before*delta>0?2*before*delta/(before+delta):0;
                float m1=after*delta>0?2*after*delta/(after+delta):0;
                if(p0==a)m0=delta;if(p3==b)m1=delta;
                result[k]=(2*t*t*t-3*t*t+1)*a[k]+(t*t*t-2*t*t+t)*m0+
                    (-2*t*t*t+3*t*t)*b[k]+(t*t*t-t*t)*m1;
            }
            return result;
        }
        public static int FinRows(int sign,bool inner)=>FinShellRows[(sign>0?0:2)+(inner?1:0)].Length/9-1;
        public static Vector3 FinShell(int sign,bool inner,float t,float v) {
            var rows=FinShellRows[(sign>0?0:2)+(inner?1:0)];int count=rows.Length/9;
            float f=t*(count-1);int row=Mathf.Min(count-2,(int)f);float u=f-row;
            Func<int,Vector3> section=col=>{
                Vector3 p0=rows[Mathf.Max(0,row-1)*9+col],p1=rows[row*9+col],p2=rows[(row+1)*9+col],p3=rows[Mathf.Min(count-1,row+2)*9+col];
                return FinCurve(p0,p1,p2,p3,u);
            };
            float across=v*8;int c=Mathf.Min(7,(int)across);Vector3 a=section(c),b=section(c+1);
            if((a-b).sqrMagnitude<.00001f*.00001f)return a;
            Vector3 p0=section(Mathf.Max(0,c-1)),p3=section(Mathf.Min(8,c+2));
            return FinCurve(p0,a,b,p3,across-c);
        }

        static readonly Dictionary<int,Bounds> SourceBounds=new Dictionary<int,Bounds>();
        static Vector3 AccessoryPoint(Vector3 p,int region) {
            Bounds source=SourceBounds[region];
            if(region==5) {
                // Measured front apron outline; keep the separate raised whale
                // and cloth depth together, excluding rear UV islands.
                return FishRebuiltModel.FitApronToGuides(p,source);
            }
            if(region==6||region==7) {
                float side=region==6?1:-1;
                Vector3 shoeDelta=p-source.center;
                return new Vector3(side*.07087f,.09959f,.02357f)+
                    Vector3.Scale(shoeDelta,new Vector3(.09140f/source.size.x,.05842f/source.size.y,.21196f/source.size.z));
            }
            float sign=region==3?1:-1;
            Vector3 center=new Vector3(sign*.2572f,.7913f,.0297f),size=new Vector3(.062f,.048f,.077f);
            Vector3 q=p-source.center;
            return center+Vector3.Scale(q,new Vector3(size.x/source.size.x,size.y/source.size.y,size.z/source.size.z));
        }
        static Vector4 Sample(Vector4[] rows,float y) {
            if(y<=rows[0].x)return rows[0];if(y>=rows[rows.Length-1].x)return rows[rows.Length-1];
            int i=0;while(y>rows[i+1].x)i++;
            float t=(y-rows[i].x)/(rows[i+1].x-rows[i].x);
            Vector4 a=rows[i],b=rows[i+1];float h=b.x-a.x;
            Vector4 m0=i==0?(b-a)/h:(b-rows[i-1])/(b.x-rows[i-1].x);
            Vector4 m1=i+2==rows.Length?(b-a)/h:(rows[i+2]-a)/(rows[i+2].x-a.x);
            Vector4 value=(2*t*t*t-3*t*t+1)*a+(t*t*t-2*t*t+t)*h*m0+(-2*t*t*t+3*t*t)*b+(t*t*t-t*t)*h*m1;
            for(int k=1;k<4;k++)value[k]=Mathf.Clamp(value[k],Mathf.Min(a[k],b[k]),Mathf.Max(a[k],b[k]));
            return value;
        }
        public static Vector3 Point(Vector3 p,int region) {
            if(region==8)return FishRearCrownGuides.Point(Point(p,2));
            if(region>=3)return AccessoryPoint(p,region);
            if(region==1) {p.x*=Sample(SkinWidth,p.y).y;return p;}
            if(region==2) {
                Vector4 g=Sample(HairEnvelope,p.y);float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.53f,.595f,p.y));
                p.x*=Mathf.Lerp(1,g.y,blend);p.z=Mathf.Lerp(p.z,p.z*g.z+g.w,blend);
                p.y+=.008f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.85f,.98f,p.y));
                // Retain a smooth backing shell inside the guided front strips.
                float crown=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.81f,.93f,p.y))*
                    Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.06f,.04f,p.z))*
                    (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.00f,1.04f,p.y)));
                float radius=Mathf.Sqrt(p.x*p.x+(p.y-.72f)*(p.y-.72f));
                float excess=Mathf.Max(0,radius-.252f);
                float radial=1-.8f*excess*Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.018f,excess))*crown/Mathf.Max(.001f,radius);
                p.x*=radial;p.y=.72f+(p.y-.72f)*radial;p.z-=.03f*crown;
                if(p.y>.87f&&p.y<.99f) {
                    Vector4 rearGuide=Sample(RearCrownCorrection,p.y);
                    float rear=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.02f,-.12f,p.z));
                    p.x*=Mathf.Lerp(1,rearGuide.y,rear);p.z+=rearGuide.z*rear;
                }
                return p;
            }
            return p;
        }
        public static void Apply(Mesh mesh,int[] regions) {
            var old=mesh.vertices;var mapped=new Vector3[old.Length];var normals=mesh.normals;
            var uv=mesh.uv;var colors=mesh.colors;
            SourceBounds.Clear();
            for(int i=0;i<old.Length;i++)if(regions[i]>=3&&regions[i]<=7) {
                Bounds b;if(!SourceBounds.TryGetValue(regions[i],out b))b=new Bounds(old[i],Vector3.zero);
                b.Encapsulate(old[i]);SourceBounds[regions[i]]=b;
            }
            const float e=.00005f;
            for(int i=0;i<old.Length;i++) {
                int region=regions[i];Vector3 p=old[i];mapped[i]=Point(p,region);if(region==0)continue;
                if(region==5&&uv[i].y>1)uv[i]=FishRebuiltModel.ApronPaintUV(mapped[i]);
                Vector3 x=(Point(p+Vector3.right*e,region)-Point(p-Vector3.right*e,region))/(2*e);
                Vector3 y=(Point(p+Vector3.up*e,region)-Point(p-Vector3.up*e,region))/(2*e);
                Vector3 z=(Point(p+Vector3.forward*e,region)-Point(p-Vector3.forward*e,region))/(2*e);
                Vector3 n=normals[i];normals[i]=(Vector3.Cross(y,z)*n.x+Vector3.Cross(z,x)*n.y+Vector3.Cross(x,y)*n.z).normalized;
                if(region==8) {
                    Vector3 target=FishRearCrownGuides.Normal(mapped[i]);
                    if(Vector3.Dot(normals[i],target)<0)target=-target;
                    normals[i]=Vector3.Slerp(normals[i],target,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,.76f,mapped[i].y))).normalized;
                    uv[i]=new Vector2(Mathf.Clamp01(.5f+mapped[i].x/.82f),(1152+Mathf.Clamp01((1.03f-mapped[i].y)/.96f)*511)/FishRebuiltModel.PaintHeight);
                    colors[i]=Color.Lerp(colors[i],Color.white*.97f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,.75f,mapped[i].y)));
                }
            }
            var shapes=new List<Tuple<string,float,Vector3[]>>();
            for(int s=0;s<mesh.blendShapeCount;s++) {
                var delta=new Vector3[old.Length];mesh.GetBlendShapeFrameVertices(s,0,delta,null,null);
                for(int i=0;i<old.Length;i++)delta[i]=Point(old[i]+delta[i],regions[i])-mapped[i];
                shapes.Add(Tuple.Create(mesh.GetBlendShapeName(s),mesh.GetBlendShapeFrameWeight(s,0),delta));
            }
            mesh.ClearBlendShapes();mesh.vertices=mapped;mesh.normals=normals;mesh.uv=uv;mesh.colors=colors;
            foreach(var s in shapes)mesh.AddBlendShapeFrame(s.Item1,s.Item2,s.Item3,new Vector3[old.Length],new Vector3[old.Length]);
            mesh.RecalculateBounds();
        }
    }
}
