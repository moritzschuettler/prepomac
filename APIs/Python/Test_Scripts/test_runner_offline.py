"""Offline checks for Run_Tests.py; no PrePoMax process is launched.

    python -m unittest test_runner_offline
"""
import io
import os
import tempfile
import unittest

import Run_Tests


def write_script(directory, name, header):
    path = os.path.join(directory, name)
    with io.open(path, "w", encoding="utf-8") as handle:
        handle.write(header + u"\nprint('body')\n")
    return path


class ReadHeaderTests(unittest.TestCase):
    def test_missing_command_and_code_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_Nothing.py", u"# no command or expected code")
            self.assertRaises(ValueError, Run_Tests.read_header, path)

    def test_missing_expected_code_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_NoCode.py", u"#  PrePoMax.com -f cube.inp --script Test_NoCode.py")
            self.assertRaises(ValueError, Run_Tests.read_header, path)

    def test_missing_command_line_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_NoCommand.py", u"#Expected process exit code 0.")
            self.assertRaises(ValueError, Run_Tests.read_header, path)

    def test_model_unit_system_and_code_are_read(self):
        header = u"#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model.py\n#Expected process exit code 3."
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_Model.py", header)
            self.assertEqual([({"-f": "cube.inp", "-u": "MM_TON_S_C"}, 3, [])], Run_Tests.read_header(path))

    def test_the_automation_server_switch_is_read(self):
        header = u"#  PrePoMax.com --script Test_Rpc.py --rpc On -g No -x Yes\n#Expected process exit code 0."
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_Rpc.py", header)
            self.assertEqual([({"--rpc": "On"}, 0, [])], Run_Tests.read_header(path))

    def test_a_test_without_a_model_is_accepted(self):
        header = u"#  PrePoMax.com --script Test_Batch.py -g No -x Yes\n#Expected process exit code 0."
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_Batch.py", header)
            self.assertEqual([({}, 0, [])], Run_Tests.read_header(path))

    def test_a_unit_system_without_a_model_is_kept(self):
        header = u"#  PrePoMax.com -u MM_TON_S_C --script Test_Mesh.py -g No -x Yes\n#Expected process exit code 0."
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_Mesh.py", header)
            self.assertEqual([({"-u": "MM_TON_S_C"}, 0, [])], Run_Tests.read_header(path))

    def test_every_command_line_is_a_run_with_its_own_code(self):
        header = (u"#  PrePoMax.com --script Test_Batch.py -g No -x Yes\n"
                  u"#\n"
                  u"#Expected process exit code 0 and a run.json with \"exitStatus\": \"Ok\".\n"
                  u"#Pass the argument 'fail':\n"
                  u"#  PrePoMax.com --script Test_Batch.py --scriptArgs fail -g No -x Yes\n"
                  u"#Expected process exit code 3.\n"
                  u"#  PrePoMax.com --script Test_Batch.py --scriptArgs sleep --timeout 30 -g No -x Yes\n"
                  u"#Expected process exit code 4.")
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_Batch.py", header)
            self.assertEqual([({}, 0, []), ({"--scriptArgs": "fail"}, 3, []),
                              ({"--scriptArgs": "sleep", "--timeout": "30"}, 4, [])], Run_Tests.read_header(path))

    def test_a_quoted_script_argument_stays_whole(self):
        header = (u"#  PrePoMax.com --script Test_Batch.py --scriptArgs \"one two\" -g No -x Yes\n"
                  u"#Expected process exit code 0.")
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_Batch.py", header)
            self.assertEqual([({"--scriptArgs": "one two"}, 0, [])], Run_Tests.read_header(path))

    def test_a_command_line_without_its_code_is_rejected(self):
        header = (u"#  PrePoMax.com --script Test_Batch.py -g No -x Yes\n#Expected process exit code 0.\n"
                  u"#  PrePoMax.com --script Test_Batch.py --scriptArgs fail -g No -x Yes")
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_Batch.py", header)
            self.assertRaises(ValueError, Run_Tests.read_header, path)

    def test_a_time_limit_that_is_not_a_number_is_rejected(self):
        header = u"#  PrePoMax.com --script Test_Batch.py --timeout <seconds>\n#Expected process exit code 4."
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_Batch.py", header)
            self.assertRaises(ValueError, Run_Tests.read_header, path)

    def test_report_expectations_belong_to_the_run_before_them(self):
        header = (u"#  PrePoMax.com --script Test_Prompt.py -g No -x Yes\n"
                  u"#Expected process exit code 5, PromptRefused.\n"
                  u"#Expected report prompts[-1].caption = \"Warning\"\n"
                  u"#Expected report prompts[-1].refused = true\n"
                  u"#Expected report error contains \"missing a section\"")
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_Prompt.py", header)
            self.assertEqual([({}, 5, [("prompts[-1].caption", "=", "Warning"), ("prompts[-1].refused", "=", True),
                                       ("error", "contains", "missing a section")])],
                             Run_Tests.read_header(path))

    def test_a_report_expectation_before_any_command_line_is_rejected(self):
        header = (u"#Expected report error contains \"x\"\n"
                  u"#  PrePoMax.com --script Test_Batch.py\n#Expected process exit code 0.")
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_Batch.py", header)
            self.assertRaises(ValueError, Run_Tests.read_header, path)

    def test_a_report_value_that_is_not_json_is_rejected(self):
        header = u"#  PrePoMax.com --script Test_Batch.py\n#Expected process exit code 0.\n#Expected report error = Warning"
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_Batch.py", header)
            self.assertRaises(ValueError, Run_Tests.read_header, path)

    def test_contains_needs_a_string(self):
        header = u"#  PrePoMax.com --script Test_Batch.py\n#Expected process exit code 0.\n#Expected report error contains 3"
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_Batch.py", header)
            self.assertRaises(ValueError, Run_Tests.read_header, path)

    def test_the_headers_of_the_scripts_are_read(self):
        # Every script in this folder must declare runs the runner can read
        tests = Run_Tests.collect(None)
        self.assertTrue(tests)
        batch_codes = sorted(test.expected_code for test in tests if test.name == "Test_Batch_Run.py")
        self.assertEqual([0, 3, 4, 7], batch_codes)
        prompt = [test for test in tests if test.name == "Test_Prompt_Refused.py"]
        self.assertEqual(1, len(prompt))
        self.assertIn(("prompts[-1].refused", "=", True), prompt[0].report_checks)


class ReportCheckTests(unittest.TestCase):
    REPORT = {"error": "Warning: 8 finite elements are missing a section assignment.",
              "prompts": [{"caption": "Info", "refused": False}, {"caption": "Warning", "refused": True}]}

    def test_matching_expectations_pass(self):
        checks = [("prompts[-1].caption", "=", "Warning"), ("prompts[1].refused", "=", True),
                  ("error", "contains", "missing a section")]
        self.assertEqual([], Run_Tests.report_problems(self.REPORT, checks))

    def test_a_different_value_fails(self):
        problems = Run_Tests.report_problems(self.REPORT, [("prompts[0].caption", "=", "Warning")])
        self.assertEqual(1, len(problems))

    def test_a_missing_text_fails(self):
        problems = Run_Tests.report_problems(self.REPORT, [("error", "contains", "time limit")])
        self.assertEqual(1, len(problems))

    def test_a_missing_path_fails(self):
        problems = Run_Tests.report_problems(self.REPORT, [("prompts[5].caption", "=", "Warning"),
                                                           ("journal.written", "=", True)])
        self.assertEqual(2, len(problems))

    def test_contains_on_a_value_that_is_not_text_fails(self):
        problems = Run_Tests.report_problems({"error": None}, [("error", "contains", "x")])
        self.assertEqual(1, len(problems))


class CommandTests(unittest.TestCase):
    def test_a_model_less_test_is_launched_without_a_file(self):
        test = Run_Tests.Test("Test_Batch.py", {}, 0)
        command = Run_Tests.build_command(test, "PrePoMax.com", "work", "report.json")
        self.assertNotIn("-f", command)
        self.assertNotIn("-u", command)
        self.assertIn("--script", command)

    def test_a_result_test_reads_the_solved_model_from_the_work_directory(self):
        test = Run_Tests.Test("Test_Result.py", {"-f": Run_Tests.RESULT_FILE}, 0)
        command = Run_Tests.build_command(test, "PrePoMax.com", "work", "report.json")
        self.assertEqual(os.path.join("work", Run_Tests.RESULT_FILE), command[command.index("-f") + 1])

    def test_a_model_test_reads_its_model_from_the_models_directory(self):
        test = Run_Tests.Test("Test_Model.py", {"-f": "cube.inp"}, 0)
        command = Run_Tests.build_command(test, "PrePoMax.com", "work", "report.json")
        self.assertEqual(os.path.join(Run_Tests.MODELS, "cube.inp"), command[command.index("-f") + 1])
        self.assertEqual(Run_Tests.DEFAULT_UNIT_SYSTEM, command[command.index("-u") + 1])

    def test_the_unit_system_of_the_header_is_passed_on(self):
        test = Run_Tests.Test("Test_Mesh.py", {"-u": "M_KG_S_C"}, 0)
        command = Run_Tests.build_command(test, "PrePoMax.com", "work", "report.json")
        self.assertEqual("M_KG_S_C", command[command.index("-u") + 1])
        self.assertNotIn("-f", command)

    def test_the_automation_server_switch_is_passed_on(self):
        test = Run_Tests.Test("Test_Rpc.py", {"--rpc": "On"}, 0)
        command = Run_Tests.build_command(test, "PrePoMax.com", "work", "report.json")
        self.assertEqual("On", command[command.index("--rpc") + 1])

    def test_a_test_that_does_not_ask_for_the_server_does_not_get_it(self):
        test = Run_Tests.Test("Test_Batch.py", {}, 0)
        command = Run_Tests.build_command(test, "PrePoMax.com", "work", "report.json")
        self.assertNotIn("--rpc", command)

    def test_the_script_arguments_are_passed_on(self):
        test = Run_Tests.Test("Test_Batch.py", {"--scriptArgs": "one two"}, 3)
        command = Run_Tests.build_command(test, "PrePoMax.com", "work", "report.json")
        self.assertEqual("one two", command[command.index("--scriptArgs") + 1])
        self.assertLess(command.index("--script"), command.index("--scriptArgs"))

    def test_the_time_limit_of_the_header_replaces_the_default(self):
        test = Run_Tests.Test("Test_Batch.py", {"--timeout": "30"}, 4)
        command = Run_Tests.build_command(test, "PrePoMax.com", "work", "report.json")
        self.assertEqual(["30"], [command[i + 1] for i, item in enumerate(command) if item == "--timeout"])
        default = Run_Tests.build_command(Run_Tests.Test("Test_Batch.py", {}, 0), "PrePoMax.com", "work", "r.json")
        self.assertEqual(str(Run_Tests.SCRIPT_TIMEOUT), default[default.index("--timeout") + 1])

    def test_the_runs_of_one_script_have_their_own_labels_and_files(self):
        single = Run_Tests.Test("Test_Batch.py", {}, 0)
        second = Run_Tests.Test("Test_Batch.py", {"--scriptArgs": "fail"}, 3, index=2, count=3)
        self.assertEqual(("Test_Batch.py", "Test_Batch.py"), (single.label, single.file_stem))
        self.assertEqual(("Test_Batch.py [2]", "Test_Batch.py.2"), (second.label, second.file_stem))


class OutputTests(unittest.TestCase):
    def test_a_passing_run_must_print_the_marker(self):
        test = Run_Tests.Test("Test_Batch.py", {}, 0)
        self.assertEqual([], Run_Tests.output_problems(test, "TEST PASSED"))
        self.assertEqual(1, len(Run_Tests.output_problems(test, "nothing")))

    def test_a_failing_run_must_not_print_the_marker(self):
        test = Run_Tests.Test("Test_Batch.py", {"--scriptArgs": "fail"}, 3)
        self.assertEqual([], Run_Tests.output_problems(test, "Traceback"))
        self.assertEqual(1, len(Run_Tests.output_problems(test, "TEST PASSED")))


class OfflineCheckTests(unittest.TestCase):
    def test_the_audits_and_these_tests_run_offline(self):
        offline = Run_Tests.collect_offline(None)
        self.assertIn("Audit_API_Coverage.py", offline)
        self.assertIn("Audit_API_Documentation.py", offline)
        self.assertIn(Run_Tests.OFFLINE_RUNNER_TESTS, offline)

    def test_a_filter_selects_the_offline_checks_by_name(self):
        self.assertEqual(["Audit_API_Documentation.py"], Run_Tests.collect_offline("api_documentation"))
        self.assertEqual([], Run_Tests.collect_offline("Model_BCs"))

    def test_several_filters_select_every_script_matching_any_of_them(self):
        names = sorted(set(test.name for test in Run_Tests.collect(["Model_BCs", "model_loads"])))
        self.assertEqual(["Test_Model_BCs.py", "Test_Model_Loads.py"], names)
        self.assertEqual(["Audit_API_Coverage.py", "Audit_API_Documentation.py"],
                         Run_Tests.collect_offline(["api_coverage", "API_Documentation"]))

    def test_a_result_test_brings_the_analysis_that_writes_its_model(self):
        tests = Run_Tests.collect(["Test_Api_View_Results"])
        self.assertEqual(["Test_Analysis.py", "Test_Api_View_Results.py"], [test.name for test in tests])


class FixtureTests(unittest.TestCase):
    def test_a_stale_solved_model_is_removed(self):
        with tempfile.TemporaryDirectory() as directory:
            stale = os.path.join(directory, Run_Tests.RESULT_FILE)
            with io.open(stale, "w", encoding="utf-8") as handle:
                handle.write(u"stale")
            Run_Tests.clear_result_fixture(directory)
            self.assertFalse(os.path.exists(stale))


if __name__ == "__main__":
    unittest.main()
