using UnityEngine;
namespace DesktopFish.Editor {
    // A smooth low-density crown fitted to sparse alpha-visible radial landmarks.
    // Only control coordinates are retained; all topology and paint are authored.
    public static class FishRearCrownGuides {
        // Registered radial controls from the local alpha-visible review.
        // Repeated pole observations are represented by their sampling weight.
        static readonly Vector4[] Landmarks = {
            new Vector4(0.2450041f,0.7700000f,-0.0932008f,1f),
            new Vector4(0.2364201f,0.7700000f,-0.1864973f,1f),
            new Vector4(0.1742933f,0.7700000f,-0.2577147f,1f),
            new Vector4(0.0933728f,0.7700000f,-0.3065395f,1f),
            new Vector4(-0.0000000f,0.7700000f,-0.3145823f,1f),
            new Vector4(-0.0937056f,0.7700000f,-0.3074541f,1f),
            new Vector4(-0.1737020f,0.7700000f,-0.2570101f,1f),
            new Vector4(-0.2350642f,0.7700000f,-0.1857144f,1f),
            new Vector4(-0.2488493f,0.7700000f,-0.0938788f,1f),
            new Vector4(0.2312040f,0.8329066f,-0.0907675f,1f),
            new Vector4(0.2164426f,0.8369676f,-0.1749632f,1f),
            new Vector4(0.1627831f,0.8378569f,-0.2439974f,1f),
            new Vector4(0.0878992f,0.8388630f,-0.2915010f,1f),
            new Vector4(-0.0000000f,0.8354291f,-0.2941845f,1f),
            new Vector4(-0.0867901f,0.8379941f,-0.2884538f,1f),
            new Vector4(-0.1617065f,0.8374082f,-0.2427143f,1f),
            new Vector4(-0.2182182f,0.8375169f,-0.1759883f,1f),
            new Vector4(-0.2325976f,0.8332858f,-0.0910132f,1f),
            new Vector4(-0.2170619f,0.8290588f,-0.0117261f,1f),
            new Vector4(0.1892621f,0.8809562f,-0.0166280f,1f),
            new Vector4(0.2027818f,0.8888822f,-0.0857559f,1f),
            new Vector4(0.1859466f,0.8939644f,-0.1573563f,1f),
            new Vector4(0.1410595f,0.8966993f,-0.2181081f,1f),
            new Vector4(0.0775261f,0.9008686f,-0.2630012f,1f),
            new Vector4(-0.0000000f,0.8915785f,-0.2605802f,1f),
            new Vector4(-0.0752339f,0.8969992f,-0.2567033f,1f),
            new Vector4(-0.1405365f,0.8962296f,-0.2174849f,1f),
            new Vector4(-0.1868605f,0.8945736f,-0.1578839f,1f),
            new Vector4(-0.2031353f,0.8890894f,-0.0858182f,1f),
            new Vector4(-0.1930839f,0.8831967f,-0.0159541f,1f),
            new Vector4(0.1597019f,0.9321656f,-0.0218402f,1f),
            new Vector4(0.1649326f,0.9374770f,-0.0790821f,1f),
            new Vector4(0.1472814f,0.9400659f,-0.1350330f,1f),
            new Vector4(0.1110701f,0.9427945f,-0.1823682f,1f),
            new Vector4(0.0607799f,0.9477084f,-0.2169913f,1f),
            new Vector4(-0.0000000f,0.9422673f,-0.2222673f,1f),
            new Vector4(-0.0596183f,0.9443121f,-0.2137998f,1f),
            new Vector4(-0.1109299f,0.9425763f,-0.1822011f,1f),
            new Vector4(-0.1475514f,0.9403777f,-0.1351888f,1f),
            new Vector4(-0.1634881f,0.9360101f,-0.0788273f,1f),
            new Vector4(-0.1601007f,0.9325705f,-0.0217699f,1f),
            new Vector4(0.1117706f,0.9665789f,-0.0302918f,1f),
            new Vector4(0.1148174f,0.9719375f,-0.0702454f,1f),
            new Vector4(0.1008986f,0.9717973f,-0.1082539f,1f),
            new Vector4(0.0735979f,0.9683164f,-0.1377105f,1f),
            new Vector4(0.0405497f,0.9753510f,-0.1614095f,1f),
            new Vector4(-0.0000000f,0.9686358f,-0.1646824f,1f),
            new Vector4(-0.0397012f,0.9710537f,-0.1590780f,1f),
            new Vector4(-0.0739685f,0.9693150f,-0.1381522f,1f),
            new Vector4(-0.1010396f,0.9720791f,-0.1083352f,1f),
            new Vector4(-0.1144281f,0.9712528f,-0.0701767f,1f),
            new Vector4(-0.1112733f,0.9657043f,-0.0303795f,1f),
            new Vector4(0.0562398f,0.9831275f,-0.0400834f,1f),
            new Vector4(0.0566591f,0.9847165f,-0.0599905f,1f),
            new Vector4(0.0488347f,0.9804484f,-0.0781947f,1f),
            new Vector4(0.0370693f,0.9852260f,-0.0941775f,1f),
            new Vector4(0.0196028f,0.9839014f,-0.1038582f,1f),
            new Vector4(-0.0000000f,0.9785397f,-0.1058781f,1f),
            new Vector4(-0.0198417f,0.9865080f,-0.1045145f,1f),
            new Vector4(-0.0372695f,0.9863883f,-0.0944161f,1f),
            new Vector4(-0.0487041f,0.9798855f,-0.0781193f,1f),
            new Vector4(-0.0565496f,0.9843016f,-0.0599712f,1f),
            new Vector4(-0.0561826f,0.9829111f,-0.0400935f,1f),
            new Vector4(-0.0000000f,0.9824750f,-0.0500000f,11f),
        };
        static readonly double[] Coefficients=FitDome();
        static double[] FitDome() {
            var matrix=new double[4,5];
            foreach(var p in Landmarks) {
                double x=p.x,y=p.y-.77,z=p.z+.05;
                var row=new[]{x*x,y*y,z*z,2*y*z};
                for(int i=0;i<4;i++) {
                    for(int j=0;j<4;j++)matrix[i,j]+=p.w*row[i]*row[j];
                    matrix[i,4]+=p.w*row[i];
                }
            }
            for(int i=0;i<4;i++) {
                int pivot=i;for(int k=i+1;k<4;k++)if(System.Math.Abs(matrix[k,i])>System.Math.Abs(matrix[pivot,i]))pivot=k;
                for(int j=0;j<5;j++){double tmp=matrix[i,j];matrix[i,j]=matrix[pivot,j];matrix[pivot,j]=tmp;}
                double scale=matrix[i,i];if(System.Math.Abs(scale)<1e-12)throw new System.InvalidOperationException("Singular crown landmark fit");
                for(int j=0;j<5;j++)matrix[i,j]/=scale;
                for(int k=0;k<4;k++)if(k!=i) {
                    double factor=matrix[k,i];for(int j=0;j<5;j++)matrix[k,j]-=factor*matrix[i,j];
                }
            }
            var q=new[]{matrix[0,4],matrix[1,4],matrix[2,4],matrix[3,4]};
            if(q[0]<=0||q[1]<=0||q[2]<=0||q[1]*q[2]<=q[3]*q[3])throw new System.InvalidOperationException("Crown fit must remain a closed convex dome");
            return q;
        }
        static void Angles(Vector3 p,out float elevation,out float angle) {
            Vector3 delta=p-new Vector3(0,.77f,-.05f);
            elevation=Mathf.Asin(Mathf.Clamp(delta.normalized.y,-1,1))*Mathf.Rad2Deg;
            angle=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;if(angle<0)angle+=360;
        }
        public static Vector3 Point(Vector3 p) {
            float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.66f,.76f,p.y));if(weight<=0)return p;
            float elevation,angle;Angles(p,out elevation,out angle);
            Vector3 target=Surface(elevation,angle),outward=(target-new Vector3(0,.77f,-.05f)).normalized;
            return Vector3.Lerp(p,target-outward*.0015f+(p-target)*.04f,weight);
        }
        public static Vector3 Normal(Vector3 p) {
            float elevation,angle;Angles(p,out elevation,out angle);
            if(elevation>89)return new Vector3(0,(float)Coefficients[1],(float)Coefficients[3]).normalized;
            elevation=Mathf.Clamp(elevation,-10,89.5f);
            Vector3 along=Surface(elevation+.05f,angle)-Surface(elevation-.05f,angle);
            Vector3 across=Surface(elevation,angle+.5f)-Surface(elevation,angle-.5f);
            return Vector3.Cross(across.normalized,along.normalized).normalized;
        }
        public static Vector3 Surface(float elevation,float angle) {
            float phi=elevation*Mathf.Deg2Rad,theta=angle*Mathf.Deg2Rad;
            Vector3 ray=new Vector3(Mathf.Sin(theta)*Mathf.Cos(phi),Mathf.Sin(phi),Mathf.Cos(theta)*Mathf.Cos(phi));
            // A positive-definite dome fitted to the upper sparse guides avoids
            // reproducing individual overlapping-strip ridges in the backing cap.
            float radius=1/Mathf.Sqrt((float)(Coefficients[0]*ray.x*ray.x+Coefficients[1]*ray.y*ray.y+Coefficients[2]*ray.z*ray.z+2*Coefficients[3]*ray.y*ray.z));
            if(elevation<0) {
                float horizontal=1/Mathf.Sqrt((float)(Coefficients[0]*Mathf.Sin(theta)*Mathf.Sin(theta)+Coefficients[2]*Mathf.Cos(theta)*Mathf.Cos(theta)));
                radius=horizontal*(1-.10f*phi)/Mathf.Cos(phi);
            }
            if(elevation>=89.999f)return new Vector3(0,.77f+radius,-.05f);
            return new Vector3(0,.77f,-.05f)+ray*radius;
        }
    }
}
