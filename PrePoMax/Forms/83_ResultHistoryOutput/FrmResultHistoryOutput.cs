// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeResults;
using CaeGlobals;
using CaeMesh;
using System.Windows.Forms;
using CaeModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;

namespace PrePoMax.Forms
{
    class FrmResultHistoryOutput : UserControls.FrmPropertyDataListView, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        private const int TableColumnWidth = 100;
        private string[] _resultHistoryOutputSetNames;
        private string _resultHistoryOutputToEditName;
        private ViewResultHistoryOutput _viewResultHistoryOutput;
        private Controller _controller;
        private TabPage[] _pages;
        private BindingSource _tableBinding;
        private bool _updatingTable;


        // Properties                                                                                                               
        public  ResultHistoryOutput ResultHistoryOutput
        {
            get { return _viewResultHistoryOutput != null ? _viewResultHistoryOutput.GetBase() : null; }
            set
            {
                if (value is ResultHistoryOutputFromField rhoff) _viewResultHistoryOutput =
                        new ViewResultHistoryOutputFromField(rhoff.DeepClone(), _controller.CurrentResult.ContainsComplexResults());
                else if (value is ResultHistoryOutputFromEquation rhofe) _viewResultHistoryOutput =
                        new ViewResultHistoryOutputFromEquation(rhofe.DeepClone());
                else if (value is ResultHistoryOutputFromIntegration rhofi) _viewResultHistoryOutput =
                        new ViewResultHistoryOutputFromIntegration(rhofi.DeepClone());
                else if (value is ResultHistoryOutputFromAffineFit rhoaf) _viewResultHistoryOutput =
                        new ViewResultHistoryOutputFromAffineFit(rhoaf.DeepClone());
                else if (value is ResultHistoryOutputFromMergedHistories rhofmh) _viewResultHistoryOutput =
                        new ViewResultHistoryOutputFromMergedHistories(rhofmh.DeepClone());
                else if (value is ResultHistoryOutputFromExtractedEntry rhofee) _viewResultHistoryOutput =
                        new ViewResultHistoryOutputFromExtractedEntry(rhofee.DeepClone());
                else if (value is ResultHistoryOutputXY rhoxy) _viewResultHistoryOutput =
                        new ViewResultHistoryOutputXY(rhoxy.DeepClone());
                else if (value is ResultHistoryOutputFromTable rhoft) _viewResultHistoryOutput =
                        new ViewResultHistoryOutputFromTable(rhoft.DeepClone());
                else throw new NotImplementedException();
            }
        }


        // Constructors                                                                                                             
        public FrmResultHistoryOutput(Controller controller)
            : base(1.8)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewResultHistoryOutput = null;
            _tableBinding = null;
            _updatingTable = false;

            //
            int i = 0;
            _pages = new TabPage[tcProperties.TabPages.Count];
            foreach (TabPage tabPage in tcProperties.TabPages)
            {
                tabPage.Paint += TabPage_Paint;
                _pages[i++] = tabPage;
            }
            //
            dgvData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvData.EnableCutMenu = true;
            dgvData.EnablePasteMenu = true;
            dgvData.EnablePlotMenu = true;
        }
        private void InitializeComponent()
        {
            this.tcProperties.SuspendLayout();
            this.tpProperties.SuspendLayout();
            this.tpDataPoints.SuspendLayout();
            this.gbType.SuspendLayout();
            this.SuspendLayout();
            // 
            // tcProperties
            // 
            this.tcProperties.Location = new System.Drawing.Point(12, 126);
            this.tcProperties.Size = new System.Drawing.Size(310, 334);
            // 
            // tpProperties
            // 
            this.tpProperties.Size = new System.Drawing.Size(302, 306);
            // 
            // tpDataPoints
            // 
            this.tpDataPoints.Size = new System.Drawing.Size(302, 306);
            // 
            // gbType
            // 
            this.gbType.Size = new System.Drawing.Size(310, 108);
            // 
            // lvTypes
            // 
            this.lvTypes.Size = new System.Drawing.Size(298, 72);
            // 
            // gbProperties
            // 
            this.gbProperties.Location = new System.Drawing.Point(12, 126);
            this.gbProperties.Size = new System.Drawing.Size(310, 334);
            // 
            // propertyGrid
            // 
            this.propertyGrid.Size = new System.Drawing.Size(296, 300);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(160, 466);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(241, 466);
            // 
            // btnOkAddNew
            // 
            this.btnOkAddNew.Location = new System.Drawing.Point(79, 466);
            // 
            // FrmResultHistoryOutput
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(334, 501);
            this.MinimumSize = new System.Drawing.Size(350, 530);
            this.Name = "FrmResultHistoryOutput";
            this.Text = "Edit History Output";
            this.tcProperties.ResumeLayout(false);
            this.tpProperties.ResumeLayout(false);
            this.tpDataPoints.ResumeLayout(false);
            this.gbType.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Overrides                                                                                                                
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible) ShowHideSelectionForm();   // accounts for minimizing/maximizing the main form
            else dgvData.HidePlot();
            //
            base.OnVisibleChanged(e);
        }
        private void TabPage_Paint(object sender, PaintEventArgs e)
        {
            using (SolidBrush fillBrush = new SolidBrush(((TabPage)sender).BackColor))
                e.Graphics.FillRectangle(fillBrush, e.ClipRectangle);
            //
            if (sender == tpDataPoints)
            {
                ActiveControl = dgvData;
                dgvData.ClearSelection();
                if (dgvData.RowCount > 0 && dgvData.Columns.Count > 0) dgvData[0, 0].Selected = true;
            }
        }
        private void TableBinding_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (!_updatingTable) _propertyItemChanged = true;
        }
        protected override void OnListViewTypeSelectedIndexChanged()
        {
            if (lvTypes.SelectedItems != null && lvTypes.SelectedItems.Count > 0)
            {
                object itemTag = lvTypes.SelectedItems[0].Tag;
                SetTabPages(itemTag);
                ShowHideSelectionForm();
                propertyGrid.SelectedObject = itemTag;
                HighlightHistoryOutput();
            }
        }
        protected override void OnPropertyGridPropertyValueChanged()
        {
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (property == nameof(_viewResultHistoryOutput.RegionType))
            {
                ShowHideSelectionForm();
                //
                HighlightHistoryOutput();
            }
            else if (_viewResultHistoryOutput is ViewResultHistoryOutputFromField vrhoff &&
                     (property == nameof(vrhoff.NodeSetName) ||
                      property == nameof(vrhoff.SurfaceName)))
            {
                HighlightHistoryOutput();
            }
            else if (_viewResultHistoryOutput is ViewResultHistoryOutputFromEquation vrhofe)
            { }
            else if (_viewResultHistoryOutput is ViewResultHistoryOutputFromTable vrhoft &&
                     (property == nameof(vrhoft.NumberOfYColumns) ||
                      property == nameof(vrhoft.XAxisUnit) ||
                      property == nameof(vrhoft.YAxisUnit)))
            {
                SetTableGrid(vrhoft);
            }
            else if (_viewResultHistoryOutput is ViewResultHistoryOutputFromAffineFit vrhoaf &&
                     (property == nameof(vrhoaf.CenterX) || property == nameof(vrhoaf.CenterY) ||
                      property == nameof(vrhoaf.Radius) || property == nameof(vrhoaf.TaperStartFraction) ||
                      property == nameof(vrhoaf.PointSource) || property == nameof(vrhoaf.Weighting) ||
                      property == nameof(vrhoaf.AreaFilterRelativeMedian) ||
                      property == nameof(vrhoaf.CellAreaToleranceFraction)))
            {
                HighlightHistoryOutput();
            }
            else if (_viewResultHistoryOutput is ViewResultHistoryOutputFromIntegration vrhofi)
            {
                if (property == nameof(vrhofi.ElementSetName)) HighlightHistoryOutput();
                else if (property == nameof(vrhofi.IntegralType))
                {
                    vrhofi.RegionType = RegionTypeEnum.Selection.ToString();
                    ShowHideSelectionForm();
                    _controller.ClearAllSelection();
                }
            }
            else if (_viewResultHistoryOutput is ViewResultHistoryOutputFromMergedHistories vrhofmh &&
                     (property == nameof(ViewResultHistoryOutputFromMergedHistories.Source1) ||
                      property == nameof(ViewResultHistoryOutputFromMergedHistories.Source2) ||
                      property == nameof(ViewResultHistoryOutputFromMergedHistories.Source3)))
            {
                PopulateDropDownLists(vrhofmh);
                propertyGrid.Refresh();
            }
            else if ((_viewResultHistoryOutput is ViewResultHistoryOutputFromExtractedEntry &&
                      property == nameof(ViewResultHistoryOutputFromExtractedEntry.Source)) ||
                     (_viewResultHistoryOutput is ViewResultHistoryOutputXY &&
                      property == nameof(ViewResultHistoryOutputXY.XSource)))
            {
                propertyGrid.Refresh();
            }
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            if (propertyGrid.SelectedObject is ViewError ve) throw new CaeException(ve.Message);
            //
            _viewResultHistoryOutput = (ViewResultHistoryOutput)propertyGrid.SelectedObject;
            if (_viewResultHistoryOutput is ViewResultHistoryOutputFromTable)
                UpdateTableDataFromGrid(true);
            //
            if (ResultHistoryOutput == null) throw new CaeException("No history output was selected.");
            // Check if the name exists
            CheckName(_resultHistoryOutputToEditName, ResultHistoryOutput.Name, _resultHistoryOutputSetNames, "history output");
            // Check that the history set can be created from the currently selected sources
            if (ResultHistoryOutput is ResultHistoryOutputFromMergedHistories merged)
                DerivedHistorySetBuilder.CreateMergedSet(_controller.CurrentResult, merged);
            else if (ResultHistoryOutput is ResultHistoryOutputFromExtractedEntry extracted)
                DerivedHistorySetBuilder.CreateExtractedSet(_controller.CurrentResult, extracted);
            else if (ResultHistoryOutput is ResultHistoryOutputXY xy)
                DerivedHistorySetBuilder.CreateXYSet(_controller.CurrentResult, xy);
            // Check selection
            if (ResultHistoryOutput.RegionType == RegionTypeEnum.Selection &&
                (ResultHistoryOutput.CreationIds == null || ResultHistoryOutput.CreationIds.Length == 0))
                throw new CaeException("The history output selection must contain at least one item.");
            // Check integration cross section thickness
            if (ResultHistoryOutput is ResultHistoryOutputFromIntegration rhofiThickness &&
                rhofiThickness.CrossSection == IntegrationScaleType.Thickness && rhofiThickness.Thickness <= 0)
                throw new CaeException("The cross section thickness must be larger than zero.");
            if (ResultHistoryOutput is ResultHistoryOutputFromIntegration rhofiCrossSection)
            {
                string crossSectionError =
                    _controller.CurrentResult.GetIntegrationCrossSectionError(rhofiCrossSection.CrossSection);
                if (crossSectionError != null) throw new CaeException(crossSectionError);
                string regionError = _controller.CurrentResult.GetIntegrationRegionError(rhofiCrossSection);
                if (regionError != null) throw new CaeException(regionError);
            }
            // Check equation
            if (ResultHistoryOutput is ResultHistoryOutputFromEquation rhofe)
            {
                HashSet<string> parentNames;
                string error = _controller.CurrentResult.CheckResultHistoryOutputEquation(rhofe.Equation, out parentNames, out _);
                if (error != null) throw new CaeException(error);
                if (parentNames.Contains(rhofe.Name)) throw new CaeException("The equation must not contain a self reference.");
                rhofe.SetParentNames(parentNames.ToArray());
                // Cyclic reference
                if (_controller.CurrentResult.AreResultHistoryOutputsInCyclicDependance(_resultHistoryOutputToEditName, rhofe))
                {
                    throw new CaeException("The selected equation creates a cyclic reference.");
                }
            }
            if ((ResultHistoryOutput is ResultHistoryOutputFromMergedHistories ||
                 ResultHistoryOutput is ResultHistoryOutputFromExtractedEntry ||
                 ResultHistoryOutput is ResultHistoryOutputXY) &&
                _controller.CurrentResult.AreResultHistoryOutputsInCyclicDependance(
                    _resultHistoryOutputToEditName, ResultHistoryOutput))
                throw new CaeException("The selected sources create a cyclic reference.");
            if (ResultHistoryOutput is ResultHistoryOutputFromTable rhoft) rhoft.CheckData();
            // Create
            if (_resultHistoryOutputToEditName == null)
            {
                _controller.AddResultHistoryOutputCommand(ResultHistoryOutput);
            }
            // Replace
            else if (_propertyItemChanged)
            {
                _controller.ReplaceResultHistoryOutputCommand(_resultHistoryOutputToEditName, ResultHistoryOutput);
            }
            // If all is successful close the ItemSetSelectionForm - except for OKAddNew
            if (!onOkAddNew) ItemSetDataEditor.SelectionForm.Hide();
        }
        protected override void OnHideOrClose()
        {
            // Close the ItemSetSelectionForm
            ItemSetDataEditor.SelectionForm.Hide();
            //
            base.OnHideOrClose();
        }
        protected override bool OnPrepareForm(string stepName, string historyOutputToEditName)
        {
            this.btnOkAddNew.Visible = historyOutputToEditName == null;
            //
            _propertyItemChanged = false;
            _stepName = null;
            _resultHistoryOutputSetNames = null;
            _resultHistoryOutputToEditName = null;
            _viewResultHistoryOutput = null;
            lvTypes.Items.Clear();
            propertyGrid.SelectedObject = null;
            SetTabPages(null);
            //
            _stepName = stepName;
            _resultHistoryOutputSetNames = _controller.GetHistoryResultSetNames();
            _resultHistoryOutputToEditName = historyOutputToEditName;
            string[] nodeSetNames = _controller.GetResultUserNodeSetNames();
            string[] elementSetNames = _controller.GetResultUserElementSetNames();
            string[] surfaceNames = _controller.GetResultUserSurfaceNames();
            Dictionary<string, string[]> filedNameComponentNames =
                _controller.CurrentResult.GetAllVisibleFiledNameComponentNames();
            Dictionary<int, int[]> stepIdStepIncrementIds = _controller.CurrentResult.GetAllExistingIncrementIds();
            //
            if (_resultHistoryOutputSetNames == null)
                throw new CaeException("The history output names must be defined first.");
            // Populate list view
            PopulateListOfHistoryOutputs(nodeSetNames, elementSetNames, surfaceNames, filedNameComponentNames,
                                         stepIdStepIncrementIds);
            // Create new history output
            if (_resultHistoryOutputToEditName == null)
            {
                lvTypes.Enabled = true;
                _viewResultHistoryOutput = null;
                //
                if (lvTypes.Items.Count == 1) _preselectIndex = 0;
                //
                HighlightHistoryOutput(); // must be here if called from the menu
            }
            // Edit existing history output
            else
            {
                ResultHistoryOutput = _controller.GetResultHistoryOutput(_resultHistoryOutputToEditName); // to clone
                _propertyItemChanged = !ResultHistoryOutput.Valid;
                //
                int selectedId;
                if (_viewResultHistoryOutput is ViewResultHistoryOutputFromField vrhoff)
                {
                    selectedId = 0;
                    // Check
                    string[] fieldNames = filedNameComponentNames.Keys.ToArray();
                    CheckMissingValueRef(ref fieldNames, vrhoff.FieldName, s => { vrhoff.FieldName = s; });
                    //
                    string[] componentNames = filedNameComponentNames[vrhoff.FieldName];
                    ResultHistoryOutputFromField rhoff = (ResultHistoryOutputFromField)vrhoff.GetBase();
                    string[] selectedComponentNames = rhoff.ComponentNames;
                    string[] intersection = componentNames.Intersect(selectedComponentNames).ToArray();
                    if (intersection.Length != selectedComponentNames.Length)
                    {
                        if (intersection.Length > 0) rhoff.ComponentNames = intersection;
                        else rhoff.ComponentNames = componentNames;
                        //
                        MessageBoxes.ShowWarning("Some selected components no longer exist. The selected components were " +
                                                 "changed to existing ones");
                    }
                    // Populate
                    vrhoff.PopulateDropDownLists(nodeSetNames, surfaceNames, filedNameComponentNames, stepIdStepIncrementIds);
                }
                else if (_viewResultHistoryOutput is ViewResultHistoryOutputFromEquation vrhofe)
                {
                    selectedId = 1;
                    // Populate
                    vrhofe.PopulateDropDownLists();
                }
                else if (_viewResultHistoryOutput is ViewResultHistoryOutputFromIntegration vrhofi)
                {
                    selectedId = 2;
                    // Check
                    string[] fieldNames = filedNameComponentNames.Keys.ToArray();
                    CheckMissingValueRef(ref fieldNames, vrhofi.FieldName, s => { vrhofi.FieldName = s; });
                    //
                    string[] componentNames = filedNameComponentNames[vrhofi.FieldName];
                    ResultHistoryOutputFromIntegration rhofi = (ResultHistoryOutputFromIntegration)vrhofi.GetBase();
                    string[] selectedComponentNames = rhofi.ComponentNames;
                    string[] intersection = componentNames.Intersect(selectedComponentNames).ToArray();
                    if (intersection.Length != selectedComponentNames.Length)
                    {
                        if (intersection.Length > 0) rhofi.ComponentNames = intersection;
                        else rhofi.ComponentNames = componentNames;
                        //
                        MessageBoxes.ShowWarning("Some selected components no longer exist. The selected components were " +
                                                 "changed to existing ones");
                    }
                    //
                    string[] deformationVariableNames = FeResults.GetPossibleDeformationFieldOutputNamesMap().Keys.ToArray();
                    CheckMissingValueRef(ref deformationVariableNames, vrhofi.DeformationVariableName,
                                         s => { vrhofi.DeformationVariableName = s; });
                    // Populate
                    vrhofi.SetDefaultCrossSection(_controller.GetDefaultResultIntegrationCrossSection());
                    vrhofi.PopulateDropDownLists(elementSetNames, surfaceNames, filedNameComponentNames);
                }
                else if (_viewResultHistoryOutput is ViewResultHistoryOutputFromAffineFit vrhoaf)
                {
                    selectedId = 3;
                    // Populate
                    vrhoaf.PopulateDropDownLists();
                }
                else if (_viewResultHistoryOutput is ViewResultHistoryOutputFromMergedHistories vrhofmh)
                {
                    selectedId = GetHistoryOutputTypeIndex("From Merged Histories");
                    // Check
                    string[] sourceNames = GetHistorySourceNames(_viewResultHistoryOutput);
                    // Populate
                    PopulateDropDownLists(vrhofmh);
                    CheckChangedHistorySources(sourceNames);
                }
                else if (_viewResultHistoryOutput is ViewResultHistoryOutputFromExtractedEntry vrhofee)
                {
                    selectedId = GetHistoryOutputTypeIndex("From Extracted Entry");
                    // Check
                    string[] sourceNames = GetHistorySourceNames(_viewResultHistoryOutput);
                    // Populate
                    PopulateDropDownLists(vrhofee);
                    CheckChangedHistorySources(sourceNames);
                }
                else if (_viewResultHistoryOutput is ViewResultHistoryOutputXY vrhoxy)
                {
                    selectedId = GetHistoryOutputTypeIndex("XY");
                    // Check
                    string[] sourceNames = GetHistorySourceNames(_viewResultHistoryOutput);
                    // Populate
                    PopulateDropDownLists(vrhoxy);
                    CheckChangedHistorySources(sourceNames);
                }
                else if (_viewResultHistoryOutput is ViewResultHistoryOutputFromTable vrhoft)
                {
                    selectedId = GetHistoryOutputTypeIndex("From Table");
                    vrhoft.PopulateDropDownLists();
                }
                else throw new NotSupportedException();
                //
                lvTypes.Items[selectedId].Tag = _viewResultHistoryOutput;
                _preselectIndex = selectedId;
            }
            //
            ShowHideSelectionForm();
            //
            propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterAndHistoryNames());
            //
            return true;
        }


        // Methods                                                                                                                  
        private void PopulateListOfHistoryOutputs(string[] nodeSetNames, string[] elementSetNames, string[] surfaceNames,
                                                  Dictionary<string, string[]> filedNameComponentNames,
                                                  Dictionary<int, int[]> stepIdStepIncrementIds)
        {
            ListViewItem item;
            FieldData fieldData = _controller.CurrentFieldData;
            // History output from field output
            item = new ListViewItem("From Field Output");
            ResultHistoryOutputFromField rhoff =
                new ResultHistoryOutputFromField(GetHistoryOutputName("From_Field"), "", RegionTypeEnum.Selection,
                                                 fieldData.Name, null);
            ViewResultHistoryOutputFromField vrhoff =
                new ViewResultHistoryOutputFromField(rhoff, _controller.CurrentResult.ContainsComplexResults());
            vrhoff.PopulateDropDownLists(nodeSetNames, surfaceNames, filedNameComponentNames, stepIdStepIncrementIds);
            item.Tag = vrhoff;
            lvTypes.Items.Add(item);
            // History output from equation
            item = new ListViewItem("From History Output by Equation");
            ResultHistoryOutputFromEquation rhofe =
                new ResultHistoryOutputFromEquation(GetHistoryOutputName("From_Equation"), "=");
            ViewResultHistoryOutputFromEquation vrhofe = new ViewResultHistoryOutputFromEquation(rhofe);
            vrhofe.PopulateDropDownLists();
            item.Tag = vrhofe;
            lvTypes.Items.Add(item);
            // History output from integration
            string defVar = FeResults.GetPossibleDeformationFieldOutputNamesMap().First().Key;
            item = new ListViewItem("From Integration");
            ResultHistoryOutputFromIntegration rhofi =
                new ResultHistoryOutputFromIntegration(GetHistoryOutputName("From_Integration"), IntegralTypeEnum.Line,
                                                       "", RegionTypeEnum.Selection, fieldData.Name, null, defVar);
            rhofi.CrossSection = _controller.GetDefaultResultIntegrationCrossSection();
            ViewResultHistoryOutputFromIntegration vrhofi = new ViewResultHistoryOutputFromIntegration(rhofi);
            vrhofi.SetDefaultCrossSection(rhofi.CrossSection);
            vrhofi.PopulateDropDownLists(elementSetNames, surfaceNames, filedNameComponentNames);
            item.Tag = vrhofi;
            lvTypes.Items.Add(item);
#if DEBUG
           // History output from affine fit (Poisson's ratio)
            item = new ListViewItem("From Affine Fit (Poisson's Ratio)");
            ResultHistoryOutputFromAffineFit rhoaf =
                new ResultHistoryOutputFromAffineFit(GetHistoryOutputName("From_Affine_Fit"));
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
            {
                BoundingBox boundingBox = _controller.CurrentResult.Mesh.BoundingBox;
                rhoaf.CenterX = (boundingBox.MinX + boundingBox.MaxX) / 2;
                rhoaf.CenterY = (boundingBox.MinY + boundingBox.MaxY) / 2;
                rhoaf.Radius = 0.5 * 0.8 * (boundingBox.MaxY - boundingBox.MinY);
            }
            ViewResultHistoryOutputFromAffineFit vrhoaf = new ViewResultHistoryOutputFromAffineFit(rhoaf);
            vrhoaf.PopulateDropDownLists();
            item.Tag = vrhoaf;
            lvTypes.Items.Add(item);
#endif
            // History output from two to four merged history components
            item = new ListViewItem("From Merged Histories");
            HistoryResultData[] mergeSources = GetMergeFirstSources();
            if (mergeSources.Length > 0)
            {
                HistoryResultData[] compatibleSources = GetCompatibleMergeSources(mergeSources[0]);
                ResultHistoryOutputFromMergedHistories merged =
                    new ResultHistoryOutputFromMergedHistories(GetHistoryOutputName("From_Merged_Histories"),
                        mergeSources[0], compatibleSources[0], null, null);
                ViewResultHistoryOutputFromMergedHistories view =
                    new ViewResultHistoryOutputFromMergedHistories(merged);
                PopulateDropDownLists(view);
                item.Tag = view;
            }
            else item.Tag = new ViewError("There are no two compatible history components to merge.");
            lvTypes.Items.Add(item);
            // History output from one extracted entry
            item = new ListViewItem("From Extracted Entry");
            HistoryResultData[] extractSources = GetAvailableHistorySources(
                _controller.GetExtractableHistoryComponents());
            if (extractSources.Length > 0)
            {
                Dictionary<string, string[]> entriesBySource = GetEntryNamesBySource(extractSources);
                ResultHistoryOutputFromExtractedEntry extracted =
                    new ResultHistoryOutputFromExtractedEntry(GetHistoryOutputName("From_Extracted_Entry"),
                        extractSources[0], entriesBySource[extractSources[0].Name][0]);
                ViewResultHistoryOutputFromExtractedEntry view =
                    new ViewResultHistoryOutputFromExtractedEntry(extracted);
                view.PopulateDropDownLists(extractSources, entriesBySource);
                item.Tag = view;
            }
            else item.Tag = new ViewError("There are no history entries containing finite data to extract.");
            lvTypes.Items.Add(item);
            // X versus Y history output
            item = new ListViewItem("XY");
            Dictionary<string, HistoryResultData[]> ySourcesByX;
            HistoryResultData[] xSources = GetXYXSources(out ySourcesByX);
            if (xSources.Length > 0)
            {
                ResultHistoryOutputXY xy = new ResultHistoryOutputXY(GetHistoryOutputName("XY"),
                    xSources[0], ySourcesByX[xSources[0].Name][0]);
                ViewResultHistoryOutputXY view = new ViewResultHistoryOutputXY(xy);
                view.PopulateDropDownLists(xSources, ySourcesByX);
                item.Tag = view;
            }
            else item.Tag = new ViewError("There are no compatible X and Y history components.");
            lvTypes.Items.Add(item);
            // History output from a user-defined table
            item = new ListViewItem("From Table");
            ResultHistoryOutputFromTable rhoft =
                new ResultHistoryOutputFromTable(GetHistoryOutputName("From_Table"));
            ViewResultHistoryOutputFromTable vrhoft = new ViewResultHistoryOutputFromTable(rhoft);
            vrhoft.PopulateDropDownLists();
            item.Tag = vrhoft;
            lvTypes.Items.Add(item);
        }
        private HistoryResultData[] GetAvailableHistorySources(IEnumerable<HistoryResultData> sources)
        {
            return sources.Where(source => source.SetName != _resultHistoryOutputToEditName).ToArray();
        }
        private HistoryResultData[] GetMergeFirstSources()
        {
            HistoryResultData[] allSources =
                GetAvailableHistorySources(_controller.GetMergeHistoryComponents());
            return allSources.Where(source => GetCompatibleMergeSources(source).Length > 0).ToArray();
        }
        private HistoryResultData[] GetCompatibleMergeSources(params HistoryResultData[] selectedSources)
        {
            return GetAvailableHistorySources(
                _controller.GetCompatibleMergeHistoryComponents(selectedSources));
        }
        private static bool ContainsSource(IEnumerable<HistoryResultData> sources, HistoryResultData source)
        {
            return source != null && sources.Any(item => item.Name == source.Name);
        }
        private Dictionary<string, string[]> GetEntryNamesBySource(HistoryResultData[] sources)
        {
            Dictionary<string, string[]> entriesBySource = new Dictionary<string, string[]>();
            foreach (HistoryResultData source in sources)
                entriesBySource.Add(source.Name, _controller.GetHistoryEntryNames(source));
            return entriesBySource;
        }
        private HistoryResultData[] GetXYXSources(
            out Dictionary<string, HistoryResultData[]> ySourcesByX)
        {
            HistoryResultData[] allXSources =
                GetAvailableHistorySources(_controller.GetXYXHistoryComponents());
            ySourcesByX = new Dictionary<string, HistoryResultData[]>();
            foreach (HistoryResultData source in allXSources)
            {
                HistoryResultData[] compatible = GetAvailableHistorySources(
                    _controller.GetCompatibleXYYHistoryComponents(source));
                ySourcesByX.Add(source.Name, compatible);
            }
            Dictionary<string, HistoryResultData[]> sourceMap = ySourcesByX;
            return allXSources.Where(source => sourceMap[source.Name].Length > 0).ToArray();
        }
        private void PopulateDropDownLists(ViewResultHistoryOutputFromMergedHistories view)
        {
            HistoryResultData[] source1Items = GetMergeFirstSources();
            ResultHistoryOutputFromMergedHistories output =
                (ResultHistoryOutputFromMergedHistories)view.GetBase();

            if (!ContainsSource(source1Items, output.Source1))
                output.Source1 = source1Items.Length > 0 ? source1Items[0] : null;

            HistoryResultData[] source2Items = output.Source1 == null
                ? new HistoryResultData[0]
                : GetCompatibleMergeSources(output.Source1);
            if (!ContainsSource(source2Items, output.Source2))
                output.Source2 = source2Items.Length > 0 ? source2Items[0] : null;

            HistoryResultData[] source3Items = output.Source1 == null || output.Source2 == null
                ? new HistoryResultData[0]
                : GetCompatibleMergeSources(output.Source1, output.Source2);
            if (output.Source3 != null && !ContainsSource(source3Items, output.Source3))
                output.Source3 = null;

            HistoryResultData[] source4Items;
            if (output.Source1 == null || output.Source2 == null)
                source4Items = new HistoryResultData[0];
            else if (output.Source3 == null)
                source4Items = GetCompatibleMergeSources(output.Source1, output.Source2);
            else
                source4Items = GetCompatibleMergeSources(output.Source1, output.Source2, output.Source3);
            if (output.Source4 != null && !ContainsSource(source4Items, output.Source4))
                output.Source4 = null;

            view.PopulateDropDownLists(source1Items, source2Items, source3Items, source4Items);
        }
        private void PopulateDropDownLists(ViewResultHistoryOutputFromExtractedEntry view)
        {
            HistoryResultData[] sources =
                GetAvailableHistorySources(_controller.GetExtractableHistoryComponents());
            view.PopulateDropDownLists(sources, GetEntryNamesBySource(sources));
        }
        private void PopulateDropDownLists(ViewResultHistoryOutputXY view)
        {
            Dictionary<string, HistoryResultData[]> ySourcesByX;
            HistoryResultData[] xSources = GetXYXSources(out ySourcesByX);
            view.PopulateDropDownLists(xSources, ySourcesByX);
        }
        private string[] GetHistorySourceNames(ViewResultHistoryOutput view)
        {
            List<string> sourceNames = new List<string>();
            ResultHistoryOutput resultHistoryOutput = view.GetBase();
            //
            if (resultHistoryOutput is ResultHistoryOutputFromMergedHistories rhofmh)
            {
                sourceNames.Add(GetHistorySourceName(rhofmh.Source1));
                sourceNames.Add(GetHistorySourceName(rhofmh.Source2));
                sourceNames.Add(GetHistorySourceName(rhofmh.Source3));
                sourceNames.Add(GetHistorySourceName(rhofmh.Source4));
            }
            else if (resultHistoryOutput is ResultHistoryOutputFromExtractedEntry rhofee)
            {
                sourceNames.Add(GetHistorySourceName(rhofee.Source));
                sourceNames.Add(rhofee.EntryName);
            }
            else if (resultHistoryOutput is ResultHistoryOutputXY rhoxy)
            {
                sourceNames.Add(GetHistorySourceName(rhoxy.XSource));
                sourceNames.Add(GetHistorySourceName(rhoxy.YSource));
            }
            else throw new NotSupportedException();
            //
            return sourceNames.ToArray();
        }
        private static string GetHistorySourceName(HistoryResultData source)
        {
            return source == null ? "" : source.GetDisplayName();
        }
        private void CheckChangedHistorySources(string[] previousSourceNames)
        {
            string[] sourceNames = GetHistorySourceNames(_viewResultHistoryOutput);
            bool changed = sourceNames.Length != previousSourceNames.Length;
            for (int i = 0; !changed && i < sourceNames.Length; i++)
                changed = sourceNames[i] != previousSourceNames[i];
            //
            if (changed)
            {
                MessageBoxes.ShowWarning("Some selected history components no longer exist. The selected history " +
                                         "components were changed to existing ones.");
                _propertyItemChanged = true;
            }
        }
        private int GetHistoryOutputTypeIndex(string typeName)
        {
            for (int i = 0; i < lvTypes.Items.Count; i++)
            {
                if (lvTypes.Items[i].Text == typeName) return i;
            }
            throw new CaeException("The history output type '" + typeName + "' cannot be found.");
        }
        private string GetHistoryOutputName(string prefix)
        {
            return _resultHistoryOutputSetNames.GetNextNumberedKey(prefix);
        }
        private void SetTabPages(object item)
        {
            if (_viewResultHistoryOutput is ViewResultHistoryOutputFromTable && dgvData.Columns.Count > 0)
                UpdateTableDataFromGrid(false);
            ClearTableGrid();
            tcProperties.TabPages.Clear();
            tcProperties.TabPages.Add(_pages[0]);   // properties
            //
            if (item == null || item is ViewError) _viewResultHistoryOutput = null;
            else if (item is ViewResultHistoryOutput view) _viewResultHistoryOutput = view;
            else throw new NotImplementedException();
            //
            if (_viewResultHistoryOutput is ViewResultHistoryOutputFromTable vrhoft)
            {
                tcProperties.TabPages.Add(_pages[1]);   // data points
                _pages[1].Text = "Data points";
                SetTableGrid(vrhoft);
            }
        }
        private void ClearTableGrid()
        {
            _updatingTable = true;
            try
            {
                if (_tableBinding != null)
                {
                    _tableBinding.ListChanged -= TableBinding_ListChanged;
                    _tableBinding.Dispose();
                    _tableBinding = null;
                }
                dgvData.DataSource = null;
                dgvData.Columns.Clear();
            }
            finally
            {
                _updatingTable = false;
            }
        }
        private void SetTableGrid(ViewResultHistoryOutputFromTable view)
        {
            int columnCount = view.NumberOfYColumns + 1;
            //
            if (object.ReferenceEquals(_viewResultHistoryOutput, view) && dgvData.Columns.Count > 0)
            {
                double[][] currentData = GetTableDataFromGrid(dgvData.Columns.Count, false);
                if (currentData != null)
                {
                    double[][] resizedData = new double[currentData.Length][];
                    for (int row = 0; row < currentData.Length; row++)
                    {
                        resizedData[row] = new double[columnCount];
                        Array.Copy(currentData[row], resizedData[row],
                                   Math.Min(currentData[row].Length, columnCount));
                    }
                    view.DataPoints = resizedData;
                }
            }
            //
            DataGridViewColumnHeadersHeightSizeMode columnHeadersHeightSizeMode = dgvData.ColumnHeadersHeightSizeMode;
            dgvData.SuspendLayout();
            try
            {
                dgvData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                ClearTableGrid();
                _updatingTable = true;
                DataTable table = new DataTable();
                for (int i = 0; i < columnCount; i++)
                    table.Columns.Add("Column" + i, typeof(double));
                //
                double[][] dataPoints = view.DataPoints;
                if (dataPoints != null)
                {
                    foreach (double[] point in dataPoints)
                    {
                        if (point == null || point.Length != columnCount) continue;
                        object[] values = new object[columnCount];
                        for (int i = 0; i < columnCount; i++) values[i] = point[i];
                        table.Rows.Add(values);
                    }
                }
                //
                _tableBinding = new BindingSource();
                _tableBinding.DataSource = table;
                dgvData.DataSource = _tableBinding;
                dgvData.AllowUserToAddRows = true;
                dgvData.AllowUserToDeleteRows = true;
                dgvData.XColName = "Default";
                dgvData.StartPlotAtZero = false;
                //
                SetTableColumn(0, GetTableColumnTitle(ResultHistoryOutputFromTable.XAxisName, view.XAxisUnit, "x"));
                for (int i = 0; i < view.NumberOfYColumns; i++)
                {
                    SetTableColumn(i + 1, GetTableColumnTitle(view.GetYColumnName(i), view.YAxisUnit, "y" + (i + 1)));
                }
                _tableBinding.ListChanged += TableBinding_ListChanged;
            }
            finally
            {
                _updatingTable = false;
                dgvData.ColumnHeadersHeightSizeMode = columnHeadersHeightSizeMode;
                dgvData.ResumeLayout(false);
            }
        }
        private void SetTableColumn(int index, string title)
        {
            DataGridViewColumn column = dgvData.Columns[index];
            column.HeaderText = title;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.BottomCenter;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            column.Width = TableColumnWidth;
            column.Tag = new StringDoubleConverter();
        }
        private static string GetTableColumnTitle(string name, string unit, string defaultName)
        {
            if (string.IsNullOrWhiteSpace(name)) name = defaultName;
            return name + (string.IsNullOrWhiteSpace(unit) ? "" : "\n[" + unit + "]");
        }
        private void UpdateTableDataFromGrid(bool requireComplete)
        {
            if (!(_viewResultHistoryOutput is ViewResultHistoryOutputFromTable view)) return;
            int expectedColumnCount = view.NumberOfYColumns + 1;
            double[][] dataPoints = GetTableDataFromGrid(expectedColumnCount, requireComplete);
            if (dataPoints != null) view.DataPoints = dataPoints;
        }
        private double[][] GetTableDataFromGrid(int expectedColumnCount, bool requireComplete)
        {
            dgvData.EndEdit();
            if (_tableBinding != null) _tableBinding.EndEdit();
            //
            if (dgvData.Columns.Count != expectedColumnCount)
            {
                if (requireComplete) throw new CaeException("The table columns do not match the value names.");
                return null;
            }
            //
            List<double[]> dataPoints = new List<double[]>();
            foreach (DataGridViewRow row in dgvData.Rows)
            {
                if (row.IsNewRow) continue;
                //
                bool allEmpty = true;
                bool rowValid = true;
                double[] values = new double[expectedColumnCount];
                for (int column = 0; column < expectedColumnCount; column++)
                {
                    object value = row.Cells[column].Value;
                    bool empty = value == null || value == DBNull.Value ||
                                 string.IsNullOrWhiteSpace(Convert.ToString(value, CultureInfo.CurrentCulture));
                    if (empty)
                    {
                        rowValid = false;
                        continue;
                    }
                    //
                    allEmpty = false;
                    double number;
                    if (!TryGetFiniteDouble(value, out number))
                    {
                        rowValid = false;
                        continue;
                    }
                    values[column] = number;
                }
                //
                if (allEmpty) continue;
                if (!rowValid)
                {
                    if (requireComplete)
                        throw new CaeException("Table row " + (row.Index + 1) +
                                               " must contain a finite number in every column.");
                    continue;
                }
                dataPoints.Add(values);
            }
            //
            if (requireComplete && dataPoints.Count == 0)
                throw new CaeException("The table must contain at least one complete data row.");
            return dataPoints.ToArray();
        }
        private static bool TryGetFiniteDouble(object value, out double number)
        {
            if (value is double doubleValue) number = doubleValue;
            else
            {
                string text = Convert.ToString(value, CultureInfo.CurrentCulture);
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out number) &&
                    !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                    return false;
            }
            return !double.IsNaN(number) && !double.IsInfinity(number);
        }
        private void HighlightHistoryOutput()
        {
            try
            {
                _controller.ClearSelectionHistory();
                //
                if (_viewResultHistoryOutput == null) { }
                else if (ResultHistoryOutput is ResultHistoryOutputFromField)
                {
                    if (ResultHistoryOutput.RegionType == RegionTypeEnum.NodeSetName ||
                        ResultHistoryOutput.RegionType == RegionTypeEnum.SurfaceName)
                    {
                        _controller.Highlight3DObjects(new object[] { ResultHistoryOutput.RegionName });
                    }
                    else if (ResultHistoryOutput.RegionType == RegionTypeEnum.Selection)
                    {
                        SetSelectItem();
                        //
                        if (ResultHistoryOutput.CreationData != null)
                        {
                            _controller.Selection = ResultHistoryOutput.CreationData.DeepCopy(true, false);
                            _controller.HighlightSelection();
                        }
                    }
                    else throw new NotImplementedException();
                }
                else if (ResultHistoryOutput is ResultHistoryOutputFromEquation)
                { }
                else if (ResultHistoryOutput is ResultHistoryOutputFromTable ||
                         ResultHistoryOutput is ResultHistoryOutputFromMergedHistories ||
                         ResultHistoryOutput is ResultHistoryOutputFromExtractedEntry ||
                         ResultHistoryOutput is ResultHistoryOutputXY)
                { }
                else if (ResultHistoryOutput is ResultHistoryOutputFromAffineFit rhoaf)
                {
                    _controller.HighlightAffineFitRegion(rhoaf);
                }
                else if (ResultHistoryOutput is ResultHistoryOutputFromIntegration)
                {
                    if (ResultHistoryOutput.RegionType == RegionTypeEnum.ElementSetName ||
                        ResultHistoryOutput.RegionType == RegionTypeEnum.SurfaceName)
                    {
                        _controller.Highlight3DObjects(new object[] { ResultHistoryOutput.RegionName });
                    }
                    else if (ResultHistoryOutput.RegionType == RegionTypeEnum.Selection)
                    {
                        SetSelectItem();
                        //
                        if (ResultHistoryOutput.CreationData != null)
                        {
                            _controller.Selection = ResultHistoryOutput.CreationData.DeepCopy(true, false);
                            _controller.HighlightSelection();
                        }
                    }
                    else throw new NotImplementedException();
                }
                else throw new NotSupportedException();
            }
            catch { }
        }
        private void ShowHideSelectionForm()
        {
            if (ResultHistoryOutput != null && ResultHistoryOutput.RegionType == RegionTypeEnum.Selection)
                ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
            else
                ItemSetDataEditor.SelectionForm.Hide();
            //
            SetSelectItem();
        }
        private void SetSelectItem()
        {
            if (ResultHistoryOutput != null && ResultHistoryOutput.RegionType == RegionTypeEnum.Selection)
            {
                // Reset the shell edge selection limits (used only by the line integral)
                _controller.Selection.EnableShellEdgeFaceSelection = false;
                _controller.Selection.LimitSelectionToShellEdges = false;
                //
                if (ResultHistoryOutput is null) { }
                else if (ResultHistoryOutput is ResultHistoryOutputFromField) _controller.SetSelectItemToNode();
                else if (ResultHistoryOutput is ResultHistoryOutputFromIntegration rhofi)
                {
                    if (rhofi.IntegralType == IntegralTypeEnum.Line)
                    {
                        // Select geometry edges (works for shell and solid parts) - see Query -> Edge
                        _controller.SetSelectItemToGeometryEdge();
                    }
                    else if (rhofi.IntegralType == IntegralTypeEnum.Surface)
                    {
                        _controller.SetSelectItemToSurface();
                    }
                    else if (rhofi.IntegralType == IntegralTypeEnum.Volume)
                    {
                        _controller.SetSelectItemToElement();
                    }
                    else throw new NotSupportedException();
                }
            }
            else _controller.SetSelectByToOff();
        }
        //
        public void SelectionChanged(int[] ids)
        {
            if (ResultHistoryOutput != null && ResultHistoryOutput.RegionType == RegionTypeEnum.Selection)
            {
                if (ResultHistoryOutput is ResultHistoryOutputFromField ||
                    ResultHistoryOutput is ResultHistoryOutputFromIntegration)
                {
                    ResultHistoryOutput.CreationIds = ids;
                    ResultHistoryOutput.CreationData = _controller.Selection.DeepCopy(true, false);
                    //
                    propertyGrid.Refresh();
                    //
                    _propertyItemChanged = true;
                }
                else throw new NotSupportedException();
            }
        }
        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightHistoryOutput();
        }
        // IFormItemSetDataParent
        public bool IsSelectionGeometryBased()
        {
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            ResultHistoryOutput resultHistoryOutput = ResultHistoryOutput;
            //
            if (resultHistoryOutput.CreationData != null) return resultHistoryOutput.CreationData.IsGeometryBased();
            else return true;
        }
        public bool IsGeometrySelectionIdBased()
        {
            bool defaultMode = _controller.Settings.Pre.GeometrySelectMode == GeometrySelectModeEnum.SelectId;
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            ResultHistoryOutput resultHistoryOutput = ResultHistoryOutput;
            //
            if (resultHistoryOutput.CreationData != null && IsSelectionGeometryBased())
                return resultHistoryOutput.CreationData.IsGeometryIdBased(defaultMode);
            else return defaultMode;
        }
    }
}
