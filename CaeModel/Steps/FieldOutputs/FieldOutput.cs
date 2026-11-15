// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Data;

namespace CaeModel
{
    [PmxType(3238)]
    public abstract class FieldOutput : NamedClass
    {
        // Variables                                                                                                                
        [PmxKey(0)] private bool _lastIterations;       //ISerializable
        [PmxKey(1)] private bool _contactElements;      //ISerializable
        [PmxKey(2)] private bool _global;               //ISerializable


        // Properties                                                                                                               
        public bool LastIterations { get { return _lastIterations; } set { _lastIterations = value; } }
        public bool ContactElements { get { return _contactElements; } set { _contactElements = value; } }
        public bool Global { get { return _global; } set { _global = value; } }


        // Constructors                                                                                                             
        public FieldOutput(string name)
            : base(name)
        {
            _global = true;
            _lastIterations = false;
            _contactElements = false;
        }
        public FieldOutput(SerializationInfo info, StreamingContext context)
           : base(info, context)
        {
            // Compatibility v2.1.0
            _global = true;
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_lastIterations":
                    case "FieldOutput+_lastIterations":     // Compatibility v2.1.0
                        _lastIterations = (bool)entry.Value; break;
                    case "_contactElements":
                    case "FieldOutput+_contactElements":    // Compatibility v2.1.0
                        _contactElements = (bool)entry.Value; break;
                    case "_global":
                        _global = (bool)entry.Value; break;
                }
            }
        }


    }
}
