using System.Windows.Forms;
using CaeGlobals;

namespace PrePoMax.Forms
{
    class FrmExtrudeShellUniform : UserControls.FrmProperties, IFormBase
    {
        // Variables
        private string _shellPartName;
        private ViewExtrudeShellUniform _viewData;
        private Controller _controller;


        // Constructors
        public FrmExtrudeShellUniform(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _shellPartName = null;
            _viewData = null;
        }
        private void InitializeComponent()
        {
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            //
            // FrmExtrudeShellUniform
            //
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Name = "FrmExtrudeShellUniform";
            this.Text = "Create Solids by Extrusion";
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);
        }


        // Overrides
        protected override bool OnPrepareForm(string stepName, string shellPartName)
        {
            this.btnOkAddNew.Visible = false;
            //
            _propertyItemChanged = false;
            _shellPartName = shellPartName;
            //
            _viewData = new ViewExtrudeShellUniform(shellPartName);
            propertyGrid.SelectedObject = _viewData;
            //
            return true;
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _viewData = (ViewExtrudeShellUniform)propertyGrid.SelectedObject;
            //
            if (string.IsNullOrWhiteSpace(_viewData.Name))
                throw new CaeException("The new part name must not be empty.");
            if (_viewData.TotalThickness <= 0)
                throw new CaeException("Total thickness must be greater than zero.");
            if (_viewData.NumSubdivisions < 1)
                throw new CaeException("Number of subdivisions must be at least 1.");
            //
            _controller.ExtrudeShellUniformCommand(_shellPartName, _viewData.Name,
                                                   _viewData.TotalThickness, _viewData.NumSubdivisions,
                                                   _viewData.CondenseNodes, _viewData.CondenseTolerance);
        }
    }
}
