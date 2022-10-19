using InnerNet;
using UnhollowerBaseLib;

namespace TownOfHost
{
    public interface ICustomWriter
    {
        public void Write(float val);
        public void Write(string val);
        public void Write(ulong val);
        public void Write(int val);
        public void Write(uint val);
        public void Write(ushort val);
        public void Write(byte val);
        public void Write(sbyte val);
        public void Write(bool val);
        public void Write(Il2CppStructArray<byte> bytes);
        public void Write(Il2CppStructArray<byte> bytes, int offset, int length);
        public void WriteBytesAndSize(Il2CppStructArray<byte> bytes);
        public void WritePacked(int val);
        public void WritePacked(uint val);
        public void WriteNetObject(InnerNetObject obj);
    }
}