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
        // The arguments of the replace_* method of the load type, each with the stored values it sets. A value an
        // argument refuses in the state the load is in - the unused group of a surface traction, the direction of
        // a pre-tension along the surface normal, the cavity name of a radiation without cavity radiation, the
        // phase of a load that is not complex - is not read back, so a read-back always goes back
        private static Dictionary<string, string[]> GetArgumentValueNames(Load load)
        {
            Dictionary<string, string[]> names = new Dictionary<string, string[]>();
            if (load is CLoad || load is GravityLoad) ModelItemValueReader.AddArguments(names, "f1", "f2", "f3");
            else if (load is MomentLoad) ModelItemValueReader.AddArguments(names, "m1", "m2", "m3");
            else if (load is DLoad) ModelItemValueReader.AddArguments(names, "magnitude", "distribution_name");
            else if (load is HydrostaticPressure)
            {
                names.Add("first_point", new string[] { "x1", "y1", "z1" });
                names.Add("second_point", new string[] { "x2", "y2", "z2" });
                names.Add("direction", new string[] { "n1", "n2", "n3" });
                ModelItemValueReader.AddArguments(names, "first_point_pressure", "second_point_pressure");
                names.Add("cutoff", new string[] { "hydrostatic_pressure_cutoff" });
            }
            else if (load is ImportedPressure)
                ModelItemValueReader.AddArguments(names, "file_name", "pressure_time", "pressure_variable_name",
                                                  "interpolator_type", "magnitude_factor", "geometry_scale_factor");
            else if (load is STLoad)
            {
                // The model writes the force without a distribution and the force per area with one
                if (PythonNameComparer.AreSame(((STLoad)load).DistributionName,
                                               Distribution.DefaultDistributionName))
                    ModelItemValueReader.AddArguments(names, "f1", "f2", "f3");
                else ModelItemValueReader.AddArguments(names, "p1", "p2", "p3");
                ModelItemValueReader.AddArguments(names, "distribution_name");
            }
            else if (load is CentrifLoad)
            {
                names.Add("axis_point", new string[] { "x", "y", "z" });
                names.Add("axis_direction", new string[] { "n1", "n2", "n3" });
                ModelItemValueReader.AddArguments(names, "rotational_speed");
            }
            else if (load is PreTensionLoad)
            {
                PreTensionLoad preTension = (PreTensionLoad)load;
                // The magnitude is the force or the displacement, whichever the pre-tension type uses
                bool isDisplacement = preTension.Type == PreTensionLoadType.Displacement;
                names.Add("magnitude", new string[] { isDisplacement ? "displacement_magnitude" : "force_magnitude" });
                if (!preTension.AutoComputeDirection) names.Add("direction", new string[] { "x", "y", "z" });
                names.Add("tension_type", new string[] { "type" });
                names.Add("auto_direction", new string[] { "auto_compute_direction" });
            }
            else if (load is CFlux) ModelItemValueReader.AddArguments(names, "magnitude", "add_flux");
            else if (load is ShellEdgeLoad || load is DFlux || load is BodyFlux)
                ModelItemValueReader.AddArguments(names, "magnitude");
            else if (load is FilmHeatTransfer)
                ModelItemValueReader.AddArguments(names, "sink_temperature", "film_coefficient",
                                                  "coefficient_amplitude_name");
            else if (load is RadiationHeatTransfer)
            {
                ModelItemValueReader.AddArguments(names, "sink_temperature", "emissivity", "cavity_radiation");
                if (((RadiationHeatTransfer)load).CavityRadiation)
                    ModelItemValueReader.AddArguments(names, "cavity_name");
                ModelItemValueReader.AddArguments(names, "emissivity_amplitude_name");
            }
            if (TakesPhase(load)) ModelItemValueReader.AddArguments(names, "phase_deg");
            //
            return names;
        }
        // A phase is written to the solver input only for a complex load, which a steady state dynamics step makes,
        // and never for a pre-tension; a thermal load is never complex
        internal static bool TakesPhase(Load load)
        {
            return load.Complex && !(load is PreTensionLoad);
        }
        // The solver input transforms only the nodes of a concentrated force and of a surface traction into a
        // coordinate system, and the dialog offers one for those two only
        internal static bool TakesCoordinateSystem(Load load)
        {
            return load is CLoad || load is STLoad;
        }
        private static Dictionary<string, object> GetValues(Load load)
        {
            return ModelItemValueReader.GetValues(load, GetArgumentValueNames(load));
        }


        // Methods
        [PythonAPI("Returns the names of the values that are specific to this load type: the names of the " +
                   "replace_* arguments that set them, and nothing else, so the values go back into the " +
                   "replace_* method of the load.", Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            return new StringListAPI(GetValues(Resolve()).Keys);
        }

        [PythonAPI("Returns a value that is specific to this load type by the name of the add_* argument that " +
                   "sets it, for example f1 or magnitude; a vector argument such as axis_point returns its " +
                   "components as a list. A value the argument would refuse in the state the load is in is not " +
                   "read back: phase_deg only for a complex load of a steady_state_dynamics step, a surface " +
                   "traction's f1 to f3 only without a distribution and p1 to p3 only with one, a pre-tension's " +
                   "direction only when auto_direction is False, and cavity_name only with cavity_radiation. " +
                   "Use get_value_names to list them.",
                   Arguments = "name: str",
                   Returns = "float, int, bool, str or list[float]")]
        public object get_value(object name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            Load load = Resolve();
            return ModelItemValueReader.GetValue(GetValues(load), nameText, GetItemType(load) + " load");
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
