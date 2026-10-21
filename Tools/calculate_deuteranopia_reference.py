"""Compute CPU references for the controlled probes; this does not render in Unity."""

import json
import math
from pathlib import Path
import sys


MATRIX = (
    (0.367322, 0.860646, -0.227968),
    (0.280085, 0.672501, 0.047413),
    (-0.011820, 0.042940, 0.968881),
)
SOURCE = {
    "paperDoi": "https://doi.org/10.1109/TVCG.2009.113",
    "matrixUrl": "https://www.inf.ufrgs.br/~oliveira/pubs_files/CVD_Simulation/CVD_Simulation.html#Tutorial",
    "matrixLocation": "Tutorial, Table 1, severity 1.0, Deuteranomaly column",
    "matrixPrecisionDecimalPlaces": 6,
    "reviewDate": "2026-10-19",
    "reviewedHtmlSha256": "379c549025f91ac05a611631114ff8202fa2d802bc29e15f79479c7985373346",
    "srgbTransferFunctions": "https://www.w3.org/TR/css-color-4/#color-conversion-code",
}


def srgb_to_linear(value):
    return value / 12.92 if value <= 0.04045 else ((value + 0.055) / 1.055) ** 2.4


def linear_to_srgb(value):
    return 12.92 * value if value <= 0.0031308 else 1.055 * value ** (1 / 2.4) - 0.055


def transform(rgb):
    return [math.fsum(a * b for a, b in zip(row, rgb)) for row in MATRIX]


def probe_reference(name, rgb8):
    linear = transform([srgb_to_linear(value / 255) for value in rgb8])
    output = [
        math.floor(linear_to_srgb(min(1.0, max(0.0, value))) * 255 + 0.5)
        for value in linear
    ]
    return {
        "name": name,
        "inputHex": "#" + "".join(f"{value:02X}" for value in rgb8),
        "unclippedLinear": linear,
        "requiresClipping": any(value < 0 or value > 1 for value in linear),
        "outputHex": "#" + "".join(f"{value:02X}" for value in output),
        "outputRgb8": output,
    }


def calculate():
    input_rgb8 = [200, 76, 76]
    input_srgb = [value / 255 for value in input_rgb8]
    input_linear = [srgb_to_linear(value) for value in input_srgb]
    output_linear = transform(input_linear)
    clipped_linear = [min(1.0, max(0.0, value)) for value in output_linear]
    output_srgb = [linear_to_srgb(value) for value in clipped_linear]
    output_rgb8 = [math.floor(value * 255 + 0.5) for value in output_srgb]
    round_trip_error = max(
        abs(linear_to_srgb(srgb_to_linear(value / 255)) * 255 - value)
        for value in range(256)
    )
    neutral_error = max(
        abs(channel - gray)
        for gray in (0.0, 0.5, 1.0)
        for channel in transform((gray, gray, gray))
    )
    checks = {
        "blackRemainsExactlyBlack": transform((0.0, 0.0, 0.0)) == [0.0, 0.0, 0.0],
        "neutralProbesWithinTolerance": neutral_error <= 0.0000011,
        "all256SrgbChannelValuesRoundTrip": round_trip_error < 0.000000001,
        "redPatchNeedsNoClipping": output_linear == clipped_linear,
    }
    return {
        "kind": "CPU numerical reference, not a Unity render or accessibility evaluation",
        "source": SOURCE,
        "pythonVersion": sys.version.split()[0],
        "model": "Machado, Oliveira, Fernandes (2009)",
        "simulation": "Deuteranopia",
        "severity": 1.0,
        "workingSpace": "Linear RGB, sRGB primaries",
        "multiplicationConvention": "Output = matrix times RGB column vector",
        "matrixRows": MATRIX,
        "rowSums": [math.fsum(row) for row in MATRIX],
        "inputHex": "#C84C4C",
        "inputRgb8": input_rgb8,
        "inputSrgb": input_srgb,
        "inputLinear": input_linear,
        "outputLinearBeforeClipping": output_linear,
        "sdrPolicy": "Clamp linear channels to [0, 1], encode sRGB, round half up to 8 bits",
        "clippingApplied": output_linear != clipped_linear,
        "outputLinearAfterClipping": clipped_linear,
        "outputSrgb": output_srgb,
        "outputRgb8": output_rgb8,
        "outputHex": "#" + "".join(f"{value:02X}" for value in output_rgb8),
        "probeCases": [probe_reference(name, rgb8) for name, rgb8 in (
            ("patch 0", (200, 76, 76)),
            ("patch 1", (76, 200, 76)),
            ("patch 2", (76, 76, 200)),
            ("neutral 0", (0, 0, 0)),
            ("neutral 1", (128, 128, 128)),
            ("neutral 2", (255, 255, 255)),
            ("gamut 0", (255, 0, 0)),
            ("gamut 1", (0, 255, 0)),
            ("gamut 2", (0, 0, 255)),
        )],
        "neutralProbeLevelsLinear": [0.0, 0.5, 1.0],
        "neutralToleranceLinear": 0.0000011,
        "neutralMaxAbsoluteErrorLinear": neutral_error,
        "srgbRoundTripMaxAbsoluteErrorIn8BitUnits": round_trip_error,
        "checks": checks,
        "allChecksPassed": all(checks.values()),
    }


if __name__ == "__main__":
    report = calculate()
    destination = Path(__file__).resolve().parents[1] / "docs/validation/deuteranopia-reference.json"
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"CPU reference: {report['inputHex']} -> {report['outputHex']}; checks passed: {report['allChecksPassed']}")
    sys.exit(0 if report["allChecksPassed"] else 1)
