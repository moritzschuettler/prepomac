using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;


namespace PrePoMax.Commands
{
    [Serializable]
    class CRenamePost : PostprocessCommand
    {
        // Variables                                                                                                                
        private Type _itemType;
        private string _itemName;
        private string _newName;
        private string _stepName;
        private ViewGeometryModelResults _view;


        // Variables                                                                                                                
        public ViewGeometryModelResults View { get { return _view; } }


        // Constructor                                                                                                              
        public CRenamePost(Type itemType, string itemName, string newName, string stepName, ViewGeometryModelResults view)
            : base("Rename")
        {
            _itemType = itemType;
            _itemName = itemName;
            _newName = newName;
            _stepName = stepName;
            _view = view;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.Rename(_itemType, _itemName, _newName, _stepName, _view);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _itemName + " to " + _newName;
        }
    }
}
