using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DesktopFish.Editor
{
    public static class FishSourceAudit
    {
        public static void Dump()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(
                AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Mzkagami" }).Single()));
            var report = new StringBuilder();
            foreach (var skin in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = skin.sharedMesh;
                var v = mesh.vertices;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    int[] ids = mesh.GetTriangles(s).Distinct().ToArray();
                    if (ids.Length == 0) continue;
                    var bounds = new Bounds(v[ids[0]], Vector3.zero);
                    foreach (int id in ids) bounds.Encapsulate(v[id]);
                    var material = skin.sharedMaterials[s];
                    report.AppendLine(s + "\t" + material.name + "\tmin " + bounds.min.ToString("F4") +
                        " max " + bounds.max.ToString("F4") + "\t" + AssetDatabase.GetAssetPath(material.mainTexture));
                }
            }
            Directory.CreateDirectory("work");
            File.WriteAllText("work/source-submeshes.tsv", report.ToString());
            var atlas = new Texture2D(2, 2);
            atlas.LoadImage(File.ReadAllBytes("Assets/Mzkagami/蓝色大肥鱼/蓝色大肥鱼.Textures/_16.png"));
            var pixels = atlas.GetPixels();
            int w = atlas.width, h = atlas.height;
            var seen = new bool[pixels.Length];
            var queue = new System.Collections.Generic.Queue<int>();
            int x0 = (int)(w * 0.45f), x1 = (int)(w * 0.66f), y0 = (int)(h * 0.10f), y1 = (int)(h * 0.37f);
            bool Blue(int i) { var c = pixels[i]; return c.b > c.r * 1.3f && c.b > c.g * 1.15f && c.r < 0.45f; }
            var blobs = new StringBuilder();
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
            {
                int start = y * w + x;
                if (seen[start] || !Blue(start)) continue;
                int minX = x, maxX = x, minY = y, maxY = y, count = 0;
                queue.Enqueue(start); seen[start] = true;
                while (queue.Count > 0)
                {
                    int i = queue.Dequeue(), xx = i % w, yy = i / w; count++;
                    minX = Mathf.Min(minX, xx); maxX = Mathf.Max(maxX, xx);
                    minY = Mathf.Min(minY, yy); maxY = Mathf.Max(maxY, yy);
                    foreach (int j in new[] { i - 1, i + 1, i - w, i + w })
                    {
                        int jx = j % w, jy = j / w;
                        if (jx < x0 || jx >= x1 || jy < y0 || jy >= y1 || seen[j] || !Blue(j)) continue;
                        seen[j] = true; queue.Enqueue(j);
                    }
                }
                if (count > 20) blobs.AppendLine(count + " UV (" + (minX / (float)w) + ", " + (minY / (float)h) + ") to (" +
                    (maxX / (float)w) + ", " + (maxY / (float)h) + ") boundary=" +
                    (minX == x0 || maxX == x1 - 1 || minY == y0 || maxY == y1 - 1));
            }
            File.WriteAllText("work/apron-ink-blobs.txt", blobs.ToString());
            Object.DestroyImmediate(atlas);
        }
    }
}
