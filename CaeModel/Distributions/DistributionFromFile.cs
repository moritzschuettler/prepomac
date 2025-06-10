using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using CaeResults;
using DynamicTypeDescriptor;
using static CaeGlobals.Geometry2;

namespace CaeModel
{
    [Serializable]
    public class DistributionFromFile : MappedDistribution, ISerializable
    {
        // Variables                                                                                                                
        private string _fileName;                                   //ISerializable
        private FileInfo _oldFileInfo;                              //ISerializable


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } set { _fileName = value; ImportDistribution(); } }


        // Constructors                                                                                                             
        public DistributionFromFile(string name)
            : base(name)
        {
            _distributionType = DistributionTypeEnum.Scalar;
            //
            _fileName = null;
            _interpolatorType = CloudInterpolatorEnum.ClosestPoint;
            InterpolatorRadius = new EquationContainer(typeof(StringLengthConverter), 1);
            //
            _oldFileInfo = null;
        }
        public DistributionFromFile(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_fileName":
                        _fileName = (string)entry.Value; break;
                    case "_oldFileInfo":
                        _oldFileInfo = (FileInfo)entry.Value; break;
                    
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        protected override void EquationChanged()
        {
            UpdateScaleTranslate();
        }
        // IContainsEquations
        public void CheckEquations()
        {
            _interpolatorRadius.CheckEquation();
        }
        //
        public bool IsProperlyDefined(out string error)
        {
            error = null;
            if (!File.Exists(_fileName))
            {
                error = "The selected file does not exist.";
                return false;
            }
            //
            return true;
        }
        public override bool IsInitialized()
        {
            return _interpolator != null;
        }
        public override bool ImportDistribution()
        {
            bool updateData = false;
            if (_fileName == null) return false;
            //
            FileInfo fileInfo = new FileInfo(_fileName);
            //
            if (fileInfo.Exists)
            {
                if (_interpolator == null) updateData = true;
                else if (_oldFileInfo == null) updateData = true;
                else if (fileInfo.Name != _oldFileInfo.Name) updateData = true;
                // Files have the same name - check if newer
                else if (fileInfo.LastWriteTimeUtc < _oldFileInfo.LastWriteTimeUtc) updateData = true;
            }
            else
            {
                string missingFile = "The file from which the distribution should be imported does not exist.";
                throw new CaeException(missingFile);
            }
            //
            if (updateData || _cloudPoints == null)
            {
                _oldFileInfo = fileInfo;
                // Get cloud points
                _cloudPoints = CloudPointReader.Read(FileName);
                if (_cloudPoints == null) throw new CaeException("No distribution data was imported.");
                //
                return UpdateScaleTranslate();
            }
            return true;
        }
        public bool UpdateScaleTranslate()
        {
            if (_cloudPoints == null) return ImportDistribution();
            else
            {
                double scaleX = ScaleX.Value;
                double scaleY = ScaleY.Value;
                double scaleZ = ScaleZ.Value;
                double translateX = TranslateX.Value;
                double translateY = TranslateY.Value;
                double translateZ = TranslateZ.Value;
                //
                CloudPoint[] cloudPoints = new CloudPoint[_cloudPoints.Length];
                Parallel.For(0, cloudPoints.Length, i =>
                //for (int i = 0; i < cloudPoints.Length; i++)
                {
                    cloudPoints[i] = new CloudPoint(_cloudPoints[i]);   // copy
                    cloudPoints[i].Coor[0] = _cloudPoints[i].Coor[0] * scaleX + translateX;
                    cloudPoints[i].Coor[1] = _cloudPoints[i].Coor[1] * scaleY + translateY;
                    cloudPoints[i].Coor[2] = _cloudPoints[i].Coor[2] * scaleZ + translateZ;
                }
                );
                // Initialize interpolator
                _interpolator = new CloudInterpolator(cloudPoints);
                //
                return true;
            }
        }
        public override void GetMagnitudeAndDistanceForPoint(double[] point, out double[] magnitude, out double[] distance)
        {
            try
            {
                double r = _interpolatorRadius.Value;
                _interpolator.InterpolateAt(point, _interpolatorType, r, out distance, out magnitude);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public override void GetMagnitudesAndDistancesForPoints(double[][] points, out double[][] magnitudes,
                                                                out double[][] distances)
        {
            try
            {
                double r = _interpolatorRadius.Value;
                magnitudes = new double[points.Length][];
                distances = new double[points.Length][];
                for (int i = 0; i < points.Length; i++)
                {
                    _interpolator.InterpolateAt(points[i], _interpolatorType, r, out distances[i], out magnitudes[i]);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_fileName", _fileName, typeof(string));
            info.AddValue("_oldFileInfo", _oldFileInfo, typeof(FileInfo));
        }
    }
}