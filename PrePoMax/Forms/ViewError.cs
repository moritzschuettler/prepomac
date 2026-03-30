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
using System.ComponentModel;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewError
    {
        // Variables                                                                                                                
        private string _message;


        // Properties                                                                                                               
        [CategoryAttribute("Error")]
        [DisplayName("Message")]
        [DescriptionAttribute("Error message.")]
        public string Message { get { return _message; } }


        // Constructor                                                                                                              
        public ViewError(string errorMessage)
        {
            _message = errorMessage;
        }
    }
}
