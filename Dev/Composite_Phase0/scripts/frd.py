"""Minimal .frd reader for the Phase 0 spike."""
import numpy as np


def read(path):
    nodes, elems, results = {}, {}, {}
    with open(path) as f:
        lines = f.read().splitlines()
    i = 0
    n = len(lines)
    while i < n:
        ln = lines[i]
        key = ln[:6].strip()
        if key == '2C':
            i += 1
            while not lines[i].startswith(' -3'):
                l2 = lines[i]
                nid = int(l2[3:13])
                nodes[nid] = tuple(float(l2[13 + 12 * k:25 + 12 * k]) for k in range(3))
                i += 1
        elif key == '3C':
            i += 1
            cur = None
            while not lines[i].startswith(' -3'):
                l2 = lines[i]
                if l2.startswith(' -1'):
                    p = l2[3:].split()
                    cur = int(p[0])
                    elems[cur] = {'type': int(p[1]), 'group': int(p[2]), 'mat': int(p[3]), 'nodes': []}
                elif l2.startswith(' -2'):
                    elems[cur]['nodes'] += [int(x) for x in l2[3:].split()]
                i += 1
        elif ln.startswith('  100C'):
            # result block header; name on the following -4 line
            i += 1
            name = lines[i][5:13].strip()
            comps = []
            i += 1
            while lines[i].startswith(' -5'):
                comps.append(lines[i][5:13].strip())
                i += 1
            data = {}
            while not lines[i].startswith(' -3'):
                l2 = lines[i]
                if l2.startswith(' -1'):
                    nid = int(l2[3:13])
                    vals = [float(l2[13 + 12 * k:25 + 12 * k]) for k in range((len(l2) - 13) // 12)]
                    data[nid] = vals
                i += 1
            results[name] = (comps, data)
        i += 1
    return nodes, elems, results
