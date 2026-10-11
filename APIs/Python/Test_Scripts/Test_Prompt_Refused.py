#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the strict prompt mode of the batch contract. Running an analysis on a model whose elements
#have no section assignment makes the application ask the user whether to continue, and in strict
#mode a question that needs an answer is refused instead of being guessed.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Prompt_Refused.py -g No -x Yes -w <dir>
#
#Expected process exit code 5, PromptRefused. The script must NOT print TEST PASSED, because the
#refusal is raised inside the run and terminates the script. The report must show the question that
#was refused, not merely some refusal:
#Expected report prompts[-1].caption = "Warning"
#Expected report prompts[-1].text contains "finite elements are missing a section assignment. Continue?"
#Expected report prompts[-1].buttons = "OKCancel"
#Expected report prompts[-1].refused = true
#Expected report error contains "finite elements are missing a section assignment"

PREFIX = "PyTest_"
JOB = PREFIX + "Refused"
STEP = PREFIX + "Static"


def main():
    if not pmx.model.is_open():
        raise Exception("TEST FAILED: a model must be open")
    # A step is needed for the analysis to have something to run, but no section is assigned
    step = pmx.model.steps.add_static(STEP)
    step.bcs.add_fixed(PREFIX + "Fix",
                       pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4]))
    #
    job = pmx.jobs.add(JOB)
    # The missing section assignment raises an OK or Cancel question, which strict mode refuses
    job.run(timeout=120)
    #
    raise Exception("TEST FAILED: the missing section assignment must be refused in strict mode")


main()
