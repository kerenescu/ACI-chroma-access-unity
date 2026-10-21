using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChromaAccess.Editor
{
    [InitializeOnLoad]
    internal static class ExperimentalPalettePreview
    {
        [Serializable]
        private sealed class PaletteConfig { public string name; public string[] colors; }
        private sealed class Entry
        {
            public Renderer renderer;
            public MaterialPropertyBlock original;
        }
        private static readonly List<Entry> Entries = new List<Entry>();
        internal static bool IsEnabled => Entries.Count != 0;

        static ExperimentalPalettePreview()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Restore;
            EditorApplication.quitting += Restore;
            EditorSceneManager.sceneClosing += (scene, removingScene) => Restore();
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode) Restore();
            };
        }

        internal static Color[] ReadColors()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Settings/ExperimentalPalette.json");
            if (asset == null) throw new InvalidOperationException("Experimental palette settings are missing.");
            var config = JsonUtility.FromJson<PaletteConfig>(asset.text);
            if (config.colors == null || config.colors.Length != 3) throw new InvalidOperationException("Expected three palette colors.");
            var colors = new Color[3];
            for (int i = 0; i < 3; i++)
                if (!ColorUtility.TryParseHtmlString(config.colors[i], out colors[i]))
                    throw new InvalidOperationException("Invalid palette color.");
            return colors;
        }

        internal static Renderer[] FindPatches()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/ControlledColors.unity")
                throw new InvalidOperationException("Open ControlledColors for the palette experiment.");
            string[] names = { "PatchRed", "PatchGreen", "PatchBlue" };
            var patches = new Renderer[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject patch = Array.Find(scene.GetRootGameObjects(), root => root.name == names[i]);
                if (patch == null || (patches[i] = patch.GetComponent<Renderer>()) == null)
                    throw new InvalidOperationException("Missing patch: " + names[i]);
            }
            return patches;
        }

        internal static void SetEnabled(bool enabled)
        {
            if (!enabled) { Restore(); return; }
            if (IsEnabled) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use Edit mode for the palette preview.");
            Renderer[] patches = FindPatches();
            Color[] colors = ReadColors();
            try
            {
                for (int i = 0; i < patches.Length; i++)
                {
                    var original = new MaterialPropertyBlock();
                    patches[i].GetPropertyBlock(original);
                    Entries.Add(new Entry { renderer = patches[i], original = original });
                    var preview = new MaterialPropertyBlock();
                    patches[i].GetPropertyBlock(preview);
                    // SetColor accepts sRGB and converts to Linear for this project.
                    preview.SetColor("_BaseColor", colors[i]);
                    patches[i].SetPropertyBlock(preview);
                }
            }
            catch { Restore(); throw; }
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        internal static void Restore()
        {
            foreach (Entry entry in Entries)
                if (entry.renderer != null)
                    entry.renderer.SetPropertyBlock(entry.original.isEmpty ? null : entry.original);
            Entries.Clear();
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
    }
}
