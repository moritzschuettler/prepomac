using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaeGlobals;
using CaeModel;
using UserControls;

namespace PrePoMax.Forms
{
    class FrmElementQuality : FrmProperties, IFormBase, IFormHighlight
    {
        // Variables                                                                                                                
        private string[] _partNames;
        private ViewElementQuality _viewElementQuality;
        private Dictionary<int, double> _elementQualities;
        private Controller _controller;


        // Properties                                                                                                               
        public string[] PartNames { get { return _partNames; } set { _partNames = value; } }
        public ViewElementQuality ElementQuality { get { return _viewElementQuality; } set { _viewElementQuality = value; } }


        // Constructors                                                                                                             
        public FrmElementQuality(Controller controller)
        {
            InitializeComponent();
            //
            _partNames = null;
            _viewElementQuality = new ViewElementQuality();
            _elementQualities = null;
            _controller = controller;
            //
            propertyGrid.SelectedObject = _viewElementQuality;
            //
            _addNew = false;
            btnOK.Visible = false;
            //
            btnOkAddNew.Text = "Create Set";
            btnOkAddNew.Location = btnOK.Location;
            btnCancel.Text = "Close";
            //
            propertyGrid.SetLabelColumnWidth(1.9);
        }
        private void InitializeComponent()
        {
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(160, 376);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(241, 376);
            // 
            // btnOkAddNew
            // 
            this.btnOkAddNew.Location = new System.Drawing.Point(79, 376);
            this.btnOkAddNew.Size = new System.Drawing.Size(75, 23);
            this.btnOkAddNew.Text = "Apply";
            // 
            // FrmPartProperties
            // 
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Name = "FrmElementQuality";
            this.Text = "Element Quality";
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Overrides                                                                                                                
        protected override void OnPropertyGridPropertyValueChanged()
        {
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (property == nameof(_viewElementQuality.ElementQualityMetric))
            {
                GetElementQuality();
            }
            else if (property == nameof(_viewElementQuality.HighlightCriteria) ||
                     property == nameof(_viewElementQuality.HighlightLimit))
            {
                HighlightElements();
            }
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            string name = _controller.Model.Mesh.ElementSets.GetNextNumberedKey("Element_qualities");
            bool largerThan = _viewElementQuality.HighlightCriteria == GmshElementQualityHighlightCriteriaEnum.Larger;
            //
            _controller.CreateElementSetFromElementQualityCommand(name, _viewElementQuality.ElementQualityMetric.ToString(),
                                                                  _partNames, largerThan, _viewElementQuality.HighlightLimit);
            //
            base.OnApply(onOkAddNew);
        }
        protected override bool OnPrepareForm(string stepName, string itemToEditName)
        {
            // Disable selection
            _controller.SetSelectByToOff();
            //
            GetElementQuality();
            //
            propertyGrid.Select();
            //
            return true;
        }


        // Methods                                                                                                                  
        private void GetElementQuality()
        {
            _elementQualities = _controller.GetElementQuality(_viewElementQuality.ElementQualityMetric.ToString(), _partNames);
            double[] sorted = _elementQualities.Values.ToArray();
            Array.Sort(sorted);
            //
            double min;
            double max;
            double average = 0;
            double standardDeviation = 0;
            double limit;
            //
            min = sorted[0];
            max = sorted[sorted.Length - 1];
            foreach (var entry in _elementQualities) average += entry.Value;
            average /= _elementQualities.Count;
            foreach (var entry in _elementQualities) standardDeviation += Math.Pow(entry.Value - average, 2);
            standardDeviation /= _elementQualities.Count;
            standardDeviation = Math.Sqrt(standardDeviation);
            //
            double ratio;
            if (_viewElementQuality.HighlightCriteria == GmshElementQualityHighlightCriteriaEnum.Smaller) ratio = 0.05;
            else ratio = 1 - 0.05;
            //
            limit = sorted[(int)(sorted.Length * ratio)];
            //
            _viewElementQuality.SetValues(min, max, average, standardDeviation, limit);
            propertyGrid.Refresh();
            //
            HighlightElements();
        }
        private void HighlightElements()
        {
            _controller.ClearAllSelection();
            //
            if (_elementQualities == null) return;
            //
            List<int> elementIds = new List<int>();
            if (_viewElementQuality.HighlightCriteria == GmshElementQualityHighlightCriteriaEnum.Smaller)
            {
                foreach (var entry in _elementQualities)
                {
                    if (entry.Value < _viewElementQuality.HighlightLimit) elementIds.Add(entry.Key);
                }
            }
            else if (_viewElementQuality.HighlightCriteria == GmshElementQualityHighlightCriteriaEnum.Larger)
            {
                foreach (var entry in _elementQualities)
                {
                    if (entry.Value > _viewElementQuality.HighlightLimit) elementIds.Add(entry.Key);
                }
            }
            //
            if (elementIds.Count > 0) _controller.HighlightElements(elementIds.ToArray());
        }
        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightElements();
        }

    }
}
