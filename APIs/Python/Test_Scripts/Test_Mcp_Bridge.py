#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the MCP server, PrePoMaxMcp.exe, the way an AI agent client uses it: over stdin and stdout,
#with the MCP handshake and one tools/call per tool. It starts a PrePoMax session of its own - with
#-g No and the automation server, so it has no window - and points the bridge at it.
#
#  PrePoMax.com --script Test_Mcp_Bridge.py -g No -x Yes -w <work directory>
#
#Expected process exit code 0.

import json
import os
import subprocess
import sys
import tempfile
import time

PROTOCOL_VERSION = "2024-11-05"
CHILD_TIMEOUT = 240
EXPECTED_TOOLS = ["list_sessions", "get_status", "describe_api", "read_model", "eval_expression",
                  "run_python", "get_history", "run_job", "get_job", "screenshot"]


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


class Bridge(object):
    """PrePoMaxMcp.exe, driven the way an MCP client drives it."""

    def __init__(self, process_id):
        executable = os.path.join(os.path.dirname(sys.executable), "PrePoMaxMcp.exe")
        check(os.path.exists(executable), "PrePoMaxMcp.exe is missing from " + os.path.dirname(executable))
        self.log = open(os.path.join(tempfile.gettempdir(), "mcp_bridge.log"), "w")
        self.process = subprocess.Popen([executable, "--pid", str(process_id)],
                                        stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=self.log)
        self._id = 0

    def send(self, message):
        self.process.stdin.write((json.dumps(message) + "\n").encode("utf-8"))
        self.process.stdin.flush()

    def call(self, method, parameters=None):
        self._id += 1
        message = {"jsonrpc": "2.0", "id": self._id, "method": method}
        if parameters is not None:
            message["params"] = parameters
        self.send(message)
        line = self.process.stdout.readline()
        check(line, "the bridge answered nothing to " + method)
        answer = json.loads(line.decode("utf-8"))
        check(answer.get("id") == self._id, method + " answered the wrong id: " + str(answer))
        return answer

    def notify(self, method, parameters=None):
        message = {"jsonrpc": "2.0", "method": method}
        if parameters is not None:
            message["params"] = parameters
        self.send(message)

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

    def close(self):
        try:
            self.process.stdin.close()
        except (IOError, OSError):
            pass
        deadline = time.time() + 20
        while time.time() < deadline:
            if self.process.poll() is not None:
                self.log.close()
                return self.process.returncode
            time.sleep(0.1)
        self.process.kill()
        self.log.close()
        return None


class ChildSession(object):
    """A PrePoMax session with no window of its own, for the bridge to attach to."""

    def __init__(self):
        self.directory = tempfile.mkdtemp(prefix="mcp_child_")
        self.log = open(os.path.join(self.directory, "child.log"), "w")
        self.process = subprocess.Popen([sys.executable, "--rpc", "On", "-g", "No", "--noSettingsSave",
                                         "--timeout", str(CHILD_TIMEOUT), "-w", self.directory],
                                        stdout=self.log, stderr=subprocess.STDOUT)

    def wait_until_listening(self, timeout=60.0):
        name = "\\\\.\\pipe\\PrePoMax." + str(self.process.pid)
        deadline = time.time() + timeout
        while time.time() < deadline:
            try:
                pipe = open(name, "r+b", buffering=0)
                pipe.close()
                return
            except (IOError, OSError):
                time.sleep(0.1)
        raise Exception("TEST FAILED: the child session never opened " + name)

    def close(self):
        self.process.kill()
        self.log.close()


def check_handshake(bridge):
    result = bridge.result("initialize", {"protocolVersion": PROTOCOL_VERSION,
                                          "capabilities": {},
                                          "clientInfo": {"name": "Test_Mcp_Bridge", "version": "1.0"}})
    check(result["protocolVersion"] == PROTOCOL_VERSION,
          "the server must answer the protocol version it was offered: " + result["protocolVersion"])
    check("tools" in result["capabilities"], "the server must offer tools: " + str(result["capabilities"]))
    check(result["serverInfo"]["name"] == "prepomax", "unexpected server name: " + result["serverInfo"]["name"])
    check("pmx" in result["instructions"], "the instructions must name the API root")
    check("busy" in result["instructions"], "the instructions must explain the busy answer")
    bridge.notify("notifications/initialized")
    # ping is answered after the handshake, which is how a client checks the server is alive
    bridge.result("ping")
    print("handshake: " + result["serverInfo"]["name"] + " " + result["serverInfo"]["version"] +
          ", protocol " + result["protocolVersion"])


def check_tool_list(bridge):
    result = bridge.result("tools/list")
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
    print("tools: " + str(len(tools)) + " tools, every one with a schema")


def check_reading_tools(bridge, process_id):
    text = bridge.tool("list_sessions")
    sessions = json.loads(text)
    check(sessions["count"] >= 1, "the child session must be listed: " + text)
    check(any(session["processId"] == process_id for session in sessions["sessions"]),
          "the child session must be among them: " + text)
    #
    status = json.loads(bridge.tool("get_status"))
    check(status["isRunning"], "the session must be running: " + str(status))
    check(status["isIdle"], "the session must be idle: " + status["busyReason"])
    #
    model = json.loads(bridge.tool("read_model"))
    check("properties" in model, "the snapshot must carry the model properties: " + str(model)[:200])
    check("parts" in model, "the snapshot must carry the collections: " + str(sorted(model)))
    check("jobs" in model, "the snapshot must carry the analyses: " + str(sorted(model)))
    print("read_model: " + str(len(model)) + " entries, unit system " + str(model["properties"]["unit_system"]))
    #
    described = json.loads(bridge.tool("describe_api", {"path": "pmx.model"}))
    names = [member["name"] for member in described["members"]]
    check("parts" in names, "pmx.model must describe its collections: " + str(names))
    print("describe_api: pmx.model offers " + str(len(names)) + " members")


def check_writing_tools(bridge):
    text = bridge.tool("run_python", {"source": "pmx.model.parameters.add('mcp_a', 1.5)"})
    result = json.loads(text)
    check(not result["failed"], "the call must not fail: " + result["error"])
    check(result["recordedCommands"] == ["CAddParameter"],
          "the change must be recorded as one command: " + str(result["recordedCommands"]))
    #
    value = json.loads(bridge.tool("eval_expression", {"expression": "pmx.model.parameters['mcp_a'].value"}))
    check(not value["failed"], "the parameter must be readable: " + value["error"])
    print("run_python: recorded " + str(result["recordedCommands"]) + ", read back " + value["repr"])
    #
    history = json.loads(bridge.tool("get_history", {"include_journal": True}))
    check(history["commandCount"] >= 1, "the history must hold the change: " + str(history))
    check("pmx.model.parameters.add" in history["journal"]["source"],
          "the journal must carry the call: " + history["journal"]["source"])
    print("get_history: " + str(history["commandCount"]) + " commands, journalled")


def check_screenshot(bridge):
    result = json.loads(bridge.tool("screenshot"))
    check(os.path.exists(result["fileName"]), "the screenshot must exist: " + result["fileName"])
    print("screenshot: " + result["length"] + " bytes")
    os.remove(result["fileName"])


def check_refusals(bridge):
    # A tool that does not exist is a protocol error, because the client asked for something it was
    # never offered
    answer = bridge.call("tools/call", {"name": "no_such_tool", "arguments": {}})
    check("error" in answer, "an unknown tool must be a protocol error: " + str(answer))
    # A tool that cannot be served is a result the agent reads, so it can decide what to do next
    text = bridge.failing_tool("run_python", {})
    check("source" in text, "the refusal must name the missing argument: " + text)
    text = bridge.failing_tool("eval_expression", {"expression": "pmx.no_such_member"})
    check(len(text) > 0, "a failing expression must say what went wrong")
    # A method the server does not serve
    answer = bridge.call("resources/list")
    check("error" in answer, "an unserved method must be an error: " + str(answer))
    print("refusals: an unknown tool, a missing argument and an unserved method")


def check_stale_pruning(bridge, process_id):
    """A session that died leaves its file behind, and the next client that looks prunes it."""
    stale = os.path.join(os.environ["LOCALAPPDATA"], "PrePoMax", "sessions", str(process_id) + ".json")
    check(os.path.exists(stale), "the killed session must have left its file: " + stale)
    sessions = json.loads(bridge.tool("list_sessions"))
    check(all(session["processId"] != process_id for session in sessions["sessions"]),
          "a session whose process is gone must not be listed: " + str(sessions))
    check(not os.path.exists(stale), "the stale file must have been pruned: " + stale)
    print("list_sessions: the file of the killed session was pruned")


def check_no_session():
    """With nothing listening, every tool says so instead of failing to start."""
    executable = os.path.join(os.path.dirname(sys.executable), "PrePoMaxMcp.exe")
    process = subprocess.Popen([executable, "--pid", "999999"],
                               stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
    try:
        request = {"jsonrpc": "2.0", "id": 1, "method": "tools/call",
                   "params": {"name": "get_status", "arguments": {}}}
        process.stdin.write((json.dumps(request) + "\n").encode("utf-8"))
        process.stdin.flush()
        answer = json.loads(process.stdout.readline().decode("utf-8"))
        text = answer["result"]["content"][0]["text"]
        check(answer["result"]["isError"], "a missing session must be reported as an error result")
        check("automation server" in text, "the message must explain what to do: " + text)
        print("no session: " + text.split(".")[0])
    finally:
        try:
            process.stdin.close()
        except (IOError, OSError):
            pass
        process.kill()


def main():
    session = ChildSession()
    bridge = None
    try:
        session.wait_until_listening()
        bridge = Bridge(session.process.pid)
        check_handshake(bridge)
        check_tool_list(bridge)
        check_reading_tools(bridge, session.process.pid)
        check_writing_tools(bridge)
        check_screenshot(bridge)
        check_refusals(bridge)
    finally:
        # The child is killed first, so the last thing the bridge is asked is whether it notices
        session.close()
        if bridge is not None:
            check_stale_pruning(bridge, session.process.pid)
        code = bridge.close() if bridge is not None else None
    check(code == 0, "the bridge must exit when its input closes, but it returned " + str(code))
    print("bridge: closed cleanly on end of input")
    #
    check_no_session()
    print("TEST PASSED")


main()
