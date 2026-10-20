// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
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
    public class AdvisorCreatorComposites
    {
        // Variables


        // Properties


        // Constructors
        public AdvisorCreatorComposites()
        {

        }


        // Methods
        public static AdvisorPanel CreatePanel(FrmMain frmMain)
        {
            AdvisorPanel advisorControl = new AdvisorPanel(frmMain.tsmiAdvisorComposites_Click, frmMain.ModelTree_ViewEvent);
            advisorControl.AddPage(Introduction(frmMain));
            advisorControl.AddPage(ImportGeometry(frmMain));
            advisorControl.AddPage(PrepareFeMesh(frmMain));
            advisorControl.AddPage(DefineRosettes(frmMain));
            advisorControl.AddPage(AssignMaterialProperties(frmMain));
            advisorControl.AddPage(CreateCompositeLayup(frmMain));
            advisorControl.AddPage(SimulateDraping(frmMain));
            advisorControl.AddPage(MapManufacturingData(frmMain));
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
            PanelItemImage advisorImage1 = new PanelItemImage(Properties.Resources.Advisor_Stacking_Sequence);
            advisorPage.Items.Add(advisorImage1);
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
        private static PanelPage DefineRosettes(FrmMain frmMain)
        {
            PanelPage advisorPage = new PanelPage();
            advisorPage.Id = "composites_define_rosettes";
            advisorPage.Title = "Define Rosettes";
            advisorPage.AssociatedView = ViewType.Geometry;
            //
            PanelItemLabel advisorLabel1 = new PanelItemLabel();
            advisorLabel1.Text = "A rosette defines the material reference direction, i.e. the direction from which the ply angles are measured. It replaces the single constant direction that is otherwise derived from the part global frame, and it can follow the shape of the part, which is what a curved composite component usually requires.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Rosettes are collected under the Rosettes node of the geometry tree. They are geometric objects and need no mesh, so they can be created as soon as the geometry is imported.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Two types are available:\n";
            advisorLabel1.Text += " - Cartesian: a constant reference direction given by the X and Y axes you enter. Use Normalize and Orthogon. to clean up the axes.\n";
            advisorLabel1.Text += " - Guided by curves: the reference direction follows a set of guide curves. For every element, the direction is taken from the nearest point of the nearest curve, so the fibers follow the geometry.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Guide curves are imported with Import curves from STEP..., also available from the context menu of a rosette. The chord tolerance controls how finely the curves are tessellated; leave it at 0 to let PrePoMax derive it from the size of the imported geometry. Each curve can be included or excluded, and reversed when it points the wrong way.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Choose 0° guided when the curves run along the fiber direction, and 90° guided when they run across it. The direction tolerance groups elements whose reference directions are close enough to share one orientation in the solver input; a larger value produces fewer element sets and a smaller input file, a smaller value follows the curves more closely.";
            advisorPage.Items.Add(advisorLabel1);
            //
            PanelItemLinkLabel advisorLinkLabel1 = new PanelItemLinkLabel();
            advisorLinkLabel1.Text = "1 Create rosette";
            advisorLinkLabel1.AddAction(frmMain.tsmiCreateRosette_Click);
            advisorLinkLabel1.IndentLevel = 0;
            advisorPage.Items.Add(advisorLinkLabel1);
            //
            PanelItemLabel advisorLabel2 = new PanelItemLabel();
            advisorLabel2.Text = "The rosette curves and the resulting directions are previewed in the 3D view while the rosette form is open, so you can check the orientation before using it.";
            advisorLabel2.Text += "\n\n";
            advisorLabel2.Text += "A rosette is used by a composite layup in two ways. Set the Layup orientation definition to Rosette and pick the rosette in the Rosette list to apply it to every ply of the layup. A single ply can also use a rosette of its own: right-click the Orientation column of the plies table and use Assign orientations from rosette..., Reset to layup rosette or Clear rosette (use angle). A ply rosette always wins over the layup rosette, and a ply that has none follows the layup.";
            advisorLabel2.Text += "\n\n";
            advisorLabel2.Text += "The ply angle is still applied on top of the rosette: the rosette only says where 0° points.";
            advisorPage.Items.Add(advisorLabel2);
            //
            PanelItemImage advisorImage1 = new PanelItemImage(Properties.Resources.Advisor_Rosette);
            advisorPage.Items.Add(advisorImage1);
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
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Only conventionnal shell element types are supported for composites today.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "First of all, the Layup orientation must be defined. Choose between modes:\n";
            advisorLabel1.Text += " - Part global: use global part axis system as reference\n";
            advisorLabel1.Text += " - Coordinate system: if you need to define a rotation from part global frame\n";
            advisorLabel1.Text += " - Rosette: take the reference direction from a rosette, so that it can follow the geometry element by element. Select the rosette of the layup in the Rosette list, or assign a rosette to a single ply from the context menu of the Orientation column.\n";
            advisorLabel1.Text += " - Discrete orientations: read the reference direction of each element from an imported manufacturing deposition path (see the Data Mapping page).";

            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Create a composite layup and add plies in the desired stacking order. A typical symmetric laminate uses ply angle pairs such as [0/45/-45/90]s.";
            advisorLabel1.Text += "\n\n";
            advisorLabel1.Text += "Each ply can be assigned to a specific region of the mesh, allowing for variable thickness and orientation across the structure.";
            advisorLabel1.Text += "You can check ply by ply the fiber direction orientation on the 3D.";
            advisorPage.Items.Add(advisorLabel1);
            //
            PanelItemLabel advisorLabel2 = new PanelItemLabel();
            advisorLabel2.Text = "The Process type states how the laminate is manufactured. It does not change the stiffness that is written to the solver: it tells PrePoMax which manufacturing tools are relevant for this layup.\n";
            advisorLabel2.Text += " - None: no manufacturing process declared. This is the default and is the right choice for a purely structural study.\n";
            advisorLabel2.Text += " - Hand layup: plies are draped by hand. The draping simulation of the next page is the tool used to check whether the fabric can conform to the surface.\n";
            advisorLabel2.Text += " - Tape placement: automated fiber or tape placement. Selecting it reveals the Tape Propagation section, where each ply is given a seed point, a propagation path type and a tape width, and the tape course is propagated over the part from that seed.\n";
            advisorLabel2.Text += " - Winding: filament winding.\n";
            advisorLabel2.Text += " - Braiding: braided preform.";
            advisorLabel2.Text += "\n\n";
            advisorLabel2.Text += "Winding and braiding are declarative for the moment: they record the intended process with the layup but do not yet drive a dedicated simulation.";
            advisorPage.Items.Add(advisorLabel2);
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
            PanelItemImage advisorImage1 = new PanelItemImage(Properties.Resources.Advisor_Draping);
            advisorPage.Items.Add(advisorImage1);
            //
            return advisorPage;
        }
        private static PanelPage MapManufacturingData(FrmMain frmMain)
        {
            PanelPage advisorPage = new PanelPage();
            advisorPage.Id = "composites_map_manufacturing_data";
            advisorPage.Title = "Data Mapping [BETA]";
            advisorPage.AssociatedView = ViewType.Model;
            //
            PanelItemLabel advisorLabel1 = new PanelItemLabel();
            advisorLabel1.Text = "[BETA] The data mapping tool is still under development. The supported file formats and the mapping results may change in a future version, so always inspect the imported layup before running an analysis.";
            advisorPage.Items.Add(advisorLabel1);
            //
            PanelItemLabel advisorLabel2 = new PanelItemLabel();
            advisorLabel2.Text = "The data mapping tool brings the layup produced by a manufacturing or draping software into the FE model, so that the analysis uses the as-manufactured ply shapes and fiber directions instead of nominal ply angles. The imported data does not have to share the mesh of the model: the mapping transfers it onto the FE mesh you meshed yourself.";
            advisorLabel2.Text += "\n\n";
            advisorLabel2.Text += "Two sources are supported.";
            advisorPage.Items.Add(advisorLabel2);
            //
            PanelItemLabel advisorLabel3 = new PanelItemLabel();
            advisorLabel3.Text = "1) HDF5 layup file (Composite CAE format, written among others by ANSYS ACP and 9T Labs). The file carries the ply surfaces with their own triangle meshes, the layer numbers, the deposition angles, the fabric material data and the element sets.";
            advisorLabel3.Text += "\n\n";
            advisorLabel3.Text += "PrePoMax reads the plies in stacking order, creates the corresponding materials and ply regions, and maps every ply surface onto the shell elements by core sampling: a line is drilled from the centroid of each element along its normal, and each ply surface crossed within the sampling depth is added to the stack of that element. A ply made of separate tow paths therefore lands on exactly the elements it covers.";
            advisorLabel3.Text += "\n\n";
            advisorLabel3.Text += "If the model has no mesh yet, a mesh is built from the ply surfaces of the file itself, so that an HDF5 file can be inspected on its own.";
            advisorPage.Items.Add(advisorLabel3);
            //
            PanelItemLinkLabel advisorLinkLabel1 = new PanelItemLinkLabel();
            advisorLinkLabel1.Text = "1 Import composite layup from HDF5";
            advisorLinkLabel1.AddAction(frmMain.tsmiImportCompositeLayupFromH5_Click);
            advisorLinkLabel1.IndentLevel = 0;
            advisorPage.Items.Add(advisorLinkLabel1);
            //
            PanelItemLabel advisorLabel4 = new PanelItemLabel();
            advisorLabel4.Text = "2) XML deposition path file. The file describes stacking points, each holding a position, the material reference direction at that position and the angle and thickness of every layer deposited there.";
            advisorLabel4.Text += "\n\n";
            advisorLabel4.Text += "This file is imported from the composite layup form itself: set the Layup orientation definition to Discrete orientations and browse for the XML file. The content is stored inside the layup, so the model no longer depends on the file afterwards.";
            advisorLabel4.Text += "\n\n";
            advisorLabel4.Text += "When the input deck is written, every element picks up the data of the nearest stacking point, elements sharing the same direction are grouped, and one orientation and one composite section are written per group. Layered solid layups are supported as well, each ply layer taking the angle of its own stacking point.";
            advisorPage.Items.Add(advisorLabel4);
            //
            PanelItemLabel advisorLabel5 = new PanelItemLabel();
            advisorLabel5.Text = "Both files carry their own length unit. Check that it matches the unit system of the model, and after the import compare the number of mapped plies and the ply regions in the 3D view against the source data.";
            advisorLabel5.Text += "\n\n";
            advisorLabel5.Text += "When several sources of fiber direction are defined for the same layup, the draping results are used first, then the discrete orientations, then the rosettes, and finally the constant direction of the layup frame.";
            advisorPage.Items.Add(advisorLabel5);
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
