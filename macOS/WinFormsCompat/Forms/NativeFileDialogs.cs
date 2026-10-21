// PrePoMax for macOS - the file and folder dialogs shown as native macOS panels through the storage provider of
// Avalonia. The panels are asynchronous; the dispatcher runs until they close so the dialogs keep the synchronous
// ShowDialog of WinForms. The test automation can answer the next dialogs with paths instead.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace System.Windows.Forms
{
    internal static class NativeFileDialogs
    {
        // Paths given by the test automation for the next dialogs; an empty entry cancels the dialog
        internal static readonly Queue<string> AutomationAnswers = new Queue<string>();

        internal static DialogResult Show(CommonDialog dialog)
        {
            if (AutomationAnswers.Count > 0) return Answer(dialog, AutomationAnswers.Dequeue());
            // A menu sweep of the test automation cancels every file dialog
            if (Automation.Sweeping)
            {
                Console.Error.WriteLine("[sweep] file dialog cancelled");
                return DialogResult.Cancel;
            }
            //
            Avalonia.Controls.Window owner = OwnerWindow();
            IStorageProvider provider = owner?.StorageProvider;
            if (provider == null) return DialogResult.Cancel;
            //
            Task<string[]> task = Pick(provider, dialog);
            Application.RunModal(() => task.IsCompleted);
            string[] paths = task.IsCompletedSuccessfully ? task.Result : null;
            if (paths == null || paths.Length == 0) return DialogResult.Cancel;
            return Accept(dialog, paths);
        }
        private static DialogResult Answer(CommonDialog dialog, string answer)
        {
            if (string.IsNullOrEmpty(answer)) return DialogResult.Cancel;
            return Accept(dialog, answer.Split('|').Select(p => p.Trim()).ToArray());
        }
        private static DialogResult Accept(CommonDialog dialog, string[] paths)
        {
            if (dialog is FolderBrowserDialog folder)
            {
                folder.SelectedPath = paths[0];
                return DialogResult.OK;
            }
            FileDialog file = (FileDialog)dialog;
            if (dialog is SaveFileDialog && file.AddExtension && !string.IsNullOrEmpty(file.DefaultExt))
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    if (IO.Path.GetExtension(paths[i]).Length == 0) paths[i] += "." + file.DefaultExt.TrimStart('.');
                }
            }
            file.FileNames = paths;
            ComponentModel.CancelEventArgs e = new ComponentModel.CancelEventArgs();
            file.OnFileOk(e);
            return e.Cancel ? DialogResult.Cancel : DialogResult.OK;
        }
        private static Avalonia.Controls.Window OwnerWindow()
        {
            Form active = Form.ActiveForm;
            if (active?.NativeWindow != null && active.Visible) return active.NativeWindow;
            foreach (Form form in Application.OpenForms) if (form.Visible && form.NativeWindow != null) active = form;
            return active?.NativeWindow;
        }
        private static async Task<string[]> Pick(IStorageProvider provider, CommonDialog dialog)
        {
            if (dialog is FolderBrowserDialog folder)
            {
                FolderPickerOpenOptions options = new FolderPickerOpenOptions
                {
                    Title = folder.UseDescriptionForTitle || string.IsNullOrEmpty(folder.Description) ? folder.Description : null,
                    SuggestedStartLocation = await Folder(provider, string.IsNullOrEmpty(folder.SelectedPath) ?
                                                                    folder.InitialDirectory : folder.SelectedPath),
                };
                IReadOnlyList<IStorageFolder> folders = await provider.OpenFolderPickerAsync(options);
                return folders.Select(f => f.TryGetLocalPath()).Where(p => p != null).ToArray();
            }
            FileDialog file = (FileDialog)dialog;
            List<FilePickerFileType> types = FileTypes(file.Filter, file.FilterIndex);
            string directory = file.InitialDirectory;
            if (string.IsNullOrEmpty(directory) && IO.Path.IsPathRooted(file.FileName))
                directory = IO.Path.GetDirectoryName(file.FileName);
            IStorageFolder start = await Folder(provider, directory);
            //
            if (dialog is SaveFileDialog save)
            {
                FilePickerSaveOptions options = new FilePickerSaveOptions
                {
                    Title = NullIfEmpty(save.Title),
                    SuggestedFileName = NullIfEmpty(IO.Path.GetFileName(save.FileName)),
                    DefaultExtension = NullIfEmpty(save.DefaultExt),
                    FileTypeChoices = types.Count > 0 ? types : null,
                    ShowOverwritePrompt = save.OverwritePrompt,
                    SuggestedStartLocation = start,
                };
                IStorageFile result = await provider.SaveFilePickerAsync(options);
                string path = result?.TryGetLocalPath();
                return path == null ? null : new[] { path };
            }
            else
            {
                OpenFileDialog open = (OpenFileDialog)dialog;
                FilePickerOpenOptions options = new FilePickerOpenOptions
                {
                    Title = NullIfEmpty(open.Title),
                    AllowMultiple = open.Multiselect,
                    FileTypeFilter = types.Count > 0 ? types : null,
                    SuggestedStartLocation = start,
                };
                IReadOnlyList<IStorageFile> files = await provider.OpenFilePickerAsync(options);
                return files.Select(f => f.TryGetLocalPath()).Where(p => p != null).ToArray();
            }
        }
        private static string NullIfEmpty(string text) { return string.IsNullOrEmpty(text) ? null : text; }
        private static async Task<IStorageFolder> Folder(IStorageProvider provider, string path)
        {
            if (string.IsNullOrEmpty(path) || !IO.Directory.Exists(path)) return null;
            try { return await provider.TryGetFolderFromPathAsync(path); }
            catch { return null; }
        }
        // "Name|*.a;*.b|Name 2|*.c" - the type of the filter index comes first because the panel starts with it
        internal static List<FilePickerFileType> FileTypes(string filter, int filterIndex)
        {
            List<FilePickerFileType> types = new List<FilePickerFileType>();
            if (string.IsNullOrEmpty(filter)) return types;
            string[] parts = filter.Split('|');
            for (int i = 0; i + 1 < parts.Length; i += 2)
            {
                string[] patterns = parts[i + 1].Split(';').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
                types.Add(new FilePickerFileType(parts[i].Trim()) { Patterns = patterns });
            }
            int index = filterIndex - 1;
            if (index > 0 && index < types.Count)
            {
                FilePickerFileType selected = types[index];
                types.RemoveAt(index);
                types.Insert(0, selected);
            }
            return types;
        }
    }
}
