using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using DynamicTypeDescriptor;
using CaeGlobals;
using CaeResults;
using System.Runtime.Serialization;

namespace CaeModel
{
    [Serializable]
    public enum DefinedTemperatureTypeEnum
    {
        [StandardValue("ByValue", DisplayName = "By value")]
        ByValue,
        [StandardValue("FromFile", DisplayName = "From file")]
        FromFile
    }
    //
    [Serializable]
                
    public class DefinedTemperature : DefinedField, IPreviewable, ISerializable
    {
        // Variables                                                                                                                
        private DefinedTemperatureTypeEnum _definedTemperatureType;     //ISerializable
        private EquationContainer _temperature;                         //ISerializable
        private string _fileName;                                       //ISerializable
        private int _stepNumber;                                        //ISerializable


        // Properties                                                                                                               
        public DefinedTemperatureTypeEnum Type { get { return _definedTemperatureType; } set { _definedTemperatureType = value; } }
        public EquationContainer Temperature { get { return _temperature; } set { SetTemp(value); } }
        public string FileName { get { return _fileName; } set { _fileName = value; } }
        public int StepNumber
        {
            get { return _stepNumber; }
            set
            {
                _stepNumber = value;
                if (_stepNumber < 1) _stepNumber = 1;
            }
        }


        // Constructors                                                                                                             
        public DefinedTemperature(string name, string regionName, RegionTypeEnum regionType, double temperature)
            : base(name, regionName, regionType)
        {
            _definedTemperatureType = DefinedTemperatureTypeEnum.ByValue;
            Temperature = new EquationContainer(typeof(StringTemperatureConverter), temperature, null);
            _fileName = null;
            _stepNumber = 1;
        }
        public DefinedTemperature(SerializationInfo info, StreamingContext context)
           : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_definedTemperatureType":
                        _definedTemperatureType = (DefinedTemperatureTypeEnum)entry.Value; break;
                    case "_temperature":
                        // Compatibility for version v2.2.3
                        if (entry.Value is double valueT)
                            Temperature = new EquationContainer(typeof(StringTemperatureConverter), valueT);
                        else
                            SetTemp((EquationContainer)entry.Value, false);
                        break;
                    case "_fileName":
                        _fileName = (string)entry.Value; break;
                    case "_stepNumber":
                        _stepNumber = (int)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        private void SetTemp(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _temperature, value, null, null, checkEquation);
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _temperature.CheckEquation();
        }
        // IPreviewable
        public FeResults GetPreview(FeMesh targetMesh, string resultName, UnitSystem unitSystem)
        {
            if (Type == DefinedTemperatureTypeEnum.ByValue)
            {
                PartExchangeData allData = new PartExchangeData();
                targetMesh.GetAllNodesAndCells(out allData.Nodes.Ids, out allData.Nodes.Coor, out allData.Cells.Ids,
                                               out allData.Cells.CellNodeIds, out allData.Cells.Types);
                //
                FeNodeSet nodeSet;
                if (RegionType == RegionTypeEnum.NodeSetName)
                {
                    nodeSet = targetMesh.NodeSets[RegionName];
                }
                else if (RegionType == RegionTypeEnum.SurfaceName)
                {
                    FeSurface surface = targetMesh.Surfaces[RegionName];
                    nodeSet = targetMesh.NodeSets[surface.NodeSetName];
                }
                else throw new NotSupportedException();
                //
                HashSet<int> nodeIds = new HashSet<int>(nodeSet.Labels);
                //
                float temperature = (float)_temperature.Value;
                float[] values = new float[allData.Nodes.Coor.Length];
                //
                for (int i = 0; i < values.Length; i++)
                {
                    if (nodeIds.Contains(allData.Nodes.Ids[i])) values[i] = temperature;
                    else values[i] = float.NaN;
                }
                //
                Dictionary<int, int> nodeIdsLookUp = new Dictionary<int, int>();
                for (int i = 0; i < allData.Nodes.Coor.Length; i++) nodeIdsLookUp.Add(allData.Nodes.Ids[i], i);
                FeResults results = new FeResults(resultName, unitSystem);
                results.SetMesh(targetMesh, nodeIdsLookUp);
                // Add distances
                FieldData fieldData = new FieldData(FOFieldNames.NdTemp);
                fieldData.GlobalIncrementId = 1;
                fieldData.StepType = StepTypeEnum.Static;
                fieldData.Time = 1;
                fieldData.MethodId = 1;
                fieldData.StepId = 1;
                fieldData.StepIncrementId = 1;
                // Add values
                Field field = new Field(fieldData.Name);
                field.AddComponent(FOComponentNames.T, values);
                results.AddField(fieldData, field);
                //
                return results;
            }
            else if (Type == DefinedTemperatureTypeEnum.FromFile)
                throw new CaeException("It is not possible to preview this defined field type.");
            else
                throw new NotSupportedException();
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_definedTemperatureType", _definedTemperatureType, typeof(DefinedTemperatureTypeEnum));
            info.AddValue("_temperature", _temperature, typeof(EquationContainer));
            info.AddValue("_fileName", _fileName, typeof(string));
            info.AddValue("_stepNumber", _stepNumber, typeof(int));
        }
    }
}
