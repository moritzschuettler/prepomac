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

namespace PrePoMax.Python.Journal
{
    // Produces the region argument a model item takes. The rule this exists for: an item added from a
    // selection does not read back as a selection. The controller converts it into an internal set at
    // add time, so the item names something like Internal_Selection-1_Load-1 with the region type of
    // that set. Emitting that name would write a script depending on a set the script never creates, so
    // the selection the item was created with is emitted instead.
    public static class PythonRegionEmitter
    {
        // Methods                                                                                                                  
        public static string Emit(PythonJournalWriter writer, string regionName, RegionTypeEnum regionType,
                                  Selection creationData)
        {
            if (creationData != null && creationData.Nodes != null && creationData.Nodes.Count > 0)
                return PythonSelectionEmitter.Emit(writer, creationData);
            //
            if (IsInternal(regionName))
                throw new NotSupportedException("the item points at the internal region '" + regionName +
                                                "' but carries no selection to rebuild it from");
            //
            if (regionType == RegionTypeEnum.None || regionType == RegionTypeEnum.Selection ||
                string.IsNullOrEmpty(regionName))
                throw new NotSupportedException("the item carries neither a named region nor a selection");
            //
            return PythonJournalWriter.Join("pmx.create.region", PythonValueFormatter.Text(regionType.ToString()),
                                            PythonValueFormatter.Text(regionName));
        }
        // A gravity, a centrifugal and a body flux load and the expansion region of a cyclic symmetry are picked as
        // parts, which is all the API takes for them, so a selection of another target an older API recorded would
        // only be refused when the journal replays
        public static void RefuseSelectionOtherThanParts(Selection creationData, string regionDescription)
        {
            if (creationData == null || creationData.Nodes == null || creationData.Nodes.Count == 0) return;
            if (creationData.SelectItem == vtkSelectItem.Part) return;
            //
            throw new NotSupportedException(regionDescription + " was recorded as a selection of " +
                                            creationData.SelectItem + " items, which only an older API took; the " +
                                            "API takes the parts the dialog picks, so the selection has no API form");
        }
        public static bool IsInternal(string regionName)
        {
            return regionName != null && regionName.StartsWith(CaeMesh.Globals.InternalSelectionName);
        }
    }
}
