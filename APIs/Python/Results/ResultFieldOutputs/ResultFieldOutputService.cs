// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeResults;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace APIs.Python
{
    internal class ResultFieldOutputService
    {
        // Variables
        private readonly ResultResolver _resolver;
        private readonly IPythonCommandGateway _commandGateway;
        private readonly PythonFileNameResolver _fileNameResolver;


        // Constructors
        public ResultFieldOutputService(ResultResolver resolver, IPythonCommandGateway commandGateway,
                                        PythonFileNameResolver fileNameResolver)
        {
            _resolver = resolver;
            _commandGateway = commandGateway;
            _fileNameResolver = fileNameResolver;
        }


        // Methods
        public string[] GetNames(string resultName)
        {
            return _resolver.ResolveResult(resultName).GetResultFieldOutputs().Select(output => output.Name).ToArray();
        }
        public ResultFieldOutput ResolveFieldOutput(string resultName, string fieldOutputName)
        {
            if (fieldOutputName == null) throw new PythonAPIException("The field output name must not be None.");
            FeResults result = _resolver.ResolveResult(resultName);
            if (!result.ContainsResultFieldOutput(fieldOutputName))
                throw new PythonAPIException("The result field output '" + fieldOutputName +
                                             "' does not exist in result '" + resultName + "'.");
            return result.GetResultFieldOutput(fieldOutputName);
        }
        public ResultFieldOutputAPI CreateAPI(string resultName, string fieldOutputName)
        {
            ResultFieldOutput fieldOutput = ResolveFieldOutput(resultName, fieldOutputName);
            string storedName = fieldOutput.Name;
            if (fieldOutput is ResultFieldOutputLimit)
                return new ResultFieldOutputLimitAPI(resultName, storedName, this);
            if (fieldOutput is ResultFieldOutputEnvelope)
                return new ResultFieldOutputEnvelopeAPI(resultName, storedName, this);
            if (fieldOutput is ResultFieldOutputEquation)
                return new ResultFieldOutputEquationAPI(resultName, storedName, this);
            if (fieldOutput is ResultFieldOutputCoordinateSystemTransform)
                return new ResultFieldOutputCoordinateSystemTransformAPI(resultName, storedName, this);
            if (fieldOutput is ResultFieldOutputMaterialAxes)
                return new ResultFieldOutputMaterialAxesAPI(resultName, storedName, this);
            if (fieldOutput is ResultFieldOutputShellPlaneStress)
                return new ResultFieldOutputShellPlaneStressAPI(resultName, storedName, this);
            if (fieldOutput is ResultFieldOutputCompositeFailure)
                return new ResultFieldOutputCompositeFailureAPI(resultName, storedName, this);
            return new ResultFieldOutputAPI(resultName, storedName, this);
        }
        public ResultFieldOutputLimitDraftAPI CreateFieldOutputLimit(string name, string fieldName,
                                                                    string componentName, string basedOn,
                                                                    object itemLimits)
        {
            LimitPlotBasedOnEnum limitType = ParseBasedOn(basedOn);
            CaeGlobals.OrderedDictionary<string, double> limits =
                PythonDictionaryConverter.ToStringDoubleDictionary(itemLimits, "item_limits");
            ValidateDefinition(name, fieldName, componentName, limits);
            return new ResultFieldOutputLimitDraftAPI(name, fieldName, componentName, limitType, limits);
        }
        public ResultFieldOutputEnvelopeDraftAPI CreateFieldOutputEnvelope(string name, string fieldName,
                                                                           string componentName)
        {
            PythonNameComparer.ValidateFormat(name, "name", null);
            ValidateRequiredString(fieldName, "field_name");
            ValidateRequiredString(componentName, "component_name");
            return new ResultFieldOutputEnvelopeDraftAPI(name, fieldName, componentName);
        }
        public ResultFieldOutputEquationDraftAPI CreateFieldOutputEquation(string name, string equation, string unit)
        {
            PythonNameComparer.ValidateFormat(name, "name", null);
            ValidateRequiredString(equation, "equation");
            if (unit == null) throw new PythonAPIException("The argument 'unit' must not be None.");
            string normalizedEquation = equation.Trim();
            if (!normalizedEquation.StartsWith("=")) normalizedEquation = "=" + normalizedEquation;
            return new ResultFieldOutputEquationDraftAPI(name, normalizedEquation, unit);
        }
        public ResultFieldOutputCoordinateSystemTransformDraftAPI CreateFieldOutputCoordinateSystemTransform(
            string name, string fieldName, string coordinateSystemName)
        {
            PythonNameComparer.ValidateFormat(name, "name", null);
            ValidateRequiredString(fieldName, "field_name");
            ValidateRequiredString(coordinateSystemName, "coordinate_system_name");
            return new ResultFieldOutputCoordinateSystemTransformDraftAPI(name, fieldName, coordinateSystemName);
        }
        public ResultFieldOutputMaterialAxesDraftAPI CreateFieldOutputMaterialAxes(string name, string fieldName)
        {
            PythonNameComparer.ValidateFormat(name, "name", null);
            ValidateRequiredString(fieldName, "field_name");
            return new ResultFieldOutputMaterialAxesDraftAPI(name, fieldName);
        }
        public ResultFieldOutputShellPlaneStressDraftAPI CreateFieldOutputShellPlaneStress(string name, string fieldName)
        {
            PythonNameComparer.ValidateFormat(name, "name", null);
            ValidateRequiredString(fieldName, "field_name");
            return new ResultFieldOutputShellPlaneStressDraftAPI(name, fieldName);
        }
        public ResultFieldOutputCompositeFailureDraftAPI CreateFieldOutputCompositeFailure(string name, string fieldName,
                                                                                          string criterion)
        {
            PythonNameComparer.ValidateFormat(name, "name", null);
            ValidateRequiredString(fieldName, "field_name");
            CompositeFailureCriterionEnum criterionValue =
                PythonValueConverter.ToEnum<CompositeFailureCriterionEnum>(criterion, "criterion");
            return new ResultFieldOutputCompositeFailureDraftAPI(name, fieldName, criterionValue);
        }
        public ResultFieldOutputAPI AddFieldOutput(string resultName, ResultFieldOutputDraftAPI output)
        {
            if (output == null) throw new PythonAPIException("The argument 'output' must not be None.");
            ResultFieldOutput fieldOutput = output.CreateBase();
            FeResults result = ResolveCurrentResult(resultName);
            Validate(result, fieldOutput, null);
            if (result.AreResultFieldOutputsInCyclicDependance(null, fieldOutput))
                throw new PythonAPIException("The field output would create a cyclic dependency.");
            // Conversion, validation, and construction are complete before the command gateway is called.
            EnsureCommandsAvailable();
            _commandGateway.AddResultFieldOutput(fieldOutput);
            //
            return CreateAPI(resultName, fieldOutput.Name);
        }
        public ResultFieldOutputAPI ReplaceFieldOutput(string resultName, string oldFieldOutputName,
                                                       ResultFieldOutputDraftAPI output)
        {
            ValidateRequiredString(oldFieldOutputName, "old_name");
            if (output == null) throw new PythonAPIException("The argument 'output' must not be None.");
            ResultFieldOutput fieldOutput = output.CreateBase();
            FeResults result = ResolveCurrentResult(resultName);
            if (!result.ContainsResultFieldOutput(oldFieldOutputName))
                throw new PythonAPIException("The result field output '" + oldFieldOutputName +
                                             "' does not exist in result '" + resultName + "'.");
            ResultFieldOutput oldFieldOutput = result.GetResultFieldOutput(oldFieldOutputName);
            string storedOldName = oldFieldOutput.Name;
            if (oldFieldOutput.GetType() != fieldOutput.GetType())
                throw new PythonAPIException("The result field output '" + storedOldName + "' is made by add_" +
                                             ResultFieldOutputAPI.GetItemType(oldFieldOutput) + ", so replace_" +
                                             ResultFieldOutputAPI.GetItemType(fieldOutput) + " cannot replace " +
                                             "it. Remove it and add the new type instead.");
            Validate(result, fieldOutput, storedOldName);
            if (result.AreResultFieldOutputsInCyclicDependance(storedOldName, fieldOutput))
                throw new PythonAPIException("The replacement would create a cyclic field-output dependency.");
            //
            EnsureCommandsAvailable();
            _commandGateway.ReplaceResultFieldOutput(storedOldName, fieldOutput);
            //
            return CreateAPI(resultName, fieldOutput.Name);
        }
        public void RenameFieldOutput(string resultName, string oldFieldOutputName, string newName)
        {
            ValidateRequiredString(oldFieldOutputName, "old_name");
            FeResults result = ResolveCurrentResult(resultName);
            if (!result.ContainsResultFieldOutput(oldFieldOutputName))
                throw new PythonAPIException("The result field output '" + oldFieldOutputName +
                                             "' does not exist in result '" + resultName + "'.");
            ResultFieldOutput fieldOutput = result.GetResultFieldOutput(oldFieldOutputName);
            PythonNameComparer.ValidateFormat(newName, "new_name", fieldOutput.Name);
            if (string.Equals(fieldOutput.Name, newName)) return;
            if (!PythonNameComparer.AreSame(newName, fieldOutput.Name) &&
                PythonNameComparer.Contains(result.GetAllFieldNames(), newName))
                throw new PythonAPIException("A field named '" + newName + "' already exists in the result.");
            //
            EnsureCommandsAvailable();
            _commandGateway.RenameResultItem(fieldOutput, newName);
        }
        public void DuplicateFieldOutputs(string resultName, object fieldOutputNames)
        {
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(fieldOutputNames, "names");
            ValidateStringList(names, "names");
            FeResults result = ResolveCurrentResult(resultName);
            string[] storedNames = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                if (!result.ContainsResultFieldOutput(names[i]))
                    throw new PythonAPIException("The result field output '" + names[i] + "' does not exist in " +
                                                 "result '" + resultName + "'.");
                storedNames[i] = result.GetResultFieldOutput(names[i]).Name;
            }
            //
            EnsureCommandsAvailable();
            _commandGateway.DuplicateResultFieldOutputs(storedNames);
        }
        // A field output and a field read from the result file are found regardless of case
        public void RemoveFieldOutputs(string resultName, object fieldOutputNames)
        {
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(fieldOutputNames, "names");
            ValidateStringList(names, "names");
            PythonNameComparer.RefuseRepeated(names, "names");
            FeResults result = ResolveCurrentResult(resultName);
            string[] storedNames = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                storedNames[i] = ResultFieldNameResolver.GetStoredFieldName(result, names[i]);
                if (storedNames[i] == null)
                    throw new PythonAPIException("The result field output '" + names[i] +
                                                 "' does not exist in result '" + resultName + "'.");
            }
            //
            EnsureCommandsAvailable();
            _commandGateway.RemoveResultFieldOutputs(storedNames);
        }
        public void RemoveFieldOutputComponents(string resultName, string fieldOutputName, object componentNames)
        {
            ValidateRequiredString(fieldOutputName, "field_output_name");
            string[] names = PythonStringListConverter.ToStringArray(componentNames, "component_names");
            ValidateStringList(names, "component_names");
            PythonNameComparer.RefuseRepeated(names, "component_names");
            FeResults result = ResolveCurrentResult(resultName);
            string storedFieldOutputName = ResolveStoredFieldName(result, resultName, fieldOutputName);
            string[] storedComponentNames = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                storedComponentNames[i] = ResultFieldNameResolver.GetStoredComponentName(result, storedFieldOutputName,
                                                                                         names[i]);
                if (storedComponentNames[i] == null)
                    throw new PythonAPIException("The component '" + names[i] + "' does not exist in field output '" +
                                                 fieldOutputName + "'.");
            }
            //
            EnsureCommandsAvailable();
            _commandGateway.RemoveResultFieldOutputComponents(storedFieldOutputName, storedComponentNames);
        }
        public void ExportFieldOutputToCsv(string resultName, string fileName, string fieldOutputName,
                                           int stepId, int incrementId)
        {
            ValidateRequiredString(fileName, "file_name");
            ValidateRequiredString(fieldOutputName, "field_output_name");
            string fullFileName = _fileNameResolver.GetFullFileName(fileName, "file_name");
            string directoryName = Path.GetDirectoryName(fullFileName);
            if (!Directory.Exists(directoryName))
                throw new PythonAPIException("The export directory '" + directoryName + "' does not exist.");
            //
            FeResults result = ResolveCurrentResult(resultName);
            string storedFieldOutputName = ResolveStoredFieldName(result, resultName, fieldOutputName);
            if (!result.GetAllStepIds().Contains(stepId))
                throw new PythonAPIException("The result step ID " + stepId + " does not exist.");
            if (!result.GetStepIncrementIds(stepId).Contains(incrementId))
                throw new PythonAPIException("The increment ID " + incrementId +
                                             " does not exist in result step " + stepId + ".");
            string[] componentNames = result.GetFieldComponentNames(storedFieldOutputName);
            if (componentNames == null || componentNames.Length == 0)
                throw new PythonAPIException("The result field output '" + fieldOutputName +
                                             "' does not contain any components.");
            FieldData fieldData = result.GetFieldData(storedFieldOutputName, componentNames[0], stepId, incrementId,
                                                      true);
            if (fieldData == null || !fieldData.Valid)
                throw new PythonAPIException("The result field output '" + fieldOutputName +
                                             "' is not available at step " + stepId + ", increment " +
                                             incrementId + ".");
            //
            EnsureCommandsAvailable();
            _commandGateway.ExportResultFieldOutput(fullFileName, resultName, fieldData);
        }
        private static void Validate(FeResults result, ResultFieldOutput fieldOutput, string replacedFieldOutputName)
        {
            PythonNameComparer.ValidateFormat(fieldOutput.Name, "name", replacedFieldOutputName);
            if (result.GetAllFieldNames().Any(existingFieldName =>
                    string.Equals(existingFieldName, fieldOutput.Name, StringComparison.OrdinalIgnoreCase) &&
                    (replacedFieldOutputName == null ||
                     !string.Equals(existingFieldName, replacedFieldOutputName, StringComparison.OrdinalIgnoreCase))))
                throw new PythonAPIException("A field named '" + fieldOutput.Name + "' already exists in the result.");
            //
            if (fieldOutput is ResultFieldOutputLimit limit) ValidateLimit(result, limit);
            else if (fieldOutput is ResultFieldOutputEnvelope envelope) ValidateEnvelope(result, envelope);
            else if (fieldOutput is ResultFieldOutputEquation equation) ValidateEquation(result, equation);
            else if (fieldOutput is ResultFieldOutputCoordinateSystemTransform transform)
                ValidateCoordinateSystemTransform(result, transform);
            else if (fieldOutput is ResultFieldOutputMaterialAxes materialAxes)
                materialAxes.FieldName = ValidateShellSourceField(result, materialAxes.FieldName);
            else if (fieldOutput is ResultFieldOutputShellPlaneStress shellPlaneStress)
                shellPlaneStress.FieldName = ValidateShellSourceField(result, shellPlaneStress.FieldName);
            else if (fieldOutput is ResultFieldOutputCompositeFailure compositeFailure)
                compositeFailure.FieldName = ValidateShellSourceField(result, compositeFailure.FieldName);
            else throw new PythonAPIException("The field output draft type '" + fieldOutput.GetType().Name +
                                              "' is not supported.");
        }
        private static void ValidateLimit(FeResults result, ResultFieldOutputLimit limit)
        {
            ValidateDefinition(limit.Name, limit.FieldName, limit.ComponentName, limit.ItemNameLimit);
            string storedLimitComponentName;
            limit.FieldName = ValidateSourceFieldComponent(result, limit.FieldName, limit.ComponentName,
                                                           out storedLimitComponentName);
            limit.ComponentName = storedLimitComponentName;
            if (result.Mesh == null) throw new PythonAPIException("The result does not contain a mesh.");
            // The mesh finds a part or an element set regardless of case, while the result checks the limit keys
            // against its own spelling, so every key is stored as the mesh spells it
            CaeGlobals.OrderedDictionary<string, double> storedItemLimits = new CaeGlobals.OrderedDictionary<string, double>("ItemNameLimit");
            foreach (var entry in limit.ItemNameLimit)
            {
                string storedName;
                if (limit.LimitPlotBasedOn == LimitPlotBasedOnEnum.Parts)
                {
                    CaeMesh.BasePart part;
                    if (!result.Mesh.Parts.TryGetValue(entry.Key, out part))
                        throw new PythonAPIException("The result part '" + entry.Key + "' does not exist.");
                    storedName = part.Name;
                }
                else if (limit.LimitPlotBasedOn == LimitPlotBasedOnEnum.ElementSets)
                {
                    CaeMesh.FeElementSet elementSet;
                    if (!result.Mesh.ElementSets.TryGetValue(entry.Key, out elementSet))
                        throw new PythonAPIException("The result element set '" + entry.Key +
                                                     "' does not exist.");
                    storedName = elementSet.Name;
                }
                else if (limit.LimitPlotBasedOn == LimitPlotBasedOnEnum.AllElements)
                {
                    storedName = LimitPlotBasedOnEnum.AllElements.GetDisplayedName();
                    if (limit.ItemNameLimit.Count != 1)
                        throw new PythonAPIException("An AllElements limit must contain exactly one limit value, " +
                                                     "under the key '" + storedName + "'.");
                    if (!PythonNameComparer.AreSame(entry.Key, storedName))
                        throw new PythonAPIException("The argument 'item_limits' of an AllElements limit takes the " +
                                                     "key '" + storedName + "', not '" + entry.Key + "'.");
                }
                else throw new PythonAPIException("Unsupported limit assignment type '" +
                                                  limit.LimitPlotBasedOn + "'.");
                if (storedItemLimits.ContainsKey(storedName))
                    throw new PythonAPIException("The argument 'item_limits' names '" + storedName + "' more than " +
                                                 "once.");
                storedItemLimits.Add(storedName, entry.Value);
            }
            limit.ItemNameLimit = storedItemLimits;
        }
        private static void ValidateEnvelope(FeResults result, ResultFieldOutputEnvelope envelope)
        {
            ValidateRequiredString(envelope.FieldName, "field_name");
            ValidateRequiredString(envelope.ComponentName, "component_name");
            string storedEnvelopeComponentName;
            envelope.FieldName = ValidateSourceFieldComponent(result, envelope.FieldName, envelope.ComponentName,
                                                              out storedEnvelopeComponentName);
            envelope.ComponentName = storedEnvelopeComponentName;
        }
        private static void ValidateEquation(FeResults result, ResultFieldOutputEquation equation)
        {
            ValidateRequiredString(equation.Equation, "equation");
            if (equation.Unit == null) throw new PythonAPIException("The argument 'unit' must not be None.");
            HashSet<string> parentNames;
            string error = result.CheckResultFieldOutputEquation(equation.Equation, out parentNames, out _);
            if (error != null) throw new PythonAPIException(error);
            if (parentNames.Contains(equation.Name))
                throw new PythonAPIException("The equation must not contain a self-reference.");
            equation.SetParentNames(parentNames.ToArray());
        }
        // The shell outputs need a complete tensor field and the shell data the model gives when the result is opened
        private static string ValidateShellSourceField(FeResults result, string fieldName)
        {
            ValidateRequiredString(fieldName, "field_name");
            string storedFieldName = ValidateSourceField(result, fieldName);
            if (result.GetFieldDataType(storedFieldName) != DataTypeEnum.Tensor)
                throw new PythonAPIException("The argument 'field_name' must name a tensor field, such as STRESS.");
            if (!result.DoesFieldContainsAllNecessaryComponents(storedFieldName))
                throw new PythonAPIException("The field output '" + storedFieldName +
                                             "' does not contain all required components.");
            if (!result.IsShellSourceField(storedFieldName))
                throw new PythonAPIException("The argument 'field_name' must name a field in global axes, not the " +
                                             "output in material axes '" + storedFieldName + "'.");
            if (!result.HasShellResultData())
                throw new PythonAPIException("The result has no shell data: it must be opened with the model it was " +
                                             "solved from, whose shell sections give the material axes.");
            return storedFieldName;
        }
        private static void ValidateCoordinateSystemTransform(
            FeResults result, ResultFieldOutputCoordinateSystemTransform transform)
        {
            ValidateRequiredString(transform.FieldName, "field_name");
            ValidateRequiredString(transform.CoordinateSystemName, "coordinate_system_name");
            transform.FieldName = ValidateSourceField(result, transform.FieldName);
            if (result.Mesh == null) throw new PythonAPIException("The result does not contain a mesh.");
            if (!result.Mesh.CoordinateSystems.ContainsValidKey(transform.CoordinateSystemName))
                throw new PythonAPIException("The result coordinate system '" + transform.CoordinateSystemName +
                                             "' does not exist or is invalid.");
            transform.CoordinateSystemName = result.Mesh.CoordinateSystems[transform.CoordinateSystemName].Name;
            DataTypeEnum dataType = result.GetFieldDataType(transform.FieldName);
            if (dataType != DataTypeEnum.Vector && dataType != DataTypeEnum.Tensor)
                throw new PythonAPIException("Only vector or tensor field outputs can be transformed.");
            if (!result.DoesFieldContainsAllNecessaryComponents(transform.FieldName))
                throw new PythonAPIException("The field output '" + transform.FieldName +
                                             "' does not contain all required components.");
        }
        // Both return the source field, and the first its component, in the spelling of the result
        private static string ValidateSourceFieldComponent(FeResults result, string fieldName, string componentName,
                                                           out string storedComponentName)
        {
            string storedFieldName = ValidateSourceField(result, fieldName);
            storedComponentName = ResultFieldNameResolver.GetStoredComponentName(result, storedFieldName,
                                                                                 componentName);
            if (storedComponentName == null)
                throw new PythonAPIException("The component '" + componentName +
                                             "' does not exist in field '" + storedFieldName + "'.");
            return storedFieldName;
        }
        private static string ValidateSourceField(FeResults result, string fieldName)
        {
            string storedFieldName = ResultFieldNameResolver.GetStoredFieldName(result, fieldName);
            if (storedFieldName != null) return storedFieldName;
            //
            throw new PythonAPIException("The source field '" + fieldName + "' does not exist in the result.");
        }
        private static LimitPlotBasedOnEnum ParseBasedOn(string basedOn)
        {
            return PythonValueConverter.ToEnum<LimitPlotBasedOnEnum>(basedOn, "based_on");
        }
        private static void ValidateDefinition(string name, string fieldName, string componentName,
                                               CaeGlobals.OrderedDictionary<string, double> itemLimits)
        {
            PythonNameComparer.ValidateFormat(name, "name", null);
            ValidateRequiredString(fieldName, "field_name");
            ValidateRequiredString(componentName, "component_name");
            if (itemLimits.Count == 0)
                throw new PythonAPIException("The argument 'item_limits' must contain at least one item.");
            //
            foreach (var entry in itemLimits)
            {
                ValidateRequiredString(entry.Key, "item_limits key");
                if (entry.Value == 0 || double.IsNaN(entry.Value) || double.IsInfinity(entry.Value))
                    throw new PythonAPIException("The limit for item '" + entry.Key +
                                                 "' must be a finite value different from 0.");
            }
        }
        private static void ValidateRequiredString(string value, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new PythonAPIException("The argument '" + argumentName + "' must be a non-empty string.");
        }
        private static void ValidateStringList(string[] values, string argumentName)
        {
            if (values.Length == 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must contain at least one item.");
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string value in values)
            {
                ValidateRequiredString(value, argumentName + " item");
                if (!names.Add(value))
                    throw new PythonAPIException("The argument '" + argumentName +
                                                 "' contains the duplicate item '" + value + "'.");
            }
        }
        private FeResults ResolveCurrentResult(string resultName)
        {
            FeResults result = _resolver.ResolveResult(resultName);
            if (!_resolver.IsCurrentResult(resultName))
                throw new PythonAPIException("This Result object is no longer the current result. " +
                                             "Get pmx.results.current again before modifying field outputs.");
            return result;
        }
        // A field output and a field read from the result file are found regardless of case
        private static string ResolveStoredFieldName(FeResults result, string resultName, string fieldName)
        {
            string storedFieldName = ResultFieldNameResolver.GetStoredFieldName(result, fieldName);
            if (storedFieldName != null) return storedFieldName;
            //
            throw new PythonAPIException("The result field output '" + fieldName + "' does not exist in result '" +
                                         resultName + "'.");
        }
        private void EnsureCommandsAvailable()
        {
            if (_commandGateway == null)
                throw new PythonAPIException("Result modifications are not available in this Python API context.");
        }
    }
}
