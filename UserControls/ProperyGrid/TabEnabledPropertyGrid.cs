// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using UserControls.Properties;
using System.Resources;
using AutocompleteMenuNS;
using CaeGlobals;
using System.Diagnostics;
using System.Drawing;

namespace UserControls
{
    // http://kiwigis.blogspot.si/2009/05/adding-tab-key-support-to-propertygrid.html
    public class TabEnabledPropertyGrid : PropertyGrid
    {
        // Variables                                                                                                                
        private AutocompleteMenu autocompleteMenu;
        private TextBox _editControl;
        private bool _tabInitialized;
        private bool _readOnly;
        private bool _selectFirstProperty;
        private bool _selectFirstCategory;
        private bool _focusOnSelectedObjectChanged;


        // Variables                                                                                                                
        public bool ReadOnly { get { return _readOnly; } set { _readOnly = value; } }
        public bool FocusOnSelectedObjectChanged
        {
            get { return _focusOnSelectedObjectChanged; }
            set { _focusOnSelectedObjectChanged = value; }
        }


        // Constructors                                                                                                             
        public TabEnabledPropertyGrid() : base()
        {
            InitializeComponent();
            //
            this.LineColor = SystemColors.Control;
            this.DisabledItemForeColor = Color.FromArgb(80, 80, 80);
            //
            _tabInitialized = false;
            _readOnly = false;
            //
            SetSelectFirstProperty();
            _focusOnSelectedObjectChanged = true;
            //
            BuildAutocompleteMenu(new string[0]);
        }
        private void InitializeComponent()
        {
            this.autocompleteMenu = new AutocompleteMenu();
            this.SuspendLayout();
            // 
            // autocompleteMenu
            // 
            this.autocompleteMenu.AllowsTabKey = true;
            this.autocompleteMenu.Font = new Font("Segoe UI", 9F);
            this.autocompleteMenu.ImageList = null;
            this.autocompleteMenu.Items = new string[0];
            this.autocompleteMenu.MaximumSize = new Size(600, 200);
            this.autocompleteMenu.MinFragmentLength = 1;
            this.autocompleteMenu.SearchPattern = "[\\w\\.\\-]+";
            this.autocompleteMenu.TargetControlWrapper = null;
            this.ResumeLayout(false);
        }
       
        
        // Event handlers                                                                                                           
        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (!this.ContainsFocus) return;
            if (autocompleteMenu.Visible)
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab || e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                //
                if (e.KeyCode == Keys.Enter) autocompleteMenu.ProcessKey((char)Keys.Return, Keys.None);
                else if (e.KeyCode == Keys.Tab) autocompleteMenu.ProcessKey((char)Keys.Tab, Keys.None);
                else if (e.KeyCode == Keys.Escape) autocompleteMenu.ProcessKey((char)Keys.Escape, Keys.None);
                //
                return;
            }
            // Handle tab key of the property grid
            if (e.KeyCode == Keys.Tab)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                // Get selected grid item
                GridItem gridItem = this.SelectedGridItem;
                if (gridItem == null) { return; }
                // Create a collection all visible child grid items in property grid
                GridItem root = gridItem;
                while (root.GridItemType != GridItemType.Root)
                {
                    root = root.Parent;
                }
                List<GridItem> gridItems = new List<GridItem>();
                this.FindItems(root, gridItems);
                // Get position of selected grid item in collection
                int index = gridItems.IndexOf(gridItem);
                int nextIndex = index + 1;
                if (nextIndex >= gridItems.Count)
                {
                    e.Handled = false;
                    e.SuppressKeyPress = false;
                    return;
                }
                // Select next grid item in collection
                this.SelectedGridItem = gridItems[nextIndex];
                SendKeys.Send("{Tab}");
            }
            
        }
        private void FindItems(GridItem item, List<GridItem> gridItems)
        {
            switch (item.GridItemType)
            {
                case GridItemType.Root:
                case GridItemType.Category:
                    foreach (GridItem i in item.GridItems)
                    {
                        this.FindItems(i, gridItems);
                    }
                    break;
                case GridItemType.Property:
                    gridItems.Add(item);
                    if (item.Expanded)
                    {
                        foreach (GridItem i in item.GridItems)
                        {
                            this.FindItems(i, gridItems);
                        }
                    }
                    break;
                case GridItemType.ArrayValue:
                    break;
            }
        }
        // Autocomplete menu
        private void AttachAutocompleteToInternalTextBox()
        {
            TextBox editTextBox = FindPropertyGridTextBox();
            if (editTextBox != null)
            {
                // Add autocompleteMenu to edit control
                _editControl = editTextBox;
                _editControl.TextChanged += EditControl_TextChanged;
                _editControl.PreviewKeyDown += EditControl_PreviewKeyDown;
                autocompleteMenu.SetAutocompleteMenu(editTextBox, autocompleteMenu);
            }
        }
        private void DetachAutocompleteFromInternalTextBox()
        {
            if (_editControl != null)
            {
                // Remove autocompleteMenu from edit control
                autocompleteMenu.TargetControlWrapper = null;
                autocompleteMenu.SetAutocompleteMenu(_editControl, null);
                _editControl.TextChanged -= EditControl_TextChanged;
                _editControl.PreviewKeyDown -= EditControl_PreviewKeyDown;
                _editControl = null;
            }
        }
        private TextBox FindPropertyGridTextBox()
        {
            foreach (Control control in Controls)
            {
                if (control.GetType().Name == "PropertyGridView")
                {
                    foreach (Control subControl in control.Controls)
                    {
                        if (subControl is TextBox tb && tb.Visible && !tb.ReadOnly)
                            return tb;
                    }
                }
            }
            return null;
        }
        private void EditControl_TextChanged(object sender, EventArgs e)
        {
            autocompleteMenu.Enabled = _editControl != null && _editControl.Text.Trim().Length > 0 &&
                                       _editControl.Text.Trim()[0] == '=';
        }
        private void EditControl_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            // Must be active to forward these keys to 
            if (autocompleteMenu.Visible)
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab || e.KeyCode == Keys.Escape)
                    e.IsInputKey = true;
            }
        }



        // Overrides                                                                                                                
        protected override void OnSelectedObjectsChanged(EventArgs e)
        {
            base.OnSelectedObjectsChanged(e);
            // Site
            if (Site == null) Site = new MySite(this);
            // For the Tab key to work set the key event handlers
            Control parent = this.Parent;
            while (!_tabInitialized && parent != null)
            {
                if (parent is Form frm)
                {
                    // Set this property to intercept all events
                    frm.KeyPreview = true;
                    // Listen for keydown event
                    frm.KeyDown += new KeyEventHandler(this.Form_KeyDown);
                    //
                    _tabInitialized = true;
                }
                //
                parent = parent.Parent;
            }
            //
            if (!IsDisposed && IsHandleCreated)
            {
                BeginInvoke(new Action(() => { SelectFirstItem(); }));
            }
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            //
            if (SelectedObject != null && !IsDisposed)
            {
                BeginInvoke(new Action(() => { SelectFirstItem(); }));
            }
        }
        protected override void OnGotFocus(EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine(DateTime.Now.Millisecond + " OnGotFocus");
            //// Get selected griditem
            //GridItem gridItem = this.SelectedGridItem;
            //if (gridItem == null) { return; }

            //// Create a collection all visible child griditems in propertygrid
            //GridItem root = gridItem;
            //while (root.GridItemType != GridItemType.Root)
            //{
            //    root = root.Parent;
            //}
            //List<GridItem> gridItems = new List<GridItem>();
            //this.FindItems(root, gridItems);

            ////this.SelectedGridItem = gridItems[0];
            //this.SelectedGridItem = gridItem;

            ////SendKeys.Send("{Tab}");

            base.OnGotFocus(e);
        }
        protected override void OnEnter(EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine(DateTime.Now.Millisecond + " OnEnter");
            // Get selected griditem
            GridItem gridItem = this.SelectedGridItem;
            if (gridItem == null) { return; }
            // Create a collection all visible child griditems in propertygrid
            GridItem root = gridItem;
            while (root.GridItemType != GridItemType.Root)
            {
                root = root.Parent;
            }
            List<GridItem> gridItems = new List<GridItem>();
            this.FindItems(root, gridItems);
            //
            if (gridItems[0].Expanded) this.SelectedGridItem = gridItems[0];
            //
            base.OnEnter(e);
        }
        protected override void OnSelectedGridItemChanged(SelectedGridItemChangedEventArgs e)
        {
            if (_readOnly)
            {
                if (e.NewSelection.GridItemType == GridItemType.Property)
                {
                    if (e.NewSelection.Parent != null && e.NewSelection.Parent.GridItemType == GridItemType.Category)
                    {
                        this.SelectedGridItem = e.NewSelection.Parent;
                        return;
                    }
                }
            }
            else base.OnSelectedGridItemChanged(e);
            //
            this.BeginInvoke(new Action(DetachAutocompleteFromInternalTextBox));
            this.BeginInvoke(new Action(AttachAutocompleteToInternalTextBox));
        }
        
        
        // Methods                                                                                                                  
        public void SetLabelColumnWidth(double labelRatio)
        {
            // get the grid view

            Control view = (Control)typeof(PropertyGrid).GetField("gridView", BindingFlags.Instance | BindingFlags.NonPublic).GetValue((PropertyGrid)this);
            //Control view = (Control)typeof(PropertyGrid).GetField("gridView", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(grid);

            // set label width
            //FieldInfo fi = view.GetType().GetField("labelWidth", BindingFlags.Instance | BindingFlags.NonPublic);
            //fi.SetValue(view, width);

            FieldInfo fi2 = view.GetType().GetField("labelRatio");
            fi2.SetValue(view, labelRatio);

            // refresh
            view.Invalidate();
        }
        public void SetSelectFirstByDefault()
        {
            _selectFirstProperty = false;
            _selectFirstCategory = false;
        }
        public void SetSelectFirstProperty()
        {
            _selectFirstProperty = true;
            _selectFirstCategory = false;
        }
        public void SetSelectFirstCategory()
        {
            _selectFirstProperty = false;
            _selectFirstCategory = true;
        }
        private void SelectFirstItem()
        {
            if (_selectFirstProperty) SelectFirstVisibleProperty();
            else if (_selectFirstCategory) SelectFirstVisibleCategory();
            //
            if (_focusOnSelectedObjectChanged) this.Focus();
        }
        public void SelectFirstVisibleProperty()
        {
            GridItem root = SelectedGridItem?.Parent?.Parent;
            if (root == null) return;
            //
            foreach (GridItem category in root.GridItems)
            {
                if (!category.Expanded) category.Expanded = true;
                //
                foreach (GridItem item in category.GridItems)
                {
                    SelectedGridItem = item;
                    return;
                }
            }
        }
        public void SelectFirstVisibleCategory()
        {
            GridItem root = SelectedGridItem?.Parent?.Parent;
            if (root == null) return;
            //
            foreach (GridItem category in root.GridItems)
            {
                SelectedGridItem = category;
                return;
            }
        }
        // Autocomplete menu
        public void BuildAutocompleteMenu(IEnumerable<string> items)
        {
            List<string> autoCompleteItems = new List<string>();
            var constants = MyNCalc.GetFunctionConstants();
            autoCompleteItems.AddRange(items);
            autoCompleteItems.AddRange(constants);
            autoCompleteItems.Sort();
            List<AutocompleteItem> acItems = new List<AutocompleteItem>();
            foreach (var item in autoCompleteItems) acItems.Add(new AutocompleteItem(item));
            //
            var snippets = MyNCalc.GetFunctionSnippets();
            foreach (var snippet in snippets) acItems.Add(new SnippetAutocompleteItem(snippet));
            // Set as autocomplete source
            autocompleteMenu.SetAutocompleteItems(acItems);
        }
    }
}
