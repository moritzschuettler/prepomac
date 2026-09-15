// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;

namespace PrePoMax.Forms
{
    // The settings every capture is taken with, the toolbar button's included: the button takes the
    // picture this form describes and the form is where that picture is described. They are kept for
    // the session, because the form outlives the dialog the way every other form of the application
    // does. The form is not modal either, so the model can still be turned, zoomed and replotted
    // between opening it and taking the picture, and OK leaves it open.
    class FrmCaptureViewport : UserControls.FrmProperties, IFormBase
    {
        // Variables                                                                                                                
        private ViewCaptureViewportProperties _viewCaptureViewportProperties;


        // Callbacks                                                                                                                
        public Func<CaptureViewportProperties> Form_GetDefaultProperties;
        public Func<CaptureViewportProperties, bool> Form_Capture;


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
            GetProperties();
            propertyGrid.SelectedObject = _viewCaptureViewportProperties;
            propertyGrid.Select();
            //
            return true;
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _viewCaptureViewportProperties = (ViewCaptureViewportProperties)propertyGrid.SelectedObject;
            // The capture is refused when the user does not want the file replaced after all, and the
            // form stays open on the settings that asked for it
            if (!Form_Capture(_viewCaptureViewportProperties.GetBase())) throw new CaeException("BreakOnApply");
        }


        // Methods                                                                                                                  
        // The settings every capture is taken with. The toolbar button asks for them without ever
        // opening the form, so the model's own folder and name are made here rather than on showing:
        // they are the first proposal, and everything the user sets afterwards is theirs to keep
        public CaptureViewportProperties GetProperties()
        {
            if (_viewCaptureViewportProperties == null)
                _viewCaptureViewportProperties = new ViewCaptureViewportProperties(Form_GetDefaultProperties());
            //
            return _viewCaptureViewportProperties.GetBase();
        }
    }
}
