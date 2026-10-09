#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the MCP endpoint the way an AI agent client uses it: over loopback HTTP, with the MCP
#handshake, one tools/call per tool, and every gate a request has to pass first. It starts PrePoMax
#sessions of its own - with -g No and the automation server, so they have no window - and talks to
#the address each one publishes in its session file.
#
#The endpoint is served by the application itself, so there is no bridge process to start and none
#to outlive it: with the session closed the address is simply unreachable, which the last check
#asserts because it is the one thing this transport gives up.
#
#It also checks what the protocol promises beyond the tools: a body is read as UTF-8 whatever its
#Content-Type says, a batch and an argument a tool does not take are refused, a value of the wrong type
#or out of the declared range is refused rather than read as the default, a tools/call sent as a
#notification is not run, the protocol version is negotiated, and the tools that run a script of their
#own leave the console scope as they found it.
#
#  PrePoMax.com --script Test_Mcp_Http.py -g No -x Yes -w <work directory>
#
#Expected process exit code 0.

import base64
import io
import json
import os
import shutil
import socket
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request

PROTOCOL_VERSION = "2024-11-05"
CHILD_TIMEOUT = 240
MAXIMUM_BODY = 4 * 1024 * 1024
SESSIONS = os.path.join(os.environ["LOCALAPPDATA"], "PrePoMax", "sessions")
MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
EXPECTED_TOOLS = ["get_guide", "list_sessions", "get_status", "describe_api", "read_model",
                  "read_topology", "eval_expression", "run_python", "get_call", "cancel_call",
                  "get_history", "run_job", "get_job", "screenshot", "exit_session"]


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def free_port(first=8790):
    """A port nothing else holds, so that the probing check below starts from a known state."""
    for port in range(first, first + 40):
        probe = socket.socket()
        try:
            probe.bind(("127.0.0.1", port))
            return port
        except OSError:
            continue
        finally:
            probe.close()
    raise Exception("TEST FAILED: no free port to test with")


def request(url, body=b"{}", token=None, method="POST", content_type="application/json", headers=None):
    """Sends one request and returns (status, text, headers). A refused connection is status 0."""
    call = urllib.request.Request(url, data=body, method=method)
    if content_type is not None:
        call.add_header("Content-Type", content_type)
    if token is not None:
        call.add_header("Authorization", "Bearer " + token)
    for name, value in (headers or {}).items():
        call.add_header(name, value)
    try:
        with urllib.request.urlopen(call, timeout=60) as answer:
            return answer.status, answer.read().decode("utf-8"), dict(answer.headers)
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode("utf-8"), dict(error.headers)
    except (urllib.error.URLError, socket.timeout) as error:
        return 0, str(error), {}


class Endpoint(object):
    """The MCP endpoint of one session, driven the way an MCP client drives it."""

    def __init__(self, url, token):
        self.url = url
        self.token = token
        self._id = 0

    def call(self, method, parameters=None):
        self._id += 1
        message = {"jsonrpc": "2.0", "id": self._id, "method": method}
        if parameters is not None:
            message["params"] = parameters
        status, text, _ = request(self.url, json.dumps(message).encode("utf-8"), self.token)
        check(status == 200, method + " answered HTTP " + str(status) + ": " + text)
        answer = json.loads(text)
        check(answer.get("id") == self._id, method + " answered the wrong id: " + text)
        return answer

    def notify(self, method, parameters=None):
        message = {"jsonrpc": "2.0", "method": method}
        if parameters is not None:
            message["params"] = parameters
        status, text, _ = request(self.url, json.dumps(message).encode("utf-8"), self.token)
        # A notification is not answered, and the endpoint says so with 202 and an empty body
        check(status == 202, method + " answered HTTP " + str(status) + ": " + text)
        check(text == "", method + " answered a notification with a body: " + text)

    def result(self, method, parameters=None):
        answer = self.call(method, parameters)
        check("error" not in answer, method + " failed: " + str(answer.get("error")))
        return answer["result"]

    def tool(self, name, arguments=None):
        """Calls one tool and returns its text, refusing a tool that reported an error."""
        result = self.result("tools/call", {"name": name, "arguments": arguments or {}})
        text = result["content"][0]["text"]
        check(not result.get("isError"), name + " reported an error: " + text)
        return text

    def failing_tool(self, name, arguments=None):
        result = self.result("tools/call", {"name": name, "arguments": arguments or {}})
        check(result.get("isError"), name + " was expected to report an error but returned " +
              result["content"][0]["text"])
        return result["content"][0]["text"]


class ChildSession(object):
    """A PrePoMax session with no window of its own, serving its own endpoint."""

    def __init__(self, port):
        self.directory = tempfile.mkdtemp(prefix="mcp_http_")
        self.log = open(os.path.join(self.directory, "child.log"), "w")
        # The cube is given explicitly, so that what the tools report is the cube and not whatever
        # the application would have started with
        self.process = subprocess.Popen([sys.executable, "--rpc", "On", "--rpcPort", str(port),
                                         "-g", "No", "--noSettingsSave",
                                         "-f", os.path.join(MODELS, "cube.inp"), "-u", "MM_TON_S_C",
                                         "--timeout", str(CHILD_TIMEOUT), "-w", self.directory],
                                        stdout=self.log, stderr=subprocess.STDOUT)
        self.info = None

    @property
    def path(self):
        return os.path.join(SESSIONS, str(self.process.pid) + ".json")

    def wait_until_listening(self, timeout=120.0):
        """The session file is the discovery contract, and it is written once the endpoint is up."""
        deadline = time.time() + timeout
        while time.time() < deadline:
            check(self.process.poll() is None, "the child session exited early:\n" + self.output())
            if os.path.exists(self.path):
                try:
                    self.info = json.load(open(self.path))
                    if self.info.get("url"):
                        return self.info
                except (ValueError, IOError):
                    pass
            time.sleep(0.2)
        raise Exception("TEST FAILED: no session file appeared at " + self.path + "\n" + self.output())

    def endpoint(self):
        return Endpoint(self.info["url"], self.info["token"])

    def output(self):
        try:
            self.log.flush()
        except ValueError:
            pass
        try:
            return open(os.path.join(self.directory, "child.log")).read()
        except (IOError, OSError):
            return ""

    def close(self):
        if self.process.poll() is None:
            self.process.kill()
            self.process.wait()
        self.log.close()
        shutil.rmtree(self.directory, ignore_errors=True)


def wait_until_idle(endpoint, timeout=120.0):
    """The server answers before the session has finished starting, and says so until it has."""
    deadline = time.time() + timeout
    reason = ""
    while time.time() < deadline:
        status = json.loads(endpoint.tool("get_status"))
        if status["isIdle"]:
            return
        reason = status["busyReason"]
        time.sleep(0.2)
    raise Exception("TEST FAILED: the session stayed busy: " + reason)


def check_publication(session, port):
    info = session.info
    check(info["port"] == port, "the endpoint must take the free port it was given, not " + str(info["port"]))
    check(info["url"] == "http://127.0.0.1:" + str(port) + "/mcp", "unexpected address: " + info["url"])
    check(len(info["token"]) >= 40, "the session file must carry an access token: " + str(len(info["token"])))
    check(info["pipeName"] == "PrePoMax." + str(session.process.pid),
          "the pipe must still be published for a client that speaks the protocol directly")
    # The token comes from this folder's settings, which is why the folder has to have been opened
    # once and given one: the server refuses to start without it and there is no switch to supply it
    check(len(info["token"]) == 43, "an access token is 43 characters: " + str(len(info["token"])))
    print("published: " + info["url"] + ", protocol " + info["protocolVersion"])


def check_gates(url, token, port):
    """The three gates of the endpoint, and the request hygiene around them."""
    status, _, headers = request(url, b'{"jsonrpc":"2.0","id":1,"method":"ping"}', None)
    check(status == 401, "a request with no token must be refused, not " + str(status))
    check(headers.get("WWW-Authenticate") == "Bearer", "a 401 must name the scheme it wants")
    wrong = token[:-1] + ("a" if token[-1] != "a" else "b")
    status, _, _ = request(url, b'{"jsonrpc":"2.0","id":1,"method":"ping"}', wrong)
    check(status == 401, "a token that differs in one character must be refused, not " + str(status))
    #
    status, _, headers = request(url, b"{}", token, headers={"Origin": "http://evil.example"})
    check(status == 403, "a request carrying an Origin must be refused, not " + str(status))
    check("Access-Control-Allow-Origin" not in headers,
          "the endpoint must never grant cross origin access: " + str(headers))
    status, _, _ = request(url, None, token, method="OPTIONS", content_type=None)
    check(status == 403, "a preflight must be refused, not " + str(status))
    #
    status, _, headers = request(url, None, token, method="GET", content_type=None)
    check(status == 405, "GET must be refused, not " + str(status))
    check(headers.get("Allow") == "POST", "a 405 must say what is allowed: " + str(headers.get("Allow")))
    status, _, _ = request(url, b"{}", token, content_type="text/plain")
    check(status == 415, "a body that is not JSON must be refused, not " + str(status))
    status, _, _ = request(url, b"x" * (MAXIMUM_BODY + 1024), token)
    check(status == 413, "an oversized body must be refused, not " + str(status))
    status, _, _ = request("http://127.0.0.1:" + str(port) + "/other", b"{}", token)
    check(status == 404, "another path must not be served, not " + str(status))
    #
    # A failure of the protocol is a 200 carrying a JSON-RPC error, because the protocol was reached
    status, text, _ = request(url, b"not json at all", token)
    check(status == 200, "a body that is not JSON is a protocol error, not HTTP " + str(status))
    check(json.loads(text)["error"]["code"] == -32700, "a parse error must be -32700: " + text)
    # Both loopback names are claimed, because http.sys routes by the Host header of the request
    status, _, _ = request("http://localhost:" + str(port) + "/mcp",
                           b'{"jsonrpc":"2.0","id":1,"method":"ping"}', token)
    check(status == 200, "the localhost address must answer as well as 127.0.0.1, not " + str(status))
    print("gates: token, origin and method refused as they must be, and both loopback names answer")


def check_handshake(endpoint):
    result = endpoint.result("initialize", {"protocolVersion": PROTOCOL_VERSION,
                                            "capabilities": {},
                                            "clientInfo": {"name": "Test_Mcp_Http", "version": "1.0"}})
    check(result["protocolVersion"] == PROTOCOL_VERSION,
          "the server must answer the protocol version it was offered: " + result["protocolVersion"])
    check("tools" in result["capabilities"], "the server must offer tools: " + str(result["capabilities"]))
    check(result["serverInfo"]["name"] == "prepomax", "unexpected server name: " + result["serverInfo"]["name"])
    check("pmx" in result["instructions"], "the instructions must name the API root")
    check("busy" in result["instructions"], "the instructions must explain the busy answer")
    endpoint.notify("notifications/initialized")
    endpoint.result("ping")
    # A version the server supports is answered as it was offered, and any other one with the latest
    # the server supports, which the client may then refuse
    result = endpoint.result("initialize", {"protocolVersion": "2025-06-18", "capabilities": {},
                                            "clientInfo": {"name": "Test_Mcp_Http", "version": "1.0"}})
    check(result["protocolVersion"] == "2025-06-18", "a supported version must be kept: " + result["protocolVersion"])
    result = endpoint.result("initialize", {"protocolVersion": "1999-01-01", "capabilities": {},
                                            "clientInfo": {"name": "Test_Mcp_Http", "version": "1.0"}})
    check(result["protocolVersion"] == "2025-06-18",
          "an unsupported version must be answered with the latest supported one: " + result["protocolVersion"])
    print("handshake: " + result["serverInfo"]["name"] + " " + result["serverInfo"]["version"] +
          ", protocol " + result["protocolVersion"])


def check_tool_list(endpoint):
    result = endpoint.result("tools/list")
    tools = dict((tool["name"], tool) for tool in result["tools"])
    for name in EXPECTED_TOOLS:
        check(name in tools, "the tool " + name + " is missing: " + str(sorted(tools)))
    check(len(tools) == len(EXPECTED_TOOLS), "unexpected tools: " + str(sorted(tools)))
    for name, tool in tools.items():
        check(len(tool["description"]) > 40, name + " needs a description an agent can act on")
        schema = tool["inputSchema"]
        check(schema["type"] == "object", name + " must take an object: " + str(schema))
        check("properties" in schema, name + " must declare its properties: " + str(schema))
    check(tools["run_python"]["annotations"]["readOnlyHint"] is False, "run_python changes the model")
    check(tools["read_model"]["annotations"]["readOnlyHint"] is True, "read_model changes nothing")
    check("source" in tools["run_python"]["inputSchema"]["required"], "run_python must require its source")
    for name, tool in tools.items():
        check(tool["inputSchema"].get("additionalProperties") is False,
              name + " must refuse an argument it does not declare: " + str(tool["inputSchema"]))
    print("tools: " + str(len(tools)) + " tools, every one with a schema")


def check_reading_tools(endpoint, session):
    url = session.info["url"]
    status = json.loads(endpoint.tool("get_status"))
    check(status["isRunning"], "the session must be running: " + str(status))
    check(status["isIdle"], "the session must be idle: " + status["busyReason"])
    check(status["endpointUrl"] == url, "status must name the endpoint: " + status["endpointUrl"])
    check(status["isWindowHidden"], "a -g No session has no window")
    #
    model = json.loads(endpoint.tool("read_model"))
    check("properties" in model, "the snapshot must carry the model properties: " + str(model)[:200])
    check("parts" in model, "the snapshot must carry the collections: " + str(sorted(model)))
    check("jobs" in model, "the snapshot must carry the analyses: " + str(sorted(model)))
    meshed = [part for part in model["parts_mesh_state"] if part["meshed"]]
    check(len(meshed) >= 1 and meshed[0]["element_count"] > 0 and meshed[0]["node_count"] > 0,
          "the snapshot must carry the mesh state of the parts: " + str(model["parts_mesh_state"]))
    check(isinstance(model.get("mesh_setup_items"), list),
          "the snapshot must carry the mesh setup items: " + str(model.get("mesh_setup_items")))
    check(isinstance(model.get("keywords", {}).get("user_keyword_count"), int),
          "the snapshot must count the user keywords: " + str(model.get("keywords")))
    check(not [entry for entry in model["unreadable"] if "setup_items" in entry or "keywords" in entry],
          "the snapshot must read the mesh setup items and the keywords: " + str(model["unreadable"]))
    print("read_model: " + str(len(model)) + " entries, unit system " + str(model["properties"]["unit_system"]))
    #
    described = json.loads(endpoint.tool("describe_api", {"path": "pmx.model"}))
    check(described["resolvedAs"] == "path", "a path must be reported as one: " + str(described["resolvedAs"]))
    names = [member["name"] for member in described["members"]]
    check("parts" in names, "pmx.model must describe its collections: " + str(names))
    print("describe_api: pmx.model offers " + str(len(names)) + " members")
    #
    topology = json.loads(endpoint.tool("read_topology"))
    check("note" in topology, "the topology must say what its IDs are for: " + str(sorted(topology)))
    check("model" in topology and "geometry" in topology,
          "the topology must cover both part collections: " + str(sorted(topology)))
    check(len(topology["model"]) == 1, "the cube is one part: " + str(topology["model"]))
    part = topology["model"][0]
    check(part["face_count"] == 6 and part["edge_count"] == 12 and part["vertex_count"] == 8,
          "the cube topology must be 6 faces, 12 edges and 8 vertices: " + str(part))
    # The top face, found the way an agent finds it, and named by the ID a selection takes
    top = [face for face in part["faces"] if face["normal"] == [0.0, 0.0, 1.0]]
    check(len(top) == 1, "exactly one face must point up: " + str(part["faces"]))
    check(top[0]["centroid"][2] == 1.0, "the top face must sit at z = 1: " + str(top[0]))
    check(top[0]["id"] in range(1, 7), "the face ID must be a 1-based face ID: " + str(top[0]))
    print("read_topology: the cube has " + str(part["face_count"]) + " faces and face " +
          str(top[0]["id"]) + " points up")
    # A part name is compared regardless of case, and a name that matched nothing is reported
    name = part["name"]
    topology = json.loads(endpoint.tool("read_topology", {"part_names": [name.upper(), "no_such_part"],
                                                          "limit": 240}))
    check(len(topology["model"]) == 1 and topology["model"][0]["name"] == name,
          "a part name must match regardless of case: " + str(topology["model"]))
    check(topology.get("unmatched_part_names") == ["no_such_part"],
          "a name that matched no part must be reported: " + str(topology.get("unmatched_part_names")))
    check(len(topology["model"][0]["faces"]) <= 240, "the limit must bound the rows of a part")
    print("read_topology: a name in other case matched, an unknown name reported")


def check_the_guide(endpoint):
    guide = endpoint.tool("get_guide")
    check(len(guide) > 2000, "the guide must be a guide, not a sentence: " + str(len(guide)))
    # The three things a caller cannot work out for itself, and the guide exists to say them
    for phrase in ("unit system", "preview()", "Edit - Edit History", "describe_api", "run_python"):
        check(phrase in guide, "the guide must mention " + phrase)
    print("get_guide: " + str(len(guide)) + " characters")


def check_describing_a_type(endpoint):
    """The half of the API that hangs off something the model does not have yet."""
    described = json.loads(endpoint.tool("describe_api", {"path": "Loads"}))
    check(described["resolvedAs"] == "type", "a type must be reported as one: " + str(described))
    names = [member["name"] for member in described["members"]]
    check("add_pressure" in names, "Loads must describe its add methods: " + str(names))
    check("names" not in described, "a type carries no item names: " + str(sorted(described)))
    # The schema carries the real parameters, with the default and the accepted values
    pressure = [member for member in described["members"] if member["name"] == "add_pressure"][0]
    parameters = dict((parameter["name"], parameter) for parameter in pressure["parameters"])
    check("region" in parameters, "add_pressure must declare its region: " + str(sorted(parameters)))
    check(parameters["name"]["isOptional"] is False, "the name of a load is required")
    check(parameters["distribution_name"]["default"] == "None",
          "an optional argument must report its default: " + str(parameters["distribution_name"]))
    #
    step = json.loads(endpoint.tool("describe_api", {"path": "Model.Steps"}))
    static = [member for member in step["members"] if member["name"] == "add_static"][0]
    parameters = dict((parameter["name"], parameter) for parameter in static["parameters"])
    check("Automatic" in parameters["incrementation_type"]["values"],
          "an enumeration valued argument must list its values: " + str(parameters["incrementation_type"]))
    print("describe_api: Loads and Model.Steps described by name, with values and defaults")
    #
    # The model and the results sides both own Nodes, so the bare name is refused with the candidates
    text = endpoint.failing_tool("describe_api", {"path": "Nodes"})
    check("Model.Nodes" in text, "the refusal must name the candidates: " + text)
    described = json.loads(endpoint.tool("describe_api", {"path": "Model.Nodes"}))
    check(described["resolvedAs"] == "type", "the qualified name must resolve: " + str(described))
    print("describe_api: an ambiguous type name is refused with its candidates")


def check_private_scripts(endpoint):
    """read_model and read_topology run Python of their own, and must leave the agent's variables alone."""
    result = json.loads(endpoint.tool("run_python", {"source": "names = 'agent names'" + os.linesep + "LIMIT = 7"}))
    check(not result["failed"], "the agent variables must be set: " + result["error"])
    endpoint.tool("read_model")
    endpoint.tool("read_topology")
    value = json.loads(endpoint.tool("eval_expression", {"expression": "[names, LIMIT]"}))
    check(value["value"] == ["agent names", 7], "a tool script must not overwrite the agent's variables: " +
          str(value["value"]))
    value = json.loads(endpoint.tool("eval_expression",
                                     {"expression": "[name for name in dir() if name.startswith('_prepomax_tool_')]"}))
    check(value["value"] == [], "a tool script must leave nothing in the scope: " + str(value["value"]))
    print("read_model and read_topology: the agent's variables survived, nothing was left behind")


def check_utf8_body(endpoint):
    """A body is UTF-8 whatever its Content-Type says, and the answer says it is UTF-8."""
    text = "\u00b0\u00b5\u0161\u010d"
    message = {"jsonrpc": "2.0", "id": 9001, "method": "tools/call",
               "params": {"name": "eval_expression",
                          "arguments": {"expression": json.dumps(text, ensure_ascii=False)}}}
    body = json.dumps(message, ensure_ascii=False).encode("utf-8")
    check(any(byte > 127 for byte in body), "the body must carry raw UTF-8, not escapes")
    # The Content-Type names no charset, which is where the ANSI code page used to be assumed
    status, answer, headers = request(endpoint.url, body, endpoint.token, content_type="application/json")
    check(status == 200, "the UTF-8 body must be served, not HTTP " + str(status) + ": " + answer)
    check("charset=utf-8" in headers.get("Content-Type", "").lower(),
          "the answer must say it is UTF-8: " + str(headers.get("Content-Type")))
    result = json.loads(answer)["result"]
    check(not result.get("isError"), "the expression must evaluate: " + result["content"][0]["text"])
    value = json.loads(result["content"][0]["text"])
    check(value["value"] == text, "the text must round trip: " + repr(value["value"]) + " is not " + repr(text))
    print("utf-8: " + repr(text) + " sent as raw UTF-8 came back unchanged")


def check_writing_tools(endpoint):
    result = json.loads(endpoint.tool("run_python", {"source": "pmx.model.parameters.add('mcp_a', 1.5)"}))
    check(not result["failed"], "the call must not fail: " + result["error"])
    check([entry["type"] for entry in result["recordedCommands"]] == ["CAddParameter"],
          "the change must be recorded as one command: " + str(result["recordedCommands"]))
    check("mcp_a" in result["recordedCommands"][0]["description"],
          "the recorded command must name the item: " + str(result["recordedCommands"]))
    #
    value = json.loads(endpoint.tool("eval_expression", {"expression": "pmx.model.parameters['mcp_a'].value"}))
    check(not value["failed"], "the parameter must be readable: " + value["error"])
    print("run_python: recorded " + str(result["recordedCommands"]) + ", read back " + value["repr"])
    #
    history = json.loads(endpoint.tool("get_history", {"include_journal": True}))
    check(history["commandCount"] >= 1, "the history must hold the change: " + str(history))
    check("pmx.model.parameters.add" in history["journal"]["source"],
          "the journal must carry the call: " + history["journal"]["source"])
    print("get_history: " + str(history["commandCount"]) + " commands, journalled")


def check_a_long_call(endpoint):
    """run_python past its timeout is answered with a call id, keeps running, and get_call collects it."""
    status = json.loads(endpoint.tool("get_status"))
    check(status["calls"] == [], "no call may be pending yet: " + str(status["calls"]))
    source = os.linesep.join(["import time", "time.sleep(4)", "pmx.model.parameters.add('mcp_long', 2.5)",
                              "print('mcp_long done')"])
    started = time.time()
    answer = json.loads(endpoint.tool("run_python", {"source": source, "timeout": 1}))
    check(answer.get("running") is True and answer["callId"] > 0, "the call must be answered as running: " + str(answer))
    check(time.time() - started < 3.0, "the caller must be answered after its timeout, not after the call")
    call_id = answer["callId"]
    status = json.loads(endpoint.tool("get_status"))
    check(not status["isIdle"] and ("Automation call " + str(call_id)) in status["busyReason"],
          "get_status must name the running call: " + str(status))
    answer = json.loads(endpoint.tool("get_call", {"call_id": call_id, "timeout": 30}))
    check("running" not in answer and not answer["failed"], "get_call must return the finished call: " + str(answer))
    check("mcp_long done" in answer["output"], "the output must be handed over: " + answer["output"])
    check([entry["type"] for entry in answer["recordedCommands"]] == ["CAddParameter"],
          "the call must have recorded its command: " + str(answer["recordedCommands"]))
    text = endpoint.failing_tool("get_call", {"call_id": call_id})
    check(str(call_id) in text, "a collected answer must not be handed over twice: " + text)
    text = endpoint.failing_tool("run_python", {"source": "pmx.model.parameters.add('mcp_never', 1.0)",
                                                "timeout": 601})
    check("timeout" in text, "a timeout out of range must be refused by name: " + text)
    value = json.loads(endpoint.tool("eval_expression", {"expression": "pmx.model.parameters.get_names()"}))
    check("mcp_long" in value["value"] and "mcp_never" not in value["value"],
          "the long call must have changed the model and the refused one not: " + str(value["value"]))
    print("run_python: a 4 s call answered as running after 1 s, collected by get_call with its command")


def check_cancel_call(endpoint):
    """A run_python that never ends is stopped by cancel_call, and the session takes the next call."""
    source = os.linesep.join(["while True:", "    pass"])
    answer = json.loads(endpoint.tool("run_python", {"source": source, "timeout": 1}))
    check(answer.get("running") is True and answer["callId"] > 0, "the loop must be answered as running: " + str(answer))
    call_id = answer["callId"]
    answer = json.loads(endpoint.tool("cancel_call", {"call_id": call_id}))
    check(answer["cancelRequested"] and answer["isInterrupted"], "the loop must be interrupted: " + str(answer))
    check(answer["isFinished"], "a pure Python loop must stop at once: " + str(answer))
    # A cancelled call failed, so get_call reports it as an error carrying the whole answer
    text = endpoint.failing_tool("get_call", {"call_id": call_id, "timeout": 30})
    check("KeyboardInterrupt" in text and "cancelled" in text,
          "the collected answer must be the interrupted call: " + text)
    value = json.loads(endpoint.tool("eval_expression", {"expression": "6 * 7"}))
    check(value["value"] == 42, "the session must answer after a cancel: " + str(value))
    print("cancel_call: a loop that never ends was stopped and the session answered the next call")


def check_screenshot(endpoint):
    answer = endpoint.result("tools/call", {"name": "screenshot", "arguments": {}})
    check(not answer.get("isError"), "the screenshot must be taken: " + answer["content"][0]["text"])
    result = json.loads(answer["content"][0]["text"])
    check(os.path.exists(result["fileName"]), "the screenshot must exist: " + result["fileName"])
    images = [block for block in answer["content"] if block["type"] == "image"]
    check(len(images) == 1 and images[0]["mimeType"] == "image/png",
          "the picture must be attached as a PNG image block: " + str([block["type"] for block in answer["content"]]))
    check(base64.b64decode(images[0]["data"])[:8] == b"\x89PNG\r\n\x1a\n", "the image block must be the PNG")
    print("screenshot: " + str(result["length"]) + " bytes, attached as an image")
    os.remove(result["fileName"])
    # A relative file name is taken in the temporary folder, not in the working directory of the process
    result = json.loads(endpoint.tool("screenshot", {"file_name": "mcp_relative.png"}))
    expected = os.path.normcase(os.path.realpath(os.path.join(tempfile.gettempdir(), "PrePoMax")))
    check(os.path.normcase(os.path.realpath(os.path.dirname(result["fileName"]))) == expected,
          "a relative name must land in the PrePoMax temporary folder: " + result["fileName"])
    os.remove(result["fileName"])
    # The optional arguments set the shot up through pmx.view before it is taken
    result = json.loads(endpoint.tool("screenshot", {"view": "femodel", "orientation": "Isometric",
                                                     "zoom_to_fit": True}))
    check(os.path.exists(result["fileName"]), "the set up screenshot must exist: " + result["fileName"])
    os.remove(result["fileName"])
    view = json.loads(endpoint.tool("eval_expression", {"expression": "pmx.view.current"}))
    check(view["value"] == "femodel", "the view must have been switched: " + str(view["value"]))
    print("screenshot: taken again after switching the view and the camera")
    # A view change is not a model change, so it records nothing
    history = json.loads(endpoint.tool("get_history"))
    for command in history["commands"]:
        check("view" not in command["type"].lower(), "a view change must not be recorded: " + str(command))


def check_refusals(endpoint):
    # A tool that does not exist is a protocol error, because the client asked for something it was
    # never offered
    answer = endpoint.call("tools/call", {"name": "no_such_tool", "arguments": {}})
    check("error" in answer, "an unknown tool must be a protocol error: " + str(answer))
    check("run_python" in answer["error"]["message"], "the refusal must list the tools: " + str(answer))
    # An argument a tool does not take is refused by name, rather than dropped
    text = endpoint.failing_tool("get_status", {"no_such_argument": 1})
    check("no_such_argument" in text, "the refusal must name the argument: " + text)
    text = endpoint.failing_tool("get_guide", {"process_id": 1})
    check("process_id" in text, "get_guide takes no process_id: " + text)
    # A batch is not served, and is answered rather than dropped
    status, text, _ = request(endpoint.url, b'[{"jsonrpc":"2.0","id":1,"method":"ping"}]', endpoint.token)
    check(status == 200 and json.loads(text)["error"]["code"] == -32600,
          "a batch must be answered with a JSON-RPC error, not HTTP " + str(status) + ": " + text)
    # exit_session is refused for a session with a window only, so here it is only checked to guard
    # unsaved changes rather than to close the session the remaining checks need
    endpoint.tool("run_python", {"source": "pmx.model.parameters.add('mcp_exit_guard', 1.0)"})
    text = endpoint.failing_tool("exit_session")
    check("unsaved changes" in text, "exit_session must guard unsaved changes: " + text)
    # A tool that cannot be served is a result the agent reads, so it can decide what to do next
    text = endpoint.failing_tool("run_python", {})
    check("source" in text, "the refusal must name the missing argument: " + text)
    text = endpoint.failing_tool("eval_expression", {"expression": "pmx.no_such_member"})
    check(len(text) > 0, "a failing expression must say what went wrong")
    # A method the server does not serve
    answer = endpoint.call("resources/list")
    check("error" in answer and answer["error"]["code"] == -32601,
          "an unserved method must be -32601: " + str(answer))
    print("refusals: an unknown tool, a missing argument and an unserved method")


def check_argument_values(endpoint):
    """A value the schema does not allow is refused by name, never read as the default or clamped."""
    count = json.loads(endpoint.tool("get_status"))["model"]["commandCount"]
    refused = [("get_history", {"include_journal": "true"}, "include_journal"),
               ("get_history", {"limit": -1}, "limit"),
               ("get_history", {"limit": "10"}, "limit"),
               ("screenshot", {"magnification": "2"}, "magnification"),
               ("screenshot", {"magnification": 5}, "magnification"),
               ("screenshot", {"transparent_background": 1}, "transparent_background"),
               ("screenshot", {"orientation": "Sideways"}, "orientation"),
               ("run_python", {"source": "pmx.model.parameters.add('mcp_typed', 1.0)", "record": 5}, "record"),
               ("run_python", {"source": "pmx.model.parameters.add('mcp_typed', 1.0)", "record": "lines"}, "record"),
               ("run_python", {"source": "pmx.model.parameters.add('mcp_typed', 1.0)", "timeout": 2.5}, "timeout"),
               ("run_python", {"source": 7}, "source"),
               ("read_topology", {"limit": "10"}, "limit"),
               ("read_topology", {"limit": 0}, "limit"),
               ("read_topology", {"limit": -5}, "limit"),
               ("read_topology", {"limit": 1000}, "limit"),
               ("read_topology", {"part_names": [5]}, "part_names"),
               ("read_topology", {"part_names": [None]}, "part_names"),
               ("read_topology", {"part_names": "Part-1"}, "part_names"),
               ("get_call", {"call_id": 1.5}, "call_id"),
               ("get_call", {"call_id": 0}, "call_id"),
               ("cancel_call", {"call_id": "1"}, "call_id"),
               ("eval_expression", {"expression": "1", "timeout": 601}, "timeout"),
               ("get_status", {"process_id": "1"}, "process_id"),
               ("exit_session", {"discard_changes": "true"}, "discard_changes")]
    for name, arguments, argument in refused:
        text = endpoint.failing_tool(name, arguments)
        check(("argument " + argument) in text and "Nothing was done" in text,
              name + " must refuse " + str(arguments) + " naming " + argument + ": " + text)
    text = endpoint.failing_tool("read_topology", {"limit": 1000})
    check("from 1 to 240" in text, "an out of range limit must be refused with its range: " + text)
    text = endpoint.failing_tool("get_history", {"include_journal": "true"})
    check("true or false" in text, "a string for a boolean must be refused as such: " + text)
    # Nothing ran: the session is still there, and nothing was recorded
    status = json.loads(endpoint.tool("get_status"))
    check(status["model"]["commandCount"] == count, "a refused call must record nothing: " + str(status["model"]))
    value = json.loads(endpoint.tool("eval_expression", {"expression": "pmx.model.parameters.get_names()"}))
    check("mcp_typed" not in value["value"], "a refused run_python must not have run: " + str(value["value"]))
    # The bounds themselves are served
    endpoint.tool("read_topology", {"limit": 1})
    endpoint.tool("get_history", {"limit": 0})
    # An option string is compared regardless of case, as the API compares it
    result = json.loads(endpoint.tool("run_python", {"source": "pass", "record": "Commands"}))
    check(not result["failed"] and result["record"] == "commands",
          "an option in another case must be taken in its declared spelling: " + str(result))
    result = json.loads(endpoint.tool("screenshot", {"view": "FEMODEL", "orientation": "isometric"}))
    os.remove(result["fileName"])
    print("argument values: " + str(len(refused)) + " values of the wrong type or out of range refused by name")


def check_notification_not_run(endpoint):
    """A tools/call without an id is a notification: it is not answered, and so it is not run either."""
    endpoint.notify("tools/call", {"name": "run_python",
                                   "arguments": {"source": "pmx.model.parameters.add('mcp_notified', 1.0)"}})
    value = json.loads(endpoint.tool("eval_expression", {"expression": "pmx.model.parameters.get_names()"}))
    check("mcp_notified" not in value["value"], "a tools/call sent as a notification must not run: " +
          str(value["value"]))
    # An id that is null is not a notification and not a valid request either
    body = json.dumps({"jsonrpc": "2.0", "id": None, "method": "tools/call",
                       "params": {"name": "run_python",
                                  "arguments": {"source": "pmx.model.parameters.add('mcp_null_id', 1.0)"}}})
    status, text, _ = request(endpoint.url, body.encode("utf-8"), endpoint.token)
    check(status == 200 and json.loads(text)["error"]["code"] == -32600,
          "a request with a null id must be an invalid request: HTTP " + str(status) + ": " + text)
    value = json.loads(endpoint.tool("eval_expression", {"expression": "pmx.model.parameters.get_names()"}))
    check("mcp_null_id" not in value["value"], "a request with a null id must not run: " + str(value["value"]))
    print("notifications: a tools/call without an id was not run, a null id was refused")


def check_second_instance(endpoint, session, port):
    """One port per instance, so the URL is the address and a process id is only an assertion."""
    second = ChildSession(port)
    second.wait_until_listening()
    check(second.info["port"] == port + 1,
          "a second instance must probe past the taken port, not take " + str(second.info["port"]))
    #
    listed = json.loads(endpoint.tool("list_sessions"))
    mine = [entry for entry in listed["sessions"] if entry["processId"] == session.process.pid]
    other = [entry for entry in listed["sessions"] if entry["processId"] == second.process.pid]
    check(len(mine) == 1 and len(other) == 1, "both sessions must be listed: " + str(listed))
    check(mine[0]["isThisSession"], "the session that answered must be marked as such: " + str(mine[0]))
    check(not other[0]["isThisSession"], "the other session must not be: " + str(other[0]))
    check(mine[0]["url"] == session.info["url"], "each session must be listed with its own url")
    check("token" not in other[0], "a session's token must not be handed to an agent: " + str(other[0]))
    #
    endpoint.tool("get_status", {"process_id": session.process.pid})
    text = endpoint.failing_tool("get_status", {"process_id": second.process.pid})
    check(second.info["url"] in text, "the refusal must name the url that would have served it: " + text)
    print("list_sessions: two instances on " + str(port) + " and " + str(second.info["port"]) +
          ", and a process id that is not this one is refused")
    return second


def check_pruning(endpoint, second):
    """A session that died leaves its file behind, and the next reader prunes it."""
    stale = second.path
    second.close()
    check(os.path.exists(stale), "the killed session must have left its file: " + stale)
    listed = json.loads(endpoint.tool("list_sessions"))
    check(all(entry["processId"] != second.process.pid for entry in listed["sessions"]),
          "a session whose process is gone must not be listed: " + str(listed))
    check(not os.path.exists(stale), "the stale file must have been pruned: " + stale)
    print("list_sessions: the file of the killed session was pruned")


def check_unreachable(session):
    """The one thing this transport gives up: with the session closed, nothing answers."""
    url, token = session.info["url"], session.info["token"]
    session.close()
    time.sleep(1.0)
    status, text, _ = request(url, b"{}", token)
    check(status == 0, "with the session closed the endpoint must be unreachable, not HTTP " + str(status))
    print("closed: the address no longer answers, which is what no bridge process means")


def main():
    port = free_port()
    session = ChildSession(port)
    try:
        session.wait_until_listening()
        check_publication(session, port)
        check_gates(session.info["url"], session.info["token"], port)
        endpoint = session.endpoint()
        check_handshake(endpoint)
        check_tool_list(endpoint)
        check_the_guide(endpoint)
        wait_until_idle(endpoint)
        check_reading_tools(endpoint, session)
        check_private_scripts(endpoint)
        check_utf8_body(endpoint)
        check_describing_a_type(endpoint)
        check_writing_tools(endpoint)
        check_a_long_call(endpoint)
        check_cancel_call(endpoint)
        check_screenshot(endpoint)
        check_refusals(endpoint)
        check_argument_values(endpoint)
        check_notification_not_run(endpoint)
        second = check_second_instance(endpoint, session, port)
        check_pruning(endpoint, second)
        check_unreachable(session)
    finally:
        session.close()
    print("TEST PASSED")


main()
