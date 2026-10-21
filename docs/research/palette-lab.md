# Comparative palette experiments

Prepared on 2026-10-21. The [editable presentation](../presentations/ChromaAccess_Palette_Experiments.pptx) contains 18 slides, source-bearing speaker notes, and ten comparative image layouts. The [comparative PNG folder](../images/palette-comparisons/) contains ten labeled layouts. Original captures and data remain in the repository; the duplicate ZIP bundle was removed in INT-013. The [local comparator](../presentations/ChromaAccess_Palette_Compare.html) lets the reader switch palettes, simulated views, and the available shape-cue cases.

## Questions explored

Which tradeoffs appear when we select colors manually, start from a curated palette, optimize for one simulated deficiency, or optimize across several views? How much original appearance must change? Can an additional shape identify an object when two source colors become close under simulation?

These experiments screen candidates for the next task tests. They do not establish a universal palette or a measured benefit for players. The unfiltered view is included to inspect the appearance offered to people with typical trichromatic vision.

## Ordinary procedures and deployed examples

Start by defining the information each color conveys. Assign colors to meaningful roles such as ally, opponent, or objective. Inspect the actual background and object size. Use a documented categorical palette as a starting point, then inspect the chosen subset in context. [Okabe and Ito's Color Universal Design guidance](https://jfly.uni-koeln.de/color/) recommends redundant coding and a curated starting set. Our subset uses orange #E69F00, reddish purple #CC79A7, and blue #0072B2; it does not represent a guarantee for every arrangement or observer.

Inspect lightness and foreground/background contrast in addition to hue. Standard relative luminance Y is useful for an explicit numerical constraint, but it does not guarantee perceived brightness for a particular observer with a deficiency. Provide an additional identifier such as a shape, pattern, icon, or direct text label. [W3C Use of Color](https://www.w3.org/WAI/WCAG22/Understanding/use-of-color.html) explains that information should have a visible route in addition to color.

[Microsoft XAG 103](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/103) describes configurable colors and additional cues. Its examples include Forza Horizon 4 presets and individual UI color choices in Call of Duty. It treats simulator views as internal development checks before player testing. Offering a preset plus individual role-color controls gives players more choice than relying on a diagnostic label alone. Audio or haptics can add another sensory route for critical information.

## Simulation is distinct from adaptation

The current simulation modes approximate a model for inspection. A future player-facing preset would change source colors assigned to object roles, with the simulation switched off during ordinary play. Turning on a deuteranopia simulation does not constitute an accessibility correction for a person with deuteranopia.

All three simulation cases use [Machado, Oliveira, and Fernandes (2009)](https://doi.org/10.1109/TVCG.2009.113), fixed severity 1.0. The original paper excludes tritanopia, so the S-cone case remains **Tritanomaly (approximation)**. The [coefficient record](machado-2009-matrices.json) preserves the source and matrix entries.

## Nine palettes

| Case | Source colors, objects A / B / C |
| --- | --- |
| Original debugging palette | #C84C4C / #4CC84C / #4C4CC8 |
| Matched-Y challenge | #C84C4C / #4C824C / #4C4CC8 |
| Okabe-Ito subset | #E69F00 / #CC79A7 / #0072B2 |
| Lightness-separated custom | #284B63 / #F4D35E / #B83B5E |
| Deutan search | #BB5500 / #EE88FF / #4C4CC8 |
| Protan search | #887711 / #55BBEE / #4C4CC8 |
| Tritanomaly search | #C84C4C / #00BBFF / #4C4CC8 |
| Balanced search | #CC4411 / #55BBEE / #4C4CC8 |
| Balanced with limited drift | #BB5500 / #88BBAA / #4C4CC8 |

The Matched-Y challenge changes the green channel to make the first two source colors nearly equal in standard relative Y. It is a constructed numerical challenge, not a perceptually matched confusion pair. The custom lightness palette is a manual comparison. The curated and manual references may change all three object colors.

## Reproducible search objective

For each selected view, transform source RGB to linear RGB, apply the published matrix where applicable, clip to the display range, and convert the result to Oklab. Compute all three pairwise Euclidean distances and maximize the minimum across the chosen views. The unfiltered view participates in the two balanced searches.

The grid uses channel values 0, 17, ..., 255 and includes the original colors. Searches modify A and B, hold object C at #4C4CC8, and constrain each changed object's source relative Y to within 0.01 of its original. Score ties within 1e-12 minimize summed source Oklab drift. The bounded variant additionally limits each drift to 0.15. These are project design choices, not observer discrimination thresholds.

| Search | Views in objective | Candidate counts A / B | Palettes evaluated |
| --- | --- | --- | --- |
| Deutan search | Deuteranopia | [172, 82] | 14104 |
| Protan search | Protanopia | [172, 82] | 14104 |
| Tritanomaly search | Tritanomaly | [172, 82] | 14104 |
| Balanced search | None, Deuteranopia, Protanopia, Tritanomaly | [172, 82] | 14104 |
| Balanced with limited drift | None, Deuteranopia, Protanopia, Tritanomaly | [62, 40] | 2480 |

The implementation finds a maximum only within these finite candidates and constraints. It is the project's original grid experiment, not an implementation of Machado and Oliveira's 2010 image recoloring method. Metric sources: [Ottosson's Oklab](https://bottosson.github.io/posts/oklab/), [W3C deltaEOK](https://www.w3.org/TR/css-color-4/#color-difference-OK), and [W3C relative luminance](https://www.w3.org/TR/WCAG22/#dfn-relative-luminance).

## Results measured from captures

Each score is the smallest of the three Oklab pair distances computed from actual captured RGB8 object samples. Source drift is the maximum distance from the original source color across A, B, and C. These values differ slightly from the continuous CPU search scores because of output quantization.

| Palette | Unfiltered | Deuteranopia | Protanopia | Tritanomaly approx. | Max source drift |
| --- | --- | --- | --- | --- | --- |
| Original debugging palette | 0.2922 | 0.1480 | 0.2203 | 0.2618 | 0.0000 |
| Matched-Y challenge | 0.2285 | 0.0459 | 0.0743 | 0.0869 | 0.2074 |
| Okabe-Ito subset | 0.2106 | 0.1927 | 0.0973 | 0.0862 | 0.3150 |
| Lightness-separated custom | 0.2482 | 0.1815 | 0.0612 | 0.3027 | 0.2786 |
| Deutan search | 0.3185 | 0.2975 | 0.2253 | 0.2220 | 0.3923 |
| Protan search | 0.2823 | 0.2697 | 0.2930 | 0.1618 | 0.2316 |
| Tritanomaly search | 0.2849 | 0.2503 | 0.2203 | 0.2876 | 0.2531 |
| Balanced search | 0.2908 | 0.2701 | 0.2779 | 0.2760 | 0.2316 |
| Balanced with limited drift | 0.2640 | 0.2039 | 0.2813 | 0.2657 | 0.1494 |

The balanced search has the largest captured worst-view minimum among these nine cases, about 0.2701, compared with 0.1480 for the original palette. The bounded variant lowers maximum source drift to 0.1494, at the cost of a smaller worst-view minimum. These are numerical tradeoffs rather than measured changes in gameplay performance.

The Okabe-Ito subset improves the deuteranopia minimum for this assignment but gives closer pairs in the protanopia and tritanomaly views. The lightness example also has a close pair under protanopia. A label such as "colorblind-friendly palette" cannot replace context-specific checks. The single-view searches demonstrate why optimizing one view should be followed by checking the others. All pair distances and the count of reduced pairs relative to baseline appear in the [analysis report](../validation/palette-lab-analysis.json).

## Redundant shape experiment

Three source cases, Original, Matched-Y, and Balanced, also have white square, circle, and triangle outlines inside the colored objects. Unity renders these as opaque meshes in the isolated scene. The colors and object roles otherwise remain the same. Color scores sample outside the outlines and therefore deliberately do not claim to measure shape recognition. Compare the Matched-Y layout with and without cues to see the extra visual identifier.

Patterns, labels, contours, sound, and haptics are relevant alternatives. Only the three geometric outlines were implemented in this capture experiment; the other approaches remain explanations or future work.

## Published research and further ideas

[Machado and Oliveira (2010)](https://doi.org/10.1111/j.1467-8659.2009.01701.x), *Real-Time Temporal-Coherent Color Contrast Enhancement for Dichromats*, describes image-dependent contrast enhancement and temporal direction handling. The project's detailed review used [Machado's author-hosted 2010 master's thesis](https://www.inf.ufrgs.br/~oliveira/students_dissertations/Masters/Gustavo_Machado_Masters_thesis_UFRGS_2010.pdf), Chapter 5, rather than the separate full journal PDF. This remains a candidate published baseline.

| Project proposal | What a future experiment would measure | Current status |
| --- | --- | --- |
| Assign colors by semantic role | Distinction between critical markers while preserving natural scenery | Known objects support palette capture; general masks remain future work |
| Short preference calibration | Which role palette a player prefers and uses accurately | Proposal; no diagnostic procedure or participant results |
| Apply cues only where needed | Identification errors versus visual clutter | Whole-object shape capture exists; task-dependent selection remains future work |
| Keep a stable palette across frames | Flicker and consistency during motion or scene transitions | Proposal; no temporal measurements |

These are combinations to test in this project, not claims of new research inventions. A practical evaluation could compare object-identification errors, response time, and preference across candidate palettes and cue conditions, with counterbalanced presentation and recorded conditions. No participant study has been run.

## Capture and verification record

The capture run started at 2026-10-21T15:53:57.0393400Z. It produced 48 PNGs: nine palettes in four views, plus three cue variants in four views. Rendering used Unity 6000.3.11f1, URP 17.3.0, PC/Linear, Direct3D12, an NVIDIA GeForce RTX 4070 Laptop GPU, and a 1440 x 540 single-sample sRGB ARGB32 SDR target. A cloned camera in an isolated preview scene used orthographic size 2 for consistent close framing. The saved scene camera remained at size 5.

All 36 simulated full-frame comparisons passed independent NumPy float64 recomputation with maximum RGB error 1/255, zero pixels outside the existing 1/255 tolerance, and zero alpha mismatches. Original source samples matched all configured colors. Renderer material and activation states returned exactly, source scene dirtiness remained false, and every pre-existing file under Assets, Packages, and ProjectSettings matched the pre-experiment SHA-256. The source scene and original materials were not saved or altered.

Evidence: [Unity capture report](../validation/palette-lab-captures.json), [independent analysis and image hashes](../validation/palette-lab-analysis.json), and raw PNGs under `docs/images/palette-lab-20261021`. Repeat graphics capture using **Tools > ChromaAccess > Capture Palette Comparisons** with ControlledColors open in Edit mode and PC quality. Repeat selection with `Tools/search_palette_lab.py`; the capture configuration is `Assets/Settings/PaletteLab.json`.

The finalized presentation has 18 editable slides, five native tables, embedded original capture bytes, and source-bearing speaker notes. All final slides were rendered and inspected. The ten comparative PNGs are exports of those slide layouts, while the 48 raw captures remain separate. The deck has not been opened in Microsoft PowerPoint.

## How to use the deliverables

Use the PPTX directly, or insert a PNG from `docs/images/palette-comparisons/` into another presentation. These layouts contain four aligned views and labels. Use `docs/images/palette-lab-20261021/` for the raw images. A [CSV summary](palette-summary.csv) contains the captured scores. Objects A, B, and C are always left, middle, and right. The HTML comparator works locally with the repository PNGs at their existing relative paths; select two palettes, choose a view, toggle available shape cues, or show all four views.

The historical Sprint 1 presentation remains unchanged. These experiments add a comparison package rather than proving the broader research question. Participant benefit, arbitrary scenes, true tritanopia simulation, HDR, GPU timing, and temporal behavior remain outside this evidence.
