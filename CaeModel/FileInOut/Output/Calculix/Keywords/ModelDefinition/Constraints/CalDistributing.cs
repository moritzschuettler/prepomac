// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Text;
using CaeModel;


namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalDistributing : CalculixKeyword
    {
        // Variables                                                                                                                
        private DistributingCoupling _distributingCoupling;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalDistributing(DistributingCoupling distributingCoupling)
        {
            _distributingCoupling = distributingCoupling;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return "*Distributing" + Environment.NewLine;
        }
        public override string GetDataString()
        {
            int[] directions = _distributingCoupling.GetDistributedDirections();
            StringBuilder sb = new StringBuilder();
            if (directions.Length == 0) return sb.ToString();
            //
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
