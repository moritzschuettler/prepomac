# Layer 3 - the MCP server

Working-level plan for the Layer 3 step of [Python_Integration_Plan.md](Python_Integration_Plan.md).
Layers 0, 1 and 2 are implemented; this is the last step of the Python work.

**The goal.** An AI agent drives **the PrePoMax session the user has open on screen**. The user
watches the model change in the viewport and the tree, keeps working alongside the agent, and can
undo what the agent did through the existing command history editor. An integration that starts its
own hidden instance and keeps a private model is explicitly not what is wanted, however fast it is.

Layer 3 is therefore two pieces:

- **an RPC server inside the running application**, over a named pipe, marshalling every call to the
  UI thread and calling the same Layer 1 API objects the console and `--script` already call;
- **`PrePoMaxMcp.exe`**, a small satellite process that speaks MCP on stdio and relays to the pipe,
  because an MCP client spawns its server and cannot spawn an already running desktop application.

## Baseline

Facts measured in the current tree. They are the reason this layer is small.

- **Layer 1 is the whole surface.** 965 `[PythonAPI]` members over 144 classes, reached from one root
  object `pmx`. Anything the user interface can do, a Python line can do, and every state change goes
  through `Controller.XxxCommand(...)`, so it is recorded, replayable and saved into the `.pmx`.
  Layer 3 needs no new model facing code at all.
- **The `[PythonAPI]` metadata is already a schema.** `BaseAPI.GetPythonMemberHelpLines()` reflects
  `Description`, `Arguments`, `Returns` and `Example` off every decorated member and renders
  `help_text()`. A machine readable `describe` is the same reflection with a different formatter.
- **The engine is already re-entered from three front doors.** `PythonEngineManager` holds one
  `PyDict` scope and an `InteractiveConsole`, and `FrmMain` drives it three ways - `PushLine` for the
  console, `ExecuteScript` for a panel script whose individual API calls are recorded as individual
  commands, and `Controller.RunPythonScriptCommand` for a script recorded as one `CRunPythonScript`.
  Layer 3 is a fourth caller of the same methods, not a new mechanism.
- **`PythonEngine.BeginAllowThreads()` is already called**, so the GIL is not held by the UI thread
  between calls and a background thread can legally acquire it. It must not, though - see the
  threading contract.
- **Layer 2 gives the agent its vocabulary.** `PythonJournalExporter.Export(commands, fileName,
  source)` turns the live history into the script a user would have written. An agent that reads the
  journal of what the user did by hand learns the exact call it should make next, and the round trip
  is already verified against two recorded histories.
- **Nothing in the tree does IPC.** No `NamedPipeServerStream`, `TcpListener` or `HttpListener`
  anywhere. There is no port, no pipe and no service to extend.
- **JSON is write only today.** `CaeGlobals/JsonWriter.cs` is 161 lines and emits; decision D4 of the
  integration plan says to revisit when Layer 3 needs parsing. It does, so a reader is added.
- **The viewport can screenshot itself.** `vtkControl.RenderToPNG(fileName)` uses
  `vtkWindowToImageFilter`. Nothing outside the process can capture the viewport - it is an OpenGL
  child window - so a screenshot tool has to be served from inside, which Layer 3 is.
- **`FrmMain` opens 45 modal dialogs.** A modal loop still pumps messages, so a cross thread `Invoke`
  *succeeds* while a dialog is open, and would run an API command underneath the dialog the user is
  editing. This is the one real correctness hazard of the layer.
- **A satellite console exe has precedent.** `PrePoMaxCmd` (~6 KB launcher) and `GmshCaller` are
  already separate projects in the solution, copied into the output by a post build event.
- **Python 3.12 is bundled**, at `<install>\Python\` with its own `Lib` and `site-packages`, so a
  bundled `.py` helper needs no user installation - but only the standard library is available unless
  a package is added, with the licence bookkeeping that implies.

## Decisions

| # | Decision | Rationale | Reversible? |
|---|---|---|---|
| L3-D1 | The server lives **inside the running application**; the agent never starts its own instance | The measured cost of a batch call is a fixed ~2.7 s of startup, but the real reason is that the model has to change on screen while the user keeps working. A private headless model is useless | n/a |
| L3-D2 | Transport is a **named pipe**, ACL'd to the current user SID, one instance per process | No port, no firewall, no URL ACL reservation, no token to leak, and the operating system does the authentication. An `http://127.0.0.1` front would need Origin validation, a bearer token in a file and a reserved port for the same result | Yes - an HTTP front can be added later onto the same dispatcher |
| L3-D3 | The MCP end is **`PrePoMaxMcp.exe`**, a C# console project in the solution, stdio in and pipe out | An MCP client spawns its server, so a bridge process is unavoidable unless the application serves HTTP. C# needs no new runtime, matches `PrePoMaxCmd`, and reuses the same JSON reader and protocol types as the server end | At 3.7 only |
| L3-D4 | The RPC surface is **ten primitives, not 965 methods**. The workhorse is "run this Python in the live session" | An agent writes Python well and the Layer 1 vocabulary is documented for it. A method per member dispatcher would be a second front door free to drift from the first, which is the risk the integration plan already names, and no MCP client can hold 965 tools | At 3.0 only |
| L3-D5 | An MCP tool that is not a protocol primitive is a **bundled Python script** under `APIs/Python/Scripts/` | The state snapshot, the mesh summary and the result extraction are Layer 1 calls with a JSON print. Writing them in Python means a new tool costs a `.py` file and a descriptor, with no C# change and no risk of a C# reimplementation of an API call | Yes |
| L3-D6 | `exec` records **individual commands** by default, exactly as the Python panel's *Execute* button does. `record='script'` is available per call | The user gets the history he would have got from doing it by hand: editable in `FrmEditCommands`, regenerable, journalable. This is not the flattening that is forbidden - the agent's calls *are* individual actions | Yes |
| L3-D7 | Every handler runs on the **UI thread**, single flight, and refuses rather than queues when the application is not idle | Reentering the model while a modal dialog is open, or while a mesher is running, is the one way this layer can corrupt a session. Refusing is always recoverable; queueing is not | At 3.2 only |
| L3-D8 | The server is **off by default** and enabled by the user, per session, from the user interface | It is an open door onto a live model. Nothing about installing PrePoMax should open it | Yes |
| L3-D9 | Everything the agent runs is **echoed into the Python panel**, and the connection is visible in the status bar | "Drive the live session" includes being able to see what was driven. An invisible agent is indistinguishable from a bug | Yes |
| L3-D10 | A **read only mode** the user can toggle refuses any call that would record a command | Inspection is the common case and the safe one. It also gives a way to let an agent look at a model that is in the middle of being edited | Yes |

Explicitly not done: no true headless server (integration plan D5 stands - `Controller` needs
`FrmMain`), no gRPC (its .NET Framework build is long deprecated), no network transport by default,
and no execution mode that expands a recorded script into commands.

## Architecture

### The processes

```
  Claude Code / Claude Desktop
      |  stdio, MCP JSON-RPC 2.0
      v
  PrePoMaxMcp.exe                      no model state; translates and relays
      |  \\.\pipe\PrePoMax.<pid>, JSON-RPC 2.0, newline framed, ACL: current user
      v
  PrePoMax.exe
      |  PrePoMax/Rpc/  server thread  ->  FrmMain.BeginInvoke  ->  UI thread
      v
  PythonEngineManager  ->  pmx (Layer 1)  ->  Controller.XxxCommand  ->  command history
```

### Where the code goes

```
CaeGlobals/
  JsonReader.cs                 the parsing half of JsonWriter: objects, arrays, strings, numbers,
                                true/false/null, no schema binding beyond a nested dictionary
PrePoMax/Rpc/
  RpcServer.cs                  pipe lifecycle, connection threads, start/stop, discovery file
  RpcSession.cs                 one connection: read a line, dispatch, write a line
  RpcRequest.cs, RpcResponse.cs the JSON-RPC 2.0 envelope and the error codes
  RpcDispatcher.cs              the method table, the single flight lock, the read only check
  RpcUiThread.cs                the marshalling contract of 3.2: guard, heartbeat, cancellable call
  RpcTaskTable.cs               long operations: id, state, start time, result, cancellation
  RpcMethods/                   one file per primitive: Status, Eval, Exec, Script, Describe,
                                History, Screenshot, Task, Shutdown
  RpcDescribeBuilder.cs         [PythonAPI] reflection to a JSON schema
  RpcSessionInfoWriter.cs       %LOCALAPPDATA%\PrePoMax\sessions\<pid>.json
PrePoMax/Settings/
  AutomationSettings.cs         enabled, auto start, read only, echo to panel
PrePoMaxMcp/                    the bridge: McpServer, McpToolCatalog, PipeClient, SessionLocator
  Tools/*.json                  the tool descriptors of 3.8
APIs/Python/Scripts/            get_state.py, get_mesh_summary.py, get_results_summary.py
```

`APIs` gains nothing. The pipe server is application code, so it may reference `PrePoMax`; the
direction `APIs` -> `PrePoMax` is still forbidden and is not needed, because the server calls
`PythonEngineManager`, not the API objects directly.

### The threading contract

This is the part that has to be right. Three mechanisms, all needed:

1. **Single flight.** One lock in `RpcDispatcher`. Two requests never execute at once, and a second
   connection is accepted but serialized. The pipe thread holds the lock; it never holds the GIL.
2. **The idle guard, checked on the UI thread.** A modal loop pumps messages, so `Invoke` returns
   while a dialog is open. The guard is therefore evaluated *inside* the marshalled delegate, where
   it can see the truth: refuse when any `Application.OpenForms` entry is `Modal && Visible`, when a
   long operation is in progress, or when a regeneration is running. The answer is
   `{"status": "busy", "reason": "A dialog is open: Edit load"}` - never a silent wait.
3. **A heartbeat and a cancellable dispatch.** A 200 ms UI thread timer stamps a tick count. Before
   marshalling, the pipe thread reads it: a stale stamp means the UI thread is inside something that
   does not pump, so the request is refused without being queued. When it is fresh, the request is
   sent with `BeginInvoke` and waited on with a timeout, and the delegate checks a cancellation token
   before it does anything - so a request that timed out cannot run afterwards.

The pipe thread touches Python only through the delegate. `Py.GIL()` is acquired on the UI thread,
inside the handler, exactly as `PythonPanel_ExecuteScript` does today.

### The protocol

JSON-RPC 2.0, one object per line, UTF-8, no framing headers. Ten methods:

| Method | Marshalled | Records | Purpose |
|---|---|---|---|
| `ping` | no | no | liveness, protocol version, application version |
| `status` | yes | no | idle or busy and why, current view, model file, unit system, command count, read only |
| `describe` | yes | no | the `[PythonAPI]` schema of a path, one level deep |
| `eval` | yes | no | evaluate an expression, return its `repr` and its JSON form when it has one |
| `exec` | yes | yes | run source in the live session; returns stdout, stderr, the commands recorded |
| `script` | yes | yes | run a bundled script from `APIs/Python/Scripts/` with arguments |
| `history` | yes | no | the command list, and the Layer 2 journal of it on request |
| `screenshot` | yes | no | `RenderToPNG` to a temp file, return the path |
| `task` | no | no | poll, list or cancel a long operation |
| `shutdown` | no | no | stop the server; never closes the application |

Errors use the standard codes plus an application range: `-32001` busy, `-32002` read only, `-32003`
refused prompt (a `PromptRefusedException` from Layer 0's strict mode), `-32004` API exception (a
`PythonAPIException`, with its message verbatim - it is written for a human), `-32005` task not
found.

A response over ~256 KB is written to a temp file and returned as a path, which is the rule the
integration plan already set for bulk arrays.

### Discovery

Each instance writes `%LOCALAPPDATA%\PrePoMax\sessions\<pid>.json` when the server starts - pid, pipe
name, protocol version, application version, model file, start time - and deletes it on stop. The
bridge lists them, drops the ones whose process is gone, and attaches to the only live one. With
several, it requires `--pid` or the `session` argument of a tool call, and says so with the list.

### What the agent gets

The bridge presents these MCP tools. The count is deliberately small.

| Tool | Maps to |
|---|---|
| `list_sessions` | the discovery files |
| `get_status` | `status` |
| `describe_api` | `describe` - the schema of `pmx`, or of any path under it |
| `read_model` | `script get_state.py` - a JSON snapshot of the model tree and its item names |
| `eval_expression` | `eval` |
| `run_python` | `exec` |
| `get_history` | `history`, journal included |
| `run_job` | `exec` of `pmx.jobs[name].run(wait=False)`, then a task id |
| `get_task` | `task` |
| `screenshot` | `screenshot` |

The server's MCP `instructions` string carries the three things an agent cannot guess: that `pmx` is
the root and `describe_api` is how to learn it, that every change is recorded and the user can undo
it from the command history editor, and that a busy answer means the user is doing something and the
call should be retried rather than worked around.

## Steps

Each step is separately buildable and separately verifiable. 3.0 to 3.6 are the application; 3.7 to
3.9 are the bridge and the documentation.

### Step 3.0 - JSON reading and the protocol types

`CaeGlobals/JsonReader.cs`: a recursive descent parser returning `Dictionary<string, object>`,
`List<object>`, `string`, `double`, `bool` and `null`. No schema binding, no attributes, no
reflection - the dispatcher reads the few fields it needs by name. `RpcRequest`/`RpcResponse` wrap it
and `JsonWriter`.

**Verify.** An offline test round trips the protocol envelopes, including a nested object, an escaped
string, an exponent, a negative zero and a malformed document that must produce a `-32700` parse
error rather than an exception.

### Step 3.1 - The pipe server and the settings

`RpcServer` on a background thread: `NamedPipeServerStream` with a `PipeSecurity` granting only
`WindowsIdentity.GetCurrent().User`, `RpcSession` per connection, and `ping`, `status` and `shutdown`
implemented. `AutomationSettings` in the settings container with *Enabled*, *Start with the
application*, *Read only* and *Echo to the Python panel*, its `View*` counterpart, and a menu item
plus a status bar indicator declared in the designer files. Discovery file written and removed.

**Verify.** A standard library Python client in `Test_Scripts` opens the pipe, calls `ping` and
`status`, and a second client is served after the first. The pipe cannot be opened from another
account. Killing the application leaves a stale discovery file that the next `list_sessions` prunes.

### Step 3.2 - The UI thread contract

`RpcUiThread` and `RpcDispatcher` as described above: single flight lock, the idle guard inside the
delegate, the heartbeat timer, `BeginInvoke` with a token and a timeout. `status` reports the guard's
reason verbatim.

**Verify.** Three cases, each run against the live application: a call while an edit dialog is open
returns busy and the dialog is untouched; a call during a mesh operation returns busy without
queueing, and the call that follows the operation succeeds; twenty concurrent calls from two clients
all complete and none interleave.

### Step 3.3 - `eval` and `exec`

Both go through `PythonEngineManager` on the UI thread. `exec` uses the panel path so individual
commands are recorded, or `Controller.RunPythonScriptCommand` for `record='script'`. The response
carries stdout, stderr, the names of the commands recorded, and the elapsed time. `eval` asserts that
no command was recorded and reports it as a warning if one was. Read only mode refuses `exec` with
`-32002` before the source runs. Echo to the Python panel, prefixed so the user can see where the
line came from.

**Verify.** `exec` adds a step and a load; both appear in the tree and the viewport, the history
holds `CAddStep` and `CAddLoad`, Edit - Regenerate reproduces the model, and the Layer 2 journal
emits the two calls. A deliberate `PythonAPIException` returns `-32004` with its message and records
nothing.

### Step 3.4 - `describe`

`RpcDescribeBuilder` reflects `[PythonAPI]` off the object a path resolves to, and emits name, kind,
description, arguments, returns, example, and for a collection its item type and current names. The
path is resolved by evaluating it in the scope, so `pmx.model.steps['Step-1'].loads` describes what
is actually there.

**Verify.** `describe('pmx')` and `describe('pmx.model.steps')` match the corresponding
`Python_API_Overview.md` sections member for member. `Audit_API_Coverage.py` gains a check that every
documented member is reachable through `describe`, so the schema cannot fall behind the API the way
hand maintained documentation can.

### Step 3.5 - State, history and screenshot

`get_state.py` walks the model tree and prints JSON: model properties, unit system, the parts with
their mesh state, the item names per collection, the steps with their contents, the jobs with their
status. `history` returns the command list and, on request, `PythonJournalExporter.Export` of it.
`screenshot` calls `RenderToPNG` into the temp folder and returns the path.

**Verify.** `read_model` on a model built by `Test_Model_Meshing.py` reports every item the script
created. `get_history` on a session recorded in the user interface returns the same journal the
Tools - Export to Python journal action writes. The screenshot is a non blank PNG of the current
viewport.

### Step 3.6 - Long operations

`RpcTaskTable` on the server side, so polling is answered on the pipe thread while the UI thread is
busy. A job is started with `run(wait=False)`, which is the application's normal asynchronous path,
so the user keeps the interface and the solver monitor. `task` polls `JobAPI.status` and returns
`get_output()` on request; cancel calls `kill()`. Meshing stays synchronous - it is refused as busy
while it runs, which is honest.

**Verify.** A job that runs for several minutes: `run_job` returns a task id immediately, `get_task`
tracks `Running` to `OK` while the user rotates the model, and the results open. A cancel kills the
solver and leaves no orphaned `ccx` process.

### Step 3.7 - `PrePoMaxMcp.exe`

A new console project, x64, .NET Framework 4.8, added to the solution and copied into the output by
the `PrePoMax` post build event next to `PrePoMax.com`. It implements the MCP methods a tools only
server needs - `initialize`, `notifications/initialized`, `tools/list`, `tools/call`, `ping` - on
stdio, and nothing else: no model knowledge, no caching, no retries beyond reconnecting a dropped
pipe. Errors map straight through; a busy answer becomes a tool result that says the user is busy,
not a protocol error, so the agent can decide to wait.

**Verify.** `claude mcp add prepomax -- "<install>\PrePoMaxMcp.exe"` connects, `tools/list` returns
the ten tools, and an agent session builds a small model end to end in the running application while
the user watches. With no application running, every tool answers with the "no session" message
rather than failing to start.

### Step 3.8 - The tool descriptors

The ten descriptors, their JSON schemas, and the server instructions. Written once, reviewed against
what an agent actually does wrong in 3.7's session, and kept in `PrePoMaxMcp/Tools/`.

**Verify.** An agent given only the tool list and no prompting builds a static analysis of an
imported STEP file: mesh, material, section, fixed set, load, job, results. Each wrong turn it takes
is a descriptor fix, not a code fix.

### Step 3.9 - Documentation, audit and tests

- `Python_API_Overview.md` gains an *Agent integration* section: how to enable the server, what the
  ten tools do, the busy contract, the read only mode, and how to undo what an agent did.
- `AGENTS.md` gains the rule that a new RPC primitive is a deliberate protocol change and that a new
  tool is normally a bundled script instead.
- `Test_Scripts` gains `Test_Rpc_Server.py`, a standard library client covering every method, the
  error codes, the busy path and the read only path, run against a live instance with the server
  enabled.
- `Audit_API_Coverage.py` gains the `describe` coverage check of 3.4.

**Verify.** `python APIs/Python/Test_Scripts/Run_Tests.py` passes with the new script included, and
`PrePoMax.sln` builds clean in `Debug|x64`.

## Risks

| Risk | Mitigation |
|---|---|
| An agent call reenters the model while a dialog is open | Step 3.2's guard is evaluated on the UI thread, where modal state is visible. Refusing is the default answer, not the exception |
| A queued call runs after its caller gave up | The delegate checks a cancellation token first, and nothing is queued while the heartbeat is stale |
| A background thread deadlocks on the GIL | The pipe thread never touches Python. `BeginAllowThreads` is already in place and the GIL is taken only inside the marshalled handler |
| An agent silently damages the model | Everything is recorded as ordinary commands, so the command history editor removes it and Regenerate rebuilds; read only mode and the panel echo make it visible while it happens |
| The pipe is an open door onto a live model | Off by default, enabled per session, ACL'd to the current user SID, no network transport, and the MCP client asks the user to approve each call |
| A second front door drifts from the first | There is no second front door: `exec` calls the same `PythonEngineManager` the console calls, and the only C# code that knows about model concepts is the screenshot and the journal call |
| The JSON reader becomes a maintenance burden | It parses a protocol we define, not arbitrary documents. If it grows past a few hundred lines, revisit D4 and take the package |
| Two instances confuse the agent | One pipe and one discovery file per pid; the bridge refuses to guess and names the candidates |

## Exit criteria

With PrePoMax open on the desktop and the automation server enabled, an agent can discover the
session, learn the API from `describe_api`, build and mesh a model, run the analysis, read results
back, and screenshot the viewport - while the user watches it happen in his own window, keeps
rotating and inspecting the model, and can delete any of it from the command history afterwards.
