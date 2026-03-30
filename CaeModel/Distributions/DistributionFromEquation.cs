// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using CaeGlobals;
using CaeMesh;
using CaeResults;
using DynamicTypeDescriptor;
using static CaeGlobals.Geometry2;

namespace CaeModel
{
    [Serializable]
    public class DistributionFromEquation : Distribution, ISerializable
    {
        // Variables                                                                                                                
        private string _equationMagnitude;          //ISerializable
        private string _equationD1;                 //ISerializable
        private string _equationD2;                 //ISerializable
        private string _equationD3;                 //ISerializable


        // Properties                                                                                                               
        public string EquationMagnitude { get { return _equationMagnitude; } set { _equationMagnitude = value; } }
        public string EquationD1 { get { return _equationD1; } set { _equationD1 = value; } }
        public string EquationD2 { get { return _equationD2; } set { _equationD2 = value; } }
        public string EquationD3 { get { return _equationD3; } set { _equationD3 = value; } }


        // Constructors                                                                                                             
        public DistributionFromEquation(string name, string equation)
            : base(name)
        {
            _equationMagnitude = "=1";
            _equationD1 = "=1";
            _equationD2 = "=1";
            _equationD3 = "=1";
            //
            _distributionType = DistributionTypeEnum.Scalar;
            _equationMagnitude = equation;
        }
        public DistributionFromEquation(string name, string equationD1, string equationD2, string equationD3)
            : base(name)
        {
            _equationMagnitude = "=1";
            _equationD1 = "=1";
            _equationD2 = "=1";
            _equationD3 = "=1";
            //
            _distributionType = DistributionTypeEnum.Vector;
            _equationD1 = equationD1;
            _equationD2 = equationD2;
            _equationD3 = equationD3;
        }
        public DistributionFromEquation(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_equationMagnitude":
                        _equationMagnitude = (string)entry.Value; break;
                    case "_equationD1":
                        _equationD1 = (string)entry.Value; break;
                    case "_equationD2":
                        _equationD2 = (string)entry.Value; break;
                    case "_equationD3":
                        _equationD3 = (string)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public string CheckEquations()
        {
            if (DistributionType == DistributionTypeEnum.Scalar)
            {
                if (EquationMagnitude.Trim().Length <= 1) return "Magnitude equations is missing.";
                return CheckEquation(EquationMagnitude);
            }
            else if (DistributionType == DistributionTypeEnum.Vector)
            {
                string error;
                if (EquationD1.Trim().Length <= 1) return "D1 equations is missing.";
                error = CheckEquation(EquationD1);
                if (error != null) return error;
                if (EquationD2.Trim().Length <= 1) return "D2 equations is missing.";
                error = CheckEquation(EquationD2);
                if (error != null) return error;
                if (EquationD3.Trim().Length <= 1) return "D3 equations is missing.";
                return CheckEquation(EquationD3);
            }
            else throw new NotSupportedException();
        }
        private string CheckEquation(string equation)
        {
            OrderedDictionary<string, object> existingParameters = MyNCalc.ExistingParameters.DeepCopy();
            //
            try
            {
                MyNCalc.ExistingParameters["x"] = 0.111;
                MyNCalc.ExistingParameters["y"] = 0.222;
                MyNCalc.ExistingParameters["z"] = 0.333;
                //
                MyNCalc.HasErrors(equation, out _);
                //
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                MyNCalc.ExistingParameters = existingParameters;
            }
        }
        //
        public override bool ImportDistribution()
        { 
            return true;
        }
        public override void GetMagnitudesAndDistancesForPoints(FeModel model, double[][] points, out double[][] magnitudes,
                                                                out double[][] distances)
        {
            distances = null;
            OrderedDictionary<string, object> existingParameters = MyNCalc.ExistingParameters.DeepCopy();
            try
            {
                CoordinateSystem cs = null;
                double[] localCoor;
                if (_coordinateSystemName != null) cs = model.Mesh.CoordinateSystems[_coordinateSystemName];
                //
                double[] x = new double[points.Length];
                double[] y = new double[points.Length];
                double[] z = new double[points.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    if (cs == null)
                    {
                        x[i] = points[i][0];
                        y[i] = points[i][1];
                        z[i] = points[i][2];
                    }
                    else
                    {
                        localCoor = cs.GetLocalCoordinates(points[i]);
                        x[i] = localCoor[0];
                        y[i] = localCoor[1];
                        z[i] = localCoor[2];
                    }
                }
                MyNCalc.ExistingParameters["x"] = x;
                MyNCalc.ExistingParameters["y"] = y;
                MyNCalc.ExistingParameters["z"] = z;
                //
                if (_distributionType == DistributionTypeEnum.Scalar)
                {
                    double[] result = MyNCalc.SolveArrayEquation(_equationMagnitude);
                    magnitudes = new double[result.Length][];
                    for (int i = 0; i < result.Length; i++) magnitudes[i] = new double[] { result[i] };
                }
                else if (_distributionType == DistributionTypeEnum.Vector)
                {
                    double[] result1 = MyNCalc.SolveArrayEquation(_equationD1);
                    double[] result2 = MyNCalc.SolveArrayEquation(_equationD2);
                    double[] result3 = MyNCalc.SolveArrayEquation(_equationD3);
                    magnitudes = new double[result1.Length][];
                    for (int i = 0; i < result1.Length; i++) magnitudes[i] = new double[] { result1[i], result2[i], result3[i] };
                }
                else throw new NotSupportedException();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                MyNCalc.ExistingParameters = existingParameters;
            }
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_equationMagnitude", _equationMagnitude, typeof(string));
            info.AddValue("_equationD1", _equationD1, typeof(string));
            info.AddValue("_equationD2", _equationD2, typeof(string));
            info.AddValue("_equationD3", _equationD3, typeof(string));
        }
    }
}