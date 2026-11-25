// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaeGlobals;
using CaeMesh;
using CaeModel;
using UserControls;

namespace PrePoMax.Forms
{
    class FrmElementQuality : FrmPropertyListView, IFormBase, IFormHighlight
    {
        // Variables                                                                                                                
        private ViewElementQuality _viewElementQuality;
        private Dictionary<int, double> _elementQualities;
        private int[] _highlightedElementIds;
        private ElementQualityMetricEnum? _computedMetric;
        private bool _computingElementQuality;
        private AnnotateWithColorEnum? _annotateWithColorBeforeForm;
        private ElementQualityMetricEnum _elementQualityMetricBeforeForm;
        private bool _keepAnnotation;
        private Controller _controller;


        // Properties                                                                                                               
        public ViewElementQuality ElementQuality { get { return _viewElementQuality; } set { _viewElementQuality = value; } }


        // Constructors                                                                                                             
        public FrmElementQuality(Controller controller)
            :base(1.8)
        {
            InitializeComponent();
            //
            _viewElementQuality = new ViewElementQuality();
            _elementQualities = null;
            _highlightedElementIds = null;
            _computedMetric = null;
            _computingElementQuality = false;
            _annotateWithColorBeforeForm = null;
            _keepAnnotation = false;
            _controller = controller;
            //
            propertyGrid.SelectedObject = _viewElementQuality;
            //
            _addNew = false;
            //
            btnOkAddNew.Text = "Create Set";
            btnCancel.Text = "Close";
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
            this.gbType.Text = "Quality metric";
            // 
            // lvTypes
            // 
            this.lvTypes.Size = new System.Drawing.Size(298, 80);
            // 
            // gbProperties
            // 
            this.gbProperties.Location = new System.Drawing.Point(12, 126);
            this.gbProperties.Size = new System.Drawing.Size(310, 342);
            // 
            // propertyGrid
            // 
            this.propertyGrid.Size = new System.Drawing.Size(298, 314);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(160, 474);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(241, 474);
            // 
            // btnOkAddNew
            // 
            this.btnOkAddNew.Location = new System.Drawing.Point(79, 474);
            // 
            // FrmElementQuality
            // 
            this.ClientSize = new System.Drawing.Size(334, 509);
            this.Name = "FrmElementQuality";
            this.Text = "Element Quality";
            this.gbType.ResumeLayout(false);
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Overrides                                                                                                                
        protected override void OnPropertyGridPropertyValueChanged()
        {
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (property == nameof(_viewElementQuality.ColorElements))
            {
                ShowElementQualityAnnotation();
                HighlightElements();
            }
            else if (property == nameof(_viewElementQuality.HighlightCriteria) ||
                property == nameof(_viewElementQuality.HighlightLimit) ||
                property == nameof(_viewElementQuality.HighlightElements))
            {
                ShowElementQuality();
            }
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnListViewTypeSelectedIndexChanged()
        {
            // The gmsh metrics pump the messages while they are computed, so a selection made meanwhile arrives here
            // again; the metric selected last is computed once the current one is done
            if (_computingElementQuality) return;
            //
            _computingElementQuality = true;
            try
            {
                while (!_closing && lvTypes.SelectedItem != null)
                {
                    ElementQualityMetricEnum metric = (ElementQualityMetricEnum)lvTypes.SelectedItem.Tag;
                    if (metric == _computedMetric) break;
                    //
                    _viewElementQuality.ElementQualityMetric = metric;
                    GetElementQuality();
                }
            }
            finally
            {
                _computingElementQuality = false;
            }
        }
        protected override void OnApply(bool onOkAddNew)
        {
            // OK closes the dialog and keeps the color annotation
            if (!onOkAddNew)
            {
                _keepAnnotation = true;
                base.OnApply(onOkAddNew);
                return;
            }
            // An element set shares its name space with the parts and the other mesh entities, since a part is
            // written to the solver input as an element set of its name
            string name = _controller.GetAllMeshEntityNames().GetNextNumberedKey("Element_qualities");
            //
            _controller.CreateElementSetFromElementQualityCommand(name, _viewElementQuality.ElementQualityMetric.ToString(),
                                                                  _controller.GetElementQualityPartNames(),
                                                                  _viewElementQuality.LargerIsWorse,
                                                                  _viewElementQuality.HighlightLimit);
            //
            base.OnApply(onOkAddNew);
        }
        protected override bool OnPrepareForm(string stepName, string itemToEditName)
        {
            // Disable selection
            _controller.SetSelectByToOff();
            //
            _annotateWithColorBeforeForm = _controller.AnnotateWithColor;
            _elementQualityMetricBeforeForm = _controller.ElementQualityMetric;
            _keepAnnotation = false;
            //
            _computedMetric = null;
            PopulateListOfMetrics();
            //
            propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            //
            return true;
        }
        protected override void OnHideOrClose()
        {
            AnnotateWithColorEnum? annotateWithColorBeforeForm = _annotateWithColorBeforeForm;
            _annotateWithColorBeforeForm = null;
            //
            base.OnHideOrClose();
            // Close returns the color annotation to its state before the dialog opened
            if (annotateWithColorBeforeForm.HasValue && !_keepAnnotation)
            {
                if (_controller.AnnotateWithColor != annotateWithColorBeforeForm.Value)
                    _controller.SetAnnotateWithColor(annotateWithColorBeforeForm.Value);
                _controller.ElementQualityMetric = _elementQualityMetricBeforeForm;
            }
        }


        // Methods
        private void PopulateListOfMetrics()
        {
            lvTypes.Items.Clear();
            ListViewItem selectedItem = null;
            foreach (ElementQualityMetricEnum metric in Enum.GetValues(typeof(ElementQualityMetricEnum)))
            {
                ListViewItem item = new ListViewItem(ViewElementQuality.GetMetricName(metric));
                item.ToolTipText = ViewElementQuality.GetMetricDescription(metric);
                item.Tag = metric;
                lvTypes.Items.Add(item);
                if (metric == _controller.ElementQualityMetric) selectedItem = item;
            }
            selectedItem.Selected = true;
        }
        private void GetElementQuality()
        {
            ElementQualityMetricEnum metric = _viewElementQuality.ElementQualityMetric;
            _computedMetric = metric;
            ShowElementQualityAnnotation();
            _elementQualities = _controller.GetElementQualityValues();
            double[] sorted = _elementQualities.Values.ToArray();
            Array.Sort(sorted);
            //
            double min = 0;
            double max = 0;
            double average = 0;
            double standardDeviation = 0;
            if (sorted.Length > 0)
            {
                min = sorted[0];
                max = sorted[sorted.Length - 1];
                foreach (double value in sorted) average += value;
                average /= sorted.Length;
                foreach (double value in sorted) standardDeviation += Math.Pow(value - average, 2);
                standardDeviation = Math.Sqrt(standardDeviation / sorted.Length);
            }
            _viewElementQuality.SetValues(min, max, average, standardDeviation, sorted.Length);
            //
            bool largerIsWorse;
            double limit;
            if (!ViewElementQuality.GetDefaultLimit(metric, out largerIsWorse, out limit) && sorted.Length > 0)
            {
                // The worst 1 % of the elements are past the limit
                limit = sorted[(int)((sorted.Length - 1) * (largerIsWorse ? 0.99 : 0.01))];
            }
            _viewElementQuality.SetLimit(largerIsWorse, limit);
            //
            ShowElementQuality();
        }
        // Without the colors the view shows the color annotation the dialog opened with, unless that one was the element
        // quality, so the highlighted elements stand out
        private void ShowElementQualityAnnotation()
        {
            ElementQualityMetricEnum metric = _viewElementQuality.ElementQualityMetric;
            if (_viewElementQuality.ColorElements)
            {
                _controller.ShowElementQualityAnnotation(metric);
            }
            else
            {
                AnnotateWithColorEnum annotateWithColor = _annotateWithColorBeforeForm ?? AnnotateWithColorEnum.None;
                if (annotateWithColor == AnnotateWithColorEnum.ElementQuality) annotateWithColor = AnnotateWithColorEnum.None;
                if (_controller.AnnotateWithColor != annotateWithColor) _controller.SetAnnotateWithColor(annotateWithColor);
                _controller.ElementQualityMetric = metric;
            }
        }
        // An element is past the limit as in CreateElementSetFromElementQuality: a larger value when it is larger than
        // the limit and a smaller value when it is not larger
        private bool IsPastLimit(double value)
        {
            return _viewElementQuality.LargerIsWorse == value > _viewElementQuality.HighlightLimit;
        }
        private void ShowElementQuality()
        {
            List<int> highlightedElementIds = new List<int>();
            foreach (var entry in _elementQualities)
            {
                if (IsPastLimit(entry.Value)) highlightedElementIds.Add(entry.Key);
            }
            _highlightedElementIds = highlightedElementIds.ToArray();
            _viewElementQuality.NumberOfHighlightedElements = _highlightedElementIds.Length;
            propertyGrid.Refresh();
            //
            HighlightElements();
        }
        private void HighlightElements()
        {
            _controller.ClearAllSelection();
            //
            if (_highlightedElementIds == null || !_viewElementQuality.HighlightElements) return;
            //
            if (_highlightedElementIds.Length > 0) _controller.HighlightElements(_highlightedElementIds);
        }
        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightElements();
        }

    }
}
