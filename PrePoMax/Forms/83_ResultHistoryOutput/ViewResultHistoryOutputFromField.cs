using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeResults;

namespace PrePoMax
{
    [Serializable]
    public class ViewResultHistoryOutputFromField : ViewResultHistoryOutput
    {
        // Variables                                                                                                                
        private bool _complexVisible;
        private ResultHistoryOutputFromField _historyOutput;
        private MultiChoiceContainer _componentContainer;
        private Dictionary<string, string[]> _filedNameComponentNames;
        private Dictionary<string, string[]> _stepIdStepIncrementIds;


        // Properties                                                                                                               
        public override string Name { get { return _historyOutput.Name; } set { _historyOutput.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Node set")]
        [DescriptionAttribute("Select the node set for the creation of the history output.")]
        [Id(3, 2)]
        public string NodeSetName { get { return _historyOutput.RegionName; } set { _historyOutput.RegionName = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(3, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the creation of the history output.")]
        [Id(4, 2)]
        public string SurfaceName { get { return _historyOutput.RegionName; } set { _historyOutput.RegionName = value; } }
        //
        [CategoryAttribute("Source")]
        [OrderedDisplayName(0, 10, "Field name")]
        [DescriptionAttribute("Filed name for the history output.")]
        [Id(1, 3)]
        public string FieldName
        {
            get { return _historyOutput.FieldName; }
            set
            {
                if (_historyOutput.FieldName != value)
                {
                    _historyOutput.FieldName = value;
                    UpdateComponents();
                }
            }
        }
        //
        [CategoryAttribute("Source")]
        [OrderedDisplayName(1, 10, "Components")]
        [DescriptionAttribute("Component names for the history output.")]
        [Id(2, 3)]
        public MultiChoiceEnum ComponentNames
        {
            get
            {
                if (_componentContainer == null) return MultiChoiceEnum.Num1;   // at initialization
                else return _componentContainer.MultiChoice;
            }
            set
            {
                _componentContainer.MultiChoice = value;
                _historyOutput.ComponentNames = _componentContainer.Names;
            }
        }
        //
        [CategoryAttribute("Source")]
        [OrderedDisplayName(2, 10, "Complex")]
        [DescriptionAttribute("Complex component for the history output.")]
        [Id(3, 3)]
        public ComplexResultTypeEnum ComplexResultType
        {
            get { return _historyOutput.ComplexResultType; }
            set
            {
                _historyOutput.ComplexResultType = value;
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("Source")]
        [OrderedDisplayName(3, 10, "Angle")]
        [DescriptionAttribute("Angle for the history output.")]
        [TypeConverter(typeof(StringAngleDegConverter))]
        [Id(4, 3)]
        public double ComplexAngleDeg
        {
            get { return _historyOutput.ComplexAngleDeg; }
            set { _historyOutput.ComplexAngleDeg = value; }
        }
        //
        [CategoryAttribute("Source")]
        [OrderedDisplayName(4, 10, "Step id")]
        [DescriptionAttribute("Step id for the history output.")]
        [Id(5, 3)]
        public string StepId
        {
            get
            {
                if (_historyOutput.StepId == -1) return ResultHistoryOutputFromField.AllSteps;
                else return _historyOutput.StepId.ToString();
            }
            set
            {
                if (value == ResultHistoryOutputFromField.AllSteps) _historyOutput.StepId = -1;
                else
                {
                    if (int.TryParse(value, out int stepId)) _historyOutput.StepId = stepId;
                    else throw new NotSupportedException();
                }
                UpdateStepIncrements();
            }
        }
        //
        [CategoryAttribute("Source")]
        [OrderedDisplayName(5, 10, "Increment id")]
        [DescriptionAttribute("Increment id for the history output.")]
        [Id(6, 3)]
        public string StepIncrementId
        {
            get
            {
                if (_historyOutput.StepIncrementId == -1) return ResultHistoryOutputFromField.AllIncrements;
                else return _historyOutput.StepIncrementId.ToString();
            }
            set
            {
                if (value == ResultHistoryOutputFromField.AllIncrements) _historyOutput.StepIncrementId = -1;
                else
                {
                    if (int.TryParse(value, out int incrementId)) _historyOutput.StepIncrementId = incrementId;
                    else throw new NotSupportedException();
                    //
                    UpdateVisibility();
                }
            }
        }
        //
        [CategoryAttribute("Source")]
        [OrderedDisplayName(6, 10, "Harmonic")]
        [DescriptionAttribute("Output harmonic oscillations as the history output.")]
        [Id(7, 3)]
        public bool Harmonic { get { return _historyOutput.Harmonic; } set { _historyOutput.Harmonic = value; } }
        //
        [CategoryAttribute("Position")]
        [OrderedDisplayName(0, 10, "Node coordinates")]
        [DescriptionAttribute("Output node coordinates in the history output.")]
        [Id(1, 4)]
        public OutputNodeCoordinatesEnum OutputNodeCoordinates
        {
            get { return _historyOutput.OutputNodeCoordinates; }
            set { _historyOutput.OutputNodeCoordinates = value; }
        }
        
        
        // Constructors                                                                                                             
        public ViewResultHistoryOutputFromField(ResultHistoryOutputFromField historyOutput, bool complexVisible)
            : base(historyOutput)
        {
            // The order is important
            _historyOutput = historyOutput;
            _complexVisible = complexVisible;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.NodeSetName, nameof(NodeSetName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            //
            SetBase(_historyOutput, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            //
            DynamicCustomTypeDescriptor.GetProperty(nameof(ComplexResultType)).SetIsBrowsable(_complexVisible);
            //
            DynamicCustomTypeDescriptor.RenameBooleanPropertyToYesNo(nameof(Harmonic));
        }


        // Methods                                                                                                                  
        public override ResultHistoryOutput GetBase()
        {
            return _historyOutput;
        }
        public void PopulateDropDownLists(string[] nodeSetNames, string[] surfaceNames,
                                          Dictionary<string, string[]> filedNameComponentNames,
                                          Dictionary<int, int[]> stepIdStepIncrementIds)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.NodeSetName, nodeSetNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            base.PopulateDropDownLists(regionTypeListItemsPairs);
            // Components
            List<string> componentNames;
            _filedNameComponentNames = new Dictionary<string, string[]>();
            foreach (var fieldEntry in filedNameComponentNames)
            {
                componentNames = new List<string>();
                foreach (var componentName in fieldEntry.Value) componentNames.Add(componentName);
                if (componentNames.Count > 0) _filedNameComponentNames.Add(fieldEntry.Key, componentNames.ToArray());
            }
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(FieldName), _filedNameComponentNames.Keys.ToArray());
            UpdateComponents(_historyOutput.ComponentNames);
            // Add "All steps" and "All increments" to the step increment dictionary
            List<string> incrementIds;
            _stepIdStepIncrementIds = new Dictionary<string, string[]> { { ResultHistoryOutputFromField.AllSteps,
                new string[] { ResultHistoryOutputFromField.AllSteps } } };
            foreach (var stepEntry in stepIdStepIncrementIds)
            {
                incrementIds = new List<string>() { ResultHistoryOutputFromField.AllIncrements };
                foreach (var incrementId in stepEntry.Value) incrementIds.Add(incrementId.ToString());
                _stepIdStepIncrementIds.Add(stepEntry.Key.ToString(), incrementIds.ToArray());
            }
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(StepId), _stepIdStepIncrementIds.Keys.ToArray());
            //
            UpdateStepIncrements();
        }
        private void UpdateComponents(string[] selectedComponentNames = null)
        {
            string[] componentNames;
            if (_filedNameComponentNames != null && _filedNameComponentNames.TryGetValue(FieldName, out componentNames) &&
                componentNames.Length > 0)
            {
                if (selectedComponentNames == null) selectedComponentNames = componentNames;
                _componentContainer = new MultiChoiceContainer(componentNames, selectedComponentNames);
                DynamicCustomTypeDescriptor.RenameMultiChoiceEnumProperty(nameof(ComponentNames), _componentContainer.EnumData);
                //
                _historyOutput.ComponentNames = _componentContainer.Names;
            }
        }
        private void UpdateStepIncrements()
        {
            string[] incrementIds;
            if (_stepIdStepIncrementIds.TryGetValue(StepId, out incrementIds) && incrementIds.Length > 1)
            {
                DynamicCustomTypeDescriptor.PopulateProperty(nameof(StepIncrementId), incrementIds);
                if (!incrementIds.Contains(StepIncrementId)) StepIncrementId = incrementIds[0];
            }
            //
            UpdateVisibility();
        }
        private void UpdateVisibility()
        {
            DynamicCustomTypeDescriptor dctd = DynamicCustomTypeDescriptor;
            bool visible = ComplexResultType == ComplexResultTypeEnum.RealAtAngle;
            //
            dctd.GetProperty(nameof(ComplexAngleDeg)).SetIsBrowsable(visible);
            visible = StepId != ResultHistoryOutputFromField.AllSteps;
            dctd.GetProperty(nameof(StepIncrementId)).SetIsBrowsable(visible);
            visible = _complexVisible && ComplexResultType == ComplexResultTypeEnum.Real &&
                      visible && StepIncrementId != ResultHistoryOutputFromField.AllSteps;
            dctd.GetProperty(nameof(Harmonic)).SetIsBrowsable(visible);
        }
    }



   
}
