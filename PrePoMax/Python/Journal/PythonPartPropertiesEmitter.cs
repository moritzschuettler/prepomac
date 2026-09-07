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
using System.Reflection;
using System.Text;
using CaeMesh;

namespace PrePoMax.Python.Journal
{
    // The part property dialog changes the name, the colour and the element types together, and the
    // history records that as one command, so one call carries all of them. The element types are
    // named the way get_element_types names them, and the type of the part itself is not among them
    // because it describes the part rather than choosing anything.
    public static class PythonPartPropertiesEmitter
    {
        // Methods                                                                                                                  
        public static void Emit(PythonJournalWriter writer, string collection, string oldPartName,
                                PartProperties properties, bool withElementTypes)
        {
            List<string> arguments = new List<string> { PythonValueFormatter.Text(oldPartName),
                                                        "name=" + PythonValueFormatter.Text(properties.Name),
                                                        "color=" +
                                                        PythonValueFormatter.Channels(properties.Color) };
            if (withElementTypes) arguments.Add("element_types=" + GetElementTypes(properties));
            //
            writer.Statement(writer.Call(collection, "replace_properties", arguments.ToArray()));
            writer.CountEmittedCommand();
        }
        private static string GetElementTypes(PartProperties properties)
        {
            List<string> entries = new List<string>();
            foreach (FieldInfo field in typeof(PartProperties).GetFields())
            {
                if (!field.FieldType.IsEnum || !field.Name.EndsWith("Type")) continue;
                if (field.Name == "PartType") continue;
                //
                object value = field.GetValue(properties);
                entries.Add(PythonValueFormatter.Text(ToPythonName(field.Name)) + ": " +
                            PythonValueFormatter.Text(value.ToString()));
            }
            //
            return "{" + string.Join(", ", entries) + "}";
        }
        // LinearTetraType becomes linear_tetra, matching what the API resolves
        private static string ToPythonName(string fieldName)
        {
            string name = fieldName.EndsWith("Type") ? fieldName.Substring(0, fieldName.Length - 4) : fieldName;
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i])) builder.Append('_');
                builder.Append(char.ToLowerInvariant(name[i]));
            }
            //
            return builder.ToString();
        }
    }
}
