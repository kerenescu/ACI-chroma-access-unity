# Exploratory three-color palette

**Historical procedure:** the Unity validation scripts and Validate menu actions described below were removed in INT-013. Recorded results remain available. Use the Simulation Preview and Capture Palette Comparisons tools for the current demonstrator. Byte-identical capture filenames resolve through `docs/images/image-aliases.json`.

Date: 2026-10-19. CPU selection and four-case Unity verification passed. This is a fixed-palette experiment for the three controlled objects, not a general image recoloring algorithm.

## Question and scope

Can we increase the worst-separated pair under the existing deuteranopia simulation by changing two source colors while keeping the third fixed and limiting source relative-luminance changes?

The experiment aims at distinguishable categories. It does not recover trichromatic red/green/blue sensations. The original patch names identify their objects; they do not describe the experimental colors.

## Sources and equations

Use the [Machado (2009) source review](machado-2009-deuteranopia.md) for the verified severity-1.0 transform. Decode sRGB, apply the matrix, then clamp to [0, 1] using the existing SDR policy. The CPU search evaluates those continuous linear results without final 8-bit quantization; the Unity experiment separately measures captured 8-bit results.

Convert the resulting linear RGB to Oklab using **Bjorn Ottosson (2020), A perceptual color space for image processing**, [original definition and linear-sRGB code](https://bottosson.github.io/posts/oklab/). That transform is a 3 x 3 linear transform, componentwise cube roots, and another 3 x 3 transform. These Oklab intermediates are an analysis transform, distinct from the physiological simulation.

For coordinates `(L, a, b)`, use the [W3C deltaEOK definition](https://www.w3.org/TR/css-color-4/#color-difference-OK):

```text
d(x, y) = sqrt((Lx - Ly)^2 + (ax - ay)^2 + (bx - by)^2)
score(palette) = min(d(S(c0), S(c1)), d(S(c0), S(c2)), d(S(c1), S(c2)))
```

Here `S` includes the simulation and output clipping, and the distance includes conversion to Oklab. We maximize the minimum distance to target the weakest pair. This proxy combines lightness and chromatic differences; it is not a measured deuteranope discrimination metric or a clinical threshold. Do not interpret a doubled score as doubled usability.

Constrain source relative luminance using the [W3C formula](https://www.w3.org/TR/WCAG22/#dfn-relative-luminance):

```text
Y = 0.2126 * R_linear + 0.7152 * G_linear + 0.0722 * B_linear
abs(Y_candidate - Y_original) <= 0.01
```

The absolute tolerance 0.01 is a project experiment choice. This standard-observer source Y constraint does not guarantee equal perceived brightness for a particular person or after simulation. No WCAG contrast-ratio compliance is evaluated.

## Search definition and CPU outcome

For the first two objects, enumerate RGB channel values `0, 17, ..., 255`, and also include each object's original color. Keep candidates meeting its Y constraint. Hold the third color at `#4C4CC8`. Search all `172 x 82 = 14104` allowed pairs. Resolve score ties within `1e-12` by the smallest summed Oklab distance from the two original source colors. This defines an optimum over a finite set only; there is no random seed or continuous optimizer.

The selected source colors are:

| Object | Original | Experimental |
| --- | --- | --- |
| Left / PatchRed | `#C84C4C` | `#BB5500` |
| Center / PatchGreen | `#4CC84C` | `#EE88FF` |
| Right / PatchBlue | `#4C4CC8` | `#4C4CC8` |

| Pair | Original after simulation | Experimental after simulation |
| --- | --- | --- |
| 0-1 | 0.148905 | 0.297927 |
| 0-2 | 0.281418 | 0.317244 |
| 1-2 | 0.386848 | 0.297682 |
| Minimum | 0.148905 | 0.297682 |

These are continuous CPU Oklab distances. The green-to-pink change has source Oklab drift about 0.39232, so the visual-style cost is substantial. Pair 1-2 becomes less separated. Retain those tradeoffs alongside the increased minimum.

Reproduce with `python Tools/search_experimental_palette.py` from the project root. It uses the standard library and the existing CPU simulation module, and overwrites [the search report](../validation/palette-search.json) and `Assets/Settings/ExperimentalPalette.json`. It does not directly change scene materials. The setting is an output of this search; rerunning replaces any manual edits to it.

## Unity check and observed results

The Editor preview uses `MaterialPropertyBlock.SetColor` to override each controlled renderer temporarily, preserving existing property blocks. Shared materials and saved scene data are not changed. [Unity's documentation](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MaterialPropertyBlock.SetColor.html) states that SetColor accepts sRGB values and converts them for a Linear project, so no extra decode is applied here. Preview overrides are restored on assembly reload, scene close, Editor quit, leaving Edit mode, or closing the preview window.

Capture four states with the same camera: Original; Original + Deuteranopia; Experimental palette; Experimental + Deuteranopia. Check simulation against the independent CPU transform within the existing 1/255 tolerance, check that recoloring affects the expected patch pixels only, compare all three observed Oklab distances, and verify exact original restoration. Run the existing simulation/identity regressions while the experimental palette is active to verify isolation and restoration. Preserve unsaved scene edits and the user's prior effect state.

These checks passed in Unity 6000.3.11f1, PC/Linear/Direct3D12, RTX 4070 Laptop GPU, at 960 x 540. Full-frame original-palette simulation error was at most 1/255; the experimental-palette simulation matched the CPU expectation at every captured RGBA8 pixel. Palette application matched the expected patch-only change. Original restoration was exact. Regression isolation and restoration passed. The saved scene/materials/renderer settings were unchanged, and unsaved scene state remained false before and after the recorded run.

| Object | Experimental source | Observed after simulation |
| --- | --- | --- |
| 0 | `#BB5500` | `#8C7C00` |
| 1 | `#EE88FF` | `#94B3FC` |
| 2 | `#4C4CC8` | `#0057C6` |

| Pair | Original simulated capture | Experimental simulated capture |
| --- | --- | --- |
| 0-1 | 0.147982 | 0.297796 |
| 0-2 | 0.283336 | 0.319059 |
| 1-2 | 0.388643 | 0.297495 |
| Minimum | 0.147982 | 0.297495 |

The captured-color analysis differs slightly from the continuous CPU search because it uses the actual quantized rendering outputs. An independent Python calculation on all six captured sample pairs agreed with the C# Oklab analysis within `1e-12`. Source Y changes were `(-0.009083, -0.003614, 0)`. The 1-2 pair worsens and the center object's hue changes substantially. The proxy improvement establishes neither restored trichromatic perception nor a quantified usability gain.

Reproduce interactively from **Tools > ChromaAccess > Simulation Preview**, using its four mode buttons and **Validate palette experiment**. The same validation is available at **Tools > ChromaAccess > Validate Palette Experiment**. It overwrites [the execution report](../validation/palette-experiment.json) and raw captures. The labeled figure records the INT-009 evidence and should be regenerated if those captures are replaced.

Evidence: [four-case figure](../images/palette-comparison.png), [original](../images/deuteranopia-original.png), [original simulated](../images/deuteranopia-simulated.png), [experimental](../images/palette-recolored.png), and [experimental simulated](../images/palette-recolored-simulated.png).

## Interpretation and further study

This is not the Machado and Oliveira (2010) recoloring method and does not substitute for studying that method. It is a small object-specific palette baseline, with no content adaptation, temporal method, standalone UI, GPU benchmark, or observer study.

[Microsoft XAG 103](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/103) supports color configurability and additional information channels. This experiment does not implement redundant shapes/text cues or establish accessibility compliance. Further work should consider semantic color expectations, original appearance, background contrast, psychophysical limitations of the proxy, and task-based evaluation with players.
