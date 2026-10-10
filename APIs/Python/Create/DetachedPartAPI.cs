// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;
using System.Linq;

namespace APIs.Python
{
    // A detached part made by pmx.create or read back from the model, which an add, replace or set member takes as
    // an argument. Its values are reported under the names of the pmx.create arguments, in the form those
    // arguments take them, so that the maker given them back as keyword arguments makes the same part. The part
    // holds a copy, so nothing a script reads from it changes the model.
    public abstract class DetachedPartAPI : BaseAPI
    {
        // Methods
        [PythonAPI("Returns the values of the part under the names of the pmx.create arguments that set them, in " +
                   "the form those arguments take them. A value that is not set is None, which the maker takes as " +
                   "its default. A value entered as an expression is returned as that expression, with its " +
                   "leading equals sign, and a table as a NumericArray, or as a list of rows when it holds an " +
                   "expression. Passing the result back as keyword arguments to the pmx.create method that made " +
                   "the part makes the same part.",
                   Returns = "dict[str, float, int, bool, str, list, NumericArray or None]",
                   Example = "pmx.create.damping(**step.get_value('damping').get_values())")]
        public Dictionary<string, object> get_values()
        {
            return GetValues();
        }

        [PythonAPI("Returns the names get_values reports for this part.", Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            return new StringListAPI(GetValues().Keys);
        }

        [PythonAPI("Returns one value of the part by the name of the pmx.create argument that sets it. Use " +
                   "get_value_names to list them.", Arguments = "name: str",
                   Returns = "float, int, bool, str, list, NumericArray or None")]
        public object get_value(object name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", true);
            if (nameText == null) throw new PythonAPIException("The argument 'name' must not be empty.");
            //
            Dictionary<string, object> values = GetValues();
            object value;
            if (!values.TryGetValue(nameText, out value))
                throw new PythonAPIException("The value '" + nameText + "' is not defined for a " +
                                             GetPartDescription() + ". Possible values are " +
                                             string.Join(", ", values.Keys) + ".");
            //
            return value;
        }
        public override string ToString()
        {
            return GetTypeName() + "(" +
                   string.Join(", ", GetValues().Select(entry => entry.Key + "=" + FormatValue(entry.Value))) + ")";
        }
        //
        protected abstract string GetTypeName();
        protected abstract string GetMakerName();
        protected abstract Dictionary<string, object> GetValues();
        protected virtual string GetPartDescription()
        {
            return GetMakerName() + " part";
        }
        //
        protected static object FromUnset(double value)
        {
            if (double.IsNaN(value) || value == -1) return null;
            //
            return value;
        }
        protected static object FromUnset(int value)
        {
            if (value == -1) return null;
            //
            return value;
        }
        protected static string FormatValue(object value)
        {
            if (value == null) return "None";
            if (value is string) return StringListAPI.ToDisplayString((string)value);
            if (value is double) return PythonValueConverter.ToRoundTripString((double)value);
            if (value is bool) return (bool)value ? "True" : "False";
            //
            return value.ToString();
        }
    }
}
