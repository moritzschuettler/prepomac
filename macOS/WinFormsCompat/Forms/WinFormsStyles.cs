// PrePoMax for macOS - styles that give the Avalonia elements of the controls the look of WinForms on Windows 10.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.Styling;

namespace WinFormsCompat.Native
{
    internal static class WinFormsStyles
    {
        public static readonly IBrush ButtonFace = new SolidColorBrush(Color.FromRgb(0xE1, 0xE1, 0xE1));
        public static readonly IBrush ButtonBorder = new SolidColorBrush(Color.FromRgb(0xAD, 0xAD, 0xAD));
        public static readonly IBrush HoverFace = new SolidColorBrush(Color.FromRgb(0xE5, 0xF1, 0xFB));
        public static readonly IBrush HoverBorder = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD7));
        public static readonly IBrush PressedFace = new SolidColorBrush(Color.FromRgb(0xCC, 0xE4, 0xF7));
        public static readonly IBrush PressedBorder = new SolidColorBrush(Color.FromRgb(0x00, 0x54, 0x99));
        public static readonly IBrush DisabledFace = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
        public static readonly IBrush DisabledBorder = new SolidColorBrush(Color.FromRgb(0xBF, 0xBF, 0xBF));
        public static readonly IBrush DisabledText = new SolidColorBrush(Color.FromRgb(0x83, 0x83, 0x83));
        public static readonly IBrush TextBoxBorder = new SolidColorBrush(Color.FromRgb(0x7A, 0x7A, 0x7A));
        public static readonly IBrush FocusBorder = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD7));
        public static readonly IBrush Window = Brushes.White;
        public static readonly IBrush Highlight = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD7));
        public static readonly IBrush ControlBack = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));

        public static Styles Create()
        {
            Styles styles = new Styles();
            // Buttons
            styles.Add(Style<Button>(s => s.OfType<Button>().Class("winforms"),
                (Button.BackgroundProperty, ButtonFace), (Button.BorderBrushProperty, ButtonBorder),
                (Button.BorderThicknessProperty, new Thickness(1)), (Button.PaddingProperty, new Thickness(2, 1)),
                (Button.CornerRadiusProperty, new CornerRadius(0)), (Button.ForegroundProperty, Brushes.Black),
                (Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Stretch),
                (Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Stretch)));
            styles.Add(Style<Button>(s => s.OfType<Button>().Class("winforms").Class(":pointerover").Template().OfType<ContentPresenter>(),
                (ContentPresenter.BackgroundProperty, HoverFace), (ContentPresenter.BorderBrushProperty, HoverBorder)));
            styles.Add(Style<Button>(s => s.OfType<Button>().Class("winforms").Class(":pressed").Template().OfType<ContentPresenter>(),
                (ContentPresenter.BackgroundProperty, PressedFace), (ContentPresenter.BorderBrushProperty, PressedBorder)));
            styles.Add(Style<Button>(s => s.OfType<Button>().Class("winforms").Class(":disabled").Template().OfType<ContentPresenter>(),
                (ContentPresenter.BackgroundProperty, DisabledFace), (ContentPresenter.BorderBrushProperty, DisabledBorder)));
            styles.Add(Style<Button>(s => s.OfType<Button>().Class("winforms").Class(":disabled"),
                (Button.ForegroundProperty, DisabledText)));
            styles.Add(Style<Button>(s => s.OfType<Button>().Class("winforms").Class("default").Template().OfType<ContentPresenter>(),
                (ContentPresenter.BorderBrushProperty, HoverBorder)));
            styles.Add(Style<Button>(s => s.OfType<Button>().Class("winforms").Class(":focus-visible").Template().OfType<ContentPresenter>(),
                (ContentPresenter.BorderBrushProperty, HoverBorder)));
            // Text boxes
            styles.Add(Style<TextBox>(s => s.OfType<TextBox>().Class("winforms"),
                (TextBox.BackgroundProperty, Window), (TextBox.BorderBrushProperty, TextBoxBorder),
                (TextBox.BorderThicknessProperty, new Thickness(1)), (TextBox.PaddingProperty, new Thickness(2, 1)),
                (TextBox.CornerRadiusProperty, new CornerRadius(0)), (TextBox.MinHeightProperty, 0.0),
                (TextBox.ForegroundProperty, Brushes.Black), (TextBox.SelectionBrushProperty, Highlight),
                (TextBox.SelectionForegroundBrushProperty, Brushes.White), (TextBox.CaretBrushProperty, Brushes.Black)));
            styles.Add(Style<TextBox>(s => s.OfType<TextBox>().Class("winforms").Class(":pointerover").Template().OfType<Border>(),
                (Border.BorderBrushProperty, Brushes.Black)));
            styles.Add(Style<TextBox>(s => s.OfType<TextBox>().Class("winforms").Class(":focus").Template().OfType<Border>(),
                (Border.BorderBrushProperty, FocusBorder)));
            styles.Add(Style<TextBox>(s => s.OfType<TextBox>().Class("winforms").Class(":disabled").Template().OfType<Border>(),
                (Border.BackgroundProperty, ControlBack), (Border.BorderBrushProperty, DisabledBorder)));
            styles.Add(Style<TextBox>(s => s.OfType<TextBox>().Class("winforms").Class("readonly").Template().OfType<Border>(),
                (Border.BackgroundProperty, ControlBack)));
            styles.Add(Style<TextBox>(s => s.OfType<TextBox>().Class("winforms").Class("noborder"),
                (TextBox.BorderThicknessProperty, new Thickness(0)), (TextBox.PaddingProperty, new Thickness(0))));
            return styles;
        }

        private static Style Style<T>(Func<Selector, Selector> selector, params (AvaloniaProperty Property, object Value)[] setters)
        {
            Style style = new Style(selector);
            foreach (var setter in setters) style.Setters.Add(new Setter(setter.Property, setter.Value));
            return style;
        }
    }
}
