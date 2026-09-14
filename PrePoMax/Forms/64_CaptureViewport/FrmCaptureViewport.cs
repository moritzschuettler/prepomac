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
using CaeGlobals;

namespace PrePoMax.Forms
{
    // The settings the toolbar button decides on its own. The form is not modal, so the model can
    // still be turned, zoomed and replotted between opening it and taking the picture, and OK leaves
    // it open with the next free file name filled in, so a run of pictures needs no typing.
    class FrmCaptureViewport : UserControls.FrmProperties, IFormBase
    {
        // Variables                                                                                                                
        private ViewCaptureViewportProperties _viewCaptureViewportProperties;


        // Callbacks                                                                                                                
        public Func<string, string> Form_GetNextFileName;
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
            // OK leaves the form open and prepares it again, and there the picture that was just taken
            // is what the next name is numbered from, so a folder the user chose is kept. A form that
            // is not visible yet is being opened, and that starts from the current model again
            string previousFileName = Visible && _viewCaptureViewportProperties != null
                                      ? _viewCaptureViewportProperties.FileName : null;
            string fileName = Form_GetNextFileName(previousFileName);
            //
            if (_viewCaptureViewportProperties == null)
                _viewCaptureViewportProperties =
                    new ViewCaptureViewportProperties(new CaptureViewportProperties(fileName));
            else _viewCaptureViewportProperties.FileName = fileName;
            //
            CreateFileNameEditor.FileName = fileName;
            CreateFileNameEditor.Filter = "Png files|*.png";
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
            CheckFileName(properties.FileName);
            //
            if (Form_Capture != null)
                Form_Capture(properties.FileName, properties.TransparentBackground, properties.Magnification);
        }


        // Methods                                                                                                                  
        private static void CheckFileName(string fileName)
        {
            if (fileName == null || fileName.Trim().Length == 0)
                throw new CaeException("The name of the picture file is missing.");
            if (Path.GetExtension(fileName).ToLower() != ".png")
                throw new CaeException("The viewport is captured into a .png file.");
            //
            string directory = Path.GetDirectoryName(fileName);
            if (directory == null || directory.Length == 0)
                throw new CaeException("The name of the picture file is missing its folder.");
            if (!Directory.Exists(directory))
                throw new CaeException("The folder '" + directory + "' does not exist.");
        }
    }
}
