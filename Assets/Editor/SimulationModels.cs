using System;
using UnityEditor;
using UnityEngine;

namespace ChromaAccess.Editor
{
    public enum SimulationKind { None, Deuteranopia, Protanopia, Tritanomaly }

    internal static class SimulationModels
    {
        // Author tutorial, Table 1, severity 1.0. Column-vector linear RGB.
        private static readonly double[,] Protanopia = {
            { 0.152286, 1.052583, -0.204868 },
            { 0.114503, 0.786281, 0.099216 },
            { -0.003882, -0.048116, 1.051998 }
        };
        private static readonly double[,] Deuteranopia = {
            { 0.367322, 0.860646, -0.227968 },
            { 0.280085, 0.672501, 0.047413 },
            { -0.011820, 0.042940, 0.968881 }
        };
        private static readonly double[,] Tritanomaly = {
            { 1.255528, -0.076749, -0.178779 },
            { -0.078411, 0.930809, 0.147602 },
            { 0.004733, 0.691367, 0.303900 }
        };
        internal static double[,] Matrix(SimulationKind kind)
        {
            switch (kind)
            {
                case SimulationKind.Deuteranopia: return Deuteranopia;
                case SimulationKind.Protanopia: return Protanopia;
                case SimulationKind.Tritanomaly: return Tritanomaly;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        internal static string Slug(SimulationKind kind) => kind.ToString().ToLowerInvariant();
        internal static string Label(SimulationKind kind) => kind == SimulationKind.None ? "Original"
            : kind == SimulationKind.Tritanomaly ? "Tritanomaly (approximation)" : kind.ToString();

        internal static Material LoadMaterial(SimulationKind kind)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + kind + ".mat");
            if (material == null || material.shader == null || material.shader.name != "ChromaAccess/" + kind)
                throw new InvalidOperationException("Missing or incorrect simulation material: " + kind);
            return material;
        }
    }
}
