"""Offline Layer 1 API coverage audit.

Checks callable add_* / replace_* parity and controller command declarations against
gateway calls. Known lifecycle/alias exceptions are explicit and printed.
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


def files(root, relative):
    return (root / relative).rglob("*.cs")


def audit(root):
    missing = []
    for path in files(root, "APIs/Python/Model"):
        source = path.read_text(encoding="utf-8-sig")
        methods = set(re.findall(r"public\s+[\w<>\[\],?]+\s+(\w+)\s*\(", source))
        for method in sorted(methods):
            if method.startswith("add_"):
                replacement = "replace_" + method[4:]
                if replacement not in methods:
                    missing.append({"file": str(path.relative_to(root)), "add": method,
                                    "missing_replace": replacement})
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
            "exceptions": EXCEPTIONS}


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
    if result["missing_replacements"] or result["unmapped_controller_commands"]:
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
