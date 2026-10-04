#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks that the inverted and the degenerate solid elements of a model part are found. Meshing keeps a
#mesh with such elements and highlights them instead of refusing it, and get_inverted_element_ids is how a
#script finds them. The script imports a mesh it writes itself: every solid element type once as it
#should be, once mirrored through the XY plane, which inverts it, and a hexahedron flattened onto its
#bottom face, which is degenerate. Each element is in an element set of its own, so its verdict can be read.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Inverted_Elements.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import tempfile

PREFIX = "PYTEST_INV_"

TETRA = [(0, 0, 0), (1, 0, 0), (0, 1, 0), (0, 0, 1)]
PYRAMID = [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0), (0.5, 0.5, 1)]
WEDGE = [(0, 0, 0), (1, 0, 0), (0, 1, 0), (0, 0, 1), (1, 0, 1), (0, 1, 1)]
HEXA = [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0), (0, 0, 1), (1, 0, 1), (1, 1, 1), (0, 1, 1)]
TETRA_EDGES = [(0, 1), (1, 2), (2, 0), (0, 3), (1, 3), (2, 3)]
HEXA_EDGES = [(0, 1), (1, 2), (2, 3), (3, 0), (4, 5), (5, 6), (6, 7), (7, 4), (0, 4), (1, 5), (2, 6), (3, 7)]


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def with_midside_nodes(corners, edges):
    midside = [tuple((corners[a][k] + corners[b][k]) / 2.0 for k in range(3)) for a, b in edges]
    return corners + midside


def mirrored(points):
    return [(x, y, -z) for x, y, z in points]


def flattened(points):
    return [(x, y, 0.0) for x, y, z in points]


# name, abaqus type, node coordinates, expected to be inverted
def get_cases():
    tetra10 = with_midside_nodes(TETRA, TETRA_EDGES)
    hexa20 = with_midside_nodes(HEXA, HEXA_EDGES)
    return [
        ("TETRA", "C3D4", TETRA, False), ("TETRA_MIRRORED", "C3D4", mirrored(TETRA), True),
        ("PYRAMID", "C3D5", PYRAMID, False), ("PYRAMID_MIRRORED", "C3D5", mirrored(PYRAMID), True),
        ("WEDGE", "C3D6", WEDGE, False), ("WEDGE_MIRRORED", "C3D6", mirrored(WEDGE), True),
        ("HEXA", "C3D8", HEXA, False), ("HEXA_MIRRORED", "C3D8", mirrored(HEXA), True),
        ("HEXA_FLAT", "C3D8", flattened(HEXA), True),
        ("TETRA10", "C3D10", tetra10, False), ("TETRA10_MIRRORED", "C3D10", mirrored(tetra10), True),
        ("HEXA20", "C3D20", hexa20, False), ("HEXA20_MIRRORED", "C3D20", mirrored(hexa20), True),
    ]


def write_mesh(file_name, cases):
    lines = ["*NODE"]
    elements = []
    node_id = 1000
    for index, (name, element_type, points, inverted) in enumerate(cases):
        node_ids = []
        for x, y, z in points:
            node_id += 1
            node_ids.append(node_id)
            lines.append("%d, %.6f, %.6f, %.6f" % (node_id, x + 3.0 * index, y, z))
        elements.append((PREFIX + name, element_type, 1000 + index + 1, node_ids))
    for elset, element_type, element_id, node_ids in elements:
        lines.append("*ELEMENT, TYPE=%s, ELSET=%s" % (element_type, elset))
        ids = [str(element_id)] + [str(i) for i in node_ids]
        # An element line holds at most 16 entries
        lines.append(", ".join(ids[:16]) + ("," if len(ids) > 16 else ""))
        if len(ids) > 16: lines.append(", ".join(ids[16:]))
    with open(file_name, "w") as file:
        file.write("\n".join(lines) + "\n")


def main():
    cases = get_cases()
    file_name = os.path.join(tempfile.mkdtemp(), "inverted_elements.inp")
    write_mesh(file_name, cases)
    pmx.import_file(file_name)
    #
    # The import names the parts itself and keeps every element set, so an element is found by its set
    reported = set()
    for part in pmx.model.parts:
        reported.update(int(i) for i in part.get_inverted_element_ids())
    set_names = dict((name.upper(), name) for name in pmx.model.element_sets.get_names())
    checked = set()
    for name, element_type, points, inverted in cases:
        set_name = set_names.get(PREFIX + name)
        check(set_name is not None, "the imported mesh has no element set " + PREFIX + name)
        element_ids = [int(i) for i in pmx.model.element_sets[set_name].get_element_ids()]
        check(len(element_ids) == 1, name + " must hold one element, got " + str(len(element_ids)))
        checked.add(element_ids[0])
        check((element_ids[0] in reported) == inverted, name + " (" + element_type + ") must " +
              ("" if inverted else "not ") + "be reported as inverted")
    check(reported <= checked, "the valid cube must report no inverted element")
    print(str(len(cases)) + " elements checked.")
    print("TEST PASSED")


main()
