// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using CaeModel;
using CaeResults;
using FileInOut.Output;
using System;
using System.Collections.Generic;
using System.IO;

namespace APIs.Python.Model
{
    internal class DocumentService
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly IPythonModelGateway _modelGateway;
        private readonly ResultResolver _resultResolver;


        // Constructors
        public DocumentService(ModelResolver modelResolver, IPythonModelGateway modelGateway,
                               ResultResolver resultResolver)
        {
            _modelResolver = modelResolver;
            _modelGateway = modelGateway;
            _resultResolver = resultResolver;
        }


        // Methods
        public void NewModel(string modelSpace, string unitSystem)
        {
            ModelSpaceEnum modelSpaceValue = ToModelSpace(modelSpace);
            UnitSystemType unitSystemValue = ToUnitSystem(unitSystem);
            //
            EnsureCommandsAvailable();
            EnsureModelCanBeReplaced("Creating a new model");
            _modelGateway.NewModel(modelSpaceValue, unitSystemValue);
        }
        public void OpenFile(string fileName)
        {
            string fullFileName = ResolveExistingFileName(fileName, "file_name");
            string extension = Path.GetExtension(fullFileName).ToLowerInvariant();
            if (IsResultFileExtension(extension))
                throw new PythonAPIException("The file '" + fileName + "' is a result file, which pmx.results.open " +
                                             "opens beside the current model.");
            if (extension != ".pmx" && extension != ".pmh")
                throw new PythonAPIException("The argument 'file_name' must name a .pmx or .pmh file.");
            //
            EnsureCommandsAvailable();
            EnsureModelCanBeReplaced("Opening a file");
            _modelGateway.OpenFile(fullFileName);
        }
        // A result file is opened beside the model, so it does not replace the command history and is recorded
        // as the same open command the application records
        public void OpenResultFile(string fileName)
        {
            string fullFileName = ResolveExistingFileName(fileName, "file_name");
            string extension = Path.GetExtension(fullFileName).ToLowerInvariant();
            if (extension == ".pmx" || extension == ".pmh")
                throw new PythonAPIException("The file '" + fileName + "' is a model file, which replaces the " +
                                             "command history and is opened by pmx.open.");
            if (!IsResultFileExtension(extension))
                throw new PythonAPIException("The argument 'file_name' must name a .frd, .dat, .odb or .foam file.");
            //
            EnsureCommandsAvailable();
            _modelGateway.OpenFile(fullFileName);
        }
        private static bool IsResultFileExtension(string extension)
        {
            return extension == ".frd" || extension == ".dat" || extension == ".odb" || extension == ".foam";
        }
        public void ImportFile(string fileName, bool onlyMaterials)
        {
            string fullFileName = ResolveExistingFileName(fileName, "file_name");
            //
            EnsureCommandsAvailable();
            _modelGateway.ImportFile(fullFileName, onlyMaterials);
        }
        public void SaveToPmx(string fileName)
        {
            string fullFileName = ResolveOutputFileName(fileName, "file_name");
            //
            EnsureCommandsAvailable();
            _modelGateway.SaveToPmx(fullFileName);
        }
        public void ExportToCalculix(string fileName)
        {
            EnsureCommandsAvailable();
            _modelGateway.ExportToCalculix(ResolveOutputFileName(fileName, "file_name"));
        }
        public void ExportToAbaqus(string fileName)
        {
            EnsureCommandsAvailable();
            _modelGateway.ExportToAbaqus(ResolveOutputFileName(fileName, "file_name"));
        }
        public void ExportTo3mf(string fileName, object edgeThicknessRatio)
        {
            double ratio = edgeThicknessRatio == null
                           ? 1
                           : PythonValueConverter.ToDouble(edgeThicknessRatio, "edge_thickness_ratio");
            if (ratio <= 0)
                throw new PythonAPIException("The argument 'edge_thickness_ratio' must be larger than zero.");
            //
            EnsureCommandsAvailable();
            _modelGateway.ExportTo3mf(ResolveOutputFileName(fileName, "file_name"), ratio);
        }
        // The extension of the file names the format, unless the caller gives it; .inp is CalculiX unless said otherwise
        public void ExportGeometry(object partNames, string fileName, string format)
        {
            string[] names = ResolveGeometryPartNames(partNames);
            string fullFileName = ResolveOutputFileName(fileName, "file_name");
            GeometryExportFormatEnum formatValue;
            if (format != null) formatValue = PythonValueConverter.ToEnum<GeometryExportFormatEnum>(format, "format");
            else if (!ExportFormatTools.TryGetGeometryFormat(fullFileName, out formatValue))
                throw new PythonAPIException("The extension of the file '" + fullFileName + "' names no geometry export " +
                                             "format. Use .stp, .brep, .stl or .mesh, or give the argument 'format'.");
            if (formatValue == GeometryExportFormatEnum.Step || formatValue == GeometryExportFormatEnum.Brep ||
                formatValue == GeometryExportFormatEnum.StepAssembly ||
                formatValue == GeometryExportFormatEnum.BrepAssembly)
                EnsureCadPartNames(names);
            //
            EnsureCommandsAvailable();
            _modelGateway.ExportGeometry(names, fullFileName, formatValue);
        }
        public void ExportMesh(object partNames, string fileName, string format)
        {
            string[] names = ResolveModelPartNames(partNames);
            string fullFileName = ResolveOutputFileName(fileName, "file_name");
            MeshExportFormatEnum formatValue;
            if (format != null) formatValue = PythonValueConverter.ToEnum<MeshExportFormatEnum>(format, "format");
            else if (!ExportFormatTools.TryGetMeshFormat(fullFileName, out formatValue))
                throw new PythonAPIException("The extension of the file '" + fullFileName + "' names no mesh export " +
                                             "format. Use .inp, .msh, .mesh or .stl, or give the argument 'format'.");
            //
            EnsureCommandsAvailable();
            _modelGateway.ExportMesh(names, fullFileName, formatValue);
        }
        public void ExportModel(string fileName, string format)
        {
            string fullFileName = ResolveOutputFileName(fileName, "file_name");
            ModelExportFormatEnum formatValue;
            if (format != null) formatValue = PythonValueConverter.ToEnum<ModelExportFormatEnum>(format, "format");
            else if (!ExportFormatTools.TryGetModelFormat(fullFileName, out formatValue))
                throw new PythonAPIException("The extension of the file '" + fullFileName + "' names no model export " +
                                             "format. Use .inp, or give the argument 'format'.");
            //
            EnsureCommandsAvailable();
            if (formatValue == ModelExportFormatEnum.Abaqus) _modelGateway.ExportToAbaqus(fullFileName);
            else _modelGateway.ExportToCalculix(fullFileName);
        }
        public void ExportResults(object partNames, string fileName, string format)
        {
            string[] names = ResolveResultPartNames(partNames);
            string fullFileName = ResolveOutputFileName(fileName, "file_name");
            ResultsExportFormatEnum formatValue;
            if (format != null) formatValue = PythonValueConverter.ToEnum<ResultsExportFormatEnum>(format, "format");
            else if (!ExportFormatTools.TryGetResultsFormat(fullFileName, out formatValue))
                throw new PythonAPIException("The extension of the file '" + fullFileName + "' names no results export " +
                                             "format. Use .inp or .stl, or give the argument 'format'.");
            //
            EnsureCommandsAvailable();
            _modelGateway.ExportResults(names, fullFileName, formatValue);
        }
        //
        public string GetModelType()
        {
            return _modelResolver.GetModel().Properties.ModelType.ToString();
        }
        public string GetGlobalResultsFileName()
        {
            return _modelResolver.GetModel().Properties.GlobalResultsFileName;
        }
        public string GetSlipWearResults()
        {
            return _modelResolver.GetModel().Properties.SlipWearResults.ToString();
        }
        public bool GetBdmRemeshing()
        {
            return _modelResolver.GetModel().Properties.BdmRemeshing;
        }
        public int GetNumberOfCycles()
        {
            return (int)_modelResolver.GetModel().Properties.NumberOfCycles.Value;
        }
        public int GetCyclesIncrement()
        {
            return (int)_modelResolver.GetModel().Properties.CyclesIncrement.Value;
        }
        public int GetNumberOfSmoothingSteps()
        {
            return (int)_modelResolver.GetModel().Properties.NumOfSmoothingSteps.Value;
        }
        public Dictionary<string, string> GetPropertyEquations()
        {
            ModelProperties properties = _modelResolver.GetModel().Properties;
            Dictionary<string, string> equations = new Dictionary<string, string>();
            AddEquation(equations, "number_of_cycles", properties.NumberOfCycles);
            AddEquation(equations, "cycles_increment", properties.CyclesIncrement);
            AddEquation(equations, "number_of_smoothing_steps", properties.NumOfSmoothingSteps);
            return equations;
        }
        // The model raises a count below its minimum to the minimum, so the value the script gave is refused instead
        private static EquationString ToCount(EquationContainer container, object value, int minimum,
                                              string argumentName)
        {
            EquationString equation = PythonValueConverter.ToEquationString(value, argumentName);
            if (PythonModelArgumentCheck.Evaluate(container, equation, argumentName) < minimum)
                throw new PythonAPIException("The argument '" + argumentName + "' must be " + minimum + " or larger.");
            //
            return equation;
        }
        // A plain value is read by its property as a number, so only an expression is an equation
        private static void AddEquation(Dictionary<string, string> equations, string name, EquationContainer equation)
        {
            if (equation != null && equation.IsEquation()) equations[name] = equation.String;
        }
        // An undefined physical constant is stored as an infinity, and reads back as None
        public object GetAbsoluteZero()
        {
            return FromUndefined(_modelResolver.GetModel().Properties.AbsoluteZero);
        }
        public object GetStefanBoltzmann()
        {
            return FromUndefined(_modelResolver.GetModel().Properties.StefanBoltzmann);
        }
        public object GetNewtonGravity()
        {
            return FromUndefined(_modelResolver.GetModel().Properties.NewtonGravity);
        }
        public void UpdateProperties(string name, string modelType, string globalResultsFileName,
                                     string slipWearResults, object numberOfCycles, object cyclesIncrement,
                                     object numOfSmoothingSteps, object bdmRemeshing, object absoluteZero,
                                     object stefanBoltzmann, object newtonGravity, bool clearUndefinedConstants)
        {
            FeModel model = _modelResolver.GetModel();
            ModelProperties properties = model.Properties.DeepClone();
            if (name != null && name.Trim().Length == 0)
                throw new PythonAPIException("The argument 'name' must not be empty.");
            if (modelType != null)
                properties.ModelType = PythonValueConverter.ToEnum<ModelType>(modelType, "model_type");
            if (globalResultsFileName != null)
            {
                if (globalResultsFileName.Trim().Length == 0)
                    throw new PythonAPIException("The argument 'global_results_file_name' must not be empty.");
                properties.GlobalResultsFileName = globalResultsFileName;
            }
            if (slipWearResults != null)
                properties.SlipWearResults = PythonValueConverter.ToEnum<SlipWearResultsEnum>(slipWearResults,
                                                                                              "slip_wear_results");
            if (numberOfCycles != null)
                properties.NumberOfCycles.Equation = ToCount(properties.NumberOfCycles, numberOfCycles, 1,
                                                             "number_of_cycles");
            if (cyclesIncrement != null)
                properties.CyclesIncrement.Equation = ToCount(properties.CyclesIncrement, cyclesIncrement, 1,
                                                              "cycles_increment");
            if (numOfSmoothingSteps != null)
                properties.NumOfSmoothingSteps.Equation = ToCount(properties.NumOfSmoothingSteps, numOfSmoothingSteps, 0,
                                                                  "number_of_smoothing_steps");
            if (bdmRemeshing != null)
                properties.BdmRemeshing = PythonValueConverter.ToBool(bdmRemeshing, "bdm_remeshing");
            properties.AbsoluteZero = ToConstant(absoluteZero, properties.AbsoluteZero, clearUndefinedConstants,
                                                 "absolute_zero");
            properties.StefanBoltzmann = ToConstant(stefanBoltzmann, properties.StefanBoltzmann,
                                                    clearUndefinedConstants, "stefan_boltzmann");
            properties.NewtonGravity = ToConstant(newtonGravity, properties.NewtonGravity, clearUndefinedConstants,
                                                  "newton_gravity");
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceModelProperties(name == null ? model.Name : name, properties);
        }
        // A property assignment sets exactly the constant it names, so None makes it undefined, the state the
        // getter reports as None
        public void SetPhysicalConstant(string argumentName, object value)
        {
            FeModel model = _modelResolver.GetModel();
            ModelProperties properties = model.Properties.DeepClone();
            double constant = value == null ? double.PositiveInfinity
                                            : PythonValueConverter.ToFiniteDouble(value, argumentName);
            if (argumentName == "absolute_zero") properties.AbsoluteZero = constant;
            else if (argumentName == "stefan_boltzmann") properties.StefanBoltzmann = constant;
            else if (argumentName == "newton_gravity") properties.NewtonGravity = constant;
            else throw new ArgumentException("The physical constant '" + argumentName + "' is unknown.");
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceModelProperties(model.Name, properties);
        }
        public string GetModelSpace()
        {
            return _modelResolver.GetModel().Properties.ModelSpace.ToString();
        }
        public string GetUnitSystem()
        {
            return _modelResolver.GetModel().UnitSystem.UnitSystemType.ToString();
        }
        public void SetModelSpaceAndUnitSystem(string modelSpace, string unitSystem)
        {
            FeModel model = _modelResolver.GetModel();
            ModelSpaceEnum modelSpaceValue = modelSpace == null ? model.Properties.ModelSpace : ToModelSpace(modelSpace);
            UnitSystemType unitSystemValue = unitSystem == null ? model.UnitSystem.UnitSystemType
                                                                : ToUnitSystem(unitSystem);
            //
            EnsureCommandsAvailable();
            _modelGateway.SetNewModelProperties(modelSpaceValue, unitSystemValue);
        }
        public string GetModelName()
        {
            return _modelResolver.GetModel().Name;
        }
        public void SetModelName(string name)
        {
            if (name == null || name.Trim().Length == 0)
                throw new PythonAPIException("The argument 'name' must not be empty.");
            //
            ModelProperties properties = _modelResolver.GetModel().Properties.DeepClone();
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceModelProperties(name, properties);
        }
        //
        // Undefined is the state of a model nothing has set yet, not a value a model can be given
        private static ModelSpaceEnum ToModelSpace(string modelSpace)
        {
            ModelSpaceEnum value = PythonValueConverter.ToEnum<ModelSpaceEnum>(modelSpace, "model_space");
            if (value == ModelSpaceEnum.Undefined)
                throw new PythonAPIException("The argument 'model_space' must name a model space; 'Undefined' " +
                                             "cannot be set.");
            return value;
        }
        private static UnitSystemType ToUnitSystem(string unitSystem)
        {
            UnitSystemType value = PythonValueConverter.ToEnum<UnitSystemType>(unitSystem, "unit_system");
            if (value == UnitSystemType.Undefined)
                throw new PythonAPIException("The argument 'unit_system' must name a unit system; 'Undefined' " +
                                             "cannot be set.");
            return value;
        }
        private static object FromUndefined(double value)
        {
            return double.IsInfinity(value) ? null : (object)value;
        }
        private static double ToConstant(object value, double current, bool clearWhenNotGiven, string argumentName)
        {
            // Clearing a physical constant is the explicit request the replace method carries, because a
            // value that is simply not given is kept
            if (value == null) return clearWhenNotGiven ? double.PositiveInfinity : current;
            //
            return PythonValueConverter.ToFiniteDouble(value, argumentName);
        }
        // Every part export has its own source: the displayed mesh, the CAD geometry or the current
        // result. A name that exists in the model mesh is no evidence that it exists in that source.
        private string[] ToPartNames(object partNames)
        {
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(partNames, "part_names");
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument 'part_names' must not be empty.");
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == null || names[i].Trim().Length == 0)
                    throw new PythonAPIException("The argument 'part_names' contains an empty name.");
            }
            //
            return names;
        }
        private void EnsureCadPartNames(string[] names)
        {
            FeMesh geometry = _modelResolver.GetGeometry();
            foreach (string name in names)
            {
                BasePart part;
                if (!geometry.Parts.TryGetValue(name, out part))
                    throw new PythonAPIException("The geometry part '" + name + "' does not exist.");
                GeometryPart geometryPart = part as GeometryPart;
                if (geometryPart == null || geometryPart.CADFileData == null)
                    throw new PythonAPIException("The geometry part '" + name + "' is not a CAD part.");
            }
        }
        private string[] ResolveGeometryPartNames(object partNames)
        {
            string[] names = ToPartNames(partNames);
            return PythonModelArgumentCheck.ToStoredPartNames(_modelResolver.GetGeometry(), names, "geometry");
        }
        private string[] ResolveModelPartNames(object partNames)
        {
            string[] names = ToPartNames(partNames);
            return PythonModelArgumentCheck.ToStoredPartNames(_modelResolver.GetMesh(), names, "mesh");
        }
        private string[] ResolveResultPartNames(object partNames)
        {
            string[] names = ToPartNames(partNames);
            FeMesh mesh = _resultResolver == null ? null : _resultResolver.GetCurrentResultMeshOrNull();
            if (mesh == null)
                throw new PythonAPIException("No current result is selected.");
            //
            return PythonModelArgumentCheck.ToStoredPartNames(mesh, names, "result");
        }
        private static string ResolveExistingFileName(string fileName, string argumentName)
        {
            string fullFileName = ResolveOutputFileName(fileName, argumentName);
            if (!File.Exists(fullFileName))
                throw new PythonAPIException("The file '" + fullFileName + "' does not exist.");
            //
            return fullFileName;
        }
        private static string ResolveOutputFileName(string fileName, string argumentName)
        {
            if (fileName == null || fileName.Trim().Length == 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be empty.");
            //
            string fullFileName;
            try
            {
                fullFileName = Path.GetFullPath(fileName);
            }
            catch (Exception ex)
            {
                throw new PythonAPIException("The argument '" + argumentName + "' is invalid: " + ex.Message);
            }
            //
            string directoryName = Path.GetDirectoryName(fullFileName);
            if (directoryName != null && directoryName.Length > 0 && !Directory.Exists(directoryName))
                throw new PythonAPIException("The directory '" + directoryName + "' does not exist.");
            //
            return fullFileName;
        }
        private void EnsureModelCanBeReplaced(string operation)
        {
            // Replacing the model rebuilds the command collection, which the running script belongs to
            if (_modelGateway.IsInsideRecordedScript())
                throw new PythonAPIException(operation + " replaces the command history and is not available inside " +
                                             "a recorded script. Use the -f command line switch to open the file " +
                                             "before the script runs, or call this from the Python console.");
        }
        private void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}
