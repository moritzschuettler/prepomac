#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks a compound whose transfinite cells are boxes and five-faced prisms, meshed together. gmsh meshes the boxes
#while the 3-sided caps of the prisms are already meshed as three blocks of quads, and must not renumber the nodes of
#the caps then, or the prisms are built on node tags that no longer hold ("The given key was not present in the
#dictionary").
#
#  - transfinite_frame.brep: a square frame of four boxes (w = 4, h = 3, the inner square 2a = 20) joined by four
#    quarter cylinder corners of radius w, fragmented into one compound of eight cells, made with gmsh.model.occ:
#      boxes: addBox(a,-a,0, w,2a,h), addBox(-a,a,0, 2a,w,h), addBox(-a-w,-a,0, w,2a,h), addBox(-a,-a-w,0, 2a,w,h)
#      corners: addCylinder(cx,cy,0, 0,0,h, w, angle=pi/2) at (a,a), (-a,a), (-a,-a), (a,-a), turned by k*pi/2
#    Every cell is recognized as transfinite. The compound meshed without a mesh setup item is hexahedra only, none
#    of them inverted, and every corner has the irregular nodes of its three blocks on one line along the height.
#
#  PrePoMax.com -f transfinite_frame.brep -u MM_TON_S_C --script Test_Model_Meshing_Transfinite_Frame.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

COMPOUND = "Compound-1"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def main():
    check(pmx.model.is_open(), "a model must be open")
    cells = [part for part in pmx.model.geometry.parts if part.name != COMPOUND]
    check(len(cells) == 8, "the frame must have eight cells: " + str([part.name for part in cells]))
    corners = [part.name for part in cells if part.get_face_data().count == 5]
    check(len(corners) == 4, "the frame must have four five-faced corners: " + str(corners))
    #
    report = pmx.model.mesh.generate(COMPOUND)[COMPOUND]
    check(report == "", "meshing the frame reported: " + report)
    data = pmx.model.elements.get_data()
    offsets = [int(i) for i in data.offsets]
    connectivity = [int(i) for i in data.connectivity]
    node_counts = [int(n) for n in data.node_counts]
    index = dict((int(element_id), i) for i, element_id in enumerate(data.ids))
    num_elements = 0
    for mesh_part in pmx.model.parts:
        element_ids = [int(i) for i in mesh_part.get_element_ids()]
        num_elements += len(element_ids)
        inverted = [int(i) for i in mesh_part.get_inverted_element_ids()]
        check(len(inverted) == 0, mesh_part.name + " has " + str(len(inverted)) + " inverted elements")
        valence = {}
        for element_id in element_ids:
            i = index[element_id]
            check(node_counts[i] in (8, 20), mesh_part.name + " must be meshed by hexahedra only: an element has " +
                  str(node_counts[i]) + " nodes")
            for node in connectivity[offsets[i]:offsets[i] + 8]:
                valence[node] = valence.get(node, 0) + 1
        if mesh_part.name not in corners:
            continue
        # The three blocks of the caps: the irregular nodes lie on one vertical line through the corner
        coordinates = dict((int(node_id), list(c)) for node_id, c in zip(mesh_part.get_node_ids(),
                                                                          mesh_part.get_node_coordinates()))
        irregular = [node for node, count in valence.items() if count not in (1, 2, 4, 8)]
        check(len(irregular) > 0, mesh_part.name + " must have the inner node of its three blocks")
        check(all(valence[node] in (3, 6) for node in irregular),
              mesh_part.name + ": the irregular nodes must have the three quads of the inner node around them")
        spread = [max(coordinates[n][k] for n in irregular) - min(coordinates[n][k] for n in irregular)
                  for k in range(3)]
        check(spread[0] < 1e-6 and spread[1] < 1e-6 and spread[2] > 0,
              "the irregular nodes of " + mesh_part.name + " do not lie on one vertical line: " + str(spread))
    print("the frame:", num_elements, "hexahedra")
    print("TEST PASSED")


main()
