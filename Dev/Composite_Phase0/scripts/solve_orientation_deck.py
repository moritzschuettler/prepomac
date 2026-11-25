"""Adds a step to the deck PrePoMax wrote, solves it and checks ccx's element axes against the expected ones.

The material is isotropic, so the stresses do not depend on the orientation, but ccx writes the .dat stresses in
the element axes: the inclined plate pulled along its own x' axis must read s11 = 150, s22 = 50 and |s12| = 86.6
in axes turned by 30 degrees, and the cylinder pulled along its axis s11 = 0 (hoop) and s22 = 100 (axial).
"""
import os
import sys
import subprocess
import numpy as np

deck, model, ccx = sys.argv[1], sys.argv[2], sys.argv[3]
nodes, sets, cur = {}, {}, None
for line in open(model):
    line = line.strip()
    if line.startswith('*NODE'):
        cur = 'node'
    elif line.startswith('*ELEMENT'):
        cur = line.split('ELSET=')[1]
        sets[cur] = []
    elif cur == 'node':
        t = line.split(',')
        nodes[int(t[0])] = np.array([float(v) for v in t[1:4]])
    else:
        t = [int(v) for v in line.split(',')]
        sets[cur].append(t)


def rx(a):
    c, s = np.cos(a), np.sin(a)
    return np.array([[1, 0, 0], [0, c, -s], [0, s, c]])


def rz(a):
    c, s = np.cos(a), np.sin(a)
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])


R = rz(np.radians(20)) @ rx(np.radians(35))
o = np.array([0, 200, 0])
inclined = sorted({n for e in sets['INCLINED'] for n in e[1:]})
local = {n: R.T @ (nodes[n] - o) for n in inclined}
cylinder = sorted({n for e in sets['CYLINDER'] for n in e[1:]})
plate = sorted({n for e in sets['PLATE'] for n in e[1:]})


def nset(name, ids):
    return [f'*NSET, NSET={name}'] + [f'{n},' for n in ids]


s = []
s += nset('N_INC', inclined)
s += nset('N_INC_X0', [n for n in inclined if abs(local[n][0]) < 1e-6])
s += nset('N_INC_X1', [n for n in inclined if abs(local[n][0] - 100) < 1e-6])
s += nset('N_INC_ORG', [n for n in inclined if np.linalg.norm(local[n]) < 1e-6])
s += nset('N_CYL_Z0', [n for n in cylinder if abs(nodes[n][2]) < 1e-6])
s += nset('N_CYL_Z1', [n for n in cylinder if abs(nodes[n][2] - 100) < 1e-6])
s += nset('N_CYL_PX', [n for n in cylinder if abs(nodes[n][2]) < 1e-6 and abs(nodes[n][1]) < 1e-6 and nodes[n][0] > 300])
s += nset('N_CYL_PY', [n for n in cylinder if abs(nodes[n][2]) < 1e-6 and abs(nodes[n][0] - 300) < 1e-6 and nodes[n][1] > 0])
s += nset('N_PLATE', plate)
s.append('*TRANSFORM, NSET=N_INC, TYPE=R')
s.append(', '.join(f'{v:.12g}' for v in list(R[:, 0]) + list(R[:, 1])))
s.append("""*STEP
*STATIC
*BOUNDARY
N_PLATE, 1, 6, 0.
N_INC_X0, 1, 1, 0.
N_INC_ORG, 2, 2, 0.
N_INC, 3, 3, 0.
N_INC_X1, 1, 1, 0.1
N_CYL_Z0, 3, 3, 0.
N_CYL_PX, 2, 2, 0.
N_CYL_PY, 1, 1, 0.
N_CYL_Z1, 3, 3, 0.05
*EL PRINT, ELSET=INCLINED
S
*EL PRINT, ELSET=CYLINDER
S
*END STEP""")
text = open(deck).read()
i = text.upper().find('*STEP')
text = (text[:i] if i >= 0 else text) + '\n'.join(s) + '\n'
d = os.path.dirname(deck)
name = os.path.splitext(os.path.basename(deck))[0] + '_solved'
open(os.path.join(d, name + '.inp'), 'w').write(text)
subprocess.run([ccx, '-i', name], cwd=d, stdout=open(os.path.join(d, name + '.log'), 'w'), stderr=subprocess.STDOUT,
               env=dict(os.environ, OMP_NUM_THREADS='1'))
vals = {}
for ln in open(os.path.join(d, name + '.dat')):
    t = ln.split()
    if len(t) >= 8 and t[0].isdigit():
        vals.setdefault(int(t[0]), []).append([float(v) for v in t[2:8]])
inc_ids = [e[0] for e in sets['INCLINED']]
cyl_ids = [e[0] for e in sets['CYLINDER']]
inc = np.array([np.mean(vals[e], axis=0) for e in inc_ids])
cyl = np.array([np.mean(vals[e], axis=0) for e in cyl_ids])
print('inclined s11 %.2f..%.2f  s22 %.2f..%.2f  |s12| %.2f..%.2f   (expected 150, 50, 86.60)' %
      (inc[:, 0].min(), inc[:, 0].max(), inc[:, 1].min(), inc[:, 1].max(), abs(inc[:, 3]).min(), abs(inc[:, 3]).max()))
print('cylinder s11 %.2f..%.2f  s22 %.2f..%.2f  |s12| max %.2f   (expected 0, 100, 0)' %
      (cyl[:, 0].min(), cyl[:, 0].max(), cyl[:, 1].min(), cyl[:, 1].max(), abs(cyl[:, 3]).max()))
