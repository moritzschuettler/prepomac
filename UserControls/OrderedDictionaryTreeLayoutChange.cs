// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;


namespace UserControls
{
    public enum OrderedDictionaryTreeLayoutAction
    {
        CreateGroup,
        RenameGroup,
        RemoveGroup,
        MoveItems,
        MoveGroup
    }

    public sealed class OrderedDictionaryTreeLayoutChange
    {
        public ViewType View { get; private set; }
        public Type ItemType { get; private set; }
        public string ParentName { get; private set; }
        public OrderedDictionaryTreeLayoutAction Action { get; private set; }
        public string[] ItemNames { get; private set; }
        public Guid[] GroupIds { get; private set; }
        public Guid? GroupId { get; private set; }
        public Guid? TargetGroupId { get; private set; }
        public int TargetIndex { get; private set; }
        public string GroupName { get; private set; }


        public OrderedDictionaryTreeLayoutChange(ViewType view, Type itemType, string parentName,
                                                 OrderedDictionaryTreeLayoutAction action, string[] itemNames,
                                                 Guid[] groupIds, Guid? groupId, Guid? targetGroupId,
                                                 int targetIndex, string groupName)
        {
            View = view;
            ItemType = itemType;
            ParentName = parentName;
            Action = action;
            ItemNames = itemNames;
            GroupIds = groupIds;
            GroupId = groupId;
            TargetGroupId = targetGroupId;
            TargetIndex = targetIndex;
            GroupName = groupName;
        }
    }
}
