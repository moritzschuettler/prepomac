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

namespace CaeModel
{
    [Serializable]
    public class InitialTranslationalVelocity : InitialCondition, IContainsEquations, IPreviewable
    {
        // Variables                                                                                                                
        private EquationContainer _v1;
        private EquationContainer _v2;
        private EquationContainer _v3;
        private EquationContainer _magnitude;


        // Properties                                                                                                               
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
        public InitialTranslationalVelocity(string name, string regionName, RegionTypeEnum regionType,
                               double v1, double v2, double v3, bool twoD)
            : base(name, regionName, regionType, twoD)
        {
            double mag = Math.Sqrt(v1 * v1 + v2 * v2 + v3 * v3);
            //
            V1 = new EquationContainer(typeof(StringVelocityConverter), v1, null);
            V2 = new EquationContainer(typeof(StringVelocityConverter), v2, null);
            V3 = new EquationContainer(typeof(StringVelocityConverter), v3, null);
            Magnitude = new EquationContainer(typeof(StringVelocityConverter), mag, null);
        }


        // Methods                                                                                                                  
        private void UpdateEquations()
        {
            try
            {
                // If error catch it silently
                if (_v1.IsEquation() || _v2.IsEquation() || _v3.IsEquation()) FEquationChanged();
                else if (_magnitude.IsEquation()) MagnitudeEquationChanged();
            }
            catch (Exception ex) { }
        }
        private void SetV1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _v1, value, null, FEquationChanged, checkEquation);
        }
        private void SetV2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _v2, value, null, FEquationChanged, checkEquation);
        }
        private void SetV3(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _v3, value, Check2D, FEquationChanged, checkEquation);
        }
        private void SetMagnitude(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _magnitude, value, CheckMagnitude, MagnitudeEquationChanged, checkEquation);
        }
        //
        private void FEquationChanged()
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
        // IContainsEquations
        public void CheckEquations()
        {
            _v1.CheckEquation();
            _v2.CheckEquation();
            _v3.CheckEquation();
            _magnitude.CheckEquation();
        }
        public virtual bool TryCheckEquations()
        {
            try
            {
                CheckEquations();
                return true;
            }
            catch (Exception ex) { return false; }
        }
        // IPreviewable
        public FeResults GetPreview(FeMesh targetMesh, string resultName, UnitSystem unitSystem)
        {
            PartExchangeData allData = new PartExchangeData();
            targetMesh.GetAllNodesAndCells(out allData.Nodes.Ids, out allData.Nodes.Coor, out allData.Cells.Ids,
                                           out allData.Cells.CellNodeIds, out allData.Cells.Types);
            //
            HashSet<int> nodeIds;
            if (RegionType == RegionTypeEnum.PartName)
            {
                nodeIds = new HashSet<int>(targetMesh.Parts[RegionName].NodeLabels);
            }
            else if (RegionType == RegionTypeEnum.ElementSetName)
            {
                FeNodeSet nodeSet = targetMesh.GetNodeSetFromPartOrElementSetName(RegionName, false);
                nodeIds = new HashSet<int>(nodeSet.Labels);
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
    }
}
