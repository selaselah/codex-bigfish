using System;using System.IO;using UnityEngine;
namespace DesktopFish.Editor {
// Exact original artwork is reused at the user's explicit request. Only alpha
// separates the gold foreground; source RGBs/highlights are not repainted.
public static class FishOriginalSkirtPattern {
public const float OriginalScale=1.25f,PixelSize=.00045f;
static readonly Vector4[] Cells={new Vector4(379,1312,112,52),new Vector4(500,1312,112,52),new Vector4(813,1615,112,52),new Vector4(602,1624,112,52),new Vector4(641,85,41,36),new Vector4(691,85,41,36),new Vector4(122,435,41,36),new Vector4(371,434,41,36),new Vector4(741,85,26,26),new Vector4(110,122,24,24)};
static readonly Vector2Int[] ResourceOrigins={new Vector2Int(0,0),new Vector2Int(128,0),new Vector2Int(0,64),new Vector2Int(128,64),new Vector2Int(0,128),new Vector2Int(48,128),new Vector2Int(96,128),new Vector2Int(144,128),new Vector2Int(192,128),new Vector2Int(224,128)};
public static Vector2 UV(int cell,float x,float y) {var c=Cells[cell];return new Vector2((c.x+8+x)/1024f,(c.y+c.w-8-y)/2048f);}
public static void Paint(Color[] pixels) {
 var t=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!ImageConversion.LoadImage(t,File.ReadAllBytes("Assets/DesktopFish/Rebuilt/OriginalSkirtPattern.png"),false))throw new InvalidOperationException("147 original artwork load");var src=t.GetPixels32();
 for(int cell=0;cell<Cells.Length;cell++){var c=Cells[cell];var origin=ResourceOrigins[cell];for(int top=0;top<(int)c.w;top++)for(int x=0;x<(int)c.z;x++)pixels[((int)c.y+(int)c.w-1-top)*1024+(int)c.x+x]=src[(t.height-1-origin.y-top)*t.width+origin.x+x];}
 UnityEngine.Object.DestroyImmediate(t);
}
}}
