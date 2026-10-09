// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python
{
    // A surface interaction property made by pmx.create or read back from a surface interaction. Its values are
    // reported under the names of the pmx.create arguments, so that
    // pmx.create.<property_type>(**property.get_values()) makes the same property again.
    public class SurfaceInteractionPropertyAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> PropertyTypes = new Dictionary<Type, string>
        {
            { typeof(Friction), "friction" },
            { typeof(GapConductance), "gap_conductance" },
            { typeof(SurfaceBehavior), "surface_behavior" }
        };
        private readonly SurfaceInteractionProperty _property;


        // Properties
        [PythonAPI("The property type, named after the pmx.create method that makes it, for example friction.",
                   Returns = "str")]
        public string property_type { get { return GetPropertyType(_property); } }


        // Constructors
        internal SurfaceInteractionPropertyAPI(SurfaceInteractionProperty property)
        {
            _property = property;
        }


        // Static methods
        internal static string GetPropertyType(SurfaceInteractionProperty property)
        {
            string propertyType;
            if (PropertyTypes.TryGetValue(property.GetType(), out propertyType)) return propertyType;
            //
            throw new PythonAPIException("The surface interaction property " + property.GetType().Name +
                                         " is not made by any pmx.create method.");
        }
        // A value the property leaves at its default is stored as NaN, which the pmx.create argument takes as None
        private static object ToValue(double value)
        {
            return double.IsNaN(value) ? null : (object)value;
        }
        private static NumericArrayAPI ToTable(double[][] table)
        {
            int columnCount = table.Length == 0 ? 0 : table[0].Length;
            double[,] values = new double[table.Length, columnCount];
            for (int row = 0; row < table.Length; row++)
            {
                for (int column = 0; column < columnCount; column++) values[row, column] = table[row][column];
            }
            //
            return new NumericArrayAPI(values);
        }
        private static string FormatValue(object value)
        {
            if (value == null) return "None";
            if (value is string) return StringListAPI.ToDisplayString((string)value);
            if (value is double) return PythonValueConverter.ToRoundTripString((double)value);
            //
            return value.ToString();
        }


        // Methods
        [PythonAPI("Returns the values of the property under the names of the pmx.create arguments that set " +
                   "them, in the form those arguments take them; a value left at its default reads back as " +
                   "None. A surface behavior reports only the values its pressure_overclosure_type uses. " +
                   "Passing the result back as keyword arguments to the pmx.create method named by " +
                   "property_type makes the same property.",
                   Returns = "dict[str, float, str, NumericArray or None]",
                   Example = "pmx.create.friction(**interaction.get_properties()[0].get_values())")]
        public Dictionary<string, object> get_values()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            if (_property is Friction friction)
            {
                values["coefficient"] = friction.Coefficient;
                values["stick_slope"] = ToValue(friction.StickSlope);
            }
            else if (_property is GapConductance gapConductance)
            {
                if (gapConductance.GapConductanceType == GapConductanceEnum.Constant)
                    values["conductance"] = gapConductance.ConductnancePressureTemp[0][0];
                else values["conductance_table"] = ToTable(gapConductance.ConductnancePressureTemp);
            }
            else if (_property is SurfaceBehavior surfaceBehavior)
            {
                // Only the values the type uses are reported, because pmx.create refuses the others
                PressureOverclosureEnum type = surfaceBehavior.PressureOverclosureType;
                values["pressure_overclosure_type"] = type.ToString();
                if (SurfaceInteractionPropertyFactory.UsesValue(type, "c0")) values["c0"] = ToValue(surfaceBehavior.C0);
                if (SurfaceInteractionPropertyFactory.UsesValue(type, "p0")) values["p0"] = surfaceBehavior.P0;
                if (SurfaceInteractionPropertyFactory.UsesValue(type, "k")) values["k"] = surfaceBehavior.K;
                if (SurfaceInteractionPropertyFactory.UsesValue(type, "s_inf")) values["s_inf"] = surfaceBehavior.Sinf;
                if (SurfaceInteractionPropertyFactory.UsesValue(type, "pressure_overclosure"))
                    values["pressure_overclosure"] = surfaceBehavior.PressureOverclosure == null ? null :
                                                     ToTable(surfaceBehavior.PressureOverclosure);
            }
            else GetPropertyType(_property);
            //
            return values;
        }

        [PythonAPI("Returns the names get_values reports for this property.", Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            return new StringListAPI(get_values().Keys);
        }

        [PythonAPI("Returns one value of the property by the name of the pmx.create argument that sets it. Use " +
                   "get_value_names to list them.", Arguments = "name: str",
                   Returns = "float, str, NumericArray or None")]
        public object get_value(string name)
        {
            if (name == null) throw new PythonAPIException("The argument 'name' must not be empty.");
            //
            Dictionary<string, object> values = get_values();
            object value;
            if (!values.TryGetValue(name.Trim(), out value))
                throw new PythonAPIException("The value '" + name + "' is not defined for a " +
                                             GetPropertyType(_property) + " surface interaction property. " +
                                             "Possible values are " + string.Join(", ", values.Keys) + ".");
            //
            return value;
        }
        internal SurfaceInteractionProperty GetPropertyCopy()
        {
            return _property.DeepClone();
        }
        public override string ToString()
        {
            return "SurfaceInteractionProperty(type=" + GetPropertyType(_property) +
                   string.Concat(get_values().Select(entry => ", " + entry.Key + "=" + FormatValue(entry.Value))) +
                   ")";
        }
    }
}
