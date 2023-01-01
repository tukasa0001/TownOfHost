#nullable enable
using System;

namespace TownOfHost.Addons;

public class AddonException : Exception
{
    public AddonException(string? message, Exception? innerException) : base(message, innerException)
    { }
}