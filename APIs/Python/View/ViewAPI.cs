// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

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
        private static readonly string[] Views = { "geometry", "femodel", "results" };
        private static readonly string[] Orientations = { "Front", "Back", "Top", "Bottom", "Left", "Right",
                                                          "Isometric", "IsometricNegative", "Normal", "Vertical" };
        private static readonly string[] ResultTypes = { "Undeformed", "Deformed", "ColorContours", "VectorPlot",
                                                         "TensorPlot" };
        private readonly IPythonViewGateway _gateway;


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


        // Constructors
        internal ViewAPI(IPythonViewGateway gateway)
        {
            _gateway = gateway;
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
        [PythonAPI("Writes a PNG of the viewport as it looks right now and returns the path. A file name that " +
                   "is not given means a file in the temporary folder.",
                   Arguments = "file_name: str = None", Returns = "str",
                   Example = "path = pmx.view.save_image('C:/temp/model.png')")]
        public string save_image(string file_name = null)
        {
            if (file_name != null && file_name.Trim().Length == 0)
                throw new PythonAPIException("The argument 'file_name' must not be empty.");
            //
            return Resolve().SaveImage(file_name == null ? null : file_name.Trim());
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
