using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Globalization;
using UnitsNet.Units;
using UnitsNet;

namespace CaeGlobals
{
    public class StringLengthUnknownConverter : StringLengthDefaultConverter
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public StringLengthUnknownConverter()
        {
            _default = "Unknown";
        }


        // Methods                                                                                                                  
    }
    

}