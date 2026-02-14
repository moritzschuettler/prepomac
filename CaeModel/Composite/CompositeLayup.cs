using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public enum CompositeElementType
{
        ConventionalShell,
        ContinuumShell,
        Solid
    }

    [Serializable]
    public class CompositeLayup : NamedClass
    {
        // Variables    
    private int _initialPlyCount;
        private CompositeElementType _elementType;


        // Properties      
        public int InitialPlyCount 
        { 
  get { return _initialPlyCount; } 
            set 
          { 
             if (value < 1) throw new CaeException("Initial ply count must be at least 1.");
            _initialPlyCount = value; 
            } 
 }
        public CompositeElementType ElementType { get { return _elementType; } set { _elementType = value; } }


        // Constructors         
        public CompositeLayup(string name, int initialPlyCount, CompositeElementType elementType)
    : base(name)
        {
   _initialPlyCount = initialPlyCount;
      _elementType = elementType;
        }


        // Methods     
    }
}
