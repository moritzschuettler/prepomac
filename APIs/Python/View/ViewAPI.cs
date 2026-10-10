// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;
using System;
using System.Collections.Generic;

namespace APIs.Python
{
    // What the application shows: which of the three trees is displayed, where the camera is, which
    // result is plotted, and a picture of it. Nothing here changes the model, so nothing here is
    // recorded in the command history - which also means a script cannot restore a view by being
    // regenerated, and should put the view back itself if it moved it.
    //
    // A picture is the one thing about a model that cannot be read: whether the load is on the face
    // that was meant is a question a screenshot settles and a property list does not.
    public class ViewAPI : BaseAPI
    {
        // Variables
        // The renderer draws a magnified picture in tiles, so this is kept small enough that the
        // application stays the one deciding what a picture of a viewport may cost
        private const int MaxMagnification = 4;
        private static readonly string[] Views = { "geometry", "femodel", "results" };
        private static readonly Dictionary<ViewExplodedMethodEnum, double> ExplodedViewMaxMagnifications =
            new Dictionary<ViewExplodedMethodEnum, double>
            {
                { ViewExplodedMethodEnum.Disassembly, 10 },
                { ViewExplodedMethodEnum.AssemblyCenter, 25 },
                { ViewExplodedMethodEnum.CenterPoint, 1000 }
            };

        private readonly IPythonViewGateway _gateway;
        private readonly ViewAnnotationsAPI _annotations;


        // Properties
        [PythonAPI("The displayed tree: 'geometry', 'femodel' or 'results'. Setting it is what the tabs above " +
                   "the model tree do.", Returns = "str")]
        public object current
        {
            get { return Resolve().GetCurrentView(); }
            set { Resolve().SetCurrentView(ValidateOneOf(ToText(value, "current"), Views, "current")); }
        }
        [PythonAPI("How the results are plotted: Undeformed, Deformed, ColorContours, VectorPlot or TensorPlot.",
                   Returns = "str")]
        public object result_type
        {
            get { return Resolve().GetResultType(); }
            set
            {
                Resolve().SetResultType(PythonValueConverter.ToEnum<ViewResultTypeEnum>(ToText(value, "result_type"),
                                                                                         "result_type").ToString());
            }
        }
        [PythonAPI("How much the plotted result is deformed, as the results toolbar sets it: Undeformed, " +
                   "TrueScale, Automatic - which fits the largest displacement to a quarter of the model size - " +
                   "its multiples Automatic025, Automatic05, Automatic2 and Automatic5, or UserDefined, which " +
                   "uses deformation_factor. Setting it needs an open result.", Returns = "str")]
        public object deformation_type
        {
            get { return Resolve().GetDeformationType(); }
            set
            {
                DeformationScaleFactorTypeEnum type =
                    PythonValueConverter.ToEnum<DeformationScaleFactorTypeEnum>(ToText(value, "deformation_type"),
                                                                                "deformation_type");
                Resolve().SetDeformationType(type.ToString());
            }
        }
        [PythonAPI("The deformation scale factor the UserDefined deformation_type uses. It is kept when another " +
                   "type is set, the way the toolbar keeps it. Setting it needs an open result.", Returns = "float")]
        public object deformation_factor
        {
            get { return Resolve().GetDeformationFactor(); }
            set { Resolve().SetDeformationFactor(PythonValueConverter.ToFiniteDouble(value, "deformation_factor")); }
        }
        [PythonAPI("The undeformed model drawn behind a ColorContours, VectorPlot or TensorPlot result: None, " +
                   "WireframeBody or SolidBody. Setting it needs an open result.", Returns = "str")]
        public object undeformed_model
        {
            get { return Resolve().GetUndeformedModelType(); }
            set
            {
                UndeformedModelTypeEnum type =
                    PythonValueConverter.ToEnum<UndeformedModelTypeEnum>(ToText(value, "undeformed_model"),
                                                                         "undeformed_model");
                Resolve().SetUndeformedModelType(type.ToString());
            }
        }
        [PythonAPI("True when the displayed view is pulled apart by an exploded view.", Returns = "bool")]
        public bool exploded_view_active { get { return Resolve().IsExplodedViewActive(); } }
        [PythonAPI("The arrow annotations of the displayed view.", Returns = "ViewAnnotations")]
        public ViewAnnotationsAPI annotations { get { return _annotations; } }


        // Constructors
        internal ViewAPI(IPythonViewGateway gateway)
        {
            _gateway = gateway;
            _annotations = new ViewAnnotationsAPI(gateway);
        }


        // Methods
        [PythonAPI("Points the camera along a standard direction. Possible values are Front, Back, Top, " +
                   "Bottom, Left, Right, Isometric, IsometricNegative, Normal and Vertical. Normal looks along " +
                   "the normal of the current selection, and Vertical puts the vertical axis upright.",
                   Arguments = "orientation: str", Returns = "None",
                   Example = "pmx.view.set_orientation('Isometric')")]
        public void set_orientation(object orientation)
        {
            string orientationText = PythonValueConverter.ToText(orientation, "orientation", false);
            Resolve().SetStandardView(PythonValueConverter.ToEnum<ViewOrientationEnum>(orientationText, "orientation")
                                                          .ToString());
        }
        [PythonAPI("Zooms the camera so that everything visible fits in the viewport.", Returns = "None")]
        public void zoom_to_fit()
        {
            Resolve().ZoomToFit();
        }
        [PythonAPI("Returns the camera as 10 values: the position, the focal point and the view up vector, " +
                   "three each, and the parallel scale. Keep them to put the view back after changing it.",
                   Returns = "list[float]")]
        public object get_parameters()
        {
            return PythonListConverter.ToPythonList(Resolve().GetViewParameters());
        }
        [PythonAPI("Sets the camera from the 10 values get_parameters returns.",
                   Arguments = "parameters: list[float]", Returns = "None",
                   Example = "camera = pmx.view.get_parameters()\n" +
                             "pmx.view.set_orientation('Top')\n" +
                             "pmx.view.set_parameters(camera)")]
        public void set_parameters(object parameters)
        {
            double[] values = PythonDoubleListConverter.ToDoubleArray(parameters, "parameters");
            if (values == null || values.Length != 10)
                throw new PythonAPIException("The argument 'parameters' must contain the 10 values " +
                                             "get_parameters returns.");
            //
            Resolve().SetViewParameters(values);
        }
        [PythonAPI("Turns the camera about the model by the given angles in degrees: azimuth_deg about the " +
                   "vertical axis of the view, elevation_deg about its horizontal axis, and roll_deg about the " +
                   "viewing direction. A mouse rotation is the same operation, so a picture from a slightly " +
                   "turned standard view is set_orientation followed by rotate.",
                   Arguments = "azimuth_deg: float, elevation_deg: float = 0, roll_deg: float = 0", Returns = "None",
                   Example = "pmx.view.set_orientation('Front')\npmx.view.rotate(30, 20)")]
        public void rotate(object azimuth_deg, object elevation_deg = null, object roll_deg = null)
        {
            double azimuthAngle = PythonValueConverter.ToFiniteDouble(azimuth_deg, "azimuth_deg");
            double elevationAngle = elevation_deg == null ? 0 : PythonValueConverter.ToFiniteDouble(elevation_deg,
                                                                                                      "elevation_deg");
            double rollAngle = roll_deg == null ? 0 : PythonValueConverter.ToFiniteDouble(roll_deg, "roll_deg");
            Resolve().Rotate(azimuthAngle, elevationAngle, rollAngle);
        }
        [PythonAPI("Moves the camera so that it looks at the given point in model coordinates, keeping the " +
                   "viewing direction and the view up. The parallel scale is half the height of the viewport " +
                   "in model units, so a smaller one shows a smaller region around the point; one that is not " +
                   "given keeps the current zoom.",
                   Arguments = "point: list[float], parallel_scale: float = None", Returns = "None",
                   Example = "pmx.view.look_at([50, 0, 10], 25)")]
        public void look_at(object point, object parallel_scale = null)
        {
            double[] target = PythonDoubleListConverter.ToDoubleArray(point, "point");
            if (target == null || target.Length != 3)
                throw new PythonAPIException("The argument 'point' must contain 3 coordinates.");
            foreach (double coordinate in target)
            {
                if (double.IsNaN(coordinate) || double.IsInfinity(coordinate))
                    throw new PythonAPIException("The argument 'point' must contain finite coordinates.");
            }
            double scale = -1;
            if (parallel_scale != null)
            {
                scale = PythonValueConverter.ToFiniteDouble(parallel_scale, "parallel_scale");
                if (scale <= 0) throw new PythonAPIException("The argument 'parallel_scale' must be positive.");
            }
            //
            double[] camera = Resolve().GetViewParameters();
            if (camera == null || camera.Length != 10)
                throw new PythonAPIException("The camera is not available in this context.");
            //
            for (int i = 0; i < 3; i++)
            {
                camera[i] = target[i] + (camera[i] - camera[i + 3]);
                camera[i + 3] = target[i];
            }
            if (scale > 0) camera[9] = scale;
            Resolve().SetViewParameters(camera);
        }
        [PythonAPI("Plots a field component of the current result, the way the toolbar above the viewport " +
                   "does. The component that is not given is the first one of the field.",
                   Arguments = "field_name: str, component_name: str = None", Returns = "None",
                   Example = "pmx.view.set_result_field('DISP', 'U3')")]
        public void set_result_field(object field_name, object component_name = null)
        {
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", true);
            string componentName = PythonValueConverter.ToText(component_name, "component_name", true);
            if (string.IsNullOrEmpty(fieldName))
                throw new PythonAPIException("The argument 'field_name' must not be empty.");
            //
            Resolve().SetResultFieldData(fieldName, componentName);
        }
        [PythonAPI("Plots a given increment of the current result. The IDs are the result step and increment " +
                   "IDs, the ones pmx.results.current.steps reports, not model step names.",
                   Arguments = "step_id: int, increment_id: int", Returns = "None",
                   Example = "pmx.view.set_result_increment(1, -1)")]
        public void set_result_increment(object step_id, object increment_id)
        {
            int stepId = PythonValueConverter.ToInt(step_id, "step_id");
            int incrementId = PythonValueConverter.ToInt(increment_id, "increment_id");
            Resolve().SetResultIncrement(stepId, incrementId);
        }
        [PythonAPI("Returns what is plotted right now as a dict of field_name, component_name, step_id and " +
                   "increment_id, the arguments set_result_field and set_result_increment take, with the IDs " +
                   "as integers. None when no result field is plotted.",
                   Returns = "dict[str, Any] or None",
                   Example = "plotted = pmx.view.get_result_field()  # {'field_name': 'DISP', 'component_name': " +
                             "'U3', 'step_id': 1, 'increment_id': 1}")]
        public Dictionary<string, object> get_result_field()
        {
            string fieldName;
            string componentName;
            int stepId;
            int incrementId;
            if (!Resolve().GetResultFieldData(out fieldName, out componentName, out stepId, out incrementId))
                return null;
            //
            Dictionary<string, object> plotted = new Dictionary<string, object>();
            plotted.Add("field_name", fieldName);
            plotted.Add("component_name", componentName);
            plotted.Add("step_id", stepId);
            plotted.Add("increment_id", incrementId);
            return plotted;
        }
        [PythonAPI("Fixes the range of the color legend of the plotted field component, so that pictures of " +
                   "different increments or models share one color scale. Values outside the range take the " +
                   "end colors. The range is kept per field component while the result is open, the way the " +
                   "legend settings keep it. Needs an open result in the results view.",
                   Arguments = "min_value: float, max_value: float", Returns = "None",
                   Example = "pmx.view.set_legend_range(0, 250)")]
        public void set_legend_range(object min_value, object max_value)
        {
            double minValue = PythonValueConverter.ToFiniteDouble(min_value, "min_value");
            double maxValue = PythonValueConverter.ToFiniteDouble(max_value, "max_value");
            if (minValue >= maxValue)
                throw new PythonAPIException("The argument 'min_value' must be smaller than 'max_value'.");
            //
            Resolve().SetLegendRange(minValue, maxValue);
        }
        [PythonAPI("Puts the color legend of the plotted field component back to the automatic range. Needs " +
                   "an open result in the results view.", Returns = "None")]
        public void reset_legend_range()
        {
            Resolve().ResetLegendRange();
        }
        [PythonAPI("Writes a PNG of the viewport as it looks right now and returns the path. A file name that " +
                   "is not given means a file in the temporary folder. A transparent background leaves out the " +
                   "background the viewport shows, so the picture can be put on a page of any color; the model, " +
                   "the legend and the labels keep their own colors. The magnification renders the picture that " +
                   "many times larger than the viewport, from 1 to 4, for a picture that is going to be printed. " +
                   "Only the model is drawn larger: the legend, the scale bar and the triad keep the size they " +
                   "have on screen, so they come out smaller in a magnified picture.",
                   Arguments = "file_name: str = None, transparent_background: bool = False, magnification: int = 1",
                   Returns = "str",
                   Example = "path = pmx.view.save_image('C:/temp/model.png', True, 2)")]
        public string save_image(object file_name = null, object transparent_background = null,
                                 object magnification = null)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", true);
            if (fileName != null && fileName.Trim().Length == 0)
                throw new PythonAPIException("The argument 'file_name' must not be empty.");
            //
            bool transparent = transparent_background != null &&
                               PythonValueConverter.ToBool(transparent_background, "transparent_background");
            int scale = magnification == null ? 1 : PythonValueConverter.ToInt(magnification, "magnification");
            if (scale < 1 || scale > MaxMagnification)
                throw new PythonAPIException("The argument 'magnification' must be from 1 to " +
                                             MaxMagnification + ".");
            //
            return Resolve().SaveImage(fileName == null ? null : fileName.Trim(), transparent, scale);
        }
        [PythonAPI("Pulls the parts of the displayed view apart, the way the exploded view dialog does. The " +
                   "method is Disassembly, which moves each part the way it would be taken off, AssemblyCenter, " +
                   "which moves the parts away from the center of the assembly, or CenterPoint, which moves them " +
                   "away from the given center. The scale factor, from 0 to 1, is the position of the dialog's " +
                   "slider, and the magnification - from 1 to 10 for Disassembly, 25 for AssemblyCenter and 1000 " +
                   "for CenterPoint - how far the full separation goes. The axes limit the movement to the " +
                   "given coordinate axes: XYZ, X, Y, Z, XY, XZ or YZ. A compound part moves as one body. Step " +
                   "by step, off when not given, and the contact tolerance, where 0 - the value when not given - " +
                   "means one thousandth of the assembly diagonal, are taken only by the Disassembly method, and " +
                   "the center only and always by the CenterPoint method; another method refuses them. An " +
                   "exploded view is a display translation only: it is not recorded, and meshing always runs " +
                   "without it.",
                   Arguments = "scale_factor: float = 0.5, method: str = 'Disassembly', magnification: float = 2, " +
                               "axes: str = 'XYZ', step_by_step: bool = None, tolerance: float = None, " +
                               "center: list[float] = None",
                   Returns = "None",
                   Example = "pmx.view.current = 'geometry'\n" +
                             "pmx.view.apply_exploded_view(1, 'AssemblyCenter', 5)\n" +
                             "pmx.view.remove_exploded_view()")]
        public void apply_exploded_view(object scale_factor = null, object method = null, object magnification = null,
                                        object axes = null, object step_by_step = null, object tolerance = null,
                                        object center = null)
        {
            string methodText = PythonValueConverter.ToText(method, "method", true);
            string axesText = PythonValueConverter.ToText(axes, "axes", true);
            double scaleFactor = scale_factor == null ? 0.5 : PythonValueConverter.ToFiniteDouble(scale_factor,
                                                                                                "scale_factor");
            if (scaleFactor < 0 || scaleFactor > 1)
                throw new PythonAPIException("The argument 'scale_factor' must be from 0 to 1.");
            ViewExplodedMethodEnum methodValue =
                methodText == null ? ViewExplodedMethodEnum.Disassembly
                               : PythonValueConverter.ToEnum<ViewExplodedMethodEnum>(methodText, "method");
            string methodName = methodValue.ToString();
            double maxMagnification = ExplodedViewMaxMagnifications[methodValue];
            double magnificationValue = magnification == null ? 2 : PythonValueConverter.ToFiniteDouble(magnification,
                                                                                                       "magnification");
            if (magnificationValue < 1 || magnificationValue > maxMagnification)
                throw new PythonAPIException("The argument 'magnification' must be from 1 to " + maxMagnification +
                                             " for the " + methodName + " method.");
            string axesName = axesText == null
                ? ViewExplodedAxesEnum.XYZ.ToString()
                : PythonValueConverter.ToEnum<ViewExplodedAxesEnum>(axesText, "axes").ToString();
            if (methodValue != ViewExplodedMethodEnum.Disassembly && (step_by_step != null || tolerance != null))
                throw new PythonAPIException("The arguments 'step_by_step' and 'tolerance' are taken only by the " +
                                             "Disassembly method, not by the " + methodName + " method.");
            if (methodValue != ViewExplodedMethodEnum.CenterPoint && center != null)
                throw new PythonAPIException("The argument 'center' is taken only by the CenterPoint method, not by " +
                                             "the " + methodName + " method.");
            bool sequential = step_by_step != null && PythonValueConverter.ToBool(step_by_step, "step_by_step");
            double toleranceValue = tolerance == null ? 0 : PythonValueConverter.ToFiniteDouble(tolerance, "tolerance");
            if (toleranceValue < 0) throw new PythonAPIException("The argument 'tolerance' must not be negative.");
            double[] centerPoint = null;
            if (center != null)
            {
                centerPoint = PythonDoubleListConverter.ToDoubleArray(center, "center");
                if (centerPoint == null || centerPoint.Length != 3)
                    throw new PythonAPIException("The argument 'center' must contain 3 coordinates.");
            }
            else if (methodValue == ViewExplodedMethodEnum.CenterPoint)
                throw new PythonAPIException("The CenterPoint method needs the argument 'center'.");
            //
            Resolve().ApplyExplodedView(methodName, scaleFactor, magnificationValue, axesName, sequential,
                                        toleranceValue, centerPoint);
        }
        [PythonAPI("Puts the parts of the displayed view back where they are, the way the exploded view " +
                   "dialog's Disable button does. Nothing happens when no exploded view is applied.",
                   Returns = "None")]
        public void remove_exploded_view()
        {
            Resolve().RemoveExplodedView();
        }
        [PythonAPI("Returns the standard orientation names set_orientation accepts.", Returns = "StringList")]
        public StringListAPI get_orientation_names()
        {
            return new StringListAPI(Enum.GetNames(typeof(ViewOrientationEnum)));
        }
        //
        private IPythonViewGateway Resolve()
        {
            if (_gateway == null)
                throw new PythonAPIException("The view is not available in this Python API context.");
            //
            return _gateway;
        }
        // A property typed as a .NET string would let pythonnet refuse a value of another type with a TypeError that
        // names no property, so the value is taken as an object and converted here
        private static string ToText(object value, string propertyName)
        {
            return PythonValueConverter.ToText(value, propertyName, false);
        }
        private static string ValidateOneOf(string value, string[] allowed, string argumentName)
        {
            if (value == null) throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            //
            foreach (string entry in allowed)
            {
                if (string.Equals(entry, value.Trim(), StringComparison.OrdinalIgnoreCase)) return entry;
            }
            //
            throw new PythonAPIException("The value '" + value + "' is not supported for the argument '" +
                                         argumentName + "'. Possible values are " +
                                         string.Join(", ", allowed) + ".");
        }
        public override string ToString()
        {
            return "View(current=" + (_gateway == null ? "none" : _gateway.GetCurrentView()) + ")";
        }
    }
}
