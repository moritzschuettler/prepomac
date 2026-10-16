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
        private static readonly string[] ImportFileExtensions = new string[] { ".stp", ".step", ".igs", ".iges", ".brep",
                                                                               ".stl", ".unv", ".vol", ".inp", ".mesh",
                                                                               ".obj", ".pmp" };
        private readonly ModelResolver _modelResolver;
        private readonly IPythonModelGateway _modelGateway;
        private readonly ResultResolver _resultResolver;
        private readonly IPythonJobGateway _jobGateway;
        private readonly PythonFileNameResolver _fileNameResolver;


        // Constructors
        public DocumentService(ModelResolver modelResolver, IPythonModelGateway modelGateway,
                               ResultResolver resultResolver, IPythonJobGateway jobGateway,
                               PythonFileNameResolver fileNameResolver)
        {
            _modelResolver = modelResolver;
            _modelGateway = modelGateway;
            _resultResolver = resultResolver;
            _jobGateway = jobGateway;
            _fileNameResolver = fileNameResolver;
        }


        // Methods
        public void NewModel(string modelSpace, string unitSystem, bool discardChanges)
        {
            ModelSpaceEnum modelSpaceValue = ToModelSpace(modelSpace);
            UnitSystemType unitSystemValue = ToUnitSystem(unitSystem);
            //
            EnsureCommandsAvailable();
            EnsureModelCanBeReplaced("Creating a new model", discardChanges);
            _modelGateway.NewModel(modelSpaceValue, unitSystemValue);
        }
        public void OpenFile(string fileName, bool discardChanges)
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
            EnsureModelCanBeReplaced("Opening a file", discardChanges);
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
        // The import dialog asks for the model space and the unit system of a model that has none before it imports,
        // which a script cannot answer, and imports a file of any other type than .inp whole although only its
        // materials were asked for
        public void ImportFile(string fileName, bool onlyMaterials)
        {
            string fullFileName = ResolveExistingFileName(fileName, "file_name");
            string extension = Path.GetExtension(fullFileName).ToLowerInvariant();
            if (Array.IndexOf(ImportFileExtensions, extension) < 0)
                throw new PythonAPIException("The argument 'file_name' must name a file of a type the import " +
                                             "takes: " + string.Join(", ", ImportFileExtensions) + ", but '" +
                                             fileName + "' was given.");
            if (onlyMaterials && extension != ".inp")
                throw new PythonAPIException("The argument 'only_materials' applies only to an .inp file, but '" +
                                             fileName + "' was given.");
            EnsureCommandsAvailable();
            FeModel model = _modelResolver.GetModel();
            if (model.Properties.ModelSpace == ModelSpaceEnum.Undefined ||
                model.UnitSystem.UnitSystemType == UnitSystemType.Undefined)
                throw new PythonAPIException("There is no model to import into. First create a new model. Create " +
                                             "it with pmx.new_model(model_space, unit_system), or set " +
                                             "pmx.model.properties.model_space and unit_system.");
            if (extension == ".pmp") ParameterService.CheckParametersFile(model, fullFileName);
            //
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
                           : PythonValueConverter.ToFiniteDouble(edgeThicknessRatio, "edge_thickness_ratio");
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
                EnsureCadPartNames(names, formatValue);
            if (formatValue == GeometryExportFormatEnum.Step || formatValue == GeometryExportFormatEnum.StepAssembly ||
                formatValue == GeometryExportFormatEnum.BrepAssembly)
                EnsureWorkDirectoryExists();
            if (formatValue == GeometryExportFormatEnum.Mmg) EnsureMmgPartNames(_modelResolver.GetGeometry(), names);
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
            if (formatValue == MeshExportFormatEnum.Mmg) EnsureMmgPartNames(_modelResolver.GetMesh(), names);
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
        // A value that is not given is the one the view shows, which the gateway reads
        public void ExportResults(object partNames, string fileName, string format, string resultName, object stepId,
                                  object incrementId, object deformationFactor)
        {
            string storedResultName = null;
            FeMesh mesh;
            if (resultName == null) mesh = GetCurrentResultMesh();
            else
            {
                storedResultName = _resultResolver == null ? null : _resultResolver.FindResultName(resultName);
                if (storedResultName == null)
                    throw new PythonAPIException("The result '" + resultName + "' is not loaded. The argument " +
                                                 "'result_name' takes the name pmx.results.get_names() lists, or " +
                                                 "the file name of one loaded result.");
                mesh = _resultResolver.ResolveResult(storedResultName).Mesh;
            }
            string[] names = PythonModelArgumentCheck.ToStoredPartNames(mesh, ToPartNames(partNames), "result");
            int? stepIdValue = stepId == null ? (int?)null : PythonValueConverter.ToInt(stepId, "step_id");
            int? incrementIdValue = incrementId == null ? (int?)null
                                                        : PythonValueConverter.ToInt(incrementId, "increment_id");
            float? factor = null;
            if (deformationFactor != null)
            {
                double factorValue = PythonValueConverter.ToFiniteDouble(deformationFactor, "deformation_factor");
                if (float.IsInfinity((float)factorValue))
                    throw new PythonAPIException("The argument 'deformation_factor' must be within the range of a " +
                                                 "single precision float, about 3.4e38: " +
                                                 factorValue.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                                                 ".");
                factor = (float)factorValue;
            }
            string fullFileName = ResolveOutputFileName(fileName, "file_name");
            ResultsExportFormatEnum formatValue;
            if (format != null) formatValue = PythonValueConverter.ToEnum<ResultsExportFormatEnum>(format, "format");
            else if (!ExportFormatTools.TryGetResultsFormat(fullFileName, out formatValue))
                throw new PythonAPIException("The extension of the file '" + fullFileName + "' names no results export " +
                                             "format. Use .inp or .stl, or give the argument 'format'.");
            //
            EnsureCommandsAvailable();
            _modelGateway.ExportResults(names, fullFileName, formatValue, storedResultName, stepIdValue, incrementIdValue,
                                        factor);
        }
        //
        public string GetModelType()
        {
            return _modelResolver.GetModel().Properties.ModelType.ToString();
        }
        // A value the model type does not use is hidden by the dialog and refused by its setter, so it reads None.
        // Regenerate with file dialogs and a batch regeneration store a full path, which the setter refuses, so a
        // path in the work directory reads as the name the setter takes; a path anywhere else, or one whose file
        // name the setter would refuse, names a file no setter can, and reads as it is stored
        public string GetGlobalResultsFileName()
        {
            ModelProperties properties = _modelResolver.GetModel().Properties;
            if (properties.ModelType != ModelType.Submodel) return null;
            //
            string fileName = properties.GlobalResultsFileName;
            string workDirectory = _jobGateway == null ? null : _jobGateway.GetWorkDirectory();
            if (fileName == null || workDirectory == null) return fileName;
            try
            {
                if (!Path.IsPathRooted(fileName)) return fileName;
                //
                char[] separators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
                string fileDirectory = Path.GetFullPath(Path.GetDirectoryName(fileName)).TrimEnd(separators);
                string fullWorkDirectory = Path.GetFullPath(workDirectory).TrimEnd(separators);
                string bareFileName = Path.GetFileName(fileName);
                if (string.Equals(fileDirectory, fullWorkDirectory, StringComparison.OrdinalIgnoreCase) &&
                    bareFileName.Length > 0 && IsGlobalResultsFileNameWithoutPath(bareFileName))
                    return bareFileName;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException ||
                                       ex is PathTooLongException)
            {
                return fileName;
            }
            return fileName;
        }
        public string GetSlipWearResults()
        {
            ModelProperties properties = GetSlipWearProperties();
            return properties == null ? null : properties.SlipWearResults.ToString();
        }
        public object GetBdmRemeshing()
        {
            ModelProperties properties = GetSlipWearProperties();
            return properties == null ? null : (object)properties.BdmRemeshing;
        }
        public object GetNumberOfCycles()
        {
            ModelProperties properties = GetSlipWearProperties();
            return properties == null ? null : (object)(int)properties.NumberOfCycles.Value;
        }
        public object GetCyclesIncrement()
        {
            ModelProperties properties = GetSlipWearProperties();
            return properties == null ? null : (object)(int)properties.CyclesIncrement.Value;
        }
        public object GetNumberOfSmoothingSteps()
        {
            ModelProperties properties = GetSlipWearProperties();
            return properties == null ? null : (object)(int)properties.NumOfSmoothingSteps.Value;
        }
        private ModelProperties GetSlipWearProperties()
        {
            ModelProperties properties = _modelResolver.GetModel().Properties;
            return properties.ModelType == ModelType.SlipWearModel ? properties : null;
        }
        public Dictionary<string, string> GetPropertyEquations()
        {
            ModelProperties properties = _modelResolver.GetModel().Properties;
            Dictionary<string, string> equations = new Dictionary<string, string>();
            // Only the model type that uses the counts takes them back
            if (properties.ModelType != ModelType.SlipWearModel) return equations;
            //
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
        public void UpdateProperties(string name, string modelSpace, string modelType, string globalResultsFileName,
                                     string slipWearResults, object numberOfCycles, object cyclesIncrement,
                                     object numOfSmoothingSteps, object bdmRemeshing, object absoluteZero,
                                     object stefanBoltzmann, object newtonGravity, bool clearUndefinedConstants)
        {
            FeModel model = _modelResolver.GetModel();
            ModelProperties properties = model.Properties.DeepClone();
            ModelType oldModelType = properties.ModelType;
            if (name != null) ValidateModelName(name);
            if (modelSpace != null) properties.ModelSpace = ToReplacedModelSpace(properties.ModelSpace, modelSpace);
            if (modelType != null)
                properties.ModelType = PythonValueConverter.ToEnum<ModelType>(modelType, "model_type");
            RefuseUnusedProperties(properties.ModelType, globalResultsFileName, slipWearResults, numberOfCycles,
                                   cyclesIncrement, numOfSmoothingSteps, bdmRemeshing);
            if (globalResultsFileName != null)
            {
                ValidateGlobalResultsFileName(globalResultsFileName);
                properties.GlobalResultsFileName = globalResultsFileName;
            }
            if (slipWearResults != null)
                properties.SlipWearResults = PythonValueConverter.ToEnum<SlipWearResultsEnum>(slipWearResults,
                                                                                              "slip_wear_results");
            if (numberOfCycles != null)
                PythonModelArgumentCheck.SetEquation(properties.NumberOfCycles,
                    ToCount(properties.NumberOfCycles, numberOfCycles, 1, "number_of_cycles"), "number_of_cycles");
            if (cyclesIncrement != null)
                PythonModelArgumentCheck.SetEquation(properties.CyclesIncrement,
                    ToCount(properties.CyclesIncrement, cyclesIncrement, 1, "cycles_increment"), "cycles_increment");
            if (numOfSmoothingSteps != null)
                PythonModelArgumentCheck.SetEquation(properties.NumOfSmoothingSteps,
                    ToCount(properties.NumOfSmoothingSteps, numOfSmoothingSteps, 0, "number_of_smoothing_steps"),
                    "number_of_smoothing_steps");
            if (bdmRemeshing != null)
                properties.BdmRemeshing = PythonValueConverter.ToBool(bdmRemeshing, "bdm_remeshing");
            properties.AbsoluteZero = ToConstant(absoluteZero, properties.AbsoluteZero, clearUndefinedConstants,
                                                 "absolute_zero");
            properties.StefanBoltzmann = ToConstant(stefanBoltzmann, properties.StefanBoltzmann,
                                                    clearUndefinedConstants, "stefan_boltzmann");
            properties.NewtonGravity = ToConstant(newtonGravity, properties.NewtonGravity, clearUndefinedConstants,
                                                  "newton_gravity");
            bool givesGlobalResultsFile = globalResultsFileName != null ||
                                          (properties.ModelType == ModelType.Submodel &&
                                           oldModelType != ModelType.Submodel);
            EnsureDefined(properties, givesGlobalResultsFile);
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
            EnsureDefined(properties, false);
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
        // The model space of a new model, set as the new model dialog sets it, which leaves the element types of an
        // existing mesh as they are; the dialog asks for it only while the model space or the unit system is
        // undefined, so a defined model space of a model with a geometry or a mesh is changed by the replace. The
        // values of a model are numbers in its unit system, which no dialog converts, so a defined unit system is
        // changed only while the model holds nothing or its model space is undefined, when the dialog asks for both
        public void SetModelSpaceAndUnitSystem(string modelSpace, string unitSystem)
        {
            FeModel model = _modelResolver.GetModel();
            ModelSpaceEnum modelSpaceValue = modelSpace == null ? model.Properties.ModelSpace : ToModelSpace(modelSpace);
            ModelSpaceEnum current = model.Properties.ModelSpace;
            if (modelSpaceValue != current && current != ModelSpaceEnum.Undefined &&
                (HasNodes(model.Geometry) || HasNodes(model.Mesh)))
                throw new PythonAPIException("The property 'model_space' sets the model space of a new model, and " +
                                             "this model already has a geometry or a mesh. Change it with " +
                                             "pmx.model.properties.replace(model_space=...), which updates the " +
                                             "element types.");
            if (modelSpace != null && modelSpaceValue.IsTwoD() &&
                (IsNotInXyPlane(model.Geometry) || IsNotInXyPlane(model.Mesh)))
                throw new PythonAPIException("Use of the 2D model space is not possible. The geometry or the mesh " +
                                             "do not contain 2D geometry in x-y plane. The property 'model_space' " +
                                             "takes " + modelSpaceValue + " only for a model in the x-y plane.");
            UnitSystemType unitSystemValue = unitSystem == null ? model.UnitSystem.UnitSystemType
                                                                : ToUnitSystem(unitSystem);
            UnitSystemType currentUnitSystem = model.UnitSystem.UnitSystemType;
            if (unitSystemValue != currentUnitSystem && currentUnitSystem != UnitSystemType.Undefined &&
                current != ModelSpaceEnum.Undefined && HasContent(model))
                throw new PythonAPIException("The property 'unit_system' sets the unit system of a new model, and " +
                                             "this model already holds a geometry, a mesh or model items, whose " +
                                             "values would be read in " + unitSystemValue + " instead of " +
                                             currentUnitSystem + " without being converted. Start a model in that unit " +
                                             "system with pmx.new_model(model_space, unit_system) instead.");
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
            ValidateModelName(name);
            //
            ModelProperties properties = _modelResolver.GetModel().Properties.DeepClone();
            EnsureDefined(properties, false);
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceModelProperties(name, properties);
        }
        //
        // The dialog takes any model name, so a journal of it may carry one the item names refuse; only a padded
        // name is refused, as it is for every other name
        private static void ValidateModelName(string name)
        {
            if (name == null || name.Trim().Length == 0)
                throw new PythonAPIException("The argument 'name' must not be empty.");
            if (name.Trim().Length != name.Length)
                throw new PythonAPIException("The argument 'name' must not start or end with a space: '" + name +
                                             "'.");
        }
        // The dialog shows the submodel file and the slip wear values only for the model type that uses them, so
        // a value given for a model type that does not use it is refused rather than stored unseen
        private static void RefuseUnusedProperties(ModelType modelType, string globalResultsFileName,
                                                   string slipWearResults, object numberOfCycles,
                                                   object cyclesIncrement, object numOfSmoothingSteps,
                                                   object bdmRemeshing)
        {
            if (modelType != ModelType.Submodel && globalResultsFileName != null)
                throw new PythonAPIException("The argument 'global_results_file_name' applies only to a Submodel, " +
                                             "but the model type is " + modelType + ".");
            if (modelType == ModelType.SlipWearModel) return;
            //
            string[] argumentNames = new string[] { "slip_wear_results", "number_of_cycles", "cycles_increment",
                                                    "number_of_smoothing_steps", "bdm_remeshing" };
            object[] values = new object[] { slipWearResults, numberOfCycles, cyclesIncrement, numOfSmoothingSteps,
                                             bdmRemeshing };
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != null)
                    throw new PythonAPIException("The argument '" + argumentNames[i] + "' applies only to a " +
                                                 "SlipWearModel, but the model type is " + modelType + ".");
            }
        }
        // The dialog stores the global results file name without its path and refuses, in these words, a name it
        // cannot write into the input file
        private static void ValidateGlobalResultsFileName(string globalResultsFileName)
        {
            if (globalResultsFileName.Length == 0)
                throw new PythonAPIException("The argument 'global_results_file_name' must not be empty; a " +
                                             "submodel needs an existing global results file.");
            if (!IsGlobalResultsFileNameWithoutPath(globalResultsFileName))
                throw new PythonAPIException("Enter the global results file name (.frd) without path. The results " +
                                             "file name must not contain any special characters. Give in " +
                                             "'global_results_file_name' the name of a file in the work directory: '" +
                                             globalResultsFileName + "'.");
        }
        private static bool IsGlobalResultsFileNameWithoutPath(string globalResultsFileName)
        {
            return !globalResultsFileName.Contains(" ") && !globalResultsFileName.Contains(":") &&
                   !globalResultsFileName.Contains("\\") && !globalResultsFileName.Contains("/") &&
                   globalResultsFileName.ToUTF8() == globalResultsFileName;
        }
        // The dialog refuses on OK a submodel whose global results file it cannot find in the work directory and a
        // slip wear model whose cycles do not divide into increments, in these words, and judges the properties it
        // ends with. A setter or a rename has no dialog OK and keeps the stored file name, so the file is looked up
        // only when the call gives it or makes the model a submodel. A journal of the dialog gives it on every replace
        // of a Submodel, as the dialog checks it on every OK, so its replay needs the file in its own work directory
        private void EnsureDefined(ModelProperties properties, bool checkGlobalResultsFile)
        {
            if (properties.ModelType == ModelType.Submodel)
            {
                if (!checkGlobalResultsFile) return;
                //
                string fileName = properties.GlobalResultsFileName == null ? "" : properties.GlobalResultsFileName;
                string workDirectory = _jobGateway == null ? null : _jobGateway.GetWorkDirectory();
                string fullFileName;
                try
                {
                    fullFileName = Path.Combine(workDirectory == null ? "" : workDirectory, fileName);
                }
                catch (ArgumentException ex)
                {
                    throw new PythonAPIException("The argument 'global_results_file_name' is invalid: " + ex.Message);
                }
                if (!File.Exists(fullFileName))
                    throw new PythonAPIException("The selected global results file " + fullFileName + " does not " +
                                                 "exist. Give in 'global_results_file_name' the name of a file in " +
                                                 "the work directory.");
            }
            else if (properties.ModelType == ModelType.SlipWearModel &&
                     properties.NumberOfCycles.Value % properties.CyclesIncrement.Value != 0)
            {
                throw new PythonAPIException("The number of slip wear cycles must be a multiple of the cycles " +
                                             "increment. Give a 'number_of_cycles' that 'cycles_increment' divides.");
            }
        }
        private static bool HasNodes(FeMesh mesh)
        {
            return mesh != null && mesh.Nodes.Count > 0;
        }
        private static bool IsNotInXyPlane(FeMesh mesh)
        {
            return HasNodes(mesh) && !mesh.BoundingBox.Is2D();
        }
        private static bool HasContent(FeModel model)
        {
            return HasNodes(model.Geometry) || HasNodes(model.Mesh) || model.Materials.Count > 0 ||
                   model.Sections.Count > 0 || model.MaterialOrientations.Count > 0 || model.Constraints.Count > 0 ||
                   model.SurfaceInteractions.Count > 0 ||
                   model.ContactPairs.Count > 0 || model.Distributions.Count > 0 || model.Amplitudes.Count > 0 ||
                   model.InitialConditions.Count > 0 || model.StepCollection.StepsList.Count > 0;
        }
        // The dialog changes the model space only within the 2D spaces or within 3D, because the element types
        // of the mesh follow it
        private static ModelSpaceEnum ToReplacedModelSpace(ModelSpaceEnum current, string modelSpace)
        {
            ModelSpaceEnum value = ToModelSpace(modelSpace);
            if (value.IsTwoD() != current.IsTwoD())
                throw new PythonAPIException("The argument 'model_space' cannot change the model space from " +
                                             current + " to " + value + ": a replace changes it only between the " +
                                             "2D spaces PlaneStress, PlaneStrain and Axisymmetric, as the dialog " +
                                             "does.");
            return value;
        }
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
        // The export refuses these parts only after it was recorded, so they are refused here in its words
        private void EnsureCadPartNames(string[] names, GeometryExportFormatEnum format)
        {
            FeMesh geometry = _modelResolver.GetGeometry();
            foreach (string name in names)
            {
                BasePart part;
                if (!geometry.Parts.TryGetValue(name, out part))
                    throw new PythonAPIException("The geometry part '" + name + "' does not exist.");
                GeometryPart geometryPart = part as GeometryPart;
                if (geometryPart == null || !geometryPart.IsCADPart)
                    throw new PythonAPIException("The part " + name + " is not a CAD part and cannot be exported to a " +
                                                 ExportFormatTools.GetExtension(format) + " file. The argument " +
                                                 "'part_names' must name parts with CAD data for this format.");
            }
        }
        private static void EnsureMmgPartNames(FeMesh mesh, string[] names)
        {
            foreach (string name in names)
            {
                BasePart part = mesh.Parts[name];
                if (part.PartType != PartType.Solid && part.PartType != PartType.Shell)
                    throw new PythonAPIException("The part " + name + " is of the type " + part.PartType + " and " +
                                                 "cannot be exported to an Mmg .mesh file. Only solid and shell parts " +
                                                 "can. The argument 'part_names' must name only those for this format.");
            }
        }
        // The modelling kernel writes a STEP file or an assembly in the work directory, and the export only reports
        // a missing one after it was recorded
        private void EnsureWorkDirectoryExists()
        {
            if (_jobGateway == null) return;
            //
            string workDirectory = _jobGateway.GetWorkDirectory();
            if (workDirectory == null || !Directory.Exists(workDirectory))
                throw new PythonAPIException("The work directory does not exist. Set an existing work directory in " +
                                             "the application settings before this export.");
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
        private FeMesh GetCurrentResultMesh()
        {
            FeMesh mesh = _resultResolver == null ? null : _resultResolver.GetCurrentResultMeshOrNull();
            if (mesh == null)
                throw new PythonAPIException("No current result is selected.");
            //
            return mesh;
        }
        private string ResolveExistingFileName(string fileName, string argumentName)
        {
            string fullFileName = ResolveOutputFileName(fileName, argumentName);
            if (!File.Exists(fullFileName))
                throw new PythonAPIException("The file '" + fullFileName + "' does not exist.");
            //
            return fullFileName;
        }
        private string ResolveOutputFileName(string fileName, string argumentName)
        {
            return _fileNameResolver.GetOutputFileName(fileName, argumentName);
        }
        private void EnsureModelCanBeReplaced(string operation, bool discardChanges)
        {
            JobService.EnsureNoAnalysisRunning(_jobGateway, operation);
            // Replacing the model rebuilds the command collection, which the running script belongs to
            if (_modelGateway.IsInsideRecordedScript())
                throw new PythonAPIException(operation + " replaces the command history and is not available inside " +
                                             "a recorded script. Use the -f command line switch to open the file " +
                                             "before the script runs, or call this from the Python console.");
            // The menu asks the user whether to save first, which a script cannot answer for them
            if (!discardChanges && _modelGateway.HasUnsavedChangesOfUser())
                throw new PythonAPIException(operation + " replaces the model, which has unsaved changes. Save it " +
                                             "with pmx.save(file_name) first, or pass discard_changes=True once " +
                                             "the user has agreed to lose them.");
        }
        private void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}
