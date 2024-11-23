using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using CaeResults;
using System.Data;
using System.Runtime.Serialization;

namespace CaeModel
{
    [Serializable]
    public class InitialTranslationalVelocity : InitialCondition, IPreviewable, ISerializable
    {
        // Variables                                                                                                                
        private int _nodeId;                        //ISerializable
        private EquationContainer _v1;              //ISerializable
        private EquationContainer _v2;              //ISerializable
        private EquationContainer _v3;              //ISerializable
        private EquationContainer _magnitude;       //ISerializable


        // Properties                                                                                                               
        public int NodeId { get { return _nodeId; } set { _nodeId = value; } }
        public EquationContainer V1 { get { UpdateEquations(); return _v1; } set { SetV1(value); } }
        public EquationContainer V2 { get { UpdateEquations(); return _v2; } set { SetV2(value); } }
        public EquationContainer V3 { get { UpdateEquations(); return _v3; } set { SetV3(value); } }
        public EquationContainer Magnitude { get { UpdateEquations(); return _magnitude; } set { SetMagnitude(value); } }
        public double GetComponent(int direction)
        {
            if (direction == 0) return V1.Value;
            else if (direction == 1) return V2.Value;
            else return V3.Value;
        }


        // Constructors                                                                                                             
        public InitialTranslationalVelocity(string name, int nodeId, double v1, double v2, double v3, bool twoD,
                                            bool constant = false)
            : this(name, null, RegionTypeEnum.NodeId, v1, v2, v3, twoD, constant)
        {
            _nodeId = nodeId;
        }
        public InitialTranslationalVelocity(string name, string regionName, RegionTypeEnum regionType,
                                            double v1, double v2, double v3, bool twoD, bool constant = false)
            : base(name, regionName, regionType, twoD)
        {
            _nodeId = -1;
            //
            double mag = Math.Sqrt(v1 * v1 + v2 * v2 + v3 * v3);
            V1 = new EquationContainer(typeof(StringVelocityConverter), v1, null, constant);
            V2 = new EquationContainer(typeof(StringVelocityConverter), v2, null, constant);
            V3 = new EquationContainer(typeof(StringVelocityConverter), v3, null, constant);
            Magnitude = new EquationContainer(typeof(StringVelocityConverter), mag, null, constant);
        }
        public InitialTranslationalVelocity(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_nodeId":
                        _nodeId = (int)entry.Value; break;
                    case "_v1":
                        SetV1((EquationContainer)entry.Value, false); break;
                    case "_v2":
                        SetV2((EquationContainer)entry.Value, false); break;
                    case "_v3":
                        SetV3((EquationContainer)entry.Value, false); break;
                    case "_magnitude":
                        SetMagnitude((EquationContainer)entry.Value, false); break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        private void UpdateEquations()
        {
            try
            {
                // If error catch it silently
                if (_v1.IsEquation() || _v2.IsEquation() || _v3.IsEquation()) VEquationChanged();
                else if (_magnitude.IsEquation()) MagnitudeEquationChanged();
            }
            catch (Exception ex) { }
        }
        private void SetV1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _v1, value, null, VEquationChanged, checkEquation);
        }
        private void SetV2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _v2, value, null, VEquationChanged, checkEquation);
        }
        private void SetV3(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _v3, value, Check2D, VEquationChanged, checkEquation);
        }
        private void SetMagnitude(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _magnitude, value, CheckMagnitude, MagnitudeEquationChanged, checkEquation);
        }
        //
        private void VEquationChanged()
        {
            double mag = Math.Sqrt(_v1.Value * _v1.Value + _v2.Value * _v2.Value + _v3.Value * _v3.Value);
            _magnitude.SetEquationFromValue(mag, false);
        }
        private void MagnitudeEquationChanged()
        {
            double mag = Math.Sqrt(_v1.Value * _v1.Value + _v2.Value * _v2.Value + _v3.Value * _v3.Value);
            double r;
            if (mag == 0) r = 0;
            else r = _magnitude.Value / mag;
            _v1.SetEquationFromValue(_v1.Value * r, false);
            _v2.SetEquationFromValue(_v2.Value * r, false);
            _v3.SetEquationFromValue(_v3.Value * r, false);
        }
        //
        private double Check2D(double value)
        {
            if (_twoD) return 0;
            else return value;
        }
        private double CheckMagnitude(double value)
        {
            if (value < 0) throw new Exception("Value of the velocity magnitude must be non-negative.");
            else return value;
        }
        public double[] GetDirection()
        {
            return new double[] { _v1.Value, _v2.Value, _v3.Value };
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _v1.CheckEquation();
            _v2.CheckEquation();
            _v3.CheckEquation();
            _magnitude.CheckEquation();
        }
        // IPreviewable
        public FeResults GetPreview(FeMesh targetMesh, string resultName, UnitSystem unitSystem)
        {
            PartExchangeData allData = new PartExchangeData();
            targetMesh.GetAllNodesAndCells(out allData.Nodes.Ids, out allData.Nodes.Coor, out allData.Cells.Ids,
                                           out allData.Cells.CellNodeIds, out allData.Cells.Types);
            //
            HashSet<int> nodeIds;
            if (RegionType == RegionTypeEnum.NodeSetName)
            {
                nodeIds = new HashSet<int>(targetMesh.NodeSets[RegionName].Labels);
            }
            else if (RegionType == RegionTypeEnum.SurfaceName)
            {
                string nodeSetName = targetMesh.Surfaces[RegionName].NodeSetName;
                nodeIds = new HashSet<int>(targetMesh.NodeSets[nodeSetName].Labels);
            }
            else if (RegionType == RegionTypeEnum.ReferencePointName)
            {
                nodeIds = new HashSet<int>();
            }
            else throw new NotSupportedException();
            //
            float v1 = (float)_v1.Value;
            float v2 = (float)_v2.Value;
            float v3 = (float)_v3.Value;
            float mag = (float)_magnitude.Value;
            //
            float[] values1 = new float[allData.Nodes.Coor.Length];
            float[] values2 = new float[allData.Nodes.Coor.Length];
            float[] values3 = new float[allData.Nodes.Coor.Length];
            float[] valuesAll = new float[allData.Nodes.Coor.Length];
            //
            for (int i = 0; i < allData.Nodes.Coor.Length; i++)
            {
                if (nodeIds.Contains(allData.Nodes.Ids[i]))
                {
                    values1[i] = v1;
                    values2[i] = v2;
                    values3[i] = v3;
                    valuesAll[i] = mag;
                }
                else
                {
                    values1[i] = float.NaN;
                    values2[i] = float.NaN;
                    values3[i] = float.NaN;
                    valuesAll[i] = float.NaN;
                }
            }
            //
            Dictionary<int, int> nodeIdsLookUp = new Dictionary<int, int>();
            for (int i = 0; i < allData.Nodes.Coor.Length; i++) nodeIdsLookUp.Add(allData.Nodes.Ids[i], i);
            FeResults results = new FeResults(resultName, unitSystem);
            results.SetMesh(targetMesh, nodeIdsLookUp);
            // Add group
            FieldData fieldData = new FieldData(FOFieldNames.Velo);
            fieldData.GlobalIncrementId = 1;
            fieldData.StepType = StepTypeEnum.Static;
            fieldData.Time = 1;
            fieldData.MethodId = 1;
            fieldData.StepId = 1;
            fieldData.StepIncrementId = 1;
            // Add values
            Field field = new Field(fieldData.Name);
            field.AddComponent(FOComponentNames.All, valuesAll);
            field.AddComponent(FOComponentNames.V1, values1);
            field.AddComponent(FOComponentNames.V2, values2);
            field.AddComponent(FOComponentNames.V3, values3);
            results.AddField(fieldData, field);
            //
            return results;
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_nodeId", _nodeId, typeof(int));
            info.AddValue("_v1", _v1, typeof(EquationContainer));
            info.AddValue("_v2", _v2, typeof(EquationContainer));
            info.AddValue("_v3", _v3, typeof(EquationContainer));
            info.AddValue("_magnitude", _magnitude, typeof(EquationContainer));
        }
    }
}
