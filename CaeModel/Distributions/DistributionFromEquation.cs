using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using CaeGlobals;
using CaeResults;
using DynamicTypeDescriptor;
using static CaeGlobals.Geometry2;

namespace CaeModel
{
    [Serializable]
    public class DistributionFromEquation : Distribution
    {
        // Variables                                                                                                                
        private string _equationMagnitude;
        private string _equationD1;
        private string _equationD2;
        private string _equationD3;


        // Properties                                                                                                               
        public string EquationMagnitude { get { return _equationMagnitude; } set { _equationMagnitude = value; } }
        public string EquationD1 { get { return _equationD1; } set { _equationD1 = value; } }
        public string EquationD2 { get { return _equationD2; } set { _equationD2 = value; } }
        public string EquationD3 { get { return _equationD3; } set { _equationD3 = value; } }


        // Constructors                                                                                                             
        public DistributionFromEquation(string name, string equation)
            : base(name)
        {
            Reset();
            //
            _distributionType = DistributionTypeEnum.Scalar;
            _equationMagnitude = equation;
        }
        public DistributionFromEquation(string name, string equationD1, string equationD2, string equationD3)
            : base(name)
        {
            Reset();
            //
            _distributionType = DistributionTypeEnum.Vector;
            _equationD1 = equationD1;
            _equationD2 = equationD2;
            _equationD3 = equationD3;
        }
        private void Reset()
        {
            _equationMagnitude = "=1";
            _equationD1 = "=1";
            _equationD2 = "=1";
            _equationD3 = "=1";
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
        public override bool IsInitialized()
        {
            return true;
        }
        public override bool ImportLoad()
        { 
            return true;
        }
        public override double[] GetMagnitudeForPoint(double[] point)
        {
            OrderedDictionary<string, object> existingParameters = MyNCalc.ExistingParameters.DeepCopy();
            try
            {
                MyNCalc.ExistingParameters["x"] = point[0];
                MyNCalc.ExistingParameters["y"] = point[1];
                MyNCalc.ExistingParameters["z"] = point[2];
                //

                if (_distributionType == DistributionTypeEnum.Scalar)
                {
                    return new double[1] { MyNCalc.SolveEquation(_equationMagnitude) };
                }
                else if (_distributionType == DistributionTypeEnum.Vector)
                {
                    double[] result = new double[3];
                    result[0] = MyNCalc.SolveEquation(_equationD1);
                    result[1] = MyNCalc.SolveEquation(_equationD2);
                    result[2] = MyNCalc.SolveEquation(_equationD3);
                    return result;
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
        public override double[][] GetMagnitudesForPoints(double[][] points)
        {
            OrderedDictionary<string, object> existingParameters = MyNCalc.ExistingParameters.DeepCopy();
            try
            {
                double[] x = new double[points.Length];
                double[] y = new double[points.Length];
                double[] z = new double[points.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    x[i] = points[i][0];
                    y[i] = points[i][1];
                    z[i] = points[i][2];
                }
                MyNCalc.ExistingParameters["x"] = x;
                MyNCalc.ExistingParameters["y"] = y;
                MyNCalc.ExistingParameters["z"] = z;
                //

                if (_distributionType == DistributionTypeEnum.Scalar)
                {
                    double[] result = MyNCalc.SolveArrayEquation(_equationMagnitude);
                    double[][] magnitudes = new double[result.Length][];
                    for (int i = 0; i < result.Length; i++) magnitudes[i] = new double[] { result[i] };
                    return magnitudes;
                }
                else if (_distributionType == DistributionTypeEnum.Vector)
                {
                    double[] result1 = MyNCalc.SolveArrayEquation(_equationD1);
                    double[] result2 = MyNCalc.SolveArrayEquation(_equationD2);
                    double[] result3 = MyNCalc.SolveArrayEquation(_equationD3);
                    double[][] magnitudes = new double[result1.Length][];
                    for (int i = 0; i < result1.Length; i++) magnitudes[i] = new double[] { result1[i], result2[i], result3[i] };
                    return magnitudes;
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
    }
}