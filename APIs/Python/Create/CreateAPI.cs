// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeJob;
using CaeMesh;
using CaeModel;

namespace APIs.Python
{
    // Makes the detached parts that model items take as arguments. A part is never committed to the
    // model; only an add method on a collection does that.
    public class CreateAPI : BaseAPI
    {
        // Variables
        private readonly SelectionService _selectionService;


        // Constructors
        internal CreateAPI(SelectionService selectionService)
        {
            _selectionService = selectionService;
        }


        // Methods
        [PythonAPI("Creates an ID-based selection. Possible view values are 'geometry', 'femodel', and " +
                   "'results'. Possible target values are 'nodes', 'elements', 'surfaces', 'edges', 'parts', " +
                   "'geometry', and 'geometry_surfaces'. Possible source values are 'nodes', 'elements', " +
                   "'vertices', 'edges', 'surfaces', 'parts', and 'geometry'. The 'geometry' source takes the geometry IDs the " +
                   "application records for a geometry pick made in the Selection by ID mode, and converts " +
                   "them to any target.",
                   Arguments = "view: str, target: str, source: str, ids: list[int] or dict[int, list[int]], " +
                               "geometry_select_mode: str = 'SelectId'",
                   Returns = "Selection",
                   Example = "pmx.create.selection_by_ids('femodel', 'nodes', 'surfaces', {1: [3]})")]
        public SelectionAPI selection_by_ids(string view, string target, string source, object ids,
                                              string geometry_select_mode = "SelectId")
        {
            return _selectionService.CreateByIds(view, target, source, ids, geometry_select_mode);
        }

        [PythonAPI("Creates a selection from a pick. The point is a location in space, and the " +
                   "application selects what lies under it, the same way a mouse pick does. It survives a " +
                   "mesh change, because the pick is resolved again rather than replayed from IDs. The " +
                   "nearest entity is picked, so a point that does not lie on the model still resolves, " +
                   "and part_ids limits which parts are searched.",
                   Arguments = "view: str, target: str, point: list[float], select_by: str, " +
                               "angle_deg: float = None, direction: list[float] = None, " +
                               "part_ids: list[int] = None, completely_inside: bool = False, precision: float = None, " +
                               "part_offsets: list[list[float]] = None",
                   Returns = "Selection",
                   Example = "pmx.create.selection_by_pick('femodel', 'surfaces', [0.5, 0.5, 1.0], 'Surface')")]
        public SelectionAPI selection_by_pick(string view, string target, object point, string select_by,
                                              object angle_deg = null, object direction = null,
                                              object part_ids = null, object completely_inside = null,
                                              object precision = null, object part_offsets = null)
        {
            return _selectionService.CreateByPick(view, target, point, select_by, angle_deg, direction, part_ids,
                                                   completely_inside, precision, part_offsets);
        }

        [PythonAPI("Creates a selection from an area pick. The planes bound the volume that is " +
                   "selected, every row being [ox, oy, oz, nx, ny, nz] of one plane, and a normal points " +
                   "out of the volume. It is what a rubber band records, whose planes are the frustum it " +
                   "swept; an axis aligned box is six planes.",
                   Arguments = "view: str, target: str, planes: list[list[float]], select_by: str, " +
                               "angle_deg: float = None, part_ids: list[int] = None, " +
                               "completely_inside: bool = False, part_offsets: list[list[float]] = None", Returns = "Selection")]
        public SelectionAPI selection_by_area(string view, string target, object planes, string select_by,
                                              object angle_deg = null, object part_ids = null,
                                              object completely_inside = null, object part_offsets = null)
        {
            return _selectionService.CreateByArea(view, target, planes, select_by, angle_deg, part_ids,
                                                   completely_inside, part_offsets);
        }

        [PythonAPI("Creates a selection from an axis aligned box, which is the six plane form of " +
                   "selection_by_area. The box is given by its two opposite corners.",
                   Arguments = "view: str, target: str, corner1: list[float], corner2: list[float], " +
                               "select_by: str, part_ids: list[int] = None, " +
                               "completely_inside: bool = False, part_offsets: list[list[float]] = None",
                   Returns = "Selection",
                   Example = "pmx.create.selection_by_box('femodel', 'nodes', [0, 0, 0.9], [1, 1, 1.1], 'Node')")]
        public SelectionAPI selection_by_box(string view, string target, object corner1, object corner2,
                                             string select_by, object part_ids = null,
                                             object completely_inside = null, object part_offsets = null)
        {
            return _selectionService.CreateByBox(view, target, corner1, corner2, select_by, part_ids,
                                                  completely_inside, part_offsets);
        }

        [PythonAPI("Selects every visible active node, element or surface in the requested view.",
                   Arguments = "view: str, target: str", Returns = "Selection")]
        public SelectionAPI selection_all(string view, string target)
        {
            return _selectionService.CreateAll(view, target);
        }

        [PythonAPI("Creates an elastic material property. Give the two values for a constant property, " +
                   "or a list of [youngs_modulus, poissons_ratio, temperature] rows.",
                   Arguments = "youngs_modulus: float or list[list[float]], poissons_ratio: float = None",
                   Returns = "MaterialProperty", Example = "pmx.create.elastic(210000, 0.3)")]
        public Elastic elastic(object youngs_modulus, object poissons_ratio = null)
        {
            return MaterialPropertyFactory.CreateElastic(youngs_modulus, poissons_ratio);
        }

        [PythonAPI("Creates a density material property. Give one value, or a list of " +
                   "[density, temperature] rows.", Arguments = "density: float or list[list[float]]",
                   Returns = "MaterialProperty")]
        public Density density(object density)
        {
            return MaterialPropertyFactory.CreateDensity(density);
        }

        [PythonAPI("Creates a combined elastic and density material property.",
                   Arguments = "youngs_modulus: float, poissons_ratio: float, density: float",
                   Returns = "MaterialProperty")]
        public ElasticWithDensity elastic_with_density(object youngs_modulus, object poissons_ratio,
                                                       object density)
        {
            return MaterialPropertyFactory.CreateElasticWithDensity(youngs_modulus, poissons_ratio, density);
        }

        [PythonAPI("Creates an orthotropic elastic property. Every row is [E1, E2, E3, v12, v13, v23, " +
                   "G12, G13, G23, temperature].", Arguments = "constants: list[list[float]]",
                   Returns = "MaterialProperty")]
        public ElasticEngineeringConstants elastic_engineering_constants(object constants)
        {
            return MaterialPropertyFactory.CreateElasticEngineeringConstants(constants);
        }

        [PythonAPI("Creates a lamina elastic property. Every row is [E1, E2, v12, G12, G13, G23, temperature].",
                   Arguments = "constants: list[list[float]]", Returns = "MaterialProperty")]
        public ElasticLamina elastic_lamina(object constants)
        {
            return MaterialPropertyFactory.CreateElasticLamina(constants);
        }

        [PythonAPI("Creates a plastic material property. Every row is [stress, strain, temperature]. " +
                   "Possible hardening values are Isotropic, Kinematic and Combined; it defaults to " +
                   "Isotropic.",
                   Arguments = "stress_strain: list[list[float]], hardening: str = None",
                   Returns = "MaterialProperty",
                   Example = "pmx.create.plastic([[250, 0, 20]], hardening='Kinematic')")]
        public Plastic plastic(object stress_strain, string hardening = null)
        {
            return MaterialPropertyFactory.CreatePlastic(stress_strain, hardening);
        }

        [PythonAPI("Creates a slip wear material property.",
                   Arguments = "hardness: float, wear_coefficient: float", Returns = "MaterialProperty")]
        public SlipWear slip_wear(object hardness, object wear_coefficient)
        {
            return MaterialPropertyFactory.CreateSlipWear(hardness, wear_coefficient);
        }

        [PythonAPI("Creates a specific heat material property. Give one value, or a list of " +
                   "[specific_heat, temperature] rows.", Arguments = "specific_heat: float or list[list[float]]",
                   Returns = "MaterialProperty")]
        public SpecificHeat specific_heat(object specific_heat)
        {
            return MaterialPropertyFactory.CreateSpecificHeat(specific_heat);
        }

        [PythonAPI("Creates a thermal conductivity material property. Give one value, or a list of " +
                   "[conductivity, temperature] rows.",
                   Arguments = "thermal_conductivity: float or list[list[float]]", Returns = "MaterialProperty")]
        public ThermalConductivity thermal_conductivity(object thermal_conductivity)
        {
            return MaterialPropertyFactory.CreateThermalConductivity(thermal_conductivity);
        }

        [PythonAPI("Creates a thermal expansion material property. Give one value, or a list of " +
                   "[expansion, temperature] rows. The zero temperature is the temperature the expansion " +
                   "starts from and defaults to 20 in model units.",
                   Arguments = "thermal_expansion: float or list[list[float]], " +
                               "zero_temperature: float = None",
                   Returns = "MaterialProperty",
                   Example = "pmx.create.thermal_expansion(1.2e-5, zero_temperature=0)")]
        public ThermalExpansion thermal_expansion(object thermal_expansion, object zero_temperature = null)
        {
            return MaterialPropertyFactory.CreateThermalExpansion(thermal_expansion, zero_temperature);
        }
        [PythonAPI("Creates a friction surface interaction property.",
                   Arguments = "coefficient: float, stick_slope: float = None",
                   Returns = "SurfaceInteractionProperty", Example = "pmx.create.friction(0.1)")]
        public Friction friction(object coefficient, object stick_slope = null)
        {
            return SurfaceInteractionPropertyFactory.CreateFriction(coefficient, stick_slope);
        }

        [PythonAPI("Creates a gap conductance surface interaction property. Give conductance for a " +
                   "constant one, or conductance_table with rows of [conductance, pressure, temperature] " +
                   "for a tabular one.",
                   Arguments = "conductance: float = None, conductance_table: list[list[float]] = None",
                   Returns = "SurfaceInteractionProperty")]
        public GapConductance gap_conductance(object conductance = null, object conductance_table = null)
        {
            return SurfaceInteractionPropertyFactory.CreateGapConductance(conductance, conductance_table);
        }

        [PythonAPI("Creates a surface behavior surface interaction property. Possible " +
                   "pressure_overclosure_type values are Hard, Linear, Exponential, Tabular and Tied. The " +
                   "type sets the defaults it implies, so a value given with it overrides that default.",
                   Arguments = "pressure_overclosure_type: str = None, c0: float = None, p0: float = None, " +
                               "k: float = None, s_inf: float = None, " +
                               "pressure_overclosure: list[list[float]] = None",
                   Returns = "SurfaceInteractionProperty",
                   Example = "pmx.create.surface_behavior('Linear', k=2000)")]
        public SurfaceBehavior surface_behavior(string pressure_overclosure_type = null, object c0 = null,
                                                object p0 = null, object k = null, object s_inf = null,
                                                object pressure_overclosure = null)
        {
            return SurfaceInteractionPropertyFactory.CreateSurfaceBehavior(pressure_overclosure_type, c0, p0,
                                                                           k, s_inf, pressure_overclosure);
        }

        [PythonAPI("Names an existing region of the model, so that a model item can point at it instead of " +
                   "carrying its own selection. Possible region_type values are NodeSetName, ElementSetName, " +
                   "SurfaceName, ReferencePointName, PartName and MassSection. A MassSection region names a " +
                   "point mass or distributed mass section and is used by the gravity and the centrifugal " +
                   "load.",
                   Arguments = "region_type: str, name: str", Returns = "Region",
                   Example = "pmx.create.region('NodeSetName', 'Bottom')")]
        public RegionAPI region(string region_type, string name)
        {
            if (name == null || name.Trim().Length == 0)
                throw new PythonAPIException("The argument 'name' must not be empty.");
            //
            return new RegionAPI(PythonValueConverter.ToEnum<RegionTypeEnum>(region_type, "region_type"), name);
        }

        [PythonAPI("Creates the splitting tool of a line drawn on the surfaces through two points, which only " +
                   "split_shell takes. Give the points, or a 'geometry' selection in the 'geometry' view that " +
                   "gives them: a vertex gives its own position and a circular edge its center. The line " +
                   "follows the surface and is continued to its boundary.",
                   Arguments = "points: list[list[float]] = None, splitter: Selection = None",
                   Returns = "SplitTool",
                   Example = "pmx.create.split_tool_line_by_two_points([[0, 0, 20], [60, 40, 20]])")]
        public SplitToolAPI split_tool_line_by_two_points(object points = null, SelectionAPI splitter = null)
        {
            SplitByLineTwoPoints splitByLineTwoPoints = new SplitByLineTwoPoints();
            SetSplitToolPoints(splitByLineTwoPoints.Points, points, splitter);
            if (points != null) splitByLineTwoPoints.NumberOfPoints = 2;
            //
            return new SplitToolAPI(splitByLineTwoPoints, splitter, "splitter", null);
        }

        [PythonAPI("Creates the splitting tool of a plane through three points. Give the points, or a " +
                   "'geometry' selection in the 'geometry' view that gives them: a vertex gives its own " +
                   "position and a circular edge its center. The offset moves the plane along its normal.",
                   Arguments = "points: list[list[float]] = None, splitter: Selection = None, offset: float = 0",
                   Returns = "SplitTool",
                   Example = "pmx.create.split_tool_plane_by_three_points([[0, 0, 10], [1, 0, 10], [0, 1, 10]])")]
        public SplitToolAPI split_tool_plane_by_three_points(object points = null, SelectionAPI splitter = null,
                                                             object offset = null)
        {
            SplitByPlaneThreePoints splitByPlaneThreePoints = new SplitByPlaneThreePoints();
            SetSplitToolPoints(splitByPlaneThreePoints.Points, points, splitter);
            if (points != null) splitByPlaneThreePoints.NumberOfPoints = 3;
            splitByPlaneThreePoints.Offset = ToSplitToolOffset(offset);
            //
            return new SplitToolAPI(splitByPlaneThreePoints, splitter, "splitter", null);
        }

        [PythonAPI("Creates the splitting tool of a plane given by a point and a normal. Give them, or a " +
                   "'geometry' selection in the 'geometry' view that gives them: a vertex gives the point, " +
                   "a straight edge or a planar, cylindrical or conical face gives the normal, and a circular " +
                   "edge gives its center. A value the selection does not give is taken from the argument, " +
                   "and one given both ways must agree. A point that is not given at all is put at the center " +
                   "of the parts being split, the way the dialog places it. The offset moves the plane along " +
                   "its normal.",
                   Arguments = "point: list[float] = None, normal: list[float] = None, splitter: Selection = None, " +
                               "offset: float = 0",
                   Returns = "SplitTool",
                   Example = "pmx.create.split_tool_plane_by_point_and_normal([30, 20, 10], [0, 0, 1])")]
        public SplitToolAPI split_tool_plane_by_point_and_normal(object point = null, object normal = null,
                                                                 SelectionAPI splitter = null, object offset = null)
        {
            if (normal == null && splitter == null)
                throw new PythonAPIException("Give the argument 'normal' or a 'splitter' selection that gives it.");
            //
            SplitByPointAndNormal splitByPointAndNormal = new SplitByPointAndNormal();
            // A value that is not given is left empty, so that what the selection gives can be told apart
            // from what was typed when the tool is applied
            splitByPointAndNormal.Point = point == null ? null : ToSplitToolVector(point, "point", false);
            splitByPointAndNormal.Normal = normal == null ? null : ToSplitToolVector(normal, "normal", true);
            splitByPointAndNormal.Offset = ToSplitToolOffset(offset);
            //
            return new SplitToolAPI(splitByPointAndNormal, splitter, "splitter", null);
        }

        [PythonAPI("Creates the splitting tool of the surfaces of the selected faces, continued past their " +
                   "boundaries. The faces are a 'geometry' or 'geometry_surfaces' selection in the 'geometry' " +
                   "view and may lie on a part that is not being split. Only a plane, a cylinder, a cone, a " +
                   "sphere and a torus continue past the face boundary; a face on a spline patch is left out. " +
                   "The offset moves every continued surface along its own normal, and a positive value grows " +
                   "the radius of a curved one.",
                   Arguments = "faces: Selection, offset: float = 0", Returns = "SplitTool",
                   Example = "pmx.create.split_tool_extend_face(faces)")]
        public SplitToolAPI split_tool_extend_face(SelectionAPI faces, object offset = null)
        {
            if (faces == null) throw new PythonAPIException("The argument 'faces' must not be None.");
            //
            SplitByExtendedFace splitByExtendedFace = new SplitByExtendedFace();
            splitByExtendedFace.Offset = ToSplitToolOffset(offset);
            //
            return new SplitToolAPI(splitByExtendedFace, faces, "faces", null);
        }

        [PythonAPI("Creates the splitting tool of the selected edges swept along a direction. The edges are " +
                   "a 'geometry' selection in the 'geometry' view. Give the direction as a vector, or as a " +
                   "'geometry' selection: one straight edge gives the way it runs, a planar face its normal, " +
                   "a cylindrical or conical face its axis, and a planar face together with a straight edge " +
                   "the direction that lies in the face and crosses the edge. A length that is not given " +
                   "sweeps the edges through the whole region being split.",
                   Arguments = "edges: Selection, direction: list[float] = None, " +
                               "direction_selection: Selection = None, length: float = None, " +
                               "both_directions: bool = True",
                   Returns = "SplitTool",
                   Example = "pmx.create.split_tool_extrude_edges(edges, direction=[0, 0, 1])")]
        public SplitToolAPI split_tool_extrude_edges(SelectionAPI edges, object direction = null,
                                                     SelectionAPI direction_selection = null, object length = null,
                                                     bool both_directions = true)
        {
            if (edges == null) throw new PythonAPIException("The argument 'edges' must not be None.");
            if ((direction == null) == (direction_selection == null))
                throw new PythonAPIException("Give either the argument 'direction' or 'direction_selection'.");
            //
            SplitByExtrudedEdges splitByExtrudedEdges = new SplitByExtrudedEdges();
            if (direction != null)
            {
                splitByExtrudedEdges.DirectionType = SplitExtrusionDirectionTypeEnum.Vector;
                splitByExtrudedEdges.Direction = ToSplitToolVector(direction, "direction", true);
            }
            else splitByExtrudedEdges.DirectionType = SplitExtrusionDirectionTypeEnum.Selected;
            //
            if (length != null)
            {
                double lengthValue = PythonValueConverter.ToFiniteDouble(length, "length");
                if (lengthValue <= 0) throw new PythonAPIException("The argument 'length' must be larger than zero.");
                splitByExtrudedEdges.LengthType = SplitExtrusionLengthTypeEnum.Value;
                splitByExtrudedEdges.Length = lengthValue;
            }
            else splitByExtrudedEdges.LengthType = SplitExtrusionLengthTypeEnum.ThroughRegion;
            splitByExtrudedEdges.BothDirections = both_directions;
            //
            return new SplitToolAPI(splitByExtrudedEdges, edges, "edges", direction_selection);
        }

        [PythonAPI("Creates a Rayleigh damping part for a dynamic step. Possible damping_type values are " +
                   "Off and Rayleigh.",
                   Arguments = "damping_type: str = None, alpha: float = None, beta: float = None",
                   Returns = "Damping", Example = "pmx.create.damping('Rayleigh', alpha=0.1, beta=0.001)")]
        public Damping damping(string damping_type = null, object alpha = null, object beta = null)
        {
            return StepPartFactory.CreateDamping(damping_type, alpha, beta);
        }

        [PythonAPI("Creates a modal damping part for a modal dynamics or steady state dynamics step. " +
                   "Possible damping_type values are Off, Constant, Direct and Rayleigh. The damping ratios " +
                   "are used by the Direct type.",
                   Arguments = "damping_type: str = None, alpha: float = None, beta: float = None, " +
                               "viscous_damping_ratio: float = None, damping_ratios: DampingRatio or " +
                               "list[DampingRatio] = None", Returns = "ModalDamping",
                   Example = "pmx.create.modal_damping('Constant', viscous_damping_ratio=0.02)")]
        public ModalDamping modal_damping(string damping_type = null, object alpha = null, object beta = null,
                                          object viscous_damping_ratio = null, object damping_ratios = null)
        {
            return StepPartFactory.CreateModalDamping(damping_type, alpha, beta, viscous_damping_ratio,
                                                      damping_ratios);
        }

        [PythonAPI("Creates a viscous damping ratio for a mode range, used by the Direct modal damping type.",
                   Arguments = "damping_ratio: float, lowest_mode: int, highest_mode: int",
                   Returns = "DampingRatio", Example = "pmx.create.damping_ratio(0.02, 1, 10)")]
        public DampingRatioAndRange damping_ratio(object damping_ratio, object lowest_mode,
                                                  object highest_mode)
        {
            return StepPartFactory.CreateDampingRatio(damping_ratio, lowest_mode, highest_mode);
        }

        [PythonAPI("Creates a time incrementation step control parameter. Only the given values override " +
                   "the CalculiX defaults.",
                   Arguments = "i0: int = None, ir: int = None, ip: int = None, ic: int = None, " +
                               "il: int = None, ig: int = None, is_value: int = None, ia: int = None, " +
                               "ij: int = None, it: int = None, df: float = None, dc: float = None, " +
                               "db: float = None, da: float = None, ds: float = None, dh: float = None, " +
                               "dd: float = None, wg: float = None", Returns = "StepControlParameter",
                   Example = "pmx.create.time_incrementation_control(i0=8, ic=32)")]
        public TimeIncrementationStepControlParameter time_incrementation_control(
            object i0 = null, object ir = null, object ip = null, object ic = null, object il = null,
            object ig = null, object is_value = null, object ia = null, object ij = null, object it = null,
            object df = null, object dc = null, object db = null, object da = null, object ds = null,
            object dh = null, object dd = null, object wg = null)
        {
            return StepPartFactory.CreateTimeIncrementationControl(i0, ir, ip, ic, il, ig, is_value, ia, ij, it,
                                                                   df, dc, db, da, ds, dh, dd, wg);
        }

        [PythonAPI("Creates a field step control parameter. Only the given values override the CalculiX " +
                   "defaults.",
                   Arguments = "rna: float = None, cna: float = None, q0a: float = None, qua: float = None, " +
                               "rpa: float = None, ea: float = None, cea: float = None, rla: float = None",
                   Returns = "StepControlParameter")]
        public FieldStepControlParameter field_control(object rna = null, object cna = null, object q0a = null,
                                                       object qua = null, object rpa = null, object ea = null,
                                                       object cea = null, object rla = null)
        {
            return StepPartFactory.CreateFieldControl(rna, cna, q0a, qua, rpa, ea, cea, rla);
        }

        [PythonAPI("Creates a contact step control parameter. Only the given values override the CalculiX " +
                   "defaults.",
                   Arguments = "delcon: float = None, alea: float = None, kscalemax: int = None, " +
                               "itf2f: int = None", Returns = "StepControlParameter")]
        public ContactStepControlParameter contact_control(object delcon = null, object alea = null,
                                                           object kscalemax = null, object itf2f = null)
        {
            return StepPartFactory.CreateContactControl(delcon, alea, kscalemax, itf2f);
        }

        [PythonAPI("Creates a reset step control parameter, which restores the CalculiX default controls.",
                   Returns = "StepControlParameter")]
        public ResetStepControlParameter reset_control()
        {
            return StepPartFactory.CreateResetControl();
        }

        [PythonAPI("Creates a detached solver environment variable, including its enabled state.",
                   Arguments = "name: str, value: str, active: bool = True", Returns = "EnvironmentVariable")]
        public EnvironmentVariable environment_variable(string name, string value, bool active = true)
        {
            JobService.ValidateEnvironmentVariable(name, value);
            return new EnvironmentVariable(name, value) { Active = active };
        }
        public override string ToString()
        {
            return "Create()";
        }
        //
        private static void SetSplitToolPoints(double[][] toolPoints, object points, SelectionAPI splitter)
        {
            // The selection gives the points again whenever the split is applied, so points given as
            // well would be replaced rather than used
            if ((points == null) == (splitter == null))
                throw new PythonAPIException("Give either the argument 'points' or a 'splitter' selection.");
            if (points == null) return;
            //
            double[][] values = PythonDoubleListConverter.ToDoubleArray2D(points, "points");
            if (values.Length != toolPoints.Length)
                throw new PythonAPIException("The argument 'points' must contain " + toolPoints.Length + " points.");
            for (int i = 0; i < values.Length; i++)
                toolPoints[i] = ToSplitToolVector(values[i], "points", false);
        }
        private static double[] ToSplitToolVector(object value, string argumentName, bool isDirection)
        {
            double[] vector = value as double[] ?? PythonDoubleListConverter.ToDoubleArray(value, argumentName);
            if (vector.Length != 3)
                throw new PythonAPIException("The argument '" + argumentName + "' must contain 3 values.");
            foreach (double component in vector)
            {
                if (double.IsNaN(component) || double.IsInfinity(component))
                    throw new PythonAPIException("The argument '" + argumentName + "' must be finite.");
            }
            if (isDirection && vector[0] == 0 && vector[1] == 0 && vector[2] == 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be a zero vector.");
            //
            return vector;
        }
        private static double ToSplitToolOffset(object offset)
        {
            return offset == null ? 0 : PythonValueConverter.ToFiniteDouble(offset, "offset");
        }
    }
}
