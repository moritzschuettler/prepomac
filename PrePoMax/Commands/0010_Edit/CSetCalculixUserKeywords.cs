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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;
using Calculix = FileInOut.Output.Calculix;
using System.Runtime.Serialization;


namespace PrePoMax.Commands
{
    [Serializable]
    class CSetCalculixUserKeywords : PreprocessCommand, ISerializable
    {
        // Variables                                                                                                                
        private OrderedDictionary<int[], Calculix.CalculixUserKeyword> _userKeywords;   //ISerializable


        // Constructor                                                                                                              
        public CSetCalculixUserKeywords(OrderedDictionary<int[], Calculix.CalculixUserKeyword> userKeywords)
            : base("Set CalculiX user keywords")
        {
            _userKeywords = userKeywords.DeepClone();
        }
        public CSetCalculixUserKeywords(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_userKeywords":
                        if (entry.Value is Dictionary<int[], Calculix.CalculixUserKeyword> cukd)
                        {
                            // Compatibility for version v.0.5.1
                            cukd.OnDeserialization(null);
                            _userKeywords = new OrderedDictionary<int[], Calculix.CalculixUserKeyword>("User CalculiX keywords", cukd);
                        }
                        else if (entry.Value is OrderedDictionary<int[], Calculix.CalculixUserKeyword> cukod)
                            _userKeywords = cukod;
                        else if (entry.Value == null) _userKeywords = null;
                        else throw new NotSupportedException();
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.SetCalculixUserKeywords(_userKeywords.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _userKeywords.Count;
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_userKeywords", _userKeywords, typeof(OrderedDictionary<int[], Calculix.CalculixUserKeyword>));
        }
    }
}
