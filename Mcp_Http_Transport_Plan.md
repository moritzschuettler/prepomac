# Moving the MCP endpoint into PrePoMax

Working-level plan for retiring `PrePoMaxMcp.exe` and serving MCP from inside the application over
loopback HTTP. It continues [Python_API_Layer3_Plan.md](Python_API_Layer3_Plan.md), whose decision
L3-D2 named an `http://127.0.0.1` front as the reversible alternative to the named pipe and whose
L3-D3 named the bridge as reversible at step 3.7. Both are being taken.

**The goal.** Nothing that serves an agent may outlive the application it serves. The agent client
connects to a URL that answers only while PrePoMax is open, instead of spawning a process that
answers for the rest of the agent session whether PrePoMax is open or not. Everything Layer 3
established stays exactly as it is - the tool surface, the busy contract, the read only mode, the
command recording, the pipe - because none of it is what is wrong.

## Why the bridge has to go

`PrePoMaxMcp.exe` is registered in the agent client's configuration as a **stdio** MCP server, so the
client starts it, one process per agent session, and ends it by closing stdin. `McpServer.Run` exits
on that EOF and on nothing else. It binds to a PrePoMax session *late*: `GetClient` and
`ResolveOnlySession` resolve one per tool call through `SessionLocator` and a named pipe, and when
none is listening `GetNoSessionMessage` returns a hint instead of quitting. That late binding is
deliberate and it is right - an agent session outlasts any single PrePoMax session - but it means the
bridge is alive while nothing it can talk to is.

The consequence is a build failure that names the wrong file:

1. `PostBuildEvent` in `PrePoMax.csproj` copies `PrePoMaxMcp.exe` into `$(TargetDir)`, and
   `PrePoMaxMcp/App.config` probes `privatePath="lib;libs"`, because that is where `PrePoMax.exe`
   keeps its assemblies.
2. `PrePoMaxMcp.csproj` project-references `CaeGlobals`, and references nothing else in the solution.
   So an idle bridge holds exactly one solution assembly open: `$(TargetDir)lib\CaeGlobals.dll`.
3. The `AfterBuild` target moves `$(OutputPath)*.dll` into `$(OutputPath)lib`, which means deleting
   the destination first. Windows permits renaming a loaded image but not deleting one, so that
   delete fails, MSBuild's `Move` task swallows it, and the `File.Move` behind it reports
   `MSB3677: Unable to move file ... Cannot create a file when that file already exists`.
4. Only `CaeGlobals.dll` is ever named, because it is the only assembly any bridge loads.

With PrePoMax closed, every open agent session still holds one of these. A build-side workaround was
written and deliberately reverted: the process lifetime is the defect, and hardening the build
against it would only hide the next symptom. **Do not reintroduce it.** Until this plan lands, a
build in that state needs the bridge processes closed first.

Two lifetime fixes inside `PrePoMaxMcp` were considered and rejected. Exiting once the attached
session closes, and exiting whenever no PrePoMax is running, both mean that closing PrePoMax kills
the tools for the rest of the agent session, because a client will not respawn a stdio server on its
own. The transport is what has to change, not the bridge's exit condition.

## Baseline

Facts measured in the current tree and on this machine. They are the reason this change is small.

- **Loopback prefixes bind without elevation.** Verified non-elevated:
  `http://127.0.0.1:8756/mcp/` binds, `http://localhost:8756/mcp/` binds, both on one `HttpListener`
  bind together, and only the wildcard `http://+:8756/mcp/` fails with *Access is denied*. So no
  administrator rights and no `netsh http add urlacl` reservation are needed, provided the prefix is
  loopback-only. A `TcpListener` on `127.0.0.1` binds as well.
- **PrePoMax is portable only.** There is no installer, so there is no install step for a URL ACL
  reservation, a firewall rule or a service registration to happen in. This is not a preference the
  plan expresses; it is a hard constraint, and it is what makes the loopback-only prefix the only
  admissible binding rather than merely the safest one. The application runs from whatever folder the
  user unzipped it into, and the user may keep several versioned folders side by side.
- **`settings.bin` lives in the portable folder.** `SettingsContainer.GetDefaultFileName` resolves it
  under `Application.StartupPath` unless `--settings` overrides it, and `--noSettingsSave` suppresses
  writing it. So anything the settings hold is scoped to that portable copy, and a run that may not
  write them cannot persist anything new.
- **`%LocalAppData%\PrePoMax\sessions\` is the shared, per-user state**, and the only such state this
  feature has. Every portable copy the user runs writes into the same folder, one file per process id,
  which is exactly why discovery lives there and not beside the executable.
- **`HttpListener` needs no package.** It is `System.Net.HttpListener` in `System.dll`, already
  referenced. `packages.config` is untouched by this plan.
- **The dispatcher is already transport-agnostic in everything but its entry point.**
  `RpcDispatcher.Dispatch(RpcRequest)` composes an envelope around a handler that returns a JSON
  string; the single-flight lock, the idle guard, the read only block and the busy state all sit
  inside it. Only `Dispatch`'s signature ties it to a parsed protocol line.
- **The bridge is 1217 lines over 10 files, and 490 of them exist only to reach another process.**
  `PipeClient` (149), `SessionLocator` (118), `SessionInfo` (55), `JsonValuePrinter` (70) and
  `Program` (98) are transport and discovery. `McpToolCatalog` (274), `McpServer` (284),
  `McpModelScript` (100), `McpTool` (44) and `McpException` (25) are the tool surface, and that is
  what moves.
- **Nothing in the tool surface streams.** All ten tools are request/response, no tool reports
  progress, the catalog is static, and the long-call problem is already answered by the busy contract
  and `AutomationSettings.RequestTimeout` rather than by a stream.
- **The discovery file already exists and already has a reader.** `RpcSessionInfoWriter` writes
  `%LocalAppData%\PrePoMax\sessions\<pid>.json` on start and `Remove()`s it on stop;
  `SessionLocator.GetSessions` reads the folder and prunes a file whose process is gone. The reader
  is on the wrong side of the process boundary, not missing.
- **The session file's folder is inside the user's profile.** Anything running as this user can read
  it. That is the same trust boundary the pipe's ACL already draws, and it bounds what a token in
  that file can be claimed to protect.
- **The application already carries the settings, the menu item and the indicator.**
  `AutomationSettings` with `ViewAutomationSettings`, `FrmMain.StartAutomationServerIfConfigured` /
  `StartAutomationServer` / `StopAutomationServer` / `ToggleAutomationServer`, the
  `tsmiAutomationServer` menu item, the `tsslAutomation` status label and the `--rpc On|ReadOnly|Off`
  command line option are all in place and all transport-neutral.
- **`8756` is free here** and carries no registered service; nothing else the user runs claims it.

## Decisions

| # | Decision | Rationale | Reversible? |
|---|---|---|---|
| T-D1 | The MCP endpoint is served **inside `PrePoMax.exe`**, over loopback HTTP. No process serves an agent but the one it drives | The endpoint's lifetime becomes the application's lifetime, which is what removes both the idle bridge and the build lock. An MCP client does not have to spawn an HTTP server, so the reason the bridge existed is gone. It also suits a portable application better than a spawned command does: a URL keeps working when the user moves or updates the folder, while a `command` path in a client configuration silently stops matching | n/a |
| T-D2 | The **named pipe stays**, on the same dispatcher | It is written, tested and documented for a non-MCP client; it needs no token; and it is the fallback if the HTTP front turns out to have a wart. Two fronts over one method table is the shape L3-D2 already sanctioned - the alternative is deleting a working transport to save nothing | Yes |
| T-D3 | Transport is **`HttpListener`**, not a raw `TcpListener` | It parses the request line, the headers and a chunked body, handles keep-alive and `Expect: 100-continue`, and routes by `Host` through its prefixes, so a request whose Host is not loopback never reaches a handler. A `TcpListener` means hand-writing HTTP/1.1 for no gain. Both bind loopback unelevated | Yes |
| T-D4 | **Streamable HTTP only, and only its JSON-response half.** `POST` answered `200 application/json` with one JSON-RPC response; `GET` answered `405` with `Allow: POST`; no `Mcp-Session-Id` issued | Nothing in the surface pushes anything, so the server→client stream would carry no traffic. Keeping every answer a single JSON body is also what makes the optional stdio shim of T-D11 a byte relay rather than a second protocol implementation | Yes - an SSE endpoint is additive |
| T-D5 | The **deprecated 2024-11-05 HTTP+SSE transport** (`GET /sse` plus `POST /messages`) is not served | It is two endpoints and a session table for clients that the two this repository cares about do not need. A client that speaks only that is the case T-D11 exists for | Yes |
| T-D6 | Security is **three gates, all of them**: loopback-only binding, a bearer token, and `Origin`/`Host` validation | This is the MCP specification's own guidance for a local HTTP server, and each gate stops something the others do not. A loopback TCP port, unlike a pipe, is reachable by any local program *and* by any page in the user's browser | No - none of the three may be dropped |
| T-D7 | The token is kept in the **portable folder's `settings.bin`**, is **only ever generated** - never typed or pasted - and is **cleared on the first run in a folder**, detected through the existing `trace.info` marker. There is no command line switch for it | This took three attempts. Per portable folder alone breaks a configured client when a new version is unpacked; per user, in `%LOCALAPPDATA%`, keeps working but leaves a credential outside the folder that deleting the folder does not remove, and PrePoMax keeps everything else beside the executable. What settles it is that `settings.bin` **ships in the redistributable**, so the store must be one the release can be prepared without: the first-run wipe makes a shipped token dead on arrival, and a *Clear the access token* action makes it dead before it ships. Generating rather than accepting a typed value follows Jupyter and Syncthing, which both generate machine credentials; storing it in the clear and showing it follows Syncthing's API key, whose maintainers rejected hashing precisely because the user has to read it back. No switch for the token means an unattended run cannot invent one, so a folder is opened once by a human before it can serve | Yes |
| T-D8 | Default port **8756**, configurable, with a **16-port probe** above it and no random fallback | The single-instance case - nearly every case - always lands on the configured port, so the configured URL is stable. A URL the user cannot predict is a URL they cannot configure, so exhausting the range is a refusal to start, not a surprise port | Yes |
| T-D9 | **One port per instance. The URL is the address.** `list_sessions` becomes a directory, and `process_id` becomes an assertion rather than a route | An in-process endpoint can only answer for its own model. Refusing a `process_id` that does not match, and naming the URL that does, is strictly safer than today, where a mistyped id silently reaches another instance | At T5 only |
| T-D10 | **`PrePoMaxMcp` is deleted**, project and folder | Its reason for existing is answered by the client connecting instead of spawning, its useful classes move in, and its lifetime is the defect. Removing the project removes the class of failure, not this instance of it | Yes |
| T-D11 | If a stdio-only client ever turns up, the answer is a **byte relay**, not a bridge: no JSON parsing, no reference to anything in this solution, and never copied into `$(TargetDir)` | Those two rules are precisely what makes it unable to lock a build output. Written against the bundled Python 3.12 it is a script and there is no binary to place at all | n/a |
| T-D12 | The HTTP front adds an **authentication boundary and nothing else**. Read only mode, the busy guard, the command history, the `exit` refusal and the single-flight lock stay in the dispatcher | A transport that also decides what the surface may do is two places to get one rule right | No |

Explicitly not done: no OAuth (the MCP authorization spec's flows exist for remote servers with a
user agent; a loopback endpoint with a file-published token is what a local server is told to do), no
CORS headers of any kind, no non-loopback prefix, no URL ACL reservation, and no change to the ten
tools or to what any of them does.

## Architecture

### Before and after

```
  before                                     after

  agent client                               agent client
      |  stdio, spawned per session              |  HTTP POST, loopback, bearer token
      v                                          v
  PrePoMaxMcp.exe   <- outlives PrePoMax      PrePoMax.exe
      |  \\.\pipe\PrePoMax.<pid>                 |  PrePoMax/Mcp/     HttpListener thread
      v                                          |  PrePoMax/Rpc/     RpcDispatcher
  PrePoMax.exe                                   v
      |  PrePoMax/Rpc/                        RpcUiThread / RpcBusyState
      v                                          v
  RpcDispatcher -> pmx -> command history     pmx (Layer 1) -> Controller.XxxCommand -> history

                                             \\.\pipe\PrePoMax.<pid> stays, same dispatcher
```

### Where the code goes

```
PrePoMax/Mcp/
  McpHttpServer.cs           HttpListener lifecycle: prefixes, port probing, accept loop, Start/Stop
  McpHttpRequestHandler.cs   one request: method, path, content type, body limit, auth, Origin, Host
  McpAccessToken.cs          generate, store, compare in constant time
  McpOriginPolicy.cs         the empty origin allow list, the Host check, the OPTIONS refusal
  McpProtocolServer.cs       initialize, ping, tools/list, tools/call - from McpServer.cs
  McpToolCatalog.cs          moved; Execute takes an RpcDispatcher instead of a PipeClient
  McpTool.cs                 moved unchanged
  McpModelScript.cs          moved unchanged
  McpException.cs            moved unchanged
PrePoMax/Rpc/
  RpcDispatcher.cs           gains Invoke(method, parameters): the method table without the envelope
  RpcRequest.cs              gains an in-process factory for a method and named arguments
  RpcSessionInfoWriter.cs    also writes url, port and token; restricts the folder ACL
  RpcSessionInfoReader.cs    from PrePoMaxMcp/SessionLocator.cs - the reader joins the writer
  RpcSessionInfo.cs          from PrePoMaxMcp/SessionInfo.cs, plus Url, Port and Token
PrePoMax/Settings/
  AutomationSettings.cs      gains Port and AccessToken
PrePoMaxMcp/                 deleted
```

**What disappears, and why in-process hosting makes it pointless.** `PipeClient` - there is no other
process to connect to. `Program.cs`, `Properties/AssemblyInfo.cs`, `App.config` and
`PrePoMaxMcp.csproj` - there is no process. `JsonValuePrinter` - it existed to re-serialize a
`JsonReader` tree that came back over the pipe, and in process the tool catalog already holds the
JSON string the handler wrote. `SessionLocator`/`SessionInfo` do **not** disappear; they move, because
`list_sessions` still has to read the *other* instances' files, and the reader belongs next to the
writer that produces them.

**The seam.** `RpcDispatcher.Dispatch(RpcRequest)` is split into
`Invoke(string method, Dictionary<string, object> parameters)`, which serializes, guards and runs the
handler and lets `RpcException`, `PromptRefusedException` and `PythonAPIException` out, and
`Dispatch`, which is `Invoke` plus the envelope and the catch chain it has today. The pipe keeps
calling `Dispatch` and behaves identically; the MCP front calls `Invoke` and catches, so no JSON
envelope is ever parsed inside one process. This is the only change to existing `Rpc` behaviour, and
it is a refactor with no new semantics.

### The HTTP surface

One endpoint, `POST http://127.0.0.1:<port>/mcp`.

| Request | Answer |
|---|---|
| `POST`, valid token, JSON body | `200 application/json` with one JSON-RPC 2.0 response object |
| `POST`, body is not JSON | `200` with a `-32700` JSON-RPC error - the protocol was reached, so the failure is the protocol's |
| `POST`, method unknown to MCP | `200` with a `-32601`, exactly as `McpServer.Handle` answers today |
| `POST` of a notification (`notifications/*`, no `id`) | `202 Accepted`, empty body |
| Missing or wrong `Authorization` | `401` with `WWW-Authenticate: Bearer`, a body that says only that a bearer token is required |
| Any `Origin` header present | `403`, no `Access-Control-Allow-*` header ever |
| `OPTIONS` | `403` - a preflight that fails is what stops a browser POST |
| `GET`, `DELETE`, anything else | `405` with `Allow: POST` |
| Body over 4 MB | `413` |
| `Content-Type` not `application/json` | `415` |

An authentication or method failure is an HTTP status because the protocol was never entered; a
protocol failure is a `200` carrying a JSON-RPC error object, which is what a client expects.
`initialize` echoes the client's `protocolVersion` and returns `McpToolCatalog.GetInstructions()`,
unchanged from `McpServer.Initialize`. A missing `MCP-Protocol-Version` header on a later request is
accepted rather than rejected, and no `Mcp-Session-Id` is issued, so the endpoint stays usable by a
client written against either revision.

### Security

The three gates of T-D6, and an honest statement of what they bound.

**Loopback-only binding.** Both `http://127.0.0.1:<port>/mcp/` and `http://localhost:<port>/mcp/` are
registered on one listener - both are needed, because `http.sys` routes by the request's `Host`
header and a client configured with `localhost` would otherwise get a `400` from a `127.0.0.1`-only
prefix. Never `+`, never a machine name: that needs elevation or a `netsh` reservation, which a
portable application has no install step to perform, and it would put the endpoint on the network.
The handler additionally asserts
`HttpListenerRequest.IsLocal` and a loopback `RemoteEndPoint`, so a future prefix mistake cannot open
the endpoint silently.

**A bearer token.** 32 bytes from `RNGCryptoServiceProvider`, base64url-encoded, generated once and
stored in `AutomationSettings.AccessToken`; required on every request as `Authorization: Bearer
<token>`; compared with a fixed-time comparison so a wrong token leaks no prefix. Published in the
session file as `token` next to `url`, so a client that can read the file needs no configuration at
all. Regenerated only from *Tools - Automation Server - Regenerate the access token*, which invalidates
every configuration holding the old one and says so before it does it.

Its scope is the portable folder, because that is where `settings.bin` is. Three consequences to
implement rather than discover: a copy of PrePoMax with no token in its settings generates one on
first start and persists it; a session that may not write settings - `--noSettingsSave`, which
`--script` turns on by default - generates an **ephemeral** token for that run instead of failing or
silently reusing none, and reports it in the output window as well as in the session file, which is
where an agent runner will read it; and two versioned folders have two independent tokens, so a user
who configures a client against one folder and then runs the other gets a `401`, with the endpoint
and token of the running copy visible in its own settings dialog.

What the token is for, stated plainly because the settings dialog will say it too: the session file
lives in the user's own profile, so any program running **as this user** can read it, exactly as any
program running as this user can open the pipe. The token stops **the browser**, which cannot read a
file, and **another local account**, which cannot read this profile. It is not a defence against code
already running as the user, and nothing served over loopback can be.

**`Origin` and `Host` validation.** This is what stops DNS rebinding, and it is the gate that has no
counterpart in the pipe. A page on `http://evil.example` can be made to resolve to `127.0.0.1` and
the browser will then POST to the port - but it must send `Origin: http://evil.example`, and it
cannot read the token. So: a request carrying **any** `Origin` header is refused with `403`, because
a native MCP client sends none; `OPTIONS` is refused; no `Access-Control-Allow-*` header is ever
emitted, so the preflight a cross-origin POST needs never succeeds; and `Host` must be
`127.0.0.1:<port>` or `localhost:<port>`. The origin allow list is written as an **empty** list
rather than left out, so that supporting a browser-based client one day is a list to fill in rather
than a check to invent.

**Request hygiene.** The body is capped at 4 MB - a recorded script can be long, a mesh may not
arrive this way - the content type is required, concurrent connections are capped the way
`RpcServer.MaximumConnections` caps pipe instances, and the dispatcher's single-flight lock is
unchanged, so two clients still cannot interleave halfway through a change.

### The port

- **Default 8756**, in `AutomationSettings.Port`, clamped to 1024-65535 the way `RequestTimeout`
  clamps itself.
- **Where the user sets it:** *Tools - Settings - Automation*, as a `ViewAutomationSettings.Port`
  property with the category, ordered display name, description and `[Id]` its neighbours carry.
  Two read-only properties join it - *Endpoint*, showing the URL currently served, and *Access
  token* - so the values the client configuration needs can be read and copied where the user is
  already standing when they configure this. A `--rpcPort <n>` command line option sits next to the
  existing `--rpc` for a `-g No` session an agent runner starts.
- **When it is taken:** `HttpListener.Start` throws `HttpListenerException` with
  `ERROR_SHARING_VIOLATION`. The server then tries the next 16 ports in turn and takes the first that
  binds, reporting the port it actually took in the output window and in the `tsslAutomation` label.
  If none of the 16 bind it does not start, and it names the range it tried; it does not fall back to
  an arbitrary free port.
- **How it is published:** `RpcSessionInfoWriter.Write` gains `url` (the full
  `http://127.0.0.1:<port>/mcp`), `port` and `token`, keeping `processId`, `pipeName`,
  `protocolVersion`, `applicationVersion`, `modelFileName` and `startTime`. `pipeName` stays because
  the pipe stays.

### Multiple instances

Today one bridge sees every instance and routes per call by `process_id`, with `list_sessions`
enumerating them. An in-process endpoint can only ever answer for its own model, so:

- Each instance binds its own port - the configured one first, then the probe range. The
  single-instance case always lands on the configured port, so the URL in the client configuration is
  stable, and a second instance is the only case that moves.
- **`list_sessions` becomes a directory, not a router.** It reads the sessions folder through
  `RpcSessionInfoReader`, prunes files whose process is gone, and reports each instance with its
  `url`, `port`, `processId`, model file and version, flagging the one that is answering the call
  (`isThisSession`). Its tool description changes accordingly: what an agent does with a second
  session is *tell the user which URL to add*, not switch to it.
- **`process_id` becomes an assertion.** Every tool may still take it. Given and matching, the call
  proceeds; given and not matching, the call is refused with the URL of the instance that does match
  - "this endpoint is process 1234; process 5678 answers on `http://127.0.0.1:8757/mcp`". Today a
  mistyped id silently reaches another instance's model, so this is a safety gain, not a loss.
- A running instance does **not** migrate onto the base port when the instance holding it closes. A
  URL whose meaning changes under a connected client is worse than a URL that is occasionally not the
  base one.
- Rejected, and worth naming: a well-known port on a "front" instance that proxies to its siblings
  over the pipe. It preserves a single configured URL, and it reintroduces exactly the cross-process
  coupling this change exists to remove, with the front instance's lifetime everyone's problem.

### The client configuration

From a spawned command to a URL. The old entry names a path inside the portable folder, so it is
whatever the user unzipped - and it breaks when they move that folder or unzip the next version
beside it:

```json
{ "mcpServers": { "prepomax": { "type": "stdio",
                                "command": "<the folder holding PrePoMax.exe>\\PrePoMaxMcp.exe" } } }
```

```json
{ "mcpServers": { "prepomax": { "type": "http",
                                "url": "http://127.0.0.1:8756/mcp",
                                "headers": { "Authorization": "Bearer <token>" } } } }
```

For Claude Code the migration is two commands:

```bash
claude mcp remove prepomax
```

```bash
claude mcp add --transport http prepomax http://127.0.0.1:8756/mcp --header "Authorization: Bearer <token>"
```

So the user does three things: remove the stdio entry, add the URL entry, and paste the token from
*Tools - Settings - Automation*. Because of T-D7 the token is pasted once and not again - and the new
entry names no path, so unzipping the next version over or beside the current folder leaves the
client configuration correct, which the stdio entry never did.

The exact configuration keys and CLI switches must be re-checked against the client's own
documentation when step T7 is written. Client configuration shapes move faster than this repository,
and the README is where a stale one will be noticed.

## Implementation status - done

All nine steps are implemented. `PrePoMax.sln` builds clean in `Debug|x64`, and the endpoint was
exercised end to end against headless sessions of the built application: 45 checks over the
publication of the address, every security gate, the protocol handshake, all ten tools, two instances
on two ports, the pruning of a dead session's file, and the unreachable address once the application
is closed.

| Step | State | Note |
|---|---|---|
| T0 dispatcher seam | Done | `Invoke` extracted; `Dispatch` is `Invoke` plus the envelope, so the pipe is unchanged |
| T1 session file and reader | Done | `url`, `port` and `token` published; reader and info moved beside the writer; folder ACL verified on both paths |
| T2 listener, settings, interface | Done | Port probe, token resolution, `--rpcPort`, the settings properties and the menu item |
| T3 security gates | Done | Written as one class with T2; every row of the status table measured |
| T4 protocol front | Done | `McpProtocolServer` from `McpServer.cs`, minus stdio and the session plumbing |
| T5 tool catalog in process | Done | `Execute` takes the dispatcher; `JsonValuePrinter` deleted; `list_sessions` a directory; `process_id` an assertion |
| T6 delete the bridge | Done | Project, solution entries, dependency, post-build copies and README bullet gone |
| T7 documentation | Done | Overview, README, `AGENTS.md`, and the superseded note on the Layer 3 plan |
| T8 tests | Done | `Test_Mcp_Http.py` replaces `Test_Mcp_Bridge.py`; `Test_Mcp_Workflow.py` ported to the endpoint |

Deliberate deviations from the plan as written:

- **T-D6, and the `ServeHttp` setting is not there.** The plan gave `AutomationSettings` a switch to
  serve the pipe without the endpoint. It was dropped: after T6 nothing shipped speaks the pipe, so
  the switch's only reachable state is a server that is running and that no client can reach. The
  master switch - *Tools - Automation Server*, off by default - remains the security control it
  always was. A `[Serializable]` settings class also cannot gain a default-true boolean without an
  inverted backing field, and buying that awkwardness for a footgun was the wrong trade.
- **T2's menu is not a submenu, and regenerating is not in the Tools menu at all.** It is in the
  context menu of the automation settings page, beside the token it replaces, which is where a user
  looking at the token expects to find it; the Tools menu keeps only the *Automation Server* toggle.
  A *Copy the endpoint* item was dropped: the settings page shows the address and the token
  read-only, and the output window prints the address on every start, so a clipboard action would be
  a third way to read something already in two places.
- **T0's seam takes an `RpcRequest`, not `(method, parameters)`.** `RpcRequest.Create(method,
  parameters)` builds one in process and `RpcDispatcher.Invoke(request)` runs it, which reuses the
  existing argument accessors instead of duplicating them for a second signature.
- **T3 was written with T2.** The gates are one class, `McpHttpRequestHandler`, and writing half of it
  and then the rest would have meant writing a request handler twice. T3 became its measurement.
- **The settings compatibility hazard was not in the plan.** `AutomationSettings` is
  `[Serializable]` and deserialization does not run the constructor, so an existing `settings.bin`
  would have loaded `Port` as zero. It carries an `[OnDeserialized]` repair, following the one
  `ColorSettings` already uses for the same reason.
- **`--noSettingsSave` suppresses writing, not reading.** A portable copy that already remembers a
  token hands it to such a run too; only a copy that remembers none makes an ephemeral one. Worth
  knowing when reading the tests, whose ephemeral checks need a copy with no `settings.bin`.

Measured on the way, and worth keeping:

- **The sessions folder was genuinely exposed.** Before the ACL tightening, its inherited rules on
  this machine granted `ReadAndExecute` to a *second local group*. The folder now holds the access
  token, so this was not a theoretical gate. Both paths were verified: a folder that does not exist
  is created with a single rule for the current user and inheritance switched off, and a folder that
  already exists inheriting is tightened in place.
- **Both loopback prefixes are needed, and one listener takes both.** Verified non-elevated;
  `http://+:<port>/` is denied, as the plan measured.
- **The regression behaves exactly as described.** With the session closed the address refuses the
  connection - there is no process to answer, and nothing pretends otherwise.

Files added: `PrePoMax/Mcp/McpAccessToken.cs`, `PrePoMax/Mcp/McpHttpServer.cs`,
`PrePoMax/Mcp/McpHttpRequestHandler.cs`, `PrePoMax/Rpc/RpcSessionInfo.cs`,
`PrePoMax/Rpc/RpcSessionInfoReader.cs`, `APIs/Python/Test_Scripts/Test_Mcp_Http.py`. Files moved out
of `PrePoMaxMcp` into `PrePoMax/Mcp`: `McpException.cs`, `McpTool.cs`, `McpModelScript.cs`,
`McpToolCatalog.cs`, and `McpServer.cs` as `McpProtocolServer.cs`. Deleted with the project:
`PipeClient.cs`, `SessionLocator.cs`, `SessionInfo.cs`, `JsonValuePrinter.cs`, `Program.cs`,
`App.config`, `Properties/AssemblyInfo.cs`, `PrePoMaxMcp.csproj`, and
`APIs/Python/Test_Scripts/Test_Mcp_Bridge.py`.

What is left, and cannot be checked from here:

- **A real agent client against the address.** The endpoint was driven by a standard-library client
  that speaks the same protocol, and `initialize`, `tools/list` and every tool answer correctly, but
  the client configuration keys in T7's snippets should be confirmed against the client's own
  documentation the first time someone connects one.
- **The interactive paths.** The settings dialog's new *Endpoint* category, the regenerate-token
  question and the port in the status bar are written but have not been looked at on screen, which
  needs the application running with a window.
- **A second account.** That the token refuses another user, like the pipe ACL before it, is reasoned
  from the profile permissions rather than measured.

## Cross-cutting rules

Every step obeys these; they are not repeated per step.

1. `.NET Framework 4.8`, `LangVersion` 7.3 in `PrePoMax`, no new NuGet package, `packages.config`
   untouched.
2. The eight-line copyright header, padded section banners, 133-column limit, and the near
   comment-free style: a comment says why a non-obvious interop, protocol or security decision exists.
3. Every visible element - the *Tools - Automation Server* submenu items, the status label - is
   declared in `FrmMain.Designer.cs` in the generated style. Nothing visible is built in code after
   `InitializeComponent`.
4. `AutomationSettings` is `[Serializable]` and lives in `settings.bin`. New fields are additive and
   defaulted in `Reset()`; nothing existing is renamed.
5. The dispatcher stays the single implementation. No handler is duplicated for the HTTP front, and
   no model concept enters `PrePoMax/Mcp/` beyond what `McpToolCatalog` already sends as Python.
6. Each step extends `APIs/Python/Test_Scripts` where it has a testable surface, and the step that
   changes the documented protocol updates `Python_API_Overview.md` in the same change.

Build verification for any step:

```bash
"/c/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe" "PrePoMax.sln" -t:Build -p:Configuration=Debug -p:Platform=x64 -nologo -verbosity:minimal -clp:ErrorsOnly
```

Until T6 lands, close every `PrePoMaxMcp.exe` before building, or the `AfterBuild` move fails with
`MSB3677` on `lib\CaeGlobals.dll`.

## Steps

T0 to T5 build the endpoint with the bridge still in place, so the pipe and the bridge keep working
throughout and nothing is unusable between two steps. T6 deletes the bridge once the endpoint has
been exercised. T7 and T8 are documentation and tests.

### Step T0 - The dispatcher seam

**Change.** Split `RpcDispatcher.Dispatch(RpcRequest)` into `Invoke(string method,
Dictionary<string, object> parameters)` - the method table, the `IsSerialized` lock, `Serve` /
`ServeOnUiThread`, the read only block, returning the handler's JSON string and letting its
exceptions out - and `Dispatch`, which is `Invoke` wrapped in the envelope and the existing catch
chain. Add an `RpcRequest` factory for a method plus named arguments, so an in-process caller builds
no JSON line.

**Verify.** `Test_Rpc_Server.py` passes unchanged against a live session: every method, every error
code, the busy path and the read only path. Nothing about the pipe's behaviour may differ, because
nothing about it changed.

### Step T1 - The session file, and the reader moves in

**Change.** `RpcSessionInfoWriter.Write` also writes `url`, `port` and `token`. Move
`SessionLocator` to `PrePoMax/Rpc/RpcSessionInfoReader.cs` and `SessionInfo` to
`RpcSessionInfo.cs`, adding `Url`, `Port` and `Token`; keep the stale-file pruning and the
`Process.HasExited` check that `SessionLocator.IsAlive` already gets right. Restrict the sessions
folder's ACL to the current user when it is created, so an unusual `%LocalAppData%` inheritance
cannot widen it.

**Verify.** Two instances of the same portable folder write two files carrying two different ports and
the same token; two instances of two different folders carry two different tokens as well. The reader
lists all of them whichever folder they came from, because the sessions folder is per user rather than
per copy. Killing one instance leaves a file the next read prunes; the folder's ACL grants only the
current user.

### Step T2 - The HTTP listener, the settings and the user interface

**Change.** `McpHttpServer` on a background thread: both loopback prefixes on one `HttpListener`, the
port probe of T-D8, an accept loop that hands each context to `McpHttpRequestHandler`, and
`Start`/`Stop` driven from `FrmMain.StartAutomationServer` and `StopAutomationServer` alongside the
pipe, so one switch still turns the whole thing on. `AutomationSettings` gains `Port` and
`AccessToken`; `McpAccessToken` generates the token, persists it through the settings when the
settings may be written and keeps it in memory for the run when they may not, per T-D7;
`ViewAutomationSettings` gains *Port*, and read-only *Endpoint* and *Access token*. `--rpcPort` joins `--rpc` in
`CommandLineOptions`, validated in `CheckForErrors`. In `FrmMain.Designer.cs`, *Tools - Automation
Server* becomes a submenu - *Start / stop*, *Copy the endpoint*, *Regenerate the access token* - and
`tsslAutomation` reports the port.

**Verify.** With the server on, a hand-written `POST` carrying the token gets a `200`; without it a
`401`. `netstat -ano` shows the listener bound to `127.0.0.1` only, never `0.0.0.0`. Starting a
second instance takes 8757 and says so in the output window and the status bar. Setting the port to
one already held by an unrelated program moves the endpoint up and reports it; blocking all 16 leaves
the server stopped with the range named. A copy whose `settings.bin` holds no token generates and
persists one on first start; the same copy run with `--noSettingsSave` serves with an ephemeral token
that appears in the output window and the session file, and leaves `settings.bin` untouched. Nothing
is elevated at any point, and the whole feature works from a folder that was only unzipped.

### Step T3 - The security gates

**Change.** `McpHttpRequestHandler` in full: method and path routing, `Content-Type` and body-size
limits, constant-time token comparison, the `IsLocal` and loopback `RemoteEndPoint` assertions, and
`McpOriginPolicy` with the empty origin allow list, the `Host` check and the `OPTIONS` refusal. The
status code table above is the specification for this step.

**Verify.** Every row of the table, driven from a standard-library client: `403` for a request
carrying any `Origin`; `403` for `OPTIONS`; `405` with `Allow: POST` for `GET`; `413` for a 5 MB
body; `415` for `text/plain`; `401` for a token one character wrong. A local HTML file that POSTs to
the endpoint is blocked by the missing preflight, and the request that a rebinding page could still
send is refused by the `Origin` check. No response ever carries an `Access-Control-Allow-*` header.

### Step T4 - The protocol front

**Change.** `McpProtocolServer` from `McpServer.cs`: `initialize`, `ping`, `tools/list`,
`tools/call`, the `notifications/*` swallow, the id handling and the JSON-RPC error composition,
minus `Run`, `Write`, the stdio reader and writer, `GetClient`, `ResolveOnlySession`, `CallSession`
and `GetNoSessionMessage`. One JSON object in, one out. `McpTool`, `McpModelScript` and
`McpException` move unchanged but for their namespace.

**Verify.** `POST` of `initialize` returns the protocol version the client asked for, the tools
capability and `GetInstructions()` verbatim; `tools/list` returns the ten tools with their schemas
and `readOnlyHint` annotations, byte-comparable with what the bridge returned before; a malformed
body returns a `-32700` envelope in a `200`; an unknown method returns `-32601`.

### Step T5 - The tool catalog in process

**Change.** `McpToolCatalog.Execute(name, arguments, RpcDispatcher)` replaces the `PipeClient`
parameter and calls `Invoke`, catching `RpcException` and turning its code and message into the
`isError` tool result the bridge produced from a parsed envelope. Delete `JsonValuePrinter` -
`Invoke` returns the handler's own JSON string. `list_sessions` reads the sessions folder through
`RpcSessionInfoReader` and reports the directory of T-D9, including `isThisSession`. `process_id`
becomes the assertion of T-D9, and the `SessionSchema` description and the `list_sessions` tool
description are rewritten to say what it now means.

**Verify.** All ten tools exercised end to end against a live session in which the user keeps working:
`describe_api` on `pmx` and on `pmx.model.steps`, `read_model` on a model built by
`Test_Model_Meshing.py`, `run_python` adding a step and a load and both appearing in the tree with
`CAddStep` and `CAddLoad` in the history, `run_job` and `get_job` across a solver run, `screenshot`
producing a non-blank PNG. A `process_id` naming another instance is refused and names its URL. Two
instances on two ports each answer for their own model. A call while an edit dialog is open is
refused as busy and the dialog is untouched. Read only mode refuses `run_python` and still serves
`read_model`.

### Step T6 - Delete the bridge and the build lines

**Change.**

- Remove the `rem PrePoMaxMcp ...` comment and both `copy` lines for `PrePoMaxMcp.exe` and
  `PrePoMaxMcp.exe.config` from the `PostBuildEvent` in `PrePoMax.csproj`.
- Remove from `PrePoMax.sln`: `{7B2D4F91-3C6A-4E58-9D1F-5A8C2E7B40D6}` from the `PrePoMax` project's
  `ProjectSection(ProjectDependencies)`, the `PrePoMaxMcp` `Project(...)`/`EndProject` pair, and its
  four `GlobalSection(ProjectConfigurationPlatforms)` lines.
- Delete `PrePoMaxMcp/`, `bin/` and `obj/` included, and confirm the GUID appears nowhere else.
- Remove the `PrePoMaxMcp` bullet from the project list in `README.md`.
- Do **not** add anything to the build to tolerate a locked `lib\CaeGlobals.dll`. The reverted
  workaround stays reverted.

**Verify.** `PrePoMax.sln` builds clean in `Debug|x64` twice in a row with an agent session open -
the state that used to fail with `MSB3677` - and `$(TargetDir)` contains no `PrePoMaxMcp.exe`. The
only remaining reason a build cannot move `lib\` is a running `PrePoMax.exe`, which is the ordinary
`MSB3027`/`MSB3021` case `CLAUDE.md` already documents.

### Step T7 - Documentation and the migration note

**Change.**

- `Python_API_Overview.md`, *Agent integration*: the two-piece description becomes one piece. The
  endpoint, the token, the port and the probe behaviour; the *Turning it on* table gains `--rpcPort`;
  the *tools* table stays as it is but for `list_sessions`; the *protocol, for another client* section
  gains the HTTP framing and the status codes and keeps the pipe as the second front. Its current
  `claude mcp add prepomax -- "C:\Program Files\PrePoMax\PrePoMaxMcp.exe"` example goes with the
  bridge, and with it an install path that never existed - PrePoMax is portable, and the replacement
  names a URL rather than a location.
- `README.md`: the configuration snippet and the two `claude mcp` commands, and the migration
  paragraph for a user who has the stdio entry.
- The mitigation of the regression below, in all three places it belongs.
- `AGENTS.md`: the Layer 3 rule that a new RPC primitive is a deliberate protocol change gains the
  security rule - the three gates of T-D6 are not optional, and a change to
  `McpHttpRequestHandler` or `McpOriginPolicy` is reviewed as a security change.

**Verify.** A reader who has the old stdio entry can get to a working endpoint from the README alone,
without reading this plan.

### Step T8 - Tests

**Change.** `Test_Rpc_Server.py` keeps covering the pipe unchanged. A new `Test_Mcp_Http.py`, standard
library only (`http.client`, `json`, `urllib`), covers the HTTP front: the status code table of T3,
`initialize` / `tools/list` / `tools/call`, every tool once, the busy path, the read only path, the
`process_id` assertion, and the token read from the session file rather than hard-coded. Both declare
their run instructions and expected exit code in the header and print `TEST PASSED`.

**Verify.** `python APIs/Python/Test_Scripts/Run_Tests.py` passes with both scripts included, against
a live session with the server on.

## The one real regression

With PrePoMax closed there is no process to answer, so instead of `GetNoSessionMessage`'s sentence
explaining that the user has to open PrePoMax and switch the server on, the agent sees an unreachable
server: `prepomax: failed to connect` in the client's server list, and a tool call that fails before
it reaches any code of ours. Nothing can be served from a process that does not exist, so the
mitigations are all in front of that point rather than at it:

- **`README.md` and `Python_API_Overview.md`** state, next to the configuration snippet, that a
  connection failure means PrePoMax is closed or its automation server is off, and name the two
  places to turn it on.
- **The settings dialog** carries the same sentence in the *Endpoint* property's description, which
  the property grid shows when the property is selected - the user is looking at exactly that text
  when they configure this.
- **A `CLAUDE.md` snippet in the README**, for the user to paste into their own project, telling the
  agent that a `prepomax` connection failure means asking the user to open PrePoMax rather than
  working around it. This is `GetNoSessionMessage` moved from a tool result into the agent's standing
  instructions, which is the closest thing to a real replacement and reaches the agent earlier than
  the tool result ever did.
- **The menu item and the status indicator** already answer "is it on?" at a glance, and after T2 the
  status label carries the port, so "is it on, and where?" is one look.
- With *Start with the application* on - the configuration a user who wants this at all will choose -
  the endpoint is up whenever PrePoMax is, and the regression is mostly theoretical. The reason it is
  not the default stays what it was: it is a door onto a live model.

What is bought for it: the endpoint's lifetime is exactly the application's lifetime. No process
outlives what it serves, no idle server holds a build output, and the failure the user does see is
the truthful one - the application is not open.

## Risks

| Risk | Mitigation |
|---|---|
| A local program or a web page reaches the endpoint | The three gates of T-D6, none of them optional: loopback-only prefixes, a bearer token in a file the browser cannot read, and an `Origin`/`OPTIONS` refusal that denies the preflight a cross-origin POST needs. T3 tests each one |
| DNS rebinding defeats the loopback binding | Rebinding changes where the request goes, not what the browser must send: the `Origin` header is present and the token is not. Both checks are required, which is why neither may be dropped for convenience |
| The token in the session file is treated as stronger than it is | Stated in the plan, in the settings dialog description and in the Overview: it bounds the browser and another account, not code running as the user. That is the boundary the pipe already had |
| The portable layout is forgotten and something needs an install step | The three gates were chosen so that none of them does; the measurement that opened this plan exists to prove the binding needs no reservation. A design change that would want an elevated one-time action has nowhere to put it and is not admissible |
| Two portable copies confuse the token | Each copy's `settings.bin` holds its own, so a client configured against one gets a `401` from the other. `list_sessions` names every running copy with its URL, and each copy shows its own endpoint and token read-only in its settings dialog |
| The port is taken and the configured URL breaks | The probe of T-D8, the port reported in the status bar and the output window, and the endpoint shown read-only in the settings dialog. The single-instance case always lands on the configured port |
| A second instance confuses the agent | One port and one session file per instance, `list_sessions` as a directory naming the URLs, and `process_id` refused rather than mis-routed when it names another instance |
| A client that speaks only stdio cannot connect | T-D11's byte relay, specified but not built, with the two rules that keep it from ever locking a build. T-D4 keeps every answer a single JSON body precisely so that the relay stays a relay |
| The HTTP front and the pipe drift | They share `RpcDispatcher.Invoke` after T0; only framing differs, and neither front holds a handler of its own |
| A client needs progress on a long call | Not served, deliberately: the busy contract and `RequestTimeout` are the existing answer, and an SSE endpoint is additive onto the same handler if a real need appears |
| The MCP revision the client speaks moves | `initialize` echoes the client's version as it already does, a missing `MCP-Protocol-Version` is tolerated, no session id is issued, and T7 re-checks the client configuration keys against current documentation |
| The build lock reappears in another form | It cannot from this direction: after T6 nothing in `$(TargetDir)` is started by anything but the user. The rule that survives is the one in T-D11 - a satellite that references a solution assembly may not live in the application's output folder |

## Exit criteria

With PrePoMax open on the desktop and the automation server on, an agent client configured with a URL
and a token discovers the same ten tools, learns the API from `describe_api`, builds and meshes a
model, runs the analysis, reads the results back and screenshots the viewport - while the user
watches it happen in their own window and can delete any of it from the command history afterwards.
`PrePoMaxMcp.exe` no longer exists, no process serves an agent but the one it drives, and
`PrePoMax.sln` builds twice in a row with an agent session open.
