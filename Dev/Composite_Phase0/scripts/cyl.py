"""Phase 2 spike: where does ccx take the shell normal when it projects an orientation (S8R and S6)?

A coarse curved cylinder (axis z) of isotropic material is pulled along z, so the global stress is a
uniaxial sigma_zz. ccx writes .dat stresses in the element orientation axes, so the angle between the
element x' axis and z follows from s11, s22, s12. The orientation x axis is a = (1, 0, 1) / sqrt(2);
projected onto the tangent plane at the angle phi it makes the angle atan(-sin(phi)) with z.
"""
import os
import sys
import subprocess
import numpy as np

OUT = sys.argv[1]
CCX = sys.argv[2]
R, H = 50.0, 100.0
NC, NZ = 8, 2


def build(elem):
    nid = {}
    nodes = {}

    def node(i2, j2):
        i2 %= 2 * NC
        if (i2, j2) not in nid:
            nid[(i2, j2)] = len(nid) + 1
            phi = np.pi * i2 / NC
            nodes[nid[(i2, j2)]] = (R * np.cos(phi), R * np.sin(phi), H * j2 / (2 * NZ))
        return nid[(i2, j2)]
    elems = {}
    for j in range(NZ):
        for i in range(NC):
            i2, j2 = 2 * i, 2 * j
            c = [node(i2, j2), node(i2 + 2, j2), node(i2 + 2, j2 + 2), node(i2, j2 + 2)]
            m = [node(i2 + 1, j2), node(i2 + 2, j2 + 1), node(i2 + 1, j2 + 2), node(i2, j2 + 1)]
            if elem == 'S8R':
                elems[len(elems) + 1] = (c + m, (i2 + 1, j2 + 1))
            else:
                ctr = node(i2 + 1, j2 + 1)
                elems[len(elems) + 1] = ([c[0], c[1], c[2], m[0], m[1], ctr], None)
                elems[len(elems) + 1] = ([c[2], c[3], c[0], m[2], m[3], ctr], None)
    used = sorted({n for e in elems.values() for n in e[0]})
    return {n: nodes[n] for n in used}, elems


def write(case, elem):
    nodes, elems = build(elem)
    d = os.path.join(OUT, case)
    os.makedirs(d, exist_ok=True)
    s = ['*NODE'] + [f'{n}, {p[0]:.10g}, {p[1]:.10g}, {p[2]:.10g}' for n, p in nodes.items()]
    s.append(f'*ELEMENT, TYPE={elem}, ELSET=EALL')
    s += [', '.join(str(v) for v in [e] + c[0]) for e, c in elems.items()]
    z0 = [n for n, p in nodes.items() if abs(p[2]) < 1e-9]
    z1 = [n for n, p in nodes.items() if abs(p[2] - H) < 1e-9]
    px = [n for n, p in nodes.items() if abs(p[2]) < 1e-9 and abs(p[1]) < 1e-9 and p[0] > 0]
    py = [n for n, p in nodes.items() if abs(p[2]) < 1e-9 and abs(p[0]) < 1e-9 and p[1] > 0]
    s += ['*NSET, NSET=NZ0'] + [f'{n},' for n in z0] + ['*NSET, NSET=NZ1'] + [f'{n},' for n in z1]
    s += [f'*NSET, NSET=NPX\n{px[0]},', f'*NSET, NSET=NPY\n{py[0]},']
    s.append('*MATERIAL, NAME=ISO\n*ELASTIC\n200000., 0.3')
    s.append('*ORIENTATION, NAME=OA, SYSTEM=RECTANGULAR\n1, 0, 1, 0, 1, 0')
    s.append('*SHELL SECTION, ELSET=EALL, MATERIAL=ISO, ORIENTATION=OA\n1.')
    s.append('*STEP\n*STATIC\n*BOUNDARY\nNZ0, 3, 3, 0.\nNPX, 2, 2, 0.\nNPY, 1, 1, 0.\nNZ1, 3, 3, 0.1\n'
             '*EL PRINT, ELSET=EALL\nS\n*END STEP')
    with open(os.path.join(d, case + '.inp'), 'w') as f:
        f.write('\n'.join(s) + '\n')
    subprocess.run([CCX, '-i', case], cwd=d, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                   env=dict(os.environ, OMP_NUM_THREADS='1'))
    vals = {}
    with open(os.path.join(d, case + '.dat')) as f:
        for ln in f:
            t = ln.split()
            if len(t) >= 8 and t[0].isdigit():
                vals.setdefault(int(t[0]), []).append([float(v) for v in t[2:8]])
    print(f'== {case}: element, measured angle, angle at the centroid, angle at xi = eta = 0 (node 1 for S6)')
    err_c, err_n = [], []
    for e, (conn, _) in elems.items():
        v = np.mean(vals[e], axis=0)
        theta = 0.5 * np.degrees(np.arctan2(2 * v[3], v[0] - v[1]))
        xyz = np.array([nodes[n] for n in conn])
        if elem == 'S8R':
            cen = xyz[:8].mean(axis=0)
            p0 = cen
        else:
            cen = xyz[:3].mean(axis=0)
            p0 = xyz[0]

        def pred(p):
            phi = np.arctan2(p[1], p[0])
            return np.degrees(np.arctan(-np.sin(phi)))

        def diff(a, b):
            d_ = (a - b + 90) % 180 - 90
            return abs(d_)
        tc, tn = pred(cen), pred(p0)
        err_c.append(min(diff(theta, tc), diff(-theta, tc)))
        err_n.append(min(diff(theta, tn), diff(-theta, tn)))
        if e <= 6:
            print(f'   {e:3d} {theta:8.3f} {tc:8.3f} {tn:8.3f}')
    print(f'   max |error| centroid {max(err_c):.3f} deg, xi = eta = 0 {max(err_n):.3f} deg')


write('cyl_s8r', 'S8R')
write('cyl_s6', 'S6')
