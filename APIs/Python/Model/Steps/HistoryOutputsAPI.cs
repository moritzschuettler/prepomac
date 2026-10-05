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

namespace APIs.Python.Model
{
    // The output variables are given by name, one or a list of them. The region is a selection created
    // by pmx.create.selection_by_ids, or a named region created by pmx.create.region; a contact history
    // output names a contact pair instead.
    public class HistoryOutputsAPI : StepItemsAPI<HistoryOutput, HistoryOutputAPI>
    {
        // Variables
        private readonly HistoryOutputService _service;


        // Constructors
        internal HistoryOutputsAPI(string stepName, HistoryOutputService service)
            : base("history output", stepName)
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds a nodal history output using command history. Possible variables are RF, U, V, NT " +
                   "and RFL. Possible totals_type values are Yes, Only and No. The selection target is nodes.",
                   Arguments = "name: str, region: Selection or Region, variables: list[str] or str, " +
                               "totals_type: str = None, global_output: bool = None",
                   Returns = "HistoryOutput",
                   Example = "step.history_outputs.add_nodal('H-Output-1', selection, ['U', 'RF'])")]
        public HistoryOutputAPI add_nodal(string name, object region, object variables,
                                          object totals_type = null, object global_output = null)
        {
            _service.ValidateName(StepName, name, null);
            NodalHistoryOutput historyOutput =
                new NodalHistoryOutput(name,
                                       PythonValueConverter.ToFlagsEnum<NodalHistoryVariable>(variables,
                                                                                              "variables"),
                                       null, RegionTypeEnum.Selection);
            _service.ApplyRegion(historyOutput, region, "nodes");
            //
            return Add(historyOutput, totals_type, global_output);
        }

        [PythonAPI("Replaces an existing nodal history output using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_nodal with " +
                               "region and variables optional", Returns = "HistoryOutput")]
        public HistoryOutputAPI replace_nodal(string old_name, string name = null, object region = null,
                                              object variables = null, object totals_type = null,
                                              object global_output = null)
        {
            NodalHistoryOutput historyOutput =
                _service.CloneForReplace<NodalHistoryOutput>(StepName, old_name, "NodalHistoryOutput");
            if (region != null) _service.ApplyRegion(historyOutput, region, "nodes");
            if (variables != null)
                historyOutput.Variables =
                    PythonValueConverter.ToFlagsEnum<NodalHistoryVariable>(variables, "variables");
            //
            return Replace(old_name, name, historyOutput, totals_type, global_output);
        }

        [PythonAPI("Adds an element history output using command history. Possible variables are S, E, ME, " +
                   "PEEQ, HFL, ENER, ELSE, ELKE, EVOL, EBHE and SDV. The selection target is elements.",
                   Arguments = "name: str, region: Selection or Region, variables: list[str] or str, and " +
                               "the shared history output arguments", Returns = "HistoryOutput")]
        public HistoryOutputAPI add_element(string name, object region, object variables,
                                            object totals_type = null, object global_output = null)
        {
            _service.ValidateName(StepName, name, null);
            ElementHistoryOutput historyOutput =
                new ElementHistoryOutput(name,
                                         PythonValueConverter.ToFlagsEnum<ElementHistoryVariable>(variables,
                                                                                                  "variables"),
                                         null, RegionTypeEnum.Selection);
            _service.ApplyRegion(historyOutput, region, "elements");
            //
            return Add(historyOutput, totals_type, global_output);
        }

        [PythonAPI("Replaces an existing element history output using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_element with " +
                               "region and variables optional", Returns = "HistoryOutput")]
        public HistoryOutputAPI replace_element(string old_name, string name = null, object region = null,
                                                object variables = null, object totals_type = null,
                                                object global_output = null)
        {
            ElementHistoryOutput historyOutput =
                _service.CloneForReplace<ElementHistoryOutput>(StepName, old_name, "ElementHistoryOutput");
            if (region != null) _service.ApplyRegion(historyOutput, region, "elements");
            if (variables != null)
                historyOutput.Variables =
                    PythonValueConverter.ToFlagsEnum<ElementHistoryVariable>(variables, "variables");
            //
            return Replace(old_name, name, historyOutput, totals_type, global_output);
        }

        [PythonAPI("Adds a section history output using command history. Possible variables are SOF, SOM " +
                   "and SOAREA. The selection target is surfaces.",
                   Arguments = "name: str, region: Selection or Region, variables: list[str] or str, and " +
                               "the shared history output arguments", Returns = "HistoryOutput")]
        public HistoryOutputAPI add_section(string name, object region, object variables,
                                            object totals_type = null, object global_output = null)
        {
            _service.ValidateName(StepName, name, null);
            SectionHistoryOutput historyOutput =
                new SectionHistoryOutput(name,
                                         PythonValueConverter.ToFlagsEnum<SectionHistoryVariableEnum>(
                                             variables, "variables"),
                                         null, RegionTypeEnum.Selection);
            _service.ApplyRegion(historyOutput, region, "surfaces");
            //
            return Add(historyOutput, totals_type, global_output);
        }

        [PythonAPI("Replaces an existing section history output using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_section with " +
                               "region and variables optional", Returns = "HistoryOutput")]
        public HistoryOutputAPI replace_section(string old_name, string name = null, object region = null,
                                                object variables = null, object totals_type = null,
                                                object global_output = null)
        {
            SectionHistoryOutput historyOutput =
                _service.CloneForReplace<SectionHistoryOutput>(StepName, old_name, "SectionHistoryOutput");
            if (region != null) _service.ApplyRegion(historyOutput, region, "surfaces");
            if (variables != null)
                historyOutput.Variables =
                    PythonValueConverter.ToFlagsEnum<SectionHistoryVariableEnum>(variables, "variables");
            //
            return Replace(old_name, name, historyOutput, totals_type, global_output);
        }

        [PythonAPI("Adds a contact history output using command history. It names a contact pair instead " +
                   "of a region. Possible variables are CDIS, CSTR, CELS, CNUM and CF.",
                   Arguments = "name: str, contact_pair_name: str, variables: list[str] or str, and the " +
                               "shared history output arguments", Returns = "HistoryOutput")]
        public HistoryOutputAPI add_contact(string name, object contact_pair_name, object variables,
                                            object totals_type = null, object global_output = null)
        {
            _service.ValidateName(StepName, name, null);
            ContactHistoryOutput historyOutput =
                new ContactHistoryOutput(name,
                                         PythonValueConverter.ToFlagsEnum<ContactHistoryVariable>(variables,
                                                                                                  "variables"),
                                         _service.ResolveContactPairName(contact_pair_name));
            //
            return Add(historyOutput, totals_type, global_output);
        }

        [PythonAPI("Replaces an existing contact history output using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_contact with " +
                               "contact_pair_name and variables optional", Returns = "HistoryOutput")]
        public HistoryOutputAPI replace_contact(string old_name, string name = null,
                                                object contact_pair_name = null, object variables = null,
                                                object totals_type = null, object global_output = null)
        {
            ContactHistoryOutput historyOutput =
                _service.CloneForReplace<ContactHistoryOutput>(StepName, old_name, "ContactHistoryOutput");
            if (contact_pair_name != null)
                historyOutput.RegionName = _service.ResolveContactPairName(contact_pair_name);
            if (variables != null)
                historyOutput.Variables =
                    PythonValueConverter.ToFlagsEnum<ContactHistoryVariable>(variables, "variables");
            //
            return Replace(old_name, name, historyOutput, totals_type, global_output);
        }

        [PythonAPI("Copies the history output to every following step using command history.",
                   Arguments = "name: str", Returns = "None")]
        public void propagate(string name)
        {
            _service.Propagate(StepName, name);
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames(StepName);
        }
        protected override HistoryOutput GetItem(string name)
        {
            return _service.GetItem(StepName, name);
        }
        protected override HistoryOutputAPI CreateItemAPI(string name)
        {
            return new HistoryOutputAPI(StepName, name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(StepName, names);
        }
        protected override void SetItemsActive(HistoryOutput[] items, bool active)
        {
            _service.SetActive(StepName, items, active);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(StepName, names);
        }
        protected override void RenameItem(HistoryOutput item, string newName)
        {
            _service.Rename(StepName, item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "HistoryOutputs(step=" + StringListAPI.ToDisplayString(StepName) + ", count=" +
                   names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        //
        private HistoryOutputAPI Add(HistoryOutput historyOutput, object totalsType, object global)
        {
            _service.ApplyCommonValues(historyOutput, totalsType, global);
            _service.Add(StepName, historyOutput);
            //
            return this[historyOutput.Name];
        }
        private HistoryOutputAPI Replace(string oldName, string name, HistoryOutput historyOutput,
                                         object totalsType, object global)
        {
            _service.ApplyCommonValues(historyOutput, totalsType, global);
            if (name != null)
            {
                _service.ValidateName(StepName, name, oldName);
                historyOutput.Name = name;
            }
            _service.Replace(StepName, oldName, historyOutput);
            //
            return this[historyOutput.Name];
        }
    }
}
