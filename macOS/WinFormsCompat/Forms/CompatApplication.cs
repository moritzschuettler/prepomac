// PrePoMax for macOS - the Avalonia application of the WinForms compatibility layer.

using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Simple;

namespace System.Windows.Forms
{
    /// <summary>The Avalonia application: the simple theme with the WinForms look on top.</summary>
    public class CompatApplication : Avalonia.Application
    {
        public override void Initialize()
        {
            RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
            Styles.Add(new SimpleTheme());
            Styles.Add(WinFormsCompat.Native.WinFormsStyles.Create());
        }

        // The default setup: the platform of the operating system
        public static void SetupDesktop(string applicationName)
        {
            Avalonia.Application.Current?.GetType();
            AppBuilder.Configure<CompatApplication>()
                      .UsePlatformDetect()
                      .With(new MacOSPlatformOptions { ShowInDock = true, DisableDefaultApplicationMenuItems = false })
                      .SetupWithoutStarting();
        }
    }
}
