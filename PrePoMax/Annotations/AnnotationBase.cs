using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace PrePoMax
{
    [Serializable]
    public abstract class AnnotationBase : NamedClass
    {
        // Variables                                                                                                                
        protected int[] _partIds;
        protected string _overriddenText;
        //
        [NonSerialized] public static Controller Controller;


        // Properties                                                                                                               
        public int[] PartIds { get { return _partIds; } set { _partIds = value; } }
        public bool IsTextOverridden { get { return _overriddenText != null; } }
        public string OverriddenText { get { return _overriddenText; } set { _overriddenText = value; } }


        // Constructors                                                                                                             
        public AnnotationBase(string name)
            : base(name)
        {
            _partIds = null;   // always visible
            _overriddenText = null;
        }


        // Methods                                                                                                                  
        public bool IsAnnotationVisible()
        {
            if (this.Visible)
            {
                if (_partIds == null) return true;
                //
                CaeMesh.FeMesh mesh = Controller.DisplayedMesh;
                if (mesh != null)
                {
                    foreach (var partId in _partIds)
                    {
                        CaeMesh.BasePart part = mesh.GetPartFromId(partId);
                        if (part != null && part.Visible) return true;
                    }
                }
            }
            //
            return false;
        }
        //
        public abstract void GetAnnotationData(out string text, out double[] coor);
        public string GetAnnotationText()
        {
            GetAnnotationData(out string text, out _);
            return text;
        }
        public string GetNotOverriddenAnnotationText()
        {
            string tmp = _overriddenText;
            _overriddenText = null;
            //
            GetAnnotationData(out string text, out _);
            //
            _overriddenText = tmp;
            //
            return text;
        }
        //
        public void ApplyExplodedViewToPosition(Vec3D position)
        {
            if (Controller.IsExplodedViewActive())
            {
                if (_partIds != null && _partIds.Length > 0)
                {
                    CaeMesh.BasePart part = Controller.AllResults.CurrentResult.Mesh.GetPartFromId(_partIds[0]);
                    position.X += part.Offset[0];
                    position.Y += part.Offset[1];
                    position.Z += part.Offset[2];
                }
            }
        }

    }
}
