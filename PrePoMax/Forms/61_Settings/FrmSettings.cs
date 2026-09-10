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
using CaeMesh;
using System.Reflection;
using System.IO;
using CaeGlobals;
using PrePoMax.Settings;
using System.Runtime.InteropServices;
using UserControls;

namespace PrePoMax.Forms
{
    public partial class FrmSettings : PrePoMaxChildForm
    {
        // Variables                                                                                                                
        private string _previousSettings;
        private Dictionary<string, IViewSettings> _viewSettings;
        private double _labelRatio = 2.3;


        // Properties                                                                                                               
        public Dictionary<string, ISettings> Settings 
        {
            get
            {
                Dictionary<string, ISettings> result = new Dictionary<string, ISettings>();
                foreach (var entry in _viewSettings)
                {
                    result.Add(entry.Key, entry.Value.GetBase());
                }
                return result;
            }
            set 
            {
                //create a clone
                _viewSettings = new Dictionary<string, IViewSettings>();
                foreach (var entry in value)
                {
                    if (entry.Value is GeneralSettings ges)
                        _viewSettings.Add(entry.Key, new ViewGeneralSettings(ges.DeepClone()));
                    else if (entry.Value is NumberFormatSettings nfs)
                        _viewSettings.Add(entry.Key, new ViewNumberFormatSettings(nfs.DeepClone()));
                    else if (entry.Value is GraphicsSettings grs)
                        _viewSettings.Add(entry.Key, new ViewGraphicsSettings(grs.DeepClone()));
                    else if (entry.Value is NavigationSettings nas)
                        _viewSettings.Add(entry.Key, new ViewNavigationSettings(nas.DeepClone()));
                    else if (entry.Value is ColorSettings cos)
                        _viewSettings.Add(entry.Key, new ViewColorSettings(cos.DeepClone()));
                    else if (entry.Value is AnnotationSettings wis)
                        _viewSettings.Add(entry.Key, new ViewAnnotationSettings(wis.DeepClone()));
                    else if (entry.Value is MeshingSettings ms)
                        _viewSettings.Add(entry.Key, new ViewMeshingSettings(ms.DeepClone()));
                    else if (entry.Value is PreSettings prs)
                        _viewSettings.Add(entry.Key, new ViewPreSettings(prs.DeepClone()));
                    else if (entry.Value is SolverSettings ss)
                        _viewSettings.Add(entry.Key, new ViewSolverSettings(ss.DeepClone()));
                    else if (entry.Value is PostSettings pos)
                        _viewSettings.Add(entry.Key, new ViewPostSettings(pos.DeepClone()));
                    else if (entry.Value is LegendSettings les)
                        _viewSettings.Add(entry.Key, new ViewLegendSettings(les.DeepClone()));
                    else if (entry.Value is StatusBlockSettings sbs)
                        _viewSettings.Add(entry.Key, new ViewStatusBlockSettings(sbs.DeepClone()));
                    else if (entry.Value is AutomationSettings aus)
                        _viewSettings.Add(entry.Key, new ViewAutomationSettings(aus.DeepClone()));
                    else throw new NotSupportedException();
                }
            } 
        }


        // Events                                                                                                                   
        public event Action<SettingsContainer> UpdateSettings;
        // The dialog makes the value, because generating it is the user's action here, and the main
        // form keeps it and restarts the server: the server belongs to the main form, not to a dialog
        // that may be cancelled. An empty value means the token was cleared
        public event Action<string> AutomationAccessTokenChanged;


        // Constructors                                                                                                             
        public FrmSettings()
        {
            InitializeComponent();
            _previousSettings = null;
            _viewSettings = null;
            //
            propertyGrid.SetLabelColumnWidth(_labelRatio);
            propertyGrid.SetSelectFirstCategory();
            propertyGrid.FocusOnSelectedObjectChanged = false;
        }


        // Event handlers                                                                                                           
        private void lvSettings_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvSettings.SelectedItems.Count > 0)
            {
                propertyGrid.SelectedObject = lvSettings.SelectedItems[0].Tag;
                //
                _previousSettings = lvSettings.SelectedItems[0].Text;
                //
                lvSettings.Focus();
            }
        }
        private void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            propertyGrid.Refresh();
            _propertyItemChanged = true;
        }
        private void cmsPropertyGrid_Opening(object sender, CancelEventArgs e)
        {
            if (propertyGrid.SelectedObject is IReset)
                tsmiResetAll.Enabled = true;
            else
                tsmiResetAll.Enabled = false;
            // Hidden rather than disabled, unlike Reset all: they apply to one page of thirteen, and
            // a permanently greyed entry on every other page reads as something that is broken
            ViewAutomationSettings automation = propertyGrid.SelectedObject as ViewAutomationSettings;
            tsmiGenerateAccessToken.Visible = automation != null;
            tsmiClearAccessToken.Visible = automation != null;
            if (automation != null) tsmiClearAccessToken.Enabled = automation.AccessToken.Length > 0;
        }
        private void tsmiGenerateAccessToken_Click(object sender, EventArgs e)
        {
            ViewAutomationSettings automation = propertyGrid.SelectedObject as ViewAutomationSettings;
            if (automation == null) return;
            //
            if (automation.AccessToken.Length > 0)
            {
                string question = "Replace the access token of the automation server?" + Environment.NewLine +
                                  Environment.NewLine +
                                  "Every AI assistant that was set up with the current token will have to be " +
                                  "given the new one.";
                if (MessageBoxes.ShowQuestionYesNo("Automation", question) != DialogResult.Yes) return;
            }
            SetAccessToken(automation, Mcp.McpAccessToken.Generate());
        }
        private void tsmiClearAccessToken_Click(object sender, EventArgs e)
        {
            ViewAutomationSettings automation = propertyGrid.SelectedObject as ViewAutomationSettings;
            if (automation == null || automation.AccessToken.Length == 0) return;
            //
            string question = "Clear the access token of the automation server?" + Environment.NewLine +
                              Environment.NewLine +
                              "The server cannot run without one, and every AI assistant that was set up with it " +
                              "stops working. Do this before preparing a redistributable, so that the settings " +
                              "file does not carry a token.";
            if (MessageBoxes.ShowQuestionYesNo("Automation", question) != DialogResult.Yes) return;
            //
            SetAccessToken(automation, "");
        }
        // The value goes into the settings being edited so the grid shows it, and to the main form so
        // that it is kept whether or not this dialog is accepted: it is an action, not an edit
        private void SetAccessToken(ViewAutomationSettings automation, string token)
        {
            automation.AccessToken = token;
            propertyGrid.Refresh();
            //
            Action<string> changed = AutomationAccessTokenChanged;
            if (changed != null) changed(token);
        }
        private void tsmiResetAll_Click(object sender, EventArgs e)
        {
            if (propertyGrid.SelectedObject is IReset resetObject)
            {
                resetObject.Reset();
                propertyGrid.Refresh();
                _propertyItemChanged = true;
            }
        }
        private void btnApply_Click(object sender, EventArgs e)
        {
            Apply();
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                if (Apply())
                {
                    //
                    DialogResult = DialogResult.OK;
                    Hide();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            Hide();
        }
        private void FrmSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }


        // Methods                                                                                                                  
        public void PrepareForm(Controller controller)
        {
            _propertyItemChanged = false;
            _viewSettings = null;
            lvSettings.Items.Clear();
            propertyGrid.SelectedObject = null;
            //
            if (controller.Settings == null)
            {
                throw new Exception("There are no settings to show.");
            }
            else
            {
                Settings = controller.Settings.ToDictionary();
                //
                ListViewItem lvi;
                foreach (var entry in _viewSettings)
                {
                    lvi = lvSettings.Items.Add(entry.Key);
                    lvi.Tag = entry.Value;
                }
            }
            // Open previously shown settings
            if (_previousSettings != null)
            {
                foreach (ListViewItem item in lvSettings.Items)
                {
                    if (item.Text == _previousSettings)
                    {
                        item.Selected = true;
                        break;
                    }
                }
            }
            else
            {
                if (lvSettings.Items.Count > 0) lvSettings.Items[0].Selected = true;
            }
            //
            controller.SetSelectByToOff();
        }
        public void SetSettingsToShow(string name)
        {
            _previousSettings = name;
        }
        public bool Apply()
        {
            Dictionary<string, ISettings> settings = Settings;
            //
            foreach (var entry in settings) entry.Value.CheckValues();
            //
            SettingsContainer settingsContainer = new SettingsContainer(settings);
            string workDirectory = settingsContainer.GetWorkDirectory();
            if (workDirectory != null && workDirectory.ContainsNonEnglishCharacters())
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel(SolverSettings.NonEnglishDirectoryWarning) ==
                    DialogResult.Cancel)
                    return false;
            }
            //
            if (_propertyItemChanged)
            {
                UpdateSettings?.Invoke(settingsContainer);
                _propertyItemChanged = false;
            }
            return true;
        }
    }
}
