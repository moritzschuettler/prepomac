// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python.Model
{
    // The verbs every named model item collection shares. The type-specific add and replace methods
    // live on the derived collection, because their arguments differ per model type.
    public abstract class ModelItemsAPI<TItem, TItemAPI> : BaseAPI, IReadOnlyList<TItemAPI>
        where TItem : NamedClass
    {
        // Variables
        private readonly string _itemTypeName;


        // Properties
        // The collection interface is what makes len() and iteration work on the Python side
        public int Count { get { return GetItemNames().Length; } }


        // Constructors
        protected ModelItemsAPI(string itemTypeName)
        {
            _itemTypeName = itemTypeName;
        }


        // Methods
        public TItemAPI __getitem__(string name)
        {
            return this[name];
        }
        public TItemAPI this[string name]
        {
            get
            {
                ResolveItem(name);
                return CreateItemAPI(name);
            }
        }
        // Explicit, so that the Python side sees a single indexer and resolves collection[name] without
        // an ambiguous overload
        TItemAPI IReadOnlyList<TItemAPI>.this[int index]
        {
            get
            {
                string[] names = GetItemNames();
                if (index < 0) index = names.Length + index;
                if (index < 0 || index >= names.Length)
                    throw new PythonAPIException("The " + _itemTypeName + " index " + index + " is out of range.");
                return CreateItemAPI(names[index]);
            }
        }
        public IEnumerator<TItemAPI> GetEnumerator()
        {
            foreach (string name in GetItemNames()) yield return CreateItemAPI(name);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public int __len__()
        {
            return GetItemNames().Length;
        }

        [PythonAPI("Returns the names of the items in the collection.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(GetItemNames());
        }

        // Python's in operator; without it, in compares the name with every item object and is always False
        public bool __contains__(string name)
        {
            return contains(name);
        }

        [PythonAPI("Returns true if an item with the specified name exists. The name is compared regardless of " +
                   "case, the way the item is found by get and by indexing.", Arguments = "name: str",
                   Returns = "bool")]
        public bool contains(string name)
        {
            // The same lookup as get, so a name contains accepts is one get finds
            return name != null && GetItem(name) != null;
        }

        [PythonAPI("Returns the item with the specified name.", Arguments = "name: str")]
        public TItemAPI get(string name)
        {
            return this[name];
        }

        [PythonAPI("Returns the first item in the collection.")]
        public TItemAPI first()
        {
            string[] names = GetItemNames();
            if (names.Length == 0) throw new PythonAPIException("The " + _itemTypeName + " collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last item in the collection.")]
        public TItemAPI last()
        {
            string[] names = GetItemNames();
            if (names.Length == 0) throw new PythonAPIException("The " + _itemTypeName + " collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Duplicates the named items using command history. A name given twice is refused.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void duplicate(object names)
        {
            DuplicateItems(ResolveNames(names, "names"));
        }

        [PythonAPI("Removes the named items using command history. A name given twice is refused.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void remove(object names)
        {
            RemoveItems(ResolveNames(names, "names"));
        }

        [PythonAPI("Activates the named items using command history. A name given twice is refused.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void activate(object names)
        {
            SetItemsActive(ResolveItems(names), true);
        }

        [PythonAPI("Deactivates the named items using command history. A deactivated item is kept in the " +
                   "model but is not written to the solver input file. A name given twice is refused.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void deactivate(object names)
        {
            SetItemsActive(ResolveItems(names), false);
        }

        [PythonAPI("Renames an item using command history.", Arguments = "old_name: str, new_name: str",
                   Returns = "None")]
        public void rename(string old_name, string new_name)
        {
            TItem item = ResolveItem(old_name);
            PythonNameComparer.ValidateFormat(new_name, "new_name", item.Name, GetAdditionalNameCharacters());
            // The stored spelling decides, so a rename that changes only the case is still carried out
            if (string.Equals(item.Name, new_name, System.StringComparison.Ordinal)) return;
            //
            RenameItem(item, new_name);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return GetType().Name + "(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        //
        protected abstract string[] GetItemNames();
        protected abstract TItem GetItem(string name);
        protected abstract TItemAPI CreateItemAPI(string name);
        protected abstract void RemoveItems(string[] names);
        protected abstract void RenameItem(TItem item, string newName);
        // Not every area has a duplicate command
        protected virtual void DuplicateItems(string[] names)
        {
            throw new PythonAPIException("Duplicating a " + _itemTypeName + " is not supported.");
        }
        // Only the characters a plain model item name takes, unless the item type accepts more
        protected virtual HashSet<char> GetAdditionalNameCharacters()
        {
            return null;
        }
        // Controller.ActivateDeactivate only knows a fixed set of item types
        protected virtual void SetItemsActive(TItem[] items, bool active)
        {
            throw new PythonAPIException("Activating and deactivating a " + _itemTypeName + " is not supported.");
        }
        //
        protected TItem ResolveItem(string name)
        {
            if (name == null) throw new PythonAPIException("The " + _itemTypeName + " name must not be None.");
            //
            TItem item = GetItem(name);
            if (item == null)
                throw new PythonAPIException("The " + _itemTypeName + " '" + name + "' does not exist.");
            //
            return item;
        }
        protected TItem[] ResolveItems(object names)
        {
            string[] resolved = ResolveNames(names, "names");
            TItem[] items = new TItem[resolved.Length];
            for (int i = 0; i < resolved.Length; i++) items[i] = ResolveItem(resolved[i]);
            //
            return items;
        }
        protected string[] ResolveNames(object names, string argumentName)
        {
            string[] resolved = PythonStringListConverter.ToStringArrayOrSingle(names, argumentName);
            if (resolved == null || resolved.Length == 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be empty.");
            //
            foreach (string name in resolved) ResolveItem(name);
            PythonNameComparer.RefuseRepeated(resolved, argumentName);
            //
            return resolved;
        }
    }
}
