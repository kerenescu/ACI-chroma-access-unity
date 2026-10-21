# Sprint 1 review: problem, literature, and simulation

Prepared on 2026-10-19. This review supports the [progress presentation](../presentations/ChromaAccess_Sprint1.pptx) and [presenter guide](../presentations/sprint-1-presenter-guide.md). The [evidence snapshot](../presentations/sprint-1-evidence.json) freezes the reports and capture hashes used in these materials. Later validator runs may replace the live reports.

## Problem and research question

Color can encode distinctions between gameplay elements. A color vision deficiency can make some distinctions harder to use. Microsoft’s [Xbox Accessibility Guideline 103](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/103) recommends additional channels for important cues and configurable color choices. This motivates investigating recoloring while retaining the need for other cues and evaluation with players.

**Working research question, proposed for discussion:** To what extent can real-time recoloring improve the distinguishability of gameplay-relevant elements under protanopia and deuteranopia simulations, while limiting changes to the image, temporal fluctuations, and GPU cost?

This is the broader project question. An improvement in a simulation-based numerical metric would answer only part of it. Benefits for players require appropriate tasks and participant evidence.

**Sprint 1 engineering question:** Does the first Unity deuteranopia implementation reproduce a source-derived linear RGB CPU reference within a predeclared error of one 8-bit code value per RGB channel, while preserving alpha and restoring the original scene state?

**Sprint 1 scope:** one static controlled scene, one published simulation endpoint, matched original/simulation captures, initial repeatable tests, and a focused comparison of relevant literature. The optional fixed-palette experiment illustrates an optimization tradeoff. Protanopia, a published recoloring implementation, semantic masks, temporal adaptation, GPU timing, and participant testing remain later work.

## Distinct operations and evidence

| Operation | Purpose | Current evidence |
| --- | --- | --- |
| Simulation | Approximate the display appearance associated with a specified color vision deficiency | Machado 2009, deuteranopia severity 1.0, CPU/GPU agreement |
| Recoloring | Change source colors to improve an explicit objective under constraints | An original finite-grid experiment for three known objects, with one color fixed |
| Evaluation | Measure implementation correctness, numerical tradeoffs, and eventually task performance | Full-frame numerical checks now; no participant study or GPU timing |

The simulation does not restore missing cone responses. A recolored display cannot be claimed to make a person with deuteranopia experience the original trichromatic red, green, and blue. The practical goal is useful differentiation under explicit constraints.

## Focused literature comparison

### Brettel, Viénot, and Mollon, 1997

*Computerized simulation of color appearance for dichromats*, JOSA A 14(10), 2647–2655. [DOI](https://doi.org/10.1364/JOSAA.14.002647), [original author-hosted paper](https://vision.psychol.cam.ac.uk/jdmollon/papers/Dichromat_simulation.pdf).

The method represents display colors in cone-response LMS coordinates and projects along the missing cone-response axis onto one of two half-planes. The half-planes share the neutral axis and use invariant-hue anchors. The method depends on display primary calibration and linear light values. Converting projected responses back to display RGB raises gamut questions.

The paper includes observations from one protanope and one deuteranope. Its discussion also describes limits in inferring subjective color appearance and variation between observers. These observations do not establish universal observer accuracy.

**Relevance:** an established alternative simulation model and a possible future cross-model comparison. **Project status:** studied, not implemented. Sprint 1 does not benchmark it against Machado or rank their accuracy.

### Machado, Oliveira, and Fernandes, 2009

*A Physiologically-based Model for Simulation of Color Vision Deficiency*, IEEE TVCG 15(6), 1291–1298. [DOI](https://doi.org/10.1109/TVCG.2009.113), [corrected original paper](https://www.inf.ufrgs.br/~oliveira/pubs_files/CVD_Simulation/Machado_Oliveira_Fernandes_CVD_Vis2009_final.pdf), [author tutorial and precomputed coefficients](https://www.inf.ufrgs.br/~oliveira/pubs_files/CVD_Simulation/CVD_Simulation.html#Tutorial).

The model combines cone responses and opponent processing. Its RGB formulation composes the inverse normal transform with the transform for the selected deficiency. The authors provide precomputed matrices for deficiency types and severity levels. The project selects only the deuteranopia endpoint at severity 1.0. It does not implement continuous severity selection.

**Selection rationale:** the published coefficients make the first case traceable, and the linear RGB matrix maps directly to the existing full-screen pass. This is an implementation choice, not evidence that this model is universally more accurate or that project performance has been measured. The author’s erratum corrects equations 17 and 18 in the paper; the author states that the supplementary matrices were already correct.

For column-vector linear RGB input, the selected matrix is:

```text
M = [  0.367322   0.860646  -0.227968 ]
    [  0.280085   0.672501   0.047413 ]
    [ -0.011820   0.042940   0.968881 ]

c_sim_linear = clamp(M * c_linear, 0, 1)
```

The CPU reference decodes sRGB, applies the matrix, clips RGB to the display range, re-encodes sRGB, and compares 8-bit output. Unity’s verified Linear rendering path provides linear values at the pass and performs the output conversion. The shader preserves alpha. Clipping can alter the ideal transform, as the blue patch’s negative red output demonstrates. See the [source walkthrough](machado-2009-deuteranopia.md) for the transfer functions and independent reference.

**Project status:** implemented and verified for this endpoint and controlled SDR pipeline. The paper’s model validation and this project’s CPU/GPU implementation check answer different questions.

### Machado and Oliveira, 2010

*Real-Time Temporal-Coherent Color Contrast Enhancement for Dichromats*, Computer Graphics Forum 29(3), 933–942. [DOI](https://doi.org/10.1111/j.1467-8659.2009.01701.x), [publisher record](https://diglib.eg.org/items/e649eef3-7f82-4338-a92d-b9f5fb7fd9bc).

**Source access:** the journal record and abstract were checked. The detailed algorithm and limitations summarized here were studied in Chapter 5, printed pages 55–65, of [Gustavo Mello Machado’s 2010 UFRGS master’s thesis](https://www.inf.ufrgs.br/~oliveira/students_dissertations/Masters/Gustavo_Machado_Masters_thesis_UFRGS_2010.pdf), *A Model for Simulation of Color Vision Deficiency and A Color Contrast Enhancement Technique for Dichromats*. This review does not claim a full reading of the separate journal PDF.

The described method estimates the dominant direction of contrast lost under dichromacy in the Lab chromatic plane. It forms weighted vectors from sampled pixel pairs and obtains a principal direction from a 2-by-2 matrix. It projects colors onto a plane containing that direction and the L* axis, then rotates that plane toward the dichromat gamut approximation. The construction preserves L* and achromatic colors.

For video, the method reuses pixel-pair locations and resolves the principal vector’s sign using the previous frame. A sign reversal can otherwise cause large apparent changes between adjacent frames. This is a specific temporal mechanism, not a general guarantee of stability in all scene transitions.

Chapter 5.2.1 describes cases where the selected direction can reduce already available contrast. The method also does not guarantee preservation of semantic colors. A reduced gamut prevents recovery of every lost distinction. These points motivate later tests of all relevant pairs, source-color changes, and temporal behavior.

**Project status:** candidate for a later published recoloring baseline, not implemented. The current finite-palette search does not reproduce this algorithm.

### Comparison and current decision

| Source | Main operation | Useful property | Assumption or limitation | Sprint 1 use |
| --- | --- | --- | --- | --- |
| Brettel et al. 1997 | Simulation in LMS | Explicit dichromat projection geometry | Calibration, gamut, observer variability | Literature comparison |
| Machado et al. 2009 | Simulation through a linear RGB matrix | Published coefficients for a traceable first case | Approximate model and display clipping | Implemented deuteranopia endpoint |
| Machado and Oliveira 2010, studied via thesis Ch. 5 | Contrast enhancement | Image-dependent direction and temporal sign handling | Semantic hues and all pair contrasts are not guaranteed | Candidate for later implementation |

The sources support selecting a first reproducible simulation and studying a separate recoloring method. They do not support treating recoloring as an inverse that reconstructs trichromatic perception.

## Unity configuration and controlled scene

The verified setup uses Unity 6000.3.11f1, URP 17.3.0, PC quality, Linear color space, Direct3D12, and an NVIDIA GeForce RTX 4070 Laptop GPU. The static ControlledColors scene uses an orthographic camera, a solid #303030 background, disabled camera post-processing, and three unlit quads. Their original sRGB colors are #C84C4C, #4CC84C, and #4C4CC8. These are debugging patches, not validated perceptual confusion pairs.

The simulation runs in a Full Screen Pass after rendering post-processing, with color fetch enabled. The tests use a 960-by-540 sRGB ARGB32 target, 24-bit depth, and one sample. This evidence concerns SDR output and does not establish HDR behavior or runtime/build support. Official version-specific references: [Unity Linear color space](https://docs.unity.com/en-us/engine/6000.3/manual/materials-and-shaders/graphics-color/color-spaces/linear-color-space/space), [URP Full Screen Pass](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/renderer-features/renderer-feature-full-screen-pass.html).

## Initial protocol and observed results

The [test protocol](../testing/sprint-1-protocol.md) defines the controls, pass criteria, captures, and restoration steps. The presentation uses the 2026-10-19 reports, around 16:28:37 UTC, preserved in the evidence snapshot. Preparing these materials did not rerun Unity or modify scene/shader assets.

| Test | Pass criterion | Observed result |
| --- | --- | --- |
| Repeated original baseline | Exact RGBA8 match | 0 differing pixels |
| Identity and asymmetric identity | Exact RGBA8 match | 0 differing pixels |
| Pass execution control | Expected magenta output over the complete target | 518400 magenta pixels |
| Scene simulation versus CPU | RGB error at most 1/255, exact alpha | Maximum 1/255, 0 pixels outside tolerance |
| Neutral probes | Same tolerance and alpha rule | Maximum 0/255 |
| Gamut and asymmetric probes | Same tolerance and alpha rule | Maximum 1/255 |
| Original restoration | Exact RGBA8 match and state restoration | 0 differing pixels, restoration flags passed |

All simulated comparisons reported zero alpha mismatches. The float input probe also confirmed linear values at the injection point, but it used a separate float target and is not an HDR validation. The source-derived Python CPU reference and its probe checks provide an independent calculation of the selected matrix, transfer functions, and clipping. Passing these checks supports correctness of the implementation for the tested conditions, not perceptual validity for every observer.

| Patch | Original sRGB | Expected CPU output | Captured Unity output |
| --- | --- | --- | --- |
| First | #C84C4C | #8B7F49 | #8B8049 |
| Second | #4CC84C | #BDAC56 | #BDAC56 |
| Third | #4C4CC8 | #0057C6 | #0057C6 |

Raw images: [original](../images/deuteranopia-original.png), [simulation](../images/deuteranopia-simulated.png). Live reports: [simulation](../validation/deuteranopia-pass.json), [identity](../validation/identity-pass.json), [independent CPU reference](../validation/deuteranopia-reference.json).

## Optional palette experiment and its tradeoffs

The original project experiment searches a finite 17-step sRGB grid for the first two objects and keeps the third fixed. The source relative-luminance constraint is an absolute change of at most 0.01 per changed object. It maximizes the minimum pairwise Oklab distance after simulation and clipping, with source drift as a tie-breaker. These choices define a limited optimization problem, not a published general recoloring method. See the [full study note](experimental-palette.md).

The selected source palette is #BB5500, #EE88FF, #4C4CC8. On captured simulated RGB values, the minimum Oklab distance increases from 0.147982 to 0.297495. Pair distances in object order 0–1, 0–2, 1–2 are:

| Pair | Original palette under simulation | Experimental palette under simulation |
| --- | --- | --- |
| 0–1 | 0.147982 | 0.297796 |
| 0–2 | 0.283336 | 0.319059 |
| 1–2 | 0.388643 | 0.297495 |

One pair becomes less separated. The source green becomes pink, with an Oklab source drift of approximately 0.392320. Near-preservation of standard relative luminance does not imply the same perceived brightness for an observer with deuteranopia. Oklab was designed around normal color vision, so distances computed on simulated display RGB are an exploratory proxy, not a calibrated discrimination threshold. There is no measured task benefit and no claim of WCAG conformance.

The search’s continuous CPU scores differ from these rounded capture-based values. Do not mix the two numerical comparisons. [Search report](../validation/palette-search.json), [captured experiment report](../validation/palette-experiment.json). Metric sources: [Ottosson’s Oklab definition](https://bottosson.github.io/posts/oklab/), [W3C deltaEOK](https://www.w3.org/TR/css-color-4/#color-difference-OK), [W3C relative luminance](https://www.w3.org/TR/WCAG22/#dfn-relative-luminance).

## Sprint 1 handoff and next increment

The original/simulation comparison and initial repeatable protocol are available. This review and the presentation supply the literature and progress materials. The research question remains a working formulation for feedback. Rehearsal, presentation delivery, and supervisor feedback remain pending.

The next small increment should settle the recoloring baseline and its acceptance criteria before further implementation. Study the 2010 method’s contrast-loss direction, temporal sign rule, and failure example. Decide which semantic color changes are acceptable. Later experiments can add protanopia, the reference recoloring method, known-object masks, temporal sequences, and GPU timings in separate verified steps. Participant evaluation requires its own study design.
