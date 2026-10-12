"""Offline API and session journal coverage audit.

Four checks, none of which launches PrePoMax:

Layer 1
    callable add / add_* and replace / replace_* parity on every collection of the API, the model and
    the results side alike, and every controller command declaration reached by a gateway call.
    Known lifecycle and alias exceptions are explicit and printed.

Layer 2
    every concrete command class carries a ToPython override, so a new command cannot be added
    without deciding what it says in Python; and every API member the journal emitters name exists
    on the API, which is what catches a renamed or misspelled member before a journal writes it.
    The exceptions to that check are checked in turn: an entry said to be an argument name must be
    the name of a parameter of an API method, and every entry must still be emitted, so the list
    cannot keep a name the emitters no longer write.
"""
import argparse
import json
import pathlib
import re
import sys

DEFAULT_ROOT = pathlib.Path(__file__).resolve().parents[3]
EXCEPTIONS = {
    "CreateMeshCommand": "controller aliases CreateMeshesCommand",
    "ClearCommand": "document lifecycle command",
    "AddDefaultJobCommand": "application default lifecycle",
    "RunPythonScriptCommand": "embedded script lifecycle",
}
# An add_* that makes something no replace_* could edit, because it is built from other items once
ADD_WITHOUT_REPLACE = {
    "add_compound": "a compound part is built once from the named parts",
    "add_group": "a model tree group is moved and renamed by the tree, not re-specified",
    "add_from_quality": "the element set it adds is replaced by element_sets.replace",
}
# Bases and helpers rather than replayable commands, so they carry no Python form of their own
JOURNAL_EXCEPTIONS = {
    "Command": "the base class, whose ToPython writes the refusal",
    "CommandsCollection": "the recorder rather than a command",
}
# The API files whose add_* members build something no replace_* re-specifies, because they make no model or
# result item: a selection collects picks and IDs, and an annotation is view state that is not recorded
ADD_WITHOUT_REPLACE_FILES = {
    "APIs/Python/Selection/SelectionAPI.cs": "add_by_* and add_all extend a selection under construction",
    "APIs/Python/View/ViewAnnotationsAPI.cs": "an annotation is view state, removed and added again",
}
# The description an EMITTED_NAME_EXCEPTIONS entry carries when it is a parameter name of an API method
ARGUMENT_NAME = "an argument name"
# What a lower case literal in an emitter can be besides an API member: the verb prefixes a member
# name is built from, the root object, the selection and filter vocabulary, and argument names
EMITTED_NAME_EXCEPTIONS = {
    "add_": "the verb a member name is built from",
    "replace_": "the verb a member name is built from",
    "pmx": "the root API object",
    "femodel": "a selection view",
    "edges": "a selection target",
    "geometry_surfaces": "a selection target",
    "axis_direction": "an argument name",
    "axis_point": "an argument name",
    "cavity_name": "an argument name",
    "coefficient_amplitude_name": "an argument name",
    "contact_pair_name": "an argument name",
    "distribution_name": "an argument name",
    "emissivity": "an argument name",
    "emissivity_amplitude_name": "an argument name",
    "film_coefficient": "an argument name",
    "hardness": "an argument name",
    "magnitude": "an argument name",
    "master_region": "an argument name",
    "origin": "an argument name",
    "part_names_before_import": "a variable the import journal binds",
    "point_in_xy": "an argument name",
    "point_on_x": "an argument name",
    "poissons_ratio": "an argument name",
    "pressure_time": "an argument name",
    "pressure_variable_name": "an argument name",
    "reference_point_name": "an argument name",
    "rotational_speed": "an argument name",
    "sink_temperature": "an argument name",
    "slave_region": "an argument name",
    "ur1": "an argument name",
    "ur2": "an argument name",
    "ur3": "an argument name",
    "variables": "an argument name",
    "wear_coefficient": "an argument name",
    "youngs_modulus": "an argument name",
    "zero_temperature": "an argument name",
}


def files(root, relative):
    return (root / relative).rglob("*.cs")


def audit(root):
    missing = []
    for path in files(root, "APIs/Python"):
        relative = str(path.relative_to(root)).replace("\\", "/")
        if relative in ADD_WITHOUT_REPLACE_FILES:
            continue
        source = path.read_text(encoding="utf-8-sig")
        methods = set(re.findall(r"public\s+[\w<>\[\],?]+\s+(\w+)\s*\(", source))
        for method in sorted(methods):
            if (method == "add" or method.startswith("add_")) and method not in ADD_WITHOUT_REPLACE:
                replacement = "replace" + method[3:]
                if replacement not in methods:
                    missing.append({"file": relative, "add": method, "missing_replace": replacement})
    controller = (root / "PrePoMax/Controller.cs").read_text(encoding="utf-8-sig")
    gateway_sources = [p.read_text(encoding="utf-8-sig") for p in files(root, "PrePoMax/Python/Commands")]
    commands = []
    for match in re.finditer(r"public\s+(?:async\s+)?[\w<>\[\],?]+\s+(\w+Command)\s*\(", controller):
        name = match.group(1)
        callers = [source for source in gateway_sources
                   if re.search(r"\." + re.escape(name) + r"\s*\(", source)]
        if not callers and name not in EXCEPTIONS:
            commands.append({"command": name, "line": controller.count("\n", 0, match.start()) + 1})
    return {"missing_replacements": missing, "unmapped_controller_commands": commands,
            "commands_without_to_python": commands_without_to_python(root),
            "emitted_names_not_on_api": emitted_names_not_on_api(root),
            "stale_emitted_name_exceptions": stale_emitted_name_exceptions(root),
            "exceptions": EXCEPTIONS, "add_without_replace": ADD_WITHOUT_REPLACE,
            "add_without_replace_files": ADD_WITHOUT_REPLACE_FILES,
            "journal_exceptions": JOURNAL_EXCEPTIONS,
            "emitted_name_exceptions": EMITTED_NAME_EXCEPTIONS}


def commands_without_to_python(root):
    """Every concrete command class must say what it is in Python."""
    without = []
    for path in files(root, "PrePoMax/Commands"):
        source = path.read_text(encoding="utf-8-sig")
        classes = re.findall(r"(?:sealed\s+)?class\s+(C[A-Za-z0-9_]+)\s*:", source)
        classes = [name for name in classes if name not in JOURNAL_EXCEPTIONS]
        overrides = source.count("public override void ToPython(")
        if overrides < len(classes):
            without.append({"file": str(path.relative_to(root)), "classes": len(classes),
                            "to_python": overrides})
    return without


def api_sources(root):
    return [path.read_text(encoding="utf-8-sig") for path in files(root, "APIs/Python")]


def api_parameter_names(root):
    """The parameter names of every public method of the API, which a keyword argument binds to."""
    names = set()
    for source in api_sources(root):
        for match in re.finditer(r"public\s+[\w<>\[\],?\s]+?\s\w+\s*\(([^)]*)\)", source):
            for parameter in match.group(1).split(","):
                declaration = parameter.split("=")[0].split()
                if declaration:
                    names.add(declaration[-1].lstrip("@"))
    return names


def emitted_names(root):
    """Every lower case string literal of the journal emitters and of the ToPython bodies of the commands."""
    emitted = {}
    sources = list(files(root, "PrePoMax/Python/Journal")) + list(files(root, "PrePoMax/Commands"))
    for path in sources:
        source = path.read_text(encoding="utf-8-sig")
        # A command is only searched inside what it says in Python
        if path.parent.name != "Journal":
            source = "\n".join(re.findall(r"public override void ToPython\(.*?\n        \}", source,
                                          re.S))
        for match in re.finditer(r'"([a-z][a-z0-9_]{2,})"', source):
            emitted.setdefault(match.group(1), str(path.relative_to(root)))
    return emitted


def stale_emitted_name_exceptions(root):
    """An exception said to be an argument name must be one, and every exception must still be emitted."""
    stale = []
    parameters = api_parameter_names(root)
    emitted = emitted_names(root)
    for name, reason in sorted(EMITTED_NAME_EXCEPTIONS.items()):
        if reason == ARGUMENT_NAME and name not in parameters:
            stale.append({"name": name, "problem": "is said to be an argument name, but no API method takes it"})
        elif name not in emitted:
            stale.append({"name": name, "problem": "is no longer emitted, so the exception is not needed"})
    return stale


def emitted_names_not_on_api(root):
    """Every member the emitters name must exist on the API.

    Every lower case string literal in a journal emitter, and in the ToPython body of a command, is
    either an API member, a member name a verb is prefixed to, or one of the argument names and
    vocabulary words the exception list carries. A literal that is none of those is a member that was
    renamed or misspelled, which is what this catches before a journal writes it.
    """
    api_members = set()
    for source in api_sources(root):
        # The declared type may carry spaces of its own - Dictionary<string, int> - and may be
        # preceded by modifiers, so it is read lazily up to the name the parameter list follows
        api_members.update(re.findall(r"public\s+[\w<>\[\],?\s]+?\s(\w+)\s*\(", source))
        api_members.update(re.findall(r"public\s+[\w<>\[\],?\s]+?\s(\w+)\s*\{", source))
    #
    emitted = emitted_names(root)
    #
    unknown = []
    for name in sorted(emitted):
        if name in EMITTED_NAME_EXCEPTIONS:
            continue
        # A type dispatch returns the member name without its verb, so the verb forms count as known
        if any(candidate in api_members
               for candidate in (name, "add_" + name, "replace_" + name, "create_" + name)):
            continue
        unknown.append({"member": name, "file": emitted[name]})
    return unknown


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=pathlib.Path, default=DEFAULT_ROOT)
    parser.add_argument("--json", type=pathlib.Path)
    args = parser.parse_args()
    result = audit(args.root.resolve())
    if args.json:
        args.json.write_text(json.dumps(result, indent=2), encoding="utf-8")
    print("Missing replacements: %d" % len(result["missing_replacements"]))
    print("Unmapped controller commands: %d" % len(result["unmapped_controller_commands"]))
    print("Commands without ToPython: %d" % len(result["commands_without_to_python"]))
    print("Emitted names not on the API: %d" % len(result["emitted_names_not_on_api"]))
    print("Stale emitted name exceptions: %d" % len(result["stale_emitted_name_exceptions"]))
    for entry in result["missing_replacements"]:
        print("   %s has %s but no %s" % (entry["file"], entry["add"], entry["missing_replace"]))
    for entry in result["commands_without_to_python"]:
        print("   %s carries %d command classes and %d ToPython overrides"
              % (entry["file"], entry["classes"], entry["to_python"]))
    for entry in result["emitted_names_not_on_api"]:
        print("   %s is emitted by %s but is not a member of the API" % (entry["member"], entry["file"]))
    for entry in result["stale_emitted_name_exceptions"]:
        print("   the exception %s %s" % (entry["name"], entry["problem"]))
    print("Exceptions: %s" % ", ".join(sorted(list(EXCEPTIONS) + list(ADD_WITHOUT_REPLACE) +
                                              list(JOURNAL_EXCEPTIONS) + list(EMITTED_NAME_EXCEPTIONS))))
    if (result["missing_replacements"] or result["unmapped_controller_commands"]
            or result["commands_without_to_python"] or result["emitted_names_not_on_api"]
            or result["stale_emitted_name_exceptions"]):
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
