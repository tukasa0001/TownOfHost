using System;
using System.Collections.Generic;

namespace TOHTOR.Extensions;

public static class RangeExtensions
{
    public static IEnumerable<int> ToEnumerator(this Range range)
    {
        List<int> ints = new();
        int start = range.Start.Value;
        int end = range.End.Value;
        if (end > start)
            for (int i = start; i <= end; i++) ints.Add(i);
        else
            for (int i = end; i <= start; i++) ints.Add(i);
        return ints;
    }
}