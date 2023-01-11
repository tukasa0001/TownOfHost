using Hazel;

namespace VentLib.Interfaces;

public interface IRpcSendable<out T>: IRpcWritable where T: IRpcSendable<T>
{
    T Read(MessageReader reader);
}