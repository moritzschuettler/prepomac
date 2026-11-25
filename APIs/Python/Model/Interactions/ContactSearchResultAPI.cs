// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using CaeModel;
using System.Collections;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    // What a contact search found. Nothing is added to the model until add is called, so a script can look at the
    // pairs first. A recorded script searches again when it is regenerated, on the mesh the model has then
    public class ContactSearchResultAPI : BaseAPI, IReadOnlyList<FoundContactPairAPI>
    {
        // Variables
        private readonly ContactPairService _service;
        private readonly ContactSearchPairTypeEnum _pairType;
        private readonly double _distance;
        private readonly List<MasterSlaveItem> _masterSlaveItems;
        private readonly FoundContactPairAPI[] _pairs;


        // Properties
        public int Count { get { return _pairs.Length; } }

        [PythonAPI("The type of the pairs the search looked for: Tie or Contact.", Returns = "str")]
        public string pair_type { get { return _pairType.ToString(); } }
        [PythonAPI("The number of pairs found.", Returns = "int")]
        public int count { get { return _pairs.Length; } }
        public FoundContactPairAPI this[int index]
        {
            get
            {
                int position = index < 0 ? _pairs.Length + index : index;
                if (position < 0 || position >= _pairs.Length)
                    throw new PythonAPIException("The index " + index + " is out of range for a search result of " +
                                                 _pairs.Length + " pairs.");
                return _pairs[position];
            }
        }


        // Constructors
        internal ContactSearchResultAPI(ContactPairService service, ContactSearchPairTypeEnum pairType, double distance,
                                        List<MasterSlaveItem> masterSlaveItems)
        {
            _service = service;
            _pairType = pairType;
            _distance = distance;
            _masterSlaveItems = masterSlaveItems != null ? masterSlaveItems : new List<MasterSlaveItem>();
            _pairs = new FoundContactPairAPI[_masterSlaveItems.Count];
            for (int i = 0; i < _pairs.Length; i++) _pairs[i] = new FoundContactPairAPI(service, _masterSlaveItems[i]);
        }


        // Methods
        public int __len__()
        {
            return _pairs.Length;
        }
        public FoundContactPairAPI __getitem__(int index)
        {
            return this[index];
        }

        [PythonAPI("Adds every pair found using command history, the way OK of the Search Contact Pairs dialog adds " +
                   "them, and returns the names of what it added. A tie is added with the search distance as its " +
                   "position tolerance, and an unresolved one as a surface holding its faces. A contact pair " +
                   "needs surface_interaction_name and is added with the search distance as its adjustment size. " +
                   "adjust is False unless given. method is NodeToSurface or SurfaceToSurface, the default, as the " +
                   "dialog offers; surface_interaction_name and method are refused for ties.",
                   Arguments = "adjust: bool = False, surface_interaction_name: str = None, " +
                               "method: str = 'SurfaceToSurface'", Returns = "StringList",
                   Example = "pmx.model.contact_pairs.search(0.01).add()")]
        public StringListAPI add(object adjust = null, object surface_interaction_name = null, object method = null)
        {
            bool adjustValue = PythonValueConverter.ToBoolOrDefault(adjust, false, "adjust");
            string surfaceInteractionName = null;
            ContactPairMethod methodValue = ContactPairMethod.SurfaceToSurface;
            if (_pairType == ContactSearchPairTypeEnum.Tie)
            {
                if (surface_interaction_name != null)
                    throw new PythonAPIException("A tie has no surface interaction. Leave out " +
                                                 "'surface_interaction_name', or search with pair_type='Contact'.");
                if (method != null)
                    throw new PythonAPIException("A tie has no contact method. Leave out 'method', or search with " +
                                                 "pair_type='Contact'.");
            }
            else
            {
                if (surface_interaction_name == null)
                    throw new PythonAPIException("A contact pair needs a surface interaction. Give " +
                                                 "'surface_interaction_name'.");
                surfaceInteractionName = _service.ResolveSurfaceInteractionName(surface_interaction_name);
                if (method != null)
                {
                    string[] possibleNames = new string[] { ContactPairMethod.NodeToSurface.ToString(),
                                                            ContactPairMethod.SurfaceToSurface.ToString() };
                    methodValue = PythonValueConverter.ToEnum<ContactPairMethod>(
                        PythonValueConverter.ToText(method, "method", false), "method", possibleNames);
                    if (methodValue != ContactPairMethod.NodeToSurface && methodValue != ContactPairMethod.SurfaceToSurface)
                        throw new PythonAPIException("The search adds contact pairs with the NodeToSurface or the " +
                                                     "SurfaceToSurface method, as the dialog does, not " + methodValue +
                                                     ". Change the method of a pair afterwards with " +
                                                     "contact_pairs.replace.");
                }
            }
            if (_masterSlaveItems.Count == 0) return new StringListAPI(new string[0]);
            //
            return new StringListAPI(_service.AddSearched(_masterSlaveItems, _pairType == ContactSearchPairTypeEnum.Tie,
                                                          adjustValue, _distance, surfaceInteractionName, methodValue));
        }
        public IEnumerator<FoundContactPairAPI> GetEnumerator()
        {
            return ((IEnumerable<FoundContactPairAPI>)_pairs).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _pairs.GetEnumerator();
        }
        public override string ToString()
        {
            return "ContactSearchResult(pair_type=" + StringListAPI.ToDisplayString(pair_type) + ", count=" +
                   _pairs.Length + ")";
        }
    }
}
