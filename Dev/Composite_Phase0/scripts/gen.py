"""Generate small CalculiX composite shell test models for the Phase 0 spike."""
import os
import sys
import numpy as np

OUT = sys.argv[1] if len(sys.argv) > 1 else '.'
L, W = 100.0, 50.0
NX, NY = 4, 2
T = 0.5  # layer thickness
DU = 0.1  # prescribed end displacement -> eps_x = 0.001

MAT = """*MATERIAL, NAME=PLY
*ELASTIC, TYPE=ENGINEERING CONSTANTS
140000., 10000., 10000., 0.3, 0.3, 0.5, 5000., 5000.
3333.333333
"""


def grid(elem='S8R', flip=False):
    """Return nodes {id: (x,y,0)} and elements {id: [node ids]} of an NX x NY plate in XY."""
    dx, dy = L / NX, W / NY
    nid = {}
    nodes = {}

    def node(i2, j2):  # half-grid indices
        key = (i2, j2)
        if key not in nid:
            nid[key] = len(nid) + 1
            nodes[nid[key]] = (i2 * dx / 2, j2 * dy / 2, 0.0)
        return nid[key]
    elems = {}
    eid = 0
    for j in range(NY):
        for i in range(NX):
            i2, j2 = 2 * i, 2 * j
            c = [node(i2, j2), node(i2 + 2, j2), node(i2 + 2, j2 + 2), node(i2, j2 + 2)]
            m = [node(i2 + 1, j2), node(i2 + 2, j2 + 1), node(i2 + 1, j2 + 2), node(i2, j2 + 1)]
            if elem in ('S8R', 'S8'):
                if flip:
                    conn = [c[0], c[3], c[2], c[1], m[3], m[2], m[1], m[0]]
                else:
                    conn = c + m
                eid += 1
                elems[eid] = conn
            elif elem in ('S4', 'S4R'):
                conn = [c[0], c[3], c[2], c[1]] if flip else c
                eid += 1
                elems[eid] = conn
            elif elem == 'S6':
                # two S6 per quad: corners then midsides
                ctr = node(i2 + 1, j2 + 1)
                t1 = [c[0], c[1], c[2], m[0], m[1], ctr]
                t2 = [c[0], c[2], c[3], ctr, m[2], m[3]]
                for t in (t1, t2):
                    eid += 1
                    elems[eid] = t
    # drop unused nodes
    used = sorted({n for e in elems.values() for n in e})
    nodes = {n: nodes[n] for n in used}
    return nodes, elems


def write(case, body_sections, elem='S8R', flip=False, rot=None, el_file='OUTPUT=3D', extra_model='',
          transform=False):
    nodes, elems = grid(elem, flip)
    R = np.eye(3) if rot is None else rot
    d = os.path.join(OUT, case)
    os.makedirs(d, exist_ok=True)
    s = ['*HEADING', case, '*NODE']
    for n, p in nodes.items():
        q = R @ np.array(p)
        s.append(f'{n}, {q[0]:.10g}, {q[1]:.10g}, {q[2]:.10g}')
    s.append(f'*ELEMENT, TYPE={elem}, ELSET=EALL')
    for e, conn in elems.items():
        s.append(', '.join(str(v) for v in [e] + conn))
    x0 = [n for n, p in nodes.items() if abs(p[0]) < 1e-9]
    x1 = [n for n, p in nodes.items() if abs(p[0] - L) < 1e-9]
    org = [n for n, p in nodes.items() if abs(p[0]) < 1e-9 and abs(p[1]) < 1e-9]
    s.append('*NSET, NSET=NALL')
    s += [f'{n},' for n in nodes]
    s.append('*NSET, NSET=NX0')
    s += [f'{n},' for n in x0]
    s.append('*NSET, NSET=NX1')
    s += [f'{n},' for n in x1]
    s.append('*NSET, NSET=NORG')
    s += [f'{n},' for n in org]
    if transform:
        a, b = R[:, 0], R[:, 1]
        s.append('*TRANSFORM, NSET=NALL, TYPE=R')
        s.append(', '.join(f'{v:.10g}' for v in list(a) + list(b)))
    s.append(MAT.strip())
    s.append(extra_model.strip() if extra_model else '')
    s.append(body_sections(elems, R).strip())
    s.append("""*STEP
*STATIC
*BOUNDARY
NX0, 1, 1, 0.
NORG, 2, 2, 0.
NALL, 3, 3, 0.
NX1, 1, 1, %g
*NODE FILE
U
*EL FILE%s
S, E
*EL PRINT, ELSET=EALL
S
*END STEP""" % (DU, (', ' + el_file) if el_file else ''))
    with open(os.path.join(d, case + '.inp'), 'w') as f:
        f.write('\n'.join(x for x in s if x != '') + '\n')


def orient(name, a, b):
    return f'*ORIENTATION, NAME={name}, SYSTEM=RECTANGULAR\n' + ', '.join(f'{v:.10g}' for v in list(a) + list(b)) + '\n'


ORS = orient('OR0', [1, 0, 0], [0, 1, 0]) + orient('OR90', [0, 1, 0], [-1, 0, 0])


def comp_090(elems, R, offset=''):
    return ORS + f'*SHELL SECTION, ELSET=EALL, COMPOSITE{offset}\n{T},,PLY,OR0\n{T},,PLY,OR90\n'


def comp_090s(elems, R):
    return ORS + f'*SHELL SECTION, ELSET=EALL, COMPOSITE\n{T},,PLY,OR0\n{T},,PLY,OR90\n{T},,PLY,OR90\n{T},,PLY,OR0\n'


def dist_orients(elems, R, angles):
    out = []
    for k, ang in enumerate(angles):
        c, s_ = np.cos(np.radians(ang)), np.sin(np.radians(ang))
        a = R @ np.array([c, s_, 0.0])
        b = R @ np.array([-s_, c, 0.0])
        out.append(f'*DISTRIBUTION, NAME=D{k}')
        out.append(', ' + ', '.join(f'{v:.10g}' for v in list(a) + list(b)))
        for e in elems:
            out.append(f'{e}, ' + ', '.join(f'{v:.10g}' for v in list(a) + list(b)))
        out.append(f'*ORIENTATION, NAME=OD{k}, SYSTEM=RECTANGULAR')
        out.append(f'D{k}')
    return '\n'.join(out) + '\n'


def comp_dist_090(elems, R):
    return dist_orients(elems, R, [0, 90]) + f'*SHELL SECTION, ELSET=EALL, COMPOSITE\n{T},,PLY,OD0\n{T},,PLY,OD1\n'


def comp_dist_090s(elems, R):
    return dist_orients(elems, R, [0, 90]) + \
        f'*SHELL SECTION, ELSET=EALL, COMPOSITE\n{T},,PLY,OD0\n{T},,PLY,OD1\n{T},,PLY,OD1\n{T},,PLY,OD0\n'


def comp_dist_angle_line(elems, R):
    # distribution plus an extra rotation line on the orientation
    return (f'*DISTRIBUTION, NAME=D0\n, 1, 0, 0, 0, 1, 0\n' + ''.join(f'{e}, 1, 0, 0, 0, 1, 0\n' for e in elems) +
            '*ORIENTATION, NAME=ODA, SYSTEM=RECTANGULAR\nD0\n3, 90.\n' + orient('OR0', [1, 0, 0], [0, 1, 0]) +
            f'*SHELL SECTION, ELSET=EALL, COMPOSITE\n{T},,PLY,OR0\n{T},,PLY,ODA\n')


def comp_no_orient(elems, R):
    return f'*SHELL SECTION, ELSET=EALL, COMPOSITE\n{T},,PLY\n{T},,PLY\n'


def homog_orient90(elems, R):
    return orient('OR90', [0, 1, 0], [-1, 0, 0]) + f'*SHELL SECTION, ELSET=EALL, MATERIAL=PLY, ORIENTATION=OR90\n{2 * T}\n'


def rot_matrix():
    def rx(a):
        c, s = np.cos(a), np.sin(a)
        return np.array([[1, 0, 0], [0, c, -s], [0, s, c]])

    def rz(a):
        c, s = np.cos(a), np.sin(a)
        return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])
    return rz(np.radians(20)) @ rx(np.radians(35))


if __name__ == '__main__':
    write('c01_order_3d', comp_090)
    write('c02_order_2d', comp_090, el_file='OUTPUT=2D')
    write('c03_order_default', comp_090, el_file='')
    write('c04_dist', comp_dist_090)
    write('c05_offset_p05', lambda e, R: comp_090(e, R, ', OFFSET=0.5'))
    write('c06_flip', comp_090, flip=True)
    write('c07_s6', comp_090, elem='S6')
    write('c08_s4r', comp_090, elem='S4R')
    write('c09_s8', comp_090, elem='S8')
    write('c10_no_orient', comp_no_orient)
    write('c11_dist_angle_line', comp_dist_angle_line)
    write('c12_rotated', comp_dist_090, rot=rot_matrix(), transform=True)
    write('c13_sym_090s', comp_090s)
    write('c14_homog_orient', homog_orient90)
    write('c15_rotated_sym', comp_dist_090s, rot=rot_matrix(), transform=True)
    print('ok')
