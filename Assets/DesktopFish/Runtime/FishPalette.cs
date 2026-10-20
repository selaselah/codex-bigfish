using UnityEngine;

namespace DesktopFish
{
    // Render-only palette adjustment; the model's source textures stay intact.
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class FishPalette : MonoBehaviour
    {
        public Shader paletteShader;
        Material material;

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!paletteShader || !paletteShader.isSupported)
            {
                Graphics.Blit(source, destination);
                return;
            }
            if (!material) material = new Material(paletteShader) { hideFlags = HideFlags.HideAndDontSave };
            Graphics.Blit(source, destination, material);
        }

        void OnDisable()
        {
            if (!material) return;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
    }
}
