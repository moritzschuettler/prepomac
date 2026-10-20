// PrePoMax for macOS - the auto complete menu of FastColoredTextBox: a list of completions under the caret of the editor.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace FastColoredTextBoxNS
{
    public enum CompareResult { Hidden, Visible, VisibleAndSelected }

    public class AutocompleteItem
    {
        public string Text;
        public int ImageIndex = -1;
        public object Tag;
        private string _toolTipTitle, _toolTipText, _menuText;
        public AutocompleteMenu Parent { get; internal set; }
        public AutocompleteItem() { }
        public AutocompleteItem(string text) { Text = text; }
        public AutocompleteItem(string text, int imageIndex) : this(text) { ImageIndex = imageIndex; }
        public AutocompleteItem(string text, int imageIndex, string menuText) : this(text, imageIndex) { _menuText = menuText; }
        public AutocompleteItem(string text, int imageIndex, string menuText, string toolTipTitle, string toolTipText) : this(text, imageIndex, menuText)
        {
            _toolTipTitle = toolTipTitle;
            _toolTipText = toolTipText;
        }
        public virtual string ToolTipTitle { get { return _toolTipTitle; } set { _toolTipTitle = value; } }
        public virtual string ToolTipText { get { return _toolTipText; } set { _toolTipText = value; } }
        public virtual string MenuText { get { return _menuText; } set { _menuText = value; } }
        public virtual Color ForeColor { get { return Color.Transparent; } set { } }
        public virtual Color BackColor { get { return Color.Transparent; } set { } }
        public virtual string GetTextForReplace() { return Text; }
        public virtual CompareResult Compare(string fragmentText)
        {
            if (Text.StartsWith(fragmentText, StringComparison.InvariantCultureIgnoreCase) && Text != fragmentText) return CompareResult.VisibleAndSelected;
            return CompareResult.Hidden;
        }
        public override string ToString() { return _menuText ?? Text; }
        public virtual void OnSelected(AutocompleteMenu popupMenu, SelectedEventArgs e) { }
    }

    public class SnippetAutocompleteItem : AutocompleteItem
    {
        public SnippetAutocompleteItem(string snippet) { Text = snippet.Replace("\r", ""); ToolTipTitle = "Code snippet:"; ToolTipText = Text; }
        public override string ToString() { return MenuText ?? Text.Replace("\n", " ").Replace("^", ""); }
        public override string GetTextForReplace() { return Text.Replace("^", ""); }
        public override CompareResult Compare(string fragmentText)
        {
            return Text.StartsWith(fragmentText, StringComparison.InvariantCultureIgnoreCase) ? CompareResult.VisibleAndSelected : CompareResult.Hidden;
        }
    }

    public class MethodAutocompleteItem : AutocompleteItem
    {
        public MethodAutocompleteItem(string text) : base(text) { }
        public override CompareResult Compare(string fragmentText)
        {
            int i = fragmentText.LastIndexOf('.');
            if (i < 0) return CompareResult.Hidden;
            string lastPart = fragmentText.Substring(i + 1);
            if (lastPart.Length == 0) return CompareResult.Visible;
            return Text.StartsWith(lastPart, StringComparison.InvariantCultureIgnoreCase) ? CompareResult.VisibleAndSelected : CompareResult.Hidden;
        }
    }

    public class SelectingEventArgs : EventArgs
    {
        public AutocompleteItem Item { get; internal set; }
        public bool Cancel { get; set; }
        public int SelectedIndex { get; set; }
        public bool Handled { get; set; }
    }

    public class SelectedEventArgs : EventArgs
    {
        public AutocompleteItem Item { get; internal set; }
        public FastColoredTextBox Tb { get; set; }
    }

    // The list of the menu: drawn items with the focused item highlighted
    public class AutocompleteListView : UserControl
    {
        private readonly AutocompleteMenu _menu;
        private readonly FastColoredTextBox _tb;
        internal List<AutocompleteItem> VisibleItems = new List<AutocompleteItem>();
        private IEnumerable<AutocompleteItem> _sourceItems = new List<AutocompleteItem>();
        private int _focussedItemIndex;
        private int _offset;
        public int ItemHeight { get; set; }
        public ImageList ImageList { get; set; }
        public Color SelectedColor { get; set; } = Color.Orange;
        public Color HoveredColor { get; set; } = Color.Red;
        public int AppearInterval { get; set; } = 500;
        public int ToolTipDuration { get; set; } = 3000;
        public event EventHandler FocussedItemIndexChanged;
        internal AutocompleteListView(FastColoredTextBox tb, AutocompleteMenu menu)
        {
            _tb = tb;
            _menu = menu;
            Font = new Font("Segoe UI", 9);
            ItemHeight = Font.Height + 4;
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
            SetStyle(ControlStyles.Selectable, false);
        }
        public int Count { get { return VisibleItems.Count; } }
        public int FocussedItemIndex
        {
            get { return _focussedItemIndex; }
            set
            {
                if (_focussedItemIndex == value) return;
                _focussedItemIndex = value;
                EnsureVisible();
                Invalidate();
                FocussedItemIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public AutocompleteItem FocussedItem
        {
            get { return _focussedItemIndex >= 0 && _focussedItemIndex < VisibleItems.Count ? VisibleItems[_focussedItemIndex] : null; }
            set { FocussedItemIndex = VisibleItems.IndexOf(value); }
        }
        public void SetAutocompleteItems(ICollection<string> items)
        {
            SetAutocompleteItems(items.Select(i => new AutocompleteItem(i)).ToList());
        }
        public void SetAutocompleteItems(IEnumerable<AutocompleteItem> items) { _sourceItems = items ?? new List<AutocompleteItem>(); }
        internal IEnumerable<AutocompleteItem> SourceItems { get { return _sourceItems; } }
        public void SelectNext(int shift)
        {
            if (VisibleItems.Count == 0) return;
            FocussedItemIndex = Math.Max(0, Math.Min(VisibleItems.Count - 1, _focussedItemIndex + shift));
        }
        private int VisibleRows { get { return Math.Max(1, Height / Math.Max(1, ItemHeight)); } }
        private void EnsureVisible()
        {
            if (_focussedItemIndex < _offset) _offset = _focussedItemIndex;
            else if (_focussedItemIndex >= _offset + VisibleRows) _offset = _focussedItemIndex - VisibleRows + 1;
            _offset = Math.Max(0, _offset);
        }
        internal void ResetOffset() { _offset = 0; }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);
            int y = 0;
            for (int i = _offset; i < VisibleItems.Count && y < Height; i++, y += ItemHeight)
            {
                AutocompleteItem item = VisibleItems[i];
                Color back = item.BackColor;
                if (i == _focussedItemIndex)
                {
                    using (SolidBrush b = new SolidBrush(back.A > 0 ? back : SelectedColor)) g.FillRectangle(b, 1, y, Width - 2, ItemHeight);
                }
                Color fore = item.ForeColor.A > 0 ? item.ForeColor : Color.Black;
                int x = 4;
                if (ImageList != null && item.ImageIndex >= 0 && item.ImageIndex < ImageList.Images.Count)
                {
                    g.DrawImage(ImageList.Images[item.ImageIndex], x, y + (ItemHeight - ImageList.ImageSize.Height) / 2);
                    x += ImageList.ImageSize.Width + 4;
                }
                using (SolidBrush b = new SolidBrush(fore))
                    g.DrawString(item.ToString(), Font, b, new RectangleF(x, y, Width - x - 2, ItemHeight), new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap });
            }
            base.OnPaint(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int index = _offset + e.Y / Math.Max(1, ItemHeight);
            if (index >= 0 && index < VisibleItems.Count)
            {
                FocussedItemIndex = index;
                if (e.Clicks == 2) _menu.OnSelecting();
            }
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            _offset = Math.Max(0, Math.Min(Math.Max(0, VisibleItems.Count - VisibleRows), _offset - Math.Sign(e.Delta) * 3));
            Invalidate();
            if (e is HandledMouseEventArgs h) h.Handled = true;
            base.OnMouseWheel(e);
        }
    }

    public class AutocompleteMenu : ToolStripDropDown
    {
        private readonly FastColoredTextBox _tb;
        private readonly AutocompleteListView _listView;
        private readonly ToolStripControlHost _host;
        private Timer _timer;
        public Range Fragment { get; internal set; }
        public string SearchPattern { get; set; } = @"[\w\.]";
        public int MinFragmentLength { get; set; } = 2;
        public bool AllowTabKey { get; set; }
        public int AppearInterval { get { return _listView.AppearInterval; } set { _listView.AppearInterval = value; } }
        public int ToolTipDuration { get { return _listView.ToolTipDuration; } set { _listView.ToolTipDuration = value; } }
        public Color SelectedColor { get { return _listView.SelectedColor; } set { _listView.SelectedColor = value; } }
        public Color HoveredColor { get { return _listView.HoveredColor; } set { _listView.HoveredColor = value; } }
        public bool AlwaysShowTooltip { get; set; }
        public new AutocompleteListView Items { get { return _listView; } }
        public ImageList ImageList { get { return _listView.ImageList; } set { _listView.ImageList = value; } }
        public new Font Font { get { return _listView.Font; } set { _listView.Font = value; _listView.ItemHeight = value.Height + 4; } }
        public new Size MinimumSize { get; set; } = new Size(180, 0);
        public new Size MaximumSize { get; set; } = new Size(180, 200);
        public event EventHandler<SelectingEventArgs> Selecting;
        public event EventHandler<SelectedEventArgs> Selected;
        public new event EventHandler<CancelEventArgs> Opening;

        public AutocompleteMenu(FastColoredTextBox tb)
        {
            _tb = tb;
            TakesFocus = false;
            AutoClose = true;
            Padding = new Padding(0);
            _listView = new AutocompleteListView(tb, this) { Size = new Size(180, 200) };
            _host = new ToolStripControlHost(_listView) { Margin = new Padding(0), AutoSize = false };
            base.Items.Add(_host);
            Fragment = new Range(tb);
            tb.KeyDown += Tb_KeyDown;
            tb.KeyPressed += Tb_KeyPressed;
            tb.LostFocus += (s, e) => { if (Visible && !_listView.Focused) Close(); };
        }
        private void Tb_KeyDown(object sender, KeyEventArgs e)
        {
            if (!Visible)
            {
                if (e.KeyData == (Keys.Control | Keys.Space)) { Show(true); e.Handled = true; }
                return;
            }
            switch (e.KeyCode)
            {
                case Keys.Down: _listView.SelectNext(1); e.Handled = true; break;
                case Keys.Up: _listView.SelectNext(-1); e.Handled = true; break;
                case Keys.PageDown: _listView.SelectNext(10); e.Handled = true; break;
                case Keys.PageUp: _listView.SelectNext(-10); e.Handled = true; break;
                case Keys.Enter: OnSelecting(); e.Handled = true; break;
                case Keys.Tab: if (AllowTabKey) { OnSelecting(); e.Handled = true; } break;
                case Keys.Escape: Close(); e.Handled = true; break;
                case Keys.Left:
                case Keys.Right:
                case Keys.Home:
                case Keys.End: Close(); break;
            }
        }
        private void Tb_KeyPressed(object sender, KeyPressEventArgs e)
        {
            bool forced = e.KeyChar == ' ' && (Control.ModifierKeys & Keys.Control) != 0;
            // The character is in the text after the key press is processed
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (Visible) Filter(forced);
                else if (MinFragmentLength > 0 || forced) StartTimer(forced);
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
        private void StartTimer(bool forced)
        {
            if (_timer == null)
            {
                _timer = new Timer();
                _timer.Tick += (s, e) => { _timer.Stop(); Show(false); };
            }
            _timer.Stop();
            _timer.Interval = Math.Max(1, AppearInterval);
            _timer.Start();
        }
        // The text before the caret that matches the search pattern
        private string GetFragment()
        {
            Regex regex = new Regex(SearchPattern);
            Place caret = _tb.Selection.Start;
            string line = _tb.GetLineText(caret.iLine);
            int end = Math.Min(caret.iChar, line.Length), start = end;
            while (start > 0 && regex.IsMatch(line[start - 1].ToString())) start--;
            Fragment = new Range(_tb, start, caret.iLine, end, caret.iLine);
            return line.Substring(start, end - start);
        }
        private void Filter(bool forced)
        {
            string fragment = GetFragment();
            List<AutocompleteItem> visible = new List<AutocompleteItem>();
            int focus = 0;
            bool selected = false;
            if (fragment.Length >= MinFragmentLength || forced)
            {
                foreach (AutocompleteItem item in _listView.SourceItems)
                {
                    item.Parent = this;
                    CompareResult result = fragment.Length == 0 && forced ? CompareResult.Visible : item.Compare(fragment);
                    if (result == CompareResult.Hidden) continue;
                    if (result == CompareResult.VisibleAndSelected && !selected) { focus = visible.Count; selected = true; }
                    visible.Add(item);
                }
            }
            _listView.VisibleItems = visible;
            _listView.ResetOffset();
            _listView.FocussedItemIndex = visible.Count > 0 ? focus : -1;
            if (visible.Count == 0) Close();
            else LayoutList();
            _listView.Invalidate();
        }
        private void LayoutList()
        {
            int height = Math.Min(MaximumSize.Height, Math.Max(MinimumSize.Height, _listView.VisibleItems.Count * _listView.ItemHeight + 2));
            int width = Math.Max(MinimumSize.Width, Math.Min(MaximumSize.Width, 180));
            _listView.Size = new Size(width, height);
            _host.Size = new Size(width, height);
        }
        public void Show(bool forced)
        {
            CancelEventArgs opening = new CancelEventArgs();
            Opening?.Invoke(this, opening);
            if (opening.Cancel) return;
            Filter(forced);
            if (_listView.VisibleItems.Count == 0) return;
            Point point = _tb.PlaceToPoint(Fragment.End);
            point.Offset(2, 2);
            if (!Visible) Show(_tb, point);
            _listView.Invalidate();
        }
        public new void Close()
        {
            _timer?.Stop();
            base.Close();
        }
        public void OnSelecting()
        {
            AutocompleteItem item = _listView.FocussedItem;
            if (item == null) { Close(); return; }
            SelectingEventArgs args = new SelectingEventArgs { Item = item, SelectedIndex = _listView.FocussedItemIndex };
            Selecting?.Invoke(this, args);
            if (args.Cancel) { _listView.FocussedItemIndex = args.SelectedIndex; return; }
            if (!args.Handled)
            {
                _tb.BeginAutoUndo();
                _tb.Selection = Fragment;
                _tb.InsertText(item.GetTextForReplace());
                _tb.EndAutoUndo();
            }
            Close();
            SelectedEventArgs selected = new SelectedEventArgs { Item = item, Tb = _tb };
            item.OnSelected(this, selected);
            Selected?.Invoke(this, selected);
            _tb.Focus();
        }
        public void SelectNext(int shift) { _listView.SelectNext(shift); }
    }
}
