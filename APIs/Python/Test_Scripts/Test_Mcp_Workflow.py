#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#The acceptance test of the agent integration: a whole analysis driven through the MCP tools and
#nothing else. It builds the model, solves it, reads a result value back, and journals the session
#it produced - the way an agent would.
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
import subprocess
import sys
import tempfile
import threading
import time

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
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


class Bridge(object):
    """PrePoMaxMcp.exe, driven the way an MCP client drives it."""

    def __init__(self, process_id):
        executable = os.path.join(os.path.dirname(sys.executable), "PrePoMaxMcp.exe")
        check(os.path.exists(executable), "PrePoMaxMcp.exe is missing from " + os.path.dirname(executable))
        self.log = open(os.path.join(tempfile.gettempdir(), "mcp_workflow_bridge.log"), "w")
        self.process = subprocess.Popen([executable, "--pid", str(process_id)],
                                        stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=self.log)
        self._id = 0
        self.call("initialize", {"protocolVersion": PROTOCOL_VERSION, "capabilities": {},
                                 "clientInfo": {"name": "Test_Mcp_Workflow", "version": "1.0"}})
        self.process.stdin.write((json.dumps({"jsonrpc": "2.0",
                                              "method": "notifications/initialized"}) + "\n").encode("utf-8"))
        self.process.stdin.flush()

    def call(self, method, parameters=None):
        self._id += 1
        message = {"jsonrpc": "2.0", "id": self._id, "method": method}
        if parameters is not None:
            message["params"] = parameters
        self.process.stdin.write((json.dumps(message) + "\n").encode("utf-8"))
        self.process.stdin.flush()
        line = self.process.stdout.readline()
        check(line, "the bridge answered nothing to " + method)
        answer = json.loads(line.decode("utf-8"))
        check("error" not in answer, method + " failed: " + str(answer.get("error")))
        return answer["result"]

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


class PipeProbe(object):
    """A second connection straight to the automation server, for measuring what a long call blocks."""

    def __init__(self, process_id, timeout=20.0):
        self.name = "\\\\.\\pipe\\PrePoMax." + str(process_id)
        self._id = 0
        # The listener arms one instance at a time, so a connection that arrives while it is arming
        # the next one is told the pipe is busy. A real client waits for it - that is what
        # NamedPipeClientStream.Connect does for the bridge - and a raw client has to do it itself
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
    """A PrePoMax session with no window of its own, for the bridge to attach to."""

    def __init__(self, model=None, unit_system="MM_TON_S_C"):
        self.directory = tempfile.mkdtemp(prefix="mcp_flow_")
        self.log = open(os.path.join(self.directory, "child.log"), "w")
        arguments = [sys.executable, "--rpc", "On", "-g", "No", "--noSettingsSave",
                     "--timeout", str(CHILD_TIMEOUT), "-w", self.directory]
        if model is not None:
            arguments += ["-f", os.path.join(MODELS, model), "-u", unit_system]
        self.process = subprocess.Popen(arguments, stdout=self.log, stderr=subprocess.STDOUT)

    def wait_until_listening(self, timeout=90.0):
        deadline = time.time() + timeout
        name = "\\\\.\\pipe\\PrePoMax." + str(self.process.pid)
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


def wait_until_idle(bridge, timeout=120.0):
    """The server answers before the file on the command line is open, and says so until it is."""
    deadline = time.time() + timeout
    reason = ""
    while time.time() < deadline:
        status = bridge.json_tool("get_status")
        if status["isIdle"]:
            return
        reason = status["busyReason"]
        time.sleep(0.2)
    raise Exception("TEST FAILED: the session stayed busy: " + reason)


def build_the_model(bridge):
    """Everything a static analysis needs, one agent call per step of the work."""
    model = bridge.json_tool("read_model")
    check(model["parts"], "the mesh must have been imported: " + str(model["parts"]))
    # A snapshot that could not read something says so rather than reporting it as empty
    check(model["unreadable"] == [], "read_model could not read: " + str(model["unreadable"]))
    print("read_model: part " + str(model["parts"][0]) + ", " + str(model["properties"]["unit_system"]))
    # An agent looks the call up before it writes it
    described = bridge.json_tool("describe_api", {"path": "pmx.model.materials"})
    names = [member["name"] for member in described["members"]]
    check("add" in names, "the material collection must describe add: " + str(names))
    #
    result = bridge.python("pmx.model.materials.add('" + PREFIX + "Steel', "
                           "[pmx.create.elastic(210000, 0.3), pmx.create.density(7.85e-9)])")
    check(result["recordedCommands"] == ["CAddMaterial"],
          "the material must be one command: " + str(result["recordedCommands"]))
    #
    bridge.python("part_selection = pmx.create.selection_by_ids('femodel', 'elements', 'parts', "
                  "[pmx.model.parts.first().id])" + os.linesep +
                  "pmx.model.sections.add_solid('" + PREFIX + "Section', '" + PREFIX + "Steel', "
                  "part_selection)")
    # The cube is a single hexahedron: nodes 1 to 4 are the bottom face, 5 to 8 the top one
    bridge.python("pmx.model.node_sets.add('" + PREFIX + "Bottom', "
                  "pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4]))")
    bridge.python("step = pmx.model.steps.add_static('" + STEP + "', time_period=1.0)" + os.linesep +
                  "step.bcs.add_fixed('" + PREFIX + "Fix', "
                  "pmx.create.region('NodeSetName', '" + PREFIX + "Bottom'))" + os.linesep +
                  "step.loads.add_concentrated_force('" + PREFIX + "Force', "
                  "pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [5, 6, 7, 8]), f3=-1000)")
    #
    model = bridge.json_tool("read_model")
    check(PREFIX + "Steel" in model["materials"], "the material is missing: " + str(model["materials"]))
    check(PREFIX + "Section" in model["sections"], "the section is missing: " + str(model["sections"]))
    check(STEP in model["steps"], "the step is missing: " + str(model["steps"]))
    step = model["steps_detail"][0]
    check(PREFIX + "Fix" in step["bcs"], "the bc is missing: " + str(step))
    check(PREFIX + "Force" in step["loads"], "the load is missing: " + str(step))
    print("built: material, section, node set, step, bc and load, all through run_python")


def solve(bridge):
    """The analysis, started without waiting and polled the way an agent has to poll it."""
    bridge.python("pmx.jobs.add('" + JOB + "')")
    started = time.time()
    result = bridge.json_tool("run_job", {"name": JOB})
    check(not result["failed"], "the analysis could not be started: " + result["error"])
    print("run_job: returned in %.1f s, recorded %s" % (time.time() - started, result["recordedCommands"]))
    #
    deadline = time.time() + 300
    status = None
    while time.time() < deadline:
        status = bridge.json_tool("get_job", {"name": JOB})["value"]["status"]
        if status not in ("None", "InQueue", "Running"):
            break
        time.sleep(0.5)
    check(status == "OK", "the analysis did not succeed, got " + str(status) + "\n" +
          bridge.json_tool("get_job", {"name": JOB, "include_output": True})["value"].get("output", ""))
    print("get_job: %s after %.1f s" % (status, time.time() - started))


def read_the_result(bridge):
    """The value the whole workflow exists to produce."""
    bridge.python("pmx.jobs.open_results('" + JOB + "')")
    value = bridge.json_tool("eval_expression", {"expression":
        "[pmx.results.current.get_nodal_value("
        "list(pmx.results.current.steps.get_ids())[0], "
        "list(pmx.results.current.steps[list(pmx.results.current.steps.get_ids())[0]]"
        ".increments.get_ids())[-1], 'DISP', 'U3', node) for node in (1, 5)]"})
    check(not value["failed"], "the result could not be read: " + value["error"])
    fixed, loaded = value["value"]
    check(abs(fixed) < 1e-12, "the fixed node must not move, got " + str(fixed))
    check(loaded < 0, "the loaded node must move down, got " + str(loaded))
    print("result: the fixed node is at %g, the loaded node at %g" % (fixed, loaded))


def journal_the_session(bridge):
    """What the agent did, as the script a user would have written."""
    history = bridge.json_tool("get_history", {"include_journal": True})
    journal = history["journal"]
    check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
    check(journal["emittedCommandCount"] == history["commandCount"],
          "every command must be emitted: " + str(journal))
    source = journal["source"]
    for expected in ["pmx.model.materials.add", "pmx.model.sections.add_solid", "pmx.model.node_sets.add",
                     "pmx.model.steps.add_static", "add_fixed", "add_concentrated_force", "pmx.jobs.add"]:
        check(expected in source, expected + " is missing from the journal:" + os.linesep + source)
    print("get_history: %d commands journalled into %d lines of Python"
          % (history["commandCount"], len(source.splitlines())))
    #
    result = bridge.json_tool("screenshot")
    check(os.path.exists(result["fileName"]), "the screenshot is missing: " + result["fileName"])
    os.remove(result["fileName"])
    print("screenshot: " + result["length"] + " bytes of the solved model")


def check_a_fast_call_does_not_mark_the_session_busy(bridge):
    """The state is only worth showing for a call that lasts, so a fast one must not touch it."""
    bridge.python("fast_call_probe = 1 + 1")
    status = bridge.json_tool("get_status")
    check(status["isIdle"], "a fast call must leave the session idle: " + status["busyReason"])
    print("fast call: the session stayed idle")


def check_a_long_call_blocks_nothing(session, bridge):
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
        bridge.python(source)
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
    after = bridge.json_tool("get_status")
    check(after["isIdle"], "the state must be given back when the call ends: " + after["busyReason"])
    print("long call: the state was given back")


def run_the_workflow():
    session = ChildSession("cube.inp")
    bridge = None
    try:
        session.wait_until_listening()
        bridge = Bridge(session.process.pid)
        wait_until_idle(bridge)
        build_the_model(bridge)
        solve(bridge)
        read_the_result(bridge)
        journal_the_session(bridge)
        check_a_fast_call_does_not_mark_the_session_busy(bridge)
        check_a_long_call_blocks_nothing(session, bridge)
    finally:
        if bridge is not None:
            bridge.close()
        session.close()


def main():
    check(os.path.exists(os.path.join(MODELS, "cube.inp")), "the cube.inp fixture is missing from " + MODELS)
    run_the_workflow()
    print("TEST PASSED")


main()
