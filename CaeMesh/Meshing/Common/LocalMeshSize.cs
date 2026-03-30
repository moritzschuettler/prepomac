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
            _meshSize = localMeshSize._meshSize;
            _numOfElements = localMeshSize._numOfElements;
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
        public int GetNumberOfElements(double edgeLength, double minH, double maxH)
        {
            if (_type == LocalMeshSizeTypeEnum.ElementSize)
            {
                double size = _meshSize;
                if (size < minH) size = minH;
                else if (size > maxH) size = maxH;
                //
                int numElements = (int)Math.Round(edgeLength / size, 0, MidpointRounding.AwayFromZero);
                if (numElements < 1) numElements = 1;
                return numElements;
            }
            else if (_type == LocalMeshSizeTypeEnum.NumberOfElements)
            {
                int minNumElements = (int)Math.Round(edgeLength / maxH, 0, MidpointRounding.AwayFromZero);
                int maxNumElements = (int)Math.Round(edgeLength / minH, 0, MidpointRounding.AwayFromZero);
                if (minNumElements < 1) minNumElements = 1;
                //
                if (_numOfElements < minNumElements) return minNumElements;
                else if (_numOfElements > maxNumElements) return maxNumElements;
                else return _numOfElements;
            }
            else throw new NotSupportedException();
        }
        public double GetMeshSize(double edgeLength, double minH, double maxH)
        {
            if (_type == LocalMeshSizeTypeEnum.ElementSize)
            {
                if (_meshSize < minH) return minH;
                else if (_meshSize > maxH) return maxH;
                else return _meshSize;
            }
            else if (_type == LocalMeshSizeTypeEnum.NumberOfElements)
            {
                double size = edgeLength / _numOfElements;
                if (size < minH) return minH;
                else if (size > maxH) return maxH;
                else return size;
            }
            else throw new NotSupportedException();
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
            info.AddValue("_numOfElements", _numOfElements, typeof(int));
        }
    }
}
