// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using APIs.Python;
using CaeGlobals;
using CaeMesh;
using CaeModel;
using FileInOut.Output;
using UserControls;

namespace PrePoMax.Python
{
    internal class ControllerPythonModelGateway : IPythonModelGateway
    {
        // Variables
        private readonly Controller _controller;


        // Constructors
        public ControllerPythonModelGateway(Controller controller)
        {
            _controller = controller;
        }


        // Methods
        public FeModel GetModel()
        {
            return _controller.Model;
        }
        public string[] GetAllMeshEntityNames()
        {
            return _controller.GetAllMeshEntityNames();
        }
        public bool IsInsideRecordedScript()
        {
            return _controller.IsCommandRecordingSuppressed;
        }
        public void RenameModelItem(NamedClass item, string newName, string stepName)
        {
            // The view is passed explicitly so that a script does not depend on the view the user left behind
            _controller.RenameCommand(item, newName, stepName, ViewGeometryModelResults.Model);
        }
        public void ActivateDeactivateItems(NamedClass[] items, bool activate, string[] stepNames)
        {
            _controller.ActivateDeactivateMultipleCommand(items, activate, stepNames);
        }
        public string GetRecognizedMeshType(string partName)
        {
            return _controller.GetRecognizedMeshType(partName).ToString();
        }
        // Document
        public void NewModel(ModelSpaceEnum modelSpace, UnitSystemType unitSystemType)
        {
            _controller.New();
            _controller.SetNewModelPropertiesCommand(modelSpace, unitSystemType);
        }
        public void OpenFile(string fileName)
        {
            _controller.OpenFileCommand(fileName);
        }
        public void ImportFile(string fileName, bool onlyMaterials)
        {
            _controller.ImportFileCommand(fileName, onlyMaterials);
        }
        public void SaveToPmx(string fileName)
        {
            _controller.SaveToPmxCommand(fileName);
        }
        public void ExportToCalculix(string fileName)
        {
            _controller.ExportToCalculixCommand(fileName);
        }
        public void ExportToAbaqus(string fileName)
        {
            _controller.ExportToAbaqusCommand(fileName);
        }
        public void ExportTo3mf(string fileName, double edgeThicknessRatio)
        {
            // Export3mfFileProperties lives in this project, so the API passes its values as primitives
            Export3mfFileProperties properties = new Export3mfFileProperties();
            properties.FileName = fileName;
            properties.EdgeThicknessRatio = edgeThicknessRatio;
            _controller.ExportTo3mfCommand(properties);
        }
        public void ExportGeometry(string[] partNames, string fileName, GeometryExportFormatEnum format)
        {
            _controller.ExportGeometryPartsCommand(partNames, fileName, format);
        }
        public void ExportMesh(string[] partNames, string fileName, MeshExportFormatEnum format)
        {
            _controller.ExportMeshPartsCommand(partNames, fileName, format);
        }
        public void ExportResults(string[] partNames, string fileName, ResultsExportFormatEnum format)
        {
            _controller.ExportResultPartsCommand(partNames, fileName, format);
        }
        // Model properties
        public void SetNewModelProperties(ModelSpaceEnum modelSpace, UnitSystemType unitSystemType)
        {
            _controller.SetNewModelPropertiesCommand(modelSpace, unitSystemType);
        }
        public void ReplaceModelProperties(string newModelName, ModelProperties newModelProperties)
        {
            _controller.ReplaceModelPropertiesCommand(newModelName, newModelProperties);
        }
        public IOrderedDictionaryItemLayout<string> GetTreeLayout(string view, string itemType, string parentName)
        {
            IOrderedDictionaryItemLayout<string> dictionary;
            try
            {
                if (!_controller.TryGetOrderedDictionaryItemLayout(GetTreeItemType(itemType), GetTreeView(view),
                                                                   parentName, out dictionary))
                    return null;
            }
            // The controller reports a collection its view does not currently hold as not supported
            catch (NotSupportedException)
            {
                return null;
            }
            return dictionary;
        }
        public void ChangeTreeLayout(string action, string view, string itemType, string parentName,
                                     string[] itemNames, string[] groupIds, string groupId, string targetGroupId,
                                     int targetIndex, string groupName)
        {
            // The API keeps the tree vocabulary in its own words, and this is where it becomes the tree types
            ViewGeometryModelResults treeView = GetTreeView(view);
            ViewType viewValue = treeView == ViewGeometryModelResults.Geometry ? ViewType.Geometry
                                 : treeView == ViewGeometryModelResults.Model ? ViewType.Model : ViewType.Results;
            OrderedDictionaryTreeLayoutAction actionValue =
                (OrderedDictionaryTreeLayoutAction)Enum.Parse(typeof(OrderedDictionaryTreeLayoutAction), action, true);
            Type itemTypeValue = GetTreeItemType(itemType);
            //
            Guid? target = string.IsNullOrEmpty(targetGroupId) ? (Guid?)null : Guid.Parse(targetGroupId);
            OrderedDictionaryTreeLayoutChange change;
            if (actionValue == OrderedDictionaryTreeLayoutAction.CreateGroup)
            {
                change = OrderedDictionaryTreeLayoutChange.CreateGroup(viewValue, itemTypeValue, parentName,
                                                                       itemNames, Guid.Parse(groupId), target,
                                                                       targetIndex, groupName);
            }
            else if (actionValue == OrderedDictionaryTreeLayoutAction.RenameGroup)
            {
                change = OrderedDictionaryTreeLayoutChange.RenameGroup(viewValue, itemTypeValue, parentName,
                                                                       Guid.Parse(groupId), groupName);
            }
            else if (actionValue == OrderedDictionaryTreeLayoutAction.RemoveGroup)
            {
                Guid[] ids = new Guid[groupIds == null ? 0 : groupIds.Length];
                for (int i = 0; i < ids.Length; i++) ids[i] = Guid.Parse(groupIds[i]);
                change = OrderedDictionaryTreeLayoutChange.RemoveGroups(viewValue, itemTypeValue, parentName, ids);
            }
            else if (actionValue == OrderedDictionaryTreeLayoutAction.MoveItems)
            {
                change = OrderedDictionaryTreeLayoutChange.MoveItems(viewValue, itemTypeValue, parentName,
                                                                     itemNames, target, targetIndex);
            }
            else
            {
                change = OrderedDictionaryTreeLayoutChange.MoveGroup(viewValue, itemTypeValue, parentName,
                                                                     Guid.Parse(groupId), target, targetIndex);
            }
            _controller.ChangeOrderedDictionaryItemLayoutCommand(change);
        }
        public void SetTreeLayout(string view, string itemType, string parentName,
                                  OrderedDictionaryItemLayout<string> layout)
        {
            ViewGeometryModelResults treeView = GetTreeView(view);
            ViewType viewValue = treeView == ViewGeometryModelResults.Geometry ? ViewType.Geometry
                                 : treeView == ViewGeometryModelResults.Model ? ViewType.Model : ViewType.Results;
            OrderedDictionaryTreeLayoutChange change =
                OrderedDictionaryTreeLayoutChange.SetLayout(viewValue, GetTreeItemType(itemType), parentName, layout);
            _controller.ChangeOrderedDictionaryItemLayoutCommand(change);
        }
        // The tree views are named the way the selections and pmx.view name them
        private static ViewGeometryModelResults GetTreeView(string view)
        {
            if (view == "geometry") return ViewGeometryModelResults.Geometry;
            if (view == "femodel") return ViewGeometryModelResults.Model;
            if (view == "results") return ViewGeometryModelResults.Results;
            //
            throw new PythonAPIException("The argument 'view' must be 'geometry', 'femodel' or 'results', not '" +
                                         view + "'.");
        }
        private static Type GetTreeItemType(string itemType)
        {
            if (IsItemType(itemType, "parts")) return typeof(CaeMesh.BasePart);
            if (IsItemType(itemType, "mesh_setup_items")) return typeof(CaeMesh.MeshSetupItem);
            if (IsItemType(itemType, "node_sets")) return typeof(CaeMesh.FeNodeSet);
            if (IsItemType(itemType, "element_sets")) return typeof(CaeMesh.FeElementSet);
            if (IsItemType(itemType, "surfaces")) return typeof(CaeMesh.FeSurface);
            if (IsItemType(itemType, "reference_points")) return typeof(CaeMesh.FeReferencePoint);
            if (IsItemType(itemType, "coordinate_systems")) return typeof(CaeMesh.CoordinateSystem);
            if (IsItemType(itemType, "materials")) return typeof(Material);
            if (IsItemType(itemType, "sections")) return typeof(Section);
            if (IsItemType(itemType, "constraints")) return typeof(CaeModel.Constraint);
            if (IsItemType(itemType, "surface_interactions")) return typeof(SurfaceInteraction);
            if (IsItemType(itemType, "contact_pairs")) return typeof(ContactPair);
            if (IsItemType(itemType, "distributions")) return typeof(Distribution);
            if (IsItemType(itemType, "amplitudes")) return typeof(Amplitude);
            if (IsItemType(itemType, "initial_conditions")) return typeof(InitialCondition);
            if (IsItemType(itemType, "jobs")) return typeof(CaeJob.AnalysisJob);
            if (IsItemType(itemType, "history_outputs")) return typeof(HistoryOutput);
            if (IsItemType(itemType, "field_outputs")) return typeof(FieldOutput);
            if (IsItemType(itemType, "boundary_conditions")) return typeof(BoundaryCondition);
            if (IsItemType(itemType, "loads")) return typeof(Load);
            if (IsItemType(itemType, "defined_fields")) return typeof(DefinedField);
            //
            throw new PythonAPIException("The model tree collection '" + itemType + "' given as the argument " +
                                         "'item_type' cannot be organized.");
        }
        // The journal needs the same vocabulary in the other direction, and the two must agree, so the
        // inverse lives next to the map
        public static string GetTreeItemTypeName(Type itemType)
        {
            if (itemType == typeof(CaeMesh.BasePart)) return "parts";
            if (itemType == typeof(CaeMesh.MeshSetupItem)) return "mesh_setup_items";
            if (itemType == typeof(CaeMesh.FeNodeSet)) return "node_sets";
            if (itemType == typeof(CaeMesh.FeElementSet)) return "element_sets";
            if (itemType == typeof(CaeMesh.FeSurface)) return "surfaces";
            if (itemType == typeof(CaeMesh.FeReferencePoint)) return "reference_points";
            if (itemType == typeof(CaeMesh.CoordinateSystem)) return "coordinate_systems";
            if (itemType == typeof(Material)) return "materials";
            if (itemType == typeof(Section)) return "sections";
            if (itemType == typeof(CaeModel.Constraint)) return "constraints";
            if (itemType == typeof(SurfaceInteraction)) return "surface_interactions";
            if (itemType == typeof(ContactPair)) return "contact_pairs";
            if (itemType == typeof(Distribution)) return "distributions";
            if (itemType == typeof(Amplitude)) return "amplitudes";
            if (itemType == typeof(InitialCondition)) return "initial_conditions";
            if (itemType == typeof(CaeJob.AnalysisJob)) return "jobs";
            if (itemType == typeof(HistoryOutput)) return "history_outputs";
            if (itemType == typeof(FieldOutput)) return "field_outputs";
            if (itemType == typeof(BoundaryCondition)) return "boundary_conditions";
            if (itemType == typeof(Load)) return "loads";
            if (itemType == typeof(DefinedField)) return "defined_fields";
            //
            throw new CaeException("The model tree collection of '" + itemType.Name +
                                   "' cannot be organized.");
        }
        private static bool IsItemType(string itemType, string name)
        {
            return string.Equals(itemType, name, StringComparison.OrdinalIgnoreCase);
        }

        // CalculiX keyword editor
        public List<FileInOut.Output.Calculix.CalculixKeyword> GetCalculixModelKeywords()
        {
            return _controller.GetCalculixModelKeywords();
        }
        public void SetCalculixUserKeywords(OrderedDictionary<int[], FileInOut.Output.Calculix.CalculixUserKeyword> keywords)
        {
            _controller.SetCalculixUserKeywordsCommand(keywords);
        }
        public void SetCalculixKeywordEditorState(OrderedDictionary<int[], FileInOut.Output.Calculix.CalculixUserKeyword> keywords,
                                                  List<FileInOut.Output.Calculix.CalculixKeywordOverride> overrides)
        {
            _controller.SetCalculixKeywordEditorStateCommand(keywords, overrides);
        }
        // Parameters
        public void AddParameter(EquationParameter parameter)
        {
            _controller.AddParameterCommand(parameter);
        }
        public void ReplaceParameter(string oldParameterName, EquationParameter parameter)
        {
            _controller.ReplaceParameterCommand(oldParameterName, parameter);
        }
        public void RemoveParameters(string[] parameterNames)
        {
            _controller.RemoveParametersCommand(parameterNames);
        }
        public void SetParameters(EquationParameter[] parameters)
        {
            _controller.SetParametersCommand(parameters);
        }
        public void ImportParameters(string fileName)
        {
            _controller.ImportParametersCommand(fileName);
        }
        public void ExportParameters(string fileName)
        {
            _controller.ExportParametersCommand(fileName);
        }
        // Amplitudes, distributions and initial conditions
        public void AddAmplitude(Amplitude amplitude)
        {
            _controller.AddAmplitudeCommand(amplitude);
        }
        public void ReplaceAmplitude(string oldAmplitudeName, Amplitude amplitude)
        {
            _controller.ReplaceAmplitudeCommand(oldAmplitudeName, amplitude);
        }
        public void DuplicateAmplitudes(string[] amplitudeNames)
        {
            _controller.DuplicateAmplitudesCommand(amplitudeNames);
        }
        public void RemoveAmplitudes(string[] amplitudeNames)
        {
            _controller.RemoveAmplitudesCommand(amplitudeNames);
        }
        public void AddDistribution(Distribution distribution)
        {
            _controller.AddDistributionCommand(distribution);
        }
        public void ReplaceDistribution(string oldDistributionName, Distribution distribution)
        {
            _controller.ReplaceDistributionCommand(oldDistributionName, distribution);
        }
        public void DuplicateDistributions(string[] distributionNames)
        {
            _controller.DuplicateDistributionsCommand(distributionNames);
        }
        public void RemoveDistributions(string[] distributionNames)
        {
            _controller.RemoveDistributionsCommand(distributionNames);
        }
        public void AddInitialCondition(InitialCondition initialCondition)
        {
            _controller.AddInitialConditionCommand(initialCondition);
        }
        public void ReplaceInitialCondition(string oldInitialConditionName, InitialCondition initialCondition)
        {
            _controller.ReplaceInitialConditionCommand(oldInitialConditionName, initialCondition);
        }
        public void DuplicateInitialConditions(string[] initialConditionNames)
        {
            _controller.DuplicateInitialConditionsCommand(initialConditionNames);
        }
        public void RemoveInitialConditions(string[] initialConditionNames)
        {
            _controller.RemoveInitialConditionsCommand(initialConditionNames);
        }
        public void HideInitialConditions(string[] initialConditionNames)
        {
            _controller.HideInitialConditionsCommand(initialConditionNames);
        }
        public void ShowInitialConditions(string[] initialConditionNames)
        {
            _controller.ShowInitialConditionsCommand(initialConditionNames);
        }
        // Materials and sections
        public void AddMaterial(Material material)
        {
            _controller.AddMaterialCommand(material);
        }
        public void ReplaceMaterial(string oldMaterialName, Material material)
        {
            _controller.ReplaceMaterialCommand(oldMaterialName, material);
        }
        public void DuplicateMaterials(string[] materialNames)
        {
            _controller.DuplicateMaterialsCommand(materialNames);
        }
        public void RemoveMaterials(string[] materialNames)
        {
            _controller.RemoveMaterialsCommand(materialNames);
        }
        public void AddSection(Section section)
        {
            _controller.AddSectionCommand(section);
        }
        public void ReplaceSection(string oldSectionName, Section section)
        {
            _controller.ReplaceSectionCommand(oldSectionName, section);
        }
        public void DuplicateSections(string[] sectionNames)
        {
            _controller.DuplicateSectionsCommand(sectionNames);
        }
        public void RemoveSections(string[] sectionNames)
        {
            _controller.RemoveSectionsCommand(sectionNames);
        }
        // Node sets
        // Constraints
        public void AddConstraint(Constraint constraint)
        {
            _controller.AddConstraintCommand(constraint);
        }
        public void ReplaceConstraint(string oldConstraintName, Constraint constraint)
        {
            _controller.ReplaceConstraintCommand(oldConstraintName, constraint);
        }
        public void DuplicateConstraints(string[] constraintNames)
        {
            _controller.DuplicateConstraintsCommand(constraintNames);
        }
        public void SwapMasterSlaveConstraints(string[] constraintNames)
        {
            _controller.SwapMasterSlaveConstraintsCommand(constraintNames);
        }
        public void MergeByMasterSlaveConstraints(string[] constraintNames)
        {
            _controller.MergeByMasterSlaveConstraintsCommand(constraintNames);
        }
        public void RemoveConstraints(string[] constraintNames)
        {
            _controller.RemoveConstraintsCommand(constraintNames);
        }
        public void HideConstraints(string[] constraintNames)
        {
            _controller.HideConstraintsCommand(constraintNames);
        }
        public void ShowConstraints(string[] constraintNames)
        {
            _controller.ShowConstraintsCommand(constraintNames);
        }
        // Surface interactions and contact pairs
        public void AddSurfaceInteraction(SurfaceInteraction surfaceInteraction)
        {
            _controller.AddSurfaceInteractionCommand(surfaceInteraction);
        }
        public void ReplaceSurfaceInteraction(string oldSurfaceInteractionName,
                                              SurfaceInteraction surfaceInteraction)
        {
            _controller.ReplaceSurfaceInteractionCommand(oldSurfaceInteractionName, surfaceInteraction);
        }
        public void DuplicateSurfaceInteractions(string[] surfaceInteractionNames)
        {
            _controller.DuplicateSurfaceInteractionsCommand(surfaceInteractionNames);
        }
        public void RemoveSurfaceInteractions(string[] surfaceInteractionNames)
        {
            _controller.RemoveSurfaceInteractionsCommand(surfaceInteractionNames);
        }
        public void AddContactPair(ContactPair contactPair)
        {
            _controller.AddContactPairCommand(contactPair);
        }
        public void ReplaceContactPair(string oldContactPairName, ContactPair contactPair)
        {
            _controller.ReplaceContactPairCommand(oldContactPairName, contactPair);
        }
        public void DuplicateContactPairs(string[] contactPairNames)
        {
            _controller.DuplicateContactPairsCommand(contactPairNames);
        }
        public void SwapMasterSlaveContactPairs(string[] contactPairNames)
        {
            _controller.SwapMasterSlaveContactPairsCommand(contactPairNames);
        }
        public void MergeByMasterSlaveContactPairs(string[] contactPairNames)
        {
            _controller.MergeByMasterSlaveContactPairsCommand(contactPairNames);
        }
        public void RemoveContactPairs(string[] contactPairNames)
        {
            _controller.RemoveContactPairsCommand(contactPairNames);
        }
        public void HideContactPairs(string[] contactPairNames)
        {
            _controller.HideContactPairsCommand(contactPairNames);
        }
        public void ShowContactPairs(string[] contactPairNames)
        {
            _controller.ShowContactPairsCommand(contactPairNames);
        }
        //
        public void AddNodeSet(FeNodeSet nodeSet)
        {
            _controller.AddNodeSetCommand(nodeSet);
        }
        public void ReplaceNodeSet(string oldNodeSetName, FeNodeSet nodeSet)
        {
            _controller.ReplaceNodeSetCommand(oldNodeSetName, nodeSet);
        }
        public void DuplicateNodeSets(string[] nodeSetNames)
        {
            _controller.DuplicateNodeSetsCommand(nodeSetNames);
        }
        public void RemoveNodeSets(string[] nodeSetNames)
        {
            _controller.RemoveNodeSetsCommand(nodeSetNames);
        }
        public void AddElementSet(FeElementSet elementSet)
        {
            _controller.AddElementSetCommand(elementSet);
        }
        public void ReplaceElementSet(string oldElementSetName, FeElementSet elementSet)
        {
            _controller.ReplaceElementSetCommand(oldElementSetName, elementSet);
        }
        public void DuplicateElementSets(string[] elementSetNames)
        {
            _controller.DuplicateElementSetsCommand(elementSetNames);
        }
        public void RemoveElementSets(string[] elementSetNames)
        {
            _controller.RemoveElementSetsCommand(elementSetNames);
        }
        public void ConvertElementSetsToMeshParts(string[] elementSetNames)
        {
            _controller.ConvertElementSetsToMeshPartsCommand(elementSetNames);
        }
        public void AddSurface(FeSurface surface)
        {
            _controller.AddSurfaceCommand(surface);
        }
        public void ReplaceSurface(string oldSurfaceName, FeSurface surface)
        {
            _controller.ReplaceSurfaceCommand(oldSurfaceName, surface);
        }
        public void DuplicateSurfaces(string[] surfaceNames)
        {
            _controller.DuplicateSurfacesCommand(surfaceNames);
        }
        public void RemoveSurfaces(string[] surfaceNames)
        {
            _controller.RemoveSurfacesCommand(surfaceNames);
        }
        public void AddModelReferencePoint(FeReferencePoint referencePoint)
        {
            _controller.AddModelReferencePointCommand(referencePoint);
        }
        public void ReplaceModelReferencePoint(string oldReferencePointName, FeReferencePoint referencePoint)
        {
            _controller.ReplaceModelReferencePointCommand(oldReferencePointName, referencePoint);
        }
        public void DuplicateModelReferencePoints(string[] referencePointNames)
        {
            _controller.DuplicateModelReferencePointsCommand(referencePointNames);
        }
        public void RemoveModelReferencePoints(string[] referencePointNames)
        {
            _controller.RemoveModelReferencePointsCommand(referencePointNames);
        }
        public void HideModelReferencePoints(string[] referencePointNames)
        {
            _controller.HideModelReferencePointsCommand(referencePointNames);
        }
        public void ShowModelReferencePoints(string[] referencePointNames)
        {
            _controller.ShowModelReferencePointsCommand(referencePointNames);
        }
        // Steps
        public SolverTypeEnum GetDefaultSolverType()
        {
            return _controller.Settings.Solver.CalculixDefaultSolverType;
        }
        // The same check FrmStep runs on OK, returned as a message so that the API can name its arguments
        public string GetCyclicSymmetryModesError(FrequencyStep step)
        {
            CyclicSymmetry cyclicSymmetry = null;
            bool containsAxisNodes = false;
            foreach (var entry in _controller.Model.Constraints)
            {
                if (entry.Value is CyclicSymmetry candidate && candidate.Active)
                {
                    if (cyclicSymmetry == null) cyclicSymmetry = candidate;
                    containsAxisNodes |= _controller.CyclicSymmetryContainsAxisNodes(candidate);
                }
            }
            if (cyclicSymmetry == null) return null;
            //
            try
            {
                _controller.Model.CheckCyclicSymmetryCompatibility(cyclicSymmetry, cyclicSymmetry.Name);
                step.CheckCyclicSymmetryModes(cyclicSymmetry.NumberOfSectors, containsAxisNodes);
            }
            catch (CaeException ex)
            {
                return ex.Message;
            }
            return null;
        }
        public void AddStep(Step step, bool copyBoundaryConditionsAndLoads)
        {
            _controller.AddStepCommand(step, copyBoundaryConditionsAndLoads);
        }
        public void ReplaceStep(string oldStepName, Step step)
        {
            _controller.ReplaceStepCommand(oldStepName, step);
        }
        public void DuplicateSteps(string[] stepNames)
        {
            _controller.DuplicateStepsCommand(stepNames);
        }
        public void RemoveSteps(string[] stepNames)
        {
            _controller.RemoveStepsCommand(stepNames);
        }
        public void MoveSteps(string[] stepNames, int targetIndex)
        {
            _controller.MoveStepsCommand(stepNames, targetIndex);
        }
        public void ReplaceStepControls(string stepName, StepControls stepControls)
        {
            _controller.ReplaceStepControlsCommand(stepName, stepControls);
        }
        // Boundary conditions
        public void AddBoundaryCondition(string stepName, BoundaryCondition boundaryCondition)
        {
            _controller.AddBoundaryConditionCommand(stepName, boundaryCondition);
        }
        public void ReplaceBoundaryCondition(string stepName, string oldBoundaryConditionName,
                                             BoundaryCondition boundaryCondition)
        {
            _controller.ReplaceBoundaryConditionCommand(stepName, oldBoundaryConditionName, boundaryCondition);
        }
        public void DuplicateBoundaryConditions(string stepName, string[] boundaryConditionNames)
        {
            _controller.DuplicateBoundaryConditionsCommand(stepName, boundaryConditionNames);
        }
        public void PropagateBoundaryCondition(string stepName, string boundaryConditionName)
        {
            _controller.PropagateBoundaryConditionCommand(stepName, boundaryConditionName);
        }
        public void RemoveBoundaryConditions(string stepName, string[] boundaryConditionNames)
        {
            _controller.RemoveBoundaryConditionsCommand(stepName, boundaryConditionNames);
        }
        public void HideBoundaryConditions(string stepName, string[] boundaryConditionNames)
        {
            _controller.HideBoundaryConditionCommand(stepName, boundaryConditionNames);
        }
        public void ShowBoundaryConditions(string stepName, string[] boundaryConditionNames)
        {
            _controller.ShowBoundaryConditionCommand(stepName, boundaryConditionNames);
        }
        // Loads
        public void AddLoad(string stepName, Load load)
        {
            _controller.AddLoadCommand(stepName, load);
        }
        public void ReplaceLoad(string stepName, string oldLoadName, Load load)
        {
            _controller.ReplaceLoadCommand(stepName, oldLoadName, load);
        }
        public void DuplicateLoads(string stepName, string[] loadNames)
        {
            _controller.DuplicateLoadsCommand(stepName, loadNames);
        }
        public void PropagateLoad(string stepName, string loadName)
        {
            _controller.PropagateLoadCommand(stepName, loadName);
        }
        public void RemoveLoads(string stepName, string[] loadNames)
        {
            _controller.RemoveLoadsCommand(stepName, loadNames);
        }
        public void HideLoads(string stepName, string[] loadNames)
        {
            _controller.HideLoadsCommand(stepName, loadNames);
        }
        public void ShowLoads(string stepName, string[] loadNames)
        {
            _controller.ShowLoadsCommand(stepName, loadNames);
        }
        // Field outputs, history outputs and defined fields
        public void AddFieldOutput(string stepName, FieldOutput fieldOutput)
        {
            _controller.AddFieldOutputCommand(stepName, fieldOutput);
        }
        public void ReplaceFieldOutput(string stepName, string oldFieldOutputName, FieldOutput fieldOutput)
        {
            _controller.ReplaceFieldOutputCommand(stepName, oldFieldOutputName, fieldOutput);
        }
        public void DuplicateFieldOutputs(string stepName, string[] fieldOutputNames)
        {
            _controller.DuplicateFieldOutputsCommand(stepName, fieldOutputNames);
        }
        public void PropagateFieldOutput(string stepName, string fieldOutputName)
        {
            _controller.PropagateFieldOutputCommand(stepName, fieldOutputName);
        }
        public void RemoveFieldOutputs(string stepName, string[] fieldOutputNames)
        {
            _controller.RemoveFieldOutputsCommand(stepName, fieldOutputNames);
        }
        public void AddHistoryOutput(string stepName, HistoryOutput historyOutput)
        {
            _controller.AddHistoryOutputCommand(stepName, historyOutput);
        }
        public void ReplaceHistoryOutput(string stepName, string oldHistoryOutputName,
                                         HistoryOutput historyOutput)
        {
            _controller.ReplaceHistoryOutputCommand(stepName, oldHistoryOutputName, historyOutput);
        }
        public void DuplicateHistoryOutputs(string stepName, string[] historyOutputNames)
        {
            _controller.DuplicateHistoryOutputsCommand(stepName, historyOutputNames);
        }
        public void PropagateHistoryOutput(string stepName, string historyOutputName)
        {
            _controller.PropagateHistoryOutputCommand(stepName, historyOutputName);
        }
        public void RemoveHistoryOutputs(string stepName, string[] historyOutputNames)
        {
            _controller.RemoveHistoryOutputsCommand(stepName, historyOutputNames);
        }
        public void AddDefinedField(string stepName, DefinedField definedField)
        {
            _controller.AddDefinedFieldCommand(stepName, definedField);
        }
        public void ReplaceDefinedField(string stepName, string oldDefinedFieldName, DefinedField definedField)
        {
            _controller.ReplaceDefinedFieldCommand(stepName, oldDefinedFieldName, definedField);
        }
        public void DuplicateDefinedFields(string stepName, string[] definedFieldNames)
        {
            _controller.DuplicateDefinedFieldsForStepCommand(stepName, definedFieldNames);
        }
        public void PropagateDefinedField(string stepName, string definedFieldName)
        {
            _controller.PropagateDefinedFieldCommand(stepName, definedFieldName);
        }
        public void RemoveDefinedFields(string stepName, string[] definedFieldNames)
        {
            _controller.RemoveDefinedFieldsForStepCommand(stepName, definedFieldNames);
        }
        public void HideDefinedFields(string stepName, string[] definedFieldNames)
        {
            _controller.HideDefinedFieldsCommand(stepName, definedFieldNames);
        }
        public void ShowDefinedFields(string stepName, string[] definedFieldNames)
        {
            _controller.ShowDefinedFieldsCommand(stepName, definedFieldNames);
        }
        //
        public void AddModelCoordinateSystem(CoordinateSystem coordinateSystem)
        {
            _controller.AddModelCoordinateSystemCommand(coordinateSystem);
        }
        public void ReplaceModelCoordinateSystem(string oldCoordinateSystemName, CoordinateSystem coordinateSystem)
        {
            _controller.ReplaceModelCoordinateSystemCommand(oldCoordinateSystemName, coordinateSystem);
        }
        public void DuplicateModelCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.DuplicateModelCoordinateSystemsCommand(coordinateSystemNames);
        }
        public void RemoveModelCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.RemoveModelCoordinateSystemsCommand(coordinateSystemNames);
        }
        public void HideModelCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.HideModelCoordinateSystemsCommand(coordinateSystemNames);
        }
        public void ShowModelCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.ShowModelCoordinateSystemsCommand(coordinateSystemNames);
        }
    }
}





