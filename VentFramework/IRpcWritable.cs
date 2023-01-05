using Hazel;

namespace VentFramework;

public interface IRpcWritable
{
    void Write(MessageWriter writer);
}