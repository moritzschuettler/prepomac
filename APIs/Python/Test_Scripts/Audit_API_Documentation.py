"""Offline audit of the Python API documentation against the C# members it describes.

None of the checks launches PrePoMax; they read the sources under APIs/Python and the hand-maintained
Python_API_Overview.md, and Run_Tests.py runs them before the scripts that need the application.

Arguments
    the Arguments string of every [PythonAPI] member names the C# parameters in their order, with the
    same optional-ness, because pythonnet binds a keyword to the C# parameter name. A string built
    from string constants is resolved first. A string that ends in 'and the arguments of add_x' or
    'and the shared ... arguments' describes only part of the list, so only the arguments it names
    are checked, and the add_x it refers to must exist.

Returns
    a Returns string of str, int, float, bool or None agrees with a C# return type of string, int,
    double, bool or void, both ways.

Overview
    every signature row of Python_API_Overview.md - | `name(arg, other=None, ...)` | - names a member
    of the type its section documents, and every argument the row lists is one that member takes. A
    row whose section names no type is matched by member name; one that still cannot be placed is
    listed, not failed.

    python Audit_API_Documentation.py [--root <repository>]

The process exit code is 0 when nothing drifted.
"""
import argparse
import pathlib
import re
import sys

DEFAULT_ROOT = pathlib.Path(__file__).resolve().parents[3]
OVERVIEW = "Python_API_Overview.md"
API_FOLDER = "APIs/Python"
# A documented argument the C# signature has to declare optional because an optional one precedes it,
# while the member refuses the call without it
REQUIRED_DESPITE_DEFAULT = {
    ("ResultReferencePointsAPI", "replace", "coordinates"): "follows the optional name; refused when not given",
}
# The section headings of the overview whose type is not the heading plus API
SECTION_TYPES = {
    "HistoryFields": "ResultHistoryOutputFieldsAPI",
    "HistoryField": "ResultHistoryOutputFieldAPI",
    "HistoryComponents": "ResultHistoryOutputComponentsAPI",
    "HistoryComponent": "ResultHistoryOutputComponentAPI",
    "HistoryEntries": "ResultHistoryOutputEntriesAPI",
    "HistoryEntry": "ResultHistoryOutputEntryAPI",
    "HistoryComponentData": "ResultHistoryOutputComponentDataAPI",
}
# The section headings of the overview that name no type in backquotes but document one
HEADING_TYPES = {
    "Model item collections": "ModelItemsAPI",
    "Part collections": "PartsAPI",
}
# The first heading of the overview whose types live outside APIs.Python.Model
RESULTS_PART = "## Results collection"
# Python built-ins a row may show beside the members, such as len(collection)
PYTHON_BUILTINS = {"len", "repr", "str", "print", "list", "dict", "iter", "next", "isinstance", "type", "int",
                   "float", "bool", "tuple", "sorted", "hasattr", "getattr", "dir"}
SIMPLE_RETURNS = {"str": {"string"}, "int": {"int", "long"}, "float": {"double", "float"}, "bool": {"bool"},
                  "None": {"void"}}
MODIFIERS = {"public", "internal", "private", "protected", "static", "virtual", "override", "abstract", "sealed",
             "new", "async", "unsafe", "extern", "readonly"}


# C# reading ###########################################################################################################
def mask(source):
    """Returns the source with the inside of every comment, string and character literal blanked.

    Positions are kept, so a match in the masked text is read back from the original one.
    """
    out = list(source)
    i, n = 0, len(source)
    while i < n:
        c = source[i]
        if c == "#" and source[source.rfind("\n", 0, i) + 1:i].strip() == "":
            end = source.find("\n", i)
            end = n if end < 0 else end
            for k in range(i, end):
                out[k] = " "
            i = end
        elif source.startswith("//", i):
            end = source.find("\n", i)
            end = n if end < 0 else end
            for k in range(i, end):
                out[k] = " "
            i = end
        elif source.startswith("/*", i):
            end = source.find("*/", i + 2)
            end = n if end < 0 else end + 2
            for k in range(i, end):
                if out[k] != "\n":
                    out[k] = " "
            i = end
        elif c == "@" and i + 1 < n and source[i + 1] == '"':
            k = i + 2
            while k < n:
                if source[k] == '"' and k + 1 < n and source[k + 1] == '"':
                    k += 2
                elif source[k] == '"':
                    break
                else:
                    k += 1
            for m in range(i + 2, k):
                if out[m] != "\n":
                    out[m] = " "
            i = k + 1
        elif c == '"' or c == "'":
            k = i + 1
            while k < n and source[k] != c:
                k += 2 if source[k] == "\\" else 1
            for m in range(i + 1, k):
                out[m] = " "
            i = k + 1
        else:
            i += 1
    return "".join(out)


def matching(masked, start, opening, closing):
    """Returns the index of the delimiter that closes the one at start."""
    depth = 0
    for i in range(start, len(masked)):
        if masked[i] == opening:
            depth += 1
        elif masked[i] == closing:
            depth -= 1
            if depth == 0:
                return i
    raise ValueError("unbalanced %s at %d" % (opening, start))


def split_top_level(text, masked=None):
    """Splits at the commas outside brackets, generics and literals."""
    masked = mask(text) if masked is None else masked
    parts, depth, start = [], 0, 0
    for i, c in enumerate(masked):
        if c in "([<{":
            depth += 1
        elif c in ")]>}":
            depth -= 1
        elif c == "," and depth == 0:
            parts.append(text[start:i])
            start = i + 1
    parts.append(text[start:])
    return [part.strip() for part in parts if part.strip()]


def unescape(literal):
    if literal.startswith("@"):
        return literal[2:-1].replace('""', '"')
    return bytes(literal[1:-1], "utf-8").decode("unicode_escape")


def evaluate(expression, constants):
    """Evaluates a concatenation of string literals and string constants; None when it is anything else."""
    pieces = split_concatenation(expression)
    value = ""
    for piece in pieces:
        if re.fullmatch(r'@?"(?:[^"\\]|\\.|"")*"', piece, re.S):
            value += unescape(piece)
        elif piece in constants and constants[piece] is not None:
            value += constants[piece]
        else:
            return None
    return value


def split_concatenation(expression):
    masked = mask(expression)
    parts, start = [], 0
    for i, c in enumerate(masked):
        if c == "+":
            parts.append(expression[start:i].strip())
            start = i + 1
    parts.append(expression[start:].strip())
    return parts


class Parameter(object):
    def __init__(self, text):
        text = re.sub(r"^\[[^\]]*\]\s*", "", text.strip())
        self.is_params = text.startswith("params ")
        declaration = text.split("=", 1)[0].strip() if "=" in mask(text) else text
        self.name = declaration.split()[-1].lstrip("@")
        self.optional = "=" in mask(text) or self.is_params


class Member(object):
    def __init__(self, type_name, name, return_type, parameters, attribute, path, line):
        self.type_name, self.name, self.return_type = type_name, name, return_type
        self.parameters, self.attribute = parameters, attribute
        self.path, self.line = path, line

    @property
    def is_method(self):
        return self.parameters is not None

    def where(self):
        return "%s:%d" % (self.path, self.line)


class ApiType(object):
    def __init__(self, name, namespace, bases):
        self.name, self.namespace, self.bases = name, namespace, bases
        self.members = {}


def read_constants(sources):
    """Every string constant of the API sources, resolved through the constants it is built from."""
    expressions = {}
    for source, masked in sources:
        for match in re.finditer(r"\bconst\s+string\s+(\w+)\s*=", masked):
            end = masked.find(";", match.end())
            expressions[match.group(1)] = source[match.end():end]
    constants = dict.fromkeys(expressions)
    for _ in range(len(expressions) + 1):
        for name, expression in expressions.items():
            if constants[name] is None:
                constants[name] = evaluate(expression, constants)
    return constants


def read_attribute(text, constants):
    """Reads [PythonAPI(...)] arguments into a dictionary; an unresolved value is reported as such."""
    values = {}
    for index, argument in enumerate(split_top_level(text)):
        named = re.match(r"^(\w+)\s*=(?!=)\s*(.*)$", argument, re.S)
        key, expression = (named.group(1), named.group(2)) if named and index > 0 else ("Description", argument)
        values[key] = evaluate(expression, constants)
        if values[key] is None:
            values[key] = UnresolvedValue(expression)
    return values


class UnresolvedValue(object):
    def __init__(self, expression):
        self.expression = " ".join(expression.split())


def read_types(root):
    files = sorted((root / API_FOLDER).rglob("*.cs"))
    loaded = []
    for path in files:
        source = path.read_text(encoding="utf-8-sig")
        loaded.append((path, source, mask(source)))
    constants = read_constants([(source, masked) for _, source, masked in loaded])
    types = {}
    for path, source, masked in loaded:
        relative = str(path.relative_to(root)).replace("\\", "/")
        namespace = re.search(r"\bnamespace\s+([\w.]+)", masked)
        namespace = namespace.group(1) if namespace else ""
        for match in re.finditer(r"\b(?:public|internal)\s+(?:(?:abstract|sealed|static|partial)\s+)*class\s+"
                                 r"(\w+)(?:<[^>{]*>)?\s*(?::\s*([^{]*))?\{", masked):
            name = match.group(1)
            bases = [re.sub(r"<.*$", "", base.strip()) for base in split_top_level(match.group(2) or "")]
            api_type = ApiType(name, namespace, bases)
            types.setdefault((namespace, name), api_type)
            body_start = match.end() - 1
            body_end = matching(masked, body_start, "{", "}")
            read_members(api_type, source, masked, body_start + 1, body_end, constants, relative)
    return types, constants


def read_members(api_type, source, masked, start, end, constants, relative):
    """Reads the public members declared directly in a class body."""
    i, depth = start, 0
    statement_start, in_expression_body = start, False
    while i < end:
        c = masked[i]
        if in_expression_body:
            if c == ";":
                in_expression_body = False
                statement_start = i + 1
        elif c == "{":
            if depth == 0:
                declare(api_type, source, masked, statement_start, i, constants, relative)
            depth += 1
        elif c == "}":
            depth -= 1
            if depth == 0:
                statement_start = i + 1
        elif c == ";" and depth == 0:
            declare(api_type, source, masked, statement_start, i, constants, relative)
            statement_start = i + 1
        elif c == "=" and depth == 0 and masked.startswith("=>", i):
            declare(api_type, source, masked, statement_start, i, constants, relative)
            in_expression_body = True
        i += 1


def declare(api_type, source, masked, start, end, constants, relative):
    text, text_masked = source[start:end], masked[start:end]
    # The attributes come first; the declaration follows the last of them
    attribute = None
    position = 0
    while True:
        lead = re.match(r"\s*\[", text_masked[position:])
        if not lead:
            break
        open_at = position + lead.end() - 1
        close_at = matching(text_masked, open_at, "[", "]")
        inner = text[open_at + 1:close_at]
        python_api = re.match(r"\s*PythonAPI\s*\(", mask(inner))
        if python_api:
            arguments_open = python_api.end() - 1
            arguments_close = matching(mask(inner), arguments_open, "(", ")")
            attribute = read_attribute(inner[arguments_open + 1:arguments_close], constants)
        position = close_at + 1
    declaration_masked = text_masked[position:]
    declaration = text[position:]
    words = re.match(r"\s*((?:\w+\s+)*)", declaration_masked).group(1).split()
    if "public" not in words:
        return
    line = source.count("\n", 0, start + position + len(declaration) - len(declaration.lstrip())) + 1
    parenthesis = declaration_masked.find("(")
    if parenthesis >= 0 and "=" not in declaration_masked[:parenthesis]:
        head = declaration_masked[:parenthesis].split()
        if not head or head[-1] in ("operator", "class") or "operator" in head or "class" in head:
            return
        name = head[-1]
        if name == api_type.name:
            return
        return_type = " ".join(word for word in head[:-1] if word not in MODIFIERS)
        close = matching(declaration_masked, parenthesis, "(", ")")
        parameter_text = declaration[parenthesis + 1:close]
        parameters = [Parameter(text) for text in split_top_level(parameter_text,
                                                                   declaration_masked[parenthesis + 1:close])]
        member = Member(api_type.name, name, return_type, parameters, attribute, relative, line)
    else:
        head = declaration_masked.split("=")[0].split()
        if len(head) < 2 or "this[" in declaration_masked or "event" in head or "class" in head:
            return
        name = head[-1]
        return_type = " ".join(word for word in head[:-1] if word not in MODIFIERS and word != "const")
        member = Member(api_type.name, name, return_type, None, attribute, relative, line)
    api_type.members.setdefault(name, []).append(member)


def lookup(types, type_name):
    """Every type of that name, in any namespace."""
    return [api_type for (_, name), api_type in sorted(types.items()) if name == type_name]


def members_of(types, api_type, name, seen=None):
    """The members of that name a type has, its own first and then the inherited ones."""
    seen = set() if seen is None else seen
    if (api_type.namespace, api_type.name) in seen:
        return []
    seen.add((api_type.namespace, api_type.name))
    found = list(api_type.members.get(name, []))
    for base in api_type.bases:
        for base_type in prefer_namespace(lookup(types, base), api_type.namespace):
            found += members_of(types, base_type, name, seen)
            break
    return found


def prefer_namespace(candidates, namespace):
    return sorted(candidates, key=lambda api_type: api_type.namespace != namespace)


# Check a: the Arguments strings ######################################################################################
PARTIAL = re.compile(r"(?:\band\s+)?the\s+(?:shared\b|arguments\s+of\b|same\s+arguments\b)", re.I)
DOCUMENTED_ARGUMENT = re.compile(r"^(\*{0,2})([A-Za-z_]\w*)\s*(?::\s*(?P<type>.*?))?\s*(?P<default>=.*)?$", re.S)


def check_arguments(types):
    issues, checked = [], 0
    for api_type in sorted(types.values(), key=lambda t: (t.namespace, t.name)):
        for name in sorted(api_type.members):
            for member in api_type.members[name]:
                if member.attribute is None or not member.is_method:
                    continue
                arguments = member.attribute.get("Arguments")
                if isinstance(arguments, UnresolvedValue):
                    issues.append((member.where(), name, "Arguments cannot be resolved: " + arguments.expression))
                    continue
                if arguments is None:
                    if member.parameters:
                        issues.append((member.where(), name, "takes %s but carries no Arguments"
                                       % ", ".join(p.name for p in member.parameters)))
                    continue
                checked += 1
                issues += check_member_arguments(types, api_type, member, arguments)
    return issues, checked


def check_member_arguments(types, api_type, member, arguments):
    issues = []
    csharp = [parameter.name for parameter in member.parameters]
    partial = PARTIAL.search(arguments)
    if partial:
        named_part = arguments[:partial.start()]
        for piece in split_top_level(named_part):
            documented = DOCUMENTED_ARGUMENT.match(piece)
            if documented and documented.group(2) not in csharp:
                issues.append((member.where(), member.name, "documents %s, which it does not take"
                               % documented.group(2)))
        for referred in re.findall(r"\barguments\s+of\s+(\w+)", arguments[partial.start():]):
            if not members_of(types, api_type, referred):
                issues.append((member.where(), member.name, "refers to the arguments of %s, which %s does not have"
                               % (referred, api_type.name)))
        return issues
    documented = []
    for piece in split_top_level(arguments):
        match = DOCUMENTED_ARGUMENT.match(piece)
        if not match:
            issues.append((member.where(), member.name, "cannot read the documented argument '%s'" % piece))
            return issues
        documented.append((match.group(2), match.group("default") is not None or bool(match.group(1))))
    names = [name for name, _ in documented]
    if names != csharp:
        issues.append((member.where(), member.name, "documents (%s) but C# takes (%s)"
                       % (", ".join(names), ", ".join(csharp))))
        return issues
    for (name, documented_optional), parameter in zip(documented, member.parameters):
        if documented_optional == parameter.optional:
            continue
        if (member.type_name, member.name, name) in REQUIRED_DESPITE_DEFAULT and not documented_optional:
            continue
        issues.append((member.where(), member.name, "%s is %s in the documentation and %s in C#"
                       % (name, "optional" if documented_optional else "required",
                          "optional" if parameter.optional else "required")))
    return issues


# Check c: the Returns strings ########################################################################################
def check_returns(types):
    issues = []
    for api_type in sorted(types.values(), key=lambda t: (t.namespace, t.name)):
        for name in sorted(api_type.members):
            for member in api_type.members[name]:
                if member.attribute is None:
                    continue
                returns = member.attribute.get("Returns")
                if isinstance(returns, UnresolvedValue):
                    issues.append((member.where(), name, "Returns cannot be resolved: " + returns.expression))
                    continue
                csharp = member.return_type
                if returns is None:
                    if member.is_method and csharp in SIMPLE_RETURNS["None"]:
                        continue
                    if csharp in {cs for values in SIMPLE_RETURNS.values() for cs in values} and csharp != "void":
                        issues.append((member.where(), name, "returns %s but carries no Returns" % csharp))
                    continue
                simple = returns.strip()
                if simple in SIMPLE_RETURNS and csharp not in SIMPLE_RETURNS[simple] and csharp != "object":
                    issues.append((member.where(), name, "Returns says %s, C# returns %s" % (simple, csharp)))
                    continue
                for python, csharp_types in SIMPLE_RETURNS.items():
                    if csharp in csharp_types and not re.match(r"^%s\b" % python, simple):
                        issues.append((member.where(), name, "C# returns %s, Returns says %s" % (csharp, simple)))
    return issues


# Check b: the overview ###############################################################################################
ROW = re.compile(r"^\|\s*(?P<cell>[^|]*?`[^|]*?)\s*\|")
CALL = re.compile(r"`(?P<path>[A-Za-z_][\w.]*)\((?P<arguments>[^`]*)\)`")


def section_type(types, heading, in_results):
    """The API type a ### heading documents, or None when the heading names no type."""
    if heading in HEADING_TYPES:
        return prefer_namespace(lookup(types, HEADING_TYPES[heading]), "APIs.Python.Model")[0]
    title = re.match(r"^`(\w+)`(?:\s*\((model)\))?\s*$", heading)
    if not title:
        return None
    name, qualifier = title.group(1), title.group(2)
    names = [SECTION_TYPES[name]] if name in SECTION_TYPES else [name + "API", "Result" + name + "API"]
    candidates = [api_type for candidate_name in names for api_type in lookup(types, candidate_name)]
    model_side = [t for t in candidates if t.namespace.endswith(".Model")]
    other_side = [t for t in candidates if not t.namespace.endswith(".Model")]
    preferred = model_side + other_side if qualifier == "model" or not in_results else other_side + model_side
    return preferred[0] if preferred else None


def row_arguments(text):
    """The (name, shows a default) pairs of a row, and whether the row leaves arguments out with '...'."""
    arguments, abbreviated = [], False
    for piece in split_top_level(text):
        if piece in ("...", "*", "/") or piece.startswith("*"):
            abbreviated = True
            continue
        name = re.match(r"^([A-Za-z_]\w*)\s*(=.*)?$", piece, re.S)
        arguments.append((name.group(1), name.group(2) is not None) if name else None)
    return arguments, abbreviated


def row_problems(member, arguments, abbreviated):
    """What a row's arguments say that the member does not; an empty string when it agrees."""
    parameters = {parameter.name: parameter for parameter in member.parameters}
    has_params = any(parameter.is_params for parameter in member.parameters)
    unknown = [name for name, _ in arguments if name not in parameters and not has_params]
    if unknown:
        return "lists %s, but it takes (%s)" % (", ".join(unknown), ", ".join(parameters))
    defaulted = [name for name, has_default in arguments if has_default and not parameters[name].optional]
    if defaulted:
        return "shows a default for %s, which it requires" % ", ".join(defaulted)
    shown = {name for name, _ in arguments}
    left_out = [parameter.name for parameter in member.parameters
                if not parameter.optional and parameter.name not in shown]
    if left_out and not abbreviated:
        return "leaves out %s, which it requires, without '...'" % ", ".join(left_out)
    return ""


def check_overview(types, root):
    path = root / OVERVIEW
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    issues, unmapped, checked = [], [], 0
    all_methods = {}
    for api_type in types.values():
        for name, members in api_type.members.items():
            all_methods.setdefault(name, []).extend(member for member in members if member.is_method)
    in_results, in_code, current = False, False, None
    for number, line in enumerate(lines, 1):
        if line.startswith("```"):
            in_code = not in_code
            continue
        if in_code:
            continue
        if line.startswith("## "):
            in_results = in_results or line.strip() == RESULTS_PART
            current = None
            continue
        if line.startswith("### "):
            current = section_type(types, line[4:].strip(), in_results)
            continue
        row = ROW.match(line)
        if not row:
            continue
        calls = list(CALL.finditer(row.group("cell")))
        # A cell that shows one call on a variable - selection.add_by_ids(...) - shows the rest on it too
        on_variable = any("." in call.group("path") for call in calls)
        for call in calls:
            dotted = call.group("path").split(".")
            name = dotted[-1]
            if len(dotted) == 1 and name in PYTHON_BUILTINS:
                continue
            where = "%s:%d" % (OVERVIEW, number)
            arguments, abbreviated = row_arguments(call.group("arguments"))
            if None in arguments:
                issues.append((where, name, "cannot read the arguments of %s" % call.group(0)))
                continue
            checked += 1
            if current is not None and not on_variable:
                members = [m for m in members_of(types, current, name) if m.is_method]
                if members:
                    problems = [row_problems(member, arguments, abbreviated) for member in members]
                    if all(problems):
                        issues.append((where, name, "%s.%s: %s" % (current.name, name, problems[0])))
                    continue
            candidates = all_methods.get(name, [])
            if not candidates:
                issues.append((where, name, "no API type has a member %s" % name))
                continue
            problems = [row_problems(member, arguments, abbreviated) for member in candidates]
            if all(problems):
                issues.append((where, name, "no member %s matches: %s" % (name, problems[0])))
            elif len({member.type_name for member in candidates}) > 1 or current is not None:
                unmapped.append((where, name, "matched by name in %s"
                                 % ", ".join(sorted({member.type_name for member in candidates}))))
    return issues, unmapped, checked


def audit(root):
    types, _ = read_types(root)
    argument_issues, argument_count = check_arguments(types)
    return_issues = check_returns(types)
    overview_issues, unmapped, row_count = check_overview(types, root)
    return {"arguments": argument_issues, "arguments_checked": argument_count, "returns": return_issues,
            "overview": overview_issues, "overview_unmapped": unmapped, "overview_checked": row_count}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=pathlib.Path, default=DEFAULT_ROOT)
    parser.add_argument("--verbose", action="store_true", help="also list the rows matched only by member name")
    args = parser.parse_args()
    result = audit(args.root.resolve())
    print("Arguments strings checked: %d, drifted: %d" % (result["arguments_checked"], len(result["arguments"])))
    for entry in result["arguments"]:
        print("   %s %s: %s" % entry)
    print("Returns strings drifted: %d" % len(result["returns"]))
    for entry in result["returns"]:
        print("   %s %s: %s" % entry)
    print("Overview signature rows checked: %d, drifted: %d, matched only by name: %d"
          % (result["overview_checked"], len(result["overview"]), len(result["overview_unmapped"])))
    for entry in result["overview"]:
        print("   %s %s: %s" % entry)
    if args.verbose:
        for entry in result["overview_unmapped"]:
            print("   (by name) %s %s: %s" % entry)
    print("Exceptions: %s" % ", ".join("%s.%s(%s)" % key for key in sorted(REQUIRED_DESPITE_DEFAULT)))
    if result["arguments"] or result["returns"] or result["overview"]:
        return 1
    print("TEST PASSED")
    return 0


if __name__ == "__main__":
    sys.exit(main())
