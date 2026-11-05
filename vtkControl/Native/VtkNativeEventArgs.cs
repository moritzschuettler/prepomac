// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace vtkControl.Native
{
    public class VtkNativeEventArgs : EventArgs
    {
        // Variables
        private readonly VtkNativeEvent _event;
        private readonly double[] _values;


        // Properties
        public VtkNativeEvent Event { get { return _event; } }
        public double[] Values { get { return _values; } }


        // Constructors
        public VtkNativeEventArgs(VtkNativeEvent nativeEvent, double[] values)
        {
            _event = nativeEvent;
            _values = values;
        }
    }
}
