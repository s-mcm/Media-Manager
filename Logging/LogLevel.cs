using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaManager.Logging
{
    public enum LogLevel
    {
        Error = 1,       // Only errors
        Info = 2,    // Errors + info messages
        Verbose = 3           // Everything including debug/verbose messages
    }
}
