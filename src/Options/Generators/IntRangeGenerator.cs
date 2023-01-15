using System;
using System.Collections.Generic;

namespace TownOfHost.Options.Generators;

public class IntRangeGenerator: IRangeGenerator
{
    private int start;
    private int end;
    private int step;

    public IntRangeGenerator(int start, int end, int step = 1)
    {
        this.start = start;
        this.end = end;
        this.step = step;
    }

    public IEnumerable<object?> GetRange()
    {
        List<object?> values = new();
        for (int i = start; i <= end; i += step) values.Add(Convert.ToInt32(Math.Round(Convert.ToDecimal(i), 2)));
        return values;
    }
}