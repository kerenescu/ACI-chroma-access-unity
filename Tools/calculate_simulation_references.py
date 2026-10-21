"""Independent double-precision CPU probes for the three published matrix cases."""
import argparse
import hashlib
import json
import math
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MATRIX_PATH = ROOT / 'docs/research/machado-2009-matrices.json'

def decode(s):
    return s / 12.92 if s <= 0.04045 else ((s + 0.055) / 1.055) ** 2.4

def encode(v):
    return 12.92 * v if v <= 0.0031308 else 1.055 * v ** (1 / 2.4) - 0.055

def reference(rgb8, matrix):
    rgb = [decode(v / 255) for v in rgb8]
    linear = [math.fsum(a*b for a,b in zip(row,rgb)) for row in matrix]
    output = [math.floor(encode(min(1,max(0,v))) * 255 + 0.5) for v in linear]
    return linear, output

def calculate(kind, matrix, source):
    probes = []
    for name,rgb8 in [('patch 0',(200,76,76)),('patch 1',(76,200,76)),('patch 2',(76,76,200)),
                      ('neutral 0',(0,0,0)),('neutral 1',(128,128,128)),('neutral 2',(255,255,255)),
                      ('gamut 0',(255,0,0)),('gamut 1',(0,255,0)),('gamut 2',(0,0,255))]:
        linear,output = reference(rgb8,matrix)
        probes.append({'name':name,'inputHex':''.join(f'{v:02X}' for v in rgb8),
                       'unclippedLinear':linear,'requiresClipping':any(v < 0 or v > 1 for v in linear),
                       'outputHex':''.join(f'{v:02X}' for v in output)})
    neutral_error = max(abs(math.fsum(row)*level-level) for row in matrix for level in (0,0.5,1))
    checks = {'blackRemainsBlack':reference((0,0,0),matrix)[1] == [0,0,0],
              'neutralRowsWithinPublishedRounding':neutral_error <= 1.1e-6,
              'all256SrgbValuesRoundTrip':max(abs(encode(decode(v/255))*255-v) for v in range(256)) < 1e-9}
    return {'kind':'CPU numerical reference, not a Unity render or observer study',
            'simulation':kind,'severity':1.0,'sourceUrl':source['sourceUrl'],
            'sourceHtmlSha256':source['sourceHtmlSha256'],'matrixRows':matrix,
            'tritanLimit':source['tritanLimit'] if kind=='Tritanomaly' else '',
            'sdrPolicy':'Decode sRGB, multiply column vector, clip linear RGB, encode sRGB, round half up',
            'probeCases':probes,'neutralMaxErrorLinear':neutral_error,'checks':checks,
            'allChecksPassed':all(checks.values())}

def capture_path(relative):
    path = ROOT / relative
    if path.exists():
        return path
    record = json.loads((ROOT / 'docs/images/image-aliases.json').read_text(encoding='utf-8'))
    return ROOT / record['aliases'][relative]


def check_captures(source):
    import numpy as np
    from PIL import Image
    results = []
    for kind,matrix in source['matrices'].items():
        slug = kind.lower()
        report = json.loads((ROOT / f'docs/validation/{slug}-pass.json').read_text())
        assert report['success'], report['message']
        assert report['matrixRowMajor'] == [v for row in matrix for v in row]
        for sample in report['samples']:
            original = [int(sample['original'][i:i+2],16) for i in (0,2,4)]
            _,output = reference(original,matrix)
            assert sample['expected'] == ''.join(f'{v:02X}' for v in output) + sample['original'][6:8]
        for group in ['', '-neutral', '-gamut', '-asymmetric']:
            before = np.array(Image.open(capture_path(f'docs/images/{slug}{group}-original.png')).convert('RGBA'))
            after = np.array(Image.open(capture_path(f'docs/images/{slug}{group}-simulated.png')).convert('RGBA'))
            rgb = before[...,:3].astype(np.float64) / 255
            linear = np.where(rgb <= .04045,rgb/12.92,((rgb+.055)/1.055)**2.4)
            transformed = np.clip(linear @ np.asarray(matrix,dtype=np.float64).T,0,1)
            encoded = np.where(transformed <= .0031308,12.92*transformed,1.055*transformed**(1/2.4)-.055)
            expected = np.floor(encoded*255+.5).astype(np.int16)
            error = np.abs(expected-after[...,:3].astype(np.int16))
            outside = int(np.count_nonzero(np.any(error > 1,axis=2)))
            alpha = int(np.count_nonzero(before[...,3] != after[...,3]))
            results.append({'simulation':kind,'group':group or 'scene','maxRgbError':int(error.max()),
                            'pixelsOutsideTolerance':outside,'alphaMismatches':alpha})
            assert outside == alpha == 0, results[-1]
    return {'success':True,'method':'Independent NumPy float64 full-frame calculation from published coefficients',
            'width':960,'height':540,'comparisons':results}

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--check-captures',action='store_true')
    args = parser.parse_args()
    source = json.loads(MATRIX_PATH.read_text(encoding='utf-8'))
    destination = ROOT / 'docs/validation'
    destination.mkdir(parents=True,exist_ok=True)
    passed = True
    for kind,matrix in source['matrices'].items():
        report = calculate(kind,matrix,source)
        reference_name = kind.lower() + ('-matrix-reference.json' if kind == 'Deuteranopia' else '-reference.json')
        (destination / reference_name).write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
        passed &= report['allChecksPassed']
        print(kind+': '+', '.join(p['inputHex']+' -> '+p['outputHex'] for p in report['probeCases'][:3]))
    if args.check_captures:
        result = check_captures(source)
        (destination / 'simulation-independent-check.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
        print('Independent full-frame checks passed: '+str(len(result['comparisons'])))
    sys.exit(0 if passed else 1)
