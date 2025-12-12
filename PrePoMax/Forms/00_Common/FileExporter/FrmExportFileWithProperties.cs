using System;
using System.Collections.Generic;
using CaeGlobals;
using System.Windows.Forms;
using System.Drawing;
using UserControls;
using CaeResults;
using System.IO;

namespace PrePoMax.Forms
{
    class FrmExportFileWithProperties : FrmProperties, IFormBase
    {
        // Variables                                                                                                                
        private ViewExportFileProperties _viewExportFileProperties;
        private string _prevDirectory;
        private Controller _controller;


        // Properties                                                                                                               
        public string PreviousDirectory { get { return _prevDirectory; } set { _prevDirectory = value; } }
        public ExportFileProperties ExportFileProperties
        {
            get { return _viewExportFileProperties == null ? null : _viewExportFileProperties.GetBase(); }
            set
            {
                CreateFileNameEditor.FileName = value.FileName;
                value.FileName = Path.Combine(_prevDirectory, Path.GetFileName(value.FileName));
                CreateFileNameEditor.Filter = value.Filter;
                //
                if (value is Export3mfFileProperties e3mf)
                    _viewExportFileProperties = new ViewExport3mfFileProperties(e3mf);
                else if (value is ExportHistoryResultSetFileProperties ehrs)
                    _viewExportFileProperties = new ViewExportHistoryResultSetFileProperties(ehrs);
                else throw new NotSupportedException();
                //
                propertyGrid.SelectedObject = _viewExportFileProperties;
                propertyGrid.Select();
            }
        }
       

        // Callbacks
        public Action<ExportFileProperties> ExportUsingFileProperties { get; set; }


        // Constructors                                                                                                             
        public FrmExportFileWithProperties(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewExportFileProperties = null;
        }
        private void InitializeComponent()
        {
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(147, 376);
            this.btnOK.Size = new System.Drawing.Size(88, 23);
            this.btnOK.Text = "Export&&Close";
            // 
            // btnCancel
            // 
            this.btnCancel.Text = "Close";
            // 
            // btnOkAddNew
            // 
            this.btnOkAddNew.Location = new System.Drawing.Point(66, 376);
            this.btnOkAddNew.Text = "Export";
            // 
            // FrmExportFileWithProperties
            // 
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Name = "FrmExportFileWithProperties";
            this.Text = "Export File";
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Overrides                                                                                                                
        protected override void OnPropertyGridPropertyValueChanged()
        {
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            if (property == nameof(ExportFileProperties.FileName))
            {
                _prevDirectory = Path.GetDirectoryName(_viewExportFileProperties.GetBase().FileName);
            }
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _viewExportFileProperties = (ViewExportFileProperties)propertyGrid.SelectedObject;
            ExportFileProperties exportFileProperties = _viewExportFileProperties.GetBase();
            //
            if (exportFileProperties.FileName == null)
                throw new CaeException("The file name to export to is missing.");
            try
            {
                //if (File.Exists(exportFileProperties.FileName)) File.Delete(exportFileProperties.FileName);
            }
            catch (Exception ex)
            {
                throw new CaeException(ex.Message);
            }
            // Create
            if (exportFileProperties != null)
            {
                if (ExportUsingFileProperties != null) ExportUsingFileProperties(exportFileProperties);
            }
        }
        protected override bool OnPrepareForm(string stepName, string itemName)
        {
            _propertyItemChanged = false;
            if (_prevDirectory == null) _prevDirectory = _controller.Settings.GetWorkDirectory();
            //
            return true;
        }
        

        // Methods                                                                                                                  
        
        
    }
}
