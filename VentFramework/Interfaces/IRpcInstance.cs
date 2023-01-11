using System;
using System.Collections.Generic;

namespace VentLib.Interfaces;

public interface IRpcInstance
{
    public static readonly Dictionary<Type, IRpcInstance> Instances = new();


    void EnableInstance()
    {
        Instances.Add(this.GetType(), this);
    }
}