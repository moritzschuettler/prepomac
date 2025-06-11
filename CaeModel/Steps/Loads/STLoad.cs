using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;
using CaeResults;
using System.IO;

namespace CaeModel
{
    [Serializable]
    public class STLoad : Load, IDistribution, IPreviewable, ISerializable
    {
        // Variables                                                                                                                
        private string _surfaceName;            //ISerializable
        private RegionTypeEnum _regionType;     //ISerializable
        private EquationContainer _f1;          //ISerializable
        private EquationContainer _f2;          //ISerializable
        private EquationContainer _f3;          //ISerializable
        private EquationContainer _fMagnitude;  //ISerializable
        private EquationContainer _p1;          //ISerializable
        private EquationContainer _p2;          //ISerializable
        private EquationContainer _p3;          //ISerializable
        private EquationContainer _pMagnitude;  //ISerializable
        private string _distributionName;       //ISerializable


        // Properties                                                                                                               
        public override string RegionName { get { return _surfaceName; } set { _surfaceName = value; } }
        public override RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public string SurfaceName { get { return _surfaceName; } set { _surfaceName = value; } }
        public EquationContainer F1 { get { UpdateEquations(); return _f1; } set { SetF1(value); } }
        public EquationContainer F2 { get { UpdateEquations(); return _f2; } set { SetF2(value); } }
        public EquationContainer F3 { get { UpdateEquations(); return _f3; } set { SetF3(value); } }
        public EquationContainer FMagnitude { get { UpdateEquations(); return _fMagnitude; } set { SetFMagnitude(value); } }
        public EquationContainer P1 { get { UpdateEquations(); return _p1; } set { SetP1(value); } }
        public EquationContainer P2 { get { UpdateEquations(); return _p2; } set { SetP2(value); } }
        public EquationContainer P3 { get { UpdateEquations(); return _p3; } set { SetP3(value); } }
        public EquationContainer PMagnitude { get { UpdateEquations(); return _pMagnitude; } set { SetPMagnitude(value); } }
        public string DistributionName { get { return _distributionName; } set { _distributionName = value; } }


        // Constructors                                                                                                             
        public STLoad(string name, string surfaceName, RegionTypeEnum regionType, double f1, double f2, double f3, bool twoD,
                      bool complex, double phaseDeg)
            : base(name, twoD, complex, phaseDeg)
        {
            _surfaceName = surfaceName;
            _regionType = regionType;
            //
            double mag = Math.Sqrt(f1 * f1 + f2 * f2 + f3 * f3);
            F1 = new EquationContainer(typeof(StringForceConverter), f1);
            F2 = new EquationContainer(typeof(StringForceConverter), f2);
            F3 = new EquationContainer(typeof(StringForceConverter), f3);
            FMagnitude = new EquationContainer(typeof(StringForceConverter), mag);
            //
            P1 = new EquationContainer(typeof(StringPressureConverter), 0);
            P2 = new EquationContainer(typeof(StringPressureConverter), 0);
            P3 = new EquationContainer(typeof(StringPressureConverter), 0);
            PMagnitude = new EquationContainer(typeof(StringPressureConverter), 0);
            //
            _distributionName = DefaultDistributionName;
        }
        public STLoad(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_surfaceName":
                        _surfaceName = (string)entry.Value; break;
                    case "_regionType":
                        _regionType = (RegionTypeEnum)entry.Value; break;
                    case "_f1":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueF1)
                            F1 = new EquationContainer(typeof(StringForceConverter), valueF1);
                        else
                            SetF1((EquationContainer)entry.Value, false);
                        break;
                    case "_f2":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueF2)
                            F2 = new EquationContainer(typeof(StringForceConverter), valueF2);
                        else
                            SetF2((EquationContainer)entry.Value, false);
                        break;
                    case "_f3":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueF3)
                            F3 = new EquationContainer(typeof(StringForceConverter), valueF3);
                        else
                            SetF3((EquationContainer)entry.Value, false);
                        break;
                    case "_fMagnitude":
                    case "_magnitude":  // Compatibility for version v2.2.4
                        SetFMagnitude((EquationContainer)entry.Value, false); break;
                    case "_p1":
                        SetP1((EquationContainer)entry.Value, false);
                        break;
                    case "_p2":
                        SetP2((EquationContainer)entry.Value, false);
                        break;
                    case "_p3":
                        SetP3((EquationContainer)entry.Value, false);
                        break;
                    case "_pMagnitude":
                        SetPMagnitude((EquationContainer)entry.Value, false); break;
                    case "_distributionName":
                        _distributionName = (string)entry.Value; break;
                    default:
                        break;
                }
            }
            // Compatibility for version v1.4.0
            if (_fMagnitude == null)
            {
                double mag = Math.Sqrt(_f1.Value * _f1.Value + _f2.Value * _f2.Value + _f3.Value * _f3.Value);
                FMagnitude = new EquationContainer(typeof(StringForceConverter), mag);
            }
            // Compatibility for version v2.2.4
            if (_distributionName == null) _distributionName = DefaultDistributionName;
            if (_p1 == null) P1 = new EquationContainer(typeof(StringPressureConverter), 0);
            if (_p2 == null) P2 = new EquationContainer(typeof(StringPressureConverter), 0);
            if (_p3 == null) P3 = new EquationContainer(typeof(StringPressureConverter), 0);
            if (_pMagnitude == null) PMagnitude = new EquationContainer(typeof(StringPressureConverter), 0);
        }


        // Methods                                                                                                                  
        private void UpdateEquations()
        {
            try
            {
                // If error catch it silently
                if (_f1.IsEquation() || _f2.IsEquation() || _f3.IsEquation()) FEquationChanged();
                else if (_fMagnitude.IsEquation()) FMagnitudeEquationChanged();
                //
                if (_p1.IsEquation() || _p2.IsEquation() || _p3.IsEquation()) PEquationChanged();
                else if (_pMagnitude.IsEquation()) PMagnitudeEquationChanged();
            }
            catch (Exception ex) { }
        }
        private void SetF1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _f1, value, null, FEquationChanged, checkEquation);
        }
        private void SetF2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _f2, value, null, FEquationChanged, checkEquation);
        }
        private void SetF3(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _f3, value, Check2D, FEquationChanged, checkEquation);
        }
        private void SetFMagnitude(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _fMagnitude, value, CheckMagnitude, FMagnitudeEquationChanged, checkEquation);
        }
        //
        private void SetP1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _p1, value, null, PEquationChanged, checkEquation);
        }
        private void SetP2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _p2, value, null, PEquationChanged, checkEquation);
        }
        private void SetP3(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _p3, value, Check2D, PEquationChanged, checkEquation);
        }
        private void SetPMagnitude(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _pMagnitude, value, CheckMagnitude, PMagnitudeEquationChanged, checkEquation);
        }
        //
        private void FEquationChanged()
        {
            double mag = Math.Sqrt(_f1.Value * _f1.Value + _f2.Value * _f2.Value + _f3.Value * _f3.Value);
            _fMagnitude.SetEquationFromValue(mag, false);
        }
        private void PEquationChanged()
        {
            double mag = Math.Sqrt(_p1.Value * _p1.Value + _p2.Value * _p2.Value + _p3.Value * _p3.Value);
            _pMagnitude.SetEquationFromValue(mag, false);
        }
        private void FMagnitudeEquationChanged()
        {
            double c1;
            double c2;
            double c3;
            //
            if (_fMagnitude.Value == 0)
            {
                c1 = 0;
                c2 = 0;
                c3 = 0;
            }
            else
            {
                double r;
                c1 = _f1.Value;
                c2 = _f2.Value;
                c3 = _f3.Value;
                double mag = Math.Sqrt(c1 * c1 + c2 * c2 + c3 * c3);
                //
                if (mag != 0)
                {
                    r = _fMagnitude.Value / mag;
                    c1 *= r;
                    c2 *= r;
                    c3 *= r;
                }
                else
                {
                    r = Math.Sqrt(Math.Pow(_fMagnitude.Value, 2) / 3);
                    c1 = r;
                    c2 = r;
                    c3 = r;
                }
            }
            //
            _f1.SetEquationFromValue(c1, false);
            _f2.SetEquationFromValue(c2, false);
            _f3.SetEquationFromValue(c3, false);
        }
        private void PMagnitudeEquationChanged()
        {
            double c1;
            double c2;
            double c3;
            //
            if (_pMagnitude.Value == 0)
            {
                c1 = 0;
                c2 = 0;
                c3 = 0;
            }
            else
            {
                double r;
                c1 = _p1.Value;
                c2 = _p2.Value;
                c3 = _p3.Value;
                double mag = Math.Sqrt(c1 * c1 + c2 * c2 + c3 * c3);
                //
                if (mag != 0)
                {
                    r = _pMagnitude.Value / mag;
                    c1 *= r;
                    c2 *= r;
                    c3 *= r;
                }
                else
                {
                    r = Math.Sqrt(Math.Pow(_pMagnitude.Value, 2) / 3);
                    c1 = r;
                    c2 = r;
                    c3 = r;
                }
            }
            //
            _p1.SetEquationFromValue(c1, false);
            _p2.SetEquationFromValue(c2, false);
            _p3.SetEquationFromValue(c3, false);
        }
        //
        private double Check2D(double value)
        {
            if (_twoD) return 0;
            else return value;
        }
        private double CheckMagnitude(double value)
        {
            if (value < 0) throw new Exception("Value of the force load magnitude must be non-negative.");
            else return value;
        }
        public double[] GetDirection(CoordinateSystem coordinateSystem, double[] coor = null)
        {
            double d1;
            double d2;
            double d3;
            if (_distributionName == DefaultDistributionName)
            {
                d1 = _f1.Value;
                d2 = _f2.Value;
                d3 = _f3.Value;
            }
            else
            {
                d1 = _p1.Value;
                d2 = _p2.Value;
                d3 = _p3.Value;
            }
            //
            if (coordinateSystem == null) return new double[] { d1, d2, d3 };
            else
            {
                Vec3D directionX = new Vec3D(GetDirectionX(coordinateSystem, coor));
                Vec3D directionY = new Vec3D(GetDirectionY(coordinateSystem, coor));
                Vec3D directionZ = new Vec3D(GetDirectionZ(coordinateSystem, coor));
                Vec3D direction = d1 * directionX + d2 * directionY + d3 * directionZ;
                return direction.Coor;
            }
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _f1.CheckEquation();
            _f2.CheckEquation();
            _f3.CheckEquation();
            _fMagnitude.CheckEquation();
            //
            _p1.CheckEquation();
            _p2.CheckEquation();
            _p3.CheckEquation();
            _pMagnitude.CheckEquation();
        }
        // IPreviewable
        public FeResults GetPreview(FeModel model, string resultName, UnitSystem unitSystem)
        {
            FeMesh targetMesh = model.Mesh;
            PartExchangeData allData = new PartExchangeData();
            targetMesh.GetAllNodesAndCells(out allData.Nodes.Ids, out allData.Nodes.Coor, out allData.Cells.Ids,
                                           out allData.Cells.CellNodeIds, out allData.Cells.Types);
            //
            bool addDistances = _distributionName != DefaultDistributionName &&
                model.Distributions[_distributionName] is MappedDistribution md &&
                md.InterpolatorType == CloudInterpolatorEnum.ClosestPoint;
            //
            double area = model.GetAreaForSTLoad(this);
            FeSurface surface = targetMesh.Surfaces[SurfaceName];
            FeNodeSet nodeSet = targetMesh.NodeSets[surface.NodeSetName];
            HashSet<int> nodeIds = new HashSet<int>(nodeSet.Labels);
            //
            float[] distancesAll = new float[allData.Nodes.Coor.Length];
            float[] distances1 = new float[allData.Nodes.Coor.Length];
            float[] distances2 = new float[allData.Nodes.Coor.Length];
            float[] distances3 = new float[allData.Nodes.Coor.Length];
            float[] forcesAll = new float[allData.Nodes.Coor.Length];
            float[] forces1 = new float[allData.Nodes.Coor.Length];
            float[] forces2 = new float[allData.Nodes.Coor.Length];
            float[] forces3 = new float[allData.Nodes.Coor.Length];
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
            double[][] forces;
            GetForcesPerAreaAndDistancesForPoints(model, coor, out distances, out forces);
            //
            Parallel.For(0, forcesAll.Length, i =>
            //for (int i = 0; i < forcesAll.Length; i++)
            {
                int nId;
                int arrayId;
                double[] distance;
                double[] force;
                //
                nId = allData.Nodes.Ids[i];
                if (nodeIds.Contains(nId))
                {
                    arrayId = nodeIdArrayId[nId];
                    force = forces[arrayId];
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
                    forces1[i] = (float)force[0];
                    forces2[i] = (float)force[1];
                    forces3[i] = (float)force[2];
                    forcesAll[i] = (float)Math.Sqrt(force[0] * force[0] +
                                                    force[1] * force[1] +
                                                    force[2] * force[2]);
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
                    forces1[i] = float.NaN;
                    forces2[i] = float.NaN;
                    forces3[i] = float.NaN;
                    forcesAll[i] = float.NaN;
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
            // Add forces
            fieldData = new FieldData(fieldData);
            fieldData.Name = FOFieldNames.ForcePerArea;
            //
            field = new Field(fieldData.Name);
            field.AddComponent(FOComponentNames.All, forcesAll);
            field.AddComponent(FOComponentNames.F1, forces1);
            field.AddComponent(FOComponentNames.F2, forces2);
            field.AddComponent(FOComponentNames.F3, forces3);
            fieldData.Unit = unitSystem.PressureUnitAbbreviation;
            results.AddField(fieldData, field);
            //
            return results;
        }
        public void GetForcesPerAreaAndDistancesForPoints(FeModel model, double[][] points,
                                                          out double[][] distances, out double[][] values)
        {
            double area = model.GetAreaForSTLoad(this);
            distances = new double[points.Length][];
            values = new double[points.Length][];
            //
            if (_distributionName == DefaultDistributionName)
            {
                double f1 = _f1.Value;
                double f2 = _f2.Value;
                double f3 = _f3.Value;
                for (int i = 0; i < points.Length; i++)
                {
                    distances[i] = null;
                    values[i] = new double[] { f1 / area, f2 / area, f3 / area };
                }
            }
            else
            {
                double p1 = _p1.Value;
                double p2 = _p2.Value;
                double p3 = _p3.Value;
                double[][] magnitudes;
                Distribution distribution = model.Distributions[_distributionName];
                //
                distribution.GetMagnitudesAndDistancesForPoints(points, out magnitudes, out distances);
                //
                for (int i = 0; i < points.Length; i++)
                {
                    if (magnitudes[i].Length == 1)
                        values[i] = new double[] { magnitudes[i][0] * p1, magnitudes[i][0] * p2, magnitudes[i][0] * p3 };
                    else if (magnitudes[i].Length == 3)
                        values[i] = new double[] { magnitudes[i][0] * p1, magnitudes[i][1] * p2, magnitudes[i][2] * p3 };
                    else throw new NotSupportedException();
                }
            }
        }
        public double[] GetForcePerAreaForPoint(FeModel model, double[] point)
        {
            double[][] points = new double[][] { point };
            double[][] values;
            GetForcesPerAreaAndDistancesForPoints(model, points, out _, out values);
            return values[0];
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_surfaceName", _surfaceName, typeof(string));
            info.AddValue("_regionType", _regionType, typeof(RegionTypeEnum));
            info.AddValue("_f1", _f1, typeof(EquationContainer));
            info.AddValue("_f2", _f2, typeof(EquationContainer));
            info.AddValue("_f3", _f3, typeof(EquationContainer));
            info.AddValue("_fMagnitude", _fMagnitude, typeof(EquationContainer));
            info.AddValue("_p1", _p1, typeof(EquationContainer));
            info.AddValue("_p2", _p2, typeof(EquationContainer));
            info.AddValue("_p3", _p3, typeof(EquationContainer));
            info.AddValue("_pMagnitude", _pMagnitude, typeof(EquationContainer));
            info.AddValue("_distributionName", _distributionName, typeof(string));
        }
    }
}
