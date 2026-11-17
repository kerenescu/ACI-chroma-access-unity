using UnityEngine;

namespace ChromaAccess.DungeonTrial
{
    /// <summary>
    /// Holds the renderers of one selectable vial.  It changes only the
    /// instance presentation; imported Kenney materials are never edited.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DungeonTrialTarget : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");
        private static readonly int PreviewModeId = Shader.PropertyToID("_PreviewMode");

        [SerializeField] private Renderer[] visualRenderers;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private GameObject[] cues;
        [SerializeField] private GameObject outline;

        private MaterialPropertyBlock propertyBlock;
        private Vector3 restingScale;
        private bool restingScaleCaptured;
        private DungeonTrialPreview previewMode;

        public void Configure(Renderer[] renderers)
        {
            visualRenderers = renderers;
            visualRoot = transform;
            CaptureRestingScale();
        }

        public void ConfigureCues(GameObject[] configuredCues)
        {
            cues = configuredCues;
            SetCue(0, false);
        }

        public void ConfigureOutline(GameObject configuredOutline)
        {
            outline = configuredOutline;
            SetOutline(false);
        }

        private void Awake()
        {
            if (visualRoot == null)
                visualRoot = transform;

            CaptureRestingScale();
        }

        public void SetColor(Color color)
        {
            if (visualRenderers == null)
                return;

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            foreach (Renderer visualRenderer in visualRenderers)
            {
                if (visualRenderer == null)
                    continue;

                int materialCount = Mathf.Max(1, visualRenderer.sharedMaterials.Length);
                for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
                {
                    visualRenderer.GetPropertyBlock(propertyBlock, materialIndex);
                    propertyBlock.SetColor(BaseColorId, color);
                    propertyBlock.SetColor(LegacyColorId, color);
                    propertyBlock.SetFloat(PreviewModeId, (float)previewMode);
                    visualRenderer.SetPropertyBlock(propertyBlock, materialIndex);
                }
            }
        }

        public void SetPreview(DungeonTrialPreview requestedPreview)
        {
            previewMode = requestedPreview;
            if (visualRenderers == null)
                return;

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            foreach (Renderer visualRenderer in visualRenderers)
            {
                if (visualRenderer == null)
                    continue;

                int materialCount = Mathf.Max(1, visualRenderer.sharedMaterials.Length);
                for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
                {
                    visualRenderer.GetPropertyBlock(propertyBlock, materialIndex);
                    propertyBlock.SetFloat(PreviewModeId, (float)previewMode);
                    visualRenderer.SetPropertyBlock(propertyBlock, materialIndex);
                }
            }
        }

        public void SetCue(int cueIndex, bool visible)
        {
            if (cues == null)
                return;

            for (int index = 0; index < cues.Length; index++)
            {
                if (cues[index] != null)
                    cues[index].SetActive(visible && index == cueIndex);
            }
        }

        public void SetOutline(bool visible)
        {
            if (outline != null)
                outline.SetActive(visible);
        }

        public void ShowSelectionFeedback(bool correct)
        {
            CaptureRestingScale();
            visualRoot.localScale = restingScale * (correct ? 1.15f : 0.88f);
        }

        public void ResetSelectionFeedback()
        {
            CaptureRestingScale();
            visualRoot.localScale = restingScale;
        }

        private void CaptureRestingScale()
        {
            if (restingScaleCaptured || visualRoot == null)
                return;

            restingScale = visualRoot.localScale;
            restingScaleCaptured = true;
        }
    }
}
