using System;
using System.ComponentModel;
using CaeGlobals;
using PrePoMax.Commands;

namespace PrePoMax.Settings
{
    [Serializable]
    public class ViewCommand
    {
        // Variables                                                                                                                
        private int _id;
        private Command _command;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Id")]
        [DescriptionAttribute("The id of the command.")]
        public int Id { get { return _id; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Date/Time")]
        [DescriptionAttribute("The date/time of the command creation.")]
        public string DateTime { get { return _command.GetDateTime(); } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Name")]
        [DescriptionAttribute("The name of the command.")]
        public string Name { get { return _command.Name; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(3, 10, "Type")]
        [DescriptionAttribute("The type of the command.")]
        public string Type
        {
            get
            {
                if (_command is IFileCommand) return "File";
                else if (_command is PreprocessCommand) return "Pre-process";
                else if (_command is AnalysisCommand) return "Analysis";
                else if (_command is PostprocessCommand) return "Post-process";
                else throw new NotSupportedException();
            }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(4, 10, "Data")]
        [DescriptionAttribute("Data of the command.")]
        public string Data { get { return _command.GetCommandString().Remove(0, _command.GetBaseCommandString().Length); } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(5, 10, "Time [s]")]
        [DescriptionAttribute("Execution time of the command.")]
        public string ExecutionTimeString
        {
            get
            {
                string time = ExecutionTime.ToString();
                string[] tmp = time.Split('.');
                if (tmp.Length == 2)
                {
                    for (int i = 0; i < 4 - tmp[1].Length; i++) tmp[1] += "0";
                    time = tmp[0] + "." + tmp[1];
                }
                else
                {
                    time = tmp[0] + ".0000";
                }
                //
                return time;
            }
        }
        //
        [Browsable(false)]
        public double ExecutionTime { get { return Math.Round(_command.TimeSpan.TotalSeconds, 4); } }
        [Browsable(false)]
        public Command Command { get { return _command; } }


        // Constructors                                                                                                             
        public ViewCommand(int id, Command command)
        {
            _id = id;
            _command = command;
        }
    }

}
