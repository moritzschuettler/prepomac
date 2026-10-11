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
using System.Reflection;
using CaeGlobals;
using System.Runtime.Serialization;
using PrePoMax.Python.Journal;

namespace PrePoMax.Commands
{
    [Serializable]
    public abstract class Command   //: ISerializable - this would mean that all derived classes must be Serializable !!!
    {
        // Variables                                                                                                                
        protected string _name;
        protected DateTime _dateCreated;
        protected TimeSpan _timeSpan;
        [OptionalField] private bool _periodicFacesSplitOnlyOnRequest;

        // Properties                                                                                                               
        public string Name { get { return _name; } }
        public DateTime DateCreated { get { return _dateCreated; } }
        public TimeSpan TimeSpan { get { return _timeSpan; } set { _timeSpan = value; } }
        /// <summary>
        /// A command made before Split Periodic Faces became a command of its own was executed while CAD import and
        /// every geometry operation split the periodic faces, and it has to be regenerated the same way.
        /// </summary>
        public bool SplitsPeriodicFacesAutomatically { get { return !_periodicFacesSplitOnlyOnRequest; } }
        // A command the history holds although its execution failed, with the changes it made before the error
        public virtual bool IsRecordedAsFailed { get { return false; } }


        // Constructors                                                                                                             
        public Command(string name)
        {
            _name = name;
            _dateCreated = DateTime.Now;
            _periodicFacesSplitOnlyOnRequest = true;
        }
        protected Command(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "Command+_name":                               // Compatibility for version v.2.5.2
                    case "_name":
                        _name = (string)entry.Value; break;
                    case "Command+_dateCreated":                        // Compatibility for version v.2.5.2
                    case "_dateCreated":
                        _dateCreated = (DateTime)entry.Value; break;
                    case "Command+_timeSpan":                           // Compatibility for version v.2.5.2
                    case "_timeSpan":
                        _timeSpan = (TimeSpan)entry.Value; break;
                    case "_periodicFacesSplitOnlyOnRequest":
                        _periodicFacesSplitOnlyOnRequest = (bool)entry.Value; break;
                }
            }
        }


        // Methods                                                                                                                  
        public virtual bool Execute(Controller receiver)
        {
            return true;
        }
        public string GetDateTime()
        {
            return _dateCreated.ToString("MM/dd/yy HH:mm:ss");
        }
        public virtual string GetCommandString()
        {
            return GetBaseCommandString();
        }
        public virtual void ToPython(PythonJournalWriter writer)
        {
            writer.Unsupported(this, "no Python form is implemented for this command");
        }
        public string GetBaseCommandString()
        {
            return _dateCreated.ToString("MM/dd/yy HH:mm:ss") + "   " + _name + ": ";
        }
        protected string GetArrayAsString(string[] array)
        {
            string names = "[";
            int count = 0;
            int maxLen = 120;
            foreach (string name in array)
            {
                names += name;
                if (++count < array.Length) names += ", ";
                if (names.Length > maxLen)
                {
                    names += "...";
                    break;
                }
            }
            names += "]";

            return names;
        }
        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            info.AddValue("_name", _name, typeof(string));
            info.AddValue("_dateCreated", _dateCreated, typeof(DateTime));
            info.AddValue("_timeSpan", _timeSpan, typeof(TimeSpan));
            info.AddValue("_periodicFacesSplitOnlyOnRequest", _periodicFacesSplitOnlyOnRequest, typeof(bool));
        }
    }
}
