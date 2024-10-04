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

namespace CaeGlobals
{
    [Serializable]
    public static class MessageBoxes
    {
        // MessageBox
        public static Form ParentForm;
        public static Action<string> WriteDataToOutput;
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
            using (new CenterWinDialog(ParentForm))
            {
                if (WriteDataToOutput != null)
                {
                    WriteDataToOutput(caption);
                    WriteDataToOutput(text);
                    if (buttons == MessageBoxButtons.OKCancel) return DialogResult.OK;
                    else if (buttons == MessageBoxButtons.YesNo) return DialogResult.Yes;
                    else if (buttons == MessageBoxButtons.YesNoCancel) return DialogResult.Yes;
                    else return DialogResult.OK;
                }
                else return MessageBox.Show(text, caption, buttons, icon);
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
            return Show(text, caption, buttons, MessageBoxIcon.Information);
        }
        // Question
        public static DialogResult ShowQuestionYesNo(string caption, string text)
        {
            return Show(text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }
        // Warning
        public static void ShowWarning(string text)
        {
            Show(text, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        public static DialogResult ShowWarningQuestionOKCancel(string text)
        {
            return Show(text, "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
        }
        public static DialogResult ShowWarningQuestionYesNo(string text)
        {
            return Show(text, "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        }
        public static DialogResult ShowWarningQuestionYesNoCancel(string text)
        {
            return Show(text, "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
        }
        public static DialogResult ShowWarningQuestionOKCancel(IWin32Window owner, string text)
        {
            return MessageBox.Show(owner, text, "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
        }
        // Error
        public static void ShowError(string text)
        {
            Show(text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        public static void ShowWorkDirectoryError()
        {
            Show("The work directory does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
