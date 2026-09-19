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

namespace CaeMesh
{
    [Serializable]
    public class SplitByExtendedFace : GeometryFeature
    {
        // Variables
        private GeomFaceType _faceType;                     //ISerializable


        // Properties
        public GeomFaceType FaceType { get { return _faceType; } set { _faceType = value; } }
        public override SplitGeometryToolTypeEnum ToolType { get { return SplitGeometryToolTypeEnum.ExtendFace; } }
        public override string DisplayName { get { return "Extend face"; } }


        // Constructors
        public SplitByExtendedFace()
            : base()
        {
            _faceType = GeomFaceType.Unknown;
        }
        public SplitByExtendedFace(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_faceType":
                        _faceType = (GeomFaceType)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods
        /// <summary>
        /// Only a surface that continues past its own boundary can be extended. A trimmed spline patch
        /// has no continuation, so it is refused when it is picked instead of after the modelling kernel
        /// has returned the geometry unchanged.
        /// </summary>
        public static bool CanBeExtended(GeomFaceType faceType)
        {
            return faceType == GeomFaceType.Plane || faceType == GeomFaceType.Cylinder ||
                   faceType == GeomFaceType.Cone || faceType == GeomFaceType.Sphere ||
                   faceType == GeomFaceType.Torus;
        }
        public override void ClearSplitterSelection()
        {
            base.ClearSplitterSelection();
            //
            _faceType = GeomFaceType.Unknown;
        }
        public override string CheckValidity()
        {
            if (_splitterIds == null || _splitterIds.Length != 1)
                return "Select one face to extend into the splitting tool.";
            if (!CanBeExtended(_faceType))
                return "A " + _faceType.ToString().ToLower() + " face cannot be extended past its boundary.";
            return null;
        }
        // ISerialization
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            info.AddValue("_faceType", _faceType, typeof(GeomFaceType));
        }
    }
}
