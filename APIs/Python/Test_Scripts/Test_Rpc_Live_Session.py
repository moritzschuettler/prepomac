#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks what the automation server promises about a session it drives: that a change an agent makes
#is recorded as the command a user would have produced by hand, and that read only mode refuses to
#change the model whatever Python is sent. It also replays the journal of replaces that set values back
#to their defaults, which a journal must write out because a replace keeps what it is not given, and
#round trips a varied model: the journal of the session that built it is replayed in a second, fresh
#session, and the CalculiX decks the two sessions export must be the same.
#
#And it checks a call that outlasts the request timeout. A session started with --rpcTimeout 5 is sent
#Python that sleeps longer than that: the caller must be answered that the call is still running, with
#an id, while the call runs to the end and records its command; wait must hand its answer over once,
#and status must name it while another change waits for its turn and is refused.
#
#Neither can be checked from inside a recorded script, because a recorded script suppresses the
#recording of the commands inside it. So this script starts sessions of its own - PrePoMax with
#-g No and the automation server, which has no window and no history of its own - and drives them
#from outside.
#
#  PrePoMax.com --script Test_Rpc_Live_Session.py -g No -x Yes -w <work directory>
#
#Expected process exit code 0.

import io
import json
import os
import re
import subprocess
import sys
import tempfile
import time

CHILD_TIMEOUT = 180
MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
KILL_PROBE = "read_only_kill_probe"
# The request timeout of the session the long calls are sent to, the smallest the server accepts
REQUEST_TIMEOUT = 5
BUSY = -32001
INVALID_PARAMS = -32602
# The two lines the CalculiX exporter stamps with a per run hash and the date it was written
VOLATILE_EXPORT_LINE = re.compile(r"^\s*\**\s*(Hash:|Date:)", re.IGNORECASE)
# A model that touches most of the areas a journal writes, including replaces and a parameter table
ROUND_TRIP_MODEL = [
    "pmx.model.parameters.set_all(['rt_load', 'rt_unit'], ['rt_unit * 1000', 1.0], ['The applied force', ''])",
    "pmx.model.materials.add('rt_steel', [pmx.create.elastic(210000, 0.3), pmx.create.density(7.85e-9)])",
    "pmx.model.materials.replace('rt_steel', description='Steel')",
    "pmx.model.sections.add_solid('rt_section', 'rt_steel', pmx.create.selection_by_ids('femodel', 'elements', "
    "'parts', [pmx.model.parts.first().id]))",
    "pmx.model.node_sets.add('rt_bottom', pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4]))",
    "pmx.model.element_sets.add('rt_cube', pmx.create.selection_by_ids('femodel', 'elements', 'elements', [1]))",
    "pmx.model.surfaces.add('rt_face', pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', {1: [2]}))",
    "pmx.model.amplitudes.add('rt_ramp', [[0, 0], [1, 1]])",
    "step = pmx.model.steps.add_static('rt_static', time_period=1.0)",
    "step.bcs.add_fixed('rt_fix', pmx.create.region('NodeSetName', 'rt_bottom'))",
    "step.bcs.add_displacement_rotation('rt_dr', pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [7, 8]), "
    "u1=0, u2=0, u3=1.5)",
    "step.bcs.replace_displacement_rotation('rt_dr', free_dofs=['U3'])",
    "step.loads.add_concentrated_force('rt_force', pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [5, 6]), "
    "f3=-1000)",
    "step.loads.add_pressure('rt_pressure', pmx.create.region('SurfaceName', 'rt_face'), 2.5)"]


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


class Client(object):
    """One connection to the automation server of another process."""

    def __init__(self, pid, timeout=60.0):
        self._id = 0
        self._pipe = None
        name = "\\\\.\\pipe\\PrePoMax." + str(pid)
        deadline = time.time() + timeout
        while True:
            try:
                self._pipe = open(name, "r+b", buffering=0)
                return
            except (IOError, OSError):
                # The child is still starting: it opens the pipe once the window is up
                if time.time() > deadline:
                    raise Exception("TEST FAILED: the child session never opened " + name)
                time.sleep(0.1)

    def close(self):
        if self._pipe is not None:
            try:
                self._pipe.close()
            except (IOError, OSError):
                pass
            self._pipe = None

    def call(self, method, parameters=None):
        self._id += 1
        request = {"jsonrpc": "2.0", "id": self._id, "method": method}
        if parameters is not None:
            request["params"] = parameters
        self._pipe.write((json.dumps(request) + "\n").encode("utf-8"))
        data = b""
        while not data.endswith(b"\n"):
            piece = self._pipe.read(1)
            check(piece, "the child closed the connection during " + method)
            data += piece
        answer = json.loads(data.decode("utf-8"))
        check(answer.get("id") == self._id, method + " answered the wrong id: " + str(answer))
        return answer

    def result(self, method, parameters=None):
        answer = self.call(method, parameters)
        check("error" not in answer, method + " failed: " + str(answer.get("error")))
        return answer["result"]

    def error(self, method, parameters=None):
        answer = self.call(method, parameters)
        check("error" in answer, method + " was expected to fail but returned " + str(answer.get("result")))
        return answer["error"]


class ChildSession(object):
    """A PrePoMax session with no window of its own, driven through its automation server."""

    def __init__(self, mode, file_name=None, arguments=None):
        self.directory = tempfile.mkdtemp(prefix="rpc_child_")
        self.log = open(os.path.join(self.directory, "child.log"), "w")
        # The model is given explicitly. A session with no window still opens the last file of the
        # interactive user when the setting says so, and the history of that model is not what this
        # test is about
        if file_name is None:
            file_name = os.path.join(MODELS, "cube.inp")
        self.process = subprocess.Popen([sys.executable, "--rpc", mode, "-g", "No", "--noSettingsSave",
                                         "-f", file_name, "-u", "MM_TON_S_C",
                                         "--timeout", str(CHILD_TIMEOUT), "-w", self.directory] +
                                        (arguments or []),
                                        stdout=self.log, stderr=subprocess.STDOUT)
        self.client = None

    def connect(self):
        """The server answers before the session has finished starting, and says so until it has."""
        self.client = Client(self.process.pid)
        deadline = time.time() + 120
        reason = ""
        while time.time() < deadline:
            status = self.client.result("status")
            if status["isIdle"]:
                return self.client
            reason = status["busyReason"]
            time.sleep(0.2)
        raise Exception("TEST FAILED: the child stayed busy: " + reason)

    def close(self):
        if self.client is not None:
            try:
                # A hidden session discards unsaved changes when it closes, so exit says so explicitly
                self.client.result("exit", {"discard_changes": True})
            except Exception:
                pass
            self.client.close()
            self.client = None
        # exit closes the application the ordinary way, so it is given a moment before it is killed
        deadline = time.time() + 30
        while time.time() < deadline:
            if self.process.poll() is not None:
                self.log.close()
                return self.process.returncode
            time.sleep(0.1)
        self.process.kill()
        self.log.close()
        return None


def check_recording(client):
    """A change an agent makes must land in the history as the command a user would have produced."""
    status = client.result("status")
    check(status["isRunning"], "the child server must be running")
    check(status["isWindowHidden"], "the child must have no window")
    check(not status["isReadOnly"], "the child must accept changes")
    check(not status["isCommandRecordingSuppressed"], "a session of its own records what it is told")
    check(status["isIdle"], "the child must be idle: " + status["busyReason"])
    before = status["model"]["commandCount"]
    # One API call is one command
    result = client.result("exec", {"source": "pmx.model.parameters.add('live_a', 1.0)"})
    check(not result["failed"], "adding a parameter must not fail: " + result["error"])
    check(len(result["recordedCommands"]) == 1, "one command must be recorded: " + str(result["recordedCommands"]))
    recorded = result["recordedCommands"][0]["type"]
    check(recorded.startswith("C"), "a recorded command must be a command class: " + recorded)
    # The description names the item, so a caller can tell one recorded command from the next
    described = result["recordedCommands"][0]["description"]
    check("live_a" in described, "the description must name the item: " + described)
    status = client.result("status")
    check(status["model"]["commandCount"] == before + 1,
          "the history must have grown by one: " + str(status["model"]["commandCount"]))
    check(status["model"]["hasUnsavedChanges"], "a changed model must be reported as changed")
    print("recording: one call recorded " + recorded)
    # Two API calls in one source are two commands
    result = client.result("exec", {"source": "pmx.model.parameters.add('live_b', 2.0)" + os.linesep +
                                              "pmx.model.parameters.add('live_c', 3.0)"})
    check(not result["failed"], "the two calls must not fail: " + result["error"])
    check(len(result["recordedCommands"]) == 2,
          "two calls must be two commands: " + str(result["recordedCommands"]))
    # A source recorded as a script is one command whatever it does inside
    result = client.result("exec", {"source": "pmx.model.parameters.add('live_d', 4.0)" + os.linesep +
                                              "pmx.model.parameters.add('live_e', 5.0)",
                                    "record": "script"})
    check(not result["failed"], "the recorded script must not fail: " + result["error"])
    check([entry["type"] for entry in result["recordedCommands"]] == ["CRunPythonScript"],
          "a script must be recorded as one command: " + str(result["recordedCommands"]))
    print("recording: two calls recorded two commands, a script recorded one")
    # Everything the agent asked for is in the model
    value = client.result("eval", {"expression": "sorted(pmx.model.parameters.get_names())"})
    check(not value["failed"], "the names must be readable: " + value["error"])
    for name in ("live_a", "live_b", "live_c", "live_d", "live_e"):
        check(name in value["value"], name + " is missing: " + str(value["value"]))
    print("recording: all five parameters are in the model")


def check_read_only(client):
    """Read only mode must refuse to change the model, whatever Python is sent."""
    status = client.result("status")
    check(status["isReadOnly"], "the child must be in read only mode")
    before = status["model"]["commandCount"]
    # Read only does not refuse a method, it blocks the command history: the source runs and the
    # change is refused where every state change of the application passes through
    result = client.result("exec", {"source": "pmx.model.parameters.add('blocked', 1.0)"})
    check(result["failed"], "a change must fail in read only mode")
    check("read only" in result["error"], "the refusal must say why: " + result["error"])
    check(result["recordedCommands"] == [], "nothing may be recorded: " + str(result["recordedCommands"]))
    # Reading keeps working, which is what an agent mostly does
    value = client.result("eval", {"expression": "pmx.model.parameters.get_names()"})
    check(not value["failed"], "reading must still work: " + value["error"])
    result = client.result("exec", {"source": "read_only_probe = len(pmx.model.parts.get_names())"})
    check(not result["failed"], "source that changes nothing must run: " + result["error"])
    # An expression that tries to change the model is refused the same way
    value = client.result("eval", {"expression": "pmx.model.parameters.add('blocked', 1.0)"})
    check(value["failed"], "an expression that changes the model must fail in read only mode")
    check("read only" in value["error"], "the refusal must say why: " + value["error"])
    check(value["recordedCommands"] == [], "nothing may be recorded: " + str(value["recordedCommands"]))
    # Nothing of it survived
    value = client.result("eval", {"expression": "pmx.model.parameters.get_names()"})
    check("blocked" not in value["value"], "the refused parameter must not exist: " + str(value["value"]))
    status = client.result("status")
    check(status["model"]["commandCount"] == before, "the history must not have grown")
    # A new model clears the history, so it is refused before the history is touched
    check(before > 0, "the opened model must have commands in its history: " + str(before))
    result = client.result("exec", {"source": "pmx.new_model('ThreeD', 'MM_TON_S_C')"})
    check(result["failed"], "a new model must fail in read only mode")
    check("read only" in result["error"], "the refusal must say why: " + result["error"])
    check(result["recordedCommands"] == [], "nothing may be recorded: " + str(result["recordedCommands"]))
    status = client.result("status")
    check(status["model"]["commandCount"] == before,
          "a refused new model must keep the history of " + str(before) + " commands, it has " +
          str(status["model"]["commandCount"]))
    result = client.result("history")
    check(result["commandCount"] == before and len(result["commands"]) == before,
          "the history must still list " + str(before) + " commands: " + str(result["commandCount"]))
    # Killing an analysis records no command, so it is refused by the read only mode itself
    value = client.result("eval", {"expression": "len(pmx.jobs)"})
    check(not value["failed"] and value["value"] == 1, "the saved analysis must be readable: " + str(value))
    result = client.result("exec", {"source": "pmx.jobs['" + KILL_PROBE + "'].kill()"})
    check(result["failed"], "killing an analysis must fail in read only mode")
    check("read only" in result["error"], "the refusal must say why: " + result["error"])
    check(result["recordedCommands"] == [], "nothing may be recorded: " + str(result["recordedCommands"]))
    print("read only: a change refused by the history, an analysis kill refused, reading unaffected")


def save_model_with_analysis(client, file_name):
    """The read only session is given a model with an analysis, because it may not add one itself."""
    result = client.result("exec", {"source": "pmx.jobs.add('" + KILL_PROBE + "')" + os.linesep +
                                              "pmx.save(r'" + file_name + "')"})
    check(not result["failed"], "the model with an analysis must be saved: " + result["error"])
    check(os.path.isfile(file_name), "the saved model is missing: " + file_name)


def check_history(client):
    """The history an agent produced, and the journal of it."""
    result = client.result("history")
    check(result["commandCount"] >= 4, "the calls of this test must be in the history: " + str(result))
    types = [command["type"] for command in result["commands"]]
    check("CAddParameter" in types, "the recorded commands must be listed: " + str(types))
    check("CRunPythonScript" in types, "the recorded script must be listed: " + str(types))
    # The journal is the session written as the Python a user would have written
    result = client.result("history", {"include_journal": True})
    journal = result["journal"]
    check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
    check(journal["emittedCommandCount"] == result["commandCount"], "every command must be emitted: " + str(journal))
    check(journal["isInline"], "a short journal must come back inline")
    source = journal["source"]
    check("pmx.model.parameters.add" in source, "the journal must carry the calls: " + source)
    check("live_a" in source, "the journal must carry the arguments: " + source)
    print("history: " + str(result["commandCount"]) + " commands journalled into " +
          str(len(source.splitlines())) + " lines of Python")
    # A limit returns the tail of the history
    result = client.result("history", {"limit": 2})
    check(len(result["commands"]) == 2, "a limit must be honoured: " + str(result["commands"]))
    check(result["firstIndex"] == result["commandCount"] - 2, "a limit must return the tail: " + str(result))


def read_replaced_values(client):
    step = "pmx.model.steps['journal_static']"
    expressions = [step + ".bcs['journal_dr'].get_value('" + dof + "')" for dof in ("u1", "u2", "u3")]
    expressions += [step + ".bcs['journal_sm'].get_value('" + dof + "')" for dof in ("u1", "u2")]
    expressions += ["pmx.model.materials['journal_steel'].description",
                    step + ".loads['journal_pt'].get_value('auto_direction')"]
    value = client.result("eval", {"expression": "[" + ", ".join(expressions) + "]"})
    check(not value["failed"], "the replaced items must be readable: " + value["error"])
    return value["value"]


def check_journal_of_replaces(client):
    """A replace keeps what it is not given, so the journal must write out a value set back to its default."""
    model = "pmx.new_model('ThreeD', 'MM_TON_S_C')"
    result = client.result("exec", {"source": model + os.linesep +
                                              "pmx.import_file(r'" + os.path.join(MODELS, "cube.inp") + "')"})
    check(not result["failed"], "the new model must be built: " + result["error"])
    # Each value is set by the add and taken back to its default by the replace
    source = os.linesep.join([
        "pmx.model.steps.add_static('journal_static', time_period=1.0)",
        "bcs = pmx.model.steps['journal_static'].bcs",
        "bcs.add_displacement_rotation('journal_dr', pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', "
        "[7, 8]), u1=0, u2=0, u3=1.5)",
        "bcs.replace_displacement_rotation('journal_dr', free_dofs=['U3'])",
        "bcs.add_submodel('journal_sm', pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1]), "
        "step_id=1, u1=True, u2=True)",
        "bcs.replace_submodel('journal_sm', u2=False)",
        "pmx.model.materials.add('journal_steel', pmx.create.elastic(210000, 0.3), description='Steel')",
        "pmx.model.materials.replace('journal_steel', description='')",
        "loads = pmx.model.steps['journal_static'].loads",
        "loads.add_pre_tension('journal_pt', pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', "
        "{1: [1]}), 500, direction=[0, 0, 1])",
        "loads.replace_pre_tension('journal_pt', auto_direction=True)"])
    result = client.result("exec", {"source": source})
    check(not result["failed"], "the replaces must not fail: " + result["error"])
    recorded = read_replaced_values(client)
    check(recorded[2] is None, "u3 was not freed: " + str(recorded))
    check(recorded[3] is True and recorded[4] is False, "the submodel u2 flag was not turned off: " + str(recorded))
    check(not recorded[5], "the material description was not cleared: " + str(recorded))
    check(recorded[6] is True, "the pre-tension did not return to the surface normal: " + str(recorded))
    # The journal writes the defaults out
    result = client.result("history", {"include_journal": True})
    journal = result["journal"]
    check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
    check(journal["isInline"], "a short journal must come back inline")
    journal_source = journal["source"]
    for expected in ["free_dofs=['U3'", "u2=False", "description=''", "auto_direction=True"]:
        check(expected in journal_source, expected + " is missing from the journal:" + os.linesep + journal_source)
    # The journal replayed on a new model rebuilds the same values
    result = client.result("exec", {"source": model})
    check(not result["failed"], "the model must be cleared for the replay: " + result["error"])
    result = client.result("exec", {"source": journal_source})
    check(not result["failed"], "the journal must replay: " + result["error"])
    replayed = read_replaced_values(client)
    check(replayed == recorded, "the replayed journal differs: " + str(replayed) + " against " + str(recorded))
    print("journal: a replace back to the defaults replays to " + str(replayed))


def export_deck(client, file_name):
    """The model as CalculiX reads it, without the lines that differ between two runs of the same model."""
    result = client.result("exec", {"source": "pmx.export_to_calculix(r'" + file_name + "')"})
    check(not result["failed"], "the model must export: " + result["error"])
    check(os.path.isfile(file_name), "the export is missing: " + file_name)
    with io.open(file_name, encoding="utf-8", errors="replace") as reader:
        return [line for line in reader.read().splitlines() if not VOLATILE_EXPORT_LINE.search(line)]


def first_difference(recorded, replayed):
    for index in range(min(len(recorded), len(replayed))):
        if recorded[index] != replayed[index]:
            return "line %d: %s against %s" % (index + 1, recorded[index], replayed[index])
    return "the decks have %d and %d lines" % (len(recorded), len(replayed))


def check_journal_round_trip(client):
    """The journal of a session rebuilds, in a session of its own, the model that session built."""
    directory = tempfile.mkdtemp(prefix="rpc_round_trip_")
    source = os.linesep.join(["pmx.new_model('ThreeD', 'MM_TON_S_C')",
                              "pmx.import_file(r'" + os.path.join(MODELS, "cube.inp") + "')"])
    result = client.result("exec", {"source": source})
    check(not result["failed"], "the new model must be built: " + result["error"])
    result = client.result("exec", {"source": os.linesep.join(ROUND_TRIP_MODEL)})
    check(not result["failed"], "the round trip model must be built: " + result["error"])
    # The journal is taken before the export, which is itself a recorded command
    result = client.result("history", {"include_journal": True})
    journal = result["journal"]
    check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
    check(journal["emittedCommandCount"] == result["commandCount"], "every command must be emitted: " + str(journal))
    check(journal["isInline"], "a short journal must come back inline")
    journal_source = journal["source"]
    for expected in ["add_pressure", "replace_displacement_rotation", "set_all", "add_solid"]:
        check(expected in journal_source, expected + " is missing from the journal:" + os.linesep + journal_source)
    recorded = export_deck(client, os.path.join(directory, "recorded.inp"))
    for expected in ["rt_steel", "rt_bottom", "rt_static"]:
        check(any(expected.upper() in line.upper() for line in recorded),
              "the recorded deck must carry " + expected + ", or the comparison checks nothing")
    # The journal replayed by a session that never saw the model
    session = ChildSession("On")
    try:
        replay = session.connect()
        result = replay.result("exec", {"source": "pmx.new_model('ThreeD', 'MM_TON_S_C')"})
        check(not result["failed"], "the model must be cleared for the replay: " + result["error"])
        result = replay.result("exec", {"source": journal_source})
        check(not result["failed"], "the journal must replay in a fresh session: " + result["error"])
        replayed = export_deck(replay, os.path.join(directory, "replayed.inp"))
    finally:
        session.close()
    check(recorded == replayed, "the replayed model differs from the recorded one: " +
          first_difference(recorded, replayed))
    print("journal: " + str(journal["emittedCommandCount"]) + " commands rebuild a deck of " + str(len(recorded)) +
          " lines in a fresh session")


def check_exit_guards(client):
    """Closing a session that has unsaved changes has to be asked for explicitly."""
    client.result("exec", {"source": "pmx.model.parameters.add('exit_guard', 1.0)"})
    error = client.error("exit")
    check("unsaved changes" in error["message"], "exit must warn about unsaved changes: " + error["message"])
    status = client.result("status")
    check(status["isRunning"], "a refused exit must leave the session alone")
    print("exit: refused while the model has unsaved changes")


def check_running_answer(answer, method, message):
    check(answer.get("running") is True, message + ": " + str(answer))
    check(isinstance(answer.get("callId"), int) and answer["callId"] > 0, "a running call needs an id: " + str(answer))
    check(answer["method"] == method, "the running call must name its method: " + str(answer))
    check("get_call" in answer["message"], "the answer must say how to collect it: " + answer["message"])
    check("failed" not in answer, "a running call has not failed or succeeded yet: " + str(answer))
    return answer["callId"]


def check_long_calls(client):
    """A call that outlasts the request timeout is answered, keeps running, is recorded and is collected once."""
    status = client.result("status")
    check(status["requestTimeout"] == REQUEST_TIMEOUT,
          "--rpcTimeout must set the request timeout: " + str(status["requestTimeout"]))
    check("wait" in status["methods"], "status must name wait: " + str(status["methods"]))
    check(status["calls"] == [], "no call may be pending yet: " + str(status["calls"]))
    result = client.result("exec", {"source": "import time"})
    check(not result["failed"], "time must import: " + result["error"])
    before = client.result("status")["model"]["commandCount"]
    # The default wait is the request timeout; the call sleeps well past it and then changes the model
    source = os.linesep.join(["time.sleep(14)", "pmx.model.parameters.add('long_a', 1.0)", "print('long_a done')"])
    started = time.time()
    answer = client.result("exec", {"source": source})
    waited = time.time() - started
    call_id = check_running_answer(answer, "exec", "a call past the request timeout must be answered as running")
    check(REQUEST_TIMEOUT - 1 <= waited < REQUEST_TIMEOUT + 4,
          "the caller must be answered after the request timeout, not after %.1f s" % waited)
    print("long call: answered after %.1f s that call %d is still running" % (waited, call_id))
    # It is reported while it runs, without asking a window it may be keeping busy
    status = client.result("status")
    check(not status["isIdle"], "a running call must make the session busy: " + str(status))
    check(("Automation call " + str(call_id)) in status["busyReason"],
          "the busy reason must name the call: " + status["busyReason"])
    pending = [entry for entry in status["calls"] if entry["callId"] == call_id]
    check(len(pending) == 1 and not pending[0]["isFinished"], "status must list the running call: " + str(status["calls"]))
    client.result("ping")
    # A change waits for its turn as long as the request timeout, and is then refused with the call named
    started = time.time()
    error = client.error("eval", {"expression": "pmx.model.parameters.add('long_refused', 1.0)"})
    check(error["code"] == BUSY, "a call behind a running one must be refused as busy: " + str(error))
    check(("Automation call " + str(call_id)) in error["message"] and "get_call" in error["message"],
          "the refusal must name the running call and how to collect it: " + error["message"])
    print("long call: a change behind it was refused after %.1f s - %s" % (time.time() - started, error["message"]))
    # Asking without waiting answers that it is still running
    answer = client.result("wait", {"call_id": call_id, "timeout": 0})
    check(check_running_answer(answer, "exec", "the call must still be running") == call_id,
          "wait must answer for the call it was asked about: " + str(answer))
    # Waiting long enough hands over exactly what exec would have answered
    answer = client.result("wait", {"call_id": call_id, "timeout": 30})
    check("running" not in answer, "the call must have finished: " + str(answer))
    check(not answer["failed"], "the long call must not fail: " + answer["error"])
    check(answer["elapsed"] >= 13.5, "the answer must be the call's own: " + str(answer["elapsed"]))
    check("long_a done" in answer["output"], "the output of the call must be handed over: " + answer["output"])
    check([entry["type"] for entry in answer["recordedCommands"]] == ["CAddParameter"] and
          "long_a" in answer["recordedCommands"][0]["description"],
          "the call must have recorded its command: " + str(answer["recordedCommands"]))
    print("long call: collected after %.1f s of work, recorded %s" % (answer["elapsed"], answer["recordedCommands"]))
    # Once
    error = client.error("wait", {"call_id": call_id})
    check(error["code"] == INVALID_PARAMS and str(call_id) in error["message"],
          "a collected answer must not be handed over twice: " + str(error))
    status = client.result("status")
    check(status["isIdle"], "the session must be idle once the call is done: " + status["busyReason"])
    check(status["calls"] == [], "nothing may be pending: " + str(status["calls"]))
    check(status["model"]["commandCount"] == before + 1,
          "the long call, and only it, must have been recorded: " + str(status["model"]["commandCount"]))
    value = client.result("eval", {"expression": "pmx.model.parameters.get_names()"})
    check("long_a" in value["value"] and "long_refused" not in value["value"],
          "the long call must have changed the model and the refused one not: " + str(value["value"]))
    # A timeout of the call's own, shorter than the request timeout
    started = time.time()
    answer = client.result("exec", {"source": "time.sleep(3)" + os.linesep + "pmx.model.parameters.add('long_b', 2.0)",
                                    "timeout": 1})
    call_id = check_running_answer(answer, "exec", "a call past its own timeout must be answered as running")
    check(time.time() - started < 2.5, "the caller must be answered after its own timeout")
    answer = client.result("wait", {"call_id": call_id, "timeout": 30})
    check(not answer["failed"] and len(answer["recordedCommands"]) == 1, "the call must be recorded: " + str(answer))
    # And one longer than the request timeout, which is waited out
    answer = client.result("exec", {"source": "time.sleep(7)", "timeout": 20})
    check("running" not in answer and not answer["failed"], "a call within its own timeout must be answered: " + str(answer))
    # An expression is answered the same way
    answer = client.result("eval", {"expression": "time.sleep(3) or 7", "timeout": 1})
    call_id = check_running_answer(answer, "eval", "a long expression must be answered as running")
    answer = client.result("wait", {"call_id": call_id, "timeout": 30})
    check(answer["value"] == 7, "the value of the expression must be handed over: " + str(answer))
    print("long call: its own timeout shortens and lengthens the wait, for exec and for eval")
    # An answer nobody collected yet does not hold the turn
    answer = client.result("exec", {"source": "time.sleep(2)" + os.linesep + "pmx.model.parameters.add('long_c', 3.0)",
                                    "timeout": 0})
    call_id = check_running_answer(answer, "exec", "a timeout of 0 must answer at once")
    deadline = time.time() + 30
    while True:
        pending = [entry for entry in client.result("status")["calls"] if entry["callId"] == call_id]
        check(len(pending) == 1, "an uncollected call must stay listed: " + str(pending))
        if pending[0]["isFinished"]:
            break
        check(time.time() < deadline, "the call never finished")
        time.sleep(0.2)
    started = time.time()
    result = client.result("exec", {"source": "pmx.model.parameters.add('long_d', 4.0)"})
    check(not result["failed"] and time.time() - started < 2.0, "a finished call must have given its turn back: " + str(result))
    answer = client.result("wait", {"call_id": call_id})
    check("long_c" in answer["recordedCommands"][0]["description"], "the kept answer must be the call's: " + str(answer))
    # A timeout out of range is refused before anything runs
    count = client.result("status")["model"]["commandCount"]
    for method, parameters in (("exec", {"source": "pmx.model.parameters.add('never', 1.0)", "timeout": 601}),
                               ("exec", {"source": "pmx.model.parameters.add('never', 1.0)", "timeout": -1}),
                               ("wait", {"call_id": call_id + 1000})):
        error = client.error(method, parameters)
        check(error["code"] == INVALID_PARAMS, method + " must refuse " + str(parameters) + ": " + str(error))
    check(client.result("status")["model"]["commandCount"] == count, "a refused call must record nothing")
    print("long call: an uncollected answer kept without holding the turn, a bad timeout refused")


def main():
    session = ChildSession("On", arguments=["--rpcTimeout", str(REQUEST_TIMEOUT)])
    try:
        check_long_calls(session.connect())
    finally:
        session.close()
    #
    session = ChildSession("On")
    try:
        client = session.connect()
        check_recording(client)
        check_history(client)
        check_journal_of_replaces(client)
        check_journal_round_trip(client)
        check_exit_guards(client)
        read_only_model = os.path.join(session.directory, "read_only.pmx")
        save_model_with_analysis(client, read_only_model)
    finally:
        code = session.close()
    check(code == 0, "the child must exit cleanly, but it returned " + str(code))
    print("exit: the child closed itself with code " + str(code))
    #
    session = ChildSession("ReadOnly", read_only_model)
    try:
        check_read_only(session.connect())
    finally:
        session.close()
    #
    print("TEST PASSED")


main()
