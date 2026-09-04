// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using APIs.Python;
using CaeGlobals;
using CaeMesh;
using CaeModel;

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
        public void ExportToStl(string[] partNames, string fileName)
        {
            _controller.ExportToStlCommand(partNames, fileName);
        }
        public void ExportTo3mf(string fileName, double edgeThicknessRatio)
        {
            // Export3mfFileProperties lives in this project, so the API passes its values as primitives
            Export3mfFileProperties properties = new Export3mfFileProperties();
            properties.FileName = fileName;
            properties.EdgeThicknessRatio = edgeThicknessRatio;
            _controller.ExportTo3mfCommand(properties);
        }
        public void ExportPartsAsGmshMesh(string fileName)
        {
            _controller.ExportPartsAsGmshMeshCommand(fileName);
        }
        public void ExportPartsAsMmgMesh(string[] partNames, string fileName, bool combine)
        {
            _controller.ExportPartsAsMmgMeshCommand(partNames, fileName, combine);
        }
        public void ExportCadPartsAsStep(string[] partNames, string fileName)
        {
            _controller.ExportCADGeometryPartsAsStepCommand(partNames, fileName);
        }
        public void ExportCadPartsAsBrep(string[] partNames, string fileName)
        {
            _controller.ExportCADGeometryPartsAsBrepCommand(partNames, fileName);
        }
        public void ExportDeformedPartsToCalculix(string[] partNames, string fileName)
        {
            _controller.ExportDeformedPartsToCalculixCommand(partNames, fileName);
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
