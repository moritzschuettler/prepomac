// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeModel;
using System.Linq;

namespace APIs.Python.Model
{
    internal class AmplitudeService : FieldItemService
    {
        // Properties
        protected override string ItemTypeName { get { return "amplitude"; } }


        // Constructors
        public AmplitudeService(ModelResolver modelResolver, SelectionService selectionService,
                                IPythonModelGateway modelGateway)
            : base(modelResolver, selectionService, modelGateway)
        {
        }


        // Methods
        public string[] GetNames()
        {
            return ModelResolver.GetModel().Amplitudes.Keys.ToArray();
        }
        public Amplitude GetAmplitude(string name)
        {
            if (name == null) return null;
            //
            Amplitude amplitude;
            if (!ModelResolver.GetModel().Amplitudes.TryGetValue(name, out amplitude)) return null;
            //
            return amplitude;
        }
        public Amplitude ResolveAmplitude(string name)
        {
            Amplitude amplitude = GetAmplitude(name);
            if (amplitude == null) throw new PythonAPIException("The amplitude '" + name + "' does not exist.");
            //
            return amplitude;
        }
        public void Add(string name, object timeAmplitude, string timeSpan, object shiftTime, object shiftAmplitude)
        {
            ValidateNewName(name, null);
            Amplitude amplitude = Build(name, timeAmplitude, timeSpan);
            ApplyShifts(amplitude, shiftTime, shiftAmplitude);
            //
            EnsureCommandsAvailable();
            ModelGateway.AddAmplitude(amplitude);
        }
        public void Replace(string oldName, string name, object timeAmplitude, string timeSpan, object shiftTime,
                            object shiftAmplitude)
        {
            Amplitude existing = ResolveAmplitude(oldName);
            string newName = name == null ? existing.Name : name;
            ValidateNewName(newName, existing.Name);
            // A value that is not given is kept
            Amplitude amplitude = existing.DeepClone();
            amplitude.Name = newName;
            if (timeAmplitude != null) amplitude.TimeAmplitude = ToTimeAmplitude(timeAmplitude);
            if (timeSpan != null)
                amplitude.TimeSpan = PythonValueConverter.ToEnum<AmplitudeTimeSpanEnum>(timeSpan, "time_span");
            ApplyShifts(amplitude, shiftTime, shiftAmplitude);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceAmplitude(existing.Name, amplitude);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateAmplitudes(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.RemoveAmplitudes(names);
        }
        public void Rename(NamedClass item, string newName)
        {
            ValidateNewName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            ModelGateway.RenameModelItem(item, newName, null);
        }
        //
        protected override string[] GetItemNames()
        {
            return GetNames();
        }
        private Amplitude Build(string name, object timeAmplitude, string timeSpan)
        {
            AmplitudeTabular amplitude = new AmplitudeTabular(name, ToTimeAmplitude(timeAmplitude));
            if (timeSpan != null)
                amplitude.TimeSpan = PythonValueConverter.ToEnum<AmplitudeTimeSpanEnum>(timeSpan, "time_span");
            //
            return amplitude;
        }
        // The dialog stores the shifts as plain numbers, not as expressions
        private static void ApplyShifts(Amplitude amplitude, object shiftTime, object shiftAmplitude)
        {
            if (shiftTime != null) amplitude.ShiftX = PythonValueConverter.ToFiniteDouble(shiftTime, "shift_time");
            if (shiftAmplitude != null)
                amplitude.ShiftY = PythonValueConverter.ToFiniteDouble(shiftAmplitude, "shift_amplitude");
        }
        private static double[][] ToTimeAmplitude(object timeAmplitude)
        {
            double[][] rows = PythonDoubleListConverter.ToFiniteDoubleArray2D(timeAmplitude, "time_amplitude");
            if (rows == null || rows.Length == 0)
                throw new PythonAPIException("The argument 'time_amplitude' must contain at least one row.");
            //
            foreach (double[] row in rows)
            {
                if (row == null || row.Length != 2)
                    throw new PythonAPIException("Every row of the argument 'time_amplitude' must contain a time " +
                                                 "and an amplitude value.");
            }
            //
            return rows;
        }
    }
}
