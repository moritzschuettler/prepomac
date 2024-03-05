using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace CaeGlobals
{
    [Serializable]
    public abstract class NamedClass //: ISerializable - this would mean that all derived classes must be Serializable !!!
    {
        // Variables                                                                                                                
        protected string _name;                         //ISerializable
        protected bool _active;                         //ISerializable
        protected bool _visible;                        //ISerializable
        protected bool _valid;                          //ISerializable
        protected bool _internal;                       //ISerializable
        protected bool _checkName;                      //ISerializable
        protected HashSet<char> _additionalCharacters;  //ISerializable


        // Properties                                                                                                               
        public virtual string Name
        {
            get { return _name; }
            set
            {
                if (_checkName) CheckNameForErrors(ref value, _additionalCharacters);
                _name = value;
            }
        }
        //
        [Browsable(false)]
        public virtual bool Active { get { return _active; } set { _active = value; } }
        //
        [Browsable(false)]
        public virtual bool Visible { get { return _visible; } set { _visible = value; } }
        //
        [Browsable(false)]
        public virtual bool Valid { get { return _valid; } set { _valid = value; } }
        //
        [Browsable(false)]
        public virtual bool Internal { get { return _internal; } set { _internal = value; } }
        

        // Constructors                                                                                                             
        public NamedClass()
            : this("NoName")
        {
        }
        public NamedClass(string name)
            : this(name, null)
        {
        }
        public NamedClass(string name, HashSet<char> additionalCharacters)
        {
            _checkName = true;
            _additionalCharacters = additionalCharacters;
            Name = name;
            //
            _active = true;
            _visible = true;
            _valid = true;
            _internal = false;
        }
        public NamedClass(NamedClass namedClass)
        {
            CopyFrom(namedClass);
        }
        public NamedClass(SerializationInfo info, StreamingContext context)
        {
            int count = 0;
            bool meshRefinement = false;
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_name":
                        _name = (string)entry.Value; count++; break;
                    case "_active":
                        _active = (bool)entry.Value; count++; break;
                    case "_visible":
                        _visible = (bool)entry.Value; count++; break;
                    case "_valid":
                        _valid = (bool)entry.Value; count++; break;
                    case "_internal":
                        _internal = (bool)entry.Value; count++; break;
                    case "_checkName":
                        _checkName = (bool)entry.Value; count++; break;
                    case "_additionalCharacters":
                        _additionalCharacters = (HashSet<char>)entry.Value; count++; break;
                    case "_maxH":
                    case "_minH":
                        meshRefinement = true; break;
                }
            }
            //
            if (count < 6)
            {
                // Compatibility v 1.4.0
                if (meshRefinement && _name == null) _name = "Test";
                //
                else throw new NotSupportedException();
            }
        }


        // Static methods
        public static string GetNameWithoutLastValue(string name, char splitter = '-')
        {
            int tmp;
            string[] parts;
            parts = name.Split(splitter);
            int numOfParts = parts.Length;
            //
            if (int.TryParse(parts.Last(), out tmp)) numOfParts--;
            //
            string newName = "";
            for (int i = 0; i < numOfParts; i++)
            {
                newName += parts[i];
                if (i < numOfParts - 1) newName += splitter;
            }
            return newName;
        }


        // Methods                                                                                                                  
        public void CopyFrom(NamedClass namedClass)
        {
            _name = namedClass._name;
            //
            _active = namedClass._active;
            _visible = namedClass._visible;
            _valid = namedClass._valid;
            _internal = namedClass._internal;
            _checkName = namedClass._checkName;
            //
            if (namedClass._additionalCharacters != null)
                _additionalCharacters = new HashSet<char>(namedClass._additionalCharacters);
            else _additionalCharacters = null;
        }
        public static bool CheckName(string name, HashSet<char> additionalCharacters)
        {
            try
            {
                CheckNameForErrors(ref name, additionalCharacters);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static string CheckNameError(string name, HashSet<char> additionalCharacters)
        {
            try
            {
                CheckNameForErrors(ref name, additionalCharacters);
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public static void CheckNameForErrors(ref string name, HashSet<char> additionalCharacters)
        {
            if (name == null) throw new CaeException("The name cannot be null.");
            if (name == "") throw new CaeException("The name cannot be an empty string.");
            // Trim spaces
            name = name.Trim();
            //
            if (name.Contains(' ')) throw new CaeException("The name cannot contain space characters: '" + name + "'.");
            if (name == "Missing") throw new CaeException("The name 'Missing' is a reserved name.");
            //
            char c;
            int letterCount = 0;
            int digitCount = 0;
            //
            if (additionalCharacters == null) additionalCharacters = new HashSet<char>();
            //
            for (int i = 0; i < name.Length; i++)
            {
                c = name[i];
                if (char.IsLetter(c)) letterCount++;
                else if (char.IsDigit(c)) digitCount++;
                else if (c != '_' && c != '-' && c != '(' && c != ')' && !additionalCharacters.Contains(c))
                {
                    string allowedChars = "minus, underscore, parenthesis";
                    foreach (var character in additionalCharacters) allowedChars += ", " + character;
                    allowedChars += ":";
                    //
                    throw new CaeException("The name can only contain a letter, a digit or characters: " + 
                                           allowedChars + " '" + name + "'.");
                }
            }
            //
            if (letterCount <= 0)
                throw new CaeException("The name must contain at least one letter: '" + name + "'.");
        }
        public static string GetErrorFreeName(string name, string prefix, HashSet<char> additionalCharacters)
        {
            if (name == null || name.Length == 0) name = prefix;
            name = name.Replace(' ', '_');
            if (char.IsDigit(name[0])) name = prefix + "-" + name;
            if (name[0] == '_') name = prefix + name;
            //
            byte[] bytes = Encoding.GetEncoding("ISO-8859-8").GetBytes(name);
            name = Encoding.UTF8.GetString(bytes);
            //
            string newName = "";
            for (int i = 0; i < name.Length; i++)
            {
                if (CheckName(newName + name[i], additionalCharacters)) newName += name[i];
                else newName += "_";
            }
            return newName;
        }
        //
        public override string ToString()
        {
            return _name;
        }

        // ISerialization
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            info.AddValue("_name", _name, typeof(string));
            info.AddValue("_active", _active, typeof(bool));
            info.AddValue("_visible", _visible, typeof(bool));
            info.AddValue("_valid", _valid, typeof(bool));
            info.AddValue("_internal", _internal, typeof(bool));
            info.AddValue("_checkName", _checkName, typeof(bool));
            info.AddValue("_additionalCharacters", _additionalCharacters, typeof(HashSet<char>));
        }
    }
}
