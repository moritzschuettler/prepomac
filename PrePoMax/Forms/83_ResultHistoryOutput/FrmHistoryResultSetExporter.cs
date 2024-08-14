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
    class FrmHistoryResultSetExporter : FrmProperties, IFormBase
    {
        // Variables                                                                                                                
        private ViewHistoryResultSetExporter _viewHistoryResultSetExporter;
        private string _workDirectory;
        private Controller _controller;


        // Properties                                                                                                               
        public HistoryResultSetExporter HistoryResultSetExporter
        {
            get { return _viewHistoryResultSetExporter.GetBase(); }
            set
            {
                HistoryResultSetExporter exporter = value.DeepClone();
                exporter.WorkingDirectory = _workDirectory;
                _viewHistoryResultSetExporter = new ViewHistoryResultSetExporter(exporter);
            }
        }
       

        // Constructors                                                                                                             
        public FrmHistoryResultSetExporter(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewHistoryResultSetExporter = null;
        }
        private void InitializeComponent()
        {
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // FrmReferencePoint
            // 
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Name = "FrmHistoryResultSetExporter";
            this.Text = "Export History Outputs";
            this.Controls.SetChildIndex(this.gbProperties, 0);
            this.Controls.SetChildIndex(this.btnCancel, 0);
            this.Controls.SetChildIndex(this.btnOK, 0);
            this.Controls.SetChildIndex(this.btnOkAddNew, 0);
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);
        }


        // Overrides                                                                                                                
        protected override void OnPropertyGridPropertyValueChanged()
        {
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnPropertyGridSelectedGridItemChanged()
        {
            object value = propertyGrid.SelectedGridItem.Value;
            if (value != null) { }
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _viewHistoryResultSetExporter = (ViewHistoryResultSetExporter)propertyGrid.SelectedObject;
            //
            if (HistoryResultSetExporter.FileName == null)
                throw new CaeException("The file name to export to is missing.");
            try
            {
                if (File.Exists(HistoryResultSetExporter.FileName)) File.Delete(HistoryResultSetExporter.FileName);
            }
            catch (Exception ex)
            {
                throw new CaeException(ex.Message);
            }
            // Create
            if (_viewHistoryResultSetExporter != null)
            {
                _controller.ExportResultHistoryOutputCommand(HistoryResultSetExporter);
            }
        }
        protected override bool OnPrepareForm(string stepName, string historyResultSetExporterToEditName)
        {
            this.btnOkAddNew.Visible = false;
            //
            _propertyItemChanged = false;
            _viewHistoryResultSetExporter = null;
            _workDirectory = _controller.Settings.GetWorkDirectory();
            string[] historyResultSets = _controller.GetHistoryResultSetNames();
            //
            if (historyResultSets == null || historyResultSets.Length == 0)
                throw new CaeException("There are no history outputs to export.");
            // Create new exporter
            if (_viewHistoryResultSetExporter == null)
            {
                HistoryResultSetExporter = new HistoryResultSetExporter("");
                _viewHistoryResultSetExporter.PopulateDropDownLists(historyResultSets);
            }
            //
            propertyGrid.SelectedObject = _viewHistoryResultSetExporter;
            propertyGrid.Select();
            //
            return true;
        }
        

        // Methods                                                                                                                  
        
        
    }
}
