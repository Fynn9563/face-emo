using UnityEngine;

namespace Suzuryg.FaceEmo.Detail.Drawing
{
    public static class DrawingUtility_Transparent
    {
        public static Texture2D GetRenderedTexture(int width, int height, Camera camera)
        {
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, mipChain: false);

            var renderTexture = RenderTexture.GetTemporary(texture.width, texture.height,
                format: RenderTextureFormat.ARGB32, readWrite: RenderTextureReadWrite.sRGB, depthBuffer: 24, antiAliasing: 8);
            try
            {
                renderTexture.wrapMode = TextureWrapMode.Clamp;
                renderTexture.filterMode = FilterMode.Bilinear;

                // Clear to transparent
                var activeRenderTextureCache = RenderTexture.active;
                RenderTexture.active = renderTexture;
                GL.Clear(true, true, Color.clear);
                RenderTexture.active = activeRenderTextureCache;

                RenderCameraTransparent(renderTexture, camera);
                CopyRenderTexture(renderTexture, texture);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(renderTexture);
            }

            texture.alphaIsTransparency = true;
            return texture;
        }

        private static void RenderCameraTransparent(RenderTexture renderTexture, Camera camera)
        {
            var targetTextureCache = camera.targetTexture;
            var aspectCache = camera.aspect;
            var clearFlagsCache = camera.clearFlags;
            var backgroundColorCache = camera.backgroundColor;
            try
            {
                camera.targetTexture = renderTexture;
                camera.aspect = (float) renderTexture.width / renderTexture.height;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.Render();
            }
            finally
            {
                camera.targetTexture = targetTextureCache;
                camera.aspect = aspectCache;
                camera.clearFlags = clearFlagsCache;
                camera.backgroundColor = backgroundColorCache;
            }
        }

        private static void CopyRenderTexture(RenderTexture source, Texture2D destination)
        {
            var activeRenderTextureCache = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                destination.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, recalculateMipMaps: false);
                destination.Apply();
            }
            finally
            {
                RenderTexture.active = activeRenderTextureCache;
            }
        }

        public static Texture2D PaddingWithTransparentPixels(Texture2D original, int outerWidth, int outerHeight)
        {
            // Create a new transparent texture
            Texture2D result = new Texture2D(outerWidth, outerHeight, TextureFormat.ARGB32, false);
            Color[] clearColorArray = new Color[outerWidth * outerHeight];
            for (int i = 0; i < clearColorArray.Length; i++) { clearColorArray[i] = new Color(0, 0, 0, 0); }
            result.SetPixels(clearColorArray);

            // Copy the original image into its top-center
            int offsetX = (outerWidth - original.width) / 2;
            int offsetY = outerHeight - original.height;
            for (int y = 0; y < original.height; y++)
            {
                for (int x = 0; x < original.width; x++)
                {
                    result.SetPixel(x + offsetX, y + offsetY, original.GetPixel(x, y));
                }
            }

            result.Apply();
            result.alphaIsTransparency = true;
            return result;
        }

        public static void ApplyGammaCorrectionGPU(Texture2D texture, float gamma)
        {
            Material gammaCorrectionMaterial = new Material(Shader.Find("Suzuryg/GammaCorrection"));
            gammaCorrectionMaterial.SetFloat("_Gamma", gamma);

            RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height);
            Graphics.Blit(texture, renderTexture, gammaCorrectionMaterial);

            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
        }
    }
}
