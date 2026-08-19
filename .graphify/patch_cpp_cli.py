"""Teach graphify's C++ extractor to read the C++/CLI wrapper in GmshCommon.

Two independent gaps stop `graphify extract` from seeing the .NET/Gmsh interop
boundary, while the 24 C# files that call across it extract fully:

1. graphify parses C++ with tree-sitter-cpp, which does not implement the
   Microsoft C++/CLI dialect. `public ref class`, `array<T>^`, `double%`,
   `gcnew` and `[assembly:...]` all become ERROR nodes, and since the failure
   lands on the type header the whole class body dissolves with it. That is the
   "3 file(s) had syntax errors and may be partially extracted" warning.
2. Even parsed cleanly, a nested C++ type is dropped: `class Inner { ... };`
   inside a class body is a `field_declaration`, which the extractor consumes as
   a data member and returns from. GmshCommon groups its whole API into nested
   classes (Option, Model, Mesh, Geo, Occ, Logger), so all of it disappears.

graphify exposes no plugin or config hook for either, so this patches the
installed package in place and must be re-run after `uv tool upgrade graphifyy`.
It is idempotent - running it against an already patched (or already fixed
upstream) install is a no-op, and each touched file is backed up once as
`<name>.py.pre-cppcli`.

    python .graphify/patch_cpp_cli.py [path/to/site-packages/graphify]

Re-extract with --force afterwards: cached per-file results are keyed on file
content, not on the extractor's version, so a plain re-run reuses them.
"""
import importlib.util
import os
import sys
from pathlib import Path

CLI_ANCHOR = "def extract_cpp(path: Path) -> dict:"

CLI_CALL_OLD = "    result = _extract_generic(path, _CPP_CONFIG)\n"
CLI_CALL_NEW = (
    "    result = _extract_generic(\n"
    "        path, _CPP_CONFIG, source_override=_cpp_cli_source_override(path)\n"
    "    )\n"
)

CLI_BLOCK = r'''# C++/CLI (`ref class`, `gcnew`, `Type^`, `Type%`, `[assembly:...]`) is a Microsoft
# dialect that tree-sitter-cpp does not implement: every one of those constructs
# becomes an ERROR node, and because `public ref class` fails at the type header
# the whole class body dissolves with it. A .NET interop wrapper is therefore
# extracted down to a handful of recovered fragments (PrePoMax/GmshCommon: 12
# nodes out of 141 static methods) while the C# side that consumes it extracts
# fully, so the graph shows the callers of an interop boundary but not the
# boundary itself.
#
# Normalize the dialect to the nearest standard C++ spelling before parsing.
# The rewrite is BYTE-LENGTH PRESERVING - removed tokens are overwritten with
# spaces, never deleted - so every node offset, line and column still points at
# the same place in the file on disk. It is applied only to files carrying a CLI
# marker, so plain C/C++/CUDA parses byte-for-byte as before.
#
# The result is a parse tree, not a compile: `^`/`%` are rewritten positionally,
# so a genuine XOR or modulo inside a CLI file becomes `*`/`&`. Both are binary
# operators of the same shape, so the expression still parses and the enclosing
# function is still extracted - which is all the extractor reads.
_CLI_MARKER_RE = re.compile(
    rb"\b(?:ref|value)[ \t]+(?:class|struct)\b|\bgcnew\b"
    rb"|^[ \t]*\[[ \t]*assembly[ \t]*:",
    re.MULTILINE,
)
# [assembly:AssemblyTitleAttribute(L"...")]; - a declaration form with no C++
# equivalent. Blanked whole-line (the trailing `;` included) rather than mapped.
_CLI_ASSEMBLY_ATTR_RE = re.compile(rb"^[ \t]*\[[ \t]*assembly[ \t]*:.*$", re.MULTILINE)
# Parameter-position marshalling attributes: `[Out] double% v`. Deliberately
# narrow - a general `\[...\]` sweep would eat array subscripts.
_CLI_PARAM_ATTR_RE = re.compile(
    rb"\[[ \t]*(?:System[ \t]*::[ \t]*Runtime[ \t]*::[ \t]*InteropServices[ \t]*::[ \t]*)?"
    rb"(?:Out|In|Optional|ParamArray)[ \t]*\]"
)
# `public ref class G` -> `class G`. The visibility keyword goes with the CLI
# keyword: `public class` at namespace scope is not valid C++ either, so leaving
# it behind would just move the ERROR one token to the right.
_CLI_TYPE_DECL_RE = re.compile(
    rb"(?:\b(?:public|private|protected)[ \t]+)?\b(?:ref|value|interface)[ \t]+"
    rb"(class|struct)\b"
)
# `public enum class E` / `public class C` - CLI visibility on a type that is
# otherwise spelled the standard way.
_CLI_TYPE_VISIBILITY_RE = re.compile(
    rb"^[ \t]*(?:public|private|protected)[ \t]+(?=(?:class|struct|enum)\b)",
    re.MULTILINE,
)
_CLI_GCNEW_RE = re.compile(rb"\bgcnew\b")
_CLI_HANDLE_TABLE = bytes.maketrans(b"^%", b"*&")


def _cli_blank(match: "re.Match[bytes]") -> bytes:
    return b" " * len(match.group(0))


def _cpp_cli_source_override(path: Path) -> "bytes | None":
    """Standard-C++ rendering of a C++/CLI file, or None to parse it as-is.

    None means "nothing to do": the file carries no CLI marker, or it could not
    be read - in which case `_extract_generic` reads it itself and reports the
    real OSError instead of this pass swallowing it.
    """
    try:
        source = path.read_bytes()
    except OSError:
        return None
    if not _CLI_MARKER_RE.search(source):
        return None
    source = _CLI_ASSEMBLY_ATTR_RE.sub(_cli_blank, source)
    source = _CLI_PARAM_ATTR_RE.sub(_cli_blank, source)
    source = _CLI_TYPE_DECL_RE.sub(
        lambda m: b" " * (len(m.group(0)) - len(m.group(1))) + m.group(1), source
    )
    source = _CLI_TYPE_VISIBILITY_RE.sub(_cli_blank, source)
    source = _CLI_GCNEW_RE.sub(b"new  ", source)
    return source.translate(_CLI_HANDLE_TABLE)


'''

NESTED_ANCHOR = (
    '        if (config.ts_module == "tree_sitter_cpp"\n'
    '                and t == "field_declaration"\n'
    "                and parent_class_nid):\n"
)

NESTED_INSERT = '''            # A nested type - `class Inner { ... };` written inside a class body -
            # parses as a field_declaration whose `type` field IS the
            # class_specifier. The data-member handling below consumes the node
            # and returns, so the nested type, and everything declared in it,
            # never reaches the walk: a wrapper that groups its API into nested
            # classes (GmshCommon: Option, Model, Mesh, Geo, Occ, Logger)
            # contributes nothing but its outer class. Descend first, keeping the
            # enclosing class as the parent so the nested type lands under a
            # `contains` edge, and return unless a member is being declared with
            # it (`class Inner { } inst;`), which the data-member pass still owns.
            _nested_type_node = node.child_by_field_name("type")
            if (_nested_type_node is not None
                    and _nested_type_node.type in config.class_types):
                walk(_nested_type_node, parent_class_nid=parent_class_nid)
                if not list(node.children_by_field_name("declarator")):
                    return
'''

PATCHES = (
    ("extract.py", "_cpp_cli_source_override",
     ((CLI_ANCHOR, CLI_BLOCK + CLI_ANCHOR), (CLI_CALL_OLD, CLI_CALL_NEW))),
    (os.path.join("extractors", "engine.py"), "_nested_type_node",
     ((NESTED_ANCHOR, NESTED_ANCHOR + NESTED_INSERT),)),
)


def locate_package(argv):
    """Directory of the installed `graphify` package."""
    if len(argv) > 1:
        given = Path(argv[1])
        return given.parent if given.is_file() else given
    spec = importlib.util.find_spec("graphify")
    if spec and spec.submodule_search_locations:
        return Path(list(spec.submodule_search_locations)[0])
    roots = (
        Path(os.environ.get("APPDATA", "")) / "uv" / "tools",
        Path.home() / ".local" / "share" / "uv" / "tools",
        Path.home() / ".local" / "pipx" / "venvs",
        Path(os.environ.get("LOCALAPPDATA", "")) / "pipx" / "pipx" / "venvs",
    )
    for root in roots:
        if not root.is_dir():
            continue
        for found in root.glob("*/[Ll]ib/**/site-packages/graphify/extract.py"):
            return found.parent
    return None


def apply(target, sentinel, edits):
    text = target.read_text(encoding="utf-8")
    if sentinel in text:
        print(f"  already patched: {target.name}")
        return True
    for old, _new in edits:
        if text.count(old) != 1:
            print(
                f"  {target}: expected exactly one occurrence of\n"
                f"    {old.splitlines()[0].strip()!r}\n"
                "  upstream changed this code - re-check the patch by hand",
                file=sys.stderr,
            )
            return False
    for old, new in edits:
        text = text.replace(old, new, 1)
    backup = target.with_suffix(".py.pre-cppcli")
    if not backup.exists():
        backup.write_text(target.read_text(encoding="utf-8"), encoding="utf-8")
    target.write_text(text, encoding="utf-8")
    print(f"  patched: {target.name} (backup: {backup.name})")
    return True


def main(argv):
    package = locate_package(argv)
    if package is None or not (package / "extract.py").is_file():
        print(
            "could not locate the installed graphify package - pass its path "
            "as an argument",
            file=sys.stderr,
        )
        return 1
    print(f"graphify package: {package}")
    ok = True
    for relative, sentinel, edits in PATCHES:
        target = package / relative
        if not target.is_file():
            print(f"  missing: {relative}", file=sys.stderr)
            ok = False
            continue
        ok = apply(target, sentinel, edits) and ok
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
