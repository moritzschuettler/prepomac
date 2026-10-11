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
using CaeMesh;
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
        public void Rotate(double azimuthAngle, double elevationAngle, double rollAngle)
        {
            ResolveForm().Rotate(azimuthAngle, elevationAngle, rollAngle);
        }
        public string GetResultType()
        {
            return _controller.ViewResultsType.ToString();
        }
        public string GetDeformationType()
        {
            return ResolveForm().GetDeformationType().ToString();
        }
        public void SetDeformationType(string deformationType)
        {
            RequireResults();
            DeformationScaleFactorTypeEnum type =
                (DeformationScaleFactorTypeEnum)Enum.Parse(typeof(DeformationScaleFactorTypeEnum), deformationType, true);
            ResolveForm().SetDeformationType(type);
        }
        public double GetDeformationFactor()
        {
            return ResolveForm().GetDeformationFactor();
        }
        public void SetDeformationFactor(double deformationFactor)
        {
            RequireResults();
            ResolveForm().SetDeformationFactor((float)deformationFactor);
        }
        public string GetUndeformedModelType()
        {
            return _controller.GetUndeformedModelType().ToString();
        }
        // The toolbar clears the undeformed model for the Undeformed and the Deformed plots, so a copy
        // asked for with one of those would be discarded; the script is told instead
        public void SetUndeformedModelType(string undeformedModelType)
        {
            RequireResults();
            UndeformedModelTypeEnum type =
                (UndeformedModelTypeEnum)Enum.Parse(typeof(UndeformedModelTypeEnum), undeformedModelType, true);
            ViewResultsTypeEnum resultType = _controller.ViewResultsType;
            if (type != UndeformedModelTypeEnum.None && (resultType == ViewResultsTypeEnum.Undeformed ||
                                                          resultType == ViewResultsTypeEnum.Deformed))
                throw new PythonAPIException("The undeformed model is drawn only behind a ColorContours, VectorPlot or " +
                                             "TensorPlot result_type; the current one is " + resultType + ".");
            //
            ResolveForm().SetResultsUndeformedModelType(type);
        }
        // The settings dialog applies a legend range by assigning the whole settings container, which
        // stores the range under the plotted field component and redraws; the same assignment is made
        // here, on the user interface thread the dialog makes it from
        public void SetLegendRange(double minValue, double maxValue)
        {
            RequireResultPlot();
            ResolveForm().InvokeIfRequired(() =>
            {
                SettingsContainer settings = _controller.Settings;
                settings.Legend.ColorSpectrum.MinMaxType = vtkControl.vtkColorSpectrumMinMaxType.Manual;
                settings.Legend.ColorSpectrum.SetUserMinMax(minValue, maxValue);
                _controller.Settings = settings;
            });
        }
        public void ResetLegendRange()
        {
            RequireResultPlot();
            ResolveForm().InvokeIfRequired(() =>
            {
                SettingsContainer settings = _controller.Settings;
                settings.Legend.ColorSpectrum.MinMaxType = vtkControl.vtkColorSpectrumMinMaxType.Automatic;
                _controller.Settings = settings;
            });
        }
        public string[] GetAnnotationNames()
        {
            return _controller.Annotations.GetCurrentAnnotationNames();
        }
        public void AddTextAnnotation(string name, string text, double[] anchorPoint)
        {
            RequireAnnotationView();
            string reservedName = FindReservedAnnotationName(name);
            if (reservedName != null)
                throw new PythonAPIException("The name '" + name + "' is reserved for the annotation " + reservedName +
                                             " of the application.");
            string existingName = FindAnnotationName(name);
            if (existingName != null)
                throw new PythonAPIException("An annotation named '" + existingName + "' already exists in the " +
                                             "displayed view.");
            //
            try
            {
                _controller.Annotations.AddTextAnnotation(name, text, anchorPoint);
            }
            catch (CaeException ex)
            {
                throw new PythonAPIException("The argument 'name' is not a valid annotation name: " + ex.Message);
            }
        }
        public string AddNodeAnnotation(int nodeId)
        {
            FeMesh mesh = RequireDisplayedMesh();
            if (!mesh.Nodes.ContainsKey(nodeId))
                throw new PythonAPIException("The displayed mesh has no node with the ID " + nodeId + ".");
            //
            return _controller.Annotations.AddNodeAnnotation(nodeId);
        }
        public string AddElementAnnotation(int elementId)
        {
            FeMesh mesh = RequireDisplayedMesh();
            if (!mesh.Elements.ContainsKey(elementId))
                throw new PythonAPIException("The displayed mesh has no element with the ID " + elementId + ".");
            //
            return _controller.Annotations.AddElementAnnotation(elementId);
        }
        public void RemoveAnnotations(string[] names)
        {
            RequireAnnotationView();
            List<string> storedNames = new List<string>();
            foreach (string name in names)
            {
                if (FindReservedAnnotationName(name) != null)
                    throw new PythonAPIException("The annotation '" + name + "' belongs to the application and cannot " +
                                                 "be removed.");
                string storedName = FindAnnotationName(name);
                if (storedName == null)
                    throw new PythonAPIException("The displayed view has no annotation named '" + name + "'. Its " +
                                                 "annotations are " + JoinNames(GetAnnotationNames()) + ".");
                if (!storedNames.Contains(storedName)) storedNames.Add(storedName);
            }
            //
            foreach (string storedName in storedNames) _controller.Annotations.RemoveCurrentArrowAnnotation(storedName);
        }
        public bool[] GetMinMaxAnnotations()
        {
            SettingsContainer settings = _controller.Settings;
            return new bool[] { settings.Post.ShowMinValueLocation, settings.Post.ShowMaxValueLocation };
        }
        public void ShowMinMaxAnnotations(bool showMin, bool showMax)
        {
            RequireResults();
            ResolveForm().InvokeIfRequired(() =>
            {
                SettingsContainer settings = _controller.Settings;
                settings.Post.ShowMinValueLocation = showMin;
                settings.Post.ShowMaxValueLocation = showMax;
                _controller.Settings = settings;
            });
        }
        // The toolbar changes the plot type only while the results view is displayed, and the plotted
        // component decides between a vector and a tensor plot, so a type that would not be shown is refused
        public void SetResultType(string resultType)
        {
            RequireResults();
            if (_controller.CurrentView != ViewGeometryModelResults.Results)
                throw new PythonAPIException("The argument 'result_type' belongs to the results view, so it must be " +
                                             "displayed: pmx.view.current = 'results'.");
            //
            ViewResultsTypeEnum type = (ViewResultsTypeEnum)Enum.Parse(typeof(ViewResultsTypeEnum), resultType, true);
            if (type == ViewResultsTypeEnum.VectorPlot || type == ViewResultsTypeEnum.TensorPlot)
            {
                FieldData fieldData = _controller.CurrentFieldData;
                ViewResultsTypeEnum drawnAs = _controller.CurrentResult.IsTensorPlotComponent(fieldData)
                                              ? ViewResultsTypeEnum.TensorPlot : ViewResultsTypeEnum.VectorPlot;
                if (type != drawnAs)
                {
                    string plotted = fieldData == null ? "nothing" : "the component " + fieldData.Component +
                                                                     " of the field " + fieldData.Name;
                    throw new PythonAPIException("The argument 'result_type' " + type + " cannot draw " + plotted +
                                                 ", which is drawn as a " + drawnAs + ". The plotted component picks " +
                                                 "VectorPlot or TensorPlot; set the field first with " +
                                                 "pmx.view.set_result_field.");
                }
            }
            ResolveForm().SetViewResultsType(type);
        }
        public bool GetResultFieldData(out string fieldName, out string componentName, out int stepId,
                                       out int incrementId)
        {
            FieldData fieldData = _controller.CurrentFieldData;
            fieldName = fieldData == null || fieldData.Name == null ? "" : fieldData.Name;
            componentName = fieldData == null || fieldData.Component == null ? "" : fieldData.Component;
            stepId = fieldData == null ? 0 : fieldData.StepId;
            incrementId = fieldData == null ? 0 : fieldData.StepIncrementId;
            return fieldData != null;
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
        // A relative name is placed where the screenshot tool of the automation server places it, so the
        // same name means the same file whichever front door asked for it
        public string SaveImage(string fileName, bool transparentBackground, int magnification)
        {
            string path;
            string extension;
            string directory;
            try
            {
                if (fileName == null) path = GetTemporaryFileName();
                else if (System.IO.Path.IsPathRooted(fileName)) path = fileName;
                else path = System.IO.Path.Combine(GetTemporaryDirectory(), fileName);
                System.IO.Path.GetFullPath(path);
                extension = System.IO.Path.GetExtension(path);
                directory = System.IO.Path.GetDirectoryName(path);
            }
            catch (Exception exception) when (IsPathException(exception))
            {
                throw new PythonAPIException("The argument 'file_name' is not a valid file name: '" + fileName + "'. " +
                                             exception.Message);
            }
            if (extension.ToLower() != ".png")
                throw new PythonAPIException("The argument 'file_name' must name a .png file, as a viewport image is " +
                                             "written as one.");
            //
            if (directory != null && directory.Length > 0 && !System.IO.Directory.Exists(directory))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                catch (Exception exception) when (IsPathException(exception) || exception is System.IO.IOException ||
                                                  exception is UnauthorizedAccessException)
                {
                    throw new PythonAPIException("The argument 'file_name' names the directory " + directory +
                                                 ", which does not exist and could not be created: " +
                                                 exception.Message);
                }
            }
            //
            ResolveForm().SaveAutomationScreenshot(path, transparentBackground, magnification);
            return path;
        }
        public bool IsExplodedViewActive()
        {
            return _controller.IsExplodedViewActive();
        }
        // The dialog applies its parameters to the displayed view, so the view the script left displayed
        // is the one that is pulled apart
        public void ApplyExplodedView(string method, double scaleFactor, double magnification, string direction,
                                      bool sequential, double tolerance, double[] center)
        {
            if (_controller.DisplayedMesh == null)
                throw new PythonAPIException("The displayed view has no parts to pull apart.");
            //
            ExplodedViewParameters parameters = new ExplodedViewParameters();
            parameters.Method = (ExplodedViewMethodEnum)Enum.Parse(typeof(ExplodedViewMethodEnum), method, true);
            parameters.Direction = (ExplodedViewDirectionEnum)Enum.Parse(typeof(ExplodedViewDirectionEnum), direction,
                                                                         true);
            parameters.ScaleFactor = scaleFactor;
            parameters.Magnification = magnification;
            parameters.Sequential = sequential;
            parameters.Tolerance = tolerance;
            if (center != null) parameters.Center = center;
            //
            ResolveForm().InvokeIfRequired(() => { _controller.ApplyExplodedView(parameters); });
        }
        public void RemoveExplodedView()
        {
            ResolveForm().InvokeIfRequired(() => { _controller.RemoveExplodedView(true); });
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
                                             "with pmx.jobs.open_results(name) or pmx.results.current = name.");
        }
        // A legend range is stored under the plotted field component of the results view, so it is
        // refused where the settings container would not store it
        private void RequireResultPlot()
        {
            RequireResults();
            if (_controller.CurrentView != ViewGeometryModelResults.Results || _controller.CurrentFieldData == null)
                throw new PythonAPIException("The legend range belongs to the plotted field, so the results view must " +
                                             "be displayed: pmx.view.current = 'results'.");
        }
        // The results view keeps its annotations per result, so without one there is nothing to keep them
        private void RequireAnnotationView()
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Results) RequireResults();
        }
        private FeMesh RequireDisplayedMesh()
        {
            RequireAnnotationView();
            FeMesh mesh = _controller.DisplayedMesh;
            if (mesh == null)
                throw new PythonAPIException("The displayed view has no mesh to annotate.");
            //
            return mesh;
        }
        private string FindAnnotationName(string name)
        {
            string[] existingNames = _controller.Annotations.GetCurrentAnnotationNames();
            foreach (string existingName in existingNames)
            {
                if (existingName == name) return existingName;
            }
            foreach (string existingName in existingNames)
            {
                if (string.Equals(existingName, name, StringComparison.OrdinalIgnoreCase)) return existingName;
            }
            //
            return null;
        }
        private static string FindReservedAnnotationName(string name)
        {
            string[] reservedNames = new string[] { AnnotationContainer.MeasureAnnotationName,
                                                    AnnotationContainer.MinAnnotationName,
                                                    AnnotationContainer.MaxAnnotationName };
            foreach (string reservedName in reservedNames)
            {
                if (string.Equals(reservedName, name, StringComparison.OrdinalIgnoreCase)) return reservedName;
            }
            //
            return null;
        }
        private static string JoinNames(string[] names)
        {
            if (names == null || names.Length == 0) return "none";
            //
            return string.Join(", ", names);
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
            return System.IO.Path.Combine(GetTemporaryDirectory(), "viewport_" + Guid.NewGuid().ToString("N") + ".png");
        }
        private static string GetTemporaryDirectory()
        {
            return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PrePoMax");
        }
        private static bool IsPathException(Exception exception)
        {
            return exception is ArgumentException || exception is NotSupportedException ||
                   exception is System.IO.PathTooLongException || exception is System.Security.SecurityException;
        }
        public static string ToViewName(ViewGeometryModelResults view)
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
