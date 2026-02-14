using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeGlobals;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

namespace PrePoMax.Forms
{
    class FrmCompositeLayup : UserControls.FrmProperties, IFormBase
    {
  // Variables    
        private string[] _compositeLayupNames;
        private string _compositeLayupToEditName;
 private ViewCompositeLayup _viewCompositeLayup;
        private Controller _controller;
        private ContextMenuStrip cmsPropertyGrid;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem tsmiResetAll;


        // Properties      
        public CompositeLayup CompositeLayup
        {
            get { return _viewCompositeLayup?.GetBase(); }
  set { _viewCompositeLayup = new ViewCompositeLayup(value.DeepClone()); }
        }


        // Constructors            
        public FrmCompositeLayup(Controller controller)
     {
            InitializeComponent();
         //
       _controller = controller;
       _viewCompositeLayup = null;
  }
   private void InitializeComponent()
  {
            this.components = new System.ComponentModel.Container();
        this.cmsPropertyGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
   this.tsmiResetAll = new System.Windows.Forms.ToolStripMenuItem();
        this.gbProperties.SuspendLayout();
       this.cmsPropertyGrid.SuspendLayout();
            this.SuspendLayout();
 // 
            // gbProperties
        // 
        this.gbProperties.Size = new System.Drawing.Size(310, 399);
      // 
   // propertyGrid
 // 
            this.propertyGrid.ContextMenuStrip = this.cmsPropertyGrid;
            this.propertyGrid.Size = new System.Drawing.Size(298, 371);
            // 
         // btnOK
   // 
      this.btnOK.Location = new System.Drawing.Point(160, 411);
    // 
       // btnCancel
          // 
this.btnCancel.Location = new System.Drawing.Point(241, 411);
            // 
    // btnOkAddNew
  // 
   this.btnOkAddNew.Location = new System.Drawing.Point(79, 411);
      // 
 // cmsPropertyGrid
            // 
       this.cmsPropertyGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
      this.tsmiResetAll});
    this.cmsPropertyGrid.Name = "cmsPropertyGrid";
        this.cmsPropertyGrid.Size = new System.Drawing.Size(118, 26);
       // 
            // tsmiResetAll
         // 
    this.tsmiResetAll.Name = "tsmiResetAll";
          this.tsmiResetAll.Size = new System.Drawing.Size(117, 22);
this.tsmiResetAll.Text = "Reset all";
      this.tsmiResetAll.Click += new System.EventHandler(this.tsmiResetAll_Click);
    // 
   // FrmCompositeLayup
// 
       this.ClientSize = new System.Drawing.Size(334, 461);
   this.Name = "FrmCompositeLayup";
     this.Text = "Create Composite Layup";
            this.Controls.SetChildIndex(this.gbProperties, 0);
  this.Controls.SetChildIndex(this.btnCancel, 0);
        this.Controls.SetChildIndex(this.btnOK, 0);
  this.Controls.SetChildIndex(this.btnOkAddNew, 0);
     this.gbProperties.ResumeLayout(false);
       this.cmsPropertyGrid.ResumeLayout(false);
     this.ResumeLayout(false);
        }


        // Event handlers                   
      private void tsmiResetAll_Click(object sender, EventArgs e)
        {
     if (_viewCompositeLayup != null)
   {
      _viewCompositeLayup.Reset();
       propertyGrid.Refresh();
    _propertyItemChanged = true;
            }
        }


        // Overrides        
        protected override void OnPropertyGridPropertyValueChanged()
        {
         base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
    if (propertyGrid.SelectedObject == null) throw new CaeException("No composite layup was selected.");
            //
            _viewCompositeLayup = (ViewCompositeLayup)propertyGrid.SelectedObject;
    //
            if (_viewCompositeLayup == null) throw new CaeException("No composite layup was selected.");
 // Check if the name exists
            CheckName(_compositeLayupToEditName, CompositeLayup.Name, _compositeLayupNames, "composite layup");
   // Create
          if (_compositeLayupToEditName == null)
            {
        _controller.AddCompositeLayupCommand(CompositeLayup);
        }
            // Replace
            else if (_propertyItemChanged)
      {
      _controller.ReplaceCompositeLayupCommand(_compositeLayupToEditName, CompositeLayup);
 }
      }
    protected override bool OnPrepareForm(string stepName, string compositeLayupToEditName)
        {
            this.btnOkAddNew.Visible = compositeLayupToEditName == null;
            //
         _propertyItemChanged = false;
            _compositeLayupNames = null;
        _compositeLayupToEditName = null;
      _viewCompositeLayup = null;
            propertyGrid.SelectedObject = null;
     //
      _compositeLayupNames = _controller.GetCompositeLayupNames();
            _compositeLayupToEditName = compositeLayupToEditName;
            //
      if (_compositeLayupNames == null)
                throw new CaeException("The composite layup names must be defined first.");
     //
            // Create new composite layup
            if (_compositeLayupToEditName == null)
   {
      string name = GetCompositeLayupName("CompositeLayup");
                CompositeLayup cl = new CompositeLayup(name, 3, CompositeElementType.ConventionalShell);
          _viewCompositeLayup = new ViewCompositeLayup(cl);
            }
         // Edit existing composite layup
   else
    {
       CompositeLayup = _controller.GetCompositeLayup(_compositeLayupToEditName); // to clone
          }
       //
 propertyGrid.SelectedObject = _viewCompositeLayup;
            //
     _controller.SetSelectByToOff();
   //
propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterNames());
     //
   return true;
      }


        // Methods        
        private string GetCompositeLayupName(string name)
        {
            return _compositeLayupNames.GetNextNumberedKey(name);
   }
    }
}
