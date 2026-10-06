// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PrePoMax.Forms
{
    class FrmSplitShell : FrmSplitSolid
    {
        // Properties
        protected override SplitTypeEnum SplitType
        {
            get { return SplitTypeEnum.Shell; }
        }


        // Constructors
        public FrmSplitShell(Controller controller)
            : base(controller)
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            this.SuspendLayout();
            //
            // FrmSplitShell
            //
            this.Name = "FrmSplitShell";
            this.Text = "Split Face";
            this.ResumeLayout(false);

        }


        // Methods
        protected override void PrepareGeometryFeatureView(ViewGeometryFeature viewGeometryFeature)
        {
            base.PrepareGeometryFeatureView(viewGeometryFeature);
            viewGeometryFeature.SetRegionSelectionText("Surfaces to split",
                                                       "Select the surfaces to split. Hold the Shift key to " +
                                                       "select more than one surface. Only the selected " +
                                                       "surfaces are divided; the part they belong to keeps " +
                                                       "its name and stays the kind of body it is.");
        }
    }
}
