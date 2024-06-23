using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeResults
{
    [Serializable]
    public enum HistoryResultFilterTypeEnum
    {
        None,
        Minumun,
        Maximum,
        Average,
        Sum
    }
    //
    [Serializable]
    public class HistoryResultFilter
    {
        // Variables                                                                                                                
        public HistoryResultFilterTypeEnum _type;
        public string _option;


        // Properties                                                                                                               
        public HistoryResultFilterTypeEnum Type { get { return _type; } set { _type = value; } }
        public string Option { get { return _option; } set { _option = value; } }


        // Constructors                                                                                                             
        public HistoryResultFilter()
        {
            _type = HistoryResultFilterTypeEnum.None;
            _option = null;
        }
    }
}
