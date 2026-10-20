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
    internal class HistoryOutputService : StepItemService<HistoryOutput>
    {
        // Constructors
        public HistoryOutputService(ModelResolver modelResolver, SelectionService selectionService,
                                    StepService stepService, IPythonModelGateway modelGateway)
            : base(modelResolver, selectionService, stepService, modelGateway, "history output")
        {
        }


        // Methods
        public override string GetItemType(HistoryOutput historyOutput)
        {
            return HistoryOutputAPI.GetItemType(historyOutput);
        }
        public void ApplyCommonValues(HistoryOutput historyOutput, object totalsType, object global)
        {
            if (totalsType != null)
                historyOutput.TotalsType =
                    PythonValueConverter.ToEnum<TotalsTypeEnum>(AsString(totalsType, "totals_type"),
                                                                 "totals_type");
            if (global != null) historyOutput.Global = PythonValueConverter.ToBool(global, "global_output");
        }
        // The controller follows a renamed contact pair by comparing names exactly, so the name is stored in the
        // model's own spelling rather than the caller's
        public string ResolveContactPairName(object contactPairName)
        {
            string name = AsString(contactPairName, "contact_pair_name");
            ContactPair contactPair;
            if (!ModelResolver.GetModel().ContactPairs.TryGetValue(name, out contactPair))
                throw new PythonAPIException("The contact pair '" + name + "' does not exist.");
            //
            return contactPair.Name;
        }
        public void Add(string stepName, HistoryOutput historyOutput)
        {
            EnsureCommandsAvailable();
            ModelGateway.AddHistoryOutput(stepName, historyOutput);
        }
        public void Replace(string stepName, string oldName, HistoryOutput historyOutput)
        {
            string storedName = ResolveItem(stepName, oldName).Name;
            ValidateName(stepName, historyOutput.Name, storedName);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceHistoryOutput(stepName, storedName, historyOutput);
        }
        public void Duplicate(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateHistoryOutputs(stepName, names);
        }
        public void Propagate(string stepName, string name)
        {
            string storedName = ResolveItem(stepName, name).Name;
            //
            EnsureCommandsAvailable();
            ModelGateway.PropagateHistoryOutput(stepName, storedName);
        }
        public void Remove(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.RemoveHistoryOutputs(stepName, names);
        }
        //
        protected override CaeGlobals.OrderedDictionary<string, HistoryOutput> GetItems(Step step)
        {
            return step.HistoryOutputs;
        }
    }
}
