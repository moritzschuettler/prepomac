// PrePoMax for macOS - the snapshots of the test automation include the pictures of the 3D views of a form.
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace vtkControl
{
    internal static class AutomationHook
    {
        [ModuleInitializer]
        internal static void Register()
        {
            Automation.SnapshotHook += (form, file) =>
            {
                int n = 0;
                foreach (vtkControl view in All(form).OfType<vtkControl>())
                {
                    System.Console.Error.WriteLine("[automation] 3D view " + view.Name + " visible " + view.Visible + " " + view.Size);
                    if (!view.Visible) continue;
                    view.RenderToPNG(System.IO.Path.ChangeExtension(file, null) + "_3d" + (n++ == 0 ? "" : n.ToString()) + ".png");
                }
            };
        }
        private static System.Collections.Generic.IEnumerable<Control> All(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (Control c in All(child)) yield return c;
            }
        }
    }
}
