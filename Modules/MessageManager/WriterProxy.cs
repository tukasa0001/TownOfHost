using System;
using Hazel;
using InnerNet;
using UnhollowerBaseLib;

namespace TownOfHost
{
    public class WriterProxy
    {
        private MessageWriter writer;
        private WriterProxy() { }

        public WriterProxy(MessageWriter writer)
        {
            this.writer = writer;
        }
    }
}