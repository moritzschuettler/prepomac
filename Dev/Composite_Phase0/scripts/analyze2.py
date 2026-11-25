"""Checks for the second round of Phase 0 cases."""
import os
import sys
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
import frd
import gen

ROOT = sys.argv[1]
R = gen.rot_matrix()


def tensor(v):
    s11, s22, s33, s12, s23, s13 = v[:6]
    return np.array([[s11, s12, s13], [s12, s22, s23], [s13, s23, s33]])


def centre_by_layer(case, rotate=None):
    nodes, elems, res = frd.read(os.path.join(ROOT, case, case + '.frd'))
    comps, data = res['STRESS']
    out = []
    for nid, c in nodes.items():
        p = np.array(c)
        q = R.T @ p if rotate is not None else p
        if abs(q[0] - 50) < 1e-4 and abs(q[1] - 25) < 1e-4 and nid in data:
            S = tensor(data[nid])
            if rotate is not None:
                S = R.T @ S @ R
            out.append((round(q[2], 4), S[0, 0], S[1, 1], S[0, 1]))
    return sorted(set(out))


print('== c17 rotated [0/90]s, stresses rotated back into the plate frame, vs c13 flat')
a = centre_by_layer('c17_rot_sym', rotate=True)
b = centre_by_layer('c13_sym_090s')
for x, y in zip(a, b):
    print(f'   z={x[0]:+.3f}  rot: {x[1]:9.3f} {x[2]:8.3f} {x[3]:8.3f}   flat: {y[1]:9.3f} {y[2]:8.3f} {y[3]:8.3f}')

print('== c18 one global orientation on the rotated plate (ccx projects global x)')
n = R[:, 2]
x = np.array([1.0, 0, 0])
p = x - x.dot(n) * n
phi = np.arctan2(p.dot(R[:, 1]), p.dot(R[:, 0]))
print(f'   expected fibre angle in the plate frame: {np.degrees(phi):.3f} deg')
E1, E2, v12, G12 = 140000., 10000., 0.3, 5000.
c, s = np.cos(phi), np.sin(phi)
Sx = c ** 4 / E1 + (1 / G12 - 2 * v12 / E1) * s * s * c * c + s ** 4 / E2
sx = 0.001 / Sx
print(f'   uniaxial estimate: sigma_x\' = {sx:.3f}, sigma1 = {sx * c * c:.3f}, sigma2 = {sx * s * s:.3f},'
      f' tau12 = {-sx * s * c:.3f}')
vals = []
with open(os.path.join(ROOT, 'c18_rot_projection', 'c18_rot_projection.dat')) as f:
    for ln in f:
        t = ln.split()
        if len(t) >= 8 and t[0].isdigit():
            vals.append([float(v) for v in t[2:8]])
v = np.mean(vals, axis=0)
print(f'   .dat mean local stresses (all ips): s11 {v[0]:.3f} s22 {v[1]:.3f} s33 {v[2]:.3f} s12 {v[3]:.3f}')

print('== c19 rotation line "3, 90." vs explicit OR90 (c01), frd centre column')
for case in ('c19_rotation_line', 'c01_order_3d'):
    print('  ', case, [(z, round(s1, 3), round(s2, 3)) for z, s1, s2, _ in centre_by_layer(case)])

print('== c20 mixed composite / plain shells with ids 100+7k')
nodes, elems, res = frd.read(os.path.join(ROOT, 'c20_mixed_ids', 'c20_mixed_ids.frd'))
shell = {}
with open(os.path.join(ROOT, 'c20_mixed_ids', 'shell_ids.txt')) as f:
    for ln in f:
        i, cx, cy, kind = ln.split()
        shell[int(i)] = (float(cx), float(cy), kind)
for eid in sorted(elems):
    e = elems[eid]
    cg = np.mean([nodes[k] for k in e['nodes']], axis=0)
    src = min(shell, key=lambda k: (shell[k][0] - cg[0]) ** 2 + (shell[k][1] - cg[1]) ** 2)
    print(f'   frd el {eid:4d} type {e["type"]} mat {e["mat"]} z {cg[2]:+.3f} -> shell {src} ({shell[src][2]})')
print('   frd node count', len(nodes))
