// PrePoMax for macOS - a test automation of the WinForms compatibility layer. When the environment variable PPM_AUTOMATION
// names a script file, the commands of the script are run one after the other on the UI thread once the main form is
// shown. The commands:
//   wait <seconds>                   waits
//   snapshot <file.png>              renders the active form (or the main form) to a PNG file
//   snapshotall <prefix>             renders every open form to <prefix>_<n>_<name>.png
//   menu <Item/Sub item/...>         clicks a menu item of the main form by the texts of the items
//   click <name or text>             clicks a button, check box or tool strip item of the active form
//   set <name> <text>                sets the text of a control of the active form
//   select <name> <index>            selects an item of a combo box or list box
//   file <path|path...>              answers the next file or folder dialog with the paths (no path cancels it)
//   invoke <type.method>             calls a static method without parameters (test hooks of the application)
//   dump [form name]                 writes the control tree of the active or the named form
//   sweep [skip text;skip text...]  clicks every menu item of the main form and closes what it opens; the
//                                    items whose path contains one of the texts are skipped
//   cellclick <grid> <row> <column>  clicks a cell of a data grid view (the column by its name or header text)
//   cell <grid> <row> <column> <text> sets the value of a cell as an edit does
//   selectrow <grid> <text>          selects the row of a data grid view or the item of a list view with the text
//   log <text>                       writes the text to the output
//   quit                             closes the application
// Each executed command is written to the error output, so the log shows how far a script got.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace System.Windows.Forms
{
    public static class Automation
    {
        private static Queue<string> _commands;
        private static DispatcherTimer _timer;

        internal static void Start()
        {
            string file = Environment.GetEnvironmentVariable("PPM_AUTOMATION");
            if (string.IsNullOrEmpty(file) || !File.Exists(file)) return;
            _commands = new Queue<string>(File.ReadAllLines(file).Select(l => l.Trim())
                                          .Where(l => l.Length > 0 && !l.StartsWith("#")));
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += (s, e) => Next();
            _timer.Start();
        }
        private static void Next()
        {
            _timer.Stop();
            if (_commands.Count == 0) return;
            string line = _commands.Dequeue();
            Console.Error.WriteLine("[automation] " + line);
            TimeSpan delay = TimeSpan.FromMilliseconds(300);
            try
            {
                string[] parts = line.Split(new[] { ' ' }, 2);
                string command = parts[0].ToLowerInvariant();
                string argument = parts.Length > 1 ? parts[1].Trim() : "";
                switch (command)
                {
                    case "wait":
                        delay = TimeSpan.FromSeconds(double.Parse(argument, System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    default:
                        // The next command is scheduled before this one runs: a command that opens a modal dialog
                        // returns only when the dialog closes, and the commands after it run in the loop of the dialog
                        _timer.Interval = delay;
                        _timer.Start();
                        Execute(command, argument);
                        return;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[automation] failed: " + ex);
            }
            _timer.Interval = delay;
            _timer.Start();
        }
        private static void Execute(string command, string argument)
        {
            try
            {
                switch (command)
                {
                    case "snapshot":
                        Snapshot(ActiveForm(), argument);
                        break;
                    case "snapshotall":
                        {
                            int n = 0;
                            foreach (Form form in Application.OpenForms.Cast<Form>().ToArray())
                            {
                                Console.Error.WriteLine("[automation] form " + form.Name + " visible " + form.Visible);
                                if (!form.Visible) continue;
                                Snapshot(form, argument + "_" + (n++) + "_" + Sanitize(form.Name) + ".png");
                            }
                            break;
                        }
                    case "menu":
                        ClickMenu(argument);
                        break;
                    case "click":
                        Click(argument);
                        break;
                    case "set":
                        {
                            string[] a = argument.Split(new[] { ' ' }, 2);
                            Control control = Find(ActiveForm(), a[0]);
                            if (control == null) throw new InvalidOperationException("No control " + a[0]);
                            control.Text = a.Length > 1 ? a[1] : "";
                            break;
                        }
                    case "select":
                        {
                            string[] a = argument.Split(' ');
                            Control control = Find(ActiveForm(), a[0]);
                            int index = int.Parse(a[1]);
                            if (control is ComboBox cb) cb.SelectedIndex = index;
                            else if (control is ListBox lb) lb.SelectedIndex = index;
                            else if (control is ListView lv) { lv.SelectedIndices.Clear(); lv.Items[index].Selected = true; }
                            else throw new InvalidOperationException("No list " + a[0]);
                            break;
                        }
                    case "file":
                        NativeFileDialogs.AutomationAnswers.Enqueue(argument);
                        break;
                    case "invoke":
                        {
                            int dot = argument.LastIndexOf('.');
                            string typeName = argument.Substring(0, dot), method = argument.Substring(dot + 1);
                            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetType(typeName))
                                                 .FirstOrDefault(t => t != null);
                            if (type == null) throw new InvalidOperationException("No type " + typeName);
                            type.GetMethod(method, Reflection.BindingFlags.Static | Reflection.BindingFlags.Public |
                                                   Reflection.BindingFlags.NonPublic).Invoke(null, null);
                            break;
                        }
                    case "dump":
                        {
                            Form form = argument.Length > 0 ? Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.Name == argument)
                                                            : ActiveForm();
                            if (form == null)
                                throw new InvalidOperationException("No form " + argument + "; open forms: " +
                                    string.Join(", ", Application.OpenForms.Cast<Form>().Select(f => f.Name + (f.Visible ? "" : " (hidden)"))));
                            Dump(form, 0);
                            break;
                        }
                        break;
                    case "screens":
                        {
                            Avalonia.Controls.Window window = ActiveForm()?.NativeWindow;
                            foreach (var screen in window?.Screens.All ?? new Avalonia.Platform.Screen[0])
                                Console.Error.WriteLine("[automation] screen bounds " + screen.Bounds + " working area " +
                                                        screen.WorkingArea + " scaling " + screen.Scaling);
                            if (window != null)
                                Console.Error.WriteLine("[automation] window position " + window.Position + " size " + window.ClientSize +
                                                        " desktop scaling " + window.DesktopScaling + " render scaling " + window.RenderScaling);
                            foreach (Screen screen in Screen.AllScreens)
                                Console.Error.WriteLine("[automation] winforms screen " + screen.Bounds + " " + screen.WorkingArea);
                            break;
                        }
                    case "log":
                        Console.Error.WriteLine("[automation] " + argument);
                        break;
                    case "cellclick":
                    case "cell":
                        {
                            string[] a = argument.Split(new[] { ' ' }, command == "cell" ? 4 : 3);
                            DataGridView grid = Find(ActiveForm(), a[0]) as DataGridView;
                            if (grid == null) throw new InvalidOperationException("No grid " + a[0]);
                            int row = int.Parse(a[1]);
                            DataGridViewColumn column = grid.Columns.Cast<DataGridViewColumn>()
                                .FirstOrDefault(c => c.Name == a[2] || c.HeaderText == a[2]) ??
                                grid.Columns[int.Parse(a[2])];
                            if (command == "cellclick") grid.AutomationClickCell(column.Index, row);
                            else
                            {
                                grid.CurrentCell = grid.Rows[row].Cells[column.Index];
                                grid.Rows[row].Cells[column.Index].Value = a.Length > 3 ? a[3] : "";
                            }
                            break;
                        }
                    case "selectrow":
                        {
                            string[] a = argument.Split(new[] { ' ' }, 2);
                            Control control = Find(ActiveForm(), a[0]);
                            if (control is DataGridView grid)
                            {
                                grid.ClearSelection();
                                foreach (DataGridViewRow row in grid.Rows)
                                {
                                    if (row.Cells.Cast<DataGridViewCell>().Any(c => Convert.ToString(c.Value) == a[1]))
                                    {
                                        row.Selected = true;
                                        grid.CurrentCell = row.Cells[0];
                                        break;
                                    }
                                }
                            }
                            else if (control is ListView list)
                            {
                                list.SelectedIndices.Clear();
                                foreach (ListViewItem item in list.Items) if (item.Text == a[1]) item.Selected = true;
                            }
                            else throw new InvalidOperationException("No grid or list " + a[0]);
                            break;
                        }
                    case "sweep":
                        StartSweep(argument);
                        break;
                    case "quit":
                        Environment.Exit(0);
                        break;
                    default:
                        Console.Error.WriteLine("[automation] unknown command " + command);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[automation] failed: " + ex);
            }
        }
        // The control tree with the bounds and the visibility of the controls
        private static void Dump(Control control, int depth)
        {
            if (control == null || depth > 12) return;
            Console.Error.WriteLine("[dump] " + new string(' ', 2 * depth) + control.GetType().Name + " '" + control.Name + "' " +
                                    control.Bounds + (control.VisibleSelf ? "" : " hidden") + " dock=" + control.Dock);
            foreach (Control child in control.Controls) Dump(child, depth + 1);
        }
        private static string Sanitize(string name)
        {
            return string.IsNullOrEmpty(name) ? "form" : new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        }
        private static Form ActiveForm()
        {
            Form active = Form.ActiveForm;
            if (active != null && active.Visible) return active;
            Form last = null;
            foreach (Form form in Application.OpenForms) if (form.Visible) last = form;
            return last;
        }
        private static void Snapshot(Form form, string file)
        {
            if (form?.Window == null) throw new InvalidOperationException("No form to render");
            Avalonia.Controls.Window window = form.Window;
            double scaling = window.RenderScaling;
            PixelSize size = new PixelSize(Math.Max(1, (int)(window.Bounds.Width * scaling)),
                                           Math.Max(1, (int)(window.Bounds.Height * scaling)));
            using (RenderTargetBitmap bitmap = new RenderTargetBitmap(size, new Vector(96 * scaling, 96 * scaling)))
            {
                bitmap.Render(window);
                string directory = Path.GetDirectoryName(Path.GetFullPath(file));
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                bitmap.Save(file);
            }
            // The 3D views draw with OpenGL, which the bitmap does not show; their own pictures are written next to it
            SnapshotHook?.Invoke(form, file);
        }
        // Menu sweep: every menu item is clicked once, then the forms and message boxes it opened are closed
        public static bool Sweeping { get; private set; }
        private static List<(string Path, ToolStripMenuItem Item)> _sweepItems;
        private static int _sweepIndex;
        private static DispatcherTimer _sweepTimer;
        private static int _sweepPhase;
        private static void StartSweep(string skipArgument)
        {
            string[] skip = (skipArgument ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                               .Select(t => t.Trim()).ToArray();
            Form main = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
            MenuStrip menu = main?.MainMenuStrip ?? FindAll(main).OfType<MenuStrip>().FirstOrDefault();
            _sweepItems = new List<(string, ToolStripMenuItem)>();
            if (menu != null) CollectMenuItems(menu.Items, "", skip);
            _sweepIndex = 0;
            _sweepPhase = 0;
            Sweeping = true;
            _commands.Clear();          // the sweep runs alone; quit follows it
            _commands.Enqueue("quit");
            _timer.Stop();
            Console.Error.WriteLine("[sweep] " + _sweepItems.Count + " menu items");
            _sweepTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _sweepTimer.Tick += (s, e) => SweepStep();
            _sweepTimer.Start();
        }
        private static void CollectMenuItems(ToolStripItemCollection items, string path, string[] skip)
        {
            foreach (ToolStripItem item in items)
            {
                if (!(item is ToolStripMenuItem mi)) continue;
                string itemPath = path + "/" + Clean(mi.Text);
                if (skip.Any(t => itemPath.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                if (mi.DropDownItems.Count > 0) CollectMenuItems(mi.DropDownItems, itemPath, skip);
                else _sweepItems.Add((itemPath, mi));
            }
        }
        private static void SweepStep()
        {
            _sweepTimer.Stop();
            try
            {
                // Close what the previous item opened: message boxes first, then the forms other than the main form
                if (WinFormsCompat.Native.MessageBoxWindow.OpenButtons != null)
                {
                    var buttons = WinFormsCompat.Native.MessageBoxWindow.OpenButtons;
                    string answer = new[] { "Cancel", "No", "OK", "Yes" }.FirstOrDefault(b => buttons.ContainsKey(b)) ??
                                    buttons.Keys.First();
                    Console.Error.WriteLine("[sweep] message box answered " + answer);
                    buttons[answer]();
                    _sweepTimer.Start();
                    return;
                }
                Form main = Application.OpenForms[0];
                foreach (Form form in Application.OpenForms.Cast<Form>().ToArray())
                {
                    if (form == main || !form.Visible) continue;
                    Control cancel = Find(form, "Cancel") ?? Find(form, "Close") ?? Find(form, "btnCancel") ?? Find(form, "btnClose");
                    Console.Error.WriteLine("[sweep] closing " + form.Name);
                    if (cancel is Button b && b.Visible && b.Enabled) b.PerformClick();
                    else form.Close();
                    if (form.Visible) form.Hide();
                    _sweepTimer.Start();
                    return;
                }
                if (_sweepIndex >= _sweepItems.Count)
                {
                    Sweeping = false;
                    Console.Error.WriteLine("[sweep] done");
                    _timer.Interval = TimeSpan.FromMilliseconds(500);
                    _timer.Start();
                    return;
                }
                var entry = _sweepItems[_sweepIndex++];
                Console.Error.WriteLine("[sweep] " + entry.Path + (entry.Item.Enabled ? "" : " (disabled)"));
                _sweepTimer.Interval = TimeSpan.FromMilliseconds(900);
                _sweepTimer.Start();        // before the click: a modal dialog runs the next steps in its loop
                if (entry.Item.Enabled) entry.Item.PerformClick();
                return;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[sweep] exception: " + ex);
            }
            _sweepTimer.Start();
        }
        // Set by the application to add pictures of native views to a snapshot
        public static Action<Form, string> SnapshotHook;
        private static void ClickMenu(string path)
        {
            string[] names = path.Split('/');
            Form main = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
            MenuStrip menu = main?.MainMenuStrip ?? FindAll(main).OfType<MenuStrip>().FirstOrDefault();
            if (menu == null) throw new InvalidOperationException("No menu");
            ToolStripItemCollection items = menu.Items;
            ToolStripMenuItem item = null;
            foreach (string name in names)
            {
                item = items.OfType<ToolStripMenuItem>().FirstOrDefault(i => Clean(i.Text) == Clean(name));
                if (item == null) throw new InvalidOperationException("No menu item " + name);
                items = item.DropDownItems;
            }
            item.PerformClick();
        }
        private static string Clean(string text) { return (text ?? "").Replace("&", "").Replace("...", "").Trim(); }
        private static void Click(string nameOrText)
        {
            // The buttons of an open message box come first
            if (WinFormsCompat.Native.MessageBoxWindow.OpenButtons != null &&
                WinFormsCompat.Native.MessageBoxWindow.OpenButtons.TryGetValue(nameOrText, out Action clickButton))
            {
                clickButton();
                return;
            }
            Form form = ActiveForm();
            Control control = Find(form, nameOrText);
            if (control is Button b) { b.PerformClick(); return; }
            if (control is CheckBox c) { c.Checked = !c.Checked; return; }
            if (control is RadioButton r) { r.PerformClick(); return; }
            // Tool strip items
            foreach (ToolStrip strip in FindAll(form).OfType<ToolStrip>())
            {
                foreach (ToolStripItem item in strip.Items)
                {
                    if (item.Name == nameOrText || Clean(item.Text) == Clean(nameOrText) || item.ToolTipText == nameOrText)
                    {
                        item.PerformClick();
                        return;
                    }
                }
            }
            if (control != null) { control.Focus(); return; }
            var open = WinFormsCompat.Native.MessageBoxWindow.OpenButtons;
            throw new InvalidOperationException("No control " + nameOrText + (open != null ? "; message box buttons: " +
                                                string.Join(", ", open.Keys) : "; no message box"));
        }
        private static Control Find(Control root, string nameOrText)
        {
            return FindAll(root).FirstOrDefault(c => c.Name == nameOrText) ??
                   FindAll(root).FirstOrDefault(c => Clean(c.Text) == Clean(nameOrText) && c.Visible);
        }
        private static IEnumerable<Control> FindAll(Control root)
        {
            if (root == null) yield break;
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (Control c in FindAll(child)) yield return c;
            }
        }
    }
}
