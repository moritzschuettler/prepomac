# Layer 2 - journaling: session brief

Layer 1 carries every command a user can issue, and this brief is the handover for the Layer 2
work, so that a session can start from it without rereading the whole history. "Complete" here means
that a path exists for every command; it does **not** mean that every argument variant and every
stored definition behind those paths has been proved. See "What Layer 1 settled, and how far" below
before treating a green audit as an acceptance criterion.

## The goal

Emit a runnable Python script from the command history. Every `Command` gains a way to say what it is
in Python, so that a session done in the user interface can be replayed, edited and re-run as a
script - the same relationship the `.pmh` history already has to a `.pmx`, but readable and editable.

The user's requirement, in their words: *"at the end I would like all of commands to have ToPython
string so i need all of them to re implemented"*. That is why Layer 1 exposed every command rather
than a convenient subset.

## What Layer 1 already settled

**Every command a user can issue has an API path.** That was the precondition for Layer 2 and it now
holds: 143 API classes, 930 `[PythonAPI]` members, 32 test scripts green.
`APIs/Python/Test_Scripts/Audit_API_Coverage.py` is the structural check behind that claim - it
reports an `add_*` without its matching `replace_*`, and a controller command no gateway calls. It is
a structural check, not a behavioural one: it proves a path exists, not that the path carries the
whole definition.

**What the audit does not prove**, and what Layer 2 must therefore not assume:

- It checks names and gateway calls, not the **argument variants** of a callable, so a command whose
  API form takes only some of the definitions its dialog offers still passes.
- It does not compare a **stored definition** against the one that was asked for, so a value silently
  clamped or a construction mode left stale still passes. Two such cases were found and fixed after
  the audit was already green - a result reference point that kept `OnPoint` after being given
  coordinates, and layered mesh inputs that were clamped rather than refused.
- It says nothing about the **compatibility commands** in `PrePoMax/Commands/_Compatibility/`. They
  are no longer created, but an old `.pmh` still loads them, so a journal must either map them or
  refuse the export with a message that names the command. Being unreachable from the API is not a
  reason to ignore them.

**Every selection node kind now has an API form**, added after Layer 1 specifically so that Layer 2
has something faithful to emit for a selection made with the mouse. Without them a journal would have
degraded every such selection to a list of IDs and lost its ability to survive a mesh change.

| Node | API form |
|---|---|
| `SelectionNodeIds` | `pmx.create.selection_by_ids(view, target, source, ids, geometry_select_mode)` |
| `SelectionNodeIds`, select all | `pmx.create.selection_all(view, target)` and `selection.add_all(operation)` |
| `SelectionNodeMouse`, point | `pmx.create.selection_by_pick(view, target, point, select_by, angle, direction, part_ids, completely_inside, precision, part_offsets)` |
| `SelectionNodeMouse`, area | `pmx.create.selection_by_area(view, target, planes, select_by, ...)`, and `selection_by_box(...)` for the axis aligned case |
| `SelectionNodeInvert` | `selection.invert()` |
| The set operation of a node | the `operation` argument of every `add_by_*`: `Add`, `Subtract`, `Intersect` or `None` |

A pick and an invert are resolved by the application through
`IPythonSelectionGateway.ResolveSelectionIds`, because only it can turn a point into IDs or know the
complement of a selection.

Facts about picking that a journal has to respect:

- It **works with the window hidden**, in `-g No`, so an unattended replay of a journalled pick is
  fine. This was measured, not assumed.
- **Only the parts a pick names are searched.** A `SelectionNodeMouse` with null `PartIds` finds
  nothing at all, silently. The API fills in every part of the mesh when the caller gives none, so a
  journal that emits a pick without `part_ids` still resolves; a journal that emits the recorded
  `PartIds` is more faithful and should prefer them.
- **The nearest entity is picked**, so a point pick never fails for a point that misses the model. A
  journal cannot rely on an error to catch a coordinate it emitted wrongly. An area pick is the
  opposite: it selects only what its planes contain, and reports when that is nothing.
- **An area pick's plane normals point out of the volume**, which is what `vtkPlanes` and
  `vtkExtractSelectedFrustum` expect. `PlaneParameters` rows are `[ox, oy, oz, nx, ny, nz]` and can be
  emitted verbatim.

**The exposability rule**, formulated with the user and applied throughout:

> A command is exposable when its arguments are the user's primary inputs. Anything derived must be
> derived at or below the controller, never in the form.

Applying it re-audited an earlier deferral list and five of six deferrals turned out to be premature.
Layer 2 inherits the rule directly: **a journal entry must emit the user's inputs, not the derived
values**, or the emitted script will not reproduce the session on a different mesh.

## Scale

226 command classes, plus 21 compatibility shims that never need `ToPython` because nothing creates
them any more. The largest groups are `0050_Model` (50), `0110_Results` (36), `0030_Geometry` (19)
and `0000_File` (14).

## Traps Layer 1 found that Layer 2 will hit

These are the ones that will produce a *wrong but plausible* journal if missed.

1. **A selection based region does not survive as a selection.** The controller converts it into an
   internal node set, element set or surface at add time, so a boundary condition added from a
   selection reads back with `region_type == 'NodeSetName'` and an internal name like
   `Internal_Selection-1_...`. A journal that emits the converted region name produces a script that
   depends on a set the script never creates. It must emit the **`CreationData` selection** instead.
2. **Tree items and one-shot operations differ in when they re-resolve, not whether.** Both store a
   `Selection`; a tree item is reselected when the mesh changes, a one-shot operation re-resolves at
   replay. Two one-shot commands were replaying from stored IDs and were fixed with a compatibility
   shim - see the plan's "Two classes of selection-bearing command".
3. **Derived values must not be emitted.** `complex` comes from the step type, `axisymmetric` from the
   model space, a cyclic symmetry's `sector_angle_deg` from its sector count, an `STLoad`'s magnitudes
   from its components. Emitting them is both noise and a future contradiction.
4. **Equations need a leading `=`.** `PythonValueConverter.ToEquationString` adds it. A journal that
   emits a bare `t * 2` writes a value, not an equation.
5. **`CRunPythonScript` is a `GeneralCommand`** and holds the whole script source. Journaling must not
   re-journal it into itself; decide early whether a recorded script is emitted verbatim or expanded.
6. **Replace semantics differ by side.** On the model side a value that is not given is kept, so a
   journal can emit a partial replace. On the results side a replace re-specifies the whole
   definition. A journal must not assume one convention.
7. **Some model setters silently drop what they are given** - `ContactPair.SmallSliding` is cleared by
   `SurfaceToSurface`, a cyclic symmetry's expansion region is discarded below two graph sectors. The
   API rejects those combinations; a journal that emits them would write a script that does less than
   the session did.

## Questions to settle before writing code

- **Where does `ToPython` live?** On the `Command` classes (they hold the arguments, but `PrePoMax`
  would then own Python syntax), or in a separate emitter that switches on command type (keeps the
  commands clean, but drifts from them). The command pattern's own precedent is `GetCommandString()`,
  which lives on the command.
- **What is the fidelity target?** A script that reproduces the model, or a script that reproduces
  the model *and* re-runs identically under regeneration? The second is stricter and is what the
  replay round trips already prove for the API path.
- **How is a selection serialised?** Every node kind now has an API form, including the area
  `planeParameters`, the invert, the select all, and the set operation each node carries, so this is
  a mapping question rather than an open one. The emitter writes the recorded operation sequence
  rather than an algebraically equivalent one, because the equivalence depends on what is visible.
- **What happens to a command with no clean API form?** Refuse, emit a comment, or fall back to
  `GetCommandString()`. Whatever it is, it should be visible in the output rather than silent.
- **What does a journal do with `partOffsets` and `precision`?** Both are now arguments of
  `selection_by_pick`, so a journal emits what was recorded. They are not a deformed result special
  case: the application takes the precision from the current view the first time **any** geometry
  pick is resolved, so a pick that omits it resolves differently after a zoom.

## Where things are

| What | Where |
|---|---|
| Strategic plan, decisions D1-D5, cross-cutting rules | `Python_Integration_Plan.md` |
| Layer 1 working plan, every step's outcome and the traps | `Python_API_Layer1_Plan.md` |
| API reference, kept in sync and mechanically checked | `Python_API_Overview.md` |
| The API itself | `APIs/Python`, model tree under `APIs/Python/Model` in namespace `APIs.Python.Model` |
| Gateways - the only way `APIs` reaches the application | `APIs/Python/Commands/IPython*Gateway.cs` |
| The commands | `PrePoMax/Commands/`, numbered by menu order |
| Test scripts and their runner | `APIs/Python/Test_Scripts/`, `Run_Tests.py` |

## Verification

```bash
"/c/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe" "PrePoMax.sln" -t:Build -p:Configuration=Debug -p:Platform=x64 -nologo -verbosity:minimal -clp:ErrorsOnly -m
```

```bash
python APIs/Python/Test_Scripts/Run_Tests.py
```

All of them must pass. The runner reads each script's header for its model and expected exit code,
and checks the exit code, the run report and the `TEST PASSED` marker. A header that declares neither
a command line nor an expected exit code is a runner failure rather than a silently skipped test.

```bash
python -m unittest test_runner_offline
python APIs/Python/Test_Scripts/Audit_API_Coverage.py
```

The first checks the runner itself and the second the structural coverage; neither launches PrePoMax.

A natural Layer 2 acceptance test extends that: run a script, journal the resulting history back to
Python, run the emitted script into a fresh model, and compare the two models.
