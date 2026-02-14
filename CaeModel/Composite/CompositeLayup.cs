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
    public enum OrientationDefinitionType
    {
        CoordinateSystem
    }

    [Serializable]
    public enum NormalDirectionType
    {
        Axis1,
        Axis2,
        Axis3
    }

    [Serializable]
    public class CompositeLayup : NamedClass
    {
        // Variables
        private int _initialPlyCount;
        private CompositeElementType _elementType;
        private OrientationDefinitionType _orientationDefinition;
        private NormalDirectionType _normalDirection;
        private List<Ply> _plies;


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

        public CompositeElementType ElementType
        {
            get { return _elementType; }
            set { _elementType = value; }
        }

        public OrientationDefinitionType OrientationDefinition
        {
            get { return _orientationDefinition; }
            set { _orientationDefinition = value; }
        }

        public NormalDirectionType NormalDirection
        {
            get { return _normalDirection; }
            set { _normalDirection = value; }
        }

        public List<Ply> Plies
        {
            get { return _plies; }
            set { _plies = value; }
        }


        // Constructors
        public CompositeLayup(string name, int initialPlyCount, CompositeElementType elementType)
            : base(name)
        {
            _initialPlyCount = initialPlyCount;
            _elementType = elementType;
            _orientationDefinition = OrientationDefinitionType.CoordinateSystem;
            _normalDirection = NormalDirectionType.Axis3;
            _plies = new List<Ply>();

            // Initialize plies based on initial ply count
            for (int i = 0; i < initialPlyCount; i++)
            {
                _plies.Add(new Ply("Ply-" + (i + 1)));
            }
        }


        // Methods
        public void AddPly(Ply ply)
        {
            _plies.Add(ply);
        }

        public void RemovePly(int index)
        {
            if (index >= 0 && index < _plies.Count)
            {
                _plies.RemoveAt(index);
            }
        }

        public void InsertPly(int index, Ply ply)
        {
            if (index >= 0 && index <= _plies.Count)
            {
                _plies.Insert(index, ply);
            }
        }
    }
}
