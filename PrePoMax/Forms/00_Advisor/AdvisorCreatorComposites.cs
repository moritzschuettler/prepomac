using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserControls;

namespace PrePoMax
{
    [Serializable]
    public static class AdvisorCreatorComposites
    {
        // Methods
        public static AdvisorPanel CreatePanel(FrmMain frmMain)
        {
            AdvisorPanel advisorControl = new AdvisorPanel(frmMain.tsmiAdvisorComposites_Click, frmMain.ModelTree_ViewEvent);
            advisorControl.AddPage(Introduction(frmMain));
            advisorControl.AddPage(ImportGeometry(frmMain));
            advisorControl.AddPage(PrepareFeMesh(frmMain));
            advisorControl.AddPage(AssignMaterialProperties(frmMain));
            advisorControl.AddPage(CreateCompositeLayup(frmMain));
            advisorControl.AddPage(SimulateDraping(frmMain));
            advisorControl.AddPage(SelectTheAnalysisType(frmMain));
            advisorControl.AddPage(DefineBcAndLoads(frmMain));
            advisorControl.AddPage(RunTheAnalysis(frmMain));
            advisorControl.AddPage(Results(frmMain));
            //
            ViewType viewType;
            if (frmMain.GetCurrentView() == ViewGeometryModelResults.Geometry) viewType = ViewType.Geometry;
            else if (frmMain.GetCurrentView() == ViewGeometryModelResults.Model) viewType = ViewType.Model;
            else if (frmMain.GetCurrentView() == ViewGeometryModelResults.Results) viewType = ViewType.Results;
            else throw new NotSupportedException();
            //
            advisorControl.PrepareControls(viewType);
            //
            return advisorControl;
        }
        //
        private static PanelPage Introduction(FrmMain frmMain)
        {
            PanelPage advisorPage = new PanelPage();
            advisorPage.Id = "composites_introduction";
            advisorPage.Title = "Introduction";
            advisorPage.AssociatedView = ViewType.Geometry;
            //
            PanelItemLabel advisorLabel1 = new PanelItemLabel();
            advisorLabel1.Text = "This advisor guides you through the creation of a composite finite element (FE) model using PrePoMax.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "The composites workflow allows you to define layered shell structures with multiple plies, each with its own material, thickness, and fiber orientation angle.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Follow the steps in this advisor to set up your composite model from geometry import to results inspection.";
            advisorPage.Items.Add(advisorLabel1);
            //
            return advisorPage;
        }
        private static PanelPage ImportGeometry(FrmMain frmMain)
        {
            PanelPage advisorPage = new PanelPage();
            advisorPage.Id = "composites_import_geometry";
            advisorPage.Title = "Import Geometry";
            advisorPage.AssociatedView = ViewType.Geometry;
            //
            PanelItemLabel advisorLabel1 = new PanelItemLabel();
            advisorLabel1.Text = "The geometry of the composite structure can be imported from CAD programs using .stp/.step or .stl file formats.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "For composites, the geometry is typically a surface or thin shell representing the mid-plane of the laminate.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Import your geometry and verify in the model tree that the correct part was loaded.";
            advisorPage.Items.Add(advisorLabel1);
            //
            return advisorPage;
        }
        private static PanelPage PrepareFeMesh(FrmMain frmMain)
        {
            PanelPage advisorPage = new PanelPage();
            advisorPage.Id = "composites_prepare_fe_mesh";
            advisorPage.Title = "Prepare the FE Mesh";
            advisorPage.AssociatedView = ViewType.Geometry;
            //
            PanelItemLabel advisorLabel1 = new PanelItemLabel();
            advisorLabel1.Text = "The geometry must be discretized into finite elements before analysis. For composite shell structures, only S8R (parabolic 8-nodes quads, reduced integration) and S6 (parabolic 6-nodes trias) shell elements are supported.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Mesh quality is important for composites: a fine, well-shaped mesh improves the accuracy of stress and failure predictions through the thickness.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Generate the mesh and inspect it to ensure element quality is acceptable.";
            advisorPage.Items.Add(advisorLabel1);
            //
            return advisorPage;
        }
        private static PanelPage AssignMaterialProperties(FrmMain frmMain)
        {
            PanelPage advisorPage = new PanelPage();
            advisorPage.Id = "composites_assign_material_properties";
            advisorPage.Title = "Assign Material Properties";
            advisorPage.AssociatedView = ViewType.Model;
            //
            PanelItemLabel advisorLabel1 = new PanelItemLabel();
            advisorLabel1.Text = "Composite structures are made of one or more ply materials. Each ply in the laminate can have a different material.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Create the orthotropic material models that will be used in the composite layup. Define the engineering constants (E1, E2, G12, nu12, ...) for each material.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Materials can be stored in or retrieved from the material library for reuse across models.";
            advisorPage.Items.Add(advisorLabel1);
            //
            return advisorPage;
        }
        private static PanelPage CreateCompositeLayup(FrmMain frmMain)
        {
            PanelPage advisorPage = new PanelPage();
            advisorPage.Id = "composites_create_composite_layup";
            advisorPage.Title = "Create Composite Layup";
            advisorPage.AssociatedView = ViewType.Model;
            //
            PanelItemLabel advisorLabel1 = new PanelItemLabel();
            advisorLabel1.Text = "The composite layup defines the stacking sequence of plies that make up the laminate. Each ply is characterized by its material, thickness, and fiber orientation angle.";
            advisorLabel1.Text = "Only conventionnal shell element types are supported for composites today.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "First of all, the Layup orientation must be defined. Choose between modes:\n";
            advisorLabel1.Text += " - Part global: use global part axis system as reference\n";
            advisorLabel1.Text += " - Coordinate system: if you need to define a rotation from part global frame\n";
            advisorLabel1.Text += " - Local element: if you want to define a local orientation for each element (e.g. based on the geometry curvature). In this case, the fiber direction is defined by the local element axis 1.";

            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Create a composite layup and add plies in the desired stacking order. A typical symmetric laminate uses ply angle pairs such as [0/45/-45/90]s.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Each ply can be assigned to a specific region of the mesh, allowing for variable thickness and orientation across the structure.";
            advisorLabel1.Text += "You can check ply by ply the fiber direction orientation on the 3D.";
            advisorPage.Items.Add(advisorLabel1);
            //
            return advisorPage;
        }
        private static PanelPage SimulateDraping(FrmMain frmMain)
        {
            PanelPage advisorPage = new PanelPage();
            advisorPage.Id = "composites_simulate_draping";
            advisorPage.Title = "Simulate Draping";
            advisorPage.AssociatedView = ViewType.Model;
            //
            PanelItemLabel advisorLabel1 = new PanelItemLabel();
            advisorLabel1.Text = "Draping simulation predicts how a flat fabric will conform to a curved surface. This is important for manufacturing feasibility.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "The kinematic draping algorithm computes the shear deformation that occurs as the fabric is draped over the geometry. High shear angles indicate areas where the fabric may wrinkle or require cutting darts.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Set the limit shear angle and cell size in the layup form, then enable the draping simulation to visualize the shear distribution.\n";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Optionally, user can map the draping simulation results (shearing angles / fiber directions) to the structural mesh, for more accurate analysis results.";
            advisorPage.Items.Add(advisorLabel1);
            //
            return advisorPage;
        }
        private static PanelPage SelectTheAnalysisType(FrmMain frmMain)
        {
            PanelPage advisorPage = new PanelPage();
            advisorPage.Id = "composites_select_the_analysis_type";
            advisorPage.Title = "Select the Analysis Type";
            advisorPage.AssociatedView = ViewType.Model;
            //
            PanelItemLabel advisorLabel1 = new PanelItemLabel();
            advisorLabel1.Text = "Several analysis types are available for composite models.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Static linear analysis computes displacements, stresses, and strains in each ply under the applied loads. This is the most common analysis for composite structures.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Frequency analysis determines the natural frequencies and mode shapes, which is useful for vibration-critical composite panels and structures.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Buckling analysis computes the buckling load factor, important for thin composite structures under compressive or shear loads.";
            advisorPage.Items.Add(advisorLabel1);
            //
            return advisorPage;
        }
        private static PanelPage DefineBcAndLoads(FrmMain frmMain)
        {
            PanelPage advisorPage = new PanelPage();
            advisorPage.Id = "composites_define_bc_and_loads";
            advisorPage.Title = "Define BC and Loads";
            advisorPage.AssociatedView = ViewType.Model;
            //
            PanelItemLabel advisorLabel1 = new PanelItemLabel();
            advisorLabel1.Text = "Boundary conditions and loads must be applied to the FE model to represent the physical constraints and loading scenario.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "For composite shells, boundary conditions are typically applied to edges or specific node sets. Common types include fixed supports, pinned supports, and symmetry conditions.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Loads can be applied as surface tractions, pressures, or concentrated forces. For composite panels, pressure loads and in-plane forces are common.";
            advisorPage.Items.Add(advisorLabel1);
            //
            return advisorPage;
        }
        private static PanelPage RunTheAnalysis(FrmMain frmMain)
        {
            PanelPage advisorPage = new PanelPage();
            advisorPage.Id = "composites_run_the_analysis";
            advisorPage.Title = "Run the Analysis";
            advisorPage.AssociatedView = ViewType.Model;
            //
            PanelItemLabel advisorLabel1 = new PanelItemLabel();
            advisorLabel1.Text = "Before running the analysis, verify that all model components are correctly defined: geometry, mesh, materials, composite layup, boundary conditions, and loads.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Run the analysis and monitor the solver output in the Monitor window. For composite models, check that the solver reports no warnings about element formulation or material data.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "After the analysis completes, load the results to inspect the ply-level response.";
            advisorPage.Items.Add(advisorLabel1);
            //
            return advisorPage;
        }
        private static PanelPage Results(FrmMain frmMain)
        {
            PanelPage advisorPage = new PanelPage();
            advisorPage.Id = "composites_results";
            advisorPage.Title = "Results";
            advisorPage.AssociatedView = ViewType.Results;
            //
            PanelItemLabel advisorLabel1 = new PanelItemLabel();
            advisorLabel1.Text = "Composite analysis results provide insight into the structural response at both the laminate and ply levels.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Displacements (DISP) and stresses (STRESS) can be inspected for each ply. Ply stresses in the material coordinate system (S11, S22, S12) are essential for failure index calculations.";
            advisorLabel1.Text += "You can create a Field Output request for failure criteria (e.g. Haschin, Tsai-Hill,..), then define material strength parameters.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Identify critical plies and regions where stresses approach material strength limits. This information guides design improvements such as adding plies, changing orientations, or reinforcing local areas.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Save the model when you are satisfied with the results.";
            advisorPage.Items.Add(advisorLabel1);
            //
            return advisorPage;
        }
    }
}
