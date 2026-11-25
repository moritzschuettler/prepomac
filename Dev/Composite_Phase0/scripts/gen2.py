"""Second round of Phase 0 cases: rotated plate, rotation line, projection, id mapping."""
import os
import sys
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
import gen

OUT = sys.argv[1]
gen.OUT = OUT
T = gen.T


def orient_rot(name, R, ang=0.0, line=None):
    c, s = np.cos(np.radians(ang)), np.sin(np.radians(ang))
    a = R @ np.array([c, s, 0.0])
    b = R @ np.array([-s, c, 0.0])
    txt = f'*ORIENTATION, NAME={name}, SYSTEM=RECTANGULAR\n' + ', '.join(f'{v:.10g}' for v in list(a) + list(b)) + '\n'
    if line:
        txt += line + '\n'
    return txt


R = gen.rot_matrix()


def rot_sym(elems, Rm):
    return (orient_rot('OR0', Rm, 0) + orient_rot('OR90', Rm, 90) +
            f'*SHELL SECTION, ELSET=EALL, COMPOSITE\n{T},,PLY,OR0\n{T},,PLY,OR90\n{T},,PLY,OR90\n{T},,PLY,OR0\n')


def rot_projection(elems, Rm):
    # one global orientation on the rotated plate: ccx must project global x onto the plate
    return gen.orient('OG', [1, 0, 0], [0, 1, 0]) + f'*SHELL SECTION, ELSET=EALL, MATERIAL=PLY, ORIENTATION=OG\n{2 * T}\n'


def rotation_line(elems, Rm):
    return (gen.orient('OR0', [1, 0, 0], [0, 1, 0]) + gen.orient('OR0R', [1, 0, 0], [0, 1, 0]).rstrip() + '\n3, 90.\n' +
            f'*SHELL SECTION, ELSET=EALL, COMPOSITE\n{T},,PLY,OR0\n{T},,PLY,OR0R\n')


gen.write('c17_rot_sym', rot_sym, rot=R, transform=True)
gen.write('c18_rot_projection', rot_projection, rot=R, transform=True)
gen.write('c19_rotation_line', rotation_line)

# c20: non-contiguous ids, two materials, composite and plain shells in one model, default output
nodes, elems = gen.grid('S8R')
ids = {k: 100 + 7 * k for k in elems}
d = os.path.join(OUT, 'c20_mixed_ids')
os.makedirs(d, exist_ok=True)
s = ['*HEADING', 'c20', '*NODE']
s += [f'{n}, {p[0]:.10g}, {p[1]:.10g}, {p[2]:.10g}' for n, p in nodes.items()]
s.append('*ELEMENT, TYPE=S8R, ELSET=EALL')
s += [', '.join(str(v) for v in [ids[e]] + c) for e, c in elems.items()]
comp = [ids[e] for e in elems if nodes[elems[e][0]][0] < 50 - 1e-9]
homo = [ids[e] for e in elems if ids[e] not in comp]
s.append('*ELSET, ELSET=ECOMP')
s += [f'{e},' for e in comp]
s.append('*ELSET, ELSET=EHOMO')
s += [f'{e},' for e in homo]
x0 = [n for n, p in nodes.items() if abs(p[0]) < 1e-9]
x1 = [n for n, p in nodes.items() if abs(p[0] - gen.L) < 1e-9]
org = [n for n, p in nodes.items() if abs(p[0]) < 1e-9 and abs(p[1]) < 1e-9]
for nm, lst in (('NALL', list(nodes)), ('NX0', x0), ('NX1', x1), ('NORG', org)):
    s.append(f'*NSET, NSET={nm}')
    s += [f'{n},' for n in lst]
s.append(gen.MAT.strip())
s.append(gen.MAT.strip().replace('NAME=PLY', 'NAME=PLY2'))
s.append(gen.ORS.strip())
s.append(f'*SHELL SECTION, ELSET=ECOMP, COMPOSITE\n{T},,PLY,OR0\n{T},,PLY2,OR90')
s.append(f'*SHELL SECTION, ELSET=EHOMO, MATERIAL=PLY, ORIENTATION=OR0\n{2 * T}')
s.append(f"""*STEP
*STATIC
*BOUNDARY
NX0, 1, 1, 0.
NORG, 2, 2, 0.
NALL, 3, 3, 0.
NX1, 1, 1, {gen.DU}
*NODE FILE
U
*EL FILE
S, E
*EL PRINT, ELSET=EALL
S
*END STEP""")
with open(os.path.join(d, 'c20_mixed_ids.inp'), 'w') as f:
    f.write('\n'.join(s) + '\n')
with open(os.path.join(d, 'shell_ids.txt'), 'w') as f:
    for e in elems:
        cg = np.mean([nodes[n] for n in elems[e][:4]], axis=0)
        f.write(f'{ids[e]} {cg[0]:.4f} {cg[1]:.4f} {"comp" if ids[e] in comp else "homo"}\n')
print('ok')
