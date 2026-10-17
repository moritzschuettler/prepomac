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
using CaeGlobals;
using CaeMesh;
using CaeModel;

namespace PrePoMax.Forms
{
    class FrmMaterialOrientation : UserControls.FrmProperties, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        private string[] _materialOrientationNames;
        private string _materialOrientationToEditName;
        private ViewMaterialOrientation _viewMaterialOrientation;
        private Controller _controller;


        // Properties                                                                                                               
        public MaterialOrientation MaterialOrientation
        {
            get { return _viewMaterialOrientation != null ? _viewMaterialOrientation.GetBase() : null; }
            set { _viewMaterialOrientation = new ViewMaterialOrientation(value.DeepClone()); }
        }


        // Constructors                                                                                                             
        public FrmMaterialOrientation(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewMaterialOrientation = null;
        }
        private void InitializeComponent()
        {
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            //
            // FrmMaterialOrientation
            //
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Name = "FrmMaterialOrientation";
            this.Text = "Edit Material Orientation";
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
        protected override void OnPropertyGridPropertyValueChanged()
        {
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (property == nameof(_viewMaterialOrientation.RegionType)) ShowHideSelectionForm();
            //
            HighlightMaterialOrientation();
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            if (propertyGrid.SelectedObject is ViewError ve) throw new CaeException(ve.Message);
            //
            _viewMaterialOrientation = (ViewMaterialOrientation)propertyGrid.SelectedObject;
            MaterialOrientation materialOrientation = MaterialOrientation;
            if (materialOrientation == null) throw new CaeException("No material orientation was selected.");
            // Check if the name exists
            CheckName(_materialOrientationToEditName, materialOrientation.Name, _materialOrientationNames,
                      "material orientation");
            // Check equations
            materialOrientation.CheckEquations();
            //
            if (materialOrientation.RegionType == RegionTypeEnum.Selection &&
                (materialOrientation.CreationIds == null || materialOrientation.CreationIds.Length == 0))
                throw new CaeException("The material orientation selection must contain at least one item.");
            //
            if (!AreRegionElementsShellElements())
                throw new CaeException("A material orientation applies to shell elements only.");
            // Create
            if (_materialOrientationToEditName == null)
            {
                _controller.AddMaterialOrientationCommand(materialOrientation);
            }
            // Replace
            else if (_propertyItemChanged)
            {
                _controller.ReplaceMaterialOrientationCommand(_materialOrientationToEditName, materialOrientation);
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
        protected override bool OnPrepareForm(string stepName, string materialOrientationToEditName)
        {
            this.btnOkAddNew.Visible = materialOrientationToEditName == null;
            //
            _propertyItemChanged = false;
            _materialOrientationNames = _controller.GetMaterialOrientationNames();
            _materialOrientationToEditName = materialOrientationToEditName;
            _viewMaterialOrientation = null;
            propertyGrid.SelectedObject = null;
            //
            List<string> shellPartNames = new List<string>();
            foreach (var entry in _controller.Model.Mesh.Parts)
            {
                if (entry.Value.PartType == PartType.Shell) shellPartNames.Add(entry.Key);
            }
            string[] partNames = shellPartNames.ToArray();
            string[] elementSetNames = _controller.GetUserElementSetNames();
            string[] coordinateSystemNames = _controller.GetModelCoordinateSystemNames();
            if (elementSetNames == null) elementSetNames = new string[0];
            if (coordinateSystemNames == null) coordinateSystemNames = new string[0];
            // Create new material orientation
            if (_materialOrientationToEditName == null)
            {
                MaterialOrientation = new MaterialOrientation(GetMaterialOrientationName(), "", RegionTypeEnum.Selection);
            }
            // Edit existing material orientation
            else
            {
                MaterialOrientation = _controller.GetMaterialOrientation(_materialOrientationToEditName); // to clone
                MaterialOrientation materialOrientation = MaterialOrientation;
                if (materialOrientation.CreationData != null)
                {
                    if (!_controller.Model.IsMaterialOrientationRegionValid(materialOrientation) ||
                        !_controller.Model.RegionValid(materialOrientation))
                    {
                        // Region invalid
                        materialOrientation.CreationData = null;
                        materialOrientation.CreationIds = null;
                        _propertyItemChanged = true;
                    }
                    materialOrientation.RegionType = RegionTypeEnum.Selection;
                }
                // Check for deleted entities
                ViewMaterialOrientation vmo = _viewMaterialOrientation;
                if (vmo.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                else if (vmo.RegionType == RegionTypeEnum.PartName.ToFriendlyString())
                    CheckMissingValueRef(ref partNames, vmo.PartName, s => { vmo.PartName = s; });
                else if (vmo.RegionType == RegionTypeEnum.ElementSetName.ToFriendlyString())
                    CheckMissingValueRef(ref elementSetNames, vmo.ElementSetName, s => { vmo.ElementSetName = s; });
                else throw new NotSupportedException();
                //
                if (vmo.CoordinateSystemName != CoordinateSystem.DefaultCoordinateSystemName)
                    CheckMissingValueRef(ref coordinateSystemNames, vmo.CoordinateSystemName,
                                         s => { vmo.CoordinateSystemName = s; });
            }
            _viewMaterialOrientation.PopulateDropDownLists(partNames, elementSetNames, coordinateSystemNames);
            propertyGrid.SelectedObject = _viewMaterialOrientation;
            //
            ShowHideSelectionForm();
            //
            HighlightMaterialOrientation();
            //
            propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            //
            return true;
        }


        // Methods                                                                                                                  
        private string GetMaterialOrientationName()
        {
            return _materialOrientationNames.GetNextNumberedKey("Material_Orientation");
        }
        private bool AreRegionElementsShellElements()
        {
            MaterialOrientation materialOrientation = MaterialOrientation;
            if (materialOrientation.RegionType == RegionTypeEnum.Selection)
                return _controller.AreElementsAllShellElements(materialOrientation.CreationIds);
            else if (materialOrientation.RegionType == RegionTypeEnum.PartName)
                return _controller.Model.Mesh.Parts[materialOrientation.RegionName].PartType == PartType.Shell;
            else if (materialOrientation.RegionType == RegionTypeEnum.ElementSetName)
                return _controller.AreElementsAllShellElements(_controller.Model.GetElementIdsOfRegion(materialOrientation));
            else return false;
        }
        private void HighlightMaterialOrientation()
        {
            try
            {
                _controller.ClearSelectionHistory();
                //
                MaterialOrientation materialOrientation = MaterialOrientation;
                if (materialOrientation == null) return;
                //
                // The region is shown by its outline, so the faces leave the axes visible
                if (materialOrientation.RegionType == RegionTypeEnum.Selection)
                {
                    SetSelectItem();
                    //
                    if (materialOrientation.CreationData != null)
                        _controller.Selection = materialOrientation.CreationData.DeepCopy(true, false);
                }
                _controller.HighlightMaterialOrientation(materialOrientation);
            }
            catch { }
        }
        private void ShowHideSelectionForm()
        {
            if (MaterialOrientation != null && MaterialOrientation.RegionType == RegionTypeEnum.Selection)
                ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
            else
                ItemSetDataEditor.SelectionForm.Hide();
            //
            SetSelectItem();
        }
        private void SetSelectItem()
        {
            if (MaterialOrientation != null && MaterialOrientation.RegionType == RegionTypeEnum.Selection)
                _controller.SetSelectItemToElement();
            else
                _controller.SetSelectByToOff();
        }
        //
        public void SelectionChanged(int[] ids)
        {
            MaterialOrientation materialOrientation = MaterialOrientation;
            if (materialOrientation != null && materialOrientation.RegionType == RegionTypeEnum.Selection)
            {
                materialOrientation.CreationIds = ids;
                materialOrientation.CreationData = _controller.Selection.DeepCopy(true, false);
                //
                propertyGrid.Refresh();
                //
                _propertyItemChanged = true;
                //
                if (ids.Length != 0) HighlightMaterialOrientation();
            }
        }
        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightMaterialOrientation();
        }
        // IFormItemSetDataParent
        public bool IsSelectionGeometryBased()
        {
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            MaterialOrientation materialOrientation = MaterialOrientation;
            //
            if (materialOrientation != null && materialOrientation.CreationData != null)
                return materialOrientation.CreationData.IsGeometryBased();
            else return true;
        }
        public bool IsGeometrySelectionIdBased()
        {
            bool defaultMode = _controller.Settings.Pre.GeometrySelectMode == GeometrySelectModeEnum.SelectId;
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            MaterialOrientation materialOrientation = MaterialOrientation;
            //
            if (materialOrientation != null && materialOrientation.CreationData != null && IsSelectionGeometryBased())
                return materialOrientation.CreationData.IsGeometryIdBased(defaultMode);
            else return defaultMode;
        }
    }
}
