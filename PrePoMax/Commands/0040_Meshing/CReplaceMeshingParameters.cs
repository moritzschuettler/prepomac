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
    class CReplaceMeshSetupItem : PreprocessCommand, ICommandWithDialog
    {
        // Variables                                                                                                                
        private string _oldMeshSetupItemName;
        private MeshSetupItem _meshSetupItem;


        // Constructor                                                                                                              
        public CReplaceMeshSetupItem(string oldMeshSetupItemName, MeshSetupItem newMeshSetupItem)
            : base("Edit mesh setup item")
        {
            _oldMeshSetupItemName = oldMeshSetupItemName;
            _meshSetupItem = newMeshSetupItem.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceMeshSetupItem(_oldMeshSetupItemName, _meshSetupItem.DeepClone());
            return true;
        }
        // ICommandWithDialog
        public bool ExecuteWithDialog(Controller receiver)
        {
            if (_meshSetupItem is MeshSetupItem msi) _meshSetupItem = receiver.EditMeshSetupItemByForm(msi.DeepClone());
            else throw new NotSupportedException("MeshSetupItemTypeException");
            //
            return Execute(receiver);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldMeshSetupItemName + ", " + _meshSetupItem.ToString();
        }
    }
}
