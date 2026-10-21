# Sprint 1 presenter guide

Prepared on 2026-10-19 for [the editable presentation](ChromaAccess_Sprint1.pptx). Allow about 9–10 minutes including a short demonstration. The [research review](../research/sprint-1-review.md) provides the detailed source comparison. Speaker notes in the deck contain slide-specific explanations and source links.

## Main message

ChromaAccess now has a controlled Unity scene, a traceable first deuteranopia simulation, and repeatable numerical checks. This establishes the foundation for later recoloring research. The palette experiment illustrates the difference between optimizing a proxy and demonstrating a benefit for players.

## Slide talking points

| Slide | Time | Talking point |
| --- | --- | --- |
| 1. ChromaAccess | 15 s | Introduce ACI and the Sprint 1 scope. |
| 2. Problem and research question | 45 s | Explain the wider recoloring question and the narrower engineering question tested now. |
| 3. Simulation, recoloring, and evaluation | 35 s | Explain the distinct purpose and evidence of each operation. |
| 4. Simulation literature | 50 s | Compare Brettel’s LMS projection with Machado’s published RGB matrices without ranking their accuracy. |
| 5. Published recoloring candidate | 55 s | Explain dominant contrast loss, projection/rotation, temporal sign handling, and semantic-color limits. |
| 6. First simulation transform | 45 s | Explain why the matrix must operate on linear RGB and why clipping matters. |
| 7. Unity configuration | 30 s | Explain the controlled scene and fixed capture conditions. |
| 8. Original and simulated scene | 45 s | Compare matched captures and show the preview if practical. |
| 9. Initial protocol and results | 50 s | Explain independent references, identity controls, the one-code-value tolerance, and state restoration. |
| 10. Optional palette experiment | 50 s | Explain the improved minimum, the pair that worsens, and the green-to-pink tradeoff. |
| 11. Sprint 1 outcome and next work | 35 s | State what the evidence supports and which experiments remain future work. |
| 12. Sources | 15 s | Identify the primary research and the source-access distinction for the 2010 method. |

These are rehearsal estimates, not a required presentation duration.

## Unity demonstration

1. Open `D:\ChromaAccess` in Unity 6000.3.11f1 and open `Assets/Scenes/ControlledColors.unity`.
2. Stay in **Edit mode**, use **PC** quality, and select the **Game** view. No keyboard shortcuts or Play mode are required.
3. Open **Tools > ChromaAccess > Simulation Preview**.
4. Click **Original**, then **Original + Deuteranopia**. Explain that the second image approximates a specified deficiency using a published model.
5. If time permits, click **Experimental palette**, then **Experimental + Deuteranopia**. Point out that the middle source patch changes from green to pink. Explain the minimum-distance objective and the pair that loses separation.
6. Return to **Original**. Closing the preview clears its temporary palette overrides.

The numerical reports and embedded deck images retain the earlier validation evidence. The Unity Validate actions were removed in INT-013. Use the preview and captured fallback during presentation; the palette capture menu remains available.

## Likely questions

**Can we make the three patches look like the original red, green, and blue to a person with deuteranopia?**

The current work does not establish that. Recoloring aims to make useful distinctions easier under stated constraints. Simulation and a display transformation do not restore the missing cone response or guarantee trichromatic color experiences.

**Why choose Machado 2009?**

The author publishes endpoint coefficients, and the matrix has a direct implementation in the verified linear rendering pass. This supports a reproducible first case. We have not demonstrated that it is more accurate than Brettel for all observers.

**Does agreement with the CPU mean the simulation is perceptually correct?**

It means this GPU implementation agrees with the selected mathematical reference under the tested conditions. The perceptual model has its own assumptions. This test provides no participant evidence.

**Why allow one 8-bit code value?**

The project declared that tolerance before integration to accommodate finite rendering and readback precision. Full-frame comparisons, neutral/gamut probes, and exact alpha checks make its use explicit. The red patch differs in its green channel by one code value. No pixel exceeds the tolerance.

**Have we implemented the published 2010 recoloring method?**

No. We studied the journal record and the author’s detailed Chapter 5 thesis description. The current palette search is an original finite-grid experiment for three known objects. A published baseline remains a later increment.

**Does the larger Oklab distance prove better accessibility?**

No. It is an exploratory numerical proxy on simulated colors. One pair worsens and the source appearance changes substantially. Task-based testing with appropriate participants would be needed to establish a player benefit.

**Do we have real-time performance measurements?**

No. The shader runs in the rendering pipeline, but the project has not measured GPU cost or timing stability. Historical performance reported by a paper is not a project measurement.

## Rehearsal checklist

- Open the deck and check that its embedded captures display correctly on the presentation screen.
- Rehearse the distinction between the wider research question and the Sprint 1 implementation check.
- Locate the Game view and preview window before presenting. Keep the captured fallback available.
- Present the palette’s source-color cost alongside its numerical gain.
- Ask for feedback on research scope, semantic color constraints, and the choice of a published recoloring baseline.
- Record feedback in the README before changing the implementation plan.
