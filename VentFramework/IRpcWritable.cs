using Hazel;

namespace VentWork;

public interface IRpcWritable
{
    void Write(MessageWriter writer);
}