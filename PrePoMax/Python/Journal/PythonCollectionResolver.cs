// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;
using CaeJob;
using CaeMesh;
using CaeModel;
using CaeResults;

namespace PrePoMax.Python.Journal
{
    // Names the collection a model item belongs to. Activating and deactivating are the only commands
    // that carry the item itself rather than the collection it came from, so the collection has to be
    // recovered from the item's type.
    public static class PythonCollectionResolver
    {
        // Methods                                                                                                                  
        public static string GetCollection(NamedClass item, string stepName)
        {
            if (item == null) throw new NotSupportedException("the command carries no item");
            //
            return GetModelCollection(item.GetType(), stepName);
        }
        public static string GetCollection(Type itemType, string stepName)
        {
            if (itemType == null) throw new NotSupportedException("the command carries no item type");
            //
            return GetModelCollection(itemType, stepName);
        }
        private static string GetModelCollection(Type itemType, string stepName)
        {
            // Only a mesh part can be activated or deactivated, and it belongs to the model parts
            if (Is<MeshPart>(itemType)) return "pmx.model.parts";
            if (Is<FeNodeSet>(itemType)) return "pmx.model.node_sets";
            if (Is<FeElementSet>(itemType)) return "pmx.model.element_sets";
            if (Is<FeSurface>(itemType)) return "pmx.model.surfaces";
            if (Is<FeReferencePoint>(itemType)) return "pmx.model.reference_points";
            if (Is<CoordinateSystem>(itemType)) return "pmx.model.coordinate_systems";
            if (Is<MeshSetupItem>(itemType)) return PythonMeshSetupEmitter.SetupItems;
            if (Is<Material>(itemType)) return "pmx.model.materials";
            if (Is<Section>(itemType)) return "pmx.model.sections";
            if (Is<Constraint>(itemType)) return "pmx.model.constraints";
            if (Is<SurfaceInteraction>(itemType)) return "pmx.model.surface_interactions";
            if (Is<ContactPair>(itemType)) return "pmx.model.contact_pairs";
            if (Is<Amplitude>(itemType)) return "pmx.model.amplitudes";
            if (Is<Distribution>(itemType)) return "pmx.model.distributions";
            if (Is<InitialCondition>(itemType)) return "pmx.model.initial_conditions";
            if (Is<EquationParameter>(itemType)) return "pmx.model.parameters";
            if (Is<Step>(itemType)) return "pmx.model.steps";
            if (Is<AnalysisJob>(itemType)) return "pmx.jobs";
            // A step item needs the step it belongs to, which only the command knows
            if (Is<BoundaryCondition>(itemType)) return StepCollection(stepName, "bcs", itemType);
            if (Is<Load>(itemType)) return StepCollection(stepName, "loads", itemType);
            if (Is<FieldOutput>(itemType)) return StepCollection(stepName, "field_outputs", itemType);
            if (Is<HistoryOutput>(itemType)) return StepCollection(stepName, "history_outputs", itemType);
            if (Is<DefinedField>(itemType)) return StepCollection(stepName, "defined_fields", itemType);
            //
            throw new NotSupportedException("the item type " + itemType.Name +
                                            " has no collection in the API");
        }
        // A rename names the item type rather than carrying the item, and the result side has its own
        // collections for the same types, so the view decides which of the two is meant
        public static string GetCollection(Type itemType, string stepName, ViewGeometryModelResults view)
        {
            if (view == ViewGeometryModelResults.Results)
            {
                if (typeof(FeReferencePoint).IsAssignableFrom(itemType))
                    return PythonResultEmitter.ReferencePoints;
                if (typeof(CoordinateSystem).IsAssignableFrom(itemType))
                    return PythonResultEmitter.CoordinateSystems;
                if (typeof(BasePart).IsAssignableFrom(itemType)) return PythonResultEmitter.Parts;
                if (typeof(ResultFieldOutput).IsAssignableFrom(itemType))
                    return PythonResultOutputEmitter.FieldOutputs;
                if (typeof(ResultHistoryOutput).IsAssignableFrom(itemType))
                    return PythonResultOutputEmitter.HistoryOutputs;
                //
                throw new NotSupportedException("the result item type " + itemType.Name +
                                                " has no collection in the API");
            }
            if (typeof(BasePart).IsAssignableFrom(itemType))
                return view == ViewGeometryModelResults.Geometry ? "pmx.model.geometry.parts"
                                                                 : "pmx.model.parts";
            return GetModelCollection(itemType, stepName);
        }
        private static bool Is<TItem>(Type itemType)
        {
            return typeof(TItem).IsAssignableFrom(itemType);
        }
        private static string StepCollection(string stepName, string collection, Type itemType)
        {
            if (string.IsNullOrEmpty(stepName))
                throw new NotSupportedException("a " + itemType.Name + " names no step");
            //
            return PythonStepItemEmitter.GetCollection(stepName, collection);
        }
    }
}
