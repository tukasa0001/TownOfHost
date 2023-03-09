#nullable enable
using System;

namespace TOHTOR.Addons;

public class AddonException : Exception
{
    public AddonException(string? message, Exception? innerException) : base(message, innerException)
    { }
}