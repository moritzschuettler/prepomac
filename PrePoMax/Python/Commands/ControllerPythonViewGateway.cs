// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using APIs.Python;
using CaeGlobals;
using CaeResults;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace PrePoMax.Python
{
    // The view side of the API. None of it goes through the command history, because none of it
    // changes the model: the same reasoning that keeps a camera angle out of the .pmx keeps it out of
    // Regenerate.
    //
    // Every call reaches the window, and a scripted call arrives on a thread that does not own it, so
    // the form methods used here are the marshalling ones.
    internal class ControllerPythonViewGateway : IPythonViewGateway
    {
        // Variables
        private const bool Animate = false;
        private readonly Controller _controller;


        // Constructors
        public ControllerPythonViewGateway(Controller controller)
        {
            _controller = controller;
        }


        // Methods
        public string GetCurrentView()
        {
            return ToViewName(_controller.CurrentView);
        }
        public void SetCurrentView(string view)
        {
            _controller.CurrentView = ToView(view);
        }
        public void SetStandardView(string orientation)
        {
            ResolveForm().SetStandardView(orientation, Animate);
        }
        public void ZoomToFit()
        {
            ResolveForm().SetZoomToFit(Animate);
        }
        public double[] GetViewParameters()
        {
            double[] position;
            double[] focalPoint;
            double[] viewUp;
            double parallelScale;
            ResolveForm().GetViewParameters(out position, out focalPoint, out viewUp, out parallelScale);
            if (position == null || focalPoint == null || viewUp == null) return new double[0];
            //
            double[] parameters = new double[10];
            Array.Copy(position, 0, parameters, 0, 3);
            Array.Copy(focalPoint, 0, parameters, 3, 3);
            Array.Copy(viewUp, 0, parameters, 6, 3);
            parameters[9] = parallelScale;
            return parameters;
        }
        public void SetViewParameters(double[] parameters)
        {
            double[] position = new double[] { parameters[0], parameters[1], parameters[2] };
            double[] focalPoint = new double[] { parameters[3], parameters[4], parameters[5] };
            double[] viewUp = new double[] { parameters[6], parameters[7], parameters[8] };
            ResolveForm().SetViewParameters(Animate, position, focalPoint, viewUp, parameters[9]);
        }
        public string GetResultType()
        {
            return _controller.ViewResultsType.ToString();
        }
        public void SetResultType(string resultType)
        {
            RequireResults();
            ResolveForm().SetViewResultsType((ViewResultsTypeEnum)Enum.Parse(typeof(ViewResultsTypeEnum),
                                                                            resultType, true));
        }
        public string[] GetResultFieldData()
        {
            FieldData fieldData = _controller.CurrentFieldData;
            if (fieldData == null) return new string[0];
            //
            return new string[] { fieldData.Name == null ? "" : fieldData.Name,
                                  fieldData.Component == null ? "" : fieldData.Component,
                                  fieldData.StepId.ToString(CultureInfo.InvariantCulture),
                                  fieldData.StepIncrementId.ToString(CultureInfo.InvariantCulture) };
        }
        public void SetResultFieldData(string fieldName, string componentName)
        {
            RequireResults();
            FeResults result = _controller.CurrentResult;
            string[] components = result.GetFieldComponentNames(fieldName);
            if (components == null || components.Length == 0)
                throw new PythonAPIException("The current result has no field named '" + fieldName + "'. The " +
                                             "fields it has are " + GetFieldNames(result) + ".");
            //
            string component = componentName == null ? components[0] : FindComponent(components, componentName);
            if (component == null)
                throw new PythonAPIException("The field '" + fieldName + "' has no component named '" +
                                             componentName + "'. Its components are " +
                                             string.Join(", ", components) + ".");
            //
            ResolveForm().SetFieldData(fieldName, component);
        }
        // The form falls back to the default increment for a combination its list does not hold, so
        // the combination is checked here: a script that asked for an increment the result does not
        // have is told, rather than shown a different one
        public void SetResultIncrement(int stepId, int incrementId)
        {
            RequireResults();
            Dictionary<int, int[]> existing = _controller.CurrentResult.GetAllExistingIncrementIds();
            int[] incrementIds;
            if (!existing.TryGetValue(stepId, out incrementIds))
                throw new PythonAPIException("The current result has no step with the ID " + stepId +
                                             ". Its step IDs are " + Join(existing.Keys) + ".");
            if (Array.IndexOf(incrementIds, incrementId) < 0)
                throw new PythonAPIException("The step " + stepId + " of the current result has no increment " +
                                             "with the ID " + incrementId + ". Its increment IDs are " +
                                             Join(incrementIds) + ".");
            //
            ResolveForm().SetStepAndIncrementIds(stepId, incrementId);
        }
        public string SaveImage(string fileName)
        {
            string path = fileName == null ? GetTemporaryFileName() : fileName;
            if (System.IO.Path.GetExtension(path).ToLower() != ".png")
                throw new PythonAPIException("A viewport image is written as a .png file.");
            //
            string directory = System.IO.Path.GetDirectoryName(path);
            if (directory != null && directory.Length > 0 && !System.IO.Directory.Exists(directory))
                throw new PythonAPIException("The directory " + directory + " does not exist.");
            //
            ResolveForm().SaveAutomationScreenshot(path);
            return path;
        }
        //
        private FrmMain ResolveForm()
        {
            FrmMain form = _controller.Form;
            if (form == null) throw new PythonAPIException("The view is not available in this context.");
            //
            return form;
        }
        private void RequireResults()
        {
            if (_controller.CurrentResult == null)
                throw new PythonAPIException("There is no open result, so there is nothing to plot. Open one " +
                                             "with pmx.jobs[name].open_results() or pmx.results.set_current(name).");
        }
        private static string FindComponent(string[] components, string componentName)
        {
            foreach (string component in components)
            {
                if (string.Equals(component, componentName, StringComparison.OrdinalIgnoreCase)) return component;
            }
            //
            return null;
        }
        private static string Join(IEnumerable<int> ids)
        {
            List<string> text = new List<string>();
            foreach (int id in ids) text.Add(id.ToString(CultureInfo.InvariantCulture));
            return string.Join(", ", text.ToArray());
        }
        private static string GetFieldNames(FeResults result)
        {
            string[] names = result.GetAllFieldNames();
            if (names == null || names.Length == 0) return "none";
            //
            return string.Join(", ", names);
        }
        private static string GetTemporaryFileName()
        {
            string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PrePoMax");
            if (!System.IO.Directory.Exists(directory)) System.IO.Directory.CreateDirectory(directory);
            return System.IO.Path.Combine(directory, "viewport_" + Guid.NewGuid().ToString("N") + ".png");
        }
        private static string ToViewName(ViewGeometryModelResults view)
        {
            if (view == ViewGeometryModelResults.Geometry) return "geometry";
            if (view == ViewGeometryModelResults.Model) return "femodel";
            return "results";
        }
        private static ViewGeometryModelResults ToView(string view)
        {
            if (view == "geometry") return ViewGeometryModelResults.Geometry;
            if (view == "femodel") return ViewGeometryModelResults.Model;
            return ViewGeometryModelResults.Results;
        }
    }
}
