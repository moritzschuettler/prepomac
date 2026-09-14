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
    // The arrow annotations of the displayed view - the labels a report picture carries. They are view
    // state: each of the three views keeps its own, the results view keeps one set per result, and
    // they are saved in the .pmx without going through the command history, the same as in the
    // application, where a label is placed by a query rather than by a command.
    public class ViewAnnotationsAPI : BaseAPI
    {
        // Variables
        private readonly IPythonViewGateway _gateway;


        // Constructors
        internal ViewAnnotationsAPI(IPythonViewGateway gateway)
        {
            _gateway = gateway;
        }


        // Methods
        [PythonAPI("Returns the names of the annotations of the displayed view. In the results view these are " +
                   "the annotations of the current result.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            string[] names = Resolve().GetAnnotationNames();
            return new StringListAPI(names == null ? new string[0] : names);
        }
        [PythonAPI("Adds a text label with an arrow pointing at a point given in model coordinates. The name " +
                   "must be free; Min_Annotation, Max_Annotation and Measure_Annotation are reserved.",
                   Arguments = "name: str, text: str, anchor_point: list[float]", Returns = "None",
                   Example = "pmx.view.annotations.add_text('Load', 'Pressure 2 MPa', [0, 0, 10])")]
        public void add_text(string name, string text, object anchor_point)
        {
            if (string.IsNullOrEmpty(name) || name.Trim().Length == 0)
                throw new PythonAPIException("The argument 'name' must not be empty.");
            if (text == null) throw new PythonAPIException("The argument 'text' must not be None.");
            //
            double[] anchor = PythonDoubleListConverter.ToDoubleArray(anchor_point, "anchor_point");
            if (anchor == null || anchor.Length != 3)
                throw new PythonAPIException("The argument 'anchor_point' must contain 3 coordinates.");
            foreach (double coordinate in anchor)
            {
                if (double.IsNaN(coordinate) || double.IsInfinity(coordinate))
                    throw new PythonAPIException("The argument 'anchor_point' must contain finite coordinates.");
            }
            //
            Resolve().AddTextAnnotation(name.Trim(), text, anchor);
        }
        [PythonAPI("Adds the annotation of a node of the displayed mesh, the one a node query places: its ID, " +
                   "its coordinates and, in the results view, the plotted value. Returns the name it was given.",
                   Arguments = "node_id: int", Returns = "str",
                   Example = "name = pmx.view.annotations.add_node(15)")]
        public string add_node(object node_id)
        {
            return Resolve().AddNodeAnnotation(PythonValueConverter.ToInt(node_id, "node_id"));
        }
        [PythonAPI("Adds the annotation of an element of the displayed mesh, the one an element query places. " +
                   "Returns the name it was given.",
                   Arguments = "element_id: int", Returns = "str",
                   Example = "name = pmx.view.annotations.add_element(1)")]
        public string add_element(object element_id)
        {
            return Resolve().AddElementAnnotation(PythonValueConverter.ToInt(element_id, "element_id"));
        }
        [PythonAPI("Removes the named annotations of the displayed view. A single name or a list of names.",
                   Arguments = "names: str | list[str]", Returns = "None",
                   Example = "pmx.view.annotations.remove(['Load', 'R_Annotation-1'])")]
        public void remove(object names)
        {
            string[] annotationNames = PythonStringListConverter.ToStringArrayOrSingle(names, "names");
            if (annotationNames.Length == 0) throw new PythonAPIException("The argument 'names' must not be empty.");
            //
            Resolve().RemoveAnnotations(annotationNames);
        }
        [PythonAPI("Shows or hides the labels at the minimum and the maximum of the plotted result, the way " +
                   "the post-processing settings do. Needs an open result.",
                   Arguments = "show_min: bool, show_max: bool", Returns = "None",
                   Example = "pmx.view.annotations.show_min_max(False, True)")]
        public void show_min_max(object show_min, object show_max)
        {
            bool showMin = PythonValueConverter.ToBool(show_min, "show_min");
            bool showMax = PythonValueConverter.ToBool(show_max, "show_max");
            Resolve().ShowMinMaxAnnotations(showMin, showMax);
        }
        [PythonAPI("Returns whether the minimum and the maximum labels are shown, as [show_min, show_max]. Keep " +
                   "them to put the user setting back after a picture.", Returns = "list[bool]",
                   Example = "shown = list(pmx.view.annotations.get_min_max())")]
        public bool[] get_min_max()
        {
            return Resolve().GetMinMaxAnnotations();
        }
        //
        private IPythonViewGateway Resolve()
        {
            if (_gateway == null)
                throw new PythonAPIException("The view is not available in this Python API context.");
            //
            return _gateway;
        }
        public override string ToString()
        {
            return "ViewAnnotations(count=" + (_gateway == null ? 0 : _gateway.GetAnnotationNames().Length) + ")";
        }
    }
}
