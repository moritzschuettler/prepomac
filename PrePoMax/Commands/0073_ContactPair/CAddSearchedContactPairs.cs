// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;
using CaeMesh;
using CaeModel;
using PrePoMax.Python.Journal;


namespace PrePoMax.Commands
{
    // The ties, contact pairs and surfaces of unresolved ties one OK of the Search Contact Pairs dialog adds. It keeps
    // what the search found, not the search, so a regeneration adds the same items under the same names
    [Serializable]
    class CAddSearchedContactPairs : PreprocessCommand
    {
        // Variables                                                                                                                
        private NamedClass[] _items;


        // Constructor                                                                                                              
        public CAddSearchedContactPairs(NamedClass[] items)
            : base("Add searched contact pairs")
        {
            _items = new NamedClass[items.Length];
            for (int i = 0; i < items.Length; i++) _items[i] = items[i].DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            NamedClass[] items = new NamedClass[_items.Length];
            for (int i = 0; i < items.Length; i++) items[i] = _items[i].DeepClone();
            receiver.AddSearchedContactPairs(items);
            return true;
        }
        // The journal writes every item as the call that adds it on its own
        public override void ToPython(PythonJournalWriter writer)
        {
            foreach (NamedClass item in _items)
            {
                if (item is FeSurface surface) new CAddSurface(surface, false).ToPython(writer);
                else if (item is Constraint constraint) new CAddConstraint(constraint, false).ToPython(writer);
                else if (item is ContactPair contactPair) new CAddContactPair(contactPair, false).ToPython(writer);
                else writer.Unsupported(this, "a searched " + item.GetType().Name + " has no API form");
            }
        }
        public override string GetCommandString()
        {
            int numOfSurfaces = 0;
            int numOfConstraints = 0;
            int numOfContactPairs = 0;
            foreach (NamedClass item in _items)
            {
                if (item is FeSurface) numOfSurfaces++;
                else if (item is Constraint) numOfConstraints++;
                else if (item is ContactPair) numOfContactPairs++;
            }
            return base.GetCommandString() + "Ties: " + numOfConstraints + ", contact pairs: " + numOfContactPairs +
                   ", surfaces: " + numOfSurfaces;
        }
    }
}
