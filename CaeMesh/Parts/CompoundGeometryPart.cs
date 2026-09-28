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
        [OptionalField]
        private int[] _cellSubPartIds;
        [OptionalField]
        private string[] _splitSubPartOrder;
        [NonSerialized] private CompoundSharedEdges _sharedEdges;


        // Properties                                                                                                               
        public string[] CreatedFromPartNames { get { return _createdFromPartNames; } set { _createdFromPartNames = value; } }
        public string[] SubPartNames
        {
            get { return _subPartNames; }
            set
            {
                if (_cellSubPartNames != null) _cellSubPartNames = GetCellValues(_cellSubPartNames, value);
                if (_cellSubPartIds != null) _cellSubPartIds = GetCellValues(_cellSubPartIds, value);
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
        /// <summary>
        /// The part id of the sub part each cell of a split compound belongs to, parallel to CellSubPartNames.
        /// A sub part is not a geometry part, but it is what is meshed into one mesh part, so it holds a part
        /// id of its own that no other part may take. The cells are replaced whenever the compound is split
        /// again, and the id of the sub part is what stays. A compound saved before the ids existed has none.
        /// </summary>
        public int[] CellSubPartIds { get { return _cellSubPartIds; } set { _cellSubPartIds = value; } }
        /// <summary>
        /// The order the sub parts of a split compound are listed in. The cells keep the order of the solids they
        /// are written out as, so a sub part made later by grouping cells would otherwise be listed wherever its
        /// first cell is. A sub part it does not name is listed after the ones it does, in the order of its cells.
        /// </summary>
        public string[] SplitSubPartOrder { get { return _splitSubPartOrder; } set { _splitSubPartOrder = value; } }
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
            _cellSubPartIds = part._cellSubPartIds?.ToArray();
            _splitSubPartOrder = part._splitSubPartOrder?.ToArray();
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
            string[] subPartNames = _cellSubPartNames.Where(name => name != null).Distinct().ToArray();
            if (_splitSubPartOrder == null) return subPartNames;
            //
            return _splitSubPartOrder.Where(name => subPartNames.Contains(name))
                                     .Concat(subPartNames.Where(name => !_splitSubPartOrder.Contains(name))).ToArray();
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
        /// <summary>
        /// The part id of a sub part of a split compound, or -1 if the compound does not hold one for it.
        /// </summary>
        public int GetSplitSubPartId(string subPartName)
        {
            if (_cellSubPartNames == null || _cellSubPartIds == null) return -1;
            //
            for (int i = 0; i < _cellSubPartNames.Length && i < _cellSubPartIds.Length; i++)
            {
                if (_cellSubPartNames[i] == subPartName && _cellSubPartIds[i] > 0) return _cellSubPartIds[i];
            }
            return -1;
        }
        public int[] GetSplitSubPartIds()
        {
            if (_cellSubPartIds == null) return new int[0];
            return _cellSubPartIds.Where(id => id > 0).Distinct().ToArray();
        }
        private T[] GetCellValues<T>(T[] cellValues, string[] subPartNames)
        {
            Dictionary<string, T> cellValuesByName = new Dictionary<string, T>();
            for (int i = 0; i < _subPartNames.Length && i < cellValues.Length; i++)
                cellValuesByName[_subPartNames[i]] = cellValues[i];
            //
            T[] values = new T[subPartNames.Length];
            for (int i = 0; i < subPartNames.Length; i++)
                cellValuesByName.TryGetValue(subPartNames[i], out values[i]);
            return values;
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
