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
        private static readonly string[] Orientations = { "Front", "Back", "Top", "Bottom", "Left", "Right",
                                                          "Isometric", "IsometricNegative", "Normal", "Vertical" };
        private static readonly string[] ResultTypes = { "Undeformed", "Deformed", "ColorContours", "VectorPlot",
                                                         "TensorPlot" };

        private readonly IPythonViewGateway _gateway;
        private readonly ViewAnnotationsAPI _annotations;


        // Properties
        [PythonAPI("The displayed tree: 'geometry', 'femodel' or 'results'. Setting it is what the tabs above " +
                   "the model tree do.", Returns = "str")]
        public string current
        {
            get { return Resolve().GetCurrentView(); }
            set { Resolve().SetCurrentView(ValidateOneOf(value, Views, "current")); }
        }
        [PythonAPI("How the results are plotted: Undeformed, Deformed, ColorContours, VectorPlot or TensorPlot.",
                   Returns = "str")]
        public string result_type
        {
            get { return Resolve().GetResultType(); }
            set { Resolve().SetResultType(ValidateOneOf(value, ResultTypes, "result_type")); }
        }
        [PythonAPI("How much the plotted result is deformed, as the results toolbar sets it: Undeformed, " +
                   "TrueScale, Automatic - which fits the largest displacement to a quarter of the model size - " +
                   "its multiples Automatic025, Automatic05, Automatic2 and Automatic5, or UserDefined, which " +
                   "uses deformation_factor. Setting it needs an open result.", Returns = "str")]
        public string deformation_type
        {
            get { return Resolve().GetDeformationType(); }
            set
            {
                DeformationScaleFactorTypeEnum type =
                    PythonValueConverter.ToEnum<DeformationScaleFactorTypeEnum>(value, "deformation_type");
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
        public string undeformed_model
        {
            get { return Resolve().GetUndeformedModelType(); }
            set
            {
                UndeformedModelTypeEnum type =
                    PythonValueConverter.ToEnum<UndeformedModelTypeEnum>(value, "undeformed_model");
                Resolve().SetUndeformedModelType(type.ToString());
            }
        }
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
        public void set_orientation(string orientation)
        {
            Resolve().SetStandardView(ValidateOneOf(orientation, Orientations, "orientation"));
        }
        [PythonAPI("Zooms the camera so that everything visible fits in the viewport.", Returns = "None")]
        public void zoom_to_fit()
        {
            Resolve().ZoomToFit();
        }
        [PythonAPI("Returns the camera as 10 values: the position, the focal point and the view up vector, " +
                   "three each, and the parallel scale. Keep them to put the view back after changing it.",
                   Returns = "NumericArray")]
        public NumericArrayAPI get_parameters()
        {
            double[] parameters = Resolve().GetViewParameters();
            return new NumericArrayAPI(parameters == null ? new double[0] : parameters);
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
        [PythonAPI("Turns the camera about the model by the given angles in degrees: azimuth about the " +
                   "vertical axis of the view, elevation about its horizontal axis, and roll about the " +
                   "viewing direction. A mouse rotation is the same operation, so a picture from a slightly " +
                   "turned standard view is set_orientation followed by rotate.",
                   Arguments = "azimuth: float, elevation: float = 0, roll: float = 0", Returns = "None",
                   Example = "pmx.view.set_orientation('Front')\npmx.view.rotate(30, 20)")]
        public void rotate(object azimuth, object elevation = null, object roll = null)
        {
            double azimuthAngle = PythonValueConverter.ToFiniteDouble(azimuth, "azimuth");
            double elevationAngle = elevation == null ? 0 : PythonValueConverter.ToFiniteDouble(elevation, "elevation");
            double rollAngle = roll == null ? 0 : PythonValueConverter.ToFiniteDouble(roll, "roll");
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
        public void set_result_field(string field_name, string component_name = null)
        {
            if (string.IsNullOrEmpty(field_name))
                throw new PythonAPIException("The argument 'field_name' must not be empty.");
            //
            Resolve().SetResultFieldData(field_name, component_name);
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
        [PythonAPI("Returns what is plotted right now as [field_name, component_name, step_id, increment_id].",
                   Returns = "StringList")]
        public StringListAPI get_result_field()
        {
            string[] data = Resolve().GetResultFieldData();
            return new StringListAPI(data == null ? new string[0] : data);
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
        public string save_image(string file_name = null, object transparent_background = null,
                                 object magnification = null)
        {
            if (file_name != null && file_name.Trim().Length == 0)
                throw new PythonAPIException("The argument 'file_name' must not be empty.");
            //
            bool transparent = transparent_background != null &&
                               PythonValueConverter.ToBool(transparent_background, "transparent_background");
            int scale = magnification == null ? 1 : PythonValueConverter.ToInt(magnification, "magnification");
            if (scale < 1 || scale > MaxMagnification)
                throw new PythonAPIException("The argument 'magnification' must be from 1 to " +
                                             MaxMagnification + ".");
            //
            return Resolve().SaveImage(file_name == null ? null : file_name.Trim(), transparent, scale);
        }
        [PythonAPI("Returns the standard orientation names set_orientation accepts.", Returns = "StringList")]
        public StringListAPI get_orientation_names()
        {
            return new StringListAPI(Orientations);
        }
        //
        private IPythonViewGateway Resolve()
        {
            if (_gateway == null)
                throw new PythonAPIException("The view is not available in this Python API context.");
            //
            return _gateway;
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
