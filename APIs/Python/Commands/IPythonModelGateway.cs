// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;
using CaeGlobals;
using CaeMesh;
using CaeModel;
using FileInOut.Output;

namespace APIs.Python
{
    public interface IPythonModelGateway
    {
        // The model instance is replaced by New and Open, so it is always read through the gateway
        FeModel GetModel();
        string[] GetAllMeshEntityNames();
        // Replacing the model also replaces the command collection, which a running script is part of
        bool IsInsideRecordedScript();
        // A recorded script run again by Regenerate, Undo, Redo, Edit History or a batch regeneration
        bool IsRerunningRecordedScript();
        // Changes made since the last save in a session with a window, whose user would lose them; a session without
        // one has nobody whose work it is
        bool HasUnsavedChangesOfUser();
        void RenameModelItem(NamedClass item, string newName, string stepName);
        void ActivateDeactivateItems(NamedClass[] items, bool activate, string[] stepNames);
        string GetRecognizedMeshType(string partName);
        // Document
        void NewModel(ModelSpaceEnum modelSpace, UnitSystemType unitSystemType);
        void OpenFile(string fileName);
        void ImportFile(string fileName, bool onlyMaterials);
        void SaveToPmx(string fileName);
        void ExportToCalculix(string fileName);
        void ExportToAbaqus(string fileName);
        void ExportTo3mf(string fileName, double edgeThicknessRatio);
        void ExportGeometry(string[] partNames, string fileName, GeometryExportFormatEnum format);
        void ExportMesh(string[] partNames, string fileName, MeshExportFormatEnum format);
        // A value that is not given is the one the view shows
        void ExportResults(string[] partNames, string fileName, ResultsExportFormatEnum format, string resultName,
                           int? stepId, int? incrementId, float? deformationFactor);
        // Model properties
        void SetNewModelProperties(ModelSpaceEnum modelSpace, UnitSystemType unitSystemType);
        void ReplaceModelProperties(string newModelName, ModelProperties newModelProperties);
        // Null when the view holds no such collection, as a model without a mesh or without an open result
        IOrderedDictionaryItemLayout<string> GetTreeLayout(string view, string itemType, string parentName);
        void ChangeTreeLayout(string action, string view, string itemType, string parentName, string[] itemNames,
                              string[] groupIds, string groupId, string targetGroupId, int targetIndex, string groupName);
        void SetTreeLayout(string view, string itemType, string parentName, OrderedDictionaryItemLayout<string> layout);
        // A part group renamed in both views at once, recorded as one command as the tree records it
        void SetLinkedPartTreeLayouts(OrderedDictionaryItemLayout<string> geometryLayout,
                                      OrderedDictionaryItemLayout<string> modelLayout);
        // CalculiX keyword editor
        List<FileInOut.Output.Calculix.CalculixKeyword> GetCalculixModelKeywords();
        void SetCalculixUserKeywords(CaeGlobals.OrderedDictionary<int[], FileInOut.Output.Calculix.CalculixUserKeyword> keywords);
        void SetCalculixKeywordEditorState(CaeGlobals.OrderedDictionary<int[], FileInOut.Output.Calculix.CalculixUserKeyword> keywords,
                                           List<FileInOut.Output.Calculix.CalculixKeywordOverride> overrides);
        // Parameters
        void AddParameter(EquationParameter parameter);
        void ReplaceParameter(string oldParameterName, EquationParameter parameter);
        void RemoveParameters(string[] parameterNames);
        void SetParameters(EquationParameter[] parameters);
        void ImportParameters(string fileName);
        void ExportParameters(string fileName);
        // Amplitudes, distributions and initial conditions
        void AddAmplitude(Amplitude amplitude);
        void ReplaceAmplitude(string oldAmplitudeName, Amplitude amplitude);
        void DuplicateAmplitudes(string[] amplitudeNames);
        void RemoveAmplitudes(string[] amplitudeNames);
        //
        void AddDistribution(Distribution distribution);
        void ReplaceDistribution(string oldDistributionName, Distribution distribution);
        void DuplicateDistributions(string[] distributionNames);
        void RemoveDistributions(string[] distributionNames);
        //
        void AddInitialCondition(InitialCondition initialCondition);
        void ReplaceInitialCondition(string oldInitialConditionName, InitialCondition initialCondition);
        void DuplicateInitialConditions(string[] initialConditionNames);
        void RemoveInitialConditions(string[] initialConditionNames);
        void HideInitialConditions(string[] initialConditionNames);
        void ShowInitialConditions(string[] initialConditionNames);
        // Materials and sections
        void AddMaterial(Material material);
        void ReplaceMaterial(string oldMaterialName, Material material);
        void DuplicateMaterials(string[] materialNames);
        void RemoveMaterials(string[] materialNames);
        //
        void AddSection(Section section);
        void ReplaceSection(string oldSectionName, Section section);
        void DuplicateSections(string[] sectionNames);
        void RemoveSections(string[] sectionNames);
        //
        void AddMaterialOrientation(MaterialOrientation materialOrientation);
        void ReplaceMaterialOrientation(string oldMaterialOrientationName, MaterialOrientation materialOrientation);
        void DuplicateMaterialOrientations(string[] materialOrientationNames);
        void RemoveMaterialOrientations(string[] materialOrientationNames);
        // Constraints
        void AddConstraint(Constraint constraint);
        void ReplaceConstraint(string oldConstraintName, Constraint constraint);
        void DuplicateConstraints(string[] constraintNames);
        void SwapMasterSlaveConstraints(string[] constraintNames);
        void MergeByMasterSlaveConstraints(string[] constraintNames);
        void RemoveConstraints(string[] constraintNames);
        void HideConstraints(string[] constraintNames);
        void ShowConstraints(string[] constraintNames);
        // Surface interactions and contact pairs
        void AddSurfaceInteraction(SurfaceInteraction surfaceInteraction);
        void ReplaceSurfaceInteraction(string oldSurfaceInteractionName, SurfaceInteraction surfaceInteraction);
        void DuplicateSurfaceInteractions(string[] surfaceInteractionNames);
        void RemoveSurfaceInteractions(string[] surfaceInteractionNames);
        //
        void AddContactPair(ContactPair contactPair);
        void ReplaceContactPair(string oldContactPairName, ContactPair contactPair);
        void DuplicateContactPairs(string[] contactPairNames);
        void SwapMasterSlaveContactPairs(string[] contactPairNames);
        void MergeByMasterSlaveContactPairs(string[] contactPairNames);
        void RemoveContactPairs(string[] contactPairNames);
        void HideContactPairs(string[] contactPairNames);
        void ShowContactPairs(string[] contactPairNames);
        // Regions
        void AddNodeSet(FeNodeSet nodeSet);
        void ReplaceNodeSet(string oldNodeSetName, FeNodeSet nodeSet);
        void DuplicateNodeSets(string[] nodeSetNames);
        void RemoveNodeSets(string[] nodeSetNames);
        //
        void AddElementSet(FeElementSet elementSet);
        void ReplaceElementSet(string oldElementSetName, FeElementSet elementSet);
        void DuplicateElementSets(string[] elementSetNames);
        void RemoveElementSets(string[] elementSetNames);
        void ConvertElementSetsToMeshParts(string[] elementSetNames);
        //
        void AddSurface(FeSurface surface);
        void ReplaceSurface(string oldSurfaceName, FeSurface surface);
        void DuplicateSurfaces(string[] surfaceNames);
        void RemoveSurfaces(string[] surfaceNames);
        //
        void AddModelReferencePoint(FeReferencePoint referencePoint);
        void ReplaceModelReferencePoint(string oldReferencePointName, FeReferencePoint referencePoint);
        void DuplicateModelReferencePoints(string[] referencePointNames);
        void RemoveModelReferencePoints(string[] referencePointNames);
        void HideModelReferencePoints(string[] referencePointNames);
        void ShowModelReferencePoints(string[] referencePointNames);
        //
        // Steps
        SolverTypeEnum GetDefaultSolverType();
        string GetCyclicSymmetryModesError(FrequencyStep step);
        void AddStep(Step step, bool copyBoundaryConditionsAndLoads);
        void ReplaceStep(string oldStepName, Step step);
        void DuplicateSteps(string[] stepNames);
        void RemoveSteps(string[] stepNames);
        void MoveSteps(string[] stepNames, int targetIndex);
        void ReplaceStepControls(string stepName, StepControls stepControls);
        // Boundary conditions
        void AddBoundaryCondition(string stepName, BoundaryCondition boundaryCondition);
        void ReplaceBoundaryCondition(string stepName, string oldBoundaryConditionName,
                                      BoundaryCondition boundaryCondition);
        void DuplicateBoundaryConditions(string stepName, string[] boundaryConditionNames);
        void PropagateBoundaryCondition(string stepName, string boundaryConditionName);
        void RemoveBoundaryConditions(string stepName, string[] boundaryConditionNames);
        void HideBoundaryConditions(string stepName, string[] boundaryConditionNames);
        void ShowBoundaryConditions(string stepName, string[] boundaryConditionNames);
        // Loads
        void AddLoad(string stepName, Load load);
        void ReplaceLoad(string stepName, string oldLoadName, Load load);
        void DuplicateLoads(string stepName, string[] loadNames);
        void PropagateLoad(string stepName, string loadName);
        void RemoveLoads(string stepName, string[] loadNames);
        void HideLoads(string stepName, string[] loadNames);
        void ShowLoads(string stepName, string[] loadNames);
        // Field outputs, history outputs and defined fields
        void AddFieldOutput(string stepName, FieldOutput fieldOutput);
        void ReplaceFieldOutput(string stepName, string oldFieldOutputName, FieldOutput fieldOutput);
        void DuplicateFieldOutputs(string stepName, string[] fieldOutputNames);
        void PropagateFieldOutput(string stepName, string fieldOutputName);
        void RemoveFieldOutputs(string stepName, string[] fieldOutputNames);
        //
        void AddHistoryOutput(string stepName, HistoryOutput historyOutput);
        void ReplaceHistoryOutput(string stepName, string oldHistoryOutputName, HistoryOutput historyOutput);
        void DuplicateHistoryOutputs(string stepName, string[] historyOutputNames);
        void PropagateHistoryOutput(string stepName, string historyOutputName);
        void RemoveHistoryOutputs(string stepName, string[] historyOutputNames);
        //
        void AddDefinedField(string stepName, DefinedField definedField);
        void ReplaceDefinedField(string stepName, string oldDefinedFieldName, DefinedField definedField);
        void DuplicateDefinedFields(string stepName, string[] definedFieldNames);
        void PropagateDefinedField(string stepName, string definedFieldName);
        void RemoveDefinedFields(string stepName, string[] definedFieldNames);
        void HideDefinedFields(string stepName, string[] definedFieldNames);
        void ShowDefinedFields(string stepName, string[] definedFieldNames);
        //
        void AddModelCoordinateSystem(CoordinateSystem coordinateSystem);
        void ReplaceModelCoordinateSystem(string oldCoordinateSystemName, CoordinateSystem coordinateSystem);
        void DuplicateModelCoordinateSystems(string[] coordinateSystemNames);
        void RemoveModelCoordinateSystems(string[] coordinateSystemNames);
        void HideModelCoordinateSystems(string[] coordinateSystemNames);
        void ShowModelCoordinateSystems(string[] coordinateSystemNames);
    }
}





