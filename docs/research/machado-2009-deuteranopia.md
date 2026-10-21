# Machado (2009): first deuteranopia reference

**Historical procedure:** the Unity validation scripts and Validate menu actions described below were removed in INT-013. Recorded results remain available. Use the Simulation Preview and Capture Palette Comparisons tools for the current demonstrator. Byte-identical capture filenames resolve through `docs/images/image-aliases.json`.

Source review: 2026-10-19. Scope: one fixed simulation case and an independent CPU reference. Unity integration was subsequently verified in README INT-008.

## Sources and the supplied PDF

**Gustavo M. Machado, Manuel M. Oliveira, and Leandro A. F. Fernandes (2009).** *A Physiologically-based Model for Simulation of Color Vision Deficiency*. IEEE Transactions on Visualization and Computer Graphics, 15(6), 1291-1298. [DOI](https://doi.org/10.1109/TVCG.2009.113). [Corrected full paper](https://www.inf.ufrgs.br/~oliveira/pubs_files/CVD_Simulation/Machado_Oliveira_Fernandes_CVD_Vis2009_final.pdf). [Authors' tutorial and Table 1](https://www.inf.ufrgs.br/~oliveira/pubs_files/CVD_Simulation/CVD_Simulation.html#Tutorial).

The six-page PDF supplied for this project is **Machado and Oliveira (2011), A Model for Simulation of Color Vision Deficiency and A Color Contrast Enhancement Technique for Dichromats**, CTD 24, pp. 10-15 ([publisher record](https://sol.sbc.org.br/index.php/ctd/article/view/29161)). Its Eq. (2), on PDF page 4, summarizes the simulation transform. It is useful background covering both simulation and recoloring; it is a different publication from the original 2009 simulation paper and contains no numerical RGB simulation matrix table.

The original paper's Eq. (11), on PDF page 4 / journal page 1294, gives:

```text
simulatedRGB = inverse(Gamma_normal) * Gamma_CVD * inputRGB
            = M_CVD * inputRGB
```

`Gamma_CVD` maps the input RGB color into the model's opponent-color coordinates for the selected deficiency. `inverse(Gamma_normal)` maps those coordinates back to RGB for the normal observer viewing the simulation. The product is precomputed, so each pixel needs one 3 x 3 multiplication. This is a simulation transform; no recoloring objective is optimized here.

The authors' page includes an erratum for Eqs. (17) and (18) and states that the supplementary matrices were computed using the correct equations. Use the corrected paper for derivations.

## Selected coefficients and their meaning

Use **Table 1, Deuteranomaly column, severity 1.0** from the authors' tutorial. The tutorial defines 0.0 as no deficiency and 1.0 as the dichromacy endpoint; this endpoint in the selected column represents deuteranopia. This first reference has one fixed severity, without interpolation.

```text
                    input R    input G    input B
output R            0.367322   0.860646  -0.227968
output G            0.280085   0.672501   0.047413
output B           -0.011820   0.042940   0.968881
```

Multiply this matrix by an RGB **column** vector. Each row computes one output channel. For example, `R_out = 0.367322 * R + 0.860646 * G - 0.227968 * B`. Keep the published six-decimal values, including their signs and small row-sum deviations. Deuteranopia concerns the M-cone photopigment in the model; it does not mean setting the image's green RGB channel to zero.

Apply the matrix to **linear RGB**. The paper derives linear transforms from additive primary spectra in Eqs. (8)-(11). The [colorspace package's official documentation](https://search.r-project.org/CRAN/refmans/colorspace/html/simulate_cvd.html) independently discusses this requirement and its correction of an older gamma-encoded implementation. It is a software cross-reference; the coefficients above were checked directly against the original authors' table.

## Worked example: the red patch

For the sRGB color `#C84C4C`, the 8-bit channels are `(200, 76, 76)`. Normalize by 255, then use the [W3C sRGB transfer functions](https://www.w3.org/TR/css-color-4/#color-conversion-code). For the nonnegative SDR values used here:

```text
decode(s) = s / 12.92                         if s <= 0.04045
            ((s + 0.055) / 1.055)^2.4         otherwise

encode(l) = 12.92 * l                         if l <= 0.0031308
            1.055 * l^(1 / 2.4) - 0.055      otherwise
```

| Step | R | G | B |
| --- | --- | --- | --- |
| Normalized sRGB input | 0.784313725 | 0.298039216 | 0.298039216 |
| Decoded linear input | 0.577580440 | 0.072271851 | 0.072271851 |
| Matrix output, linear | 0.257882812 | 0.213801135 | 0.066299175 |
| Encoded sRGB output | 0.544807362 | 0.499740613 | 0.285576109 |
| Rounded 8-bit output | 139 | 127 | 73 |

Result: **`#C84C4C -> #8B7F49`**. The display values are rounded for explanation; the calculation retains floating-point precision until the final quantization.

For this SDR reference, the output policy is to clamp linear channels to `[0, 1]` before encoding, then round the encoded values to the nearest 8-bit integer, with half values rounded upward. No clipping is needed for this red patch. Clipping is a project output policy, not an additional physiological part of the model. Other input colors can produce negative or greater-than-one components.

## Reproduction and verification

Run `python Tools/calculate_deuteranopia_reference.py` from the project root, using Python 3. It uses the standard library and overwrites [the numerical report](../validation/deuteranopia-reference.json). The [script](../../Tools/calculate_deuteranopia_reference.py) now includes nine scene/neutral/gamut probes. It does not access Unity or alter assets.

The matrix constants were compared against an independently extracted copy of the nine entries in the authors' HTML table. The reviewed HTML SHA-256 is `379c549025f91ac05a611631114ff8202fa2d802bc29e15f79479c7985373346`. This fingerprint identifies the reviewed page bytes; it is not a permanent hash of all future versions of that URL.

The CPU calculation also checks exact black preservation, neutral inputs at linear levels 0, 0.5, and 1 within `1.1e-6`, and encode/decode round trips for all 256 possible 8-bit channel values. Row sums are `(1.000000, 0.999999, 1.000001)`, explaining the small neutral deviation caused by the published coefficient precision. These checks passed; their numeric errors are recorded in the report.

## Integration boundary and next study

The Unity renderer now has separate identity and deuteranopia effects. **Tools > ChromaAccess > Simulation Preview** exposes Original / Original + Deuteranopia, plus separate experimental-palette modes. **Validate simulation** (also **Tools > ChromaAccess > Validate Deuteranopia**) runs the [test protocol](../testing/sprint-1-protocol.md), including the identity regression, and overwrites [the GPU report](../validation/deuteranopia-pass.json). It isolates and restores any active experimental palette.

The CPU expectation remains `#8B7F49`; the captured Unity red center was `#8B8049`. The one-code-value difference is within the protocol's predeclared 1/255 tolerance. Green and blue patch centers matched their CPU expectations exactly. Full-frame comparisons, neutrals, saturated gamut probes, and the asymmetric scene passed; state restoration and opaque alpha checks passed. This validates the recorded SDR implementation conditions, not every rendering configuration.

A separate floating-point diagnostic measured a linear input close to `(0.577580512, 0.072271861, 0.072271861)` at the effect's injection point. The shader applies the matrix directly to sampled RGB and returns the original alpha, with output RGB clamped to [0, 1]. No explicit sRGB transfer conversion is used in the shader. The diagnostic used a linear floating-point target; the primary comparison used an sRGB 8-bit target. The precise rendering stage responsible for the small SDR discrepancy was not isolated.

This calculation establishes a reproducible implementation target for one model case. It does not establish a perceptual match for every observer, accessibility improvements, HDR behavior, temporal behavior, or GPU cost. Further study: the paper's normalization constraints in Eq. (9), the distinction between cone responses and RGB channels, source-buffer precision at the selected URP injection point, and out-of-gamut handling.
