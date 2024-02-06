using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class MassSection : Section, ISerializable
    {
        // Variables                                                                                                                
        private EquationContainer _mass;            //ISerializable
        [NonSerialized]
        public string ElementSetName;               // used only for temporary storage while exporting to Calculix .inp


        // Properties                                                                                                               
        public EquationContainer Mass { get { return _mass; } set { SetMass(value); } }


        // Constructors                                                                                                             
        public MassSection(string name, string regionName, RegionTypeEnum regionType, double mass, bool twoD)
            : base(name, null, regionName, regionType, 1, twoD)
        {
            // The constructor must work with m = 0
            SetMass(new EquationContainer(typeof(StringMassConverter), mass), false);
        }
        public MassSection(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_mass":
                        SetMass((EquationContainer)entry.Value, false);
                        break;
                    default:
                        break;
                }
            }
        }

        // Methods                                                                                                                  
        private void SetMass(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _mass, value, CheckPositive, checkEquation);
        }
        private double CheckPositive(double value)
        {
            if (value < 0) throw new Exception("Value of the mass must be larger or equal to zero.");
            else return value;
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _mass.CheckEquation();
        }
        public override bool TryCheckEquations()
        {
            try
            {
                base.CheckEquations();
                //
                CheckEquations();
                return true;
            }
            catch (Exception ex) { return false; }
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_mass", _mass, typeof(EquationContainer));
        }
    }
}