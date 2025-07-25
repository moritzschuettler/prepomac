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
                
    public class DefinedTemperature : DefinedField, IDistribution, IPreviewable, ISerializable
    {
        // Variables                                                                                                                
        private DefinedTemperatureTypeEnum _definedTemperatureType;     //ISerializable
        private int _nodeId;                                            //ISerializable
        private EquationContainer _temperature;                         //ISerializable
        private string _fileName;                                       //ISerializable
        private int _stepNumber;                                        //ISerializable
        private string _distributionName;                               //ISerializable


        // Properties                                                                                                               
        public DefinedTemperatureTypeEnum Type { get { return _definedTemperatureType; } set { _definedTemperatureType = value; } }
        public int NodeId { get { return _nodeId; } set { _nodeId = value; } }
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
        public string DistributionName { get { return _distributionName; } set { _distributionName = value; } }


        // Constructors                                                                                                             
        public DefinedTemperature(string name, int nodeId, double temperature, bool constant = false)
           : this(name, "", RegionTypeEnum.NodeId, temperature, constant)
        {
            _nodeId = nodeId;
        }
        public DefinedTemperature(string name, string regionName, RegionTypeEnum regionType, double temperature,
                                  bool constant = false)
            : base(name, regionName, regionType)
        {
            _definedTemperatureType = DefinedTemperatureTypeEnum.ByValue;
            _nodeId = -1;
            Temperature = new EquationContainer(typeof(StringTemperatureConverter), temperature, null, constant);
            _fileName = null;
            _stepNumber = 1;
            _distributionName = Distribution.DefaultDistributionName;
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
                    case "_nodeId":
                        _nodeId = (int)entry.Value; break;
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
                    case "_distributionName":
                        _distributionName = (string)entry.Value; break;
                    default:
                        break;
                }
            }
            // Compatibility for version v2.2.4
            if (_distributionName == null) _distributionName = Distribution.DefaultDistributionName;
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
        public FeResults GetPreview(FeModel model, string resultName, UnitSystem unitSystem)
        {
            FeMesh targetMesh = model.Mesh;
            if (Type == DefinedTemperatureTypeEnum.ByValue)
            {
                PartExchangeData allData = new PartExchangeData();
                targetMesh.GetAllNodesAndCells(out allData.Nodes.Ids, out allData.Nodes.Coor, out allData.Cells.Ids,
                                               out allData.Cells.CellNodeIds, out allData.Cells.Types);
                //
                bool addDistances = _distributionName != Distribution.DefaultDistributionName &&
                    model.Distributions[_distributionName] is MappedDistribution md &&
                    md.InterpolatorType == CloudInterpolatorEnum.ClosestPoint;
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
                float[] distancesAll = new float[allData.Nodes.Coor.Length];
                float[] distances1 = new float[allData.Nodes.Coor.Length];
                float[] distances2 = new float[allData.Nodes.Coor.Length];
                float[] distances3 = new float[allData.Nodes.Coor.Length];
                float[] temperaturesAll = new float[allData.Nodes.Coor.Length];
                //
                int count = 0;
                int nodeId;
                double[][] coor = new double[nodeSet.Labels.Length][];
                Dictionary<int, int> nodeIdArrayId = new Dictionary<int, int>();
                for (int i = 0; i < allData.Nodes.Coor.Length; i++)
                {
                    nodeId = allData.Nodes.Ids[i];
                    if (nodeIds.Contains(nodeId))
                    {
                        coor[count] = allData.Nodes.Coor[i];
                        nodeIdArrayId[nodeId] = count;
                        count++;
                    }
                }
                double[][] distances;
                double[] temperatures;
                GetTemperaturesAndDistancesForPoints(model, coor, out distances, out temperatures);
                //
                Parallel.For(0, temperaturesAll.Length, i =>
                //for (int i = 0; i < forcesAll.Length; i++)
                {
                    int nId;
                    int arrayId;
                    double[] distance;
                    double temperature;
                    //
                    nId = allData.Nodes.Ids[i];
                    if (nodeIds.Contains(nId))
                    {
                        arrayId = nodeIdArrayId[nId];
                        temperature = temperatures[arrayId];
                        //
                        if (addDistances)
                        {
                            distance = distances[arrayId];
                            distances1[i] = (float)distance[0];
                            distances2[i] = (float)distance[1];
                            distances3[i] = (float)distance[2];
                            distancesAll[i] = (float)Math.Sqrt(distance[0] * distance[0] +
                                                               distance[1] * distance[1] +
                                                               distance[2] * distance[2]);
                        }
                        temperaturesAll[i] = (float)temperature;
                    }
                    else
                    {
                        if (addDistances)
                        {
                            distances1[i] = float.NaN;
                            distances2[i] = float.NaN;
                            distances3[i] = float.NaN;
                            distancesAll[i] = float.NaN;
                        }
                        temperaturesAll[i] = float.NaN;
                    }
                }
                );
                //
                Dictionary<int, int> nodeIdsLookUp = new Dictionary<int, int>();
                for (int i = 0; i < allData.Nodes.Coor.Length; i++) nodeIdsLookUp.Add(allData.Nodes.Ids[i], i);
                FeResults results = new FeResults(resultName, unitSystem);
                results.SetMesh(targetMesh, nodeIdsLookUp);
                // Prepare field data
                Field field;
                FieldData fieldData = new FieldData(FOFieldNames.Distance);
                fieldData.GlobalIncrementId = 1;
                fieldData.StepType = StepTypeEnum.Static;
                fieldData.Time = 1;
                fieldData.MethodId = 1;
                fieldData.StepId = 1;
                fieldData.StepIncrementId = 1;
                //
                if (addDistances)
                {
                    // Distances
                    field = new Field(fieldData.Name);
                    field.AddComponent(FOComponentNames.All, distancesAll);
                    field.AddComponent(FOComponentNames.D1, distances1);
                    field.AddComponent(FOComponentNames.D2, distances2);
                    field.AddComponent(FOComponentNames.D3, distances3);
                    results.AddField(fieldData, field);
                }
                // Add temperature
                fieldData = new FieldData(fieldData);
                fieldData.Name = FOFieldNames.NdTemp;
                //
                field = new Field(fieldData.Name);
                field.AddComponent(FOComponentNames.T, temperaturesAll);
                results.AddField(fieldData, field);
                //
                return results;
            }
            else if (Type == DefinedTemperatureTypeEnum.FromFile)
                throw new CaeException("It is not possible to preview this defined field type.");
            else
                throw new NotSupportedException();
        }
        public void GetTemperaturesAndDistancesForPoints(FeModel model, double[][] points,
                                                         out double[][] distances, out double[] values)
        {
            distances = new double[points.Length][];
            values = new double[points.Length];
            double temperature = _temperature.Value;
            //
            if (_distributionName == Distribution.DefaultDistributionName)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    distances[i] = null;
                    values[i] = temperature;
                }
            }
            else
            {
                double[][] temperatures;
                Distribution distribution = model.Distributions[_distributionName];
                //
                distribution.GetMagnitudesAndDistancesForPoints(model, points, out temperatures, out distances);
                //
                for (int i = 0; i < points.Length; i++)
                {
                    if (temperatures[i].Length == 1)
                        values[i] = temperatures[i][0] * temperature;
                    else if (temperatures[i].Length == 3)
                        throw new CaeException("The selected distribution is not a scalar type distribution.");
                    else throw new NotSupportedException();
                }
            }
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_definedTemperatureType", _definedTemperatureType, typeof(DefinedTemperatureTypeEnum));
            info.AddValue("_nodeId", _nodeId, typeof(int));
            info.AddValue("_temperature", _temperature, typeof(EquationContainer));
            info.AddValue("_fileName", _fileName, typeof(string));
            info.AddValue("_stepNumber", _stepNumber, typeof(int));
            info.AddValue("_distributionName", _distributionName, typeof(string));
        }
    }
}
