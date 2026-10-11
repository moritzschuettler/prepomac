#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks what the automation server promises about a session it drives: that a change an agent makes
#is recorded as the command a user would have produced by hand, and that read only mode refuses to
#change the model whatever Python is sent. It also replays the journal of replaces that set values back
#to their defaults, which a journal must write out because a replace keeps what it is not given, and
#round trips a varied model: the journal of the session that built it is replayed in a second, fresh
#session, and the CalculiX decks the two sessions export must be the same. A third journal carries the
#states the dialogs can store and the API once refused: step controls left at their unset defaults, an
#initial increment above the maximum one, frequency limits that are unset or have no upper limit, a
#propagation into or from steps that do not take the item, a defined temperature with an amplitude read
#from a file, complex phases, and a radiation load whose cavity radiation was turned off. A step journal
#writes only the values each step type uses - the increments of a modal dynamics step after a frequency
#step, a steady state initial increment above the hidden period, a Direct period below the hidden minimum,
#the time range only for nonharmonic loading, the damping values of the damping type - and replays to the
#same deck. A fourth carries
#successive tree layout changes - nested groups, every group removed, ungrouped items reordered - physical
#constants made undefined and a deactivated part; pmx.open and pmx.results.open each refuse the file the
#other one opens. Another carries the values only the dialogs set before the API took them: a vector
#distribution, the coordinate system of a distribution, the shifts of an amplitude, fixed degrees of
#freedom, values taken back to Default, and a material whose values are expressions. One more carries
#Element and Node surfaces, a contact adjustment size taken back to Default and the colors the
#constraint, contact pair and initial condition dialogs set. One more carries a
#recorded script that binds selection_1, which must not change the journal lines after it, and the global
#results file of a submodel, which must exist, so a cleared one is refused. A recorded script that fails part way must be
#recorded with what it changed before the error and its journal must run it to that error and go on, a
#sys.exit() included, while
#one that fails before changing anything records nothing. Two model checks of elements without a section
#must leave one Missing_section set, and so must their journal.
#
#And it checks a call that outlasts the request timeout. A session started with --rpcTimeout 5 is sent
#Python that sleeps longer than that: the caller must be answered that the call is still running, with
#an id, while the call runs to the end and records its command; wait must hand its answer over once,
#and status must name it while another change waits for its turn and is refused. A call that never ends
#must be stopped by cancel, keep what it recorded before it was stopped and leave the session free for
#the next call, and a flood of printed output must come back cut to its beginning and its end. A call
#cancelled while it still waits for the window, before its Python has started, must be told that the
#interrupt is pending, and must then end without running its Python or recording anything; one that
#reaches its Python while the cancel still waits for it must be reported as interrupted there. A value of
#the wrong type is refused on the pipe with the argument named.
#
#Read only mode is also checked against a status call that arrives while a long exec runs: status
#must not lift the block the exec holds, so the change the exec tries after it is still refused.
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
    "step = pmx.model.steps.add_static('rt_static')",
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
    check(status["isRegenerating"] is False, "the child must not be regenerating: " + str(status))
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


# Python bytecode that keeps the call busy for a few seconds and then tries a change. It loops rather
# than sleeps, so the interpreter is running the whole time the status calls arrive
READ_ONLY_LATE_CHANGE = '''
import time
end = time.time() + 4.0
while time.time() < end:
    pass
pmx.model.parameters.add('blocked_late', 1.0)
'''


def check_read_only_during_status(client):
    """A status answered while an exec runs must not lift the read only block that exec holds."""
    before = client.result("status")["model"]["commandCount"]
    answer = client.result("exec", {"source": READ_ONLY_LATE_CHANGE, "timeout": 0})
    check(answer.get("running") is True, "the exec must be answered as running at once: " + str(answer))
    call_id = answer["callId"]
    # status is not serialized, so it is answered while the exec holds the turn - several times, to
    # be sure at least one of them finishes before the exec reaches its change
    deadline = time.time() + 2.0
    count = 0
    while time.time() < deadline:
        status = client.result("status")
        check(status["isReadOnly"], "the session must stay read only")
        count += 1
        time.sleep(0.1)
    check(count >= 3, "status must answer while the exec runs, it answered " + str(count) + " times")
    result = client.result("wait", {"call_id": call_id, "timeout": 60})
    check("running" not in result, "the exec must have finished: " + str(result))
    check(result["failed"], "the change after the status calls must fail in read only mode: " + str(result))
    check("read only" in result["error"], "the refusal must say why: " + result["error"])
    check("--rpc ReadOnly" in result["error"], "the refusal must name where read only comes from: " + result["error"])
    check(result["recordedCommands"] == [], "nothing may be recorded: " + str(result["recordedCommands"]))
    value = client.result("eval", {"expression": "pmx.model.parameters.get_names()"})
    check("blocked_late" not in value["value"], "the refused parameter must not exist: " + str(value["value"]))
    check(client.result("status")["model"]["commandCount"] == before, "the history must not have grown")
    print("read only: " + str(count) + " status calls during a long exec left its change refused")


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


def check_recording_across_open(client, directory):
    """A call that replaces the history lists only its own commands, never those an opened file brings."""
    file_name = os.path.join(directory, "recording_across_open.pmx")
    result = client.result("exec", {"source": "pmx.save(r'" + file_name + "')"})
    check(not result["failed"], "the model must be saved: " + result["error"])
    check("historyReplaced" not in result, "a save does not replace the history: " + str(result))
    result = client.result("exec", {"source": "pmx.open(r'" + file_name + "')" + os.linesep +
                                              "pmx.model.parameters.add('after_open', 1.0)"})
    check(not result["failed"], "the model must be opened: " + result["error"])
    check(result.get("historyReplaced") is True and len(result.get("historyMessage", "")) > 0,
          "an open must be reported as replacing the history: " + str(result))
    descriptions = [entry["description"] for entry in result["recordedCommands"]]
    check(not any("live_a" in description for description in descriptions),
          "the history of the opened file must not be listed as recorded: " + str(descriptions))
    check(result["recordedCommands"][-1]["type"] == "CAddParameter" and "after_open" in descriptions[-1],
          "the command the call made after the open must be listed: " + str(descriptions))
    value = client.result("eval", {"expression": "pmx.new_model('ThreeD', 'MM_TON_S_C')"})
    check(value.get("historyReplaced") is True and "warning" in value,
          "an expression that starts a new model must be warned about: " + str(value))
    print("recording: an open lists " + str(len(descriptions)) + " own command(s), not the file's history")


def read_replaced_values(client):
    step = "pmx.model.steps['journal_static']"
    expressions = [step + ".bcs['journal_dr'].get_value('" + dof + "')" for dof in ("u1", "u2", "u3")]
    expressions += [step + ".bcs['journal_sm'].get_value('" + dof + "')" for dof in ("u1", "u2")]
    expressions += ["pmx.model.materials['journal_steel'].description",
                    step + ".loads['journal_pt'].get_value('auto_direction')",
                    "pmx.model.contact_pairs['journal_cp'].method",
                    "pmx.model.contact_pairs['journal_cp'].small_sliding"]
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
        "pmx.model.steps.add_static('journal_static')",
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
        "loads.replace_pre_tension('journal_pt', auto_direction=True)",
        "pmx.model.surface_interactions.add('journal_si', [pmx.create.surface_behavior('Hard'), "
        "pmx.create.friction(0.1)])",
        "pmx.model.contact_pairs.add('journal_cp', 'journal_si', pmx.create.selection_by_ids('femodel', "
        "'surfaces', 'surfaces', {1: [1]}), pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', "
        "{1: [2]}), method='NodeToSurface', small_sliding=True)",
        "pmx.model.contact_pairs.replace('journal_cp', method='SurfaceToSurface', small_sliding=False)"])
    result = client.result("exec", {"source": source})
    check(not result["failed"], "the replaces must not fail: " + result["error"])
    recorded = read_replaced_values(client)
    check(recorded[2] is None, "u3 was not freed: " + str(recorded))
    check(recorded[3] is True and recorded[4] is False, "the submodel u2 flag was not turned off: " + str(recorded))
    check(not recorded[5], "the material description was not cleared: " + str(recorded))
    check(recorded[6] is True, "the pre-tension did not return to the surface normal: " + str(recorded))
    check(recorded[7] == "SurfaceToSurface" and recorded[8] is False,
          "the contact pair did not become surface to surface without small sliding: " + str(recorded))
    # The journal writes the defaults out
    result = client.result("history", {"include_journal": True})
    journal = result["journal"]
    check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
    check(journal["isInline"], "a short journal must come back inline")
    journal_source = journal["source"]
    # A replace to SurfaceToSurface over a kept small sliding is refused unless small_sliding is given
    start = journal_source.find("contact_pairs.replace(")
    check(start >= 0, "the contact pair replace is missing from the journal:" + os.linesep + journal_source)
    lines = journal_source[start:].splitlines()
    statement = lines[0]
    for line in lines[1:]:
        if not line[:1].isspace():
            break
        statement += line
    check("small_sliding=False" in statement,
          "the contact pair replace must write small_sliding=False: " + statement)
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


def read_job_values(client):
    expressions = []
    for name in ("journal_job_b", "journal_job_c"):
        job = "pmx.jobs['" + name + "']"
        expressions += [job + ".fem_solver", job + ".argument", job + ".compatibility_mode",
                        job + ".number_of_cpus", "len(" + job + ".environment_variables)"]
    value = client.result("eval", {"expression": "[" + ", ".join(expressions) + "]"})
    check(not value["failed"], "the analyses must be readable: " + value["error"])
    return value["value"]


def check_journal_of_jobs(client):
    """The journal of an analysis carries the solver, an edited argument and every override a replace takes back."""
    model = "pmx.new_model('ThreeD', 'MM_TON_S_C')"
    result = client.result("exec", {"source": model + os.linesep +
                                              "pmx.import_file(r'" + os.path.join(MODELS, "cube.inp") + "')"})
    check(not result["failed"], "the new model must be built: " + result["error"])
    source = os.linesep.join([
        "pmx.jobs.add('journal_job_a', fem_solver='Abaqus', compatibility_mode=True, number_of_cpus=3, "
        "environment_variables=[pmx.create.environment_variable('PMX_JOURNAL', 'one')])",
        "pmx.jobs.replace('journal_job_a', 'journal_job_b', compatibility_mode=False, environment_variables=[], "
        "inherit_number_of_cpus=True)",
        "pmx.jobs.add('journal_job_c', argument='-i custom')",
        "pmx.jobs.replace('journal_job_c', fem_solver='FEMaster')"])
    result = client.result("exec", {"source": source})
    check(not result["failed"], "the analyses must be built: " + result["error"])
    recorded = read_job_values(client)
    # A renamed analysis whose argument was the solver default follows the new name, an edited one is kept
    check(recorded[0] == "Abaqus" and recorded[1] == "job=journal_job_b",
          "the renamed Abaqus analysis must run job=journal_job_b: " + str(recorded))
    check(recorded[2] is False and recorded[4] == 0,
          "the replace must switch compatibility mode off and empty the environment: " + str(recorded))
    check(recorded[5] == "FEMaster" and recorded[6] == "-i custom",
          "the edited argument must survive the new solver: " + str(recorded))
    result = client.result("history", {"include_journal": True})
    journal = result["journal"]
    check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
    journal_source = journal["source"]
    for expected in ["fem_solver='Abaqus'", "compatibility_mode=False", "environment_variables=[]",
                     "inherit_number_of_cpus=True", "argument='-i custom'"]:
        check(expected in journal_source, expected + " is missing from the journal:" + os.linesep + journal_source)
    result = client.result("exec", {"source": model})
    check(not result["failed"], "the model must be cleared for the replay: " + result["error"])
    result = client.result("exec", {"source": journal_source})
    check(not result["failed"], "the journal must replay: " + result["error"])
    replayed = read_job_values(client)
    check(replayed == recorded, "the replayed analyses differ: " + str(replayed) + " against " + str(recorded))
    print("journal: the analyses replay to " + str(replayed))


def read_model_states(client):
    steps = "pmx.model.steps"
    expressions = [steps + "['state_static'].get_value('initial_time_increment')",
                   steps + "['state_static'].get_value('max_time_increment')",
                   "list(" + steps + "['state_static'].get_control_types())",
                   steps + "['state_frequency'].get_value('lower_frequency')",
                   steps + "['state_frequency'].get_value('upper_frequency')",
                   steps + "['state_frequency_cleared'].get_value('lower_frequency')",
                   steps + "['state_frequency_cleared'].get_value('upper_frequency')",
                   steps + "['state_steady'].loads.contains('state_force')",
                   steps + "['state_frequency'].bcs.contains('state_fix')",
                   steps + "['state_static'].defined_fields['state_temperature'].item_type",
                   steps + "['state_static'].defined_fields['state_temperature'].amplitude_name",
                   steps + "['state_steady'].loads['state_phase'].get_equations()['phase_deg'].replace(' ', '')",
                   steps + "['state_steady'].bcs['state_rotation'].get_value('phase_deg')",
                   "'phase_deg' in list(" + steps + "['state_steady'].loads['state_tension'].get_value_names())",
                   steps + "['state_steady'].loads['state_tension'].get_value('magnitude')",
                   steps + "['state_heat'].loads['state_radiation'].get_value('cavity_radiation')",
                   steps + "['state_default'].incrementation_type",
                   steps + "['state_default'].get_value('time_period')",
                   steps + "['state_default'].max_increments"]
    value = client.result("eval", {"expression": "[" + ", ".join(expressions) + "]"})
    check(not value["failed"], "the model states must be readable: " + value["error"])
    return value["value"]


def check_journal_of_model_states(client):
    """States the dialogs store, built through the API, must come back from their journal."""
    model = "pmx.new_model('ThreeD', 'MM_TON_S_C')"
    result = client.result("exec", {"source": model + os.linesep +
                                              "pmx.import_file(r'" + os.path.join(MODELS, "cube.inp") + "')"})
    check(not result["failed"], "the new model must be built: " + result["error"])
    # A temperature read from a file only needs the file to exist until the analysis is written
    temperature_file = os.path.join(tempfile.mkdtemp(prefix="rpc_states_"), "temperature.frd")
    open(temperature_file, "w").close()
    source = os.linesep.join([
        "node = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1])",
        "face = pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', {1: [1]})",
        "pmx.model.amplitudes.add('state_ramp', [[0, 0], [1, 1]])",
        # An initial increment above the maximum one, and controls whose unset values the journal leaves out
        "static = pmx.model.steps.add_static('state_static', time_period=1.0, initial_time_increment=0.5, "
        "max_time_increment=0.1, incrementation_type='Automatic')",
        # A step set back to the Default incrementation keeps its time values hidden, as the dialog does, and
        # the journal of the replace must not write them, or the replay is refused
        "pmx.model.steps.add_static('state_default', time_period=2.0, initial_time_increment=0.25, "
        "max_increments=40, incrementation_type='Automatic')",
        "pmx.model.steps.replace_static('state_default', incrementation_type='Default')",
        "static.set_controls([pmx.create.time_incrementation_control(i0=8), pmx.create.field_control(rna=0.01), "
        "pmx.create.contact_control(delcon=0.002)])",
        "static.loads.add_concentrated_force('state_force', node, f1=1)",
        "static.bcs.add_fixed('state_fix', node)",
        # The conversion to a file keeps nothing the file form does not use
        "static.defined_fields.add_temperature('state_temperature', node, 20, amplitude_name='state_ramp')",
        "static.defined_fields.replace_temperature_from_file('state_temperature', file_name=r'" +
        temperature_file + "')",
        # A lower limit with no upper limit, and limits cleared again
        "pmx.model.steps.add_frequency('state_frequency', lower_frequency=5.0, upper_frequency=0)",
        "pmx.model.steps.add_frequency('state_frequency_cleared', lower_frequency=5.0, upper_frequency=50.0)",
        "pmx.model.steps.replace_frequency('state_frequency_cleared', lower_frequency=float('nan'))",
        "steady = pmx.model.steps.add_steady_state_dynamics('state_steady')",
        "steady.loads.add_concentrated_force('state_phase', node, f1=1, phase_deg='=15 * 2')",
        "steady.bcs.add_displacement_rotation('state_rotation', node, u1=0.1, phase_deg=45)",
        "steady.loads.add_pre_tension('state_tension', face, 500)",
        "steady.loads.replace_pre_tension('state_tension', magnitude=600)",
        "heat = pmx.model.steps.add_heat_transfer('state_heat')",
        "heat.loads.add_radiation_heat_transfer('state_radiation', face, 20, 0.5, cavity_radiation=True, "
        "cavity_name='C1')",
        "heat.loads.replace_radiation_heat_transfer('state_radiation', cavity_radiation=False)",
        # The frequency steps take no load and no defined temperature, the heat transfer step neither, and from
        # the last step nothing follows: the dialog propagates anyway and skips what does not take the item
        "static.loads.propagate('state_force')",
        "static.bcs.propagate('state_fix')",
        "static.defined_fields.propagate('state_temperature')",
        "heat.loads.propagate('state_radiation')",
        "heat.field_outputs.propagate(list(heat.field_outputs.get_names())[0])"])
    try:
        result = client.result("exec", {"source": source})
        check(not result["failed"], "the model states must be accepted: " + result["error"])
        recorded = read_model_states(client)
        check(recorded[0] == 0.5 and recorded[1] == 0.1, "the increments were not kept: " + str(recorded))
        check(recorded[3] == 5.0 and recorded[4] == 0, "the frequency limits were not kept: " + str(recorded))
        check(recorded[5] is None and recorded[6] == 50.0, "the lower limit was not cleared: " + str(recorded))
        check(recorded[7] is True and recorded[8] is True, "the propagation did not reach the steps that take " +
              "the items: " + str(recorded))
        check(recorded[9] == "temperature_from_file" and recorded[10] == "Default",
              "the defined temperature was not read from a file without an amplitude: " + str(recorded))
        check(recorded[11] == "=15*2" and recorded[12] == 45, "the phases were not kept: " + str(recorded))
        check(recorded[13] is False and recorded[14] == 600, "the pre-tension took a phase: " + str(recorded))
        check(recorded[15] is False, "the cavity radiation was not turned off: " + str(recorded))
        check(recorded[16] == "Default" and recorded[17] is None and recorded[18] is None,
              "a Default static step must read its hidden time values as None: " + str(recorded))
        result = client.result("history", {"include_journal": True})
        journal = result["journal"]
        check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
        check(journal["isInline"], "a short journal must come back inline")
        journal_source = journal["source"]
        # The unset control values and the unset limit are written so that the API takes them back
        for unexpected in ["is_value", "ij", "it", "ds", "dh", "wg", "q0a", "qua"]:
            check(not re.search(r"[(\s]" + unexpected + "=", journal_source),
                  unexpected + " must be left out of the journal:" + os.linesep + journal_source)
        for expected in ["lower_frequency=float('nan')", "upper_frequency=0.0", "cavity_radiation=False",
                         "replace_temperature_from_file", "propagate("]:
            check(expected in journal_source, expected + " is missing from the journal:" + os.linesep +
                  journal_source)
        # The journal replayed on a new model rebuilds the same states
        result = client.result("exec", {"source": model})
        check(not result["failed"], "the model must be cleared for the replay: " + result["error"])
        result = client.result("exec", {"source": journal_source})
        check(not result["failed"], "the journal of the model states must replay: " + result["error"] +
              os.linesep + journal_source)
        replayed = read_model_states(client)
        check(replayed == recorded, "the replayed states differ: " + str(replayed) + " against " + str(recorded))
    finally:
        os.remove(temperature_file)
        os.rmdir(os.path.dirname(temperature_file))
    print("journal: the states the dialogs store replay to " + str(replayed))


def read_step_values(client):
    steps = "pmx.model.steps"
    expressions = [steps + "['step_md_after'].max_increments",
                   steps + "['step_md_after'].incrementation_type",
                   "repr(" + steps + "['step_md_after'].get_value('modal_damping'))",
                   steps + "['step_md_steady'].get_value('initial_time_increment')",
                   steps + "['step_md_steady'].get_value('time_period')",
                   "repr(" + steps + "['step_md_steady'].get_value('modal_damping'))",
                   steps + "['step_direct'].get_value('time_period')",
                   steps + "['step_harmonic'].get_value('time_lower')",
                   "repr(" + steps + "['step_harmonic'].get_value('modal_damping'))",
                   steps + "['step_nonharmonic'].get_value('time_upper')",
                   steps + "['step_nonharmonic'].get_value('number_of_data_points')",
                   "repr(" + steps + "['step_dynamic'].get_value('damping'))",
                   "repr(" + steps + "['step_dynamic_off'].get_value('damping'))",
                   steps + "['step_frequency'].max_increments",
                   steps + "['step_heat'].nlgeom"]
    value = client.result("eval", {"expression": "[" + ", ".join(expressions) + "]"})
    check(not value["failed"], "the step values must be readable: " + value["error"])
    return value["value"]


def journal_call(journal_source, marker):
    """The text of the journal call that starts with the marker, up to the next step call, without the white space
    the journal wraps a long call with."""
    compact = re.sub(r"\s+", "", journal_source)
    start = compact.find(marker)
    check(start >= 0, marker + " is missing from the journal:" + os.linesep + journal_source)
    end = compact.find("pmx.model.steps.", start + len(marker))
    return compact[start:] if end < 0 else compact[start:end]


def check_journal_of_step_values(client):
    """A step journal writes the values each step type uses and no other, and replays to the same deck."""
    model = "pmx.new_model('ThreeD', 'MM_TON_S_C')"
    result = client.result("exec", {"source": model + os.linesep +
                                              "pmx.import_file(r'" + os.path.join(MODELS, "cube.inp") + "')"})
    check(not result["failed"], "the new model must be built: " + result["error"])
    source = os.linesep.join([
        # A modal dynamics step after a frequency step keeps the Automatic type, so its increments are written
        "pmx.model.steps.add_frequency('step_frequency')",
        "pmx.model.steps.add_modal_dynamics('step_md_after', max_increments=500, time_period=2.0, "
        "initial_time_increment=0.1, modal_damping=pmx.create.modal_damping('Rayleigh', 0.1, 0.001))",
        # A steady state step keeps an initial increment above the time period it does not use
        "pmx.model.steps.add_modal_dynamics('step_md_steady', steady_state=True, initial_time_increment=5.0, "
        "modal_damping=pmx.create.modal_damping('Direct', damping_ratios=[pmx.create.damping_ratio(0.02, 1, 5)]))",
        # A Direct step takes a period below the minimum increment it does not use
        "pmx.model.steps.add_static('step_direct', time_period=1.0, initial_time_increment=1e-7, "
        "incrementation_type='Direct')",
        "pmx.model.steps.replace_static('step_direct', time_period=1e-6)",
        "pmx.model.steps.add_steady_state_dynamics('step_harmonic', "
        "modal_damping=pmx.create.modal_damping('Constant', viscous_damping_ratio=0.02))",
        "pmx.model.steps.add_steady_state_dynamics('step_nonharmonic', harmonic=False, number_of_data_points=12, "
        "number_of_fourier_terms=8, time_lower=0.5, time_upper=2.0)",
        "pmx.model.steps.add_dynamic('step_dynamic', damping=pmx.create.damping('Rayleigh', 0.1, 0.001))",
        "pmx.model.steps.add_dynamic('step_dynamic_off', damping=pmx.create.damping('Off'))",
        "pmx.model.steps.add_heat_transfer('step_heat')"])
    directory = tempfile.mkdtemp(prefix="rpc_step_values_")
    result = client.result("exec", {"source": source})
    check(not result["failed"], "the step values must be accepted: " + result["error"])
    recorded = read_step_values(client)
    check(recorded[0] == 500 and recorded[1] == "Automatic",
          "a modal dynamics step after a frequency step lost its increments: " + str(recorded))
    check(recorded[3] == 5.0 and recorded[4] is None,
          "a steady state modal dynamics step did not keep its initial increment: " + str(recorded))
    check(recorded[6] == 1e-6, "a Direct step did not take a short period: " + str(recorded))
    check(recorded[7] is None and recorded[9] == 2.0 and recorded[10] == 12,
          "the time range must read back only for nonharmonic loading: " + str(recorded))
    check(recorded[13] is None and recorded[14] is False,
          "a frequency step must read max_increments as None, a heat transfer step nlgeom as False: " +
          str(recorded))
    result = client.result("history", {"include_journal": True})
    journal = result["journal"]
    check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
    check(journal["isInline"], "a short journal must come back inline")
    journal_source = journal["source"]
    # Each step writes only the values its type uses, which are the values its dialog shows
    expectations = [("add_frequency('step_frequency'", [], ["max_increments=", "incrementation_type="]),
                    ("add_modal_dynamics('step_md_after'", ["max_increments=500", "'Rayleigh',0.1,0.001"],
                     ["incrementation_type=", "viscous_damping_ratio=", "min_time_increment="]),
                    ("add_modal_dynamics('step_md_steady'", ["initial_time_increment=5", "damping_ratios="],
                     ["time_period=", "viscous_damping_ratio="]),
                    ("add_steady_state_dynamics('step_harmonic'", ["viscous_damping_ratio="],
                     ["time_lower=", "time_upper=", "number_of_fourier_terms=", "max_increments=",
                      "incrementation_type=", "damping_ratios="]),
                    ("add_steady_state_dynamics('step_nonharmonic'",
                     ["number_of_data_points=12", "number_of_fourier_terms=8", "time_upper=2"], []),
                    ("add_dynamic('step_dynamic_off'", ["damping('Off')"], []),
                    ("add_heat_transfer('step_heat'", [], ["nlgeom="])]
    for marker, expected_texts, unexpected_texts in expectations:
        call = journal_call(journal_source, marker)
        for expected in expected_texts:
            check(expected in call, expected + " is missing from the journal call:" + os.linesep + call)
        for unexpected in unexpected_texts:
            check(unexpected not in call, unexpected + " must be left out of the journal call:" + os.linesep + call)
    recorded_deck = export_deck(client, os.path.join(directory, "recorded.inp"))
    # The journal replayed on a new model rebuilds the same steps and the same deck
    result = client.result("exec", {"source": model})
    check(not result["failed"], "the model must be cleared for the replay: " + result["error"])
    result = client.result("exec", {"source": journal_source})
    check(not result["failed"], "the journal of the step values must replay: " + result["error"] + os.linesep +
          journal_source)
    replayed = read_step_values(client)
    check(replayed == recorded, "the replayed step values differ: " + str(replayed) + " against " + str(recorded))
    replayed_deck = export_deck(client, os.path.join(directory, "replayed.inp"))
    check(replayed_deck == recorded_deck, "the replayed steps write another deck: " +
          first_difference(recorded_deck, replayed_deck))
    print("journal: the step values replay to " + str(replayed))


def read_layout_states(client):
    tree = "pmx.model.tree"
    expressions = ["str(" + tree + ".get_layout('femodel', 'node_sets'))",
                   "str(" + tree + ".get_layout('femodel', 'element_sets'))",
                   "pmx.model.properties.absolute_zero",
                   "pmx.model.properties.stefan_boltzmann",
                   "pmx.model.properties.newton_gravity",
                   "[part.active for part in pmx.model.parts]"]
    value = client.result("eval", {"expression": "[" + ", ".join(expressions) + "]"})
    check(not value["failed"], "the layouts and properties must be readable: " + value["error"])
    return value["value"]


def check_journal_of_layouts(client):
    """Tree layouts, physical constants left undefined and a deactivated part must come back from their journal."""
    model = "pmx.new_model('ThreeD', 'MM_TON_S_C')"
    result = client.result("exec", {"source": model + os.linesep +
                                              "pmx.import_file(r'" + os.path.join(MODELS, "cube.inp") + "')"})
    check(not result["failed"], "the new model must be built: " + result["error"])
    source = os.linesep.join([
        "node = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1])",
        "element = pmx.create.selection_by_ids('femodel', 'elements', 'elements', [1])",
        "for name in ('tree_a', 'tree_b', 'tree_c'): pmx.model.node_sets.add(name, node)",
        "for name in ('tree_e1', 'tree_e2'): pmx.model.element_sets.add(name, element)",
        "tree = pmx.model.tree",
        # Each gesture records the whole resulting layout, and every one of them must replay after the others:
        # a nested group, an ungrouped item moved, every group removed and the ungrouped items reordered
        "g1 = tree.add_group('femodel', 'node_sets', 'tree_g1', item_names=['tree_a'])",
        "g2 = tree.add_group('femodel', 'node_sets', 'tree_g2', parent_group_id=g1, item_names=['tree_b'])",
        "tree.move_items('femodel', 'node_sets', 'tree_c', target_index=0)",
        "tree.remove_groups('femodel', 'node_sets', [g1])",
        "tree.remove_groups('femodel', 'node_sets', [g2])",
        "tree.move_items('femodel', 'node_sets', 'tree_b', target_index=0)",
        # A layout left grouped, built by two successive groups and a rename
        "e1 = tree.add_group('femodel', 'element_sets', 'tree_eg1', item_names=['tree_e1'])",
        "tree.add_group('femodel', 'element_sets', 'tree_eg2', item_names=['tree_e2'])",
        "tree.rename_group('femodel', 'element_sets', e1, 'tree_eg1_renamed')",
        # A constant set and then made undefined again, which has no Python value of its own
        "pmx.model.properties.replace(stefan_boltzmann=5.67e-11)",
        "pmx.model.properties.replace(absolute_zero=-273.15, clear_undefined_constants=True)",
        "pmx.model.parts.deactivate(list(pmx.model.parts.get_names())[0])"])
    result = client.result("exec", {"source": source})
    check(not result["failed"], "the layouts must be built: " + result["error"])
    recorded = read_layout_states(client)
    check("tree_g" not in recorded[0], "every node set group must be removed: " + recorded[0])
    check(recorded[0].startswith("['tree_b'"), "the ungrouped node sets were not reordered: " + recorded[0])
    check("tree_eg1_renamed" in recorded[1] and "tree_eg2" in recorded[1],
          "the element set groups are missing: " + recorded[1])
    check(recorded[2] == -273.15 and recorded[3] is None and recorded[4] is None,
          "only the absolute zero may stay defined: " + str(recorded))
    check(False in recorded[5], "the part was not deactivated: " + str(recorded))
    # Setting the layout a collection already has records nothing
    status = client.result("status")
    before = status["model"]["commandCount"]
    result = client.result("exec", {"source": "pmx.model.tree.set_layout('femodel', 'element_sets', "
                                              "pmx.model.tree.get_layout('femodel', 'element_sets'))"})
    check(not result["failed"], "setting the same layout must not fail: " + result["error"])
    check(client.result("status")["model"]["commandCount"] == before, "setting the same layout must record nothing")
    result = client.result("history", {"include_journal": True})
    journal = result["journal"]
    check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
    check(journal["emittedCommandCount"] == result["commandCount"], "every command must be emitted: " + str(journal))
    journal_source = journal["source"]
    check("add_group(" not in journal_source, "a layout must be journalled as set_layout:" + os.linesep +
          journal_source)
    for expected in ["set_layout(", "clear_undefined_constants=True", "pmx.model.parts.deactivate("]:
        check(expected in journal_source, expected + " is missing from the journal:" + os.linesep + journal_source)
    check("stefan_boltzmann=inf" not in journal_source, "an undefined constant has no Python value")
    # The journal replayed on a new model rebuilds the same layouts
    result = client.result("exec", {"source": model})
    check(not result["failed"], "the model must be cleared for the replay: " + result["error"])
    result = client.result("exec", {"source": journal_source})
    check(not result["failed"], "the journal of the layouts must replay: " + result["error"] + os.linesep +
          journal_source)
    replayed = read_layout_states(client)
    check(replayed == recorded, "the replayed layouts differ: " + str(replayed) + " against " + str(recorded))
    print("journal: the tree layouts, constants and part activity replay to " + str(replayed))


def check_result_file_opening(client):
    """A result file opens beside the model and a model file replaces it, so each goes through its own member."""
    directory = tempfile.mkdtemp(prefix="rpc_open_")
    model_file = os.path.join(directory, "model.pmx")
    result_file = os.path.join(directory, "result.frd")
    for name in (model_file, result_file):
        open(name, "w").close()
    try:
        before = client.result("status")["model"]["commandCount"]
        result = client.result("exec", {"source": "pmx.results.open(r'" + model_file + "')"})
        check(result["failed"] and "pmx.open" in result["error"],
              "pmx.results.open must refuse a model file and name pmx.open: " + str(result))
        result = client.result("exec", {"source": "pmx.open(r'" + result_file + "')"})
        check(result["failed"] and "pmx.results.open" in result["error"],
              "pmx.open must refuse a result file and name pmx.results.open: " + str(result))
        check(client.result("status")["model"]["commandCount"] == before, "a refused open must record nothing")
    finally:
        for name in (model_file, result_file):
            os.remove(name)
        os.rmdir(directory)
    print("open: a model file and a result file are each refused by the member that does not open them")


def read_dialog_values(client):
    distributions = "pmx.model.distributions"
    bc = "pmx.model.steps['dialog_static'].bcs['dialog_fixed']"
    constraints = "pmx.model.constraints"
    steel = "list(pmx.model.materials['dialog_steel'].get_properties())"
    expressions = ["list(" + distributions + "['dialog_vector'].equation)",
                   distributions + "['dialog_local'].coordinate_system_name",
                   distributions + "['dialog_global'].coordinate_system_name",
                   "pmx.model.amplitudes['dialog_shift'].shift_time",
                   "pmx.model.amplitudes['dialog_shift'].shift_amplitude",
                   "pmx.model.amplitudes['dialog_shift_back'].shift_time",
                   "list(" + bc + ".get_value('fixed_dofs'))",
                   bc + ".get_value('u1')",
                   constraints + "['dialog_tie'].get_value('position_tolerance')",
                   constraints + "['dialog_compression'].get_value('spring_stiffness')",
                   constraints + "['dialog_compression'].get_value('tensile_force_at_negative_infinity')",
                   steel + "[0].get_value('youngs_modulus')",
                   steel + "[1].get_value('zero_temperature')",
                   "list(pmx.model.steps['dialog_static'].bcs['dialog_valued'].get_value('fixed_dofs'))"]
    value = client.result("eval", {"expression": "[" + ", ".join(expressions) + "]"})
    check(not value["failed"], "the dialog values must be readable: " + value["error"])
    return value["value"]


def check_journal_of_dialog_values(client):
    """Values only the dialogs set until the API took them must come back from their journal."""
    model = "pmx.new_model('ThreeD', 'MM_TON_S_C')"
    result = client.result("exec", {"source": model + os.linesep +
                                              "pmx.import_file(r'" + os.path.join(MODELS, "cube.inp") + "')"})
    check(not result["failed"], "the new model must be built: " + result["error"])
    source = os.linesep.join([
        "node = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1])",
        "def face(face_id):",
        "    return pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', {1: [face_id]})",
        # A vector distribution keeps an unused magnitude equation, and a distribution its coordinate system
        "pmx.model.distributions.add_from_equation('dialog_vector', ['x', 'y', '2 * z'])",
        "pmx.model.coordinate_systems.add('dialog_cs', [0, 0, 0], [0, 1, 0], [-1, 0, 0])",
        "pmx.model.distributions.add_from_equation('dialog_local', 'x', coordinate_system_name='DIALOG_CS')",
        "pmx.model.distributions.add_from_equation('dialog_global', 'y', coordinate_system_name='dialog_cs')",
        "pmx.model.distributions.replace_from_equation('dialog_global', coordinate_system_name='Global')",
        # The shifts of an amplitude, one taken back to zero
        "pmx.model.amplitudes.add('dialog_shift', [[0, 0], [1, 1]], shift_time=0.5, shift_amplitude=0.25)",
        "pmx.model.amplitudes.add('dialog_shift_back', [[0, 0], [1, 1]], shift_time=0.5)",
        "pmx.model.amplitudes.replace('dialog_shift_back', shift_time=0)",
        # Degrees of freedom the dialog shows as Fixed, and fixed ones taken back to values
        "static = pmx.model.steps.add_static('dialog_static')",
        "static.bcs.add_displacement_rotation('dialog_fixed', node, fixed_dofs=['U1', 'U3'])",
        "static.bcs.replace_displacement_rotation('dialog_fixed', free_dofs=['U1', 'U3'], fixed_dofs=['U2'])",
        "static.bcs.add_displacement_rotation('dialog_valued', node, fixed_dofs=['U1'])",
        "static.bcs.replace_displacement_rotation('dialog_valued', u1=0, u2=0.5)",
        # Values the dialogs offer as Default, set and taken back to Default
        "pmx.model.constraints.add_tie('dialog_tie', face(1), face(2), position_tolerance=0.01)",
        "pmx.model.constraints.replace_tie('dialog_tie', position_tolerance=float('nan'))",
        "pmx.model.constraints.add_compression_only('dialog_compression', face(3), spring_stiffness=1000, "
        "tensile_force_at_negative_infinity=5)",
        "pmx.model.constraints.replace_compression_only('dialog_compression', spring_stiffness=float('nan'), "
        "tensile_force_at_negative_infinity=float('nan'))",
        # A parametric material, with a zero temperature expression that evaluates to zero
        "pmx.model.parameters.add('dialog_e', 210000)",
        "pmx.model.parameters.add('dialog_zero', 0)",
        "pmx.model.materials.add('dialog_steel', [pmx.create.elastic('dialog_e', 0.3), "
        "pmx.create.thermal_expansion(1.2e-5, zero_temperature='dialog_zero')])"])
    result = client.result("exec", {"source": source})
    check(not result["failed"], "the dialog values must be accepted: " + result["error"])
    recorded = read_dialog_values(client)
    check(recorded[0] == ["=x", "=y", "=2 * z"], "the vector equations were not kept: " + str(recorded))
    check(recorded[1] == "dialog_cs" and recorded[2] == "Global",
          "the coordinate systems were not kept: " + str(recorded))
    check(recorded[3] == 0.5 and recorded[4] == 0.25 and recorded[5] == 0,
          "the amplitude shifts were not kept: " + str(recorded))
    check(recorded[6] == ["U2"] and recorded[7] is None, "the fixed degrees of freedom were not kept: " + str(recorded))
    check(recorded[8] is None and recorded[9] is None and recorded[10] is None,
          "the values did not go back to Default: " + str(recorded))
    check(recorded[11] == "=dialog_e" and recorded[12] == "=dialog_zero",
          "the material expressions were not kept: " + str(recorded))
    check(recorded[13] == [], "the values did not replace the fixed degree of freedom: " + str(recorded))
    result = client.result("history", {"include_journal": True})
    journal = result["journal"]
    check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
    check(journal["isInline"], "a short journal must come back inline")
    journal_source = journal["source"]
    for expected in ["['=x', '=y', '=2 * z']", "coordinate_system_name='dialog_cs'", "coordinate_system_name='Global'",
                     "shift_time=0.0", "shift_amplitude=0.25", "fixed_dofs=['U1', 'U3']", "fixed_dofs=['U2']",
                     "position_tolerance=float('nan')", "spring_stiffness=float('nan')",
                     "tensile_force_at_negative_infinity=float('nan')", "pmx.create.elastic('dialog_e'",
                     "zero_temperature='dialog_zero'"]:
        check(expected in journal_source, expected + " is missing from the journal:" + os.linesep + journal_source)
    result = client.result("exec", {"source": model})
    check(not result["failed"], "the model must be cleared for the replay: " + result["error"])
    result = client.result("exec", {"source": journal_source})
    check(not result["failed"], "the journal of the dialog values must replay: " + result["error"] + os.linesep +
          journal_source)
    replayed = read_dialog_values(client)
    check(replayed == recorded, "the replayed values differ: " + str(replayed) + " against " + str(recorded))
    print("journal: the values only the dialogs set replay to " + str(replayed))


def read_dialog_appearance(client):
    surfaces = "pmx.model.surfaces"
    constraints = "pmx.model.constraints"
    pair = "pmx.model.contact_pairs['look_pair']"
    expressions = [surfaces + "['look_node'].surface_type",
                   surfaces + "['look_from_set'].surface_type",
                   surfaces + "['look_back'].surface_type",
                   pair + ".adjust",
                   pair + ".adjustment_size",
                   "list(" + pair + ".master_color)",
                   "list(" + pair + ".slave_color)",
                   "list(" + constraints + "['look_tie'].master_color)",
                   "list(" + constraints + "['look_tie'].slave_color)",
                   "list(" + constraints + "['look_rigid'].color)",
                   "list(" + constraints + "['look_spring'].color)",
                   "list(pmx.model.initial_conditions['look_temperature'].color)"]
    value = client.result("eval", {"expression": "[" + ", ".join(expressions) + "]"})
    check(not value["failed"], "the surface types and colors must be readable: " + value["error"])
    return value["value"]


def check_journal_of_dialog_appearance(client):
    """Surface types, a contact adjustment back to Default and the dialog colors must come back from their journal."""
    model = "pmx.new_model('ThreeD', 'MM_TON_S_C')"
    result = client.result("exec", {"source": model + os.linesep +
                                              "pmx.import_file(r'" + os.path.join(MODELS, "cube.inp") + "')"})
    check(not result["failed"], "the new model must be built: " + result["error"])
    source = os.linesep.join([
        "nodes = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4])",
        "def face(face_id):",
        "    return pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', {1: [face_id]})",
        "pmx.model.node_sets.add('look_set', nodes)",
        "pmx.model.reference_points.add('look_rp', [0, 0, 2])",
        # A Node surface from a selection and from a node set, and one taken back to Element by a replace
        "pmx.model.surfaces.add('look_node', face(1), surface_type='Node')",
        "pmx.model.surfaces.add_from_node_set('look_from_set', 'look_set')",
        "pmx.model.surfaces.replace_from_node_set('look_from_set', surface_type='Node')",
        "pmx.model.surfaces.add('look_back', face(2), surface_type='Node')",
        "pmx.model.surfaces.replace('look_back', surface_type='Element')",
        # An adjustment size taken back to the Default the dialog offers, and the colors of the two surfaces
        "pmx.model.surface_interactions.add('look_si', [pmx.create.surface_behavior('Hard'), "
        "pmx.create.friction(0.1)])",
        "pmx.model.contact_pairs.add('look_pair', 'look_si', face(3), face(4), adjustment_size=0.01, "
        "master_color=[10, 20, 30], slave_color='Red')",
        "pmx.model.contact_pairs.replace('look_pair', adjustment_size=float('nan'), master_color='Yellow')",
        # Two surface colors on a tie, taken back to the default, and one color on the other constraints
        "pmx.model.constraints.add_tie('look_tie', face(5), face(6), master_color=[1, 2, 3], slave_color=[4, 5, 6, 7])",
        "pmx.model.constraints.replace_tie('look_tie', slave_color='Yellow')",
        "pmx.model.constraints.add_rigid_body('look_rigid', 'look_rp', nodes, color='Green')",
        "pmx.model.constraints.add_point_spring('look_spring', nodes, k1=1, color=[9, 8, 7])",
        "pmx.model.constraints.replace_point_spring('look_spring', color='Yellow')",
        "pmx.model.initial_conditions.add_temperature('look_temperature', nodes, 20, color=[50, 60, 70])"])
    result = client.result("exec", {"source": source})
    check(not result["failed"], "the surface types and colors must be accepted: " + result["error"])
    recorded = read_dialog_appearance(client)
    check(recorded[0:3] == ["Node", "Node", "Element"], "the surface types were not kept: " + str(recorded))
    check(recorded[3] is True and recorded[4] is None, "the adjustment size did not go back to Default: " +
          str(recorded))
    check(recorded[5] == [255, 255, 0, 255] and recorded[6] == [255, 0, 0, 255],
          "the contact pair colors were not kept: " + str(recorded))
    check(recorded[7] == [1, 2, 3, 255] and recorded[8] == [255, 255, 0, 255],
          "the tie colors were not kept: " + str(recorded))
    check(recorded[9] == [0, 128, 0, 255] and recorded[10] == [255, 255, 0, 255],
          "the constraint colors were not kept: " + str(recorded))
    check(recorded[11] == [50, 60, 70, 255], "the initial condition color was not kept: " + str(recorded))
    result = client.result("history", {"include_journal": True})
    journal = result["journal"]
    check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
    check(journal["isInline"], "a short journal must come back inline")
    journal_source = journal["source"]
    # An add leaves the defaults out, a replace writes them, because a replace keeps what it is not given
    for expected in ["surface_type='Node'", "surface_type='Element'", "adjustment_size=float('nan')",
                     "master_color=[10, 20, 30]", "slave_color=[255, 0, 0]", "master_color=[255, 255, 0]",
                     "slave_color=[4, 5, 6, 7]", "slave_color=[255, 255, 0]", "color=[0, 128, 0]",
                     "color=[9, 8, 7]", "color=[255, 255, 0]", "color=[50, 60, 70]"]:
        check(expected in journal_source, expected + " is missing from the journal:" + os.linesep + journal_source)
    check("adjustment_size=None" not in journal_source, "a Default adjustment size has no None form:" + os.linesep +
          journal_source)
    result = client.result("exec", {"source": model})
    check(not result["failed"], "the model must be cleared for the replay: " + result["error"])
    result = client.result("exec", {"source": journal_source})
    check(not result["failed"], "the journal of the surface types and colors must replay: " + result["error"] +
          os.linesep + journal_source)
    replayed = read_dialog_appearance(client)
    check(replayed == recorded, "the replayed values differ: " + str(replayed) + " against " + str(recorded))
    print("journal: the surface types, the Default adjustment and the dialog colors replay to " + str(replayed))


def read_script_states(client):
    sets = "pmx.model.node_sets"
    expressions = ["sorted(int(i) for i in " + sets + "['script_" + name + "'].get_node_ids())"
                   for name in ("before", "inside", "after")]
    expressions += ["pmx.model.properties.global_results_file_name"]
    value = client.result("eval", {"expression": "[" + ", ".join(expressions) + "]"})
    check(not value["failed"], "the node sets and properties must be readable: " + value["error"])
    return value["value"]


def read_work_directory(client):
    """The work directory of the session's settings, which a new analysis inherits."""
    name = "rpc_work_directory"
    result = client.result("exec", {"source": "pmx.jobs.add('" + name + "')"})
    check(not result["failed"], "the probe analysis must be added: " + result["error"])
    value = client.result("eval", {"expression": "pmx.jobs['" + name + "'].work_directory"})
    check(not value["failed"], "the work directory must be readable: " + value["error"])
    result = client.result("exec", {"source": "pmx.jobs.remove('" + name + "')"})
    check(not result["failed"], "the probe analysis must be removed: " + result["error"])
    return value["value"]


def check_journal_of_script_and_properties(client):
    """A recorded script that binds a name the journal uses must not change the lines after it, and the global
    results file of a submodel must come back from the journal. The dialog takes a submodel only with a file it
    finds in the work directory, named without its path, so a cleared file name and a path are refused rather than
    recorded."""
    work_directory = read_work_directory(client)
    global_file = "rpc_global_results.frd"
    full_global_file = os.path.join(work_directory, global_file)
    open(full_global_file, "w").close()
    try:
        model = "pmx.new_model('ThreeD', 'MM_TON_S_C')"
        result = client.result("exec", {"source": model + os.linesep +
                                                  "pmx.import_file(r'" + os.path.join(MODELS, "cube.inp") + "')"})
        check(not result["failed"], "the new model must be built: " + result["error"])
        first_node = "pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1])"
        result = client.result("exec", {"source": "pmx.model.node_sets.add('script_before', " + first_node + ")"})
        check(not result["failed"], "the first node set must be added: " + result["error"])
        # The script binds selection_1, the name the journal gives the selection of the first node set
        script = os.linesep.join(["selection_1 = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [2, 3])",
                                  "pmx.model.node_sets.add('script_inside', selection_1)"])
        result = client.result("exec", {"source": script, "record": "script"})
        check(not result["failed"], "the recorded script must not fail: " + result["error"])
        source = os.linesep.join(["pmx.model.node_sets.add('script_after', " + first_node + ")",
                                  "pmx.model.properties.replace(model_type='Submodel', "
                                  "global_results_file_name='" + global_file + "')"])
        result = client.result("exec", {"source": source})
        check(not result["failed"], "the node set and the submodel file must be accepted: " + result["error"])
        for value in ["''", "r'" + full_global_file + "'"]:
            result = client.result("exec", {"source": "pmx.model.properties.replace(global_results_file_name=" +
                                                      value + ")"})
            check(result["failed"] and "'global_results_file_name'" in result["error"],
                  "the global results file " + value + " of a submodel must be refused: " + str(result["error"]))
        recorded = read_script_states(client)
        check(recorded == [[1], [2, 3], [1], global_file], "the recorded states are not the ones built: " +
              str(recorded))
        result = client.result("history", {"include_journal": True})
        journal = result["journal"]
        check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
        journal_source = journal["source"]
        for expected in ["selection_1 = pmx.create.selection_by_ids", "compile(_recorded_script_source",
                         "model_type='Submodel'"]:
            check(expected in journal_source,
                  expected + " is missing from the journal:" + os.linesep + journal_source)
        result = client.result("exec", {"source": model})
        check(not result["failed"], "the model must be cleared for the replay: " + result["error"])
        result = client.result("exec", {"source": journal_source})
        check(not result["failed"], "the journal of the script must replay: " + result["error"] + os.linesep +
              journal_source)
        replayed = read_script_states(client)
        check(replayed == recorded, "the replayed states differ: " + str(replayed) + " against " + str(recorded))
        print("journal: a recorded script and a submodel's global results file replay to " + str(replayed))
    finally:
        os.remove(full_global_file)


def read_failed_script_states(client):
    names = ("failed_before", "failed_after", "failed_exit", "failed_later")
    value = client.result("eval", {"expression": "[pmx.model.node_sets.contains(name) for name in " +
                                                 str(list(names)) + "]"})
    check(not value["failed"], "the node sets must be readable: " + value["error"])
    return value["value"]


def check_journal_of_failed_script(client):
    """A recorded script that fails part way keeps what it changed before the error, so the history records it and
    its journal runs it to the same error and goes on; one that changed nothing records nothing."""
    model = "pmx.new_model('ThreeD', 'MM_TON_S_C')"
    result = client.result("exec", {"source": model + os.linesep +
                                              "pmx.import_file(r'" + os.path.join(MODELS, "cube.inp") + "')"})
    check(not result["failed"], "the new model must be built: " + result["error"])
    first_node = "pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1])"
    script = os.linesep.join(["pmx.model.node_sets.add('failed_before', " + first_node + ")",
                              "raise ValueError('planned failure')",
                              "pmx.model.node_sets.add('failed_after', " + first_node + ")"])
    result = client.result("exec", {"source": script, "record": "script"})
    check(result["failed"], "the script must fail: " + str(result))
    check("planned failure" in result["error"], "the error of the script must be reported: " + result["error"])
    check("recorded in the command history" in result["error"],
          "the user must be told the failed script was recorded: " + result["error"])
    check([entry["type"] for entry in result["recordedCommands"]] == ["CRunPythonScript"],
          "the failed script must be recorded as one command: " + str(result["recordedCommands"]))
    result = client.result("exec", {"source": "raise ValueError('nothing changed')", "record": "script"})
    check(result["failed"], "the second script must fail: " + str(result))
    check(result["recordedCommands"] == [],
          "a script that failed before changing anything must not be recorded: " + str(result["recordedCommands"]))
    check("recorded in the command history" not in result["error"],
          "a script that was not recorded must not be said to be: " + result["error"])
    # A script that ends in sys.exit() after a change is recorded the same way, and its journal must not stop there
    script = os.linesep.join(["pmx.model.node_sets.add('failed_exit', " + first_node + ")",
                              "import sys",
                              "sys.exit(3)"])
    result = client.result("exec", {"source": script, "record": "script"})
    check(result["failed"], "the script ending in sys.exit() must fail: " + str(result))
    check([entry["type"] for entry in result["recordedCommands"]] == ["CRunPythonScript"],
          "the script ending in sys.exit() must be recorded as one command: " + str(result["recordedCommands"]))
    result = client.result("exec", {"source": "pmx.model.node_sets.add('failed_later', " + first_node + ")"})
    check(not result["failed"], "the node set after the script must be added: " + result["error"])
    recorded = read_failed_script_states(client)
    check(recorded == [True, False, True, True], "the states are not the ones built: " + str(recorded))
    result = client.result("history", {"include_journal": True})
    journal = result["journal"]
    check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
    journal_source = journal["source"]
    check("except BaseException as _recorded_script_error" in journal_source,
          "the journal must let the failed script fail and go on:" + os.linesep + journal_source)
    check("except KeyboardInterrupt:" in journal_source,
          "the journal must still stop when it is interrupted:" + os.linesep + journal_source)
    result = client.result("exec", {"source": model})
    check(not result["failed"], "the model must be cleared for the replay: " + result["error"])
    result = client.result("exec", {"source": journal_source})
    check(not result["failed"], "the journal of the failed script must replay: " + result["error"] + os.linesep +
          journal_source)
    check("planned failure" in result["output"], "the replay must report the failure: " + result["output"])
    check("SystemExit" in result["output"], "the replay must report the sys.exit() and go on: " + result["output"])
    replayed = read_failed_script_states(client)
    check(replayed == recorded, "the replayed states differ: " + str(replayed) + " against " + str(recorded))
    print("journal: a script that failed part way replays to " + str(replayed))


def read_missing_section_sets(client):
    value = client.result("eval", {"expression": "sorted(name for name in pmx.model.element_sets.get_names() " +
                                                 "if name.startswith('Missing_section'))"})
    check(not value["failed"], "the element sets must be readable: " + value["error"])
    return value["value"]


def check_journal_of_missing_section(client):
    """A run that finds elements without a section records a set of them before the run is recorded. The run
    repeated, and the journal replayed, must keep that one set rather than make another."""
    model = "pmx.new_model('ThreeD', 'MM_TON_S_C')"
    result = client.result("exec", {"source": model + os.linesep +
                                              "pmx.import_file(r'" + os.path.join(MODELS, "cube.inp") + "')"})
    check(not result["failed"], "the new model must be built: " + result["error"])
    source = os.linesep.join([
        "step = pmx.model.steps.add_static('missing_static')",
        "step.bcs.add_fixed('missing_fix', pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4]))",
        "job = pmx.jobs.add('missing_job')",
        "job.check_model(timeout=120)",
        "job.check_model(timeout=120)"])
    result = client.result("exec", {"source": source})
    check(not result["failed"], "the model without a section must be checked: " + result["error"])
    recorded = read_missing_section_sets(client)
    check(recorded == ["Missing_section-1"], "the two runs must leave one set: " + str(recorded))
    result = client.result("history", {"include_journal": True})
    journal = result["journal"]
    check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
    journal_source = journal["source"]
    result = client.result("exec", {"source": model})
    check(not result["failed"], "the model must be cleared for the replay: " + result["error"])
    result = client.result("exec", {"source": journal_source})
    check(not result["failed"], "the journal of the runs must replay: " + result["error"] + os.linesep +
          journal_source)
    replayed = read_missing_section_sets(client)
    check(replayed == recorded, "the replayed sets differ: " + str(replayed) + " against " + str(recorded))
    print("journal: two runs without a section replay to " + str(replayed))


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
    # The wait for the turn counts against the timeout of the call, so a shorter one is refused sooner and
    # a timeout of 0 at once, and neither runs
    for timeout, bound in ((1, 2.5), (0, 1.5)):
        started = time.time()
        error = client.error("exec", {"source": "pmx.model.parameters.add('long_short_refused', 1.0)",
                                      "timeout": timeout})
        waited = time.time() - started
        check(error["code"] == BUSY and ("Automation call " + str(call_id)) in error["message"],
              "a call behind a running one must be refused as busy within its timeout: " + str(error))
        check(waited < bound, "a timeout of %d must bound the wait for the turn, not %.1f s" % (timeout, waited))
    print("long call: a timeout of 1 and of 0 bounded the wait for the turn, and the calls were refused")
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
    check("long_a" in value["value"] and "long_refused" not in value["value"] and
          "long_short_refused" not in value["value"],
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


def check_cancel(client):
    """A call that never ends is interrupted by cancel, keeps what it recorded, and frees the session."""
    status = client.result("status")
    check("cancel" in status["methods"], "status must name cancel: " + str(status["methods"]))
    before = status["model"]["commandCount"]
    source = os.linesep.join(["pmx.model.parameters.add('cancel_a', 1.0)", "while True:", "    pass"])
    answer = client.result("exec", {"source": source, "timeout": 1})
    call_id = check_running_answer(answer, "exec", "a loop that never ends must be answered as running")
    check("cancel_call" in answer["message"], "the running answer must say how to stop it: " + answer["message"])
    # Answered while the loop holds the turn
    started = time.time()
    result = client.result("cancel", {"call_id": call_id})
    check(time.time() - started < 5, "cancel must not wait for the call it stops")
    check(result["callId"] == call_id and result["cancelRequested"] and result["isInterrupted"],
          "the loop must be interrupted: " + str(result))
    check(result["isFinished"], "a loop of pure Python must stop at once: " + str(result))
    answer = client.result("wait", {"call_id": call_id, "timeout": 30})
    check("running" not in answer and answer["failed"], "the cancelled call must have failed: " + str(answer))
    check("KeyboardInterrupt" in answer["error"], "the error must be the interrupt: " + answer["error"])
    check(answer.get("cancelled") is True, "the answer must say it was cancelled: " + str(answer))
    check([entry["type"] for entry in answer["recordedCommands"]] == ["CAddParameter"],
          "what was recorded before the interrupt must be kept and listed: " + str(answer["recordedCommands"]))
    status = client.result("status")
    check(status["isIdle"], "the session must be idle once the call stopped: " + status["busyReason"])
    check(status["model"]["commandCount"] == before + 1, "only the command before the loop may be recorded")
    # The next call runs, and the interrupt did not linger into it
    result = client.result("exec", {"source": "cancel_probe = sum(range(10))" + os.linesep + "print(cancel_probe)"})
    check(not result["failed"] and "45" in result["output"], "the next exec must work: " + str(result))
    check("cancelled" not in result, "a call that was not cancelled must not say so: " + str(result))
    # An expression is cancelled the same way
    answer = client.result("eval", {"expression": "sum(1 for _ in iter(int, 1))", "timeout": 1})
    call_id = check_running_answer(answer, "eval", "an endless expression must be answered as running")
    result = client.result("cancel", {"call_id": call_id})
    check(result["isInterrupted"] and result["isFinished"], "the expression must be interrupted: " + str(result))
    answer = client.result("wait", {"call_id": call_id, "timeout": 30})
    check(answer["failed"] and "KeyboardInterrupt" in answer["error"], "the expression must have failed: " + str(answer))
    value = client.result("eval", {"expression": "6 * 7"})
    check(value["value"] == 42, "the next eval must work: " + str(value))
    # Code that swallows the first interrupt is stopped by the second cancel, which delivers one of its own
    source = os.linesep.join(["try:", "    while True:", "        pass", "except:", "    pass",
                              "cancel_swallowed = True", "while True:", "    pass"])
    answer = client.result("exec", {"source": source, "timeout": 1})
    call_id = check_running_answer(answer, "exec", "a loop that swallows an interrupt must be answered as running")
    result = client.result("cancel", {"call_id": call_id})
    check(result["isInterrupted"] and not result["isFinished"],
          "the first interrupt must be delivered and swallowed: " + str(result))
    check("cancel it again" in result["message"], "the answer must say to cancel again: " + result["message"])
    started = time.time()
    result = client.result("cancel", {"call_id": call_id})
    check(time.time() - started < 5, "a second cancel must not wait for the call it stops")
    check(result["isInterrupted"] and result["isFinished"], "the second cancel must stop the loop: " + str(result))
    answer = client.result("wait", {"call_id": call_id, "timeout": 30})
    check(answer["failed"] and "KeyboardInterrupt" in answer["error"] and answer.get("cancelled") is True,
          "the twice cancelled call must have failed with the interrupt: " + str(answer))
    value = client.result("eval", {"expression": "cancel_swallowed"})
    check(value["value"] is True, "the first interrupt must have been swallowed by the script: " + str(value))
    # A call that is not there is refused; a finished one has nothing to cancel
    error = client.error("cancel", {"call_id": call_id + 1000})
    check(error["code"] == INVALID_PARAMS, "cancel of an unknown call must be refused: " + str(error))
    answer = client.result("exec", {"source": "pmx.model.parameters.add('cancel_b', 2.0)", "timeout": 0})
    call_id = check_running_answer(answer, "exec", "a timeout of 0 must answer at once")
    deadline = time.time() + 30
    while not [entry for entry in client.result("status")["calls"]
               if entry["callId"] == call_id and entry["isFinished"]]:
        check(time.time() < deadline, "the call never finished")
        time.sleep(0.2)
    result = client.result("cancel", {"call_id": call_id})
    check(not result["cancelRequested"] and result["isFinished"], "a finished call cannot be cancelled: " + str(result))
    check(not result["isInterrupted"] and not result["isInterruptPending"] and "interrupted" not in result["message"],
          "the answer for a finished call must not claim an interrupt: " + str(result))
    answer = client.result("wait", {"call_id": call_id})
    check(not answer["failed"] and "cancelled" not in answer, "the finished call must keep its answer: " + str(answer))
    print("cancel: a loop, an endless expression and a loop that swallowed an interrupt stopped, the command "
          "before the loop kept, a finished call not reported as interrupted")


def check_cancel_before_python(client):
    """A call cancelled before its Python starts is told the interrupt is pending, and never runs it."""
    status = client.result("status")
    check(status["requestTimeout"] >= 20, "the session needs a request timeout the hold fits in: " +
          str(status["requestTimeout"]))
    before = status["model"]["commandCount"]
    # The window is held from a thread of the session's own once the call that starts it has finished, so
    # the next call gets the turn but waits for the window before it reaches its Python
    hold = os.linesep.join(["import threading, time",
                            "from System import Action",
                            "from System.Windows.Forms import Application",
                            "hold_form = [form for form in Application.OpenForms if form.GetType().Name == 'FrmMain'][0]",
                            "def hold_window():",
                            "    time.sleep(2)",
                            "    hold_form.BeginInvoke(Action(lambda: time.sleep(10)))",
                            "threading.Thread(target=hold_window).start()"])
    result = client.result("exec", {"source": hold})
    check(not result["failed"], "the window hold must be set up: " + result["error"])
    time.sleep(3.5)
    answer = client.result("exec", {"source": "pmx.model.parameters.add('cancel_early', 1.0)", "timeout": 0})
    call_id = check_running_answer(answer, "exec", "a call waiting for the window must be answered as running")
    result = client.result("cancel", {"call_id": call_id})
    check(result["cancelRequested"] and not result["isInterrupted"] and result["isInterruptPending"],
          "a call that has not reached its Python must report the interrupt as pending: " + str(result))
    check(not result["isFinished"], "the call must still be waiting for the window: " + str(result))
    check("pending" in result["message"] and "does not run" in result["message"],
          "the message must say the interrupt waits for its Python: " + result["message"])
    answer = client.result("wait", {"call_id": call_id, "timeout": 60})
    check("running" not in answer and answer["failed"] and "KeyboardInterrupt" in answer["error"] and
          answer.get("cancelled") is True, "the call must stop before its Python runs: " + str(answer))
    check(answer["recordedCommands"] == [], "nothing may be recorded: " + str(answer["recordedCommands"]))
    status = client.result("status")
    check(status["model"]["commandCount"] == before, "the cancelled call must record nothing")
    print("cancel: a call still waiting for the window reported the interrupt pending and never ran its Python")


def check_cancel_reaching_start(client):
    """A call the cancel lets on to its start does not run its Python, and is told so once it has finished.

    The window is held from a thread of the session's own until the server has taken the cancel, so the call
    waits for the window when the cancel arrives and reaches its start while the cancel still waits for it -
    without a race against a clock. The other start, a call already past that check and waiting for the GIL,
    cannot be produced from outside: the call needs the GIL before the check as well, so a thread holding it
    stops the call before the check, not after it.
    """
    before = client.result("status")["model"]["commandCount"]
    hold = os.linesep.join(["import threading, time",
                            "from System import Action",
                            "from System.Reflection import BindingFlags",
                            "from System.Windows.Forms import Application",
                            "hold_form = [form for form in Application.OpenForms if form.GetType().Name == 'FrmMain'][0]",
                            "hold_server = hold_form.GetType().GetField('_automationServer', BindingFlags.NonPublic | "
                            "BindingFlags.Instance).GetValue(hold_form)",
                            "def hold_until_cancelled():",
                            "    deadline = time.time() + 60",
                            "    while time.time() < deadline:",
                            "        call = hold_server.GetDispatcher().GetRunningCall()",
                            "        if call is not None and call.IsCancelRequested: return",
                            "        time.sleep(0.01)",
                            "def hold_window():",
                            "    time.sleep(2)",
                            "    hold_form.BeginInvoke(Action(hold_until_cancelled))",
                            "threading.Thread(target=hold_window).start()"])
    result = client.result("exec", {"source": hold})
    check(not result["failed"], "the window hold must be set up: " + result["error"])
    time.sleep(3.5)
    answer = client.result("exec", {"source": "pmx.model.parameters.add('cancel_late', 1.0)", "timeout": 0})
    call_id = check_running_answer(answer, "exec", "a call waiting for the window must be answered as running")
    result = client.result("cancel", {"call_id": call_id})
    check(result["cancelRequested"] and not result["isInterrupted"] and not result["isInterruptPending"] and
          result["isFinished"], "a call that met the cancel at its start must be reported as finished, with no " +
          "interrupt pending: " + str(result))
    check("cancelled before its Python started" in result["message"],
          "the message must say its Python never started: " + result["message"])
    answer = client.result("wait", {"call_id": call_id, "timeout": 60})
    check(answer["failed"] and "KeyboardInterrupt" in answer["error"] and answer.get("cancelled") is True,
          "the call must stop before its Python starts: " + str(answer))
    check(answer["recordedCommands"] == [], "nothing may be recorded: " + str(answer["recordedCommands"]))
    check(client.result("status")["model"]["commandCount"] == before, "the cancelled call must record nothing")
    print("cancel: a call let on to its start by the cancel never ran its Python and was reported finished")


def check_argument_types(client):
    """A value of the wrong type is refused on the pipe with the argument named, before anything runs."""
    count = client.result("status")["model"]["commandCount"]
    for method, parameters, argument in (("exec", {"source": "pmx.model.parameters.add('typed', 1.0)",
                                                   "timeout": "10"}, "timeout"),
                                         ("exec", {"source": "pmx.model.parameters.add('typed', 1.0)",
                                                   "record": True}, "record"),
                                         ("history", {"limit": "10"}, "limit"),
                                         ("history", {"include_journal": "true"}, "include_journal"),
                                         ("wait", {"call_id": 1.5}, "call_id")):
        error = client.error(method, parameters)
        check(error["code"] == INVALID_PARAMS and ("argument " + argument + " must be") in error["message"],
              method + " must refuse " + str(parameters) + " naming " + argument + ": " + str(error))
    check(client.result("status")["model"]["commandCount"] == count, "a refused call must record nothing")
    print("argument types: values of the wrong type refused by name, nothing recorded")


def check_output_cap(client):
    """A flood of printed output is cut to its beginning and its end, with the cut said in between."""
    source = os.linesep.join(["for i in range(300000):", "    print('flood line', i)"])
    result = client.result("exec", {"source": source, "timeout": 120})
    check(not result["failed"], "the flood must run: " + result["error"])
    output = result["output"]
    check(len(output) < 1200000, "the output must be capped, it has %d characters" % len(output))
    check("[output truncated: " in output and " characters omitted]" in output,
          "the cut must be marked: " + output[999900:1000200])
    check(output.startswith("flood line 0\n"), "the beginning must be kept: " + output[:40])
    check(output.rstrip().endswith("flood line 299999"), "the end must be kept: " + output[-60:])
    # A short output is not touched
    result = client.result("exec", {"source": "print('short')"})
    check(result["output"].strip() == "short", "a short output must be kept whole: " + result["output"])
    print("output: %d characters kept of a flood of over 5 million, the cut marked" % len(output))


def main():
    session = ChildSession("On", arguments=["--rpcTimeout", str(REQUEST_TIMEOUT)])
    try:
        client = session.connect()
        check_long_calls(client)
        check_cancel(client)
        check_output_cap(client)
    finally:
        session.close()
    #
    session = ChildSession("On", arguments=["--rpcTimeout", "30"])
    try:
        client = session.connect()
        check_cancel_before_python(client)
        check_cancel_reaching_start(client)
        check_argument_types(client)
        check_recording(client)
        check_history(client)
        check_recording_across_open(client, session.directory)
        check_journal_of_replaces(client)
        check_journal_of_jobs(client)
        check_journal_of_model_states(client)
        check_journal_of_step_values(client)
        check_journal_of_dialog_values(client)
        check_journal_of_dialog_appearance(client)
        check_journal_of_script_and_properties(client)
        check_journal_of_failed_script(client)
        check_journal_of_missing_section(client)
        check_journal_of_layouts(client)
        check_result_file_opening(client)
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
        client = session.connect()
        check_read_only(client)
        check_read_only_during_status(client)
    finally:
        session.close()
    #
    print("TEST PASSED")


main()
