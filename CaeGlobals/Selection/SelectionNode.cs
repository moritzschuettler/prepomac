using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace CaeGlobals
{
    [Serializable]
    public abstract class SelectionNode : ISerializable
    {
        // Variables                                                                                                                
        protected vtkSelectOperation _selectOperation;      //ISerializable
        protected double _hash;


        // Properties                                                                                                               
        public vtkSelectOperation SelectOperation { get { return _selectOperation; } }
        public double Hash { get { return _hash; } set { _hash = value; } }


        // Constructors                                                                                                             
        public SelectionNode(vtkSelectOperation selectOperation)
        {
            _selectOperation = selectOperation;
            _hash = -1;
        }
        public SelectionNode(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_selectOpreation":    // compatibility version 2.2.2
                    case "_selectOperation":
                        _selectOperation = (vtkSelectOperation)entry.Value;
                        break;
                    case "_hash":
                        _hash = (double)entry.Value;
                        break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public void SetSelectOperation(vtkSelectOperation selectOperation)
        {
            _selectOperation = selectOperation;
        }
        // ISerialization
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            info.AddValue("_selectOperation", _selectOperation, typeof(vtkSelectOperation));
            info.AddValue("_hash", _hash, typeof(double));
        }
    }
}
