// PrePoMax for macOS - the Avalonia element of a WinForms control.

using System.Collections.Generic;
using System.Drawing;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using Color = System.Drawing.Color;
using Brushes = Avalonia.Media.Brushes;
using AC = Avalonia.Controls;

namespace System.Windows.Forms
{
    internal class ControlHost : AC.Panel
    {
        public readonly Control Owner;
        public AC.Control Content { get; private set; }
        private bool _paintMessagePending;
        // The control that receives the mouse events until the button is released (WinForms capture)
        public static Control CapturedControl;
        private IPointer _capturedPointer;
        private bool _contentDirty = true;
        private SKPicture _picture;

        // Draws the background and the OnPaint output under the content and the children
        private readonly PaintLayer _paintLayer;

        public ControlHost(Control owner)
        {
            Owner = owner;
            ClipToBounds = true;
            Focusable = false;
            Background = Brushes.Transparent;
            _paintLayer = new PaintLayer(this);
            Children.Add(_paintLayer);
            AddHandler(PointerPressedEvent, OnPointerPressedHandler, RoutingStrategies.Bubble, true);
            AddHandler(PointerReleasedEvent, OnPointerReleasedHandler, RoutingStrategies.Bubble, true);
            AddHandler(PointerMovedEvent, OnPointerMovedHandler, RoutingStrategies.Bubble, true);
            AddHandler(PointerWheelChangedEvent, OnPointerWheelHandler, RoutingStrategies.Bubble, true);
            AddHandler(PointerEnteredEvent, OnPointerEnteredHandler, RoutingStrategies.Direct, true);
            AddHandler(PointerExitedEvent, OnPointerExitedHandler, RoutingStrategies.Direct, true);
            AddHandler(GotFocusEvent, OnGotFocusHandler, RoutingStrategies.Bubble, true);
            AddHandler(DragDrop.DragEnterEvent, OnDragEnterHandler);
            AddHandler(DragDrop.DragOverEvent, OnDragOverHandler);
            AddHandler(DragDrop.DragLeaveEvent, OnDragLeaveHandler);
            AddHandler(DragDrop.DropEvent, OnDropHandler);
        }

        // Children
        public void SetContent(AC.Control content)
        {
            if (Content != null) Children.Remove(Content);
            Content = content;
            if (content != null) Children.Insert(1, content);
            InvalidateArrange();
        }
        // A visual drawn over the paint layer and under the children that does not take the mouse (an OpenGL view)
        public AC.Control NativeVisual { get; private set; }
        public void SetNativeVisual(AC.Control visual)
        {
            if (NativeVisual != null) Children.Remove(NativeVisual);
            NativeVisual = visual;
            if (visual != null)
            {
                visual.IsHitTestVisible = false;
                Children.Insert(1, visual);
            }
            InvalidateArrange();
        }
        public void AddChild(ControlHost child)
        {
            if (child.Parent is AC.Panel oldParent) oldParent.Children.Remove(child);
            Children.Add(child);
            UpdateZOrder();
        }
        public void RemoveChild(ControlHost child)
        {
            Children.Remove(child);
        }
        // Pictures drawn with CreateGraphics, shown over the control until it is invalidated
        private OverlayLayer _overlayLayer;
        public void AddOverlayPicture(SKPicture picture)
        {
            if (_overlayLayer == null)
            {
                _overlayLayer = new OverlayLayer(this);
                Children.Add(_overlayLayer);
            }
            else if (Children.IndexOf(_overlayLayer) != Children.Count - 1)
            {
                Children.Remove(_overlayLayer);
                Children.Add(_overlayLayer);
            }
            _overlayLayer.Pictures.Add(picture);
            _overlayLayer.InvalidateVisual();
        }
        public void ClearOverlayPictures()
        {
            if (_overlayLayer == null || _overlayLayer.Pictures.Count == 0) return;
            _overlayLayer.Pictures.Clear();
            _overlayLayer.InvalidateVisual();
        }
        private sealed class OverlayLayer : AC.Control
        {
            private readonly ControlHost _host;
            public readonly List<SKPicture> Pictures = new List<SKPicture>();
            public OverlayLayer(ControlHost host)
            {
                _host = host;
                IsHitTestVisible = false;
            }
            public override void Render(DrawingContext context)
            {
                Padding insets = _host.Owner.NonClientInsets;
                foreach (SKPicture picture in Pictures)
                    context.Custom(new PictureDrawOperation(new Rect(Bounds.Size), picture, insets.Left, insets.Top));
            }
        }
        // Elements drawn over the children (scroll bars) with their rectangle in the host
        private readonly List<(AC.Control Element, Func<Rect> Rect)> _overlays = new List<(AC.Control, Func<Rect>)>();
        public void AddOverlay(AC.Control element, Func<Rect> rect)
        {
            _overlays.Add((element, rect));
            Children.Add(element);
            InvalidateArrange();
        }
        public void RemoveOverlay(AC.Control element)
        {
            _overlays.RemoveAll(o => o.Element == element);
            Children.Remove(element);
        }
        // Avalonia draws the later children on top; WinForms has the front control first
        public void UpdateZOrder()
        {
            if (!Owner.HasChildren) return;
            var hosts = new List<AC.Control>();
            var controls = Owner.Controls;
            for (int i = controls.Count - 1; i >= 0; i--) hosts.Add(controls[i].Host);
            int start = Content != null ? 2 : 1;
            bool same = Children.Count == start + hosts.Count + _overlays.Count;
            for (int i = 0; same && i < hosts.Count; i++) same = Children[start + i] == hosts[i];
            if (same) return;
            foreach (var h in hosts) Children.Remove(h);
            foreach (var o in _overlays) Children.Remove(o.Element);
            foreach (var h in hosts) Children.Add(h);
            foreach (var o in _overlays) Children.Add(o.Element);
        }

        // Layout
        protected override Avalonia.Size MeasureOverride(Avalonia.Size availableSize)
        {
            Rectangle content = Owner.ContentBounds;
            _paintLayer.Measure(new Avalonia.Size(Owner.Width, Owner.Height));
            Content?.Measure(new Avalonia.Size(Math.Max(0, content.Width), Math.Max(0, content.Height)));
            NativeVisual?.Measure(new Avalonia.Size(Math.Max(0, content.Width), Math.Max(0, content.Height)));
            if (Owner.HasChildren)
            {
                foreach (Control child in Owner.Controls.Items)
                {
                    if (!child.VisibleSelf) continue;
                    child.Host.Measure(new Avalonia.Size(child.Width, child.Height));
                }
            }
            return new Avalonia.Size(Owner.Width, Owner.Height);
        }
        protected override Avalonia.Size ArrangeOverride(Avalonia.Size finalSize)
        {
            Owner.OnHostArranged(finalSize);
            Rectangle content = Owner.ContentBounds;
            _paintLayer.Arrange(new Rect(finalSize));
            Content?.Arrange(new Rect(content.X, content.Y, Math.Max(0, content.Width), Math.Max(0, content.Height)));
            NativeVisual?.Arrange(new Rect(content.X, content.Y, Math.Max(0, content.Width), Math.Max(0, content.Height)));
            if (Owner.HasChildren)
            {
                System.Drawing.Point origin = Owner.ChildOrigin;
                foreach (Control child in Owner.Controls.Items)
                {
                    Rectangle b = child.Bounds;
                    child.Host.Arrange(new Rect(origin.X + b.X, origin.Y + b.Y, Math.Max(0, b.Width), Math.Max(0, b.Height)));
                }
            }
            foreach (var o in _overlays)
            {
                Rect r = o.Rect();
                o.Element.Measure(r.Size);
                o.Element.Arrange(r);
            }
            if (_overlayLayer != null)
            {
                _overlayLayer.Measure(finalSize);
                _overlayLayer.Arrange(new Rect(finalSize));
            }
            return finalSize;
        }

        // Painting
        public void InvalidateContent() { _contentDirty = true; }
        public void UpdateBackground() { _paintLayer.InvalidateVisual(); }
        public new void InvalidateVisual()
        {
            base.InvalidateVisual();
            _paintLayer?.InvalidateVisual();
        }
        private sealed class PaintLayer : AC.Control
        {
            private readonly ControlHost _host;
            public PaintLayer(ControlHost host)
            {
                _host = host;
                IsHitTestVisible = false;
            }
            public override void Render(DrawingContext context) { _host.RenderLayer(context); }
        }
        internal void RenderLayer(DrawingContext context)
        {
            // Controls that override WndProc see WM_PAINT when they are drawn
            if (Owner.FiltersMouseMessages && !_paintMessagePending)
            {
                _paintMessagePending = true;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _paintMessagePending = false;
                    try { if (!Owner.IsDisposed) Owner.FilterMessage(0x000F, IntPtr.Zero, IntPtr.Zero); }
                    catch { }
                }, Avalonia.Threading.DispatcherPriority.Background);
            }
            Rect rect = new Rect(Bounds.Size);
            Color back = Owner.PaintedBackColor;
            if (!back.IsEmpty && back.A > 0)
                context.FillRectangle(new SolidColorBrush(Avalonia.Media.Color.FromArgb(back.A, back.R, back.G, back.B)), rect);
            if (Owner.PaintsItself)
            {
                if (_picture == null || _contentDirty || true)
                {
                    // The previous picture is not disposed here: the render thread may still draw it from an earlier
                    // frame, and disposing it under it crashes Skia. It is released by its finalizer once unreferenced
                    _picture = Record((float)rect.Width, (float)rect.Height);
                    _contentDirty = false;
                }
                if (_picture != null) context.Custom(new PictureDrawOperation(rect, _picture));
            }
        }
        private SKPicture Record(float width, float height)
        {
            if (width <= 0 || height <= 0) return null;
            using (SKPictureRecorder recorder = new SKPictureRecorder())
            {
                SKCanvas canvas = recorder.BeginRecording(new SKRect(0, 0, width, height));
                Owner.PaintNonClient(canvas, width, height);
                Padding insets = Owner.NonClientInsets;
                canvas.Save();
                canvas.ClipRect(new SKRect(insets.Left, insets.Top, width - insets.Right, height - insets.Bottom));
                canvas.Translate(insets.Left, insets.Top);
                System.Drawing.Size client = Owner.ClientSize;
                using (Graphics g = new Graphics(canvas, new SizeF(client.Width, client.Height)))
                {
                    Owner.PaintInternal(g, new Rectangle(0, 0, client.Width, client.Height));
                }
                canvas.Restore();
                return recorder.EndRecording();
            }
        }

        private sealed class PictureDrawOperation : ICustomDrawOperation
        {
            private readonly SKPicture _picture;
            private readonly float _dx, _dy;
            public Rect Bounds { get; }
            public PictureDrawOperation(Rect bounds, SKPicture picture, float dx = 0, float dy = 0)
            {
                Bounds = bounds;
                _picture = picture;
                _dx = dx;
                _dy = dy;
            }
            public bool HitTest(Avalonia.Point p) { return Bounds.Contains(p); }
            public bool Equals(ICustomDrawOperation other) { return other is PictureDrawOperation o && o._picture == _picture; }
            public void Render(ImmediateDrawingContext context)
            {
                var feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (feature == null) return;
                using (var lease = feature.Lease())
                {
                    if (_dx != 0 || _dy != 0)
                    {
                        lease.SkCanvas.Save();
                        lease.SkCanvas.Translate(_dx, _dy);
                        lease.SkCanvas.DrawPicture(_picture);
                        lease.SkCanvas.Restore();
                    }
                    else lease.SkCanvas.DrawPicture(_picture);
                }
            }
            public void Dispose() { }
        }

        // Mouse
        // The control of a routed event is the control of the nearest host of the source
        public static Control OwnerOf(object source)
        {
            Visual v = source as Visual;
            while (v != null)
            {
                if (v is ControlHost host) return host.Owner;
                v = v.GetVisualParentSafe();
            }
            return null;
        }
        private bool IsMine(RoutedEventArgs e)
        {
            Control target = CapturedControl ?? OwnerOf(e.Source);
            return target == Owner;
        }
        private System.Drawing.Point ClientPoint(PointerEventArgs e)
        {
            Avalonia.Point p = e.GetPosition(this);
            Padding insets = Owner.NonClientInsets;
            return new System.Drawing.Point((int)Math.Floor(p.X) - insets.Left, (int)Math.Floor(p.Y) - insets.Top);
        }
        private void OnPointerPressedHandler(object sender, PointerPressedEventArgs e)
        {
            Input.Update(e);
            if (CapturedControl != null && CapturedControl != Owner) return;
            if (OwnerOf(e.Source) != Owner) return;
            PointerPoint point = e.GetCurrentPoint(this);
            MouseButtons button = Input.ButtonOf(point.Properties.PointerUpdateKind);
            System.Drawing.Point p = ClientPoint(e);
            if (Content == null || Owner.CapturesMouseOverContent)
            {
                CapturedControl = Owner;
                _capturedPointer = e.Pointer;
                e.Pointer.Capture(this);
            }
            if (Owner.CanSelectOnClick && Content == null)
            {
                Focusable = true;
                Focus();
            }
            Toolkit.Dispatch(() => Owner.HandleMouseDown(new MouseEventArgs(button, e.ClickCount, p.X, p.Y, 0)));
        }
        private void OnPointerReleasedHandler(object sender, PointerReleasedEventArgs e)
        {
            Input.Update(e);
            if (!IsMine(e)) return;
            MouseButtons button = Input.ButtonOf(e.InitialPressMouseButton);
            System.Drawing.Point p = ClientPoint(e);
            bool inside = new Rect(Bounds.Size).Contains(e.GetPosition(this));
            if (CapturedControl == Owner)
            {
                CapturedControl = null;
                _capturedPointer?.Capture(null);
                _capturedPointer = null;
            }
            Toolkit.Dispatch(() => Owner.HandleMouseUp(new MouseEventArgs(button, 1, p.X, p.Y, 0), inside));
            if (button == MouseButtons.Right && Content != null && Owner.ContextMenuStrip != null && inside && !Owner.HandlesContextMenu)
                e.Handled = true;
        }
        private void OnPointerMovedHandler(object sender, PointerEventArgs e)
        {
            Input.Update(e);
            if (!IsMine(e)) return;
            if (!Owner.MouseIsInside && CapturedControl == null) Owner.HandleMouseEnter();
            System.Drawing.Point p = ClientPoint(e);
            Toolkit.Dispatch(() => Owner.HandleMouseMove(new MouseEventArgs(Input.MouseButtons, 0, p.X, p.Y, 0)));
        }
        private void OnPointerWheelHandler(object sender, PointerWheelEventArgs e)
        {
            Input.Update(e);
            if (!IsMine(e)) return;
            System.Drawing.Point p = ClientPoint(e);
            int delta = Input.WheelDelta(e.Delta.Y);
            if (delta == 0) return;
            HandledMouseEventArgs args = new HandledMouseEventArgs(MouseButtons.None, 0, p.X, p.Y, delta);
            Toolkit.Dispatch(() => Owner.HandleMouseWheel(args));
            if (args.Handled) e.Handled = true;
        }
        private void OnPointerEnteredHandler(object sender, PointerEventArgs e)
        {
            Toolkit.Dispatch(() => Owner.HandleMouseEnter());
        }
        private void OnPointerExitedHandler(object sender, PointerEventArgs e)
        {
            if (CapturedControl == Owner) return;
            Toolkit.Dispatch(() => Owner.HandleMouseLeave());
        }
        private void OnGotFocusHandler(object sender, GotFocusEventArgs e)
        {
            if (OwnerOf(e.Source) != Owner) return;
            Toolkit.FocusChanged(Owner);
        }
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
        }

        // Drag and drop
        private DragEventArgs ToArgs(Avalonia.Input.DragEventArgs e)
        {
            Avalonia.Point p = e.GetPosition(this);
            System.Drawing.Point screen = Owner.PointToScreen(new System.Drawing.Point((int)p.X, (int)p.Y));
            return new DragEventArgs(new AvaloniaDataObject(e.Data), 0, screen.X, screen.Y, DragDropEffects.All,
                                     Toolkit.ToEffects(e.DragEffects));
        }
        private void OnDragEnterHandler(object sender, Avalonia.Input.DragEventArgs e)
        {
            if (OwnerOf(e.Source) != Owner || !Owner.AllowDrop) return;
            DragEventArgs args = ToArgs(e);
            Owner.RaiseDragEnter(args);
            e.DragEffects = Toolkit.FromEffects(args.Effect);
            e.Handled = true;
        }
        private void OnDragOverHandler(object sender, Avalonia.Input.DragEventArgs e)
        {
            if (OwnerOf(e.Source) != Owner || !Owner.AllowDrop) return;
            DragEventArgs args = ToArgs(e);
            args.Effect = Toolkit.ToEffects(e.DragEffects);
            Owner.RaiseDragOver(args);
            e.DragEffects = Toolkit.FromEffects(args.Effect);
            e.Handled = true;
        }
        private void OnDragLeaveHandler(object sender, RoutedEventArgs e)
        {
            if (OwnerOf(e.Source) != Owner || !Owner.AllowDrop) return;
            Owner.RaiseDragLeave(EventArgs.Empty);
        }
        private void OnDropHandler(object sender, Avalonia.Input.DragEventArgs e)
        {
            if (OwnerOf(e.Source) != Owner || !Owner.AllowDrop) return;
            DragEventArgs args = ToArgs(e);
            Toolkit.Dispatch(() => Owner.RaiseDragDrop(args));
            e.Handled = true;
        }
    }

    internal static class VisualExtensions
    {
        public static Visual GetVisualParentSafe(this Visual visual)
        {
            return Avalonia.VisualTree.VisualExtensions.GetVisualParent(visual);
        }
    }
}
