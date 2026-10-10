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
    public class AmplitudesAPI : ModelItemsAPI<Amplitude, AmplitudeAPI>
    {
        // Variables
        private readonly AmplitudeService _service;


        // Constructors
        internal AmplitudesAPI(AmplitudeService service)
            : base("amplitude")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds a tabular amplitude using command history. Every row is [time, amplitude]. " +
                   "The time span is StepTime or TotalTime. shift_time and shift_amplitude move every time and " +
                   "every amplitude value by a fixed amount; both default to 0.",
                   Arguments = "name: str, time_amplitude: list[list[float]], time_span: str = None, " +
                               "shift_time: float = None, shift_amplitude: float = None",
                   Returns = "Amplitude",
                   Example = "pmx.model.amplitudes.add('Amp-1', [[0, 0], [1, 1]])")]
        public AmplitudeAPI add(object name, object time_amplitude, object time_span = null, object shift_time = null,
                                object shift_amplitude = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string timeSpan = PythonValueConverter.ToText(time_span, "time_span", true);
            _service.Add(nameText, time_amplitude, timeSpan, shift_time, shift_amplitude);
            return this[nameText];
        }

        [PythonAPI("Replaces an existing amplitude using command history. A value that is not given is kept.",
                   Arguments = "old_name: str, name: str = None, time_amplitude: list[list[float]] = None, " +
                               "time_span: str = None, shift_time: float = None, shift_amplitude: float = None",
                   Returns = "Amplitude")]
        public AmplitudeAPI replace(object old_name, object name = null, object time_amplitude = null,
                                    object time_span = null, object shift_time = null, object shift_amplitude = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            string timeSpan = PythonValueConverter.ToText(time_span, "time_span", true);
            _service.Replace(oldName, nameText, time_amplitude, timeSpan, shift_time, shift_amplitude);
            return this[nameText == null ? oldName : nameText];
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override Amplitude GetItem(string name)
        {
            return _service.GetAmplitude(name);
        }
        protected override AmplitudeAPI CreateItemAPI(string name)
        {
            return new AmplitudeAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(Amplitude item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "Amplitudes(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
