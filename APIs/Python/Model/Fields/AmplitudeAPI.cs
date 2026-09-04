// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;

namespace APIs.Python.Model
{
    public class AmplitudeAPI : BaseAPI
    {
        // Variables
        private readonly string _amplitudeName;
        private readonly AmplitudeService _service;


        // Properties
        [PythonAPI("The amplitude name.", Returns = "str")]
        public string name { get { return _service.ResolveAmplitude(_amplitudeName).Name; } }
        [PythonAPI("The amplitude time span: StepTime or TotalTime.", Returns = "str")]
        public string time_span { get { return _service.ResolveAmplitude(_amplitudeName).TimeSpan.ToString(); } }
        [PythonAPI("The number of time and amplitude rows.", Returns = "int")]
        public int row_count
        {
            get
            {
                double[][] rows = _service.ResolveAmplitude(_amplitudeName).TimeAmplitude;
                return rows == null ? 0 : rows.Length;
            }
        }


        // Constructors
        internal AmplitudeAPI(string amplitudeName, AmplitudeService service)
        {
            _amplitudeName = amplitudeName;
            _service = service;
        }


        // Methods
        [PythonAPI("Returns the time and amplitude rows.", Returns = "NumericArray")]
        public NumericArrayAPI get_time_amplitude()
        {
            double[][] rows = _service.ResolveAmplitude(_amplitudeName).TimeAmplitude;
            return new NumericArrayAPI(rows == null ? new double[0][] : rows);
        }
        public override string ToString()
        {
            return "Amplitude(name=" + StringListAPI.ToDisplayString(_amplitudeName) + ", rows=" + row_count + ")";
        }
    }
}
