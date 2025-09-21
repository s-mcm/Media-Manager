using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaManager.Logging
{
    public class ConsoleLogger : ILogger
    {
        private readonly LogLevel currentLevel;

        public ConsoleLogger(LogLevel level)
        {
            currentLevel = level;
        }

        public void Log(string message, LogLevel level)
        {
            if (level <= currentLevel)
            {
                Console.WriteLine($"[{level}] {message}");
            }
        }
    }
}
