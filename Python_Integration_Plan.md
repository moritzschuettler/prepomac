# PrePoMax Python Integration Plan

Staged plan for extending Python support from the current post-processing surface to the whole
application. Two layers are in scope here:

- **Layer 0 - agent-grade batch contract.** Make an unattended PrePoMax run observable and safe to
  drive from a script or an AI agent.
- **Layer 1 - the automation API.** Expose the full model-building surface through the same command
  path the desktop UI uses.

Deferred to separate plans, but their hooks are preserved throughout: **Layer 2 journaling** (emit a
Python script from the command history) and **Layer 3 RPC server** (a second front door onto Layer 1).

## Decisions taken

| # | Decision | Rationale | Reversible? |
|---|---|---|---|
| D1 | Layer 0 lands before Layer 1 | Layer 0 is small and is what makes every Layer 1 slice verifiable unattended | n/a |
| D2 | API shape: one typed `*DraftAPI` per creatable model type, over a **generic** name-keyed collection base | Keeps the existing pattern's autocomplete and `help()`; the generic base removes ~150 hand-written collection classes | At step 1.0 only |
| D3 | Scope: pre-processing + analysis + results. View/camera and application settings only where a script needs them for reproducibility | Keeps the surface bounded; view scripting has no effect on model correctness | Yes, additive |
| D4 | JSON is emitted by a small write-only serializer in `CaeGlobals`, not a NuGet package | `AGENTS.md` discourages new packages; a write-only emitter is ~120 lines. Revisit when Layer 3 needs parsing | Yes |
| D5 | True headless (no `FrmMain`) is **not** attempted | `Controller(FrmMain)` with 662 `_form.` call sites; `-g No` already runs minimized and hidden | n/a |

**D5 consequence:** an unattended run still requires an interactive Windows desktop session. It works
for an agent running on the user's machine or an autologon VM; it will not work in session 0, a
service, or a container.

## Cross-cutting rules

Every step in this plan obeys these. They are not repeated per step.

1. Any state change goes through `Controller.XxxCommand(...)` so it is recorded, replayable, and
   saved into the `.pmx`. Never call the plain `Controller.Xxx(...)` from API code.
2. The API project reaches the application only through the gateway interfaces in
   `APIs/Python/Commands/`. `APIs` must never reference `PrePoMax`.
3. Conversion and validation complete **before** the gateway is called, so a rejected call leaves no
   partial command in the history.
4. Every added or changed member carries `[PythonAPI]` metadata and a matching entry in
   `Python_API_Overview.md`, in the same change.
5. Every step adds or extends a script in `APIs/Python/Test_Scripts/`.
6. C# 7.3 baseline in `PrePoMax` and `APIs`; 133-column limit; padded section banners.

Build verification for any step:

```bash
"/c/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe" "PrePoMax.sln" -t:Build -p:Configuration=Debug -p:Platform=x64 -nologo -verbosity:minimal -clp:ErrorsOnly
```

---

# Layer 0 - agent-grade batch contract

Six independent steps. None depends on Layer 1, and each is separately verifiable. Total size is
small - this is the cheapest high-value work in the plan.

## Implementation status - done

All six steps are implemented. `Debug|x64` builds clean and the contract was exercised against the
built application.

| Step | State | Note |
|---|---|---|
| 0.1 exit codes | Done | Verified: 0, 1, 3, 4. Code 5 is implemented but see below |
| 0.2 `--script` | Done | Runs after the `-f` file is opened, recorded as one history command, `sys.argv` populated |
| 0.3 strict prompts | Partly verified | Decision prompts abort; single button notifications are recorded and do not abort |
| 0.4 `--report` | Done | Written on success, script failure and timeout |
| 0.5 run isolation | Done | Two concurrent runs verified; `settings.bin` unchanged after all runs |
| 0.6 timeout | Done, one deferral | `--timeout` verified end to end; the blocking job API call belongs to step 1.9 |

Deliberate deviations from the plan as written:

- **0.6.** The plan said to wire a blocking job path "through to the API surface". There is no job API
  until step 1.9, so only the run-level watchdog and the `JobStatus` reporting landed here. The
  blocking `job.run(wait=True, timeout=...)` call is part of 1.9, and reuses the same existing
  synchronous path (`Controller.PrepareAndRunJob(..., asynchronous: false)`).
- **0.4.** A command line or option error exits with code 1 without writing a report. At that point
  the report path itself has not been validated, so there is nowhere reliable to write it.
- **0.3.** The refusal path (exit code 5) could not be exercised at runtime: every decision dialog in
  the current code needs a model, a running solver, or a recovery file, and none of those can be
  produced from a script before Layer 1 exists. It should be re-checked during step 1.1.

Files added: `CaeGlobals/JsonWriter.cs`, `CaeGlobals/PromptRefusedException.cs`,
`CaeGlobals/CommonClasses/MessageBoxMode.cs`, `CaeGlobals/CommonClasses/MessageBoxPrompt.cs`,
`PrePoMax/CommandLine/ExitCode.cs`, `PrePoMax/CommandLine/BatchRunReport.cs`,
`PrePoMax/Python/PythonScriptException.cs`, `APIs/Python/Test_Scripts/Test_Batch_Run.py`.

The command line surface is documented in `Python_API_Overview.md`, section
*Unattended script execution*.

## Step 0.1 - Meaningful exit codes

**Problem.** `PrePoMax/Program.cs:62` and `:132` both end in `Process.GetCurrentProcess().Kill()`.
Success and failure terminate identically, so a caller cannot tell them apart without scraping
stdout for `"Process finished successfully."`.

**Change.** Set the process exit code explicitly before the self-kill on both paths. Define the code
table:

| Code | Meaning |
|---|---|
| 0 | Completed, no errors |
| 1 | Command-line / option error |
| 2 | Model or regeneration error (a `CaeException` reached the top) |
| 3 | Script error (unhandled Python exception) |
| 4 | Timed out |
| 5 | Refused an interactive prompt in strict mode (see 0.3) |

**Verify.** `prepomax -r good.pmx -g No -x Yes` returns 0; a deliberately broken `.pmx` returns 2.

## Step 0.2 - `--script` entry point

**Problem.** `PrePoMax/CommandLine/CommandLineOptions.cs:227` rejects `-g No` unless `-r` is given:
*"The No GUI switch can only be used for regeneration."* There is no way to run a script unattended.

**Change.**

- Add `--script <file.py>` to `CommandLineOptions`, and allow `-g No` when either `-r` or `--script`
  is present. Update `CheckForErrors` accordingly.
- In `FrmMain`, after startup completes, run the script through the recorded path
  (`Controller.RunPythonScriptCommand`) so the session stays replayable.
- Honour `-x`/`ExitAfterRegeneration` for the script path too, reusing `_closeAfterRegeneration`.
- Add `--scriptArgs`, surfaced to the script as `sys.argv`.

**Verify.** A script that opens a model, prints the part count, and exits, run with `-g No -x Yes`,
produces the expected stdout and exit code 0.

## Step 0.3 - Strict non-interactive mode

**Problem.** In `-g No`, `CaeGlobals/MessageBoxes.cs:56` prints the prompt and returns the *default
affirmative* - `Yes` for `YesNo`, `OK` for `OKCancel`. An unattended run silently accepts overwrite
and discard prompts, and the caller gets no signal that a decision was made for it.

**Change.**

- Add a third `MessageBoxes` mode alongside interactive and auto-answer: **strict**, which records
  the prompt and throws a dedicated exception, aborting the run with exit code 5.
- Add `--onPrompt <fail|accept>`, defaulting to **fail** for `--script` and **accept** for `-r`,
  preserving today's regeneration behaviour.
- Record every prompt encountered - text, caption, buttons, answer - into the run report (0.4), in
  both modes.

**Verify.** A script that triggers an overwrite prompt aborts with code 5 under `--onPrompt fail`
and completes under `accept`, with the prompt listed in the report either way.

## Step 0.4 - Structured run report

**Problem.** All output is free-form console prose. An agent has to parse English.

**Change.**

- Add a minimal write-only JSON emitter to `CaeGlobals` (per D4).
- Add `--report <file.json>` containing:
  - invocation: arguments, version, work directory, start/end time, exit code;
  - commands executed, with name and duration (`Command.Name` and `Command.TimeSpan` already exist);
  - warnings and errors, each with its source;
  - prompts encountered (0.3);
  - job results: name, `JobStatus`, solver time, result file paths;
  - artefacts written: `.pmx`, `.inp`, `.frd`, exported files.
- Write the report even on failure, in a `finally`.

**Verify.** A failing run still produces a well-formed report whose `exitCode` matches the process.

## Step 0.5 - Run isolation

**Problem.** `PrePoMax/Settings/SettingsContainer.cs:206` and `:225` read and write `settings.bin`
in `Application.StartupPath` - the shared install directory. Parallel agent runs race on it, and one
run can permanently change the interactive user's settings.

**Change.**

- Add `--settings <file>` to load settings from an explicit path, and `--noSettingsSave` to suppress
  writing them back. Default both on for `--script`.
- Require an explicit `-w`/`--workDirectory` for `--script` runs, or default to a per-run temp
  directory, so meshing scratch files (`geometry.brep`, `gmshData`, `mesh.inp`, ...) cannot collide.
- Confirm no other shared-state writes to `StartupPath` remain on the batch path (`materials.lib`,
  `history.pmh`, `Temp\`).

**Verify.** Two concurrent `--script` runs on the same install complete without interfering, and the
interactive `settings.bin` is byte-identical afterwards.

## Step 0.6 - Blocking job execution with timeout

**Problem.** The solver is asynchronous. A script has no way to wait for it, and a wedged solver
hangs the process forever.

**Change.**

- `Controller.PrepareAndRunJob(jobName, onlyCheckModel, asynchronous)` already accepts a synchronous
  flag, and `AnalysisJob.Submit(..., useBackgroundWorker: false)` already runs inline - wire a
  blocking path through to the API surface rather than adding a new mechanism.
- Add `--timeout <seconds>` for the whole run: on expiry, kill the running solver, write the report,
  exit with code 4.
- Surface `JobStatus` (`OK`, `Killed`, `TimedOut`, `Failed`, `FailedWithResults`) verbatim in the
  report; do not collapse it to a boolean.

**Verify.** A job that exceeds `--timeout` terminates the solver process, writes a report with
`TimedOut`, and exits 4 without leaving an orphaned `ccx` process.

**Layer 0 exit criteria.** An agent can run
`prepomax --script build.py -g No -w <dir> --report r.json --timeout 600`, branch on the exit code,
and read the outcome from `r.json` without parsing prose.

---

# Layer 1 - the automation API

**The working-level plan is [Python_API_Layer1_Plan.md](Python_API_Layer1_Plan.md).** It carries the
folder layout, the gateway split, the collection and draft contracts, the per-step type lists and the
acceptance tests. The summary below stays as the strategic view.

Roughly 220 `Controller.XxxCommand(...)` methods exist across 43 menu regions; the current API covers
the result field/history outputs and selection only. The work is high-volume but mechanical - the
architecture does not need to change. The creatable **model** type count is about 88, not the ~150
suggested by the `View*` wrapper count, which is what makes typed drafts affordable.

Steps 1.1 onward follow the order a model is actually built, so each step is usable on its own and
the test scripts accumulate into a full workflow.

## Step 1.0 - Foundations

Everything else depends on this step. Do not start 1.1 until it is settled.

1. **Project references.** `APIs.csproj` currently references `CaeGlobals`, `CaeMesh`, `CaeResults`
   only. Add `CaeModel` and `CaeJob`. Confirm no reverse reference to `PrePoMax` is introduced.
2. **Generic collection base.** `ModelItemsAPI<TItem>` in `APIs/Python/Model/Common/`, providing
   `names()`, `__getitem__`, `__len__`, `__iter__`, `get`, `remove`, `rename`, `duplicate`,
   `activate`/`deactivate`, `hide`/`show`. One concrete subclass per model category (~20), not per
   model type (~150).
3. **Draft base.** `ModelItemDraftAPI` mirroring the existing `ResultFieldOutputDraftAPI` shape:
   immutable, validated at construction, `CreateBase()` returns the `CaeModel` object.
4. **Gateway split.** Keep `IPythonCommandGateway` for results; add `IPythonModelGateway`,
   `IPythonMeshGateway`, and `IPythonJobGateway` rather than growing one interface to 220 members.
   Implement each in `PrePoMax/Python/Commands/`.
5. **Region abstraction.** Model items reference regions as `(RegionName, RegionTypeEnum)`, not as
   id lists. Add a `RegionAPI` that accepts a named set/surface/part or a `SelectionAPI` and resolves
   to that pair. Sets and surfaces themselves carry `Selection CreationData`, so selection-built
   regions stay regenerable - preserve that, do not resolve to ids early.
6. **Equations and units.** User-facing numeric inputs are `EquationString`/`EquationContainer` with
   unit-aware converters, never plain `double`. Define the Python convention once - a `float` is
   taken in current model units, a `str` is parsed as an equation - and wire it through one helper.
7. **Naming convention.** Fix and document the C# to Python name mapping (`snake_case`), the
   enum-as-string convention, and the `create_*` / `add` / `replace` / `remove` verb set. Every later
   step follows it.
8. **Extend `SelectionFactoryAPI`** from result views to the `geometry` and `femodel` views, and add
   the non-id selection nodes the UI uses.

**Verify.** Solution builds; a test script creates a selection in the model view, builds a region
from it, and reads it back.

## Steps 1.1 - 1.9 - the surface, in model-build order

Each step delivers: draft classes for its types, a collection class, gateway methods, `[PythonAPI]`
metadata, a `Python_API_Overview.md` section, and a test script.

| Step | Area | Controller regions | Notable content |
|---|---|---|---|
| 1.1 | Document and application | File menu, Parameters, Settings | open/save/import/export `.pmx`/`.inp`/`.stl`/`.step`, unit system, `EquationParameterCollection` |
| 1.2 | Geometry and parts | Geometry part / CAD part / STL part, Model part | import, transform, split, defeature, part properties |
| 1.3 | Meshing | Mesh, Mesh setup items | mesh parameters, local sizes, mesh setup items, remesh; long-running, so use the 0.6 blocking path |
| 1.4 | Regions | Node set, Element set, Surface, Reference point, Coordinate system | built on the 1.0 region/selection plumbing |
| 1.5 | Materials and sections | Material, Section | 8 material property types; 7 section types |
| 1.6 | Field definitions | Amplitude, Distribution, Initial condition | 3 distribution types, tabular amplitudes, 3 initial-condition types |
| 1.7 | Interactions | Constraint, Surface interaction, Contact pair | 10 constraint types; master/slave region pairs |
| 1.8 | Steps and step content | Step, BC, Load, Defined field, Field output, History output | the largest slice: ~12 step types, 5 BC types, ~20 load types |
| 1.9 | Analysis | Analysis menu | job create/replace/run/monitor/kill, `JobStatus`, result opening |

Ordering notes:

- 1.4 must precede 1.5-1.8, since sections, constraints, BCs and loads all reference named regions.
- 1.8 is large enough to split by sub-area (steps, then BCs, then loads, then outputs) if a single
  change set becomes unwieldy.
- 1.9 depends on Layer 0 step 0.6 for the blocking and timeout semantics.

## Step 1.10 - Reconcile the existing results API

The results, mesh and selection APIs already exist and were written before the model surface. Once
1.0-1.9 land, align them: same naming convention, same region abstraction, same collection base.
Preserve the current public names where they are already documented, or provide aliases - existing
user scripts must keep working.

## Step 1.11 - Documentation and test suite

- `Python_API_Overview.md` brought to full coverage and checked against the `[PythonAPI]` members.
- A `Test_Scripts` suite that builds, meshes, solves and post-processes a small model end to end,
  runnable through Layer 0's `--script` with a report assertion.

**Layer 1 exit criteria.** A single Python script, run unattended, can go from an imported CAD file
to extracted result values, and the `.pmx` saved at the end regenerates to the same model.

---

# Deferred, with hooks preserved

**Layer 2 - journaling.** Add `GetPythonString()` alongside the existing `Command.GetCommandString()`
on each `C*` class, and a "Save session as Python" action. Depends on Layer 1's vocabulary being
stable, because the emitted script must be exactly what a user would have written. This is the
cheapest way to make the API self-documenting, and the main thing that makes an agent write correct
scripts.

**Layer 3 - RPC server.** JSON-RPC 2.0 over a named pipe, ACL-restricted to the current user, with an
optional `http://localhost:<port>/` front onto the same dispatcher. Every handler marshals to the UI
thread via `Invoke` and calls the **same** Layer 1 API objects - it is a second front door, never a
parallel implementation. Long operations return a job id and are polled. Bulk arrays are returned as
a temp `.npy` path rather than inline JSON. A `describe()` method can be generated from the existing
`[PythonAPI]` metadata, giving an agent runtime schema discovery; an MCP wrapper over the pipe is
then a few hundred lines. Avoid gRPC on .NET Framework 4.8 - the only build that supports it is long
deprecated.

# Risks

| Risk | Mitigation |
|---|---|
| Layer 1 volume stalls the effort | Steps 1.1-1.9 are independently shippable and ordered by usefulness; stop anywhere and the API still works |
| Two API front doors diverge (embedded console vs RPC) | The service layer is the single implementation; transports never touch `Controller` directly |
| Persisted-command compatibility breaks | No serialized field renames without a `_Compatibility` shim; API changes add commands, never alter existing ones |
| Auto-answered prompts corrupt an agent run | Step 0.3 strict mode defaults to fail for `--script` |
| Parallel runs corrupt shared settings | Step 0.5 |
| Desktop-session requirement surprises a CI user | Documented explicitly in D5; not solvable without the GUI/kernel split |
