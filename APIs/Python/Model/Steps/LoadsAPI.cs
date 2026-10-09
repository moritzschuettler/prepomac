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

namespace APIs.Python.Model
{
    // The region is a selection created by any pmx.create.selection_* function, or a named region created
    // by pmx.create.region. Every add method takes the values of its own load type first and the values
    // shared by all load types last. A value that is not given keeps the model default, and on a
    // replace it keeps the value the load already carries.
    public class LoadsAPI : StepItemsAPI<Load, LoadAPI>
    {
        // Variables
        private const string NodeTarget = "nodes";
        private const string SurfaceTarget = "surfaces";
        private const string ElementTarget = "elements";
        private readonly LoadService _service;


        // Constructors
        internal LoadsAPI(string stepName, LoadService service)
            : base("load", stepName)
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds a concentrated force using command history. A two dimensional model refuses a nonzero " +
                   "f3. The phase_deg applies only in a steady_state_dynamics step, where a load is complex, " +
                   "and is refused elsewhere. The selection target is nodes.",
                   Arguments = "name: str, region: Selection or Region, f1: float = None, " +
                               "f2: float = None, f3: float = None, amplitude_name: str = None, " +
                               "coordinate_system_name: str = None, phase_deg: float = None, " +
                               "color: list[int] or str = None", Returns = "Load",
                   Example = "step.loads.add_concentrated_force('Force-1', selection, f3=-1000)")]
        public LoadAPI add_concentrated_force(string name, object region, object f1 = null, object f2 = null,
                                              object f3 = null, object amplitude_name = null,
                                              object coordinate_system_name = null, object phase_deg = null,
                                              object color = null)
        {
            _service.ValidateName(StepName, name, null);
            CLoad load = new CLoad(name, null, RegionTypeEnum.Selection, 0, 0, 0, TwoD(), Complex(), 0);
            _service.ApplyRegion(load, region, NodeTarget);
            LoadFactory.ApplyConcentratedForce(load, f1, f2, f3);
            //
            return Add(load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Replaces an existing concentrated force using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_concentrated_force", Returns = "Load")]
        public LoadAPI replace_concentrated_force(string old_name, string name = null, object region = null,
                                                  object f1 = null, object f2 = null, object f3 = null,
                                                  object amplitude_name = null,
                                                  object coordinate_system_name = null, object phase_deg = null,
                                                  object color = null)
        {
            CLoad load = _service.CloneForReplace<CLoad>(StepName, old_name, "concentrated_force");
            ApplyRegionIfGiven(load, region, NodeTarget);
            LoadFactory.ApplyConcentratedForce(load, f1, f2, f3);
            //
            return Replace(old_name, name, load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Adds a moment using command history. A two dimensional model refuses a nonzero m1 or m2. " +
                   "The selection target is nodes.",
                   Arguments = "name: str, region: Selection or Region, m1: float = None, " +
                               "m2: float = None, m3: float = None, and the shared load arguments",
                   Returns = "Load")]
        public LoadAPI add_moment(string name, object region, object m1 = null, object m2 = null,
                                  object m3 = null, object amplitude_name = null,
                                  object coordinate_system_name = null, object phase_deg = null,
                                  object color = null)
        {
            _service.ValidateName(StepName, name, null);
            MomentLoad load = new MomentLoad(name, null, RegionTypeEnum.Selection, 0, 0, 0, TwoD(), Complex(), 0);
            _service.ApplyRegion(load, region, NodeTarget);
            LoadFactory.ApplyMoment(load, m1, m2, m3);
            //
            return Add(load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Replaces an existing moment using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_moment",
                   Returns = "Load")]
        public LoadAPI replace_moment(string old_name, string name = null, object region = null, object m1 = null,
                                      object m2 = null, object m3 = null, object amplitude_name = null,
                                      object coordinate_system_name = null, object phase_deg = null,
                                      object color = null)
        {
            MomentLoad load = _service.CloneForReplace<MomentLoad>(StepName, old_name, "moment");
            ApplyRegionIfGiven(load, region, NodeTarget);
            LoadFactory.ApplyMoment(load, m1, m2, m3);
            //
            return Replace(old_name, name, load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Adds a pressure using command history. The selection target is surfaces.",
                   Arguments = "name: str, region: Selection or Region, magnitude: float, " +
                               "distribution_name: str = None, and the shared load arguments",
                   Returns = "Load", Example = "step.loads.add_pressure('Pressure-1', selection, 2.5)")]
        public LoadAPI add_pressure(string name, object region, object magnitude,
                                    object distribution_name = null, object amplitude_name = null,
                                    object coordinate_system_name = null, object phase_deg = null,
                                    object color = null)
        {
            _service.ValidateName(StepName, name, null);
            DLoad load = new DLoad(name, null, RegionTypeEnum.Selection, 0, TwoD(), Complex(), 0);
            _service.ApplyRegion(load, region, SurfaceTarget);
            LoadFactory.ApplyPressure(load, magnitude);
            if (distribution_name != null)
                load.DistributionName = _service.ResolveScalarDistributionName(distribution_name, "a pressure");
            //
            return Add(load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Replaces an existing pressure using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_pressure with " +
                               "magnitude optional", Returns = "Load")]
        public LoadAPI replace_pressure(string old_name, string name = null, object region = null,
                                        object magnitude = null, object distribution_name = null,
                                        object amplitude_name = null, object coordinate_system_name = null,
                                        object phase_deg = null, object color = null)
        {
            DLoad load = _service.CloneForReplace<DLoad>(StepName, old_name, "pressure");
            ApplyRegionIfGiven(load, region, SurfaceTarget);
            LoadFactory.ApplyPressure(load, magnitude);
            if (distribution_name != null)
                load.DistributionName = _service.ResolveScalarDistributionName(distribution_name, "a pressure");
            //
            return Replace(old_name, name, load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Adds a hydrostatic pressure using command history. The pressure varies linearly along " +
                   "the given direction between the two points. Possible cutoff values are None, Positive " +
                   "and Negative. A two dimensional model refuses a nonzero third component of a point or " +
                   "the direction. The selection target is surfaces.",
                   Arguments = "name: str, region: Selection or Region, first_point: list[float] = None, " +
                               "second_point: list[float] = None, direction: list[float] = None, " +
                               "first_point_pressure: float = None, second_point_pressure: float = None, " +
                               "cutoff: str = None, and the shared load arguments", Returns = "Load")]
        public LoadAPI add_hydrostatic_pressure(string name, object region, object first_point = null,
                                                object second_point = null, object direction = null,
                                                object first_point_pressure = null,
                                                object second_point_pressure = null, object cutoff = null,
                                                object amplitude_name = null,
                                                object coordinate_system_name = null, object phase_deg = null,
                                                object color = null)
        {
            _service.ValidateName(StepName, name, null);
            HydrostaticPressure load =
                new HydrostaticPressure(name, null, RegionTypeEnum.Selection, TwoD(), Complex(), 0);
            _service.ApplyRegion(load, region, SurfaceTarget);
            LoadFactory.ApplyHydrostaticPressure(load, first_point, second_point, direction,
                                                 first_point_pressure, second_point_pressure, cutoff);
            //
            return Add(load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Replaces an existing hydrostatic pressure using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_hydrostatic_pressure", Returns = "Load")]
        public LoadAPI replace_hydrostatic_pressure(string old_name, string name = null, object region = null,
                                                    object first_point = null, object second_point = null,
                                                    object direction = null, object first_point_pressure = null,
                                                    object second_point_pressure = null, object cutoff = null,
                                                    object amplitude_name = null,
                                                    object coordinate_system_name = null,
                                                    object phase_deg = null, object color = null)
        {
            HydrostaticPressure load =
                _service.CloneForReplace<HydrostaticPressure>(StepName, old_name, "hydrostatic_pressure");
            ApplyRegionIfGiven(load, region, SurfaceTarget);
            LoadFactory.ApplyHydrostaticPressure(load, first_point, second_point, direction,
                                                 first_point_pressure, second_point_pressure, cutoff);
            //
            return Replace(old_name, name, load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Adds an imported pressure using command history. The pressure is read from a results " +
                   "file and interpolated onto the surface, so the time and the variable name must name a " +
                   "field that the file contains: the model reads the file on every model check. The " +
                   "selection target is surfaces.",
                   Arguments = "name: str, region: Selection or Region, file_name: str, " +
                               "pressure_time: str, pressure_variable_name: str, " +
                               "interpolator_type: str = None, magnitude_factor: float = None, " +
                               "geometry_scale_factor: float = None, and the shared load arguments",
                   Returns = "Load")]
        public LoadAPI add_imported_pressure(string name, object region, object file_name, object pressure_time,
                                             object pressure_variable_name,
                                             object interpolator_type = null, object magnitude_factor = null,
                                             object geometry_scale_factor = null, object amplitude_name = null,
                                             object coordinate_system_name = null, object phase_deg = null,
                                             object color = null)
        {
            _service.ValidateName(StepName, name, null);
            ImportedPressure load =
                new ImportedPressure(name, null, RegionTypeEnum.Selection, TwoD(), Complex(), 0);
            _service.ApplyRegion(load, region, SurfaceTarget);
            load.FileName = _service.ResolveExistingFileName(file_name, "file_name");
            LoadFactory.ApplyImportedPressure(load, pressure_time, pressure_variable_name, interpolator_type,
                                              magnitude_factor, geometry_scale_factor);
            //
            return Add(load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Replaces an existing imported pressure using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_imported_pressure with file_name optional", Returns = "Load")]
        public LoadAPI replace_imported_pressure(string old_name, string name = null, object region = null,
                                                 object file_name = null, object pressure_time = null,
                                                 object pressure_variable_name = null,
                                                 object interpolator_type = null, object magnitude_factor = null,
                                                 object geometry_scale_factor = null,
                                                 object amplitude_name = null,
                                                 object coordinate_system_name = null, object phase_deg = null,
                                                 object color = null)
        {
            ImportedPressure load =
                _service.CloneForReplace<ImportedPressure>(StepName, old_name, "imported_pressure");
            ApplyRegionIfGiven(load, region, SurfaceTarget);
            if (file_name != null) load.FileName = _service.ResolveExistingFileName(file_name, "file_name");
            LoadFactory.ApplyImportedPressure(load, pressure_time, pressure_variable_name, interpolator_type,
                                              magnitude_factor, geometry_scale_factor);
            //
            return Replace(old_name, name, load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Adds a surface traction using command history. Give the force components f1 to f3 " +
                   "without a distribution, or the force per area components p1 to p3 together with a " +
                   "distribution; the model writes only the group that matches the distribution, so the other " +
                   "group is refused, on a replace for the distribution the load carries after the call. A two " +
                   "dimensional model refuses a nonzero f3 or p3. The selection target is surfaces.",
                   Arguments = "name: str, region: Selection or Region, f1: float = None, f2: float = None, " +
                               "f3: float = None, p1: float = None, p2: float = None, p3: float = None, " +
                               "distribution_name: str = None, and the shared load arguments",
                   Returns = "Load")]
        public LoadAPI add_surface_traction(string name, object region, object f1 = null, object f2 = null,
                                            object f3 = null, object p1 = null, object p2 = null,
                                            object p3 = null, object distribution_name = null,
                                            object amplitude_name = null,
                                            object coordinate_system_name = null, object phase_deg = null,
                                            object color = null)
        {
            _service.ValidateName(StepName, name, null);
            STLoad load = new STLoad(name, null, RegionTypeEnum.Selection, 0, 0, 0, TwoD(), Complex(), 0);
            _service.ApplyRegion(load, region, SurfaceTarget);
            if (distribution_name != null)
                load.DistributionName = _service.ResolveDistributionName(distribution_name);
            LoadFactory.ApplySurfaceTraction(load, f1, f2, f3, p1, p2, p3);
            //
            return Add(load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Replaces an existing surface traction using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_surface_traction",
                   Returns = "Load")]
        public LoadAPI replace_surface_traction(string old_name, string name = null, object region = null,
                                                object f1 = null, object f2 = null, object f3 = null,
                                                object p1 = null, object p2 = null, object p3 = null,
                                                object distribution_name = null, object amplitude_name = null,
                                                object coordinate_system_name = null, object phase_deg = null,
                                                object color = null)
        {
            STLoad load = _service.CloneForReplace<STLoad>(StepName, old_name, "surface_traction");
            ApplyRegionIfGiven(load, region, SurfaceTarget);
            if (distribution_name != null)
                load.DistributionName = _service.ResolveDistributionName(distribution_name);
            LoadFactory.ApplySurfaceTraction(load, f1, f2, f3, p1, p2, p3);
            //
            return Replace(old_name, name, load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Adds a shell edge load using command history. The selection target is surfaces.",
                   Arguments = "name: str, region: Selection or Region, magnitude: float, and the shared " +
                               "load arguments", Returns = "Load")]
        public LoadAPI add_shell_edge_load(string name, object region, object magnitude,
                                           object amplitude_name = null,
                                           object coordinate_system_name = null, object phase_deg = null,
                                           object color = null)
        {
            _service.ValidateName(StepName, name, null);
            ShellEdgeLoad load = new ShellEdgeLoad(name, null, RegionTypeEnum.Selection, 0, TwoD(), Complex(), 0);
            _service.ApplyRegion(load, region, SurfaceTarget);
            LoadFactory.ApplyShellEdgeLoad(load, magnitude);
            //
            return Add(load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Replaces an existing shell edge load using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_shell_edge_load " +
                               "with magnitude optional", Returns = "Load")]
        public LoadAPI replace_shell_edge_load(string old_name, string name = null, object region = null,
                                               object magnitude = null, object amplitude_name = null,
                                               object coordinate_system_name = null, object phase_deg = null,
                                               object color = null)
        {
            ShellEdgeLoad load = _service.CloneForReplace<ShellEdgeLoad>(StepName, old_name, "shell_edge_load");
            ApplyRegionIfGiven(load, region, SurfaceTarget);
            LoadFactory.ApplyShellEdgeLoad(load, magnitude);
            //
            return Replace(old_name, name, load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Adds a gravity load using command history. A two dimensional model refuses a nonzero f3. " +
                   "The selection target is elements.",
                   Arguments = "name: str, region: Selection or Region, f1: float = None, f2: float = None, " +
                               "f3: float = None, and the shared load arguments", Returns = "Load",
                   Example = "step.loads.add_gravity('Gravity-1', part_selection, f3=-9810)")]
        public LoadAPI add_gravity(string name, object region, object f1 = null, object f2 = null,
                                   object f3 = null, object amplitude_name = null,
                                   object coordinate_system_name = null, object phase_deg = null,
                                   object color = null)
        {
            _service.ValidateName(StepName, name, null);
            GravityLoad load = new GravityLoad(name, null, RegionTypeEnum.Selection, TwoD(), Complex(), 0);
            _service.ApplyRegion(load, region, ElementTarget);
            LoadFactory.ApplyGravity(load, f1, f2, f3);
            //
            return Add(load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Replaces an existing gravity load using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_gravity",
                   Returns = "Load")]
        public LoadAPI replace_gravity(string old_name, string name = null, object region = null,
                                       object f1 = null, object f2 = null, object f3 = null,
                                       object amplitude_name = null, object coordinate_system_name = null,
                                       object phase_deg = null, object color = null)
        {
            GravityLoad load = _service.CloneForReplace<GravityLoad>(StepName, old_name, "gravity");
            ApplyRegionIfGiven(load, region, ElementTarget);
            LoadFactory.ApplyGravity(load, f1, f2, f3);
            //
            return Replace(old_name, name, load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Adds a centrifugal load using command history. The axis is given by a point and a " +
                   "direction, and the rotational_speed is in rad/s and must not be negative. An axisymmetric " +
                   "model turns about its y axis, so there axis_point must be [0, 0, 0] and axis_direction " +
                   "[0, 1, 0] when given. The selection target is elements.",
                   Arguments = "name: str, region: Selection or Region, axis_point: list[float] = None, " +
                               "axis_direction: list[float] = None, rotational_speed: float = None, and the " +
                               "shared load arguments", Returns = "Load")]
        public LoadAPI add_centrifugal(string name, object region, object axis_point = null,
                                       object axis_direction = null, object rotational_speed = null,
                                       object amplitude_name = null, object coordinate_system_name = null,
                                       object phase_deg = null, object color = null)
        {
            _service.ValidateName(StepName, name, null);
            // The dialog derives the axisymmetric flag from the model space
            CentrifLoad load = new CentrifLoad(name, null, RegionTypeEnum.Selection, TwoD(),
                                               _service.GetIsAxisymmetric(), Complex(), 0);
            _service.ApplyRegion(load, region, ElementTarget);
            LoadFactory.ApplyCentrifugal(load, axis_point, axis_direction, rotational_speed);
            //
            return Add(load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Replaces an existing centrifugal load using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_centrifugal",
                   Returns = "Load")]
        public LoadAPI replace_centrifugal(string old_name, string name = null, object region = null,
                                           object axis_point = null, object axis_direction = null,
                                           object rotational_speed = null, object amplitude_name = null,
                                           object coordinate_system_name = null, object phase_deg = null,
                                           object color = null)
        {
            CentrifLoad load = _service.CloneForReplace<CentrifLoad>(StepName, old_name, "centrifugal");
            ApplyRegionIfGiven(load, region, ElementTarget);
            LoadFactory.ApplyCentrifugal(load, axis_point, axis_direction, rotational_speed);
            //
            return Replace(old_name, name, load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Adds a pre-tension load using command history. The direction is the surface normal " +
                   "unless one is given. Possible tension_type values are Force and Displacement. It needs a " +
                   "three dimensional model, and as it is never complex it refuses phase_deg. The selection " +
                   "target is surfaces.",
                   Arguments = "name: str, region: Selection or Region, magnitude: float, " +
                               "direction: list[float] = None, tension_type: str = None, and the shared load " +
                               "arguments", Returns = "Load")]
        public LoadAPI add_pre_tension(string name, object region, object magnitude, object direction = null,
                                       object tension_type = null, object amplitude_name = null,
                                       object coordinate_system_name = null, object phase_deg = null,
                                       object color = null)
        {
            _service.ValidateName(StepName, name, null);
            PreTensionLoad load = new PreTensionLoad(name, null, RegionTypeEnum.Selection, 0, TwoD());
            _service.ApplyRegion(load, region, SurfaceTarget);
            LoadFactory.ApplyPreTension(load, magnitude, direction, tension_type, null);
            //
            return Add(load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Replaces an existing pre-tension load using command history, keeping every value that is " +
                   "not given. A tension_type that changes the type needs the magnitude as well, and " +
                   "auto_direction=True returns to the surface normal.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_pre_tension with " +
                               "magnitude optional, and auto_direction: bool = None", Returns = "Load")]
        public LoadAPI replace_pre_tension(string old_name, string name = null, object region = null,
                                           object magnitude = null, object direction = null,
                                           object tension_type = null, object amplitude_name = null,
                                           object coordinate_system_name = null, object phase_deg = null,
                                           object color = null, object auto_direction = null)
        {
            PreTensionLoad load = _service.CloneForReplace<PreTensionLoad>(StepName, old_name, "pre_tension");
            ApplyRegionIfGiven(load, region, SurfaceTarget);
            LoadFactory.ApplyPreTension(load, magnitude, direction, tension_type, auto_direction);
            //
            return Replace(old_name, name, load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Adds a concentrated flux using command history. A thermal load is never complex, so it " +
                   "refuses phase_deg. The selection target is nodes.",
                   Arguments = "name: str, region: Selection or Region, magnitude: float, " +
                               "add_flux: bool = None, and the shared load arguments", Returns = "Load")]
        public LoadAPI add_concentrated_flux(string name, object region, object magnitude,
                                             object add_flux = null, object amplitude_name = null,
                                             object coordinate_system_name = null, object phase_deg = null,
                                             object color = null)
        {
            _service.ValidateName(StepName, name, null);
            CFlux load = new CFlux(name, null, RegionTypeEnum.Selection, 0, TwoD());
            _service.ApplyRegion(load, region, NodeTarget);
            LoadFactory.ApplyConcentratedFlux(load, magnitude, add_flux);
            //
            return Add(load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Replaces an existing concentrated flux using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_concentrated_flux with magnitude optional", Returns = "Load")]
        public LoadAPI replace_concentrated_flux(string old_name, string name = null, object region = null,
                                                 object magnitude = null, object add_flux = null,
                                                 object amplitude_name = null,
                                                 object coordinate_system_name = null, object phase_deg = null,
                                                 object color = null)
        {
            CFlux load = _service.CloneForReplace<CFlux>(StepName, old_name, "concentrated_flux");
            ApplyRegionIfGiven(load, region, NodeTarget);
            LoadFactory.ApplyConcentratedFlux(load, magnitude, add_flux);
            //
            return Replace(old_name, name, load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Adds a surface flux using command history. The selection target is surfaces.",
                   Arguments = "name: str, region: Selection or Region, magnitude: float, and the shared " +
                               "load arguments", Returns = "Load")]
        public LoadAPI add_surface_flux(string name, object region, object magnitude,
                                        object amplitude_name = null, object coordinate_system_name = null,
                                        object phase_deg = null, object color = null)
        {
            _service.ValidateName(StepName, name, null);
            DFlux load = new DFlux(name, null, RegionTypeEnum.Selection, 0, TwoD());
            _service.ApplyRegion(load, region, SurfaceTarget);
            LoadFactory.ApplySurfaceFlux(load, magnitude);
            //
            return Add(load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Replaces an existing surface flux using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_surface_flux with " +
                               "magnitude optional", Returns = "Load")]
        public LoadAPI replace_surface_flux(string old_name, string name = null, object region = null,
                                            object magnitude = null, object amplitude_name = null,
                                            object coordinate_system_name = null, object phase_deg = null,
                                            object color = null)
        {
            DFlux load = _service.CloneForReplace<DFlux>(StepName, old_name, "surface_flux");
            ApplyRegionIfGiven(load, region, SurfaceTarget);
            LoadFactory.ApplySurfaceFlux(load, magnitude);
            //
            return Replace(old_name, name, load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Adds a body flux using command history. The selection target is elements.",
                   Arguments = "name: str, region: Selection or Region, magnitude: float, and the shared " +
                               "load arguments", Returns = "Load")]
        public LoadAPI add_body_flux(string name, object region, object magnitude, object amplitude_name = null,
                                     object coordinate_system_name = null, object phase_deg = null,
                                     object color = null)
        {
            _service.ValidateName(StepName, name, null);
            BodyFlux load = new BodyFlux(name, null, RegionTypeEnum.Selection, 0, TwoD());
            _service.ApplyRegion(load, region, ElementTarget);
            LoadFactory.ApplyBodyFlux(load, magnitude);
            //
            return Add(load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Replaces an existing body flux using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_body_flux with " +
                               "magnitude optional", Returns = "Load")]
        public LoadAPI replace_body_flux(string old_name, string name = null, object region = null,
                                         object magnitude = null, object amplitude_name = null,
                                         object coordinate_system_name = null, object phase_deg = null,
                                         object color = null)
        {
            BodyFlux load = _service.CloneForReplace<BodyFlux>(StepName, old_name, "body_flux");
            ApplyRegionIfGiven(load, region, ElementTarget);
            LoadFactory.ApplyBodyFlux(load, magnitude);
            //
            return Replace(old_name, name, load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Adds a film heat transfer load using command history. The film coefficient can carry " +
                   "its own amplitude. The selection target is surfaces.",
                   Arguments = "name: str, region: Selection or Region, sink_temperature: float, " +
                               "film_coefficient: float, coefficient_amplitude_name: str = None, and the " +
                               "shared load arguments", Returns = "Load")]
        public LoadAPI add_film_heat_transfer(string name, object region, object sink_temperature,
                                              object film_coefficient,
                                              object coefficient_amplitude_name = null,
                                              object amplitude_name = null,
                                              object coordinate_system_name = null, object phase_deg = null,
                                              object color = null)
        {
            _service.ValidateName(StepName, name, null);
            FilmHeatTransfer load = new FilmHeatTransfer(name, null, RegionTypeEnum.Selection, 0, 0, TwoD());
            _service.ApplyRegion(load, region, SurfaceTarget);
            LoadFactory.ApplyFilmHeatTransfer(load, sink_temperature, film_coefficient);
            if (coefficient_amplitude_name != null)
                load.CoefficientAmplitudeName =
                    _service.ResolveAmplitudeName(coefficient_amplitude_name, "coefficient_amplitude_name");
            //
            return Add(load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Replaces an existing film heat transfer load using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_film_heat_transfer with the values optional", Returns = "Load")]
        public LoadAPI replace_film_heat_transfer(string old_name, string name = null, object region = null,
                                                  object sink_temperature = null, object film_coefficient = null,
                                                  object coefficient_amplitude_name = null,
                                                  object amplitude_name = null,
                                                  object coordinate_system_name = null, object phase_deg = null,
                                                  object color = null)
        {
            FilmHeatTransfer load =
                _service.CloneForReplace<FilmHeatTransfer>(StepName, old_name, "film_heat_transfer");
            ApplyRegionIfGiven(load, region, SurfaceTarget);
            LoadFactory.ApplyFilmHeatTransfer(load, sink_temperature, film_coefficient);
            if (coefficient_amplitude_name != null)
                load.CoefficientAmplitudeName =
                    _service.ResolveAmplitudeName(coefficient_amplitude_name, "coefficient_amplitude_name");
            //
            return Replace(old_name, name, load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Adds a radiation heat transfer load using command history. The emissivity is between 0 " +
                   "and 1 and can carry its own amplitude, and a cavity radiation needs a cavity name of at most " +
                   "three characters, which the solver limits it to. The selection target is surfaces.",
                   Arguments = "name: str, region: Selection or Region, sink_temperature: float, " +
                               "emissivity: float, cavity_radiation: bool = None, cavity_name: str = None, " +
                               "emissivity_amplitude_name: str = None, and the shared load arguments",
                   Returns = "Load")]
        public LoadAPI add_radiation_heat_transfer(string name, object region, object sink_temperature,
                                                   object emissivity, object cavity_radiation = null,
                                                   object cavity_name = null,
                                                   object emissivity_amplitude_name = null,
                                                   object amplitude_name = null,
                                                   object coordinate_system_name = null, object phase_deg = null,
                                                   object color = null)
        {
            _service.ValidateName(StepName, name, null);
            RadiationHeatTransfer load =
                new RadiationHeatTransfer(name, null, RegionTypeEnum.Selection, 0, 0, TwoD());
            _service.ApplyRegion(load, region, SurfaceTarget);
            LoadFactory.ApplyRadiationHeatTransfer(load, sink_temperature, emissivity, cavity_radiation);
            ApplyRadiationNames(load, cavity_name, emissivity_amplitude_name);
            //
            return Add(load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Replaces an existing radiation heat transfer load using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_radiation_heat_transfer with the values optional", Returns = "Load")]
        public LoadAPI replace_radiation_heat_transfer(string old_name, string name = null, object region = null,
                                                       object sink_temperature = null, object emissivity = null,
                                                       object cavity_radiation = null, object cavity_name = null,
                                                       object emissivity_amplitude_name = null,
                                                       object amplitude_name = null,
                                                       object coordinate_system_name = null,
                                                       object phase_deg = null, object color = null)
        {
            RadiationHeatTransfer load =
                _service.CloneForReplace<RadiationHeatTransfer>(StepName, old_name, "radiation_heat_transfer");
            ApplyRegionIfGiven(load, region, SurfaceTarget);
            LoadFactory.ApplyRadiationHeatTransfer(load, sink_temperature, emissivity, cavity_radiation);
            ApplyRadiationNames(load, cavity_name, emissivity_amplitude_name);
            //
            return Replace(old_name, name, load, amplitude_name, coordinate_system_name, phase_deg, color);
        }

        [PythonAPI("Copies the load to every following step using command history. It is refused when a " +
                   "following step does not support the load type.",
                   Arguments = "name: str", Returns = "None")]
        public void propagate(string name)
        {
            _service.Propagate(StepName, name);
        }

        [PythonAPI("Hides the symbols of the named loads using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void hide(object names)
        {
            _service.Hide(StepName, ResolveNames(names, "names"));
        }

        [PythonAPI("Shows the symbols of the named loads using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void show(object names)
        {
            _service.Show(StepName, ResolveNames(names, "names"));
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames(StepName);
        }
        protected override Load GetItem(string name)
        {
            return _service.GetItem(StepName, name);
        }
        protected override LoadAPI CreateItemAPI(string name)
        {
            return new LoadAPI(StepName, name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(StepName, names);
        }
        protected override void SetItemsActive(Load[] items, bool active)
        {
            _service.SetActive(StepName, items, active);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(StepName, names);
        }
        protected override void RenameItem(Load item, string newName)
        {
            _service.Rename(StepName, item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "Loads(step=" + StringListAPI.ToDisplayString(StepName) + ", count=" + names.Length +
                   ", names=" + new StringListAPI(names).ToString() + ")";
        }
        //
        private bool TwoD()
        {
            return _service.GetIsTwoDimensional();
        }
        private bool Complex()
        {
            return _service.GetIsComplex(StepName);
        }
        private void ApplyRegionIfGiven(Load load, object region, string target)
        {
            if (region != null) _service.ApplyRegion(load, region, target);
        }
        private void ApplyRadiationNames(RadiationHeatTransfer load, object cavityName,
                                         object emissivityAmplitudeName)
        {
            if (cavityName != null)
            {
                string cavity = cavityName as string;
                if (cavity == null)
                    throw new PythonAPIException("The argument 'cavity_name' must be a string.");
                // The load cuts a longer name to the three characters the solver takes
                if (cavity.Length > 3)
                    throw new PythonAPIException("The argument 'cavity_name' must not be longer than three " +
                                                 "characters: '" + cavity + "'.");
                load.CavityName = cavity;
            }
            if (emissivityAmplitudeName != null)
                load.EmissivityAmplitudeName =
                    _service.ResolveAmplitudeName(emissivityAmplitudeName, "emissivity_amplitude_name");
        }
        private LoadAPI Add(Load load, object amplitudeName, object coordinateSystemName, object phaseDeg,
                            object color)
        {
            _service.ApplyCommonValues(load, amplitudeName, coordinateSystemName, phaseDeg, color);
            _service.Add(StepName, load);
            //
            return this[load.Name];
        }
        private LoadAPI Replace(string oldName, string name, Load load, object amplitudeName,
                                object coordinateSystemName, object phaseDeg, object color)
        {
            _service.ApplyCommonValues(load, amplitudeName, coordinateSystemName, phaseDeg, color);
            if (name != null)
            {
                _service.ValidateName(StepName, name, oldName);
                load.Name = name;
            }
            _service.Replace(StepName, oldName, load);
            //
            return this[load.Name];
        }
    }
}
