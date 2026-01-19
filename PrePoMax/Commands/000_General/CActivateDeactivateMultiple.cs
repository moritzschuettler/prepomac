using CaeGlobals;
using CaeMesh;
using CaeModel;
using FastColoredTextBoxNS;
using PrePoMax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PrePoMax.Commands
{
    [Serializable]
    class CActivateDeactivateMultiple : PreprocessCommand
    {
        // Variables                                                                                                                
        private NamedClass[] _items;
        private string[] _itemNames;
        private Type[] _itemTypes;
        private bool _activate;
        private string[] _stepNames;


        // Constructor                                                                                                              
        public CActivateDeactivateMultiple(NamedClass[] items, bool activate, string[] stepNames)
            : base("Activate/deactivate multiple")
        {
            CloneItems(items);
            _activate = activate;
            _stepNames = stepNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            if (_itemNames == null) CloneItems(_items);     // compatibility v2.4.3
            receiver.ActivateDeactivateMultiple(DecloneItems(receiver), _activate, _stepNames);
            return true;
        }
        public override string GetCommandString()
        {
            if (_itemNames == null) CloneItems(_items);     // compatibility v2.4.3
            return base.GetCommandString() + GetArrayAsString(_itemNames);
        }
        public bool ContainsMeshSetupItem()
        {
            foreach (var item in _items)
            {
                if (item is MeshSetupItem) return true;
            }
            return false;
        }
        private void CloneItems(NamedClass[] items)
        {
            _items = new NamedClass[items.Length];
            _itemNames = new string[items.Length];
            _itemTypes = new Type[items.Length];
            //
            for (int i = 0; i < items.Length; i++)
            {
                _itemNames[i] = items[i].Name;
                //
                if (items[i] is IStoreAsNameInCommands) _itemTypes[i] = items[i].GetType();
                else _items[i] = items[i].DeepClone();
            }
        }
        private NamedClass[] DecloneItems(Controller receiver)
        {
            NamedClass[] items = new NamedClass[_items.Length];
            //
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i] == null)
                {
                    if (_itemTypes[i] == typeof(GeometryPart) || _itemTypes[i] == typeof(CompoundGeometryPart))
                        items[i] = receiver.GetGeometryPart(_itemNames[i]);
                    else if (_itemTypes[i] == typeof(MeshPart))
                        items[i] = receiver.GetModelPart(_itemNames[i]);
                    else if (_itemTypes[i] == typeof(ResultPart))
                        items[i] = receiver.GetResultPart(_itemNames[i]);
                    else throw new NotSupportedException();
                }
                else items[i] = _items[i].DeepClone();
            }
            return items;
        }
    }
}