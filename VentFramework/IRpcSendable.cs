using Hazel;

namespace VentFramework;

public interface IRpcSendable<out T>: IRpcWritable where T: IRpcSendable<T>
{
    T Read(MessageReader reader);
}