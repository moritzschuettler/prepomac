// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UserControls
{
    [Serializable]
    public enum ViewGeometryModelResults
    {
        Geometry,
        Model,
        Results
    }
    //
    [Serializable]
    public class PanelPage
    {
        // Variables                                                                                                                
        protected int _minOffset;
        protected int _horizontalOffset;
        protected int _verticalOffset;
        //
        protected string _id;
        protected string _title;
        protected ViewType _associatedView;
        //
        protected List<PanelItem> _items;


        // Properties                                                                                                               
        public int HorizontalOffset
        {
            get { return _horizontalOffset; }
            set
            {
                _horizontalOffset = value;
                if (_horizontalOffset < 10) _horizontalOffset = 10;
            }
        }
        public int VerticalOffset
        {
            get { return _verticalOffset; }
            set
            {
                _verticalOffset = value;
                if (_verticalOffset < 0) _verticalOffset = 0;
            }
        }
        //
        public string Id { get { return _id; } set { _id = value; } }
        public string Title { get { return _title; } set { _title = value; } }
        public ViewType AssociatedView { get { return _associatedView; } set { _associatedView = value; } }
        //
        public List<PanelItem> Items { get { return _items; } }


        // Events                                                                                                                   
        public event Action<ViewType> SetViewEvent;


        // Constructors                                                                                                             
        public PanelPage()
        {
            _minOffset = 10;
            _horizontalOffset = _minOffset;
            _verticalOffset = _minOffset;
            //
            _id = null;
            _items = new List<PanelItem>();
        }


        // Event handlers                                                                                                           
        private void LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (sender is LinkLabel ll && ll.Tag is PanelItemLinkLabel pill && pill != null)
            {
                SetViewEvent?.Invoke(_associatedView);
                pill.Activate();
            }
        }


        // Methods                                                                                                                  
        public Control[] GetControls()
        {
            List<Control> controls = new List<Control>();
            //
            foreach (var item in _items)
            {
                if (item is PanelItemLabel pil)
                {
                    Label label = new Label();
                    label.AutoSize = true;
                    label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    label.Left = pil.IndentLevel * _horizontalOffset;
                    label.Text = pil.Text;
                    label.Tag = pil;
                    
                    //
                    controls.Add(label);
                }
                else if (item is PanelItemLinkLabel pill)
                {
                    LinkLabel linkLabel = new LinkLabel();
                    linkLabel.AutoSize = true;
                    linkLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    linkLabel.LinkBehavior = LinkBehavior.NeverUnderline;
                    linkLabel.Left = pill.IndentLevel * _horizontalOffset;
                    linkLabel.Text = pill.Text;
                    linkLabel.Tag = pill;
                    linkLabel.LinkClicked += LinkLabel_LinkClicked;
                    //
                    controls.Add(linkLabel);
                }
                else throw new NotSupportedException();
            }
            return controls.ToArray();
        }
        public void UpdateControlPositions(Control parentControl)
        {
            int y = 0;
            bool prevControlLinkLabel = false;
            foreach (var control in parentControl.Controls)
            {
                if (control is Label lab)
                {
                    if (lab is LinkLabel lnklab)
                    {
                        if (prevControlLinkLabel) y += 2;
                        else y += _verticalOffset;
                        //
                        lnklab.Top = y;
                        y += lnklab.Height;
                        //
                        prevControlLinkLabel = true;
                    }
                    else
                    {
                        y += _verticalOffset;
                        lab.Top = y;
                        lab.MaximumSize = new System.Drawing.Size(parentControl.Width - lab.Left, 0);
                        y += lab.Height;
                        //
                        prevControlLinkLabel = false;
                    }
                }
                else throw new NotSupportedException();
            }
            //
            parentControl.Height = y + _verticalOffset;
        }
    }
}
