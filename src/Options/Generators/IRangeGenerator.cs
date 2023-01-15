using System.Collections.Generic;

namespace TownOfHost.Options.Generators;

public interface IRangeGenerator
{
    IEnumerable<object?> GetRange();
}