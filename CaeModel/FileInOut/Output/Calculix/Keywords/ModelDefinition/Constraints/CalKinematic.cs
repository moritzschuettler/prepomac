// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Text;
using CaeModel;
using CaeGlobals;


namespace FileInOut.Output.Calculix
{
    [Serializable]
    [PmxType(3082)]
    internal class CalKinematic : CalculixKeyword
    {
        // Variables                                                                                                                
        [PmxKey(0)] private KinematicCoupling _kinematicCoupling;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalKinematic(KinematicCoupling kinematicCoupling)
        {
            _kinematicCoupling = kinematicCoupling;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return "*Kinematic" + Environment.NewLine;
        }
        public override string GetDataString()
        {
            int[] directions = _kinematicCoupling.GetConstrainedDirections();
            if (directions.Length == 0) throw new NotSupportedException();
            //
            StringBuilder sb = new StringBuilder();
            int first = directions[0];
            int last = first;
            for (int i = 1; i <= directions.Length; i++)
            {
                if (i < directions.Length && directions[i] == last + 1)
                {
                    last = directions[i];
                }
                else
                {
                    if (first == last) sb.AppendLine(first.ToString());
                    else sb.AppendFormat("{0}, {1}", first, last).AppendLine();
                    //
                    if (i < directions.Length)
                    {
                        first = directions[i];
                        last = first;
                    }
                }
            }
            return sb.ToString();
        }
    }
}
