using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CaeGlobals;
using CaeModel;

namespace PrePoMax.Forms
{
    class FrmExtrudeShellFromSections : UserControls.FrmProperties, IFormBase
    {
        // Variables
        private string _shellPartName;
        private ViewExtrudeShellFromSections _viewData;
        private Controller _controller;


        // Constructors
        public FrmExtrudeShellFromSections(Controller controller)
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
            // FrmExtrudeShellFromSections
            //
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Name = "FrmExtrudeShellFromSections";
            this.Text = "Extrude Shell Mesh from Sections";
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
            CompositeLayup layup = _controller.GetCompositeLayupForShellPart(shellPartName);
            if (layup == null)
            {
                MessageBox.Show("No composite layup found for part '" + shellPartName + "'.\n" +
                                "Assign a composite layup with at least one ply before using this command.",
                                "Extrude Shell Mesh from Sections", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            //
            List<Ply> plies = layup.Symmetric
                ? GetSymmetricPlies(layup.Plies)
                : new List<Ply>(layup.Plies);
            //
            _viewData = new ViewExtrudeShellFromSections(shellPartName, layup.Name, plies.Count,
                                                         layup.ShellOffset.ToString());
            propertyGrid.SelectedObject = _viewData;
            //
            return true;
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _viewData = (ViewExtrudeShellFromSections)propertyGrid.SelectedObject;
            //
            if (string.IsNullOrWhiteSpace(_viewData.Name))
                throw new CaeException("The new part name must not be empty.");
            //
            _controller.ExtrudeShellFromSectionsCommand(_shellPartName, _viewData.Name,
                                                        _viewData.CondenseNodes, _viewData.CondenseTolerance);
        }


        // Helpers
        private List<Ply> GetSymmetricPlies(List<Ply> plies)
        {
            var result = new List<Ply>(plies);
            for (int i = plies.Count - 1; i >= 0; i--)
                result.Add(plies[i]);
            return result;
        }
    }
}
