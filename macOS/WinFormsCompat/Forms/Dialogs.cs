// PrePoMax for macOS - the WinForms file dialogs. They are shown as native panels through PlatformHooks.ShowCommonDialog.

using System.ComponentModel;
using System.Drawing.Design;

namespace System.Windows.Forms
{
    public enum FormWindowState { Normal = 0, Minimized = 1, Maximized = 2 }

    public abstract class CommonDialog : Component
    {
        public object Tag { get; set; }
        public DialogResult ShowDialog() { return ShowDialog(null); }
        public DialogResult ShowDialog(IWin32Window owner) { return PlatformHooks.ShowCommonDialog(this); }
        public virtual void Reset() { }
    }

    public abstract class FileDialog : CommonDialog
    {
        private string[] _fileNames = new string[0];

        public string Filter { get; set; } = string.Empty;
        public int FilterIndex { get; set; } = 1;
        public string InitialDirectory { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string DefaultExt { get; set; } = string.Empty;
        public bool AddExtension { get; set; } = true;
        public bool CheckFileExists { get; set; }
        public bool CheckPathExists { get; set; } = true;
        public bool RestoreDirectory { get; set; }
        public bool ValidateNames { get; set; } = true;
        public bool DereferenceLinks { get; set; } = true;
        public bool SupportMultiDottedExtensions { get; set; }
        public bool AutoUpgradeEnabled { get; set; } = true;
        public bool ShowHelp { get; set; }
        public string FileName
        {
            get { return _fileNames.Length > 0 ? _fileNames[0] : string.Empty; }
            set { _fileNames = string.IsNullOrEmpty(value) ? new string[0] : new string[] { value }; }
        }
        public string[] FileNames
        {
            get { return (string[])_fileNames.Clone(); }
            set { _fileNames = value ?? new string[0]; }
        }
        public event CancelEventHandler FileOk;
        public void OnFileOk(CancelEventArgs e) { FileOk?.Invoke(this, e); }
    }

    public sealed class OpenFileDialog : FileDialog
    {
        public OpenFileDialog() { CheckFileExists = true; }
        public bool Multiselect { get; set; }
        public bool ReadOnlyChecked { get; set; }
        public bool ShowReadOnly { get; set; }
        public IO.Stream OpenFile() { return IO.File.OpenRead(FileName); }
    }

    public sealed class SaveFileDialog : FileDialog
    {
        public bool OverwritePrompt { get; set; } = true;
        public bool CreatePrompt { get; set; }
    }

    public sealed class FolderBrowserDialog : CommonDialog
    {
        public string SelectedPath { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool ShowNewFolderButton { get; set; } = true;
        public bool UseDescriptionForTitle { get; set; }
        public string InitialDirectory { get; set; } = string.Empty;
        public Environment.SpecialFolder RootFolder { get; set; } = Environment.SpecialFolder.Desktop;
    }

    public static partial class PlatformHooks
    {
        // Shows the dialog and fills in the selected paths
        public static Func<CommonDialog, DialogResult> ShowCommonDialog { get; set; } = NativeFileDialogs.Show;
    }
}

namespace System.Windows.Forms
{
    // ActiveX hosting does not exist on macOS; the name is referenced by using static directives only
    public class AxHost : Control
    {
        protected AxHost(string clsid) { }
    }
}

namespace System.Windows.Forms.Design
{
    public static class AxImporter { }

    public class FileNameEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { return UITypeEditorEditStyle.Modal; }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "All files|*.*";
            InitializeDialog(dialog);
            if (value is string path && path.Length > 0)
            {
                dialog.FileName = path;
                try { dialog.InitialDirectory = IO.Path.GetDirectoryName(path); } catch { }
            }
            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : value;
        }
        protected virtual void InitializeDialog(OpenFileDialog openFileDialog) { }
    }

    public class FolderNameEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { return UITypeEditorEditStyle.Modal; }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (value is string path) dialog.SelectedPath = path;
            return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : value;
        }
    }
}
