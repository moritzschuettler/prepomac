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
- Result data is exposed through `pmx.results.current`. Assign a loaded result name or a `Result` to it to change it: `pmx.results.current = result_names[-1]`.
- Result steps and increments are indexed by result IDs, not by model step names.
- Field result data is node-based. Only history output can contain integration point values.
- Integration point history entry names use the form `elementId_intPointId`.
- Option strings of the result side are the PascalCase names of the application's enumerations (`AllNodes`, `FieldByGeometry`, `RealAtAngle`, ...). They are matched regardless of case and read back in
  that spelling. Only a name is an option: a number such as `'3'` and a comma separated list are refused, and an
  argument that takes several options takes them as a list. Result and mesh members take snake_case arguments (`node_id`, `step_id`, `field_name`, ...). `contains` takes `name`, or
  `id` where the collection is keyed by an ID (result steps, increments and elements).
- Return shapes follow one rule. Bulk numeric data - one value or row per node, element, face or edge (coordinates, values, areas, centroids,
  normals, offsets), tables and history columns - is a NumPy array where NumPy is available and a `NumericArray` otherwise. NumPy is optional.
  A short value of a fixed size - a point, a vector, a bounding box, the camera parameters, a 3 by 3 matrix as three rows - is a plain Python
  `list` (`list[float]`, `list[list[float]]`), so it prints, compares and unpacks like the list a script passes in. Every list of IDs (node,
  element, face, edge, vertex, geometry, step and increment IDs, keyword indices, ...) is an `IntList` and every list of names is a `StringList`,
  with or without NumPy. The one bulk ID column is the element `connectivity`, a NumPy array (an `IntList` without NumPy) read in slices with
  `offsets`. A map is a `dict`, below.
- Every `color` argument is a `list[int]` or a `str`: `[R, G, B]` or `[R, G, B, A]` with channels from 0 to 255, or a .NET color name such as `'Red'`.
  A color read back is always a plain `list` `[R, G, B, A]`, which goes back into the `color` argument.
- A map a member returns (`get_settings`, `get_equations`, `get_element_types`, `mesh.generate`, the geometry operations' counts, ...) is a Python `dict`, so it prints its content and supports `in`, `.items()` and indexing.
- Methods that construct Python-side objects do not modify PrePoMax. They make either a detached part
  that an `add` takes as an argument - a maker on `pmx.create`, such as `pmx.create.elastic(...)`,
  `pmx.create.region(...)` or `pmx.create.selection_by_ids(...)` - or a result output draft or filter
  (`create_field_output_equation(...)`, `create_median_filter(...)`), which are the only `create_*` methods.
  A method that changes the model is named for what it does: a collection makes an item with `add_*`, and
  the mesher runs with `mesh.generate`.
- Collection `add_*` and `replace_*` methods validate and make the command-recorded change. On the
  **model** side a value that is not given is kept, so a replace changes only what it is told; on the
  **results** side a replace re-specifies the whole definition, because a result draft is built whole
  rather than cloned.
- Every collection `add_*` has a matching `replace_*` except these: `geometry.parts.add_compound` (a compound
  is built once from the named parts), `element_sets.add_from_quality` (the element set it adds is replaced
  by `element_sets.replace`) and `tree.add_group` (a group is moved and renamed by the tree, not re-specified).
  The view annotations `pmx.view.annotations.add_text`, `add_node` and `add_element` have none either: they
  are view state rather than model items, and an annotation is removed and added again.
- Names follow one pattern across the API. A count is `number_of_*` (`number_of_frequencies`,
  `number_of_cpus`), an angle in degrees ends in `_deg` (`angle_deg`, `edge_angle_deg`, `rotate_angle_deg`,
  `transfinite_angle_deg`, `azimuth_deg`), the step of a results file is `step_id`, and a point, a direction
  or a centre is one list rather than one argument per coordinate (`center=[x, y]`,
  `axis_point=[x, y, z]`).
- The collection verbs `hide`, `show`, `remove` and `duplicate` take `names`, a name or a list of names.
- The type an item reads back as is always `item_type`: the name of the `add_*` method that makes it,
  without `add_` - `'concentrated_force'`, `'static'`, `'from_field'`, `'from_equation'`. A `replace_*` of
  another type is refused in those words: `... is made by add_X, so replace_Y cannot replace it`.
- Names are compared the way the model keys its collections: regardless of case, so `steel` clashes with `Steel`.
  A name with surrounding spaces is no name - the model stores none - so `get`, `contains`, a region, a `replace_*`,
  `rename`, `remove` and every other verb that finds an item by name report it as not existing. Renaming an item to its own name in another case is allowed. Coordinate systems have a
  name space of their own; the parts, sets, surfaces and reference points share one.
- A name given to `add_*`, `replace_*` or `rename` is checked **before anything is recorded**, and is refused when
  it is empty, starts or ends with a space, contains a space or a character other than letters, digits and
  `- _ ( )`, or contains no letter. Materials and element sets also take `@`; parameter names do not take `-`.
  The name an item already carries is not checked again, so an item named by an import can still be replaced.
- A value the dialog takes as an expression takes a number or an equation string. A number is passed on as the
  shortest text that reads back as the same double, so nothing is lost on the way in; the model itself then keeps a
  plain number to 15 significant digits, as it does for a value typed in a dialog. An equation
  the application cannot evaluate - `'=1+'`, `'=undefined_param'` - is refused naming the argument, a component of a
  point or a vector as `point[1]`, before anything is recorded. A value the dialog takes as a plain number takes a
  number only: a string, even `'10'`, is refused as `must be a number`, because no equation is evaluated there.
- A keyword argument a method does not take is refused with a `TypeError`, as Python refuses one for a function of
  its own, and nothing is recorded: `add() got an unexpected keyword argument 'descripton'. Did you mean
  'description'?`. So is a keyword naming an argument already given by position.
- A call the API refuses raises `PythonAPIException`, a Python exception carrying only the message, so the
  traceback ends in `PythonAPIException: The argument 'name' must not be empty.` and no .NET stack trace. It
  derives from `Exception` and is a builtin, so a script catches it by name without importing anything:
  `except PythonAPIException as e:`. Any other .NET exception reaching a script is a fault of PrePoMax and keeps
  its .NET type and stack trace.
- A boolean, whole number or number argument is checked by the API, not by .NET overload resolution, so a
  wrong value is refused naming the argument: `None` for a required one, `2.5` for a whole number, `1` for a
  boolean. A whole number also takes a float with a whole value, such as `2.0`, and an optional
  argument given as `None` takes its documented default.
- So are a name or option string and an API object: a number for a name is refused as `The argument 'name'
  must be a string, not int.`, and a region where a selection is taken as `The argument 'selection' takes a
  Selection made by pmx.create.selection_by_ids, ..., not Region.`. The same holds for a filter, a draft and a
  split tool, and for `in` on a collection; `contains(None)` and `None in` a collection are `False`,
  and an ID collection takes a whole float in `in` as `contains` does.

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
| **File - Export - Python session journal \*.py** | Writes the journal of the open model's history. |
| **Edit - Edit History**, then **File - Save As Python** in its *Edit Commands* dialog | Writes the journal of the history shown in the dialog, including a reordered or shortened one. |
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
and expanding it would replace live computation with the values it produced the first time. Its
source is written as a string and executed with `exec` in a namespace of its own holding `pmx`,
`__name__` and `__file__`, as the recorded run had, so a name the script binds - `selection_1`, say -
cannot change the journal lines after it.

`APIs/Python/Test_Scripts/Test_Rpc_Live_Session.py` checks the whole path: it builds a varied model
in a session driven through the automation server, journals that session, replays the emitted script
in a fresh session, and compares the CalculiX export of both models.

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
| 1 | Command line or option error, also a `-g No` session started with `--rpc` whose automation server could not start |
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
| `--rpcTimeout <s>` | The request timeout for this run, 5 to 600 s, instead of the one in the settings. Not `--timeout`, which limits the whole run. |

There is deliberately **no switch for the token**. An unattended run reads the one in the folder's
settings, so a folder has to be opened once with its window and given a token before `-g No` can
serve. A session with no window does not prompt a user who is not there: when the server it was
started for cannot start - there is no token, or every port it may take is in use - it exits with
code 1 and writes the reason to the console, rather than staying open where nothing can reach it or
close it. With its window, switching the server on without a token offers to generate one.

**A session with no window starts empty unless it is given a file.** It does not open the last file
of the interactive user, for the same reason it is not asked whether to recover a crashed session:
the setting is a convenience for the person who last closed the application, and a hidden session
would otherwise inherit that person's model and their whole command history without being told to.
Name the model with `-f` when an agent should find one open.

Point the agent client at the address. Take it from **Tools - Settings - Automation**, under
*Connection*, or from the output window when the server starts, rather than assuming it: the port is
8756 unless that was taken, and then the server answers on the next free one, which the *Address*
shows while it runs. For Claude Code:

```bash
claude mcp add --scope user --transport http prepomax http://127.0.0.1:8756/mcp --header "Authorization: Bearer <token>"
```

`--scope user` registers the server for every project; without it Claude Code registers it for the
current directory only. `/mcp` in Claude Code shows the connection and reconnects it - a client that
was connected before PrePoMax was closed and opened again has to reconnect.

Another MCP client takes the same address and header in its configuration, usually in this form:

```json
{"mcpServers": {"prepomax": {"type": "http", "url": "http://127.0.0.1:8756/mcp",
                             "headers": {"Authorization": "Bearer <token>"}}}}
```

A client that speaks only stdio needs a bridge to HTTP, such as `mcp-remote`.

To have the server on whenever PrePoMax runs, set *Start with the application* in
**Tools - Settings - Automation**; otherwise switch it on after every start with
**Tools - Automation Server**.

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
your assistant, or keep the old one: start the new version once and close it, then copy
`settings.bin` over from the old folder. One copied in before that first start loses its token.

**A client that cannot connect is telling you that PrePoMax is closed, or that its automation server
is off.** Nothing answers on the address unless the session is open - that is the point of serving it
from the application - so the fix is to open PrePoMax and switch the server on, not to retry. It is
worth telling the agent so in the project's own instructions:

```markdown
The prepomax MCP server drives a live PrePoMax session. If it cannot connect, ask me to open
PrePoMax and switch on Tools - Automation Server; do not work around it.
```

### The tools

| Tool | What it does |
| --- | --- |
| `get_guide` | How to drive a session, served by the application so it matches the version. The first thing to read. |
| `list_sessions` | The running instances whose server is on, each with the address it answers on and the model file it has open, marking the one that answered. |
| `get_status` | Idle or busy and why, the open model, the view (`model.currentView`: `'geometry'`, `'femodel'` or `'results'`, as `pmx.view.current` names it), the model space (`model.modelSpace`), the unit system (`model.unitSystem`), the command count, read only, whether Regenerate is replaying the history (`isRegenerating`) or a recorded script keeps what runs out of the history (`isCommandRecordingSuppressed`), and under `calls` the long calls still running or waiting to be collected. A window that does not answer is reported as busy. |
| `describe_api` | The `[PythonAPI]` schema of `pmx`, of any path under it, or of an API type by name, read off the API itself. |
| `read_model` | A JSON snapshot of the model: properties, the parts with their mesh state (meshed or not, node and element count), collections, mesh setup items, how many user keywords and keyword edits there are, the parameters with their equation, value and description (`parameters_detail`), steps with their content, analyses with their status. |
| `read_topology` | The faces, edges and vertices of the parts, each with the ID a selection takes, its size, its centroid and its normal - faces largest first, edges longest first, vertices in index order, at most `limit` of each per part and 240 rows in the whole answer. The parts are listed under `geometry` and `femodel`, the words the views use. Part names match regardless of case and are not trimmed; a name that matches no part, a padded one included, is reported. Leaving `part_names` out reads every part; an empty list is refused. |
| `eval_expression` | One expression, with its value as JSON when it has one. |
| `run_python` | Python against the live model, recorded as commands or as one script. A call that outlasts its wait is answered with a `callId`. |
| `get_call` | The answer of a call that outlasted its wait, collected by its `callId`, shaped the way the tool that started it shapes it - `run_python` and `eval_expression`, and `read_model`, `read_topology`, `run_job` and `get_job`, which run Python too. An answer whose response never reached the client stays collectable. |
| `cancel_call` | Stops a call that is still running, by its `callId`, at its next Python statement. Answered within a few seconds. |
| `get_history` | The command history, and the journal of it as a Python script. A journal over 200,000 characters is written to a temporary file and returned as its path. `journal_file` writes the journal to that `.py` file instead, as the `history` method does: it needs `include_journal`, and a relative name goes under `%TEMP%\PrePoMax`. |
| `run_job` | Starts an analysis without waiting, so the user keeps the interface, and returns the status it started with. No monitor is opened; one the user opens is a dialog like any other, so the session answers busy until it is closed. |
| `get_job` | The status of an analysis and whether it is up to date, and its solver output on request. |
| `screenshot` | A PNG of the viewport, optionally after setting the view, the camera and the plotted result. Returns the path and the picture as an image, up to 4 MB. |
| `exit_session` | Closes a session without a window (`-g No`); refused for one the user has open. `discard_changes` closes it with unsaved changes. |

The surface is deliberately small. The model surface is the Python API, which the agent writes
through `run_python` and learns through `describe_api`, so there is no tool per API member - far too
many to list, and a second front door would be free to drift from the first. A tool that
is not a protocol primitive is a piece of Python the catalog holds: `read_model` and `read_topology`
are both one, so adding another costs nothing on the application side. Such a script runs as one
private function that is deleted afterwards, so it leaves nothing in the console scope the agent's
own variables live in, and the Python panel shows a one line note for it rather than its source.

A tool refuses an argument it does not take, naming it, instead of running without it, and every
schema says `additionalProperties: false`. A value is checked against the schema too: one of the wrong
type - `"true"` for a boolean, `"10"` or `1.5` for an integer, a number in a list of names - one outside
the declared `minimum` and `maximum`, or one that is not among the `enum` values is refused with the
argument named, rather than read as the default or clamped into range. A `null` for an optional
argument is taken as leaving it out, so the tool runs with its default; a `null` for a required one is
refused.

Every tool except `get_guide` and `list_sessions`, which are not about one session, also takes an
optional `process_id`. **It is checked, not routed.** One address serves one
session, so a call that names another instance is refused with the address that would have served it,
rather than served against the wrong model. `list_sessions` is the directory of what is open - use it
to tell the user which address to configure, not to switch.

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
  A value that is no option at all is refused listing the same values, so a member that accepts
  only part of an enumeration names only that part. The `view`, `target` and `source` of a
  selection are lower case names rather than enumeration names, and are listed the same way.
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
reports as `model.unitSystem`. The instructions say so, because nothing else will: 210000 is a correct
steel modulus in `MM_TON_S_C` and wrong by a factor of a million in `M_KG_S_C`.

### A skill for a client project

`AgentSkill\prepomax\SKILL.md`, which the build puts next to `PrePoMax.exe` (in the repository it
is `AgentSkill/prepomax/SKILL.md`), is a Claude Code skill that tells an agent to read `get_guide`
first and names the handful of habits that are specific to this application. It is short on purpose:
the guide and the schema are served by the application and cannot drift, and a skill that repeated
them would. Copy the `prepomax` folder to `<project>/.claude/skills/prepomax/` to have it in one
project, or to `~/.claude/skills/prepomax/` to have it in all of them.

### What an agent may and may not do

- **Every change is recorded.** `run_python` records each API call as its own command by default, so
  the history stays editable in **Edit - Edit History**, regenerable, and journalable. `record='script'`
  records the source as one command instead, which is what keeps a computed model parametric.
- **Busy is an answer.** A call that arrives while a dialog is open or a mesher is running is refused
  with the reason, and nothing is done. The question is asked on the thread that owns the window,
  because that is the only place the truth is visible: an item dialog of this application is not
  modal, and a modal one keeps pumping messages.
- **A long call leaves the window usable, and says it is working.** The work runs off the thread
  that owns the window, the way a `--script` run runs it, so the window keeps answering while a long
  call is in flight. Calls that reach the model are served one at a time, so two agents cannot
  interleave halfway through a change: meanwhile only the calls that report, collect or cancel are
  answered, as the next point lists.
- **A call that outlasts its wait is answered, not abandoned.** `run_python` and `eval_expression`
  (`exec` and `eval` over RPC) run on a thread of their own, and the caller waits as long as the request
  timeout says - 30 s by default, the `--rpcTimeout` of the run - or as long as the call's own optional
  `timeout` argument says, 0 to 600 s (0 answers at once). The wait for the turn counts against it:
  the turn is waited for no longer than that wait, nor longer than the request timeout, and a call
  that does not get it by then is refused as busy and never runs - at once for a timeout of 0 while
  another call holds the turn - so a request never lasts longer than its `timeout`, or the request
  timeout when it gives none. A call still running then is answered with
  `{running, callId, method, elapsed, message}`; it goes on to the end on that thread, is recorded
  normally, and keeps its answer until `wait` - `get_call` over MCP, with the `callId` and an optional
  `timeout` - collects it. The answer is given up only once the response carrying it has been written:
  one whose client had given up first - its own request timeout shorter than the wait - or had sent
  `notifications/cancelled` for the request stays collectable, and `status` lists it under `calls`.
  Uncollected answers are kept for an hour, the latest 16 of them. Keep `timeout` below the request
  timeout of the client, or pass 0 and collect with `get_call` for long work. `get_call` answers
  `running` again when the call is still working. It
  keeps the turn meanwhile, so every other call waits for it - reading ones included - and is refused
  as busy with the call named once it has waited the request timeout. Only `ping`, `status`, `wait`,
  `cancel` and `shutdown` are answered past it - on MCP `get_status`, `get_call`, `cancel_call`,
  `get_guide` and `list_sessions`; `status` reports it as the busy reason without asking the window,
  and lists it under `calls`.
- **A call that should not finish is cancelled.** `cancel` - `cancel_call` over MCP - with the
  `callId` raises `KeyboardInterrupt` in the Python of that call. It is never serialized behind the
  call it stops, and read only mode does not refuse it, because it changes nothing in the model. The
  interrupt lands at the next Python statement: an operation of the application the call is inside -
  meshing, an import, a regeneration - cannot be interrupted and finishes first, and a blocking
  wait - `time.sleep`, a socket, `input()` - is not interrupted until it returns. Every cancel delivers a
  new interrupt, so a call whose code caught the last one - a bare `except:` - is stopped by
  cancelling it again. The answer says what actually happened: `isInterrupted` when the interrupt was
  delivered, `isInterruptPending` while it waits - for an operation of the application to return, or
  for a call that has not reached its Python yet, which then does not run it and ends with the
  `KeyboardInterrupt` or with the refusal it meets first, such as busy - and `isFinished` whether the
  call has stopped; a call that had finished first is not reported as interrupted, and one that
  reached its Python while the cancel waited for it is reported as interrupted. The call's own answer, collected with `wait`, carries the error, `cancelled: true`,
  and the commands it recorded before it stopped, which stay in the history. A call cancelled before
  its Python started does not start it, and one cancelled while it was about to start is interrupted
  at its first statement. The Python panel offers the same for the user: a console line or a script
  that runs longer than a second shows a small *Python is running* window with a **Stop** button,
  served by a thread of its own because the window that runs the script is not answering. It does
  not take the focus, and **Stop** can be pressed again after two seconds while the Python still runs.
- **Printed output is capped.** What one call, console line or script prints to `stdout` or
  `stderr` keeps its first 1,000,000 and its last 100,000 characters, with
  `[output truncated: N characters omitted]` between them, in the answer and in the Python panel.
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
  can get around it. Reading keeps working. The block is held only while a call that waits its turn
  runs - `status` takes none, so polling it does not refuse the user's own edits - and it is
  counted, so a call that ends never lifts the block another call still holds. The refusal names
  where read only comes from: the setting, or `--rpc ReadOnly` for the whole run.
- **The session belongs to the user.** `exit` - `exit_session` over MCP - is refused for a session
  that has a window - only a hidden `-g No` session can be closed through the server - and
  `shutdown` closes the door an agent came through without touching the application.
- **Everything is visible.** What the agent runs is echoed into the Python panel when it is open, and
  named in the output window in any case.

### Undoing what an agent did

The same way as anything else: **Edit - Edit History** lists the commands the agent recorded, they
can be removed or reordered there, and **Edit - Regenerate History** rebuilds the model without them.

### The protocol, for another client

The MCP endpoint is Streamable HTTP, and only its JSON half: a client `POST`s one JSON-RPC 2.0
object to `http://127.0.0.1:<port>/mcp` with `Content-Type: application/json` and
`Authorization: Bearer <token>`, and gets one JSON-RPC response back. `GET` is answered `405` - the
endpoint serves no event stream, because nothing in its surface pushes - and no session id is issued.
`initialize` answers with the client's protocol version when it is one the endpoint supports
(`2024-11-05`, `2025-03-26`, `2025-06-18`) and with the latest of those otherwise. A batch - a JSON
array - is answered with a JSON-RPC error, and so is a request whose `jsonrpc` is not `"2.0"`. A
notification - an object without an `id` - is answered
`202` with no body and is not run, whatever its method: a `tools/call` is a request in MCP, and one
sent as a notification would change the model with no way to say what it did. The one notification
that has an effect is `notifications/cancelled`, which keeps the answer of the request it names
collectable with `get_call`; it does not stop the call - `cancel_call` does that. Its request id is
the only key, because no session id is issued: two clients that use the same ids at the same time
can cross, so one's cancel keeps the other's answer collectable as well, or one's new request with a
cancelled id lets the answer of the other's cancelled call be given up. An `id` that is `null`,
or neither a string nor a number, is an invalid request. The body is read as UTF-8 whatever its `Content-Type`
says, and every answer is `application/json; charset=utf-8`. A request that reaches the protocol and
fails there is a `200` carrying a JSON-RPC error; a request refused by a gate is an HTTP status,
because it never reached the protocol.

**The gates.** Three, and each stops something the other two do not. Only the two loopback prefixes
are claimed, so nothing off this computer can reach it. The bearer token is required on every
request and compared in constant time. Any request carrying an `Origin` header is refused, `OPTIONS`
is refused, and no `Access-Control-Allow-*` header is ever answered with, so no page in a browser can
reach the endpoint even if it is told that its own name resolves to `127.0.0.1`. That also keeps
out an MCP client that runs in a browser; give it a bridge such as `mcp-remote`. A body over 4 MB, a
body that is not JSON, and any other path are refused as well.

What the token bounds is worth being exact about: it is stored in a file the user can read, so
anything running **as that user** can read it, exactly as anything running as that user can open the
pipe. It stops a web page, which cannot read a file at all. It stops another account logged into the
machine - which can reach a loopback port, and is the boundary the named pipe used to get from
Windows for free - **only when the PrePoMax folder is readable by this user alone**, such as a folder
inside the user profile. `settings.bin` is given no access rules of its own and inherits those of the
folder it is in, and a folder under `C:\` is readable by every local account by default, so there any
other account can read the token. It is not a defence against code already running as the user, and
nothing served over loopback can be. While the server runs the token is also in the session file under
`%LOCALAPPDATA%\PrePoMax\sessions\`, which is why that folder is created with a single rule for
that user and with inheritance switched off; the file is removed when the server stops.

**The port.** 8756 by default, set in *Tools - Settings - Automation* or with `--rpcPort`. When it is
taken - by another PrePoMax, or by another program - the endpoint takes the first free port of the
fifteen above it and says which in the output window and the status bar. A port changed in the
settings takes effect the next time the server is started. One port per instance, so
the address is the address of one session.

**Replacing and clearing it.** The context menu of the *Access token* entry offers *Generate the
access token*, which replaces it after asking, and *Clear the access token*, which removes it - use
that before preparing a redistributable. Either way a running server is stopped first, and restarted
onto the new token, so it never keeps serving one that is no longer written down. Every client
configured with the old token needs the new one. Both actions sit beside the value they change.
*Reset all* on the page leaves the token as it is: the token is the result of an action that asks
first, not a setting, and a running server goes on serving the one it started with.

**The pipe.** The same dispatcher is also served on `\\.\pipe\PrePoMax.<process id>`, JSON-RPC
2.0, one object per line, UTF-8, with no token: the operating system authenticates the caller, and
only the user running PrePoMax may open it. It is there for a client that speaks the protocol
directly. Every method is a request and is answered, with one exception: `shutdown` may be sent as a
notification, without an `id`, and is then run without an answer. A notification is never answered,
so not even a refusal comes back: a `shutdown` notification with an argument the method does not
take is refused in silence and does nothing. Send `shutdown` as a request to see that it ran. Any
other notification is dropped unanswered and not run, as on MCP, because an `exec` nobody waits for
would change the model with no way to say what it did, and would hold the turn unseen. A malformed object without an `id` - a wrong
`jsonrpc`, no `method`, `params` that are not an object - is a notification as well and is dropped
the same way; a line that is not JSON, a batch and a line that is not an object carry no `id` that
could be read, and are answered with a `null` one. An `id` that is `null`, or neither a string nor
a number - an object, an array, a boolean - is an invalid request, answered with `-32600` and a
`null` `id`. Either way a running instance writes
`%LOCALAPPDATA%\PrePoMax\sessions\<process id>.json` while its server is on - process id, pipe
name, `url`, `port`, `token`, versions, model file, start time - which is how a client finds it. A
file whose process no longer exists is stale, and the next instance asked for `list_sessions` prunes
it. The folder is restricted to the current user, because the token is in it.

| Method | What it does |
| --- | --- |
| `ping` | Liveness and versions. Answered without touching the model, so it answers while the application is busy. |
| `status` | The state, and the methods this server serves. `connectionCount` counts the pipe connections and an MCP client that sent a request in the last minute. `isRegenerating` is true while Regenerate or an undo replays the history; `isCommandRecordingSuppressed` is true while a recorded script runs, outside a replay, so the commands it executes reach no history of their own. |
| `describe` | The API schema of a `path` - a name followed by `.attribute` and `['name']` or `[index]` steps, read without being evaluated - or of an API type name. |
| `eval` | Evaluates an `expression`; `timeout` is the wait in seconds. |
| `exec` | Runs `source`; `record` is `commands` or `script`, regardless of case, and `timeout` is the wait in seconds. |
| `wait` | Collects the answer of an `exec` or `eval` that outlasted its wait, by its `call_id`, waiting up to `timeout` seconds; `get_call` over MCP. |
| `cancel` | Interrupts a running `exec` or `eval`, by its `call_id`; `cancel_call` over MCP. Never waits for the turn, and is served in read only mode. |
| `history` | The commands - the last `limit` of them, 200 by default, 0 for all, a negative one refused - and the journal with `include_journal`, written to `journal_file` when it is given - a relative one under the temporary folder's `PrePoMax` folder, as `screenshot` places it, and a missing folder it points into created. `journal_file` without `include_journal`, an empty one and one that is not a `.py` file are refused. |
| `screenshot` | Writes a PNG to `file_name`, or to the temporary folder, and returns the path; `transparent_background` and `magnification` (1 to 4) as in the capture dialog. A `file_name` that is not a valid file name is refused. |
| `shutdown` | Stops the server; never closes the application. |
| `exit` | Closes a hidden session; refused for one with a window. `discard_changes` closes it with unsaved changes. |

An argument of the wrong type - `"10"` or `2.5` for a whole number, `"true"` for a boolean, a number
for a string - is refused with `-32602` naming the argument, rather than read as its default. An
argument the method does not take - a misspelt `timout`, or a `timeout` given to `history` - is
refused the same way, naming it, and nothing is done, as the MCP endpoint refuses it. A method the
server does not serve is refused with `-32601` at once, without waiting for a running call to end.

The error codes are the standard JSON-RPC ones plus an application range: `-32001` busy, `-32003` a
refused prompt, `-32004` an API error and `-32005` not allowed - `exit` for a session with a window.
Read only has no code of its own: it blocks the command history, not a method, so an `exec` or `eval`
that tries a change in read only mode is answered as a failed call, with the refusal in its `error`.

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

step = pmx.model.steps.add_static('Step-1', time_period=1.0, incrementation_type='Automatic')
step.bcs.add_fixed('Fix-1', pmx.create.region('NodeSetName', 'Bottom'))
step.loads.add_pressure('Pressure-1', top_selection, 2.5)
step.field_outputs.add_nodal('NF-Output-1', ['U', 'RF'])
```

The results tree is reached the same way, one level at a time.

```python
pmx
pmx.results
result_names = pmx.results.get_names()
pmx.results.current = result_names[-1]
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
| `to_array()` | Returns a copy of the strings as a plain Python `list[str]`. |

### `IntList`

Read-only list wrapper for integers.

| Member | Description |
| --- | --- |
| `count` | Number of IDs. |
| `len(list)` | Number of IDs. |
| `list[index]` | ID at zero-based index. |
| `item in list` | Checks whether the ID exists. |
| `contains(item)` | Checks whether the ID exists. |
| `to_array()` | Returns a copy of the integers as a plain Python `list[int]`. |

### `NumericArray`

Read-only fallback for numeric arrays when NumPy is unavailable.

| Member | Description |
| --- | --- |
| `shape` | Array dimensions as an `IntList`. |
| `size` | Total number of values: the product of the shape, as NumPy reports it. |
| `len(array)` | Number of rows, the length of the first dimension. |
| `array[index]` | Value or row at the specified index. |
| `to_array()` | Returns a copied CLR array. |

## Root API

### `PrePoMaxAPI`

Root object exposed as `pmx`.

| Member | Description |
| --- | --- |
| `model` | The model tree of the currently open model. |
| `results` | Loaded result names and the currently selected result. |
| `selection` | Highlights a selection in the view and clears the highlight. Selections are made by `create`. |
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
| `pmx.new_model(model_space, unit_system)` | Creates a new empty model. `Undefined` is refused for either argument. **Console only.** |
| `pmx.open(file_name)` | Opens a .pmx or .pmh model file, which replaces the command history; a .pmh history is regenerated whole, as **File - Open** regenerates it. A result file is refused and named to `pmx.results.open`. **Console only.** |
| `pmx.import_file(file_name, only_materials=False)` | Imports a geometry or mesh file into the current model. |
| `pmx.save(file_name)` | Saves the model to a .pmx file. |
| `pmx.export_to_calculix(file_name)` | Exports a CalculiX .inp file. |
| `pmx.export_to_abaqus(file_name)` | Exports an Abaqus .inp file. |
| `pmx.export_to_3mf(file_name, edge_thickness_ratio=1)` | Exports the model to a 3MF file. `edge_thickness_ratio` must be finite and larger than zero. |
| `pmx.export_geometry(part_names, file_name, format=None)` | Exports the named geometry parts; the extension picks .stp/.step, .brep, .stl or .mesh (Mmg). |
| `pmx.export_mesh(part_names, file_name, format=None)` | Exports the nodes and elements of the named mesh parts; the extension picks .inp (CalculiX, or `format="Abaqus"`), .msh (Gmsh), .mesh (Mmg) or .stl. |
| `pmx.export_model(file_name, format=None)` | Exports the whole model as a solver input deck; .inp is CalculiX unless `format="Abaqus"`. |
| `pmx.export_results(part_names, file_name, format=None)` | Exports the named parts of the current result in their deformed shape; the extension picks .inp (CalculiX mesh) or .stl. |

The four source based exports mirror the **File - Export** menu, whose items are Geometry, Mesh, Model
and Results: the item names what is exported and the save dialog's filter, or the extension typed,
names the format. `export_geometry` takes **geometry** parts, refuses a part without CAD data for
.stp and .brep, and refuses a part that is neither a solid nor a shell - an imported .stl body - for
.mesh; `export_mesh` takes the parts of the **model mesh** and writes only their nodes and
elements, which is how a part of a meshed model is handed to another program without its steps,
loads and boundary conditions; `export_results` takes the parts of the **current result**. The
`format` argument is needed only when the extension does not name the format - an Abaqus .inp - or
when the file should carry another extension; its values are the PascalCase format names, matched
regardless of case and written by the journal in this spelling (`"Calculix"`, `"Abaqus"`, `"Gmsh"`,
`"Mmg"`, `"Stl"`, `"Step"`, `"Brep"`, `"StepAssembly"`, `"BrepAssembly"`). A journal of a history
recorded before these four existed emits the old per format exports through them, except an .stl or
Mmg export of the displayed view, whose source was never recorded and which the journal reports as
unsupported.

`"Step"` and `"Brep"` write one file per part, named `<file_name>_<part_name>`, and are what the
extension alone selects. `"StepAssembly"` and `"BrepAssembly"` carry the same extensions but write
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
| `name` | The model name. Writable; an empty name, or one that starts or ends with a space, is refused. |
| `model_space` | `ThreeD`, `PlaneStress`, `PlaneStrain` or `Axisymmetric`. Writable; `Undefined` and `None` are refused. Assigning it sets the model space of a new model, as the new model dialog does, and leaves the element types as they are, so once the model has a geometry or a mesh a defined model space is changed by `replace(model_space=...)`, which updates them. |
| `unit_system` | `M_KG_S_C`, `MM_TON_S_C`, `M_TON_S_C`, `IN_LB_S_F` or `UNIT_LESS`. Writable; `Undefined` and `None` are refused. |
| `model_type` | `GeneralModel`, `Submodel` or `SlipWearModel`. Writable. A `Submodel` needs an existing global results file and a `SlipWearModel` cycles its increment divides, so a change to one of them is made by `replace(...)` together with those values. |
| `global_results_file_name` | The results file a submodel reads its boundary values from. Writable on a `Submodel`; as the dialog does, it takes a file name without path, with no space, `:`, `\`, `/` or non UTF-8 character, and the file must exist in the work directory, so it cannot be cleared. Another model type reads it as `None`. |
| `slip_wear_results` | Which results a slip wear model keeps. Writable on a `SlipWearModel`; another model type reads it as `None`. |
| `number_of_cycles`, `cycles_increment`, `number_of_smoothing_steps` | The slip wear model values as whole numbers (`int`), evaluated from the value or equation they were entered with. Writable on a `SlipWearModel` with an `int` or a `str` equation, as `replace(...)` takes them, and `get_equations()` returns the equation. Another model type reads them as `None`. |
| `get_equations()` | The expressions those three values were entered with, without evaluating them, as a `dict[str, str]`, on a `SlipWearModel`; another model type returns an empty `dict`. Only a value entered as an expression is returned, with its leading `=`; a value entered as a plain number is left out, and the property of the same name returns it. |
| `bdm_remeshing` | Whether a slip wear model uses BDM remeshing. Writable on a `SlipWearModel`; another model type reads it as `None`. |
| `absolute_zero`, `stefan_boltzmann`, `newton_gravity` | The physical constants as a `float`, or `None` while they are undefined. Writable; assigning `None` makes that constant undefined. |
| `replace(name=None, model_space=None, model_type=None, ..., clear_undefined_constants=False)` | Replaces several properties in one command. A value that is not given is kept. `model_space` changes the model space between `PlaneStress`, `PlaneStrain` and `Axisymmetric` and updates the element types of the mesh, as the dialog does; a change between a 2D space and `ThreeD`, and `Undefined`, are refused. `number_of_cycles`, `cycles_increment` and `number_of_smoothing_steps` take an `int` or a `str` equation, rounded to a whole number. `number_of_cycles` and `cycles_increment` must be 1 or larger and `number_of_smoothing_steps` 0 or larger; a smaller value is refused naming the argument rather than raised to the minimum. A value the model type does not use - `global_results_file_name` on a model that is not a `Submodel`, `slip_wear_results`, `number_of_cycles`, `cycles_increment`, `number_of_smoothing_steps` or `bdm_remeshing` on one that is not a `SlipWearModel` - is refused naming it, judged by the `model_type` the call ends with, so `replace(model_type='SlipWearModel', number_of_cycles=10)` is accepted. As the dialog does, the properties a call ends with are refused when a `SlipWearModel` has a `number_of_cycles` its `cycles_increment` does not divide, or when a `Submodel` has no global results file in the work directory; the file is looked up only when the call gives `global_results_file_name` or makes the model a `Submodel`, so a physical constant or a new name set on a submodel, and a replayed journal, do not depend on the work directory. An assignment to any property, a new name and a physical constant included, is judged on the properties it leaves in the same way. |

A physical constant that has never been given reads back as `None`. Because a value that is not given
is kept, clearing one is an explicit request: `replace(clear_undefined_constants=True)` makes every
constant it was not given undefined again. Only a physical constant takes `None` as a value:
assigning `None` to any other writable property is refused naming it, rather than leaving the value
as it was, and so is a value that is not a string for a property that takes one.

```python
pmx.model.properties.replace(model_type='Submodel', global_results_file_name='global.frd')
pmx.model.properties.absolute_zero = -273.15
```

### `Parameters`

| Member | Description |
| --- | --- |
| `add(name, value, description=None)` | Adds a parameter. The value is a number or an equation string. |
| `replace(old_name, name=None, value=None, description=None)` | Replaces a parameter. A value that is not given is kept. A new name or value that would leave another parameter unable to be evaluated is refused naming that parameter, as the parameter editor refuses such a table; `rename` and `remove` refuse the same way, and removing a parameter together with every one that uses it is allowed. `set_all` changes the parameters together. |
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
An equation that cannot be evaluated - one that uses its own parameter, directly or through others,
or a parameter that does not exist - is refused naming `value` (or the name in `names`), and nothing
is recorded. A name with leading or trailing spaces is refused by `set_all` as by `add`, never trimmed.

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
| `contains(name)`, `name in collection` | True when an item with that name exists, regardless of case: exactly the names `get` finds, so a name padded with spaces is not found. |
| `get(name)`, `collection[name]` | The item with that name. |
| `len(collection)`, iteration | The count, and iteration in model order. |
| `first()`, `last()` | The first and the last item. |
| `duplicate(names)` | Duplicates the named items using command history. Not every area supports it. |
| `remove(names)` | Removes the named items using command history. |
| `activate(names)`, `deactivate(names)` | Keeps the items in the model but stops writing them to the solver input file. Not every area supports it. |
| `rename(old_name, new_name)` | Renames an item using command history. A `new_name` spelled exactly as the item's stored name changes nothing. |

A name is found regardless of case, but it is passed on in the model's own spelling: `parts.hide('part-1')` hides
the part `Part-1`, and a name an item refers to - the material of a section, the reference point of a constraint, the
surface interaction of a contact pair, the contact pair of a history output - is stored as the model spells it.

Every verb that takes `names`, `hide` and `show` included, refuses a name given twice, in any case, naming `names`; an
operation on model or geometry parts refuses a part named twice in `part_names` the same way.

A collection is indexed **by name only**. Positional access is `list(collection)[index]` or
`get_names()`, because two indexers cannot be resolved from the Python side. `duplicate`,
`activate` and `deactivate` raise a clear error in the areas that have no such command - parameters
cannot be duplicated, and only materials, initial conditions, constraints, contact pairs, mesh setup
items, model parts, steps and the step content can be activated and deactivated. Each item of those
collections reads its state back as `active`.

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
| `generate(part_names)` | Meshes the named geometry parts. The call returns when the meshing has finished, and reports every named part with the reason it was not meshed and an empty reason for one that was. A call that meshes nothing at all raises instead, and records no command. |
| `generate_boundary_layer(selection, thickness)` | Generates a prismatic boundary layer on the selected faces. `thickness` must be a finite value larger than 0. As in the dialog, the model is in 3D model space. |
| `remesh_elements(region, ...)` | Remeshes the elements of a region. The region is a selection or an `ElementSetName` region; the selection target is `elements`, as the dialog picks them, or `geometry`. `max_size` and `min_size` are at least 0, and a `min_size` larger than the `max_size` it ends up with - the one given or the default - is refused naming both; `hausdorff` is larger than 0. |
| `thicken_shell_mesh(part_names, thickness, number_of_layers=1, offset=0, keep_model_edges=False)` | Thickens the shell mesh of the named parts. `thickness` must be larger than 0 and `number_of_layers` at least 1. As in the dialog, the model is in 3D model space and every named part is a manifold, consistently oriented shell part. |
| `split_part_mesh_using_surface(base_part_region, splitter_surface_region, ...)` | Splits a part mesh with a surface. The base part region is a selection or a `PartName` region, the splitter surface region a selection or a `SurfaceName` region. The base part selection target is `parts`, as the dialog picks them, or `geometry`, and the splitter surface selection target is `surfaces`. `max_size` and `min_size` are checked as by `remesh_elements`. As in the dialog, the base part is a solid part meshed with tetrahedra only. |
| `update_nodal_coordinates_from_file(file_name)` | Reads new nodal coordinates from a file. |

The five tools are **one-shot operations**: they run once and are replayed only during a regenerate.
The tools given a selection - `generate_boundary_layer`, and `remesh_elements` and
`split_part_mesh_using_surface` when their region is a selection - store that selection, not the
resolved IDs, so a regenerate that rebuilds a different mesh re-resolves them. `thicken_shell_mesh`
names its parts and `update_nodal_coordinates_from_file` its file.

### `MeshSetupItems`

Every setup item is created from a selection built in the `geometry` view, because the application
rejects a setup item without creation data. The target follows the item type, as it does in the dialog.

| Member | Description |
| --- | --- |
| `add_meshing_parameters(name, selection, ...)` | Meshing parameters. Every value that is not given keeps the application default. |
| `add_local_mesh_size(name, selection, element_size=None, number_of_elements=None)` | A local mesh size. Exactly one of the two values is given. |
| `add_shell_mmg(name, selection, ...)` | An MMG shell mesh item. It selects MMG for STL shell parts and defines its geometry approximation. |
| `add_thicken_shell_mesh(name, selection, thickness, number_of_layers=1, offset=0, keep_model_edges=False)` | Thickens a shell mesh. |
| `add_netgen_mesh(name, selection, number_of_optimize_steps_2d=None, number_of_optimize_steps_3d=None, quad_dominated=None, transfinite_three_sided=None, transfinite_four_sided=None, transfinite_angle_deg=None)` | A Netgen mesh item for solid and shell parts - tetrahedra on a solid, triangles or a quad-dominated mesh on a shell. The mesh size comes from the meshing parameters; the item carries the settings that apply to Netgen alone. It asks for Netgen on a solid part whose geometry would otherwise be meshed transfinitely or swept. A part meshed by Netgen without this item uses its default values. |
| `add_tetrahedral_gmsh(name, selection, ...)` | A Gmsh tetrahedral mesh item. |
| `add_shell_gmsh(name, selection, ...)` | A Gmsh shell mesh item. |
| `add_transfinite_mesh(name, selection, allow_pyramid_elements=False, ...)` | A Gmsh transfinite mesh item. |
| `add_sweep_mesh(name, selection, number_of_global_smooth_steps=0, ...)` | A Gmsh sweep mesh item. The selection names the source surfaces; the model derives the sweep direction, the centre, the side surfaces and the layer edges from them. A part selection - a solid or a compound of solids - lets the model find the source surfaces itself. Either selection may span several parts that stand apart from each other, and each of them is then swept on its own. A cell of a compound the sweep cannot reach is meshed by tetrahedra, by Netgen, with a pyramid on every quadrilateral a swept cell leaves on the face they share, rather than refusing the compound, and the output window says why each such cell is left out - what keeps a cell that sweeps on its own from sweeping with the others, or why it cannot be swept at all; a compound all of whose cells sweep is swept as before. A part selection also revolves a solid, or a cell of a compound, that the sweep between two ends cannot reach and that is turned about an axis its source touches: its source is turned through equal layers, and the elements beside the axis collapse into wedges, pyramids and tetrahedra. A wall of a revolved cell may be split along the revolution, by the faces of a neighbour standing on it, and the layers then end at the angles of the split. Revolved cells about one axis are turned the same way round, so that the target of one is the source of the next; a revolved cell shares its walls with other revolved cells and with swept cells, whose counts are settled with its own, hands its surfaces on as the sources of swept cells, and is revolved from the target of a swept cell, which is meshed before it. A swept cell ending on the target of a revolved cell is swept the other way round, from it, when its other end is free or the source of a revolved cell. Swept and revolved cells may form a ring, as the sides and the corners of a frame do; a swept cell closes it, ending on the meshed source of the cell after it. A cell that could be swept as well as revolved is swept, unless its sweep meets a revolved cell in any other way: it then follows that cell and is revolved about the same axis, and the cells it touches follow it in turn. A cell a revolved cell would share a surface with in any other way leaves it to tetrahedra. `symmetry` is `Automatic` (the default for a new item) or `Off`: with `Automatic`, a compound whose cells are mirror images across the planes it is split on sweeps one symmetric piece of its cells and mirrors the mesh onto the others; a cell of the piece the sweep cannot reach is meshed by tetrahedra and mirrored the same way. The symmetry is used only when every cell has a mirror image, every edge of an image is an edge of the cell it is meshed from, and every other mesh setup item selects the images of what it selects; otherwise the compound is swept whole, and when it has a mirror plane the output window says what keeps it from being used - a vertex or edge of one cell its image does not have, named with how far off it is. An item read from a file written before the argument existed has `Off`. The structured sources are the 3-sided and 4-sided faces every mesher structures, by the item's `transfinite_three_sided`, `transfinite_four_sided` and `transfinite_angle_deg`, whether gmsh or QuadWild meshes them: a side may be a chain of edges, a 3-sided source is three blocks of quads around one inner node, or with `transfinite_three_sided="Polar"` the grid collapsed at one corner, which sweeps into hexahedra and a fan of wedges along that corner as a revolution would, opposite sides are balanced to the coarser count together with the walls, and the meshing is refused when numbers of elements set on edges, or the layer counts of a neighbouring block, cannot agree with them. A structured source whose grid folds - a quadrilateral with a corner of 180 degrees or more, from which every swept element would be inverted - is refused with the surface named; a quadrilateral like that in a source the sweep meshes free is split into two triangles, which sweep into wedges. `number_of_global_smooth_steps` is 0 or larger; a negative count is refused. |
| `add_shell_quad_wild(name, selection, preview=None, smooth=None, alpha=None, triangulation_mesher=None, flow_profile=None, keep_files_on_failure=None, debug_visualization=None, old_algorithm=None, pin_edge_nodes=None, edge_rotations=None, singularity_position=None, number_of_search_rounds=None, boundary_layer=None, transfinite_three_sided=None, transfinite_four_sided=None, transfinite_angle_deg=None)` | A QuadWild shell mesh item. `keep_files_on_failure=True` retains its MeshBatch folder when a QuadWild stage or mesh validation fails. `debug_visualization=True` adds the QuadWild patches as parts, the irregular, prescribed, patch-corner and seam nodes as node sets, the sheets of a shell compound as element sets, and the cross field and feature lines as wire parts; with `preview=True` it shows the traced triangle patches. `old_algorithm=True` meshes without the QuadWild patch layout and mesh quality improvements, for comparison. `pin_edge_nodes=True` keeps every triangulation node on the model edges, as a sweep source meshed after the 1D mesh would need, for comparison. `edge_rotations=True` turns on the quality-driven edge rotations, which change the connectivity where the quads are poor; by default they are off and the connectivity of the quad generation is kept. `singularity_position` (0 = off, 2 to 4 worth testing) weighs placing the irregular node of every 3-, 5- or 6-sided patch where its separatrices fit the patch geometry, for comparison. `number_of_search_rounds` (0 = off) is the number of rounds of the search over the element counts of the patch sides; every round tries the sides of the three worst patches at one or two elements more or fewer and keeps the best change, at the cost of a quantisation and a full smoothing per trial. `boundary_layer` is `None` (default), `Holes` (one ring of regular quads around every closed edge loop without corners) or `All` (along every model edge; helps fine meshes, can hurt coarse ones and shell compounds). `transfinite_three_sided` (`Mapped` by default, `Polar` or `No`) and `transfinite_four_sided` (`True` by default) take the model faces with 3 and 4 sides whose corner angles lie between 180° minus `transfinite_angle_deg` (degrees, default 135) and `transfinite_angle_deg` out of the QuadWild patch layout and mesh them as structured grids (4 sides) or three structured blocks around one inner node (3 sides), or with `Polar` the grid collapsed at one corner, whose cells there are a fan of triangles and whose side across from it QuadWild gives an even count, like the transfinite faces of gmsh; QuadWild still chooses their element counts together with the rest of the mesh and gives the opposite sides of a 4-sided face equal counts. A sweep source meshed by QuadWild follows the transfinite settings of its sweep mesh item. `preview=True` shows the triangulation without running QuadWild, so, as in the dialog, `smooth`, `alpha`, `flow_profile`, `boundary_layer` and the three transfinite settings are refused naming `preview` and left out of `get_settings` and the journal while it is on. Every value that is not given keeps the application default. |
| `replace_meshing_parameters`, `replace_local_mesh_size`, `replace_shell_mmg`, `replace_thicken_shell_mesh`, `replace_netgen_mesh`, `replace_tetrahedral_gmsh`, `replace_shell_gmsh`, `replace_transfinite_mesh`, `replace_sweep_mesh`, `replace_shell_quad_wild` | Every `add_*` has a matching `replace_*` taking `old_name` first, then an optional `name` and `selection`, then the same arguments. A value that is not given is kept, and the region survives a replacement that does not name a new selection. |

A `replace_*` edits one item type: naming an item of another type is refused, saying which `add_*` made it - `The mesh setup item 'X' is made by add_netgen_mesh, so replace_sweep_mesh cannot replace it.`

An item the model cannot honour is **refused rather than recorded**, on an add and on a replace that
names a new selection, and the refusal says why - a shell mesher on a solid part, a swept mesh on
an STL part, a mesher defined on some of the sub-parts of a compound part and not the others. This is
the question the dialog asks before it applies. An item that was accepted and
later became invalid, because the part it covered was removed, becomes valid again as soon as a
replace points it at geometry it can be defined on: a replacement is judged by the definition it
carries, not by the verdict of the item it replaces.

`add_meshing_parameters` accepts `relative_size`, `factor_max`, `factor_min`, `max_size`,
`min_size`, `grading`, `elements_per_edge`, `elements_per_curve`, `second_order`,
`midside_nodes_on_geometry`, `split_compound_mesh`, `merge_compound_parts` and `merge_cells`.
`relative_size=True` sizes the Netgen mesh by `factor_max` and `factor_min` times the bounding box diagonal of
each part, and `relative_size=False` by `max_size` and `min_size`. The dialog offers the switch only in its
advanced view and meshes by the sizes in its basic view, whatever switch it keeps hidden there, so
`relative_size=True` also turns the advanced view on, and `relative_size` reads back `True` only for an item that
is sized by its factors. Only Netgen honours the relative sizes: Gmsh, Mmg and the limits of a local mesh size
always use `max_size` and `min_size`, so those two are taken and read back by `get_settings` in either mode. The
factors are used only with `relative_size=True`, so with `False` they are refused naming `relative_size` - judged
on the switch the item ends up with, the one given or the one it keeps - and left out of `get_settings`; they keep
their values for a later switch back. `midside_nodes_on_geometry` is used only for second order elements, and the
dialog shows it only then, so it is refused with `second_order=False` - the one given or the one the item keeps - and
left out of `get_settings` and the journal. The Netgen fineness is written for no mesher and the dialog does not
show it, so the API does not take it.
`merge_cells`, `True` by default, meshes each part a split cut as one mesh part; `False` keeps its
cells as separate, conforming mesh parts, so they can carry different sections. The Netgen settings
`number_of_optimize_steps_2d`, `number_of_optimize_steps_3d`, `quad_dominated`, `transfinite_three_sided`,
`transfinite_four_sided` and `transfinite_angle_deg` belong to `add_netgen_mesh`. `transfinite_three_sided`
(`Mapped` by default, `Polar` or `No`) and `transfinite_four_sided` (on by default) mesh the 3-sided and the
4-sided faces every mesher structures as structured grids - a 3-sided face as three blocks of quads around one
inner node, or with `Polar` the grid collapsed at one corner with triangles there - with quads when `quad_dominated` is set and triangles otherwise. Opposite sides are balanced to the coarser count, a number of elements set on an
edge as a number is kept (one an element size gives is not), and the meshing is refused when the set numbers cannot
agree. A compound is structured on the faces of its solids; a face two solids share is one face. An item read from
a file written before a setting existed has it off.
Like the dialog, it derives `max_size` and `min_size` from the size of the selected parts, and the
arguments of the call are applied on top of those. `replace_meshing_parameters` does the same when
it is given a new `selection` and no other value, so the sizes follow the geometry the item now
covers; a replace that also sets a value is the caller's and keeps everything it is not told about.
This is what the dialog does when the selection changes and nothing was typed into the property
grid. Only sizes that still equal the part defaults of the item's previous region follow it: sizes
an earlier call set are kept, the way the dialog keeps the stored values of an item it edits. The
relative switch, and the view that offers it, are kept as they are.

Every `add_*` and `replace_*` of a mesh setup item asks the model whether it can define the item, the way every apply of
the dialog does, so a value the model refuses - a QuadWild `alpha` outside 0 to 1, for one - is refused by a replace that
changes only that value, and nothing is recorded.

Before that, every value is checked against the range the model holds it to, and a value outside it is refused naming
the argument rather than clamped, moved or passed on: `max_size` and `min_size` at least 0; `factor_max`, `factor_min`
and `grading` larger than 0 and at most 1; `elements_per_edge` and `elements_per_curve` larger than 0;
`element_size` larger than 0 and `number_of_elements` at least 1; `number_of_optimize_steps_2d` and `number_of_optimize_steps_3d`
at least 0; the Netgen `transfinite_angle_deg` between 0 and 180, both excluded, and the QuadWild one from 90 to 180 (the Gmsh
items and their dialog take any finite `transfinite_angle_deg` and `recombine_min_quality`: a face keeps its
transfinite constraint while its corner angles lie between 180 minus the angle and the angle, so 180 or more keeps it
on every face);
`hausdorff` and `relative_hausdorff` larger than 0; `thickness` larger than 0 and `number_of_layers` at least 1;
`quad_wild_alpha` of a sweep from 0 to 1; `singularity_position` and `number_of_search_rounds` at least 0. A value the
item does not use in the state it ends up with is refused before its range, naming the argument that would make it
used: the factors against `relative_size`, the two Hausdorff distances of a shell MMG item against `relative_size`,
the QuadWild settings of a sweep against `algorithm_2d`, the `recombine_min_quality` of a Gmsh item against
`algorithm_recombine='None'`, the `transfinite_angle_deg` of a Gmsh, Netgen or QuadWild item against
`transfinite_three_sided='No'` together with `transfinite_four_sided=False`, and the `smooth`, `alpha`,
`flow_profile`, `boundary_layer` and transfinite settings of a QuadWild shell item against `preview=True`, which
shows the triangulation without running QuadWild. Such a value is also left out of `get_settings`. A minimum is checked
against the maximum the item ends up with - the one given, or the one it keeps or takes from the selected parts - so a
`min_size` larger than `max_size`, or a `factor_min` larger than `factor_max`, is refused naming both instead of
dragging the other value along, and `split_compound_mesh` and `merge_compound_parts` cannot both end up `True`.
`triangulation_mesher` of a QuadWild item is `Gmsh`, `Netgen` or `Mmg`.

`add_shell_mmg` accepts `relative_size`, `relative_hausdorff`, `hausdorff` and `keep_model_edges`.
`relative_size=True`, the default, uses `relative_hausdorff` times the bounding box diagonal of the part, and
`relative_size=False` the absolute `hausdorff`. The dialog shows only the distance in use, so the other one is
refused naming `relative_size` and left out of `get_settings`, and keeps its value for a later switch back.
Adding this item to an STL shell selects MMG; without an explicit mesher setup item, Netgen is used.
Like the dialog, it derives `hausdorff` from the size of the selected parts - the bounding box
diagonal times `relative_hausdorff` - and the arguments of the call are applied on top of that.
`replace_shell_mmg` rebuilds it from the newly selected parts under the same rule as
`replace_meshing_parameters`, keeping `relative_size`. MMG continues to use the shared maximum/minimum size and element order
values from `meshing_parameters`.

Every Gmsh item accepts the settings its dialog shows and no others, so the arguments differ per
item, with one exception: the transfinite mesh also takes `algorithm_2d` and `algorithm_3d`, which its dialog keeps
hidden, because they set the mesh size the 2D algorithm needs and mesh the faces and volumes it does not structure.
A shell has no volume and a sweep builds its volume mesh itself, so neither takes `algorithm_3d`.
`recombine_min_quality` is shown and taken only while `algorithm_recombine` is not `None`, and
`transfinite_angle_deg` only while `transfinite_three_sided` is not `No` or `transfinite_four_sided` is `True`.
`transfinite_three_sided` of a Gmsh item is `Polar`, `Mapped` (the default for a new item) or `No`: a
3-sided face with `Polar` is the structured grid gmsh collapses at one corner, with triangles there and arcs around
it as a revolution about that corner would make, and its two sides meeting there carry one number of elements. The
corner collapsed is the one whose other two corners lie closest to a right angle, of corners within a degree of that
the one between the two sides closest in length, so the axis of a pie cut from a cylinder. `Mapped` is three blocks
of quads around one inner node, `No` a free mesh. An item read from a file written while the setting was a switch
has `Polar` where it was on. The Netgen and QuadWild items and the meshing parameters take the same three values
(`No` the default of the meshing parameters, `Mapped` of a Netgen and a QuadWild item), and collapse the same
corner; one read from a file written while the setting was a switch has `Mapped` where it was on, the grid they made
then. QuadWild gives the side across from the collapsed corner an even number of elements, as its quads need; a
polar face the counts fixed on its sides do not allow is meshed as an ordinary patch.

| Item | Settings |
| --- | --- |
| `tetrahedral_gmsh` | `algorithm_2d`, `algorithm_3d`, `recombine_min_quality`, `algorithm_recombine`, `transfinite_three_sided`, `transfinite_four_sided`, `transfinite_angle_deg`, `optimize_first_order_solid`, `optimize_high_order` |
| `shell_gmsh` | `algorithm_2d`, `recombine_min_quality`, `algorithm_recombine`, `transfinite_three_sided`, `transfinite_four_sided`, `transfinite_angle_deg`, `optimize_first_order_shell` |
| `transfinite_mesh` | `allow_pyramid_elements`, `algorithm_2d`, `algorithm_3d`, `recombine`, `transfinite_angle_deg`, `transfinite_three_sided` (`Polar` caps the five-faced cells with the grid gmsh collapses at one corner and fills them with wedges and hexahedra; `Mapped` meshes the caps as three blocks of quads and, recombined, the cells by hexahedra only) |
| `sweep_mesh` | `number_of_global_smooth_steps`, `algorithm_2d` (a gmsh algorithm, or `QuadWild`: QuadWild meshes the source surfaces with quadrilaterals and chooses the element count of every source edge that no edge mesh size, other sweep block or neighbouring part fixes), `recombine_min_quality`, `algorithm_recombine`, `transfinite_three_sided`, `transfinite_four_sided`, `transfinite_angle_deg`, `optimize_first_order_shell`, `elements_per_layer`, `quad_wild_flow_profile`, `quad_wild_alpha`, `quad_wild_smooth`, `symmetry` (`Automatic` or `Off`, see `add_sweep_mesh`). gmsh meshes the walls whichever mesher meshes the sources, so the recombination and optimisation settings apply with either `algorithm_2d`; as in the dialog, the `quad_wild_*` settings are used only with `algorithm_2d='QuadWild'`, refused naming `algorithm_2d` otherwise and left out of `get_settings` |

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
failed = {name: why for name, why in pmx.model.mesh.generate(part.name).items() if why}
```

### `MeshSetupItem`

| Member | Description |
| --- | --- |
| `name` | The item name. |
| `item_type` | The item type, named after the `add_*` method that makes it without `add_`: `meshing_parameters`, `local_mesh_size`, `shell_mmg`, `thicken_shell_mesh`, `netgen_mesh`, `tetrahedral_gmsh`, `shell_gmsh`, `transfinite_mesh`, `sweep_mesh` or `shell_quad_wild`. |
| `mesher_type` | `None`, `Netgen`, `Mmg`, `Gmsh` or `QuadWild`. |
| `active`, `valid` | Item state. |
| `get_geometry_ids()` | The geometry IDs the item was created from, as an `IntList`. |
| `get_settings()` | The stored mesher settings of this item type, as a `dict[str, Any]`, keyed by the name of the `replace_*` argument that sets each - `max_size`, `min_size`, `element_size`, `number_of_optimize_steps_2d`, `number_of_optimize_steps_3d`, `number_of_search_rounds`, `algorithm_2d`, `algorithm_3d`, `elements_per_layer` for a sweep, `transfinite_angle_deg` - in the form that argument takes: a transfinite mesh reports `recombine` as a `bool`, not the algorithm it picks, and a local mesh size only the one of `element_size` and `number_of_elements` it uses. A setting the item does not use in its current state - a factor of absolute meshing parameters, the Hausdorff distance a shell MMG item does not use, a recombination quality without recombination, a transfinite angle with no face structured, the quadrangulation settings of a QuadWild preview, the QuadWild settings of a gmsh sweep - is left out, as the replace refuses it. Nothing else is returned, so `replace_<type>(name, **item.get_settings())` changes nothing; a list valued setting such as `elements_per_layer` is a Python list. |

### `Nodes` and `Elements` of the model mesh

Reached as `pmx.model.nodes` and `pmx.model.elements`. They are the editing side of the model mesh,
not the read-only result mesh of the same name under `pmx.results`.

| Member | Description |
| --- | --- |
| `nodes.count`, `elements.count` | The node and element counts of the model mesh. |
| `nodes.renumber(start_id)` | Renumbers the nodes using command history. |
| `nodes.merge_coincident(selection, tolerance=None, nodes_to_keep=None)` | Merges the coincident nodes of the selected geometry. The selection is made in the `femodel` view with the `geometry` target, or the `nodes` target the dialog picks. `nodes_to_keep` is `SmallerID` or `LargerID`. |
| `elements.renumber(start_id)` | Renumbers the elements using command history. |

### Part collections

`pmx.model.geometry.parts` and `pmx.model.parts` share the collection verbs plus the members below.
Every `part_names` argument accepts a single name or a list of names.

| Member | Description |
| --- | --- |
| `hide(part_names)`, `show(part_names)` | Part visibility. |
| `set_color(part_names, color)` | The color is `[R, G, B]`, `[R, G, B, A]`, or a color name such as `'Green'`. An alpha below 25 is refused naming `color`: the dialog raises it to 25, so a part never becomes invisible. |
| `reset_color(part_names)` | Restores the default color. |
| `set_opacity(part_names, opacity)` | The alpha channel of the part color as an integer from 25, the most transparent value the dialog allows, to 255 for opaque; a lower value is refused. Geometry, model and result parts take it alike. |
| `translate(part_names, translate_vector, number_of_copies=0)` | Translates the parts. |
| `rotate(part_names, rotate_center, rotate_axis, rotate_angle_deg, number_of_copies=0)` | Rotates the parts about an axis; the angle is in degrees and right handed about the axis. |
| `mirror(part_names, mirror_point, mirror_direction, copy=False)` | Mirrors the parts over the plane through the point with the direction as its normal. |
| `scale(part_names, scale_center, scale_factors, copy=False)` | Scales the parts. Every point, vector, factor and angle of the four transforms must be finite; a NaN or an infinity is refused naming the argument, a component as `scale_factors[1]`. |
| `get_element_type_names()` | The names of the 14 element type slots a part carries, for example `linear_tetra`. |
| `get_element_types(part_name)` | The element type of every slot of a part, as a `dict[str, str]` of slot name to element type name, for example `{'linear_tetra': 'C3D4'}` - the form `set_element_type` and `replace_properties` take. The part type is not a slot, and only the element kinds the part contains are listed. |
| `set_element_type(part_names, element_type, value)` | Sets one element type slot using command history. A value that names no element type of the slot is refused, and so is `None`. An element kind the part does not contain is refused with an error naming the kinds it has. |
| `replace_properties(old_name, name=None, color=None, element_types=None)` | Replaces the name, the colour and the element type slots in one call. The element types are a map of the slot name to its value, the way `get_element_types` names them. A value that is not given is kept. An element kind the part does not contain is refused, naming the kinds it has, and so is the value `None`. A color with an alpha below 25 is refused, as by `set_color`. |

`GeometryParts` adds `add_compound`, `regenerate_compound`, `swap_geometries`,
`convert_shell_to_solid`, `convert_solid_to_shell`, `find_edges_by_angle`, `flip_face_orientations`,
`split_solid`, `split_face`, `rename_split_sub_part`, `group_split_cells`, `fuse_bodies`, `defeature`, `offset_face`,
`delete_cad_faces`, `fuse_faces`, `repair`, `repair_preview`, `split_periodic_faces`, `flip_stl_normals` and
`delete_stl_faces`. The selection based methods take a
selection built in the `geometry` view, with the `geometry` target except for the two splits: `split_solid` takes the
`parts` target and `split_face` the `geometry_surfaces` target, which is what the two dialogs pick. The face
operations - `defeature`, `offset_face`, `flip_face_orientations`, `delete_cad_faces`, `fuse_faces` and
`delete_stl_faces` - take either `geometry` or `geometry_surfaces`, the target their dialogs pick and the journal
writes.

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

`defeature` returns the number of faces every selected part actually lost. The faces picked on the
sub-parts or split cells of a compound are removed from the compound as a whole, so a feature on a face two
of them share, or one running across them, closes on both sides and they still meet where they did; the
compound and its sub-parts keep their names and part ids.
The modelling kernel returns a shape whether or not it closed anything, so a part that reports `0`
is a feature it could not remove: a blind hole is only closed when its bottom face is selected as
well as its wall.

`split_solid(selection, tool, fuzzy_tolerance=0)` and `split_face(selection, tool,
fuzzy_tolerance=0)` are the Split Solid and Split Face dialogs. The tool is made by one of the
`pmx.create.split_tool_*` methods, and both return the number of faces every part of the selection
gained, as a `dict[str, int]`, so a part the tool missed reports zero. A solid split names parts: one
sub-part of a compound names that solid only and the compound names all of them, and a part the tool
cuts in two is replaced by a compound part of the pieces, which does not keep its name. A face split
names surfaces: only those are divided, the part keeps its name and stays the kind of body it is, and
a part whose divided surfaces would not give back the same body is left as it was. The line through
two points is drawn on a surface, so only `split_face` takes it. The fuzzy tolerance is how far apart
the modelling kernel still takes geometry as coincident; leave it at zero unless an imported geometry
is too inaccurate to be split. A split whose parts the exploded view holds at different offsets is
refused, because the tool would not cut where it appears to.

```python
region = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])
tool = pmx.create.split_tool_plane_by_point_and_normal([30, 20, 10], [0, 0, 1])
added = pmx.model.geometry.parts.split_solid(region, tool)   # {'Solid-1': 1}

top = pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces', {part.id: [3]})
line = pmx.create.split_tool_line_by_two_points([[0, 0, 20], [60, 40, 20]])
pmx.model.geometry.parts.split_face(top, line)
```

A compound part made by a solid split gathers its cells under the solids they were cut from, and
those are the sub-parts the tree shows. `part.split_sub_part_names` lists them as a `StringList`, empty for any other
part, and `rename_split_sub_part(part_name, old_name, new_name)` renames one the way renaming it in
the tree does: its cells are renamed after it, and so is the mesh part it was meshed into. A name
another part already uses is refused, compared regardless of case as the tree compares it. The sub-part
and cell names `rename_split_sub_part`, `group_split_cells` and `fuse_bodies` take are matched regardless
of case, a body named twice in any spelling is refused, and a new name is checked as every name is.

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
`part.get_split_cell_names(sub_part_name)` lists the cells of a sub-part as a `StringList`.

```python
steel = compound.get_split_cell_names('Web')[:2]
pmx.model.geometry.parts.group_split_cells(compound.name, steel, 'Steel')
```

`add_compound(part_names)` imprints the parts on one another, so a solid is cut wherever another part
crosses it, and where two parts overlap the overlap is one solid of the compound. A compound of plain
parts makes every solid a sub-part of its own. When one of the parts is a compound made by `split_solid`,
the compound is split as well: its solids are gathered under the parts they came from - the cells of a
split part stay one sub-part, and so do the pieces of a plain part the others cut - and a solid where
the parts overlap is a sub-part of its own, `Overlap-1`, ..., which `group_split_cells` puts under the
part it belongs to. The parts the compound is made of stay in the model, hidden, so its sub-parts take
names and part ids of their own, as a copy does. `regenerate_compound` keeps the sub-parts the compound
has, including any grouping or renaming done on it.

`fuse_bodies(part_name, body_names)` is Fuse Bodies in the tree, and it changes the geometry: the
named bodies of a compound become one solid. A body is a sub-part of the compound, whether the
compound was imported, created with `add_compound` or made by `split_solid`; of a split compound
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
were created, as a `StringList`.

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

`fuse_faces(selection)` is Fuse Faces in the CAD Part menu and undoes what `split_face` did: the
selected faces become one across every edge two of them share where they lie on one surface, and the
edges left on the outside are joined where the cut left a point on them. The shape does not change, so
a part keeps its name, its id and its bodies. Faces of the sub-parts of a compound are fused on the
compound as a whole, and two faces are only fused when they bound the same solids, so the solids still
meet. A fused face that closes on itself, such as the two halves of a cylinder, stays one face. It
returns the number of faces every part lost, and a part whose faces lie on different surfaces reports
zero rather than being refused.

`repair(part_names, sew_faces=True, fix_shape=True, remove_small_faces=True, remove_small_edges=True,
fuse_faces=True, join_edges=True, tolerance=None)` is Repair in the CAD Part menu and works on whole
CAD parts, a sub-part with its compound. The repairs run in that order: sewing joins loose faces and
shells of a part without solids, fixing the shape fixes edge tolerances, open wires, missing curves on
surfaces and orientations, the small faces and edges are those narrower or shorter than the tolerance,
`fuse_faces` fuses every two neighbouring faces on one surface - including those a split divided - and
`join_edges` joins edges on one curve. The tolerance defaults to one ten-thousandth of the diagonal of
each part. A repair that would change the number of solids, or leave a valid shape invalid, leaves the
part as it was. It returns the number of faces every part lost; a part with nothing to repair reports
zero. Switching every repair off, a tolerance that is not positive and a part that does not exist are
refused.

`repair_preview` takes the arguments `repair` takes and runs the repair on a copy, the way Preview in the
Repair dialog does: nothing changes and no command is recorded. It returns a `RepairPreview` for every part
the repair would run on, keyed by its name, so a sub-part is answered under its compound. `edges` lists, as a plain Python list, the
edges the repair would remove or sew, each as an array of the points sampled on it with shape `(n, 3)`, and
`points` the vertices it would remove with shape `(point_count, 3)`; `edge_count` and `point_count` count
them, and `refused` is `True` when the repair would leave the part as it was.

`split_periodic_faces(part_names, number_of_faces=2)` is Split Periodic Faces in the CAD Part menu. Every
periodic face of the named CAD parts - a face that closes on itself, such as the side of a cylinder with
a single seam - is split into `number_of_faces` faces. A sub-part is split with its compound, so the faces
its solids share stay shared, and a part keeps its name, its id and its bodies. It returns the number of
faces every part gained; a part without periodic faces reports zero. Fewer than two faces and a part that
does not exist are refused. Neither the import of a CAD file nor any geometry operation splits periodic
faces, and none of them has to be split for meshing: a sweep meshes a wall closed along a single seam with
the structured grid of its layers around the seam, and finds the mirror symmetry of a compound whatever
angle the seams lie at. The split is for the pieces wanted as faces of their own, to select or to load one
half of a bore. Only the history of an older file, regenerated with Split periodic faces in Settings ->
General, still splits them as it was recorded.

```python
top = pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces', {part.id: [3, 15]})
fused = pmx.model.geometry.parts.fuse_faces(top)   # {'Solid-1': 1}
preview = pmx.model.geometry.parts.repair_preview('Solid-1', tolerance=0.01)['Solid-1']
print(preview.edge_count, preview.point_count)   # 1 2
removed = pmx.model.geometry.parts.repair('Solid-1', tolerance=0.01)
added = pmx.model.geometry.parts.split_periodic_faces('Solid-1')   # the faces each part gained
```

`ModelParts` adds `merge` and `find_edges_by_angle`. Both collections take `find_edges_by_angle(part_names, edge_angle_deg)`, with the angle in degrees, finite and from 0 to 90 as the dialog limits it; any other angle is refused naming `edge_angle_deg`.

```python
pmx.model.parts.translate('Solid-1', [0, 0, 10])
pmx.model.geometry.parts.rotate('Solid-1', [0, 0, 0], [0, 0, 1], 45, number_of_copies=7)
pmx.model.parts.set_color('Solid-1', [255, 0, 0])

selection = pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces', {1: [3]})
# Either remove the faces ...
removed = pmx.model.geometry.parts.defeature(selection)   # {'Solid-1': 4}
# ... or, on the part as it was, offset them
names = pmx.model.geometry.parts.offset_face(selection, -1.5)   # StringList: Solid-1_Offset-1
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
| `color` | The part color as a plain list `[R, G, B, A]`. |
| `visible`, `active` | Part state. |
| `volume`, `center_of_mass` | The canonical part measurements, the center of mass as `[x, y, z]`. Geometry parts use exact CAD values. Model parts keep mesh-computed values and use CAD only to fill a missing value. |
| `surface_area` | The total surface area. A mesh computes an area only for a shell part, so a meshed solid reports the area of the CAD part it was meshed from, and `0.0` when there is none - an imported mesh has none. The area of the mesh itself is the sum of `get_face_data().areas`. |
| `get_inertia_at_center_of_mass()`, `get_inertia_at_origin()` | The 3 by 3 inertia matrices as three rows (`list[list[float]]`), or an empty list when unavailable. They are **second moments of volume**: the density is 1, so the values are in model length units to the fifth power, and a mass inertia is the matrix multiplied by the material density. Only CAD geometry computes them, so a meshed part carries the matrices of the CAD part it was meshed from and an imported mesh has none. |
| `get_bounding_box()` | `[xMin, xMax, yMin, yMax, zMin, zMax]` as a `list[float]`. Geometry parts use the CAD entity bounds when available. |
| `get_node_ids()`, `get_element_ids()` | The part node and element IDs, as `IntList`s. |
| `get_node_coordinates()` | The coordinates of the part nodes with shape `(node_count, 3)`, in the order `get_node_ids()` reports them. This is the route to the nodes of a geometry part, because `pmx.model.nodes` is the model mesh and holds nothing until the model is meshed. |
| `get_face_data()` | The faces of the part as `FaceData`. |
| `get_edge_data()` | The edges of the part as `EdgeData`. |
| `get_vertex_data()` | The vertices of the part as `VertexData`. |
| `get_geometry_metadata()` | Supplemental CAD metadata such as analytic axes, primitive parameters, orientation, entity bounds and topology links. |

`GeometryPart` adds `is_cad_part` and `mesh_type`, the most structured mesh the recognition finds the
part supports whatever the mesh setup items ask for: `Transfinite`, `Sweep`, `Revolve`, `Tetrahedral`,
`Free` for a CAD shell, which gmsh meshes with a structured grid on the faces that can carry one, or for an
open stl based part, `Tetrahedral` for a closed stl based part, `Other` for any other part, or `Unknown` for a part
the recognition has not classified or that cannot be meshed. A compound part reports the most restrictive type of its sub-parts,
except that one in which some cells sweep and others do not reports `Sweep`: the sweep meshes the cells it
cannot reach by tetrahedra. `Revolve` is a solid the sweep between two ends cannot mesh and that is the
revolution of a group of coplanar surfaces about an axis they touch; a sweep item, or the default sweep of a
part with no mesh setup item, meshes it by a revolved block. A revolved solid clear of the axis is left to the sweep.
The recognition depends on the geometry alone, so an exploded view does not change it.
It also adds `split_sub_part_names` (a `StringList`), the sub-parts of a compound part made by `split_solid`,
which `rename_split_sub_part` renames; it is empty for any other part. `get_split_cell_names(sub_part_name)`
lists the cells of one of them as a `StringList`, which `group_split_cells` takes.

`ModelPart` adds `get_inverted_element_ids()`, an `IntList` of the IDs of the inverted or degenerate solid elements
of the part - those with a scaled Jacobian at a corner that is zero or negative. Meshing keeps such
elements rather than refusing the mesh: it writes `<part> has <n> inverted or degenerate elements`
to the output window, where the line is a link that highlights them, and highlights them once
meshing ends. `mesh.generate` reports such a part as meshed, so a script finds the elements here.

#### `FaceData`, `EdgeData` and `VertexData`

The topology of one part, read off the visualization the application keeps for picking. It is how a
script finds the entity it means without a pick in the viewport, and the IDs it reports are the ones
a selection takes.

| Member | Description |
| --- | --- |
| `count` | The number of faces, edges or vertices. |
| `ids` | The 1-based face or edge IDs of the part, as an `IntList`, as the `surfaces` and `edges` selection sources take them. `VertexData` has `node_ids` instead, because a vertex is named by its node. |
| `geometry_ids` | The geometry ID of every entity as an `IntList`, as the `geometry` selection source takes them. |
| `areas`, `lengths` | The canonical area of every face and length of every edge, in model units. Geometry parts use exact CAD values; model and result parts keep mesh-computed values. |
| `centroids` | The centroid of every face or edge with shape `(count, 3)`. Geometry parts use exact CAD centroids when available; mesh parts use the mean of their node coordinates. `VertexData` has `coordinates`. |
| `normals` | Faces only. The mean of the cell normals of every face with shape `(count, 3)`. A curved face has no single normal, so it is the direction the face points on average. |
| `types` | The geometric type of every entity: `Plane`, `Cylinder`, `Cone` and so on for a face, `Line`, `Circle`, `Ellipse` and so on for an edge. |

`ids`, `geometry_ids` and `node_ids` are `IntList`s. Every other array is a NumPy array where NumPy is available, and a `NumericArray` otherwise.
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
`str`; numeric vectors return a `list[float]`, a property whose name ends in `_ids` an `IntList`, and text vectors a `StringList`. Common properties include
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
| `add(name, time_amplitude, time_span=None, shift_time=None, shift_amplitude=None)` | Adds a tabular amplitude. Every row is `[time, amplitude]`. `shift_time` and `shift_amplitude` move every time and every amplitude value by a fixed amount, as the dialog's Modify fields do; both are plain numbers and default to 0. |
| `replace(old_name, name=None, time_amplitude=None, time_span=None, shift_time=None, shift_amplitude=None)` | Replaces an existing amplitude. Everything that is not given is kept, the shifts included. |

`Amplitude` exposes `name`, `time_span`, `shift_time`, `shift_amplitude`, `row_count` and `get_time_amplitude()`.

### `Distributions`

| Member | Description |
| --- | --- |
| `add_from_equation(name, equation, coordinate_system_name=None)` | One equation string for the magnitude, or three, one per direction. `coordinate_system_name` names the coordinate system `x`, `y` and `z` are measured in; it is `'Global'` by default, matched regardless of case and stored in the model's spelling. |
| `add_from_file(name, file_name, interpolator_type=None, interpolator_radius=None, scale=None, translate=None, coordinate_system_name=None)` | Reads the distribution from a point cloud file. `coordinate_system_name` is taken as by `add_from_equation`. The `ClosestPoint` interpolator, the default, has no radius, which the dialog then hides, so `interpolator_radius` is refused with it - judged on the interpolator the distribution ends up with - and reads back `None`. |
| `replace_from_equation(old_name, name=None, equation=None, coordinate_system_name=None)` | Replaces an equation distribution. A value that is not given is kept, the coordinate system included; `'Global'` sets it back. A distribution read from a file is refused - a replace never turns one form into the other. An equation that changes a scalar distribution into a vector one, or back, is refused while an initial condition, a boundary condition, a load or a defined field uses it. |
| `replace_from_file(old_name, name=None, file_name=None, interpolator_type=None, interpolator_radius=None, scale=None, translate=None, coordinate_system_name=None)` | Replaces a file distribution. A value that is not given is kept, and an equation distribution is refused. A new `file_name` is read again, and a file that changes a scalar distribution into a vector one, or back, is refused while an item uses the distribution, as for `replace_from_equation`. A file that cannot be read is refused naming `file_name`. |

`Distribution` exposes `name`, `item_type` (`'from_equation'` or `'from_file'`), `equation`, `file_name`,
`interpolator_type`, `interpolator_radius`, `scale`, `translate`, `coordinate_system_name` and `get_equations()`.

| Property | Description |
| --- | --- |
| `equation` | The equation of an equation distribution, in the shape `add_from_equation` takes it: a `str` for a scalar distribution, a `StringList` of three for a vector one, stored with the leading `=`. `None` for a file distribution. |
| `file_name` | The point cloud file of a file distribution, otherwise `None`. |
| `interpolator_type` | `ClosestPoint`, `Gauss` or `Shepard` for a file distribution, otherwise `None`. |
| `interpolator_radius` | The interpolator radius of a file distribution, in model units, otherwise `None`. A `ClosestPoint` distribution has no radius and reads back `None`, and `get_equations()` leaves it out. |
| `scale` | The scale factors of a file distribution as a `list[float]` of three, otherwise `None`. |
| `translate` | The translation of a file distribution in model units as a `list[float]` of three; `None` on an equation distribution. |
| `coordinate_system_name` | The name of the coordinate system the distribution is defined in, `'Global'` when none is set; the `coordinate_system_name` argument takes it back. |
| `get_equations()` | The expressions a file distribution was entered with, under the `add_from_file` argument names: `interpolator_radius`, and `scale` and `translate` as a list of three (`dict[str, str or list]`). Only a value entered as an expression is returned, with its leading `=`; a value entered as a plain number is left out, and the property of the same name returns it; a list with an expression in any component is returned whole, its plain components as numbers. An equation distribution returns an empty `dict`; its equation is the `equation` property. |

### `InitialConditions`

| Member | Description |
| --- | --- |
| `add_temperature(name, region, temperature, distribution_name=None, color=None)` | Initial temperature. The selection target is `geometry`, as the dialog picks it, or `nodes`. A distribution scales the temperature over the region; `'Constant'`, in any case, names none. |
| `add_translational_velocity(name, region, v1, v2, v3, color=None)` | Initial translational velocity. The selection target is `geometry`, as the dialog picks it, or `nodes`. At least one of `v1`, `v2` and `v3` must be nonzero, as the dialog refuses a zero velocity, and in a 2D model `v3` must be zero. |
| `add_angular_velocity(name, region, axis_point, axis_direction, rotational_speed, color=None)` | Initial angular velocity. The selection target is `geometry`, as the dialog picks it, or `nodes`. The `rotational_speed` is in rad/s and must be larger than 0, as the dialog refuses a zero speed. In a 2D model the third value of `axis_point` and the first two of `axis_direction` must be zero, and an `axis_direction` of zero length is refused. |
| `replace_temperature(old_name, name=None, region=None, temperature=None, distribution_name=None, color=None)` | Replaces an initial temperature. A value that is not given is kept. |
| `replace_translational_velocity(old_name, name=None, region=None, v1=None, v2=None, v3=None, color=None)` | Replaces an initial translational velocity. A given value is checked as on the add, and the velocity the replace ends with must not be zero. |
| `replace_angular_velocity(old_name, name=None, region=None, axis_point=None, axis_direction=None, rotational_speed=None, color=None)` | Replaces an initial angular velocity. The `rotational_speed` is in rad/s. A given value is checked as on the add, an equation by the value it evaluates to. |
| `hide(names)`, `show(names)` | Initial condition visibility. |

`InitialCondition` exposes `name`, `item_type` (`'temperature'`, `'translational_velocity'` or
`'angular_velocity'`), `region_name`, `region_type`, `active`, `visible`, `color` (the symbol color as a plain list
`[R, G, B, A]`, which the `color` argument every `add_*` and `replace_*` takes goes back into), `valid`,
`get_value_names()`, `get_value(name)` and
`get_equations()`, which returns the expressions the values were entered with, under the names `get_value` uses, as a
`dict[str, str or list]`. Only a value entered as an expression is returned, with its leading `=`; a plain number is
left out, and `get_value` returns it. A vector argument such as `axis_point` with an expression in any component is a
list, its plain components as numbers, so it can be passed back as the argument.
`get_value` takes the names of the `replace_*` arguments, and only those, so the values go back into the replace of
the type, and `axis_point` and `axis_direction` of an angular velocity read back as lists of three.

```python
pmx.model.amplitudes.add('Amp-1', [[0, 0], [1, 1]])
pmx.model.distributions.add_from_equation('D-1', 'x + y')
nodes = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3])
pmx.model.initial_conditions.add_temperature('IC-1', nodes, 20)
```

### `Materials`

| Member | Description |
| --- | --- |
| `add(name, properties, description=None, temperature_dependent=False)` | Adds a material. The properties are created by `pmx.create`. Set `temperature_dependent=True` to write every row with its temperature. A material holds one elastic property and one density, so `density` together with `elastic_with_density`, which holds the density, is refused. |
| `replace(old_name, name=None, properties=None, description=None, temperature_dependent=None)` | Replaces an existing material. Omitted values are kept. |

`Material` exposes `name`, `description`, `active`, `temperature_dependent`, `property_count`, `get_properties()` and
`get_property_types()`, which names every property after the `pmx.create` maker that makes it: `elastic`, `density`,
`elastic_with_density`, `elastic_engineering_constants`, `elastic_lamina`, `plastic`, `slip_wear`, `specific_heat`,
`thermal_conductivity` or `thermal_expansion`. `get_properties()` returns detached `MaterialProperty` copies as a plain Python list, and the
list can be passed straight to `materials.replace`:
`pmx.model.materials.replace('Steel', properties=pmx.model.materials['Steel'].get_properties())`.

Every `pmx.create` property maker (`elastic`, `density`, `elastic_with_density`, `elastic_engineering_constants`,
`elastic_lamina`, `plastic`, `slip_wear`, `specific_heat`, `thermal_conductivity`, `thermal_expansion`) returns a
`MaterialProperty`:

| Member | Description |
| --- | --- |
| `property_type` | The name of the `pmx.create` maker that makes the property, for example `'elastic'`. |
| `get_values()` | The values under the `pmx.create` argument names, so that `pmx.create.<property_type>(**p.get_values())` rebuilds the property. A single row at a plain temperature of 0 reads as scalars; otherwise the whole table is one `NumericArray` under the first argument, its last column being the temperature. A value entered as an expression reads back as that expression, with its leading `=`, and a table holding one is a plain list of rows, its other values numbers. |
| `get_value(name)` | One value, by the `pmx.create` argument name. |
| `get_value_names()` | The names `get_values()` reports, as a `StringList`. |

Every value a property maker takes may be an **equation string**, as the material dialog takes an expression:
`pmx.create.elastic('=E_steel', 0.3)` keeps `=E_steel` in the material, so a parameter change reaches it. The value
is evaluated when the property is made and checked as the dialog checks it, a refusal naming the argument.

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

A material may hold only one elastic property and one property of every other type, as the dialog adds each type
once; a second one is refused naming `properties`.

A material that is **not** temperature dependent writes no temperature and, of every property but `plastic`, only
the first row. Such a property with more than one row, or with a non-zero temperature, is therefore refused with a
message that names `temperature_dependent`, rather than written short. A replace checks the merged material when it
is given `properties` or `temperature_dependent`. A session journal writes a material that is not temperature
dependent with the rows the analysis uses.

### `Sections`

| Member | Description |
| --- | --- |
| `add_solid(name, material_name, region, thickness=None)` | Solid section. The selection target is `elements` or `parts`, as the dialog picks them. The thickness is written only in a 2D model, where it is 1 when not given, so it is refused in a 3D one. |
| `add_shell(name, material_name, region, thickness=1, offset=0)` | Shell section. The selection target is `elements` or `parts`. The offset of the mid surface from the selected geometry is given in shell thicknesses. |
| `add_membrane(name, material_name, region, thickness=1, offset=0)` | Membrane section. The selection target is `elements` or `parts`. The offset is given in membrane thicknesses. |
| `add_point_mass(name, region, mass)` | Point mass section. The selection target is `nodes`. |
| `add_distributed_mass(name, region, mass)` | Distributed mass section. The region is a selection or a named region; the selection target is `surfaces`. |
| `replace_solid(old_name, name=None, material_name=None, region=None, thickness=None)` | Replaces a solid section. A value that is not given is kept. |
| `replace_shell(old_name, name=None, material_name=None, region=None, thickness=None, offset=None)` | Replaces a shell section. |
| `replace_membrane(old_name, name=None, material_name=None, region=None, thickness=None, offset=None)` | Replaces a membrane section. |
| `replace_point_mass(old_name, name=None, region=None, mass=None)` | Replaces a point mass section. |
| `replace_distributed_mass(old_name, name=None, region=None, mass=None)` | Replaces a distributed mass section. The region is a surface region, as on `add_distributed_mass`. |

A `thickness` must be larger than 0 and a `mass` zero or larger, on an add and on a replace. The
`material_name` of a solid, shell or membrane section is required on an add, so `None` is refused there;
on a replace, `None` keeps the material.

As the dialog does, the region must suit the section type, judged on an add and on the section a replace ends
with; otherwise the call is refused with `The section type and the section region are not compatible.` and a hint
naming `region`. In a 3D model a solid section needs solid parts or solid elements, and a shell or a membrane
section shell parts or shell elements. In a 2D model a solid section needs the parts of the 2D mesh or their
elements, and a shell, a membrane or a point mass section is taken on a selection only, never on a named region.

A replacement is typed: replacing a solid section as a shell section is refused rather than silently
turning it into one, with `The section 'X' is made by add_solid, so replace_shell cannot replace it.`

`Section` exposes `name`, `item_type` (`'solid'`, `'shell'`, `'membrane'`, `'point_mass'` or `'distributed_mass'`), `material_name`, `region_name`, `region_type`, `thickness`
(of a shell or membrane section, or of a solid section in a 2D model, otherwise `None`, as the replace of the type takes it),
`offset` (of a shell or membrane section, otherwise `None`), `mass` (of a mass section, otherwise `None`), `valid` and
`get_equations()`, the expressions `thickness`, `offset` and `mass` were entered with, as a `dict[str, str]`; a value
entered as a plain number is left out, and the property of the same name returns it.

```python
elements = pmx.create.selection_by_ids('femodel', 'elements', 'elements', [1, 2, 3])
pmx.model.sections.add_solid('Sec-1', 'Steel', elements)
```

A section takes a selection or a named region made by `pmx.create.region`. The application converts a
selection into an internal element or node set named after the section, exactly as it does for a
section created in a dialog, so `region_type` reads back as `ElementSetName` rather than `Selection`.

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
| `get_node_ids()` | The node IDs as `IntList`. |
| `get_center_of_gravity()` | The center of gravity as `[x, y, z]`, or an empty list when the node set has none. |

An internal node set, element set or surface (`is_internal`) is the converted selection of the item that made it,
and the model tree does not show it, so `replace`, `rename` and `remove` refuse it; change the region of that item
instead.

### `ElementSets`

| Member | Description |
| --- | --- |
| `add(name, selection)` | Creates an element set from a selection. The selection target is `elements`. |
| `replace(old_name, name=None, selection=None)` | Replaces an existing element set. A value that is not given is kept. |
| `add_from_quality(name, metric, part_names, larger_than, limit)` | Adds an element set of the elements of the named model parts whose quality metric is larger, or with `larger_than=False` not larger, than the limit, and returns the `ElementSet`. `metric` is one of the gmsh quality measures `minDetJac`, `maxDetJac`, `minSJ`, `minSICN`, `minSIGE`, `gamma`, `innerRadius`, `outerRadius`, `minIsotropy`, `angleShape`, `minEdge`, `maxEdge` or `volume`, in any case; a name another mesh entity or a part uses is refused, because a part is written to the solver input as an element set of its name, and the dialog numbers its set past those names for the same reason. A part named twice in `part_names` is refused. It has no `replace_*`; replace the set with `replace`. |
| `convert_to_mesh_parts(names)` | Converts the named element sets into mesh parts. |

`ElementSet` exposes `name`, `count`, `valid`, `is_internal` and `get_element_ids()` (an `IntList`).

### `Surfaces`

| Member | Description |
| --- | --- |
| `add(name, selection, surface_type=None)` | Creates a surface from a selection. The selection target is `surfaces`. |
| `add_from_node_set(name, node_set_name, surface_type=None)` | Creates a surface from an existing node set. |
| `replace(old_name, name=None, selection=None, surface_type=None)` | Replaces an existing surface. A value that is not given is kept, the `surface_type` included. |
| `replace_from_node_set(old_name, name=None, node_set_name=None, surface_type=None)` | Replaces a surface by another node set. A node set that is not given is the one the surface already uses, and the `surface_type` is kept when not given. |

`surface_type` is `Element`, a surface of element faces and the default, or `Node`, a surface of the nodes on those
faces, as the dialog offers them. A load acts on an `Element` surface only: the load dialog lists no other, so a
load whose region names a `Node` surface is refused naming the argument. The other items take either type.

`Surface` exposes `name`, `created_from` (`Selection`, `NodeSet` or `Faces`), `surface_type`
(`Element` or `Node`, which the `surface_type` argument takes back), `area`, `node_set_name`, `valid`, `is_internal`
and `get_face_ids()` (an `IntList`).

### `ReferencePoints`

| Member | Description |
| --- | --- |
| `add(name, coordinates, name_visible=None, color=None)` | Creates a reference point at `[x, y, z]`. In a 2D model the point is a 2D point, as the dialog makes it, and a non-zero `z` is refused. |
| `add_from_selection(name, created_from, selection, name_visible=None, color=None)` | Creates a reference point whose coordinates the mesh computes from selected points. |
| `add_from_region(name, created_from, region_type, region_name, name_visible=None, color=None)` | Creates a reference point whose coordinates the mesh computes from a named region. |
| `replace(old_name, name=None, coordinates=None, name_visible=None, color=None)` | Replaces an existing reference point. Coordinates that are not given are kept, and giving them turns the point back into a coordinate point. |
| `replace_from_selection(old_name, name=None, created_from=None, selection=None, name_visible=None, color=None)` | Replaces a reference point by selected mesh points. |
| `replace_from_region(old_name, name=None, created_from=None, region_type=None, region_name=None, name_visible=None, color=None)` | Replaces a reference point by a named region. |
| `hide(names)`, `show(names)` | Reference point visibility. |

`created_from` decides which of the two add methods applies. The three point modes `OnPoint`,
`BetweenTwoPoints` and `CircleCenter` take a selection with exactly as many points as the mode
consumes - one, two or three; `CenterOfGravity` and `BoundingBoxCenter` take a named region instead, whose
type is `NodeSetName` or `SurfaceName`.

`name_visible` decides whether the point name is drawn next to its symbol, and `color` takes
`[red, green, blue]`, `[red, green, blue, alpha]` or a color name. Both are part of the stored definition and keep
their defaults when they are not given.

`ReferencePoint` exposes `name`, `created_from`, `name_visible`, `color`, `visible`, `valid`,
`get_coordinates()` (`[x, y, z]`), `get_equations()` (a `dict[str, list]` with the coordinates under
`coordinates`, the argument of `add` and `replace`, when any of them was entered as an expression; its plain
coordinates are numbers, and a point without an expression returns an empty `dict`) and `get_construction()` (a
`dict[str, str]`), which returns how the point is defined under `created_from`, `region_type` and `region_name`, the
arguments of `add_from_region`. `region_type` and `region_name` are `None` unless the point is built from a region
(`CenterOfGravity` or `BoundingBoxCenter`); the selected points are not returned.

`replace_from_selection` and `replace_from_region` keep the stored selection or region only while the point is still of
that kind. A replace that switches the kind drops the data of the old kind, so a later replace cannot bring it back.

### `CoordinateSystems`

| Member | Description |
| --- | --- |
| `add(name, origin, point_on_x, point_in_xy, coordinate_system_type=None, name_visible=None)` | Creates a coordinate system from three points. The type is `Rectangular` or `Cylindrical`. In a 2D model a point with a non-zero `z` is refused, since the coordinate system stores every `z` as zero. |
| `add_from_selections(name, origin_created_from=None, origin_selection=None, ..., origin=None, point_on_x=None, point_in_xy=None, coordinate_system_type=None, name_visible=None)` | Creates a coordinate system whose points the mesh computes from selections. Each of the three points takes either a mode and its selection together, or its coordinates. |
| `replace(old_name, name=None, origin=None, point_on_x=None, point_in_xy=None, coordinate_system_type=None, name_visible=None)` | Replaces a coordinate system. A value that is not given is kept. |
| `replace_from_selections(old_name, name=None, ...)` | Replaces a coordinate system whose points come from selections or coordinates. |
| `hide(names)`, `show(names)` | Coordinate system visibility. |

A point given as coordinates and a point given as a selection can be mixed in one coordinate system,
which is what the dialog offers when one point is picked and another typed.

`CoordinateSystem` exposes `name`, `coordinate_system_type`, `name_visible`, `visible`, `valid`, `get_origin()`,
`get_point_on_x()`, `get_point_in_xy()` (each `[x, y, z]`), `get_equations()` (a `dict[str, list]` keyed by the
arguments `origin`, `point_on_x` and `point_in_xy`; a point is returned as a list of three when any of its coordinates
was entered as an expression, its plain coordinates as numbers, and a point without an expression is left out) and
`get_construction()` (a `dict[str, str]`), which tells how each of the three points is defined under
`origin_created_from`, `point_on_x_created_from` and `point_in_xy_created_from`, the arguments of
`add_from_selections`: `OnPoint`, `BetweenTwoPoints` or `CircleCenter` for a point computed from a selection, and
`None` for a point given by its coordinates.

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
| `get_model_keywords()` | The generated keywords, as a plain Python list, each with the index path that addresses it and its complete text. |
| `get_user_keywords()` | The user keywords, as a plain Python list, each with the index path it is inserted at. |
| `get_overrides()` | The user edits of the generated keywords, as a plain Python list, each with its `indices` (an `IntList`), `data`, `disabled`, `suppressed` and `original_signature`. |
| `set_user_keyword(indices, data)` | Inserts or replaces one user keyword. The index path names an insertion position, so its last index may be the number of keywords it is inserted among. |
| `remove_user_keyword(indices)` | Removes one user keyword. |
| `replace_user_keywords(keywords)` | Replaces every user keyword by index path and text pairs. |
| `set_override(indices, data=None, disabled=None)` | Edits one generated keyword. The text replaces it and `disabled` writes it as a comment. A value that is not given is kept. |
| `remove_override(indices)`, `clear_overrides()` | Removes the user edits. `clear_overrides()` is refused when there are no overrides. |

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

Reached as `pmx.model.tree`. It groups and orders the items of the model tree. `view` is `'geometry'`,
`'femodel'` or `'results'` (in any case), and `item_type` names the collection being organized, also in any case; a
step owned collection finds its `step_name` regardless of case too. `get_item_types(view)`
returns the collections a view supports:

| View | `item_type` |
| --- | --- |
| `geometry` | `parts`, `mesh_setup_items` |
| `femodel` | `parts`, `node_sets`, `element_sets`, `surfaces`, `reference_points`, `coordinate_systems`, `materials`, `sections`, `constraints`, `surface_interactions`, `contact_pairs`, `distributions`, `amplitudes`, `initial_conditions`, `jobs`, and the step owned `history_outputs`, `field_outputs`, `boundary_conditions`, `loads`, `defined_fields` |
| `results` | `parts`, `node_sets`, `element_sets`, `surfaces`, `reference_points`, `coordinate_systems` |

A step owned collection also takes `step_name`, the step that owns it; every other collection refuses
that argument. The steps themselves are not a groupable collection. A call is refused, before anything is
recorded, for an unknown view, group or item, an unknown step, an index out of range, and a group id or item
name that already exists.

| Member | Description |
| --- | --- |
| `get_item_types(view)` | The collections that can be organized in a view. |
| `add_group(view, item_type, group_name, group_id=None, parent_group_id=None, target_index=0, item_names=None, step_name=None)` | Adds a group and optionally moves items into it. Returns the group id. The empty id `00000000-0000-0000-0000-000000000000` is refused, here and wherever a group id is taken. It has no `replace_group`: `rename_group` and `move_group` change a group. |
| `rename_group(view, item_type, group_id, group_name, step_name=None)` | Renames a group. |
| `remove_groups(view, item_type, group_ids, step_name=None)` | Removes groups. The items they hold stay in the tree. |
| `move_items(view, item_type, item_names, target_group_id=None, target_index=0, step_name=None)` | Moves items into a group, or out of every group. Item names here and in `add_group` are found regardless of case and passed on in the model's spelling. |
| `move_group(view, item_type, group_id, target_group_id=None, target_index=0, step_name=None)` | Moves a group. |
| `get_layout(view, item_type, step_name=None)` | The whole layout of the collection, in the form `set_layout` takes. |
| `set_layout(view, item_type, layout, step_name=None)` | Sets the whole layout of the collection at once. |

`add_group` and `rename_group` validate every group name like a model item name and refuse a bad one with an
error naming `group_name`. A group of the geometry or mesh `parts` also shares the namespace of the parts, so it
cannot take the name of a part or of another part group.

A group is identified by a GUID that the application stores with the layout. `add_group` generates
one when it is not given and returns it, so a script does not have to invent an id while a journal
can pass the recorded one.

A layout, as `get_layout` returns it and `set_layout` takes it, is a list in tree order whose entries are
item names and groups; a group is a dict with the keys `'group_name'`, `'group_id'` and `'items'`, the
last one a list of the same form. `set_layout` must name every item of the collection exactly once,
checks the group names as `add_group` does, and gives a group without a `'group_id'` a new one. The names are
checked against the names every group carries once the layout is set, so two groups may swap their names in one
call. A part group of the geometry and the mesh - one group shown in both views, with one id - that the layout
renames is renamed in the other view as well, as `rename_group` renames it, which the journal writes as a second
`set_layout`. A group whose new name is still the name of the compound part of another group is refused: rename that
group first. A list of
item names alone removes every group and sets the order of the items. Setting the layout the collection
already has records nothing, so the call is idempotent; the journal writes every tree layout change of
the application as one `set_layout` call with the resulting layout. The expanded or collapsed state of a
group is a view state, not part of the layout.

```python
group = pmx.model.tree.add_group('femodel', 'parts', 'Housing', item_names=['Solid-1'])
pmx.model.tree.move_items('femodel', 'parts', 'Solid-2', target_group_id=group)
layout = pmx.model.tree.get_layout('femodel', 'parts')
pmx.model.tree.set_layout('femodel', 'parts', ['Solid-2', 'Solid-1'])   # removes the group
```

## Interactions

### `Constraints`

Reached as `pmx.model.constraints`. Every region argument is a `Selection` or a `Region`. Every
`add_*` has a matching `replace_*` taking `old_name` first, then an optional `name`, then the
arguments of its `add_*`, all optional. Each takes the colors the dialog shows for its type: a tie, a cyclic
symmetry and a joint take `master_color` and `slave_color`, the colors of their two surfaces; every other
constraint takes one `color`. Each is a `color` value as described at the top.

| Member | Selection target | Description |
| --- | --- | --- |
| `add_tie(name, master_region, slave_region, position_tolerance=None, adjust=None, master_color=None, slave_color=None)` | surfaces, surfaces | A tie. `position_tolerance` must be zero or larger, or `float('nan')` for `Default`. |
| `add_cyclic_symmetry(name, master_region, slave_region, region=None, axis_point=None, axis_direction=None, number_of_sectors=None, number_of_graph_sectors=None, check_sector_angle=None, position_tolerance=None, adjust=None, master_color=None, slave_color=None)` | surfaces, surfaces, parts | A cyclic symmetry, refused in a 2D model. The result expansion `region` is picked as parts, as the dialog picks them, and named as an `ElementSetName` or a `PartName` region. `number_of_sectors` is at least 2. |
| `add_joint(name, master_region, slave_region, joint_type=None, center_defined_by=None, center=None, axis_defined_by=None, axis_direction=None, formulation=None, stiffness_defined_by=None, stiffness=None, master_color=None, slave_color=None)` | surfaces, surfaces | A joint between two surfaces: Hinge, Cylindrical, Slider, Ball or Weld, written as rigid bodies or as springs. Refused in a 2D model. A `center`, `axis_direction` or `stiffness` given together with a definition that does not use it - `center_defined_by='MasterSurface'`, `axis_defined_by='MasterSurface'`, `stiffness_defined_by='FromModel'` - is refused naming both. A Ball or a Weld joint has no axis, which the dialog then hides, so `axis_defined_by` and `axis_direction` are refused for it and not read back, and an `axis_direction` of zero length is refused. |
| `add_rigid_body(name, reference_point_name, region, color=None)` | geometry or nodes | A rigid body. The dialog picks geometry. |
| `add_kinematic_coupling(name, reference_point_name, region, u1=None, u2=None, u3=None, color=None)` | surfaces | A kinematic coupling. At least one of `u1`, `u2` and `u3` must stay `True`. In a 2D model `u3=True` is refused. |
| `add_distributing_coupling(name, reference_point_name, region, u1=None, ..., ur3=None, color=None)` | surfaces | A distributing coupling. At least one of `u1` to `ur3` must stay `True`. In a 2D model `u3`, `ur1` and `ur2` set to `True` are refused. |
| `add_compression_only(name, region, clearance=None, spring_stiffness=None, tensile_force_at_negative_infinity=None, offset=None, non_linear=None, color=None)` | surfaces | A compression only constraint. `spring_stiffness` and `tensile_force_at_negative_infinity` take `float('nan')` for `Default`. |
| `add_point_spring(name, region, k1=None, k2=None, k3=None, color=None)` | nodes | A point spring. A stiffness must be zero or larger, at least one larger than 0, and `k3` zero in a 2D model. |
| `add_surface_spring(name, region, k1=None, k2=None, k3=None, stiffness_per_area=None, color=None)` | surfaces | A surface spring. A stiffness must be zero or larger, at least one larger than 0, and `k3` zero in a 2D model. |
| `swap_master_slave(names)` | | Swaps the two sides. |
| `merge_by_master_slave(names)` | | Merges the constraints that share a master and a slave region. |
| `hide(names)`, `show(names)` | | Symbol visibility. |

`Constraint` exposes `name`, `item_type` (the `add_*` name without `add_`, `'tie'` or `'point_spring'`), `active`, `visible`, `master_region_type`,
`master_region_name`, `slave_region_type`, `slave_region_name`, the colors of its type as plain lists `[R, G, B, A]` -
`master_color` and `slave_color` of a tie, a cyclic symmetry and a joint, `color` of every other type, the other ones
refused naming those the type has - `region_type` and `region_name`, the region the `region` argument sets (refused
for a tie and a joint, `None` for a cyclic symmetry without a result expansion region), `sector_angle_deg` (`None`
but for a cyclic symmetry), and the values of its own type through `get_value_names()` and `get_value(name)`, named
like the `replace_*` arguments and nothing else, so `replace_<type>(name, **{k: c.get_value(k) for k in
c.get_value_names()})` changes nothing. A joint reads back `center` only when `center_defined_by` is `Coordinates`,
`axis_defined_by` only on a joint with an axis and `axis_direction` only when it is `UserDefined` there, `stiffness_defined_by` only
with the `Springs` formulation and `stiffness` only when it is `UserDefined`, and no `center_defined_by` for a centre
picked in the dialog, because the arguments refuse those. `get_equations()` returns the expressions the values were entered
with, under the names `get_value` uses, as a `dict[str, str or list]`. Only a value entered as an expression is
returned, with its leading `=`; a plain number is left out, and `get_value` returns it. A vector argument such as
`axis_point` with an expression in any component is a list, its plain components as numbers.

A value the dialog shows as **`Default`** - the `position_tolerance` of a tie and a cyclic symmetry, the
`spring_stiffness` and the `tensile_force_at_negative_infinity` of a compression only constraint - is left
at `Default` when it is not given and reads back as `None` from `get_value`. Because `None` keeps the value
on a replace, `float('nan')` sets it back to `Default`, as it clears a frequency limit of a step.

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
region**, required when `number_of_graph_sectors` is larger than 1 and refused otherwise, because the
application would discard it. The rule is checked on the final state, so a replace sees the values it keeps.
`replace_cyclic_symmetry` takes `clear_expansion_region=None`; `True` drops the region, which a replace that lowers
`number_of_graph_sectors` to 1 needs, and it cannot be combined with `region`. `sector_angle_deg` is derived as
`360 / number_of_sectors` and is the read-only property of that name, while `axis_point` and `axis_direction`, read
through `get_value`, read back as
lists of three. `number_of_graph_sectors` lies between 1 and `number_of_sectors`, checked on the merged values: a
replace that lowers `number_of_sectors` below the kept `number_of_graph_sectors` is refused rather than letting the
model lower the graph sectors on its own.

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
switches it to `UserDefined` and must be positive. `stiffness` and `stiffness_defined_by` are refused with the
`RigidBody` formulation, which has no springs, and `axis_defined_by` and `axis_direction` with a `Ball` or `Weld`
joint, which has no axis - the joint type it ends up with decides. An `axis_direction` of zero length is refused, as
the dialog refuses it. The values read back through `get_value` carry the names of the arguments: `joint_type`,
`center_defined_by`, `center` (a list of three), `axis_defined_by`, `axis_direction` (a list of three),
`formulation`, `stiffness_defined_by` and `stiffness`.

### `SurfaceInteractions`

Reached as `pmx.model.surface_interactions`. A surface interaction is composed of properties made by
`pmx.create`, the same way a material is composed of material properties.

| Member | Description |
| --- | --- |
| `add(name, properties)` | Adds a surface interaction. One property, or a list of them, each type at most once; a repeated type is refused naming `properties`. As the dialog does, the properties must hold a `surface_behavior` - otherwise `Surface interaction must define a surface behavior.` - and a `Tied` surface behavior also a `friction`, which gives the stick slope. |
| `replace(old_name, name=None, properties=None)` | Replaces a surface interaction. A value that is not given is kept; `properties`, when given, replaces the whole property list and is refused as on `add`. |

`SurfaceInteraction` exposes `name`, `property_count`, `get_properties()` and `get_property_types()`, which names every
property after the `pmx.create` maker that makes it: `friction`, `gap_conductance` or `surface_behavior`.
`get_properties()` returns detached `SurfaceInteractionProperty` copies as a plain Python list, and the list can be passed straight to
`surface_interactions.replace`:
`pmx.model.surface_interactions.replace('SI-1', properties=pmx.model.surface_interactions['SI-1'].get_properties())`.

The three properties are made by `pmx.create`:

| Member | Description |
| --- | --- |
| `friction(coefficient, stick_slope=None)` | Friction. Both values must be larger than 0. |
| `gap_conductance(conductance=None, conductance_table=None)` | Gap conductance. Give one or the other: a finite number for a constant one, or rows of `[conductance, pressure, temperature]` for a tabular one. |
| `surface_behavior(pressure_overclosure_type=None, c0=None, p0=None, k=None, s_inf=None, pressure_overclosure=None)` | Surface behavior. The type is `Hard`, `Linear`, `Exponential`, `Tabular` or `Tied`. Each type takes only the values it writes - `Hard` none, `Linear` `k`, `s_inf` and `c0`, `Exponential` `c0` and `p0`, `Tabular` `pressure_overclosure`, `Tied` `k` - and refuses the others; `c0`, `p0`, `k` and `s_inf` must be larger than 0. |

Each maker returns a `SurfaceInteractionProperty`:

| Member | Description |
| --- | --- |
| `property_type` | The name of the `pmx.create` maker that makes the property: `'friction'`, `'gap_conductance'` or `'surface_behavior'`. |
| `get_values()` | The values under the `pmx.create` argument names, so that `pmx.create.<property_type>(**p.get_values())` rebuilds the property. A value left at its default (NaN) reads as `None`. A constant gap conductance reads as `conductance`, a tabular one as `conductance_table`. A surface behavior reports only the values its type uses. |
| `get_value(name)` | One value, by the `pmx.create` argument name. |
| `get_value_names()` | The names `get_values()` reports, as a `StringList`. |

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
| `add(name, surface_interaction_name, master_region, slave_region, method=None, small_sliding=None, adjust=None, adjustment_size=None, master_color=None, slave_color=None)` | Adds a contact pair. `method` is `NodeToSurface`, `SurfaceToSurface`, `Mortar` or `Massless`. |
| `replace(old_name, name=None, ...)` | Replaces a contact pair. A value that is not given keeps what the contact pair carries. A `method` other than `NodeToSurface` on a pair with small sliding is refused unless `small_sliding=False` is given too, because no other method supports it. |
| `swap_master_slave(names)` | Swaps the two sides. |
| `merge_by_master_slave(names)` | Merges the contact pairs that share a master and a slave surface. |
| `hide(names)`, `show(names)` | Symbol visibility. |

`ContactPair` exposes `name`, `surface_interaction_name`, `method`, `small_sliding`, `adjust`,
`adjustment_size`, `active`, `visible`, `master_color` and `slave_color` (the colors of the two surfaces as plain
lists `[R, G, B, A]`, which the arguments of the same name take back), and `master_region_type`,
`master_region_name`, `slave_region_type` and `slave_region_name`.

```python
pmx.model.contact_pairs.add('CP-1', 'SI-1', master_selection, slave_selection)
```

Contact pairs may mix methods; the analysis warns before a run. **Small sliding is written for
`NodeToSurface` only** - the model clears it for `SurfaceToSurface` and leaves it unwritten for `Mortar`
and `Massless`, so the API refuses `small_sliding=True` with any other method instead of letting the
request be dropped, and `small_sliding` reads back `False` for them. An `adjustment_size` turns the
adjustment on, the way the dialog presents the two together; it must be finite and zero or larger, or `float('nan')` for
the `Default` size the dialog offers, is refused together with `adjust=False`, and reads back as `None` when it is
`Default`. Because `None` keeps the size on a replace, `float('nan')` is how a replace sets it back to `Default`.
The dialog hides the size while `adjust` is `False`, so it then reads back as `None` too, and the stored size comes
back when the adjustment is turned on again. A replace that does not change the method keeps the small sliding the
pair stores, even where the method does not write it; only a change of method clears it.

## Steps and step content

The names in this section belong to the **model** tree under `pmx.model.steps`. The results side has
collections of the same name under `pmx.results`, documented further below.

Two rules hold everywhere in this section:

- **Every value is an optional named argument, and a value that is not given is not written.** A new
  item therefore keeps the application default, and `replace_*` keeps whatever the item already
  carries. `replace_static('Step-1', nlgeom=False)` changes one value and leaves the rest alone.
- **The type cannot change on a replace**, because `replace_*` clones the existing item, the same
  restriction the dialog has, and the refusal names both methods: `... is made by add_static, so replace_frequency cannot replace it`. Changing the type is a `remove` followed by an `add`. A `replace_<type>` edits
  only that exact type, even where one type derives from another: `replace_static` refuses a heat transfer,
  dynamic, modal dynamic, slip wear or temperature displacement step, `replace_heat_transfer` refuses a coupled or
  uncoupled temperature displacement step, and `replace_tie` refuses a cyclic symmetry. This holds for steps,
  constraints and step items.
- **A value the model would change is refused instead.** A value outside the range a setter clamps to, an
  out of plane value of a two dimensional model, a `phase_deg` outside a steady state dynamics step, or a
  combination the item would correct is refused before a command is recorded, naming the argument. A replace
  checks the values it is given together with the ones the item keeps.

A step name, and the default names `Default` (amplitude), `Global` (coordinate system) and `Constant`
(distribution), are matched without regard to case, and the name the model uses is what is stored and read
back: `pmx.model.steps['step-1']` reaches `Step-1`.

Every `add_*` listed below has a matching `replace_*` - `add_static` and `replace_static`,
`add_pressure` and `replace_pressure`, and so on. A `replace_*` takes `old_name` first, then an
optional `name` that renames the item in the same call, and then the arguments of its `add_*`, all
of them optional. Only the `replace_*` methods that differ from that pattern are listed separately.

### `Steps` (model)

Reached as `pmx.model.steps`. It shares the collection verbs, including `activate` and `deactivate`.

| Member | Description |
| --- | --- |
| `add_static(name, time_period=None, initial_time_increment=None, min_time_increment=None, max_time_increment=None, ...)` | A static step. A new one has the incrementation type `Default`, which uses no time values: `time_period`, `initial_time_increment` and `max_increments` take `incrementation_type` `Automatic` or `Direct`, and `min_time_increment` and `max_time_increment` take `Automatic`, as the dialog shows them. A value the incrementation type the call ends with does not use is refused naming it, and `get_value` and `max_increments` read it as `None`. It uses every other shared argument. The time period and the increments are positive; neither the initial increment nor, under `Automatic`, the minimum one may exceed the time period, and a shorter period shortens a minimum increment the type does not use, as the dialog does. An initial increment above the maximum one is kept as given, as the dialog does. |
| `add_slip_wear(name, ...)` | A slip wear step. It takes the arguments of `add_static`. |
| `add_boundary_displacement(name, ...)` | A boundary displacement step, which follows a slip wear step. It is not written to the solver input, which takes only its boundary conditions, so it uses none of the shared arguments: `max_increments` is refused and reads `None`, and every other one is accepted only as the value the step carries. |
| `add_frequency(name, number_of_frequencies=None, lower_frequency=None, upper_frequency=None, storage=None, cyclic_symmetry_mode_min=None, cyclic_symmetry_mode_max=None, ...)` | A frequency step. `number_of_frequencies` is at least 1, the limits are not negative and the lower one is below the upper one, unless the upper one is 0, which means no upper limit. `float('nan')` leaves a limit unset, which is how a replace clears one, and `get_value` reads an unset limit as `None`. The modes are not negative with the minimum not above the maximum; `float('nan')` leaves a mode unset, and `get_value` reads an unset mode as `None`. With an active cyclic symmetry the modes are checked against its number of sectors, as the dialog does. Of the shared arguments it uses `nlgeom`, `solver_type`, `output_frequency`, `perturbation` and `run_analysis`: `max_increments` is refused and reads `None`, and `incrementation_type` is accepted only as `Default`, as the dialog keeps it. |
| `add_complex_frequency(name, number_of_frequencies=None, ...)` | A complex frequency step. `number_of_frequencies` is at least 1. Of the shared arguments it uses `nlgeom`, `output_frequency`, `perturbation` and `run_analysis`: `max_increments` is refused and reads `None`, `incrementation_type` is accepted only as `Default`, and `solver_type`, which the solver input of this step does not carry, only as the value the step carries. |
| `add_buckle(name, number_of_buckling_factors=None, accuracy=None, ...)` | A buckle step. `number_of_buckling_factors` is at least 1 and `accuracy` finite and positive. Of the shared arguments it uses `nlgeom`, `solver_type`, `output_frequency`, `perturbation` and `run_analysis`: `max_increments` is refused and reads `None`, and `incrementation_type` is accepted only as `Default`. |
| `add_modal_dynamics(name, steady_state=None, relative_error=None, modal_damping=None, ...)` | A modal dynamics step. `relative_error` is between 0 and 1. It also takes the arguments of `add_static`, but uses its initial increment always, `time_period` unless it is a steady state step, `relative_error` only then, and no `min_time_increment` or `max_time_increment`; a value it does not use is refused, and `get_value` reads it as `None`. A steady state step raises the time period it does not use to the initial increment, so the initial increment is kept as given. The step always has the incrementation type `Automatic`, as the dialog keeps it, so `incrementation_type` is accepted only as `Automatic` and `max_increments` is always used; it uses every other shared argument. |
| `add_steady_state_dynamics(name, harmonic=None, lower_frequency=None, upper_frequency=None, number_of_data_points=None, bias=None, number_of_fourier_terms=None, time_lower=None, time_upper=None, modal_damping=None, ...)` | A steady state dynamics step. The frequencies and the times are finite, the lower frequency does not exceed the upper one and `time_lower` not `time_upper`, also on a replace with the value the step keeps, `number_of_data_points` is at least 2, `bias` at least 1 and `number_of_fourier_terms` at least 1. `number_of_fourier_terms`, `time_lower` and `time_upper` are used only for nonharmonic loading, `harmonic=False`, as the dialog shows them; a harmonic step refuses them and `get_value` reads them as `None`. Of the shared arguments it uses `nlgeom`, `solver_type`, `output_frequency`, `perturbation` and `run_analysis`: `max_increments` is refused and reads `None`, and `incrementation_type` is accepted only as `Default`. |
| `add_dynamic(name, alpha=None, solution_procedure=None, relative_to_absolute=None, damping=None, ...)` | A dynamic step. `alpha` is between -1/3 and 0. It also takes the arguments of `add_static`, and uses them and the shared ones as `add_static` does. |
| `add_heat_transfer(name, steady_state=None, deltmx=None, ...)` | A heat transfer step. `deltmx` limits the temperature change in an increment and must be positive; `float('inf')`, the default, does not limit it. It also takes the arguments of `add_static` and uses them as `add_static` does, and every shared argument but `nlgeom`, which its dialog does not show and which is accepted only as False or the value the step carries. |
| `add_uncoupled_temp_disp(name, ...)` | An uncoupled temperature displacement step. It takes the arguments of `add_heat_transfer`, and uses `nlgeom` as well. |
| `add_coupled_temp_disp(name, ...)` | A coupled temperature displacement step. It takes the arguments of `add_heat_transfer`, and uses `nlgeom` as well. |
| `move(names, target_index)` | Moves the named steps. The index is **zero based** and is evaluated after the moved steps have been removed. A step named twice is refused. |

Every `add_*` has a matching `replace_*` that takes `old_name` first and `name` second, and every
`add_*` ends with the arguments shared by all step types:

| Shared argument | Description |
| --- | --- |
| `nlgeom` | Accounts for the geometric nonlinearity. A heat transfer and a boundary displacement step do not use it. |
| `max_increments` | The maximum number of increments, at least 1. A step of the `add_static` arguments uses it under the incrementation type `Automatic` or `Direct` only, and refuses it under `Default`; a frequency, complex frequency, buckle, steady state dynamics or boundary displacement step never uses it. |
| `incrementation_type` | `Default`, `Automatic` or `Direct`. Only a step of the `add_static` arguments other than a modal dynamics step uses it; the other types accept only the type their dialog keeps, `Automatic` for a modal dynamics step and `Default` for the rest, or the value the step carries. |
| `solver_type` | `Default`, `PaStiX`, `Pardiso`, `Spooles`, `IterativeScaling` or `IterativeCholesky`. A new step starts from the solver type in the application settings. A complex frequency step does not use it. |
| `output_frequency` | The results output frequency, at least 0, or `float('nan')` to leave it unset, as a new step has it; `None` keeps it on a replace, and the step property reads an unset one as `None`. |
| `perturbation` | Makes the step a perturbation step. The model rejects it for a step type that does not support one. |
| `run_analysis` | False solves nothing and only checks the model. |
| `copy_bcs_and_loads` | `add_*` only. Copies the boundary conditions, loads and defined fields of the last step into the new one. An item of a type the new step does not support is left out, as the dialog does. |

`replace_frequency` and `replace_steady_state_dynamics` accept `lower_frequency` and `upper_frequency` together, so both limits
can move past each other in one call. A single limit is checked against the other limit the step keeps, a time
period against the increments a static step keeps, and a cyclic symmetry mode against the other mode.

```python
step = pmx.model.steps.add_static('Step-1', time_period=1.0, incrementation_type='Automatic', nlgeom=True)
pmx.model.steps.replace_static('Step-1', max_time_increment=0.1)
```

A new step already carries the default field outputs the application would create: a nodal and an
element one for most types, so `len(step.field_outputs)` is 2 before anything is added; a slip wear step
also has a contact one, 3 in all, and a boundary displacement step has none.

### `Step` (model)

| Member | Description |
| --- | --- |
| `name`, `item_type` | The step name and the type, named after the `add_*` method that makes it without `add_`: `static`, `slip_wear`, `boundary_displacement`, `frequency`, `complex_frequency`, `buckle`, `modal_dynamics`, `steady_state_dynamics`, `dynamic`, `heat_transfer`, `uncoupled_temp_disp` or `coupled_temp_disp`. |
| `active` | True when the step is written to the solver input file. |
| `nlgeom`, `perturbation`, `run_analysis` | The shared solution flags. |
| `max_increments`, `incrementation_type`, `solver_type`, `output_frequency` | The shared solution values. `max_increments` is `None` where the solver input does not carry it: for a step of the `add_static` arguments whose incrementation type is `Default`, and for a frequency, complex frequency, buckle, steady state dynamics or boundary displacement step. |
| `bcs` | The boundary conditions of the step. |
| `loads` | The loads of the step. |
| `field_outputs` | The field outputs of the step. |
| `history_outputs` | The history outputs of the step. |
| `defined_fields` | The defined fields of the step. |
| `get_value_names()` | The names of the values specific to this step type, the `replace_*` arguments that set them and nothing else. |
| `get_value(name)` | One value specific to this step type, for example `time_period` or `number_of_frequencies`; an unset frequency limit or cyclic symmetry mode is `None`, and so is a value the step does not use: a time value or `relative_error`, and the Fourier terms and time range of a harmonic steady state dynamics step. |
| `get_control_types()` | The types of the step control parameters, named after the `pmx.create` maker that makes each: `time_incrementation_control`, `field_control`, `contact_control` or `reset_control`. |
| `get_controls()` | Detached copies of the step control parameters, the `StepControlParameter` parts `pmx.create` makes, as a plain list that goes back into `set_controls`. |
| `set_controls(parameters)` | Replaces the step control parameters. They are made by `pmx.create`. |
| `clear_controls()` | Removes the step control parameters. |

The values shared by every step type are properties; the values of the concrete type are read by
name, because they differ per type. `get_value_names()` lists what a given step offers, and the values go back
into the replace of the step type, so `replace_<type>(name, **{k: step.get_value(k) for k in step.get_value_names()})`
changes nothing. `step.set_controls(step.get_controls())` likewise keeps the controls.

```python
print(list(step.get_value_names()))
print(step.get_value('time_period'))
step.set_controls([pmx.create.time_incrementation_control(i0=8, ic=32)])
```

A step control set is replaced as a whole, because the application has a single command for it.
`get_value('damping')` and `get_value('modal_damping')` return a copy of the part `pmx.create.damping`
or `pmx.create.modal_damping` makes, so it goes back into the `damping` or `modal_damping` argument of
the step's add or replace. It is a `Damping` or `ModalDamping` detached part (see `DetachedPart` below),
whose `get_values()` reads its values under the maker's argument names, so
`pmx.create.damping(**step.get_value('damping').get_values())` makes the same part. Nothing read from it
changes the model.

### `BoundaryConditions`

Reached as `step.bcs`. It shares the collection verbs. Every `region` argument is a `Selection` or a
`Region`, and the selection target is `geometry`, as the dialogs pick it, or `nodes` for all four types.

| Member | Description |
| --- | --- |
| `add_fixed(name, region, color=None)` | A fixed boundary condition. |
| `add_displacement_rotation(name, region, u1=None, u2=None, u3=None, ur1=None, ur2=None, ur3=None, distribution_name=None, amplitude_name=None, coordinate_system_name=None, phase_deg=None, color=None, fixed_dofs=None)` | A displacement and rotation boundary condition. A distribution scales its prescribed values over the region. A two dimensional model refuses a value for `u3`, `ur1` or `ur2`, and `phase_deg` is refused outside a steady state dynamics step; it is at least 0 and below 360 degrees. `fixed_dofs` names the degrees of freedom the dialog shows as `Fixed`. |
| `add_submodel(name, region, step_id=None, u1=None, u2=None, u3=None, ur1=None, ur2=None, ur3=None, color=None)` | A submodel boundary condition. Its degrees of freedom are booleans, and `step_id` is at least 1. A two dimensional model has no `u3`, `ur1` and `ur2`, so `True` for any of them is refused there. |
| `add_temperature(name, region, temperature, distribution_name=None, amplitude_name=None, color=None)` | A temperature boundary condition. |
| `propagate(name)` | Copies the boundary condition to every following step. A following step that does not support the type is skipped, as the dialog does, so from the last step, or when no following step supports it, nothing changes. |
| `hide(names)`, `show(names)` | Symbol visibility. |

```python
step.bcs.add_fixed('Fix-1', pmx.create.region('NodeSetName', 'Bottom'))
step.bcs.add_displacement_rotation('BC-1', selection, u1=0, u2=0, u3=1.5)
```

A degree of freedom of a displacement and rotation boundary condition that is **not given is left
unconstrained**, and reads back as `None`. Because a missing argument means "keep" on a replace,
`replace_displacement_rotation` takes `free_dofs=['U3']` to unconstrain one again; its values are `U1`,
`U2`, `U3`, `UR1`, `UR2` and `UR3`, in any case. A degree of freedom named in `free_dofs` cannot also be
given a value in the same call - the value would be lost - so that is refused. At least one degree of
freedom must stay constrained, by a value or in `fixed_dofs`: the dialog refuses a boundary condition that
constrains nothing, and so does an add without one and a replace that frees every one. A submodel boundary
condition likewise needs at least one of its degrees of freedom `True`.

The dialog also offers **`Fixed`**, a degree of freedom written to the solver input without a value.
`add_displacement_rotation` and `replace_displacement_rotation` take those as `fixed_dofs=['U1', 'U2']`,
with the values `U1`, `U2` and `U3`, in any case: the dialog offers `Fixed` for the displacements only, so a
rotation in `fixed_dofs` is refused - give it `0` instead. A fixed degree of freedom reads back as `None`
from `get_value`, and `get_value('fixed_dofs')` lists the fixed ones, so the list goes back into the
argument; `get_value('free_dofs')` lists the unconstrained ones, which `free_dofs` of
`replace_displacement_rotation` takes back. As the dialog does, the API refuses a degree of freedom both fixed and given a value, or both
fixed and named in `free_dofs`, fixed degrees of freedom next to valued ones, and a fixed `U3` in a two
dimensional model.

A `distribution_name` on a displacement and rotation boundary condition multiplies its prescribed values
node by node: a `Scalar` distribution scales all of them, a `Vector` distribution scales `u1`, `u2` and
`u3` by its three components. A value of 0 stays 0. The API refuses it before a command is recorded on a
reference point region, when no degree of freedom has a nonzero value, and for a `Vector` distribution
when `ur1`, `ur2` or `ur3` has a nonzero value.

```python
pmx.model.distributions.add_from_equation('Ramp', 'x / 100')
step.bcs.add_displacement_rotation('BC-2', selection, u1=0, u2=0, u3=1.5, distribution_name='Ramp')
```

A reference point region is accepted only by a fixed and by a displacement and rotation boundary
condition, the two types the dialogs offer it for. `add_submodel` and `add_temperature` refuse
one before a command is recorded, and so does a `replace_*` of a type that takes none.

The step decides which types it supports - a static step takes no temperature boundary condition, a
heat transfer step takes nothing else - and the API rejects an unsupported type before it records a
command, naming the step type.

### `BoundaryCondition`

| Member | Description |
| --- | --- |
| `name`, `step_name` | The boundary condition name and the step it belongs to. |
| `item_type` | `fixed`, `displacement_rotation`, `submodel` or `temperature`, named after the `add_*` method. |
| `active`, `visible` | Item state. |
| `region_type`, `region_name` | The region the boundary condition is applied to. |
| `amplitude_name`, `coordinate_system_name` | The names the boundary condition refers to. |
| `color` | The symbol color as a plain list `[R, G, B, A]`. |
| `get_value_names()`, `get_value(name)` | The values specific to this boundary condition type, named like the `replace_*` arguments and nothing else, so they go back into the replace of the type; a displacement and rotation boundary condition of a steady state dynamics step, which is complex, also reads back `phase_deg`. A displacement and rotation boundary condition reads a free and a fixed degree of freedom back as `None` and lists the fixed ones under `fixed_dofs` and the free ones under `free_dofs`, each a `list[str]`. |
| `get_equations()` | The expressions the values were entered with, under the names `get_value` uses (for example `u1` or `phase_deg`), as a `dict[str, str]`. Only a value entered as an expression is returned, with its leading `=`; a value entered as a plain number is left out, and `get_value` returns it. |

### `Loads`

Reached as `step.loads`. It shares the collection verbs. Every `region` argument is a `Selection` or
a `Region`; the selection target follows the load type. A gravity, a centrifugal and a body flux load
pick parts, as their dialog does, because the selection becomes an element set made of the picked parts;
named, their region is an `ElementSetName` or a `PartName` region.

| Member | Selection target | Description |
| --- | --- | --- |
| `add_concentrated_force(name, region, f1=None, f2=None, f3=None, ...)` | nodes | A concentrated force. |
| `add_moment(name, region, m1=None, m2=None, m3=None, ...)` | nodes | A moment. |
| `add_pressure(name, region, magnitude, distribution_name=None, ...)` | surfaces | A pressure. |
| `add_hydrostatic_pressure(name, region, first_point=None, second_point=None, direction=None, first_point_pressure=None, second_point_pressure=None, cutoff=None, ...)` | surfaces | A hydrostatic pressure. `cutoff` is `None`, `Positive` or `Negative`. The default `direction` is zero, so a direction must be given, and the two points must lie apart along it. |
| `add_imported_pressure(name, region, file_name, pressure_time, pressure_variable_name, interpolator_type=None, magnitude_factor=None, geometry_scale_factor=None, ...)` | surfaces | A pressure read from an OpenFOAM case, named by its `.foam` file, and interpolated onto the surface. As the dialog does, a case without results, a `pressure_time` the case does not hold and a `pressure_variable_name` that is not a scalar field of that time are refused, on an add and on the load a replace ends with. |
| `add_surface_traction(name, region, f1=None, f2=None, f3=None, p1=None, p2=None, p3=None, distribution_name=None, ...)` | surfaces | A surface traction. |
| `add_shell_edge_load(name, region, magnitude, ...)` | surfaces | A shell edge load. |
| `add_gravity(name, region, f1=None, f2=None, f3=None, ...)` | parts | A gravity load. |
| `add_centrifugal(name, region, axis_point=None, axis_direction=None, rotational_speed=None, ...)` | parts | A centrifugal load. The `rotational_speed` is in rad/s. The defaults of `axis_direction` and `rotational_speed` are zero, so a model that is not axisymmetric needs the direction, and every model the speed. |
| `add_pre_tension(name, region, magnitude, direction=None, tension_type=None, ..., auto_direction=None)` | surfaces | A pre-tension load. `tension_type` is `Force` or `Displacement`. |
| `add_concentrated_flux(name, region, magnitude, add_flux=None, ...)` | nodes | A concentrated flux. |
| `add_surface_flux(name, region, magnitude, ...)` | surfaces | A surface flux. |
| `add_body_flux(name, region, magnitude, ...)` | parts | A body flux. |
| `add_film_heat_transfer(name, region, sink_temperature, film_coefficient, coefficient_amplitude_name=None, ...)` | surfaces | A film heat transfer load. |
| `add_radiation_heat_transfer(name, region, sink_temperature, emissivity, cavity_radiation=None, cavity_name=None, emissivity_amplitude_name=None, ...)` | surfaces | A radiation heat transfer load. |
| `propagate(name)` | | Copies the load to every following step. A following step that does not support the load type is skipped, as the dialog does, so from the last step, or when no following step supports it, nothing changes. |
| `hide(names)`, `show(names)` | | Symbol visibility. |

Every `add_*` ends with `amplitude_name`, `coordinate_system_name`, `phase_deg` and `color`, and has
a matching `replace_*` that takes `old_name` first and `name` second. A value an add needs - the
`magnitude` of a pressure, a shell edge load, a pre-tension or a flux, the `sink_temperature` and
`film_coefficient` of a film heat transfer, the `sink_temperature` and `emissivity` of a radiation - is
refused as `None` on the add, while `None` keeps the value on a replace.

```python
step.loads.add_concentrated_force('Force-1', selection, f3=-1000)
step.loads.add_pressure('Pressure-1', pmx.create.region('SurfaceName', 'Top'), 2.5)
step.loads.add_gravity('Gravity-1', part_selection, f3=-9810)
```

The details the model imposes:

- **A surface traction has two independent component groups.** `f1`-`f3` are the total force and
  `p1`-`p3` the force per area. The solver is given the group that matches the distribution: the
  force components without a distribution, the force per area components with one. The other group is
  refused, checked against the distribution the load carries after the call.
- **A coordinate system belongs to a concentrated force or a surface traction.** The solver input
  transforms only their nodes, and the dialog offers one only for them, so any other load refuses a
  `coordinate_system_name` other than `Global`.
- **A phase belongs to a complex load.** Only a steady state dynamics step makes its loads complex, so
  `phase_deg` is refused in any other step and by a thermal or pre-tension load, and is read back only
  where it is taken. A phase is at least 0 and below 360 degrees: the load would wrap any other value
  into that range and store the number in place of an expression, so it is refused instead.
- **A two dimensional model** refuses a nonzero `f3` of a concentrated force, gravity or surface traction,
  `p3`, `m1` and `m2` of a moment, and the third component of a hydrostatic point or direction. An
  axisymmetric model turns a centrifugal load about its y axis, so there `axis_point` must be `[0, 0, 0]`
  and `axis_direction` `[0, 1, 0]` when given. A negative `rotational_speed` is refused.
- **A load acts.** The dialog refuses on OK a load that would do nothing, and so does the API, in the
  dialog's words and naming the arguments, judged on the load an add or a replace ends with and on the
  value an expression evaluates to: a concentrated force, a moment, a gravity load or the component group
  a surface traction writes with every component 0, a pressure, a shell edge load or a pre-tension
  `Force` with a magnitude of 0, a centrifugal load with a zero axis or a `rotational_speed` of 0, and a
  hydrostatic pressure whose direction is zero or whose two points lie on one plane across it.
- **A direction is not zero.** A given `axis_direction` of a centrifugal load and `direction` of a hydrostatic
  pressure or a pre-tension load of zero length is refused naming the argument, as the dialog refuses it; so are
  the `axis_direction` of a cyclic symmetry, a joint and an initial angular velocity.
- A **pre-tension** load needs a three dimensional model, and computes its direction from the surface
  normal unless a `direction` is given. `auto_direction=True`, the default of an add, cannot be combined with
  `direction`, and `auto_direction=False` needs one. `replace_pre_tension` keeps every value that is not given, takes
  `auto_direction=True` to return to the surface normal, and refuses a
  `tension_type` that changes the type unless the `magnitude` is given as well.
- A **cavity name** has at most three characters, a solver limit, and the emissivity is between 0 and 1.
  A cavity radiation needs a cavity name, and a `cavity_name` is refused unless cavity radiation is on;
  a replace checks the values the load keeps, and `cavity_radiation=False` keeps a stored cavity name unused,
  as the dialog does.

An **imported pressure** is read from its results file on every model check, so `pressure_time` and
`pressure_variable_name` must name a field the file actually contains.

### `Load`

| Member | Description |
| --- | --- |
| `name`, `step_name` | The load name and the step it belongs to. |
| `item_type` | The load type, named after the `add_*` method that makes it without `add_`: `concentrated_force`, `moment`, `pressure`, `hydrostatic_pressure`, `imported_pressure`, `surface_traction`, `shell_edge_load`, `gravity`, `centrifugal`, `pre_tension`, `concentrated_flux`, `surface_flux`, `body_flux`, `film_heat_transfer` or `radiation_heat_transfer`. |
| `active`, `visible` | Item state. |
| `region_type`, `region_name` | The region the load is applied to. |
| `amplitude_name`, `coordinate_system_name` | The names the load refers to. |
| `color` | The symbol color as a plain list `[R, G, B, A]`. |
| `get_value_names()`, `get_value(name)` | The values specific to this load type, named like the `replace_*` arguments and nothing else, so they go back into the replace of the type, and `phase_deg` for a complex load other than a pre-tension. A value the argument would refuse in the state the load is in is left out: the `f1` to `f3` of a surface traction with a distribution and its `p1` to `p3` without one, the `direction` of a pre-tension whose `auto_direction` is True, and the `cavity_name` of a radiation without `cavity_radiation`. A vector reads back as one list of three: `first_point`, `second_point` and `direction` of a hydrostatic pressure, `axis_point` and `axis_direction` of a centrifugal load, and `direction` of a pre-tension. The `cutoff` of a hydrostatic pressure and the `magnitude`, `tension_type` and `auto_direction` of a pre-tension are single values. |
| `get_equations()` | The expressions the values were entered with, under the names `get_value` uses, as a `dict[str, str or list]`. Only a value entered as an expression is returned, with its leading `=`; a value entered as a plain number is left out, and `get_value` returns it. A vector argument such as `axis_point` with an expression in any component is a list, its plain components as numbers. |

### `FieldOutputs` (model)

Reached as `step.field_outputs`. A field output covers the whole model, so it carries no region. The
variables are given by name, one or a list of them.

| Member | Description |
| --- | --- |
| `add_nodal(name, variables, last_iterations=None, contact_elements=None, global_output=None)` | A nodal field output. `RF`, `U`, `PU`, `V`, `NT`, `PNT`, `RFL`. |
| `add_element(name, variables, output=None, ...)` | An element field output. `S`, `PHS`, `E`, `ME`, `PEEQ`, `ENER`, `HFL`, `ERR`, `HER`, `ZZS`, `SDV`. `output` is `Default`, `TwoD` or `ThreeD`. |
| `add_contact(name, variables, ...)` | A contact field output. `CDIS`, `CSTR`, `CELS`, `PCON`. |
| `propagate(name)` | Copies the field output to every following step. From the last step nothing changes, as in the dialog. |

```python
step.field_outputs.add_nodal('NF-Output-1', ['U', 'RF'])
step.field_outputs.add_element('EF-Output-1', ['S', 'E'])
```

### `FieldOutput` (model)

| Member | Description |
| --- | --- |
| `name`, `step_name` | The field output name and the step it belongs to. |
| `item_type` | `nodal`, `element` or `contact`, named after the `add_*` method. |
| `active` | True when the field output is written to the solver input file. |
| `last_iterations` | Writes the results of the last iteration of a diverged increment. |
| `contact_elements` | Writes the results for the contact elements. |
| `global_output` | Writes the results in the global coordinate system. |
| `get_variables()` | The requested output variables as a list of names. |
| `get_value_names()`, `get_value(name)` | The values specific to this field output type, named like the `replace_*` arguments: `variables`, and `output` of an element output. `variables` reads back as a list of names, the form the `variables` argument takes, even for a single variable. |

### `HistoryOutputs` (model)

Reached as `step.history_outputs`. Unlike a field output it carries a region.

| Member | Selection target | Description |
| --- | --- | --- |
| `add_nodal(name, region, variables, totals_type=None, global_output=None)` | nodes | A nodal history output. `RF`, `U`, `V`, `NT`, `RFL`. |
| `add_element(name, region, variables, ...)` | elements | An element history output. `S`, `E`, `ME`, `PEEQ`, `HFL`, `ENER`, `ELSE`, `ELKE`, `EVOL`, `EBHE`, `SDV`. |
| `add_section(name, region, variables, ...)` | surfaces | A section history output. `SOF`, `SOM`, `SOAREA`. |
| `add_contact(name, contact_pair_name, variables, ...)` | - | A contact history output. It names a **contact pair** instead of a region. `CDIS`, `CSTR`, `CELS`, `CNUM`, `CF`. |
| `propagate(name)` | | Copies the history output to every following step. From the last step nothing changes, as in the dialog. |

`totals_type` is `Yes`, `Only` or `No`, and decides whether the sum of the external forces is printed
in addition to, or instead of, the individual values.

```python
step.history_outputs.add_nodal('H-Output-1', selection, ['U', 'RF'], totals_type='Only')
```

### `HistoryOutput` (model)

| Member | Description |
| --- | --- |
| `name`, `step_name` | The history output name and the step it belongs to. |
| `item_type` | `nodal`, `element`, `section` or `contact`, named after the `add_*` method. |
| `active` | True when the history output is written to the solver input file. |
| `region_type`, `region_name` | The region, or the contact pair, the history output is written for. |
| `totals_type`, `global_output` | The shared history output values. |
| `get_variables()` | The requested output variables as a list of names. |
| `get_value_names()`, `get_value(name)` | The values specific to this history output type, named like the `replace_*` arguments. `variables` reads back as a list of names, the form the `variables` argument takes, and a contact history output reads its contact pair back as `contact_pair_name`. |

### `DefinedFields`

Reached as `step.defined_fields`. A defined temperature has two forms, and each one is its own add
method, so the type follows from the method that is used.

| Member | Description |
| --- | --- |
| `add_temperature(name, region, temperature, distribution_name=None, amplitude_name=None, color=None)` | A defined temperature given by value. The selection target is `geometry`, as the dialog picks it, or `nodes`. |
| `add_temperature_from_file(name, file_name, step_id=None, color=None)` | A defined temperature read from a results file. It covers the whole model, so it takes no region, and it is written without an amplitude, so it takes none. `step_id` is at least 1. |
| `propagate(name)` | Copies the defined field to every following step. A following step that does not support the type is skipped, as the dialog does, so from the last step, or when no following step supports it, nothing changes. |
| `hide(names)`, `show(names)` | Symbol visibility. |

`replace_temperature` and `replace_temperature_from_file` switch the defined temperature type: `replace_temperature` on
a defined field read from a file needs both a `region` and a `temperature` and makes it one given by value, and
`replace_temperature_from_file` on one given by value needs a `file_name` and makes it one read from a file, with the
amplitude `Default`, because a temperature read from a file is written without one. Values not given are kept.

A `distribution_name` must name a `Scalar` distribution for `bcs.add_temperature` and `bcs.replace_temperature`,
`defined_fields.add_temperature` and `defined_fields.replace_temperature`, and `loads.add_pressure` and
`loads.replace_pressure`. `loads.add_surface_traction` and `loads.replace_surface_traction`, and
`bcs.add_displacement_rotation` and `bcs.replace_displacement_rotation`, accept a `Scalar` or a `Vector` distribution.

### `DefinedField`

| Member | Description |
| --- | --- |
| `name`, `step_name` | The defined field name and the step it belongs to. |
| `item_type` | `temperature` or `temperature_from_file`, named after the `add_*` method that makes it. |
| `active`, `visible` | Item state. |
| `region_type`, `region_name` | The region the defined field is applied to. |
| `amplitude_name` | The amplitude the defined field refers to. |
| `color` | The symbol color as a plain list `[R, G, B, A]`. |
| `get_value_names()`, `get_value(name)` | The values specific to this defined field type, named like the `replace_*` arguments: `temperature` and `distribution_name`, or `file_name` and `step_id` for a temperature read from a file. |
| `get_equations()` | The expressions the values were entered with, under the names `get_value` uses (for example `temperature`), as a `dict[str, str]`. Only a value entered as an expression is returned, with its leading `=`; a value entered as a plain number is left out, and `get_value` returns it. |

## Parts

### `Create`

Reached as `pmx.create`. A model item is committed by an `add` method on its collection; `pmx.create`
only makes the detached **parts** that those methods take as arguments, and records no command.

| Member | Description |
| --- | --- |
| `selection_by_ids(view, target, source, ids)` | Creates an ID-based selection. |
| `elastic(youngs_modulus, poissons_ratio=None)` | Elastic property. Two values, or a list of `[E, v, temperature]` rows; `poissons_ratio` is required with a single `youngs_modulus`. Any value of this and of every other material property maker may be an equation string. |
| `elastic_with_density(youngs_modulus, poissons_ratio, density)` | Combined elastic and density property. |
| `elastic_engineering_constants(constants)` | Orthotropic elastic. Rows of `[E1, E2, E3, v12, v13, v23, G12, G13, G23, temperature]`, each physically stable, with strictly increasing temperatures. |
| `elastic_lamina(constants)` | Lamina elastic. Rows of `[E1, E2, v12, G12, G13, G23, temperature]`, each physically stable, with strictly increasing temperatures. |
| `density(density)` | Density. One value, or rows of `[density, temperature]`. |
| `plastic(stress_strain, hardening=None)` | Plastic. Rows of `[stress, strain, temperature]`. `hardening` is `Isotropic`, `Kinematic` or `Combined` and defaults to `Isotropic`. |
| `specific_heat(specific_heat)` | Specific heat. One value, or rows of `[value, temperature]`. |
| `thermal_conductivity(thermal_conductivity)` | Thermal conductivity, same shape. |
| `thermal_expansion(thermal_expansion, zero_temperature=None)` | Thermal expansion, same shape. `zero_temperature` is the temperature the expansion starts from and defaults to 20 in model units. |
| `slip_wear(hardness, wear_coefficient)` | Slip wear property. |
| `region(region_type, name)` | Names an existing region of the model. Only the region types a named region of the model carries are taken; `None`, `NodeId`, `ElementId`, `Selection` and `ContactPair` are refused. |
| `split_tool_line_by_two_points(points=None, splitter=None)` | A line drawn on the surfaces through two points, for `split_face` only. |
| `split_tool_plane_by_three_points(points=None, splitter=None, offset=0)` | A plane through three points. |
| `split_tool_plane_by_point_and_normal(point=None, normal=None, splitter=None, offset=0)` | A plane given by a point and a normal. |
| `split_tool_extend_face(faces, offset=0)` | The surfaces of the selected faces, continued past their boundaries. |
| `split_tool_extrude_edges(edges, direction=None, direction_selection=None, length=None, both_directions=True)` | The selected edges swept along a direction. |
| `damping(damping_type=None, alpha=None, beta=None)` | Rayleigh damping for a dynamic step, a `Damping` part. `damping_type` is `Off`, the default, or `Rayleigh`. `alpha` and `beta` must be finite, and only the `Rayleigh` type takes them. A value the type does not use reads back as `None`. |
| `modal_damping(damping_type=None, alpha=None, beta=None, viscous_damping_ratio=None, damping_ratios=None)` | Modal damping for a modal dynamics or steady state dynamics step, a `ModalDamping` part. `damping_type` is `Off`, the default, `Constant`, `Direct` or `Rayleigh`, and each type takes only the values it uses: `alpha` and `beta` the `Rayleigh` type, `viscous_damping_ratio` the `Constant` type and `damping_ratios` the `Direct` type, which needs them. The real values must be finite. A value the type does not use reads back as `None`. |
| `damping_ratio(damping_ratio, lowest_mode, highest_mode)` | A viscous damping ratio for a mode range, used by the `Direct` modal damping type, a `DampingRatio` part. The ratio must be finite, `lowest_mode` at least 1 and `highest_mode` at least `lowest_mode`. |
| `time_incrementation_control(...)` | A time incrementation step control parameter, a `StepControlParameter` part. |
| `field_control(...)` | A field step control parameter, a `StepControlParameter` part. |
| `contact_control(...)` | A contact step control parameter, a `StepControlParameter` part. |
| `reset_control()` | A reset step control parameter, which restores the CalculiX defaults, a `StepControlParameter` part. |
| `environment_variable(name, value, active=True)` | A solver environment variable for `pmx.jobs.add` and `replace`, an `EnvironmentVariable` part. |

A tabular property takes either the values of a single row, or a list of rows whose **last value is
always the temperature**. `plastic`, `elastic_engineering_constants` and `elastic_lamina` always
require the list form, because a single row is not enough to describe them.

The three step control makers take one optional argument per CalculiX parameter, and only the values
that are given override the solver defaults - `time_incrementation_control` takes `i0`, `ir`, `ip`,
`ic`, `il`, `ig`, `is_value`, `ia`, `ij`, `it`, `df`, `dc`, `db`, `da`, `ds`, `dh`, `dd` and `wg`;
`field_control` takes `rna`, `cna`, `q0a`, `qua`, `rpa`, `ea`, `cea` and `rla`; `contact_control`
takes `delcon`, `alea`, `kscalemax` and `itf2f`. A value left out or `None` keeps the solver default;
`is_value`, `ij`, `it`, `ds`, `dh` and `wg` of the time incrementation and `q0a` and `qua` of the field
control are unset by default, and the journal leaves out every value that is still the default. A value the
parameter would clamp is refused: the integer values are at least 1, the real values finite and at least 0,
and `alea` is between 0 and 1.

### `DetachedPart`

The damping parts, the step control parameters and the environment variable are detached parts:
`pmx.create.damping`, `modal_damping` and `damping_ratio` make a `Damping`, a `ModalDamping` and a
`DampingRatio`, the four `*_control` makers a `StepControlParameter`, and `environment_variable` an
`EnvironmentVariable`. They are also what `step.get_value('damping')`, `step.get_value('modal_damping')`,
`step.get_controls()` and `job.environment_variables` return, as copies, so changing nothing in the model.
None of them hands out the .NET model object; each one reads back under the argument names of its maker.
A `MaterialProperty` and a `SurfaceInteractionProperty` are detached parts too, with the same three members.
`get_value` finds a value by its exact name: a padded name is no name and is not trimmed.

| Member | Description |
| --- | --- |
| `get_values()` | The values under the argument names of the `pmx.create` maker, as a `dict`, so that the maker given them back as keyword arguments makes the same part - `pmx.create.damping(**d.get_values())`. An option is its PascalCase name, a value that is not set (`is_value`, `ij`, `it`, `ds`, `dh`, `wg`, `q0a`, `qua` left at their defaults, `damping_ratios` of a modal damping without ratios, a damping value its damping type does not use) is `None`, and the damping ratios of a modal damping are a plain list of `DampingRatio` parts. A reset control has no values. |
| `get_value(name)` | One value, by the maker's argument name. |
| `get_value_names()` | The names `get_values()` reports, as a `StringList`. |
| `control_type` | `StepControlParameter` only: the maker that made it, `time_incrementation_control`, `field_control`, `contact_control` or `reset_control`, so `getattr(pmx.create, c.control_type)(**c.get_values())` rebuilds it. |
| `name`, `value`, `active` | `EnvironmentVariable` only: the variable as `environment_variable` takes it. |

```python
controls = step.get_controls()
print([c.control_type for c in controls], controls[0].get_values())
damping = pmx.create.damping('Rayleigh', alpha=0.1, beta=0.001)
print(damping.get_values())   # {'damping_type': 'Rayleigh', 'alpha': 0.1, 'beta': 0.001}
```

### `SplitTool`

Created by a `pmx.create.split_tool_*` method and taken by `split_solid` and `split_face`. Its only
member is `tool_type`, the name of the method that made it without `split_tool_`, for example
`'plane_by_three_points'` for `split_tool_plane_by_three_points`. A tool is a detached description:
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
`ElementSetName`, `SurfaceName`, `ReferencePointName`, `PartName` or `MassSection`; any other region type
names no region of the model and is refused when the region is created.

A named region of a type the item's dialog does not offer is **refused**, before anything is recorded. What
each item takes:

| Selection target | Accepted region types |
| --- | --- |
| `nodes` | `NodeSetName`; `SurfaceName`, except on a concentrated force, a moment, a concentrated flux, a point spring and a point mass; `ReferencePointName`, only on a boundary condition (`fixed`, `displacement_rotation`), a concentrated force, a moment, a point spring, a point mass, a nodal history output and the initial translational and angular velocity |
| `elements` | `ElementSetName`; `PartName`, except on an element history output |
| `surfaces` | `SurfaceName` |

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
| `add(name, fem_solver=None, executable=None, argument=None, work_directory=None, compatibility_mode=None, number_of_cpus=None, environment_variables=None)` | Adds an analysis. `fem_solver` is `Calculix`, `Abaqus` or `FEMaster`. |
| `replace(old_name, name=None, ..., inherit_executable=None, inherit_work_directory=None, inherit_number_of_cpus=None, inherit_environment_variables=None)` | Replaces an analysis. A value that is not given is kept. An `argument` edited away from the solver default survives a new name or solver; otherwise it follows them, so renaming `Job-1` to `Job-2` makes the argument `Job-2` and `fem_solver='Abaqus'` makes it `job=Job-2`. A given `argument` always wins. `rename` treats the argument the same way, so an edited argument survives it too. |
| `open_results(name)` | Opens the results of the analysis. They become the current result of `pmx.results`. |

`len(pmx.jobs)` and `contains(name)` also work where there is no job gateway, and report no analyses; every command
that changes or runs an analysis is refused there.

A value given here is an override: the application settings fill in only what the analysis does not
carry itself, so adding another analysis or changing the settings leaves an explicit executable, work
directory, CPU count or environment alone. An `inherit_*` switch is how an override is given back, so
that the value follows the settings again; it cannot be combined with the value it replaces.

```python
job = pmx.jobs.add('Job-1', number_of_cpus=4,
                   environment_variables=[pmx.create.environment_variable('OMP_STACKSIZE', '256M')])
pmx.jobs.replace('Job-1', inherit_number_of_cpus=True)
```

### `Job`

| Member | Description |
| --- | --- |
| `name` | The analysis name. |
| `fem_solver` | The FEM solver. |
| `executable`, `argument`, `work_directory` | What the solver is run as. The argument names the input file and defaults to the analysis name. The work directory is where the input file is written, where the previous run's files are deleted and where the solver runs, so an analysis with its own work directory is self contained. |
| `compatibility_mode` | Runs the solver in the simplified mode. |
| `number_of_cpus` | The number of solver CPUs. |
| `environment_variables` | Detached copies of the solver environment variables, `EnvironmentVariable` parts (see `DetachedPart`), as a plain `list` that goes back into `environment_variables` of `add` and `replace`. |
| `inherit_executable`, `inherit_work_directory`, `inherit_number_of_cpus`, `inherit_environment_variables` | True when the value follows the application settings, False when the analysis overrides it. They go back into the `inherit_*` arguments of `replace`, so a copy of an analysis gives an inherited value back as `inherit_*=True` rather than as the value, which would turn it into an override. |
| `status` | The status of the last run: `None`, `InQueue`, `Running`, `OK`, `Killed`, `TimedOut`, `Failed` or `FailedWithResults`. |
| `results_file_name` | The name of the results file the analysis writes. |
| `is_up_to_date` | True when the results match the current model. |
| `run(wait=True, timeout=None)` | Writes the solver input file and runs the analysis. Returns the status. The timeout is in seconds and needs `wait`; it kills the solver and leaves the analysis with the status `TimedOut`. A preparation the application refuses raises instead of returning a stale status. |
| `check_model(wait=True, timeout=None)` | The same, but only checks the model without solving it. |
| `kill()` | Kills a running analysis. Refused in read-only automation mode. |
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
the console or an automation call, because a batch run that ends while the solver is still working
kills it.

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
| `result_type` | How the results are plotted: `Undeformed`, `Deformed`, `ColorContours`, `VectorPlot` or `TensorPlot`, in any case. Settable. |
| `set_orientation(orientation)` | Points the camera along a standard direction: `Front`, `Back`, `Top`, `Bottom`, `Left`, `Right`, `Isometric`, `IsometricNegative`, `Normal` or `Vertical`, in any case. |
| `get_orientation_names()` | The orientation names `set_orientation` accepts. |
| `zoom_to_fit()` | Fits everything visible in the viewport. |
| `get_parameters()` | The camera as a `list[float]` of 10 values: position, focal point and view up, three each, then the parallel scale. |
| `set_parameters(parameters)` | Sets the camera from those 10 values. |
| `rotate(azimuth_deg, elevation_deg=0, roll_deg=0)` | Turns the camera about the model by angles in degrees: `azimuth_deg` about the vertical axis of the view, `elevation_deg` about its horizontal axis, `roll_deg` about the viewing direction. |
| `look_at(point, parallel_scale=None)` | Moves the camera so that it looks at a point in model coordinates, keeping the viewing direction and the view up. The parallel scale is half the viewport height in model units; one that is not given keeps the zoom. |
| `set_result_field(field_name, component_name=None)` | Plots a field component of the current result. The component that is not given is the first one of the field. |
| `set_result_increment(step_id, increment_id)` | Plots a given increment. The IDs are result IDs, the ones `pmx.results.current.steps` reports. |
| `get_result_field()` | What is plotted now, as a `dict` of `field_name`, `component_name`, `step_id` (`int`) and `increment_id` (`int`), the values `set_result_field` and `set_result_increment` take; `None` when no result field is plotted. |
| `deformation_type` | How much the plotted result is deformed, as the results toolbar sets it: `Undeformed`, `TrueScale`, `Automatic`, its multiples `Automatic025`, `Automatic05`, `Automatic2`, `Automatic5`, or `UserDefined`. Settable; setting it needs an open result. |
| `deformation_factor` | The scale factor the `UserDefined` deformation type uses. It is kept when another type is set, the way the toolbar keeps it. Settable; setting it needs an open result. |
| `undeformed_model` | The undeformed model drawn behind a `ColorContours`, `VectorPlot` or `TensorPlot` result: `None`, `WireframeBody` or `SolidBody`. Settable; setting it needs an open result, and is refused for the `Undeformed` and `Deformed` plots, which would discard it. |
| `set_legend_range(min_value, max_value)` | Fixes the color legend range of the plotted field component, so pictures of different increments or models share one color scale. Kept per field component while the result is open, the way the legend settings keep it. Needs an open result in the results view. |
| `reset_legend_range()` | Puts the legend of the plotted field component back to the automatic range. |
| `save_image(file_name=None, transparent_background=False, magnification=1)` | Writes a PNG of the viewport and returns the path. A file name that is not given means a file in the temporary folder. A transparent background leaves out the background the viewport shows, so the picture can be put on a page of any color; the model, the legend and the labels keep their own colors. The magnification, 1 to 4, renders the picture that many times larger than the viewport, for one that is going to be printed - only the model is drawn larger, the legend, the scale bar and the triad keep the size they have on screen. |
| `apply_exploded_view(scale_factor=0.5, method='Disassembly', magnification=2, axes='XYZ', step_by_step=None, tolerance=None, center=None)` | Pulls the parts of the displayed view apart, the way the exploded view dialog does. `method` is `Disassembly`, `AssemblyCenter` or `CenterPoint`, in any case, and `CenterPoint` needs `center`. The scale factor, 0 to 1, is the position of the dialog's slider; the magnification is 1 to 10 for `Disassembly`, 25 for `AssemblyCenter` and 1000 for `CenterPoint`. `axes` limits the movement to the coordinate axes `XYZ`, `X`, `Y`, `Z`, `XY`, `XZ` or `YZ`, in any case. A compound part moves as one body. `step_by_step`, off when not given, and `tolerance`, where 0 - the value when not given - means one thousandth of the assembly diagonal, are taken only by `Disassembly`, and `center` only by `CenterPoint`; another method refuses them rather than ignoring them. A value out of range is refused, not clamped. |
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
| `add_text(name, text, anchor_point)` | Adds a text label with an arrow pointing at a point in model coordinates. The name must be free, and a name with leading or trailing spaces is refused. Returns the name it was given. |
| `add_node(node_id)` | Adds the annotation a node query places: ID, coordinates and, in the results view, the plotted value. Returns the generated name. |
| `add_element(element_id)` | Adds the annotation an element query places. Returns the generated name. |
| `remove(names)` | Removes the named annotations. A single name or a list of names; a reserved or unknown name is refused. |
| `show_min_max(show_min, show_max)` | Shows or hides the labels at the minimum and the maximum of the plotted result, the way the post-processing settings do. Needs an open result. |
| `get_min_max()` | Whether those labels are shown, as the `list[bool]` `[show_min, show_max]`. Keep them to put the user setting back. |

```python
shown = pmx.view.annotations.get_min_max()
pmx.view.annotations.show_min_max(False, True)
label = pmx.view.annotations.add_text('Load', 'Pressure 2 MPa', [0, 0, 10])
name = pmx.view.annotations.add_node(15)
path = pmx.view.save_image()
pmx.view.annotations.remove([label, name])
pmx.view.annotations.show_min_max(shown[0], shown[1])
```

## Selections

### `SelectionFactory`

`pmx.selection` highlights a selection in the view and clears the highlight. Selections themselves are made by `pmx.create` (`selection_by_ids`, `selection_by_pick`, `selection_by_area`, `selection_by_box` and `selection_all`, below) and are independent of the live UI selection. The `view` is required and remains stored in the selection so command replay uses the correct mesh.

| Member | Description |
| --- | --- |
| `highlight(selection)` | Switches to the stored view and highlights the selection. |
| `clear_highlight()` | Clears the interactive selection and its highlight. |

Possible `view` values are `"geometry"`, `"femodel"`, and `"results"`.

Possible `target` values are `"nodes"`, `"elements"`, `"surfaces"`, `"edges"`, `"parts"`, `"geometry"`, and `"geometry_surfaces"`. Possible `source` values are `"nodes"`, `"elements"`, `"vertices"`, `"edges"`, `"surfaces"`, `"parts"`, and `"geometry"`.

The view, the target and the source are matched regardless of case, and the selection stores them in the lowercase
spelling above, so `selection.view` and `selection.target` read back the same whichever way they were written.

Nodes, elements, vertices, and parts use a list of IDs. Vertex IDs are the node IDs displayed by Query. Edge and surface IDs are 1-based within each part and use a part-to-ID mapping:

```python
selection = pmx.create.selection_by_ids(
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
| `pmx.create.selection_by_pick(view, target, point, select_by, angle_deg=None, direction=None, part_ids=None, completely_inside=False, precision=None, part_offsets=None)` | Creates a selection from a point pick. |
| `pmx.create.selection_by_area(view, target, planes, select_by, angle_deg=None, part_ids=None, completely_inside=False, part_offsets=None)` | Creates a selection from an area pick. Every row of `planes` is `[ox, oy, oz, nx, ny, nz]`, and a normal points **out** of the volume. |
| `pmx.create.selection_by_box(view, target, corner1, corner2, select_by, part_ids=None, completely_inside=False, part_offsets=None)` | The six plane form of `selection_by_area`, given by two opposite corners. |
| `pmx.create.selection_all(view, target)` | Creates a selection of everything the view holds. |
| `selection.add_by_ids(source, ids, operation=None)`, `add_by_pick(point, select_by, angle_deg=None, ..., operation=None)`, `add_by_area(planes, select_by, angle_deg=None, ..., operation=None)` | Adds another item to a selection. |
| `selection.add_by_box(corner1, corner2, select_by, part_ids=None, completely_inside=False, operation=None, part_offsets=None)` | Adds an axis aligned box, the six plane form of `add_by_area`. |
| `selection.add_all(operation='Add')` | Adds everything the view holds. |
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

`configure` records the options the selection was made with and returns the selection, so it chains.
The application applies them to every pick of the selection, while editing and on replay, and an option that is
not given keeps its current value:

- `max_geometry_ids` and `max_item_ids` cap the number of geometry ids and of items the selection holds. A pick
  that would exceed the cap is dropped, a cap of `1` makes each pick replace the previous one, and `-1`, the
  default of a new selection, means unlimited. Any other value below `1` is refused, naming the argument.
- `limit_to` keeps every pick on the parts of the first one: `None` (the default) takes any part, `FirstPart`
  only the first part, `FirstGeometryType` only parts of its part type, such as solid or shell, and
  `FirstMesherType` only parts meshed by its mesher. It is matched in any case.
- `shell_edges_only` limits a pick to shell edge faces and `enable_shell_edge_faces` lets a pick take them; both
  are off by default.

`get_options()` reads them back.

The `view` is `'geometry'`, `'femodel'` or `'results'`, in any case, and reads back lower case.

`select_by` is `Node`, `Element`, `Edge`, `Surface`, `EdgeAngle`, `SurfaceAngle`, `Part`, `Geometry`,
`GeometryVertex`, `GeometryEdge`, `GeometrySurface`, `GeometryEdgeAngle`, `GeometrySurfaceAngle` or
`GeometryPart`. The four angle based modes need an `angle_deg`, in degrees, finite and from 0 to 180, the range
of the angle between two faces; the selection dialog keeps the angle it records in the same range, and a journal
writes a larger angle from an older history as 180, which selects the same items.

```python
top = pmx.create.selection_by_pick('femodel', 'nodes', [0.5, 0.5, 1.0], 'Surface')
pmx.model.node_sets.add('Top', top)
```

A pick and a list of IDs can live in the same selection, and a pick survives a mesh change, because
it is resolved again rather than replayed from the IDs it once produced. That is what makes it worth
having over `selection_by_ids` for a script meant to outlive one mesh.

```python
top = pmx.create.selection_by_box('femodel', 'nodes', [0, 0, 0.9], [1, 1, 1.1], 'Node')
all_but_top = pmx.create.selection_by_box('femodel', 'nodes', [0, 0, 0.9], [1, 1, 1.1], 'Node').invert()
```

Four things to know about how the application resolves a selection:

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
| `add_by_ids(source, ids, operation=None, geometry_select_mode='SelectId')` | Adds another compatible ID source. The select mode applies only to geometry source IDs. An edge or a surface ID counts from 1 on its part; 0 or a negative ID is refused. |
| `add_all(operation='Add')` | Adds everything the view holds. |
| `configure(max_geometry_ids=None, max_item_ids=None, limit_to=None, shell_edges_only=None, enable_shell_edge_faces=None)` | Records the selection options and returns the selection. |
| `get_options()` | The recorded selection options, as a `dict[str, Any]`. |
| `get_sources()` | Returns the source types as `StringList`. |
| `get_ids()` | Resolves the selection against the current mesh and returns the IDs as `IntList`. |
| `preview()` | Resolves the selection and reports what it found, as `SelectionPreview`. |

### `SelectionPreview`

The dry run of a selection: what it resolves to right now, without creating a model item and without
recording anything. A pick that found the wrong face is caught here rather than after it has become
a boundary condition.

| Member | Description |
| --- | --- |
| `target` | The resolved item type. |
| `count` | The number of resolved items. Zero means the selection finds nothing in the current view, which is reported rather than thrown. |
| `ids` | The resolved IDs as an `IntList`: node IDs, element IDs, part IDs or geometry IDs, following the target. |
| `part_names` | The names of the parts the resolved items belong to. |
| `bounding_box` | `[xMin, xMax, yMin, yMax, zMin, zMax]` of the resolved items as a `list[float]`, or an empty list when nothing resolved. |
| `centroid` | The mean of the node coordinates of the resolved items as `[x, y, z]`, or an empty list. |

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
| `current` | The currently selected `Result`. Assigning a loaded result name or a `Result` selects that result through command history, the way the results list of the toolbar does. |
| `history_outputs` | Returns history outputs for the currently selected result. |
| `field_outputs` | Returns field outputs for the currently selected result. |
| `get_names()` | Returns result names as `StringList`. |
| `open(file_name)` | Opens a .frd, .dat, .odb or .foam result file beside the current model through command history, the way **File - Open** does for a result file. The model and its command history are kept, so it is available inside a recorded script and the journal writes it. A .pmx or .pmh file is refused and named to `pmx.open`. |
| `remove_current()` | Removes the current result through command history. |
| `clear()` | Removes every loaded result through command history. |
| `contains(name)` | Checks whether a result exists. |

Example:

```python
names = pmx.results.get_names()
pmx.results.current = names[-1]
result = pmx.results.current
```

### `Result`

Single loaded result file.

| Member | Description |
| --- | --- |
| `name` | Immutable user-visible result name. |
| `file_name` | Source result file name. |
| `date_time` | Result file date and time, as an ISO-8601 string in local time without fractions of a second or a time zone, for example `'2026-10-10T14:03:22'`. |
| `unit_system` | Result unit system. |
| `steps` | Result steps indexed by result step ID. |
| `mesh` | Result mesh. |
| `history_outputs` | Result history outputs indexed by history output name. |
| `field_outputs` | User-defined result field output definitions. |
| `get_nodal_value(step_id, increment_id, field_name, component_name, node_id)` | Returns one nodal field component value. |
| `get_nodal_coordinates(node_id)` | Returns the undeformed node coordinates as `[x, y, z]`. |
| `get_deformed_nodal_coordinates(step_id, increment_id, node_id)` | Returns true-scale deformed coordinates as `[x, y, z]` using `DISP`. |
| `get_history_output_entries(history_output_name, field_name, component_name)` | Returns the entries in a history output component. |
| `get_history_output_entry(history_output_name, field_name, component_name, entry_name)` | Returns one named history output entry. |

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
| `type` | `"Median"`, `"Average"`, or `"Smooth"`. |
| `source` | `"AllNodes"` or `"SurfaceNodes"`. |
| `number_of_layers` | Number of neighbouring node layers. |
| `number_of_iterations` | Number of filter iterations. |

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
| `create_median_filter(source="AllNodes", number_of_layers=1, number_of_iterations=1)` | Creates a median filter. `source` can be `"AllNodes"` or `"SurfaceNodes"`, in any case. |
| `create_average_filter(source="AllNodes", number_of_layers=1, number_of_iterations=1)` | Creates an average filter. `source` can be `"AllNodes"` or `"SurfaceNodes"`, in any case. |
| `create_smooth_filter(source="AllNodes", number_of_layers=1, number_of_iterations=1)` | Creates a smooth filter. `source` can be `"AllNodes"` or `"SurfaceNodes"`, in any case. |
| `add(output)` | Validates and adds a `FieldOutputDraft` using command history. |
| `replace(old_name, output)` | Validates and replaces a field-output definition using command history. The draft must be of the type the replaced output is made by: a replace never turns a limit into an equation, and is refused naming the `add_*` that made the old one. The same holds for every `replace_*`. |
| `rename(old_name, new_name)` | Renames a result field output using command history. |
| `duplicate(names)` | Duplicates field outputs using command history. A name or a list of names. |
| `remove(names)` | Removes field outputs using command history. A name or a list of names. |
| `remove_components(field_output_name, component_names)` | Removes components from a field output using command history. |
| `export_to_csv(file_name, field_output_name, step_id, increment_id)` | Exports all field components at one result increment using command history. |

The `name` of every draft is checked as a model item name is, when the draft is created: an empty name, one with leading or trailing spaces or with characters a name cannot hold is refused.

`based_on` is required and must be `"Parts"`, `"ElementSets"`, or `"AllElements"` (in any case; it reads back in this spelling). `item_limits` must be a non-empty Python dictionary with finite, nonzero values. Its keys are part names, element-set names, or the single key `"All elements"` when `based_on="AllElements"`. Part and element-set names and the `"All elements"` key are matched regardless of case and stored as the result spells them, so `item_limits` reads back in that spelling; two keys that name the same item are refused. The source `field_name` of a limit, an envelope or a coordinate system transform, and the `coordinate_system_name` of a transform, are matched regardless of case as well and stored in the result's spelling, so a user-defined field output is found under any case.

Creation only produces a Python-side draft and does not call the controller or modify command history. Assign optional filters to the draft, then call `add(output)`. The complete draft is converted and validated before the postprocessing command is created. If the current result changes after a `Result` object was obtained, get `pmx.results.current` again before adding the draft.

Filter creation is available directly on `pmx.results.field_outputs` and `result.field_outputs`. The filter `source` must be `"AllNodes"` or `"SurfaceNodes"` (in any case); `number_of_layers` and `number_of_iterations` must both be at least 1.

One limit for all elements:

```python
result = pmx.results.current

output = result.field_outputs.create_field_output_limit(
    name="Stress_limit",
    field_name="STRESS",
    component_name="MISES",
    based_on="AllElements",
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
    based_on="AllElements",
    item_limits={"All elements": 235.0}
)

output.filter1 = pmx.results.field_outputs.create_median_filter(
    source="SurfaceNodes",
    number_of_layers=2
)
output.filter2 = pmx.results.field_outputs.create_smooth_filter(
    number_of_iterations=3
)

filtered_output = result.field_outputs.add(output)
```

Different limits by part:

```python
output = result.field_outputs.create_field_output_limit(
    name="Stress_limits_by_part",
    field_name="STRESS",
    component_name="MISES",
    based_on="Parts",
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
    based_on="AllElements",
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
outputs.add_equation('SEQV2', 'STRESS.MISES * 2')

# The same thing in two steps, which still works
draft = outputs.create_field_output_equation('SEQV3', 'STRESS.MISES * 3')
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
| `based_on` | `"Parts"`, `"ElementSets"`, or `"AllElements"`. |
| `item_limits` | Draft limit values as a `dict[str, float]` keyed by item name; it goes back into `item_limits` of `add_limit`. |

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
| `item_type` | `limit`, `envelope`, `equation` or `coordinate_system_transform`, named after the `add_*` method that makes it. |
| `filter1` | First `FieldOutputFilter`, or `None` when disabled. |
| `filter2` | Second `FieldOutputFilter`, or `None` when disabled. |

### `FieldOutputLimit`

Limit-based field output returned by `add(output)` or collection lookup.

| Member | Description |
| --- | --- |
| `name` | Field output name. |
| `field_name` | Source field name. |
| `component_name` | Source component name. |
| `based_on` | `"Parts"`, `"ElementSets"`, or `"AllElements"`. |
| `item_limits` | Limit values as a `dict[str, float]` keyed by item name; it goes back into `item_limits` of `add_limit`, `replace_limit` or `create_field_output_limit`. |

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
| `contains(id)` | Checks whether the step exists. |

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
| `contains(id)` | Checks whether the increment exists. |

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
| `contains(name)` | Checks whether the field exists. |

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
| `contains(name)` | Checks whether the component exists. |

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
| `ids` | Node IDs as an `IntList`. |
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
| `ids` | Node IDs as an `IntList`. |
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
| `get_ids()` | Returns node IDs as `IntList`. |
| `get_coordinates()` | Returns undeformed node coordinates. |

### `NodeData`

Undeformed node coordinate data.

| Member | Description |
| --- | --- |
| `ids` | Node IDs as an `IntList`. |
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
| `contains(id)` | Checks whether the element exists. |
| `get_node_ids(element_id)` | Returns node IDs for one element as `IntList`. |

### `Element`

Single mesh element.

| Member | Description |
| --- | --- |
| `id` | Element ID. |
| `type_name` | Element type name. |
| `dimension` | Element dimension. |
| `get_node_ids()` | All node IDs of the element in PrePoMax element order, as `IntList`. |
| `node_count` | Number of element nodes. |

Example:

```python
element = result.mesh.elements[10]
element.get_node_ids()
```

### `ElementData`

Bulk element connectivity data.

| Member | Description |
| --- | --- |
| `ids` | Element IDs as an `IntList`. |
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
node_ids = result.mesh.elements[element_id].get_node_ids()
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
| `contains(name)` | Checks whether the part exists. |
| `hide(part_names)`, `show(part_names)` | Result part visibility. A name or a list of names, in any case. |
| `set_color(part_names, color)`, `reset_color(part_names)` | The result part colour. An alpha below 25 is refused, as in the dialog. |
| `set_opacity(part_names, opacity)` | The alpha channel of the result part color, from 25, the most transparent value the dialog allows, to 255 for opaque. |
| `set_representation(part_names, result_representation)` | Turns the result representation on or off. |
| `replace_properties(old_name, name=None, color=None)` | Replaces the part properties. A property that is not given is kept. A color with an alpha below 25 is refused. |
| `rename(old_name, new_name)` | Renames a result part using command history. Only a part the result was read with can be renamed; use `replace_properties` for any other. |
| `merge(part_names)` | Merges result parts into one. |
| `remove(names)` | Removes result parts. |

The commands that change result parts (`replace_properties`, `set_representation`, `merge`, `remove`) act on the current
result, so they are refused for a result that is not the current one.

### `ReferencePoints` and `CoordinateSystems` of the result mesh

Reached as `result.mesh.reference_points` and `result.mesh.coordinate_systems`. Unlike the read-only
result regions below, they are edited through the results side of the command history, because a
result field transformation is expressed in a result coordinate system.

| Member | Description |
| --- | --- |
| `len(collection)`, iteration, `get_names()` | The collection has a length. Iterating yields `ReferencePoint` or `CoordinateSystem` items, and `get_names()` gives the names as a `StringList`. |
| `collection[name]`, `get(name)` | The item with that name. |
| `first()`, `last()` | The first or the last item. |
| `reference_points.add(name, coordinates, name_visible=None, color=None)` | Adds a result reference point. Every `add*` and `replace*` returns the item. |
| `reference_points.add_from_selection(name, created_from, selection, name_visible=None, color=None)` | Adds one from selected result mesh points. The mode is `OnPoint`, `BetweenTwoPoints` or `CircleCenter`. |
| `reference_points.add_from_region(name, created_from, region_type, region_name, name_visible=None, color=None)` | Adds one from a named result region. The mode is `CenterOfGravity` or `BoundingBoxCenter`, the region type `NodeSetName` or `SurfaceName`. |
| `reference_points.replace(old_name, name=None, coordinates=None, name_visible=None, color=None)` | Replaces one. The name and the display values may be left out and are kept. The coordinates are the whole definition, so although the signature defaults them to `None` they are required and a replace without them is refused; applying them turns the point back into a coordinate point. |
| `reference_points.replace_from_selection(old_name, name=None, created_from=None, selection=None, name_visible=None, color=None)` | Replaces one by selected points. The mode and the selection are the whole definition. |
| `reference_points.replace_from_region(old_name, name=None, created_from=None, region_type=None, region_name=None, name_visible=None, color=None)` | Replaces one by a named region. The mode and the region are the whole definition. |
| `coordinate_systems.add(name, origin, point_on_x, point_in_xy, coordinate_system_type=None, name_visible=None)` | Adds a result coordinate system. |
| `coordinate_systems.add_from_selections(name, origin_created_from=None, origin_selection=None, point_on_x_created_from=None, point_on_x_selection=None, point_in_xy_created_from=None, point_in_xy_selection=None, origin=None, point_on_x=None, point_in_xy=None, coordinate_system_type=None, name_visible=None)` | Adds one whose points may be selected on the result mesh. Each point takes either coordinates or a mode and a selection, and a point that is not given keeps the default coordinates. The arguments are in the order of the model's `add_from_selections`. |
| `coordinate_systems.replace(old_name, name=None, origin=None, point_on_x=None, point_in_xy=None, coordinate_system_type=None, name_visible=None)` | Replaces one. The three points are the whole definition. |
| `coordinate_systems.replace_from_selections(old_name, name=None, origin_created_from=None, origin_selection=None, ...)` | Replaces one the way `add_from_selections` builds it. A result replacement re-specifies the whole definition, so every point is required. |
| `duplicate(names)`, `remove(names)`, `hide(names)`, `show(names)` | The shared verbs. |
| `rename(old_name, new_name)` | Renames a result reference point or coordinate system using command history. |
| `contains(name)` | True when the result mesh has a reference point or coordinate system of that name, regardless of case. |

`add` and `add_from_selections` on `coordinate_systems` create the system in the plane of a two dimensional model, as the
dialog does.

A result `ReferencePoint`:

| Member | Description |
| --- | --- |
| `name` | The reference point name. |
| `created_from` | How the point was created: `Coordinates`, `OnPoint`, `BetweenTwoPoints`, `CircleCenter`, `CenterOfGravity` or `BoundingBoxCenter`. |
| `name_visible`, `color`, `visible` | Whether the name is drawn next to the symbol, the symbol colour as a plain list `[R, G, B, A]`, and the visibility. |
| `get_coordinates()` | The coordinates computed on the result mesh, as `[x, y, z]`. |
| `get_equations()` | The coordinates entered as expressions, under the argument `coordinates` as a list `[x, y, z]` that goes back into `add` and `replace`; a coordinate entered as a plain number stays a number in the list. Coordinates entered as plain numbers, or computed from a selection, are left out; `get_coordinates()` returns them. |
| `get_construction()` | How the point is built, under `created_from`, `region_type` and `region_name`. `region_type` and `region_name` are `None` unless `created_from` is `CenterOfGravity` or `BoundingBoxCenter`, the modes `add_from_region` takes. |

A result `CoordinateSystem`:

| Member | Description |
| --- | --- |
| `name` | The coordinate system name. |
| `coordinate_system_type` | `Rectangular` or `Cylindrical`. |
| `name_visible`, `visible` | Whether the name is drawn next to the symbol, and the visibility. |
| `get_origin()`, `get_point_on_x()`, `get_point_in_xy()` | The three defining points computed on the result mesh, each as `[x, y, z]`. |
| `get_equations()` | The points entered with expressions, under the arguments `origin`, `point_on_x` and `point_in_xy`, each a list `[x, y, z]` that goes back into `add` and `replace`; a coordinate entered as a plain number stays a number in the list. A point entered as plain numbers, or computed from a selection, is left out; `get_origin()`, `get_point_on_x()` and `get_point_in_xy()` return it. |
| `get_construction()` | How each point is built, under the arguments `origin_created_from`, `point_on_x_created_from` and `point_in_xy_created_from` of `add_from_selections` and `replace_from_selections`: `OnPoint`, `BetweenTwoPoints` or `CircleCenter` for a point computed from a selection, and `None` for a point given by its coordinates. |

### `Part`

Single mesh part.

| Member | Description |
| --- | --- |
| `name` | Part name. |
| `get_node_ids()` | Node IDs belonging to the part, as `IntList`. |
| `get_element_ids()` | Element IDs belonging to the part, as `IntList`. |
| `get_node_coordinates()` | The undeformed coordinates of the part nodes with shape `(node_count, 3)`, in the order `get_node_ids()` reports them. |
| `volume`, `center_of_mass` | Mesh-computed part measurements, the center of mass as `[x, y, z]`, with CAD used only when a mesh value was missing. |
| `surface_area` | The total surface area. A mesh computes an area only for a shell part, so a solid reports the area of the CAD part it was meshed from, and `0.0` when there is none. The area of the mesh itself is the sum of `get_face_data().areas`. |
| `get_bounding_box()` | The mesh-computed `[xMin, xMax, yMin, yMax, zMin, zMax]` bounds as a `list[float]`. |
| `get_inertia_at_center_of_mass()`, `get_inertia_at_origin()` | The 3 by 3 inertia matrices as three rows, or an empty list when unavailable. They are second moments of volume with a density of 1, in model length units to the fifth power, exactly as on a model part. A mesh computes none, so these are the matrices of the CAD part the mesh came from and an imported mesh has none. |
| `get_face_data()`, `get_edge_data()`, `get_vertex_data()` | Result-mesh topology using the same `FaceData`, `EdgeData` and `VertexData` objects described for model parts. |
| `get_geometry_metadata()` | Supplemental CAD metadata transferred through the model mesh into the result. |

### Read-only result regions

Node sets, element sets, and surfaces are copied from the model into the result mesh. They can be inspected and
used by result history outputs, but they cannot be created, changed, or removed through the results API.

The `node_sets`, `element_sets`, and `surfaces` collections support indexing by name, `len()`, `get_names()`,
`first()`, `last()`, and `contains(name)`.

| Object | Members |
| --- | --- |
| `NodeSet` | `name`, `count`, `get_node_ids()` (an `IntList`) |
| `ElementSet` | `name`, `count`, `get_element_ids()` (an `IntList`) |
| `Surface` | `name`, `get_face_ids()` and `get_node_ids()` (`IntList`s), `area` |

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

A user-defined history output is found regardless of case, never with surrounding spaces, and every command is
given its stored name. A history output read from the solver files is keyed by its exact name: that name
is found first, and otherwise the one history output whose name differs from it only in case.

| Member | Description |
| --- | --- |
| `len(result.history_outputs)` | Number of history outputs. |
| `result.history_outputs[history_output_name]` | Gets a history output by name. |
| `history_output_name in result.history_outputs` | Checks whether the history output exists. |
| `get_names()` | Returns history output names as `StringList`. |
| `first()` | Returns the first history output. |
| `last()` | Returns the last history output. |
| `contains(name)` | Checks whether the history output exists. |
| `create_history_output_from_field(name, field_name, component_names, region)` | Creates a From Field draft. The region is a `Selection`, a `Region` (`NodeSetName` or `SurfaceName`), a result `NodeSet` or a result `Surface`. |
| `create_history_output_from_equation(name, equation, unit="/")` | Creates a From History Output by Equation draft. |
| `create_history_output_from_integration(name, integral_type, region, field_name=None, component_names=None, integral_result="Geometry", deformation_variable_name="Displacements", unit="/", cross_section="None", thickness=None)` | Creates a line, surface, or volume integration draft. The `region` is required: a `Selection`, a `Region`, a result `ElementSet` or a result `Surface`, as the table below allows. `integral_type` is `"Line"`, `"Surface"` or `"Volume"`. `integral_result` is `"Geometry"` (region size), `"Field"` (field integral), or `"FieldByGeometry"` (region average of the field); `field_name` and `component_names` are required for `"Field"` and `"FieldByGeometry"` and refused for `"Geometry"`. `cross_section` (`"None"`, `"Thickness"`, or `"Axisymmetric"`) is the out-of-plane scaling of 2D output; a line and a volume integral take it on a 2D and a 3D result alike, as the dialog does, and a surface integral is never scaled and refuses any value but `"None"`. `thickness` is taken only with the `"Thickness"` cross section, where it defaults to 1.0, and refused otherwise. |
| `create_history_output_from_merged_histories(name, source1, source2, source3=None, source4=None)` | Creates a From Merged Histories draft that merges two to four history components into one history output. |
| `create_history_output_from_extracted_entry(name, source, entry_name)` | Creates a From Extracted Entry draft that copies one named entry of a history component into its own history output. |
| `create_history_output_xy(name, x_source, y_source)` | Creates an XY draft that plots the entries of the Y history component against the values of the X history component. |
| `create_history_output_from_table(name, data_points, y_column_names=None, x_axis_unit="/", y_axis_unit="/")` | Creates a From Table draft from user-defined data. Every row holds the X value followed by one value per Y column. |
| `create_minimum_filter(return_value="Column")` | Creates a minimum filter; `return_value` is `"Row"` or `"Column"`, in any case. |
| `create_maximum_filter(return_value="Column")` | Creates a maximum filter; `return_value` is `"Row"` or `"Column"`, in any case. |
| `create_sum_filter(operate_on="Columns")` | Creates a sum filter; `operate_on` is `"Rows"` or `"Columns"`, in any case. |
| `create_average_filter(operate_on="Columns")` | Creates an average filter; `operate_on` is `"Rows"` or `"Columns"`, in any case. |
| `add(output)` | Validates and adds a draft using command history. |
| `replace(old_name, output)` | Validates and replaces a result history-output definition. The draft must be of the type the replaced output is made by, the same as for every `replace_*`; otherwise it is refused naming the `add_*` that made the old one. |
| `rename(old_name, new_name)` | Renames a user-defined result history output using command history. Solver-generated history outputs cannot be renamed. |
| `duplicate(names)` | Duplicates user-defined history outputs using command history. A name or a list of names. Solver-generated history outputs cannot be duplicated. |
| `remove(names)` | Removes history outputs using command history. A name or a list of names. |
| `remove_fields(history_output_name, field_names)` | Removes fields from a history output using command history. |
| `remove_components(history_output_name, field_name, component_names)` | Removes components from a history-output field using command history. |
| `export_to_csv(file_name, history_output_names, delimiter=",")` | Exports all fields and components of the selected history outputs to CSV using command history. |

The `name` of every draft is checked as a model item name is, when the draft is created: an empty name, one with
leading or trailing spaces or with characters a name cannot hold is refused.


Creation examples:

```python
result = pmx.results.current

nodes = pmx.create.selection_by_ids(
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
output.filter1 = result.history_outputs.create_average_filter(operate_on="Rows")
output.filter2 = result.history_outputs.create_maximum_filter(return_value="Row")
result.history_outputs.add(output)
```

Existing result regions, and a `Region` made by `pmx.create.region`, are passed directly through the same `region` argument. A `Region` is resolved by name against the result mesh, and a `PartName` region is refused:

```python
output = result.history_outputs.create_history_output_from_field(
    name="Node_set_DISP",
    field_name="DISP",
    component_names=["U1"],
    region=result.mesh.node_sets["Node_Set-1"]
)
result.history_outputs.add(output)
```

An equation names a history value as `HISTORY_OUTPUT.FIELD.COMPONENT`, for example the `U1` component
of the `DISP` field of the history output made above:

```python
output = result.history_outputs.create_history_output_from_equation(
    name="Node_set_DISP_x2",
    equation="=Node_set_DISP.DISP.U1 * 2",
    unit="mm"
)
result.history_outputs.add(output)
```

Line integration integrates a field over geometry edges (works for both shell and solid parts).
For 2D output (plane stress/strain or axisymmetric) set `cross_section` to apply the out-of-plane
scaling; `"Thickness"` additionally uses the `thickness` argument, while `"Axisymmetric"` scales by
`2*pi*r` (the radial coordinate is the X coordinate):

```python
edges = pmx.create.selection_by_ids(
    view="results",
    target="edges",
    source="edges",
    ids={1: [3, 4]}
)

output = result.history_outputs.create_history_output_from_integration(
    name="Line_Force",
    integral_type="Line",
    field_name="STRESS",
    component_names=["S11"],
    region=edges,
    integral_result="Field",
    cross_section="Thickness",
    thickness=0.01,
    unit="N"
)
result.history_outputs.add(output)
```

`integral_result` selects what the integral evaluates: `"Geometry"` (the region size, integrating a unit
field; the default), `"Field"` (the field integral, as in the example above), or `"FieldByGeometry"` (the region
average of the field). `"Geometry"` and `"Field"` report one entry per element, so a Sum filter gives the total size
and the region total; `"FieldByGeometry"` instead sums the element integrals and the element measures
over the whole region and divides the sums, so it returns a single `REGION` entry that is already the
measure-weighted average (no Sum filter needed). `field_name` and `component_names` are refused for
`"Geometry"`, which integrates no field, and read back as `None` there. The geometry result is reported in a single `SIZE` component; note that the cross section
scaling changes its dimension, so a scaled line integral returns an area and a scaled surface integral a
volume:

```python
faces = pmx.create.selection_by_ids(
    view="results",
    target="surfaces",
    source="surfaces",
    ids={1: [3]}
)

output = result.history_outputs.create_history_output_from_integration(
    name="Surface_area",
    integral_type="Surface",
    region=faces,
    integral_result="Geometry",
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
| From Field | All | `"nodes"` | Node set or surface, or a `NodeSetName` or `SurfaceName` `Region` |
| From Integration | `"Line"` | `"edges"` | None (selection only) |
| From Integration | `"Surface"` | `"surfaces"` | Surface, or a `SurfaceName` `Region` |
| From Integration | `"Volume"` | `"elements"` | Element set, or an `ElementSetName` `Region` |
| From Equation | All | None | None |
| From Merged Histories | All | None | None |
| From Extracted Entry | All | None | None |
| XY | All | None | None |
| From Table | All | None | None |

Possible `deformation_variable_name` values are `"Displacements"`, `"Forces"`, `"SurfaceNormals"`, `"WearDepths"`, `"MeshDeformation"`, `"DispDef"` and `"DispDefDepth"`, in any case; they read back in this spelling. `DispDef` is the deformation the user interface lists as `Disp&Def`, and `DispDefDepth` the one it lists as `Disp&Def&Depth`.

The history outputs have the same single call counterparts: `add_from_field`, `add_from_equation`,
`add_from_integration`, `add_from_merged_histories`, `add_from_extracted_entry`, `add_xy`
and `add_from_table`, each with a matching `replace_*`, and each taking the optional `filter1` and `filter2` as
well. `add_from_integration` takes `region` as a required argument after `integral_type`, the way the draft does.
A source of `add_from_merged_histories`, `add_from_extracted_entry` and `add_xy` is a `HistoryComponent` or a
list of three names, as for the drafts; the names are matched regardless of case and stored in the spelling of
the result. `add_from_field` and `replace_from_field` take the same `region` as the draft and
also take the options of the From Field draft - `complex_result_type`, `complex_angle_deg`, `step_id`, `increment_id`,
`harmonic` and `output_node_coordinates` - and an option that is not given keeps the default of the draft. The
dialog shows `complex_result_type` only for a result with complex results, `complex_angle_deg` only at `RealAtAngle`
and `harmonic` only for complex results at `Real` with a single `step_id` and `increment_id`, so the definition that
is added - by these calls or from a draft - is refused naming the option outside that state: a type other than
`Real`, a nonzero angle, or `harmonic=True`. The draft form is kept here as well, because a
history output draft carries a lot of state and is sometimes easier to build in steps.

### `HistoryOutputFilter`

Immutable Python filter specification assigned to `filter1` or `filter2` on any history-output draft.
Filters are applied in order, and `filter2` cannot be used without `filter1`.

| Member | Description |
| --- | --- |
| `type` | `"Minimum"`, `"Maximum"`, `"Sum"`, or `"Average"`. |
| `return_value` | `"Row"` or `"Column"` for a minimum or maximum filter, the `return_value` of `create_minimum_filter` and `create_maximum_filter`; `None` for a sum or average filter. |
| `operate_on` | `"Rows"` or `"Columns"` for a sum or average filter, the `operate_on` of `create_sum_filter` and `create_average_filter`; `None` for a minimum or maximum filter. |

### `HistoryOutputDraft`

Base object for history-output definitions that have not yet been added to the result.

| Member | Description |
| --- | --- |
| `name` | History output name. |
| `region` | The selection, the named `Region` or the read-only result mesh region the output uses, read back as it was given; `None` for the equation, merged, extracted, XY, and table drafts. |
| `filter1` | Optional first `HistoryOutputFilter`. |
| `filter2` | Optional second `HistoryOutputFilter`; requires `filter1`. |

### `HistoryOutputFromMergedHistoriesDraft`

From Merged Histories draft. Adds to `HistoryOutputDraft`:

| Member | Description |
| --- | --- |
| `sources` | Merged source components, a plain Python list of `StringList` objects `[history_output_name, field_name, component_name]` - the form `source1` to `source4` take, so it can be passed back as `*sources`. Names may contain dots. |

### `HistoryOutputFromExtractedEntryDraft`

From Extracted Entry draft. Adds to `HistoryOutputDraft`:

| Member | Description |
| --- | --- |
| `source` | Source component as a `StringList` `[history_output_name, field_name, component_name]`, the form the `source` argument takes. Names may contain dots. |
| `entry_name` | Name of the extracted history entry. |

### `HistoryOutputXYDraft`

XY draft. Adds to `HistoryOutputDraft`:

| Member | Description |
| --- | --- |
| `x_source` | X component as a `StringList` `[history_output_name, field_name, component_name]`. |
| `y_source` | Y component as a `StringList` `[history_output_name, field_name, component_name]`. |

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
outputs return this base object. A From Affine Fit output is made in the user interface only: the API reads it but
offers no way to add or replace one, and a session journal reports such a command as not emitted.

| Member | Description |
| --- | --- |
| `name` | History output name. |
| `fields` | History fields in the output. |
| `is_user_defined` | `True` when the output was defined by the user instead of produced by the solver. |
| `item_type` | `from_field`, `from_equation`, `from_integration`, `from_merged_histories`, `from_extracted_entry`, `xy` or `from_table`, named after the `add_*` method that makes it, `from_affine_fit` for a From Affine Fit output made in the user interface, which no `add_*` method makes, or `None` for a solver-generated output. |
| `filter1` | First `HistoryOutputFilter`, or `None` when disabled or the output is solver-generated. |
| `filter2` | Second `HistoryOutputFilter`, or `None` when disabled or the output is solver-generated. |

Reading a definition back:

```python
history_output = result.history_outputs["Edge_DISP"]
if history_output.is_user_defined and history_output.item_type == "from_field":
    print(history_output.field_name, list(history_output.component_names), history_output.region_type)
```

### `HistoryOutputFromField`

| Member | Description |
| --- | --- |
| `field_name` | Source field name. |
| `component_names` | Selected field component names. |
| `region_type` | `"Selection"`, `"NodeSetName"`, or `"SurfaceName"`. |
| `region_name` | Region name, or `None` when the region is a selection. |
| `complex_result_type` | `"Real"`, `"Imaginary"`, `"Magnitude"`, `"Phase"`, `"RealAtAngle"`, `"Max"`, `"AngleAtMax"`, `"Min"` or `"AngleAtMin"`. |
| `complex_angle_deg` | Complex result angle in degrees, or `None` when `complex_result_type` is not `"RealAtAngle"`, the only type that uses it. |
| `step_id` | Result step ID, or `None` when all steps are used. |
| `increment_id` | Result increment ID, or `None` when all increments are used. |
| `harmonic` | Whether harmonic oscillations are included. |
| `output_node_coordinates` | `"Off"`, `"Undeformed"`, or `"Deformed"`. |

### `HistoryOutputFromEquation`

| Member | Description |
| --- | --- |
| `equation` | Validated history-output equation. |
| `unit` | User-defined result unit. |
| `component_name` | Generated component name, always `VALUE`. |

### `HistoryOutputFromIntegration`

| Member | Description |
| --- | --- |
| `integral_type` | `"Line"`, `"Surface"`, or `"Volume"`. |
| `integral_result` | `"Geometry"`, `"Field"`, or `"FieldByGeometry"`. |
| `field_name` | Source field name, or `None` for the `"Geometry"` integral result. |
| `component_names` | Selected field component names, or `None` for the `"Geometry"` integral result. |
| `region_type` | `"Selection"`, `"ElementSetName"`, or `"SurfaceName"`. |
| `region_name` | Region name, or `None` when the region is a selection. |
| `deformation_variable_name` | The deformation the region follows, in the spelling `create_history_output_from_integration` takes, such as `SurfaceNormals`. |
| `unit` | User-defined result unit. |
| `cross_section` | `"None"`, `"Thickness"`, or `"Axisymmetric"`. |
| `thickness` | Out-of-plane thickness, or `None` when `cross_section` is not `"Thickness"`. |

### `HistoryOutputFromMergedHistories`

| Member | Description |
| --- | --- |
| `sources` | Merged source components, a plain Python list of `StringList` objects `[history_output_name, field_name, component_name]` - the form `source1` to `source4` take, so it can be passed back as `*sources`. Names may contain dots. |

### `HistoryOutputFromExtractedEntry`

| Member | Description |
| --- | --- |
| `source` | Source component as a `StringList` `[history_output_name, field_name, component_name]`, the form the `source` argument takes. Names may contain dots. |
| `entry_name` | Name of the extracted history entry. |

### `HistoryOutputXY`

| Member | Description |
| --- | --- |
| `x_source` | X component as a `StringList` `[history_output_name, field_name, component_name]`. |
| `y_source` | Y component as a `StringList` `[history_output_name, field_name, component_name]`. |

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
| `contains(name)` | Checks whether the field exists. |

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
| `contains(name)` | Checks whether the component exists. |

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
| `contains(name)` | Checks whether the entry exists. |

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
model item, and on a result reference point or coordinate system, returns the expressions that were entered rather
than the numbers they evaluated to, so a parameter change still reaches the value. A value entered as a plain number
is not an expression and is left out; `get_value` or the property of the same name returns it.

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
| `PythonAPIException` | Exception type used for API errors. Python receives it as the builtin exception class of the same name, carrying only the message (`PythonApiExceptionEncoder`). |
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
      GeometryMetadata
    Create
    Jobs
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
      Interactions
      Keywords
      Materials
      Mesh
      Meshing
      Parts
      Regions
      Sections
      Steps
      Tree
    Results
      Fields
      ResultFieldOutputs
      ResultHistoryOutputs
    Scripts
    Selection
    Test_Scripts
    View

PrePoMax/Python
  Commands
  Engine
  Journal
```

`Test_Scripts` holds the scripts that exercise the API through the batch mode; each one declares in
its header the exit code it expects - 0 for all but `Test_Prompt_Refused.py`, which expects 5 - and a
passing script ends with `TEST PASSED`.

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

`Run_Tests.py` runs the offline checks first, before any script that needs PrePoMax: every `Audit_*.py` script
and the unit tests of the runner. They launch no PrePoMax process, take about a second, count as tests of the run,
and a filter selects them by name like any other test.

`Audit_API_Coverage.py` is the offline structural check. It fails on an `add_*` without its matching `replace_*`
(except the ones made once from other items: `add_compound`, `add_group` and `add_from_quality`), a controller
`*Command` method no gateway calls (except the lifecycle commands and aliases it lists), a command class under
`PrePoMax/Commands` without a `ToPython` override, and a lower case name a journal emitter or a command's
`ToPython` writes that is not a member of the API - a renamed or misspelled member - unless it is one of the
verb prefixes, selection words and argument names it lists. It prints each count and the exceptions it allowed;
`--json <file>` also writes the findings.

`Audit_API_Documentation.py` checks this overview and the `[PythonAPI]` metadata against the C# members. The
`Arguments` string of every member must name the C# parameters in their order, with the same optional-ness; a
string that ends in `and the arguments of add_x` or `and the shared ... arguments` is checked for the arguments
it names, and the `add_x` it refers to must exist. A `Returns` of `str`, `int`, `float`, `bool` or `None` must
agree with the C# return type, both ways. Every signature row of the overview must name a member of the type its
section documents, with only arguments that member takes; a row it cannot place is listed rather than failed.
It exits with 0 only when nothing drifted.

`test_runner_offline.py` checks the runner itself with `python -m unittest test_runner_offline`, launching no
PrePoMax process.

## Notes for documentation generation

- The `[PythonAPI]` attribute descriptions are the best source for generated member documentation.
- `ToString()` implementations provide concise expression output; `print(...)` uses the full display text for arrays and named collections.
- Keep implementation-only methods unmarked by `[PythonAPI]` if they should be hidden from help and autocomplete.
- Prefer documenting collection access with `[...]` because this is the intended Python style.
