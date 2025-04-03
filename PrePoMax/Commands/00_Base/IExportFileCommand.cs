using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using CaeGlobals;
using System.Runtime.Serialization;

namespace PrePoMax.Commands
{
    public interface IExportFileCommand : IFileCommand
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        string FileName { get; set; }


        // Constructors                                                                                                             


        // Methods                                                                                                                  
    }
}
