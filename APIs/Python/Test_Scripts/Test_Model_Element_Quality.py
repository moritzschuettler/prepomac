#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the standard element quality metrics through element_sets.add_from_quality on one element of every
#type: an ideal hexahedron (1), tetrahedron (2), wedge (3) and pyramid (4), an inverted tetrahedron (5), and
#the shells - a unit square (11), a warped quadrilateral (12), a parallelogram (13) and an equilateral
#triangle (14). The gmsh scaled Jacobian must find the same inverted element.
#
#  PrePoMax.com -f element_quality.inp -u MM_TON_S_C --script Test_Model_Element_Quality.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def get_ids(metric, larger_than, limit):
    sets = pmx.model.element_sets
    name = sets.get_names()
    name = PREFIX + metric + "_" + str(len(name))
    element_set = sets.add_from_quality(name, metric, list(pmx.model.parts.get_names()), larger_than, limit)
    ids = set(element_set.get_element_ids())
    sets.remove([name])
    return ids


def expect(metric, larger_than, limit, expected):
    ids = get_ids(metric, larger_than, limit)
    word = "larger than" if larger_than else "not larger than"
    check(ids == set(expected), metric + " " + word + " " + str(limit) + " gives " + str(sorted(ids)) +
          " instead of " + str(sorted(expected)))


def test_element_quality():
    check(pmx.model.elements.count == 9, "the model has " + str(pmx.model.elements.count) + " elements instead of 9")
    # Every ideal element has the scaled Jacobian 1, whatever its type, and only the inverted one is negative
    expect("ScaledJacobian", True, 0.99, [1, 2, 3, 4, 11, 14])
    expect("ScaledJacobian", False, 0.0, [5])
    expect("minSJ", False, 0.0, [5])
    # The warped quadrilateral and the parallelogram have edges of two lengths
    expect("EdgeRatio", True, 1.01, [12, 13])
    # The parallelogram has the corner angles 51.3 and 128.7 degrees; the others 60 or 90
    expect("MinimumAngle", False, 59.9, [13])
    expect("MaximumAngle", True, 120, [13])
    # The warpage is measured on quadrilateral faces only, so the tetrahedra and the triangle are never in a set
    expect("Warpage", True, 1, [12])
    expect("Warpage", False, 1, [1, 3, 4, 11, 13])
    expect("Skew", True, 0.5, [13])
    expect("Taper", True, 0.01, [12])
    # The aspect ratio is defined for triangles, quadrilaterals and tetrahedra
    ideal = get_ids("AspectRatio", False, 1.001)
    check({2, 11, 14} <= ideal and not ({1, 3, 4, 13} & ideal),
          "AspectRatio not larger than 1.001 gives " + str(sorted(ideal)))
    # The option is read regardless of case
    expect("scaledjacobian", False, 0.0, [5])


test_element_quality()
print("TEST PASSED")
