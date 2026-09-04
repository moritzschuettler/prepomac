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
using System.ComponentModel;
using DynamicTypeDescriptor;
using CaeGlobals;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewElasticEngineeringConstants : ViewMaterialProperty
    {
        // Variables                                                                                                                
        private List<ElasticEngineeringConstantsDataPoint> _points;


        // Properties                                                                                                               
        public override string Name
        {
            get { return "Engineering Constants"; }
        }
        //
        [Browsable(false)]
        public List<ElasticEngineeringConstantsDataPoint> DataPoints { get { return _points; } set { _points = value; } }
        //
        [CategoryAttribute("Normal moduli")]
        [OrderedDisplayName(0, 3, "E1")]
        [DescriptionAttribute("Young's modulus in the material 1-direction.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString YoungsModulus1
        {
            get { return GetEquation(ElasticEngineeringConstants.YoungsModulus1Id); }
            set { SetEquation(ElasticEngineeringConstants.YoungsModulus1Id, value); }
        }
        //
        [CategoryAttribute("Normal moduli")]
        [OrderedDisplayName(1, 3, "E2")]
        [DescriptionAttribute("Young's modulus in the material 2-direction.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString YoungsModulus2
        {
            get { return GetEquation(ElasticEngineeringConstants.YoungsModulus2Id); }
            set { SetEquation(ElasticEngineeringConstants.YoungsModulus2Id, value); }
        }
        //
        [CategoryAttribute("Normal moduli")]
        [OrderedDisplayName(2, 3, "E3")]
        [DescriptionAttribute("Young's modulus in the material 3-direction.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString YoungsModulus3
        {
            get { return GetEquation(ElasticEngineeringConstants.YoungsModulus3Id); }
            set { SetEquation(ElasticEngineeringConstants.YoungsModulus3Id, value); }
        }
        //
        [CategoryAttribute("Poisson's ratios")]
        [OrderedDisplayName(0, 3, "v12")]
        [DescriptionAttribute("Poisson's ratio v12: the negative ratio of strain in direction 2 to strain in direction 1 " +
                              "for uniaxial loading in direction 1.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString PoissonsRatio12
        {
            get { return GetEquation(ElasticEngineeringConstants.PoissonsRatio12Id); }
            set { SetEquation(ElasticEngineeringConstants.PoissonsRatio12Id, value); }
        }
        //
        [CategoryAttribute("Poisson's ratios")]
        [OrderedDisplayName(1, 3, "v13")]
        [DescriptionAttribute("Poisson's ratio v13: the negative ratio of strain in direction 3 to strain in direction 1 " +
                              "for uniaxial loading in direction 1.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString PoissonsRatio13
        {
            get { return GetEquation(ElasticEngineeringConstants.PoissonsRatio13Id); }
            set { SetEquation(ElasticEngineeringConstants.PoissonsRatio13Id, value); }
        }
        //
        [CategoryAttribute("Poisson's ratios")]
        [OrderedDisplayName(2, 3, "v23")]
        [DescriptionAttribute("Poisson's ratio v23: the negative ratio of strain in direction 3 to strain in direction 2 " +
                              "for uniaxial loading in direction 2.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString PoissonsRatio23
        {
            get { return GetEquation(ElasticEngineeringConstants.PoissonsRatio23Id); }
            set { SetEquation(ElasticEngineeringConstants.PoissonsRatio23Id, value); }
        }
        //
        [CategoryAttribute("Shear moduli")]
        [OrderedDisplayName(0, 3, "G12")]
        [DescriptionAttribute("Shear modulus in the material 1-2 plane.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString ShearModulus12
        {
            get { return GetEquation(ElasticEngineeringConstants.ShearModulus12Id); }
            set { SetEquation(ElasticEngineeringConstants.ShearModulus12Id, value); }
        }
        //
        [CategoryAttribute("Shear moduli")]
        [OrderedDisplayName(1, 3, "G13")]
        [DescriptionAttribute("Shear modulus in the material 1-3 plane.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString ShearModulus13
        {
            get { return GetEquation(ElasticEngineeringConstants.ShearModulus13Id); }
            set { SetEquation(ElasticEngineeringConstants.ShearModulus13Id, value); }
        }
        //
        [CategoryAttribute("Shear moduli")]
        [OrderedDisplayName(2, 3, "G23")]
        [DescriptionAttribute("Shear modulus in the material 2-3 plane.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString ShearModulus23
        {
            get { return GetEquation(ElasticEngineeringConstants.ShearModulus23Id); }
            set { SetEquation(ElasticEngineeringConstants.ShearModulus23Id, value); }
        }


        // Constructors                                                                                                             
        public ViewElasticEngineeringConstants(ElasticEngineeringConstants elasticEngineeringConstants)
        {
            _points = new List<ElasticEngineeringConstantsDataPoint>();
            EquationContainer[][] data = elasticEngineeringConstants.EngineeringConstantsTemp;
            for (int i = 0; i < data.Length; i++) _points.Add(new ElasticEngineeringConstantsDataPoint(data[i]));
            //
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        private EquationString GetEquation(int id)
        {
            if (_points != null && _points.Count > 0) return GetContainer(_points[0], id).Equation;
            else return new EquationString("0");
        }
        private void SetEquation(int id, EquationString value)
        {
            if (_points != null && _points.Count > 0) GetContainer(_points[0], id).Equation = value;
        }
        private EquationContainer GetContainer(ElasticEngineeringConstantsDataPoint point, int id)
        {
            if (id == ElasticEngineeringConstants.YoungsModulus1Id) return point.YoungsModulus1;
            else if (id == ElasticEngineeringConstants.YoungsModulus2Id) return point.YoungsModulus2;
            else if (id == ElasticEngineeringConstants.YoungsModulus3Id) return point.YoungsModulus3;
            else if (id == ElasticEngineeringConstants.PoissonsRatio12Id) return point.PoissonsRatio12;
            else if (id == ElasticEngineeringConstants.PoissonsRatio13Id) return point.PoissonsRatio13;
            else if (id == ElasticEngineeringConstants.PoissonsRatio23Id) return point.PoissonsRatio23;
            else if (id == ElasticEngineeringConstants.ShearModulus12Id) return point.ShearModulus12;
            else if (id == ElasticEngineeringConstants.ShearModulus13Id) return point.ShearModulus13;
            else if (id == ElasticEngineeringConstants.ShearModulus23Id) return point.ShearModulus23;
            else throw new NotSupportedException();
        }
        public override MaterialProperty GetBase()
        {
            EquationContainer[][] data = new EquationContainer[_points.Count][];
            for (int i = 0; i < _points.Count; i++)
            {
                ElasticEngineeringConstantsDataPoint point = _points[i];
                data[i] = new EquationContainer[ElasticEngineeringConstants.NumberOfValues];
                data[i][ElasticEngineeringConstants.YoungsModulus1Id] = point.YoungsModulus1;
                data[i][ElasticEngineeringConstants.YoungsModulus2Id] = point.YoungsModulus2;
                data[i][ElasticEngineeringConstants.YoungsModulus3Id] = point.YoungsModulus3;
                data[i][ElasticEngineeringConstants.PoissonsRatio12Id] = point.PoissonsRatio12;
                data[i][ElasticEngineeringConstants.PoissonsRatio13Id] = point.PoissonsRatio13;
                data[i][ElasticEngineeringConstants.PoissonsRatio23Id] = point.PoissonsRatio23;
                data[i][ElasticEngineeringConstants.ShearModulus12Id] = point.ShearModulus12;
                data[i][ElasticEngineeringConstants.ShearModulus13Id] = point.ShearModulus13;
                data[i][ElasticEngineeringConstants.ShearModulus23Id] = point.ShearModulus23;
                data[i][ElasticEngineeringConstants.TemperatureId] = point.Temperature;
            }
            return new ElasticEngineeringConstants(data);
        }
    }
}
