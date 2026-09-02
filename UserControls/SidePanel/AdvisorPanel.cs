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
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UserControls
{
    public partial class AdvisorPanel : SidePanel
    {
        // Variables                                                                                                                
        protected LinkedListNode<PanelPage> _currentPageNode;
        protected LinkedList<PanelPage> _pages;
        private bool _restoringState;


        // Properties                                                                                                               
        

        // Events                                                                                                                   
        public event Action<object, EventArgs> CloseButtonPressedEvent;
        public event Action<ViewType> SetViewEvent;
        public event Action StateChanged;


        // Constructor                                                                                                              
        public AdvisorPanel(Action<object, EventArgs> CloseButtonPressed, Action<ViewType> SetView)
        {
            DoubleBuffered = true;
            //
            InitializeComponent();
            //
            _currentPageNode = null;
            _pages = new LinkedList<PanelPage>();
            //
            CloseButtonPressedEvent = CloseButtonPressed;
            SetViewEvent = SetView;
        }


        // Event handlers                                                                                                           
        private void AdvisorPanel_Resize(object sender, EventArgs e)
        {
            UpdateCurrentPageControlPositions();
        }
        //
        private void btnClose_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                this.btnClose.Image = global::UserControls.Properties.Resources.Close_White;
            }
            catch
            { }
        }
        private void btnClose_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                this.btnClose.Image = global::UserControls.Properties.Resources.Close;
            }
            catch
            { }
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            try
            {
                CloseButtonPressedEvent?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                CaeGlobals.ExceptionTools.Show(this, ex);
            }
        }
        //
        private void lnklabPrevious_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (_currentPageNode != null && _currentPageNode.Previous != null)
            {
                _currentPageNode = _currentPageNode.Previous;
            }
            //
            ShowCurrentPage();
            OnStateChanged();
        }
        private void lnklabNext_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (_currentPageNode != null && _currentPageNode.Next != null)
            {
                _currentPageNode = _currentPageNode.Next;
            }
            //
            ShowCurrentPage();
            //
            ShowCurrentPage();
            OnStateChanged();
        }
        private void lnklabContents_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            _currentPageNode = (LinkedListNode<PanelPage>)((LinkLabel)sender).Tag;
            //
            ShowCurrentPage();
            OnStateChanged();
        }
       

        // Methods                                                                                                                  
        public void AddPage(PanelPage advisorPage)
        {
            advisorPage.SetViewEvent += SetViewEvent;
            //
            var page = _pages.AddLast(advisorPage);
            //
            if (_currentPageNode == null) _currentPageNode = page;
        }
        override public void PrepareControls(ViewType viewType)
        {
            string previousPageId = _currentPageNode == null ? null : _currentPageNode.Value.Id;
            int verticalOffset = 2;
            int horozontalOffset = 0;
            int y = 4;
            int count = 1;
            panContents.Controls.Clear();
            //
            for (LinkedListNode<PanelPage> page = _pages.First; page != null; page = page.Next)
            {
                y += verticalOffset;
                //
                LinkLabel linkLabel = new LinkLabel();
                linkLabel.AutoSize = true;
                linkLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                linkLabel.LinkBehavior = LinkBehavior.NeverUnderline;
                linkLabel.Top = y;
                linkLabel.Left = horozontalOffset;
                linkLabel.Text = count + "  " + page.Value.Title;
                linkLabel.Tag = page;
                linkLabel.LinkClicked += lnklabContents_LinkClicked;
                //
                panContents.Controls.Add(linkLabel);
                //
                count++;
                y += linkLabel.Height;
            }
            //
            panContents.Height = y;
            labTitle.Top = panContents.Bottom + 10;
            panPage.Top = labTitle.Bottom;
            //
            if (_currentPageNode.Value.AssociatedView != viewType)
            {
                for (LinkedListNode<PanelPage> page = _pages.First; page != null; page = page.Next)
                {
                    if (page.Value.AssociatedView == viewType)
                    {
                        _currentPageNode = page;
                        break;
                    }
                }
            }
            //
            ShowCurrentPage();
            if (_currentPageNode != null && previousPageId != _currentPageNode.Value.Id) OnStateChanged();
        }
        override public void UpdateDesign()
        {
            UpdateCurrentPageControlPositions();
        }
        public AdvisorPanelState CaptureState()
        {
            AdvisorPanelState state = new AdvisorPanelState();
            if (_currentPageNode != null) state.PageId = _currentPageNode.Value.Id;
            state.VerticalScrollPosition = Math.Max(0, -AutoScrollPosition.Y);
            return state;
        }
        public void RestoreState(AdvisorPanelState state)
        {
            if (state == null) return;
            //
            _restoringState = true;
            try
            {
                if (state.PageId != null)
                {
                    for (LinkedListNode<PanelPage> page = _pages.First; page != null; page = page.Next)
                    {
                        if (page.Value.Id == state.PageId)
                        {
                            _currentPageNode = page;
                            break;
                        }
                    }
                }
                //
                ShowCurrentPage();
                AutoScrollPosition = new Point(0, state.VerticalScrollPosition);
            }
            finally
            {
                _restoringState = false;
            }
        }
        private void OnStateChanged()
        {
            if (!_restoringState) StateChanged?.Invoke();
        }
        //
        private void ShowCurrentPage()
        {
            bool prevCScroll = VScroll;
            ShowCurrentPageOnce();
            //
            if (prevCScroll != VScroll) ShowCurrentPageOnce();
        }
        private void ShowCurrentPageOnce()
        {
            if (_currentPageNode != null)
            {
                string title = "";
                foreach (var control in panContents.Controls)
                {
                    if (control is LinkLabel ll)
                    {
                        if (ll.Tag == _currentPageNode)
                        {
                            ll.Font = new Font(ll.Font, FontStyle.Bold);
                            title = ll.Text;
                        }
                        else if (ll.Font.Bold) ll.Font = new Font(ll.Font, FontStyle.Regular);
                    }
                }
                SetViewEvent?.Invoke(_currentPageNode.Value.AssociatedView);
                // Title
                labTitle.Text = title == "" ? _currentPageNode.Value.Title : title;
                // Add controls
                panPage.Controls.Clear();
                panPage.Controls.AddRange(_currentPageNode.Value.GetControls());
                // Update positions
                UpdateCurrentPageControlPositions();
                // Previous/Next
                lnklabPrevious.Visible = _currentPageNode.Previous != null;
                lnklabNext.Visible = _currentPageNode.Next != null;
            }
        }
        private void ShowPageByIndex(int pageIndex)
        {
            if (pageIndex < 0) pageIndex = 0;
            if (pageIndex >= _pages.Count) pageIndex = _pages.Count - 1;
            //
            int count = 0;
            for (LinkedListNode<PanelPage> page = _pages.First; page != null && count++ != pageIndex; page = page.Next)
            {
                _currentPageNode = page;
            }
            //
            ShowCurrentPage();
        }
        private void UpdateCurrentPageControlPositions()
        {
            UpdateCurrentPageControlPositionsOnce();
            //
            UpdateCurrentPageControlPositionsOnce();
        }
        private void UpdateCurrentPageControlPositionsOnce()
        {
            if (_currentPageNode != null)
            {
                _currentPageNode.Value.UpdateControlPositions(panPage);
                //
                panLine.Top = panPage.Bottom;
                lnklabPrevious.Top = panLine.Bottom + 10;
                lnklabNext.Top = panLine.Bottom + 10;
                //
                panBackground.Height = lnklabPrevious.Bottom + 10;
            }
        }


    }
}
