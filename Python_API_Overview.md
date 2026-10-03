# PrePoMax Python API Overview

This document is a simple overview of the Python API objects exposed by PrePoMax.
The root object available in the Python console is `pmx`.

## General conventions

- `pmx` is the root `PrePoMaxAPI` object.
- API objects implement `help()`, which prints documented members to the console.
- Collection-like objects support `len(...)`, iteration, indexing with `[...]`, and `in` checks.
  Indexing is **by name**, or by ID where the collection is keyed by one; positional access is
  `list(collection)[index]`, because two indexers cannot be resolved from the Python side.
- Ordered object collections provide `first()` and `last()` convenience methods.
- Name collections return `StringList` objects. ID collections return `IntList` objects.
- Long API arrays and named collections are shortened when evaluated directly. Use `print(array)` to display every item.
- Result names are strings. In many projects they are full file paths and are used to select the current result.
- Result data is exposed through `pmx.results.current`. Use `set_current(result_name)` to change it.
- Result steps and increments are indexed by result IDs, not by model step names.
- Field result data is node-based. Only history output can contain integration point values.
- Integration point history entry names use the form `elementId_intPointId`.
- NumPy is optional. Where available, numeric arrays are returned as NumPy arrays. Otherwise, `NumericArray` or typed API list wrappers are returned.
- Methods that construct Python-side objects start with `create_` and do not modify PrePoMax. They
  make either a detached part that an `add` takes as an argument (`pmx.create.elastic(...)`,
  `pmx.create.region(...)`), or a result output draft (`create_field_output_equation(...)`).
- Collection `add_*` and `replace_*` methods validate and make the command-recorded change. On the
  **model** side a value that is not given is kept, so a replace changes only what it is told; on the
  **results** side a replace re-specifies the whole definition, because a result draft is built whole
  rather than cloned.

## Script execution and regeneration

- **Run script** and **Run selection** execute in the shared interactive Python scope. State-changing API calls are recorded
  as individual PrePoMax commands.
- **Record and Run** stores the complete editor source as one phase-neutral history command and then executes it. Commands invoked
  by that script still use the normal controller command path, but they are not added to history a second time.
- Recorded scripts run in a fresh scope containing a new `pmx`, `__name__ = "__main__"`, and `__file__ = "<string>"`.
  They cannot depend on variables or imports left in the interactive console by earlier work.
- A recorded script is added to history only when it completes without an error. Full-history regeneration executes the embedded
  source at the same position in history; phase-only regeneration skips recorded scripts.

## Session journal

Every command the application records can say what it is in Python, so a session done in the user
interface can be exported as a script, edited, and run again. The journal is the same vocabulary this
document describes: what it writes is what a user would have written.

| Where | What it does |
| --- | --- |
| **File - Export - Python script** | Writes the journal of the open model's history. |
| **Edit - Commands - Save As Python** | Writes the journal of the history shown in the dialog, including a reordered or shortened one. |
| `--journal <file.py>` | Writes the journal when a batch run ends, on a failing run too. |

A journal names the file the session started from in its header rather than opening it, because
replacing the model rebuilds the command collection a running script belongs to. Replay it with the
`-f` switch:

```bash
PrePoMax.com -f model.pmx --script journal.py -g No -x Yes -w <work directory>
```

What a journal will not do silently: a command it cannot emit becomes a comment naming the command
and the reason, and the export reports how many it wrote. A recorded Python script is emitted
verbatim rather than expanded into the calls it made, because regeneration re-runs its computations
and expanding it would replace live computation with the values it produced the first time.

`APIs/Python/Test_Scripts/Journal_Round_Trip.py` checks the whole path: it regenerates a recorded
`.pmh`, journals it, replays the emitted script into a fresh instance, and compares the CalculiX
export of both models.

## Unattended script execution

`--script <file.py>` runs a script without a user present. The script is executed after the file given by `-f` is opened, and it
is recorded as a single history command exactly like **Record and Run**. Combine it with `-g No` to keep the window hidden and
`-x Yes` to exit when the script finishes.

```
PrePoMax.com --script build.py --scriptArgs "40 steel" -f model.pmx -g No -x Yes -w C:\work --report run.json --timeout 600
```

- `--scriptArgs` is split on spaces and reaches the script as `sys.argv[1:]`; `sys.argv[0]` is the script file name.
- `--onPrompt` decides what happens when a dialog asks for a decision and nobody can answer it. `Fail` aborts the run, `Accept`
  answers with the default answer. A script defaults to `Fail`, a regeneration defaults to `Accept`. Single button dialogs only
  report and never abort the run.
- `--timeout <seconds>` limits the whole run. On expiry the running analyses are killed and the process exits.
- `--settings <file>` reads the settings from an explicit file, and `--noSettingsSave` does not write them back. A script run
  never writes the settings file, so parallel runs cannot disturb each other or the interactive user.
- `--report <file.json>` writes a machine readable record of the run: the invocation, the executed commands and their durations,
  the reported messages, the answered or refused prompts, the analysis jobs with their status, and the produced files. The report
  is written also when the run fails.
- `--save <file.pmx>` saves the model after the script finishes. The save happens after the script command is recorded, so the
  saved file regenerates the scripted session; a `pmx.save` call from inside the script cannot, because the script command is
  added to the history only once it has completed.

The process exit code is part of the command line contract:

| Code | Meaning |
| --- | --- |
| 0 | Completed, no errors |
| 1 | Command line or option error |
| 2 | Model or regeneration error |
| 3 | Script error - an unhandled Python exception |
| 4 | Timed out |
| 5 | An interactive prompt was refused because `--onPrompt` is `Fail` |
| 6 | The `PrePoMax.com` launcher could not start the application |

## Agent integration

An AI agent can drive **the session the user has open**, through the same Python API this document
describes. The model changes in the viewport and the tree while the agent works, every change is
recorded as the command the user would have produced by hand, and the user keeps working alongside it.

PrePoMax serves the **MCP endpoint itself**, over HTTP on this computer only. There is no bridge
process: the client connects to an address instead of starting anything, so nothing is left running
when PrePoMax is closed. The same **automation server** also keeps its named pipe, for a client that
speaks the protocol directly rather than MCP; both fronts go through one dispatcher, and both go up
and down on the one switch.

### Turning it on

The server is off until the user turns it on. Unpacking PrePoMax opens nothing.

| Where | What it does |
| --- | --- |
| **Tools - Automation Server** | Starts and stops the server for this session. The status bar shows the port while it runs. |
| **Tools - Settings - Automation** | *Start with the application*, *Read only*, *Echo to the Python panel*, the request timeout, and under *Connection* the port, the address and the access token. |
| `--rpc On` | Starts the server for the run. It also allows `-g No` on its own, which is a session with no window for an agent to drive. |
| `--rpc ReadOnly` | The same, with the model protected against change for the whole run. |
| `--rpcPort <n>` | The port for this run, instead of the one in the settings. |

There is deliberately **no switch for the token**. An unattended run reads the one in the folder's
settings, so a folder has to be opened once with its window and given a token before `-g No` can
serve; a session with no window refuses with that instruction rather than prompting a user who is
not there.

**A session with no window starts empty unless it is given a file.** It does not open the last file
of the interactive user, for the same reason it is not asked whether to recover a crashed session:
the setting is a convenience for the person who last closed the application, and a hidden session
would otherwise inherit that person's model and their whole command history without being told to.
Name the model with `-f` when an agent should find one open.

Point the agent client at the address. For Claude Code:

```bash
claude mcp add --transport http prepomax http://127.0.0.1:8756/mcp --header "Authorization: Bearer <token>"
```

The address and the token are both in **Tools - Settings - Automation**, under *Connection*. **The
server will not start without a token**, and there is no command line switch for one: run PrePoMax
once, right click the *Access token* entry, and choose *Generate the access token*. It is kept in
that folder's `settings.bin`, so deleting the portable folder deletes it, and it can only be
generated - never typed or pasted, which is what keeps it long and random.

**A newly unpacked PrePoMax starts without one, deliberately.** `settings.bin` is part of the
redistributable, so a token left in it would be shipped to every user, identical for all of them and
readable by anyone. Two things prevent that: the settings page has a *Clear the access token* action
to use before preparing a release, and the first run in any folder clears the token found in its
settings before anything can serve with it - PrePoMax detects that from the absence of its own
`trace.info` marker. So after an update you generate a new token in the new folder and give it to
your assistant, or copy `settings.bin` forward as you would to keep any other setting.

**A client that cannot connect is telling you that PrePoMax is closed, or that its automation server
is off.** Nothing answers on the address unless the session is open - that is the point of serving it
from the application - so the fix is to open PrePoMax and switch the server on, not to retry. It is
worth telling the agent so in the project's own instructions:

```markdown
The prepomax MCP server drives a live PrePoMax session. If it cannot connect, ask me to open
PrePoMax and switch on Tools - Automation Server; do not work around it.
```

### Migrating from PrePoMaxMcp.exe

An earlier version shipped `PrePoMaxMcp.exe`, a bridge the client started on stdio. It is gone,
because a process the client starts outlives the application it serves: with PrePoMax closed, every
open agent session still held an idle bridge. Remove the old entry and add the address:

```bash
claude mcp remove prepomax
```

Nothing else changes - the same ten tools, the same behaviour. The new entry names no path, so moving
the PrePoMax folder or unpacking a new version leaves it correct, which the old one never did.

### The tools

| Tool | What it does |
| --- | --- |
| `get_guide` | How to drive a session, served by the application so it matches the version. The first thing to read. |
| `list_sessions` | The running instances whose server is on, each with the address it answers on, marking the one that answered. |
| `get_status` | Idle or busy and why, the open model, the view, the unit system, the command count, read only. |
| `describe_api` | The `[PythonAPI]` schema of `pmx`, of any path under it, or of an API type by name, read off the API itself. |
| `read_model` | A JSON snapshot of the model: properties, collections, steps with their content, analyses with their status. |
| `read_topology` | The faces, edges and vertices of the parts, each with the ID a selection takes, its size, its centroid and its normal. |
| `eval_expression` | One expression, with its value as JSON when it has one. |
| `run_python` | Python against the live model, recorded as commands or as one script. |
| `get_history` | The command history, and the journal of it as a Python script. |
| `run_job` | Starts an analysis without waiting, so the user keeps the interface. |
| `get_job` | The status of an analysis, and its solver output on request. |
| `screenshot` | A PNG of the viewport, optionally after setting the view, the camera and the plotted result. |

The surface is deliberately small. The model surface is the Python API, which the agent writes
through `run_python` and learns through `describe_api`, so there is no tool per API member - 965 of
them could not be listed, and a second front door would be free to drift from the first. A tool that
is not a protocol primitive is a piece of Python the catalog holds: `read_model` and `read_topology`
are both one, so adding another costs nothing on the application side.

### What the agent gets that it cannot guess

Four things an agent has no way to work out from the model alone, and where each one comes from.

- **How the work is ordered.** `get_guide` is a short procedural guide held by the application, not
  by the client, so a client of any kind gets the guide of the version it is talking to. The MCP
  `instructions` string says to read it before the first change.
- **The values an argument accepts.** `describe_api` reports, for every method, its real parameters
  with the default the signature carries and - for a string argument the API validates against an
  enumeration - the values it accepts. The enumerations come from `PythonEnumVocabulary`, which is
  audited against every `ToEnum` site by `Test_Api_Enum_Vocabulary.py`, so an argument added without
  an entry is reported rather than silently described without its values.
- **A collection that does not exist yet.** `describe_api` also takes an API type name - `Loads`,
  `Model.Steps` - so the half of the API that hangs off something the model does not have can still
  be read. Creating a step to see what loads it would offer is a change to the user's model made for
  no other reason. A type name followed by a member path - `Result.mesh`,
  `Result.steps.last().increments` - is walked through the declared member types for the same
  reason, so the results side can be read before an analysis has run; a member declared as `object`
  ends the walk with a message to describe it as a live path. A name the model and the results sides
  share is refused with the candidates.
- **Which entity is which.** `read_topology` names every face by the ID a selection takes and gives
  it an area, a centroid and a normal, so the top face of a part is found rather than guessed. The
  answer is bounded twice: per part, and over the whole answer, so an assembly reports every part's
  summary and details the first of them.

**What a call recorded is named.** `run_python` and `eval_expression` report `recordedCommands` as
the command class and the line the command history editor shows, which names the item, so a call
that wrote several things is not just a list of types. `get_history` carries the same description.

**Units are not converted.** A plain number is a value in the model's unit system, which `get_status`
reports as `unitSystem`. The instructions say so, because nothing else will: 210000 is a correct
steel modulus in `MM_TON_S_C` and wrong by a factor of a million in `M_KG_S_C`.

### A skill for a client project

`.claude/skills/prepomax/SKILL.md` is a Claude Code skill that tells an agent to read `get_guide`
first and names the handful of habits that are specific to this application. It is short on purpose:
the guide and the schema are served by the application and cannot drift, and a skill that repeated
them would. Copy that folder into a client project to have it available there.

Every tool also takes an optional `process_id`. **It is checked, not routed.** One address serves one
session, so a call that names another instance is refused with the address that would have served it,
rather than served against the wrong model. `list_sessions` is the directory of what is open - use it
to tell the user which address to configure, not to switch.

### What an agent may and may not do

- **Every change is recorded.** `run_python` records each API call as its own command by default, so
  the history stays editable in **Edit - Commands**, regenerable, and journalable. `record='script'`
  records the source as one command instead, which is what keeps a computed model parametric.
- **Busy is an answer.** A call that arrives while a dialog is open or a mesher is running is refused
  with the reason, and nothing is done. The question is asked on the thread that owns the window,
  because that is the only place the truth is visible: an item dialog of this application is not
  modal, and a modal one keeps pumping messages.
- **A long call blocks nothing else, but it does say it is working.** The work runs on the
  connection it arrived on, the way a `--script` run runs it, so the window stays usable and another
  connection is still answered while a six second call is in flight. Calls that reach the model are
  served one at a time, so two agents cannot interleave halfway through a change.
- **A call that lasts marks the application busy.** Once an agent call has been running for a third
  of a second it takes the state the application uses for its own long operations - the status bar
  says *Automation server...*, the menus and toolbars go quiet, and the view stops redrawing - so the
  user cannot start an edit in the middle of a change. A call that finishes sooner never touches the
  window, because a status bar that blinks on every call reads as a fault.
- **A script of the user's own and an agent call exclude each other.** Running a script from the
  Python panel marks the application busy the same way, so an agent call that arrives while it runs
  is refused with the reason; and a script does not start while an agent call holds the state, and
  says so in the console rather than failing later.
- **Read only means the model cannot change.** It does not refuse a method; it blocks the command
  history, which every state change of the application passes through, so no Python an agent sends
  can get around it. Reading keeps working.
- **The session belongs to the user.** `exit` is refused for a session that has a window - only a
  hidden `-g No` session can be closed through the server - and `shutdown` closes the door an agent
  came through without touching the application.
- **Everything is visible.** What the agent runs is echoed into the Python panel when it is open, and
  named in the output window in any case.

### Undoing what an agent did

The same way as anything else: **Edit - Commands** lists the commands the agent recorded, they can be
removed or reordered there, and **Edit - Regenerate** rebuilds the model without them.

### The protocol, for another client

The MCP endpoint is Streamable HTTP, and only its JSON half: a client `POST`s one JSON-RPC 2.0
object to `http://127.0.0.1:<port>/mcp` with `Content-Type: application/json` and
`Authorization: Bearer <token>`, and gets one JSON-RPC response back. `GET` is answered `405` - the
endpoint serves no event stream, because nothing in its surface pushes - and no session id is issued,
so a client written against either revision of the specification works. A request that reaches the
protocol and fails there is a `200` carrying a JSON-RPC error; a request refused by a gate is an HTTP
status, because it never reached the protocol.

**The gates.** Three, and each stops something the other two do not. Only the two loopback prefixes
are claimed, so nothing off this computer can reach it. The bearer token is required on every
request and compared in constant time. Any request carrying an `Origin` header is refused, `OPTIONS`
is refused, and no `Access-Control-Allow-*` header is ever answered with, so no page in a browser can
reach the endpoint even if it is told that its own name resolves to `127.0.0.1`. A body over 4 MB, a
body that is not JSON, and any other path are refused as well.

What the token bounds is worth being exact about: it is stored in a file the user can read, so
anything running **as that user** can read it, exactly as anything running as that user can open the
pipe. It stops another account logged into the machine - which can reach a loopback port, and is the
boundary the named pipe used to get from Windows for free - and it stops a web page, which cannot
read a file at all. It is not a defence against code already running as the user, and nothing served
over loopback can be. While the server runs the token is also in the session file under
`%LOCALAPPDATA%\PrePoMax\sessions\`, which is why that folder is created with a single rule for
that user and with inheritance switched off; the file is removed when the server stops.

**The port.** 8756 by default, set in *Tools - Settings - Automation* or with `--rpcPort`. When it is
taken - by another PrePoMax, or by another program - the endpoint takes the first free port in the
sixteen above it and says which in the output window and the status bar. One port per instance, so
the address is the address of one session.

**Replacing and clearing it.** The context menu of the *Access token* entry offers *Generate the
access token*, which replaces it after asking, and *Clear the access token*, which removes it - use
that before preparing a redistributable. Either way a running server is stopped first, and restarted
onto the new token, so it never keeps serving one that is no longer written down. Every client
configured with the old token needs the new one. Both actions sit beside the value they change.

**The pipe.** The same dispatcher is also served on `\\.\pipe\PrePoMax.<process id>`, JSON-RPC
2.0, one object per line, UTF-8, with no token: the operating system authenticates the caller, and
only the user running PrePoMax may open it. It is there for a client that speaks the protocol
directly. Either way a running instance writes
`%LOCALAPPDATA%\PrePoMax\sessions\<process id>.json` while its server is on - process id, pipe
name, `url`, `port`, `token`, versions, model file, start time - which is how a client finds it. A
file whose process no longer exists is stale, and the next instance asked for `list_sessions` prunes
it. The folder is restricted to the current user, because the token is in it.

| Method | What it does |
| --- | --- |
| `ping` | Liveness and versions. Answered without touching the model, so it answers while the application is busy. |
| `status` | The state, and the methods this server serves. |
| `describe` | The API schema of a path. |
| `eval` | Evaluates an expression. |
| `exec` | Runs source; `record` is `commands` or `script`. |
| `history` | The commands, and the journal with `include_journal`. |
| `screenshot` | Writes a PNG and returns the path. |
| `shutdown` | Stops the server; never closes the application. |
| `exit` | Closes a hidden session; refused for one with a window. |

The error codes are the standard JSON-RPC ones plus an application range: `-32001` busy, `-32002`
read only, `-32003` a refused prompt, `-32004` an API error, `-32005` an unknown task.

`Test_Rpc_Server.py`, `Test_Rpc_Live_Session.py`, `Test_Mcp_Http.py` and `Test_Mcp_Workflow.py` in
`APIs/Python/Test_Scripts` check the layers of this: the protocol on the pipe, what a driven session
records, the endpoint with every gate and tool, and a whole analysis driven through the tools alone.

## Typical access path

The model tree mirrors the application tree, so a scripted model is built in the order the tree is
built.

```python
pmx.model.materials.add('Steel', pmx.create.elastic_with_density(210000, 0.3, 7.85e-9))
pmx.model.sections.add_solid('Section-1', 'Steel', part_selection)
pmx.model.node_sets.add('Bottom', bottom_selection)

step = pmx.model.steps.add_static('Step-1', time_period=1.0)
step.bcs.add_fixed('Fix-1', pmx.create.region('NodeSetName', 'Bottom'))
step.loads.add_pressure('Pressure-1', top_selection, 2.5)
step.field_outputs.add_nodal('NF-Output-1', ['U', 'RF'])
```

The results tree is reached the same way, one level at a time.

```python
pmx
pmx.results
result_names = pmx.results.get_names()
pmx.results.set_current(result_names[-1])
result = pmx.results.current
step = result.steps[1]
increment = step.increments[1]
field = increment.fields['DISP']
component = field.components['U1']
data = component.get_data()
node_id = int(data.ids[0])
value = component.get_value(node_id)
```

## Common API base

### `BaseAPI`

Base class for user-facing API objects.

| Member | Description |
| --- | --- |
| `help()` | Prints documented API members for the object. |
| `repr(obj)` | Returns a concise object or array description. |
| `str(obj)` / `print(obj)` | Returns the complete array or named-collection contents when applicable. |

### `StringList`

Read-only list wrapper for strings.

| Member | Description |
| --- | --- |
| `count` | Number of strings. |
| `len(list)` | Number of strings. |
| `list[index]` | String at zero-based index. |
| `item in list` | Checks whether the string exists. |
| `contains(item)` | Checks whether the string exists. |
| `to_array()` | Returns a copied CLR string array. |

### `IntList`

Read-only list wrapper for integers.

| Member | Description |
| --- | --- |
| `count` | Number of IDs. |
| `len(list)` | Number of IDs. |
| `list[index]` | ID at zero-based index. |
| `item in list` | Checks whether the ID exists. |
| `contains(item)` | Checks whether the ID exists. |
| `to_array()` | Returns a copied CLR integer array. |

### `StringDoubleMap`

Read-only ordered mapping from string names to floating-point values.

| Member | Description |
| --- | --- |
| `count` | Number of name-value pairs. |
| `len(mapping)` | Number of name-value pairs. |
| `mapping[name]` | Value for the specified name. |
| `name in mapping` | Checks whether a name exists. |
| `get_names()` | Returns names as `StringList`. |
| `contains(name)` | Checks whether a name exists. |

### `NumericArray`

Read-only fallback for numeric arrays when NumPy is unavailable.

| Member | Description |
| --- | --- |
| `shape` | Array dimensions as an `IntList`. |
| `count` | Total number of values. |
| `len(array)` | Length of the first dimension. |
| `array[index]` | Value or row at the specified index. |
| `to_array()` | Returns a copied CLR array. |

## Root API

### `PrePoMaxAPI`

Root object exposed as `pmx`.

| Member | Description |
| --- | --- |
| `model` | The model tree of the currently open model. |
| `results` | Loaded result names and the currently selected result. |
| `selection` | Creates and highlights reusable view-aware selections. |
| `create` | Creates the detached parts that model items take as arguments. |
| `jobs` | The analyses of the model. |
| `view` | What the application shows, and a picture of it. |
| `help()` | Prints documented members. |

The document methods listed under **Document** below also live on this object.

Example:

```python
pmx
pmx.model
pmx.results
pmx.selection
pmx.create
pmx.help()
```

## Document

The document operations sit on the root object, because they act on the application rather than on a
model item.

| Member | Description |
| --- | --- |
| `pmx.new_model(model_space, unit_system)` | Creates a new empty model. **Console only.** |
| `pmx.open(file_name)` | Opens a .pmx, .pmh or .frd file. **Console only.** |
| `pmx.import_file(file_name, only_materials=False)` | Imports a geometry or mesh file into the current model. |
| `pmx.save(file_name)` | Saves the model to a .pmx file. |
| `pmx.export_to_calculix(file_name)` | Exports a CalculiX .inp file. |
| `pmx.export_to_abaqus(file_name)` | Exports an Abaqus .inp file. |
| `pmx.export_to_3mf(file_name, edge_thickness_ratio)` | Exports the model to a 3MF file. |
| `pmx.export_geometry(part_names, file_name, format=None)` | Exports the named geometry parts; the extension picks .stp/.step, .brep, .stl or .mesh (Mmg). |
| `pmx.export_mesh(part_names, file_name, format=None)` | Exports the nodes and elements of the named mesh parts; the extension picks .inp (CalculiX, or `format="abaqus"`), .msh (Gmsh), .mesh (Mmg) or .stl. |
| `pmx.export_model(file_name, format=None)` | Exports the whole model as a solver input deck; .inp is CalculiX unless `format="abaqus"`. |
| `pmx.export_results(part_names, file_name, format=None)` | Exports the named parts of the current result in their deformed shape; the extension picks .inp (CalculiX mesh) or .stl. |

The four source based exports mirror the **File - Export** menu, whose items are Geometry, Mesh, Model
and Results: the item names what is exported and the save dialog's filter, or the extension typed,
names the format. `export_geometry` takes **geometry** parts, refuses a part without CAD data for
.stp and .brep, and refuses a part that is neither a solid nor a shell - an imported .stl body - for
.mesh; `export_mesh` takes the parts of the **model mesh** and writes only their nodes and
elements, which is how a part of a meshed model is handed to another program without its steps,
loads and boundary conditions; `export_results` takes the parts of the **current result**. The
`format` argument is needed only when the extension does not name the format - an Abaqus .inp - or
when the file should carry another extension; its values are the format names in lower case
(`"calculix"`, `"abaqus"`, `"gmsh"`, `"mmg"`, `"stl"`, `"step"`, `"brep"`,
`"stepassembly"`, `"brepassembly"`). A journal of a history
recorded before these four existed emits the old per format exports through them, except an .stl or
Mmg export of the displayed view, whose source was never recorded and which the journal reports as
unsupported.

`"step"` and `"brep"` write one file per part, named `<file_name>_<part_name>`, and are what the
extension alone selects. `"stepassembly"` and `"brepassembly"` carry the same extensions but write
a single file holding every named part as one compound, which is how an assembly leaves PrePoMax
with its parts side by side. The parts are gathered unchanged - they are not imprinted on one
another as the **Create compound part** command does - so the file is the assembly PrePoMax holds,
not a partitioned version of it. In the **File - Export - Geometry** dialog the two pairs are the
filters STEP parts, STEP assembly, BREP parts and BREP assembly.

`new_model` and `open` replace the command history, which a recorded script is itself part of, so both
raise inside a recorded script. Open the file with the `-f` command line switch instead, or call them
from the Python console.

To save a scripted session so that it can be regenerated, use the `--save` command line switch rather
than `pmx.save`. The save then happens after the script command is recorded, so the saved history
contains the script; a `pmx.save` call from inside the script writes the model correctly but its
embedded history stops just before the running script.

## Model

### `Model`

Reached as `pmx.model`. It mirrors the application model tree, so `results`, `jobs`, `selection` and
`create` are siblings of `model`, not members of it.

| Member | Description |
| --- | --- |
| `name` | The model name. |
| `properties` | The model name, model space and unit system. |
| `parameters` | The equation parameters of the model. |
| `geometry` | The geometry branch of the model. |
| `mesh` | The meshing branch of the model. |
| `nodes` | The nodes of the model mesh. |
| `elements` | The elements of the model mesh. |
| `parts` | The mesh parts of the model. |
| `amplitudes` | The amplitudes of the model. |
| `distributions` | The distributions of the model. |
| `initial_conditions` | The initial conditions of the model. |
| `materials` | The materials of the model. |
| `sections` | The sections of the model. |
| `constraints` | The constraints of the model. |
| `surface_interactions` | The surface interactions of the model. |
| `contact_pairs` | The contact pairs of the model. |
| `node_sets` | The node sets of the model. |
| `element_sets` | The element sets of the model. |
| `surfaces` | The surfaces of the model. |
| `reference_points` | The reference points of the model. |
| `coordinate_systems` | The coordinate systems of the model. |
| `steps` | The analysis steps of the model. |
| `keywords` | The CalculiX keyword editor of the model. |
| `tree` | The grouping and ordering of the model tree. |
| `is_open()` | True when a model is open. |

### `ModelProperties`

| Member | Description |
| --- | --- |
| `name` | The model name. Writable. |
| `model_space` | `ThreeD`, `PlaneStress`, `PlaneStrain` or `Axisymmetric`. Writable. |
| `unit_system` | `M_KG_S_C`, `MM_TON_S_C`, `M_TON_S_C`, `IN_LB_S_F` or `UNIT_LESS`. Writable. |
| `model_type` | `GeneralModel`, `Submodel` or `SlipWearModel`. Writable. |
| `global_results_file_name` | The results file a submodel reads its boundary values from. Writable. |
| `slip_wear_results` | Which results a slip wear model keeps. Writable. |
| `number_of_cycles`, `cycles_increment`, `num_of_smoothing_steps` | The slip wear model values, as the value or equation they were entered with. Writable. |
| `bdm_remeshing` | Whether a slip wear model uses BDM remeshing. Writable. |
| `absolute_zero`, `stefan_boltzmann`, `newton_gravity` | The physical constants, or `None` while they are undefined. Writable. |
| `replace(name=None, model_type=None, ..., clear_undefined_constants=False)` | Replaces several properties in one command. A value that is not given is kept. |

A physical constant that has never been given reads back as `None`. Because a value that is not given
is kept, clearing one is an explicit request: `replace(clear_undefined_constants=True)` makes every
constant it was not given undefined again.

```python
pmx.model.properties.replace(model_type='Submodel', global_results_file_name='global.frd')
pmx.model.properties.absolute_zero = -273.15
```

### `Parameters`

| Member | Description |
| --- | --- |
| `add(name, value, description=None)` | Adds a parameter. The value is a number or an equation string. |
| `replace(old_name, name=None, value=None, description=None)` | Replaces a parameter. A value that is not given is kept. |
| `set_all(names, values, descriptions=None)` | Replaces the whole table. The lists give the parameters in the order they are shown. |
| `import_from_file(file_name)` | Imports the parameters from a .pmp file. |
| `export_to_file(file_name)` | Exports the parameters to a .pmp file. |

```python
pmx.model.parameters.add('t', 2.5, description='Plate thickness')
pmx.model.parameters.add('area', 't * 4')
print(pmx.model.parameters['area'].value)
pmx.model.parameters.set_all(['area', 't'], ['t * 4', 2.5])
```

A number argument is a value in the current model units; a string argument is an equation. Parameters
have no duplicate command, so `duplicate` raises. `rename` is carried out as a replacement, and a
renamed parameter is **not** rewritten inside the equations that reference it.

`add` and `replace` evaluate the equation as they are called, so they can only use a parameter that
already exists. `set_all` resolves the whole table at once, so a parameter may use a parameter that
is given after it - which is what the parameters editor of the application records when its rows are
reordered. The order of the lists is the order of the table, and they must be the same length.

A description is free text shown beside the parameter in the editor. It is kept by a `replace`
that is not given one, cleared by an empty string, and written as the fourth field of a `.pmp`
file.

### `Parameter`

| Member | Description |
| --- | --- |
| `name` | The parameter name. |
| `equation` | The value or equation as entered. |
| `value` | The evaluated value. |
| `description` | The description, or None. |

### Model item collections

Every named model item collection shares the same verbs. The type-specific creation methods differ
per area and are documented with that area.

| Member | Description |
| --- | --- |
| `get_names()` | Names of the items in the collection. |
| `contains(name)` | True when an item with that name exists. |
| `get(name)`, `collection[name]` | The item with that name. |
| `len(collection)`, iteration | The count, and iteration in model order. |
| `first()`, `last()` | The first and the last item. |
| `duplicate(names)` | Duplicates the named items using command history. Not every area supports it. |
| `remove(names)` | Removes the named items using command history. |
| `activate(names)`, `deactivate(names)` | Keeps the items in the model but stops writing them to the solver input file. Not every area supports it. |
| `rename(old_name, new_name)` | Renames an item using command history. |

A collection is indexed **by name only**. Positional access is `list(collection)[index]` or
`get_names()`, because two indexers cannot be resolved from the Python side. `duplicate`,
`activate` and `deactivate` raise a clear error in the areas that have no such command - parameters
cannot be duplicated, and only materials, initial conditions, mesh setup items, model parts, steps
and the step content can be activated and deactivated.

### `Geometry`

Reached as `pmx.model.geometry`.

| Member | Description |
| --- | --- |
| `parts` | The geometry parts of the model. |

### `Mesh` (model)

Reached as `pmx.model.mesh`.

| Member | Description |
| --- | --- |
| `setup_items` | The mesh setup items of the model. |
| `create(part_names)` | Meshes the named geometry parts. The call returns when the meshing has finished, and reports every named part with the reason it was not meshed and an empty reason for one that was. A call that meshes nothing at all raises instead, and records no command. |
| `create_boundary_layer(selection, thickness)` | Creates a prismatic boundary layer on the selected faces. |
| `remesh_elements(region, ...)` | Remeshes the elements of a region. The region is a selection or an `ElementSetName` region. |
| `thicken_shell_mesh(selection, thickness, number_of_layers=1, offset=0)` | Thickens the selected shell mesh. |
| `split_part_mesh_using_surface(base_part_region, splitter_surface_region, ...)` | Splits a part mesh with a surface. The base part region is a selection or a `PartName` region, the splitter surface region a selection or a `SurfaceName` region. |
| `update_nodal_coordinates_from_file(file_name)` | Reads new nodal coordinates from a file. |

The five tools are **one-shot operations**: they run once and are replayed only during a regenerate.
Each one stores the selection it was given, not the resolved IDs, so a regenerate that rebuilds a
different mesh re-resolves them.

### `MeshSetupItems`

Every setup item is created from a selection built in the `geometry` view, because the application
rejects a setup item without creation data. The target follows the item type, as it does in the dialog.

| Member | Description |
| --- | --- |
| `add_meshing_parameters(name, selection, ...)` | Meshing parameters. Every value that is not given keeps the application default. |
| `add_local_mesh_size(name, selection, element_size=None, number_of_elements=None)` | A local mesh size. Exactly one of the two values is given. |
| `add_shell_mmg(name, selection, ...)` | An MMG shell mesh item. It selects MMG for STL shell parts and defines its geometry approximation. |
| `add_thicken_shell_mesh(name, selection, thickness, number_of_layers=1, offset=0, keep_model_edges=False)` | Thickens a shell mesh. |
| `add_netgen_mesh(name, selection, optimize_steps_2d=None, optimize_steps_3d=None, quad_dominated=None, transfinite_three_sided=None, transfinite_four_sided=None, transfinite_angle=None)` | A Netgen mesh item for solid and shell parts - tetrahedra on a solid, triangles or a quad-dominated mesh on a shell. The mesh size comes from the meshing parameters; the item carries the settings that apply to Netgen alone. It asks for Netgen on a solid part whose geometry would otherwise be meshed transfinitely or swept. A part meshed by Netgen without this item uses its default values. |
| `add_tetrahedral_gmsh(name, selection, ...)` | A Gmsh tetrahedral mesh item. |
| `add_shell_gmsh(name, selection, ...)` | A Gmsh shell mesh item. |
| `add_transfinite_mesh(name, selection, allow_pyramid_elements=False, ...)` | A Gmsh transfinite mesh item. |
| `add_sweep_mesh(name, selection, number_of_global_smooth_steps=0, ...)` | A Gmsh sweep mesh item. The selection names the source surfaces; the model derives the sweep direction, the centre, the side surfaces and the layer edges from them. A part selection - a solid or a compound of solids - lets the model find the source surfaces itself. Either selection may span several parts that stand apart from each other, and each of them is then swept on its own. A cell of a compound the sweep cannot reach is meshed by tetrahedra, by Netgen, with a pyramid on every quadrilateral a swept cell leaves on the face they share, rather than refusing the compound; a compound all of whose cells sweep is swept as before. A part selection also revolves a solid, or a cell of a compound, that the sweep between two ends cannot reach and that is turned about an axis its source touches: its source is turned through equal layers, and the elements beside the axis collapse into wedges, pyramids and tetrahedra. A wall of a revolved cell may be split along the revolution, by the faces of a neighbour standing on it, and the layers then end at the angles of the split. Revolved cells about one axis are turned the same way round, so that the target of one is the source of the next; a revolved cell shares its walls with other revolved cells and with swept cells, whose counts are settled with its own, hands its surfaces on as the sources of swept cells, and is revolved from the target of a swept cell, which is meshed before it. A swept cell ending on the target of a revolved cell is swept the other way round, from it, when its other end is free or the source of a revolved cell. Swept and revolved cells may form a ring, as the sides and the corners of a frame do; a swept cell closes it, ending on the meshed source of the cell after it. A cell that could be swept as well as revolved is swept, unless its sweep meets a revolved cell in any other way: it then follows that cell and is revolved about the same axis, and the cells it touches follow it in turn. A cell a revolved cell would share a surface with in any other way leaves it to tetrahedra. `symmetry` is `Automatic` (the default for a new item) or `Off`: with `Automatic`, a compound whose cells are mirror images across the planes it is split on sweeps one symmetric piece of its cells and mirrors the mesh onto the others; a cell of the piece the sweep cannot reach is meshed by tetrahedra and mirrored the same way. The symmetry is used only when every cell has a mirror image, every edge of an image is an edge of the cell it is meshed from, and every other mesh setup item selects the images of what it selects; otherwise the compound is swept whole. An item read from a file written before the argument existed has `Off`. The structured sources are the 3-sided and 4-sided faces every mesher structures, by the item's `transfinite_three_sided`, `transfinite_four_sided` and `transfinite_angle`, whether gmsh or QuadWild meshes them: a side may be a chain of edges, a 3-sided source is three blocks of quads around one inner node, opposite sides are balanced to the coarser count together with the walls, and the meshing is refused when numbers of elements set on edges, or the layer counts of a neighbouring block, cannot agree with them. |
| `add_shell_quad_wild(name, selection, preview=None, smooth=None, alpha=None, triangulation_mesher=None, flow_profile=None, keep_files_on_failure=None, debug_visualization=None, old_algorithm=None, pin_edge_nodes=None, edge_rotations=None, singularity_position=None, count_search=None, boundary_layer=None, transfinite_three_sided=None, transfinite_four_sided=None, transfinite_angle=None)` | A QuadWild shell mesh item. `keep_files_on_failure=True` retains its MeshBatch folder when a QuadWild stage or mesh validation fails. `debug_visualization=True` adds the QuadWild patches as parts, the irregular, prescribed, patch-corner and seam nodes as node sets, the sheets of a shell compound as element sets, and the cross field and feature lines as wire parts; with `preview=True` it shows the traced triangle patches. `old_algorithm=True` meshes without the QuadWild patch layout and mesh quality improvements, for comparison. `pin_edge_nodes=True` keeps every triangulation node on the model edges, as a sweep source meshed after the 1D mesh would need, for comparison. `edge_rotations=True` turns on the quality-driven edge rotations, which change the connectivity where the quads are poor; by default they are off and the connectivity of the quad generation is kept. `singularity_position` (0 = off, 2 to 4 worth testing) weighs placing the irregular node of every 3-, 5- or 6-sided patch where its separatrices fit the patch geometry, for comparison. `count_search` (0 = off) is the number of rounds of the search over the element counts of the patch sides; every round tries the sides of the three worst patches at one or two elements more or fewer and keeps the best change, at the cost of a quantisation and a full smoothing per trial. `boundary_layer` is `None` (default), `Holes` (one ring of regular quads around every closed edge loop without corners) or `All` (along every model edge; helps fine meshes, can hurt coarse ones and shell compounds). `transfinite_three_sided` and `transfinite_four_sided` (both `True` by default) take the model faces with 3 and 4 sides whose corner angles lie between 180° minus `transfinite_angle` (degrees, default 135) and `transfinite_angle` out of the QuadWild patch layout and mesh them as structured grids (4 sides) or three structured blocks around one inner node (3 sides), like the transfinite faces of gmsh; QuadWild still chooses their element counts together with the rest of the mesh and gives the opposite sides of a 4-sided face equal counts. A sweep source meshed by QuadWild follows the transfinite settings of its sweep mesh item. Every value that is not given keeps the application default. |
| `replace_meshing_parameters`, `replace_local_mesh_size`, `replace_shell_mmg`, `replace_thicken_shell_mesh`, `replace_netgen_mesh`, `replace_tetrahedral_gmsh`, `replace_shell_gmsh`, `replace_transfinite_mesh`, `replace_sweep_mesh`, `replace_shell_quad_wild` | Every `add_*` has a matching `replace_*` taking `old_name` first, then an optional `name` and `selection`, then the same arguments. A value that is not given is kept, and the region survives a replacement that does not name a new selection. |

An item the model cannot honour is **refused rather than recorded**, on an add and on a replace that
names a new selection, and the refusal says why - a shell mesher on a solid part, a swept mesh on
an STL part, a mesher defined on some of the sub-parts of a compound part and not the others. This is
the question the dialog asks before it applies. An item that was accepted and
later became invalid, because the part it covered was removed, becomes valid again as soon as a
replace points it at geometry it can be defined on: a replacement is judged by the definition it
carries, not by the verdict of the item it replaces.

`add_meshing_parameters` accepts `relative_size`, `factor_max`, `factor_min`, `max_size`,
`min_size`, `fineness`, `grading`, `elements_per_edge`, `elements_per_curve`, `second_order`,
`midside_nodes_on_geometry`, `split_compound_mesh`, `merge_compound_parts` and `merge_cells`.
`merge_cells`, `True` by default, meshes each part a split cut as one mesh part; `False` keeps its
cells as separate, conforming mesh parts, so they can carry different sections. The Netgen settings
`optimize_steps_2d`, `optimize_steps_3d`, `quad_dominated`, `transfinite_three_sided`,
`transfinite_four_sided` and `transfinite_angle` belong to `add_netgen_mesh`. The two switches, off by default,
mesh the 3-sided and the 4-sided faces every mesher structures as structured grids - a 3-sided face as three
blocks of quads around one inner node - with quads when `quad_dominated` is set and triangles otherwise. Opposite sides are balanced to the coarser count, a number of elements set on an
edge as a number is kept (one an element size gives is not), and the meshing is refused when the set numbers cannot
agree.
Like the dialog, it derives `max_size` and `min_size` from the size of the selected parts, and the
arguments of the call are applied on top of those. `replace_meshing_parameters` does the same when
it is given a new `selection` and no other value, so the sizes follow the geometry the item now
covers; a replace that also sets a value is the caller's and keeps everything it is not told about.
This is what the dialog does when the selection changes and nothing was typed into the property
grid.

`add_shell_mmg` accepts `relative_size`, `relative_hausdorff`, `hausdorff` and `keep_model_edges`.
Adding this item to an STL shell selects MMG; without an explicit mesher setup item, Netgen is used.
Like the dialog, it derives `hausdorff` from the size of the selected parts - the bounding box
diagonal times `relative_hausdorff` - and the arguments of the call are applied on top of that.
`replace_shell_mmg` rebuilds it from the newly selected parts under the same rule as
`replace_meshing_parameters`. MMG continues to use the shared maximum/minimum size and element order
values from `meshing_parameters`.

Every Gmsh item accepts the settings its dialog shows and no others, so the arguments differ per
item:

| Item | Settings |
| --- | --- |
| `tetrahedral_gmsh` | `algorithm_2d`, `algorithm_3d`, `recombine_min_quality`, `algorithm_recombine`, `transfinite_three_sided`, `transfinite_four_sided`, `optimize_first_order_solid`, `optimize_high_order` |
| `shell_gmsh` | `algorithm_2d`, `algorithm_3d`, `recombine_min_quality`, `algorithm_recombine`, `transfinite_three_sided`, `transfinite_four_sided`, `transfinite_angle`, `optimize_first_order_shell` |
| `transfinite_mesh` | `allow_pyramid_elements`, `algorithm_2d`, `algorithm_3d`, `recombine`, `transfinite_angle` |
| `sweep_mesh` | `number_of_global_smooth_steps`, `algorithm_2d` (a gmsh algorithm, or `QuadWild`: QuadWild meshes the source surfaces with quadrilaterals and chooses the element count of every source edge that no edge mesh size, other sweep block or neighbouring part fixes), `algorithm_3d`, `recombine_min_quality`, `algorithm_recombine`, `transfinite_three_sided`, `transfinite_four_sided`, `transfinite_angle`, `optimize_first_order_shell`, `elements_per_layer`, `keep_imprint_edges` (keep the edges carried across a prism to follow an imprint as edges of the mesh; off by default), `quad_wild_flow_profile`, `quad_wild_alpha`, `quad_wild_smooth`, `symmetry` (`Automatic` or `Off`, see `add_sweep_mesh`) |

A mesh setup item takes the selection its dialog would have recorded, which is also what a session
journal replays: a whole part mesher - `meshing_parameters`, `shell_mmg`, `shell_gmsh`,
`shell_quad_wild`, `thicken_shell_mesh`, `netgen_mesh`, `tetrahedral_gmsh` and
`transfinite_mesh` - takes a `parts`
selection or the `geometry` those parts cover, an element size takes `geometry`, a number of
elements takes `edges`, and a swept mesh takes `geometry_surfaces` - the
source faces it is built from. A swept mesh also takes `parts`, and then finds its source
faces itself. A target the item is not defined on is refused rather than silently
stored.

```python
part = pmx.model.geometry.parts.first()
selection = pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id])
pmx.model.mesh.setup_items.add_meshing_parameters('MP-1', selection, max_size=5.0)
failed = {name: why for name, why in pmx.model.mesh.create(part.name).items() if why}
```

### `MeshSetupItem`

| Member | Description |
| --- | --- |
| `name` | The item name. |
| `item_type` | The item type, for example `MeshingParameters` or `LocalMeshSize`. |
| `mesher_type` | `None`, `Netgen`, `Mmg`, `Gmsh` or `QuadWild`. |
| `active`, `valid` | Item state. |
| `get_geometry_ids()` | The geometry IDs the item was created from. |
| `get_settings()` | The stored mesher settings of this item type, as a dictionary. List valued settings such as `layer_sizes` and `num_of_elements_per_layer` are included, so the dictionary is the complete definition. |

### `Nodes` and `Elements` of the model mesh

Reached as `pmx.model.nodes` and `pmx.model.elements`. They are the editing side of the model mesh,
not the read-only result mesh of the same name under `pmx.results`.

| Member | Description |
| --- | --- |
| `nodes.count`, `elements.count` | The node and element counts of the model mesh. |
| `nodes.renumber(start_id=1)` | Renumbers the nodes using command history. |
| `nodes.merge_coincident(selection=None, distance=None)` | Merges coincident nodes. A selection that is not given means the whole mesh. |
| `elements.renumber(start_id=1)` | Renumbers the elements using command history. |
| `elements.create_element_set_from_quality(name, quality_type, limit, ...)` | Creates an element set from an element quality measure. |

### Part collections

`pmx.model.geometry.parts` and `pmx.model.parts` share the collection verbs plus the members below.
Every `part_names` argument accepts a single name or a list of names.

| Member | Description |
| --- | --- |
| `hide(part_names)`, `show(part_names)` | Part visibility. |
| `set_color(part_names, color)` | The color is `[R, G, B]`, `[R, G, B, A]`, or a color name such as `'Green'`. |
| `reset_color(part_names)` | Restores the default color. |
| `set_transparency(part_names, transparency)` | 0 is fully transparent, 255 is opaque. |
| `translate(part_names, translate_vector, number_of_copies=0)` | Translates the parts. |
| `rotate(part_names, rotate_center, rotate_axis, rotate_angle, number_of_copies=0)` | Rotates the parts about an axis; the angle is in degrees and right handed about the axis. |
| `mirror(part_names, mirror_point, mirror_direction, copy=False)` | Mirrors the parts over the plane through the point with the direction as its normal. |
| `scale(part_names, scale_center, scale_factors, copy=False)` | Scales the parts. |
| `get_element_type_names()` | The names of the 14 element type slots a part carries, for example `linear_tetra`. |
| `get_element_types(part_name)` | The element type of every slot of a part. |
| `set_element_type(part_names, element_type_name, value)` | Sets one element type slot using command history. |
| `replace_properties(old_name, name=None, color=None, element_types=None)` | Replaces the name, the colour and the element type slots in one call. The element types are a map of the slot name to its value, the way `get_element_types` names them. A value that is not given is kept. |

`GeometryParts` adds `create_compound`, `regenerate_compound`, `swap_geometries`,
`convert_shell_to_solid`, `convert_solid_to_shell`, `find_edges_by_angle`, `flip_face_orientations`,
`split_solid`, `split_shell`, `rename_split_sub_part`, `group_split_cells`, `fuse_bodies`, `defeature`, `offset_face`,
`delete_cad_faces`, `flip_stl_normals` and `delete_stl_faces`. The selection based methods take a selection built in the
`geometry` view, with the `geometry` target except for the two splits: `split_solid` takes the `parts` target and
`split_shell` the `geometry_surfaces` target, which is what the two dialogs pick.

Each of `defeature`, `flip_face_orientations`, `delete_cad_faces` and `delete_stl_faces` reports what
it did to every part it was asked about, as a `dict[str, int]`, and a part it could not touch reports
zero. They all act on one kind of part only - defeaturing on a CAD solid, flipping on a CAD shell,
deleting faces on a CAD solid or shell, deleting on an STL part - and a selection that names any other
kind is not refused, it simply leaves that part alone, which is what the zero says.

`delete_cad_faces` takes the faces out and leaves the gap open, where `defeature` closes it. A solid
that loses a face is no longer closed and becomes a shell part, keeping its name and part id. When the
faces a part keeps fall apart into more pieces that no longer touch than it had, the largest piece stays
the part and every other piece becomes a shell part of its own, named after it (`Shell_part-1-2`, ...). A
part whose last face is deleted is deleted with it - in a compound a face it shared with a neighbour
counts too. A compound holds only solids, so a sub-part or a split cell that loses a
face leaves it as a shell part with its own name and part id, and a face two of them share is deleted
from both. The solids that lost nothing stay the compound with its name and id, and one solid left
becomes a plain part with its name and id, as it does after `fuse_bodies`.

`defeature` returns the number of faces every selected part actually lost.
The modelling kernel returns a shape whether or not it closed anything, so a part that reports `0`
is a feature it could not remove: a blind hole is only closed when its bottom face is selected as
well as its wall.

`split_solid(selection, tool, fuzzy_tolerance=0)` and `split_shell(selection, tool,
fuzzy_tolerance=0)` are the Split Solid and Split Shell dialogs. The tool is made by one of the
`pmx.create.split_tool_*` methods, and both return the number of faces every part of the selection
gained, as a `dict[str, int]`, so a part the tool missed reports zero. A solid split names parts: one
sub-part of a compound names that solid only and the compound names all of them, and a part the tool
cuts in two is replaced by a compound part of the pieces, which does not keep its name. A shell split
names surfaces: only those are divided, the part keeps its name and stays the kind of body it is, and
a part whose divided surfaces would not give back the same body is left as it was. The line through
two points is drawn on a surface, so only `split_shell` takes it. The fuzzy tolerance is how far apart
the modelling kernel still takes geometry as coincident; leave it at zero unless an imported geometry
is too inaccurate to be split. A split whose parts the exploded view holds at different offsets is
refused, because the tool would not cut where it appears to.

```python
region = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])
tool = pmx.create.split_tool_plane_by_point_and_normal([30, 20, 10], [0, 0, 1])
added = pmx.model.geometry.parts.split_solid(region, tool)   # {'Solid-1': 1}

top = pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces', {part.id: [3]})
line = pmx.create.split_tool_line_by_two_points([[0, 0, 20], [60, 40, 20]])
pmx.model.geometry.parts.split_shell(top, line)
```

A compound part made by a solid split gathers its cells under the solids they were cut from, and
those are the sub-parts the tree shows. `part.split_sub_part_names` lists them, empty for any other
part, and `rename_split_sub_part(part_name, old_name, new_name)` renames one the way renaming it in
the tree does: its cells are renamed after it, and so is the mesh part it was meshed into. A name
another part already uses is refused, compared regardless of case as the tree compares it.

```python
compound = [part for part in pmx.model.geometry.parts if part.split_sub_part_names][0]
pmx.model.geometry.parts.rename_split_sub_part(compound.name, compound.split_sub_part_names[0], 'Web')
```

`group_split_cells(part_name, cell_names, sub_part_name=None)` is Group Cells in the tree: it puts the
named cells under one sub-part without changing the geometry, so that with the cells merged, the
default, they are meshed into one mesh part. That is how the regions of one solid cut for different
materials are gathered as the materials need them. The sub-part is the one named, joined if the
compound has it and created with a part id of its own otherwise, listed after the sub-parts the
compound already has. Without a name it is the largest sub-part whose cells are all among the named
ones, so a sub-part given whole together with further cells takes them in, and otherwise a new
sub-part named after the one the first cell belongs to. The
cells that join a sub-part take its color, a sub-part left without cells is gone, and the method
returns the name of the sub-part. A grouping that would change nothing is refused, and so is putting
all the cells of one sub-part under a new name, which is what `rename_split_sub_part` does.
`part.get_split_cell_names(sub_part_name)` lists the cells of a sub-part.

```python
steel = compound.get_split_cell_names('Web')[:2]
pmx.model.geometry.parts.group_split_cells(compound.name, steel, 'Steel')
```

`fuse_bodies(part_name, body_names)` is Fuse Bodies in the tree, and it changes the geometry: the
named bodies of a compound become one solid. A body is a sub-part of the compound, whether the
compound was imported, created with `create_compound` or made by `split_solid`; of a split compound
it may also be a cell, and a sub-part stands for all of its cells. The faces the solids share are
taken out and the edges left on their outside are healed where the faces on both sides lie on one
surface, so cells cut from one solid come back as that solid, with the faces and edges it had. Only
what the fused solids hold alone is healed, so the solids around them keep the faces they share with
the result and the compound stays conformal. The solids must touch over faces, and there must be at
least two of them; solids that only meet along an edge are refused.

The compound keeps its name and its id. The sub-parts of an ordinary compound that are not fused
keep their names, ids and colors, and the fused solid takes those of the first sub-part fused. Cells
of several sub-parts of a split compound are first grouped under one, as `group_split_cells` would
group them: the largest of those sub-parts whose cells are all named, or else the sub-part of the
first cell, and a sub-part left without cells is gone. Fusing the whole compound gives back a plain
part carrying the name and the id of the sub-part it ends in, which undoes a split. The method
returns the name of the fused solid, or of that part.

```python
pmx.model.geometry.parts.fuse_bodies(compound.name, ['Web', 'Flange'])
```

`offset_face(selection, offset=0)` is the one that adds rather than changes: the picked surfaces
are lifted out of the parts that carry them and become new shell parts, named after the part they
were taken from, and that part is left exactly as it was. It returns the names of the parts that
were created.

The unit of a face offset is the geometry the surfaces are written out of, and the surfaces picked on
one geometry are sewn together where they touch, so they come back as one body rather than as loose
faces. A selection spanning three parts therefore gives three parts, while the surfaces of several
sub-parts of one compound give one part, named after the compound: a sub-part carries no geometry of
its own, the compound is what all of them are written out of, and the faces of two sub-parts that
meet are sewn along the edge they share exactly as the faces of one part are.

A non zero `offset` moves the new surfaces along their own normals, outwards when it is
positive: half the wall thickness entered as a negative value on an outer surface is a mid-surface.
A connected patch is offset as a whole, so the faces that meet are trimmed against each other rather
than coming apart at the edge they share, and an offset the modelling kernel cannot make fails the
face offset for that part instead of returning the surfaces unmoved.

`ModelParts` adds `merge` and `find_edges_by_angle`.

```python
pmx.model.parts.translate('Solid-1', [0, 0, 10])
pmx.model.geometry.parts.rotate('Solid-1', [0, 0, 0], [0, 0, 1], 45, number_of_copies=7)
pmx.model.parts.set_color('Solid-1', [255, 0, 0])

selection = pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces', {1: [3]})
removed = pmx.model.geometry.parts.defeature(selection)   # {'Solid-1': 4}
names = pmx.model.geometry.parts.offset_face(selection, -1.5)   # ['Solid-1_Offset-1']
```

`translate` and `rotate` take `number_of_copies`: 0 moves the parts, and a larger value keeps them
and creates that many copies, copy k transformed k times, which makes a linear or a circular pattern.
`mirror` and `scale` take `copy` instead. A zero rotation axis or mirror normal is refused.

On geometry parts a move replaces every part in place, so it keeps its name, its part id and the
order of its faces, and a copy adds new parts. A CAD part is transformed exactly by the modelling
kernel, except that a non-uniform scale turns its curves and surfaces into splines. A compound is
transformed as one shape, so its sub-parts cannot be transformed on their own, and none of the
transforms can be done while the exploded view is on.

### `GeometryPart` and `ModelPart`

| Member | Description |
| --- | --- |
| `name`, `id` | The part name and ID. |
| `part_type` | `Solid`, `SolidAsShell`, `Shell`, `Wire`, `Compound` or `Unknown`. |
| `node_count`, `element_count` | Node and element counts. |
| `color` | The part color as `[R, G, B, A]`. |
| `visible`, `active` | Part state. |
| `volume`, `center_of_mass` | The canonical part measurements. Geometry parts use exact CAD values. Model parts keep mesh-computed values and use CAD only to fill a missing value. |
| `surface_area` | The total surface area. A mesh computes an area only for a shell part, so a meshed solid reports the area of the CAD part it was meshed from, and `0.0` when there is none - an imported mesh has none. The area of the mesh itself is the sum of `get_face_data().areas`. |
| `get_inertia_at_center_of_mass()`, `get_inertia_at_origin()` | The 3 by 3 inertia matrices, or an empty array when unavailable. They are **second moments of volume**: the density is 1, so the values are in model length units to the fifth power, and a mass inertia is the matrix multiplied by the material density. Only CAD geometry computes them, so a meshed part carries the matrices of the CAD part it was meshed from and an imported mesh has none. |
| `get_bounding_box()` | `[xMin, xMax, yMin, yMax, zMin, zMax]`. Geometry parts use the CAD entity bounds when available. |
| `get_node_ids()`, `get_element_ids()` | The part node and element IDs. |
| `get_node_coordinates()` | The coordinates of the part nodes with shape `(node_count, 3)`, in the order `get_node_ids()` reports them. This is the route to the nodes of a geometry part, because `pmx.model.nodes` is the model mesh and holds nothing until the model is meshed. |
| `get_face_data()` | The faces of the part as `FaceData`. |
| `get_edge_data()` | The edges of the part as `EdgeData`. |
| `get_vertex_data()` | The vertices of the part as `VertexData`. |
| `get_geometry_metadata()` | Supplemental CAD metadata such as analytic axes, primitive parameters, orientation, entity bounds and topology links. |

`GeometryPart` adds `is_cad_part` and `mesh_type`, the most structured mesh the recognition finds the
part supports whatever the mesh setup items ask for: `Transfinite`, `Sweep`, `Revolve`, `Tetrahedral`,
`Free` for a CAD shell, which gmsh meshes with a structured grid on the faces that can carry one, or
`Other` for an stl based part. A compound part reports the most restrictive type of its sub-parts,
except that one in which some cells sweep and others do not reports `Sweep`: the sweep meshes the cells it
cannot reach by tetrahedra. `Revolve` is a solid the sweep between two ends cannot mesh and that is the
revolution of a group of coplanar surfaces about an axis they touch; a sweep item, or the default sweep of a
part with no mesh setup item, meshes it by a revolved block. A revolved solid clear of the axis is left to the sweep.
The recognition depends on the geometry alone, so an exploded view does not change it.
It also adds `split_sub_part_names`, the sub-parts of a compound part made by `split_solid`,
which `rename_split_sub_part` renames; it is empty for any other part. `get_split_cell_names(sub_part_name)`
lists the cells of one of them, which `group_split_cells` takes.

`ModelPart` adds `get_inverted_element_ids()`, the IDs of the inverted or degenerate solid elements
of the part - those with a scaled Jacobian at a corner that is zero or negative. Meshing keeps such
elements rather than refusing the mesh: it writes `<part> has <n> inverted or degenerate elements`
to the output window, where the line is a link that highlights them, and highlights them once
meshing ends. `mesh.create` reports such a part as meshed, so a script finds the elements here.

#### `FaceData`, `EdgeData` and `VertexData`

The topology of one part, read off the visualization the application keeps for picking. It is how a
script finds the entity it means without a pick in the viewport, and the IDs it reports are the ones
a selection takes.

| Member | Description |
| --- | --- |
| `count` | The number of faces, edges or vertices. |
| `ids` | The 1-based face or edge IDs of the part, as the `surfaces` and `edges` selection sources take them. `VertexData` has `node_ids` instead, because a vertex is named by its node. |
| `geometry_ids` | The geometry ID of every entity, as the `geometry` selection source takes them. |
| `areas`, `lengths` | The canonical area of every face and length of every edge, in model units. Geometry parts use exact CAD values; model and result parts keep mesh-computed values. |
| `centroids` | The centroid of every face or edge with shape `(count, 3)`. Geometry parts use exact CAD centroids when available; mesh parts use the mean of their node coordinates. `VertexData` has `coordinates`. |
| `normals` | Faces only. The mean of the cell normals of every face with shape `(count, 3)`. A curved face has no single normal, so it is the direction the face points on average. |
| `types` | The geometric type of every entity: `Plane`, `Cylinder`, `Cone` and so on for a face, `Line`, `Circle`, `Ellipse` and so on for an edge. |

Every array is a NumPy array where NumPy is available, and an `IntList` or `NumericArray` otherwise.
A part type that has no faces - a wire, or a compound whose surfaces are picked on its sub parts -
reports `count` 0 rather than failing.

#### `GeometryMetadata`

`get_geometry_metadata()` returns an extensible view of the versioned metadata appended to a Netgen
`.vis` file. `available`, `format_version`, `coordinate_system`, `length_unit` and `entity_kinds`
describe the block. `get_entities(kind)` returns the solids, shells, faces or edges as an iterable
`GeometryMetadataEntities` collection; `get(id)` resolves a 1-based entity ID.

Each `GeometryMetadataEntity` exposes `kind`, `id`, `property_names` and
`get_property(name, default=None)`. Properties are passed through generically, so a new Netgen
property becomes available without adding a dedicated Python member. Scalar values return `float` or
`str`; vectors return a NumPy array, `NumericArray` or `StringList`. Common properties include
`axis`, `x_direction`, `y_direction`, `radius`, `center`, `apex`, `major_radius`, `minor_radius`,
`orientation`, `bounding_box`, `face_ids` and `edge_ids`. Circular edges add `closed` and
`sweep_angle_rad`. Cylindrical faces belonging to one solid add `convexity`: `Convex` means the
material lies toward the cylinder axis, while `Concave` means it lies away from the axis.

The property set varies by entity type - a cylinder has a `radius`, a torus adds `center`,
`major_radius` and `minor_radius`, a cone has `apex`, `semi_angle_rad` and `reference_radius` and no
`radius` at all - so `get_property` returns the default rather than raising when this entity does not
carry the name, and a caller that walks faces of mixed types does not have to test `property_names`
before every read. An empty name, and a canonical measurement that belongs to the part, face or edge
data instead, are still refused.

Area, length, volume, centroid and type are deliberately absent from `property_names`: the part,
`FaceData` and `EdgeData` members above are their single canonical source. During meshing, PrePoMax
copies only properties missing on the target and only maps face or edge metadata when the topology
counts still agree. Results receive the resulting metadata with their mesh parts.

```python
part = pmx.model.geometry.parts.first()
metadata = part.get_geometry_metadata()
for face in metadata.get_entities('face'):
    radius = face.get_property('radius')            # None on a face that carries no radius
    if radius is not None:
        print(face.id, radius, face.get_property('axis'))
```

```python
part = pmx.model.parts['Solid_part-1']
faces = part.get_face_data()
# The top face is the one whose normal points up
top = max(range(faces.count), key=lambda i: faces.normals[i][2])
selection = pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces',
                                        {part.id: [int(faces.ids[top])]})
```

### `Amplitudes`

| Member | Description |
| --- | --- |
| `add(name, time_amplitude, time_span=None)` | Adds a tabular amplitude. Every row is `[time, amplitude]`. |
| `replace(old_name, name=None, time_amplitude=None, time_span=None)` | Replaces an existing amplitude. A value that is not given is kept. |

`Amplitude` exposes `name`, `time_span`, `row_count` and `get_time_amplitude()`.

### `Distributions`

| Member | Description |
| --- | --- |
| `add_from_equation(name, equation)` | One equation string for the magnitude, or three, one per direction. |
| `add_from_file(name, file_name, interpolator_type=None, interpolator_radius=None, scale=None, translate=None)` | Reads the distribution from a point cloud file. |
| `replace_from_equation(old_name, name, equation)` | Replaces an equation distribution. |
| `replace_from_file(old_name, name=None, file_name=None, interpolator_type=None, interpolator_radius=None, scale=None, translate=None)` | Replaces a file distribution. A value that is not given is kept. |

`Distribution` exposes `name` and `distribution_type`.

### `InitialConditions`

| Member | Description |
| --- | --- |
| `add_temperature(name, region, temperature, distribution_name=None)` | Initial temperature. The selection target is `nodes`. A distribution scales the temperature over the region. |
| `add_translational_velocity(name, region, v1, v2, v3)` | Initial translational velocity. |
| `add_angular_velocity(name, region, axis_point, axis_direction, rotational_speed)` | Initial angular velocity. |
| `replace_temperature(old_name, name=None, region=None, temperature=None, distribution_name=None)` | Replaces an initial temperature. A value that is not given is kept. |
| `replace_translational_velocity(old_name, name=None, region=None, v1=None, v2=None, v3=None)` | Replaces an initial translational velocity. |
| `replace_angular_velocity(old_name, name=None, region=None, axis_point=None, axis_direction=None, rotational_speed=None)` | Replaces an initial angular velocity. |
| `hide(names)`, `show(names)` | Initial condition visibility. |

`InitialCondition` exposes `name`, `condition_type`, `region_name`, `region_type`, `visible`, `valid`
and `get_equations()`, which returns the stored values as the numbers or expressions they were
entered with.

```python
pmx.model.amplitudes.add('Amp-1', [[0, 0], [1, 1]])
pmx.model.distributions.add_from_equation('D-1', 'x + y')
nodes = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3])
pmx.model.initial_conditions.add_temperature('IC-1', nodes, 20)
```

### `Materials`

| Member | Description |
| --- | --- |
| `add(name, properties, description=None, temperature_dependent=False)` | Adds a material. The properties are created by `pmx.create`. Set `temperature_dependent=True` to write every row with its temperature. |
| `replace(old_name, name=None, properties=None, description=None, temperature_dependent=None)` | Replaces an existing material. Omitted values are kept. |

`Material` exposes `name`, `description`, `temperature_dependent`, `property_count` and
`get_property_types()`.

```python
pmx.model.materials.add('Steel', [pmx.create.elastic(210000, 0.3),
                                  pmx.create.density(7.85e-9)])
# A single property does not have to be wrapped in a list
pmx.model.materials.add('Alu', pmx.create.elastic_with_density(70000, 0.33, 2.7e-9))
# Temperature dependent - the last value of every row is the temperature
pmx.model.materials.add('Steel-T',
                        pmx.create.elastic([[210000, 0.3, 0], [180000, 0.32, 400]]),
                        temperature_dependent=True)
```

A material may hold only one elastic property; the model rejects a second one.

### `Sections`

| Member | Description |
| --- | --- |
| `add_solid(name, material_name, selection, thickness=1)` | Solid section. The selection target is `elements`. |
| `add_shell(name, material_name, selection, thickness=1, offset=0)` | Shell section. The offset of the mid surface from the selected geometry is given in shell thicknesses. |
| `add_membrane(name, material_name, selection, thickness=1, offset=0)` | Membrane section. The offset is given in membrane thicknesses. |
| `add_point_mass(name, selection, mass)` | Point mass section. The selection target is `nodes`. |
| `add_distributed_mass(name, selection, mass)` | Distributed mass section. |
| `replace_solid(old_name, name=None, material_name=None, region=None, thickness=None)` | Replaces a solid section. A value that is not given is kept. |
| `replace_shell(old_name, name=None, material_name=None, region=None, thickness=None, offset=None)` | Replaces a shell section. |
| `replace_membrane(old_name, name=None, material_name=None, region=None, thickness=None, offset=None)` | Replaces a membrane section. |
| `replace_point_mass(old_name, name=None, region=None, mass=None)` | Replaces a point mass section. |
| `replace_distributed_mass(old_name, name=None, region=None, mass=None)` | Replaces a distributed mass section. |

A replacement is typed: replacing a solid section as a shell section is refused rather than silently
turning it into one.

`Section` exposes `name`, `section_type`, `material_name`, `region_name`, `region_type`, `thickness`
and `valid`.

```python
elements = pmx.create.selection_by_ids('femodel', 'elements', 'elements', [1, 2, 3])
pmx.model.sections.add_solid('Sec-1', 'Steel', elements)
```

A section is passed a selection, not a named region. The application converts it into an internal
element or node set named after the section, exactly as it does for a section created in a dialog, so
`region_type` reads back as `ElementSetName` rather than `Selection`.

### `NodeSets`

| Member | Description |
| --- | --- |
| `add(name, selection)` | Creates a node set from a selection and adds it to the model. |
| `replace(old_name, name=None, selection=None)` | Replaces an existing node set. A value that is not given is kept. |

```python
selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4])
node_set = pmx.model.node_sets.add('Fixed', selection)
print(node_set.count, node_set.get_node_ids())
```

### `NodeSet`

| Member | Description |
| --- | --- |
| `name` | The node set name. |
| `count` | The number of nodes. |
| `valid` | True when the node set resolved during the last regeneration. |
| `is_internal` | True when the node set was created internally for another model item. |
| `get_node_ids()` | The node IDs. |
| `get_center_of_gravity()` | The center of gravity. |

### `ElementSets`

| Member | Description |
| --- | --- |
| `add(name, selection)` | Creates an element set from a selection. The selection target is `elements`. |
| `replace(old_name, name=None, selection=None)` | Replaces an existing element set. A value that is not given is kept. |
| `convert_to_mesh_parts(names)` | Converts the named element sets into mesh parts. |

`ElementSet` exposes `name`, `count`, `valid`, `is_internal` and `get_element_ids()`.

### `Surfaces`

| Member | Description |
| --- | --- |
| `add(name, selection)` | Creates a surface from a selection. The selection target is `surfaces`. |
| `add_from_node_set(name, node_set_name)` | Creates a surface from an existing node set. |
| `replace(old_name, name=None, selection=None)` | Replaces an existing surface. A value that is not given is kept. |
| `replace_from_node_set(old_name, name=None, node_set_name=None)` | Replaces a surface by another node set. A node set that is not given is the one the surface already uses. |

`Surface` exposes `name`, `created_from` (`Selection`, `NodeSet` or `Faces`), `surface_type`
(`Element` or `Node`), `area`, `node_set_name`, `valid`, `is_internal` and `get_face_ids()`.

### `ReferencePoints`

| Member | Description |
| --- | --- |
| `add(name, coordinates, name_visible=None, color=None)` | Creates a reference point at `[x, y, z]`. |
| `add_from_selection(name, created_from, selection, name_visible=None, color=None)` | Creates a reference point whose coordinates the mesh computes from selected points. |
| `add_from_region(name, created_from, region_type, region_name, name_visible=None, color=None)` | Creates a reference point whose coordinates the mesh computes from a named region. |
| `replace(old_name, name=None, coordinates=None, name_visible=None, color=None)` | Replaces an existing reference point. Coordinates that are not given are kept, and giving them turns the point back into a coordinate point. |
| `replace_from_selection(old_name, name=None, created_from=None, selection=None, name_visible=None, color=None)` | Replaces a reference point by selected mesh points. |
| `replace_from_region(old_name, name=None, created_from=None, region_type=None, region_name=None, name_visible=None, color=None)` | Replaces a reference point by a named region. |
| `hide(names)`, `show(names)` | Reference point visibility. |

`created_from` decides which of the two add methods applies. `OnPoint`, `BetweenTwoPoints`,
`CircleCenterBy3Points` and the other point modes take a selection with exactly as many points as
the mode consumes; `CenterOfGravity` and `BoundingBoxCenter` take a named region instead, whose
type is `NodeSetName` or `SurfaceName`.

`name_visible` decides whether the point name is drawn next to its symbol, and `color` takes
`[red, green, blue]` or `[red, green, blue, alpha]`. Both are part of the stored definition and keep
their defaults when they are not given.

`ReferencePoint` exposes `name`, `created_from`, `name_visible`, `color`, `visible`, `valid`,
`get_coordinates()` and `get_construction()`, which returns how the point is defined: its mode, and
the region or the selected point IDs it is built from.

### `CoordinateSystems`

| Member | Description |
| --- | --- |
| `add(name, origin, point_on_x, point_in_xy, coordinate_system_type=None, name_visible=None)` | Creates a coordinate system from three points. The type is `Rectangular` or `Cylindrical`. |
| `add_from_selections(name, origin_created_from=None, origin_selection=None, ..., origin=None, point_on_x=None, point_in_xy=None, coordinate_system_type=None, name_visible=None)` | Creates a coordinate system whose points the mesh computes from selections. Each of the three points takes either a mode and its selection together, or its coordinates. |
| `replace(old_name, name=None, origin=None, point_on_x=None, point_in_xy=None, coordinate_system_type=None, name_visible=None)` | Replaces a coordinate system. A value that is not given is kept. |
| `replace_from_selections(old_name, name=None, ...)` | Replaces a coordinate system whose points come from selections or coordinates. |
| `hide(names)`, `show(names)` | Coordinate system visibility. |

A point given as coordinates and a point given as a selection can be mixed in one coordinate system,
which is what the dialog offers when one point is picked and another typed.

`CoordinateSystem` exposes `name`, `coordinate_system_type`, `name_visible`, `visible`, `valid`, `get_origin()`,
`get_point_on_x()`, `get_point_in_xy()`, `get_equations()` and `get_construction_modes()`, which
tells how each of the three points is defined.

```python
top = pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', {1: [1]})
pmx.model.surfaces.add('Top', top)
pmx.model.reference_points.add('RP-1', [0, 0, 100])
pmx.model.coordinate_systems.add('CS-1', [0, 0, 0], [1, 0, 0], [0, 1, 0])
```

A region name is unique across **all** mesh entities, so a surface cannot take a name already used by
a node set, an element set or a part.

### `Keywords`

Reached as `pmx.model.keywords`. It is the CalculiX keyword editor: a keyword is addressed by the
index path it has in the generated keyword tree, which is the path the editor tree shows, so a script
edits the keyword the user would click.

| Member | Description |
| --- | --- |
| `count`, `override_count` | The number of user keywords and of edited generated keywords. |
| `get_model_keywords()` | The generated keywords, each with the index path that addresses it and its complete text. |
| `get_user_keywords()` | The user keywords, each with the index path it is inserted at. |
| `get_overrides()` | The user edits of the generated keywords, each with its `indices`, `data`, `disabled`, `suppressed` and `original_signature`. |
| `set_user_keyword(indices, data)` | Inserts or replaces one user keyword. The index path names an insertion position, so its last index may be the number of keywords it is inserted among. |
| `remove_user_keyword(indices)` | Removes one user keyword. |
| `replace_user_keywords(keywords)` | Replaces every user keyword by index path and text pairs. |
| `set_override(indices, data=None, disabled=None)` | Edits one generated keyword. The text replaces it and `disabled` writes it as a comment. A value that is not given is kept. |
| `remove_override(indices)`, `clear_overrides()` | Removes the user edits. |

An override also stores a fingerprint of the generated keyword it was made against, which the
application uses to drop an edit whose keyword has since changed. It is derived here rather than
taken from the caller, so `set_override` names only the keyword and the change.

An index path that addresses nothing is refused when it is given. A path that stops addressing
anything **later**, because the model changed, is not an error: the export suppresses that keyword or
override and reports it, and `suppressed` reads back that state. Suppression is therefore a stored
property of an entry rather than a rejected input, and editing one entry leaves the suppression of
the others alone.

```python
generated = pmx.model.keywords.get_model_keywords()
pmx.model.keywords.set_user_keyword(generated[-1].indices, '*NO ANALYSIS')
pmx.model.keywords.set_override(generated[0].indices, disabled=True)
```

### `Tree`

Reached as `pmx.model.tree`. It groups and orders the items of the model tree. `view` is `Geometry`,
`Model` or `Results`, and `item_type` names the collection being organized. `get_item_types(view)`
returns the collections a view supports:

| View | `item_type` |
| --- | --- |
| `Geometry` | `parts`, `mesh_setup_items` |
| `Model` | `parts`, `node_sets`, `element_sets`, `surfaces`, `reference_points`, `coordinate_systems`, `materials`, `sections`, `constraints`, `surface_interactions`, `contact_pairs`, `distributions`, `amplitudes`, `initial_conditions`, `jobs`, and the step owned `history_outputs`, `field_outputs`, `boundary_conditions`, `loads`, `defined_fields` |
| `Results` | `parts`, `node_sets`, `element_sets`, `surfaces`, `reference_points`, `coordinate_systems` |

A step owned collection also takes `step_name`, the step that owns it; every other collection refuses
that argument. The steps themselves are not a groupable collection.

| Member | Description |
| --- | --- |
| `get_item_types(view)` | The collections that can be organized in a view. |
| `create_group(view, item_type, group_name, group_id=None, parent_group_id=None, target_index=0, item_names=None, step_name=None)` | Creates a group and optionally moves items into it. Returns the group id. |
| `rename_group(view, item_type, group_id, group_name, step_name=None)` | Renames a group. |
| `remove_groups(view, item_type, group_ids, step_name=None)` | Removes groups. The items they hold stay in the tree. |
| `move_items(view, item_type, item_names, target_group_id=None, target_index=0, step_name=None)` | Moves items into a group, or out of every group. |
| `move_group(view, item_type, group_id, target_group_id=None, target_index=0, step_name=None)` | Moves a group. |

A group is identified by a GUID that the application stores with the layout. `create_group` generates
one when it is not given and returns it, so a script does not have to invent an id while a journal
can pass the recorded one.

```python
group = pmx.model.tree.create_group('Model', 'parts', 'Housing', item_names=['Solid-1'])
pmx.model.tree.move_items('Model', 'parts', 'Solid-2', target_group_id=group)
```

## Interactions

### `Constraints`

Reached as `pmx.model.constraints`. Every region argument is a `Selection` or a `Region`. Every
`add_*` has a matching `replace_*` taking `old_name` first, then an optional `name`, then the
arguments of its `add_*`, all optional.

| Member | Selection target | Description |
| --- | --- | --- |
| `add_tie(name, master_region, slave_region, position_tolerance=None, adjust=None)` | surfaces, surfaces | A tie. |
| `add_cyclic_symmetry(name, master_region, slave_region, region=None, axis_point=None, axis_direction=None, number_of_sectors=None, number_of_graph_sectors=None, check_sector_angle=None, position_tolerance=None, adjust=None)` | surfaces, surfaces, elements | A cyclic symmetry. |
| `add_joint(name, master_region, slave_region, joint_type=None, center_defined_by=None, center=None, axis_defined_by=None, axis_direction=None, formulation=None, stiffness_defined_by=None, stiffness=None)` | surfaces, surfaces | A joint between two surfaces: Hinge, Cylindrical, Slider, Ball or Weld, written as rigid bodies or as springs. |
| `add_rigid_body(name, reference_point_name, region)` | nodes | A rigid body. |
| `add_kinematic_coupling(name, reference_point_name, region, u1=None, u2=None, u3=None)` | surfaces | A kinematic coupling. |
| `add_distributing_coupling(name, reference_point_name, region, u1=None, ..., ur3=None)` | surfaces | A distributing coupling. |
| `add_compression_only(name, region, clearance=None, spring_stiffness=None, tensile_force_at_negative_infinity=None, offset=None, non_linear=None)` | surfaces | A compression only constraint. |
| `add_point_spring(name, region, k1=None, k2=None, k3=None)` | nodes | A point spring. |
| `add_surface_spring(name, region, k1=None, k2=None, k3=None, stiffness_per_area=None)` | surfaces | A surface spring. |
| `swap_master_slave(names)` | | Swaps the two sides. |
| `merge_by_master_slave(names)` | | Merges the constraints that share a master and a slave region. |
| `hide(names)`, `show(names)` | | Symbol visibility. |

`Constraint` exposes `name`, `constraint_type`, `active`, `visible`, `master_region_type`,
`master_region_name`, `slave_region_type`, `slave_region_name`, and the values of its own type
through `get_value_names()` and `get_value(name)`.

```python
pmx.model.constraints.add_tie('Tie-1', master_selection, slave_selection)
pmx.model.constraints.add_rigid_body('RB-1', 'RP-1', node_selection)
```

The three region shapes are not obvious from the type names:

- A **tie** and a **cyclic symmetry** have a master and a slave region, both surfaces.
- A **rigid body** and the two **couplings** name a reference point, which is stored as the *master*
  side, so `master_region_type` reads back as `ReferencePointName` and the region itself is the slave
  side.
- The **springs** and the **compression only** constraint have a single region, which is stored as
  the master side.

A cyclic symmetry adds a third region on top of its master and slave surfaces: the **result expansion
region**, used only when `number_of_graph_sectors` is larger than 1. Giving it with a single graph
sector is rejected, because the application would discard it. `sector_angle_deg` is derived as
`360 / number_of_sectors` and is read through `get_value`.

A **joint** connects two surfaces of a 3D model and locks their relative motion about the joint centre
according to `joint_type`: `Hinge`, `Cylindrical`, `Slider`, `Ball` or `Weld`. `center_defined_by` is
`MasterSurface` or `Coordinates` and `axis_defined_by` is `MasterSurface` or `UserDefined`; the master
surface gives its centroid and, when it is cylindrical, its axis. A given `center` switches the centre
to `Coordinates` and a given `axis_direction` switches the axis to `UserDefined`. The dialog offers
three further centre modes - `OnPoint`, `BetweenTwoPoints` and `CircleCenter` - which pick one, two or
three nodes in the viewport and resolve to the centre coordinates; a script cannot pick, so they are
refused here and `center` is given instead. A session journal emits a picked centre as `Coordinates`
and the coordinates it resolved to.

`formulation` decides how the joint is written. `RigidBody`, the default, makes each surface a rigid
body whose reference and rotation nodes sit at the joint centre and couples the two sides by
`*EQUATION` cards; it is exact for a ball and a weld, and for a hinge, cylindrical or slider joint
whose axis direction stays fixed in space, but a rigid body on shell elements does not converge in a
geometrically nonlinear step. `Springs` connects every surface node by axial `SPRINGA` elements to a
frame of nodes on the joint axis and ties the master and slave frames by linear equations; it needs
no rigid body, solves on shell elements, and is as stiff as its springs. `stiffness_defined_by` is
`FromModel`, where each spring gets 100 times the Young's modulus of the surfaces times the square
root of the surface area divided by the number of surface nodes, or `UserDefined`; a given `stiffness`
switches it to `UserDefined` and must be positive. The values read back through `get_value` are
`joint_type`, `center_definition`, `x`, `y`, `z`, `axis_definition`, `n1`, `n2`, `n3`, `formulation`,
`stiffness_definition` and `stiffness`.

### `SurfaceInteractions`

Reached as `pmx.model.surface_interactions`. A surface interaction is composed of properties made by
`pmx.create`, the same way a material is composed of material properties.

| Member | Description |
| --- | --- |
| `add(name, properties)` | Adds a surface interaction. One property, or a list of them. |
| `replace(old_name, name, properties)` | Replaces a surface interaction. |

`SurfaceInteraction` exposes `name`, `property_count` and `get_property_types()`.

The three properties are made by `pmx.create`:

| Member | Description |
| --- | --- |
| `friction(coefficient, stick_slope=None)` | Friction. |
| `gap_conductance(conductance=None, conductance_table=None)` | Gap conductance. Give one or the other: a value for a constant one, or rows of `[conductance, pressure, temperature]` for a tabular one. |
| `surface_behavior(pressure_overclosure_type=None, c0=None, p0=None, k=None, s_inf=None, pressure_overclosure=None)` | Surface behavior. The type is `Hard`, `Linear`, `Exponential`, `Tabular` or `Tied`. |

Setting `pressure_overclosure_type` resets the values to the defaults that type implies, so the API
applies it first and a value given alongside it overrides that default.

```python
pmx.model.surface_interactions.add('SI-1', [pmx.create.friction(0.1),
                                            pmx.create.surface_behavior('Linear', k=2000)])
```

### `ContactPairs`

Reached as `pmx.model.contact_pairs`. A contact pair has two regions, so it takes a master and a
slave one; both are a `Selection` or a `Region`, and both name surfaces.

| Member | Description |
| --- | --- |
| `add(name, surface_interaction_name, master_region, slave_region, method=None, small_sliding=None, adjust=None, adjustment_size=None)` | Adds a contact pair. `method` is `NodeToSurface`, `SurfaceToSurface` or `Mortar`. |
| `replace(old_name, name=None, ...)` | Replaces a contact pair. A value that is not given keeps what the contact pair carries. |
| `swap_master_slave(names)` | Swaps the two sides. |
| `merge_by_master_slave(names)` | Merges the contact pairs that share a master and a slave surface. |
| `hide(names)`, `show(names)` | Symbol visibility. |

`ContactPair` exposes `name`, `surface_interaction_name`, `method`, `small_sliding`, `adjust`,
`adjustment_size`, `active`, `visible`, and `master_region_type`, `master_region_name`,
`slave_region_type` and `slave_region_name`.

```python
pmx.model.contact_pairs.add('CP-1', 'SI-1', master_selection, slave_selection)
```

All the contact pairs of a model must use the same method. **Small sliding is supported by
`NodeToSurface` and `Mortar` only** - the model clears it for `SurfaceToSurface` rather than refusing
it, so the API rejects that combination instead of letting the request be dropped. An
`adjustment_size` turns the adjustment on, the way the dialog presents the two together, and
`adjustment_size` reads back as `None` when no size is given.

## Steps and step content

The names in this section belong to the **model** tree under `pmx.model.steps`. The results side has
collections of the same name under `pmx.results`, documented further below.

Two rules hold everywhere in this section:

- **Every value is an optional named argument, and a value that is not given is not written.** A new
  item therefore keeps the application default, and `replace_*` keeps whatever the item already
  carries. `replace_static('Step-1', nlgeom=False)` changes one value and leaves the rest alone.
- **The type cannot change on a replace**, because `replace_*` clones the existing item, the same
  restriction the dialog has. Changing the type is a `remove` followed by an `add`.

Every `add_*` listed below has a matching `replace_*` - `add_static` and `replace_static`,
`add_pressure` and `replace_pressure`, and so on. A `replace_*` takes `old_name` first, then an
optional `name` that renames the item in the same call, and then the arguments of its `add_*`, all
of them optional. Only the `replace_*` methods that differ from that pattern are listed separately.

### `Steps` (model)

Reached as `pmx.model.steps`. It shares the collection verbs, including `activate` and `deactivate`.

| Member | Description |
| --- | --- |
| `add_static(name, time_period=None, initial_time_increment=None, min_time_increment=None, max_time_increment=None, direct=None, ...)` | A static step. |
| `add_slip_wear(name, ...)` | A slip wear step. It takes the arguments of `add_static`. |
| `add_boundary_displacement(name, ...)` | A boundary displacement step, which follows a slip wear step. |
| `add_frequency(name, num_of_frequencies=None, lower_frequency=None, upper_frequency=None, storage=None, cyclic_symmetry_mode_min=None, cyclic_symmetry_mode_max=None, ...)` | A frequency step. |
| `add_complex_frequency(name, num_of_frequencies=None, ...)` | A complex frequency step. |
| `add_buckle(name, num_of_buckling_factors=None, accuracy=None, ...)` | A buckle step. |
| `add_modal_dynamics(name, steady_state=None, relative_error=None, modal_damping=None, ...)` | A modal dynamics step. It also takes the arguments of `add_static`. |
| `add_steady_state_dynamics(name, harmonic=None, lower_frequency=None, upper_frequency=None, num_data_points=None, bias=None, num_fourier_terms=None, time_lower=None, time_upper=None, modal_damping=None, ...)` | A steady state dynamics step. |
| `add_dynamic(name, alpha=None, solution_procedure=None, relative_to_absolute=None, damping=None, ...)` | A dynamic step. It also takes the arguments of `add_static`. |
| `add_heat_transfer(name, steady_state=None, deltmx=None, ...)` | A heat transfer step. It also takes the arguments of `add_static`. |
| `add_uncoupled_temp_disp(name, ...)` | An uncoupled temperature displacement step. It takes the arguments of `add_heat_transfer`. |
| `add_coupled_temp_disp(name, ...)` | A coupled temperature displacement step. It takes the arguments of `add_heat_transfer`. |
| `move(names, target_index)` | Moves the named steps. The index is **zero based** and is evaluated after the moved steps have been removed. |

Every `add_*` has a matching `replace_*` that takes `old_name` first and `name` second, and every
`add_*` ends with the arguments shared by all step types:

| Shared argument | Description |
| --- | --- |
| `nlgeom` | Accounts for the geometric nonlinearity. |
| `max_increments` | The maximum number of increments. |
| `incrementation_type` | `Default`, `Automatic` or `Direct`. |
| `solver_type` | `Default`, `PaStiX`, `Pardiso`, `Spooles`, `IterativeScaling` or `IterativeCholesky`. A new step starts from the solver type in the application settings. |
| `output_frequency` | The results output frequency. |
| `perturbation` | Makes the step a perturbation step. The model rejects it for a step type that does not support one. |
| `run_analysis` | False solves nothing and only checks the model. |
| `copy_bcs_and_loads` | `add_*` only. Copies the boundary conditions, loads and defined fields of the last step into the new one. |

```python
step = pmx.model.steps.add_static('Step-1', time_period=1.0, nlgeom=True)
pmx.model.steps.replace_static('Step-1', max_time_increment=0.1)
```

A new step of every type already carries the default field outputs the application would create, so
`len(step.field_outputs)` is 2 before anything is added.

### `Step` (model)

| Member | Description |
| --- | --- |
| `name`, `step_type` | The step name and the type, for example `StaticStep`. |
| `active` | True when the step is written to the solver input file. |
| `nlgeom`, `perturbation`, `run_analysis` | The shared solution flags. |
| `max_increments`, `incrementation_type`, `solver_type`, `output_frequency` | The shared solution values. |
| `bcs` | The boundary conditions of the step. |
| `loads` | The loads of the step. |
| `field_outputs` | The field outputs of the step. |
| `history_outputs` | The history outputs of the step. |
| `defined_fields` | The defined fields of the step. |
| `get_value_names()` | The names of the values specific to this step type. |
| `get_value(name)` | One value specific to this step type, for example `time_period`. |
| `get_control_types()` | The type names of the step control parameters. |
| `set_controls(parameters)` | Replaces the step control parameters. They are made by `pmx.create`. |
| `clear_controls()` | Removes the step control parameters. |

The values shared by every step type are properties; the values of the concrete type are read by
name, because they differ per type. `get_value_names()` lists what a given step offers.

```python
print(list(step.get_value_names()))
print(step.get_value('time_period'))
step.set_controls([pmx.create.time_incrementation_control(i0=8, ic=32)])
```

A step control set is replaced as a whole, because the application has a single command for it. A
`Damping` or `ModalDamping` value is flattened into `damping_type`, `damping_alpha`, `damping_beta`,
`damping_viscous_damping_ratio` and `damping_ratio_count`.

### `BoundaryConditions`

Reached as `step.bcs`. It shares the collection verbs. Every `region` argument is a `Selection` or a
`Region`, and the selection target is `nodes` for all four types.

| Member | Description |
| --- | --- |
| `add_fixed(name, region, color=None)` | A fixed boundary condition. |
| `add_displacement_rotation(name, region, u1=None, u2=None, u3=None, ur1=None, ur2=None, ur3=None, amplitude_name=None, coordinate_system_name=None, phase_deg=None, color=None)` | A displacement and rotation boundary condition. |
| `add_submodel(name, region, step_number=None, u1=None, u2=None, u3=None, ur1=None, ur2=None, ur3=None, color=None)` | A submodel boundary condition. Its degrees of freedom are booleans. |
| `add_temperature(name, region, temperature, distribution_name=None, amplitude_name=None, color=None)` | A temperature boundary condition. |
| `propagate(name)` | Copies the boundary condition to every following step. |
| `hide(names)`, `show(names)` | Symbol visibility. |

```python
step.bcs.add_fixed('Fix-1', pmx.create.region('NodeSetName', 'Bottom'))
step.bcs.add_displacement_rotation('BC-1', selection, u1=0, u2=0, u3=1.5)
```

A degree of freedom of a displacement and rotation boundary condition that is **not given is left
unconstrained**, and reads back as `None`. Because a missing argument means "keep" on a replace,
`replace_displacement_rotation` takes `free_dofs=['U3']` to unconstrain one again.

A reference point region is accepted only by a fixed and by a displacement and rotation boundary
condition, the two types the dialogs offer it for. `add_submodel` and `add_temperature` refuse
one before a command is recorded.

The step decides which types it supports - a static step takes no temperature boundary condition, a
heat transfer step takes nothing else - and the API rejects an unsupported type before it records a
command, naming the step type.

### `BoundaryCondition`

| Member | Description |
| --- | --- |
| `name`, `step_name` | The boundary condition name and the step it belongs to. |
| `boundary_condition_type` | `FixedBC`, `DisplacementRotation`, `SubmodelBC` or `TemperatureBC`. |
| `active`, `visible` | Item state. |
| `region_type`, `region_name` | The region the boundary condition is applied to. |
| `amplitude_name`, `coordinate_system_name` | The names the boundary condition refers to. |
| `color` | The symbol color as `[R, G, B, A]`. |
| `get_value_names()`, `get_value(name)` | The values specific to this boundary condition type. |

### `Loads`

Reached as `step.loads`. It shares the collection verbs. Every `region` argument is a `Selection` or
a `Region`; the selection target follows the load type.

| Member | Selection target | Description |
| --- | --- | --- |
| `add_concentrated_force(name, region, f1=None, f2=None, f3=None, ...)` | nodes | A concentrated force. |
| `add_moment(name, region, m1=None, m2=None, m3=None, ...)` | nodes | A moment. |
| `add_pressure(name, region, magnitude, distribution_name=None, ...)` | surfaces | A pressure. |
| `add_hydrostatic_pressure(name, region, first_point=None, second_point=None, direction=None, first_point_pressure=None, second_point_pressure=None, cutoff=None, ...)` | surfaces | A hydrostatic pressure. `cutoff` is `None`, `Positive` or `Negative`. |
| `add_imported_pressure(name, region, file_name, pressure_time, pressure_variable_name, interpolator_type=None, magnitude_factor=None, geometry_scale_factor=None, ...)` | surfaces | A pressure read from a results file. |
| `add_surface_traction(name, region, f1=None, f2=None, f3=None, p1=None, p2=None, p3=None, distribution_name=None, ...)` | surfaces | A surface traction. |
| `add_shell_edge_load(name, region, magnitude, ...)` | surfaces | A shell edge load. |
| `add_gravity(name, region, f1=None, f2=None, f3=None, ...)` | elements | A gravity load. |
| `add_centrifugal(name, region, axis_point=None, axis_direction=None, rotational_speed=None, ...)` | elements | A centrifugal load. |
| `add_pre_tension(name, region, magnitude, direction=None, load_type=None, ...)` | surfaces | A pre-tension load. `load_type` is `Force` or `Displacement`. |
| `add_concentrated_flux(name, region, magnitude, add_flux=None, ...)` | nodes | A concentrated flux. |
| `add_surface_flux(name, region, magnitude, ...)` | surfaces | A surface flux. |
| `add_body_flux(name, region, magnitude, ...)` | elements | A body flux. |
| `add_film_heat_transfer(name, region, sink_temperature, film_coefficient, coefficient_amplitude_name=None, ...)` | surfaces | A film heat transfer load. |
| `add_radiation_heat_transfer(name, region, sink_temperature, emissivity, cavity_radiation=None, cavity_name=None, emissivity_amplitude_name=None, ...)` | surfaces | A radiation heat transfer load. |
| `propagate(name)` | | Copies the load to every following step. |
| `hide(names)`, `show(names)` | | Symbol visibility. |

Every `add_*` ends with `amplitude_name`, `coordinate_system_name`, `phase_deg` and `color`, and has
a matching `replace_*` that takes `old_name` first and `name` second.

```python
step.loads.add_concentrated_force('Force-1', selection, f3=-1000)
step.loads.add_pressure('Pressure-1', pmx.create.region('SurfaceName', 'Top'), 2.5)
step.loads.add_gravity('Gravity-1', part_selection, f3=-9810)
```

Three details the model imposes:

- **A surface traction has two independent component groups.** `f1`-`f3` are the total force and
  `p1`-`p3` the force per area. The solver is given the group that matches the distribution: the
  force components without a distribution, the force per area components with one.
- A **pre-tension** load needs a three dimensional model, and computes its direction from the surface
  normal unless a `direction` is given.
- A **cavity name** is truncated to three characters, a solver limit.

An **imported pressure** is read from its results file on every model check, so `pressure_time` and
`pressure_variable_name` must name a field the file actually contains.

### `Load`

| Member | Description |
| --- | --- |
| `name`, `step_name` | The load name and the step it belongs to. |
| `load_type` | The load type, for example `CLoad` or `DLoad`. |
| `active`, `visible` | Item state. |
| `region_type`, `region_name` | The region the load is applied to. |
| `amplitude_name`, `coordinate_system_name` | The names the load refers to. |
| `color` | The symbol color as `[R, G, B, A]`. |
| `get_value_names()`, `get_value(name)` | The values specific to this load type. |

### `FieldOutputs` (model)

Reached as `step.field_outputs`. A field output covers the whole model, so it carries no region. The
variables are given by name, one or a list of them.

| Member | Description |
| --- | --- |
| `add_nodal(name, variables, last_iterations=None, contact_elements=None, global_output=None)` | A nodal field output. `RF`, `U`, `PU`, `V`, `NT`, `PNT`, `RFL`. |
| `add_element(name, variables, output=None, ...)` | An element field output. `S`, `PHS`, `E`, `ME`, `PEEQ`, `ENER`, `HFL`, `ERR`, `HER`, `ZZS`, `SDV`. `output` is `Default`, `TwoD` or `ThreeD`. |
| `add_contact(name, variables, ...)` | A contact field output. `CDIS`, `CSTR`, `CELS`, `PCON`. |
| `propagate(name)` | Copies the field output to every following step. |

```python
step.field_outputs.add_nodal('NF-Output-1', ['U', 'RF'])
step.field_outputs.add_element('EF-Output-1', ['S', 'E'])
```

### `FieldOutput` (model)

| Member | Description |
| --- | --- |
| `name`, `step_name` | The field output name and the step it belongs to. |
| `field_output_type` | `NodalFieldOutput`, `ElementFieldOutput` or `ContactFieldOutput`. |
| `active` | True when the field output is written to the solver input file. |
| `last_iterations` | Writes the results of the last iteration of a diverged increment. |
| `contact_elements` | Writes the results for the contact elements. |
| `global_output` | Writes the results in the global coordinate system. |
| `get_variables()` | The requested output variables as a list of names. |
| `get_value_names()`, `get_value(name)` | The values specific to this field output type. |

### `HistoryOutputs` (model)

Reached as `step.history_outputs`. Unlike a field output it carries a region.

| Member | Selection target | Description |
| --- | --- | --- |
| `add_nodal(name, region, variables, totals_type=None, global_output=None)` | nodes | A nodal history output. `RF`, `U`, `V`, `NT`, `RFL`. |
| `add_element(name, region, variables, ...)` | elements | An element history output. `S`, `E`, `ME`, `PEEQ`, `HFL`, `ENER`, `ELSE`, `ELKE`, `EVOL`, `EBHE`, `SDV`. |
| `add_section(name, region, variables, ...)` | surfaces | A section history output. `SOF`, `SOM`, `SOAREA`. |
| `add_contact(name, contact_pair_name, variables, ...)` | - | A contact history output. It names a **contact pair** instead of a region. `CDIS`, `CSTR`, `CELS`, `CNUM`, `CF`. |
| `propagate(name)` | | Copies the history output to every following step. |

`totals_type` is `Yes`, `Only` or `No`, and decides whether the sum of the external forces is printed
in addition to, or instead of, the individual values.

```python
step.history_outputs.add_nodal('H-Output-1', selection, ['U', 'RF'], totals_type='Only')
```

### `HistoryOutput` (model)

| Member | Description |
| --- | --- |
| `name`, `step_name` | The history output name and the step it belongs to. |
| `history_output_type` | `NodalHistoryOutput`, `ElementHistoryOutput`, `ContactHistoryOutput` or `SectionHistoryOutput`. |
| `active` | True when the history output is written to the solver input file. |
| `region_type`, `region_name` | The region, or the contact pair, the history output is written for. |
| `totals_type`, `global_output` | The shared history output values. |
| `get_variables()` | The requested output variables as a list of names. |
| `get_value_names()`, `get_value(name)` | The values specific to this history output type. |

### `DefinedFields`

Reached as `step.defined_fields`. A defined temperature has two forms, and each one is its own add
method, so the type follows from the method that is used.

| Member | Description |
| --- | --- |
| `add_temperature(name, region, temperature, distribution_name=None, amplitude_name=None, color=None)` | A defined temperature given by value. The selection target is `nodes`. |
| `add_temperature_from_file(name, file_name, step_number=None, amplitude_name=None, color=None)` | A defined temperature read from a results file. It covers the whole model, so it takes no region. |
| `propagate(name)` | Copies the defined field to every following step. |
| `hide(names)`, `show(names)` | Symbol visibility. |

`replace_temperature` and `replace_temperature_from_file` each refuse a defined field of the other
form, because the two take different arguments.

### `DefinedField`

| Member | Description |
| --- | --- |
| `name`, `step_name` | The defined field name and the step it belongs to. |
| `defined_field_type` | The type, for example `DefinedTemperature`. |
| `active`, `visible` | Item state. |
| `region_type`, `region_name` | The region the defined field is applied to. |
| `amplitude_name` | The amplitude the defined field refers to. |
| `color` | The symbol color as `[R, G, B, A]`. |
| `get_value_names()`, `get_value(name)` | The values specific to this defined field type. |

## Parts

### `Create`

Reached as `pmx.create`. A model item is committed by an `add` method on its collection; `pmx.create`
only makes the detached **parts** that those methods take as arguments, and records no command.

| Member | Description |
| --- | --- |
| `selection_by_ids(view, target, source, ids)` | Creates an ID-based selection. |
| `elastic(youngs_modulus, poissons_ratio=None)` | Elastic property. Two values, or a list of `[E, v, temperature]` rows. |
| `elastic_with_density(youngs_modulus, poissons_ratio, density)` | Combined elastic and density property. |
| `elastic_engineering_constants(constants)` | Orthotropic elastic. Rows of `[E1, E2, E3, v12, v13, v23, G12, G13, G23, temperature]`. |
| `elastic_lamina(constants)` | Lamina elastic. Rows of `[E1, E2, v12, G12, G13, G23, temperature]`. |
| `density(density)` | Density. One value, or rows of `[density, temperature]`. |
| `plastic(stress_strain, hardening=None)` | Plastic. Rows of `[stress, strain, temperature]`. `hardening` is `Isotropic`, `Kinematic` or `Combined` and defaults to `Isotropic`. |
| `specific_heat(specific_heat)` | Specific heat. One value, or rows of `[value, temperature]`. |
| `thermal_conductivity(thermal_conductivity)` | Thermal conductivity, same shape. |
| `thermal_expansion(thermal_expansion, zero_temperature=None)` | Thermal expansion, same shape. `zero_temperature` is the temperature the expansion starts from and defaults to 20 in model units. |
| `slip_wear(hardness, wear_coefficient)` | Slip wear property. |
| `region(region_type, name)` | Names an existing region of the model. |
| `split_tool_line_by_two_points(points=None, splitter=None)` | A line drawn on the surfaces through two points, for `split_shell` only. |
| `split_tool_plane_by_three_points(points=None, splitter=None, offset=0)` | A plane through three points. |
| `split_tool_plane_by_point_and_normal(point=None, normal=None, splitter=None, offset=0)` | A plane given by a point and a normal. |
| `split_tool_extend_face(faces, offset=0)` | The surfaces of the selected faces, continued past their boundaries. |
| `split_tool_extrude_edges(edges, direction=None, direction_selection=None, length=None, both_directions=True)` | The selected edges swept along a direction. |
| `damping(damping_type=None, alpha=None, beta=None)` | Rayleigh damping for a dynamic step. |
| `modal_damping(damping_type=None, alpha=None, beta=None, viscous_damping_ratio=None, damping_ratios=None)` | Modal damping for a modal dynamics or steady state dynamics step. |
| `damping_ratio(damping_ratio, lowest_mode, highest_mode)` | A viscous damping ratio for a mode range, used by the `Direct` modal damping type. |
| `time_incrementation_control(...)` | A time incrementation step control parameter. |
| `field_control(...)` | A field step control parameter. |
| `contact_control(...)` | A contact step control parameter. |
| `reset_control()` | A reset step control parameter, which restores the CalculiX defaults. |

A tabular property takes either the values of a single row, or a list of rows whose **last value is
always the temperature**. `plastic`, `elastic_engineering_constants` and `elastic_lamina` always
require the list form, because a single row is not enough to describe them.

The three step control makers take one optional argument per CalculiX parameter, and only the values
that are given override the solver defaults - `time_incrementation_control` takes `i0`, `ir`, `ip`,
`ic`, `il`, `ig`, `is_value`, `ia`, `ij`, `it`, `df`, `dc`, `db`, `da`, `ds`, `dh`, `dd` and `wg`;
`field_control` takes `rna`, `cna`, `q0a`, `qua`, `rpa`, `ea`, `cea` and `rla`; `contact_control`
takes `delcon`, `alea`, `kscalemax` and `itf2f`.

### `SplitTool`

Created by a `pmx.create.split_tool_*` method and taken by `split_solid` and `split_shell`. Its only
member is `tool_type`, the operation as the split dialogs name it. A tool is a detached description:
its selections are built in the `geometry` view and resolved only when a split applies it, so one
tool can be used on several regions, and a pick is read again when the history is regenerated.

- The points of the two point line and of the three point plane are given as coordinates or as a
  `geometry` selection, never both, because the selection would replace them. A vertex gives its own
  position and a circular edge its center.
- The point and normal plane takes both ways at once: a vertex gives the point, a straight edge or a
  planar, cylindrical or conical face the normal, and what the selection does not give is taken from
  the argument. A value given both ways must agree, or the split is refused. A point not given at all
  is put at the center of the parts being split, where the dialog puts the plane before anything is
  picked. `normal` is required unless the selection is given.
- The faces of `split_tool_extend_face` are a `geometry` or `geometry_surfaces` selection and may lie
  on a part that is not being split. Only a plane, a cylinder, a cone, a sphere and a torus continue
  past the face boundary; a face on a spline patch is left out, and a tool left with none is refused.
- `split_tool_extrude_edges` takes the direction as a vector or as a selection, exactly one of them:
  one straight edge gives the way it runs, a planar face its normal, a cylindrical or conical face its
  axis, and a planar face with a straight edge the direction that lies in the face and crosses the
  edge. A `length` that is not given sweeps the edges through the whole region.
- `offset` moves a plane along its normal, and every continued surface of an extended face along its
  own normal, a positive value growing the radius of a curved one.

### `Region`

Created by `pmx.create.region(region_type, name)`. It points a model item at an existing region of
the model instead of giving the item its own selection. `region_type` is `NodeSetName`,
`ElementSetName`, `SurfaceName`, `ReferencePointName`, `PartName` or `MassSection`.

A `MassSection` region names a point mass or a distributed mass section, and only the gravity and the
centrifugal load accept it, because only those two act on a mass. Giving it to any other item is
refused rather than silently stored.

| Member | Description |
| --- | --- |
| `region_type` | The region type. |
| `name` | The name of the region. |

```python
step.bcs.add_fixed('Fix-1', pmx.create.region('NodeSetName', 'Bottom'))
```

A region is a detached description, so the name is checked when the item that uses it is built, not
when the region is created. Every model item that takes a `region` argument accepts either a
`Region` or a `Selection`.

A selection based region does **not** read back as `Selection`. The application converts it into an
internal node set, element set or surface when the item is added, so `region_type` reads back as
`NodeSetName`, `ElementSetName` or `SurfaceName` either way, and `region_name` then names a set the
user did not create.

## Analyses

### `Jobs`

Reached as `pmx.jobs`. It shares the collection verbs, except `activate` and `deactivate`, which an
analysis does not support. A value that is not given comes from the application settings, so an
analysis added without arguments is the one the dialog would offer.

| Member | Description |
| --- | --- |
| `add(name, fem_solver=None, executable=None, argument=None, work_directory=None, compatibility_mode=None, num_cpus=None, environment_variables=None)` | Adds an analysis. `fem_solver` is `Calculix`, `Abaqus` or `FEMaster`. |
| `replace(old_name, name=None, ..., inherit_executable=None, inherit_work_directory=None, inherit_num_cpus=None, inherit_environment_variables=None)` | Replaces an analysis. A value that is not given is kept. |
| `open_results(name)` | Opens the results of the analysis. They become the current result of `pmx.results`. |

A value given here is an override: the application settings fill in only what the analysis does not
carry itself, so adding another analysis or changing the settings leaves an explicit executable, work
directory, CPU count or environment alone. An `inherit_*` switch is how an override is given back, so
that the value follows the settings again; it cannot be combined with the value it replaces.

```python
job = pmx.jobs.add('Job-1', num_cpus=4,
                   environment_variables=[pmx.create.environment_variable('OMP_STACKSIZE', '256M')])
pmx.jobs.replace('Job-1', inherit_num_cpus=True)
```

### `Job`

| Member | Description |
| --- | --- |
| `name` | The analysis name. |
| `fem_solver` | The FEM solver. |
| `executable`, `argument`, `work_directory` | What the solver is run as. The argument names the input file and defaults to the analysis name. The work directory is where the input file is written, where the previous run's files are deleted and where the solver runs, so an analysis with its own work directory is self contained. |
| `compatibility_mode` | Runs the solver in the simplified mode. |
| `num_cpus` | The number of solver CPUs. |
| `environment_variables` | Detached copies of the solver environment variables. |
| `status` | The status of the last run: `None`, `InQueue`, `Running`, `OK`, `Killed`, `TimedOut`, `Failed` or `FailedWithResults`. |
| `results_file_name` | The name of the results file the analysis writes. |
| `is_up_to_date` | True when the results match the current model. |
| `run(wait=True, timeout=None)` | Writes the solver input file and runs the analysis. Returns the status. The timeout is in seconds and needs `wait`; it kills the solver and leaves the analysis with the status `TimedOut`. A preparation the application refuses raises instead of returning a stale status. |
| `check_model(wait=True, timeout=None)` | The same, but only checks the model without solving it. |
| `kill()` | Kills a running analysis. |
| `get_output()` | Everything the solver wrote to its output. |

```python
job = pmx.jobs.add('Job-1')
status = job.run(timeout=600)
if status != 'OK':
    print(job.get_output())
pmx.jobs.open_results('Job-1')
```

`run` and `check_model` return the job status rather than a bool, because a run that starts is not a
run that succeeds. With `wait` the call returns only when the solver has finished, which is what an
unattended script needs; `wait=False` starts the solver and returns at once, and is only useful from
the console, because a batch run that ends while the solver is still working kills it.

A `timeout` in seconds kills the solver and returns `TimedOut`. It needs `wait`, since a run that is
not waited for has nothing to time out. The `--timeout` command line switch stays the outer safety
net for the whole process.

A refused prompt during a run ends the process with exit code 5. Running an analysis whose elements
have no section assignment is the usual way to meet one, because the application asks whether to
continue.

## View

### `View`

`pmx.view` is what the application shows: which of the three trees is displayed, where the camera
is, which result is plotted, and a picture of it.

**Nothing here is recorded in the command history**, because none of it changes the model. The same
reasoning that keeps a camera angle out of the `.pmx` keeps it out of Regenerate, so a script that
moved the view should put it back itself rather than expecting replay to.

| Member | Description |
| --- | --- |
| `current` | The displayed tree: `'geometry'`, `'femodel'` or `'results'`. Settable. |
| `result_type` | How the results are plotted: `Undeformed`, `Deformed`, `ColorContours`, `VectorPlot` or `TensorPlot`. Settable. |
| `set_orientation(orientation)` | Points the camera along a standard direction: `Front`, `Back`, `Top`, `Bottom`, `Left`, `Right`, `Isometric`, `IsometricNegative`, `Normal` or `Vertical`. |
| `get_orientation_names()` | The orientation names `set_orientation` accepts. |
| `zoom_to_fit()` | Fits everything visible in the viewport. |
| `get_parameters()` | The camera as 10 values: position, focal point and view up, three each, then the parallel scale. |
| `set_parameters(parameters)` | Sets the camera from those 10 values. |
| `rotate(azimuth, elevation=0, roll=0)` | Turns the camera about the model by angles in degrees: azimuth about the vertical axis of the view, elevation about its horizontal axis, roll about the viewing direction. |
| `look_at(point, parallel_scale=None)` | Moves the camera so that it looks at a point in model coordinates, keeping the viewing direction and the view up. The parallel scale is half the viewport height in model units; one that is not given keeps the zoom. |
| `set_result_field(field_name, component_name=None)` | Plots a field component of the current result. The component that is not given is the first one of the field. |
| `set_result_increment(step_id, increment_id)` | Plots a given increment. The IDs are result IDs, the ones `pmx.results.current.steps` reports. |
| `get_result_field()` | What is plotted now, as `[field_name, component_name, step_id, increment_id]`. |
| `deformation_type` | How much the plotted result is deformed, as the results toolbar sets it: `Undeformed`, `TrueScale`, `Automatic`, its multiples `Automatic025`, `Automatic05`, `Automatic2`, `Automatic5`, or `UserDefined`. Settable; setting it needs an open result. |
| `deformation_factor` | The scale factor the `UserDefined` deformation type uses. It is kept when another type is set, the way the toolbar keeps it. Settable; setting it needs an open result. |
| `undeformed_model` | The undeformed model drawn behind a `ColorContours`, `VectorPlot` or `TensorPlot` result: `None`, `WireframeBody` or `SolidBody`. Settable; setting it needs an open result, and is refused for the `Undeformed` and `Deformed` plots, which would discard it. |
| `set_legend_range(min_value, max_value)` | Fixes the color legend range of the plotted field component, so pictures of different increments or models share one color scale. Kept per field component while the result is open, the way the legend settings keep it. Needs an open result in the results view. |
| `reset_legend_range()` | Puts the legend of the plotted field component back to the automatic range. |
| `save_image(file_name=None, transparent_background=False, magnification=1)` | Writes a PNG of the viewport and returns the path. A file name that is not given means a file in the temporary folder. A transparent background leaves out the background the viewport shows, so the picture can be put on a page of any color; the model, the legend and the labels keep their own colors. The magnification, 1 to 4, renders the picture that many times larger than the viewport, for one that is going to be printed - only the model is drawn larger, the legend, the scale bar and the triad keep the size they have on screen. |
| `apply_exploded_view(scale_factor=0.5, method='Disassembly', magnification=2, direction='XYZ', step_by_step=False, tolerance=0, center=None)` | Pulls the parts of the displayed view apart, the way the exploded view dialog does. `method` is `Disassembly`, `AssemblyCenter` or `CenterPoint`, which needs `center`. The scale factor, 0 to 1, is the position of the dialog's slider; the magnification is 1 to 10 for `Disassembly`, 25 for `AssemblyCenter` and 1000 for `CenterPoint`. `direction` limits the movement to `XYZ`, `X`, `Y`, `Z`, `XY`, `XZ` or `YZ`. A compound part moves as one body. `step_by_step` and `tolerance`, where 0 means one thousandth of the assembly diagonal, belong to `Disassembly`. A value out of range is refused, not clamped. |
| `remove_exploded_view()` | Puts the parts of the displayed view back, the way the dialog's Disable button does. Nothing happens when no exploded view is applied. |
| `exploded_view_active` | True when the displayed view is pulled apart by an exploded view. |
| `annotations` | The arrow annotations of the displayed view, a `ViewAnnotations`. |

Part visibility is not here: `hide` and `show` on the part collections are model changes and go
through the command history like any other. An exploded view is view state like the camera: each of
the three views keeps its own, and meshing always runs without it.

```python
camera = pmx.view.get_parameters()
pmx.view.current = 'results'
pmx.view.set_result_field('DISP', 'U3')
pmx.view.deformation_type = 'TrueScale'
pmx.view.set_legend_range(0, 0.5)
pmx.view.set_orientation('Isometric')
pmx.view.rotate(20, 10)
pmx.view.zoom_to_fit()
path = pmx.view.save_image(None, True, 2)
pmx.view.reset_legend_range()
pmx.view.set_parameters(camera)
```

### `ViewAnnotations`

`pmx.view.annotations` holds the arrow annotations of the displayed view - the labels a report
picture carries. They are view state, not model changes: each of the three views keeps its own set,
the results view keeps one set per result, and they are saved in the `.pmx` without going through the
command history, the same as a label placed by a query in the application. The names
`Min_Annotation`, `Max_Annotation` and `Measure_Annotation` are reserved.

| Member | Description |
| --- | --- |
| `get_names()` | The names of the annotations of the displayed view. |
| `add_text(name, text, anchor_point)` | Adds a text label with an arrow pointing at a point in model coordinates. The name must be free. |
| `add_node(node_id)` | Adds the annotation a node query places: ID, coordinates and, in the results view, the plotted value. Returns the generated name. |
| `add_element(element_id)` | Adds the annotation an element query places. Returns the generated name. |
| `remove(names)` | Removes the named annotations. A single name or a list of names; a reserved or unknown name is refused. |
| `show_min_max(show_min, show_max)` | Shows or hides the labels at the minimum and the maximum of the plotted result, the way the post-processing settings do. Needs an open result. |
| `get_min_max()` | Whether those labels are shown, as `[show_min, show_max]`. Keep them to put the user setting back. |

```python
shown = list(pmx.view.annotations.get_min_max())
pmx.view.annotations.show_min_max(False, True)
pmx.view.annotations.add_text('Load', 'Pressure 2 MPa', [0, 0, 10])
name = pmx.view.annotations.add_node(15)
path = pmx.view.save_image()
pmx.view.annotations.remove(['Load', name])
pmx.view.annotations.show_min_max(shown[0], shown[1])
```

## Selections

### `SelectionFactory`

`pmx.selection` creates selection drafts that are independent of the live UI selection. The `view` is required and remains stored in the selection so command replay uses the correct mesh.

| Member | Description |
| --- | --- |
| `create_by_ids(view, target, source, ids)` | Creates an ID-based selection. |
| `highlight(selection)` | Switches to the stored view and highlights the selection. |
| `clear_highlight()` | Clears the interactive selection and its highlight. |

Possible `view` values are `"geometry"`, `"femodel"`, and `"results"`.

Possible `target` values are `"nodes"`, `"elements"`, `"surfaces"`, `"edges"`, `"parts"`, `"geometry"`, and `"geometry_surfaces"`. Possible `source` values are `"nodes"`, `"elements"`, `"vertices"`, `"edges"`, `"surfaces"`, and `"parts"`.

Nodes, elements, vertices, and parts use a list of IDs. Vertex IDs are the node IDs displayed by Query. Edge and surface IDs are 1-based within each part and use a part-to-ID mapping:

```python
selection = pmx.selection.create_by_ids(
    view="results",
    target="nodes",
    source="edges",
    ids={3: [1, 2], 7: [4]}
)

selection.add_by_ids(source="vertices", ids=[10282])
pmx.selection.highlight(selection)
```

Supported conversions:

| Target | Sources |
| --- | --- |
| `nodes` | `nodes`, `vertices`, `edges`, `surfaces`, `parts` |
| `elements` | `elements`, `surfaces`, `parts` |
| `surfaces` | `surfaces`, `parts` |
| `edges` | `edges` |
| `parts` | `vertices`, `edges`, `surfaces`, `parts` |
| `geometry` | `vertices`, `edges`, `surfaces`, `parts` |
| `geometry_surfaces` | `surfaces` |
| any target | `geometry` |

The `geometry` source is the exception to the table. It takes the **geometry IDs** the application
records for a geometry pick made in the *Selection by ID* mode, unchanged, and converts them to
whatever target the selection carries. The other geometry sources take per part item IDs and the
application turns them into geometry IDs itself, so `geometry` is what a script uses to give back an
ID list it read from a recorded selection, and it is what the session journal emits.

```python
selection = pmx.create.selection_by_ids('geometry', 'surfaces', 'geometry', [2000003, 2000004])
```

A selection is built either from IDs or from a **pick** - a point in space, which the application
resolves the way it resolves a mouse click.

| Member | Description |
| --- | --- |
| `pmx.create.selection_by_ids(view, target, source, ids, geometry_select_mode='SelectId')` | Creates a selection from IDs. |
| `pmx.create.selection_by_pick(view, target, point, select_by, angle=None, direction=None, part_ids=None, completely_inside=False, precision=None, part_offsets=None)` | Creates a selection from a point pick. |
| `pmx.create.selection_by_area(view, target, planes, select_by, angle=None, part_ids=None, completely_inside=False)` | Creates a selection from an area pick. Every row of `planes` is `[ox, oy, oz, nx, ny, nz]`, and a normal points **out** of the volume. |
| `pmx.create.selection_by_box(view, target, corner1, corner2, select_by, ...)` | The six plane form of `selection_by_area`, given by two opposite corners. |
| `pmx.create.selection_all(view, target)` | Creates a selection of everything the view holds. |
| `selection.add_by_ids(source, ids, operation=None)`, `add_by_pick(..., operation=None)`, `add_by_area(..., operation=None)` | Adds another item to a selection. |
| `selection.add_all(operation=None)` | Adds everything the view holds. |
| `selection.invert()` | Inverts what the selection has selected so far. |
| `selection.configure(...)`, `selection.get_options()` | The recorded selection options. |

Every item a selection carries names the set operation that joins it to what came before:
`Add`, `Subtract`, `Intersect`, or `None`, which replaces the accumulated selection the way a fresh
click does. The first ordinary item defaults to `None` and every later one to `Add`, which is the
sequence a plain list of items records.

```python
selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3])
selection.add_by_ids('nodes', [2], operation='Subtract')
selection.add_by_ids('nodes', [1, 4], operation='Intersect')
```

`configure` records the options the selection was made with and returns the selection, so it chains:
`max_geometry_ids`, `max_item_ids`, `limit_to` (`None`, `FirstPart`, `FirstGeometryType` or
`FirstMesherType`), `shell_edges_only` and `enable_shell_edge_faces`. `get_options()` reads them
back.

`select_by` is `Node`, `Element`, `Edge`, `Surface`, `EdgeAngle`, `SurfaceAngle`, `Part`, `Geometry`,
`GeometryVertex`, `GeometryEdge`, `GeometrySurface`, `GeometryEdgeAngle`, `GeometrySurfaceAngle` or
`GeometryPart`. The four angle based modes need an `angle`.

```python
top = pmx.create.selection_by_pick('femodel', 'nodes', [0.5, 0.5, 1.0], 'Surface')
pmx.model.node_sets.add('Top', top)
```

A pick and a list of IDs can live in the same selection, and a pick survives a mesh change, because
it is resolved again rather than replayed from the IDs it once produced. That is what makes it worth
having over `selection_by_ids` for a script meant to outlive one mesh.

```python
top = pmx.create.selection_by_box('femodel', 'nodes', [0, 0, 0.9], [1, 1, 1.1], 'Node')
bottom = pmx.create.selection_by_box('femodel', 'nodes', [0, 0, 0.9], [1, 1, 1.1], 'Node').invert()
```

Three things to know about how the application resolves a selection:

- **A point pick takes the nearest entity.** A point that does not lie on the model still resolves, to
  whatever is closest, so a mistyped coordinate selects the wrong face rather than failing. An **area
  pick does not**: it selects only what lies inside its planes, so an empty box resolves to nothing
  and is reported.
- **Only the named parts are searched.** `part_ids` limits a pick; when it is not given the whole mesh
  is searched, which is the useful default for "whatever lies under this point". A pick that names
  its part and one that searches every part resolve to the same entity.
- **`precision` and `part_offsets` are part of a recorded pick.** The application takes the precision
  from the current view the first time a geometry pick is resolved, and the offsets are what an
  exploded view moved the parts by. A script that reproduces a recorded pick passes both, so that the
  same point resolves the same way whatever the view is now.
- **A pick and an invert are resolved by the application**, not by the API, because only it can turn a
  point into IDs or know what the complement of a selection is. They therefore need a live
  application, and are rejected in a results only API context.

### `Selection`

| Member | Description |
| --- | --- |
| `view` | Immutable selection view. |
| `target` | Immutable resulting item type. |
| `count` | Number of source IDs. |
| `add_by_ids(source, ids, operation=None, geometry_select_mode='SelectId')` | Adds another compatible ID source. The select mode applies only to geometry source IDs. |
| `add_all(operation=None)` | Adds everything the view holds. |
| `configure(max_geometry_ids=None, max_item_ids=None, limit_to=None, shell_edges_only=None, enable_shell_edge_faces=None)` | Records the selection options and returns the selection. |
| `get_options()` | The recorded selection options. |
| `get_sources()` | Returns the source types as `StringList`. |
| `get_ids()` | Resolves the selection against the current mesh and returns the IDs. |
| `preview()` | Resolves the selection and reports what it found, as `SelectionPreview`. |

### `SelectionPreview`

The dry run of a selection: what it resolves to right now, without creating a model item and without
recording anything. A pick that found the wrong face is caught here rather than after it has become
a boundary condition.

| Member | Description |
| --- | --- |
| `target` | The resolved item type. |
| `count` | The number of resolved items. Zero means the selection finds nothing in the current view, which is reported rather than thrown. |
| `ids` | The resolved IDs: node IDs, element IDs, part IDs or geometry IDs, following the target. |
| `part_names` | The names of the parts the resolved items belong to. |
| `bounding_box` | `[xMin, xMax, yMin, yMax, zMin, zMax]` of the resolved items, or an empty array when nothing resolved. |
| `centroid` | The mean of the node coordinates of the resolved items, or an empty array. |

```python
top = pmx.create.selection_by_pick('femodel', 'nodes', [0.5, 0.5, 1.0], 'Surface')
preview = top.preview()
if preview.count == 0 or abs(preview.centroid[2] - 1.0) > 1e-6:
    raise Exception('the pick did not find the top face')
pmx.model.node_sets.add('Top', top)
```
## Results collection

### `Results`

Collection of loaded result files.

| Member | Description |
| --- | --- |
| `len(pmx.results)` | Number of loaded results. |
| `current` | Returns the currently selected `Result`. |
| `history_outputs` | Returns history outputs for the currently selected result. |
| `field_outputs` | Returns field outputs for the currently selected result. |
| `get_names()` | Returns result names as `StringList`. |
| `set_current(result_name)` | Sets the current result through command history and returns it. |
| `remove_current()` | Removes the current result through command history. |
| `clear()` | Removes every loaded result through command history. |
| `contains(result_name)` | Checks whether a result exists. |

Example:

```python
names = pmx.results.get_names()
result = pmx.results.set_current(names[-1])
# Equivalent after selection:
result = pmx.results.current
```

### `Result`

Single loaded result file.

| Member | Description |
| --- | --- |
| `name` | Immutable user-visible result name. |
| `file_name` | Source result file name. |
| `date_time` | Result file date and time. |
| `unit_system` | Result unit system. |
| `steps` | Result steps indexed by result step ID. |
| `mesh` | Result mesh. |
| `history_outputs` | Result history outputs indexed by history output name. |
| `field_outputs` | User-defined result field output definitions. |
| `get_nodal_value(step_id, increment_id, field_name, component_name, node_id)` | Returns one nodal field component value. |
| `get_nodal_coordinates(node_id)` | Returns the undeformed node coordinates as `[x, y, z]`. |
| `get_deformed_nodal_coordinates(step_id, increment_id, node_id)` | Returns true-scale deformed coordinates using `DISP`. |
| `get_history_output_entries(history_name, field_name, component_name)` | Returns the entries in a history output component. |
| `get_history_output_entry(history_name, field_name, component_name, entry_name)` | Returns one named history output entry. |

Example:

```python
result.name
result.steps
result.mesh
result.history_outputs
result.field_outputs
node_id = int(result.mesh.nodes.get_ids()[0])
coordinates = result.get_nodal_coordinates(node_id)
deformed_coordinates = result.get_deformed_nodal_coordinates(1, 1, node_id)
value = result.get_nodal_value(1, 1, 'DISP', 'U1', node_id)
```

## Result field output definitions

### `FieldOutputFilter`

Immutable Python filter specification used while creating result field outputs.

| Member | Description |
| --- | --- |
| `type` | `"median"`, `"average"`, or `"smooth"`. |
| `source` | `"all_nodes"` or `"surface_nodes"`. |
| `layers` | Number of neighbouring node layers. |
| `iterations` | Number of filter iterations. |

### `FieldOutputs`

Collection of user-defined result field outputs. These are postprocessing definitions that generate derived fields.

| Member | Description |
| --- | --- |
| `len(result.field_outputs)` | Number of user-defined result field outputs. |
| `result.field_outputs[name]` | Gets a field output definition by name. |
| `name in result.field_outputs` | Checks whether a definition exists. |
| `get_names()` | Returns definition names as `StringList`. |
| `first()` | Returns the first field output definition. |
| `last()` | Returns the last field output definition. |
| `contains(name)` | Checks whether a definition exists. |
| `create_field_output_limit(name, field_name, component_name, based_on, item_limits)` | Creates a part-, element-set-, or all-elements-based limit draft without modifying the result. |
| `create_field_output_envelope(name, field_name, component_name)` | Creates an envelope draft with `MAX`, `MIN`, and `AVERAGE` components. |
| `create_field_output_equation(name, equation, unit="/")` | Creates an equation draft. References use case-sensitive `FIELD.COMPONENT` names. |
| `create_field_output_coordinate_system_transform(name, field_name, coordinate_system_name)` | Creates a coordinate-system transform draft for a vector or tensor field. |
| `create_median_filter(source="all_nodes", layers=1, iterations=1)` | Creates a median filter. `source` can be `"all_nodes"` or `"surface_nodes"`. |
| `create_average_filter(source="all_nodes", layers=1, iterations=1)` | Creates an average filter. `source` can be `"all_nodes"` or `"surface_nodes"`. |
| `create_smooth_filter(source="all_nodes", layers=1, iterations=1)` | Creates a smooth filter. `source` can be `"all_nodes"` or `"surface_nodes"`. |
| `add(output)` | Validates and adds a `FieldOutputDraft` using command history. |
| `replace(old_name, output)` | Validates and replaces a field-output definition using command history. |
| `remove(field_output_names)` | Removes field outputs using command history. The argument is a non-empty list of names. |
| `remove_components(field_output_name, component_names)` | Removes components from a field output using command history. |
| `export_to_csv(file_name, field_output_name, step_id, increment_id)` | Exports all field components at one result increment using command history. |

`based_on` is required and must be `"parts"`, `"element_sets"`, or `"all_elements"`. `item_limits` must be a non-empty Python dictionary with finite, nonzero values. Its keys are part names, element-set names, or the single key `"All elements"` when `based_on="all_elements"`.

Creation only produces a Python-side draft and does not call the controller or modify command history. Assign optional filters to the draft, then call `add(output)`. The complete draft is converted and validated before the postprocessing command is created. If the current result changes after a `Result` object was obtained, get `pmx.results.current` again before adding the draft.

Filter creation is available directly on `pmx.results.field_outputs` and `result.field_outputs`. The filter `source` must be `"all_nodes"` or `"surface_nodes"`; `layers` and `iterations` must both be at least 1.

One limit for all elements:

```python
result = pmx.results.current

output = result.field_outputs.create_field_output_limit(
    name="Stress_limit",
    field_name="STRESS",
    component_name="MISES",
    based_on="all_elements",
    item_limits={"All elements": 235.0}
)

limit_output = result.field_outputs.add(output)
```

One limit with two filters:

```python
output = result.field_outputs.create_field_output_limit(
    name="Filtered_stress_limit",
    field_name="STRESS",
    component_name="MISES",
    based_on="all_elements",
    item_limits={"All elements": 235.0}
)

output.filter1 = pmx.results.field_outputs.create_median_filter(
    source="surface_nodes",
    layers=2
)
output.filter2 = pmx.results.field_outputs.create_smooth_filter(
    iterations=3
)

filtered_output = result.field_outputs.add(output)
```

Different limits by part:

```python
output = result.field_outputs.create_field_output_limit(
    name="Stress_limits_by_part",
    field_name="STRESS",
    component_name="MISES",
    based_on="parts",
    item_limits={
        "Part-1": 235.0,
        "Part-2": 180.0
    }
)

limit_output = result.field_outputs.add(output)
```

Envelope across all result increments:

```python
output = result.field_outputs.create_field_output_envelope(
    name="Stress_envelope",
    field_name="STRESS",
    component_name="MISES"
)
result.field_outputs.add(output)
```

Equation with the generated component `VALUE`:

```python
output = result.field_outputs.create_field_output_equation(
    name="Normalized_stress",
    equation="=STRESS.MISES / 235.0",
    unit="/"
)
result.field_outputs.add(output)
```

Coordinate-system transform:

```python
output = result.field_outputs.create_field_output_coordinate_system_transform(
    name="Local_stress",
    field_name="STRESS",
    coordinate_system_name="Local_CS"
)
result.field_outputs.add(output)
```

Replace an existing definition:

```python
replacement = result.field_outputs.create_field_output_limit(
    name="Stress_limit",
    field_name="STRESS",
    component_name="MISES",
    based_on="all_elements",
    item_limits={"All elements": 250.0}
)

result.field_outputs.replace("Stress_limit", replacement)
```

Remove definitions or stored fields and components:

```python
result.field_outputs.remove(["Stress_limit", "Old_field"])
result.field_outputs.remove_components("DISP", ["U3"])
```

Export every component of a field at one increment to a file:

```python
result.field_outputs.export_to_csv(
    file_name=r"C:\Temp\Stress_limit.csv",
    field_output_name="Stress_limit",
    step_id=1,
    increment_id=1
)
```

Every `create_*` above has a single call counterpart that creates the definition and adds it to the
result in one step: `add_limit`, `add_envelope`, `add_equation` and
`add_coordinate_system_transform`, each taking the arguments of its `create_*` plus the optional
`filter1` and `filter2`, and each with a matching `replace_*` that takes `old_name` first.

```python
outputs = pmx.results.current.field_outputs
outputs.add_equation('SEQV2', 'STRESS.Mises * 2')

# The same thing in two steps, which still works
draft = outputs.create_field_output_equation('SEQV3', 'STRESS.Mises * 3')
outputs.add(draft)
```

A results `replace_*` **re-specifies the whole definition**, unlike a model `replace_*` where a value
that is not given is kept. A result draft is built whole rather than cloned from the existing
definition, so there is nothing to keep.

### `FieldOutputDraft`

Python-side field-output definition that has not yet been added to the result.

| Member | Description |
| --- | --- |
| `name` | Field output name. |
| `filter1` | Optional first `FieldOutputFilter`; writable before `add(output)`. |
| `filter2` | Optional second `FieldOutputFilter`; writable before `add(output)`. |

`filter2` cannot be used unless `filter1` is also assigned.

### `FieldOutputLimitDraft`

Limit field-output draft returned by `create_field_output_limit`.

| Member | Description |
| --- | --- |
| `name` | Field output name. |
| `field_name` | Source field name. |
| `component_name` | Source component name. |
| `based_on` | `"parts"`, `"element_sets"`, or `"all_elements"`. |
| `item_limits` | Draft limit values as a read-only `StringDoubleMap`. |

### `FieldOutputEnvelopeDraft`

Envelope draft returned by `create_field_output_envelope`. It produces `MAX`, `MIN`, and `AVERAGE` components.

| Member | Description |
| --- | --- |
| `name` | Field output name. |
| `field_name` | Source field name. |
| `component_name` | Source component name. |

### `FieldOutputEquationDraft`

Equation draft returned by `create_field_output_equation`.

| Member | Description |
| --- | --- |
| `name` | Field output name. |
| `equation` | Equation using case-sensitive `FIELD.COMPONENT` references. |
| `unit` | User-defined result unit. |
| `component_name` | Generated component name, always `VALUE`. |

### `FieldOutputCoordinateSystemTransformDraft`

Coordinate-system transform draft returned by `create_field_output_coordinate_system_transform`.

| Member | Description |
| --- | --- |
| `name` | Field output name. |
| `field_name` | Source vector or tensor field name. |
| `coordinate_system_name` | Result coordinate system name. |

### `FieldOutput`

Base representation of a user-defined result field output.

| Member | Description |
| --- | --- |
| `name` | Field output name. |
| `type_name` | C# field output definition type. |
| `filter1` | First `FieldOutputFilter`, or `None` when disabled. |
| `filter2` | Second `FieldOutputFilter`, or `None` when disabled. |

### `FieldOutputLimit`

Limit-based field output returned by `add(output)` or collection lookup.

| Member | Description |
| --- | --- |
| `name` | Field output name. |
| `field_name` | Source field name. |
| `component_name` | Source component name. |
| `based_on` | `"parts"`, `"element_sets"`, or `"all_elements"`. |
| `item_limits` | Limit values as a `StringDoubleMap`. |

### `FieldOutputEnvelope`

| Member | Description |
| --- | --- |
| `field_name` | Source field name. |
| `component_name` | Source component name. |

### `FieldOutputEquation`

| Member | Description |
| --- | --- |
| `equation` | Validated field equation. |
| `unit` | User-defined result unit. |
| `component_name` | Generated component name, always `VALUE`. |

### `FieldOutputCoordinateSystemTransform`

| Member | Description |
| --- | --- |
| `field_name` | Source vector or tensor field name. |
| `coordinate_system_name` | Result coordinate system name. |

## Steps and increments

### `Steps`

Collection of result steps.

| Member | Description |
| --- | --- |
| `len(result.steps)` | Number of result steps. |
| `result.steps[step_id]` | Gets a step directly by result step ID. |
| `step_id in result.steps` | Checks whether the step exists. |
| `get_ids()` | Returns step IDs as `IntList`. |
| `first()` | Returns the first result step. |
| `last()` | Returns the last result step. |
| `contains(step_id)` | Checks whether the step exists. |

### `Step`

Single result step.

| Member | Description |
| --- | --- |
| `id` | Result step ID. |
| `increments` | Increments belonging to the step. |

### `Increments`

Collection of increments inside a result step.

| Member | Description |
| --- | --- |
| `len(step.increments)` | Number of increments. |
| `step.increments[increment_id]` | Gets an increment directly by increment ID. |
| `increment_id in step.increments` | Checks whether the increment exists. |
| `get_ids()` | Returns increment IDs as `IntList`. |
| `first()` | Returns the first increment in the step. |
| `last()` | Returns the last increment in the step. |
| `contains(increment_id)` | Checks whether the increment exists. |

### `Increment`

Single result increment.

| Member | Description |
| --- | --- |
| `id` | Increment ID. |
| `step_time` | Step time for this increment. |
| `fields` | Field outputs available for this increment. |


## Field results

Field result data is node-based. The returned IDs are node IDs.

### `Fields`

Collection of field outputs for one increment.

| Member | Description |
| --- | --- |
| `len(increment.fields)` | Number of fields. |
| `increment.fields[field_name]` | Gets a field by name. |
| `field_name in increment.fields` | Checks whether the field exists. |
| `get_names()` | Returns field names as `StringList`. |
| `first()` | Returns the first field in the increment. |
| `last()` | Returns the last field in the increment. |
| `contains(field_name)` | Checks whether the field exists. |

### `Field`

Single field output, such as displacement, stress, or temperature.

| Member | Description |
| --- | --- |
| `name` | Field name. |
| `components` | Components available for this field. |
| `get_data()` | Returns all components as `FieldMultiComponentData`. |
| `get_extremes(component_name)` | Returns the largest and the smallest value of one component, and the node IDs they sit on, as `FieldExtremes`. |

### `FieldComponents`

Collection of field components.

| Member | Description |
| --- | --- |
| `len(field.components)` | Number of components. |
| `field.components[component_name]` | Gets a component by name. |
| `component_name in field.components` | Checks whether the component exists. |
| `get_names()` | Returns component names as `StringList`. |
| `first()` | Returns the first field component. |
| `last()` | Returns the last field component. |
| `contains(component_name)` | Checks whether the component exists. |

### `FieldComponent`

Single scalar component of a field result.

| Member | Description |
| --- | --- |
| `name` | Component name. |
| `get_data()` | Returns scalar component data as `FieldData`. |
| `get_extremes()` | Returns the extremes of this component as `FieldExtremes`. |
| `get_value(node_id)` | Returns this component value for one node ID. |

### `FieldData`

Node-based scalar field component data.

| Member | Description |
| --- | --- |
| `ids` | Node IDs. |
| `values` | Scalar values for the component. |
| `location` | Result location, normally node-based. |
| `unit` | Component unit. |
| `count` | Number of values. |

Example:

```python
disp = increment.fields['DISP']
u1 = disp.components['U1'].get_data()
u1.ids
u1.values
```

### `FieldMultiComponentData`

Node-based data for all components of a field.

| Member | Description |
| --- | --- |
| `ids` | Node IDs. |
| `values` | Values arranged by node and component. |
| `component_names` | Component names as `StringList`. |
| `location` | Result location, normally node-based. |
| `unit` | Field unit. |
| `count` | Number of nodes. |

Example:

```python
data = increment.fields['DISP'].get_data()
data.component_names
data.values
```

### `FieldExtremes`

The largest and the smallest value of one field component, and the nodes they sit on. The scan
happens in the application, so the peak a report needs is read without transferring the values it was
found in - the matrix behind `get_data()` is one row per node by one column per component, which for
even a small model is tens of thousands of numbers for two of them.

| Member | Description |
| --- | --- |
| `component_name` | The component the extremes were read from. |
| `max_value`, `max_node_id` | The largest value, and the ID of the node carrying it. |
| `min_value`, `min_node_id` | The smallest value, and the ID of the node carrying it. |
| `unit` | The component unit. |

Example:

```python
peak = increment.fields['STRESS'].get_extremes('MISES')
print(peak.max_value, peak.unit, 'at node', peak.max_node_id)
```

Nodes without a value are skipped rather than reported as the extreme, and a component with no value
at any node is refused rather than answered with a NaN.

## Mesh

### `Mesh`

Mesh associated with a result.

| Member | Description |
| --- | --- |
| `nodes` | Mesh nodes. |
| `elements` | Mesh elements. |
| `parts` | Mesh parts. |
| `node_sets` | User-visible, read-only result node sets. |
| `element_sets` | User-visible, read-only result element sets. |
| `surfaces` | User-visible, read-only result surfaces. |
| `reference_points` | The result reference points. |
| `coordinate_systems` | The result coordinate systems. |

### `Nodes`

Collection of mesh nodes.

| Member | Description |
| --- | --- |
| `count` | Number of nodes. |
| `get_data()` | Returns `NodeData`. |
| `get_ids()` | Returns node IDs. |
| `get_coordinates()` | Returns undeformed node coordinates. |

### `NodeData`

Undeformed node coordinate data.

| Member | Description |
| --- | --- |
| `ids` | Node IDs. |
| `coordinates` | Undeformed node coordinates. |
| `count` | Number of nodes. |

### `Elements`

Collection of mesh elements.

| Member | Description |
| --- | --- |
| `count` | Number of elements. |
| `len(result.mesh.elements)` | Number of elements. |
| `result.mesh.elements[element_id]` | Gets an element by ID. |
| `element_id in result.mesh.elements` | Checks whether the element exists. |
| `get_data()` | Returns `ElementData`. |
| `get_ids()` | Returns element IDs as `IntList`. |
| `first()` | Returns the first mesh element. |
| `last()` | Returns the last mesh element. |
| `contains(element_id)` | Checks whether the element exists. |
| `get_node_ids(element_id)` | Returns node IDs for one element as `IntList`. |

### `Element`

Single mesh element.

| Member | Description |
| --- | --- |
| `id` | Element ID. |
| `type_name` | Element type name. |
| `dimension` | Element dimension. |
| `node_ids` | All node IDs of the element in PrePoMax element order. |
| `node_count` | Number of element nodes. |

Example:

```python
element = result.mesh.elements[10]
element.node_ids
```

### `ElementData`

Bulk element connectivity data.

| Member | Description |
| --- | --- |
| `ids` | Element IDs. |
| `offsets` | Start index of each element connectivity span. |
| `node_counts` | Number of nodes for each element. |
| `connectivity` | Flattened node IDs for all elements. |
| `count` | Number of elements. |
| `get_node_ids(element_id)` | Returns node IDs for one element as `IntList`. |

`offsets` are needed only when reading the flattened `connectivity` array directly.
For element index `i`, its node IDs are stored in:

```python
connectivity[offsets[i]:offsets[i + 1]]
```

For normal use, prefer:

```python
node_ids = result.mesh.elements.get_node_ids(element_id)
# or
node_ids = result.mesh.elements[element_id].node_ids
```

### `Parts`

Collection of mesh parts.

| Member | Description |
| --- | --- |
| `len(result.mesh.parts)` | Number of parts. |
| `result.mesh.parts[part_name]` | Gets a part by name. |
| `part_name in result.mesh.parts` | Checks whether the part exists. |
| `get_names()` | Returns part names as `StringList`. |
| `first()` | Returns the first mesh part. |
| `last()` | Returns the last mesh part. |
| `contains(part_name)` | Checks whether the part exists. |
| `hide(part_names)`, `show(part_names)` | Result part visibility. |
| `set_color(part_names, color)`, `reset_color(part_names)` | The result part colour. |
| `set_transparency(part_names, transparency)` | The alpha channel, from 0 for invisible to 255 for opaque. |
| `set_representation(part_names, result_representation)` | Turns the result representation on or off. |
| `replace_properties(old_name, name=None, color=None)` | Replaces the part properties. A property that is not given is kept. |
| `merge(part_names)` | Merges result parts into one. |
| `remove(part_names)` | Removes result parts. |

### `ReferencePoints` and `CoordinateSystems` of the result mesh

Reached as `result.mesh.reference_points` and `result.mesh.coordinate_systems`. Unlike the read-only
result regions below, they are edited through the results side of the command history, because a
result field transformation is expressed in a result coordinate system.

| Member | Description |
| --- | --- |
| `len(collection)`, iteration, `get_names()` | The names in the collection. |
| `get_definition(name)` | The stored coordinates, as the values or expressions they were entered with. Three for a reference point, nine for a coordinate system. |
| `reference_points.get_created_from(name)` | How the point was created: `Coordinates`, `OnPoint`, `BetweenTwoPoints`, `CircleCenter`, `CenterOfGravity` or `BoundingBoxCenter`. |
| `reference_points.get_name_visible(name)`, `reference_points.get_color(name)` | Whether the point name is drawn next to its symbol, and the symbol colour channels. |
| `reference_points.add(name, coordinates, name_visible=None, color=None)` | Adds a result reference point. |
| `reference_points.add_from_selection(name, created_from, selection, name_visible=None, color=None)` | Adds one from selected result mesh points. The mode is `OnPoint`, `BetweenTwoPoints` or `CircleCenter`. |
| `reference_points.add_from_region(name, created_from, region_type, region_name, name_visible=None, color=None)` | Adds one from a named result region. The mode is `CenterOfGravity` or `BoundingBoxCenter`, the region type `NodeSetName` or `SurfaceName`. |
| `reference_points.replace(old_name, name=None, coordinates=None, name_visible=None, color=None)` | Replaces one. The coordinates are the whole definition, and applying them turns the point back into a coordinate point. |
| `reference_points.replace_from_selection(old_name, name=None, created_from=None, selection=None, name_visible=None, color=None)` | Replaces one by selected points. The mode and the selection are the whole definition. |
| `reference_points.replace_from_region(old_name, name=None, created_from=None, region_type=None, region_name=None, name_visible=None, color=None)` | Replaces one by a named region. The mode and the region are the whole definition. |
| `coordinate_systems.get_coordinate_system_type(name)` | `Rectangular` or `Cylindrical`. |
| `coordinate_systems.get_name_visible(name)` | Whether the coordinate system name is drawn next to its symbol. |
| `coordinate_systems.get_created_from(name)` | How the origin, the point on X and the point in XY were created. |
| `coordinate_systems.add(name, origin, point_on_x, point_in_xy, coordinate_system_type=None, name_visible=None)` | Adds a result coordinate system. |
| `coordinate_systems.add_from_selections(name, coordinate_system_type=None, origin=None, origin_created_from=None, origin_selection=None, point_on_x=None, point_on_x_created_from=None, point_on_x_selection=None, point_in_xy=None, point_in_xy_created_from=None, point_in_xy_selection=None, name_visible=None)` | Adds one whose points may be selected on the result mesh. Each point takes either coordinates or a mode and a selection, and a point that is not given keeps the default coordinates. |
| `coordinate_systems.replace(old_name, name=None, origin=None, point_on_x=None, point_in_xy=None, coordinate_system_type=None, name_visible=None)` | Replaces one. The three points are the whole definition. |
| `coordinate_systems.replace_from_selections(old_name, name=None, coordinate_system_type=None, ...)` | Replaces one the way `add_from_selections` builds it. A result replacement re-specifies the whole definition, so every point is required. |
| `duplicate(names)`, `remove(names)`, `hide(names)`, `show(names)` | The shared verbs. |

### `Part`

Single mesh part.

| Member | Description |
| --- | --- |
| `name` | Part name. |
| `node_ids` | Node IDs belonging to the part. |
| `element_ids` | Element IDs belonging to the part. |
| `get_node_coordinates()` | The undeformed coordinates of the part nodes with shape `(node_count, 3)`, in the order `node_ids` reports them. |
| `volume`, `center_of_mass` | Mesh-computed part measurements, with CAD used only when a mesh value was missing. |
| `surface_area` | The total surface area. A mesh computes an area only for a shell part, so a solid reports the area of the CAD part it was meshed from, and `0.0` when there is none. The area of the mesh itself is the sum of `get_face_data().areas`. |
| `get_bounding_box()` | The mesh-computed `[xMin, xMax, yMin, yMax, zMin, zMax]` bounds. |
| `get_inertia_at_center_of_mass()`, `get_inertia_at_origin()` | The 3 by 3 inertia matrices, or an empty array when unavailable. They are second moments of volume with a density of 1, in model length units to the fifth power, exactly as on a model part. A mesh computes none, so these are the matrices of the CAD part the mesh came from and an imported mesh has none. |
| `get_face_data()`, `get_edge_data()`, `get_vertex_data()` | Result-mesh topology using the same `FaceData`, `EdgeData` and `VertexData` objects described for model parts. |
| `get_geometry_metadata()` | Supplemental CAD metadata transferred through the model mesh into the result. |

### Read-only result regions

Node sets, element sets, and surfaces are copied from the model into the result mesh. They can be inspected and
used by result history outputs, but they cannot be created, changed, or removed through the results API.

The `node_sets`, `element_sets`, and `surfaces` collections support indexing by name, `len()`, `get_names()`,
`first()`, `last()`, and `contains(name)`.

| Object | Members |
| --- | --- |
| `NodeSet` | `name`, `count`, `node_ids` |
| `ElementSet` | `name`, `count`, `element_ids` |
| `Surface` | `name`, `face_ids`, `node_ids`, `area` |

Example:

```python
node_set = result.mesh.node_sets["Node_Set-1"]
element_set = result.mesh.element_sets["Element_Set-1"]
surface = result.mesh.surfaces["Surface-1"]
```

## History results

History outputs can contain node, element, or integration point values depending on the output definition.
Integration point entries use names like `elementId_intPointId`.

### `HistoryOutputs`

Collection of result history outputs.

| Member | Description |
| --- | --- |
| `len(result.history_outputs)` | Number of history outputs. |
| `result.history_outputs[history_name]` | Gets a history output by name. |
| `history_name in result.history_outputs` | Checks whether the history output exists. |
| `get_names()` | Returns history output names as `StringList`. |
| `first()` | Returns the first history output. |
| `last()` | Returns the last history output. |
| `contains(history_name)` | Checks whether the history output exists. |
| `create_history_output_from_field(name, field_name, component_names, region)` | Creates a From Field draft using a node selection, node set, or surface. |
| `create_history_output_from_equation(name, equation, unit="/")` | Creates a From History Output by Equation draft. |
| `create_history_output_from_integration(name, integral_type, field_name=None, component_names=None, region=None, integral_result="geometry", deformation_variable_name="Displacements", unit="/", cross_section="none", thickness=1.0)` | Creates a line, surface, or volume integration draft. `integral_result` is `"geometry"` (region size), `"field"` (field integral), or `"field_by_geometry"` (region average of the field); `field_name`/`component_names` are ignored for `"geometry"`. `cross_section` (`"none"`, `"thickness"`, or `"axisymmetric"`) applies the out-of-plane scaling for 2D output on line and volume integrals. |
| `create_history_output_from_merged_histories(name, source1, source2, source3=None, source4=None)` | Creates a From Merged Histories draft that merges two to four history components into one history output. |
| `create_history_output_from_extracted_entry(name, source, entry_name)` | Creates a From Extracted Entry draft that copies one named entry of a history component into its own history output. |
| `create_history_output_xy(name, x_source, y_source)` | Creates an XY draft that plots the entries of the Y history component against the values of the X history component. |
| `create_history_output_from_table(name, data_points, y_column_names=None, x_axis_unit="/", y_axis_unit="/")` | Creates a From Table draft from user-defined data. Every row holds the X value followed by one value per Y column. |
| `create_minimum_filter(return_value="column")` | Creates a minimum filter; `return_value` is `"row"` or `"column"`. |
| `create_maximum_filter(return_value="column")` | Creates a maximum filter; `return_value` is `"row"` or `"column"`. |
| `create_sum_filter(operate_on="columns")` | Creates a sum filter; `operate_on` is `"rows"` or `"columns"`. |
| `create_average_filter(operate_on="columns")` | Creates an average filter; `operate_on` is `"rows"` or `"columns"`. |
| `add(output)` | Validates and adds a draft using command history. |
| `replace(old_name, output)` | Validates and replaces a result history-output definition. |
| `duplicate(history_output_names)` | Duplicates user-defined history outputs using command history. Solver-generated history outputs cannot be duplicated. |
| `remove(history_output_names)` | Removes history outputs using command history. |
| `remove_fields(history_output_name, field_names)` | Removes fields from a history output using command history. |
| `remove_components(history_output_name, field_name, component_names)` | Removes components from a history-output field using command history. |
| `export_to_csv(file_name, history_output_names, delimiter=",")` | Exports all fields and components of the selected history outputs to CSV using command history. |


Creation examples:

```python
result = pmx.results.current

nodes = pmx.selection.create_by_ids(
    view="results",
    target="nodes",
    source="edges",
    ids={3: [1, 2]}
)

output = result.history_outputs.create_history_output_from_field(
    name="Edge_DISP",
    field_name="DISP",
    component_names=["U1", "U2", "U3"],
    region=nodes
)
output.step_id = None
output.increment_id = None
output.filter1 = result.history_outputs.create_average_filter(operate_on="rows")
output.filter2 = result.history_outputs.create_maximum_filter(return_value="row")
result.history_outputs.add(output)
```

Existing result regions are passed directly through the same `region` argument:

```python
output = result.history_outputs.create_history_output_from_field(
    name="Node_set_DISP",
    field_name="DISP",
    component_names=["U1"],
    region=result.mesh.node_sets["Node_Set-1"]
)
result.history_outputs.add(output)
```

```python
output = result.history_outputs.create_history_output_from_equation(
    name="Energy_sum",
    equation="Energy_1 + Energy_2",
    unit="J"
)
result.history_outputs.add(output)
```

Line integration integrates a field over geometry edges (works for both shell and solid parts).
For 2D output (plane stress/strain or axisymmetric) set `cross_section` to apply the out-of-plane
scaling; `"thickness"` additionally uses the `thickness` argument, while `"axisymmetric"` scales by
`2*pi*r` (the radial coordinate is the X coordinate):

```python
edges = pmx.selection.create_by_ids(
    view="results",
    target="edges",
    source="edges",
    ids={1: [3, 4]}
)

output = result.history_outputs.create_history_output_from_integration(
    name="Line_Force",
    integral_type="line",
    field_name="STRESS",
    component_names=["S11"],
    region=edges,
    cross_section="thickness",
    thickness=0.01,
    unit="N"
)
result.history_outputs.add(output)
```

`integral_result` selects what the integral evaluates: `"geometry"` (the region size, integrating a unit
field; the default), `"field"` (the field integral), or `"field_by_geometry"` (the region average of the
field). `"geometry"` and `"field"` report one entry per element, so a Sum filter gives the total size
and the region total; `"field_by_geometry"` instead sums the element integrals and the element measures
over the whole region and divides the sums, so it returns a single `REGION` entry that is already the
measure-weighted average (no Sum filter needed). `field_name` and `component_names` are not needed for
`"geometry"`. The geometry result is reported in a single `SIZE` component; note that the cross section
scaling changes its dimension, so a scaled line integral returns an area and a scaled surface integral a
volume:

```python
output = result.history_outputs.create_history_output_from_integration(
    name="Surface_area",
    integral_type="surface",
    region=faces,
    integral_result="geometry",
    unit="mm2"
)
result.history_outputs.add(output)
```

`create_history_output_from_merged_histories`, `create_history_output_from_extracted_entry`, and
`create_history_output_xy` build a new history output from history components that already exist in the result.
Each source argument is either a `HistoryComponent` object or a list of three strings
`[history_output_name, field_name, component_name]`:

```python
history_output = result.history_outputs["Node_DISP"]
source = history_output.fields["DISP"].components["U1"]
same_source = ["Node_DISP", "DISP", "U1"]
```

Merging collects the entries of two to four history components into a single history output. All merged entries
must use the same unit and must share a common reference time; entries of the later sources are interpolated to
the times of the first source. Each source may be used only once:

```python
output = result.history_outputs.create_history_output_from_merged_histories(
    name="Merged_reactions",
    source1=["Node_set_1_FORC", "FORC", "F1"],
    source2=["Node_set_2_FORC", "FORC", "F1"]
)
result.history_outputs.add(output)
```

Extraction copies a single named entry of a history component into its own history output. The entry must exist
in the source component and must contain finite data:

```python
component = result.history_outputs["Node_DISP"].fields["DISP"].components["U1"]
output = result.history_outputs.create_history_output_from_extracted_entry(
    name="Node_5_U1",
    source=component,
    entry_name=str(component.get_entry_names()[0])
)
result.history_outputs.add(output)
```

An XY output replaces the time axis with the values of another history component. The X component must contain
exactly one entry, all entries of the Y component must use the same unit, and both sources must share a common
reference time:

```python
output = result.history_outputs.create_history_output_xy(
    name="Force_vs_displacement",
    x_source=["Node_DISP", "DISP", "U1"],
    y_source=["Node_FORC", "FORC", "F1"]
)
result.history_outputs.add(output)
```

A table output needs no result data at all. The number of Y columns is taken from the row length, all rows must
contain the same number of values, every value must be finite, and the Y-column names must be unique. When
`y_column_names` is omitted, the columns are named `Y1`, `Y2`, ...:

```python
output = result.history_outputs.create_history_output_from_table(
    name="Measured_curve",
    data_points=[[0.0, 0.0, 0.0], [1.0, 12.5, 11.0], [2.0, 24.0, 23.5]],
    y_column_names=["Test_1", "Test_2"],
    x_axis_unit="mm",
    y_axis_unit="N"
)
result.history_outputs.add(output)
```

All four output types produce a single `Table.Values` field component whose entries are the Y columns, and they
accept `filter1`/`filter2` like every other history-output draft.

The supported region combinations are:

| History output | Mode | Selection target | Existing result region |
| --- | --- | --- | --- |
| From Field | All | `"nodes"` | Node set or surface |
| From Integration | `"line"` | `"edges"` | None (selection only) |
| From Integration | `"surface"` | `"surfaces"` | Surface |
| From Integration | `"volume"` | `"elements"` | Element set |
| From Equation | All | None | None |
| From Merged Histories | All | None | None |
| From Extracted Entry | All | None | None |
| XY | All | None | None |
| From Table | All | None | None |

Possible deformation variable names are `"Displacements"`, `"Forces"`, `"Surface Normals"`, `"Wear Depths"`, `"Mesh Deformation"`, `"Disp&Def"`, and `"Disp&Def&Depth"`.

The history outputs have the same single call counterparts: `add_from_field`, `add_from_equation`,
`add_from_integration`, `add_from_merged_histories`, `add_from_extracted_entry`, `add_xy`,
`add_from_table` and `add_from_affine_fit`, each with a matching `replace_*`. The affine fit measures
the strain of a circular patch of the result mesh, so it names the circle - `center_x`, `center_y`
and `radius` - rather than a region, and the rest of its values carry the defaults the dialog offers. The draft form is kept here as well, because a
history output draft carries a lot of state and is sometimes easier to build in steps.

### `HistoryOutputFilter`

Immutable Python filter specification assigned to `filter1` or `filter2` on any history-output draft.
Filters are applied in order, and `filter2` cannot be used without `filter1`.

| Member | Description |
| --- | --- |
| `type` | `"minimum"`, `"maximum"`, `"sum"`, or `"average"`. |
| `option` | `"row"`, `"column"`, `"rows"`, or `"columns"`, depending on the filter type. |

### `HistoryOutputDraft`

Base object for history-output definitions that have not yet been added to the result.

| Member | Description |
| --- | --- |
| `name` | History output name. |
| `region` | Integrated selection or read-only result mesh region; `None` for the equation, merged, extracted, XY, and table drafts. |
| `filter1` | Optional first `HistoryOutputFilter`. |
| `filter2` | Optional second `HistoryOutputFilter`; requires `filter1`. |

### `HistoryOutputFromMergedHistoriesDraft`

From Merged Histories draft. Adds to `HistoryOutputDraft`:

| Member | Description |
| --- | --- |
| `sources` | Merged source components as `"history_output_name.field_name.component_name"` strings. |

### `HistoryOutputFromExtractedEntryDraft`

From Extracted Entry draft. Adds to `HistoryOutputDraft`:

| Member | Description |
| --- | --- |
| `source` | Source component as `"history_output_name.field_name.component_name"`. |
| `entry_name` | Name of the extracted history entry. |

### `HistoryOutputXYDraft`

XY draft. Adds to `HistoryOutputDraft`:

| Member | Description |
| --- | --- |
| `x_source` | X component as `"history_output_name.field_name.component_name"`. |
| `y_source` | Y component as `"history_output_name.field_name.component_name"`. |

### `HistoryOutputFromTableDraft`

From Table draft. Adds to `HistoryOutputDraft`:

| Member | Description |
| --- | --- |
| `data_points` | Table rows, each holding the X value followed by one value per Y column. |
| `y_column_names` | Y-column names. |
| `number_of_y_columns` | Number of Y columns. |
| `x_axis_unit` | User-defined X-axis unit. |
| `y_axis_unit` | User-defined unit shared by all Y columns. |

### `HistoryOutput`

Base representation of a history output. The `history_outputs` collection holds both the history outputs produced
by the solver and those defined by the user, so `is_user_defined` tells the two apart. Looking a user-defined
output up by name returns the matching type-specific object below; solver-generated outputs and From Affine Fit
outputs return this base object.

| Member | Description |
| --- | --- |
| `name` | History output name. |
| `fields` | History fields in the output. |
| `is_user_defined` | `True` when the output was defined by the user instead of produced by the solver. |
| `type_name` | C# history output definition type, or `None` for a solver-generated output. |
| `filter1` | First `HistoryOutputFilter`, or `None` when disabled or the output is solver-generated. |
| `filter2` | Second `HistoryOutputFilter`, or `None` when disabled or the output is solver-generated. |

Reading a definition back:

```python
history_output = result.history_outputs["Edge_DISP"]
if history_output.is_user_defined and history_output.type_name == "ResultHistoryOutputFromField":
    print(history_output.field_name, list(history_output.component_names), history_output.region_type)
```

### `HistoryOutputFromField`

| Member | Description |
| --- | --- |
| `field_name` | Source field name. |
| `component_names` | Selected field component names. |
| `region_type` | `"selection"`, `"node_set"`, or `"surface"`. |
| `region_name` | Region name, or `None` when the region is a selection. |
| `complex_result_type` | Complex result representation. |
| `complex_angle_deg` | Complex result angle in degrees. |
| `step_id` | Result step ID, or `None` when all steps are used. |
| `increment_id` | Result increment ID, or `None` when all increments are used. |
| `harmonic` | Whether harmonic oscillations are included. |
| `output_node_coordinates` | `"off"`, `"undeformed"`, or `"deformed"`. |

### `HistoryOutputFromEquation`

| Member | Description |
| --- | --- |
| `equation` | Validated history-output equation. |
| `unit` | User-defined result unit. |
| `component_name` | Generated component name, always `VALUE`. |

### `HistoryOutputFromIntegration`

| Member | Description |
| --- | --- |
| `integral_type` | `"line"`, `"surface"`, or `"volume"`. |
| `integral_result` | `"geometry"`, `"field"`, or `"field_by_geometry"`. |
| `field_name` | Source field name. |
| `component_names` | Selected field component names. |
| `region_type` | `"selection"`, `"element_set"`, or `"surface"`. |
| `region_name` | Region name, or `None` when the region is a selection. |
| `deformation_variable_name` | Deformation variable name. |
| `unit` | User-defined result unit. |
| `cross_section` | `"none"`, `"thickness"`, or `"axisymmetric"`. |
| `thickness` | Out-of-plane thickness used when `cross_section` is `"thickness"`. |

### `HistoryOutputFromMergedHistories`

| Member | Description |
| --- | --- |
| `sources` | Merged source components as `"history_output_name.field_name.component_name"` strings. |

### `HistoryOutputFromExtractedEntry`

| Member | Description |
| --- | --- |
| `source` | Source component as `"history_output_name.field_name.component_name"`. |
| `entry_name` | Name of the extracted history entry. |

### `HistoryOutputXY`

| Member | Description |
| --- | --- |
| `x_source` | X component as `"history_output_name.field_name.component_name"`. |
| `y_source` | Y component as `"history_output_name.field_name.component_name"`. |

### `HistoryOutputFromTable`

| Member | Description |
| --- | --- |
| `data_points` | Table rows, each holding the X value followed by one value per Y column. |
| `y_column_names` | Y-column names. |
| `number_of_y_columns` | Number of Y columns. |
| `x_axis_unit` | User-defined X-axis unit. |
| `y_axis_unit` | User-defined unit shared by all Y columns. |

### `HistoryFields`

Collection of history fields.

| Member | Description |
| --- | --- |
| `len(history_output.fields)` | Number of history fields. |
| `history_output.fields[field_name]` | Gets a history field by name. |
| `field_name in history_output.fields` | Checks whether the field exists. |
| `get_names()` | Returns field names as `StringList`. |
| `first()` | Returns the first history field. |
| `last()` | Returns the last history field. |
| `contains(field_name)` | Checks whether the field exists. |

### `HistoryField`

Single history field.

| Member | Description |
| --- | --- |
| `name` | History field name. |
| `components` | Components in the history field. |

### `HistoryComponents`

Collection of history components.

| Member | Description |
| --- | --- |
| `len(history_field.components)` | Number of components. |
| `history_field.components[component_name]` | Gets a component by name. |
| `component_name in history_field.components` | Checks whether the component exists. |
| `get_names()` | Returns component names as `StringList`. |
| `first()` | Returns the first history component. |
| `last()` | Returns the last history component. |
| `contains(component_name)` | Checks whether the component exists. |

### `HistoryComponent`

Single history component.

| Member | Description |
| --- | --- |
| `name` | Component name. |
| `entries` | Named history entries for this component. |
| `get_entry_names()` | Returns entry names as `StringList`. |
| `get_data()` | Returns data when entries share a compatible time axis or only one entry exists. |

### `HistoryEntries`

Collection of named history entries.

| Member | Description |
| --- | --- |
| `len(component.entries)` | Number of entries. |
| `component.entries[entry_name]` | Gets one entry by name. |
| `entry_name in component.entries` | Checks whether the entry exists. |
| `get_names()` | Returns entry names as `StringList`. |
| `first()` | Returns the first history entry. |
| `last()` | Returns the last history entry. |
| `contains(entry_name)` | Checks whether the entry exists. |

### `HistoryEntry`

Single named history entry.

| Member | Description |
| --- | --- |
| `name` | Entry name. |
| `get_data()` | Returns data for this entry as `HistoryEntryData`. |

### `HistoryComponentData`

History data returned from a complete history component.

| Member | Description |
| --- | --- |
| `time` | Time values. |
| `values` | History values. |
| `unit` | Value unit. |
| `count` | Number of time points. |
| `entry_names` | Entry names represented in `values`. |

### `HistoryEntryData`

History data for one selected entry.

| Member | Description |
| --- | --- |
| `entry_name` | Selected entry name as a string. |
| `time` | Time values for this entry. |
| `values` | History values for this entry. |
| `unit` | Value unit. |
| `count` | Number of time points. |

Example:

```python
history_names = result.history_outputs.get_names()
history_output = result.history_outputs[history_names[0]]
field = history_output.fields[history_output.fields.get_names()[0]]
component = field.components[field.components.get_names()[0]]
entry_names = component.get_entry_names()
entry = component.entries[entry_names[0]]
data = entry.get_data()

# Equivalent convenience access
entries = result.get_history_output_entries(history_names[0], field.name, component.name)
entry = result.get_history_output_entry(history_names[0], field.name, component.name, entry_names[0])
```

## What a replacement keeps

On the model side, a `replace_*` clones the stored definition and changes only what it is told, so a
value that is not given is kept: a section thickness, an initial condition component, a mesher
setting, a physical constant, and the region the item was created from all survive a replacement that
does not name them. A replacement is typed, so replacing an item as the wrong kind is refused rather
than silently converting it.

On the results side a replacement re-specifies the whole definition, because a result draft is built
whole rather than cloned. The result reference points and coordinate systems therefore take their
complete coordinates, and only the name may be left out.

A value the user could type as an expression is stored as the expression. `get_equations()` on a
model item, and `get_definition(name)` on a result region, return what was entered rather than the
number it evaluated to, so a parameter change still reaches the value.

## Journal and unattended-run boundaries

`new_model` and `open` replace the command history and are therefore unavailable inside a recorded
script. Start from the file you want with `-f`, or call those operations from the interactive
console. For a reproducible scripted save use the batch `--save` option after the script completes; a
save called from inside the running recorded script is written before that script command is added to
the history.

A script run from a file receives its own path in `sys.argv[0]` and `__file__`, so it can find the
assets next to it. A recorded script that is replayed from the history has no file of its own and
uses the virtual `__file__` value `<string>`. `--scriptArgs` supplies `sys.argv[1:]` in both cases.

## Internal and infrastructure classes

These classes exist in the implementation but are not normally used directly from the Python console.

| Class | Purpose |
| --- | --- |
| `PythonAPIAttribute` | Marks API members that should appear in generated help and autocomplete. |
| `PythonAPIException` | Exception type used for API errors reported to Python. |
| `PythonEngineManager` | Hosts and manages the embedded Python engine and console execution. |
| `PythonConsoleResult` | Holds execution output, errors, and display text for console commands. |
| API service classes | Resolve PrePoMax result objects and convert internal data into API objects. |


## Source Folder Layout

The Python API resides in the `APIs` class-library project. Its source is grouped by object area under the `Python` folder, with helper and data-service classes stored beside the APIs they support. PrePoMax keeps only the controller and embedded-engine host adapters.

Most classes use the `APIs.Python` namespace. The **model tree** uses `APIs.Python.Model`, because the
model and the results sides own several of the same names - `Mesh`, `Nodes`, `Elements`, `Parts`,
`Steps`, `Step`, `FieldOutputs` and `HistoryOutputs` all exist on both sides. The namespace split
keeps the natural name on each side instead of renaming one of them.

Where a name is not shared, C# classes and files use explicit names such as `ResultStepAPI`,
`ResultIncrementAPI`, `ResultHistoryOutputAPI`, and `ResultHistoryOutputFieldAPI`. Python-visible
names remain `Step`, `Increment`, `HistoryOutput`, and `HistoryField`.

```text
APIs
  Python
    Commands
    Common
    Create
    Mesh
      ElementSets
      Elements
      NodeSets
      Nodes
      Parts
      Surfaces
    Model
      Common
      Document
      Fields
      Geometry
      Materials
      Mesh
      Meshing
      Parts
      Regions
      Sections
      Steps
    Results
      Fields
      ResultFieldOutputs
      ResultHistoryOutputs
    Scripts
    Selection
    Test_Scripts

PrePoMax/Python
  Commands
  Engine
```

`Test_Scripts` holds the scripts that exercise the API through the batch mode; every one of them is
expected to end with `TEST PASSED` and exit code 0.

## Test suite

`APIs/Python/Test_Scripts` holds the scripts that exercise the API through the batch mode. Every one
of them declares in its own header how it is run and which process exit code it expects:

```
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Loads.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
```

`Run_Tests.py` runs the whole suite. It is a plain script, run with the system Python outside
PrePoMax, and it reads those headers rather than keeping a second list of what to run:

```
python APIs/Python/Test_Scripts/Run_Tests.py
```

For every script it checks the process exit code, that the run report agrees with it, and that a
passing script printed `TEST PASSED`. The model files the headers name live in `Test_Scripts/Models`.
A script whose header names `PyTest_Job.frd` reads the solved model that `Test_Analysis.py` leaves in
the work directory, so those run last.

A header that declares neither a command line nor an expected exit code is a runner failure rather
than a silently skipped test, and a filter that matches nothing is refused. The model file is the one
part of a header that may be left out, for a test of the batch contract that opens no model.

`Test_Analysis.py` is the end to end case: it builds a material, a section, a step, a boundary
condition and a load on the cube, solves it with CalculiX, opens the results and reads a displacement
back. `Test_Prompt_Refused.py` is the one script that is expected to **fail**, with exit code 5, and
it proves that an unattended run refuses a question rather than guessing an answer.

The coverage scripts check what a journal will depend on: `Test_Model_Definition_Coverage.py` and
`Test_Model_Meshing_Coverage.py` that a replacement keeps what it is not told,
`Test_Model_Region_Coverage.py` and `Test_Model_Keyword_Tree_Coverage.py` the region, keyword and
tree surfaces, `Test_Selection_Correctness.py` and `Test_Selection_Pick.py` the selection algebra and
the recorded pick, `Test_Job_Correctness.py` the analysis settings and the timeout, and
`Test_Result_Coverage.py` with `Test_Result_Regions_Coverage.py` the result editing commands.

`Audit_API_Coverage.py` is the offline structural check: it reports an `add_*` without its matching
`replace_*`, and a controller command no gateway calls. `test_runner_offline.py` checks the runner
itself with `python -m unittest test_runner_offline`, launching no PrePoMax process.

## Notes for documentation generation

- The `[PythonAPI]` attribute descriptions are the best source for generated member documentation.
- `ToString()` implementations provide concise expression output; `print(...)` uses the full display text for arrays and named collections.
- Keep implementation-only methods unmarked by `[PythonAPI]` if they should be hidden from help and autocomplete.
- Prefer documenting collection access with `[...]` because this is the intended Python style.
