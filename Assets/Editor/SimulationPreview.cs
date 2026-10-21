using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ChromaAccess.Editor
{
    internal sealed class SimulationPreview : EditorWindow
    {
        internal const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        internal const string FeatureName = "ChromaAccess Simulation";

        [MenuItem("Tools/ChromaAccess/Simulation Preview")]
        private static void Open()
        {
            GetWindow<SimulationPreview>("ChromaAccess").minSize = new Vector2(395, 355);
        }

        internal static FullScreenPassRendererFeature FindFeature(string name)
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (data != null)
                foreach (var feature in data.rendererFeatures)
                    if (feature is FullScreenPassRendererFeature pass && pass.name == name)
                        return pass;
            throw new InvalidOperationException("Missing renderer feature: " + name);
        }

        internal static SimulationKind CurrentMode
        {
            get
            {
                var feature = FindFeature(FeatureName);
                if (!feature.isActive) return SimulationKind.None;
                foreach (SimulationKind kind in new[] { SimulationKind.Deuteranopia, SimulationKind.Protanopia, SimulationKind.Tritanomaly })
                    if (feature.passMaterial == SimulationModels.LoadMaterial(kind)) return kind;
                throw new InvalidOperationException("The simulation pass uses an unknown material.");
            }
        }

        internal static void SetMode(SimulationKind kind)
        {
            var effect = FindFeature(FeatureName);
            if (kind != SimulationKind.None) effect.passMaterial = SimulationModels.LoadMaterial(kind);
            FindFeature("ChromaAccess Identity").SetActive(kind == SimulationKind.None);
            effect.SetActive(kind != SimulationKind.None);
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Color vision simulation", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Machado et al. (2009), fixed severity 1.0");
            try
            {
                SimulationKind mode = CurrentMode;
                EditorGUILayout.LabelField("Current mode", SimulationModels.Label(mode));
                EditorGUILayout.LabelField("Current palette", ExperimentalPalettePreview.IsEnabled ? "Experimental" : "Original");
                EditorGUILayout.Space();
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (GUILayout.Button("Original")) SetPreview(false, SimulationKind.None);
                    if (GUILayout.Button("Original + Deuteranopia")) SetPreview(false, SimulationKind.Deuteranopia);
                    if (GUILayout.Button("Original + Protanopia")) SetPreview(false, SimulationKind.Protanopia);
                    if (GUILayout.Button("Original + Tritanomaly (approximation)")) SetPreview(false, SimulationKind.Tritanomaly);
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Palette experiment for deuteranopia", EditorStyles.boldLabel);
                    if (GUILayout.Button("Experimental palette")) SetPreview(true, SimulationKind.None);
                    if (GUILayout.Button("Experimental + Deuteranopia")) SetPreview(true, SimulationKind.Deuteranopia);
                }
                EditorGUILayout.HelpBox("Inspect the Game view in Edit mode. Tritanomaly is an approximation, not tritanopia. The palette experiment targets deuteranopia only.", MessageType.Info);
            }
            catch (Exception exception) { EditorGUILayout.HelpBox(exception.Message, MessageType.Error); }
        }

        internal static void SetPreview(bool recolored, SimulationKind kind)
        {
            ExperimentalPalettePreview.SetEnabled(recolored);
            SetMode(kind);
        }

        private void OnDisable() { ExperimentalPalettePreview.Restore(); }
    }
}
