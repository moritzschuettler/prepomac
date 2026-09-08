#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks what the automation server promises about a session it drives: that a change an agent makes
#is recorded as the command a user would have produced by hand, and that read only mode refuses to
#change the model whatever Python is sent.
#
#Neither can be checked from inside a recorded script, because a recorded script suppresses the
#recording of the commands inside it. So this script starts sessions of its own - PrePoMax with
#-g No and the automation server, which has no window and no history of its own - and drives them
#from outside.
#
#  PrePoMax.com --script Test_Rpc_Live_Session.py -g No -x Yes -w <work directory>
#
#Expected process exit code 0.

import json
import os
import subprocess
import sys
import tempfile
import time

CHILD_TIMEOUT = 180


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

    def __init__(self, mode):
        self.directory = tempfile.mkdtemp(prefix="rpc_child_")
        self.log = open(os.path.join(self.directory, "child.log"), "w")
        self.process = subprocess.Popen([sys.executable, "--rpc", mode, "-g", "No", "--noSettingsSave",
                                         "--timeout", str(CHILD_TIMEOUT), "-w", self.directory],
                                        stdout=self.log, stderr=subprocess.STDOUT)
        self.client = None

    def connect(self):
        self.client = Client(self.process.pid)
        return self.client

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
    recorded = result["recordedCommands"][0]
    check(recorded.startswith("C"), "a recorded command must be a command class: " + recorded)
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
    check(result["recordedCommands"] == ["CRunPythonScript"],
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
    print("read only: a change refused by the history, reading unaffected")


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


def check_exit_guards(client):
    """Closing a session that has unsaved changes has to be asked for explicitly."""
    client.result("exec", {"source": "pmx.model.parameters.add('exit_guard', 1.0)"})
    error = client.error("exit")
    check("unsaved changes" in error["message"], "exit must warn about unsaved changes: " + error["message"])
    status = client.result("status")
    check(status["isRunning"], "a refused exit must leave the session alone")
    print("exit: refused while the model has unsaved changes")


def main():
    session = ChildSession("On")
    try:
        client = session.connect()
        check_recording(client)
        check_history(client)
        check_exit_guards(client)
    finally:
        code = session.close()
    check(code == 0, "the child must exit cleanly, but it returned " + str(code))
    print("exit: the child closed itself with code " + str(code))
    #
    session = ChildSession("ReadOnly")
    try:
        check_read_only(session.connect())
    finally:
        session.close()
    #
    print("TEST PASSED")


main()
