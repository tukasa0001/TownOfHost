using System;
using Hazel;
using InnerNet;
using UnhollowerBaseLib;

namespace TownOfHost
{
    public class WriterProxy : ICustomWriter
    {
        private MessageWriter writer;
        private WriterProxy() { }

        public WriterProxy(MessageWriter writer)
        {
            this.writer = writer;
        }

        public void Write(float val) => writer.Write(val);
        public void Write(string val) => writer.Write(val);
        public void Write(ulong val) => writer.Write(val);
        public void Write(int val) => writer.Write(val);
        public void Write(uint val) => writer.Write(val);
        public void Write(ushort val) => writer.Write(val);
        public void Write(byte val) => writer.Write(val);
        public void Write(sbyte val) => writer.Write(val);
        public void Write(bool val) => writer.Write(val);
        public void Write(Il2CppStructArray<byte> bytes) => writer.Write(bytes);
        public void Write(Il2CppStructArray<byte> bytes, int offset, int length) => writer.Write(bytes, offset, length);
        public void WriteBytesAndSize(Il2CppStructArray<byte> bytes) => writer.WriteBytesAndSize(bytes);
        public void WritePacked(int val) => writer.WritePacked(val);
        public void WritePacked(uint val) => writer.WritePacked(val);
        public void WriteNetObject(InnerNetObject obj) => writer.WriteNetObject(obj);
    }
}