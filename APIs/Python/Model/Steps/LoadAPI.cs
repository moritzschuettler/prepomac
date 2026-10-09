// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;
using System;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class LoadAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> ItemTypes = new Dictionary<Type, string>
        {
            { typeof(CLoad), "concentrated_force" },
            { typeof(MomentLoad), "moment" },
            { typeof(DLoad), "pressure" },
            { typeof(HydrostaticPressure), "hydrostatic_pressure" },
            { typeof(ImportedPressure), "imported_pressure" },
            { typeof(STLoad), "surface_traction" },
            { typeof(ShellEdgeLoad), "shell_edge_load" },
            { typeof(GravityLoad), "gravity" },
            { typeof(CentrifLoad), "centrifugal" },
            { typeof(PreTensionLoad), "pre_tension" },
            { typeof(CFlux), "concentrated_flux" },
            { typeof(DFlux), "surface_flux" },
            { typeof(BodyFlux), "body_flux" },
            { typeof(FilmHeatTransfer), "film_heat_transfer" },
            { typeof(RadiationHeatTransfer), "radiation_heat_transfer" }
        };
        private readonly string _stepName;
        private readonly string _loadName;
        private readonly LoadService _service;


        // Properties
        [PythonAPI("The load name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The name of the step the load belongs to.", Returns = "str")]
        public string step_name { get { return _stepName; } }
        [PythonAPI("The load type, named after the add_* method that makes it without the add_ prefix, for " +
                   "example concentrated_force or pressure.", Returns = "str")]
        public string item_type { get { return GetItemType(Resolve()); } }
        [PythonAPI("True when the load is written to the solver input file.", Returns = "bool")]
        public bool active { get { return Resolve().Active; } }
        [PythonAPI("True when the load symbols are drawn.", Returns = "bool")]
        public bool visible { get { return Resolve().Visible; } }
        [PythonAPI("The region type, for example NodeSetName or SurfaceName.", Returns = "str")]
        public string region_type { get { return Resolve().RegionType.ToString(); } }
        [PythonAPI("The name of the region the load is applied to.", Returns = "str")]
        public string region_name { get { return Resolve().RegionName; } }
        [PythonAPI("The amplitude name.", Returns = "str")]
        public string amplitude_name { get { return Resolve().AmplitudeName; } }
        [PythonAPI("The coordinate system name.", Returns = "str")]
        public string coordinate_system_name { get { return Resolve().CoordinateSystemName; } }
        [PythonAPI("The symbol color as a list [R, G, B, A] of channel values, which the color argument takes " +
                   "back.", Returns = "list[int]")]
        public object color { get { return PythonColorConverter.ToChannels(Resolve().Color); } }


        // Constructors
        internal LoadAPI(string stepName, string loadName, LoadService service)
        {
            _stepName = stepName;
            _loadName = loadName;
            _service = service;
        }


        // Static methods
        internal static string GetItemType(Load load)
        {
            string itemType;
            if (ItemTypes.TryGetValue(load.GetType(), out itemType)) return itemType;
            //
            throw new PythonAPIException("The load '" + load.Name + "' is a " + load.GetType().Name +
                                         ", which no add_* method of the Python API makes.");
        }
        private static Dictionary<string, string[]> GetArgumentValueNames(Load load)
        {
            Dictionary<string, string[]> names = new Dictionary<string, string[]>();
            if (load is HydrostaticPressure)
            {
                names.Add("first_point", new string[] { "x1", "y1", "z1" });
                names.Add("second_point", new string[] { "x2", "y2", "z2" });
                names.Add("direction", new string[] { "n1", "n2", "n3" });
                names.Add("cutoff", new string[] { "hydrostatic_pressure_cutoff" });
            }
            else if (load is CentrifLoad)
            {
                names.Add("axis_point", new string[] { "x", "y", "z" });
                names.Add("axis_direction", new string[] { "n1", "n2", "n3" });
            }
            else if (load is PreTensionLoad)
            {
                // The magnitude is the force or the displacement, whichever the pre-tension type uses
                bool isDisplacement = ((PreTensionLoad)load).Type == PreTensionLoadType.Displacement;
                names.Add("magnitude", new string[] { isDisplacement ? "displacement_magnitude" : "force_magnitude" });
                names.Add("direction", new string[] { "x", "y", "z" });
                names.Add("tension_type", new string[] { "type" });
                names.Add("auto_direction", new string[] { "auto_compute_direction" });
            }
            //
            return names;
        }
        // The phase is stored on every load, so the shared reader leaves it out; it is an add_* argument of each
        private static Dictionary<string, object> GetValues(Load load)
        {
            Dictionary<string, object> values = ModelItemValueReader.GetValues(load, typeof(Load),
                                                                               GetArgumentValueNames(load));
            values["phase_deg"] = load.PhaseDeg.Value;
            //
            return values;
        }


        // Methods
        [PythonAPI("Returns the names of the values that are specific to this load type: the add_* argument " +
                   "names, followed by the stored values no argument sets.", Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            return new StringListAPI(GetValues(Resolve()).Keys);
        }

        [PythonAPI("Returns a value that is specific to this load type by the name of the add_* argument that " +
                   "sets it, for example f1, magnitude or phase_deg; a vector argument such as axis_point " +
                   "returns its components as a list. Use get_value_names to list them.", Arguments = "name: str",
                   Returns = "float, int, bool, str or list[float]")]
        public object get_value(string name)
        {
            Load load = Resolve();
            return ModelItemValueReader.GetValue(GetValues(load), name, GetItemType(load) + " load");
        }
        [PythonAPI("Returns the expressions the load was entered with, without evaluating them, under the names " +
                   "get_value uses. Only a value entered as an expression is returned, with its leading equals " +
                   "sign; a value entered as a plain number is left out, and get_value returns it as a number. A " +
                   "vector argument such as axis_point with an expression in any component is returned as a " +
                   "list, its plain components as numbers.", Returns = "dict[str, str or list]")]
        public Dictionary<string, object> get_equations()
        {
            Load load = Resolve();
            return ModelItemValueReader.GetEquations(load, GetArgumentValueNames(load));
        }
        public override string ToString()
        {
            Load load = Resolve();
            return "Load(name=" + StringListAPI.ToDisplayString(load.Name) + ", type=" + GetItemType(load) +
                   ", step=" + StringListAPI.ToDisplayString(_stepName) + ")";
        }
        //
        private Load Resolve()
        {
            return _service.ResolveItem(_stepName, _loadName);
        }
    }
}
