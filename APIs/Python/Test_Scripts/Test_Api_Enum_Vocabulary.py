#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Audits PythonEnumVocabulary against the API sources. An API member takes an enumeration valued
#argument as a plain string, so the enumeration it is validated against is known only at the ToEnum
#call inside the member - nothing reflecting over the member can recover it. The vocabulary is that
#mapping written down once, so that describe_api can list the values an argument accepts.
#
#Written down once means it can fall behind. This finds every ToEnum and ToFlagsEnum site in
#APIs/Python, takes the argument name each one validates, and checks that the vocabulary resolves it
#to the same enumeration. An argument added without an entry is reported here rather than silently
#described without its values. The view, the target and the source of a selection are lower case
#names rather than enumeration names, and are checked to be listed all the same.
#
#  PrePoMax.com --script Test_Api_Enum_Vocabulary.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import io
import os
import re
import sys

import clr
from APIs.Python import PythonEnumVocabulary

# APIs/Python, two folders above this script
SOURCES = os.path.dirname(os.path.dirname(os.path.abspath(sys.argv[0])))
CALL = re.compile(r"To(?:Flags)?Enum<([A-Za-z0-9_]+)>\s*\((.{0,400}?)\)\s*[;,)]", re.S)
ARGUMENT = re.compile(r'"([a-z0-9_]+)"')
# A Python facing argument declaration and a Python facing property. Both are matched where they are
# declared rather than by parsing whole signatures, which multi line argument lists defeat
PARAMETER_NAME = re.compile(r"\b(?:string|object|int|double|bool|float|SelectionAPI)\s+"
                            r"([a-z][a-z0-9_]*)\s*(?=[,)=])")
PROPERTY_NAME = re.compile(r"public\s+[A-Za-z0-9_<>\[\],\.]+\s+([a-z][a-z0-9_]*)\s*(?=\{|\r?\n\s*\{)")
# A name that never reaches a caller: it is built inside the member from arguments of its own
INTERNAL_NAMES = ("_created_from",)


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def api_sources():
    for root, folders, files in os.walk(SOURCES):
        if os.path.basename(root) == "Test_Scripts":
            folders[:] = []
            continue
        for name in files:
            if name.endswith(".cs"):
                yield os.path.join(root, name)


def read_members():
    """Every Python facing argument name and property name of the API sources."""
    names = set()
    for path in api_sources():
        with io.open(path, encoding="utf-8") as reader:
            text = reader.read()
        names.update(PARAMETER_NAME.findall(text))
        names.update(PROPERTY_NAME.findall(text))
    return names


def read_sites():
    """Every argument name a ToEnum site validates, with the enumerations it validates against."""
    sites = {}
    for path in api_sources():
        with io.open(path, encoding="utf-8") as reader:
            text = reader.read()
        for match in CALL.finditer(text):
            names = ARGUMENT.findall(match.group(2))
            if not names:
                continue
            sites.setdefault(names[-1], set()).add(match.group(1))
    return sites


def is_property_key(key):
    """A property is keyed as its declaring type, its name and its name again."""
    parts = key.split(".")
    return len(parts) == 3 and parts[1] == parts[2]


def describing_types(argument):
    """The enumerations the vocabulary offers for the argument, however the key is qualified."""
    described = set()
    unqualified = PythonEnumVocabulary.GetType(None, None, argument)
    if unqualified is not None:
        described.add(unqualified.Name)
    types = PythonEnumVocabulary.GetTypes()
    for key in types.keys():
        if is_property_key(key):
            continue
        if key == argument or key.endswith("." + argument):
            described.add(types[key].Name)
    return described


def test_every_site_is_described(sites):
    missing, wrong = [], []
    for argument in sorted(sites):
        if argument in INTERNAL_NAMES:
            continue
        described = describing_types(argument)
        if not described:
            missing.append(argument + " -> " + ", ".join(sorted(sites[argument])))
        elif described != sites[argument]:
            wrong.append("%s: the sources validate against %s, the vocabulary says %s" %
                         (argument, ", ".join(sorted(sites[argument])), ", ".join(sorted(described))))
    check(not missing, "these enumeration valued arguments have no entry in PythonEnumVocabulary: " +
                       "; ".join(missing))
    check(not wrong, "these entries name the wrong enumeration: " + "; ".join(wrong))
    print("audited %d enumeration valued arguments against the API sources" % len(sites))


def test_every_entry_names_a_real_argument(members):
    """An entry for an argument that was renamed or removed would describe nothing.

    A few arguments reach their ToEnum site through a variable rather than a literal - the three
    coordinate system points share one helper - so this checks the entries against the argument
    names of the API rather than against the sites.
    """
    unknown = []
    for key in PythonEnumVocabulary.GetTypes().keys():
        argument = key.split(".")[-1]
        if argument not in members:
            unknown.append(key)
    check(not unknown, "these entries name an argument the API does not have: " + ", ".join(unknown))
    print("every one of the %d entries names an argument the API still takes" %
          len(PythonEnumVocabulary.GetTypes()))


def test_values_are_readable():
    for key in PythonEnumVocabulary.GetTypes().keys():
        parts = key.split(".")
        argument = parts[-1]
        member = parts[-2] if len(parts) > 1 else None
        declaring = parts[0] if len(parts) == 3 else None
        values = list(PythonEnumVocabulary.GetValues(declaring, member, argument))
        check(len(values) > 0, "the entry " + key + " resolves to an enumeration with no values")
    check(PythonEnumVocabulary.GetType(None, None, "no_such_argument") is None,
          "an argument that is not enumeration valued must resolve to nothing")
    # The qualified keys exist so that one name can mean two vocabularies
    field_variables = list(PythonEnumVocabulary.GetValues("FieldOutputsAPI", "add_nodal", "variables"))
    history_variables = list(PythonEnumVocabulary.GetValues("HistoryOutputsAPI", "add_nodal", "variables"))
    check(field_variables != history_variables,
          "a field output and a history output must not share the variable vocabulary")
    # The degrees of freedom a displacement rotation frees are listed like any other option string
    free_dofs = list(PythonEnumVocabulary.GetValues("BoundaryConditionsAPI", "replace_displacement_rotation",
                                                    "free_dofs"))
    check(free_dofs == ["U1", "U2", "U3", "UR1", "UR2", "UR3"], "unexpected free_dofs values " + str(free_dofs))
    fixed_dofs = list(PythonEnumVocabulary.GetValues("BoundaryConditionsAPI", "add_displacement_rotation",
                                                     "fixed_dofs"))
    check(fixed_dofs == ["U1", "U2", "U3"], "unexpected fixed_dofs values " + str(fixed_dofs))
    # The view options, the history filter options and the deformation are option strings like any other
    expected = {("ViewAPI", "set_orientation", "orientation"): "IsometricNegative",
                ("ViewAPI", "result_type", "result_type"): "ColorContours",
                ("ViewAPI", "apply_exploded_view", "method"): "CenterPoint",
                ("ViewAPI", "apply_exploded_view", "axes"): "XZ",
                ("ResultHistoryOutputsAPI", "create_minimum_filter", "return_value"): "Row",
                ("ResultHistoryOutputsAPI", "create_sum_filter", "operate_on"): "Columns",
                ("SelectionAPI", "configure", "limit_to"): "FirstMesherType",
                ("ResultHistoryOutputsAPI", "create_history_output_from_integration",
                 "deformation_variable_name"): "SurfaceNormals"}
    for (declaring, member, argument), value in expected.items():
        values = PythonEnumVocabulary.GetValues(declaring, member, argument)
        check(values is not None and value in list(values),
              "the vocabulary does not describe %s.%s: %s" % (member, argument, values))
    # A read-back region_type lists every region type, as an item converted from a selection reads back
    load_region_types = list(PythonEnumVocabulary.GetValues("LoadAPI", "region_type", "region_type"))
    check("Selection" in load_region_types and "NodeId" in load_region_types,
          "a read-back region_type must list every region type: " + str(load_region_types))
    contact_methods = list(PythonEnumVocabulary.GetValues("ContactPairsAPI", "add", "method"))
    check("CenterPoint" not in contact_methods, "the exploded view method leaked into the contact pair method")
    print("every entry resolves to a non empty list of values")


def test_property_lookups():
    """describe_api looks a property up as its declaring type, its name and its name again.

    Where a property shares its name with an argument of another vocabulary, the property key must
    win, or the schema lists the values of the other enumeration.
    """
    expected = {("DistributionAPI", "interpolator_type"): ["ClosestPoint", "Gauss", "Shepard"],
                ("SurfaceAPI", "created_from"): ["Selection", "NodeSet", "Faces"],
                ("ResultFieldOutputFilterAPI", "source"): ["AllNodes", "SurfaceNodes"],
                ("ContactPairAPI", "method"): ["NodeToSurface", "SurfaceToSurface", "Mortar", "Massless"],
                ("ReferencePointAPI", "created_from"): ["Coordinates", "OnPoint", "BetweenTwoPoints",
                                                        "CircleCenter", "CenterOfGravity", "BoundingBoxCenter"]}
    for (declaring, name), values in expected.items():
        described = PythonEnumVocabulary.GetValues(declaring, name, name)
        check(described is not None and list(described) == values,
              "the property %s.%s is described with %s, not %s" %
              (declaring, name, None if described is None else list(described), values))
    print("the properties that share a name with an argument resolve to their own vocabulary")


def test_narrowed_values():
    """A member that always refuses some values of its enumeration must not list them."""
    by_selection = ["OnPoint", "BetweenTwoPoints", "CircleCenter"]
    by_region = ["CenterOfGravity", "BoundingBoxCenter"]
    expected = {("ReferencePointsAPI", "add_from_selection", "created_from"): by_selection,
                ("ReferencePointsAPI", "replace_from_selection", "created_from"): by_selection,
                ("ResultReferencePointsAPI", "add_from_selection", "created_from"): by_selection,
                ("ReferencePointsAPI", "add_from_region", "created_from"): by_region,
                ("ResultReferencePointsAPI", "replace_from_region", "created_from"): by_region}
    model_spaces = ["ThreeD", "PlaneStress", "PlaneStrain", "Axisymmetric"]
    unit_systems = [name for name in PythonEnumVocabulary.GetTypes()["unit_system"].GetEnumNames()
                    if name != "Undefined"]
    for declaring, member in [("PrePoMaxAPI", "new_model"), ("ModelPropertiesAPI", "replace"),
                              ("ModelPropertiesAPI", "model_space")]:
        expected[(declaring, member, "model_space")] = model_spaces
    for declaring, member in [("PrePoMaxAPI", "new_model"), ("ModelPropertiesAPI", "unit_system")]:
        expected[(declaring, member, "unit_system")] = unit_systems
    for collection in ["CoordinateSystemsAPI", "ResultCoordinateSystemsAPI"]:
        for member in ["add_from_selections", "replace_from_selections"]:
            for point in ["origin", "point_on_x", "point_in_xy"]:
                expected[(collection, member, point + "_created_from")] = by_selection
    # A reference point takes a node set or a surface as its region, and pmx.create.region only the types a
    # named region of the model carries
    for collection in ["ReferencePointsAPI", "ResultReferencePointsAPI"]:
        for member in ["add_from_region", "replace_from_region"]:
            expected[(collection, member, "region_type")] = ["NodeSetName", "SurfaceName"]
    expected[("CreateAPI", "region", "region_type")] = ["NodeSetName", "ElementSetName", "SurfaceName",
                                                        "ReferencePointName", "PartName", "MassSection"]
    # Every pick, area and box selection refuses the select_by modes that pick nothing, and every operation
    # refuses Invert, which invert() adds
    select_by = [name for name in PythonEnumVocabulary.GetTypes()["select_by"].GetEnumNames()
                 if name not in ("Off", "Default", "Id", "Widget")]
    for declaring, member in [("CreateAPI", "selection_by_pick"), ("CreateAPI", "selection_by_area"),
                              ("CreateAPI", "selection_by_box"), ("SelectionAPI", "add_by_pick"),
                              ("SelectionAPI", "add_by_area"), ("SelectionAPI", "add_by_box")]:
        expected[(declaring, member, "select_by")] = select_by
    operations = [name for name in PythonEnumVocabulary.GetTypes()["operation"].GetEnumNames() if name != "Invert"]
    for member in ["add_by_ids", "add_by_pick", "add_by_area", "add_by_box", "add_all"]:
        expected[("SelectionAPI", member, "operation")] = operations
    # A joint centre picked by nodes in the dialog has no argument; a script gives its coordinates
    for member in ["add_joint", "replace_joint"]:
        expected[("ConstraintsAPI", member, "center_defined_by")] = ["MasterSurface", "Coordinates"]
    for (declaring, member, argument), values in expected.items():
        described = PythonEnumVocabulary.GetValues(declaring, member, argument)
        check(described is not None and list(described) == values,
              "%s.%s(%s) is described with %s, not %s" %
              (declaring, member, argument, None if described is None else list(described), values))
        check(PythonEnumVocabulary.GetType(declaring, member, argument) is not None,
              "%s.%s(%s) must still name its enumeration" % (declaring, member, argument))
    allowed = PythonEnumVocabulary.GetAllowedValues()
    types = PythonEnumVocabulary.GetTypes()
    for key in allowed.keys():
        check(key in types, "the narrowed key " + key + " has no enumeration")
        names = set(types[key].GetEnumNames())
        check(all(value in names for value in allowed[key]),
              "the narrowed key " + key + " lists a value its enumeration does not have")
    print("the members that accept part of an enumeration list only that part")


def test_refusals_list_the_narrowed_values():
    """An unknown value is refused listing only the values the member accepts, as the schema lists them."""
    def message_of(function):
        try:
            function()
        except Exception as ex:
            return str(ex)
        return None
    message = message_of(lambda: pmx.create.region("NoSuchType", "Any"))
    check(message is not None and "'region_type'" in message and "NodeSetName" in message and
          "Selection" not in message and "NodeId" not in message,
          "the region_type refusal must list only the named region types: " + str(message))
    message = message_of(lambda: pmx.create.selection_by_pick("femodel", "nodes", [0, 0, 0], "NoSuchMode"))
    if message is not None and "'select_by'" in message:
        check("Widget" not in message and "Surface" in message,
              "the select_by refusal must not list the modes a pick refuses: " + str(message))
    print("the refusals of an unknown value list only the values the member accepts")


def test_selection_names(members):
    """The view, the target and the source of a selection are lower case names, not enumeration names.

    They are listed for describe_api all the same, keyed by the member, and every key must name an
    argument the API takes; the names must be the ones the selection service accepts.
    """
    names = PythonEnumVocabulary.GetLowerCaseNames()
    unknown = [key for key in names.keys() if key.split(".")[-1] not in members]
    check(not unknown, "these selection name entries name an argument the API does not have: " + ", ".join(unknown))
    targets = ["nodes", "elements", "surfaces", "edges", "parts", "geometry", "geometry_surfaces"]
    sources = ["nodes", "elements", "vertices", "edges", "surfaces", "parts", "geometry"]
    expected = {("CreateAPI", "selection_by_ids", "target"): targets,
                ("CreateAPI", "selection_by_ids", "source"): sources,
                ("CreateAPI", "selection_by_pick", "target"): targets,
                ("CreateAPI", "selection_by_box", "view"): ["geometry", "femodel", "results"],
                ("CreateAPI", "selection_all", "target"): ["nodes", "elements", "surfaces"],
                ("SelectionAPI", "add_by_ids", "source"): sources}
    for (declaring, member, argument), values in expected.items():
        described = PythonEnumVocabulary.GetValues(declaring, member, argument)
        check(described is not None and list(described) == values,
              "%s.%s(%s) is described with %s, not %s" %
              (declaring, member, argument, None if described is None else list(described), values))
        check(PythonEnumVocabulary.GetType(declaring, member, argument) is None,
              "%s.%s(%s) is not parsed as an enumeration" % (declaring, member, argument))
    # The filter source of a result field output keeps its own enumeration
    filter_sources = list(PythonEnumVocabulary.GetValues("ResultFieldOutputsAPI", "create_median_filter", "source"))
    check(filter_sources == ["AllNodes", "SurfaceNodes"], "the filter source lost its vocabulary: " +
          str(filter_sources))
    print("the view, the target and the source of a selection list the names they accept")


def main():
    check(os.path.isdir(SOURCES), "the API sources must be beside the test scripts: " + SOURCES)
    sites = read_sites()
    check(len(sites) > 20, "the audit found too few ToEnum sites to be reading the sources: " + str(len(sites)))
    members = read_members()
    check(len(members) > 200, "the audit found too few API arguments to be reading the sources: " +
                              str(len(members)))
    test_every_site_is_described(sites)
    test_every_entry_names_a_real_argument(members)
    test_values_are_readable()
    test_property_lookups()
    test_narrowed_values()
    test_refusals_list_the_narrowed_values()
    test_selection_names(members)
    print("TEST PASSED")


main()
