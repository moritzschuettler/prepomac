// PrePoMax for macOS - the message box of WinForms as a modal Avalonia window (text with an icon, buttons on a gray band).

using System.Collections.Generic;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using AC = Avalonia.Controls;
using System.Windows.Forms;
using Button = Avalonia.Controls.Button;
using Clipboard = System.Windows.Forms.Clipboard;
using Application = System.Windows.Forms.Application;
using Toolkit = System.Windows.Forms.Toolkit;

namespace WinFormsCompat.Native
{
    internal static class MessageBoxWindow
    {
        // The buttons of the open message box by their texts, for the test automation
        internal static Dictionary<string, Action> OpenButtons;

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon,
                                        MessageBoxDefaultButton defaultButton)
        {
            if (!Dispatcher.UIThread.CheckAccess())
                return Dispatcher.UIThread.Invoke(() => Show(text, caption, buttons, icon, defaultButton));
            Toolkit.EnsureInitialized();
            DialogResult result = DialogResult.None;
            List<(string Text, DialogResult Result)> list = Buttons(buttons);
            Console.Error.WriteLine("[messagebox] " + caption + ": " + text);
            Dictionary<string, Action> previousButtons = OpenButtons;
            Dictionary<string, Action> openButtons = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
            OpenButtons = openButtons;
            Window window = new Window
            {
                Title = caption ?? string.Empty,
                SizeToContent = SizeToContent.WidthAndHeight,
                CanResize = false,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                MinWidth = 180,
                MaxWidth = 700,
                Background = Brushes.White,
            };
            Grid grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
            StackPanel top = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Margin = new Thickness(24, 22, 24, 24), Spacing = 12 };
            AC.Control iconElement = IconElement(icon);
            if (iconElement != null) top.Children.Add(iconElement);
            SelectableTextBlock message = new SelectableTextBlock
            {
                Text = text ?? string.Empty,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 560,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
            };
            top.Children.Add(message);
            grid.Children.Add(top);
            Border band = new Border { Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)), Padding = new Thickness(10, 10, 10, 10) };
            StackPanel buttonPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 8 };
            int defaultIndex = defaultButton == MessageBoxDefaultButton.Button2 ? 1 : defaultButton == MessageBoxDefaultButton.Button3 ? 2 : 0;
            Button focusButton = null;
            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                Button button = new Button
                {
                    Content = entry.Text,
                    MinWidth = 75,
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    IsDefault = i == defaultIndex,
                    IsCancel = entry.Result == DialogResult.Cancel || (list.Count == 1),
                };
                button.Classes.Add("winforms");
                button.Click += (s, e) =>
                {
                    result = entry.Result;
                    window.Close();
                };
                openButtons[entry.Text.Replace("&", "")] = () => { result = entry.Result; window.Close(); };
                buttonPanel.Children.Add(button);
                if (i == defaultIndex) focusButton = button;
            }
            band.Child = buttonPanel;
            Grid.SetRow(band, 1);
            grid.Children.Add(band);
            window.Content = grid;
            window.KeyDown += (s, e) =>
            {
                if (e.Key == Key.C && (e.KeyModifiers & (KeyModifiers.Meta | KeyModifiers.Control)) != 0)
                {
                    Clipboard.SetText((caption ?? "") + Environment.NewLine + (text ?? ""));
                    e.Handled = true;
                }
            };
            bool closed = false;
            window.Closed += (s, e) => closed = true;
            window.Opened += (s, e) => focusButton?.Focus();
            Window owner = ActiveWindow();
            if (owner != null) _ = window.ShowDialog(owner);
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                window.Show();
            }
            try { Application.RunModal(() => closed); }
            finally { OpenButtons = previousButtons; }
            if (result == DialogResult.None)
                result = list.Exists(b => b.Result == DialogResult.Cancel) ? DialogResult.Cancel :
                         list.Count == 1 ? list[0].Result : DialogResult.None;
            return result;
        }

        private static Window ActiveWindow()
        {
            Form active = Form.ActiveForm;
            if (active?.Window != null && active.Window.IsVisible) return active.Window;
            for (int i = Application.OpenForms.Count - 1; i >= 0; i--)
            {
                Form f = Application.OpenForms[i];
                if (f.Window != null && f.Window.IsVisible) return f.Window;
            }
            return null;
        }

        private static List<(string, DialogResult)> Buttons(MessageBoxButtons buttons)
        {
            switch (buttons)
            {
                case MessageBoxButtons.OKCancel: return new List<(string, DialogResult)> { ("OK", DialogResult.OK), ("Cancel", DialogResult.Cancel) };
                case MessageBoxButtons.AbortRetryIgnore:
                    return new List<(string, DialogResult)> { ("Abort", DialogResult.Abort), ("Retry", DialogResult.Retry), ("Ignore", DialogResult.Ignore) };
                case MessageBoxButtons.YesNoCancel:
                    return new List<(string, DialogResult)> { ("Yes", DialogResult.Yes), ("No", DialogResult.No), ("Cancel", DialogResult.Cancel) };
                case MessageBoxButtons.YesNo: return new List<(string, DialogResult)> { ("Yes", DialogResult.Yes), ("No", DialogResult.No) };
                case MessageBoxButtons.RetryCancel: return new List<(string, DialogResult)> { ("Retry", DialogResult.Retry), ("Cancel", DialogResult.Cancel) };
                case MessageBoxButtons.CancelTryContinue:
                    return new List<(string, DialogResult)> { ("Cancel", DialogResult.Cancel), ("Try Again", DialogResult.TryAgain), ("Continue", DialogResult.Continue) };
                default: return new List<(string, DialogResult)> { ("OK", DialogResult.OK) };
            }
        }

        // The icons of the Windows message box drawn as vector shapes
        private static AC.Control IconElement(MessageBoxIcon icon)
        {
            Color fill;
            string glyph;
            switch (icon)
            {
                case MessageBoxIcon.Error: fill = Color.FromRgb(0xE8, 0x11, 0x23); glyph = "✕"; break;
                case MessageBoxIcon.Warning: fill = Color.FromRgb(0xFF, 0xB9, 0x00); glyph = "!"; break;
                case MessageBoxIcon.Information: fill = Color.FromRgb(0x00, 0x78, 0xD7); glyph = "i"; break;
                case MessageBoxIcon.Question: fill = Color.FromRgb(0x00, 0x78, 0xD7); glyph = "?"; break;
                default: return null;
            }
            Grid grid = new Grid { Width = 32, Height = 32, VerticalAlignment = VerticalAlignment.Top };
            if (icon == MessageBoxIcon.Warning)
            {
                grid.Children.Add(new AC.Shapes.Path
                {
                    Data = Geometry.Parse("M16,2 L31,29 L1,29 Z"),
                    Fill = new SolidColorBrush(fill),
                });
            }
            else grid.Children.Add(new AC.Shapes.Ellipse { Fill = new SolidColorBrush(fill) });
            grid.Children.Add(new TextBlock
            {
                Text = glyph,
                Foreground = icon == MessageBoxIcon.Warning ? Brushes.Black : Brushes.White,
                FontWeight = FontWeight.Bold,
                FontSize = 18,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = icon == MessageBoxIcon.Warning ? new Thickness(0, 6, 0, 0) : new Thickness(0),
            });
            return grid;
        }
    }
}
