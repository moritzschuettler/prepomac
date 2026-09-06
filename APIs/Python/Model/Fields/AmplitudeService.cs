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
        public void Add(string name, object timeAmplitude, string timeSpan)
        {
            ValidateNewName(name, null);
            Amplitude amplitude = Build(name, timeAmplitude, timeSpan);
            //
            EnsureCommandsAvailable();
            ModelGateway.AddAmplitude(amplitude);
        }
        public void Replace(string oldName, string name, object timeAmplitude, string timeSpan)
        {
            Amplitude existing = ResolveAmplitude(oldName);
            string newName = name == null ? oldName : name;
            ValidateNewName(newName, oldName);
            // A value that is not given is kept, so the time span alone is a valid change
            Amplitude amplitude;
            if (timeAmplitude == null)
            {
                amplitude = existing.DeepClone();
                amplitude.Name = newName;
                if (timeSpan != null)
                    amplitude.TimeSpan = PythonValueConverter.ToEnum<AmplitudeTimeSpanEnum>(timeSpan, "time_span");
            }
            else amplitude = Build(newName, timeAmplitude, timeSpan == null ? existing.TimeSpan.ToString() : timeSpan);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceAmplitude(oldName, amplitude);
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
            double[][] rows = PythonDoubleListConverter.ToDoubleArray2D(timeAmplitude, "time_amplitude");
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
            AmplitudeTabular amplitude = new AmplitudeTabular(name, rows);
            if (timeSpan != null)
                amplitude.TimeSpan = PythonValueConverter.ToEnum<AmplitudeTimeSpanEnum>(timeSpan, "time_span");
            //
            return amplitude;
        }
    }
}
