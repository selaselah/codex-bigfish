using UnityEngine;
namespace DesktopFish.Editor {
    // Independent folded cloth facets from sparse visible boundary positions.
    // The old own cloth sheet, alpha edge and arclength UV map are unchanged.
    public static class FishHeadbandFoldPaintGuides {
        static readonly Vector3[][] Folds={new[]{new Vector3(0.140000000f,0.111979898f,0.156250183f),new Vector3(0.379999995f,0.105467371f,0.144534961f),new Vector3(0.560000002f,0.100582976f,0.135748544f),new Vector3(0.740000010f,0.095702757f,0.130873751f),new Vector3(0.920000017f,0.103510644f,0.114259447f),new Vector3(1.000000000f,0.103510644f,0.114259447f)},new[]{new Vector3(0.140000000f,0.205080155f,0.249671097f),new Vector3(0.379999995f,0.201171245f,0.235352826f),new Vector3(0.560000002f,0.198239561f,0.224614123f),new Vector3(0.740000010f,0.197261900f,0.219738507f),new Vector3(0.920000017f,0.206061512f,0.206061512f),new Vector3(1.000000000f,0.206061512f,0.206061512f)},new[]{new Vector3(0.140000000f,0.288419636f,0.330074446f),new Vector3(0.379999995f,0.287109914f,0.318359856f),new Vector3(0.560000002f,0.286127622f,0.309573913f),new Vector3(0.740000010f,0.287104744f,0.304695322f),new Vector3(0.920000017f,0.294921565f,0.294921565f),new Vector3(1.000000000f,0.294921565f,0.294921565f)},new[]{new Vector3(0.140000000f,0.364909403f,0.420574596f),new Vector3(0.379999995f,0.366209864f,0.410157475f),new Vector3(0.560000002f,0.367185209f,0.402344634f),new Vector3(0.740000010f,0.368160978f,0.401366778f),new Vector3(0.920000017f,0.377929020f,0.401373203f),new Vector3(1.000000000f,0.377929020f,0.401373203f)},new[]{new Vector3(0.140000000f,0.472335400f,0.541993339f),new Vector3(0.379999995f,0.473632349f,0.526368066f),new Vector3(0.560000002f,0.474605061f,0.514649110f),new Vector3(0.740000010f,0.477530657f,0.510744665f),new Vector3(0.920000017f,0.479483248f,0.494140663f),new Vector3(1.000000000f,0.479483248f,0.494140663f)},new[]{new Vector3(0.140000000f,0.581380506f,0.641275364f),new Vector3(0.379999995f,0.583982953f,0.630861481f),new Vector3(0.560000002f,0.585934788f,0.623051069f),new Vector3(0.740000010f,0.588860048f,0.620117325f),new Vector3(0.920000017f,0.601560248f,0.619146860f),new Vector3(1.000000000f,0.601560248f,0.619146860f)},new[]{new Vector3(0.140000000f,0.678710049f,0.735677659f),new Vector3(0.379999995f,0.686521252f,0.726562823f),new Vector3(0.560000002f,0.692379655f,0.719726696f),new Vector3(0.740000010f,0.696280331f,0.717775767f),new Vector3(0.920000017f,0.698229808f,0.709962307f),new Vector3(1.000000000f,0.698229808f,0.709962307f)},new[]{new Vector3(0.140000000f,0.763673539f,0.824225489f),new Vector3(0.379999995f,0.771482422f,0.816409940f),new Vector3(0.560000002f,0.777339084f,0.810548278f),new Vector3(0.740000010f,0.783200807f,0.811525458f),new Vector3(0.920000017f,0.795888222f,0.795888222f),new Vector3(1.000000000f,0.795888222f,0.795888222f)},new[]{new Vector3(0.140000000f,0.863281039f,0.916670543f),new Vector3(0.379999995f,0.871090729f,0.914064468f),new Vector3(0.560000002f,0.876947996f,0.912109911f),new Vector3(0.740000010f,0.880849016f,0.916993595f),new Vector3(0.920000017f,0.884749752f,0.898436412f),new Vector3(1.000000000f,0.884749752f,0.898436412f)}};
        static Vector2 Edges(Vector3[] rows,float v) {
            int i=0;while(i<rows.Length-2&&v>rows[i+1].x)i++;
            float t=Mathf.InverseLerp(rows[i].x,rows[i+1].x,v);
            return new Vector2(Mathf.Lerp(rows[i].y,rows[i+1].y,t),Mathf.Lerp(rows[i].z,rows[i+1].z,t));
        }
        public static Color Paint(float u,float v) {
            float shade=0;
            if(v>=.14f)foreach(var fold in Folds) {
                Vector2 edge=Edges(fold,v);float width=Mathf.Max(.001f,edge.y-edge.x),phase=(u-edge.x)/width;
                // Two converging edges and an asymmetric planar fabric face.
                float aa=.0016f;
                float left=1-Mathf.SmoothStep(0,1,Mathf.Abs(u-edge.x)/aa);
                float right=1-Mathf.SmoothStep(0,1,Mathf.Abs(u-edge.y)/aa);
                float face=0;
                if(phase>=0&&phase<=1)face=.24f*(1-phase)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.14f,.38f,v));
                shade=Mathf.Max(shade,Mathf.Max(left*.54f,right*.38f)+face);
            }
            Color color=Color.Lerp(Color.white,new Color(.73f,.77f,.88f,1),Mathf.Clamp01(shade));
            // Thin continuous sewn binding, independently painted.
            float seam=1-Mathf.SmoothStep(0,1,Mathf.Abs(v-.14f)/.009f);
            return Color.Lerp(color,new Color(.50f,.57f,.74f,1),seam*.78f);
        }
    }
}
