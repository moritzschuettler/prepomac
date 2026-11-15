#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Writes a fingerprint of the open model and its results as JSON, and optionally saves the model, so that
#Run_Corpus.py can compare a model before and after a save, a reopen or a regeneration.
#
#  PrePoMax.com -f <model.pmx> --script Corpus_Fingerprint.py --scriptArgs "<out.json> [<save.pmx>]" -g No -x Yes -w <dir>
#
#The fingerprint holds the item names of every model collection, the mesh counts, the CalculiX input deck the
#model exports with its comment lines left out, and the loaded results. Nothing is recorded in the history.

import json
import sys

unreadable = []


def safe(function, path):
    try:
        return function()
    except Exception as reason:
        unreadable.append(path + ": " + str(reason).splitlines()[0][0:300])
        return None


def names(owner, name, path):
    return safe(lambda: list(getattr(owner, name).get_names()), path)


MODEL_COLLECTIONS = ["parts", "node_sets", "element_sets", "surfaces", "reference_points",
                     "coordinate_systems", "materials", "sections", "material_orientations", "constraints",
                     "surface_interactions", "contact_pairs", "distributions", "amplitudes",
                     "initial_conditions", "parameters", "steps"]
STEP_COLLECTIONS = ["bcs", "loads", "defined_fields", "field_outputs", "history_outputs"]
PROPERTIES = ["name", "model_space", "unit_system", "model_type"]


def read_model():
    model = {}
    model["properties"] = {name: safe(lambda: str(getattr(pmx.model.properties, name)), "properties." + name)
                           for name in PROPERTIES}
    for collection in MODEL_COLLECTIONS:
        model[collection] = names(pmx.model, collection, "model." + collection)
    model["geometry_parts"] = names(pmx.model.geometry, "parts", "model.geometry.parts")
    model["mesh_setup_items"] = names(pmx.model.mesh, "setup_items", "model.mesh.setup_items")
    parts = []
    for part_name in model["parts"] or []:
        part = pmx.model.parts[part_name]
        parts.append([part_name, safe(lambda: str(part.part_type), part_name + ".part_type"),
                      safe(lambda: part.node_count, part_name + ".node_count"),
                      safe(lambda: part.element_count, part_name + ".element_count")])
    model["parts_detail"] = parts
    model["node_count"] = safe(lambda: pmx.model.nodes.count, "model.nodes.count")
    model["element_count"] = safe(lambda: pmx.model.elements.count, "model.elements.count")
    steps = []
    for step_name in model["steps"] or []:
        step = pmx.model.steps[step_name]
        entry = {"name": step_name, "type": safe(lambda: str(step.item_type), step_name + ".item_type")}
        for collection in STEP_COLLECTIONS:
            entry[collection] = names(step, collection, step_name + "." + collection)
        steps.append(entry)
    model["steps_detail"] = steps
    parameters = []
    for parameter_name in model["parameters"] or []:
        parameter = pmx.model.parameters[parameter_name]
        parameters.append([parameter_name, safe(lambda: str(parameter.equation), parameter_name + ".equation")])
    model["parameters_detail"] = parameters
    model["jobs"] = names(pmx, "jobs", "jobs")
    return model


def read_results():
    results = {"names": names(pmx, "results", "results")}
    if not results["names"]:
        return results
    result = safe(lambda: pmx.results.current, "results.current")
    if result is None:
        return results
    results["current"] = {
        "name": safe(lambda: result.name, "result.name"),
        "unit_system": safe(lambda: str(result.unit_system), "result.unit_system"),
        "step_count": safe(lambda: len(result.steps), "result.steps"),
        "node_count": safe(lambda: result.mesh.nodes.count, "result.mesh.nodes.count"),
        "element_count": safe(lambda: result.mesh.elements.count, "result.mesh.elements.count"),
        "field_outputs": names(result, "field_outputs", "result.field_outputs"),
        "history_outputs": names(result, "history_outputs", "result.history_outputs"),
    }
    return results


def read_input_deck(file_name):
    if safe(lambda: pmx.export_to_calculix(file_name), "export_to_calculix") is None and \
            any(entry.startswith("export_to_calculix") for entry in unreadable):
        return None
    lines = []
    with open(file_name, "r", encoding="utf-8", errors="replace") as file:
        for line in file:
            line = line.rstrip()
            # A comment carries the program version and the date the deck was written
            if line.startswith("**"):
                continue
            lines.append(line)
    return lines


def main():
    arguments = sys.argv[1:]
    output_file_name = arguments[0]
    fingerprint = {"model": read_model(), "results": read_results()}
    fingerprint["input_deck"] = read_input_deck(output_file_name + ".inp")
    fingerprint["unreadable"] = unreadable
    with open(output_file_name, "w", encoding="utf-8") as file:
        json.dump(fingerprint, file, indent=1, default=str)
    if len(arguments) > 1:
        pmx.save(arguments[1])
    print("TEST PASSED")


main()
