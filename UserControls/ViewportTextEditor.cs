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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UserControls
{
    public enum ViewportEditModeEnum
    {
        None,
        Annotation,
        LegendMinimum,
        LegendMaximum
    }

    public partial class ViewportTextEditor : UserControl
    {
        // Variables                                                                                                                
        private int _pad = 6;
        private Rectangle _parentArea;
        private Point _initialLocation;
        private ViewportEditModeEnum _viewportEditMode;


        // Properties                                                                                                               
        public ViewportEditModeEnum ViewportEditMode
        {
            get { return _viewportEditMode; }
            set
            {
                _viewportEditMode = value;
                SetError(false);
            }
        }
        public Size MinSize { get { return vertbData.MinSize; } set { vertbData.MinSize = value; } }
        public Rectangle ParentArea
        {
            get { return _parentArea; }
            set
            {
                _parentArea = value;
                vertbData.MaxSize = new Size(_parentArea.Size.Width - _pad, _parentArea.Size.Height - _pad);
            }
        }
        public new Point Location
        {
            get { return base.Location; }
            set
            {
                base.Location = value;
                _initialLocation = value;
            }
        }
        public override string Text
        {
            get { return vertbData.Text; }
            set
            {
                vertbData.Text = value;
                vertbData_SizeChanged(null, null);
            }
        }


        // Events                                                                                                                   
        public event Action CommitRequested;
        public event Action CancelRequested;
        public event Action NextRequested;


        // Event handlers                                                                                                           
        private void vertbData_SizeChanged(object sender, EventArgs e)
        {
            Size = new Size(vertbData.Width + _pad, vertbData.Height + _pad);
            Point location = _initialLocation;
            //
            if (_initialLocation.X + Width > _parentArea.Right) location.X = _parentArea.Right - Width;
            if (_initialLocation.Y + Height > _parentArea.Bottom) location.Y = _parentArea.Bottom - Height;
            //
            base.Location = location;
        }
        private void vertbData_TextChanged(object sender, EventArgs e)
        {
            SetError(false);
        }


        // Constructors                                                                                                             
        public ViewportTextEditor()
        {
            InitializeComponent();
            _viewportEditMode = ViewportEditModeEnum.None;
            vertbData.TextChanged += vertbData_TextChanged;
        }

        // Methods                                                                                                                  
        public bool IsOrContainsControl(Control control)
        {
            if (this == control) return true;
            else if (pBorder == control) return true;
            else if (vertbData == control) return true;
            else return false;
        }
        public void SetCaretAtEnd()
        {
            vertbData.Focus();
            vertbData.SelectionStart = vertbData.TextLength;
            vertbData.SelectionLength = 0;
        }
        public void SetError(bool error)
        {
            if (error) pBorder.BackColor = Color.Red;
            else pBorder.BackColor = SystemColors.Highlight;
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                CancelRequested?.Invoke();
                return true;
            }
            //
            bool legendValue = _viewportEditMode == ViewportEditModeEnum.LegendMinimum ||
                               _viewportEditMode == ViewportEditModeEnum.LegendMaximum;
            if (_viewportEditMode == ViewportEditModeEnum.Annotation &&
                keyData == (Keys.Control | Keys.Enter))
            {
                CommitRequested?.Invoke();
                return true;
            }
            else if (legendValue)
            {
                if (keyData == Keys.Enter)
                {
                    CommitRequested?.Invoke();
                    return true;
                }
                else if (keyData == Keys.Tab || keyData == (Keys.Shift | Keys.Tab))
                {
                    NextRequested?.Invoke();
                    return true;
                }
            }
            //
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
