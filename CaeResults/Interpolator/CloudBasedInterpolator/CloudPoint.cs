using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using CaeMesh;

namespace CaeResults
{
    public class CloudPoint
    {
        // Properties                                                                                                               
        public double[] Coor;
        public double[] Values;


        // Constructor                                                                                                              
        public CloudPoint()
        {
            Coor = null;
            Values = null;
        }
    }
}
