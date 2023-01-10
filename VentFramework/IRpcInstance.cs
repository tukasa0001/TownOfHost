using System;
using System.Collections.Generic;

namespace VentLib;

public interface IRpcInstance
{
    public static readonly Dictionary<Type, IRpcInstance> Instances = new();


    void EnableInstance()
    {
        Instances.Add(this.GetType(), this);
    }
}