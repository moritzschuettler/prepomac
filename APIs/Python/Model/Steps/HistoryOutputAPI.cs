// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;
using System;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class HistoryOutputAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> ItemTypes = new Dictionary<Type, string>
        {
            { typeof(NodalHistoryOutput), "nodal" },
            { typeof(ElementHistoryOutput), "element" },
            { typeof(SectionHistoryOutput), "section" },
            { typeof(ContactHistoryOutput), "contact" }
        };
        private readonly string _stepName;
        private readonly string _historyOutputName;
        private readonly HistoryOutputService _service;


        // Properties
        [PythonAPI("The history output name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The name of the step the history output belongs to.", Returns = "str")]
        public string step_name { get { return _stepName; } }
        [PythonAPI("The history output type, named after the add_* method that makes it without the add_ " +
                   "prefix: nodal, element, section or contact.", Returns = "str")]
        public string item_type { get { return GetItemType(Resolve()); } }
        [PythonAPI("True when the history output is written to the solver input file.", Returns = "bool")]
        public bool active { get { return Resolve().Active; } }
        [PythonAPI("The region type, for example NodeSetName or ContactPair.", Returns = "str")]
        public string region_type { get { return Resolve().RegionType.ToString(); } }
        [PythonAPI("The name of the region the history output is written for.", Returns = "str")]
        public string region_name { get { return Resolve().RegionName; } }
        [PythonAPI("Whether the sum of the external forces is printed: Yes, Only or No.", Returns = "str")]
        public string totals_type { get { return Resolve().TotalsType.ToString(); } }
        [PythonAPI("True when the results are written in the global coordinate system.", Returns = "bool")]
        public bool global_output { get { return Resolve().Global; } }


        // Constructors
        internal HistoryOutputAPI(string stepName, string historyOutputName, HistoryOutputService service)
        {
            _stepName = stepName;
            _historyOutputName = historyOutputName;
            _service = service;
        }


        // Static methods
        internal static string GetItemType(HistoryOutput historyOutput)
        {
            string itemType;
            if (ItemTypes.TryGetValue(historyOutput.GetType(), out itemType)) return itemType;
            //
            throw new PythonAPIException("The history output '" + historyOutput.Name + "' is a " +
                                         historyOutput.GetType().Name + ", which no add_* method of the " +
                                         "Python API makes.");
        }
        private static Dictionary<string, object> GetValues(HistoryOutput historyOutput)
        {
            // The totals type and the global output are shared by every type and are properties of the output
            Dictionary<string, object> values =
                ModelItemValueReader.GetValues(historyOutput, ModelItemValueReader.Arguments("variables"));
            // A contact history output names its contact pair as its region
            if (historyOutput is ContactHistoryOutput) values["contact_pair_name"] = historyOutput.RegionName;
            //
            return values;
        }


        // Methods
        [PythonAPI("Returns the names of the requested output variables.", Returns = "StringList")]
        public StringListAPI get_variables()
        {
            HistoryOutput historyOutput = Resolve();
            NodalHistoryOutput nodal = historyOutput as NodalHistoryOutput;
            if (nodal != null)
                return new StringListAPI(PythonValueConverter.FromFlagsEnum(typeof(NodalHistoryVariable),
                                                                            nodal.Variables));
            ElementHistoryOutput element = historyOutput as ElementHistoryOutput;
            if (element != null)
                return new StringListAPI(PythonValueConverter.FromFlagsEnum(typeof(ElementHistoryVariable),
                                                                             element.Variables));
            ContactHistoryOutput contact = historyOutput as ContactHistoryOutput;
            if (contact != null)
                return new StringListAPI(PythonValueConverter.FromFlagsEnum(typeof(ContactHistoryVariable),
                                                                             contact.Variables));
            SectionHistoryOutput section = historyOutput as SectionHistoryOutput;
            if (section != null)
                return new StringListAPI(PythonValueConverter.FromFlagsEnum(typeof(SectionHistoryVariableEnum),
                                                                             section.Variables));
            //
            throw new PythonAPIException("The history output type " + historyOutput.GetType().Name +
                                         " is not supported.");
        }

        [PythonAPI("Returns the names of the values that are specific to this history output type: the names of " +
                   "the replace_* arguments that set them, and nothing else, so the values go back into the " +
                   "replace_* method of the history output.", Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            return new StringListAPI(GetValues(Resolve()).Keys);
        }

        [PythonAPI("Returns a value that is specific to this history output type by the name of the add_* " +
                   "argument that sets it, for example variables or contact_pair_name. Use get_value_names to " +
                   "list them. The variables are returned as a list of names, the form the variables argument " +
                   "takes.", Arguments = "name: str", Returns = "float, int, bool, str or list[str]")]
        public object get_value(string name)
        {
            HistoryOutput historyOutput = Resolve();
            return ModelItemValueReader.GetValue(GetValues(historyOutput), name,
                                                 GetItemType(historyOutput) + " history output");
        }
        public override string ToString()
        {
            HistoryOutput historyOutput = Resolve();
            return "HistoryOutput(name=" + StringListAPI.ToDisplayString(historyOutput.Name) + ", type=" +
                   GetItemType(historyOutput) + ", step=" + StringListAPI.ToDisplayString(_stepName) + ")";
        }
        //
        private HistoryOutput Resolve()
        {
            return _service.ResolveItem(_stepName, _historyOutputName);
        }
    }
}
