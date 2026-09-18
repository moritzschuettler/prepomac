// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;
using DynamicTypeDescriptor;
using System.Linq.Expressions;
using System.Drawing;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace CaeGlobals
{
    [Serializable]
    public static class MessageBoxes
    {
        // MessageBox
        public static Form ParentForm;
        public static Action<string> WriteDataToOutput;
        public static MessageBoxModeEnum Mode = MessageBoxModeEnum.Interactive;
        public static Action<MessageBoxPrompt> PromptRecorder;
        // A call of the automation server runs on its own connection thread while the window stays
        // interactive for the user. A dialog opened from there is shown on the window and the call waits
        // for it, so the agent waits for a human at the keyboard - which is the one thing the server
        // exists not to need. A handler marks its thread and every prompt on it is answered the way an
        // unattended run answers it: a report goes to the output and a decision is refused
        [ThreadStatic] private static bool _isNonInteractiveThread;
        public static bool IsNonInteractiveThread
        {
            get { return _isNonInteractiveThread; }
            set { _isNonInteractiveThread = value; }
        }
        // Base
        public static DialogResult Show(string text)
        {
            return Show(text, "");
        }
        public static DialogResult Show(string text, string caption)
        {
            return Show(text, caption, MessageBoxButtons.OK);
        }
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
        {
            return Show(text, caption, buttons, MessageBoxIcon.None);
        }
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            DialogResult result = DialogResult.None;
            // Read on the calling thread, because the marked thread is the one that asked and the hop below
            // lands on the user interface thread, which is never marked
            MessageBoxModeEnum mode = GetEffectiveMode();
            InvokeIfRequired(() => result = ShowCrossThread(text, caption, buttons, icon, mode));
            return result;
        }
        public static DialogResult ShowCrossThread(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return ShowCrossThread(text, caption, buttons, icon, GetEffectiveMode());
        }
        private static MessageBoxModeEnum GetEffectiveMode()
        {
            if (Mode == MessageBoxModeEnum.Interactive && _isNonInteractiveThread) return MessageBoxModeEnum.Strict;
            return Mode;
        }
        private static DialogResult ShowCrossThread(string text, string caption, MessageBoxButtons buttons,
                                                    MessageBoxIcon icon, MessageBoxModeEnum mode)
        {
            using (new CenterWinDialog(ParentForm))
            {
                if (mode == MessageBoxModeEnum.Interactive) return MessageBox.Show(ParentForm, text, caption, buttons, icon);
                //
                if (WriteDataToOutput != null)
                {
                    WriteDataToOutput(caption);
                    WriteDataToOutput(text);
                }
                // A single button box only reports; anything else asks for a decision the caller cannot make unattended
                bool isDecision = buttons != MessageBoxButtons.OK;
                if (isDecision && mode == MessageBoxModeEnum.Strict)
                {
                    RecordPrompt(caption, text, buttons, DialogResult.None, true, true);
                    throw new PromptRefusedException("An interactive prompt was refused in the strict non-interactive mode." +
                                                     Environment.NewLine + caption + ": " + text);
                }
                //
                DialogResult result = GetNonInteractiveResult(buttons);
                RecordPrompt(caption, text, buttons, result, isDecision, false);
                return result;
            }
        }
        private static DialogResult GetNonInteractiveResult(MessageBoxButtons buttons)
        {
            if (buttons == MessageBoxButtons.OKCancel) return DialogResult.OK;
            else if (buttons == MessageBoxButtons.YesNo) return DialogResult.Yes;
            else if (buttons == MessageBoxButtons.YesNoCancel) return DialogResult.Yes;
            else return DialogResult.OK;
        }
        private static void RecordPrompt(string caption, string text, MessageBoxButtons buttons, DialogResult answer,
                                         bool isDecision, bool isRefused)
        {
            Action<MessageBoxPrompt> recorder = PromptRecorder;
            if (recorder == null) return;
            //
            recorder(new MessageBoxPrompt(caption, text, buttons.ToString(), answer.ToString(), isDecision, isRefused));
        }
        private static object InvokeIfRequired(Func<object> function)
        {
            if (ParentForm.InvokeRequired)
            {
                return (object)ParentForm.Invoke((MethodInvoker)delegate () { function(); });
            }
            else
            {
                return function();
            }
        }


        // Information
        public static void ShowInfo(string text)
        {
            ShowInfo(text, "");
        }
        public static void ShowInfo(string text, string caption)
        {
            ShowInfo(text, caption, MessageBoxButtons.OK);
        }
        public static DialogResult ShowInfo(string text, string caption, MessageBoxButtons buttons)
        {
            return Show(text, caption, buttons, MessageBoxIcon.None);
        }
        // Question
        public static DialogResult ShowQuestionYesNo(string caption, string text)
        {
            return Show(text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }
        // Warning
        public static void ShowWarning(string text)
        {
            Show(text, "Warning", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
        public static DialogResult ShowWarningQuestionOKCancel(string text)
        {
            return Show(text, "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        }
        public static DialogResult ShowWarningQuestionYesNo(string text)
        {
            return Show(text, "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }
        public static DialogResult ShowWarningQuestionYesNoCancel(string text)
        {
            return Show(text, "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        }
        public static DialogResult ShowWarningQuestionYesNoCancel(string caption, string text)
        {
            return Show(text, caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        }
        public static DialogResult ShowWarningQuestionOKCancel(IWin32Window owner, string text)
        {
            // The owner based overload must not open a blocking dialog when no user is present
            if (GetEffectiveMode() != MessageBoxModeEnum.Interactive)
                return Show(text, "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            //
            return MessageBox.Show(owner, text, "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        }
        // Error
        public static void ShowError(string text)
        {
            Show(text, "Error", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
        public static void ShowWorkDirectoryError()
        {
            Show("The work directory does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    }
}
