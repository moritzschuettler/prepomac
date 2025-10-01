using CaeGlobals;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaeModel;

namespace PrePoMax.Forms
{    
    public partial class FrmUserVew : UserControls.PrePoMaxChildForm, IFormBase
    {
        // Variables                                                                                                                
        private Controller _controller;
        private ViewUserViewParameters _viewUserViewParameters;
        private int _initialPadding;
        static bool _collapsed = true;

        // Properties                                                                                                               


        // Constructors                                                                                                             
        public FrmUserVew(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            //
            pgProperties.SetLabelColumnWidth(2.25);
            //
            lvViews.Items.Clear();
        }
        public List<UserViewParameters> UserViewParameters
        {
            get
            {
                List<UserViewParameters> list = new List<UserViewParameters>();
                foreach (ListViewItem item in lvViews.Items)
                {
                    if (item.Tag is UserViewParameters uvp) list.Add(uvp);
                    else throw new NotSupportedException();
                }
                return list;
            }
            set
            {
                lvViews.Items.Clear();
                if (value != null)
                {
                    foreach (UserViewParameters uvp in value)
                    {
                        ListViewItem listViewItem = new ListViewItem(uvp.Name);
                        listViewItem.Tag = uvp.DeepClone();
                        lvViews.Items.Add(listViewItem);
                    }
                    if (lvViews.Items.Count > 0) lvViews.Items[0].Selected = true;
                }
            }
        }


        // Event handlers                                                                                                           
        private void FrmUserVew_Load(object sender, EventArgs e)
        {
            _initialPadding = gbProperties.Bottom - btnOK.Top; // use the same equation as for newPadding
            //
            gbProperties.IsCollapsed = _collapsed;
        }
        private void FrmFollowerView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                btnCancel_Click(null, null);
            }
        }
        private void FrmFollowerView_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.Visible)
                {
                    this.Enabled = true;    // activating selection disables the form
                }
                else
                {
                    if (DialogResult == DialogResult.OK) {}
                    else if (DialogResult == DialogResult.Cancel) Cancel();
                    // The form was closed from frmMain.CloseAllForms
                    else if (DialogResult == DialogResult.None) Cancel();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void lvViews_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (lvViews.PossiblySelectedItems.Count == 1)
                {
                    // Clear
                    pgProperties.SelectedObject = null;
                    //
                    if (lvViews.PossiblySelectedItems[0].Tag is UserViewParameters uvp)
                    {
                        _viewUserViewParameters = new ViewUserViewParameters(uvp);
                    }
                    else throw new NotSupportedException();
                    //
                    pgProperties.SelectedObject = _viewUserViewParameters;
                }
            }
            catch
            { }
        }
        private void lvViews_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            btnApply_Click(null, null);
        }
        private void gbProperties_OnCollapsedChanged(object sender)
        {
            try
            {
                int newPadding = gbProperties.Bottom - btnOK.Top;
                if (newPadding != _initialPadding)
                {
                    int delta = newPadding - _initialPadding;
                    gbProperties.Anchor &= ~AnchorStyles.Bottom;
                    this.Height += delta;
                    gbProperties.Anchor |= AnchorStyles.Bottom;
                }
                _collapsed = gbProperties.IsCollapsed;
            }
            catch
            { }
        }
        private void pgProperties_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            try
            {
                string property = pgProperties.SelectedGridItem.PropertyDescriptor.Name;
                //
                if (property == nameof(_viewUserViewParameters.Name))
                {
                    if (lvViews.PossiblySelectedItems.Count == 1)
                    {
                        lvViews.PossiblySelectedItems[0].Text = _viewUserViewParameters.Name;
                    }
                }
                else
                {
                    //btnApply_Click(null, null);
                }
            }
            catch
            { }
        }
        //
        private void btnCreate_Click(object sender, EventArgs e)
        {
            try
            {
                HashSet<string> names = new HashSet<string>();
                foreach (ListViewItem item in lvViews.Items) names.Add(item.Text);
                //
                string name = names.GetNextNumberedKey("User_view");
                ListViewItem listViewItem = new ListViewItem(name);
                listViewItem.Tag = new UserViewParameters(name);
                listViewItem.Selected = true;
                lvViews.Items.Add(listViewItem);
                //
                btnUpdate_Click(null, null);
            }
            catch
            { }
        }
        private void btnApply_Click(object sender, EventArgs e)
        {
            try
            {
                if (_viewUserViewParameters != null)
                {
                    UserViewParameters parameters = _viewUserViewParameters.GetBase();
                    _controller.SetViewParameters(true, parameters.Position, parameters.FocalPoint,
                                                  parameters.UpVector, parameters.ParallelScale);
                }
            }
            catch
            { }
        }
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (_viewUserViewParameters != null)
                {
                    UserViewParameters parameters = _viewUserViewParameters.GetBase();
                    _controller.GetViewParameters(out double[] position, out double[] focalPoint,
                                                  out double[] upVector, out double parallelScale);
                    parameters.Position = position;
                    parameters.FocalPoint = focalPoint;
                    parameters.UpVector = upVector;
                    parameters.ParallelScale = parallelScale;
                    //
                    pgProperties.Refresh();
                }
            }
            catch
            { }
        }
        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            try
            {
                int currentIndex = lvViews.PossiblySelectedItems[0].Index;
                if (currentIndex == -1) return;
                //
                ListViewItem item = lvViews.Items[currentIndex];
                if (currentIndex > 0)
                {
                    lvViews.Items.RemoveAt(currentIndex);
                    lvViews.Items.Insert(currentIndex - 1, item);
                }
                _propertyItemChanged = true;
            }
            catch
            { }
        }
        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            try
            {
                int currentIndex = lvViews.PossiblySelectedItems[0].Index;
                if (currentIndex == -1) return;
                //
                ListViewItem item = lvViews.Items[currentIndex];
                if (currentIndex < lvViews.Items.Count - 1)
                {
                    lvViews.Items.RemoveAt(currentIndex);
                    lvViews.Items.Insert(currentIndex + 1, item);
                }
                _propertyItemChanged = true;
            }
            catch
            { }
        }
        private void btnRemove_Click(object sender, EventArgs e)
        {
            try
            {
                if (lvViews.PossiblySelectedItems.Count == 1)
                {
                    ListViewItem item = lvViews.PossiblySelectedItems[0];
                    int index = item.Index;
                    if (index == lvViews.Items.Count - 1) index--;
                    lvViews.Items.Remove(item);
                    //
                    if (lvViews.Items.Count > 0) lvViews.Items[index].Selected = true;
                    else pgProperties.SelectedObject = null;
                    //
                    _propertyItemChanged = true;
                }
            }
            catch
            { }
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                HashSet<string> names = new HashSet<string>();
                List<UserViewParameters> list = new List<UserViewParameters>();
                foreach (ListViewItem item in lvViews.Items)
                {
                    if (item.Tag is UserViewParameters uvp)
                    {
                        list.Add(uvp);
                        names.Add(uvp.Name);
                    }
                    else throw new NotSupportedException();
                }
                //
                if (list.Count != names.Count)
                    throw new CaeException("The names of user views must be unique.");
                //
                _controller.SetUserViewParameters(list);
                //
                this.DialogResult = DialogResult.OK;
                Hide();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Hide();
        }
        

        // Methods                                                                                                                  

        // IFormBase
        public bool PrepareForm(string stepName, string itemToEditName)
        {
            this.DialogResult = DialogResult.None;
            //
            pgProperties.Refresh();
            //
            _controller.SetSelectByToOff();
            //
            pgProperties.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            //
            return true;
        }
        //
        private void Cancel()
        {
        }

        
        //



    }
}
