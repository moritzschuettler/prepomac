#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the automation server. The script is its own client: it runs inside the application and
#connects to the named pipe of its own process, so the whole contract is checked unattended.
#
#  PrePoMax.com --script Test_Rpc_Server.py --rpc On -g No -x Yes -w <work directory>
#
#Expected process exit code 0.

import json
import os
import time

PIPE_NAME = r"\\.\pipe\PrePoMax." + str(os.getpid())
PARSE_ERROR = -32700
INVALID_REQUEST = -32600
METHOD_NOT_FOUND = -32601
INVALID_PARAMS = -32602


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


class Client(object):
    """One connection to the automation server of this process."""

    def __init__(self, timeout=10.0):
        self._id = 0
        self._pipe = None
        deadline = time.time() + timeout
        while True:
            try:
                self._pipe = open(PIPE_NAME, "r+b", buffering=0)
                return
            except (IOError, OSError):
                # The listener creates its first pipe instance on a thread of its own
                if time.time() > deadline:
                    raise
                time.sleep(0.05)

    def close(self):
        if self._pipe is not None:
            self._pipe.close()
            self._pipe = None

    def send_line(self, line):
        self._pipe.write((line + "\n").encode("utf-8"))

    def read_raw_line(self):
        data = b""
        while not data.endswith(b"\n"):
            piece = self._pipe.read(1)
            if not piece:
                return None
            data += piece
        return data.decode("utf-8")

    def read_line(self):
        line = self.read_raw_line()
        return None if line is None else json.loads(line)

    def strict_result(self, method, parameters):
        """Calls a method and parses its answer as strict JSON, which has no NaN or Infinity token."""
        self._id += 1
        self.send_line(json.dumps({"jsonrpc": "2.0", "id": self._id, "method": method, "params": parameters}))
        line = self.read_raw_line()
        check(line is not None, "the server closed the connection during " + method)

        def refuse_constant(token):
            raise Exception("TEST FAILED: " + method + " answered with the non JSON token " + token + ": " + line)

        answer = json.loads(line, parse_constant=refuse_constant)
        check(answer.get("id") == self._id, method + " answered the wrong id: " + str(answer))
        check("error" not in answer, method + " failed: " + str(answer.get("error")))
        return answer["result"]

    def call(self, method, parameters=None):
        self._id += 1
        request = {"jsonrpc": "2.0", "id": self._id, "method": method}
        if parameters is not None:
            request["params"] = parameters
        self.send_line(json.dumps(request))
        answer = self.read_line()
        check(answer is not None, "the server closed the connection during " + method)
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

    def refusal(self, line):
        """Sends a raw line that is not a valid request and returns the error it is answered with."""
        self.send_line(line)
        answer = self.read_line()
        check(answer is not None, "the server closed the connection on <" + line + ">")
        check("error" in answer, "<" + line + "> was accepted")
        return answer["error"]


def check_ping(client):
    result = client.result("ping")
    check(result["processId"] == os.getpid(), "ping must report the process it runs in")
    check(result["protocolVersion"] == "1.0", "unexpected protocol version: " + result["protocolVersion"])
    check(result["pipeName"] == "PrePoMax." + str(os.getpid()), "unexpected pipe name: " + result["pipeName"])
    check(len(result["applicationVersion"]) > 0, "ping must report the application version")
    print("ping: " + result["applicationName"] + ", protocol " + result["protocolVersion"])


def check_status(client):
    result = client.result("status")
    check(result["isRunning"], "the server must report itself as running")
    check(result["connectionCount"] >= 1, "status must count this connection")
    check("ping" in result["methods"], "status must name the methods it serves")
    check("status" in result["methods"], "status must name itself")
    check("shutdown" in result["methods"], "status must name shutdown")
    check(result["requestTimeout"] >= 5, "the request timeout must be a usable value")
    # status runs on the user interface thread, so it also answers the idle question
    check("isIdle" in result, "status must report whether the application is idle")
    check(result["isIdle"], "the application must be idle during this test: " + result["busyReason"])
    check(result["busyReason"] == "", "an idle application must give no reason: " + result["busyReason"])
    model = result["model"]
    check(model["isOpen"], "a model must be open even when no file was given")
    check(model["currentView"] in ("Geometry", "Model", "Results"), "unexpected view: " + model["currentView"])
    check(model["commandCount"] >= 0, "the command count must be reported")
    check(len(model["unitSystem"]) > 0, "the unit system must be reported")
    print("status: " + str(result["connectionCount"]) + " connection(s), read only " + str(result["isReadOnly"]))
    print("status: idle, view " + model["currentView"] + ", " + str(model["commandCount"]) + " commands, " +
          model["unitSystem"])


def check_errors(client):
    error = client.error("no_such_method")
    check(error["code"] == METHOD_NOT_FOUND, "an unknown method must be MethodNotFound: " + str(error))
    check(error["name"] == "MethodNotFound", "the error must carry its name: " + str(error))
    check(len(error["message"]) > 0, "an error must carry a message")
    #
    refusals = [("{oops", PARSE_ERROR),
                ("not json at all", PARSE_ERROR),
                ('[{"jsonrpc":"2.0","id":1,"method":"ping"}]', INVALID_REQUEST),
                ('"ping"', INVALID_REQUEST),
                ('{"id":1,"method":"ping"}', INVALID_REQUEST),
                ('{"jsonrpc":"1.0","id":1,"method":"ping"}', INVALID_REQUEST),
                ('{"jsonrpc":"2.0","id":1}', INVALID_REQUEST),
                ('{"jsonrpc":"2.0","id":true,"method":"ping"}', INVALID_REQUEST),
                ('{"jsonrpc":"2.0","id":1,"method":"ping","params":[1]}', INVALID_PARAMS)]
    for line, code in refusals:
        error = client.refusal(line)
        check(error["code"] == code, "<" + line + "> must be " + str(code) + " but was " + str(error))
    # A blank line is skipped rather than answered, so the next answer belongs to the call after it
    client.send_line("   ")
    check_ping(client)
    # The connection survives every one of them
    check_ping(client)
    print("errors: " + str(len(refusals) + 1) + " refusals, the connection stayed open")


def check_eval(client):
    # A plain expression, with its machine readable value
    result = client.result("eval", {"expression": "1 + 1"})
    check(result["value"] == 2, "1 + 1 must evaluate to 2: " + str(result))
    check(result["type"] == "int", "the type must be reported: " + result["type"])
    check(result["repr"] == "2", "the repr must be reported: " + result["repr"])
    check(not result["failed"], "a valid expression must not fail")
    check(result["recordedCommands"] == [], "an expression must record nothing: " + str(result["recordedCommands"]))
    # The model is reachable, and a .NET collection still comes back as JSON
    result = client.result("eval", {"expression": "pmx.model.parameters.get_names()"})
    check(result["hasValue"], "a name list must have a JSON form: " + str(result))
    check(isinstance(result["value"], list), "the names must be a list: " + str(result["value"]))
    # An expression that prints is not lost
    result = client.result("eval", {"expression": "[print('from eval'), 7][1]"})
    check(result["value"] == 7, "the value must survive the print: " + str(result))
    check("from eval" in result["output"], "what an expression prints must be returned: " + result["output"])
    # A failing expression is a result, not a protocol error: the agent has to read the traceback
    result = client.result("eval", {"expression": "1 / 0"})
    check(result["failed"], "a division by zero must fail")
    check("ZeroDivisionError" in result["error"], "the traceback must name the error: " + result["error"])
    # NaN and infinity have no JSON form, so they come back as null and the answer stays valid JSON
    result = client.strict_result("eval", {"expression": "float('nan')"})
    check(result["hasValue"] and result["value"] is None, "NaN must come back as null: " + str(result))
    check(result["repr"] == "nan", "the repr must still show NaN: " + result["repr"])
    result = client.strict_result("eval", {"expression": "[1, float('inf'), {'low': float('-inf')}]"})
    check(result["value"] == [1, None, {"low": None}], "infinity must come back as null: " + str(result))
    # A missing argument is refused before anything runs
    error = client.error("eval", {})
    check(error["code"] == INVALID_PARAMS, "a missing expression must be InvalidParams: " + str(error))
    print("eval: values, JSON, non finite numbers as null, printed output and tracebacks")


def check_exec(client):
    # The scope is the one the Python console uses, so it is the same scope across calls
    result = client.result("exec", {"source": "automation_probe = 41 + 1"})
    check(not result["failed"], "the assignment must not fail: " + result["error"])
    check(result["record"] == "commands", "commands must be the default recording mode")
    check(result["recordedCommands"] == [], "an assignment must record nothing")
    value = client.result("eval", {"expression": "automation_probe"})
    check(value["value"] == 42, "the variable must survive the call: " + str(value))
    # What is printed comes back
    result = client.result("exec", {"source": "print('hello from exec')"})
    check("hello from exec" in result["output"], "the output must be returned: " + result["output"])
    # A change of the model goes through, and the parameter can be read back
    result = client.result("exec", {"source": "pmx.model.parameters.add('rpc_t', 2.5)"})
    check(not result["failed"], "adding a parameter must not fail: " + result["error"])
    value = client.result("eval", {"expression": "pmx.model.parameters['rpc_t'].value"})
    check(not value["failed"], "the parameter must be readable back: " + value["error"])
    # This script is itself a recorded command, and a recorded script suppresses the recording of the
    # commands inside it - including these. What the history gets is checked against a session of its
    # own, in Test_Rpc_Live_Session.py
    status = client.result("status")
    check(status["isCommandRecordingSuppressed"],
          "a recorded script must suppress recording, so this test cannot check the history")
    check(result["recordedCommands"] == [],
          "nothing can be recorded while recording is suppressed: " + str(result["recordedCommands"]))
    result = client.result("exec", {"source": "pmx.model.parameters.add('rpc_u', 3.5)", "record": "script"})
    check(not result["failed"], "the recorded script must not fail: " + result["error"])
    # A failing source is a result with a traceback, and the connection stays usable
    result = client.result("exec", {"source": "raise ValueError('deliberate')"})
    check(result["failed"], "a raising source must fail")
    check("deliberate" in result["error"], "the traceback must carry the message: " + result["error"])
    # A call the API refuses answers its message, not the .NET stack trace of the application
    for method, key in (("exec", "source"), ("eval", "expression")):
        result = client.result(method, {key: "pmx.model.materials.add('', [])"})
        check(result["failed"], "a refused " + method + " must fail")
        check("must not be empty" in result["error"], "the refusal must carry its message: " + result["error"])
        check("   at " not in result["error"], "a refusal must carry no .NET frame: " + result["error"])
        check("APIs.Python." not in result["error"], "a refusal must not name the .NET type: " + result["error"])
    result = client.result("exec", {"source": "pmx.model.materials.add('', [])"})
    check("PythonAPIException: The argument 'name' must not be empty." in result["error"],
          "the traceback must name the refusal as PythonAPIException: " + result["error"])
    error = client.error("exec", {})
    check(error["code"] == INVALID_PARAMS, "a missing source must be InvalidParams: " + str(error))
    print("exec: shared scope, individual commands, one command per script, tracebacks")


def check_api_refusal_in_script():
    # A script sees a refusal as a Python exception carrying only its message, catchable by its own name
    # and still by Exception
    try:
        pmx.model.materials.add('', [])
    except PythonAPIException as exception:
        check(str(exception) == "The argument 'name' must not be empty.",
              "a refusal must carry only its message: " + str(exception))
        check(isinstance(exception, Exception), "a refusal must be an Exception")
        check(type(exception).__name__ == "PythonAPIException", "a refusal must be a PythonAPIException")
    else:
        raise Exception("TEST FAILED: an empty material name was accepted")
    try:
        pmx.model.materials.add('', [])
    except Exception as exception:
        check("   at " not in str(exception), "a refusal must carry no .NET frame: " + str(exception))
    print("refusal: a PythonAPIException with its message only")


def check_describe(client):
    # The root, which is how an agent starts
    result = client.result("describe")
    check(result["path"] == "pmx", "the default path must be pmx: " + result["path"])
    check(result["isApiObject"], "pmx must be recognized as an API object")
    names = [member["name"] for member in result["members"]]
    for name in ("model", "results", "jobs", "create", "selection"):
        check(name in names, "pmx must offer " + name + ": " + str(names))
    print("describe: pmx offers " + str(len(names)) + " members")
    # A collection describes its verbs and what it holds right now
    result = client.result("describe", {"path": "pmx.model.parameters"})
    members = dict((member["name"], member) for member in result["members"])
    check("add" in members, "a collection must describe add: " + str(sorted(members)))
    check(members["add"]["kind"] == "method", "add must be a method")
    check(len(members["add"]["arguments"]) > 0, "add must name its arguments")
    check(len(members["add"]["description"]) > 0, "every member must carry its description")
    check("names" in result, "a collection must report the names it holds")
    print("describe: " + result["type"] + " offers " + str(len(members)) + " members")
    # A member the documentation shows must be reachable through describe
    result = client.result("describe", {"path": "pmx.model.properties"})
    members = dict((member["name"], member) for member in result["members"])
    check("unit_system" in members, "the model properties must describe unit_system")
    check(members["unit_system"]["kind"] == "property", "unit_system must be a writable property")
    # The half of the API that hangs off something the model does not have yet is described by its
    # type name, and that name may be written the way the model tree reads it: Mesh.SetupItems is
    # MeshSetupItems spelled with a separator, not a namespace nobody has
    for spelling in ("MeshSetupItems", "Model.MeshSetupItems", "Mesh.SetupItems"):
        result = client.result("describe", {"path": spelling})
        check(result["resolvedAs"] == "type", spelling + " must resolve as a type: " + str(result))
        check(result["type"] == "MeshSetupItemsAPI",
              spelling + " must resolve to the mesh setup items: " + result["type"])
    print("describe: a type name resolves however its separators are written")
    # A type name followed by a member path names the type the member returns, so the results side
    # can be read before an analysis has run
    result = client.result("describe", {"path": "Result.mesh.nodes"})
    check(result["resolvedAs"] == "type", "a member path below a type must resolve as a type: " + str(result))
    check(result["type"] == "NodesAPI", "Result.mesh.nodes must resolve to the result nodes: " + result["type"])
    result = client.result("describe", {"path": "Result.steps.last().increments"})
    check(result["type"] == "ResultIncrementsAPI",
          "a called member must be walked through its return type: " + result["type"])
    error = client.error("describe", {"path": "Result.no_such_member"})
    check(error["code"] == INVALID_PARAMS and "no member" in error["message"],
          "a missing member must be refused by name: " + str(error))
    print("describe: a member path below a type is walked through the declared member types")
    # A name that is neither a path nor a type is refused rather than left to block the connection
    error = client.error("describe", {"path": "Mesh.NoSuchThing"})
    check(error["code"] == INVALID_PARAMS, "an unknown type name must be InvalidParams: " + str(error))
    # A path that does not resolve is a refusal that says why
    error = client.error("describe", {"path": "pmx.no_such_thing"})
    check(error["code"] == INVALID_PARAMS, "an unresolvable path must be InvalidParams: " + str(error))
    check("no_such_thing" in error["message"], "the refusal must name the path: " + error["message"])
    print("describe: an unresolvable path is refused")
    # A path that ends at a method or at a plain value property describes that member, read off its owner
    result = client.result("describe", {"path": "pmx.create.selection_by_ids"})
    check(result["resolvedAs"] == "member" and result["type"] == "CreateAPI",
          "a method path must describe the method: " + str(result))
    member = result["members"][0]
    check(member["name"] == "selection_by_ids" and member["kind"] == "method" and member["parameters"],
          "the method must come with its parameters: " + str(member))
    result = client.result("describe", {"path": "pmx.model.properties.unit_system"})
    check(result["resolvedAs"] == "member" and result["members"][0]["kind"] == "property",
          "a property path must describe the property: " + str(result))
    result = client.result("describe", {"path": "Loads.add_pressure"})
    check(result["resolvedAs"] == "member" and result["members"][0]["name"] == "add_pressure",
          "a method below a type name must be described too: " + str(result))
    # A property holding an API object still describes the object's type
    result = client.result("describe", {"path": "Result.mesh.nodes"})
    check(result["resolvedAs"] == "type", "an API object property must keep resolving as a type: " + str(result))
    # A mistyped path is answered with the paths it most likely meant
    error = client.error("describe", {"path": "pmx.model.geometry_parts"})
    check("Did you mean pmx.model.geometry.parts?" in error["message"],
          "a dot written as an underscore must be suggested back: " + error["message"])
    error = client.error("describe", {"path": "pmx.model.stepz"})
    check("pmx.model.steps" in error["message"], "a near miss must be suggested: " + error["message"])
    print("describe: members are described on their own, and near misses are suggested")


def check_history(client):
    # The command that runs this script reaches the history only after it finishes, so here the
    # history is empty and only its shape can be checked. What it holds is checked against a session
    # of its own, in Test_Rpc_Live_Session.py
    result = client.result("history")
    check(result["commandCount"] == len(result["commands"]), "the count must match the list: " + str(result))
    for command in result["commands"]:
        check("index" in command and "type" in command and "name" in command,
              "a listed command must carry its index, type and name: " + str(command))
    # An empty history still produces a journal, with nothing but its header
    result = client.result("history", {"include_journal": True})
    journal = result["journal"]
    check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
    check(journal["isInline"], "a short journal must come back inline")
    check("PrePoMax" in journal["source"], "the journal must carry its header: " + journal["source"][:80])
    print("history: " + str(result["commandCount"]) + " commands, journal of " +
          str(len(journal["source"])) + " characters")


def check_screenshot(client):
    result = client.result("screenshot")
    check(result["fileName"].endswith(".png"), "a screenshot must be a png: " + result["fileName"])
    check(os.path.exists(result["fileName"]), "the file must exist: " + result["fileName"])
    print("screenshot: " + result["fileName"] + ", " + result["length"] + " bytes")
    os.remove(result["fileName"])
    # Only a png is accepted
    error = client.error("screenshot", {"file_name": "viewport.jpg"})
    check(error["code"] == INVALID_PARAMS, "another extension must be refused: " + str(error))
    # The two options of the capture dialog are served here as well
    result = client.result("screenshot", {"transparent_background": True, "magnification": 2})
    check(os.path.exists(result["fileName"]), "the transparent file must exist: " + result["fileName"])
    check(not result["isEmpty"], "the transparent file must not be empty: " + result["fileName"])
    print("screenshot: transparent and magnified, " + result["length"] + " bytes")
    os.remove(result["fileName"])
    error = client.error("screenshot", {"magnification": 9})
    check(error["code"] == INVALID_PARAMS, "a magnification of 9 must be refused: " + str(error))


def check_second_connection():
    first = Client()
    second = Client()
    try:
        check_ping(first)
        check_ping(second)
        result = second.result("status")
        check(result["connectionCount"] == 2, "two connections must be counted: " + str(result["connectionCount"]))
        print("connections: two clients served at once")
    finally:
        second.close()
        first.close()


DIALOG_PROBE = '''
import clr
clr.AddReference('CaeGlobals')
from CaeGlobals import MessageBoxes, MessageBoxModeEnum
marked = MessageBoxes.IsNonInteractiveThread
previous = MessageBoxes.Mode
MessageBoxes.Mode = MessageBoxModeEnum.Interactive
refused = False
try:
    MessageBoxes.ShowWarning('A warning raised inside an automation call.')
    MessageBoxes.ShowWarningQuestionYesNo('A decision raised inside an automation call.')
except Exception:
    refused = True
finally:
    MessageBoxes.Mode = previous
print('marked', marked, 'refused', refused)
'''


# Nothing an agent runs may open a dialog. A handler runs on its connection thread while the window
# stays the user's, so a dialog would be shown on the window and the call would wait for a human at
# the keyboard - holding the turn every later call queues behind. The mode is forced to the one a
# session with a window has, because this session has none and would answer for itself anyway.
def check_no_dialogs(client):
    result = client.result("exec", {"source": DIALOG_PROBE})
    check(not result["failed"], "the dialog probe failed: " + str(result))
    check("marked True" in result["output"],
          "a handler must run with its prompts answered rather than shown: " + result["output"])
    check("refused True" in result["output"],
          "a decision must be refused rather than waited on: " + result["output"])
    print("exec: a prompt raised inside a call is answered, never shown")


def check_shutdown():
    client = Client()
    try:
        result = client.result("shutdown")
        check(result["stopping"], "shutdown must report that it stops")
    finally:
        client.close()
    # The server closes the pipe, so a new connection is refused once the stop went through
    deadline = time.time() + 10.0
    while time.time() < deadline:
        try:
            late = Client(timeout=0.2)
            late.close()
            time.sleep(0.1)
        except (IOError, OSError):
            print("shutdown: the pipe is gone")
            return
    raise Exception("TEST FAILED: the pipe still accepts connections after shutdown")


def main():
    check_api_refusal_in_script()
    client = Client()
    try:
        check_ping(client)
        check_status(client)
        check_errors(client)
        check_describe(client)
        check_eval(client)
        check_exec(client)
        check_no_dialogs(client)
        check_history(client)
        check_screenshot(client)
    finally:
        client.close()
    #
    check_second_connection()
    check_shutdown()
    #
    print("TEST PASSED")


main()
