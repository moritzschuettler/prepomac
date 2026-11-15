// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using System.Runtime.Serialization;

namespace CaeModel
{
    [PmxType(3179)]
    public class ElasticWithDensity : MaterialProperty
    {
        // Variables                                                                                                                
        [PmxKey(0)] private EquationContainer _youngsModulus;          //ISerializable
        [PmxKey(1)] private EquationContainer _poissonsRatio;          //ISerializable
        [PmxKey(2)] private EquationContainer _density;                //ISerializable


        // Properties                                                                                                               
        public EquationContainer YoungsModulus { get { return _youngsModulus; } set { SetYoungsModulus(value); } }
        public EquationContainer PoissonsRatio { get { return _poissonsRatio; } set { SetPoissonsRatio(value); } }
        public EquationContainer Density { get { return _density; } set { SetDensity(value); } }


        // Constructors                                                                                                             
        public ElasticWithDensity(double youngsModulus, double poissonsRatio, double density)
        {
            // The constructor must work with E = 0
            SetYoungsModulus(new EquationContainer(typeof(StringPressureConverter), youngsModulus), false);
            // Use the method to perform any checks necessary
            PoissonsRatio = new EquationContainer(typeof(StringDoubleConverter), poissonsRatio);
            // The constructor must work with rho = 0
            SetDensity(new EquationContainer(typeof(StringDensityConverter), density), false);
        }
        public ElasticWithDensity(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_youngsModulus":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueE)
                            YoungsModulus = new EquationContainer(typeof(StringPressureConverter), valueE);
                        else
                            SetYoungsModulus((EquationContainer)entry.Value, false);
                        break;
                    case "_poissonsRatio":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueV)
                            PoissonsRatio = new EquationContainer(typeof(StringDoubleConverter), valueV);
                        else
                            SetPoissonsRatio((EquationContainer)entry.Value, false);
                        break;
                    case "_density":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueRho)
                            Density = new EquationContainer(typeof(StringDensityConverter), valueRho);
                        else
                            SetDensity((EquationContainer)entry.Value, false);
                        break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        [OnDeserialized]
        private void RestoreEquationCallbacks(StreamingContext context)
        {
            SetYoungsModulus(_youngsModulus, false);
            SetPoissonsRatio(_poissonsRatio, false);
            SetDensity(_density, false);
        }
        private void SetYoungsModulus(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _youngsModulus, value, CheckPositive, checkEquation);
        }
        private void SetPoissonsRatio(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _poissonsRatio, value, null, checkEquation);
        }
        private void SetDensity(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _density, value, CheckPositive, checkEquation);
        }
        //
        private double CheckPositive(double value)
        {
            if (value <= 0) throw new CaeException(_positive);
            else return value;
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            _youngsModulus.CheckEquation();
            _poissonsRatio.CheckEquation();
            _density.CheckEquation();
        }
    }
}
