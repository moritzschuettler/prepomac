#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#The acceptance test of the agent integration: a whole analysis driven through the MCP tools and
#nothing else. It builds the model, solves it, reads a result value back, and journals the session
#it produced - the way an agent would.
#
#The tools are reached over the MCP endpoint the application serves itself, on loopback HTTP, at the
#address the session publishes in its session file.
#
#It also checks what a long call does to the rest of the session. A handler runs on the connection
#it arrived on, the way a --script run drives the controller from a thread pool thread, so a call
#that takes six seconds must leave the window and the other connections alone. Marshalling the
#handler instead - which is what this layer did first - froze the window for the duration and
#deadlocked outright on opening results, which waits for asynchronous work of its own.
#
#  PrePoMax.com --script Test_Mcp_Workflow.py -g No -x Yes -w <work directory>
#
#Expected process exit code 0.

import json
import os
import shutil
import socket
import subprocess
import sys
import tempfile
import threading
import time
import urllib.error
import urllib.request

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
SESSIONS = os.path.join(os.environ["LOCALAPPDATA"], "PrePoMax", "sessions")
PROTOCOL_VERSION = "2024-11-05"
CHILD_TIMEOUT = 600
PREFIX = "Mcp_"
JOB = PREFIX + "Job"
STEP = PREFIX + "Static"
# How long the measured call holds its own thread
LONG_CALL_SECONDS = 6


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def free_port(first=8800):
    """A port nothing else holds, so the session lands on the one it is given."""
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


class Endpoint(object):
    """The MCP endpoint of the session, driven the way an MCP client drives it."""

    def __init__(self, url, token):
        self.url = url
        self.token = token
        self._id = 0
        self.call("initialize", {"protocolVersion": PROTOCOL_VERSION, "capabilities": {},
                                 "clientInfo": {"name": "Test_Mcp_Workflow", "version": "1.0"}})
        self.notify("notifications/initialized")

    def _post(self, message, timeout):
        request = urllib.request.Request(self.url, data=json.dumps(message).encode("utf-8"), method="POST")
        request.add_header("Content-Type", "application/json")
        request.add_header("Authorization", "Bearer " + self.token)
        try:
            with urllib.request.urlopen(request, timeout=timeout) as answer:
                return answer.status, answer.read().decode("utf-8")
        except urllib.error.HTTPError as error:
            return error.code, error.read().decode("utf-8")

    def call(self, method, parameters=None, timeout=300):
        self._id += 1
        message = {"jsonrpc": "2.0", "id": self._id, "method": method}
        if parameters is not None:
            message["params"] = parameters
        status, text = self._post(message, timeout)
        check(status == 200, method + " answered HTTP " + str(status) + ": " + text)
        answer = json.loads(text)
        check("error" not in answer, method + " failed: " + str(answer.get("error")))
        return answer["result"]

    def notify(self, method, parameters=None):
        message = {"jsonrpc": "2.0", "method": method}
        if parameters is not None:
            message["params"] = parameters
        status, text = self._post(message, 60)
        check(status == 202, method + " answered HTTP " + str(status) + ": " + text)

    def tool(self, name, arguments=None):
        result = self.call("tools/call", {"name": name, "arguments": arguments or {}})
        text = result["content"][0]["text"]
        check(not result.get("isError"), name + " reported an error: " + text)
        return text

    def json_tool(self, name, arguments=None):
        return json.loads(self.tool(name, arguments))

    def python(self, source):
        """run_python, the way an agent writes a step of its work."""
        return self.json_tool("run_python", {"source": source})

    def close(self):
        # There is nothing to shut down: the endpoint is the application, and an HTTP request holds
        # no connection of its own between calls
        return 0


class PipeProbe(object):
    """A second connection straight to the automation server, for measuring what a long call blocks."""

    def __init__(self, process_id, timeout=20.0):
        self.name = "\\\\.\\pipe\\PrePoMax." + str(process_id)
        self._id = 0
        # The listener arms one instance at a time, so a connection that arrives while it is arming
        # the next one is told the pipe is busy. A real client waits for it - that is what
        # NamedPipeClientStream.Connect does - and a raw client has to do it itself
        deadline = time.time() + timeout
        while True:
            try:
                self._pipe = open(self.name, "r+b", buffering=0)
                return
            except (IOError, OSError):
                if time.time() > deadline:
                    raise
                time.sleep(0.05)

    def call(self, method):
        self._id += 1
        request = {"jsonrpc": "2.0", "id": self._id, "method": method}
        self._pipe.write((json.dumps(request) + "\n").encode("utf-8"))
        data = b""
        while not data.endswith(b"\n"):
            piece = self._pipe.read(1)
            check(piece, "the session closed the connection during " + method)
            data += piece
        return json.loads(data.decode("utf-8"))

    def close(self):
        try:
            self._pipe.close()
        except (IOError, OSError):
            pass


class ChildSession(object):
    """A PrePoMax session with no window of its own, serving its own MCP endpoint."""

    def __init__(self, model=None, unit_system="MM_TON_S_C"):
        self.directory = tempfile.mkdtemp(prefix="mcp_flow_")
        self.log = open(os.path.join(self.directory, "child.log"), "w")
        arguments = [sys.executable, "--rpc", "On", "--rpcPort", str(free_port()), "-g", "No",
                     "--noSettingsSave", "--timeout", str(CHILD_TIMEOUT), "-w", self.directory]
        if model is not None:
            arguments += ["-f", os.path.join(MODELS, model), "-u", unit_system]
        self.process = subprocess.Popen(arguments, stdout=self.log, stderr=subprocess.STDOUT)
        self.info = None

    @property
    def path(self):
        return os.path.join(SESSIONS, str(self.process.pid) + ".json")

    def wait_until_listening(self, timeout=120.0):
        """The session file is the discovery contract, written once the endpoint is up."""
        deadline = time.time() + timeout
        while time.time() < deadline:
            check(self.process.poll() is None, "the child session exited early")
            if os.path.exists(self.path):
                try:
                    self.info = json.load(open(self.path))
                    if self.info.get("url"):
                        return self.info
                except (ValueError, IOError):
                    pass
            time.sleep(0.2)
        raise Exception("TEST FAILED: no session file appeared at " + self.path)

    def endpoint(self):
        return Endpoint(self.info["url"], self.info["token"])

    def close(self):
        if self.process.poll() is None:
            self.process.kill()
            self.process.wait()
        self.log.close()
        shutil.rmtree(self.directory, ignore_errors=True)


def wait_until_idle(endpoint, timeout=120.0):
    """The server answers before the file on the command line is open, and says so until it is."""
    deadline = time.time() + timeout
    reason = ""
    while time.time() < deadline:
        status = endpoint.json_tool("get_status")
        if status["isIdle"]:
            return
        reason = status["busyReason"]
        time.sleep(0.2)
    raise Exception("TEST FAILED: the session stayed busy: " + reason)


def build_the_model(endpoint):
    """Everything a static analysis needs, one agent call per step of the work."""
    model = endpoint.json_tool("read_model")
    check(model["parts"], "the mesh must have been imported: " + str(model["parts"]))
    # A snapshot that could not read something says so rather than reporting it as empty
    check(model["unreadable"] == [], "read_model could not read: " + str(model["unreadable"]))
    print("read_model: part " + str(model["parts"][0]) + ", " + str(model["properties"]["unit_system"]))
    # An agent looks the call up before it writes it
    described = endpoint.json_tool("describe_api", {"path": "pmx.model.materials"})
    names = [member["name"] for member in described["members"]]
    check("add" in names, "the material collection must describe add: " + str(names))
    #
    result = endpoint.python("pmx.model.materials.add('" + PREFIX + "Steel', "
                           "[pmx.create.elastic(210000, 0.3), pmx.create.density(7.85e-9)])")
    check([entry["type"] for entry in result["recordedCommands"]] == ["CAddMaterial"],
          "the material must be one command: " + str(result["recordedCommands"]))
    # The whole parameter table is one command, and a parameter may use one given after it
    result = endpoint.python("pmx.model.parameters.set_all(['" + PREFIX + "load', '" + PREFIX + "unit'], "
                           "['" + PREFIX + "unit * 1000', 1.0], ['The applied force', ''])")
    check([entry["type"] for entry in result["recordedCommands"]] == ["CSetParameters"],
          "the parameter table must be one command: " + str(result["recordedCommands"]))
    #
    endpoint.python("part_selection = pmx.create.selection_by_ids('femodel', 'elements', 'parts', "
                  "[pmx.model.parts.first().id])" + os.linesep +
                  "pmx.model.sections.add_solid('" + PREFIX + "Section', '" + PREFIX + "Steel', "
                  "part_selection)")
    # The cube is a single hexahedron: nodes 1 to 4 are the bottom face, 5 to 8 the top one
    endpoint.python("pmx.model.node_sets.add('" + PREFIX + "Bottom', "
                  "pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4]))")
    endpoint.python("step = pmx.model.steps.add_static('" + STEP + "', time_period=1.0)" + os.linesep +
                  "step.bcs.add_fixed('" + PREFIX + "Fix', "
                  "pmx.create.region('NodeSetName', '" + PREFIX + "Bottom'))" + os.linesep +
                  "step.loads.add_concentrated_force('" + PREFIX + "Force', "
                  "pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [5, 6, 7, 8]), f3=-1000)")
    #
    model = endpoint.json_tool("read_model")
    check(PREFIX + "Steel" in model["materials"], "the material is missing: " + str(model["materials"]))
    check(PREFIX + "Section" in model["sections"], "the section is missing: " + str(model["sections"]))
    check(STEP in model["steps"], "the step is missing: " + str(model["steps"]))
    step = model["steps_detail"][0]
    check(PREFIX + "Fix" in step["bcs"], "the bc is missing: " + str(step))
    check(PREFIX + "Force" in step["loads"], "the load is missing: " + str(step))
    # A parameter is reported whole, so that an agent sees what it evaluates to and what it is for
    described = [p for p in model["parameters_detail"] if p["name"] == PREFIX + "load"]
    check(len(described) == 1, "the parameter is missing: " + str(model["parameters_detail"]))
    check(described[0]["value"] == 1000.0 and described[0]["description"] == "The applied force" and
          described[0]["equation"] == "=" + PREFIX + "unit*1000",
          "the parameter was not read whole: " + str(described[0]))
    print("built: material, section, node set, step, bc and load, all through run_python")


def solve(endpoint):
    """The analysis, started without waiting and polled the way an agent has to poll it."""
    endpoint.python("pmx.jobs.add('" + JOB + "')")
    started = time.time()
    answer = endpoint.json_tool("run_job", {"name": JOB})
    result = answer["run"]
    check(not result["failed"], "the analysis could not be started: " + result["error"])
    check(answer["name"] == JOB and answer["status"] != "", "run_job did not report the status: " + str(answer))
    print("run_job: %s, returned in %.1f s, recorded %s" % (answer["status"], time.time() - started,
                                                           result["recordedCommands"]))
    #
    deadline = time.time() + 300
    status = None
    while time.time() < deadline:
        status = endpoint.json_tool("get_job", {"name": JOB})["value"]["status"]
        if status not in ("None", "InQueue", "Running"):
            break
        time.sleep(0.5)
    check(status == "OK", "the analysis did not succeed, got " + str(status) + "\n" +
          endpoint.json_tool("get_job", {"name": JOB, "include_output": True})["value"].get("output", ""))
    print("get_job: %s after %.1f s" % (status, time.time() - started))


def read_the_result(endpoint):
    """The value the whole workflow exists to produce."""
    endpoint.python("pmx.jobs.open_results('" + JOB + "')")
    value = endpoint.json_tool("eval_expression", {"expression":
        "[pmx.results.current.get_nodal_value("
        "list(pmx.results.current.steps.get_ids())[0], "
        "list(pmx.results.current.steps[list(pmx.results.current.steps.get_ids())[0]]"
        ".increments.get_ids())[-1], 'DISP', 'U3', node) for node in (1, 5)]"})
    check(not value["failed"], "the result could not be read: " + value["error"])
    fixed, loaded = value["value"]
    check(abs(fixed) < 1e-12, "the fixed node must not move, got " + str(fixed))
    check(loaded < 0, "the loaded node must move down, got " + str(loaded))
    print("result: the fixed node is at %g, the loaded node at %g" % (fixed, loaded))


def journal_the_session(endpoint):
    """What the agent did, as the script a user would have written."""
    history = endpoint.json_tool("get_history", {"include_journal": True})
    journal = history["journal"]
    check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
    check(journal["emittedCommandCount"] == history["commandCount"],
          "every command must be emitted: " + str(journal))
    source = journal["source"]
    for expected in ["pmx.model.materials.add", "pmx.model.parameters.set_all", "'The applied force'",
                     "pmx.model.sections.add_solid", "pmx.model.node_sets.add", "pmx.model.steps.add_static",
                     "add_fixed", "add_concentrated_force", "pmx.jobs.add"]:
        check(expected in source, expected + " is missing from the journal:" + os.linesep + source)
    print("get_history: %d commands journalled into %d lines of Python"
          % (history["commandCount"], len(source.splitlines())))
    #
    result = endpoint.json_tool("screenshot")
    check(os.path.exists(result["fileName"]), "the screenshot is missing: " + result["fileName"])
    os.remove(result["fileName"])
    print("screenshot: " + str(result["length"]) + " bytes of the solved model")
    # The picture an agent takes for a report: no background, and rendered larger than the viewport
    result = endpoint.json_tool("screenshot", {"view": "results", "orientation": "Isometric",
                                               "zoom_to_fit": True, "transparent_background": True,
                                               "magnification": 2})
    check(os.path.exists(result["fileName"]), "the report screenshot is missing: " + result["fileName"])
    check(not result["isEmpty"], "the report screenshot must not be empty: " + result["fileName"])
    os.remove(result["fileName"])
    print("screenshot: " + str(result["length"]) + " bytes, transparent and magnified")


def check_a_fast_call_does_not_mark_the_session_busy(endpoint):
    """The state is only worth showing for a call that lasts, so a fast one must not touch it."""
    endpoint.python("fast_call_probe = 1 + 1")
    status = endpoint.json_tool("get_status")
    check(status["isIdle"], "a fast call must leave the session idle: " + status["busyReason"])
    print("fast call: the session stayed idle")


def check_a_long_call_blocks_nothing(session, endpoint):
    """A long call must mark itself busy, and leave the window and the other connections alone.

    It runs on the connection it arrived on, so the thread that owns the window stays free: ping is
    answered throughout, and a status from another connection - which does hop to that thread for the
    idle question - comes back long before the call is over. Once the call has lasted long enough to
    be worth showing, it takes the application's busy state, so the user cannot start editing in the
    middle of it and the status says which call is holding things.
    """
    pings = PipeProbe(session.process.pid)
    marshalled = PipeProbe(session.process.pid)
    gaps = []
    answers = []
    stop = threading.Event()

    def keep_pinging():
        while not stop.is_set():
            started = time.time()
            try:
                pings.call("ping")
            except Exception:
                break
            gaps.append(time.time() - started)
            time.sleep(0.1)

    def call_status_once():
        started = time.time()
        try:
            answer = marshalled.call("status")
            answers.append((time.time() - started, "error" in answer, answer.get("result", {})))
        except Exception:
            answers.append((time.time() - started, True, {}))

    pinger = threading.Thread(target=keep_pinging)
    pinger.start()
    waiter = threading.Thread(target=call_status_once)
    try:
        started = time.time()
        # A sleep is the honest proxy for a mesher: it holds the thread and nothing else
        source = "import time" + os.linesep + "time.sleep(" + str(LONG_CALL_SECONDS) + ")"
        threading.Timer(1.0, waiter.start).start()
        endpoint.python(source)
        elapsed = time.time() - started
    finally:
        stop.set()
        pinger.join(timeout=10)
        waiter.join(timeout=60)
        pings.close()
        marshalled.close()
    #
    check(elapsed >= LONG_CALL_SECONDS, "the long call must have taken its time, got %.1f s" % elapsed)
    check(gaps, "ping must have been answered during the long call")
    check(max(gaps) < 1.0, "ping must stay immediate during a long call, worst was %.2f s" % max(gaps))
    check(answers, "a status from another connection must come back during a long call")
    waited, refused, status = answers[0]
    check(not refused, "the status was refused after %.1f s: the window was not free" % waited)
    check(waited < LONG_CALL_SECONDS / 2.0,
          "the status waited %.1f s of a %d s call, so it was queued behind it" % (waited, LONG_CALL_SECONDS))
    # The call had been running for a second by then, so it had taken the state
    check(not status["isIdle"], "a long call must mark the session busy: " + str(status))
    check("Automation" in status["busyReason"],
          "the reason must name what is holding it: " + status["busyReason"])
    print("long call: %.1f s on its own connection" % elapsed)
    print("long call: ping answered %d times throughout, worst %.2f s" % (len(gaps), max(gaps)))
    print("long call: a status from another connection answered in %.1f s, so the window stayed free"
          % waited)
    print("long call: while it ran the session reported busy - " + status["busyReason"])
    # And the state is given back
    after = endpoint.json_tool("get_status")
    check(after["isIdle"], "the state must be given back when the call ends: " + after["busyReason"])
    print("long call: the state was given back")


def run_the_workflow():
    session = ChildSession("cube.inp")
    endpoint = None
    try:
        session.wait_until_listening()
        endpoint = session.endpoint()
        wait_until_idle(endpoint)
        build_the_model(endpoint)
        solve(endpoint)
        read_the_result(endpoint)
        journal_the_session(endpoint)
        check_a_fast_call_does_not_mark_the_session_busy(endpoint)
        check_a_long_call_blocks_nothing(session, endpoint)
    finally:
        if endpoint is not None:
            endpoint.close()
        session.close()


def main():
    check(os.path.exists(os.path.join(MODELS, "cube.inp")), "the cube.inp fixture is missing from " + MODELS)
    run_the_workflow()
    print("TEST PASSED")


main()
