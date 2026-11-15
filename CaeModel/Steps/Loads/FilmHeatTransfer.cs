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
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;

namespace CaeModel
{
    [PmxType(3260)]
    public class FilmHeatTransfer : Load
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _surfaceName;                    //ISerializable
        [PmxKey(1)] private RegionTypeEnum _regionType;             //ISerializable
        [PmxKey(2)] private EquationContainer _sinkTemperature;     //ISerializable
        [PmxKey(3)] private EquationContainer _filmCoefficient;     //ISerializable
        [PmxKey(4)] protected string _coefficientAmplitudeName;     //ISerializable


        // Properties                                                                                                               
        public string SurfaceName { get { return _surfaceName; } set { _surfaceName = value; } }
        public override string RegionName { get { return _surfaceName; } set { _surfaceName = value; } }
        public override RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public EquationContainer SinkTemperature { get { return _sinkTemperature; } set { SetSinkTemperature(value); } }
        public EquationContainer FilmCoefficient { get { return _filmCoefficient; } set { SetFilmCoefficient(value); } }
        public string CoefficientAmplitudeName
        {
            get
            {
                if (_coefficientAmplitudeName == null) return Amplitude.DefaultAmplitudeName;
                else return _coefficientAmplitudeName;
            }
            set
            {
                _coefficientAmplitudeName = value;
                if (_coefficientAmplitudeName == Amplitude.DefaultAmplitudeName) _coefficientAmplitudeName = null;
            }
        }


        // Constructors                                                                                                             
        public FilmHeatTransfer(string name, string surfaceName, RegionTypeEnum regionType, double sinkTemperature,
                                double filmCoefficient, bool twoD)
            : base(name, twoD) 
        {
            _surfaceName = surfaceName;
            _regionType = regionType;
            SinkTemperature = new EquationContainer(typeof(StringTemperatureConverter), sinkTemperature);
            FilmCoefficient = new EquationContainer(typeof(StringHeatTransferCoefficientConverter), filmCoefficient);
            _coefficientAmplitudeName = null;
        }
        public FilmHeatTransfer(SerializationInfo info, StreamingContext context)
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
                    case "_sinkTemperature":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueSink)
                            SinkTemperature = new EquationContainer(typeof(StringTemperatureConverter), valueSink);
                        else
                            SetSinkTemperature((EquationContainer)entry.Value, false);
                        break;
                    case "_filmCoefficient":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueCoefficient)
                            FilmCoefficient = new EquationContainer(typeof(StringHeatTransferCoefficientConverter),
                                                                    valueCoefficient);
                        else
                            SetFilmCoefficient((EquationContainer)entry.Value, false);
                        break;
                    case "_coefficientAmplitudeName":
                        _coefficientAmplitudeName = (string)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        [OnDeserialized]
        private void RestoreEquationCallbacks(StreamingContext context)
        {
            SetSinkTemperature(_sinkTemperature, false);
            SetFilmCoefficient(_filmCoefficient, false);
        }
        private void SetSinkTemperature(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _sinkTemperature, value, null, checkEquation);
        }
        private void SetFilmCoefficient(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _filmCoefficient, value, null, checkEquation);
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _sinkTemperature.CheckEquation();
            _filmCoefficient.CheckEquation();
        }
    }
}
