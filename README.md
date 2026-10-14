# ChromaAccess

**Real-time color adaptation for players with color vision deficiencies.**

## Overview

ChromaAccess aims to develop and evaluate recoloring methods for images and gameplay sequences, with integration into Unity. The goal is to improve the distinction between important game elements for players with color vision deficiencies while preserving visual structure and artistic style.

## Objectives

- Simulate protanopia and deuteranopia using published color vision models.
- Compare existing recoloring methods and adapt them to game scenes.
- Prioritize contrast between important objects and their surroundings using annotated masks.
- Apply color transformations through configurable Unity shaders.
- Reduce unintended color fluctuations between frames.

## Methodology and Evaluation

The project will compare fixed global recoloring, recoloring adapted to scene colors, and a variant that prioritizes important gameplay regions.

Evaluation will consider:

- Object distinguishability.
- Contrast under simulated color vision deficiencies.
- Temporal stability.
- GPU processing time.

## Proposed Technologies

- Unity
- C#
- HLSL / Shader Graph
- Python
- OpenCV

## Initial References

1. Machado, Oliveira, Fernandes (2009).
   [A Physiologically-based Model for Simulation of Color Vision Deficiency](https://doi.org/10.1109/TVCG.2009.113).
   IEEE Transactions on Visualization and Computer Graphics.

2. Machado, Oliveira (2010).
   [Real-Time Temporal-Coherent Color Contrast Enhancement for Dichromats](https://doi.org/10.1111/j.1467-8659.2009.01701.x).
   Computer Graphics Forum.

3. Microsoft.
   [Xbox Accessibility Guideline 103](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/103).

## Status

Initial stage: project definition and literature review.
The features described above are planned objectives.
