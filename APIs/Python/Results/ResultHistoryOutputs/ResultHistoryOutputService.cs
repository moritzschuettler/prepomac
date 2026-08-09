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
        public void RemoveHistoryOutputs(string resultName, object historyOutputNames)
        {
            string[] names = PythonStringListConverter.ToStringArray(historyOutputNames, "history_output_names");
            ValidateStringList(names, "history_output_names");
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
            ValidateNameSyntax(output.Name);
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
                if (!namedRegionTypes.Contains(meshRegion.RegionType))
                    throw new PythonAPIException("The region type '" + meshRegion.RegionType +
                                                 "' is not supported by this history output.");
                return;
            }
            if (region == null) throw new PythonAPIException("The argument 'region' must not be None.");
            throw new PythonAPIException("The argument 'region' must be a Selection, NodeSet, ElementSet, or Surface.");
        }
        private static IntegralTypeEnum ParseIntegralType(string integralType)
        {
            if (integralType == "line") return IntegralTypeEnum.Line;
            if (integralType == "surface") return IntegralTypeEnum.Surface;
            if (integralType == "volume") return IntegralTypeEnum.Volume;
            throw new PythonAPIException("The argument 'integral_type' must be 'line', 'surface' or 'volume'.");
        }
        private static IntegralResultEnum ParseIntegralResult(string integralResult)
        {
            if (integralResult == null || integralResult == "geometry") return IntegralResultEnum.Geometry;
            if (integralResult == "field") return IntegralResultEnum.Field;
            if (integralResult == "field_by_geometry") return IntegralResultEnum.FieldByGeometry;
            throw new PythonAPIException("The argument 'integral_result' must be 'geometry', 'field' or " +
                                         "'field_by_geometry'.");
        }
        private static IntegrationScaleType ParseCrossSection(string crossSection)
        {
            if (crossSection == null || crossSection == "none") return IntegrationScaleType.None;
            if (crossSection == "thickness") return IntegrationScaleType.Thickness;
            if (crossSection == "axisymmetric") return IntegrationScaleType.Axisymmetric;
            throw new PythonAPIException("The argument 'cross_section' must be 'none', 'thickness' or 'axisymmetric'.");
        }
        private static void ValidateCrossSection(IntegralTypeEnum integralType, IntegrationScaleType crossSection,
                                                 double thickness)
        {
            // Out-of-plane scaling only makes sense for the line and (2D) volume integrals
            if (integralType == IntegralTypeEnum.Surface && crossSection != IntegrationScaleType.None)
                throw new PythonAPIException("The 'cross_section' scaling is only supported for 'line' and " +
                                             "'volume' integral types.");
            if (crossSection == IntegrationScaleType.Thickness && thickness <= 0)
                throw new PythonAPIException("The argument 'thickness' must be larger than zero when " +
                                             "cross_section is 'thickness'.");
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
            ValidateRequiredString(name, "name");
            if (!NamedClass.CheckName(name, null))
                throw new PythonAPIException("The history output name contains spaces or unsupported special " +
                                             "characters.");
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
