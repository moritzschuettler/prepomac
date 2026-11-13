// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python.Model
{
    // One pair a contact search found. It is not a model item: it becomes one when the search result is added, or
    // when its two regions are given to add_tie or contact_pairs.add
    public class FoundContactPairAPI : BaseAPI
    {
        // Variables
        private readonly ContactPairService _service;
        private readonly MasterSlaveItem _masterSlaveItem;


        // Properties
        [PythonAPI("The name the pair is added under. Pairs that share a name, or whose name the model already " +
                   "holds, are numbered when they are added.", Returns = "str")]
        public string name { get { return _masterSlaveItem.Name; } }
        [PythonAPI("The kind of the faces of the master and of the slave side joined by a dash, such as " +
                   "Solid-Solid or Shell-Edge, each Solid, Shell, Edge for shell edge faces or Mixed. An unresolved " +
                   "pair names its one side.", Returns = "str")]
        public string geometry_type { get { return _masterSlaveItem.GeometryTypeName; } }
        [PythonAPI("True for a tie whose master and slave sides the search could not tell apart. Its faces are all " +
                   "on the master side, and adding it adds a surface holding them instead of a tie.", Returns = "bool")]
        public bool unresolved { get { return _masterSlaveItem.Unresolved; } }
        [PythonAPI("The faces of the master side as a new 'femodel' surface selection of geometry IDs.",
                   Returns = "Selection")]
        public SelectionAPI master_region { get { return _service.CreateSurfaceSelection(_masterSlaveItem.MasterGeometryIds); } }
        [PythonAPI("The faces of the slave side as a new 'femodel' surface selection of geometry IDs, or None for an " +
                   "unresolved pair.", Returns = "Selection or None")]
        public SelectionAPI slave_region
        {
            get
            {
                if (_masterSlaveItem.SlaveGeometryIds == null || _masterSlaveItem.SlaveGeometryIds.Count == 0) return null;
                //
                return _service.CreateSurfaceSelection(_masterSlaveItem.SlaveGeometryIds);
            }
        }
        [PythonAPI("The geometry IDs of the master faces, sorted. The 'geometry' selection source takes them back, so " +
                   "a found pair written to a file can be added again while the geometry stays the same.",
                   Returns = "IntList")]
        public IntListAPI master_geometry_ids { get { return ToSortedIntList(_masterSlaveItem.MasterGeometryIds); } }
        [PythonAPI("The geometry IDs of the slave faces, sorted, or an empty list for an unresolved pair.",
                   Returns = "IntList")]
        public IntListAPI slave_geometry_ids { get { return ToSortedIntList(_masterSlaveItem.SlaveGeometryIds); } }
        [PythonAPI("The names of the parts the master faces belong to, sorted.", Returns = "StringList")]
        public StringListAPI master_part_names { get { return ToPartNames(_masterSlaveItem.MasterGeometryIds); } }
        [PythonAPI("The names of the parts the slave faces belong to, sorted, or an empty list for an unresolved pair.",
                   Returns = "StringList")]
        public StringListAPI slave_part_names { get { return ToPartNames(_masterSlaveItem.SlaveGeometryIds); } }


        // Constructors
        internal FoundContactPairAPI(ContactPairService service, MasterSlaveItem masterSlaveItem)
        {
            _service = service;
            _masterSlaveItem = masterSlaveItem;
        }


        // Methods
        private static IntListAPI ToSortedIntList(HashSet<int> geometryIds)
        {
            if (geometryIds == null) return new IntListAPI(new int[0]);
            //
            int[] ids = geometryIds.ToArray();
            Array.Sort(ids);
            return new IntListAPI(ids);
        }
        private StringListAPI ToPartNames(HashSet<int> geometryIds)
        {
            if (geometryIds == null || geometryIds.Count == 0) return new StringListAPI(new string[0]);
            //
            return new StringListAPI(_service.GetPartNames(geometryIds));
        }
        public override string ToString()
        {
            return "FoundContactPair(name=" + StringListAPI.ToDisplayString(name) + ", geometry_type=" +
                   StringListAPI.ToDisplayString(geometry_type) + (unresolved ? ", unresolved=True" : "") + ")";
        }
    }
}
