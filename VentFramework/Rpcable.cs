using Hazel;

namespace VentFramework;

public abstract class RpcSendable<T>: IRpcWritable where T: RpcSendable<T>
{
    public abstract T Read(MessageReader reader);

    public abstract void Write(MessageWriter writer);
}