# Three simulation modes

**Historical procedure:** the Unity validation scripts and Validate menu actions described below were removed in INT-013. Recorded results remain available. Use the Simulation Preview and Capture Palette Comparisons tools for the current demonstrator. Byte-identical capture filenames resolve through `docs/images/image-aliases.json`.

Extension prepared on 2026-10-21. All modes use Machado, Oliveira, and Fernandes (2009), fixed severity 1.0, linear RGB input, RGB clipping to [0,1], and unchanged alpha. This extension adds simulation cases, not a new recoloring method.

## Source and naming

- **Protanopia:** the published Protanomaly column at severity 1.0 supplies the endpoint used for the model's protanopic approximation. The affected cone response is L.
- **Deuteranopia:** the existing endpoint remains available and uses the Deuteranomaly column. The affected cone response is M.
- **Tritanomaly (approximation):** the Tritanomaly column at severity 1.0 approximates an altered S-cone response. It does **not** model the absence of the S-cone contribution and must not be called tritanopia.

The [corrected original paper](https://www.inf.ufrgs.br/~oliveira/pubs_files/CVD_Simulation/Machado_Oliveira_Fernandes_CVD_Vis2009_final.pdf), sections 4.2.1 and 4.3, explicitly excludes tritanopia and describes tritanomaly as a shift-based approximation. [DOI](https://doi.org/10.1109/TVCG.2009.113). Coefficients come from the [author's tutorial, Table 1, severity 1.0](https://www.inf.ufrgs.br/~oliveira/pubs_files/CVD_Simulation/CVD_Simulation.html#Tutorial).

The author page timed out during this extension. The coefficients were extracted and checked from the primary HTML downloaded on 2026-10-19. The [coefficient record](machado-2009-matrices.json) records its SHA-256, column names, review date, and all matrix entries. No third-party matrix implementation supplies the reference.

For a column vector c of linear RGB values, each output channel is the dot product of one row of M with c. The CPU reference decodes sRGB before multiplication and re-encodes after clipping. The verified Unity Linear path performs the corresponding input/output handling around the shader. [W3C transfer functions](https://www.w3.org/TR/css-color-4/#color-conversion-code).

```text
Protanopia                 Tritanomaly (approximation)
 0.152286  1.052583 -0.204868   1.255528 -0.076749 -0.178779
 0.114503  0.786281  0.099216  -0.078411  0.930809  0.147602
-0.003882 -0.048116  1.051998   0.004733  0.691367  0.303900
```

Clipping can change the ideal transformed color. An endpoint matrix is not a calibrated account of every observer's color experience. These patches remain debugging colors, not validated confusion pairs.

## Preview

Open ControlledColors in Unity 6000.3.11f1, select PC quality, stay in Edit mode, and inspect the Game view. Open **Tools > ChromaAccess > Simulation Preview**. Click **Original**, **Original + Deuteranopia**, **Original + Protanopia**, or **Original + Tritanomaly (approximation)**. No keyboard shortcuts or Play mode are required.

One **ChromaAccess Simulation** Full Screen Pass changes its material to the chosen simulation. Only one simulation runs at once. Original uses the identity pass. The saved renderer defaults to Original and keeps the deuteranopia material assigned to its inactive simulation slot. The two added shaders use the same URP injection and color-fetch settings as the existing effect. [Unity Full Screen Pass reference](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/renderer-features/renderer-feature-full-screen-pass.html).

The experimental palette controls remain specific to deuteranopia. The added source-palette simulation buttons reset that palette to the original colors. The palette's distance gains must not be generalized to the added modes.

## Repeatable checks

Use **Validate selected simulation**, the individual **Validate Protanopia / Validate Tritanomaly / Validate Deuteranopia** menu actions, or **Validate all simulations**. The suite uses the existing [initial protocol](../testing/sprint-1-protocol.md):

1. Repeat an original full-frame capture and require an exact match.
2. Run the identity, asymmetric identity, and full-frame execution controls.
3. Compare each simulated pixel with a double-precision CPU reference. Require RGB error at most 1/255 and identical alpha.
4. Repeat with black/gray/white, saturated RGB, and a shifted patch to cover neutral, gamut, and orientation behavior.
5. Restore the original materials, patch position, simulation material, activation flags, and prior palette preview.
6. Run each validator while a different simulation and the experimental palette are active. Require the prior rendered image to return exactly.
7. Run the existing deuteranopia palette regression while tritanomaly is active. Require the prior tritanomaly view to return exactly.
8. Switch through Original and all three simulation choices and require distinct captures and exclusive activation.

The independent Python tool computes nine reference probes per mode. With `--check-captures`, it also recalculates every pixel of all twelve scene/neutral/gamut/asymmetric image pairs using NumPy float64 and verifies the C# report samples.

## Evidence

Current results are recorded in [protanopia report](../validation/protanopia-pass.json), [tritanomaly report](../validation/tritanomaly-pass.json), [deuteranopia report](../validation/deuteranopia-pass.json), [mode/restoration suite](../validation/simulation-suite.json), and [independent pixel checks](../validation/simulation-independent-check.json). Raw captures use the matching mode prefix under `docs/images`.

The Sprint 1 presentation remains a snapshot of 2026-10-19. These additions do not silently revise its embedded images or numerical results.

## Recorded run on 2026-10-21

The graphics-enabled Unity batch run completed with exit code 0. The suite started at 15:35:27 UTC. Rendering conditions were Unity 6000.3.11f1, URP 17.3.0, PC/Linear, Direct3D12, NVIDIA GeForce RTX 4070 Laptop GPU, and 960 x 540 sRGB ARGB32 output.

| Mode | Scene max RGB error | Neutral max | Gamut max | Asymmetric max | Outside tolerance / alpha mismatches |
| --- | --- | --- | --- | --- | --- |
| Protanopia | 0/255 | 0/255 | 0/255 | 0/255 | 0 / 0 |
| Deuteranopia | 1/255 | 0/255 | 1/255 | 1/255 | 0 / 0 |
| Tritanomaly approximation | 0/255 | 0/255 | 0/255 | 0/255 | 0 / 0 |

The 1/255 criterion remains unchanged. Exact agreement for these added cases does not establish exact agreement on every possible image or rendering configuration.

| Original source | Protanopia capture | Tritanomaly capture |
| --- | --- | --- |
| #C84C4C | #6C654B | #DB334D |
| #4CC84C | #CCB63E | #33C3AE |
| #4C4CC8 | #0062CC | #006B83 |

All 27 report samples and twelve full-frame image pairs passed an independent Python check. Cross-mode validation and the palette regression restored the prior rendered images with zero differing pixels. All four preview choices produced distinct images and correct activation states. No unsaved scene changes existed before or after validation. The added captures were visually inspected. The saved scene, original material files, Packages, and ProjectSettings matched their pre-change hashes.

## Limitations and next study

These checks concern the tested PC/Linear SDR path and opaque controlled patches. They provide no participant outcomes, HDR validation, clinical calibration, general recoloring result, or GPU timing. Tritanomaly remains an explicit model approximation. A dedicated tritanopia simulation would need a suitable different model and separate source/calibration review. Study differences between missing and altered cone responses, gamut handling, and cross-model comparisons before extending the simulation family further.
