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
using CaeResults;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace APIs.Python
{
    internal class ResultHistoryOutputService
    {
        // Variables
        private readonly ResultResolver _resolver;
        private readonly SelectionService _selectionService;
        private readonly IPythonCommandGateway _commandGateway;


        // Constructors
        public ResultHistoryOutputService(ResultResolver resolver, SelectionService selectionService,
                                          IPythonCommandGateway commandGateway)
        {
            _resolver = resolver;
            _selectionService = selectionService;
            _commandGateway = commandGateway;
        }


        // Methods
        public ResultHistoryOutputFromFieldDraftAPI CreateFromField(string name, string fieldName,
                                                                    object componentNames,
                                                                    object region)
        {
            ValidateNameSyntax(name);
            ValidateRequiredString(fieldName, "field_name");
            string[] components = PythonStringListConverter.ToStringArray(componentNames, "component_names");
            ValidateStringList(components, "component_names");
            ValidateRegionArgument(region, "nodes", RegionTypeEnum.NodeSetName, RegionTypeEnum.SurfaceName);
            return new ResultHistoryOutputFromFieldDraftAPI(name, fieldName, components, region);
        }
        public ResultHistoryOutputFromEquationDraftAPI CreateFromEquation(string name, string equation, string unit)
        {
            ValidateNameSyntax(name);
            ValidateRequiredString(equation, "equation");
            if (unit == null) throw new PythonAPIException("The argument 'unit' must not be None.");
            string normalizedEquation = equation.Trim();
            if (!normalizedEquation.StartsWith("=")) normalizedEquation = "=" + normalizedEquation;
            return new ResultHistoryOutputFromEquationDraftAPI(name, normalizedEquation, unit);
        }
        public ResultHistoryOutputFromIntegrationDraftAPI CreateFromIntegration(string name, string integralType,
                                                                                 string fieldName,
                                                                                 object componentNames,
                                                                                 object region,
                                                                                 string integralResult,
                                                                                 string deformationVariableName,
                                                                                 string unit,
                                                                                 string crossSection,
                                                                                 double thickness)
        {
            ValidateNameSyntax(name);
            IntegralTypeEnum type = ParseIntegralType(integralType);
            IntegralResultEnum resultType = ParseIntegralResult(integralResult);
            // The geometry (measure) result integrates the unit field, so it needs no field/components
            string[] components = null;
            if (resultType != IntegralResultEnum.Geometry)
            {
                ValidateRequiredString(fieldName, "field_name");
                components = PythonStringListConverter.ToStringArray(componentNames, "component_names");
                ValidateStringList(components, "component_names");
            }
            ValidateDeformationVariableName(deformationVariableName);
            if (unit == null) throw new PythonAPIException("The argument 'unit' must not be None.");
            IntegrationScaleType scale = ParseCrossSection(crossSection);
            ValidateCrossSection(type, scale, thickness);
            if (type == IntegralTypeEnum.Line)
                ValidateRegionArgument(region, "edges");
            else if (type == IntegralTypeEnum.Surface)
                ValidateRegionArgument(region, "surfaces", RegionTypeEnum.SurfaceName);
            else ValidateRegionArgument(region, "elements", RegionTypeEnum.ElementSetName);
            return new ResultHistoryOutputFromIntegrationDraftAPI(name, type, resultType, fieldName, components, region,
                                                                   deformationVariableName, unit, scale, thickness);
        }
        public ResultHistoryOutputFromMergedHistoriesDraftAPI CreateFromMergedHistories(string name, object source1,
                                                                                        object source2, object source3,
                                                                                        object source4)
        {
            ValidateNameSyntax(name);
            if (source3 == null && source4 != null)
                throw new PythonAPIException("The argument 'source4' cannot be used without 'source3'.");
            List<HistoryResultData> sources = new List<HistoryResultData>();
            sources.Add(ParseHistorySource(source1, "source1"));
            sources.Add(ParseHistorySource(source2, "source2"));
            if (source3 != null) sources.Add(ParseHistorySource(source3, "source3"));
            if (source4 != null) sources.Add(ParseHistorySource(source4, "source4"));
            ValidateUniqueHistorySources(sources);
            return new ResultHistoryOutputFromMergedHistoriesDraftAPI(name, sources.ToArray());
        }
        public ResultHistoryOutputFromExtractedEntryDraftAPI CreateFromExtractedEntry(string name, object source,
                                                                                      string entryName)
        {
            ValidateNameSyntax(name);
            HistoryResultData historySource = ParseHistorySource(source, "source");
            ValidateRequiredString(entryName, "entry_name");
            return new ResultHistoryOutputFromExtractedEntryDraftAPI(name, historySource, entryName);
        }
        public ResultHistoryOutputXYDraftAPI CreateXY(string name, object xSource, object ySource)
        {
            ValidateNameSyntax(name);
            HistoryResultData x = ParseHistorySource(xSource, "x_source");
            HistoryResultData y = ParseHistorySource(ySource, "y_source");
            return new ResultHistoryOutputXYDraftAPI(name, x, y);
        }
        public ResultHistoryOutputFromTableDraftAPI CreateFromTable(string name, object dataPoints,
                                                                    object yColumnNames, string xAxisUnit,
                                                                    string yAxisUnit)
        {
            ValidateNameSyntax(name);
            double[][] rows = PythonDoubleListConverter.ToDoubleArray2D(dataPoints, "data_points");
            if (rows.Length == 0)
                throw new PythonAPIException("The argument 'data_points' must contain at least one row.");
            if (rows[0].Length < 2)
                throw new PythonAPIException("Each row in 'data_points' must contain the X value and at least one " +
                                             "Y value.");
            foreach (double[] row in rows)
            {
                if (row.Length != rows[0].Length)
                    throw new PythonAPIException("All rows in 'data_points' must contain the same number of values.");
            }
            int numberOfYColumns = rows[0].Length - 1;
            string[] columnNames;
            if (yColumnNames == null) columnNames = GetDefaultYColumnNames(numberOfYColumns);
            else
            {
                columnNames = PythonStringListConverter.ToStringArray(yColumnNames, "y_column_names");
                ValidateStringList(columnNames, "y_column_names");
                if (columnNames.Length != numberOfYColumns)
                    throw new PythonAPIException("The argument 'y_column_names' must contain " + numberOfYColumns +
                                                 " names to match the 'data_points' columns.");
            }
            if (xAxisUnit == null) throw new PythonAPIException("The argument 'x_axis_unit' must not be None.");
            if (yAxisUnit == null) throw new PythonAPIException("The argument 'y_axis_unit' must not be None.");
            return new ResultHistoryOutputFromTableDraftAPI(name, rows, columnNames, xAxisUnit, yAxisUnit);
        }
        public string AddHistoryOutput(string resultName, ResultHistoryOutputDraftAPI output)
        {
            if (output == null) throw new PythonAPIException("The argument 'output' must not be None.");
            FeResults result = ResolveCurrentResult(resultName);
            ResultHistoryOutput historyOutput = output.CreateBase(_selectionService, resultName);
            Validate(result, historyOutput, null);
            EnsureCommandsAvailable();
            _commandGateway.AddResultHistoryOutput(historyOutput);
            return historyOutput.Name;
        }
        public string ReplaceHistoryOutput(string resultName, string oldName, ResultHistoryOutputDraftAPI output)
        {
            ValidateRequiredString(oldName, "old_name");
            if (output == null) throw new PythonAPIException("The argument 'output' must not be None.");
            FeResults result = ResolveCurrentResult(resultName);
            if (!result.ContainsResultHistoryOutput(oldName))
                throw new PythonAPIException("The result history output definition '" + oldName +
                                             "' does not exist in the current result.");
            ResultHistoryOutput historyOutput = output.CreateBase(_selectionService, resultName);
            Validate(result, historyOutput, oldName);
            EnsureCommandsAvailable();
            _commandGateway.ReplaceResultHistoryOutput(oldName, historyOutput);
            return historyOutput.Name;
        }
        public void RenameHistoryOutput(string resultName, string oldName, string newName)
        {
            ValidateRequiredString(oldName, "old_name");
            FeResults result = ResolveCurrentResult(resultName);
            ResolveHistorySet(result, oldName);
            // Only a user-defined history output carries a definition the rename can be applied to
            ResultHistoryOutput historyOutput = result.GetResultHistoryOutputOrNull(oldName);
            if (historyOutput == null)
                throw new PythonAPIException("The history output '" + oldName +
                                             "' is not a user-defined history output and cannot be renamed.");
            PythonNameComparer.ValidateFormat(newName, "new_name", historyOutput.Name);
            if (string.Equals(historyOutput.Name, newName)) return;
            if (!PythonNameComparer.AreSame(newName, historyOutput.Name) &&
                PythonNameComparer.Contains(result.GetHistory().Sets.Keys, newName))
                throw new PythonAPIException("A history output named '" + newName + "' already exists in the result.");
            //
            EnsureCommandsAvailable();
            _commandGateway.RenameResultItem(historyOutput, newName);
        }
        public void DuplicateHistoryOutputs(string resultName, object historyOutputNames)
        {
            string[] names = PythonStringListConverter.ToStringArray(historyOutputNames, "names");
            ValidateStringList(names, "names");
            FeResults result = ResolveCurrentResult(resultName);
            foreach (string name in names)
            {
                // Only user-defined history outputs carry a definition that can be duplicated
                if (result.GetResultHistoryOutputOrNull(name) == null)
                    throw new PythonAPIException("The history output '" + name +
                                                 "' is not a user-defined history output and cannot be duplicated.");
            }
            EnsureCommandsAvailable();
            _commandGateway.DuplicateResultHistoryOutputs(names);
        }
        public void ExportHistoryOutputsToCsv(string resultName, string fileName, object historyOutputNames,
                                              string delimiter)
        {
            ValidateRequiredString(fileName, "file_name");
            ValidateRequiredString(delimiter, "delimiter");
            string[] names = PythonStringListConverter.ToStringArray(historyOutputNames, "history_output_names");
            ValidateStringList(names, "history_output_names");
            string fullFileName;
            try
            {
                fullFileName = Path.GetFullPath(fileName);
            }
            catch (Exception ex)
            {
                throw new PythonAPIException("The argument 'file_name' is invalid: " + ex.Message);
            }
            string directoryName = Path.GetDirectoryName(fullFileName);
            if (!Directory.Exists(directoryName))
                throw new PythonAPIException("The export directory '" + directoryName + "' does not exist.");
            FeResults result = ResolveCurrentResult(resultName);
            foreach (string name in names) ResolveHistorySet(result, name);
            EnsureCommandsAvailable();
            _commandGateway.ExportResultHistoryOutput(fullFileName, names, delimiter);
        }
        public void RemoveHistoryFields(string resultName, string historyOutputName, object fieldNames)
        {
            ValidateRequiredString(historyOutputName, "history_output_name");
            string[] names = PythonStringListConverter.ToStringArray(fieldNames, "field_names");
            ValidateStringList(names, "field_names");
            FeResults result = ResolveCurrentResult(resultName);
            HistoryResultSet set = ResolveHistorySet(result, historyOutputName);
            foreach (string name in names)
            {
                if (!set.Fields.ContainsKey(name))
                    throw new PythonAPIException("The history field '" + name + "' does not exist in history output '" +
                                                 historyOutputName + "'.");
            }
            EnsureCommandsAvailable();
            _commandGateway.RemoveResultHistoryFields(historyOutputName, names);
        }
        public void RemoveHistoryComponents(string resultName, string historyOutputName, string fieldName,
                                            object componentNames)
        {
            ValidateRequiredString(historyOutputName, "history_output_name");
            ValidateRequiredString(fieldName, "field_name");
            string[] names = PythonStringListConverter.ToStringArray(componentNames, "component_names");
            ValidateStringList(names, "component_names");
            FeResults result = ResolveCurrentResult(resultName);
            HistoryResultSet set = ResolveHistorySet(result, historyOutputName);
            HistoryResultField field;
            if (!set.Fields.TryGetValue(fieldName, out field))
                throw new PythonAPIException("The history field '" + fieldName + "' does not exist in history output '" +
                                             historyOutputName + "'.");
            foreach (string name in names)
            {
                if (!field.Components.ContainsKey(name))
                    throw new PythonAPIException("The history component '" + name + "' does not exist in field '" +
                                                 fieldName + "'.");
            }
            EnsureCommandsAvailable();
            _commandGateway.RemoveResultHistoryComponents(historyOutputName, fieldName, names);
        }
        private static HistoryResultSet ResolveHistorySet(FeResults result, string historyOutputName)
        {
            HistoryResultSet set;
            if (!result.GetHistory().Sets.TryGetValue(historyOutputName, out set))
                throw new PythonAPIException("The history output '" + historyOutputName +
                                             "' does not exist in the current result.");
            return set;
        }
        public void RemoveHistoryOutputs(string resultName, object historyOutputNames)
        {
            string[] names = PythonStringListConverter.ToStringArray(historyOutputNames, "names");
            ValidateStringList(names, "names");
            FeResults result = ResolveCurrentResult(resultName);
            string[] existingNames = result.GetHistory().Sets.Keys.ToArray();
            foreach (string name in names)
            {
                if (!existingNames.Contains(name))
                    throw new PythonAPIException("The history output '" + name +
                                                 "' does not exist in the current result.");
            }
            EnsureCommandsAvailable();
            _commandGateway.RemoveResultHistoryOutputs(names);
        }
        private static void Validate(FeResults result, ResultHistoryOutput output, string replacedName)
        {
            ValidateNameSyntax(output.Name, replacedName);
            string[] existingNames = result.GetHistory().Sets.Keys.ToArray();
            if (existingNames.Any(name => string.Equals(name, output.Name, StringComparison.OrdinalIgnoreCase) &&
                                          (replacedName == null ||
                                           !string.Equals(name, replacedName, StringComparison.OrdinalIgnoreCase))))
                throw new PythonAPIException("A history output named '" + output.Name +
                                             "' already exists in the result.");
            if (output is ResultHistoryOutputFromField fromField) ValidateFromField(result, fromField);
            else if (output is ResultHistoryOutputFromEquation fromEquation)
                ValidateFromEquation(result, fromEquation, replacedName);
            else if (output is ResultHistoryOutputFromIntegration fromIntegration)
                ValidateFromIntegration(result, fromIntegration);
            else if (output is ResultHistoryOutputFromMergedHistories fromMergedHistories)
                ValidateFromMergedHistories(result, fromMergedHistories, replacedName);
            else if (output is ResultHistoryOutputFromExtractedEntry fromExtractedEntry)
                ValidateFromExtractedEntry(result, fromExtractedEntry, replacedName);
            else if (output is ResultHistoryOutputXY xy) ValidateXY(result, xy, replacedName);
            else if (output is ResultHistoryOutputFromTable fromTable) ValidateFromTable(fromTable);
            else throw new PythonAPIException("The result history output type '" + output.GetType().Name +
                                              "' is not supported.");
        }
        private static void ValidateFromField(FeResults result, ResultHistoryOutputFromField output)
        {
            ValidateRegion(result, output, "nodes", RegionTypeEnum.NodeSetName, RegionTypeEnum.SurfaceName);
            ValidateFieldComponents(result, output.FieldName, output.ComponentNames);
            if (output.StepId == -1)
            {
                if (output.StepIncrementId != -1)
                    throw new PythonAPIException("An increment ID cannot be selected when all steps are used.");
            }
            else
            {
                if (!result.GetAllStepIds().Contains(output.StepId))
                    throw new PythonAPIException("Result step ID " + output.StepId + " does not exist.");
                if (output.StepIncrementId != -1 &&
                    !result.GetStepIncrementIds(output.StepId).Contains(output.StepIncrementId))
                    throw new PythonAPIException("Increment ID " + output.StepIncrementId +
                                                 " does not exist in result step " + output.StepId + ".");
            }
        }
        private static void ValidateFromEquation(FeResults result, ResultHistoryOutputFromEquation output,
                                                 string replacedName)
        {
            ValidateRequiredString(output.Equation, "equation");
            if (output.Unit == null) throw new PythonAPIException("The equation unit must not be None.");
            HashSet<string> parentNames;
            string error = result.CheckResultHistoryOutputEquation(output.Equation, out parentNames, out _);
            if (error != null) throw new PythonAPIException(error);
            if (parentNames.Contains(output.Name))
                throw new PythonAPIException("The equation must not contain a self-reference.");
            output.SetParentNames(parentNames.ToArray());
            if (result.AreResultHistoryOutputsInCyclicDependance(replacedName, output))
                throw new PythonAPIException("The history output equation would create a cyclic dependency.");
        }
        private static void ValidateFromIntegration(FeResults result, ResultHistoryOutputFromIntegration output)
        {
            if (output.IntegralType == IntegralTypeEnum.Line)
                ValidateRegion(result, output, "edges");
            else if (output.IntegralType == IntegralTypeEnum.Surface)
                ValidateRegion(result, output, "surfaces", RegionTypeEnum.SurfaceName);
            else ValidateRegion(result, output, "elements", RegionTypeEnum.ElementSetName);
            // The geometry (measure) result integrates the unit field, so it needs no field/components
            if (output.IntegralResult != IntegralResultEnum.Geometry)
                ValidateFieldComponents(result, output.FieldName, output.ComponentNames);
            ValidateDeformationField(result, output.DeformationVariableName);
            if (output.Unit == null) throw new PythonAPIException("The integration unit must not be None.");
            ValidateCrossSection(output.IntegralType, output.CrossSection, output.Thickness);
        }
        private static void ValidateFromMergedHistories(FeResults result,
                                                       ResultHistoryOutputFromMergedHistories output,
                                                       string replacedName)
        {
            HistoryResultData[] sources = output.GetSources();
            if (sources.Length < 2)
                throw new PythonAPIException("At least two history sources are required for merging.");
            ValidateHistorySourcesExist(result, sources);
            try
            {
                DerivedHistorySetBuilder.CreateMergedSet(result, output);
            }
            catch (CaeException ex)
            {
                throw new PythonAPIException(ex.Message);
            }
            ValidateHistorySourceDependencies(result, output, replacedName);
        }
        private static void ValidateFromExtractedEntry(FeResults result, ResultHistoryOutputFromExtractedEntry output,
                                                       string replacedName)
        {
            ValidateRequiredString(output.EntryName, "entry_name");
            ValidateHistorySourcesExist(result, new HistoryResultData[] { output.Source });
            try
            {
                DerivedHistorySetBuilder.CreateExtractedSet(result, output);
            }
            catch (CaeException ex)
            {
                throw new PythonAPIException(ex.Message);
            }
            ValidateHistorySourceDependencies(result, output, replacedName);
        }
        private static void ValidateXY(FeResults result, ResultHistoryOutputXY output, string replacedName)
        {
            ValidateHistorySourcesExist(result, new HistoryResultData[] { output.XSource, output.YSource });
            try
            {
                DerivedHistorySetBuilder.CreateXYSet(result, output);
            }
            catch (CaeException ex)
            {
                throw new PythonAPIException(ex.Message);
            }
            ValidateHistorySourceDependencies(result, output, replacedName);
        }
        private static void ValidateFromTable(ResultHistoryOutputFromTable output)
        {
            try
            {
                output.CheckData();
            }
            catch (CaeException ex)
            {
                throw new PythonAPIException(ex.Message);
            }
        }
        private static void ValidateHistorySourcesExist(FeResults result, HistoryResultData[] sources)
        {
            foreach (HistoryResultData source in sources)
            {
                if (source == null) throw new PythonAPIException("The history source must not be None.");
                if (result.GetHistoryResultComponent(source.SetName, source.FieldName, source.ComponentName) == null)
                    throw new PythonAPIException("The history source '" + source.GetDisplayName() +
                                                 "' does not exist in the current result.");
            }
        }
        private static void ValidateHistorySourceDependencies(FeResults result, ResultHistoryOutput output,
                                                              string replacedName)
        {
            if (result.AreResultHistoryOutputsInCyclicDependance(replacedName, output))
                throw new PythonAPIException("The selected history sources create a cyclic dependency.");
        }
        private static HistoryResultData ParseHistorySource(object source, string argumentName)
        {
            if (source == null) throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            ResultHistoryOutputComponentAPI component = source as ResultHistoryOutputComponentAPI;
            if (component != null)
                return new HistoryResultData(component.HistoryOutputName, component.FieldName,
                                             component.ComponentName);
            string[] names;
            try
            {
                names = PythonStringListConverter.ToStringArray(source, argumentName);
            }
            catch (PythonAPIException)
            {
                throw InvalidHistorySource(argumentName);
            }
            if (names.Length != 3) throw InvalidHistorySource(argumentName);
            foreach (string name in names) ValidateRequiredString(name, argumentName + " item");
            return new HistoryResultData(names[0], names[1], names[2]);
        }
        private static PythonAPIException InvalidHistorySource(string argumentName)
        {
            return new PythonAPIException("The argument '" + argumentName + "' must be a HistoryComponent or a list " +
                                          "of three strings [history_output_name, field_name, component_name].");
        }
        private static void ValidateUniqueHistorySources(List<HistoryResultData> sources)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (HistoryResultData source in sources)
            {
                if (!names.Add(source.GetDisplayName()))
                    throw new PythonAPIException("The history source '" + source.GetDisplayName() +
                                                 "' is selected more than once.");
            }
        }
        private static string[] GetDefaultYColumnNames(int numberOfYColumns)
        {
            string[] columnNames = new string[numberOfYColumns];
            for (int column = 0; column < numberOfYColumns; column++)
                columnNames[column] = ResultHistoryOutputFromTable.YAxisName + (column + 1);
            return columnNames;
        }
        private static void ValidateRegion(FeResults result, ResultHistoryOutput output, string selectionTarget,
                                           params RegionTypeEnum[] namedRegionTypes)
        {
            if (output.RegionType == RegionTypeEnum.Selection)
            {
                if (output.CreationData == null || output.CreationIds == null || output.CreationIds.Length == 0)
                    throw new PythonAPIException("The history output selection must contain at least one item.");
                return;
            }
            if (!namedRegionTypes.Contains(output.RegionType))
                throw new PythonAPIException("The region type '" + output.RegionType +
                                             "' is not supported by this history output.");
            ValidateRequiredString(output.RegionName, "region name");
            if (output.RegionType == RegionTypeEnum.NodeSetName)
            {
                FeNodeSet nodeSet;
                if (!result.Mesh.NodeSets.TryGetValue(output.RegionName, out nodeSet) || nodeSet.Internal)
                    throw new PythonAPIException("The node set '" + output.RegionName +
                                                 "' does not exist in the current result.");
                if (nodeSet.Labels == null || nodeSet.Labels.Length == 0)
                    throw new PythonAPIException("The node set '" + output.RegionName + "' is empty.");
            }
            else if (output.RegionType == RegionTypeEnum.ElementSetName)
            {
                FeElementSet elementSet;
                if (!result.Mesh.ElementSets.TryGetValue(output.RegionName, out elementSet) || elementSet.Internal)
                    throw new PythonAPIException("The element set '" + output.RegionName +
                                                 "' does not exist in the current result.");
                if (elementSet.Labels == null || elementSet.Labels.Length == 0)
                    throw new PythonAPIException("The element set '" + output.RegionName + "' is empty.");
            }
            else if (output.RegionType == RegionTypeEnum.SurfaceName)
            {
                FeSurface surface;
                if (!result.Mesh.Surfaces.TryGetValue(output.RegionName, out surface) || surface.Internal)
                    throw new PythonAPIException("The surface '" + output.RegionName +
                                                 "' does not exist in the current result.");
                if (selectionTarget == "nodes")
                {
                    FeNodeSet surfaceNodeSet;
                    if (surface.NodeSetName == null ||
                        !result.Mesh.NodeSets.TryGetValue(surface.NodeSetName, out surfaceNodeSet) ||
                        surfaceNodeSet.Labels == null || surfaceNodeSet.Labels.Length == 0)
                        throw new PythonAPIException("The surface '" + output.RegionName +
                                                     "' does not contain any nodes.");
                }
                else if (surface.FaceIds == null || surface.FaceIds.Length == 0)
                    throw new PythonAPIException("The surface '" + output.RegionName +
                                                 "' does not contain any faces.");
            }
        }
        private static void ValidateFieldComponents(FeResults result, string fieldName, string[] componentNames)
        {
            ValidateRequiredString(fieldName, "field_name");
            ValidateStringList(componentNames, "component_names");
            if (!result.GetAllFieldNames().Contains(fieldName))
                throw new PythonAPIException("The source field '" + fieldName + "' does not exist in the result.");
            string[] existingComponents = result.GetFieldComponentNames(fieldName);
            foreach (string componentName in componentNames)
            {
                if (existingComponents == null || !existingComponents.Contains(componentName))
                    throw new PythonAPIException("The component '" + componentName +
                                                 "' does not exist in field '" + fieldName + "'.");
            }
        }
        private static void ValidateDeformationField(FeResults result, string deformationVariableName)
        {
            ValidateDeformationVariableName(deformationVariableName);
            string fieldName = FeResults.GetPossibleDeformationFieldOutputNamesMap()[deformationVariableName];
            if (!result.GetAllFieldNames().Contains(fieldName))
                throw new PythonAPIException("The deformation field '" + fieldName +
                                             "' does not exist in the result.");
        }
        private static void ValidateSelection(SelectionAPI selection, string target)
        {
            if (selection == null) throw new PythonAPIException("The argument 'selection' must not be None.");
            if (selection.view != "results")
                throw new PythonAPIException("The selection belongs to the '" + selection.view +
                                             "' view, but a result history output requires a 'results' selection.");
            if (selection.target != target)
                throw new PythonAPIException("The selection target must be '" + target + "' for this history output.");
        }
        private static void ValidateRegionArgument(object region, string selectionTarget,
                                                   params RegionTypeEnum[] namedRegionTypes)
        {
            if (region is SelectionAPI selection)
            {
                ValidateSelection(selection, selectionTarget);
                return;
            }
            if (region is ResultMeshRegionAPI meshRegion)
            {
                ValidateNamedRegionType(meshRegion.RegionType, namedRegionTypes);
                return;
            }
            if (region is RegionAPI named)
            {
                ValidateNamedRegionType(named.RegionType, namedRegionTypes);
                return;
            }
            if (region == null) throw new PythonAPIException("The argument 'region' must not be None.");
            throw new PythonAPIException("The argument 'region' must be a selection created by a " +
                                         "pmx.create.selection_* function, a named region created by " +
                                         "pmx.create.region, or a result node set, element set or surface.");
        }
        private static void ValidateNamedRegionType(RegionTypeEnum regionType, RegionTypeEnum[] namedRegionTypes)
        {
            if (namedRegionTypes.Contains(regionType)) return;
            //
            if (namedRegionTypes.Length == 0)
                throw new PythonAPIException("The argument 'region' accepts only a selection for this history " +
                                             "output, but a " + regionType + " region was given.");
            string[] names = new string[namedRegionTypes.Length];
            for (int i = 0; i < names.Length; i++) names[i] = namedRegionTypes[i].ToString();
            throw new PythonAPIException("The argument 'region' accepts a selection or a " + string.Join(", ", names) +
                                         " region, but a " + regionType + " region was given.");
        }
        private static IntegralTypeEnum ParseIntegralType(string integralType)
        {
            return PythonValueConverter.ToEnum<IntegralTypeEnum>(integralType, "integral_type");
        }
        private static IntegralResultEnum ParseIntegralResult(string integralResult)
        {
            if (integralResult == null) return IntegralResultEnum.Geometry;
            //
            return PythonValueConverter.ToEnum<IntegralResultEnum>(integralResult, "integral_result");
        }
        private static IntegrationScaleType ParseCrossSection(string crossSection)
        {
            if (crossSection == null) return IntegrationScaleType.None;
            //
            return PythonValueConverter.ToEnum<IntegrationScaleType>(crossSection, "cross_section");
        }
        private static void ValidateCrossSection(IntegralTypeEnum integralType, IntegrationScaleType crossSection,
                                                 double thickness)
        {
            // Out-of-plane scaling only makes sense for the line and (2D) volume integrals
            if (integralType == IntegralTypeEnum.Surface && crossSection != IntegrationScaleType.None)
                throw new PythonAPIException("The 'cross_section' scaling is only supported for the Line and " +
                                             "Volume integral types.");
            if (crossSection == IntegrationScaleType.Thickness && thickness <= 0)
                throw new PythonAPIException("The argument 'thickness' must be larger than zero when " +
                                             "cross_section is Thickness.");
        }
        private static void ValidateDeformationVariableName(string deformationVariableName)
        {
            ValidateRequiredString(deformationVariableName, "deformation_variable_name");
            if (!FeResults.GetPossibleDeformationFieldOutputNamesMap().ContainsKey(deformationVariableName))
                throw new PythonAPIException("Unsupported deformation_variable_name '" +
                                             deformationVariableName + "'.");
        }
        private static void ValidateNameSyntax(string name)
        {
            ValidateNameSyntax(name, null);
        }
        // The model trims a name it is given, so a padded name is refused rather than stored under another spelling
        private static void ValidateNameSyntax(string name, string allowedExistingName)
        {
            PythonNameComparer.ValidateFormat(name, "name", allowedExistingName);
        }
        private static void ValidateRequiredString(string value, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new PythonAPIException("The argument '" + argumentName + "' must be a non-empty string.");
        }
        private static void ValidateStringList(string[] values, string argumentName)
        {
            if (values == null || values.Length == 0)
                throw new PythonAPIException("The argument '" + argumentName +
                                             "' must contain at least one item.");
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
                                             "Get pmx.results.current again before modifying history outputs.");
            return result;
        }
        private void EnsureCommandsAvailable()
        {
            if (_commandGateway == null)
                throw new PythonAPIException("Result modifications are not available in this Python API context.");
        }
    }
}
