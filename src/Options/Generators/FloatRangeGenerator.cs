using System;
using System.Collections.Generic;
using TownOfHost.Extensions;

namespace TownOfHost.ReduxOptions;

public class FloatRangeGenerator: IRangeGenerator
{
    private float start;
    private float end;
    private float step;

    public FloatRangeGenerator(float start, float end, float step = 1)
    {
        this.start = start;
        this.end = end;
        this.step = step;
    }

    public IEnumerable<object> GetRange()
    {
        List<object> values = new();
        for (float i = start; i <= end; i += step) values.Add(Convert.ToSingle(Math.Round(Convert.ToDecimal(i), 2)));
        return values;
    }
}