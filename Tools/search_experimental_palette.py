"""Finite-grid palette experiment, not the Machado (2010) recoloring algorithm."""

import itertools
import json
import math
from pathlib import Path

from calculate_deuteranopia_reference import srgb_to_linear, transform


ORIGINAL = ((200, 76, 76), (76, 200, 76), (76, 76, 200))
Y_TOLERANCE = 0.01


def linear_rgb(rgb8):
    return tuple(srgb_to_linear(v / 255) for v in rgb8)


def oklab(rgb):
    r, g, b = rgb
    l = (0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b) ** (1 / 3)
    m = (0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b) ** (1 / 3)
    s = (0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b) ** (1 / 3)
    return (
        0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
        1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
        0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s,
    )


def distance(a, b):
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))


def luminance(rgb):
    return sum(a * b for a, b in zip((0.2126, 0.7152, 0.0722), rgb))


def simulated_lab(rgb8):
    return oklab(tuple(min(1.0, max(0.0, v)) for v in transform(linear_rgb(rgb8))))


def pair_distances(labs):
    return [distance(labs[i], labs[j]) for i, j in ((0, 1), (0, 2), (1, 2))]


def hex_color(rgb):
    return "#" + "".join(f"{v:02X}" for v in rgb)


def run():
    baseline_labs = [oklab(linear_rgb(c)) for c in ORIGINAL]
    baseline_simulated = [simulated_lab(c) for c in ORIGINAL]
    baseline_y = [luminance(linear_rgb(c)) for c in ORIGINAL]
    groups = []
    for index in (0, 1):
        candidates = sorted(set(itertools.product(range(0, 256, 17), repeat=3)) | {ORIGINAL[index]})
        groups.append([
            (c, simulated_lab(c), distance(oklab(linear_rgb(c)), baseline_labs[index]))
            for c in candidates
            if abs(luminance(linear_rgb(c)) - baseline_y[index]) <= Y_TOLERANCE
        ])
    best_score = -1.0
    best_drift = math.inf
    best = None
    for first, second in itertools.product(*groups):
        score = min(pair_distances((first[1], second[1], baseline_simulated[2])))
        drift = first[2] + second[2]
        if score > best_score + 1e-12 or (abs(score - best_score) <= 1e-12 and drift < best_drift):
            best_score, best_drift, best = score, drift, (first[0], second[0], ORIGINAL[2])
    assert best is not None
    report = {
        "kind": "Finite-grid exploratory palette selection; no observer validation",
        "selection": "Maximize minimum pairwise Oklab distance after Machado deuteranopia simulation and SDR clipping; tie-break by minimum total original-color Oklab drift",
        "gridChannelStepRgb8": 17,
        "candidateCounts": [len(group) for group in groups],
        "evaluatedPalettes": len(groups[0]) * len(groups[1]),
        "blueHeldFixed": True,
        "relativeLuminanceTolerance": Y_TOLERANCE,
        "relativeLuminanceWeights": [0.2126, 0.7152, 0.0722],
        "pairOrder": ["0-1", "0-2", "1-2"],
        "originalHex": [hex_color(c) for c in ORIGINAL],
        "paletteHex": [hex_color(c) for c in best],
        "originalRgb8": ORIGINAL,
        "paletteRgb8": best,
        "originalRelativeLuminance": baseline_y,
        "paletteRelativeLuminance": [luminance(linear_rgb(c)) for c in best],
        "originalSimulatedPairDistancesOklab": pair_distances(baseline_simulated),
        "paletteSimulatedPairDistancesOklab": pair_distances([simulated_lab(c) for c in best]),
        "originalMinimumSimulatedDistanceOklab": min(pair_distances(baseline_simulated)),
        "paletteMinimumSimulatedDistanceOklab": best_score,
        "originalColorDriftOklab": [distance(oklab(linear_rgb(c)), baseline_labs[i]) for i, c in enumerate(best)],
        "sources": {
            "simulation": "https://www.inf.ufrgs.br/~oliveira/pubs_files/CVD_Simulation/CVD_Simulation.html#Tutorial",
            "oklab": "https://bottosson.github.io/posts/oklab/",
            "distance": "https://www.w3.org/TR/css-color-4/#color-difference-OK",
            "luminance": "https://www.w3.org/TR/WCAG22/#dfn-relative-luminance",
        },
        "limitations": "Optimum only for this finite candidate set and constraints; Oklab distance of simulated colors is an exploratory proxy, not a calibrated deuteranope discrimination metric; pairwise scores can trade off; no semantic hue preservation or gameplay benefit is established.",
    }
    root = Path(__file__).resolve().parents[1]
    output = root / "docs/validation/palette-search.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    settings = root / "Assets/Settings/ExperimentalPalette.json"
    settings.parent.mkdir(parents=True, exist_ok=True)
    settings.write_text(json.dumps({"name": "Exploratory palette v1", "colors": report["paletteHex"]}, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({k: report[k] for k in ("paletteHex", "candidateCounts", "evaluatedPalettes", "originalSimulatedPairDistancesOklab", "paletteSimulatedPairDistancesOklab", "originalColorDriftOklab")}, indent=2))


if __name__ == "__main__":
    run()
