// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.IO;
using System.Windows.Forms;
using CaeGlobals;

namespace PrePoMax.Forms
{
    // The settings the toolbar button decides on its own. The form is not modal, so the model can
    // still be turned, zoomed and replotted between opening it and taking the picture, and OK leaves
    // it open, so a run of pictures of one model needs no typing at all.
    class FrmCaptureViewport : UserControls.FrmProperties, IFormBase
    {
        // Variables                                                                                                                
        private ViewCaptureViewportProperties _viewCaptureViewportProperties;


        // Callbacks                                                                                                                
        public Func<CaptureViewportProperties> Form_GetDefaultProperties;
        public Action<string, bool, int> Form_Capture;


        // Constructors                                                                                                             
        public FrmCaptureViewport()
            : base(1.6)
        {
            InitializeComponent();
            //
            _viewCaptureViewportProperties = null;
        }
        private void InitializeComponent()
        {
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            //
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Name = "FrmCaptureViewport";
            this.Text = "Capture Viewport";
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);
        }


        // Overrides                                                                                                                
        protected override bool OnPrepareForm(string stepName, string itemToEditName)
        {
            _propertyItemChanged = false;
            propertyGrid.SelectedObject = null;
            // OK leaves the form open and prepares it again, and the folder and the name the user set
            // there are theirs to keep. A form that is not visible yet is being opened, and that
            // starts from the folder and the name of the model again
            if (!Visible || _viewCaptureViewportProperties == null)
                _viewCaptureViewportProperties = new ViewCaptureViewportProperties(Form_GetDefaultProperties());
            //
            propertyGrid.SelectedObject = _viewCaptureViewportProperties;
            propertyGrid.Select();
            //
            return true;
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _viewCaptureViewportProperties = (ViewCaptureViewportProperties)propertyGrid.SelectedObject;
            CaptureViewportProperties properties = _viewCaptureViewportProperties.GetBase();
            string fileName = properties.GetFileName();
            // A numbered name is free by the time it is returned, so this only ever asks about a name
            // the user chose to keep as it is
            if (File.Exists(fileName) &&
                MessageBoxes.ShowWarningQuestionYesNo("The file '" + fileName + "' already exists. Replace it?") !=
                DialogResult.Yes)
                throw new CaeException("BreakOnApply");
            //
            if (Form_Capture != null)
                Form_Capture(fileName, properties.TransparentBackground, properties.Magnification);
        }
    }
}
