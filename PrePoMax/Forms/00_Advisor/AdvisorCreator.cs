// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
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
    public static class AdvisorCreator
    {
        // Methods                                                                                                                  
        public static AdvisorPanel CreatePanel(FrmMain frmMain)
        {
            AdvisorPanel advisorPanel = new AdvisorPanel(frmMain.tsmiAdvisor_Click, frmMain.ModelTree_ViewEvent);
            advisorPanel.AddPage(Introduction(frmMain));
            advisorPanel.AddPage(ImportGeometry(frmMain));
            advisorPanel.AddPage(PrepareFeMesh(frmMain));
            advisorPanel.AddPage(AssignMaterialProperties(frmMain));
            advisorPanel.AddPage(SelectTheAnalysisType(frmMain));
            advisorPanel.AddPage(CreateTheBoundaryConditions(frmMain));
            advisorPanel.AddPage(DefineTheLoads(frmMain));
            advisorPanel.AddPage(RunTheAnalysis(frmMain));
            advisorPanel.AddPage(Results(frmMain));
            //
            ViewType viewType;
            if (frmMain.GetCurrentView() == ViewGeometryModelResults.Geometry) viewType = ViewType.Geometry;
            else if (frmMain.GetCurrentView() == ViewGeometryModelResults.Model) viewType = ViewType.Model;
            else if (frmMain.GetCurrentView() == ViewGeometryModelResults.Results) viewType = ViewType.Results;
            else throw new NotSupportedException();
            //
            advisorPanel.PrepareControls(viewType);
            //
            return advisorPanel;
        }
        //
        private static PanelPage Introduction(FrmMain frmMain)
        {
            PanelPage panelPage = new PanelPage();
            panelPage.Title = "Introduction";
            panelPage.AssociatedView = ViewType.Geometry;
            //
            PanelItemLabel panelLabel1 = new PanelItemLabel();
            panelLabel1.Text = "This advisor helps you to build a simple finite element (FE) model. It limits the FE model to a single component and a linear elastic material model.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "No constraints or contacts are currently supported by the advisor.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "Three different types of FE analysis can be performed using the advisor: static linear analysis, frequency analysis and buckling analysis.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "To create a new FE model select the option 1 and choose the desired system of units.";
            panelPage.Items.Add(panelLabel1);
            //
            PanelItemLinkLabel panelLinkLabel1 = new PanelItemLinkLabel();
            panelLinkLabel1.Text = "1  Create new FE model";
            panelLinkLabel1.AddAction(frmMain.tsmiNew_Click);
            panelLinkLabel1.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel1);
            //
            PanelItemLabel panelLabel2 = new PanelItemLabel();
            panelLabel2.Text = "After the unit system is created, select Next to continue.";
            panelPage.Items.Add(panelLabel2);
            //
            return panelPage;
        }
        private static PanelPage ImportGeometry(FrmMain frmMain)
        {
            PanelPage panelPage = new PanelPage();
            panelPage.Title = "Import Geometry";
            panelPage.AssociatedView = ViewType.Geometry;
            //
            PanelItemLabel panelLabel1 = new PanelItemLabel();
            panelLabel1.Text = "The geometry can be imported into PrePoMax from other CAD programs using .stp/.step or .stl file formats.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "To import the geometry, select the option 1 and browse for the geometry file to import. Wait until the geometry is imported and then check the model tree on the left that only one part was imported. To rename the part select the option 2 and change the Name field.";
            panelPage.Items.Add(panelLabel1);
            //
            PanelItemLinkLabel panelLinkLabel1 = new PanelItemLinkLabel();
            panelLinkLabel1.Text = "1 Import geometry";
            panelLinkLabel1.AddAction(frmMain.tsmiImportFile_Click);
            panelLinkLabel1.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel1);
            //
            PanelItemLinkLabel panelLinkLabel2 = new PanelItemLinkLabel();
            panelLinkLabel2.Text = "2 Rename geometry part";
            panelLinkLabel2.AddAction(frmMain.tsmiEditGeometryPart_Click);
            panelLinkLabel2.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel2);
            //
            PanelItemLabel panelLabel2 = new PanelItemLabel();
            panelLabel2.Text = "Select Next to continue.";
            panelPage.Items.Add(panelLabel2);
            //
            return panelPage;
        }
        private static PanelPage PrepareFeMesh(FrmMain frmMain)
        {
            PanelPage panelPage = new PanelPage();
            panelPage.Title = "Prepare the FE Mesh";
            panelPage.AssociatedView = ViewType.Geometry;
            //
            PanelItemLabel panelLabel1 = new PanelItemLabel();
            panelLabel1.Text = "The geometry must be discretized into finite elements, to perform any type of analysis in the PrePoMax. Smaller finite elements result in better accuracy but require longer computational time.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "Select the option 1 to use the default finite element size and create the FE mesh.";
            panelPage.Items.Add(panelLabel1);
            //
            PanelItemLinkLabel panelLinkLabel1 = new PanelItemLinkLabel();
            panelLinkLabel1.Text = "1 Create default mesh";
            panelLinkLabel1.AddAction(frmMain.CreateDefaultMesh);
            panelLinkLabel1.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel1);
            //
            return panelPage;
        }
        private static PanelPage AssignMaterialProperties(FrmMain frmMain)
        {
            PanelPage panelPage = new PanelPage();
            panelPage.Title = "Assign Material Properties";
            panelPage.AssociatedView = ViewType.Model;
            //
            PanelItemLabel panelLabel1 = new PanelItemLabel();
            panelLabel1.Text = "Material properties are defined using a material model. A new material model can be created using the option 1. Material models can also be stored or retrieved from the material library using the option 2.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "After the definition of the material model its properties must be assigned to the FE mesh. Use the option 3 to assign the created material model to the FE mesh. The FE mesh must be selected using the mouse.";
            panelPage.Items.Add(panelLabel1);
            //
            PanelItemLinkLabel panelLinkLabel1 = new PanelItemLinkLabel();
            panelLinkLabel1.Text = "1 Create material model";
            panelLinkLabel1.AddAction(frmMain.CreateSimpleMaterial);
            panelLinkLabel1.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel1);
            //
            PanelItemLinkLabel panelLinkLabel2 = new PanelItemLinkLabel();
            panelLinkLabel2.Text = "2 Use material library";
            panelLinkLabel2.AddAction(frmMain.tsmiMaterialLibrary_Click);
            panelLinkLabel2.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel2);
            //
            PanelItemLinkLabel panelLinkLabel3 = new PanelItemLinkLabel();
            panelLinkLabel3.Text = "3 Assign material model";
            panelLinkLabel3.AddAction(frmMain.tsmiCreateSection_Click);
            panelLinkLabel3.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel3);
            //
            PanelItemLabel panelLabel2 = new PanelItemLabel();
            panelLabel2.Text = "Select Next to continue.";
            panelPage.Items.Add(panelLabel2);
            //
            return panelPage;
        }
        private static PanelPage SelectTheAnalysisType(FrmMain frmMain)
        {
            PanelPage panelPage = new PanelPage();
            panelPage.Title = "Select the Analysis Type";
            panelPage.AssociatedView = ViewType.Model;
            //
            PanelItemLabel panelLabel1 = new PanelItemLabel();
            panelLabel1.Text = "Different analysis types can be performed using this advisor. For most cases, the default analysis settings can be used.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "If displacements, stresses and strains are to be computed, create a static linear analysis using the option 1 (Static step).";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "To find the lowest eigenfrequencies of the FE model, select the option 2 for the frequency analysis (Frequency step).";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "When the FE model is loaded in compression, buckling might occur. To determine the buckling factor choose the option 3 (Buckle step).";
            panelPage.Items.Add(panelLabel1);
            //
            PanelItemLinkLabel panelLinkLabel1 = new PanelItemLinkLabel();
            panelLinkLabel1.Text = "1 Static linear analysis";
            panelLinkLabel1.AddAction(frmMain.tsmiCreateStep_Click, null, new EventArgs<int>(0));
            panelLinkLabel1.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel1);
            //
            PanelItemLinkLabel panelLinkLabel2 = new PanelItemLinkLabel();
            panelLinkLabel2.Text = "2 Frequency analysis";
            panelLinkLabel2.AddAction(frmMain.tsmiCreateStep_Click, null, new EventArgs<int>(2));
            panelLinkLabel2.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel2);
            //
            PanelItemLinkLabel panelLinkLabel3 = new PanelItemLinkLabel();
            panelLinkLabel3.Text = "3 Buckling analysis";
            panelLinkLabel3.AddAction(frmMain.tsmiCreateStep_Click, null, new EventArgs<int>(3));
            panelLinkLabel3.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel3);
            //
            PanelItemLabel panelLabel2 = new PanelItemLabel();
            panelLabel2.Text = "Select Next to continue.";
            panelPage.Items.Add(panelLabel2);
            //
            return panelPage;
        }
        private static PanelPage CreateTheBoundaryConditions(FrmMain frmMain)
        {
            PanelPage panelPage = new PanelPage();
            panelPage.Title = "Create the Boundary Conditions";
            panelPage.AssociatedView = ViewType.Model;
            //
            PanelItemLabel panelLabel1 = new PanelItemLabel();
            panelLabel1.Text = "For a static and buckling analysis, the movement of the FE model must be prevented using a boundary condition. For a frequency analysis, the boundary condition is optional.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "To add a boundary condition that prevents the movement in all directions, choose the option 1 and then select the region to constrain.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "To individually specify the directions in which the movement is prescribed, select the option 2 and then select the region to constrain.";
            panelPage.Items.Add(panelLabel1);
            //
            PanelItemLinkLabel panelLinkLabel1 = new PanelItemLinkLabel();
            panelLinkLabel1.Text = "1 Fixed ";
            panelLinkLabel1.AddAction(frmMain.tsmiCreateBC_Click, null, new EventArgs<int>(0));
            panelLinkLabel1.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel1);
            //
            PanelItemLinkLabel panelLinkLabel2 = new PanelItemLinkLabel();
            panelLinkLabel2.Text = "2 Displacement/Rotation";
            panelLinkLabel2.AddAction(frmMain.tsmiCreateBC_Click, null, new EventArgs<int>(1));
            panelLinkLabel2.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel2);
            //
            PanelItemLabel panelLabel2 = new PanelItemLabel();
            panelLabel2.Text = "Select Next to continue.";
            panelPage.Items.Add(panelLabel2);
            //
            return panelPage;
        }
        private static PanelPage DefineTheLoads(FrmMain frmMain)
        {
            PanelPage panelPage = new PanelPage();
            panelPage.Title = "Define the Loads";
            panelPage.AssociatedView = ViewType.Model;
            //
            PanelItemLabel panelLabel1 = new PanelItemLabel();
            panelLabel1.Text = "For a static and buckling analysis the loads must be defined. For a frequency analysis, no loads are allowed.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "To add a load defined by a force magnitude and direction, select the option 1 (Surface traction) and then choose the region to load.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "If the FE model is loaded by a pressure, select the option 2 (Pressure) and then choose the region to load.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "When the FE model is large and bulky, the gravity must be taken into account. To assign a gravity load, select the option 3 (Gravity load) and then choose the region to load.";
            panelPage.Items.Add(panelLabel1);
            //
            PanelItemLinkLabel panelLinkLabel1 = new PanelItemLinkLabel();
            panelLinkLabel1.Text = "1 Surface traction";
            panelLinkLabel1.AddAction(frmMain.tsmiCreateLoad_Click, null, new EventArgs<int>(5));
            panelLinkLabel1.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel1);
            //
            PanelItemLinkLabel panelLinkLabel2 = new PanelItemLinkLabel();
            panelLinkLabel2.Text = "2 Pressure";
            panelLinkLabel2.AddAction(frmMain.tsmiCreateLoad_Click, null, new EventArgs<int>(2));
            panelLinkLabel2.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel2);
            //
            PanelItemLinkLabel panelLinkLabel3 = new PanelItemLinkLabel();
            panelLinkLabel3.Text = "3 Gravity load";
            panelLinkLabel3.AddAction(frmMain.tsmiCreateLoad_Click, null, new EventArgs<int>(7));
            panelLinkLabel3.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel3);
            //
            PanelItemLabel panelLabel2 = new PanelItemLabel();
            panelLabel2.Text = "Select Next to continue.";
            panelPage.Items.Add(panelLabel2);
            //
            return panelPage;
        }
        private static PanelPage RunTheAnalysis(FrmMain frmMain)
        {
            PanelPage panelPage = new PanelPage();
            panelPage.Title = "Run the Analysis";
            panelPage.AssociatedView = ViewType.Model;
            //
            PanelItemLabel panelLabel1 = new PanelItemLabel();
            panelLabel1.Text = "To run the analysis, select the option 1. A Monitor window will open where you can follow the analysis process. After the analysis completes, close the Monitor window.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "After the analysis completes, the results can be loaded by using the option 2.";
            panelPage.Items.Add(panelLabel1);
            //
            PanelItemLinkLabel panelLinkLabel1 = new PanelItemLinkLabel();
            panelLinkLabel1.Text = "1 Run";
            panelLinkLabel1.AddAction(frmMain.tsmiRunAnalysis_Click);
            panelLinkLabel1.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel1);
            //
            PanelItemLinkLabel panelLinkLabel2 = new PanelItemLinkLabel();
            panelLinkLabel2.Text = "2 Results";
            panelLinkLabel2.AddAction(frmMain.tsmiResultsAnalysis_Click);
            panelLinkLabel2.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel2);
            //
            PanelItemLabel panelLabel2 = new PanelItemLabel();
            panelLabel2.Text = "Select Next to continue.";
            panelPage.Items.Add(panelLabel2);
            //
            return panelPage;
        }
        private static PanelPage Results(FrmMain frmMain)
        {
            PanelPage panelPage = new PanelPage();
            panelPage.Title = "Results";
            panelPage.AssociatedView = ViewType.Results;
            //
            PanelItemLabel panelLabel1 = new PanelItemLabel();
            panelLabel1.Text = "Depending on the analysis type, different results are of interest.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "For static linear analysis, displacements (DISP) and stresses (STRESS) are usually inspected. Their components can be displayed by selecting them in the results tree.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "Eigenfrequency and eigenshape are important for frequency analysis. The eigenshape is displayed as the deformation of the FE Model while the eigenfrequency is reported in the status block. By default, 10 eigenfrequencies are computed. The displayed eigenfrequency can be changed by using the Step, Increment drop-down menu in the results toolbar.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "The buckling analysis reports the buckling factor in the status bar. Using the buckling factor, the limit load can be computed by multiplying it with the load defined on the FE Model. The limit load represents the load at which the FE Model buckles.";
            panelLabel1.Text += "\n\n";
            panelLabel1.Text += "At the end of you work, don't forget to save the model by using the option 1.";
            panelPage.Items.Add(panelLabel1);
            //
            PanelItemLinkLabel panelLinkLabel1 = new PanelItemLinkLabel();
            panelLinkLabel1.Text = "1 Save";
            panelLinkLabel1.AddAction(frmMain.tsmiSave_Click);
            panelLinkLabel1.IndentLevel = 0;
            panelPage.Items.Add(panelLinkLabel1);
            //
            return panelPage;
        }
    }
}

