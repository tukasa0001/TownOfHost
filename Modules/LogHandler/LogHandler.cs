using System;
using System.Runtime.CompilerServices;
using Hazel;
using TownOfHost;

namespace TownOfHost.Modules
{
    class LogHandler : ILogHandler
    {
        public string Tag { get; }
        public LogHandler(string tag)
        {
            Tag = tag;
        }

        public void Info(string text, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "")
            => Logger.Info(text, Tag, true, lineNumber, fileName);
        public void Warn(string text, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "")
            => Logger.Warn(text, Tag, true, lineNumber, fileName);
        public void Error(string text, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "")
            => Logger.Error(text, Tag, true, lineNumber, fileName);
        public void Fatal(string text, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "")
            => Logger.Fatal(text, Tag, true, lineNumber, fileName);
        public void Msg(string text, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "")
            => Logger.Msg(text, Tag, true, lineNumber, fileName);
        public void Exception(Exception ex, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "")
            => Logger.Exception(ex, Tag, lineNumber, fileName);
    }
}