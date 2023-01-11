using Hazel;

namespace VentLib.Interfaces;

public interface IRpcWritable
{
    void Write(MessageWriter writer);
}