using Hazel;

namespace VentLib;

public interface IRpcWritable
{
    void Write(MessageWriter writer);
}