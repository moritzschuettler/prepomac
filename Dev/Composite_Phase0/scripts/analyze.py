"""Report the layout of expanded composite elements in each spike case."""
import os
import sys
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
import frd

ROOT = sys.argv[1]
cases = sys.argv[2:]
for case in cases:
    p = os.path.join(ROOT, case, case + '.frd')
    if not os.path.exists(p):
        print(f'== {case}: no frd')
        continue
    nodes, elems, res = frd.read(p)
    print(f'== {case}: {len(nodes)} nodes, {len(elems)} elements, types {sorted({e["type"] for e in elems.values()})}')
    ids = sorted(elems)
    print('   element ids:', ids[:12], '...' if len(ids) > 12 else '')
    # layer by mean z (in the plate frame use the coordinate along the mean normal)
    coor = {k: np.array(v) for k, v in nodes.items()}
    for eid in ids[:6]:
        e = elems[eid]
        z = [coor[nn][2] for nn in e['nodes']]
        print(f'   el {eid} type {e["type"]} mat {e["mat"]} nodes {len(e["nodes"])} z [{min(z):.4f}, {max(z):.4f}]')
    # shared nodes between elements on different z ranges
    node_users = {}
    for eid, e in elems.items():
        for nn in e['nodes']:
            node_users.setdefault(nn, []).append(eid)
    if 'STRESS' in res:
        comps, data = res['STRESS']
        # pick the node closest to the plate centre per distinct z
        zs = sorted({round(c[2], 6) for c in coor.values()})
        print('   distinct z of nodes:', zs[:12])
        cx, cy = 50.0, 25.0
        for z in zs:
            cand = [k for k, c in coor.items() if abs(c[2] - z) < 1e-6]
            k = min(cand, key=lambda q: (coor[q][0] - cx) ** 2 + (coor[q][1] - cy) ** 2)
            s = data.get(k)
            users = node_users.get(k, [])
            print(f'   z={z:+.4f} node {k} at ({coor[k][0]:.1f},{coor[k][1]:.1f}) used by {len(users)} el {users[:4]}'
                  f' S=' + (' '.join(f'{v:10.3f}' for v in s[:6]) if s else 'none'))
    if 'DISP' in res:
        comps, data = res['DISP']
        mx = max(abs(v[0]) for v in data.values())
        print(f'   max |UX| = {mx:.5f}')
