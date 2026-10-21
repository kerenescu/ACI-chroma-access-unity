using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ChromaAccess.Editor
{
    public static class PaletteLabCapture
    {
        private const int Width = 1440, Height = 540;
        private const string Folder = "docs/images/palette-lab-20261021";
        [Serializable] private sealed class Palette { public string id; public string name; public string[] colors; }
        [Serializable] private sealed class Config { public Palette[] palettes; }
        [Serializable] public sealed class Frame
        {
            public string paletteId;
            public string mode;
            public bool cues;
            public string path;
            public string[] sampledRgb;
        }
        [Serializable] public sealed class Report
        {
            public bool success;
            public string message;
            public string timestampUtc;
            public string unityVersion;
            public string graphicsApi;
            public string gpu;
            public string colorSpace;
            public int width = Width, height = Height;
            public float cameraOrthographicSize = 2;
            public bool isolatedPreviewScene = true;
            public bool originalStateRestored;
            public bool sourceSceneDirtyBefore, sourceSceneDirtyAfter;
            public List<Frame> frames = new List<Frame>();
        }

        [MenuItem("Tools/ChromaAccess/Capture Palette Comparisons")]
        public static void FromMenu()
        {
            Report report = Run();
            if (report.success) Debug.Log("Palette lab captured " + report.frames.Count + " frames. See docs/validation/palette-lab-captures.json");
            else Debug.LogError(report.message);
        }

        public static Report Run()
        {
            Scene sourceScene = SceneManager.GetActiveScene();
            var report = new Report {
                timestampUtc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(), gpu = SystemInfo.graphicsDeviceName,
                colorSpace = QualitySettings.activeColorSpace.ToString(), sourceSceneDirtyBefore = sourceScene.isDirty
            };
            var effect = SimulationPreview.FindFeature(SimulationPreview.FeatureName);
            var identity = SimulationPreview.FindFeature("ChromaAccess Identity");
            Material savedMaterial = effect.passMaterial;
            bool savedEffect = effect.isActive, savedIdentity = identity.isActive;
            Scene preview = default;
            var temporary = new List<UnityEngine.Object>();
            try
            {
                if (sourceScene.path != "Assets/Scenes/ControlledColors.unity" || EditorApplication.isPlayingOrWillChangePlaymode
                    || QualitySettings.activeColorSpace != ColorSpace.Linear)
                    throw new InvalidOperationException("Use ControlledColors in Edit mode with PC/Linear rendering.");
                var config = JsonUtility.FromJson<Config>(File.ReadAllText("Assets/Settings/PaletteLab.json"));
                if (config.palettes == null || config.palettes.Length != 9) throw new InvalidOperationException("Expected nine palette cases.");
                Renderer[] sourcePatches = ExperimentalPalettePreview.FindPatches();
                Camera sourceCamera = Camera.main;
                if (sourceCamera == null) throw new InvalidOperationException("Main Camera is missing.");
                preview = EditorSceneManager.NewPreviewScene();
                GameObject cameraObject = UnityEngine.Object.Instantiate(sourceCamera.gameObject);
                SceneManager.MoveGameObjectToScene(cameraObject, preview);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.scene = preview;
                camera.orthographicSize = 2;
                camera.aspect = (float)Width / Height;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                var patches = new Renderer[3];
                for (int i = 0; i < 3; i++)
                {
                    GameObject clone = UnityEngine.Object.Instantiate(sourcePatches[i].gameObject);
                    SceneManager.MoveGameObjectToScene(clone, preview);
                    patches[i] = clone.GetComponent<Renderer>();
                }
                var cueRoot = new GameObject("Redundant shape cues");
                SceneManager.MoveGameObjectToScene(cueRoot, preview);
                var white = new Material(sourcePatches[0].sharedMaterial) { hideFlags = HideFlags.HideAndDontSave };
                white.SetColor("_BaseColor", Color.white);
                white.SetFloat("_Cull", 0);
                temporary.Add(white);
                for (int i = 0; i < 3; i++)
                {
                    var cue = new GameObject("Cue " + i, typeof(MeshFilter), typeof(MeshRenderer));
                    SceneManager.MoveGameObjectToScene(cue, preview);
                    cue.transform.SetParent(cueRoot.transform, false);
                    cue.transform.position = patches[i].transform.position + new Vector3(0, 0, -0.02f);
                    Mesh mesh = CueMesh(i);
                    temporary.Add(mesh);
                    cue.GetComponent<MeshFilter>().sharedMesh = mesh;
                    cue.GetComponent<MeshRenderer>().sharedMaterial = white;
                }
                Directory.CreateDirectory(Folder);
                foreach (Palette palette in config.palettes)
                {
                    if (palette.colors == null || palette.colors.Length != 3) throw new InvalidOperationException("Invalid palette.");
                    for (int i = 0; i < 3; i++)
                    {
                        if (!ColorUtility.TryParseHtmlString(palette.colors[i], out Color color)) throw new InvalidOperationException("Invalid color.");
                        var block = new MaterialPropertyBlock();
                        block.SetColor("_BaseColor", color);
                        patches[i].SetPropertyBlock(block);
                    }
                    foreach (bool cues in new[] { false, true })
                    {
                        if (cues && palette.id != "baseline" && palette.id != "matched-y" && palette.id != "balanced") continue;
                        cueRoot.SetActive(cues);
                        foreach (SimulationKind kind in new[] { SimulationKind.None, SimulationKind.Deuteranopia, SimulationKind.Protanopia, SimulationKind.Tritanomaly })
                        {
                            SimulationPreview.SetMode(kind);
                            if (kind != SimulationKind.None && ShaderUtil.ShaderHasError(effect.passMaterial.shader)) throw new InvalidOperationException("Simulation shader error.");
                            Texture2D image = Capture(camera);
                            try
                            {
                                string file = Folder + "/" + palette.id + (cues ? "-cues" : "") + "-" + kind.ToString().ToLowerInvariant() + ".png";
                                File.WriteAllBytes(file, image.EncodeToPNG());
                                var samples = new string[3];
                                for (int i = 0; i < 3; i++)
                                {
                                    Vector3 p = camera.WorldToViewportPoint(patches[i].transform.position + new Vector3(-.7f, .7f, 0));
                                    Color32 pixel = image.GetPixel((int)(p.x * Width), (int)(p.y * Height));
                                    samples[i] = ColorUtility.ToHtmlStringRGBA(pixel);
                                    if (kind == SimulationKind.None && samples[i] != palette.colors[i].TrimStart('#').ToUpperInvariant() + "FF")
                                        throw new InvalidOperationException("Original palette sample mismatch: " + palette.id + " object " + i + " = " + samples[i]);
                                }
                                report.frames.Add(new Frame { paletteId = palette.id, mode = kind.ToString(), cues = cues, path = file, sampledRgb = samples });
                            }
                            finally { UnityEngine.Object.DestroyImmediate(image); }
                        }
                    }
                }
                report.success = report.frames.Count == 48;
                report.message = report.success ? "Captured nine palettes in four modes, plus three shape-cue variants in four modes." : "Unexpected capture count.";
            }
            catch (Exception exception) { report.success = false; report.message = exception.ToString(); }
            finally
            {
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
                foreach (var item in temporary) if (item != null) UnityEngine.Object.DestroyImmediate(item);
                effect.passMaterial = savedMaterial;
                effect.SetActive(savedEffect);
                identity.SetActive(savedIdentity);
                report.sourceSceneDirtyAfter = sourceScene.isDirty;
                report.originalStateRestored = effect.passMaterial == savedMaterial && effect.isActive == savedEffect && identity.isActive == savedIdentity
                    && report.sourceSceneDirtyAfter == report.sourceSceneDirtyBefore;
                report.success &= report.originalStateRestored;
                Directory.CreateDirectory("docs/validation");
                File.WriteAllText("docs/validation/palette-lab-captures.json", JsonUtility.ToJson(report, true));
            }
            return report;
        }

        private static Mesh CueMesh(int kind)
        {
            int segments = kind == 0 ? 4 : kind == 1 ? 48 : 3;
            float phase = kind == 0 ? Mathf.PI / 4 : Mathf.PI / 2;
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float angle = phase + i * 2 * Mathf.PI / segments;
                vertices[i * 2] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * .58f;
                vertices[i * 2 + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * .40f;
                int next = (i + 1) % segments;
                int k = i * 6;
                triangles[k] = i * 2; triangles[k + 1] = next * 2; triangles[k + 2] = i * 2 + 1;
                triangles[k + 3] = next * 2; triangles[k + 4] = next * 2 + 1; triangles[k + 5] = i * 2 + 1;
            }
            var mesh = new Mesh { name = "Opaque redundant cue " + kind, hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = vertices; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static Texture2D Capture(Camera camera)
        {
            RenderTexture target = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request)) throw new InvalidOperationException("URP render request unavailable.");
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                image = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
                return image;
            }
            catch { if (image != null) UnityEngine.Object.DestroyImmediate(image); throw; }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); }
        }

        public static void RunBatch()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use the capture menu in an interactive session.");
            EditorSceneManager.OpenScene("Assets/Scenes/ControlledColors.unity", OpenSceneMode.Single);
            QualitySettings.SetQualityLevel(Array.IndexOf(QualitySettings.names, "PC"), true);
            EditorApplication.delayCall += () => {
                try { Report result = Run(); Debug.Log("Palette lab: " + result.message); EditorApplication.Exit(result.success ? 0 : 1); }
                catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
            };
        }
    }
}
