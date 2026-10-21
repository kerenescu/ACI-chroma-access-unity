# Sprint 1: initial original/simulation test protocol

**Historical procedure:** the Unity validation scripts and Validate menu actions described below were removed in INT-013. Recorded results remain available. Use the Simulation Preview and Capture Palette Comparisons tools for the current demonstrator. Byte-identical capture filenames resolve through `docs/images/image-aliases.json`.

Date: 2026-10-19. Status: **first execution passed**. The procedures and 1/255 tolerance were defined before implementation; the execution record below reports the observed results.

## First execution record

Unity 6000.3.11f1 / URP 17.3.0, PC quality, Linear color space, Direct3D12, NVIDIA GeForce RTX 4070 Laptop GPU. Latest GPU report timestamp: `2026-10-19T15:48:08.4785743Z`.

| Check | Observed result |
| --- | --- |
| Repeated original and final original restoration | 0 differing RGBA8 pixels |
| Identity regression and mode activation switching | Passed |
| Main simulation, full frame | Maximum RGB error 1/255; 0 pixels outside tolerance; 0 alpha mismatches |
| Neutral probes | Maximum RGB error 0/255 |
| Gamut and asymmetric probes | Maximum RGB error 1/255; 0 pixels outside tolerance |
| Linear input diagnostic | Maximum red-input error about 7.12e-8 in a separate floating-point capture |
| Temporary materials, transforms, and effect activation state | Restored; saved scene unchanged; no unsaved scene changes before or after |

Red center: CPU `#8B7F49`, GPU `#8B8049`. Green and blue centers matched their expected `#BDAC56` and `#0057C6`. Main-scene CPU comparison: 11664 pixels differ by at most one code value; the overall original/simulation comparison changes 34992 pixels. This is approximate agreement within the specified tolerance, not bit-exact agreement.

Evidence: [GPU report](../validation/deuteranopia-pass.json), [independent CPU reference](../validation/deuteranopia-reference.json), [original capture](../images/deuteranopia-original.png), [simulation capture](../images/deuteranopia-simulated.png), and [labeled comparison](../images/deuteranopia-comparison.png). The saved renderer was reopened and validation passed again after finalizing its feature map. Source and GPU shader compilation completed without errors.

## Purpose and scope

Demonstrate a first repeatable comparison between the original ControlledColors scene and a deuteranopia simulation using Machado, Oliveira, and Fernandes (2009), severity 1.0. Verify the implementation against an independent CPU reference before using the simulation to investigate recoloring.

Simulation is an approximation. Passing these checks does not establish player accessibility benefits, perceptual accuracy for each observer, or recoloring effectiveness.

## Existing evidence

- [Identity validation report](../validation/identity-pass.json): the existing full-screen pass matches the baseline, including an asymmetric scene, and an execution control proves that it runs.
- [CPU reference report](../validation/deuteranopia-reference.json): the source-checked matrix maps the red patch's sRGB `#C84C4C` to `#8B7F49` using linear RGB and the recorded SDR output policy.
- [Source review](../research/machado-2009-deuteranopia.md): original paper, tutorial coefficients, equations, encoding, assumptions, and limitations.

The Unity simulation and an Editor preview now exist. Open **Tools > ChromaAccess > Simulation Preview**, inspect the Game view, and use Original / Original + Deuteranopia. In Edit mode, use **Validate simulation**, or **Tools > ChromaAccess > Validate Deuteranopia**. The separate experimental-palette modes are covered in [their own study](../research/experimental-palette.md); this simulation validation temporarily disables that palette and restores its prior state. These controls operate in the Editor; no runtime/build interface is implemented. The CPU report remains distinct from Unity captures.

## Controlled conditions

Use Unity 6000.3.11f1, URP 17.3.0, PC quality and renderer, and Linear project color space. Use the same ControlledColors camera, transforms, materials, background, and disabled camera post-processing for the paired captures. Match the existing 960 x 540 sRGB ARGB32, single-sample capture configuration.

Record the actual graphics API, GPU, render-target format, resolution, pipeline, effect state, and scene state for each run. Preserve existing unsaved scene edits. Temporary validation probes must be restored after capture, including when validation fails.

Before implementing the matrix multiplication, inspect the color buffer supplied at the selected URP injection point. Confirm the encoding of samples and target writes; do not decode samples a second time if Unity already provides linear values.

## Checks

| Check | Procedure | Acceptance and evidence |
| --- | --- | --- |
| Original baseline | Capture with simulation disabled, then repeat without scene changes | Both captures match in RGBA8; record any nonzero difference before interpreting simulation results |
| Identity regression | Run the existing identity validator with the simulation disabled | The full identity, asymmetric, and execution-control checks still pass |
| Red numerical reference | Capture with deuteranopia enabled and sample the center of PatchRed | Compare with CPU expectation `#8B7F49`; record each channel's signed error and the maximum absolute error |
| Other color probes | Extend the independent CPU reference to the green and blue patch inputs, then compare their captured interiors | Compare with the numerical expectations, without substituting visual appearance for numeric evidence |
| Neutral probes | Temporarily render black, a documented neutral gray, and white; compare original/simulated interiors | Preserve neutrality within the declared numeric tolerance; retain the published matrix's small rounding deviations in the CPU calculation |
| Gamut probes | Use documented saturated input probes whose CPU matrix outputs leave [0, 1] | Record unclipped values and compare the rendered result with the declared SDR clipping policy |
| Orientation | Temporarily move the red patch upward, capture original and simulated versions | The transformed patch appears at the same location; no vertical flip or displaced geometry |
| Alpha and restoration | Compare alpha in the opaque captures, then disable simulation and recapture | Opaque alpha remains unchanged; the final original capture matches the initial baseline; temporary state is restored |

For CPU-versus-GPU interior pixel checks, use an **initial maximum absolute error of 1/255 per RGB channel** in the final 8-bit sRGB capture. This is an engineering tolerance for the SDR implementation, not a perceptual threshold or an established GPU result. Record actual errors even when they pass. Investigate failures; document and justify any protocol revision before rerunning, rather than silently widening the threshold.

Compare patch interiors away from rasterized edges. The existing scene tests only opaque alpha; it cannot establish transparent-content behavior. The independent CPU calculator now includes all nine scene, neutral, and saturated RGB reference probes. Full-frame expected colors are computed from the original RGBA8 capture, so their precision is bounded by that capture.

## Evidence to retain

Save a matched original/simulation image pair, clearly labeled with the model and severity. Keep an execution report with configuration, numeric expectations, observed pixels, channel errors, pass/fail outcomes, and state-restoration results. Record the run and its limitations in the README development journal.

A comparison can use labeled paired images or a repeatable mode switch. A particular UI layout is not required to establish the first numerical comparison. If a runtime interface is added, verify its mode labels and that Original really disables the simulation.

## Sprint completion and presentation

Sprint 1 can close when the scoped problem and research question, focused simulation/recoloring literature review, recorded Unity configuration, controlled scene, first verified Unity simulation, and idea/sources/progress presentation are available. The expected demonstrator is the paired original/deuteranopia comparison with a repeatable initial protocol and honest limitations.

Suggested presentation flow: problem and working question; simulation versus recoloring and the source choices; Unity scene and comparison; verification evidence, limitations, and the next experiment. This is an outline, not a finished presentation.

Recoloring experiments, object masks, temporal stability studies, GPU benchmarks, and user studies are subsequent work. Do not report these as Sprint 1 results without actual evidence.
