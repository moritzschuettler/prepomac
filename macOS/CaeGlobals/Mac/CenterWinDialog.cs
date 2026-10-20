// PrePoMax for macOS: replaces CaeGlobals/CenterWinDialog.cs (a Win32 hook that centres message boxes on the owner).
// macOS sheets and Avalonia dialogs are centred by the platform, so there is nothing to do.
using System;
using System.Windows.Forms;

class CenterWinDialog : IDisposable
{
    public CenterWinDialog(Form owner) { }
    public void Dispose() { }
}
