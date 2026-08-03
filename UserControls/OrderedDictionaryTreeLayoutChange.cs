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


        public static OrderedDictionaryTreeLayoutChange CreateGroup(
            ViewType view, Type itemType, string parentName, string[] itemNames, Guid groupId,
            Guid? parentGroupId, int index, string groupName)
        {
            return new OrderedDictionaryTreeLayoutChange(
                view, itemType, parentName, OrderedDictionaryTreeLayoutAction.CreateGroup, itemNames, null,
                groupId, parentGroupId, index, groupName);
        }
        public static OrderedDictionaryTreeLayoutChange RenameGroup(
            ViewType view, Type itemType, string parentName, Guid groupId, string newName)
        {
            return new OrderedDictionaryTreeLayoutChange(
                view, itemType, parentName, OrderedDictionaryTreeLayoutAction.RenameGroup, null, null,
                groupId, null, -1, newName);
        }
        public static OrderedDictionaryTreeLayoutChange RemoveGroups(
            ViewType view, Type itemType, string parentName, Guid[] groupIds)
        {
            return new OrderedDictionaryTreeLayoutChange(
                view, itemType, parentName, OrderedDictionaryTreeLayoutAction.RemoveGroup, null, groupIds,
                null, null, -1, null);
        }
        public static OrderedDictionaryTreeLayoutChange MoveItems(
            ViewType view, Type itemType, string parentName, string[] itemNames, Guid? targetGroupId,
            int targetIndex)
        {
            return new OrderedDictionaryTreeLayoutChange(
                view, itemType, parentName, OrderedDictionaryTreeLayoutAction.MoveItems, itemNames, null,
                null, targetGroupId, targetIndex, null);
        }
        public static OrderedDictionaryTreeLayoutChange MoveGroup(
            ViewType view, Type itemType, string parentName, Guid groupId, Guid? targetGroupId,
            int targetIndex)
        {
            return new OrderedDictionaryTreeLayoutChange(
                view, itemType, parentName, OrderedDictionaryTreeLayoutAction.MoveGroup, null, null,
                groupId, targetGroupId, targetIndex, null);
        }


        private OrderedDictionaryTreeLayoutChange(ViewType view, Type itemType, string parentName,
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
