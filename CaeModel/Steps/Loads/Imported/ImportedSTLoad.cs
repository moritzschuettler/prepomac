using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using CaeResults;
using System.IO;
using System.Runtime.Serialization;

namespace CaeModel
{
    [Serializable]
    public class ImportedSTLoad : Load, IPreviewable, ISerializable
    {
        // Variables                                                                                                                
        private string _surfaceName;                        //ISerializable
        private RegionTypeEnum _regionType;                 //ISerializable
        private string _fileName;                           //ISerializable
        private CloudInterpolatorEnum _interpolatorType;    //ISerializable
        private EquationContainer _interpolatorRadius;      //ISerializable
        private EquationContainer _magnitudeFactor;         //ISerializable
        private EquationContainer _geomScaleFactor;         //ISerializable
        //
        private FileInfo _oldFileInfo;                      //ISerializable
        //
        [NonSerialized] private double _prevScaleFactor;
        [NonSerialized] private CloudInterpolator _interpolator;


        // Properties                                                                                                               
        public override string RegionName { get { return _surfaceName; } set { _surfaceName = value; } }
        public override RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public string SurfaceName { get { return _surfaceName; } set { _surfaceName = value; } }
        public string FileName { get { return _fileName; } set { _fileName = value; ImportLoad(); } }
        public CloudInterpolatorEnum InterpolatorType { get { return _interpolatorType; } set { _interpolatorType = value; } }
        public EquationContainer InterpolatorRadius
        {
            get { return _interpolatorRadius; }
            set { SetInterpolatorRadius(value); }
        }
        public EquationContainer MagnitudeFactor
        {
            get { return _magnitudeFactor; }
            set { SetMagnitudeFactor(value); }
        }
        public EquationContainer GeometryScaleFactor
        {
            get { return _geomScaleFactor; }
            set { SetGeomScaleFactor(value); }
        }
        public CloudInterpolator Interpolator { get { return _interpolator; } }


        // Constructors                                                                                                             
        public ImportedSTLoad(string name, string surfaceName, RegionTypeEnum regionType, bool twoD, bool complex, double phaseDeg)
            : base(name, twoD, complex, phaseDeg)
        {
            _surfaceName = surfaceName;
            _regionType = regionType;
            //
            _fileName = null;
            _interpolatorType = CloudInterpolatorEnum.ClosestPoint;
            InterpolatorRadius = new EquationContainer(typeof(StringLengthConverter), 1);
            MagnitudeFactor = new EquationContainer(typeof(StringDoubleConverter), 1);
            GeometryScaleFactor = new EquationContainer(typeof(StringDoubleConverter), 1);
            //
            _oldFileInfo = null;
        }
        public ImportedSTLoad(SerializationInfo info, StreamingContext context)
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
                    case "_fileName":
                        _fileName = (string)entry.Value; break;
                    case "_interpolatorType":
                        _interpolatorType = (CloudInterpolatorEnum)entry.Value; break;
                    case "_interpolatorRadius":
                        SetInterpolatorRadius((EquationContainer)entry.Value, false); break;
                    case "_magnitudeFactor":
                        SetMagnitudeFactor((EquationContainer)entry.Value, false); break;
                    case "_geomScaleFactor":
                        SetGeomScaleFactor((EquationContainer)entry.Value, false); break;
                    case "_oldFileInfo":
                        _oldFileInfo = (FileInfo)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        private void SetInterpolatorRadius(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _interpolatorRadius, value, CheckPositive, checkEquation);
        }
        private void SetMagnitudeFactor(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _magnitudeFactor, value, null, checkEquation);
        }
        private void SetGeomScaleFactor(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _geomScaleFactor, value, null, EquationChanged, checkEquation);
        }
        //
        private double CheckPositive(double value)
        {
            if (value <= 0) throw new CaeException("The value must be larger than 0.");
            else return value;
        }
        private void EquationChanged()
        {
            ImportLoad();
        }
        
        // IContainsEquations
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _interpolatorRadius.CheckEquation();
            _magnitudeFactor.CheckEquation();
            _geomScaleFactor.CheckEquation();
        }
        //
        public bool IsProperlyDefined(out string error)
        {
            error = "";
            if (!File.Exists(_fileName))
            {
                error = "The selected file does not exist.";
                return false;
            }
            //
            return true;
        }
        public bool IsInitialized()
        {
            return _interpolator != null;
        }
        public void ImportLoad()
        {
            bool updateData = false;
            if (_fileName == null) return;
            //
            FileInfo fileInfo = new FileInfo(_fileName);
            double scaleFactor = _geomScaleFactor.Value;
            //
            if (fileInfo.Exists)
            {
                if (_interpolator == null) updateData = true;
                else if (_oldFileInfo == null) updateData = true;
                else if (fileInfo.Name != _oldFileInfo.Name) updateData = true;
                // Files have the same name - check if newer
                else if (fileInfo.LastWriteTimeUtc < _oldFileInfo.LastWriteTimeUtc) updateData = true;
                else if (_prevScaleFactor != scaleFactor) updateData = true;
            }
            else
            {
                string missingFile = "The file from which the load should be imported does not exist.";
                throw new CaeException(missingFile);
            }
            //
            if (updateData)
            {
                _oldFileInfo = fileInfo;
                // Get cloud points
                CloudPoint[] cloudPoints = CloudPointReader.Read(FileName);
                if (cloudPoints == null) throw new CaeException("No load data was imported.");
                // Scale point locations
                
                if (scaleFactor != 1)
                {
                    Parallel.For(0, cloudPoints.Length, i =>
                    //for (int i = 0; i < cloudPoints.Length; i++)
                    {
                        cloudPoints[i].Coor[0] *= scaleFactor;
                        cloudPoints[i].Coor[1] *= scaleFactor;
                        cloudPoints[i].Coor[2] *= scaleFactor;
                    }
                    );
                    _prevScaleFactor = scaleFactor;
                }
                // Initialize interpolator
                _interpolator = new CloudInterpolator(cloudPoints);
            }
        }
        public FeResults GetPreview(FeMesh targetMesh, string resultName, UnitSystem unitSystem)
        {
            ImportLoad();
            //
            PartExchangeData allData = new PartExchangeData();
            targetMesh.GetAllNodesAndCells(out allData.Nodes.Ids, out allData.Nodes.Coor, out allData.Cells.Ids,
                                           out allData.Cells.CellNodeIds, out allData.Cells.Types);
            //
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
            Parallel.For(0, forcesAll.Length, i =>
            //for (int i = 0; i < forcesAll.Length; i++)
            {
                double[] distance;
                double[] force;
                //
                if (nodeIds.Contains(allData.Nodes.Ids[i]))
                {
                    GetForcePerAreaAndDistanceForPoint(allData.Nodes.Coor[i], out distance, out force);
                    //
                    distances1[i] = (float)distance[0];
                    distances2[i] = (float)distance[1];
                    distances3[i] = (float)distance[2];
                    distancesAll[i] = (float)Math.Sqrt(distance[0] * distance[0] +
                                                       distance[1] * distance[1] +
                                                       distance[2] * distance[2]);
                    forces1[i] = (float)force[0];
                    forces2[i] = (float)force[1];
                    forces3[i] = (float)force[2];
                    forcesAll[i] = (float)Math.Sqrt(force[0] * force[0] +
                                                    force[1] * force[1] +
                                                    force[2] * force[2]);
                }
                else
                {
                    distances1[i] = float.NaN;
                    distances2[i] = float.NaN;
                    distances3[i] = float.NaN;
                    distancesAll[i] = float.NaN;
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
            // Add distances
            FieldData fieldData = new FieldData(FOFieldNames.Distance);
            fieldData.GlobalIncrementId = 1;
            fieldData.StepType = StepTypeEnum.Static;
            fieldData.Time = 1;
            fieldData.MethodId = 1;
            fieldData.StepId = 1;
            fieldData.StepIncrementId = 1;
            // Distances
            Field field = new Field(fieldData.Name);
            field.AddComponent(FOComponentNames.All, distancesAll);
            field.AddComponent(FOComponentNames.D1, distances1);
            field.AddComponent(FOComponentNames.D2, distances2);
            field.AddComponent(FOComponentNames.D3, distances3);
            results.AddField(fieldData, field);
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
        public void GetForcePerAreaAndDistanceForPoint(double[] point, out double[] distance, out double[] values)
        {
            _interpolator.InterpolateAt(point, _interpolatorType, _interpolatorRadius.Value,
                                        out distance, out values);
            for (int i = 0; i < values.Length; i++) values[i] *= _magnitudeFactor.Value;
            
        }
        public double[] GetForcePerAreaForPoint(double[] point)
        {
            _interpolator.InterpolateAt(point, _interpolatorType, _interpolatorRadius.Value, 
                                        out double[] distance, out double[] values);
            for (int i = 0; i < values.Length; i++) values[i] *= _magnitudeFactor.Value;
            return values;
        }
        
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_surfaceName", _surfaceName, typeof(string));
            info.AddValue("_regionType", _regionType, typeof(RegionTypeEnum));
            info.AddValue("_fileName", _fileName, typeof(string));
            info.AddValue("_interpolatorType", _interpolatorType, typeof(InterpolatorEnum));
            info.AddValue("_interpolatorRadius", _interpolatorRadius, typeof(EquationContainer));
            info.AddValue("_magnitudeFactor", _magnitudeFactor, typeof(EquationContainer));
            info.AddValue("_geomScaleFactor", _geomScaleFactor, typeof(EquationContainer));
            info.AddValue("_oldFileInfo", _oldFileInfo, typeof(FileInfo));
        }
    }
}
