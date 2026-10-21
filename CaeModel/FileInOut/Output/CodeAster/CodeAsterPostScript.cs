// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace FileInOut.Output
{
    /// <summary>
    /// The Python the Code_Aster command file ends with: it writes the results of every static step into a CalculiX
    /// results file (.frd), which PrePoMax opens as it opens the results of CalculiX. The displacements are written for
    /// every node, the stresses of solids in global axes, the von Mises stress of plain shells at their two surfaces,
    /// and for composite shells the Tsai-Wu failure index and reserve factor of the most loaded ply with its number,
    /// the ply stresses turned from the axes of the laminate into the axes of the ply.
    /// </summary>
    internal static class CodeAsterPostScript
    {
        public const string Text = @"
# ---- PrePoMax: the results of the static steps as a CalculiX results file ----
import json as _ppm_json
import math as _ppm_math
import numpy as _ppm_np

_PPM = _ppm_json.load(open(_PPM_DATA_FILE))
_ppm_conn = mesh.getConnectivity()
_ppm_ids = _PPM['nodes']
_ppm_frd = open(_PPM_FRD_FILE, 'w')
_ppm_blocks = 0


def _ppm_e(value):
    # A node a field does not cover reads NaN, as PrePoMax shows a ply without a strength
    if value != value:
        return '         NaN'
    if value in (float('inf'), float('-inf')):
        value = 1e30
    return '%12.5E' % value


def _ppm_all_nodes(rows, count):
    for index in range(len(_ppm_ids)):
        if index not in rows:
            rows[index] = [float('nan')] * count
    return rows


def _ppm_header():
    w = _ppm_frd.write
    w('    1C\n')
    w('    1U' + _PPM['heading'][:66].ljust(66) + '\n')
    w('    1UPGM               Code_Aster                                      \n')
    w('    2C' + ('%30d' % len(_ppm_ids)) + ' ' * 37 + '1\n')
    for index, node_id in enumerate(_ppm_ids):
        x, y, z = _ppm_coords[index]
        w(' -1' + ('%10d' % node_id) + _ppm_e(x) + _ppm_e(y) + _ppm_e(z) + '\n')
    w(' -3\n')
    elements = _PPM['elements']
    w('    3C' + ('%30d' % len(elements)) + ' ' * 37 + '1\n')
    for element_id, frd_type, nodes in elements:
        w(' -1' + ('%10d' % element_id) + ('%5d' % frd_type) + ('%5d' % 0) + ('%5d' % 1) + '\n')
        for i in range(0, len(nodes), 10):
            w(' -2' + ''.join('%10d' % n for n in nodes[i:i + 10]) + '\n')
    w(' -3\n')


def _ppm_block(step, name, components, rows, kind):
    '''rows: {node index: [values]}; kind 1 scalar, 2 vector, 4 tensor'''
    w = _ppm_frd.write
    global _ppm_blocks
    _ppm_blocks += 1
    w('    1PSTEP' + ('%26d' % _ppm_blocks) + ('%12d' % 1) + ('%12d' % step) + ' ' * 10 + '\n')
    w('  100CL  101' + ('%12.9f' % float(step)) + ('%12d' % len(rows)) + ' ' * 21 + '0' + ('%5d' % step) +
      '           1\n')
    count = len(components) + (1 if kind == 2 else 0)
    w(' -4  ' + name.ljust(8) + ('%5d' % count) + '    1\n')
    tensor = {'SXX': (1, 1), 'SYY': (2, 2), 'SZZ': (3, 3), 'SXY': (1, 2), 'SYZ': (2, 3), 'SZX': (3, 1)}
    for i, component in enumerate(components):
        if kind == 4:
            a, b = tensor[component]
            w(' -5  ' + component.ljust(8) + '    1    4' + ('%5d' % a) + ('%5d' % b) + '\n')
        else:
            w(' -5  ' + component.ljust(8) + '    1' + ('%5d' % kind) + ('%5d' % (i + 1)) + '    0\n')
    if kind == 2:
        w(' -5  ALL         1    2    0    0    1ALL\n')
    for index in sorted(rows):
        w(' -1' + ('%10d' % _ppm_ids[index]) + ''.join(_ppm_e(v) for v in rows[index]) + '\n')
    w(' -3\n')


def _ppm_nodal(field, components):
    rows = {}
    for k, component in enumerate(components):
        values, description = field.getValuesWithDescription(component, [])
        for value, index in zip(values, description[0]):
            rows.setdefault(index, [0.0] * len(components))[k] = value
    return rows


_ppm_conn_array = None


def _ppm_elno(field, components, group):
    '''The values of the components of an ELNO field of a group with the node of every value, as numpy arrays'''
    global _ppm_conn_array
    if _ppm_conn_array is None:
        width = max(len(c) for c in _ppm_conn)
        _ppm_conn_array = _ppm_np.full((len(_ppm_conn), width), -1, dtype=_ppm_np.int64)
        for i, c in enumerate(_ppm_conn):
            _ppm_conn_array[i, :len(c)] = c
    arrays = []
    nodes = None
    for component in components:
        values, description = field.getValuesWithDescription(component, [group])
        arrays.append(_ppm_np.asarray(values, dtype=float))
        if nodes is None:
            nodes = _ppm_conn_array[_ppm_np.asarray(description[0]), _ppm_np.asarray(description[2])]
    return nodes, arrays


def _ppm_ply(sxx, syy, sxy, angle, strength):
    '''The Tsai-Wu index and reserve factor of a ply turned by the angle, for numpy arrays of the stresses'''
    c, s = _ppm_math.cos(_ppm_math.radians(angle)), _ppm_math.sin(_ppm_math.radians(angle))
    s1 = c * c * sxx + s * s * syy + 2 * c * s * sxy
    s2 = s * s * sxx + c * c * syy - 2 * c * s * sxy
    t12 = -c * s * sxx + c * s * syy + (c * c - s * s) * sxy
    xt, xc, yt, yc, s12, f12 = strength
    f1, f2 = 1 / xt - 1 / xc, 1 / yt - 1 / yc
    f11, f22, f66 = 1 / (xt * xc), 1 / (yt * yc), 1 / s12 ** 2
    f12 = f12 * _ppm_math.sqrt(f11 * f22)
    quadratic = f11 * s1 * s1 + f22 * s2 * s2 + f66 * t12 * t12 + 2 * f12 * s1 * s2
    linear = f1 * s1 + f2 * s2
    index = quadratic + linear
    with _ppm_np.errstate(divide='ignore', invalid='ignore'):
        reserve = _ppm_np.where(quadratic > 0,
                                (-linear + _ppm_np.sqrt(linear * linear + 4 * quadratic)) / (2 * quadratic),
                                _ppm_np.where(linear > 0, 1 / linear, 1e30))
    return index, reserve


def _ppm_keep_worst(best, nodes, index, reserve, ply):
    '''best: node -> [index, reserve, ply] with the smallest reserve'''
    order = _ppm_np.argsort(reserve, kind='stable')
    unique, first = _ppm_np.unique(nodes[order], return_index=True)
    for node, i in zip(unique.tolist(), order[first].tolist()):
        row = best.get(node)
        if row is None or reserve[i] < row[1]:
            best[node] = [float(index[i]), float(reserve[i]), float(ply)]


def _ppm_step(result, step):
    _ppm_block(step, 'DISP', ['D1', 'D2', 'D3'], _ppm_nodal(result.getField('DEPL', 1), ['DX', 'DY', 'DZ']), 2)
    if _PPM['solid_groups']:
        stress = CALC_CHAMP(RESULTAT=result, GROUP_MA=_PPM['solid_groups'], CONTRAINTE='SIGM_NOEU')
        rows = _ppm_nodal(stress.getField('SIGM_NOEU', 1), ['SIXX', 'SIYY', 'SIZZ', 'SIXY', 'SIYZ', 'SIXZ'])
        _ppm_block(step, 'STRESS', ['SXX', 'SYY', 'SZZ', 'SXY', 'SYZ', 'SZX'], rows, 4)
    shells = _PPM['shell_groups'] + [g['group'] for g in _PPM['composite_groups']]
    if not shells:
        return
    shell_result = CALC_CHAMP(RESULTAT=result, GROUP_MA=shells, CONTRAINTE='SIGM_ELNO')
    # Plain shells: the von Mises stress at the bottom and top surface
    if _PPM['shell_groups']:
        rows = {}
        for k, level in enumerate(('INF', 'SUP')):
            for group in _PPM['shell_groups']:
                layer = POST_CHAMP(RESULTAT=shell_result, GROUP_MA=group,
                                   EXTR_COQUE=_F(NOM_CHAM='SIGM_ELNO', NUME_COUCHE=1, NIVE_COUCHE=level))
                nodes, (a, b, t) = _ppm_elno(layer.getField('SIGM_ELNO', 1), ('SIXX', 'SIYY', 'SIXY'), group)
                mises = _ppm_np.sqrt(_ppm_np.maximum(0.0, a * a - a * b + b * b + 3 * t * t))
                peak = _ppm_np.zeros(len(_ppm_ids))
                _ppm_np.maximum.at(peak, nodes, mises)
                for node in _ppm_np.unique(nodes).tolist():
                    rows.setdefault(node, [0.0, 0.0])
                    rows[node][k] = max(rows[node][k], float(peak[node]))
        _ppm_block(step, 'SHELLVM', ['BOTTOM', 'TOP'], _ppm_all_nodes(rows, 2), 1)
    # Composite shells: every ply at both of its surfaces, in the axes of the ply
    rows = {}
    for group in _PPM['composite_groups']:
        for number, ply in enumerate(group['layers'], start=1):
            if ply['strength'] is None:
                continue
            for level in ('INF', 'SUP'):
                layer = POST_CHAMP(RESULTAT=shell_result, GROUP_MA=group['group'],
                                   EXTR_COQUE=_F(NOM_CHAM='SIGM_ELNO', NUME_COUCHE=number, NIVE_COUCHE=level))
                nodes, (a, b, t) = _ppm_elno(layer.getField('SIGM_ELNO', 1), ('SIXX', 'SIYY', 'SIXY'),
                                             group['group'])
                index, reserve = _ppm_ply(a, b, t, ply['angle'], ply['strength'])
                _ppm_keep_worst(rows, nodes, index, reserve, number)
    if rows:
        _ppm_block(step, 'TSAIWU', ['INDEX', 'RESERVE', 'PLY'], _ppm_all_nodes(rows, 3), 1)


_ppm_coords = mesh.getCoordinates().getValues()
_ppm_coords = [_ppm_coords[3 * i:3 * i + 3] for i in range(len(_ppm_coords) // 3)]
_ppm_header()
for _ppm_number, _ppm_result in enumerate(_ppm_results, start=1):
    _ppm_step(_ppm_result, _ppm_number)
_ppm_frd.write('9999\n')
_ppm_frd.close()
";
    }
}
