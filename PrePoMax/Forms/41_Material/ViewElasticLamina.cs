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
    public class ViewElasticLamina : ViewMaterialProperty
    {
        // Variables                                                                                                                
        private List<ElasticLaminaDataPoint> _points;


        // Properties                                                                                                               
        public override string Name
        {
            get { return "Lamina"; }
        }
        //
        [Browsable(false)]
        public List<ElasticLaminaDataPoint> DataPoints { get { return _points; } set { _points = value; } }
        //
        [CategoryAttribute("Normal moduli")]
        [OrderedDisplayName(0, 2, "E1")]
        [DescriptionAttribute("Young's modulus in the material 1-direction.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString YoungsModulus1
        {
            get { return GetEquation(ElasticLamina.YoungsModulus1Id); }
            set { SetEquation(ElasticLamina.YoungsModulus1Id, value); }
        }
        //
        [CategoryAttribute("Normal moduli")]
        [OrderedDisplayName(1, 2, "E2")]
        [DescriptionAttribute("Young's modulus in the material 2-direction.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString YoungsModulus2
        {
            get { return GetEquation(ElasticLamina.YoungsModulus2Id); }
            set { SetEquation(ElasticLamina.YoungsModulus2Id, value); }
        }
        //
        [CategoryAttribute("Poisson's ratios")]
        [OrderedDisplayName(0, 1, "v12")]
        [DescriptionAttribute("Poisson's ratio v12: the negative ratio of strain in direction 2 to strain in direction 1 " +
                              "for uniaxial loading in direction 1.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString PoissonsRatio12
        {
            get { return GetEquation(ElasticLamina.PoissonsRatio12Id); }
            set { SetEquation(ElasticLamina.PoissonsRatio12Id, value); }
        }
        //
        [CategoryAttribute("Shear moduli")]
        [OrderedDisplayName(0, 3, "G12")]
        [DescriptionAttribute("Shear modulus in the material 1-2 plane.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString ShearModulus12
        {
            get { return GetEquation(ElasticLamina.ShearModulus12Id); }
            set { SetEquation(ElasticLamina.ShearModulus12Id, value); }
        }
        //
        [CategoryAttribute("Shear moduli")]
        [OrderedDisplayName(1, 3, "G13")]
        [DescriptionAttribute("Transverse shear modulus in the material 1-3 plane.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString ShearModulus13
        {
            get { return GetEquation(ElasticLamina.ShearModulus13Id); }
            set { SetEquation(ElasticLamina.ShearModulus13Id, value); }
        }
        //
        [CategoryAttribute("Shear moduli")]
        [OrderedDisplayName(2, 3, "G23")]
        [DescriptionAttribute("Transverse shear modulus in the material 2-3 plane.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString ShearModulus23
        {
            get { return GetEquation(ElasticLamina.ShearModulus23Id); }
            set { SetEquation(ElasticLamina.ShearModulus23Id, value); }
        }


        // Constructor                                                                                                              
        public ViewElasticLamina(ElasticLamina elasticLamina)
        {
            _points = new List<ElasticLaminaDataPoint>();
            EquationContainer[][] data = elasticLamina.LaminaConstantsTemp;
            for (int i = 0; i < data.Length; i++) _points.Add(new ElasticLaminaDataPoint(data[i]));
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
        private EquationContainer GetContainer(ElasticLaminaDataPoint point, int id)
        {
            if (id == ElasticLamina.YoungsModulus1Id) return point.YoungsModulus1;
            else if (id == ElasticLamina.YoungsModulus2Id) return point.YoungsModulus2;
            else if (id == ElasticLamina.PoissonsRatio12Id) return point.PoissonsRatio12;
            else if (id == ElasticLamina.ShearModulus12Id) return point.ShearModulus12;
            else if (id == ElasticLamina.ShearModulus13Id) return point.ShearModulus13;
            else if (id == ElasticLamina.ShearModulus23Id) return point.ShearModulus23;
            else throw new NotSupportedException();
        }
        public override MaterialProperty GetBase()
        {
            EquationContainer[][] data = new EquationContainer[_points.Count][];
            for (int i = 0; i < _points.Count; i++)
            {
                ElasticLaminaDataPoint point = _points[i];
                data[i] = new EquationContainer[ElasticLamina.NumberOfValues];
                data[i][ElasticLamina.YoungsModulus1Id] = point.YoungsModulus1;
                data[i][ElasticLamina.YoungsModulus2Id] = point.YoungsModulus2;
                data[i][ElasticLamina.PoissonsRatio12Id] = point.PoissonsRatio12;
                data[i][ElasticLamina.ShearModulus12Id] = point.ShearModulus12;
                data[i][ElasticLamina.ShearModulus13Id] = point.ShearModulus13;
                data[i][ElasticLamina.ShearModulus23Id] = point.ShearModulus23;
                data[i][ElasticLamina.TemperatureId] = point.Temperature;
            }
            return new ElasticLamina(data);
        }
    }
}
