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
using System.Runtime.Serialization;
using CaeGlobals;

namespace CaeMesh
{
    [Serializable]
    public class CompoundGeometryPart : GeometryPart
    {
        // Variables                                                                                                                
        private string[] _createdFromPartNames;
        private string[] _subPartNames;
        private Guid _linkedGroupId;
        [OptionalField]
        private string[] _cellSubPartNames;
        [NonSerialized] private CompoundSharedEdges _sharedEdges;


        // Properties                                                                                                               
        public string[] CreatedFromPartNames { get { return _createdFromPartNames; } set { _createdFromPartNames = value; } }
        public string[] SubPartNames
        {
            get { return _subPartNames; }
            set
            {
                if (_cellSubPartNames != null) _cellSubPartNames = GetCellSubPartNames(value);
                _subPartNames = value;
            }
        }
        public Guid LinkedGroupId { get { return _linkedGroupId; } }
        /// <summary>
        /// The sub part each cell of a split compound belongs to, one for every name in SubPartNames.
        /// A split cuts a part into cells, and the part it cut is what the user still sees and meshes,
        /// so the cells are gathered under it. A compound that was not made by splitting has none.
        /// </summary>
        public string[] CellSubPartNames { get { return _cellSubPartNames; } set { _cellSubPartNames = value; } }
        public bool CreatedBySplit { get { return _cellSubPartNames != null; } }


        // Constructors                                                                                                             
        public CompoundGeometryPart(string name, string[] createdFromPartNames, string[] subPartNames)
            : base(name, -111, new int[0], new int[0], new Type[0])
        {
            _createdFromPartNames = createdFromPartNames;
            _subPartNames = subPartNames;
            _linkedGroupId = Guid.NewGuid();
            _partType = PartType.Compound;
        }

        public CompoundGeometryPart(CompoundGeometryPart part)
            : this(part.Name, part.CreatedFromPartNames, part.SubPartNames)
        {
            _linkedGroupId = part._linkedGroupId;
            _cellSubPartNames = part._cellSubPartNames?.ToArray();
        }


        // Methods                                                                                                                  
        public override BasePart DeepCopy()
        {
            return new GeometryPart(this);
        }
        public override PartProperties GetProperties()
        {
            PartProperties properties = base.GetProperties();
            return properties;
        }
        public override void SetProperties(PartProperties properties)
        {
            base.SetProperties(properties);
        }
        public string[] GetSplitSubPartNames()
        {
            if (_cellSubPartNames == null) return new string[0];
            return _cellSubPartNames.Where(name => name != null).Distinct().ToArray();
        }
        public string[] GetCellNames(string subPartName)
        {
            List<string> cellNames = new List<string>();
            if (_cellSubPartNames == null) return cellNames.ToArray();
            //
            for (int i = 0; i < _subPartNames.Length; i++)
            {
                if (_cellSubPartNames[i] == subPartName) cellNames.Add(_subPartNames[i]);
            }
            return cellNames.ToArray();
        }
        private string[] GetCellSubPartNames(string[] subPartNames)
        {
            Dictionary<string, string> cellSubPartNames = new Dictionary<string, string>();
            for (int i = 0; i < _subPartNames.Length && i < _cellSubPartNames.Length; i++)
                cellSubPartNames[_subPartNames[i]] = _cellSubPartNames[i];
            //
            string[] names = new string[subPartNames.Length];
            for (int i = 0; i < subPartNames.Length; i++)
                cellSubPartNames.TryGetValue(subPartNames[i], out names[i]);
            return names;
        }
        public CompoundSharedEdges GetSharedEdges(FeMesh mesh)
        {
            string signature = CompoundSharedEdges.GetSignature(this, mesh);
            if (_sharedEdges == null || _sharedEdges.Signature != signature)
                _sharedEdges = new CompoundSharedEdges(this, mesh, signature);
            //
            return _sharedEdges;
        }
        
        
    }
}
