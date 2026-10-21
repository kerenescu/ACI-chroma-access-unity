using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChromaAccess.Editor
{
    // Owns only temporary image targets; the scene renderer is never changed.
    internal sealed class ImageSimulationRenderer : IDisposable
    {
        private readonly Material material;
        private readonly Dictionary<SimulationKind, RenderTexture> targets = new Dictionary<SimulationKind, RenderTexture>();
        private Texture2D source;
        private Texture2D encodedSource;
        private Hash128 sourceHash;

        internal ImageSimulationRenderer()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/ImageSimulation.shader");
            if (shader == null || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("The image simulation shader is unavailable or failed to compile.");
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        internal RenderTexture Render(Texture2D image, SimulationKind kind)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (QualitySettings.activeColorSpace != ColorSpace.Linear)
                throw new InvalidOperationException("Image simulation requires the project's Linear color space.");
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(image)) as TextureImporter;
            if (importer == null || !importer.sRGBTexture || importer.textureType != TextureImporterType.Default)
                throw new InvalidOperationException("Use a Default color texture with sRGB (Color Texture) enabled.");
            if (source != image || sourceHash != image.imageContentsHash)
            {
                ClearTargets();
                if (encodedSource != null) UnityEngine.Object.DestroyImmediate(encodedSource);
                encodedSource = LoadEncodedImage(image);
                source = image;
                sourceHash = image.imageContentsHash;
            }
            if (targets.TryGetValue(kind, out RenderTexture cached)) return cached;

            double[,] matrix = kind == SimulationKind.None ? null : SimulationModels.Matrix(kind);
            for (int row = 0; row < 3; row++)
            {
                Vector4 values = matrix == null
                    ? new Vector4(row == 0 ? 1 : 0, row == 1 ? 1 : 0, row == 2 ? 1 : 0, 0)
                    : new Vector4((float)matrix[row, 0], (float)matrix[row, 1], (float)matrix[row, 2], 0);
                material.SetVector("_Row" + row, values);
            }
            var target = new RenderTexture(image.width, image.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) {
                name = image.name + " - " + SimulationModels.Label(kind),
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                antiAliasing = 1
            };
            RenderTexture previous = RenderTexture.active;
            bool previousWrite = GL.sRGBWrite;
            try
            {
                if (!target.Create()) throw new InvalidOperationException("Could not allocate the image render target.");
                GL.sRGBWrite = true;
                Graphics.Blit(encodedSource, target, material, 0);
                targets.Add(kind, target);
                return target;
            }
            catch { target.Release(); UnityEngine.Object.DestroyImmediate(target); throw; }
            finally { RenderTexture.active = previous; GL.sRGBWrite = previousWrite; }
        }

        private static Texture2D LoadEncodedImage(Texture2D image)
        {
            string assetPath = AssetDatabase.GetAssetPath(image);
            string extension = Path.GetExtension(assetPath).ToLowerInvariant();
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                throw new InvalidOperationException("Choose an imported PNG or JPG color image.");
            string file = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            var encoded = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) {
                name = image.name + " encoded source", hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            try
            {
                if (!ImageConversion.LoadImage(encoded, File.ReadAllBytes(file), true))
                    throw new InvalidOperationException("Could not decode the source image.");
                if (encoded.width != image.width || encoded.height != image.height)
                    throw new InvalidOperationException("Disable NPOT resizing and raise the texture import size to preserve the source dimensions.");
                return encoded;
            }
            catch { UnityEngine.Object.DestroyImmediate(encoded); throw; }
        }

        internal void Export(Texture2D image, SimulationKind kind, string path)
        {
            RenderTexture target = Render(image, kind);
            RenderTexture previous = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                RenderTexture.active = target;
                pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false, false);
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
                pixels.Apply(false, false);
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            }
        }

        private void ClearTargets()
        {
            foreach (RenderTexture target in targets.Values)
            {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
            targets.Clear();
        }

        public void Dispose()
        {
            ClearTargets();
            if (encodedSource != null) UnityEngine.Object.DestroyImmediate(encodedSource);
            UnityEngine.Object.DestroyImmediate(material);
        }
    }

    internal sealed class ImageSampleImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Images/SimulationSamples/", StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
        }
    }
}
