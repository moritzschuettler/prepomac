using System;
using System.Collections.Generic;
using System.Linq;
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
        public const string TotalForce = "TOTAL FORCE";
        public const string Stresses = "STRESSES";
        public const string Strains = "STRAINS";
        public const string MechanicalStrains = "MECHANICAL_STRAINS";
        public const string EquivalentPlasticStrains = "EQUIVALENT_PLASTIC_STRAIN";
        public const string InternalEnergyDensity = "INTERNAL_ENERGY_DENSITY";
        // Thermal
        public const string Temperatures = "TEMPERATURES";
        public const string HeatGeneration = "HEAT_GENERATION";
        public const string TotalHeatGeneration = "TOTAL_HEAT_GENERATION";
        // Frequency
        public const string EigenvalueOutput = "EIGENVALUE_OUTPUT";
        public const string ParticipationFactors = "PARTICIPATION_FACTORS";
        public const string EffectiveModalMass = "EFFECTIVE_MODAL_MASS";
        public const string TotalEffectiveModalMass = "TOTAL_EFFECTIVE_MODAL_MASS";
        public const string TotalEffectiveMass = "TOTAL_EFFECTIVE_MASS";
        public const string RelativeEffectiveModalMass = "RELATIVE_EFFECTIVE_MODAL_MASS";
        public const string RelativeTotalEffectiveModalMass = "RELATIVE_TOTAL_EFFECTIVE_MODAL_MASS";
        // Steady state
        public const string ParticipationFactorsForFrequency = "PARTICIPATION_FACTORS_FOR_FREQUENCY";
        // Complex frequency
        public const string ParticipationFactorsForMode = "PARTICIPATION_FACTORS_FOR_MODE";
        public const string ModalAssuranceCriterium = "MODAL_ASSURANCE_CRITERIUM";
        public const string AxisDirection = "AXIS_DIRECTION";
        public const string TurningDirection = "TURNING_DIRECTION";
        //
        public const string RelativeContactDisplacement = "RELATIVE_CONTACT_DISPLACEMENT";
        public const string ContactStress = "CONTACT_STRESS";
        public const string ContactPrintEnergy = "CONTACT_PRINT_ENERGY";    // for CalculiX 2.21
        public const string ContactSpringEnergy = "CONTACT_SPRING_ENERGY";
        public const string TotalNumberOfContactElements = "TOTAL_NUMBER_OF_CONTACT_ELEMENTS";
        public const string StatisticsForSlaveSet = "STATISTICS_FOR_SLAVE_SET";
        public const string TotalSurfaceForce = "TOTAL_SURFACE_FORCE";
        public const string MomentAboutOrigin = "MOMENT_ABOUT_ORIGIN";
        public const string CenterOgGravityCG = "CENTER_OF_GRAVITY_CG";
        public const string MeanSurfaceNormal = "MEAN_SURFACE_NORMAL";
        public const string MomentAboutCG = "MOMENT_ABOUT_CG";
        public const string SurfaceArea = "SURFACE_AREA";
        public const string NormalSurfaceForce = "NORMAL_SURFACE_FORCE";
        public const string ShearSurfaceForce = "SHEAR_SURFACE_FORCE";
        // Element
        public const string Volume = "VOLUME";
        public const string TotalVolume = "TOTAL_VOLUME";
        public const string InternalEnergy = "INTERNAL_ENERGY";
        public const string TotalInternalEnergy = "TOTAL_INTERNAL_ENERGY";
        // Thermal
        public const string HeatFlux = "HEAT_FLUX";
        public const string BodyHeating = "BODY_HEATING";
        public const string TotalBodyHeating = "TOTAL_BODY_HEATING";
        // Wear
        public const string SlidingDistance = "SLIDING_DISTANCE";
        public const string SurfaceNormal = "SURFACE_NORMAL";
        // Error
        public const string Error = "ERROR";


        // Methods
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
        public const string X = "X";
        public const string Y = "Y";
        public const string Z = "Z";

    }


}
