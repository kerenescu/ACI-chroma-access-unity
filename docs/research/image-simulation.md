# Image simulation workspace

## Use

Open **Tools > ChromaAccess > Image Simulation**, or click **Open image simulations** in the existing scene preview. No scene selection or Play mode is required.

1. Choose **Bunny illustration**, **Outdoor photo**, or **Abandoned tent** from **Example image**.
2. Choose Original, Deuteranopia, Protanopia, or Tritanomaly (approximation) under **Simulation**.
3. Switch between **Selected mode**, **Compare original**, and **All four views**. The four-view layout fixes its panels to Original, Deuteranopia, Protanopia, and approximate Tritanomaly.
4. Use **Export selected PNG** for the chosen simulation, or **Export all four PNGs** for a new timestamped folder. These export images at their imported dimensions; resizing the window only changes the preview.

**Tools > ChromaAccess > Export All Example Images** exports all twelve combinations into a timestamped directory under `Exports/ImageSimulations`. Generated exports are ignored by Git. The same export can run using `-executeMethod ChromaAccess.Editor.ImageSimulationWindow.ExportAllExamples` with Unity batch mode and a graphics device.

Additional PNG or JPG images can be imported as Default sRGB color textures and dragged into the Image field. For unchanged dimensions/colors, use uncompressed import, no mipmaps, no NPOT scaling, and a suitable maximum texture size. The three supplied examples receive these settings automatically.

## Inputs and implementation

The [source manifest](image-sources.json) records supplied filenames, dimensions, and SHA-256 hashes. Original bytes are copied into `Assets/Images/SimulationSamples`:

| Example | Dimensions | Original supplied file |
| --- | --- | --- |
| Bunny illustration | 736 x 1472 | image(20261011-102546).png |
| Outdoor photo | 1049 x 1046 | codex-clipboard-fba58f21-59e4-424f-be51-c0f0f53320b9.png |
| Abandoned tent | 3840 x 2160 | tent_abandonat.png |

The Editor window uses a separate image shader and temporary render textures. Matrix coefficients come from the existing `SimulationModels` implementation; scene renderer features, scene objects, and source textures are not modified. The image renderer reuses cached outputs until the image contents change and releases its resources when the window closes.

The pipeline loads original PNG/JPG bytes into a temporary uncompressed non-sRGB texture, explicitly decodes the piecewise sRGB transfer function in the shader, applies the selected fixed severity-1.0 matrix, clips linear output to [0,1], and writes an eight-bit sRGB render target. The temporary texture stores encoded color values as raw data; the imported source asset remains a normal sRGB color texture. This avoids relying on hardware sRGB sampling precision near cancellation/clipping. A custom input whose import dimensions differ from its file dimensions is rejected with a corrective import message. Original uses an identity matrix. PNG readback retains alpha. Sample imports use a 4096 maximum dimension, no resizing for NPOT dimensions, no compression, no mipmaps, and no alpha color dilation. Image fitting retains the aspect ratio.

## Sources and limits

- [Machado et al. (2009), author tutorial and coefficient table](https://www.inf.ufrgs.br/~oliveira/pubs_files/CVD_Simulation/CVD_Simulation.html#Tutorial): the same three fixed matrix cases as the scene implementation. See the existing [mode study](simulation-modes.md) for the S-cone limitation.
- [Unity 6.3 Graphics.Blit](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Graphics.Blit.html): explicit source/destination targets, active target restoration, and sRGB write state.
- [Unity RenderTextureReadWrite](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RenderTextureReadWrite.html) and [TextureImporter.sRGBTexture](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TextureImporter-sRGBTexture.html): texture decode and render-target encode behavior in Linear color space.
- [Unity ImageConversion.LoadImage](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ImageConversion.LoadImage.html): loading encoded image data without gamma conversion into a texture whose sampling interpretation is chosen explicitly.
- The three input images were supplied by the user. External authorship/licensing was not established by this intervention.

This is a static SDR simulation workspace, not adaptive recoloring or a participant study. Severity remains fixed at 1.0, and the S-cone mode is approximate Tritanomaly, not Tritanopia. Images are interpreted as sRGB; this is not an ICC-managed or HDR image viewer. The preview runs in the Unity Editor and is not a runtime player UI.

## Further study

Compare the two illustrations and photograph to identify possible lost distinctions. Record candidate regions and semantic roles before attempting recoloring. Study gamut clipping, image color profiles, simulated versus observed discrimination, and whether texture/shape cues preserve useful information.

## Recorded verification

Unity 6000.3.11f1 compiled and exported all twelve combinations in an isolated project using Direct3D12 and Linear color space. Original RGBA pixel values matched each supplied PNG exactly. Nine simulated images matched an independent NumPy float64 calculation within 1/255 per RGB channel, with zero pixels outside that tolerance and no alpha mismatches. All source dimensions were preserved, including 3840 x 2160. This is an implementation comparison, not an observer result. The existing ControlledColors scene and renderer assets were unchanged.
