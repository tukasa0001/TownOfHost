using System;
using System.Collections.Generic;

namespace VentWork;

public interface IRpcInstance
{
    public static readonly Dictionary<Type, IRpcInstance> Instances = new();


    void EnableInstance()
    {
        Instances.Add(this.GetType(), this);
    }
}