// PrePoMax for macOS - the system colors of Windows 10.
// On macOS .NET returns the colors of Windows XP for SystemColors (Control is beige). The table of System.Drawing.Primitives is
// changed once to the default colors of Windows 10, so that the forms look like on Windows.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    public static class SystemColorsPatch
    {
        private static bool _applied;

        [ModuleInitializer]
        internal static void Initialize() { Apply(); }

        public static unsafe void Apply()
        {
            if (_applied || !OperatingSystem.IsMacOS()) return;
            _applied = true;
            try
            {
                ReadOnlySpan<uint> table = ColorValueTable(null);
                if (table.Length <= (int)KnownColor.MenuHighlight) return;
                uint* p = (uint*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(table));
                const long page = 16384;
                long start = (long)p & ~(page - 1);
                long end = (long)(p + table.Length);
                // VM_PROT_READ | VM_PROT_WRITE | VM_PROT_COPY: a private writable copy of the pages
                if (mach_vm_protect(mach_task_self(), (ulong)start, (ulong)(end - start), 0, 1 | 2 | 0x10) != 0) return;
                Set(p, KnownColor.ActiveBorder, 0xFFB4B4B4);
                Set(p, KnownColor.ActiveCaption, 0xFF99B4D1);
                Set(p, KnownColor.ActiveCaptionText, 0xFF000000);
                Set(p, KnownColor.AppWorkspace, 0xFFABABAB);
                Set(p, KnownColor.Control, 0xFFF0F0F0);
                Set(p, KnownColor.ControlDark, 0xFFA0A0A0);
                Set(p, KnownColor.ControlDarkDark, 0xFF696969);
                Set(p, KnownColor.ControlLight, 0xFFE3E3E3);
                Set(p, KnownColor.ControlLightLight, 0xFFFFFFFF);
                Set(p, KnownColor.ControlText, 0xFF000000);
                Set(p, KnownColor.Desktop, 0xFF000000);
                Set(p, KnownColor.GrayText, 0xFF6D6D6D);
                Set(p, KnownColor.Highlight, 0xFF0078D7);
                Set(p, KnownColor.HighlightText, 0xFFFFFFFF);
                Set(p, KnownColor.HotTrack, 0xFF0066CC);
                Set(p, KnownColor.InactiveBorder, 0xFFF4F7FC);
                Set(p, KnownColor.InactiveCaption, 0xFFBFCDDB);
                Set(p, KnownColor.InactiveCaptionText, 0xFF000000);
                Set(p, KnownColor.Info, 0xFFFFFFE1);
                Set(p, KnownColor.InfoText, 0xFF000000);
                Set(p, KnownColor.Menu, 0xFFF0F0F0);
                Set(p, KnownColor.MenuText, 0xFF000000);
                Set(p, KnownColor.ScrollBar, 0xFFC8C8C8);
                Set(p, KnownColor.Window, 0xFFFFFFFF);
                Set(p, KnownColor.WindowFrame, 0xFF646464);
                Set(p, KnownColor.WindowText, 0xFF000000);
                Set(p, KnownColor.ButtonFace, 0xFFF0F0F0);
                Set(p, KnownColor.ButtonHighlight, 0xFFFFFFFF);
                Set(p, KnownColor.ButtonShadow, 0xFFA0A0A0);
                Set(p, KnownColor.GradientActiveCaption, 0xFFB9D1EA);
                Set(p, KnownColor.GradientInactiveCaption, 0xFFD7E4F2);
                Set(p, KnownColor.MenuBar, 0xFFF0F0F0);
                Set(p, KnownColor.MenuHighlight, 0xFF3399FF);
                // The pages also hold code: they become readable and executable again
                mach_vm_protect(mach_task_self(), (ulong)start, (ulong)(end - start), 0, 1 | 4);
            }
            catch
            {
            }
        }
        private static unsafe void Set(uint* table, KnownColor color, uint argb) { table[(int)color] = argb; }

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_ColorValueTable")]
        private static extern ReadOnlySpan<uint> ColorValueTable([UnsafeAccessorType("System.Drawing.KnownColorTable, System.Drawing.Primitives")] object table);
        [DllImport("libc")]
        private static extern int mach_vm_protect(uint task, ulong address, ulong size, int setMaximum, int newProtection);
        [DllImport("libc")]
        private static extern uint mach_task_self();
    }
}
