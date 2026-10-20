// Accord Core Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;

namespace CaeGlobals
{
    /// <summary>
    ///   Ordered dictionary.
    /// </summary>
    /// 
    /// <remarks>
    ///   This class provides a ordered dictionary implementation for C#/.NET. Unlike the rest
    ///   of the framework, this class is available under a MIT license, so please feel free to
    ///   re-use its source code in your own projects.
    /// </remarks>
    /// 
    /// <typeparam name="TKey">The types of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// 
    [Serializable]
    public class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializable, IDeserializationCallback,
                                                   IOrderedDictionaryItemLayout<TKey>
    {
        // Variables                                                                                                                
        private string _name;                               //ISerializable
        private List<TKey> _list;                           //ISerializable
        private Dictionary<TKey, TValue> _dictionary;       //ISerializable
        private OrderedDictionaryItemLayout<TKey> _itemLayout;

        private const int _itemLayoutMagic = 0x314C444F;     // "ODL1"
        private const int _itemLayoutVersion = 1;


        // Properties
        /// <summary>
        /// Returns true when this dictionary contains organizational group nodes.
        /// A false value represents the original flat ordered-dictionary behaviour.
        /// </summary>
        public bool HasItemLayout
        {
            get { return _itemLayout != null && ContainsGroup(_itemLayout.MutableNodes); }
        }

        /// <summary>
        /// Returns the optional, immutable item hierarchy. Null represents a flat layout.
        /// </summary>
        public OrderedDictionaryItemLayout<TKey> ItemLayout
        {
            get { return HasItemLayout ? _itemLayout : null; }
        }
        public IEqualityComparer<TKey> Comparer
        {
            get { return _dictionary.Comparer; }
        }


        // Constructors                                                                                                             
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class.
        /// </summary>
        public OrderedDictionary(string name)
        {
            _name = name;
            _dictionary = new Dictionary<TKey, TValue>();
            _list = new List<TKey>();
            _itemLayout = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// 
        /// <param name="capacity">The initial number of elements that the <see cref="OrderedDictionary{TKey, TValue}"/> can contain.</param>
        /// 
        public OrderedDictionary(string name, int capacity)
        {
            _name = name;
            _dictionary = new Dictionary<TKey, TValue>(capacity);
            _list = new List<TKey>(capacity);
            _itemLayout = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// 
        /// <param name="comparer">The IEqualityComparer implementation to use when comparing keys, or null to use 
        ///     the default EqualityComparer for the type of the key.</param>
        /// 
        public OrderedDictionary(string name, IEqualityComparer<TKey> comparer)
        {
            _name = name;
            _dictionary = new Dictionary<TKey, TValue>(comparer);
            _list = new List<TKey>();
            _itemLayout = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// 
        /// <param name="capacity">The initial number of elements that the <see cref="OrderedDictionary{TKey, TValue}"/> can contain.</param>
        /// <param name="comparer">The IEqualityComparer implementation to use when comparing keys, or null to use 
        ///     the default EqualityComparer for the type of the key.</param>
        /// 
        public OrderedDictionary(string name, int capacity, IEqualityComparer<TKey> comparer)
        {
            _name = name;
            _dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
            _list = new List<TKey>(capacity);
            _itemLayout = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// 
        /// <param name="dictionary">The System.Collections.Generic.IDictionary`2 whose elements are copied to the
        ///     new <see cref="OrderedDictionary{TKey, TValue}"/>.</param>
        /// 
        public OrderedDictionary(string name, IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null) throw new ArgumentNullException("dictionary");
            //
            _name = name;
            if (dictionary is CaeGlobals.OrderedDictionary<TKey, TValue> orderedDictionary)
            {
                _dictionary = new Dictionary<TKey, TValue>(orderedDictionary._dictionary.Count,
                                                           orderedDictionary._dictionary.Comparer);
                foreach (KeyValuePair<TKey, TValue> entry in orderedDictionary)
                {
                    _dictionary.Add(entry.Key, entry.Value);
                }
                _list = new List<TKey>(orderedDictionary._list);
                _itemLayout = orderedDictionary._itemLayout == null ? null : orderedDictionary._itemLayout.DeepCopy();
            }
            else
            {
                _dictionary = new Dictionary<TKey, TValue>(dictionary.Count);
                _list = new List<TKey>(dictionary.Count);
                foreach (KeyValuePair<TKey, TValue> entry in dictionary)
                {
                    _dictionary.Add(entry.Key, entry.Value);
                    _list.Add(entry.Key);
                }
                _itemLayout = null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// 
        /// <param name="dictionary">The System.Collections.Generic.IDictionary`2 whose elements are copied to the
        ///     new <see cref="OrderedDictionary{TKey, TValue}"/>.</param>
        /// <param name="comparer">The IEqualityComparer implementation to use when comparing keys, or null to use 
        ///     the default EqualityComparer for the type of the key.</param>
        /// 
        public OrderedDictionary(string name, IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            if (dictionary == null) throw new ArgumentNullException("dictionary");
            //
            _name = name;
            _dictionary = new Dictionary<TKey, TValue>(dictionary.Count, comparer);
            _list = new List<TKey>(dictionary.Count);
            foreach (KeyValuePair<TKey, TValue> entry in dictionary)
            {
                _dictionary.Add(entry.Key, entry.Value);
                _list.Add(entry.Key);
            }
            //
            if (dictionary is CaeGlobals.OrderedDictionary<TKey, TValue> orderedDictionary)
            {
                _itemLayout = orderedDictionary._itemLayout == null ? null : orderedDictionary._itemLayout.DeepCopy();
                if (_itemLayout != null) SetItemLayout(_itemLayout);
            }
            else _itemLayout = null;
        }
        public OrderedDictionary(CaeGlobals.OrderedDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null) throw new ArgumentNullException("dictionary");
            //
            _name = dictionary._name;
            _dictionary = new Dictionary<TKey, TValue>(dictionary._dictionary.Count, dictionary._dictionary.Comparer);
            foreach (KeyValuePair<TKey, TValue> entry in dictionary)
            {
                _dictionary.Add(entry.Key, entry.Value);
            }
            _list = new List<TKey>(dictionary._list);
            _itemLayout = dictionary._itemLayout == null ? null : dictionary._itemLayout.DeepCopy();
        }
        //ISerializable
        public OrderedDictionary(SerializationInfo info, StreamingContext context)
        {
            byte[] itemLayoutData = null;
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_name":
                        _name = (string)entry.Value; break;
                    case "list":        // Compatibility v1.3.4
                    case "_list":
                        _list = (List<TKey>)entry.Value; break;
                    case "dictionary":  // Compatibility v1.3.4
                    case "_dictionary":
                        _dictionary = (Dictionary<TKey, TValue>)entry.Value;
                        break;
                    case "_itemLayout":
                        itemLayoutData = entry.Value as byte[];
                        break;
                }
            }
            // Compatibility v1.3.4
            if (String.IsNullOrEmpty(_name)) _name = "Ordered dictionary";
            if (_dictionary == null) _dictionary = new Dictionary<TKey, TValue>();
            if (_list == null) _list = new List<TKey>(_dictionary.Keys);
            //
            _itemLayout = DeserializeItemLayout(itemLayoutData);
        }


        // Methods                                                                                                                  
        /// <summary>
        ///   Gets the <typeparam ref="TValue"/> at the specified index.
        /// </summary>
        /// 
        /// <param name="index">The index.</param>
        /// 
        public TValue GetValueByIndex(int index)
        {
            return this[_list[index]];
        }

        /// <summary>
        ///   Gets or sets the <typeparam ref="TValue"/> with the specified key.
        /// </summary>
        /// 
        /// <param name="key">The key.</param>
        /// 
        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (_dictionary.TryGetValue(key, out value)) return value;
                else  throw new CaeException("The given key '" + key.ToString() + "' was not present in the dictionary.");
            }
            set
            {
                if (!_dictionary.ContainsKey(key))
                {
                    _list.Add(key);
                    AddItemNodeToRoot(key);
                }
                _dictionary[key] = value;
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// 
        /// <value>The keys.</value>
        /// 
        public ICollection<TKey> Keys
        {
            get { return _list.AsReadOnly(); }
        }

        public void SortKeysAs(ICollection<TKey> keys)
        {
            List<TKey> orderedKeys = ValidateAndCopyKeys(keys);
            //
            if (_itemLayout != null)
            {
                OrderedDictionaryItemLayout<TKey> reorderedLayout = _itemLayout.DeepCopy();
                Dictionary<TKey, int> keyOrder = new Dictionary<TKey, int>(_dictionary.Comparer);
                for (int i = 0; i < orderedKeys.Count; i++) keyOrder.Add(orderedKeys[i], i);
                //
                SortLayoutNodes(reorderedLayout.MutableNodes, keyOrder);
                List<TKey> flattenedKeys = GetFlattenedKeys(reorderedLayout.MutableNodes);
                if (!KeysEqual(flattenedKeys, orderedKeys))
                {
                    throw new InvalidOperationException("The requested key order is incompatible with the current item groups. " +
                                                        "Move the grouped items explicitly.");
                }
                _itemLayout = reorderedLayout;
            }
            //
            _list = orderedKeys;
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// 
        public ICollection<TValue> Values
        {
            get { return _list.Select(x => _dictionary[x]).ToList(); }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// 
        public int Count
        {
            get { return _dictionary.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// 
        /// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
        /// 
        public bool IsReadOnly
        {
            get { return ((IDictionary<TKey, TValue>)_dictionary).IsReadOnly; }
        }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// 
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// 
        public void Add(TKey key, TValue value)
        {
            if (_dictionary.ContainsKey(key))// || _list.Contains(key))
            {
                string name = _name;
                if (name != null && name.Length > 0) name += " ";
                throw new Exception("The dictionary " + name + "already contains the key " + key.ToString() + ".");
            }
            //
            _dictionary.Add(key, value);
            _list.Add(key);
            AddItemNodeToRoot(key);
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// 
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// 
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }
        /// <summary>
        /// Inserts an element with the provided index and key and value to the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// 
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// 
        public void Insert(int index, TKey key, TValue value)
        {
            if (index < 0 || index > _list.Count) throw new IndexOutOfRangeException();
            //
            if (_dictionary.ContainsKey(key)) // || _list.Contains(key))
            {
                string name = _name;
                if (name != null && name.Length > 0) name += " ";
                throw new Exception("The dictionary " + name + "already contains the key " + key.ToString() + ".");
            }
            //
            InsertItemNodeAtFlattenedIndex(index, key);
            _dictionary.Add(key, value);
            _list.Insert(index, key);
        }

        /// <summary>
        /// Adds another dictionary to the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// 
        /// <param name="dicToAdd">The dictionary to add.</param>
        /// 
        public void AddRange(Dictionary<TKey, TValue> dicToAdd)
        {
            foreach (var item in dicToAdd) Add(item.Key, item.Value);
        }

        /// <summary>
        /// Replace an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// 
        /// <param name="oldKey">The object to use as the key of the element to remove.</param>
        /// <param name="newKey">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// 
        public bool Replace(TKey oldKey, TKey newKey, TValue value)
        {
            if (!_dictionary.ContainsKey(oldKey)) return false;
            //
            bool sameKey = _dictionary.Comparer.Equals(oldKey, newKey);
            if (!sameKey && _dictionary.ContainsKey(newKey))
                throw new ArgumentException("The replacement key already exists in the dictionary.", "newKey");
            //
            int index = FindKeyIndex(oldKey);
            if (index < 0) throw new InvalidOperationException("The ordered key list is inconsistent with the dictionary.");
            //
            ReplaceItemNodeKey(oldKey, newKey);
            _dictionary.Remove(oldKey);
            _dictionary.Add(newKey, value);
            _list[index] = newKey;
            return true;
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// 
        public void Clear()
        {
            _dictionary.Clear();
            _list.Clear();
            _itemLayout = null;
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
        /// </summary>
        /// 
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// 
        /// <returns>true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.</returns>
        /// 
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)_dictionary).Contains(item);
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the specified key.
        /// </summary>
        /// 
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2" />.</param>
        /// 
        /// <returns>true if the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the key; otherwise, false.</returns>
        /// 
        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// 
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// 
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<TKey, TValue> pair in this)
                array[arrayIndex++] = pair;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// 
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        /// 
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (TKey key in _list)
                yield return new KeyValuePair<TKey, TValue>(key, _dictionary[key]);
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// 
        /// <param name="key">The key of the element to remove.</param>
        /// 
        /// <returns>true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key" /> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2" />.</returns>
        /// 
        public bool Remove(TKey key)
        {
            if (_dictionary.Remove(key))
            {
                RemoveKeyFromList(key);
                RemoveItemNode(key);
                return true;
            }

            return false;
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            if (_dictionary.TryGetValue(key, out value))
            {
                _dictionary.Remove(key);
                RemoveKeyFromList(key);
                RemoveItemNode(key);
                return true;
            }
            //
            return false;
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// 
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// 
        /// <returns>true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        /// 
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (((IDictionary<TKey, TValue>)_dictionary).Remove(item))
            {
                RemoveKeyFromList(item.Key);
                RemoveItemNode(item.Key);
                return true;
            }

            return false;
        }


        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// 
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed uninitialized.</param>
        /// 
        /// <returns>true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the specified key; otherwise, false.</returns>
        /// 
        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);
            if (key == null) return false;
            else return ((IDictionary<TKey, TValue>)_dictionary).TryGetValue(key, out value);
        }

        // Item layout
        /// <summary>
        /// Returns a detached snapshot of the item hierarchy. A flat hierarchy is returned when no groups exist.
        /// </summary>
        public OrderedDictionaryItemLayout<TKey> GetItemLayoutSnapshot()
        {
            if (_itemLayout != null) return _itemLayout.DeepCopy();
            return CreateFlatItemLayout();
        }

        /// <summary>
        /// Applies a complete hierarchy and updates the dictionary enumeration order to its depth-first item order.
        /// </summary>
        public void SetItemLayout(OrderedDictionaryItemLayout<TKey> itemLayout)
        {
            if (itemLayout == null)
            {
                ResetItemLayout();
                return;
            }
            //
            OrderedDictionaryItemLayout<TKey> layoutCopy = itemLayout.DeepCopy();
            List<TKey> flattenedKeys = ValidateItemLayout(layoutCopy.MutableNodes);
            //
            _list = flattenedKeys;
            _itemLayout = ContainsGroup(layoutCopy.MutableNodes) ? layoutCopy : null;
        }

        /// <summary>
        /// Applies a hierarchy that no longer matches the dictionary keys. Item nodes of keys that were removed
        /// from the dictionary are dropped, empty groups are retained and dictionary keys that are missing from
        /// the hierarchy are appended to the layout root in their current order.
        /// </summary>
        public void RestoreItemLayout(OrderedDictionaryItemLayout<TKey> itemLayout)
        {
            if (itemLayout == null)
            {
                ResetItemLayout();
                return;
            }
            //
            OrderedDictionaryItemLayout<TKey> layoutCopy = itemLayout.DeepCopy();
            HashSet<TKey> layoutKeys = new HashSet<TKey>(_dictionary.Comparer);
            RemoveUnknownItemNodes(layoutCopy.MutableNodes, layoutKeys);
            //
            foreach (TKey key in _list)
            {
                if (layoutKeys.Contains(key)) continue;
                layoutCopy.MutableNodes.Add(OrderedDictionaryItemNode<TKey>.CreateItem(key));
            }
            //
            SetItemLayout(layoutCopy);
        }

        /// <summary>
        /// Removes all organizational groups while retaining the current flat item order.
        /// </summary>
        public void ResetItemLayout()
        {
            _itemLayout = null;
        }

        /// <summary>
        /// Creates an empty group at the end of the root layout.
        /// </summary>
        public Guid CreateGroup(string name)
        {
            EnsureItemLayout();
            return CreateGroup(name, null, _itemLayout.MutableNodes.Count);
        }

        /// <summary>
        /// Creates an empty group under the selected parent group. A null parent identifies the layout root.
        /// </summary>
        public Guid CreateGroup(string name, Guid? parentGroupId, int index)
        {
            return CreateGroup(Guid.NewGuid(), name, parentGroupId, index);
        }

        /// <summary>
        /// Creates an empty group with a specified stable id. This allows corresponding groups in separate
        /// ordered dictionaries to be linked without using their names as identity.
        /// </summary>
        public Guid CreateGroup(Guid groupId, string name, Guid? parentGroupId, int index)
        {
            if (groupId == Guid.Empty) throw new ArgumentException("The group id must not be empty.", "groupId");
            name = OrderedDictionaryItemNode<TKey>.ValidateGroupName(name);
            EnsureItemLayout();
            //
            OrderedDictionaryItemNode<TKey> existingNode;
            List<OrderedDictionaryItemNode<TKey>> existingParent;
            if (TryFindNodeById(_itemLayout.MutableNodes, groupId, out existingNode, out existingParent))
                throw new ArgumentException("The group id already exists in the item layout.", "groupId");
            //
            List<OrderedDictionaryItemNode<TKey>> children = GetGroupChildren(parentGroupId);
            if (index < 0 || index > children.Count) throw new ArgumentOutOfRangeException("index");
            EnsureUniqueGroupName(children, name, Guid.Empty);
            //
            OrderedDictionaryItemNode<TKey> group =
                OrderedDictionaryItemNode<TKey>.CreateGroup(groupId, name, null);
            children.Insert(index, group);
            return group.Id;
        }

        /// <summary>
        /// Renames a group. Group names are unique among sibling groups.
        /// </summary>
        public void RenameGroup(Guid groupId, string newName)
        {
            newName = OrderedDictionaryItemNode<TKey>.ValidateGroupName(newName);
            if (_itemLayout == null) throw new KeyNotFoundException("The group was not found.");
            //
            OrderedDictionaryItemNode<TKey> group;
            List<OrderedDictionaryItemNode<TKey>> parent;
            if (!TryFindNodeById(_itemLayout.MutableNodes, groupId, out group, out parent) || !group.IsGroup)
                throw new KeyNotFoundException("The group was not found.");
            //
            EnsureUniqueGroupName(parent, newName, groupId);
            group.SetName(newName);
        }

        /// <summary>
        /// Removes a group and moves its children to the former position of the group.
        /// Dictionary items are never deleted by this operation.
        /// </summary>
        public void RemoveGroup(Guid groupId)
        {
            if (_itemLayout == null) throw new KeyNotFoundException("The group was not found.");
            //
            OrderedDictionaryItemNode<TKey> group;
            List<OrderedDictionaryItemNode<TKey>> parent;
            if (!TryFindNodeById(_itemLayout.MutableNodes, groupId, out group, out parent) || !group.IsGroup)
                throw new KeyNotFoundException("The group was not found.");
            //
            foreach (OrderedDictionaryItemNode<TKey> child in group.MutableChildren)
            {
                if (!child.IsGroup) continue;
                //
                foreach (OrderedDictionaryItemNode<TKey> sibling in parent)
                {
                    if (sibling.Id != groupId && sibling.IsGroup &&
                        String.Equals(sibling.Name, child.Name, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Removing the group would create duplicate sibling group names.");
                }
            }
            //
            int index = parent.IndexOf(group);
            parent.RemoveAt(index);
            parent.InsertRange(index, group.MutableChildren);
            NormalizeItemLayout();
        }

        /// <summary>
        /// Moves dictionary items into a group or the root. The target index is evaluated after the items are removed.
        /// Selected items retain their current relative order.
        /// </summary>
        public void MoveItems(ICollection<TKey> keys, Guid? targetGroupId, int targetIndex)
        {
            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Count == 0) return;
            //
            HashSet<TKey> selectedKeys = new HashSet<TKey>(_dictionary.Comparer);
            foreach (TKey key in keys)
            {
                if (!_dictionary.ContainsKey(key)) throw new KeyNotFoundException("An item to move was not found.");
                if (!selectedKeys.Add(key)) throw new ArgumentException("An item can only be moved once.", "keys");
            }
            //
            EnsureItemLayout();
            List<OrderedDictionaryItemNode<TKey>> targetChildren = GetGroupChildren(targetGroupId);
            List<OrderedDictionaryItemNode<TKey>> nodesToMove = new List<OrderedDictionaryItemNode<TKey>>();
            List<List<OrderedDictionaryItemNode<TKey>>> sourceParents =
                new List<List<OrderedDictionaryItemNode<TKey>>>();
            //
            foreach (TKey key in _list)
            {
                if (!selectedKeys.Contains(key)) continue;
                //
                OrderedDictionaryItemNode<TKey> node;
                List<OrderedDictionaryItemNode<TKey>> parent;
                if (!TryFindItemNode(_itemLayout.MutableNodes, key, out node, out parent))
                    throw new InvalidOperationException("The item layout is inconsistent with the dictionary.");
                nodesToMove.Add(node);
                sourceParents.Add(parent);
            }
            //
            int targetCountAfterRemoval = targetChildren.Count;
            foreach (List<OrderedDictionaryItemNode<TKey>> sourceParent in sourceParents)
            {
                if (ReferenceEquals(sourceParent, targetChildren)) targetCountAfterRemoval--;
            }
            if (targetIndex < 0 || targetIndex > targetCountAfterRemoval)
                throw new ArgumentOutOfRangeException("targetIndex");
            //
            for (int i = 0; i < nodesToMove.Count; i++)
            {
                sourceParents[i].Remove(nodesToMove[i]);
            }
            //
            targetChildren.InsertRange(targetIndex, nodesToMove);
            SyncListFromItemLayout();
            NormalizeItemLayout();
        }

        /// <summary>
        /// Moves a group into another group or the root. A group cannot be moved into itself or one of its descendants.
        /// The target index is evaluated after the group is removed.
        /// </summary>
        public void MoveGroup(Guid groupId, Guid? targetGroupId, int targetIndex)
        {
            if (_itemLayout == null) throw new KeyNotFoundException("The group was not found.");
            //
            OrderedDictionaryItemNode<TKey> group;
            List<OrderedDictionaryItemNode<TKey>> currentParent;
            if (!TryFindNodeById(_itemLayout.MutableNodes, groupId, out group, out currentParent) || !group.IsGroup)
                throw new KeyNotFoundException("The group was not found.");
            //
            if (targetGroupId.HasValue &&
                (targetGroupId.Value == groupId || ContainsNodeId(group.MutableChildren, targetGroupId.Value)))
                throw new InvalidOperationException("A group cannot be moved into itself or one of its descendants.");
            //
            List<OrderedDictionaryItemNode<TKey>> targetChildren = GetGroupChildren(targetGroupId);
            EnsureUniqueGroupName(targetChildren, group.Name, groupId);
            int targetCountAfterRemoval = targetChildren.Count - (ReferenceEquals(currentParent, targetChildren) ? 1 : 0);
            if (targetIndex < 0 || targetIndex > targetCountAfterRemoval)
                throw new ArgumentOutOfRangeException("targetIndex");
            //
            currentParent.Remove(group);
            //
            targetChildren.Insert(targetIndex, group);
            SyncListFromItemLayout();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// 
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        /// 
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        //
        public CaeGlobals.OrderedDictionary<TKey, TValue> DeepCopy()
        {
            return new CaeGlobals.OrderedDictionary<TKey, TValue>(this);
        }

        // Private item layout methods
        private void EnsureItemLayout()
        {
            if (_itemLayout != null) return;
            _itemLayout = CreateFlatItemLayout();
        }
        private OrderedDictionaryItemLayout<TKey> CreateFlatItemLayout()
        {
            List<OrderedDictionaryItemNode<TKey>> nodes = new List<OrderedDictionaryItemNode<TKey>>(_list.Count);
            foreach (TKey key in _list) nodes.Add(OrderedDictionaryItemNode<TKey>.CreateItem(key));
            return new OrderedDictionaryItemLayout<TKey>(nodes);
        }
        private void RemoveUnknownItemNodes(List<OrderedDictionaryItemNode<TKey>> nodes, HashSet<TKey> itemKeys)
        {
            int i = 0;
            while (i < nodes.Count)
            {
                OrderedDictionaryItemNode<TKey> node = nodes[i];
                if (node.IsGroup) RemoveUnknownItemNodes(node.MutableChildren, itemKeys);
                else if (!_dictionary.ContainsKey(node.Key) || !itemKeys.Add(node.Key))
                {
                    nodes.RemoveAt(i);
                    continue;
                }
                i++;
            }
        }
        private void NormalizeItemLayout()
        {
            if (_itemLayout != null && !ContainsGroup(_itemLayout.MutableNodes)) _itemLayout = null;
        }
        private void SyncListFromItemLayout()
        {
            if (_itemLayout != null) _list = GetFlattenedKeys(_itemLayout.MutableNodes);
        }
        private void AddItemNodeToRoot(TKey key)
        {
            if (_itemLayout != null) _itemLayout.MutableNodes.Add(OrderedDictionaryItemNode<TKey>.CreateItem(key));
        }
        private void InsertItemNodeAtFlattenedIndex(int index, TKey key)
        {
            if (_itemLayout == null) return;
            //
            if (index == _list.Count)
            {
                _itemLayout.MutableNodes.Add(OrderedDictionaryItemNode<TKey>.CreateItem(key));
                return;
            }
            //
            TKey nextKey = _list[index];
            OrderedDictionaryItemNode<TKey> nextNode;
            List<OrderedDictionaryItemNode<TKey>> parent;
            if (!TryFindItemNode(_itemLayout.MutableNodes, nextKey, out nextNode, out parent))
                throw new InvalidOperationException("The item layout is inconsistent with the dictionary.");
            parent.Insert(parent.IndexOf(nextNode), OrderedDictionaryItemNode<TKey>.CreateItem(key));
        }
        private void ReplaceItemNodeKey(TKey oldKey, TKey newKey)
        {
            if (_itemLayout == null) return;
            //
            OrderedDictionaryItemNode<TKey> node;
            List<OrderedDictionaryItemNode<TKey>> parent;
            if (!TryFindItemNode(_itemLayout.MutableNodes, oldKey, out node, out parent))
                throw new InvalidOperationException("The item layout is inconsistent with the dictionary.");
            node.SetKey(newKey);
        }
        private void RemoveItemNode(TKey key)
        {
            if (_itemLayout == null) return;
            //
            OrderedDictionaryItemNode<TKey> node;
            List<OrderedDictionaryItemNode<TKey>> parent;
            if (!TryFindItemNode(_itemLayout.MutableNodes, key, out node, out parent))
                throw new InvalidOperationException("The item layout is inconsistent with the dictionary.");
            parent.Remove(node);
        }
        private void RemoveKeyFromList(TKey key)
        {
            int index = FindKeyIndex(key);
            if (index < 0) throw new InvalidOperationException("The ordered key list is inconsistent with the dictionary.");
            _list.RemoveAt(index);
        }
        private int FindKeyIndex(TKey key)
        {
            return _list.FindIndex(delegate (TKey existingKey)
            {
                return _dictionary.Comparer.Equals(existingKey, key);
            });
        }
        private List<TKey> ValidateAndCopyKeys(ICollection<TKey> keys)
        {
            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Count != _dictionary.Count)
                throw new ArgumentException("The key order must contain every dictionary key exactly once.", "keys");
            //
            HashSet<TKey> uniqueKeys = new HashSet<TKey>(_dictionary.Comparer);
            List<TKey> orderedKeys = new List<TKey>(keys.Count);
            foreach (TKey key in keys)
            {
                if (!_dictionary.ContainsKey(key))
                    throw new ArgumentException("The key order contains a key that is not in the dictionary.", "keys");
                if (!uniqueKeys.Add(key))
                    throw new ArgumentException("The key order contains a duplicate key.", "keys");
                orderedKeys.Add(key);
            }
            return orderedKeys;
        }
        private List<TKey> ValidateItemLayout(List<OrderedDictionaryItemNode<TKey>> nodes)
        {
            HashSet<Guid> nodeIds = new HashSet<Guid>();
            HashSet<TKey> itemKeys = new HashSet<TKey>(_dictionary.Comparer);
            List<TKey> flattenedKeys = new List<TKey>(_dictionary.Count);
            ValidateItemLayoutNodes(nodes, nodeIds, itemKeys, flattenedKeys);
            //
            if (itemKeys.Count != _dictionary.Count)
                throw new ArgumentException("The item layout must contain every dictionary key exactly once.", "itemLayout");
            return flattenedKeys;
        }
        private void ValidateItemLayoutNodes(List<OrderedDictionaryItemNode<TKey>> nodes, HashSet<Guid> nodeIds,
                                             HashSet<TKey> itemKeys, List<TKey> flattenedKeys)
        {
            HashSet<string> siblingGroupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (OrderedDictionaryItemNode<TKey> node in nodes)
            {
                if (node == null) throw new ArgumentException("An item layout node must not be null.", "itemLayout");
                if (node.Id == Guid.Empty || !nodeIds.Add(node.Id))
                    throw new ArgumentException("Every item layout node must have a unique non-empty id.", "itemLayout");
                //
                if (node.IsItem)
                {
                    if (node.MutableChildren.Count != 0)
                        throw new ArgumentException("An item node cannot contain child nodes.", "itemLayout");
                    if (!_dictionary.ContainsKey(node.Key))
                        throw new ArgumentException("The item layout contains a key that is not in the dictionary.",
                                                    "itemLayout");
                    if (!itemKeys.Add(node.Key))
                        throw new ArgumentException("The item layout contains a duplicate dictionary key.", "itemLayout");
                    flattenedKeys.Add(node.Key);
                }
                else if (node.IsGroup)
                {
                    string name = OrderedDictionaryItemNode<TKey>.ValidateGroupName(node.Name);
                    if (!siblingGroupNames.Add(name))
                        throw new ArgumentException("Sibling groups must have unique names.", "itemLayout");
                    ValidateItemLayoutNodes(node.MutableChildren, nodeIds, itemKeys, flattenedKeys);
                }
                else throw new ArgumentException("The item layout contains an unsupported node type.", "itemLayout");
            }
        }
        private List<OrderedDictionaryItemNode<TKey>> GetGroupChildren(Guid? groupId)
        {
            if (!groupId.HasValue) return _itemLayout.MutableNodes;
            //
            OrderedDictionaryItemNode<TKey> group;
            List<OrderedDictionaryItemNode<TKey>> parent;
            if (!TryFindNodeById(_itemLayout.MutableNodes, groupId.Value, out group, out parent) || !group.IsGroup)
                throw new KeyNotFoundException("The target group was not found.");
            return group.MutableChildren;
        }
        private void EnsureUniqueGroupName(List<OrderedDictionaryItemNode<TKey>> siblings, string name, Guid ignoredNodeId)
        {
            foreach (OrderedDictionaryItemNode<TKey> sibling in siblings)
            {
                if (sibling.IsGroup && sibling.Id != ignoredNodeId &&
                    String.Equals(sibling.Name, name, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("A sibling group with the same name already exists.", "name");
            }
        }
        private bool TryFindNodeById(List<OrderedDictionaryItemNode<TKey>> nodes, Guid nodeId,
                                     out OrderedDictionaryItemNode<TKey> result,
                                     out List<OrderedDictionaryItemNode<TKey>> parent)
        {
            foreach (OrderedDictionaryItemNode<TKey> node in nodes)
            {
                if (node.Id == nodeId)
                {
                    result = node;
                    parent = nodes;
                    return true;
                }
                if (node.IsGroup && TryFindNodeById(node.MutableChildren, nodeId, out result, out parent)) return true;
            }
            result = null;
            parent = null;
            return false;
        }
        private bool TryFindItemNode(List<OrderedDictionaryItemNode<TKey>> nodes, TKey key,
                                     out OrderedDictionaryItemNode<TKey> result,
                                     out List<OrderedDictionaryItemNode<TKey>> parent)
        {
            foreach (OrderedDictionaryItemNode<TKey> node in nodes)
            {
                if (node.IsItem && _dictionary.Comparer.Equals(node.Key, key))
                {
                    result = node;
                    parent = nodes;
                    return true;
                }
                if (node.IsGroup && TryFindItemNode(node.MutableChildren, key, out result, out parent)) return true;
            }
            result = null;
            parent = null;
            return false;
        }
        private bool ContainsNodeId(List<OrderedDictionaryItemNode<TKey>> nodes, Guid nodeId)
        {
            OrderedDictionaryItemNode<TKey> node;
            List<OrderedDictionaryItemNode<TKey>> parent;
            return TryFindNodeById(nodes, nodeId, out node, out parent);
        }
        private bool ContainsGroup(List<OrderedDictionaryItemNode<TKey>> nodes)
        {
            foreach (OrderedDictionaryItemNode<TKey> node in nodes)
            {
                if (node.IsGroup) return true;
            }
            return false;
        }
        private List<TKey> GetFlattenedKeys(List<OrderedDictionaryItemNode<TKey>> nodes)
        {
            List<TKey> keys = new List<TKey>();
            AddFlattenedKeys(nodes, keys);
            return keys;
        }
        private void AddFlattenedKeys(List<OrderedDictionaryItemNode<TKey>> nodes, List<TKey> keys)
        {
            foreach (OrderedDictionaryItemNode<TKey> node in nodes)
            {
                if (node.IsItem) keys.Add(node.Key);
                else AddFlattenedKeys(node.MutableChildren, keys);
            }
        }
        private void SortLayoutNodes(List<OrderedDictionaryItemNode<TKey>> nodes, Dictionary<TKey, int> keyOrder)
        {
            foreach (OrderedDictionaryItemNode<TKey> node in nodes)
            {
                if (node.IsGroup) SortLayoutNodes(node.MutableChildren, keyOrder);
            }
            //
            List<int> occupiedIndices = new List<int>();
            List<OrderedDictionaryItemNode<TKey>> nonEmptyNodes = new List<OrderedDictionaryItemNode<TKey>>();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (GetFirstKeyOrder(nodes[i], keyOrder) != Int32.MaxValue)
                {
                    occupiedIndices.Add(i);
                    nonEmptyNodes.Add(nodes[i]);
                }
            }
            nonEmptyNodes.Sort(delegate (OrderedDictionaryItemNode<TKey> x, OrderedDictionaryItemNode<TKey> y)
            {
                return GetFirstKeyOrder(x, keyOrder).CompareTo(GetFirstKeyOrder(y, keyOrder));
            });
            for (int i = 0; i < occupiedIndices.Count; i++) nodes[occupiedIndices[i]] = nonEmptyNodes[i];
        }
        private int GetFirstKeyOrder(OrderedDictionaryItemNode<TKey> node, Dictionary<TKey, int> keyOrder)
        {
            if (node.IsItem) return keyOrder[node.Key];
            //
            int first = Int32.MaxValue;
            foreach (OrderedDictionaryItemNode<TKey> child in node.MutableChildren)
            {
                int childFirst = GetFirstKeyOrder(child, keyOrder);
                if (childFirst < first) first = childFirst;
            }
            return first;
        }
        private bool KeysEqual(List<TKey> first, List<TKey> second)
        {
            if (first.Count != second.Count) return false;
            for (int i = 0; i < first.Count; i++)
            {
                if (!_dictionary.Comparer.Equals(first[i], second[i])) return false;
            }
            return true;
        }
        
        // ISerialization
        public void OnDeserialization(object sender)
        {
            _dictionary.OnDeserialization(sender);
            //
            if (!IsOrderedKeyListValid())
            {
                _list = new List<TKey>(_dictionary.Keys);
                _itemLayout = null;
                return;
            }
            //
            if (_itemLayout != null)
            {
                try
                {
                    SetItemLayout(_itemLayout);
                }
                catch
                {
                    // A damaged or unsupported layout must not prevent the underlying dictionary from loading.
                    _itemLayout = null;
                }
            }
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            info.AddValue("_name", _name, typeof(string));
            info.AddValue("_list", _list, typeof(List<TKey>));
            info.AddValue("_dictionary", _dictionary, typeof(Dictionary<TKey, TValue>));
            info.AddValue("_itemLayout", SerializeItemLayout(), typeof(byte[]));
        }
        private bool IsOrderedKeyListValid()
        {
            if (_list == null || _list.Count != _dictionary.Count) return false;
            //
            HashSet<TKey> keys = new HashSet<TKey>(_dictionary.Comparer);
            foreach (TKey key in _list)
            {
                if (!_dictionary.ContainsKey(key) || !keys.Add(key)) return false;
            }
            return true;
        }
        private byte[] SerializeItemLayout()
        {
            if (!HasItemLayout) return null;
            //
            Dictionary<TKey, int> keyIndices = new Dictionary<TKey, int>(_dictionary.Comparer);
            for (int i = 0; i < _list.Count; i++) keyIndices.Add(_list[i], i);
            //
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(_itemLayoutMagic);
                writer.Write(_itemLayoutVersion);
                writer.Write(_itemLayout.MutableNodes.Count);
                foreach (OrderedDictionaryItemNode<TKey> node in _itemLayout.MutableNodes)
                {
                    SerializeItemLayoutNode(writer, node, keyIndices);
                }
                writer.Flush();
                return stream.ToArray();
            }
        }
        private void SerializeItemLayoutNode(BinaryWriter writer, OrderedDictionaryItemNode<TKey> node,
                                             Dictionary<TKey, int> keyIndices)
        {
            writer.Write((byte)node.NodeType);
            writer.Write(node.Id.ToByteArray());
            //
            if (node.IsItem)
            {
                int keyIndex;
                if (!keyIndices.TryGetValue(node.Key, out keyIndex))
                    throw new SerializationException("The item layout contains a key that is not in the dictionary.");
                writer.Write(keyIndex);
            }
            else
            {
                writer.Write(node.Name);
                writer.Write(node.MutableChildren.Count);
                foreach (OrderedDictionaryItemNode<TKey> child in node.MutableChildren)
                {
                    SerializeItemLayoutNode(writer, child, keyIndices);
                }
            }
        }
        private OrderedDictionaryItemLayout<TKey> DeserializeItemLayout(byte[] data)
        {
            if (data == null || data.Length == 0) return null;
            //
            try
            {
                using (MemoryStream stream = new MemoryStream(data, false))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadInt32() != _itemLayoutMagic) return null;
                    if (reader.ReadInt32() != _itemLayoutVersion) return null;
                    //
                    int count = reader.ReadInt32();
                    if (count < 0) return null;
                    //
                    List<OrderedDictionaryItemNode<TKey>> nodes =
                        new List<OrderedDictionaryItemNode<TKey>>(count);
                    for (int i = 0; i < count; i++) nodes.Add(DeserializeItemLayoutNode(reader));
                    if (stream.Position != stream.Length) return null;
                    return new OrderedDictionaryItemLayout<TKey>(nodes);
                }
            }
            catch
            {
                return null;
            }
        }
        private OrderedDictionaryItemNode<TKey> DeserializeItemLayoutNode(BinaryReader reader)
        {
            OrderedDictionaryItemNodeType nodeType = (OrderedDictionaryItemNodeType)reader.ReadByte();
            byte[] guidBytes = reader.ReadBytes(16);
            if (guidBytes.Length != 16) throw new SerializationException("Invalid item layout node id.");
            Guid id = new Guid(guidBytes);
            //
            if (nodeType == OrderedDictionaryItemNodeType.Item)
            {
                int keyIndex = reader.ReadInt32();
                if (keyIndex < 0 || keyIndex >= _list.Count)
                    throw new SerializationException("Invalid item layout key index.");
                return OrderedDictionaryItemNode<TKey>.CreateItem(id, _list[keyIndex]);
            }
            else if (nodeType == OrderedDictionaryItemNodeType.Group)
            {
                string name = reader.ReadString();
                int count = reader.ReadInt32();
                if (count < 0) throw new SerializationException("Invalid item layout child count.");
                //
                List<OrderedDictionaryItemNode<TKey>> children =
                    new List<OrderedDictionaryItemNode<TKey>>(count);
                for (int i = 0; i < count; i++) children.Add(DeserializeItemLayoutNode(reader));
                return OrderedDictionaryItemNode<TKey>.CreateGroup(id, name, children);
            }
            else throw new SerializationException("Unsupported item layout node type.");
        }
    }
}
