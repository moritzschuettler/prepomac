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
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;
using PrePoMax.Python.Journal;


namespace PrePoMax.Commands
{
    [Serializable]
    class CAddElementSet : PreprocessCommand
    {
        // Variables                                                                                                                
        private FeElementSet _elementSet;


        // Constructor                                                                                                              
        public CAddElementSet(FeElementSet elementSet)
            : base("Add element set")
        {
            _elementSet = elementSet.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddElementSet(_elementSet.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            if (_elementSet.CreationData != null)
            {
                PythonModelItemEmitter.AddFromSelection(writer, "pmx.model.element_sets", _elementSet,
                                                        _elementSet.CreationData);
                return;
            }
            // A set the application made from element IDs is rebuilt from them. The set of the elements a run found
            // without a section is made again by the replayed run when it reaches the check, so it is added only
            // when that run did not make it, and the commands naming it replay either way.
            Selection selection = new Selection();
            selection.CurrentView = (int)ViewGeometryModelResults.Model;
            selection.SelectItem = vtkSelectItem.Element;
            selection.Add(new SelectionNodeIds(vtkSelectOperation.None, false, _elementSet.Labels));
            string selectionVariable = PythonSelectionEmitter.Emit(writer, selection);
            string name = PythonValueFormatter.Text(_elementSet.Name);
            string add = writer.Call("pmx.model.element_sets", "add", name, selectionVariable);
            if (_elementSet.Name.StartsWith(Globals.MissingSectionName))
            {
                writer.Line("if not pmx.model.element_sets.contains(" + name + "):");
                writer.Line("    " + add);
            }
            else writer.Statement(add);
            PythonModelItemEmitter.EmitState(writer, "pmx.model.element_sets", _elementSet);
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _elementSet.ToString();
        }
    }
}
