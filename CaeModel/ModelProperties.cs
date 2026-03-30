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
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CaeGlobals;
using CaeMesh;
using CaeResults;
using DynamicTypeDescriptor;
using FileInOut.Output.Calculix;
using static System.Collections.Specialized.BitVector32;

namespace CaeModel
{
    [Serializable]
    public enum ModelType
    {
        [StandardValue("General", DisplayName = "General model")]
        GeneralModel,
        [StandardValue("Submodel", DisplayName = "Submodel")]
        Submodel,
        [StandardValue("SlipWear", DisplayName = "Slip wear model")]
        SlipWearModel
    }
    //
    [Serializable]
    public enum ModelSpaceEnum
    {
        [StandardValue("Undefined", Visible = false)]
        Undefined = 0,
        [StandardValue("ThreeD", DisplayName = "3D")]
        ThreeD = 1,
        [StandardValue("PlaneStress", DisplayName = "2D plane stress")]
        PlaneStress = 2,
        [StandardValue("PlaneStrain", DisplayName = "2D plane strain")]
        PlaneStrain = 3,
        [StandardValue("Axisymmetric", DisplayName = "2D axisymmetric")]
        Axisymmetric = 4
    }    
    //
    public static class ExtensionMethods
    {
        // ModelSpaceEnum
        public static bool IsTwoD(this ModelSpaceEnum modelSpace)
        {
            return (int)modelSpace > 1; // 2, 3 or 4 is 2D
        }
        //
        public static Dictionary<Type, HashSet<Enum>> GetAvailableElementTypes(this ModelSpaceEnum modelSpace,
                                                                               bool allowMixedModel)
        {            
            List<Type> elementTypes = new List<Type>();
            elementTypes.Add(typeof(FeElementTypeLinearTria));
            elementTypes.Add(typeof(FeElementTypeParabolicTria));
            elementTypes.Add(typeof(FeElementTypeLinearQuad));
            elementTypes.Add(typeof(FeElementTypeParabolicQuad));
            elementTypes.Add(typeof(FeElementTypeLinearTetra));
            elementTypes.Add(typeof(FeElementTypeParabolicTetra));
            elementTypes.Add(typeof(FeElementTypeLinearWedge));
            elementTypes.Add(typeof(FeElementTypeParabolicWedge));
            elementTypes.Add(typeof(FeElementTypeLinearHexa));
            elementTypes.Add(typeof(FeElementTypeParabolicHexa));
            //
            int type = 0;
            if (modelSpace == ModelSpaceEnum.ThreeD) type = 1;
            else if (modelSpace == ModelSpaceEnum.PlaneStress) type = 2;
            else if (modelSpace == ModelSpaceEnum.PlaneStrain) type = 3;
            else if (modelSpace == ModelSpaceEnum.Axisymmetric) type = 4;
            HashSet<Enum> elementEnums;
            Dictionary<Type, HashSet<Enum>> elementTypeEnums = new Dictionary<Type, HashSet<Enum>>();
            //
            foreach (Type elementType in elementTypes)
            {
                foreach (var item in Enum.GetValues(elementType))
                {
                    if ((int)item / 10 == type || (allowMixedModel && type == 4 && (int)item / 10 == 2))
                    {
                        if (elementTypeEnums.TryGetValue(elementType, out elementEnums)) elementEnums.Add((Enum)item);
                        else elementTypeEnums.Add(elementType, new HashSet<Enum>() { (Enum)item });
                    }
                }
            }
            //
            return elementTypeEnums;
        }
        //
        public static Dictionary<Type, HashSet<string>> GetUnavailableElementTypeNames(this ModelSpaceEnum modelSpace)
        {
            List<Type> elementTypes = new List<Type>();
            elementTypes.Add(typeof(FeElementTypeLinearTria));
            elementTypes.Add(typeof(FeElementTypeParabolicTria));
            elementTypes.Add(typeof(FeElementTypeLinearQuad));
            elementTypes.Add(typeof(FeElementTypeParabolicQuad));
            elementTypes.Add(typeof(FeElementTypeLinearTetra));
            elementTypes.Add(typeof(FeElementTypeParabolicTetra));
            elementTypes.Add(typeof(FeElementTypeLinearWedge));
            elementTypes.Add(typeof(FeElementTypeParabolicWedge));
            elementTypes.Add(typeof(FeElementTypeLinearHexa));
            elementTypes.Add(typeof(FeElementTypeParabolicHexa));
            //
            int type = 0;
            if (modelSpace == ModelSpaceEnum.ThreeD) type = 1;
            else if (modelSpace == ModelSpaceEnum.PlaneStress) type = 2;
            else if (modelSpace == ModelSpaceEnum.PlaneStrain) type = 3;
            else if (modelSpace == ModelSpaceEnum.Axisymmetric) type = 4;
            HashSet<string> elementEnums;
            Dictionary<Type, HashSet<string>> unavailableElementTypeNames = new Dictionary<Type, HashSet<string>>();
            //
            foreach (Type elementType in elementTypes)
            {
                foreach (var item in Enum.GetValues(elementType))
                {
                    // Remove all elements of the wrong type
                    if ((int)item > 0 && (int)item / 10 != type) 
                    {
                        // For axisymmetric models allow plane stress elements
                        if (type == 4 && (int)item > 0 && (int)item / 10 == 2) continue;
                        //
                        if (unavailableElementTypeNames.TryGetValue(elementType, out elementEnums))
                            elementEnums.Add(item.ToString());
                        else
                            unavailableElementTypeNames.Add(elementType, new HashSet<string>() { item.ToString() });
                    }
                }
            }
            //
            return unavailableElementTypeNames;
        }
    }
    //
    [Serializable]
    public class ModelProperties : ISerializable, IContainsEquations
    {
        // Variables                                                                                                                
        private ModelSpaceEnum _modelSpace;                 //ISerializable
        private ModelType _modelType;                       //ISerializable
        // Submodel
        private string _globalResultsFileName;              //ISerializable
        // Slip wear model
        private SlipWearResultsEnum _slipWearResults;       //ISerializable
        private EquationContainer _numberOfCycles;          //ISerializable
        private EquationContainer _cyclesIncrement;         //ISerializable
        private EquationContainer _numOfSmoothingSteps;     //ISerializable
        private bool _bdmRemeshing;                         //ISerializable
        //
        private double _absoluteZero;                       //ISerializable
        private double _stefanBoltzmann;                    //ISerializable
        private double _newtonGravity;                      //ISerializable


        // Properties                                                                                                               
        public ModelSpaceEnum ModelSpace { get { return _modelSpace; } set { _modelSpace = value; } }
        public ModelType ModelType { get { return _modelType; } set { _modelType = value; } }
        //
        public string GlobalResultsFileName
        {
            get { return _modelType == ModelType.Submodel ? _globalResultsFileName : null; }
            set { _globalResultsFileName = value; }
        }
        //
        public SlipWearResultsEnum SlipWearResults { get { return _slipWearResults; } set { _slipWearResults = value; } }
        public EquationContainer NumberOfCycles { get { return _numberOfCycles; } set { SetNumOfCycles(value); } }
        public EquationContainer CyclesIncrement { get { return _cyclesIncrement; } set { SetCyclesIncrement(value); } }
        public EquationContainer NumOfSmoothingSteps { get { return _numOfSmoothingSteps; } set { SetNumOfSmoothingSteps(value); } }
        public bool BdmRemeshing { get { return _bdmRemeshing; } set { _bdmRemeshing = value; } }
        //
        public double AbsoluteZero { get { return _absoluteZero; } set { _absoluteZero = value; } }
        public double StefanBoltzmann { get { return _stefanBoltzmann; } set { _stefanBoltzmann = value; } }
        public double NewtonGravity { get { return _newtonGravity; } set { _newtonGravity = value; } }


        // Constructors                                                                                                             
        public ModelProperties()
        {
            _modelSpace = ModelSpaceEnum.Undefined;
            _modelType = ModelType.GeneralModel;
            // Submodel
            _globalResultsFileName = null;
            // Slip wear model
            _slipWearResults = SlipWearResultsEnum.All;
            NumberOfCycles = new EquationContainer(typeof(StringIntegerConverter), 1, CheckLessThanOne);
            CyclesIncrement = new EquationContainer(typeof(StringIntegerConverter), 1, CheckLessThanOne);
            NumOfSmoothingSteps = new EquationContainer(typeof(StringIntegerConverter), 1, CheckNegative);
            _bdmRemeshing = false;
            //
            _absoluteZero = double.PositiveInfinity;
            _stefanBoltzmann = double.PositiveInfinity;
            _newtonGravity = double.PositiveInfinity;
        }
        public ModelProperties(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "ModelSpace":              // Compatibility for version v1.4.0
                    case "_modelSpace":
                        _modelSpace = (ModelSpaceEnum)entry.Value; break;
                    case "ModelType":               // Compatibility for version v1.4.0
                    case "_modelType":
                        _modelType = (ModelType)entry.Value; break;
                    case "GlobalResultsFileName":   // Compatibility for version v1.4.0
                    case "_globalResultsFileName":
                        _globalResultsFileName = (string)entry.Value; break;
                    case "SlipWearResults":         // Compatibility for version v1.4.0
                    case "_slipWearResults":
                        _slipWearResults = (SlipWearResultsEnum)entry.Value; break;
                    case "_numberOfCycles":
                        if (entry.Value is int valueNC)
                            NumberOfCycles = new EquationContainer(typeof(StringIntegerConverter), valueNC);
                        else
                            SetNumOfCycles((EquationContainer)entry.Value, false);
                        break;
                    case "_cyclesIncrement":
                        if (entry.Value is int valueCI)
                            CyclesIncrement = new EquationContainer(typeof(StringIntegerConverter), valueCI);
                        else
                            SetCyclesIncrement((EquationContainer)entry.Value, false);
                        break;
                    case "_numOfSmoothingSteps":
                        // Compatibility for version v2.3.9
                        if (entry.Value is int valueNSS)
                            NumOfSmoothingSteps = new EquationContainer(typeof(StringIntegerConverter), valueNSS);
                        else
                            SetNumOfSmoothingSteps((EquationContainer)entry.Value, false);
                        break;
                    case "_bdmRemeshing":
                        _bdmRemeshing = (bool)entry.Value; break;
                    case "AbsoluteZero":            // Compatibility for version v1.4.0
                    case "_absoluteZero":
                        _absoluteZero = (double)entry.Value; break;
                    case "StefanBoltzmann":         // Compatibility for version v1.4.0
                    case "_stefanBoltzmann":
                        _stefanBoltzmann = (double)entry.Value; break;
                    case "NewtonGravity":           // Compatibility for version v1.4.0
                    case "_newtonGravity":
                        _newtonGravity = (double)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        private void SetNumOfCycles(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _numberOfCycles, value, CheckLessThanOne, checkEquation);
        }
        private void SetCyclesIncrement(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _cyclesIncrement, value, CheckLessThanOne, checkEquation);
        }
        private void SetNumOfSmoothingSteps(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _numOfSmoothingSteps, value, CheckNegative, checkEquation);
        }
        private double CheckNegative(double value)
        {
            return value < 0 ? 0 : value;
        }
        private double CheckLessThanOne(double value)
        {
            return value < 1 ? 1 : value;
        }
        public bool IsAbsoluteZeroDefined()
        {
            return AbsoluteZero != double.PositiveInfinity;
        }
        public bool IsStefanBoltzmannDefined()
        {
            return StefanBoltzmann != double.PositiveInfinity;
        }
        // IContainsEquations
        public virtual void CheckEquations()
        {
            _numOfSmoothingSteps.CheckEquation();
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
        // ISerialization
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            info.AddValue("ModelSpace", _modelSpace, typeof(ModelSpaceEnum));
            info.AddValue("ModelType", _modelType, typeof(ModelType));
            info.AddValue("GlobalResultsFileName", _globalResultsFileName, typeof(string));
            info.AddValue("SlipWearResults", _slipWearResults, typeof(SlipWearResultsEnum));
            info.AddValue("_numberOfCycles", _numberOfCycles, typeof(EquationContainer));
            info.AddValue("_cyclesIncrement", _cyclesIncrement, typeof(EquationContainer));
            info.AddValue("_numOfSmoothingSteps", _numOfSmoothingSteps, typeof(EquationContainer));
            info.AddValue("_bdmRemeshing", _bdmRemeshing, typeof(bool));
            info.AddValue("AbsoluteZero", _absoluteZero, typeof(double));
            info.AddValue("StefanBoltzmann", _stefanBoltzmann, typeof(double));
            info.AddValue("NewtonGravity", _newtonGravity, typeof(double));
        }
    }
}
