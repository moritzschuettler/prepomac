using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace CaeResults
{
    [Serializable]
    public class ResultFieldOutputCoordinateSystemTransform : ResultFieldOutput
    {
        // Variables                                                                                                                
        private string _fieldName;
        private string[] _componentNames;
        private string _coordinateSystemName;


        // Properties                                                                                                               
        public string FieldName { get { return _fieldName; } set { _fieldName = value; } }
        public string CoordinateSystemName { get { return _coordinateSystemName; } set { _coordinateSystemName = value; } }


        // Constructors                                                                                                             
        public ResultFieldOutputCoordinateSystemTransform(string name, string filedName, string coordinateSystemName)
            : base(name)
        {
            _fieldName = filedName;
            _componentNames = null;
            _coordinateSystemName = coordinateSystemName;
        }


        // Methods                                                                                                                  
        public override string[] GetParentNames()
        {
            return new string[] { _fieldName };
        }
        public void SetComponentNames(string[] componentNames)
        {
            _componentNames = componentNames;
        }
        public override string[] GetComponentNames()
        {
            return _componentNames;
        }
    }
}
