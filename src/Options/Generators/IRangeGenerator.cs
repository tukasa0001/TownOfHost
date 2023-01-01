using System.Collections.Generic;

namespace TownOfHost.ReduxOptions;

public interface IRangeGenerator
{
    IEnumerable<object> GetRange();
}