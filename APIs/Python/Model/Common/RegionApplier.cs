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
using CaeModel;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    // Points a model item at its region. A selection is stored as a selection based region, which the
    // controller converts into an internal node set or surface when the item is added; a named region
    // points at an existing entity of the model.
    internal static class RegionApplier
    {
        // Methods
        public static void Apply(IMultiRegion item, object region, string argumentName, string target,
                                 SelectionService selectionService, ModelResolver modelResolver)
        {
            Apply(item, region, argumentName, new string[] { target }, selectionService, modelResolver);
        }
        public static void Apply(IMultiRegion item, object region, string argumentName, string[] targets,
                                 SelectionService selectionService, ModelResolver modelResolver)
        {
            if (region == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            //
            SelectionAPI selection = region as SelectionAPI;
            if (selection != null)
            {
                SelectionCompilation compilation = selectionService.CompileForView(selection, "femodel", targets);
                item.RegionType = RegionTypeEnum.Selection;
                item.RegionName = null;
                item.CreationData = compilation.Selection;
                item.CreationIds = compilation.Ids;
                return;
            }
            //
            RegionAPI named = region as RegionAPI;
            if (named == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must be a selection created " +
                                             "by pmx.create.selection_by_ids, or a named region created by " +
                                             "pmx.create.region.");
            //
            ValidateRegionName(named, argumentName, targets, modelResolver, item);
            item.RegionType = named.RegionType;
            item.RegionName = named.name;
            item.CreationData = null;
            item.CreationIds = null;
        }
        //
        private static void ValidateRegionName(RegionAPI region, string argumentName, string[] targets,
                                               ModelResolver modelResolver, IMultiRegion item)
        {
            FeModel model = modelResolver.GetModel();
            // A mass section names a section of the model rather than an entity of the mesh, and only
            // the two loads that act on a mass accept it
            if (region.RegionType == RegionTypeEnum.MassSection)
            {
                if (!(item is GravityLoad) && !(item is CentrifLoad))
                    throw new PythonAPIException("A MassSection region can only be used by a gravity load " +
                                                 "or by a centrifugal load.");
                //
                ValidateMassSectionName(region, model);
                return;
            }
            //
            ValidateRegionType(region.RegionType, argumentName, targets, item);
            //
            FeMesh mesh = model.Mesh;
            if (mesh == null)
                throw new PythonAPIException("A named region needs a mesh, and the model has none.");
            //
            bool exists;
            switch (region.RegionType)
            {
                case RegionTypeEnum.NodeSetName:
                    exists = mesh.NodeSets.ContainsKey(region.name); break;
                case RegionTypeEnum.ElementSetName:
                    exists = mesh.ElementSets.ContainsKey(region.name); break;
                case RegionTypeEnum.SurfaceName:
                    exists = mesh.Surfaces.ContainsKey(region.name); break;
                case RegionTypeEnum.ReferencePointName:
                    exists = mesh.ReferencePoints.ContainsKey(region.name); break;
                case RegionTypeEnum.PartName:
                    exists = mesh.Parts.ContainsKey(region.name); break;
                default:
                    throw new PythonAPIException("The region type " + region.RegionType + " cannot name a " +
                                                 "region. Possible values are NodeSetName, ElementSetName, " +
                                                 "SurfaceName, ReferencePointName, PartName and MassSection.");
            }
            //
            if (!exists)
                throw new PythonAPIException("The " + region.RegionType + " region '" + region.name +
                                             "' does not exist.");
        }
        // The model accepts any named region on any item and only flags the item invalid later, so the region types
        // the dialog of the item offers are the ones a script may name
        private static void ValidateRegionType(RegionTypeEnum regionType, string argumentName, string[] targets,
                                               IMultiRegion item)
        {
            List<RegionTypeEnum> accepted = GetAcceptedRegionTypes(targets, item);
            if (accepted == null || accepted.Contains(regionType)) return;
            //
            string[] names = new string[accepted.Count];
            for (int i = 0; i < names.Length; i++) names[i] = accepted[i].ToString();
            throw new PythonAPIException("The argument '" + argumentName + "' accepts a selection or a " +
                                         string.Join(", ", names) + " region, but a " + regionType +
                                         " region was given.");
        }
        // A target the item is not defined on through a named region, such as geometry, is checked by its caller
        private static List<RegionTypeEnum> GetAcceptedRegionTypes(string[] targets, IMultiRegion item)
        {
            List<RegionTypeEnum> accepted = new List<RegionTypeEnum>();
            foreach (string target in targets)
            {
                if (target == "nodes")
                {
                    AddOnce(accepted, RegionTypeEnum.NodeSetName);
                    if (AcceptsSurfaceForNodes(item)) AddOnce(accepted, RegionTypeEnum.SurfaceName);
                    if (AcceptsReferencePoint(item)) AddOnce(accepted, RegionTypeEnum.ReferencePointName);
                }
                else if (target == "elements")
                {
                    AddOnce(accepted, RegionTypeEnum.ElementSetName);
                    if (!(item is ElementHistoryOutput)) AddOnce(accepted, RegionTypeEnum.PartName);
                }
                else if (target == "parts") AddOnce(accepted, RegionTypeEnum.PartName);
                else if (target == "surfaces") AddOnce(accepted, RegionTypeEnum.SurfaceName);
                else return null;
            }
            //
            return accepted;
        }
        private static void AddOnce(List<RegionTypeEnum> regionTypes, RegionTypeEnum regionType)
        {
            if (!regionTypes.Contains(regionType)) regionTypes.Add(regionType);
        }
        // A concentrated load, a point spring and a point mass act on nodes or on a reference point only
        private static bool AcceptsSurfaceForNodes(IMultiRegion item)
        {
            return !(item is CLoad || item is MomentLoad || item is CFlux || item is PointSpring ||
                     item is PointMassSection);
        }
        // BoundaryConditionService narrows the boundary conditions down to the types that support it
        private static bool AcceptsReferencePoint(IMultiRegion item)
        {
            return item is BoundaryCondition || item is CLoad || item is MomentLoad || item is PointSpring ||
                   item is PointMassSection || item is NodalHistoryOutput ||
                   item is InitialTranslationalVelocity || item is InitialAngularVelocity;
        }
        //
        private static void ValidateMassSectionName(RegionAPI region, FeModel model)
        {
            Section section;
            if (!model.Sections.TryGetValue(region.name, out section))
                throw new PythonAPIException("The MassSection region '" + region.name + "' does not exist.");
            //
            if (!(section is MassSection))
                throw new PythonAPIException("The section '" + region.name + "' is a " +
                                             SectionAPI.GetItemType(section) + " section and cannot be used as a " +
                                             "MassSection region. Use a point mass or a distributed mass section.");
        }
    }
}
