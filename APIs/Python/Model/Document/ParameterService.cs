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
        private readonly PythonFileNameResolver _fileNameResolver;


        // Constructors
        public ParameterService(ModelResolver modelResolver, IPythonModelGateway modelGateway,
                                PythonFileNameResolver fileNameResolver)
        {
            _modelResolver = modelResolver;
            _modelGateway = modelGateway;
            _fileNameResolver = fileNameResolver;
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
            EquationParameter parameter = Build(name, "name", value, null);
            parameter.Description = description;
            //
            EnsureCommandsAvailable();
            _modelGateway.AddParameter(parameter);
        }
        public void Replace(string oldName, string name, object value, string description)
        {
            string storedName = ResolveParameter(oldName).Name;
            bool isRenamed = name != null && !string.Equals(name, storedName, StringComparison.Ordinal);
            Replace(storedName, name, value, description, isRenamed ? "name" : "value");
        }
        private void Replace(string oldName, string name, object value, string description, string changedArgumentName)
        {
            EquationParameter existing = ResolveParameter(oldName);
            oldName = existing.Name;
            string newName = name == null ? oldName : name;
            string nameArgumentName = changedArgumentName == "new_name" ? "new_name" : "name";
            // A value that is not given is kept, so a rename alone keeps the expression
            EquationParameter parameter;
            if (value == null)
            {
                ValidateName(newName, nameArgumentName, oldName);
                parameter = new EquationParameter();
                parameter.Name = newName;
                SetEquation(parameter, new EquationString(existing.EquationStr.Equation), oldName);
            }
            else parameter = Build(newName, nameArgumentName, value, oldName);
            // A description that is not given is kept, and an empty one clears it
            parameter.Description = description == null ? existing.Description : description;
            //
            List<KeyValuePair<string, string>> resultingEquations = GetEquations(new string[] { oldName });
            resultingEquations.Add(new KeyValuePair<string, string>(parameter.Name, parameter.EquationStr.Equation));
            RefuseBrokenParameters(resultingEquations, changedArgumentName, "changes");
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceParameter(oldName, parameter);
        }
        public void Remove(string[] names)
        {
            RefuseBrokenParameters(GetEquations(names), "names", "removes");
            //
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
                ValidateNameFormat(parameterNames[i], "names", null);
                if (!usedNames.Add(parameterNames[i]))
                    throw new PythonAPIException("The parameter '" + parameterNames[i] + "' is given more than once.");
            }
            // A parameter may use a parameter that is given after it, so all of the values are resolved before
            // the parameters are built: EquationContainer evaluates an equation before it stores it
            CaeGlobals.OrderedDictionary<string, object> existingParameters = MyNCalc.ExistingParameters;
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
            Replace(parameter.Name, newName, parameter.EquationStr.Equation, null, "new_name");
        }
        public void ImportFromFile(string fileName)
        {
            string fullFileName = ResolveFileName(fileName);
            if (!File.Exists(fullFileName))
                throw new PythonAPIException("The parameters file '" + fullFileName + "' does not exist.");
            //
            EnsureCommandsAvailable();
            CheckParametersFile(_modelResolver.GetModel(), fullFileName);
            _modelGateway.ImportParameters(fullFileName);
        }
        // The file is read into a collection of its own the way the import reads it, so a file the import refuses
        // is refused naming it before anything is recorded
        public static void CheckParametersFile(FeModel model, string fullFileName)
        {
            try
            {
                new EquationParameterCollection().ImportFromFile(fullFileName, false, model.GetPropertyParameters());
            }
            catch (Exception ex) when (ex is CaeException || ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new PythonAPIException("The parameters file '" + fullFileName + "' cannot be imported: " +
                                             ex.Message);
            }
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
        private EquationParameter Build(string name, string nameArgumentName, object value, string allowedExistingName)
        {
            ValidateName(name, nameArgumentName, allowedExistingName);
            EquationString equation = PythonValueConverter.ToEquationString(value, "value");
            //
            EquationParameter parameter = new EquationParameter();
            parameter.Name = name;
            SetEquation(parameter, equation, allowedExistingName);
            //
            return parameter;
        }
        // The parameter is evaluated together with the others of the model, the way set_all evaluates them, so an
        // equation that uses itself or a parameter that does not exist is refused naming the value rather than
        // failing inside the model
        private void SetEquation(EquationParameter parameter, EquationString equation, string replacedName)
        {
            List<KeyValuePair<string, string>> nameEquations =
                GetEquations(replacedName == null ? new string[0] : new string[] { replacedName });
            nameEquations.Add(new KeyValuePair<string, string>(parameter.Name, equation.Equation));
            //
            CaeGlobals.OrderedDictionary<string, object> existingParameters = MyNCalc.ExistingParameters;
            try
            {
                Dictionary<string, double> resolvedValues;
                Dictionary<string, string> errors =
                    EquationParameterResolver.Evaluate(nameEquations, _modelResolver.GetModel().GetPropertyParameters(),
                                                       out resolvedValues);
                string error;
                if (errors.TryGetValue(parameter.Name, out error))
                    throw new PythonAPIException("The argument 'value' of the parameter '" + parameter.Name +
                                                 "' cannot be evaluated: " + error);
            }
            finally
            {
                // A rejected call must leave the parameters of the model registered
                MyNCalc.ExistingParameters = existingParameters;
            }
            //
            try
            {
                parameter.EquationStr = equation;
            }
            catch (Exception ex)
            {
                throw new PythonAPIException("The argument 'value' of the parameter '" + parameter.Name +
                                             "' cannot be evaluated: " + ex.Message);
            }
        }
        private List<KeyValuePair<string, string>> GetEquations(string[] excludedNames)
        {
            List<KeyValuePair<string, string>> nameEquations = new List<KeyValuePair<string, string>>();
            foreach (string existingName in GetNames())
            {
                if (PythonNameComparer.Contains(excludedNames, existingName)) continue;
                //
                EquationParameter existing = GetParameter(existingName);
                if (existing != null && existing.EquationStr != null)
                    nameEquations.Add(new KeyValuePair<string, string>(existingName, existing.EquationStr.Equation));
            }
            //
            return nameEquations;
        }
        // The model evaluates the whole table after every change and reports a parameter that no longer evaluates
        // in a message box, while the parameter editor refuses to apply such a table. So a change that breaks a
        // parameter which evaluated before is refused, naming the parameters that depend on what it changes
        private void RefuseBrokenParameters(List<KeyValuePair<string, string>> resultingEquations, string argumentName,
                                            string change)
        {
            Dictionary<string, string> errorsBefore = EvaluateEquations(GetEquations(new string[0]));
            Dictionary<string, string> errorsAfter = EvaluateEquations(resultingEquations);
            List<string> brokenNames = new List<string>();
            foreach (KeyValuePair<string, string> entry in errorsAfter)
            {
                if (!errorsBefore.ContainsKey(entry.Key)) brokenNames.Add(entry.Key);
            }
            if (brokenNames.Count == 0) return;
            //
            throw new PythonAPIException("The argument '" + argumentName + "' " + change + " a parameter that the " +
                                         "parameters '" + string.Join("', '", brokenNames) + "' depend on, so they " +
                                         "could no longer be evaluated: " + errorsAfter[brokenNames[0]] + " Change " +
                                         "them first, or give the whole table to set_all.");
        }
        private Dictionary<string, string> EvaluateEquations(List<KeyValuePair<string, string>> nameEquations)
        {
            CaeGlobals.OrderedDictionary<string, object> existingParameters = MyNCalc.ExistingParameters;
            try
            {
                Dictionary<string, double> resolvedValues;
                return EquationParameterResolver.Evaluate(nameEquations, _modelResolver.GetModel().GetPropertyParameters(),
                                                          out resolvedValues);
            }
            finally
            {
                // A rejected call must leave the parameters of the model registered
                MyNCalc.ExistingParameters = existingParameters;
            }
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
        private void ValidateName(string name, string argumentName, string allowedExistingName)
        {
            ValidateNameFormat(name, argumentName, allowedExistingName);
            if (allowedExistingName != null && PythonNameComparer.AreSame(name, allowedExistingName)) return;
            // As the parameter editor refuses them: an equation reads a name starting with a digit as a number, and a
            // reserved name - a constant such as pi, a coordinate, a model property - shadows the parameter
            if (char.IsDigit(name[0]))
                throw new PythonAPIException("The argument '" + argumentName + "' must not start with a digit.");
            foreach (string reservedName in MyNCalc.GetReservedParameterNames()
                                                   .Concat(_modelResolver.GetModel().GetPropertyParameters().Keys))
            {
                if (string.Equals(name, reservedName, StringComparison.OrdinalIgnoreCase))
                    throw new PythonAPIException("The argument '" + argumentName + "' names '" + name + "', which " +
                                                 "is reserved in equations.");
            }
            //
            foreach (string existingName in GetNames())
            {
                if (PythonNameComparer.AreSame(name, existingName))
                    throw new PythonAPIException("The parameter '" + name + "' already exists.");
            }
        }
        private static void ValidateNameFormat(string name, string argumentName, string allowedExistingName)
        {
            PythonNameComparer.ValidateFormat(name, argumentName, allowedExistingName);
            // The parameter name is used inside equations, so it must not carry the minus character
            if (name.Contains("-"))
                throw new PythonAPIException("The argument '" + argumentName + "' must not contain the hyphen " +
                                             "character, because a parameter name is used inside equations.");
        }
        private string ResolveFileName(string fileName)
        {
            return _fileNameResolver.GetFullFileName(fileName, "file_name");
        }
        private void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}
