"""Reproducible finite-grid comparisons. Scores are exploratory Oklab proxies."""
import itertools
import json
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
MATRIX_RECORD = ROOT / 'docs/research/machado-2009-matrices.json'
if not MATRIX_RECORD.exists():
    MATRIX_RECORD = Path('D:/ChromaAccess/docs/research/machado-2009-matrices.json')
MATRICES = json.loads(MATRIX_RECORD.read_text())['matrices']
VIEWS = ['None', 'Deuteranopia', 'Protanopia', 'Tritanomaly']
ORIGINAL = np.array([[200,76,76],[76,200,76],[76,76,200]],dtype=float)
LMS = np.array([[.4122214708,.5363325363,.0514459929],[.2119034982,.6806995451,.1073969566],[.0883024619,.2817188376,.6299787005]])
LAB = np.array([[.2104542553,.7936177850,-.0040720468],[1.9779984951,-2.4285922050,.4505937099],[.0259040371,.7827717662,-.8086757660]])

def decode(rgb8):
    s=np.asarray(rgb8)/255
    return np.where(s<=.04045,s/12.92,((s+.055)/1.055)**2.4)

def oklab(rgb):
    return np.cbrt(np.asarray(rgb) @ LMS.T) @ LAB.T

def spaces(rgb8):
    linear=decode(rgb8)
    return np.asarray([oklab(linear if view=='None' else np.clip(linear @ np.asarray(MATRICES[view]).T,0,1)) for view in VIEWS])

def hexes(rgb8):
    return ['#'+''.join(f'{int(v):02X}' for v in color) for color in rgb8]

def summary(rgb8):
    views=spaces(rgb8)
    distances=np.stack([np.linalg.norm(views[:,i]-views[:,j],axis=-1) for i,j in [(0,1),(0,2),(1,2)]],axis=-1)
    return {'pairDistances':dict(zip(VIEWS,distances.tolist())),
            'minimumDistances':dict(zip(VIEWS,distances.min(axis=-1).tolist())),
            'sourceOklabDrift':np.linalg.norm(oklab(decode(rgb8))-oklab(decode(ORIGINAL)),axis=-1).tolist(),
            'sourceRelativeY':(decode(rgb8) @ np.array([.2126,.7152,.0722])).tolist()}

def search(targets,drift_limit=None):
    source_lab=oklab(decode(ORIGINAL))
    source_y=decode(ORIGINAL) @ np.array([.2126,.7152,.0722])
    grid=np.array(list(itertools.product(range(0,256,17),repeat=3)),dtype=float)
    groups=[]
    for i in (0,1):
        candidates=np.unique(np.vstack([grid,ORIGINAL[i]]),axis=0)
        labs=oklab(decode(candidates))
        drift=np.linalg.norm(labs-source_lab[i],axis=-1)
        eligible=np.abs(decode(candidates) @ np.array([.2126,.7152,.0722])-source_y[i])<=.01
        if drift_limit is not None: eligible &= drift <= drift_limit
        candidates=candidates[eligible]
        groups.append((candidates,spaces(candidates),drift[eligible]))
    a,sa,da=groups[0]; b,sb,db=groups[1]
    fixed=spaces(ORIGINAL[2:3])[:,0,:]
    pair01=np.linalg.norm(sa[:,:,None,:]-sb[:,None,:,:],axis=-1)
    pair02=np.linalg.norm(sa-fixed[:,None,:],axis=-1)[:,:,None]
    pair12=np.linalg.norm(sb-fixed[:,None,:],axis=-1)[:,None,:]
    all_scores=np.minimum(np.minimum(pair01,pair02),pair12)
    objective=all_scores[[VIEWS.index(v) for v in targets]].min(axis=0)
    score=float(objective.max())
    tied=np.argwhere(objective>=score-1e-12)
    i,j=min(tied.tolist(),key=lambda ij:float(da[ij[0]]+db[ij[1]]))
    palette=np.array([a[i],b[j],ORIGINAL[2]])
    return palette,{'targetViews':targets,'candidateCounts':[len(a),len(b)],'evaluatedPalettes':len(a)*len(b),
                    'scoreContinuousCpu':score,'maxSourceDriftConstraint':drift_limit,
                    'sourceYAbsoluteTolerance':.01,'thirdObjectFixed':True,'gridChannelStep':17}

def run():
    y=decode(ORIGINAL[0]) @ np.array([.2126,.7152,.0722])
    challenge_green=min(range(256),key=lambda g:abs(decode([76,g,76]) @ np.array([.2126,.7152,.0722])-y))
    rows=[('baseline','Original debugging palette',ORIGINAL,'Existing scene colors, without a perceptual confusion-pair claim.'),
          ('matched-y','Matched-Y challenge',np.array([[200,76,76],[76,challenge_green,76],[76,76,200]]),'First two colors have nearly equal standard relative Y. This is a constructed numerical challenge, not an observer-matched pair.'),
          ('okabe-ito','Okabe-Ito subset',np.array([[230,159,0],[204,121,167],[0,114,178]]),'Orange, reddish purple, and blue from Okabe and Ito. The selected subset and role assignment still need context-specific testing.'),
          ('lightness','Lightness-separated custom',np.array([[40,75,99],[244,211,94],[184,59,94]]),'Manually chosen differences in source lightness. Standard Y does not guarantee perceived-brightness differences for every observer.')]
    palettes=[]
    for slug,name,rgb,explanation in rows:
        palettes.append({'id':slug,'name':name,'colors':hexes(rgb),'explanation':explanation,'search':None,'continuousCpu':summary(rgb)})
    for slug,name,targets,bound in [
        ('deutan-search','Deutan search',['Deuteranopia'],None),
        ('protan-search','Protan search',['Protanopia'],None),
        ('tritan-search','Tritanomaly search',['Tritanomaly'],None),
        ('balanced','Balanced search',VIEWS,None),
        ('bounded','Balanced with limited drift',VIEWS,.15)]:
        rgb,search_info=search(targets,bound)
        palettes.append({'id':slug,'name':name,'colors':hexes(rgb),
                         'explanation':'Finite-grid maximum of the minimum Oklab pair distance over the selected views. Preserves source relative Y within 0.01 and keeps object C fixed. Numerical proxy only.',
                         'search':search_info,'continuousCpu':summary(rgb)})
    config={'preparedDate':'2026-10-21','views':VIEWS,'palettes':palettes,
            'sources':{'simulation':json.loads(MATRIX_RECORD.read_text())['sourceUrl'],
                       'okabeIto':'https://jfly.uni-koeln.de/color/',
                       'okabeItoFigure':'https://jfly.uni-koeln.de/color/image/pallete.jpg',
                       'oklab':'https://bottosson.github.io/posts/oklab/',
                       'distance':'https://www.w3.org/TR/css-color-4/#color-difference-OK',
                       'luminance':'https://www.w3.org/TR/WCAG22/#dfn-relative-luminance'},
            'limitations':'Fixed scene, finite grid, source-Y constraint, third object fixed in searched cases. Simulated Oklab is not a calibrated discrimination threshold. No participant benefit or universal palette is established.'}
    destination=ROOT/'Assets/Settings/PaletteLab.json'
    destination.parent.mkdir(parents=True,exist_ok=True)
    destination.write_text(json.dumps(config,indent=2)+'\n',encoding='utf-8')
    for p in palettes: print(p['id'],p['colors'],{v:round(n,4) for v,n in p['continuousCpu']['minimumDistances'].items()})

if __name__=='__main__': run()
