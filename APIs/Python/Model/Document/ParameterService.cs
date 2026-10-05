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
using System.Collections.Generic;
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
        public void Add(string name, object value, string description)
        {
            EquationParameter parameter = Build(name, value, null);
            parameter.Description = description;
            //
            EnsureCommandsAvailable();
            _modelGateway.AddParameter(parameter);
        }
        public void Replace(string oldName, string name, object value, string description)
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
            // A description that is not given is kept, and an empty one clears it
            parameter.Description = description == null ? existing.Description : description;
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceParameter(oldName, parameter);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.RemoveParameters(names);
        }
        public void SetAll(object names, object values, object descriptions)
        {
            string[] parameterNames = PythonStringListConverter.ToStringArrayOrSingle(names, "names");
            if (parameterNames == null || parameterNames.Length == 0)
                throw new PythonAPIException("The argument 'names' must not be empty.");
            EquationString[] equations = PythonValueConverter.ToEquationArray(values, "values", parameterNames.Length);
            string[] parameterDescriptions = null;
            if (descriptions != null)
            {
                parameterDescriptions = PythonStringListConverter.ToStringArrayOrSingle(descriptions, "descriptions");
                if (parameterDescriptions.Length != parameterNames.Length)
                    throw new PythonAPIException("The argument 'descriptions' must contain " + parameterNames.Length +
                                                 " items, one for every name.");
            }
            //
            HashSet<string> usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < parameterNames.Length; i++)
            {
                ValidateNameFormat(parameterNames[i], "names");
                parameterNames[i] = parameterNames[i].Trim();
                if (!usedNames.Add(parameterNames[i]))
                    throw new PythonAPIException("The parameter '" + parameterNames[i] + "' is given more than once.");
            }
            // A parameter may use a parameter that is given after it, so all of the values are resolved before
            // the parameters are built: EquationContainer evaluates an equation before it stores it
            OrderedDictionary<string, object> existingParameters = MyNCalc.ExistingParameters;
            EquationParameter[] parameters = new EquationParameter[parameterNames.Length];
            try
            {
                List<KeyValuePair<string, string>> nameEquations = new List<KeyValuePair<string, string>>();
                for (int i = 0; i < parameterNames.Length; i++)
                    nameEquations.Add(new KeyValuePair<string, string>(parameterNames[i], equations[i].Equation));
                //
                Dictionary<string, double> resolvedValues;
                Dictionary<string, string> errors =
                    EquationParameterResolver.Evaluate(nameEquations, _modelResolver.GetModel().GetPropertyParameters(),
                                                       out resolvedValues);
                if (errors.Count > 0) throw new PythonAPIException(GetFirstError(parameterNames, errors));
                //
                for (int i = 0; i < parameterNames.Length; i++)
                {
                    parameters[i] = new EquationParameter();
                    parameters[i].Name = parameterNames[i];
                    parameters[i].EquationStr = equations[i];
                    if (parameterDescriptions != null && parameterDescriptions[i].Length > 0)
                        parameters[i].Description = parameterDescriptions[i];
                }
            }
            finally
            {
                // A rejected call must leave the parameters of the model registered
                MyNCalc.ExistingParameters = existingParameters;
            }
            //
            EnsureCommandsAvailable();
            _modelGateway.SetParameters(parameters);
        }
        public void Rename(EquationParameter parameter, string newName)
        {
            // The parameters have no rename command, so the replacement carries the new name
            Replace(parameter.Name, newName, parameter.EquationStr.Equation, null);
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
        private static string GetFirstError(string[] parameterNames, Dictionary<string, string> errors)
        {
            foreach (string name in parameterNames)
            {
                if (errors.ContainsKey(name))
                    return "The parameter '" + name + "' cannot be evaluated: " + errors[name];
            }
            //
            return "The parameters cannot be evaluated.";
        }
        private void ValidateName(string name, string allowedExistingName)
        {
            ValidateNameFormat(name, "name");
            if (allowedExistingName != null && PythonNameComparer.AreSame(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetNames())
            {
                if (PythonNameComparer.AreSame(name, existingName))
                    throw new PythonAPIException("The parameter '" + name + "' already exists.");
            }
        }
        private static void ValidateNameFormat(string name, string argumentName)
        {
            if (name == null || name.Trim().Length == 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be empty.");
            // The parameter name is used inside equations, so it must not carry the minus character
            if (name.Contains("-"))
                throw new PythonAPIException("The parameter name must not contain the hyphen character.");
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
