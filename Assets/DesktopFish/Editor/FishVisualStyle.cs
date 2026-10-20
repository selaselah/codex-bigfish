using UnityEditor;
using UnityEngine;

namespace DesktopFish.Editor
{
    public static class FishVisualStyle
    {
        public static bool Enabled = true;
        const string Folder = "Assets/DesktopFish/Generated/VisualMaterials";

        public static Material Apply(Material source)
        {
            if (!Enabled) return source;
            string name = source.name;
            bool apron = name == "Illustrated Apron";
            bool headband = name.EndsWith("HAIR_03");
            bool tail = name.Contains("FoxTail");
            bool hair = (name.Contains("Hair") || name.Contains("HAIR")) && !headband;
            // The user prefers the original illustrated hair and gold decoration.
            // Keep their exact source materials rather than rebuilding these marks.
            bool goldSkirt = name.StartsWith("N00_002_03_Tops") && name.EndsWith("CLOTH_02");
            bool goldCuff = name.StartsWith("N00_010_01_Onepiece") && name.EndsWith("CLOTH_04");
            if (hair || goldSkirt || goldCuff) return source;
            bool cloth = name.Contains("CLOTH") && !name.Contains("FoxTail") && !name.Contains("CatEar");
            if (!apron && !hair && !cloth && !headband && !tail) return source;
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/DesktopFish/Generated", "VisualMaterials");
            var material = new Material(source) { name = name + " Mascot", shaderKeywords = new string[0] };
            if (apron) material.SetFloat("_MascotMode", 1);
            else
            {
                material.shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/DesktopFish/Shaders/MascotSurface.shader");
                material.SetFloat("_Kind", tail ? 3 : headband ? 2 : hair ? 0 : 1);
                material.SetFloat("_Glint", name.EndsWith("HAIR_01") ? 1 : name.EndsWith("HAIR_02") ? 2 : 0);
                material.SetFloat("_Decoration", name.StartsWith("N00_002_03_Tops") && name.EndsWith("CLOTH_02") ? 1 :
                    name.StartsWith("N00_010_01_Onepiece") && name.EndsWith("CLOTH_04") ? 2 : 0);
                material.SetFloat("_EdgeWidth", hair || tail ? 0.0007f : 0.0005f);
                material.SetFloat("_Cutoff", 0.5f);
                material.renderQueue = 2450;
            }
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            string path = Folder + "/" + guid + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing)
            {
                EditorUtility.CopySerialized(material, existing);
                Object.DestroyImmediate(material);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
