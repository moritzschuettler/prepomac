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
        [OrderedDisplayName(3, 10, "Data")]
        [DescriptionAttribute("Data of the command.")]
        public string Data { get { return _command.GetCommandString().Remove(0, _command.GetBaseCommandString().Length); } }
        //
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
