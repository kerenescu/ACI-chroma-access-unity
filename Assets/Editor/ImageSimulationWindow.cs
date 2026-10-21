using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChromaAccess.Editor
{
    public sealed class ImageSimulationWindow : EditorWindow
    {
        private const string SampleFolder = "Assets/Images/SimulationSamples/";
        private static readonly string[] SampleFiles = { "bunny-illustration.png", "outdoor-photo.png", "abandoned-tent.png" };
        private static readonly string[] SampleNames = { "Custom texture", "Bunny illustration", "Outdoor photo", "Abandoned tent" };
        private static readonly string[] ModeNames = { "Original", "Deuteranopia", "Protanopia", "Tritanomaly (approximation)" };
        private static readonly string[] ViewNames = { "Selected mode", "Compare original", "All four views" };
        [SerializeField] private Texture2D image;
        [SerializeField] private int sampleIndex = 1;
        [SerializeField] private SimulationKind mode = SimulationKind.Deuteranopia;
        [SerializeField] private int view = 1;
        private ImageSimulationRenderer renderer;
        private string status;

        [MenuItem("Tools/ChromaAccess/Image Simulation")]
        public static void Open()
        {
            var window = GetWindow<ImageSimulationWindow>("ChromaAccess Images");
            window.minSize = new Vector2(780, 560);
            window.Show();
        }

        private void OnEnable()
        {
            minSize = new Vector2(780, 560);
            if (image == null && sampleIndex > 0) image = LoadSample(Mathf.Clamp(sampleIndex - 1, 0, SampleFiles.Length - 1));
        }

        private static Texture2D LoadSample(int index)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(SampleFolder + SampleFiles[index]);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Image simulation", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Machado et al. (2009) | Fixed severity 1.0");
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup("Example image", sampleIndex, SampleNames);
            if (EditorGUI.EndChangeCheck()) { sampleIndex = selected; image = selected > 0 ? LoadSample(selected - 1) : null; status = null; }
            EditorGUI.BeginChangeCheck();
            image = (Texture2D)EditorGUILayout.ObjectField("Image (PNG or JPG)", image, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck()) { sampleIndex = 0; status = null; }
            using (new EditorGUI.DisabledScope(view == 2))
                mode = (SimulationKind)EditorGUILayout.Popup("Simulation", (int)mode, ModeNames);
            view = GUILayout.Toolbar(view, ViewNames);
            EditorGUILayout.HelpBox("Simulation approximates color appearance. Tritanomaly is an approximation, not tritanopia.", MessageType.Info);

            using (new EditorGUI.DisabledScope(image == null))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Export selected PNG")) ExportSelected();
                if (GUILayout.Button("Export all four PNGs")) ExportCurrent();
            }
            EditorGUILayout.LabelField(image == null ? "Choose an example image or drag a color texture above."
                : image.width + " x " + image.height + " | Aspect ratio preserved | PNG export at imported resolution", EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.LabelField(status, EditorStyles.wordWrappedMiniLabel);
            Rect area = GUILayoutUtility.GetRect(1, 100000, 1, 100000, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (image == null || Event.current.type != EventType.Repaint) return;
            try
            {
                if (renderer == null) renderer = new ImageSimulationRenderer();
                if (view == 0) DrawPanel(area, mode);
                else if (view == 1)
                {
                    float width = (area.width - 8) / 2;
                    DrawPanel(new Rect(area.x, area.y, width, area.height), SimulationKind.None);
                    DrawPanel(new Rect(area.x + width + 8, area.y, width, area.height), mode);
                }
                else
                {
                    float width = (area.width - 8) / 2, height = (area.height - 8) / 2;
                    for (int i = 0; i < 4; i++)
                        DrawPanel(new Rect(area.x + (i % 2) * (width + 8), area.y + (i / 2) * (height + 8), width, height), (SimulationKind)i);
                }
            }
            catch (Exception exception) { EditorGUI.HelpBox(area, exception.Message, MessageType.Error); }
        }

        private void DrawPanel(Rect panel, SimulationKind kind)
        {
            EditorGUI.DrawRect(panel, new Color(.12f, .12f, .12f));
            GUI.Label(new Rect(panel.x + 8, panel.y + 4, panel.width - 16, 22), SimulationModels.Label(kind), EditorStyles.boldLabel);
            Rect imageRect = new Rect(panel.x + 8, panel.y + 28, Mathf.Max(1, panel.width - 16), Mathf.Max(1, panel.height - 36));
            EditorGUI.DrawPreviewTexture(imageRect, renderer.Render(image, kind), null, ScaleMode.ScaleToFit);
        }

        private void ExportSelected()
        {
            string path = EditorUtility.SaveFilePanel("Export image simulation", ProjectRoot,
                image.name + "-" + SimulationModels.Slug(mode), "png");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                if (renderer == null) renderer = new ImageSimulationRenderer();
                renderer.Export(image, mode, path);
                status = "Saved " + Path.GetFileName(path);
            }
            catch (Exception exception) { status = exception.Message; Debug.LogException(exception); }
        }

        private void ExportCurrent()
        {
            string parent = EditorUtility.SaveFolderPanel("Choose a parent folder for the four PNGs", ProjectRoot, "");
            if (string.IsNullOrEmpty(parent)) return;
            try
            {
                string folder = NewExportFolder(parent, image.name);
                using (var export = new ImageSimulationRenderer()) ExportImage(export, image, folder);
                status = "Saved four PNGs in " + folder;
                EditorUtility.RevealInFinder(folder);
            }
            catch (Exception exception) { status = exception.Message; Debug.LogException(exception); }
        }

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string NewExportFolder(string parent, string label)
        {
            string folder = Path.Combine(parent, label + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(folder);
            return folder;
        }

        private static void ExportImage(ImageSimulationRenderer export, Texture2D texture, string folder)
        {
            if (texture == null) throw new InvalidOperationException("An example image is missing.");
            for (int i = 0; i < 4; i++)
                export.Export(texture, (SimulationKind)i, Path.Combine(folder, texture.name + "-" + SimulationModels.Slug((SimulationKind)i) + ".png"));
        }

        // Also usable with -executeMethod for reproducible exports without opening a scene.
        [MenuItem("Tools/ChromaAccess/Export All Example Images")]
        public static void ExportAllExamples()
        {
            string folder = NewExportFolder(Path.Combine(ProjectRoot, "Exports", "ImageSimulations"), "examples");
            using (var export = new ImageSimulationRenderer())
                for (int i = 0; i < SampleFiles.Length; i++) ExportImage(export, LoadSample(i), folder);
            File.WriteAllText(Path.Combine(folder, "export-info.txt"),
                "Unity " + Application.unityVersion + "\n" + SystemInfo.graphicsDeviceType + "\n" + QualitySettings.activeColorSpace
                + "\nMachado 2009 severity 1.0; Tritanomaly approximation\n12 PNGs; original imported dimensions; RGBA32 sRGB output\n");
            Debug.Log("Image simulations exported to " + folder);
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(folder);
        }

        private void OnDisable()
        {
            renderer?.Dispose();
            renderer = null;
        }
    }
}
