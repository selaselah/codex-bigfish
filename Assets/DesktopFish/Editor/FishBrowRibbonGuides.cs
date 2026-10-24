using UnityEngine;
namespace DesktopFish.Editor {
// Independently authored, tapered marks constrained by sparse native pigment
// boundaries. The lower warm crease's outer continuation is an own design.
public static class FishBrowRibbonGuides {
static readonly Vector3[] Brow={new Vector3(.1245f,.7418f,0),new Vector3(.112f,.7469f,.00055f),new Vector3(.0966f,.7512f,.00124f),new Vector3(.0811f,.7533f,.00167f),new Vector3(.0657f,.7521f,.00132f),new Vector3(.053f,.7495f,.00018f)};
static readonly Vector3[] Crease={new Vector3(.124f,.7188f,0),new Vector3(.111f,.7191f,.00016f),new Vector3(.097f,.7193f,.00030f),new Vector3(.085f,.71875f,.00046f),new Vector3(.0734f,.71672f,.00084f),new Vector3(.0618f,.71262f,.00115f),new Vector3(.057f,.7098f,.00070f)};
public static Vector3 Band(float t,bool eyebrow) {
 var a=eyebrow?Brow:Crease;float x=Mathf.Lerp(a[0].x,a[a.Length-1].x,t);int i=0;while(i<a.Length-2&&x<a[i+1].x)i++;
 float u=(a[i].x-x)/(a[i].x-a[i+1].x);var p=a[i];var q=a[i+1];
 float m0=i==0?(q.y-p.y)/(q.x-p.x):(q.y-a[i-1].y)/(q.x-a[i-1].x);
 float m1=i==a.Length-2?(q.y-p.y)/(q.x-p.x):(a[i+2].y-p.y)/(a[i+2].x-p.x);
 float h=q.x-p.x,y=(2*u*u*u-3*u*u+1)*p.y+(u*u*u-2*u*u+u)*h*m0+(-2*u*u*u+3*u*u)*q.y+(u*u*u-u*u)*h*m1;
 return new Vector3(x,y,Mathf.Lerp(p.z,q.z,u));
}
}}
