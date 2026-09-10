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
#  PrePoMax.com --script Test_Mcp_Http.py -g No -x Yes -w <work directory>
#
#Expected process exit code 0.

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
EXPECTED_TOOLS = ["list_sessions", "get_status", "describe_api", "read_model", "eval_expression",
                  "run_python", "get_history", "run_job", "get_job", "screenshot"]


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
        self.process = subprocess.Popen([sys.executable, "--rpc", "On", "--rpcPort", str(port),
                                         "-g", "No", "--noSettingsSave",
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
    print("read_model: " + str(len(model)) + " entries, unit system " + str(model["properties"]["unit_system"]))
    #
    described = json.loads(endpoint.tool("describe_api", {"path": "pmx.model"}))
    names = [member["name"] for member in described["members"]]
    check("parts" in names, "pmx.model must describe its collections: " + str(names))
    print("describe_api: pmx.model offers " + str(len(names)) + " members")


def check_writing_tools(endpoint):
    result = json.loads(endpoint.tool("run_python", {"source": "pmx.model.parameters.add('mcp_a', 1.5)"}))
    check(not result["failed"], "the call must not fail: " + result["error"])
    check(result["recordedCommands"] == ["CAddParameter"],
          "the change must be recorded as one command: " + str(result["recordedCommands"]))
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


def check_screenshot(endpoint):
    result = json.loads(endpoint.tool("screenshot"))
    check(os.path.exists(result["fileName"]), "the screenshot must exist: " + result["fileName"])
    print("screenshot: " + result["length"] + " bytes")
    os.remove(result["fileName"])


def check_refusals(endpoint):
    # A tool that does not exist is a protocol error, because the client asked for something it was
    # never offered
    answer = endpoint.call("tools/call", {"name": "no_such_tool", "arguments": {}})
    check("error" in answer, "an unknown tool must be a protocol error: " + str(answer))
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
        check_reading_tools(endpoint, session)
        check_writing_tools(endpoint)
        check_screenshot(endpoint)
        check_refusals(endpoint)
        second = check_second_instance(endpoint, session, port)
        check_pruning(endpoint, second)
        check_unreachable(session)
    finally:
        session.close()
    print("TEST PASSED")


main()
