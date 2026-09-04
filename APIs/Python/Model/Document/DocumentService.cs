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
using System;
using System.IO;

namespace APIs.Python.Model
{
    internal class DocumentService
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly IPythonModelGateway _modelGateway;


        // Constructors
        public DocumentService(ModelResolver modelResolver, IPythonModelGateway modelGateway)
        {
            _modelResolver = modelResolver;
            _modelGateway = modelGateway;
        }


        // Methods
        public void NewModel(string modelSpace, string unitSystem)
        {
            ModelSpaceEnum modelSpaceValue = PythonValueConverter.ToEnum<ModelSpaceEnum>(modelSpace, "model_space");
            UnitSystemType unitSystemValue = PythonValueConverter.ToEnum<UnitSystemType>(unitSystem, "unit_system");
            //
            EnsureCommandsAvailable();
            EnsureModelCanBeReplaced("Creating a new model");
            _modelGateway.NewModel(modelSpaceValue, unitSystemValue);
        }
        public void OpenFile(string fileName)
        {
            string fullFileName = ResolveExistingFileName(fileName, "file_name");
            //
            EnsureCommandsAvailable();
            EnsureModelCanBeReplaced("Opening a file");
            _modelGateway.OpenFile(fullFileName);
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
        public void ExportToStl(object partNames, string fileName)
        {
            string[] names = ResolvePartNames(partNames);
            EnsureCommandsAvailable();
            _modelGateway.ExportToStl(names, ResolveOutputFileName(fileName, "file_name"));
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
        public void ExportPartsAsGmshMesh(string fileName)
        {
            EnsureCommandsAvailable();
            _modelGateway.ExportPartsAsGmshMesh(ResolveOutputFileName(fileName, "file_name"));
        }
        public void ExportPartsAsMmgMesh(object partNames, string fileName, bool combine)
        {
            string[] names = ResolvePartNames(partNames);
            EnsureCommandsAvailable();
            _modelGateway.ExportPartsAsMmgMesh(names, ResolveOutputFileName(fileName, "file_name"), combine);
        }
        public void ExportCadPartsAsStep(object partNames, string fileName)
        {
            string[] names = ResolvePartNames(partNames);
            EnsureCommandsAvailable();
            _modelGateway.ExportCadPartsAsStep(names, ResolveOutputFileName(fileName, "file_name"));
        }
        public void ExportCadPartsAsBrep(object partNames, string fileName)
        {
            string[] names = ResolvePartNames(partNames);
            EnsureCommandsAvailable();
            _modelGateway.ExportCadPartsAsBrep(names, ResolveOutputFileName(fileName, "file_name"));
        }
        public void ExportDeformedPartsToCalculix(object partNames, string fileName)
        {
            string[] names = ResolvePartNames(partNames);
            EnsureCommandsAvailable();
            _modelGateway.ExportDeformedPartsToCalculix(names, ResolveOutputFileName(fileName, "file_name"));
        }
        //
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
            ModelSpaceEnum modelSpaceValue = modelSpace == null
                                             ? model.Properties.ModelSpace
                                             : PythonValueConverter.ToEnum<ModelSpaceEnum>(modelSpace, "model_space");
            UnitSystemType unitSystemValue = unitSystem == null
                                             ? model.UnitSystem.UnitSystemType
                                             : PythonValueConverter.ToEnum<UnitSystemType>(unitSystem, "unit_system");
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
        private string[] ResolvePartNames(object partNames)
        {
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(partNames, "part_names");
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument 'part_names' must not be empty.");
            //
            return names;
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
