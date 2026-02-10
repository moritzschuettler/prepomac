using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace CaeMesh
{
    [Serializable]
    public enum LocalMeshSizeTypeEnum
    {
        [StandardValue("ElementSize", DisplayName = "Element size")]
        ElementSize,
        //
        [StandardValue("NumberOfElements", DisplayName = "Number of elements")]
        NumberOfElements,
    }

    [Serializable]
    public class LocalMeshSize : MeshSetupItem, ISerializable
    {
        // Variables                                                                                                                
        private LocalMeshSizeTypeEnum _type;    //ISerializable
        private double _meshSize;               //ISerializable
        private int _numOfElements;             //ISerializable


        // Properties                                                                                                               
        public LocalMeshSizeTypeEnum Type { get { return _type; } set { _type = value; } }
        public double MeshSize 
        {
            get { return _meshSize; } 
            set
            {
                _meshSize = value;
                if (_meshSize < 1E-10) _meshSize = 1E-10;
            } 
        }
        public int NumOfElements { get { return _numOfElements; } set { _numOfElements = value < 1 ? 1 : value; } }


        // Constructors                                                                                                             
        public LocalMeshSize(string name)
            : base(name)
        {
            Reset();
        }
        public LocalMeshSize(LocalMeshSize localMeshSize)
            : this("tmpName")
        {
            CopyFrom(localMeshSize);
            //
            _type = localMeshSize.Type;
            _meshSize = localMeshSize.MeshSize;
            _numOfElements = localMeshSize.NumOfElements;
        }
        public LocalMeshSize(FeMeshRefinement meshRefinement)
            : this("tmpName")
        {
            CopyFrom(meshRefinement);
            //
            _type = LocalMeshSizeTypeEnum.ElementSize;
            _meshSize = meshRefinement.MeshSize;
        }
        public LocalMeshSize(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Reset();    // Compatibility v2.4.4
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_type":
                        _type = (LocalMeshSizeTypeEnum)entry.Value; break;
                    case "_meshSize":
                        _meshSize = (double)entry.Value; break;
                    case "_numOfElements":
                        _numOfElements = (int)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public override void Reset()
        {
            base.Reset();
            //
            _type = LocalMeshSizeTypeEnum.ElementSize;
            _meshSize = 1;
            _numOfElements = 2;
        }
        public LocalMeshSize DeepCopy()
        {
            return new LocalMeshSize(this);
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            info.AddValue("_type", _type, typeof(LocalMeshSizeTypeEnum));
            info.AddValue("_meshSize", _meshSize, typeof(double));
            info.AddValue("_elementsPerEdge", _numOfElements, typeof(int));
        }
    }
}
