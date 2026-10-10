#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks that the journal of the Gmsh mesh setup items, of relative meshing parameters and of the heat transfer
#steps replays: a windowless
#session opened on the model records the items one command each over its MCP endpoint, and this session,
#which starts from an empty model as the journal does, runs the journal and must end up with the same values.
#
#Every mesh setup value is given away from its default, so that an argument the emitter writes and the API
#does not take - or writes in a form the API refuses - fails the replay rather than passing unnoticed. The
#steps cover deltmx, whose default is infinite: it must be emitted on an add, and a replace must be able to
#put a finite value back to the default.
#
#  PrePoMax.com --script Test_Journal_Mesh_Setup.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import json
import os
import shutil
import socket
import subprocess
import sys
import tempfile
import time
import urllib.request

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
MODEL = "box_and_cylinder.brep"
SESSIONS = os.path.join(os.environ["LOCALAPPDATA"], "PrePoMax", "sessions")
PROTOCOL_VERSION = "2024-11-05"
CHILD_TIMEOUT = 600
PREFIX = "PyTest_Journal_"
TETRAHEDRAL = PREFIX + "Tet"
TRANSFINITE = PREFIX + "Transfinite"
TRANSFINITE_OFF = PREFIX + "Transfinite_Off"
SWEEP = PREFIX + "Sweep"
PARAMETERS = PREFIX + "Parameters"
HEAT_TRANSFER = PREFIX + "Heat_Transfer"
UNCOUPLED = PREFIX + "Uncoupled"
COUPLED = PREFIX + "Coupled"
RECORDED = """
box = [part for part in pmx.model.geometry.parts if part.get_face_data().count == 6][0]
cylinder = [part for part in pmx.model.geometry.parts if part.get_face_data().count != 6][0]
box_selection = pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [box.id])
cylinder_selection = pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [cylinder.id])
items = pmx.model.mesh.setup_items
items.add_tetrahedral_gmsh('%(tetrahedral)s', cylinder_selection, algorithm_recombine='Blossom',
                           recombine_min_quality=0.2, transfinite_four_sided=True, transfinite_angle_deg=150.0,
                           optimize_high_order='HighOrder')
items.add_transfinite_mesh('%(transfinite)s', box_selection, recombine=True, transfinite_angle_deg=110.0)
items.add_transfinite_mesh('%(transfinite_off)s', box_selection, recombine=False)
items.add_sweep_mesh('%(sweep)s', pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces',
                                                             {box.id: [5]}),
                     transfinite_four_sided=True, transfinite_angle_deg=100.0)
items.replace_sweep_mesh('%(sweep)s', algorithm_2d='QuadWild', algorithm_recombine='Blossom',
                         recombine_min_quality=0.3, optimize_first_order_shell='Laplace2D')
items.add_meshing_parameters('%(parameters)s', cylinder_selection, relative_size=True, factor_max=0.2,
                             factor_min=0.02, max_size=7.0, min_size=0.5)
steps = pmx.model.steps
steps.add_heat_transfer('%(heat_transfer)s')
steps.add_uncoupled_temp_disp('%(uncoupled)s', deltmx=5.0)
steps.add_coupled_temp_disp('%(coupled)s', deltmx=2.5)
steps.replace_coupled_temp_disp('%(coupled)s', deltmx=float('inf'))
""" % {"tetrahedral": TETRAHEDRAL, "transfinite": TRANSFINITE, "transfinite_off": TRANSFINITE_OFF,
       "sweep": SWEEP, "parameters": PARAMETERS, "heat_transfer": HEAT_TRANSFER, "uncoupled": UNCOUPLED,
       "coupled": COUPLED}
CHECKED = [(TETRAHEDRAL, ["algorithm_recombine", "recombine_min_quality", "transfinite_four_sided",
                          "transfinite_angle_deg", "optimize_high_order"]),
           (TRANSFINITE, ["recombine", "transfinite_angle_deg"]),
           (TRANSFINITE_OFF, ["recombine"]),
           (SWEEP, ["algorithm_2d", "algorithm_recombine", "recombine_min_quality", "optimize_first_order_shell",
                    "transfinite_four_sided", "transfinite_angle_deg"]),
           (PARAMETERS, ["relative_size", "factor_max", "factor_min", "max_size", "min_size"])]
STEPS = {HEAT_TRANSFER: "inf", UNCOUPLED: "5.0", COUPLED: "inf"}


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def free_port(first=8840):
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
    """The MCP endpoint of the recording session."""

    def __init__(self, url, token):
        self.url = url
        self.token = token
        self._id = 0
        self.call("initialize", {"protocolVersion": PROTOCOL_VERSION, "capabilities": {},
                                 "clientInfo": {"name": "Test_Journal_Mesh_Setup", "version": "1.0"}})
        self.notify("notifications/initialized")

    def _post(self, message, timeout):
        request = urllib.request.Request(self.url, data=json.dumps(message).encode("utf-8"), method="POST")
        request.add_header("Content-Type", "application/json")
        request.add_header("Authorization", "Bearer " + self.token)
        with urllib.request.urlopen(request, timeout=timeout) as answer:
            return answer.status, answer.read().decode("utf-8")

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

    def notify(self, method):
        status, text = self._post({"jsonrpc": "2.0", "method": method}, 60)
        check(status == 202, method + " answered HTTP " + str(status) + ": " + text)

    def json_tool(self, name, arguments=None):
        result = self.call("tools/call", {"name": name, "arguments": arguments or {}})
        text = result["content"][0]["text"]
        check(not result.get("isError"), name + " reported an error: " + text)
        return json.loads(text)


class ChildSession(object):
    """A PrePoMax session with no window of its own, opened on the model."""

    def __init__(self):
        self.directory = tempfile.mkdtemp(prefix="journal_mesh_setup_")
        self.log = open(os.path.join(self.directory, "child.log"), "w")
        arguments = [sys.executable, "--rpc", "On", "--rpcPort", str(free_port()), "-g", "No",
                     "--noSettingsSave", "--timeout", str(CHILD_TIMEOUT), "-w", self.directory,
                     "-f", os.path.join(MODELS, MODEL), "-u", "MM_TON_S_C"]
        self.process = subprocess.Popen(arguments, stdout=self.log, stderr=subprocess.STDOUT)

    def endpoint(self, timeout=120.0):
        path = os.path.join(SESSIONS, str(self.process.pid) + ".json")
        deadline = time.time() + timeout
        while time.time() < deadline:
            check(self.process.poll() is None, "the recording session exited early")
            if os.path.exists(path):
                try:
                    info = json.load(open(path))
                    if info.get("url"):
                        return Endpoint(info["url"], info["token"])
                except (ValueError, IOError):
                    pass
            time.sleep(0.2)
        raise Exception("TEST FAILED: no session file appeared at " + path)

    def close(self):
        if self.process.poll() is None:
            self.process.kill()
            self.process.wait()
        self.log.close()
        shutil.rmtree(self.directory, ignore_errors=True)


def wait_until_idle(endpoint, timeout=120.0):
    deadline = time.time() + timeout
    while time.time() < deadline:
        if endpoint.json_tool("get_status")["isIdle"]:
            return
        time.sleep(0.2)
    raise Exception("TEST FAILED: the recording session stayed busy")


def recorded_values(endpoint):
    values = {}
    for name, keys in CHECKED:
        # The settings are a .NET dictionary, which has no JSON form, so each value is read as its text
        answer = endpoint.json_tool("eval_expression", {"expression":
                                    "[str(pmx.model.mesh.setup_items['" + name + "'].get_settings()[key]) "
                                    "for key in " + repr(keys) + "]"})
        check(not answer["failed"], "the recorded " + name + " could not be read: " + answer["error"])
        values[name] = dict(zip(keys, answer["value"]))
    for name in STEPS:
        answer = endpoint.json_tool("eval_expression", {"expression":
                                    "str(pmx.model.steps['" + name + "'].get_value('deltmx'))"})
        check(not answer["failed"], "the recorded " + name + " could not be read: " + answer["error"])
        values[name] = answer["value"]
    return values


def record():
    """The journal of the items, and their values, as the recording session holds them."""
    session = ChildSession()
    try:
        endpoint = session.endpoint()
        wait_until_idle(endpoint)
        result = endpoint.json_tool("run_python", {"source": RECORDED})
        check(not result.get("failed"), "the items could not be recorded: " + str(result))
        history = endpoint.json_tool("get_history", {"include_journal": True})
        journal = history["journal"]
        check(journal["refusedCommandCount"] == 0, "nothing may be refused: " + str(journal["refusals"]))
        return journal["source"], recorded_values(endpoint)
    finally:
        session.close()


def main():
    check(os.path.exists(os.path.join(MODELS, MODEL)), "the " + MODEL + " fixture is missing from " + MODELS)
    source, recorded = record()
    # An argument the API does not take is ignored rather than refused, so the recording is checked first
    check(recorded[TETRAHEDRAL]["transfinite_angle_deg"] == "150.0",
          "the tetrahedral item did not take its transfinite angle: " + str(recorded[TETRAHEDRAL]))
    check(recorded[TRANSFINITE_OFF]["recombine"] == "False",
          "the transfinite item did not take recombine=False: " + str(recorded[TRANSFINITE_OFF]))
    # gmsh meshes the walls of a QuadWild sweep, so its recombination and optimisation are written and replayed
    check(recorded[SWEEP]["algorithm_2d"] == "QuadWild" and recorded[SWEEP]["recombine_min_quality"] == "0.3" and
          recorded[SWEEP]["optimize_first_order_shell"] == "Laplace2D",
          "the QuadWild sweep did not take its gmsh settings: " + str(recorded[SWEEP]))
    # The sizes are used by Gmsh and Mmg in the relative mode as well, so they are written alongside the factors
    check(recorded[PARAMETERS]["relative_size"] == "True" and recorded[PARAMETERS]["max_size"] == "7.0",
          "the relative meshing parameters did not take their sizes: " + str(recorded[PARAMETERS]))
    for name, deltmx in STEPS.items():
        check(recorded[name] == deltmx, name + " recorded deltmx " + recorded[name] + ", expected " + deltmx)
    for name in [name for name, _ in CHECKED] + list(STEPS):
        check(name in source, name + " is missing from the journal:" + os.linesep + source)
    check("replace_coupled_temp_disp" in source, "the replace of " + COUPLED + " is missing from the journal:" +
          os.linesep + source)
    try:
        exec(source, {"pmx": pmx})
    except Exception as ex:
        raise Exception("TEST FAILED: the journal did not replay: " + str(ex) + os.linesep + source)
    items = pmx.model.mesh.setup_items
    for name, keys in CHECKED:
        replayed = items[name].get_settings()
        for key in keys:
            check(str(replayed[key]) == recorded[name][key],
                  name + "." + key + " replayed as " + str(replayed[key]) + ", recorded as " +
                  recorded[name][key])
    for name, deltmx in STEPS.items():
        replayed = str(pmx.model.steps[name].get_value("deltmx"))
        check(replayed == deltmx, name + ".deltmx replayed as " + replayed + ", recorded as " + deltmx)
    items.remove([name for name, _ in CHECKED])
    pmx.model.steps.remove(list(STEPS))
    print("TEST PASSED")


main()
