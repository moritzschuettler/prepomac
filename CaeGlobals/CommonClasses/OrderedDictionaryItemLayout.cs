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
using System.Collections.ObjectModel;

namespace CaeGlobals
{
    /// <summary>
    /// Provides access to the item hierarchy without exposing the dictionary value type.
    /// This is primarily used by the model-tree command routing layer.
    /// </summary>
    public interface IOrderedDictionaryItemLayout<TKey>
    {
        ICollection<TKey> Keys { get; }
        IEqualityComparer<TKey> Comparer { get; }
        OrderedDictionaryItemLayout<TKey> GetItemLayoutSnapshot();
        void SetItemLayout(OrderedDictionaryItemLayout<TKey> itemLayout);
        Guid CreateGroup(string name, Guid? parentGroupId, int index);
        void RenameGroup(Guid groupId, string newName);
        void RemoveGroup(Guid groupId);
        void MoveItems(ICollection<TKey> keys, Guid? targetGroupId, int targetIndex);
        void MoveGroup(Guid groupId, Guid? targetGroupId, int targetIndex);
    }

    public enum OrderedDictionaryItemNodeType
    {
        Item,
        Group
    }

    /// <summary>
    /// A UI-independent node used to organize the items of an <see cref="OrderedDictionary{TKey, TValue}"/>.
    /// Item nodes reference dictionary keys while group nodes contain child nodes.
    /// </summary>
    [Serializable]
    public sealed class OrderedDictionaryItemNode<TKey>
    {
        private Guid _id;
        private OrderedDictionaryItemNodeType _nodeType;
        private TKey _key;
        private string _name;
        private List<OrderedDictionaryItemNode<TKey>> _children;


        public Guid Id { get { return _id; } }
        public OrderedDictionaryItemNodeType NodeType { get { return _nodeType; } }
        public bool IsItem { get { return _nodeType == OrderedDictionaryItemNodeType.Item; } }
        public bool IsGroup { get { return _nodeType == OrderedDictionaryItemNodeType.Group; } }
        public TKey Key { get { return _key; } }
        public string Name { get { return _name; } }
        public ReadOnlyCollection<OrderedDictionaryItemNode<TKey>> Children
        {
            get { return _children.AsReadOnly(); }
        }


        private OrderedDictionaryItemNode(Guid id, OrderedDictionaryItemNodeType nodeType, TKey key, string name,
                                          IEnumerable<OrderedDictionaryItemNode<TKey>> children)
        {
            if (id == Guid.Empty) throw new ArgumentException("The node id must not be empty.", "id");
            //
            _id = id;
            _nodeType = nodeType;
            _key = key;
            _name = name;
            _children = new List<OrderedDictionaryItemNode<TKey>>();
            //
            if (children != null)
            {
                foreach (OrderedDictionaryItemNode<TKey> child in children)
                {
                    if (child == null) throw new ArgumentException("A layout node must not be null.", "children");
                    _children.Add(child.DeepCopy());
                }
            }
        }


        public static OrderedDictionaryItemNode<TKey> CreateItem(TKey key)
        {
            if (ReferenceEquals(key, null)) throw new ArgumentNullException("key");
            return new OrderedDictionaryItemNode<TKey>(Guid.NewGuid(), OrderedDictionaryItemNodeType.Item, key, null, null);
        }
        public static OrderedDictionaryItemNode<TKey> CreateGroup(string name)
        {
            return CreateGroup(name, null);
        }
        public static OrderedDictionaryItemNode<TKey> CreateGroup(string name,
                                                                  IEnumerable<OrderedDictionaryItemNode<TKey>> children)
        {
            name = ValidateGroupName(name);
            return new OrderedDictionaryItemNode<TKey>(Guid.NewGuid(), OrderedDictionaryItemNodeType.Group,
                                                       default(TKey), name, children);
        }


        internal static OrderedDictionaryItemNode<TKey> CreateItem(Guid id, TKey key)
        {
            return new OrderedDictionaryItemNode<TKey>(id, OrderedDictionaryItemNodeType.Item, key, null, null);
        }
        internal static OrderedDictionaryItemNode<TKey> CreateGroup(Guid id, string name,
                                                                     IEnumerable<OrderedDictionaryItemNode<TKey>> children)
        {
            return new OrderedDictionaryItemNode<TKey>(id, OrderedDictionaryItemNodeType.Group, default(TKey),
                                                       ValidateGroupName(name), children);
        }
        internal List<OrderedDictionaryItemNode<TKey>> MutableChildren
        {
            get { return _children; }
        }
        internal void SetKey(TKey key)
        {
            if (!IsItem) throw new InvalidOperationException("Only item nodes contain keys.");
            if (ReferenceEquals(key, null)) throw new ArgumentNullException("key");
            _key = key;
        }
        internal void SetName(string name)
        {
            if (!IsGroup) throw new InvalidOperationException("Only group nodes contain names.");
            _name = ValidateGroupName(name);
        }
        internal OrderedDictionaryItemNode<TKey> DeepCopy()
        {
            return new OrderedDictionaryItemNode<TKey>(_id, _nodeType, _key, _name, _children);
        }
        internal static string ValidateGroupName(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentException("The group name must not be empty.", "name");
            return name.Trim();
        }
    }

    /// <summary>
    /// An immutable snapshot of the hierarchy used to display an ordered dictionary.
    /// </summary>
    [Serializable]
    public sealed class OrderedDictionaryItemLayout<TKey>
    {
        private List<OrderedDictionaryItemNode<TKey>> _nodes;


        public ReadOnlyCollection<OrderedDictionaryItemNode<TKey>> Nodes
        {
            get { return _nodes.AsReadOnly(); }
        }


        public OrderedDictionaryItemLayout(IEnumerable<OrderedDictionaryItemNode<TKey>> nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            //
            _nodes = new List<OrderedDictionaryItemNode<TKey>>();
            foreach (OrderedDictionaryItemNode<TKey> node in nodes)
            {
                if (node == null) throw new ArgumentException("A layout node must not be null.", "nodes");
                _nodes.Add(node.DeepCopy());
            }
        }


        internal List<OrderedDictionaryItemNode<TKey>> MutableNodes
        {
            get { return _nodes; }
        }
        internal OrderedDictionaryItemLayout<TKey> DeepCopy()
        {
            return new OrderedDictionaryItemLayout<TKey>(_nodes);
        }
    }
}
