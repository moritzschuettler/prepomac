using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;
using UnitsNet.Units;

namespace CaeGlobals
{
    static class MyUnit
    {
        static public readonly int NoUnit = 255;
        static public readonly EnergyUnit PoundForceInch = (EnergyUnit)100;
        static public readonly PowerUnit PoundForceInchPerSecond = (PowerUnit)101;
        static public readonly ThermalConductivityUnit PoundForcePerSecondFahrenheit = (ThermalConductivityUnit)102;
        static public readonly HeatTransferCoefficientUnit PoundForcePerInchSecondFahrenheit = (HeatTransferCoefficientUnit)103;
        static public readonly PowerPerAreaUnit PoundForcePerInchSecond = (PowerPerAreaUnit)104;
        static public readonly PowerPerVolumeUnit PoundForcePerSquareInchSecond = (PowerPerVolumeUnit)105;
        //
        static public readonly MassUnit PoundForceSquareSecondPerInch = (MassUnit)106;
        static public readonly DensityUnit PoundForceSquareSecondPerQuadInch = (DensityUnit)107;
        static public readonly SpecificHeatUnit SquareInchPerSquareSecondFahrenheit = (SpecificHeatUnit)108;
        static public readonly StefanBoltzmannUnit PoundForcePerInchSecondQuadFahrenheit = (StefanBoltzmannUnit)109;
        static public readonly NewtonGravityUnit QuadInchPerPoundForceQuadSecond = (NewtonGravityUnit)110;
        //
        //http://www2.me.rochester.edu/courses/ME204/nx_help/index.html#uid:id1246862
        //
        static public string PoundForceInchAbbreviation = "lbf·in";                                 // Energy
        static public string PoundForceInchPerSecondAbbreviation = "lbf·in/s";                      // Power
        static public string PoundForcePerSecondFahrenheitAbbreviation = "lbf/(s·°F)";              // Thermal conductivity
        static public string PoundForcePerInchSecondFahrenheitAbbreviation = "lbf/(in·s·°F)";       // Heat transfer coefficient
        static public string PoundForcePerInchSecondAbbreviation = "lbf/(in·s)";                    // Power per area
        static public string PoundForcePerSquareInchSecondAbbreviation = "lbf/(in²·s)";             // Power per volume
        //
        static public string PoundForceSquareSecondPerInchAbbreviation = "lbf·s²/in";               // Mass
        static public string PoundForceSquareSecondPerQuadInchAbbreviation = "lbf·s²/in⁴";          // Density
        static public string SquareInchPerSquareSecondFahrenheitAbbreviation = "in²/(s²·°F)";       // SpecificHeat
        static public string PoundForcePerInchSecondQuadFahrenheitAbbreviation = "lbf/(in·s·°F⁴)";  // Stefan-Boltzmann
        static public string QuadInchPerPoundForceQuadSecondAbbreviation = "in⁴/(lbf·s⁴)";          // Newton

    }
}
