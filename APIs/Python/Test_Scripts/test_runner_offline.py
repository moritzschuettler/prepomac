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

    def test_model_and_code_are_read(self):
        header = u"#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model.py\n#Expected process exit code 3."
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_Model.py", header)
            self.assertEqual(("cube.inp", 3), Run_Tests.read_header(path))

    def test_a_test_without_a_model_is_accepted(self):
        header = u"#  PrePoMax.com --script Test_Batch.py -g No -x Yes\n#Expected process exit code 0."
        with tempfile.TemporaryDirectory() as directory:
            path = write_script(directory, "Test_Batch.py", header)
            self.assertEqual((None, 0), Run_Tests.read_header(path))


class CommandTests(unittest.TestCase):
    def test_a_model_less_test_is_launched_without_a_file(self):
        test = Run_Tests.Test("Test_Batch.py", None, 0)
        command = Run_Tests.build_command(test, "PrePoMax.com", "work", "report.json")
        self.assertNotIn("-f", command)
        self.assertIn("--script", command)

    def test_a_result_test_reads_the_solved_model_from_the_work_directory(self):
        test = Run_Tests.Test("Test_Result.py", Run_Tests.RESULT_FILE, 0)
        command = Run_Tests.build_command(test, "PrePoMax.com", "work", "report.json")
        self.assertEqual(os.path.join("work", Run_Tests.RESULT_FILE), command[command.index("-f") + 1])

    def test_a_model_test_reads_its_model_from_the_models_directory(self):
        test = Run_Tests.Test("Test_Model.py", "cube.inp", 0)
        command = Run_Tests.build_command(test, "PrePoMax.com", "work", "report.json")
        self.assertEqual(os.path.join(Run_Tests.MODELS, "cube.inp"), command[command.index("-f") + 1])


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
