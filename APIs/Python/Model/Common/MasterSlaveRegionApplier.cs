// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;

namespace APIs.Python.Model
{
    // Points a constraint or a contact pair at its two regions. It is the master and slave counterpart
    // of RegionApplier, and it reuses it by adapting one side at a time.
    internal static class MasterSlaveRegionApplier
    {
        // Methods
        public static void ApplyMaster(IMasterSlaveMultiRegion item, object region, string argumentName,
                                       string target, SelectionService selectionService,
                                       ModelResolver modelResolver)
        {
            MasterSide master = new MasterSide(item);
            RegionApplier.Apply(master, region, argumentName, target, selectionService, modelResolver);
        }
        public static void ApplySlave(IMasterSlaveMultiRegion item, object region, string argumentName,
                                      string target, SelectionService selectionService,
                                      ModelResolver modelResolver)
        {
            SlaveSide slave = new SlaveSide(item);
            RegionApplier.Apply(slave, region, argumentName, target, selectionService, modelResolver);
        }
        //
        private class MasterSide : IMultiRegion
        {
            private readonly IMasterSlaveMultiRegion _item;
            public MasterSide(IMasterSlaveMultiRegion item) { _item = item; }
            public string RegionName
            {
                get { return _item.MasterRegionName; }
                set { _item.MasterRegionName = value; }
            }
            public RegionTypeEnum RegionType
            {
                get { return _item.MasterRegionType; }
                set { _item.MasterRegionType = value; }
            }
            public int[] CreationIds
            {
                get { return _item.MasterCreationIds; }
                set { _item.MasterCreationIds = value; }
            }
            public Selection CreationData
            {
                get { return _item.MasterCreationData; }
                set { _item.MasterCreationData = value; }
            }
        }
        private class SlaveSide : IMultiRegion
        {
            private readonly IMasterSlaveMultiRegion _item;
            public SlaveSide(IMasterSlaveMultiRegion item) { _item = item; }
            public string RegionName
            {
                get { return _item.SlaveRegionName; }
                set { _item.SlaveRegionName = value; }
            }
            public RegionTypeEnum RegionType
            {
                get { return _item.SlaveRegionType; }
                set { _item.SlaveRegionType = value; }
            }
            public int[] CreationIds
            {
                get { return _item.SlaveCreationIds; }
                set { _item.SlaveCreationIds = value; }
            }
            public Selection CreationData
            {
                get { return _item.SlaveCreationData; }
                set { _item.SlaveCreationData = value; }
            }
        }
    }
}
