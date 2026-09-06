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
using System.Linq;

namespace APIs.Python.Model
{
    internal class ParameterService
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly IPythonModelGateway _modelGateway;


        // Constructors
        public ParameterService(ModelResolver modelResolver, IPythonModelGateway modelGateway)
        {
            _modelResolver = modelResolver;
            _modelGateway = modelGateway;
        }


        // Methods
        public string[] GetNames()
        {
            return _modelResolver.GetModel().Parameters.Keys.ToArray();
        }
        public EquationParameter GetParameter(string parameterName)
        {
            if (parameterName == null) return null;
            //
            EquationParameter parameter;
            if (!_modelResolver.GetModel().Parameters.TryGetValue(parameterName, out parameter)) return null;
            //
            return parameter;
        }
        public EquationParameter ResolveParameter(string parameterName)
        {
            EquationParameter parameter = GetParameter(parameterName);
            if (parameter == null)
                throw new PythonAPIException("The parameter '" + parameterName + "' does not exist.");
            //
            return parameter;
        }
        public void Add(string name, object value)
        {
            EquationParameter parameter = Build(name, value, null);
            //
            EnsureCommandsAvailable();
            _modelGateway.AddParameter(parameter);
        }
        public void Replace(string oldName, string name, object value)
        {
            EquationParameter existing = ResolveParameter(oldName);
            string newName = name == null ? oldName : name;
            // A value that is not given is kept, so a rename alone keeps the expression
            EquationParameter parameter;
            if (value == null)
            {
                ValidateName(newName, oldName);
                parameter = new EquationParameter();
                parameter.Name = newName;
                parameter.EquationStr = new EquationString(existing.EquationStr.Equation);
            }
            else parameter = Build(newName, value, oldName);
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceParameter(oldName, parameter);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.RemoveParameters(names);
        }
        public void Rename(EquationParameter parameter, string newName)
        {
            // The parameters have no rename command, so the replacement carries the new name
            Replace(parameter.Name, newName, parameter.EquationStr.Equation);
        }
        public void ImportFromFile(string fileName)
        {
            string fullFileName = ResolveFileName(fileName);
            if (!File.Exists(fullFileName))
                throw new PythonAPIException("The parameters file '" + fullFileName + "' does not exist.");
            //
            EnsureCommandsAvailable();
            _modelGateway.ImportParameters(fullFileName);
        }
        public void ExportToFile(string fileName)
        {
            string fullFileName = ResolveFileName(fileName);
            string directoryName = Path.GetDirectoryName(fullFileName);
            if (directoryName != null && directoryName.Length > 0 && !Directory.Exists(directoryName))
                throw new PythonAPIException("The directory '" + directoryName + "' does not exist.");
            //
            EnsureCommandsAvailable();
            _modelGateway.ExportParameters(fullFileName);
        }
        //
        private EquationParameter Build(string name, object value, string allowedExistingName)
        {
            ValidateName(name, allowedExistingName);
            EquationString equation = PythonValueConverter.ToEquationString(value, "value");
            //
            EquationParameter parameter = new EquationParameter();
            parameter.Name = name;
            parameter.EquationStr = equation;
            //
            return parameter;
        }
        private void ValidateName(string name, string allowedExistingName)
        {
            if (name == null || name.Trim().Length == 0)
                throw new PythonAPIException("The argument 'name' must not be empty.");
            // The parameter name is used inside equations, so it must not carry the minus character
            if (name.Contains("-"))
                throw new PythonAPIException("The parameter name must not contain the hyphen character.");
            if (allowedExistingName != null && string.Equals(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetNames())
            {
                if (string.Equals(name, existingName))
                    throw new PythonAPIException("The parameter '" + name + "' already exists.");
            }
        }
        private static string ResolveFileName(string fileName)
        {
            if (fileName == null || fileName.Trim().Length == 0)
                throw new PythonAPIException("The argument 'file_name' must not be empty.");
            //
            try
            {
                return Path.GetFullPath(fileName);
            }
            catch (Exception ex)
            {
                throw new PythonAPIException("The argument 'file_name' is invalid: " + ex.Message);
            }
        }
        private void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}
