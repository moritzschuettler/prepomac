using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeResults;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewResultFieldOutputLimit : ViewResultFieldOutput
    {
        // Variables                                                                                                                
        private List<LimitPartDataPoint> _partPoints;
        private List<LimitElementSetDataPoint> _elementSetPoints;
        private Dictionary<string, string[]> _filedNameComponentNames;


        // Properties                                                                                                               
        private ResultFieldOutputLimit ResultFieldOutput
        {
            get { return (ResultFieldOutputLimit)_resultFieldOutput; }
        }
        public override string Name { get { return ResultFieldOutput.Name; } set { ResultFieldOutput.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Field name")]
        [DescriptionAttribute("Filed name for the field output.")]
        [Id(2, 1)]
        public string FieldName
        {
            get { return ResultFieldOutput.FieldName; }
            set
            {
                if (ResultFieldOutput.FieldName != value)
                {
                    ResultFieldOutput.FieldName = value;
                    UpdateComponents();
                }
            }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Component name")]
        [DescriptionAttribute("Component name for the field output.")]
        [Id(3, 1)]
        public string ComponentName
        {
            get { return ResultFieldOutput.ComponentName; }
            set { ResultFieldOutput.ComponentName = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Limit based on")]
        [DescriptionAttribute("Select how the limit values will be defined for the field output.")]
        [Id(4, 1)]
        public LimitPlotBasedOnEnum LimitPlotBasedOn
        {
            get { return ResultFieldOutput.LimitPlotBasedOn; }
            set { ResultFieldOutput.LimitPlotBasedOn = value; }
        }
        //
        [Browsable(false)]
        public List<LimitPartDataPoint> PartPoints
        {
            get { return _partPoints; }
            set { _partPoints = value; }
        }
        //
        [Browsable(false)]
        public List<LimitElementSetDataPoint> ElementSetPoints
        {
            get { return _elementSetPoints; }
            set { _elementSetPoints = value; }
        }
        [Browsable(false)]
        public object DataPoints
        {
            get
            {
                if (LimitPlotBasedOn == LimitPlotBasedOnEnum.Parts) return _partPoints;
                if (LimitPlotBasedOn == LimitPlotBasedOnEnum.ElementSets) return _elementSetPoints;
                else throw new NotSupportedException();
            }
        }


        // Constructors                                                                                                             
        public ViewResultFieldOutputLimit(ResultFieldOutputLimit resultFieldOutput, string[] partNames,
                                          string[] elementSetNames, ref bool propertyChanged)
            : base(resultFieldOutput)
        {
            // Parts
            bool valid = true;
            double limit;
            _partPoints = new List<LimitPartDataPoint>();
            foreach (var partName in partNames)
            {
                limit = 0;
                valid &= ResultFieldOutput.ItemNameLimit.TryGetValue(partName, out limit);
                _partPoints.Add(new LimitPartDataPoint(partName, limit));
            }
            // Element sets
            if (elementSetNames.Length == 0) elementSetNames = new string[] { ResultFieldOutputLimit.AllElementsName };
            //
            _elementSetPoints = new List<LimitElementSetDataPoint>();
            foreach (var elementSetName in elementSetNames)
            {
                limit = 0;
                valid &= ResultFieldOutput.ItemNameLimit.TryGetValue(elementSetName, out limit);
                _elementSetPoints.Add(new LimitElementSetDataPoint(elementSetName, limit));
            }
            //
            if (!valid) propertyChanged = true;
        }


        // Methods                                                                                                                  
        public override ResultFieldOutput GetBase()
        {
            ResultFieldOutput.ItemNameLimit.Clear();
            //
            if (LimitPlotBasedOn == LimitPlotBasedOnEnum.Parts)
            {
                foreach (var point in _partPoints)
                {
                    ResultFieldOutput.ItemNameLimit.Add(point.PartName, point.Limit);
                }
            }
            else if (LimitPlotBasedOn == LimitPlotBasedOnEnum.ElementSets)
            {
                foreach (var point in _elementSetPoints)
                {
                    ResultFieldOutput.ItemNameLimit.Add(point.ElementSetName, point.Limit);
                }
            }
            else throw new NotSupportedException();
            //
            return _resultFieldOutput;
        }
        public void PopulateDropDownLists(Dictionary<string, string[]> filedNameComponentNames)
        {
            _filedNameComponentNames = filedNameComponentNames;
            _dctd.PopulateProperty(nameof(FieldName), _filedNameComponentNames.Keys.ToArray());
            UpdateComponents();
        }
        private void UpdateComponents()
        {
            string[] componentNames;
            if (_filedNameComponentNames != null && _filedNameComponentNames.TryGetValue(FieldName, out componentNames) &&
                componentNames != null && componentNames.Length > 0)
            {
                _dctd.PopulateProperty(nameof(ComponentName), componentNames);
                if (!componentNames.Contains(ComponentName)) ComponentName = componentNames[0];
            }
        }
        private void UpdateVisibility()
        {
           
        }
    }



   
}
