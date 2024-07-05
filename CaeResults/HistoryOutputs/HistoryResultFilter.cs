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
        Minimum,
        Maximum,
        Sum,
        Average
    }
    //
    [Serializable]
    public class HistoryResultFilter
    {
        // Variables                                                                                                                
        public static string Row = "Row";
        public static string Column = "Column";
        public static string Rows = "Rows";
        public static string Columns = "Columns";
        //
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
