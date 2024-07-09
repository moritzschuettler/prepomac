using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using CaeModel;
using System.Windows.Forms;
using System.Drawing;
using UserControls;
using System.Xml.Linq;
using CaeResults;

namespace PrePoMax.Forms
{
    class FrmHistoryResultSetExporter : FrmProperties, IFormBase
    {
        // Variables                                                                                                                
        private ViewHistoryResultSetExporter _viewHistoryResultSetExporter;
        private Controller _controller;


        // Properties                                                                                                               
        public HistoryResultSetExporter HistoryResultSetExporter
        {
            get { return _viewHistoryResultSetExporter.GetBase(); }
            set { _viewHistoryResultSetExporter = new ViewHistoryResultSetExporter(value.DeepClone()); }
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
            // Create
            if (_viewHistoryResultSetExporter == null)
            {
                //AddReferencePointCommand(ReferencePoint);
            }
        }
        protected override bool OnPrepareForm(string stepName, string historyResultSetExporterToEditName)
        {
            this.btnOkAddNew.Visible = false;
            //
            _propertyItemChanged = false;
            _viewHistoryResultSetExporter = null;
            // Create new exporter
            if (_viewHistoryResultSetExporter == null)
            {
                HistoryResultSetExporter = new HistoryResultSetExporter("");
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
