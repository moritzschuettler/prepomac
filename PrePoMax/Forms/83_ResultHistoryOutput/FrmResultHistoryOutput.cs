using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeResults;
using CaeGlobals;
using System.Windows.Forms;
using CaeModel;

namespace PrePoMax.Forms
{
    class FrmResultHistoryOutput : UserControls.FrmPropertyListView, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        private string[] _resultHistoryOutputSetNames;
        private string _resultHistoryOutputToEditName;
        private ViewResultHistoryOutput _viewResultHistoryOutput;
        private Controller _controller;


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
                else throw new NotImplementedException();
            }
        }


        // Constructors                                                                                                             
        public FrmResultHistoryOutput(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewResultHistoryOutput = null;
        }
        private void InitializeComponent()
        {
            this.gbType.SuspendLayout();
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbType
            // 
            this.gbType.Size = new System.Drawing.Size(310, 108);
            // 
            // lvTypes
            // 
            this.lvTypes.Size = new System.Drawing.Size(298, 80);
            // 
            // gbProperties
            // 
            this.gbProperties.Location = new System.Drawing.Point(12, 126);
            this.gbProperties.Size = new System.Drawing.Size(310, 324);
            // 
            // propertyGrid
            // 
            this.propertyGrid.Size = new System.Drawing.Size(298, 296);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(160, 456);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(241, 456);
            // 
            // btnOkAddNew
            // 
            this.btnOkAddNew.Location = new System.Drawing.Point(79, 456);
            // 
            // FrmResultHistoryOutput
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(334, 491);
            this.MinimumSize = new System.Drawing.Size(350, 530);
            this.Name = "FrmResultHistoryOutput";
            this.Text = "Edit History Output";
            this.gbType.ResumeLayout(false);
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Overrides                                                                                                                
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible) ShowHideSelectionForm();   // accounts for minimizing/maximizing the main form
            //
            base.OnVisibleChanged(e);
        }
        protected override void OnListViewTypeSelectedIndexChanged()
        {
            if (lvTypes.SelectedItems != null && lvTypes.SelectedItems.Count > 0)
            {
                object itemTag = lvTypes.SelectedItems[0].Tag;
                if (itemTag is ViewError) _viewResultHistoryOutput = null;
                else if (itemTag is ViewResultHistoryOutputFromField vrhoff) _viewResultHistoryOutput = vrhoff;
                else if (itemTag is ViewResultHistoryOutputFromEquation vrhofe) _viewResultHistoryOutput = vrhofe;
                else throw new NotImplementedException();
                //
                ShowHideSelectionForm();
                //
                propertyGrid.SelectedObject = itemTag;
                //
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
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            if (propertyGrid.SelectedObject is ViewError ve) throw new CaeException(ve.Message);
            //
            _viewResultHistoryOutput = (ViewResultHistoryOutput)propertyGrid.SelectedObject;
            //
            if (ResultHistoryOutput == null) throw new CaeException("No history output was selected.");
            // Check if the name exists
            CheckName(_resultHistoryOutputToEditName, ResultHistoryOutput.Name, _resultHistoryOutputSetNames, "history output");
            // Check selection
            if (ResultHistoryOutput.RegionType == RegionTypeEnum.Selection &&
                (ResultHistoryOutput.CreationIds == null || ResultHistoryOutput.CreationIds.Length == 0))
                throw new CaeException("The history output selection must contain at least one item.");
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
            //
            _stepName = stepName;
            _resultHistoryOutputSetNames = _controller.GetHistoryResultSetNames();
            _resultHistoryOutputToEditName = historyOutputToEditName;
            string[] nodeSetNames = _controller.GetResultUserNodeSetNames();
            string[] surfaceNames = _controller.GetResultUserSurfaceNames();
            Dictionary<string, string[]> filedNameComponentNames =
                _controller.CurrentResult.GetAllVisibleFiledNameComponentNames();
            Dictionary<int, int[]> stepIdStepIncrementIds = _controller.CurrentResult.GetAllExistingIncrementIds();
            //
            if (_resultHistoryOutputSetNames == null)
                throw new CaeException("The history output names must be defined first.");
            // Populate list view
            PopulateListOfHistoryOutputs(nodeSetNames, surfaceNames, filedNameComponentNames, stepIdStepIncrementIds);
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
                    string[] componentNames = filedNameComponentNames[vrhoff.FieldName];
                    ResultHistoryOutputFromField hroff = (ResultHistoryOutputFromField)vrhoff.GetBase();
                    string[] selectedComponentNames = hroff.ComponentNames;
                    string[] intersection = componentNames.Intersect(selectedComponentNames).ToArray();
                    if (intersection.Length != selectedComponentNames.Length)
                    {
                        if (intersection.Length > 0) hroff.ComponentNames = intersection;
                        else hroff.ComponentNames = componentNames;
                        //
                        MessageBoxes.ShowWarning("Some selected components no longer exist. The selected components were " +
                            "cnaged to existing ones");
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
                else throw new NotSupportedException();
                //
                lvTypes.Items[selectedId].Tag = _viewResultHistoryOutput;
                _preselectIndex = selectedId;
            }
            //
            ShowHideSelectionForm();
            //
            return true;
        }


        // Methods                                                                                                                  
        private void PopulateListOfHistoryOutputs(string[] nodeSetNames, string[] surfaceNames,
                                                  Dictionary<string, string[]> filedNameComponentNames,
                                                  Dictionary<int, int[]> stepIdStepIncrementIds) 
        {
            ListViewItem item;
            FieldData fieldData = _controller.CurrentFieldData;
            // History output from field output
            item = new ListViewItem("From Field Output");
            ResultHistoryOutputFromField rhoff =
                new ResultHistoryOutputFromField(GetHistoryOutputName("From_Field"), fieldData.Name, null, "", RegionTypeEnum.Selection);
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
        }
        private string GetHistoryOutputName(string prefix)
        {
            return _resultHistoryOutputSetNames.GetNextNumberedKey(prefix);
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
                            _controller.Selection = ResultHistoryOutput.CreationData.DeepClone();
                            _controller.HighlightSelection();
                        }
                    }
                    else throw new NotImplementedException();
                }
                else if (ResultHistoryOutput is ResultHistoryOutputFromEquation)
                { }
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
                if (ResultHistoryOutput is null) { }
                else if (ResultHistoryOutput is ResultHistoryOutputFromField) _controller.SetSelectItemToNode();
            }
            else _controller.SetSelectByToOff();
        }
        //
        public void SelectionChanged(int[] ids)
        {
            if (ResultHistoryOutput != null && ResultHistoryOutput.RegionType == RegionTypeEnum.Selection)
            {
                if (ResultHistoryOutput is ResultHistoryOutputFromField)
                {
                    ResultHistoryOutput.CreationIds = ids;
                    ResultHistoryOutput.CreationData = _controller.Selection.DeepClone();
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
