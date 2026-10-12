// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;


using PrePoMax.Python.Journal;

namespace PrePoMax.Commands
{
    [Serializable]
    internal sealed class OrderedDictionaryItemLayoutCommandData
    {
        private Type _itemType;
        private ViewGeometryModelResults _view;
        private string _parentName;
        private OrderedDictionaryItemLayout<string> _itemLayout;
        private string _details;
        [OptionalField]
        private Dictionary<Guid, string> _compoundPartGroupNames;


        public OrderedDictionaryItemLayoutCommandData(Type itemType, ViewGeometryModelResults view, string parentName,
                                                      OrderedDictionaryItemLayout<string> itemLayout,
                                                      Dictionary<Guid, string> compoundPartGroupNames, string details)
        {
            _itemType = itemType;
            _view = view;
            _parentName = parentName;
            _itemLayout = new OrderedDictionaryItemLayout<string>(itemLayout.Nodes);
            _compoundPartGroupNames = compoundPartGroupNames == null
                                      ? null : new Dictionary<Guid, string>(compoundPartGroupNames);
            _details = details;
        }


        public bool Execute(Controller receiver)
        {
            receiver.SetOrderedDictionaryItemLayout(_itemType, _view, _parentName, _itemLayout,
                                                    _compoundPartGroupNames);
            return true;
        }
        public string GetDetails()
        {
            return _details;
        }
        public void ToPython(PythonJournalWriter writer)
        {
            PythonTreeLayoutEmitter.Emit(writer, _view, _itemType, _parentName, _itemLayout, _compoundPartGroupNames);
        }
    }

    /// <summary>
    /// Stores one complete resulting layout for a geometry/model tree organization gesture.
    /// </summary>
    [Serializable]
    internal sealed class CSetOrderedDictionaryItemLayout : PreprocessCommand
    {
        private OrderedDictionaryItemLayoutCommandData _data;


        public CSetOrderedDictionaryItemLayout(string name, Type itemType, ViewGeometryModelResults view,
                                               string parentName, OrderedDictionaryItemLayout<string> itemLayout,
                                               Dictionary<Guid, string> compoundPartGroupNames, string details)
            : base(name)
        {
            _data = new OrderedDictionaryItemLayoutCommandData(itemType, view, parentName, itemLayout,
                                                               compoundPartGroupNames, details);
        }


        public override bool Execute(Controller receiver)
        {
            return _data.Execute(receiver);
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            _data.ToPython(writer);
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _data.GetDetails();
        }
    }

    /// <summary>
    /// Stores the geometry and model part layouts produced by one linked-group rename.
    /// Both layouts are replayed by one history command.
    /// </summary>
    [Serializable]
    internal sealed class CSetLinkedPartGroupLayouts : PreprocessCommand
    {
        private OrderedDictionaryItemLayout<string> _geometryLayout;
        private OrderedDictionaryItemLayout<string> _modelLayout;
        private string _details;
        [OptionalField]
        private Dictionary<Guid, string> _compoundPartGroupNames;


        public CSetLinkedPartGroupLayouts(string name, OrderedDictionaryItemLayout<string> geometryLayout,
                                          OrderedDictionaryItemLayout<string> modelLayout,
                                          Dictionary<Guid, string> compoundPartGroupNames, string details)
            : base(name)
        {
            _geometryLayout = new OrderedDictionaryItemLayout<string>(geometryLayout.Nodes);
            _modelLayout = new OrderedDictionaryItemLayout<string>(modelLayout.Nodes);
            _compoundPartGroupNames = compoundPartGroupNames == null
                                      ? null : new Dictionary<Guid, string>(compoundPartGroupNames);
            _details = details;
        }


        public override bool Execute(Controller receiver)
        {
            receiver.SetLinkedPartGroupLayouts(_geometryLayout, _modelLayout, _compoundPartGroupNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonTreeLayoutEmitter.Emit(writer, ViewGeometryModelResults.Geometry, typeof(CaeMesh.BasePart),
                                         null, _geometryLayout, _compoundPartGroupNames);
            PythonTreeLayoutEmitter.Emit(writer, ViewGeometryModelResults.Model, typeof(CaeMesh.BasePart),
                                         null, _modelLayout, _compoundPartGroupNames);
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _details;
        }
    }

    /// <summary>
    /// Results-tree counterpart required by the existing postprocessing replay classifier.
    /// </summary>
    [Serializable]
    internal sealed class CSetResultOrderedDictionaryItemLayout : PostprocessCommand
    {
        private OrderedDictionaryItemLayoutCommandData _data;


        public CSetResultOrderedDictionaryItemLayout(string name, Type itemType, ViewGeometryModelResults view,
                                                     string parentName,
                                                     OrderedDictionaryItemLayout<string> itemLayout, string details)
            : base(name)
        {
            _data = new OrderedDictionaryItemLayoutCommandData(itemType, view, parentName, itemLayout, null, details);
        }


        public override bool Execute(Controller receiver)
        {
            return _data.Execute(receiver);
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            _data.ToPython(writer);
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _data.GetDetails();
        }
    }
}
