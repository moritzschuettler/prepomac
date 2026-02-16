using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CaeResults
{
    class HOSetNames
    {
        public const string ContactWear = "CONTACT_WEAR";
        public const string AllContactElements = "ALL_CONTACT_ELEMENTS";
    }
    class HOFieldNames
    {
        public const string ComplexRealSuffix = "_RE";
        public const string ComplexImaginarySuffix = "_IM";
        public const string ComplexMagnitudeSuffix = "_MAG";
        public const string ComplexPhaseSuffix = "_PHA";
        //
        public const string Time = "TIME";
        public const string Factor = "FACTOR";
        public const string Frequency = "FREQUENCY";
        public const string Rotation = "ROTATION";
        public const string Buckling = "BUCKLING";
        // Nodal
        public const string Coordinates = "COORDINATES";
        public const string Displacements = "DISPLACEMENTS";
        public const string Velocities = "VELOCITIES";
        public const string Forces = "FORCES";
        public const string TotalForce = "TOTAL_FORCE";                                                 //_
        public const string Stresses = "STRESSES";
        public const string Strains = "STRAINS";
        public const string MechanicalStrains = "MECHANICAL_STRAINS";                                   //_
        public const string EquivalentPlasticStrains = "EQUIVALENT_PLASTIC_STRAIN";                     //_
        public const string InternalEnergyDensity = "INTERNAL_ENERGY_DENSITY";                          //_
        // Thermal
        public const string Temperatures = "TEMPERATURES";
        public const string HeatGeneration = "HEAT_GENERATION";                                         //_
        public const string TotalHeatGeneration = "TOTAL_HEAT_GENERATION";                              //_
        // Frequency
        public const string EigenvalueOutput = "EIGENVALUE_OUTPUT";                                     //_
        public const string ParticipationFactors = "PARTICIPATION_FACTORS";                             //_
        public const string EffectiveModalMass = "EFFECTIVE_MODAL_MASS";                                //_
        public const string TotalEffectiveModalMass = "TOTAL_EFFECTIVE_MODAL_MASS";                     //_
        public const string TotalEffectiveMass = "TOTAL_EFFECTIVE_MASS";                                //_
        public const string RelativeEffectiveModalMass = "RELATIVE_EFFECTIVE_MODAL_MASS";               //_
        public const string RelativeTotalEffectiveModalMass = "RELATIVE_TOTAL_EFFECTIVE_MODAL_MASS";    //_
        // Steady state
        public const string ParticipationFactorsForFrequency = "PARTICIPATION_FACTORS_FOR_FREQUENCY";   //_
        // Complex frequency
        public const string ParticipationFactorsForMode = "PARTICIPATION_FACTORS_FOR_MODE";             //_
        public const string ModalAssuranceCriterium = "MODAL_ASSURANCE_CRITERIUM";                      //_
        public const string AxisDirection = "AXIS_DIRECTION";                                           //_
        public const string TurningDirection = "TURNING_DIRECTION";                                     //_
        // Contact
        public const string RelativeContactDisplacement = "RELATIVE_CONTACT_DISPLACEMENT";              //_
        public const string ContactStress = "CONTACT_STRESS";                                           //_
        public const string ContactPrintEnergy = "CONTACT_PRINT_ENERGY";                                //_
        public const string ContactSpringEnergy = "CONTACT_SPRING_ENERGY";                              //_
        public const string TotalNumberOfContactElements = "TOTAL_NUMBER_OF_CONTACT_ELEMENTS";          //_
        // Set
        public const string StatisticsForSlaveSet = "STATISTICS_FOR_SLAVE_SET";                         //_
        public const string StatisticsForSurfaceSet = "STATISTICS_FOR_SURFACE_SET";                     //_
        public const string TotalSurfaceForce = "TOTAL_SURFACE_FORCE";                                  //_
        public const string MomentAboutOrigin = "MOMENT_ABOUT_ORIGIN";                                  //_
        public const string CenterOfGravityCG = "CENTER_OF_GRAVITY_CG";                                 //_
        public const string MeanSurfaceNormal = "MEAN_SURFACE_NORMAL";                                  //_
        public const string MomentAboutCG = "MOMENT_ABOUT_CG";                                          //_
        public const string SurfaceArea = "SURFACE_AREA";                                               //_
        public const string SurfaceLoads = "SURFACE_LOADS";                                             //_
        // Element
        public const string Volume = "VOLUME";
        public const string TotalVolume = "TOTAL_VOLUME";                                               //_
        public const string InternalEnergy = "INTERNAL_ENERGY";                                         //_
        public const string TotalInternalEnergy = "TOTAL_INTERNAL_ENERGY";                              //_
        // Thermal
        public const string HeatFlux = "HEAT_FLUX";                                                     //_
        public const string BodyHeating = "BODY_HEATING";                                               //_
        public const string TotalBodyHeating = "TOTAL_BODY_HEATING";                                    //_
        // Wear
        public const string SlidingDistance = "SLIDING_DISTANCE";                                       //_
        public const string SurfaceNormal = "SURFACE_NORMAL";                                           //_
        // Error
        public const string Error = "ERROR";


        // Methods
        public static string FixHOFieldNames(string equation)
        {
            string name;
            string result = equation;
            List<string> allNames =
                typeof(HOFieldNames)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue())
                .ToList();
            //
            foreach (var constantName in allNames)
            {
                if (!constantName.StartsWith("_") && constantName.Contains("_"))    // skipp the suffixes
                {
                    name = constantName.Replace("_", " ");
                    result = result.Replace(name, constantName);
                }
            }
            //
            return result;
        }

        public static string GetNoSuffixName(string name)
        {
            if (name.EndsWith(ComplexRealSuffix))
                return name.Substring(0, name.Length - ComplexRealSuffix.Length);
            else if (name.EndsWith(ComplexImaginarySuffix))
                return name.Substring(0, name.Length - ComplexImaginarySuffix.Length);
            else if (name.EndsWith(ComplexMagnitudeSuffix))
                return name.Substring(0, name.Length - ComplexMagnitudeSuffix.Length);
            else if (name.EndsWith(ComplexPhaseSuffix))
                return name.Substring(0, name.Length - ComplexPhaseSuffix.Length);
            else return name;
        }
        public static bool HasRealComplexSuffix(string name)
        {
            if (name.EndsWith(ComplexRealSuffix)) return true;
            else return false;
        }
        public static bool HasComplexSuffix(string name)
        {
            if (name.EndsWith(ComplexRealSuffix) ||
                name.EndsWith(ComplexImaginarySuffix) ||
                name.EndsWith(ComplexMagnitudeSuffix) ||
                name.EndsWith(ComplexPhaseSuffix))
                return true;
            else return false;
        }
    }
    //
    class HOComponentNames
    {
        public const string All = "ALL";
        //
        public const string Mises = "MISES";
        public const string Tresca = "TRESCA";
        public const string S11 = "S11";
        public const string S22 = "S22";
        public const string S33 = "S33";
        public const string S12 = "S12";
        public const string S23 = "S23";
        public const string S13 = "S13";
        //
        public const string SgnMaxAbsPri = "SGN_MAX_ABS_PRI";
        public const string PrincipalMax = "PRINCIPAL_MAX";
        public const string PrincipalMid = "PRINCIPAL_MID";
        public const string PrincipalMin = "PRINCIPAL_MIN";
        //
        public const string E11 = "E11";
        public const string E22 = "E22";
        public const string E33 = "E33";
        public const string E12 = "E12";
        public const string E23 = "E23";
        public const string E13 = "E13";
        //
        public const string Tang1 = "TANG1";
        public const string Tang2 = "TANG2";
        public const string S1 = "S1";
        public const string S2 = "S2";
        //
        public const string N1 = "N1";
        public const string N2 = "N2";
        public const string N3 = "N3";
        // Frequency
        public const string EIGENVALUE = "EIGENVALUE";
        public const string OMEGA = "OMEGA";
        public const string FREQUENCY = "FREQUENCY";
        public const string FREQUENCY_IM = "FREQUENCY_IM";
        // Complex frequency
        public const string PAR_FACTOR = "PAR_FACTOR";  // participation factor
        public const string PAR_FACTOR_IM = "PAR_FACTOR_IM";
        public const string MODE = "MODE";
        public const string DIRECTION = "DIRECTION";
        // Buckling
        public const string MODENUMBER = "MODE_NUMBER";
        public const string BUCKLINGFACTOR = "BUCKLING_FACTOR";
        //
        public const string XCOMPONENT = "X_COMPONENT";
        public const string YCOMPONENT = "Y_COMPONENT";
        public const string ZCOMPONENT = "Z_COMPONENT";
        public const string XROTATION = "X_ROTATION";
        public const string YROTATION = "Y_ROTATION";
        public const string ZROTATION = "Z_ROTATION";
        //
        public const string NORMAL_FORCE = "NORMAL_FORCE";
        public const string SHEAR_FORCE = "SHEAR_FORCE";
        public const string TORQUE = "TORQUE";
        public const string BENDING_MOMENT = "BENDING_MOMENT";
        //
        public const string X = "X";
        public const string Y = "Y";
        public const string Z = "Z";

    }


}
