// PrePoMax for macOS - WinForms compatibility types
// The shared PrePoMax sources reference these WinForms types; on macOS the user interface is Avalonia and the
// behaviour of message boxes and UI thread invokes is provided by handlers installed by the macOS application.

using System.Collections;
using System.ComponentModel;

namespace System.Windows.Forms
{
    public enum DialogResult { None = 0, OK = 1, Cancel = 2, Abort = 3, Retry = 4, Ignore = 5, Yes = 6, No = 7, TryAgain = 10, Continue = 11 }

    public enum MessageBoxButtons { OK = 0, OKCancel = 1, AbortRetryIgnore = 2, YesNoCancel = 3, YesNo = 4, RetryCancel = 5, CancelTryContinue = 6 }

    public enum MessageBoxIcon
    {
        None = 0, Hand = 16, Stop = 16, Error = 16, Question = 32, Exclamation = 48, Warning = 48, Asterisk = 64, Information = 64
    }

    public enum MessageBoxDefaultButton { Button1 = 0, Button2 = 256, Button3 = 512, Button4 = 768 }



    /// <summary>Hooks of the macOS application.</summary>
    public static partial class PlatformHooks
    {
        public static Func<string, string, MessageBoxButtons, MessageBoxIcon, MessageBoxDefaultButton, DialogResult> ShowMessageBox
        { get; set; } = (text, caption, buttons, icon, defaultButton) => WinFormsCompat.Native.MessageBoxWindow.Show(text, caption, buttons, icon, defaultButton);
    }

    public static class MessageBox
    {
        public static DialogResult Show(string text)
            => PlatformHooks.ShowMessageBox(text, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
        public static DialogResult Show(string text, string caption)
            => PlatformHooks.ShowMessageBox(text, caption, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
            => PlatformHooks.ShowMessageBox(text, caption, buttons, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
            => PlatformHooks.ShowMessageBox(text, caption, buttons, icon, MessageBoxDefaultButton.Button1);
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon,
                                        MessageBoxDefaultButton defaultButton)
            => PlatformHooks.ShowMessageBox(text, caption, buttons, icon, defaultButton);
        public static DialogResult Show(IWin32Window owner, string text)
            => Show(text);
        public static DialogResult Show(IWin32Window owner, string text, string caption)
            => Show(text, caption);
        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons)
            => Show(text, caption, buttons);
        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons,
                                        MessageBoxIcon icon)
            => Show(text, caption, buttons, icon);
        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons,
                                        MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
            => Show(text, caption, buttons, icon, defaultButton);
    }


}

namespace System.Windows.Forms.Design
{
    public interface IWindowsFormsEditorService
    {
        void CloseDropDown();
        void DropDownControl(Control control);
        DialogResult ShowDialog(Form dialog);
    }
}

namespace System.Drawing.Design
{
    using System.Windows.Forms;

    public enum UITypeEditorEditStyle { None = 1, Modal = 2, DropDown = 3 }

    public class UITypeEditor
    {
        public virtual bool IsDropDownResizable { get { return false; } }
        public virtual object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { return value; }
        public object EditValue(IServiceProvider provider, object value) { return EditValue(null, provider, value); }
        public virtual UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { return UITypeEditorEditStyle.None; }
        public UITypeEditorEditStyle GetEditStyle() { return GetEditStyle(null); }
        public virtual bool GetPaintValueSupported(ITypeDescriptorContext context) { return false; }
        public bool GetPaintValueSupported() { return GetPaintValueSupported(null); }
        public virtual void PaintValue(PaintValueEventArgs e) { }
    }

    public class PaintValueEventArgs : EventArgs
    {
        public ITypeDescriptorContext Context { get; }
        public object Value { get; }
        public Graphics Graphics { get; }
        public Rectangle Bounds { get; }
        public PaintValueEventArgs(ITypeDescriptorContext context, object value, Graphics graphics, Rectangle bounds)
        {
            Context = context;
            Value = value;
            Graphics = graphics;
            Bounds = bounds;
        }
    }

    public delegate void PropertyValueUIItemInvokeHandler(ITypeDescriptorContext context, PropertyDescriptor descriptor,
                                                          PropertyValueUIItem invokedItem);

    public delegate void PropertyValueUIHandler(ITypeDescriptorContext context, PropertyDescriptor propDesc,
                                                ArrayList valueUIItemList);

    public class PropertyValueUIItem
    {
        public virtual Image Image { get; }
        public virtual PropertyValueUIItemInvokeHandler InvokeHandler { get; }
        public virtual string ToolTip { get; }
        public PropertyValueUIItem(Image uiItemImage, PropertyValueUIItemInvokeHandler handler, string tooltip)
        {
            Image = uiItemImage;
            InvokeHandler = handler;
            ToolTip = tooltip;
        }
        public virtual void Reset() { }
    }

    public interface IPropertyValueUIService
    {
        event EventHandler PropertyUIValueItemsChanged;
        void AddPropertyValueUIHandler(PropertyValueUIHandler newHandler);
        PropertyValueUIItem[] GetPropertyUIValueItems(ITypeDescriptorContext context, PropertyDescriptor propDesc);
        void NotifyPropertyValueUIItemsChanged();
        void RemovePropertyValueUIHandler(PropertyValueUIHandler newHandler);
    }
}

namespace System.ComponentModel.Design
{
    public class ObjectSelectorEditor : System.Drawing.Design.UITypeEditor { }
}

// Namespaces of .NET Framework that the shared sources import without using
namespace System.Runtime.InteropServices.WindowsRuntime
{
    public static class WindowsRuntimeNamespaceMarker { }
}

namespace System.Runtime.Remoting.Contexts
{
    public static class RemotingContextsNamespaceMarker { }
}


namespace System.Net.Configuration
{
    public static class NetConfigurationNamespaceMarker { }
}

namespace System.Security.RightsManagement
{
    public static class RightsManagementNamespaceMarker { }
}

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
    public static class SystemRuntimeRemotingMetadataW3cXsd2001Marker { }
}

namespace System.Configuration
{
    public static class SystemConfigurationMarker { }
}

namespace System.Data.OleDb
{
    public static class SystemDataOleDbMarker { }
}

namespace System.Diagnostics.Eventing.Reader
{
    public static class SystemDiagnosticsEventingReaderMarker { }
}

namespace System.IO.Ports
{
    public static class SystemIOPortsMarker { }
}

namespace System.Security.Permissions
{
    public static class SystemSecurityPermissionsMarker { }
}

namespace System.Windows
{
    public static class SystemWindowsMarker { }
}

namespace System.Windows.Input
{
    public static class SystemWindowsInputMarker { }
}

namespace System.Drawing.Design
{
    using System.ComponentModel;

    /// <summary>The macOS property grid edits a value of a UITypeEditor through this hook.</summary>
    public static class UITypeEditorHost
    {
        // (editor, context, value) -> new value
        public static Func<UITypeEditor, ITypeDescriptorContext, object, object> Edit { get; set; } = (editor, context, value) => value;
    }
}
