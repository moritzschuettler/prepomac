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
        private GeomFaceType[] _faceTypes;                  //ISerializable


        // Properties
        /// <summary>
        /// The surface each picked face lies on, one for one with the splitter ids.
        /// </summary>
        public GeomFaceType[] FaceTypes { get { return _faceTypes; } set { _faceTypes = value; } }
        public override SplitGeometryToolTypeEnum ToolType { get { return SplitGeometryToolTypeEnum.ExtendFace; } }
        public override string DisplayName { get { return "Extend face"; } }


        // Constructors
        public SplitByExtendedFace()
            : base()
        {
            _faceTypes = null;
        }
        public SplitByExtendedFace(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_faceTypes":
                        _faceTypes = (GeomFaceType[])entry.Value; break;
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
        /// <summary>
        /// Which of the picked faces are extended. A face that cannot be continued past its boundary
        /// is left out rather than refused: it is also left out of the symbols, so what is drawn is
        /// what will cut.
        /// </summary>
        public bool IsExtended(int index)
        {
            return _faceTypes != null && index >= 0 && index < _faceTypes.Length &&
                   CanBeExtended(_faceTypes[index]);
        }
        public int GetNumberOfExtendedFaces()
        {
            if (_faceTypes == null) return 0;
            //
            int count = 0;
            for (int i = 0; i < _faceTypes.Length; i++) { if (CanBeExtended(_faceTypes[i])) count++; }
            return count;
        }
        public override void ClearSplitterSelection()
        {
            base.ClearSplitterSelection();
            //
            _faceTypes = null;
        }
        public override string CheckValidity()
        {
            if (_splitterIds == null || _splitterIds.Length == 0)
                return "Select at least one face to extend into the splitting tool.";
            if (GetNumberOfExtendedFaces() == 0)
                return "None of the selected faces can be extended past its boundary.";
            return null;
        }
        // ISerialization
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            info.AddValue("_faceTypes", _faceTypes, typeof(GeomFaceType[]));
        }
    }
}
