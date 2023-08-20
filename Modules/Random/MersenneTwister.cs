// Copyright(c) 2015 vpmedia
// Released under the MIT license

// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:

// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;

namespace TownOfHost;

public class MersenneTwister : IRandom
{
    // 参考元
    public const string REFERENCE_HOMEPAGE = "http://www.math.sci.hiroshima-u.ac.jp/m-mat/MT/mt.html";
    public const string REFERENCE_SOURCE_CODE = "https://github.com/vpmedia/template-unity/blob/master/Framework/Assets/Frameworks/URandom/MersenneTwister.cs";

    public MersenneTwister() : this((Int32)DateTime.UtcNow.Ticks) { }
    public MersenneTwister(Int32 seed)
    {
        Init((UInt32)seed);
    }

    /// <summary>
    /// 数値の上限を設定
    /// これより下の値の一部は参考元のソースより拝借
    /// </summary>
    private const Int32 N = 624;
    private const Int32 M = 397;
    private const UInt32 MatrixA = 0x9908b0df;
    private const UInt32 UpperMask = 0x80000000;
    private const UInt32 LowerMask = 0x7fffffff;
    private const UInt32 TemperingMaskB = 0x9d2c5680;
    private const UInt32 TemperingMaskC = 0xefc60000;

    private static UInt32 ShiftU(UInt32 y)
    {
        return y >> 11;
    }

    private static UInt32 ShiftS(UInt32 y)
    {
        return y << 7;
    }

    private static UInt32 ShiftT(UInt32 y)
    {
        return y << 15;
    }

    private static UInt32 ShiftL(UInt32 y)
    {
        return y >> 18;
    }

    private readonly UInt32[] _mt = new UInt32[N];
    private Int16 _mtItems;
    private readonly UInt32[] _mag01 = { 0x0, MatrixA };

    private void Init(UInt32 seed)
    {
        _mt[0] = seed & 0xffffffffU;

        for (_mtItems = 1; _mtItems < N; _mtItems++)
        {
            _mt[_mtItems] = (uint)(1812433253U * (_mt[_mtItems - 1] ^ (_mt[_mtItems - 1] >> 30)) + _mtItems);
            _mt[_mtItems] &= 0xffffffffU;
        }
    }

    public uint Next()
    {
        UInt32 y;

        /* _mag01[x] = x * MatrixA  for x=0,1 */
        if (_mtItems >= N) /* generate N words at one time */
        {
            Int16 kk = 0;

            for (; kk < N - M; ++kk)
            {
                y = (_mt[kk] & UpperMask) | (_mt[kk + 1] & LowerMask);
                _mt[kk] = _mt[kk + M] ^ (y >> 1) ^ _mag01[y & 0x1];
            }

            for (; kk < N - 1; ++kk)
            {
                y = (_mt[kk] & UpperMask) | (_mt[kk + 1] & LowerMask);
                _mt[kk] = _mt[kk + (M - N)] ^ (y >> 1) ^ _mag01[y & 0x1];
            }

            y = (_mt[N - 1] & UpperMask) | (_mt[0] & LowerMask);
            _mt[N - 1] = _mt[M - 1] ^ (y >> 1) ^ _mag01[y & 0x1];

            _mtItems = 0;
        }

        y = _mt[_mtItems++];
        y ^= ShiftU(y);
        y ^= ShiftS(y) & TemperingMaskB;
        y ^= ShiftT(y) & TemperingMaskC;
        y ^= ShiftL(y);

        return y;
    }

    public int Next(int minValue, int maxValue)
    {
        if (minValue < 0) throw new ArgumentOutOfRangeException(nameof(minValue), "minValue must be bigger than 0.");
        else if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be bigger than 0.");
        else if (minValue > maxValue) throw new ArgumentException("maxValue must be bigger than minValue.");
        else if (minValue == maxValue) return minValue;

        return (int)(minValue + (Next() % (maxValue - minValue)));
    }

    public int Next(int maxValue) => Next(0, maxValue);
}