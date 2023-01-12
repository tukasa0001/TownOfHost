using System;
using System.Runtime.CompilerServices;
using Hazel;
using TownOfHost;

namespace TownOfHost.Modules
{
    public interface ILogHandler
    {
        public void Info(string text);
        public void Warn(string text);
        public void Error(string text);
        public void Fatal(string text);
        public void Msg(string text);
        public void Exception(Exception ex);
    }
}