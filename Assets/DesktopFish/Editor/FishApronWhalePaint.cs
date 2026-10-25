using System;using System.IO;using UnityEngine;
namespace DesktopFish.Editor {
// The original whale ink silhouette, eye/belly openings, heart-shaped tail and
// four spray marks replace the coarse hand-built ellipse and detached triangles.
// This changes only RGB in the existing apron field; cloth alpha/UV stay intact.
public static class FishApronWhalePaint {
public static void Paint(Color[] pixels) {
 var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
 if(!ImageConversion.LoadImage(source,File.ReadAllBytes("Assets/DesktopFish/Rebuilt/OriginalApronWhale.png"),false))throw new InvalidOperationException("148 original whale image");source.wrapMode=TextureWrapMode.Clamp;
 const float left=-.0575f,right=.0575f,bottom=.295f,top=.382f;
 var tint=new Color(.35f,.54f,.75f,1);
 for(int y=1680;y<2048;y++)for(int x=16;x<496;x++) {
  float px=(x+.5f-16)/480*.212f-.106f,py=(y+.5f-1680)/352*.175f+.235f;
  Color cloth=FishApronSheetGuides.Paint(px,py);int index=y*1024+x;
  if(px>=left&&px<=right&&py>=bottom&&py<=top) {
   float u=(right-px)/(right-left),v=(py-bottom)/(top-bottom);
   // CPU texel-centre alignment matches native GPU sampling, as checked in132.
   Color ink=source.GetPixelBilinear(u-.5f/source.width,v-.5f/source.height);
   Color styled=Color.Lerp(ink,tint,.75f);cloth=Color.Lerp(cloth,styled,ink.a);
  }
  cloth.a=pixels[index].a;pixels[index]=cloth;
 }
 UnityEngine.Object.DestroyImmediate(source);
}
}
}
