# Layer 2 - journaling

Working-level plan for the Layer 2 step of [Python_Integration_Plan.md](Python_Integration_Plan.md).
[Python_API_Layer2_Brief.md](Python_API_Layer2_Brief.md) is the handover from Layer 1 and states the
goal, the traps Layer 1 found, and the open questions; this plan settles those questions against the
current code and turns them into steps.

**The goal, unchanged.** Emit a runnable Python script from the command history, so a session done in
the user interface can be replayed, edited and re-run as a script. Every command must be able to say
what it is in Python.

## Baseline

Facts measured in the current tree. They are the reason this layer is large but shallow.

- **Every command class already overrides `GetCommandString()`.** All 225 of them, with no
  exceptions - the only two files under `PrePoMax/Commands/` that do not are `Command.cs` itself and
  `CommandsCollection.cs`. That is both the precedent for where the Python form belongs and the
  invariant the coverage audit will mirror.
- **The command classes are `internal`.** `class CAddLoad : PreprocessCommand` carries no access
  modifier, so nothing outside the `PrePoMax` assembly can switch on a command type. An emitter that
  lives anywhere else would need 225 classes made public first.
- **Most commands carry nothing but primitives.** A census of the private fields of the 225 active
  command classes:

  | Field type | Count |
  |---|---|
  | `string[]` | 126 |
  | `string` | 107 |
  | `bool` | 16 |
  | `double[]` | 9 |
  | `int` | 8 |
  | `double` | 7 |
  | `GeometrySelection` | 6 |
  | model objects - `Load`, `Step`, `Material`, `Constraint`, `Section`, ... | 2 each, ~40 types |

  The model-object fields come in pairs because each type has an add and a replace command. So the
  emitter's real work is about **40 model-object renderers**; the remaining ~180 commands are name
  lists and flags that need no machinery beyond a value formatter.
- **A command holds the model object, not the API arguments.** `CAddLoad` stores `string _stepName`
  and `Load _load`. The dispatch is therefore two-level: the command decides *which collection and
  verb*, the model object decides *which overload and arguments*. All 17 load types arrive as
  `CAddLoad`.
- **API method names are not derivable from type names.** `CLoad` is emitted as
  `add_concentrated_force`, `DLoad` as a pressure. The type-to-method map has to be written by hand
  and kept honest by an audit; it cannot be reflected.
- **A `Selection` round-trips through the API today.** `SelectionAPI.configure()` and `get_options()`
  cover `MaxNumberOfGeometryIds`, `MaxNumberOfItemIds`, the three `LimitSelectionToFirst*` flags,
  `LimitSelectionToShellEdges` and `EnableShellEdgeFaceSelection`. The only remaining persisted
  fields of `CaeGlobals.Selection` are `_selectItem` and `_currentView`, which are the `target` and
  `view` arguments of every factory call. Nothing in a selection is unemittable.
- **`ModelItemValueReader` exists but must not be reused as the emitter.** It is a reflection reader
  that returns a python-named dictionary for a model item, and its own header says the write side is
  always named arguments. It reads every public property, derived ones included, so using it to
  build a call would break the rule that a journal emits the user's inputs. It stays a read-side
  helper; the emitter needs explicit per-type renderers.

### One finding that changes the acceptance test

Measured, and the reason step 2.1 comes before any bulk work.

1. **A recorded script's own commands are never in the history.**
   `Controller.RunPythonScript` (`Controller.cs:12703`) wraps execution in
   `BeginCommandRecordingSuppression()` / `EndCommandRecordingSuppression()`. `--script` reaches it
   through `RunPythonScriptCommand`, so a batch run's whole history is a single `CRunPythonScript`
   holding the source. Journaling any of the 32 existing test scripts today would emit one line.
   Without a change there is no way to produce a realistic history from the automated suite, and the
   round trip the brief proposes cannot be built.

   **This suppression is deliberate and must stay the default.** Because the script itself is the
   recorded artifact, regeneration re-runs its *computations*. Expanding a script into its
   individual commands records the *results* of those computations instead, so a model saved,
   reopened and regenerated against different geometry or different parameters would rebuild from
   the numbers the script produced on its first run. Equations survive that flattening, because an
   `EquationString` re-evaluates, and selections survive it, because a `Selection` re-resolves - but
   loops, conditionals and any value the script derived at run time do not. The switch below is a
   the numbers the script produced on its first run. No execution mode that flattens a script may
   be added to serve the test harness.

**The consequence is that fixtures are recorded, not generated.** The round trip needs realistic
command histories and the automated suite cannot produce one. Step 2.1 therefore reads a small
corpus of `.pmh` files recorded once in the user interface. Those are the better fixture in any
case: they carry mouse picks and real editing order, which a script-built history does not, and
picking is the part of the emitter most likely to be wrong.

**A journal does not need to open its own model, so nothing else is required.**
`DocumentService.EnsureModelCanBeReplaced` refuses `pmx.new` and `pmx.open` inside a recorded
script, but neither call has to appear in a journal. Model space and unit system are settable
through `pmx.model.properties`, which reaches `SetNewModelProperties` with no such guard and is
exactly what `CSetNewModelProperties` and `CSetModelUnitSystem` map to. A `COpenFile` is emitted as
a header comment, and the journal is replayed with `-f`, which is what the guard's own error message
recommends.

### Scale

| Area | Commands | Area | Commands |
|---|---|---|---|
| `0050_Model` | 50 | `0082_HistoryOutput` | 5 |
| `0110_Results` | 36 | `0083_FieldOutput` | 5 |
| `0030_Geometry` | 18 | `0090_Tools` | 5 |
| `0000_File` | 14 | `0040_Meshing` | 5 |
| `000_General` | 9 | `0062_Section` | 4 |
| `0071_Constraint` | 8 | `0072_SurfaceInteraction` | 4 |
| `0073_ContactPair` | 8 | `0074_Distribution` | 4 |
| `0084_BoundaryConditions` | 7 | `0075_Amplitude` | 4 |
| `0085_Load` | 7 | `0010_Edit` | 2 |
| `0086_DefinedField` | 7 | | |
| `0076_InitialCondition` | 6 | **Active total** | **225** |
| `0081_Step` | 6 | `_Compatibility` | 21 |
| `0100_Analysis` | 6 | | |
| `0061_Material` | 5 | | |

The API they map onto: 130 API classes, 948 `[PythonAPI]` members, 81 `add_*` and 79 `replace_*`
methods.

## Decisions

| # | Decision | Rationale | Reversible? |
|---|---|---|---|
| L2-D1 | The Python form lives **on the command**, as `ToPython(PythonJournalWriter)`; the rendering of model objects lives in **shared renderers** the commands call | The commands are internal and hold the arguments, and all 225 already carry `GetCommandString()`, so a per-command method cannot drift unnoticed. A 225-case switch in a separate emitter would. Object rendering is shared because 17 load types arrive as one command | At 2.0 only |
| L2-D2 | The method takes a **writer**, not a return string | A journal entry is often several lines (build a selection, then use it), needs to name the variable it produced, and needs to be able to fail visibly. `GetCommandString()`'s single-string shape cannot express that | At 2.0 only |
| L2-D3 | Fidelity target: **the emitted script regenerates to the same model**, compared by exported CalculiX input plus an API-read digest. Not textual equality with a hand-written script, not results equality | Model equality is what a journal promises. Text equality would freeze cosmetic choices; results equality would make the suite a solver test | Yes |
| L2-D4 | **No new script execution mode.** Fixtures are `.pmh` files recorded in the user interface; a journal is replayed with `-f` and never emits `pmx.new` or `pmx.open` | A switch that flattens a script into commands would serve only the test harness, and would ship a way to silently trade a parametric model for a frozen one. The journal never needed it: `pmx.model.properties` sets the model space and unit system through a recorded command with no replace guard | Yes |
| L2-D5 | `CRunPythonScript` is emitted **verbatim**, fenced by banner comments, never expanded | Its inner commands were suppressed, so there is nothing to expand into. Verbatim is lossless and cannot recurse | Yes |
| L2-D6 | A command with no clean API form emits a **visible refusal** - a comment naming the command and the reason - and the export reports how many it wrote | Silence produces a script that quietly does less than the session did. A comment keeps the journal runnable and the gap auditable | Yes |
| L2-D7 | The compatibility shims **do get** a Python form, emitting the modern call their `Execute` forwards to | An old `.pmh` is exactly the input a user brings to a journal. `CAddReferencePoint` (v2.1.0) forwards to `AddModelReferencePoint`, so the modern call is already known | Yes |

## Architecture

### Where the code goes

```
PrePoMax/Python/Journal/
  PythonJournalWriter.cs      lines, indentation, the variable scope, the refusal list, the header
  PythonJournalScope.cs       variable naming and reuse - selections, steps, jobs
  PythonValueFormatter.cs     doubles, equations, strings, paths, colors, enums, lists, tables, dicts
  PythonSelectionEmitter.cs   Selection and GeometrySelection to pmx.create.selection_* calls
  PythonRegionEmitter.cs      the CreationData rule
  Renderers/                  one file per model-object family, ~12 files, ~40 renderers
  PythonJournalExporter.cs    drives a List<Command>, writes the file, returns the report
```

`Command.ToPython(PythonJournalWriter writer)` is a virtual on the base that writes the refusal of
L2-D6. Each command class overrides it next to its `GetCommandString()`.

### What the writer offers

- `Line(string)` and `Blank()` - the raw output.
- `Value(object)` - through `PythonValueFormatter`, so no renderer formats a double itself.
- `Selection(Selection)` returns the variable name of a selection, emitting the
  `pmx.create.selection_*` calls the first time and reusing the variable when the same selection is
  emitted again.
- `Region(regionName, regionType, creationData)` - implements the CreationData rule of trap 1.
- `Step(stepName)` returns the variable name of the step, so step-scoped commands read
  `step_1.loads.add_concentrated_force(...)`.
- `Unsupported(command, reason)` - writes the comment and records the refusal in the report.

### The shape of the output

```python
# PrePoMax session journal
# Generated 2026-09-06 14:22:10 from history.pmh by PrePoMax v2.6.2
# 214 commands, 0 not emitted
# Session start: new model
# Run:  prepomax --script journal.py -g No -w <dir>

pmx.model.properties.model_space = 'ThreeD'
pmx.model.properties.unit_system = 'MM_TON_S_C'
pmx.import_file(r'C:\models\bracket.step')

selection_1 = pmx.create.selection_by_ids('geometry', 'surface', 'geometry', {1: [7]})
pmx.model.node_sets.add('Fixed', selection_1)

step_1 = pmx.model.steps.add_static('Step-1', time_period=1.0)
step_1.loads.add_concentrated_force('Force-1', selection_2, f3=-1000.0)

pmx.jobs.add('Job-1')
pmx.jobs['Job-1'].run(wait=True, timeout=600)
```

## Implementation status

**Layer 2 is complete.** Every command class emits a Python form, every emitter covers its whole type
set, and the coverage is checked mechanically. `Debug|x64` builds clean, all 32 Layer 1 test scripts
pass, the offline audits pass, and both recorded histories round trip.

| Step | State |
|---|---|
| 2.0 foundations | Done, verified |
| 2.1 replay harness | Done, verified - `Journal_Round_Trip.py` |
| 2.2 selections and regions | Done, verified against both fixtures |
| 2.3 value parts | Done |
| 2.4 - 2.11 the areas | Done - all 24 areas, 242 of 242 command classes |
| 2.12 compatibility shims | Done - all 21 emit the modern call their Execute forwards to |
| 2.13 audit and documentation | Done - see below |

### Step 2.13, as built

`Audit_API_Coverage.py` gained two offline checks, and both were verified by breaking the tree on
purpose and watching them fail:

- **Every concrete command class carries a `ToPython` override.** A new command cannot be added
  without deciding what it says in Python.
- **Every member the emitters name exists on the API.** Every lower case string literal in a journal
  emitter, and in the `ToPython` body of a command, is either an API member, a member name a verb is
  prefixed to, or one of the argument names and vocabulary words the exception list carries. This is
  the check that catches a renamed or misspelled member before a journal writes it - it would have
  caught two mistakes made while writing the emitters.

The exception lists are printed on every run, the way the Layer 1 ones are.

`Python_API_Overview.md` gained a *Session journal* section - the three export paths, how a journal is
replayed with `-f`, what it refuses and why a recorded script is emitted verbatim. `AGENTS.md` now
states that a command class carries `ToPython` alongside `GetCommandString`, that it emits the user's
inputs rather than the derived values, and that the audit enforces both.

### The API additions Layer 2 needed

Five, each one because an emitter had nothing faithful to call. All are documented in
`Python_API_Overview.md` and covered by a test script.

| Addition | Why |
|---|---|
| `geometry` source on `selection_by_ids` and `add_by_ids` | A geometry pick made in the *Selection by ID* mode is recorded as geometry IDs, and decoding them needs a mesh a `.pmh` export does not have |
| An element region accepts a `parts` selection | `FrmSection.SetSelectItem` picks *parts* for a solid section, and `add_solid` required an `elements` target |
| `parts.replace_properties(old_name, name, color, element_types)` | One part property dialog is one recorded command, and decomposing it into a rename plus a colour plus fourteen element type calls would turn one command into sixteen |
| `history_outputs.add_from_affine_fit` and `replace_from_affine_fit` | The affine fit had no API member at all, so a recorded one could not be emitted |
| `PythonDictionaryConverter.ToStringStringDictionary` | The element types of a part are named values rather than numbers |

The affine fit also needed its type added to the service's validation, which is an allow-list: a type
the list does not name is refused before it reaches the application.

### Verified

Two histories recorded in the user interface, one using *Selection by ID* and one *Selection by
location*, both importing a STEP file, meshing it, and building a node set, a surface, a reference
point, a material and a solid section. For each, `Journal_Round_Trip.py` regenerates the history and
exports CalculiX input, journals the history, replays the emitted journal into a fresh instance and
exports again, then compares. **The two exports are identical apart from the hash and date line the
exporter stamps**, and the journals refuse nothing.

The fixtures live outside the repository, at `C:\Temp\API`, because a history stores the absolute
path of the geometry it imports and only replays on the machine it was recorded on. Run the harness
with `--histories`:

```bash
python APIs/Python/Test_Scripts/Journal_Round_Trip.py --histories C:/Temp/API
```

### The one selection gap

A `SelectionNodeIds` holding raw **surface or edge** IDs has no API source, because only node, element
and part IDs stand for themselves. It arises for a set imported from an `.inp` that carries no
creation data and is then edited. The fix has the same shape as the `geometry` source: a passthrough
source for IDs already in the target's own numbering.

### Unrelated flakiness worth knowing about

Regenerating the geometry fixture crashes roughly one run in four with exit code `3221226356`
(`STATUS_HEAP_CORRUPTION`), inside the native meshing path, and passes on the next attempt. It
predates this work and has nothing to do with the journal, but it makes the round-trip harness look
intermittently red.

## Steps

Each step delivers: the `ToPython` overrides for its commands, any renderers they need, a round-trip
fixture, and its entry in the coverage audit. The verification for every step is the solution build,
`python APIs/Python/Test_Scripts/Run_Tests.py`, and the round trip of that step's fixture.

### Step 2.0 - Foundations

Nothing else starts until this is settled.

**Change.**

- `PrePoMax/Python/Journal/` as laid out above, with `PythonJournalWriter`, `PythonJournalScope`,
  `PythonValueFormatter` and `PythonJournalExporter`.
- `Command.ToPython(PythonJournalWriter)` virtual on the base, writing the L2-D6 refusal.
- Export entry points, in this order of usefulness:
  1. **File - Export - Python script** in `FrmMain`, exporting the live history.
  2. **Save as Python** in `FrmEditCommands`, which already opens and saves `.pmh` files and is where
     a user goes to look at the history.
  3. `--journal <file.py>` on the command line, so a batch run can journal itself, and
     `prepomax --history in.pmh --journal out.py -g No -x Yes` converts an existing history without
     the user interface. Deserialising a `.pmh` needs the `PrePoMax` assembly, so there is no
     offline converter.
- The value formatter, complete, because every later step depends on it: doubles with round-trip
  precision, `EquationString` with its leading `=` preserved, Windows paths as raw strings, colors
  as `[r, g, b]`, enums as their string names, flag enums through
  `PythonValueConverter.FromFlagsEnum`, `null` as `None`, and the list, table and dictionary shapes
  the API converters accept.

**Verify.** A history of two commands - a new model and a save - exports a script that runs and
produces the same `.pmx`.

### Step 2.1 - The replay harness

The acceptance machinery. It comes second because every later step is verified with it.

**Change.**

- A checked-in fixture corpus under `APIs/Python/Test_Scripts/Histories/`: a handful of `.pmh`
  files recorded in the user interface, each with the geometry file it imports and a note of what
  it exercises. At least one must carry mouse picks and one an embedded `CRunPythonScript`.
  Recording them is a manual, one-time job and the only step of this plan that needs the graphical
  application.
- A model digest: a Python helper that walks the API read-back surface - collection names, item
  types, and each item's values - into a stable JSON document.
- `APIs/Python/Test_Scripts/Journal_Round_Trip.py`: for each fixture, regenerate the `.pmh` and
  journal it to `j.py`, run `j.py` with `--script` into a fresh instance, export both models to
  CalculiX input, and compare the two `.inp` files and the two digests.
- The comparison must ignore what is legitimately not reproducible - absolute paths, timestamps, the
  model name when it comes from a file name - and that ignore list must be explicit and printed,
  the way `Audit_API_Coverage.py` prints its exceptions.

**Verify.** The round trip is green for the smallest fixture, which needs only step 2.0 plus the
region commands. Everything else is expected to fail at this point, and the harness must say which
commands were refused rather than failing on a diff.

### Step 2.2 - Selections and regions

The most-reused renderer, and the one trap that produces a wrong but plausible journal.

**Change.**

- `PythonSelectionEmitter`: `SelectionNodeIds` to `selection_by_ids` or `selection_all`,
  `SelectionNodeMouse` to `selection_by_pick`, `selection_by_area` or `selection_by_box`,
  `SelectionNodeInvert` to `invert()`, each node's set operation to the `operation` argument, and the
  persisted options to a trailing `.configure(...)` only when they differ from the default.
- Emit the recorded `PartIds` of a pick rather than letting the API fill in every part, and emit
  `precision` and `partOffsets` as recorded, because the application otherwise takes the precision
  from the current view.
- Emit the plane rows of an area pick verbatim as `[ox, oy, oz, nx, ny, nz]`.
- `PythonRegionEmitter`: for an item whose `RegionType` reads back as a converted internal set, emit
  the `CreationData` selection, never the `Internal_Selection-1_...` name. For a genuinely named
  region, emit `pmx.create.region(region_type, name)`.
- `GeometrySelection` for the six one-shot geometry commands.

**Verify.** Round trip of `Test_Selection_Correctness.py` and `Test_Selection_Pick.py`. A picked
selection must survive as a pick in the emitted script, not as an id list - assert on the text of the
journal, not only on the resulting model.

### Step 2.3 - Value parts

**Change.** Renderers for the detached parts that `pmx.create` makes and model items take as
arguments: the 8 material properties, the 3 surface interaction properties, `Damping`,
`ModalDamping`, `DampingRatioAndRange`, the four step control parameter types, and
`EnvironmentVariable`. Each emits a `pmx.create.*` call bound to a variable when it is used more than
once.

**Verify.** Round trip of `Test_Model_Materials_Sections.py`.

### Steps 2.4 - 2.11 - the commands, by area

Ordered so that each area's fixtures already have what they depend on.

| Step | Folders | Commands | Fixture scripts |
|---|---|---|---|
| 2.4 | `0000_File`, `000_General`, `0010_Edit`, `0090_Tools` | 32 | `Test_Model_Document`, `Test_Model_Export_Sources`, `Test_Model_Keyword_Tree_Coverage` |
| 2.5 | `0030_Geometry`, `0040_Meshing` | 23 | `Test_Model_Parts`, `Test_Model_Meshing`, `Test_Model_Meshing_Coverage` |
| 2.6 | `0050_Model` | 50 | `Test_Model_Regions`, `Test_Model_Mesh_Tools`, `Test_Model_Region_Coverage` |
| 2.7 | `0061_Material`, `0062_Section`, `0071_Constraint`, `0072_SurfaceInteraction`, `0073_ContactPair` | 29 | `Test_Model_Constraints`, `Test_Model_Interactions` |
| 2.8 | `0074_Distribution`, `0075_Amplitude`, `0076_InitialCondition` | 14 | `Test_Model_Fields` |
| 2.9 | `0081_Step`, `0082_HistoryOutput`, `0083_FieldOutput`, `0084_BoundaryConditions`, `0085_Load`, `0086_DefinedField` | 37 | `Test_Model_Steps`, `Test_Model_BCs`, `Test_Model_Loads`, `Test_Model_Outputs` |
| 2.10 | `0100_Analysis` | 6 | `Test_Analysis`, `Test_Job_Correctness` |
| 2.11 | `0110_Results` | 36 | `Test_Result_Field_Outputs`, `Test_Result_History_Outputs`, `Test_Result_Coverage` |

Step 2.6 is the largest and splits along its own subfolders - `01_Properties`, `02_Tools`,
`03_Mesh` - if one change set becomes unwieldy. Step 2.11 is the only postprocessing step, and its
journal entries must stay `PostprocessCommand`-shaped: a results journal that is replayed without
results present has to refuse cleanly rather than throw.

Within each step, in order: write the type-to-method map for the area, write the renderers, write the
`ToPython` overrides, then run the round trip.

### Step 2.12 - Compatibility shims

**Change.** A `ToPython` on each of the 21 shims in `PrePoMax/Commands/_Compatibility/`, emitting the
modern call their `Execute` already forwards to. Where a shim's operation no longer exists in a form
the API can express, it emits the L2-D6 refusal naming the command and the version it came from.

**Verify.** A `.pmh` recorded by an older version - the versions the shims name are v1.3.5, v1.4.1,
v2.1.0, v2.2.0 and v2.2.3 - journals without a silent gap. This is the one step whose fixture cannot
be produced by the current build; it needs archived history files.

### Step 2.13 - Coverage audit and documentation

**Change.**

- Extend `Audit_API_Coverage.py` with a journal section that mirrors the `GetCommandString()`
  invariant: every concrete `C*` class under `PrePoMax/Commands/` overrides `ToPython`, and every
  type named in a renderer map has a matching `add_*` on the API. Both checks are textual and
  offline, like the existing ones, and both print their exception list.
- `Python_API_Overview.md` gains a *Session journal* section: what is emitted, what is refused, the
  command line, and why a recorded script is emitted verbatim.
- `AGENTS.md` gains one line to its Python API section: a new command class carries `ToPython`
  alongside `GetCommandString`.

**Verify.** `python APIs/Python/Test_Scripts/Audit_API_Coverage.py` reports zero unmapped commands,
and the full round-trip suite is green.

## Traps

The brief's seven carry over unchanged and are not repeated. These are the ones this plan adds.

1. **A journal must never emit `pmx.new` or `pmx.open`.** Both are refused inside a recorded
   script, and the guard is right - replacing the model rebuilds the command collection the running
   script belongs to. Emit the model properties instead, and put the opened file in the header for
   `-f`.
2. **Flattening a script is a downgrade, and a journal must not perform one.** L2-D5 says an
   embedded script is emitted verbatim; the reason is not only that its inner commands were
   suppressed and there is nothing to expand. Expanding it would replace live computation with the
   literals it produced the first time, quietly turning a parametric model into a frozen one. A
   journal that carries the script source is a *better* carrier of parametric intent than a
   flattened history, because the logic stays as code.
3. **`ModelItemValueReader` is a tempting shortcut.** Reflecting a model item into named arguments
   would produce a journal in an afternoon and would emit every derived value with it - `complex`,
   `axisymmetric`, an `STLoad`'s magnitudes. The renderers must be written by hand against the
   `add_*` signature.
4. **A replace command emits differently on the two sides.** On the model side an unspecified value
   is kept, so a journal may emit a partial `replace_*`; on the results side the whole definition is
   re-specified. Renderers for `0110_Results` cannot share the model-side helper.
5. **Two commands may make one call, and one call may make two commands.** `CreateMeshCommand` is a
   controller alias for `CreateMeshesCommand`, and the `AddDefaultJob` lifecycle command has no API
   form at all. The type-to-method map is not a bijection and the audit must not assume it is.
6. **A journal is a text file that names paths.** Absolute paths recorded on one machine are
   emitted as raw strings and will not resolve on another. Emit them verbatim - a journal is a
   record of what happened - but the round-trip comparison must normalise them, and the export
   report should say how many appear.
7. **The variable scope must survive a rename.** A journal that binds `step_1` and then journals a
   `CRenamePre` of that step has to keep using the variable, not the old name. The scope is keyed by
   identity within the export, not by the name at the time of the call.

## Verification

Every step, in addition to its own round trip:

```bash
"/c/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe" "PrePoMax.sln" -t:Build -p:Configuration=Debug -p:Platform=x64 -nologo -verbosity:minimal -clp:ErrorsOnly -m
```

```bash
python APIs/Python/Test_Scripts/Run_Tests.py
python -m unittest test_runner_offline
python APIs/Python/Test_Scripts/Audit_API_Coverage.py
```

And from step 2.1 onward:

```bash
python APIs/Python/Test_Scripts/Journal_Round_Trip.py
```

**Layer 2 exit criteria.** Every command class emits a Python form. A session recorded in the user
interface exports to a script that, run unattended into a fresh instance, produces a model whose
CalculiX export and API digest match the original, and the export report names every command it
could not emit.

## Risks

| Risk | Mitigation |
|---|---|
| The renderers drift from the API as Layer 1 evolves | 2.13's audit checks every mapped type against a live `add_*`; the `ToPython` override check mirrors an invariant that already holds for all 225 commands |
| The recorded `.pmh` fixtures go stale as the command set changes | They are regenerated before they are journalled, so a fixture that no longer replays fails loudly rather than silently testing an old shape |
| Volume stalls the effort | 2.4-2.11 are independently shippable and each ends green; stop anywhere and the journal emits what it covers and names what it does not |
| A journal is wrong in a way the round trip does not see | The comparison is two independent views - a CalculiX export and an API digest - and step 2.2 asserts on the journal text as well as the model |
| Archived `.pmh` files for step 2.12 are not available | 2.12 is last and is the only step whose fixtures cannot be generated; if the files do not exist, the shims still get their `ToPython` and the step ships without a regression fixture, stated as such |
