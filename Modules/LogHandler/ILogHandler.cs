using System;
using System.Runtime.CompilerServices;
using Hazel;
using TownOfHost;

namespace TownOfHost.Modules
{
    public interface ILogHandler
    {
        public void Info(string text, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "");
        public void Warn(string text, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "");
        public void Error(string text, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "");
        public void Fatal(string text, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "");
        public void Msg(string text, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "");
        public void Exception(Exception ex, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "");
    }
}