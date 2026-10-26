// PrePoMax for macOS - the Avalonia application of the WinForms compatibility layer.

using System.Linq;
using Avalonia;
using Avalonia.Headless;
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
            bool batch = IsBatchRun();
            // A session without a window runs on the headless platform: it needs neither the window server nor the
            // display link, which cannot start while the display sleeps or the screen is locked. Its pictures are
            // rendered by an OpenGL context of their own
            if (batch && Environment.GetEnvironmentVariable("PPM_NATIVE_BATCH") != "1")
            {
                AppBuilder.Configure<CompatApplication>()
                          .UseSkia()
                          .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
                          .SetupWithoutStarting();
                return;
            }
            // The display link of macOS cannot start while the display sleeps or the screen is locked - which a session
            // started then, or a window opened later, then fails with - so a timer of 60 Hz paces the rendering; the
            // display link is used only on request (PPM_DISPLAY_LINK=1) and while the display is awake
            bool timerRendering = batch || IsMainDisplayAsleep() ||
                                  Environment.GetEnvironmentVariable("PPM_DISPLAY_LINK") != "1";
            AppBuilder builder = AppBuilder.Configure<CompatApplication>()
                      .UsePlatformDetect()
                      .With(new MacOSPlatformOptions { ShowInDock = !batch, DisableDefaultApplicationMenuItems = false });
            // The user interface font, where the system font is not used (see System.Drawing.Font.UiFamily)
            if (System.Drawing.Font.UiFamily != null)
                builder = builder.With(new Avalonia.Media.FontManagerOptions { DefaultFamilyName = System.Drawing.Font.UiFamily });
            builder
                      .AfterSetup(setup =>
                      {
                          if (timerRendering)
                          {
                              // Avalonia's own timer render loop; its constructor is internal
                              object timer = Activator.CreateInstance(typeof(Avalonia.Rendering.DefaultRenderTimer),
                                  Reflection.BindingFlags.Instance | Reflection.BindingFlags.Public | Reflection.BindingFlags.NonPublic,
                                  null, new object[] { 60 }, null);
                              ReplaceInnerRenderTimer((Avalonia.Rendering.IRenderTimer)timer);
                          }
                      })
                      .SetupWithoutStarting();
        }
        // Avalonia.Native wraps the display link of macOS in a ThreadProxyRenderTimer, which the compositor already holds
        // after the setup; the display link is only started with the first frame, so the inner timer of the proxy is
        // replaced by a timer of a thread before that
        private static void ReplaceInnerRenderTimer(Avalonia.Rendering.IRenderTimer timer)
        {
            try
            {
                object locator = typeof(AvaloniaLocator).GetProperty("Current",
                    Reflection.BindingFlags.Static | Reflection.BindingFlags.Public | Reflection.BindingFlags.NonPublic).GetValue(null);
                object current = locator.GetType().GetMethod("GetService", new[] { typeof(Type) })
                                        .Invoke(locator, new object[] { typeof(Avalonia.Rendering.IRenderTimer) });
                Reflection.FieldInfo inner = current?.GetType().GetField("_inner",
                    Reflection.BindingFlags.Instance | Reflection.BindingFlags.NonPublic);
                if (inner == null) throw new InvalidOperationException("The render timer has no inner timer.");
                inner.SetValue(current, timer);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("The render timer could not be replaced: " + ex.Message);
            }
        }
        // PrePoMax runs without its window with the option -g No (--showGui No)
        private static bool IsBatchRun()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if ((args[i] == "-g" || args[i] == "--showGui") && args[i + 1].Equals("No", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (args[i].StartsWith("--showGui=", StringComparison.Ordinal) &&
                    args[i].EndsWith("No", StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
        [Runtime.InteropServices.DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern uint CGMainDisplayID();
        [Runtime.InteropServices.DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern int CGDisplayIsAsleep(uint display);
        private static bool IsMainDisplayAsleep()
        {
            try { return CGDisplayIsAsleep(CGMainDisplayID()) != 0; }
            catch { return false; }
        }
    }
}
