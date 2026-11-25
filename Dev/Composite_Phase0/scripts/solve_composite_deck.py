"""Adds a step to the composite deck PrePoMax wrote, solves it and compares the ply stresses with Phase 0.

Both plates are pulled along their own x axis to a strain of 0.001 with the normal displacement held, as c13 of
Phase 0. The .dat stresses are in the ply axes: the 0 degree plies must read s11 = 140.6, s22 = 2.41 and the 90
degree plies s11 = -2.41, s22 = 10.15 (c13).
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
        sets[cur].append([int(v) for v in line.split(',')])


def rx(a):
    c, s = np.cos(a), np.sin(a)
    return np.array([[1, 0, 0], [0, c, -s], [0, s, c]])


def rz(a):
    c, s = np.cos(a), np.sin(a)
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])


R = rz(np.radians(20)) @ rx(np.radians(35))
frames = {'PLATE': (np.eye(3), np.zeros(3)), 'INCLINED': (R, np.array([0, 200, 0]))}
s = []
for name, (Rm, o) in frames.items():
    ids = sorted({n for e in sets[name] for n in e[1:]})
    local = {n: Rm.T @ (nodes[n] - o) for n in ids}
    for tag, sel in [('ALL', ids), ('X0', [n for n in ids if abs(local[n][0]) < 1e-6]),
                     ('X1', [n for n in ids if abs(local[n][0] - 100) < 1e-6]),
                     ('ORG', [n for n in ids if np.linalg.norm(local[n]) < 1e-6])]:
        s += [f'*NSET, NSET=N_{name}_{tag}'] + [f'{n},' for n in sel]
    if name == 'INCLINED':
        s.append(f'*TRANSFORM, NSET=N_{name}_ALL, TYPE=R')
        s.append(', '.join(f'{v:.12g}' for v in list(Rm[:, 0]) + list(Rm[:, 1])))
cyl = sorted({n for e in sets['CYLINDER'] for n in e[1:]})
s += ['*NSET, NSET=N_CYL'] + [f'{n},' for n in cyl]
s.append('*STEP\n*STATIC\n*BOUNDARY\nN_CYL, 1, 6, 0.')
for name in frames:
    s.append(f'N_{name}_X0, 1, 1, 0.\nN_{name}_ORG, 2, 2, 0.\nN_{name}_ALL, 3, 3, 0.\nN_{name}_X1, 1, 1, 0.1')
s.append('*EL PRINT, ELSET=PLATE\nS\n*EL PRINT, ELSET=INCLINED\nS\n*END STEP')
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
for part in frames:
    ids = [e[0] for e in sets[part]]
    stress = np.array([vals[e] for e in ids])          # element, integration point, component
    plies = stress.reshape(len(ids), 4, 8, 6).mean(axis=(0, 2))
    print(part, ' '.join('ply %d: s11 %8.3f s22 %7.3f' % (k + 1, plies[k][0], plies[k][1]) for k in range(4)))
